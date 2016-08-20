namespace Zen.Trunk.Torrent.Client.Encryption
{
	using System;
	using System.Text;
	using System.Threading.Tasks;
	using Zen.Trunk.Torrent.Client.Messages;

	/// <summary>
	/// Class to handle message stream encryption for initiating connections
	/// </summary>
	public class PeerAEncryption : EncryptedSocket
	{
		private byte[] _verifyBytes;
		private byte[] _b;

		public PeerAEncryption(byte[] InfoHash, EncryptionTypes allowedEncryption)
			: base(allowedEncryption)
		{
			SKEY = InfoHash;
		}

		protected override async Task ReceiveY()
		{
			try
			{
				await base.ReceiveY();

				// 2 B->A: Diffie Hellman Yb, PadB
				await StepThree();
			}
			catch (EncryptionException)
			{
				throw;
			}
			catch (Exception ex)
			{
				throw new EncryptionException("Handshake ReceiveY exception", ex);
			}
		}

		private async Task StepThree()
		{
			try
			{
				CreateCryptors("keyA", "keyB");

				// 3 A->B: HASH('req1', S)
				byte[] req1 = Hash(Encoding.ASCII.GetBytes("req1"), S);

				// ... HASH('req2', SKEY)
				byte[] req2 = Hash(Encoding.ASCII.GetBytes("req2"), SKEY);

				// ... HASH('req3', S)
				byte[] req3 = Hash(Encoding.ASCII.GetBytes("req3"), S);

				// HASH('req2', SKEY) xor HASH('req3', S)
				for (int i = 0; i < req2.Length; i++)
					req2[i] ^= req3[i];

				byte[] padC = GeneratePad();

				// 3 A->B: HASH('req1', S), HASH('req2', SKEY) xor HASH('req3', S), ENCRYPT(VC, crypto_provide, len(PadC), ...
				byte[] buffer = new byte[req1.Length + req2.Length + VerificationConstant.Length + CryptoProvide.Length
										+ 2 + padC.Length + 2 + InitialPayload.Length];

				int offset = 0;
				offset += Message.Write(buffer, offset, req1);
				offset += Message.Write(buffer, offset, req2);
				offset += Message.Write(buffer, offset, DoEncrypt(VerificationConstant));
				offset += Message.Write(buffer, offset, DoEncrypt(CryptoProvide));
				offset += Message.Write(buffer, offset, DoEncrypt(Len(padC)));
				offset += Message.Write(buffer, offset, DoEncrypt(padC));

				// ... PadC, len(IA)), ENCRYPT(IA)
				offset += Message.Write(buffer, offset, DoEncrypt(Len(InitialPayload)));
				offset += Message.Write(buffer, offset, DoEncrypt(InitialPayload));

				// Send the entire message in one go
				await SendMessage(buffer);
				InitialPayload = new byte[0];

				await Synchronize(DoDecrypt(VerificationConstant), 616); // 4 B->A: ENCRYPT(VC)
			}
			catch (EncryptionException)
			{
				throw;
			}
			catch (Exception ex)
			{
				throw new EncryptionException("Handshake->PeerA->StepThree exception", ex);
			}
		}

		protected override async Task Synchronize(byte[] syncData, int syncStopPoint)
		{
			try
			{
				await base.Synchronize(syncData, syncStopPoint);

				_verifyBytes = new byte[4 + 2];
				await ReceiveMessage(_verifyBytes, _verifyBytes.Length); // crypto_select, len(padD) ...

				byte[] myCS = new byte[4];
				byte[] lenPadD = new byte[2];

				DoDecrypt(_verifyBytes, 0, _verifyBytes.Length);

				Array.Copy(_verifyBytes, 0, myCS, 0, myCS.Length); // crypto_select

				//SelectCrypto(myCS);
				_b = myCS;
				Array.Copy(_verifyBytes, myCS.Length, lenPadD, 0, lenPadD.Length); // len(padD)

				PadD = new byte[DeLen(lenPadD)];

				await ReceiveMessage(PadD, PadD.Length);

				DoDecrypt(PadD, 0, PadD.Length); // padD
				SelectCrypto(_b, true);
				Ready();
			}
			catch (EncryptionException)
			{
				throw;
			}
			catch (Exception ex)
			{
				throw new EncryptionException("Handshake->PeerA->Synchronise exception", ex);
			}
		}
	}
}