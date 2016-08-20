namespace Zen.Trunk.Torrent.Client.Encryption
{
	using System;
	using System.Text;
	using System.Threading.Tasks;
	using Zen.Trunk.Torrent.Client.Messages;
	using Zen.Trunk.Torrent.Common;

	/// <summary>
	/// Class to handle message stream encryption for receiving connections
	/// </summary>
	public class PeerBEncryption : EncryptedSocket
	{
		private byte[][] _possibleSKEYs = null;
		private byte[] _verifyBytes;
		private byte[] _b;

		public PeerBEncryption(byte[][] possibleSKEYs, EncryptionTypes allowedEncryption)
			: base(allowedEncryption)
		{
			this._possibleSKEYs = possibleSKEYs;
		}

		protected override async Task ReceiveY()
		{
			try
			{
				// 2 B->A: Diffie Hellman Yb, PadB
				await base.ReceiveY();

				byte[] req1 = Hash(Encoding.ASCII.GetBytes("req1"), S);
				await Synchronize(req1, 628); // 3 A->B: HASH('req1', S)
			}
			catch (EncryptionException)
			{
				throw;
			}
			catch (Exception ex)
			{
				throw new EncryptionException("Handshake->PeerB->ReceiveY exception", ex);
			}
		}

		protected override async Task Synchronize(byte[] syncData, int syncStopPoint)
		{
			try
			{
				await base.Synchronize(syncData, syncStopPoint);

				// Get verification message
				_verifyBytes = new byte[20 + VerificationConstant.Length + 4 + 2]; // ... HASH('req2', SKEY) xor HASH('req3', S), ENCRYPT(VC, crypto_provide, len(PadC), PadC, len(IA))
				await ReceiveMessage(_verifyBytes, _verifyBytes.Length);

				// Does torrent hash match?
				byte[] torrentHash = new byte[20];
				Array.Copy(_verifyBytes, 0, torrentHash, 0, torrentHash.Length); // HASH('req2', SKEY) xor HASH('req3', S)
				if (!MatchSKEY(torrentHash))
				{
					throw new EncryptionException("No valid SKey found");
				}

				// Create crytocraphic keys
				CreateCryptors("keyB", "keyA");
				DoDecrypt(_verifyBytes, 20, 14); // ENCRYPT(VC, ...

				// Validate verification constant
				byte[] myVC = new byte[8];
				Array.Copy(_verifyBytes, 20, myVC, 0, myVC.Length);
				if (!Toolbox.ByteMatch(myVC, VerificationConstant))
				{
					throw new EncryptionException("Verification constant was invalid");
				}

				// Get the crypto-provider to use
				byte[] myCP = new byte[4];
				Array.Copy(_verifyBytes, 28, myCP, 0, myCP.Length); // ...crypto_provide ...

				// We need to select the crypto *after* we send our response, otherwise the wrong
				// encryption will be used on the response
				_b = myCP;
				byte[] lenPadC = new byte[2];
				Array.Copy(_verifyBytes, 32, lenPadC, 0, lenPadC.Length); // ... len(padC) ...

				PadC = new byte[DeLen(lenPadC) + 2];
				await ReceiveMessage(PadC, PadC.Length); // padC
				DoDecrypt(PadC, 0, PadC.Length);

				byte[] lenInitialPayload = new byte[2]; // ... len(IA))
				Array.Copy(PadC, PadC.Length - 2, lenInitialPayload, 0, 2);

				RemoteInitialPayload = new byte[DeLen(lenInitialPayload)]; // ... ENCRYPT(IA)
				await ReceiveMessage(RemoteInitialPayload, RemoteInitialPayload.Length);
				DoDecrypt(RemoteInitialPayload, 0, RemoteInitialPayload.Length); // ... ENCRYPT(IA)

				await StepFour();
			}
			catch (EncryptionException)
			{
				throw;
			}
			catch (Exception ex)
			{
				throw new EncryptionException("Handshake->PeerB->Synchronise exception", ex);
			}
		}

		private async Task StepFour()
		{
			try
			{
				byte[] padD = GeneratePad();
				SelectCrypto(_b, false);
				// 4 B->A: ENCRYPT(VC, crypto_select, len(padD), padD)
				byte[] buffer = new byte[VerificationConstant.Length + CryptoSelect.Length + 2 + padD.Length];

				int offset = 0;
				offset += Message.Write(buffer, offset, VerificationConstant);
				offset += Message.Write(buffer, offset, CryptoSelect);
				offset += Message.Write(buffer, offset, Len(padD));
				offset += Message.Write(buffer, offset, padD);

				DoEncrypt(buffer, 0, buffer.Length);
				await SendMessage(buffer);

				SelectCrypto(_b, true);

				Ready();
			}
			catch (EncryptionException)
			{
				throw;
			}
			catch (Exception ex)
			{
				throw new EncryptionException("Handshake->PeerB->Step four exception", ex);
			}
		}


		/// <summary>
		/// Matches a torrent based on whether the HASH('req2', SKEY) xor HASH('req3', S) matches, where SKEY is the InfoHash of the torrent
		/// and sets the SKEY to the InfoHash of the matched torrent.
		/// </summary>
		/// <returns>true if a match has been found</returns>
		private bool MatchSKEY(byte[] torrentHash)
		{
			try
			{
				for (int i = 0; i < _possibleSKEYs.Length; i++)
				{
					byte[] req2 = Hash(Encoding.ASCII.GetBytes("req2"), _possibleSKEYs[i]);
					byte[] req3 = Hash(Encoding.ASCII.GetBytes("req3"), S);

					bool match = true;
					for (int j = 0; j < req2.Length && match; j++)
					{
						match = torrentHash[j] == (req2[j] ^ req3[j]);
					}

					if (match)
					{
						SKEY = _possibleSKEYs[i];
						return true;
					}
				}
				return false;
			}
			catch (EncryptionException)
			{
				throw;
			}
			catch (Exception ex)
			{
				throw new EncryptionException("MatchSKey failure", ex);
			}
		}
	}
}
