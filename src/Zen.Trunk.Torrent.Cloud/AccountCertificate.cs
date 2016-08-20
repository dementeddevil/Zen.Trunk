namespace Zen.Trunk.Torrent.Cloud
{
	using System;
	using System.IO;
	using System.Linq;
	using System.Security;
	using System.Security.Cryptography;
	using System.Text;
	using System.Threading;
	using Zen.Certification;
	using Zen.Trunk.Torrent.Repository;

	public class AccountCertificate
	{
		private Guid[] _validNonces;

		public string GenerateCertificatePassword(Guid torrentAccountId)
		{
			// TODO: Use cryptographic random number generator
			Random rng = new Random(DateTime.UtcNow.Millisecond);
			StringBuilder sb = new StringBuilder();
			for (int index = 0; index < 15; ++index)
			{
				if ((index % 4) == 3)
				{
					sb.Append('-');
				}
				else
				{
					sb.Append('0' + rng.Next(10));
				}
			}
			string password = sb.ToString();

			using (var context = new TorrentModelContainer())
			{
				// Generate certificate
				Certificate certGen = new Certificate(null);
				byte[] certPayload = certGen.CreateSelfSignedCertificate(
					string.Format("CN=\"{0:N}\",OU=\"Torrent Account\",OU=\"Torrent Cloud\",O=\"Zen Design Corp\",C=\"TH\"", torrentAccountId),
					DateTime.UtcNow,
					DateTime.UtcNow.AddYears(1),
					SecureStringFromString(password),
					2048);

				// Find or create account record
				TorrentAccountCertificate cert = context
					.TorrentAccountCertificates
					.FirstOrDefault((item) => item.TorrentAccountId == torrentAccountId);
				if (cert == null)
				{
					cert = new TorrentAccountCertificate
						{
							TorrentAccountId = torrentAccountId,
						};
					context.TorrentAccountCertificates.Add(cert);
				}

				// Update information
				cert.PasswordAttempts = 0;
				cert.OneTimePassword = GeneratePasswordHash(password);
				cert.CertificatePayload = certPayload;

				// Save changes
				context.SaveChanges();

				// Return our generated password
				return password;
			}
		}

		public byte[] GetCertificate(Guid torrentAccountId, string password)
		{
			using (var context = new TorrentModelContainer())
			{
				TorrentAccountCertificate cert = context
					.TorrentAccountCertificates
					.FirstOrDefault((item) => item.TorrentAccountId == torrentAccountId);
				if (cert != null &&
					cert.PasswordAttempts < 10 &&
					ValidatePasswordHash(password, cert.OneTimePassword))
				{
					return cert.CertificatePayload;
				}

				// If we found a certificate then increment the number of
				//	password attempts
				if (cert != null)
				{
					++cert.PasswordAttempts;

					// TODO: If this value passes 10 then we need to lockout
					//	the associated torrent account and force user to go
					//	through account verification process again.

					context.SaveChanges();
				}

				// Sleep for a random amount of time before throwing exception
				//	to invalidate attempts to profile this method.
				Random rng = new Random(DateTime.UtcNow.Millisecond);
				Thread.Sleep(rng.Next(1000));
				throw new ArgumentException("Torrent account not found or locked-out, password incorrect or expired.");
			}
		}

		//public Guid GetTorrentAccountIdFromCertificateThumbprint(byte[] thumbprint)
		//{
		//}

		private string GeneratePasswordHash(string password)
		{
			return GeneratePasswordHash(password, GetLatestNonce());
		}

		private bool ValidatePasswordHash(string password, string hash)
		{
			foreach (var nonce in _validNonces)
			{
				if (hash == GeneratePasswordHash(password, nonce))
				{
					return true;
				}
			}
			return false;
		}

		private SecureString SecureStringFromString(string password)
		{
			SecureString secure = new SecureString();
			foreach (var character in password)
			{
				secure.AppendChar(character);
			}
			return secure;
		}

		private string GeneratePasswordHash(string password, Guid nonce)
		{
			// Generate hashing algorithm
			using (var des = TripleDESCryptoServiceProvider.Create())
			{
				using (var hasher = SHA512Managed.Create())
				{
					// Setup key
					// TODO: Determine suitable IV value
					// TODO: Determine whether we need a longer key
					// TODO: Our nonce should be our IV and our encryption key
					//	should come from somewhere else...
					//	Perhaps our service certificate private key??
					des.Key = nonce.ToByteArray();

					MemoryStream resultStream = new MemoryStream();
					CryptoStream base64Stream =
						new CryptoStream(
							resultStream,
							new ToBase64Transform(),
							CryptoStreamMode.Write);
					CryptoStream hashStream =
						new CryptoStream(
							base64Stream,
							hasher,
							CryptoStreamMode.Write);
					CryptoStream encryptStream =
						new CryptoStream(
							hashStream,
							des.CreateEncryptor(),
							CryptoStreamMode.Write);

					byte[] passwordBytes = Encoding.Unicode.GetBytes(password);
					encryptStream.Write(passwordBytes, 0, passwordBytes.Length);
					encryptStream.Flush();
					encryptStream.FlushFinalBlock();

					resultStream.Position = 0;
					StreamReader reader = new StreamReader(resultStream, true);
					return reader.ReadToEnd();
				}
			}
		}

		private Guid GetLatestNonce()
		{
			// TODO: Cache this value for 15 minutes to reduce possibility of DoS
			return GenerateNonce(TimeSpan.FromHours(6), 3);
		}

		private Guid GenerateNonce(TimeSpan minimumTimeSinceLastNonce, int maximumSecurityNonces)
		{
			using (var context = new TorrentModelContainer())
			{
				DateTime utcNow = DateTime.UtcNow;
				DateTime minAge = utcNow - minimumTimeSinceLastNonce;
				var latestNonce = context
					.SecurityNonces
					.OrderByDescending((item) => item.CreatedDate)
					.FirstOrDefault();
				if (latestNonce == null || latestNonce.CreatedDate < minAge)
				{
					latestNonce =
						new SecurityNonce
						{
							SecurityNonceId = Guid.NewGuid(),
							CreatedDate = utcNow
						};
					context.SecurityNonces.Add(latestNonce);
					context.SaveChanges();
				}

				// Update array of valid nonces
				_validNonces = context
					.SecurityNonces
					.OrderByDescending((item) => item.CreatedDate)
					.Take(maximumSecurityNonces)
					.Select((item) => item.SecurityNonceId)
					.ToArray();

				// Discard oldest nonces to maintain maximum number of valid nonces
				if (context.SecurityNonces.Count() > maximumSecurityNonces)
				{
					foreach (var nonce in context.SecurityNonces.ToArray())
					{
						if (!_validNonces.Contains(nonce.SecurityNonceId))
						{
							context.SecurityNonces.Remove(nonce);
						}
					}
					context.SaveChanges();
				}

				return latestNonce.SecurityNonceId;
			}
		}
	}
}
