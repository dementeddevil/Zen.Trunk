namespace Zen.Trunk.Torrent.Dht
{
	using System;
	using System.Security.Cryptography;
	using Zen.Trunk.Torrent.Bencoding;

	internal class TokenManager
	{
		private byte[] secret;
		private byte[] previousSecret;
		private DateTime LastSecretGeneration;
		private RandomNumberGenerator random;
		private SHA1 sha1;
		private TimeSpan timeout = TimeSpan.FromMinutes(5);

		internal TimeSpan Timeout
		{
			get
			{
				return timeout;
			}
			set
			{
				timeout = value;
			}
		}

		public TokenManager()
		{
			sha1 = SHA1.Create();
			random = RandomNumberGenerator.Create();
			LastSecretGeneration = DateTime.MinValue; //in order to force the update
			secret = new byte[10];
			previousSecret = new byte[10];
			random.GetNonZeroBytes(secret);
			random.GetNonZeroBytes(previousSecret);
		}
		public BEncodedString GenerateToken(Node node)
		{
			return GetToken(node, secret);
		}

		public bool VerifyToken(Node node, BEncodedString token)
		{
			return (token.Equals(GetToken(node, secret)) || token.Equals(GetToken(node, previousSecret)));
		}

		private BEncodedString GetToken(Node node, byte[] s)
		{
			//refresh secret needed
			if (LastSecretGeneration.Add(timeout) < DateTime.UtcNow)
			{
				LastSecretGeneration = DateTime.UtcNow;
				secret.CopyTo(previousSecret, 0);
				random.GetNonZeroBytes(secret);
			}

			byte[] n = node.CompactPort().TextBytes;
			sha1.Initialize();
			sha1.TransformBlock(n, 0, n.Length, n, 0);
			sha1.TransformFinalBlock(s, 0, s.Length);

			return (BEncodedString)sha1.Hash;
		}
	}
}
