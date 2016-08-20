//
// EncryptedSocket.cs
//
// Authors:
//   Yiduo Wang planetbeing@gmail.com
//   Alan McGovern alan.mcgovern@gmail.com
//
// Copyright (C) 2007 Yiduo Wang
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//


using System;
using System.Text;
using System.Security.Cryptography;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Zen.Trunk.Torrent.Client.Encryption;
using Zen.Trunk.Torrent.Common;
using Zen.Trunk.Torrent.Client.Connections;
using Zen.Trunk.Torrent.Client.Messages;
using System.Threading.Tasks;
using System.Diagnostics;


namespace Zen.Trunk.Torrent.Client.Encryption
{
	public class EncryptionException : TorrentException
	{
		public EncryptionException()
		{

		}

		public EncryptionException(string message)
			: base(message)
		{

		}

		public EncryptionException(string message, Exception innerException)
			: base(message, innerException)
		{

		}
	}

	/// <summary>
	/// The class that handles.Message Stream Encryption for a connection
	/// </summary>
	public class EncryptedSocket : IEncryptor
	{
		#region Private Fields
		private RandomNumberGenerator _random;
		private SHA1 _hasher;

		// Cryptors for the handshaking
		private RC4 _encryptor = null;
		private RC4 _decryptor = null;

		// Cryptors for the data transmission
		private IEncryption _streamEncryptor;
		private IEncryption _streamDecryptor;

		private EncryptionTypes _allowedEncryption;

		private byte[] _X; // A 160 bit random integer
		private byte[] _Y; // 2^X mod P
		private byte[] _otherY = null;

		private IConnection _socket;

		// Data to be passed to initial ReceiveMessage requests
		private byte[] _initialBuffer;
		private int _initialBufferOffset;
		private int _initialBufferCount;

		// State information to be checked against abort conditions
		private int _bytesReceived;

		// State information for synchronization
		private byte[] _synchronizeData = null;
		private byte[] _synchronizeWindow = null;
		private int _syncStopPoint;
		#endregion

		#region Protected Fields
		protected byte[] S = null;
		protected byte[] SKEY = null;

		protected byte[] PadC = null;
		protected byte[] PadD = null;

		protected byte[] VerificationConstant = new byte[8];

		protected byte[] CryptoProvide = new byte[] { 0x00, 0x00, 0x00, 0x03 };

		protected byte[] InitialPayload;
		protected byte[] RemoteInitialPayload;

		protected byte[] CryptoSelect;
		#endregion

		#region Public Constructors
		/// <summary>
		/// Initializes a new instance of the <see cref="EncryptedSocket"/> class.
		/// </summary>
		/// <param name="allowedEncryption">The allowed encryption.</param>
		public EncryptedSocket(EncryptionTypes allowedEncryption)
		{
			_random = RNGCryptoServiceProvider.Create();
			_hasher = SHA1.Create();

			GenerateX();
			GenerateY();

			InitialPayload = new byte[0];
			RemoteInitialPayload = new byte[0];

			_bytesReceived = 0;

			SetMinCryptoAllowed(allowedEncryption);
		}
		#endregion

		#region Public Properties
		public IEncryption Encryptor
		{
			get
			{
				return _streamEncryptor;
			}
		}

		public IEncryption Decryptor
		{
			get
			{
				return _streamDecryptor;
			}
		}

		public byte[] InitialData
		{
			get
			{
				return RemoteInitialPayload;
			}
		}
		#endregion

		#region Interface implementation
		/// <summary>
		/// Begins the message stream encryption handshaking process, beginning with some data
		/// already received from the socket.
		/// </summary>
		/// <param name="socket">The socket to perform handshaking with</param>
		/// <param name="initialBuffer">Buffer containing soome data already received from the socket</param>
		/// <param name="offset">Offset to begin reading in initialBuffer</param>
		/// <param name="count">Number of bytes to read from initialBuffer</param>
		public Task HandshakeAsync(IConnection socket, byte[] initialBuffer, int offset, int count)
		{
			_initialBuffer = initialBuffer;
			_initialBufferOffset = offset;
			_initialBufferCount = count;
			return HandshakeAsync(socket);
		}

		/// <summary>
		/// Begins the message stream encryption handshaking process
		/// </summary>
		/// <param name="socket">The socket to perform handshaking with</param>
		public Task HandshakeAsync(IConnection socket)
		{
			if (socket == null)
			{
				throw new ArgumentNullException("socket");
			}

			if(_socket != null)
			{
				throw new InvalidOperationException("HandshakeAsync already called.");
			}
			_socket = socket;

			// Either "1 A->B: Diffie Hellman Ya, PadA" or "2 B->A: Diffie Hellman Yb, PadB"
			// These two steps will be done simultaneously to save time due to latency
			return Task.WhenAll(
				SendY(),
				ReceiveY());
		}

		/// <summary>
		/// Encrypts some data (should only be called after onEncryptorReady)
		/// </summary>
		/// <param name="buffer">Buffer with the data to encrypt</param>
		/// <param name="offset">Offset to begin encryption</param>
		/// <param name="count">Number of bytes to encrypt</param>
		public void Encrypt(byte[] data, int offset, int length)
		{
			_streamEncryptor.Encrypt(data, offset, data, offset, length);
		}

		/// <summary>
		/// Decrypts some data (should only be called after onEncryptorReady)
		/// </summary>
		/// <param name="buffer">Buffer with the data to decrypt</param>
		/// <param name="offset">Offset to begin decryption</param>
		/// <param name="count">Number of bytes to decrypt</param>
		public void Decrypt(byte[] data, int offset, int length)
		{
			_streamDecryptor.Decrypt(data, offset, data, offset, length);
		}

		private int RandomNumber(int max)
		{
			byte[] b = new byte[4];
			_random.GetBytes(b);
			uint val = BitConverter.ToUInt32(b, 0);
			return (int)(val % max);
		}
		#endregion

		#region Diffie-Hellman Key Exchange Functions

		/// <summary>
		/// Send Y to the remote client, with a random padding that is 0 to 512 bytes long
		/// (Either "1 A->B: Diffie Hellman Ya, PadA" or "2 B->A: Diffie Hellman Yb, PadB")
		/// </summary>
		protected Task SendY()
		{
			byte[] toSend = new byte[96 + RandomNumber(512)];
			_random.GetBytes(toSend);

			Buffer.BlockCopy(_Y, 0, toSend, 0, 96);

			return SendMessage(toSend);
		}

		/// <summary>
		/// Receive the first 768 bits of the transmission from the remote client, which is Y in the protocol
		/// (Either "1 A->B: Diffie Hellman Ya, PadA" or "2 B->A: Diffie Hellman Yb, PadB")
		/// </summary>
		protected virtual async Task ReceiveY()
		{
			_otherY = new byte[96];
			await ReceiveMessage(_otherY, 96);

			S = ModuloCalculator.Calculate(_otherY, _X);
		}
		#endregion

		#region Synchronization functions
		/// <summary>
		/// Read data from the socket until the byte string in syncData is read, or until syncStopPoint
		/// is reached (in that case, there is an EncryptionError).
		/// (Either "3 A->B: HASH('req1', S)" or "4 B->A: ENCRYPT(VC)")
		/// </summary>
		/// <param name="syncData">Buffer with the data to synchronize to</param>
		/// <param name="syncStopPoint">Maximum number of bytes (measured from the total received from the socket since connection) to read before giving up</param>
		protected virtual async Task Synchronize(byte[] syncData, int syncStopPoint)
		{
			try
			{
				// The strategy here is to create a window the size of the data to synchronize and just refill that until its contents match syncData
				_synchronizeData = syncData;
				_synchronizeWindow = new byte[syncData.Length];
				this._syncStopPoint = syncStopPoint;

				if (_bytesReceived > syncStopPoint)
				{
					throw new EncryptionException("Couldn't synchronise 1");
				}

				int filled = 0;
				bool matched = false;
				while (!matched)
				{
					int count = await NetworkIO.EnqueueReceive(_socket, _synchronizeWindow, filled, _synchronizeWindow.Length - filled);
					_bytesReceived += count;
					filled += count; // count of the bytes currently in synchronizeWindow

					matched = true;
					for (int i = 0; i < filled && matched; i++)
					{
						if (_synchronizeData[i] != _synchronizeWindow[i])
						{
							matched = false;
						}
					}

					if (!matched)
					{
						if (_bytesReceived > syncStopPoint)
						{
							throw new EncryptionException("Could not resynchronise the stream");
						}

						// See if the current window contains the first byte of the expected synchronize data
						// No need to check synchronizeWindow[0] as otherwise we could loop forever receiving 0 bytes
						int shift = -1;
						for (int i = 1; i < _synchronizeWindow.Length && shift == -1; i++)
						{
							if (_synchronizeWindow[i] == _synchronizeData[0])
							{
								shift = i;
							}
						}

						// The current data is all useless, so read an entire new window of data
						if (shift == -1)
						{
							filled = 0;
						}
						else
						{
							// Shuffle everything left by 'shift' (the first good byte) and fill the rest of the window
							Buffer.BlockCopy(_synchronizeWindow, shift, _synchronizeWindow, 0, _synchronizeWindow.Length - shift);
							filled = _synchronizeWindow.Length - shift;
						}
					}
				}
			}
			catch (EncryptionException)
			{
				throw;
			}
			catch (Exception ex)
			{
				throw new EncryptionException("Handshake synchronisation exception", ex);
			}
		}
		#endregion

		#region I/O Functions
		protected Task ReceiveMessage(byte[] buffer, int length)
		{
			try
			{
				if (length == 0)
				{
					return CompletedTask.Default;
				}

				if (_initialBuffer != null)
				{
					int toCopy = Math.Min(_initialBufferCount, length);
					Array.Copy(_initialBuffer, _initialBufferOffset, buffer, 0, toCopy);

					if (toCopy == _initialBufferCount)
					{
						_initialBufferCount = 0;
						_initialBufferOffset = 0;
						_initialBuffer = new byte[0];
					}
					else
					{
						_initialBufferOffset += toCopy;
						_initialBufferCount -= toCopy;
					}

					if (toCopy == length)
					{
						return CompletedTask.Default;
					}

					return NetworkIO.EnqueueReceive(_socket, buffer, toCopy, length - toCopy);
				}
				else
				{
					return NetworkIO.EnqueueReceive(_socket, buffer, 0, length);
				}
			}
			catch (Exception ex)
			{
				throw new ProtocolException("ReceiveMessage exception", ex);
			}
		}

		protected async Task SendMessage(byte[] toSend)
		{
			try
			{
				if (toSend.Length > 0)
				{
					int count = await NetworkIO.EnqueueSend(_socket, toSend, 0, toSend.Length);
					Debug.Assert(count == toSend.Length);
				}
			}
			catch (Exception ex)
			{
				throw new ProtocolException("SendMessage exception", ex);
			}
		}
		#endregion

		#region Cryptography Setup
		/// <summary>
		/// Generate a 160 bit random number for X
		/// </summary>
		private void GenerateX()
		{
			_X = new byte[20];

			_random.GetBytes(_X);
		}

		/// <summary>
		/// Calculate 2^X mod P
		/// </summary>
		private void GenerateY()
		{
			_Y = ModuloCalculator.Calculate(ModuloCalculator.TWO, _X);
		}

		/// <summary>
		/// Instantiate the cryptors with the keys: Hash(encryptionSalt, S, SKEY) for the encryptor and
		/// Hash(encryptionSalt, S, SKEY) for the decryptor.
		/// (encryptionSalt should be "keyA" if you're A, "keyB" if you're B, and reverse for decryptionSalt)
		/// </summary>
		/// <param name="encryptionSalt">The salt to calculate the encryption key with</param>
		/// <param name="decryptionSalt">The salt to calculate the decryption key with</param>
		protected void CreateCryptors(string encryptionSalt, string decryptionSalt)
		{
			_encryptor = new RC4(Hash(Encoding.ASCII.GetBytes(encryptionSalt), S, SKEY));
			_decryptor = new RC4(Hash(Encoding.ASCII.GetBytes(decryptionSalt), S, SKEY));
		}

		/// <summary>
		/// Sets CryptoSelect and initializes the stream encryptor and decryptor based on the selected method.
		/// </summary>
		/// <param name="remoteCryptoBytes">The cryptographic methods supported/wanted by the remote client in CryptoProvide format. The highest order one available will be selected</param>
		protected virtual int SelectCrypto(byte[] remoteCryptoBytes, bool replace)
		{
			CryptoSelect = new byte[remoteCryptoBytes.Length];

			// '2' corresponds to RC4Full
			if ((remoteCryptoBytes[3] & 2) == 2 && Toolbox.HasEncryption(_allowedEncryption, EncryptionTypes.RC4Full))
			{
				CryptoSelect[3] |= 2;
				if (replace)
				{
					_streamEncryptor = _encryptor;
					_streamDecryptor = _decryptor;
				}
				return 2;
			}

			// '1' corresponds to RC4Header
			if ((remoteCryptoBytes[3] & 1) == 1 && Toolbox.HasEncryption(_allowedEncryption, EncryptionTypes.RC4Header))
			{
				CryptoSelect[3] |= 1;
				if (replace)
				{
					_streamEncryptor = new RC4Header();
					_streamDecryptor = new RC4Header();
				}
				return 1;
			}

			throw new EncryptionException("No valid encryption method detected");
		}
		#endregion

		#region Utility Functions

		/// <summary>
		/// Concatenates several byte buffers
		/// </summary>
		/// <param name="data">Buffers to concatenate</param>
		/// <returns>Resulting concatenated buffer</returns>
		protected byte[] Combine(params byte[][] data)
		{
			int cursor = 0;
			int totalLength = 0;
			byte[] combined;

			foreach (byte[] datum in data)
			{
				totalLength += datum.Length;
			}

			combined = new byte[totalLength];

			for (int i = 0; i < data.Length; i++)
			{
				cursor += Message.Write(combined, cursor, data[i]);
			}

			return combined;
		}

		/// <summary>
		/// Hash some data with SHA1
		/// </summary>
		/// <param name="data">Buffers to hash</param>
		/// <returns>20-byte hash</returns>
		protected byte[] Hash(params byte[][] data)
		{
			return _hasher.ComputeHash(Combine(data));
		}

		/// <summary>
		/// Converts a 2-byte big endian integer into an int (reverses operation of Len())
		/// </summary>
		/// <param name="data">2 byte buffer</param>
		/// <returns>int</returns>
		protected int DeLen(byte[] data)
		{
			return (int)(data[0] << 8) + data[1];
		}

		/// <summary>
		/// Returns a 2-byte buffer with the length of data
		/// </summary>
		protected byte[] Len(byte[] data)
		{
			byte[] lenBuffer = new byte[2];
			lenBuffer[0] = (byte)((data.Length >> 8) & 0xff);
			lenBuffer[1] = (byte)((data.Length) & 0xff);
			return lenBuffer;
		}

		/// <summary>
		/// Returns a 0 to 512 byte 0-filled pad.
		/// </summary>
		protected byte[] GeneratePad()
		{
			return new byte[RandomNumber(512)];
		}
		#endregion

		#region Miscellaneous

		protected byte[] DoEncrypt(byte[] data)
		{
			byte[] d = (byte[])data.Clone();
			_encryptor.Encrypt(d);
			return d;
		}

		/// <summary>
		/// Encrypts some data with the RC4 encryptor used in handshaking
		/// </summary>
		/// <param name="buffer">Buffer with the data to encrypt</param>
		/// <param name="offset">Offset to begin encryption</param>
		/// <param name="count">Number of bytes to encrypt</param>
		protected void DoEncrypt(byte[] data, int offset, int length)
		{
			_encryptor.Encrypt(data, offset, data, offset, length);
		}

		/// <summary>
		/// Decrypts some data with the RC4 encryptor used in handshaking
		/// </summary>
		/// <param name="data">Buffers with the data to decrypt</param>
		/// <returns>Buffer with decrypted data</returns>
		protected byte[] DoDecrypt(byte[] data)
		{
			byte[] d = (byte[])data.Clone();
			_decryptor.Decrypt(d);
			return d;
		}

		/// <summary>
		/// Decrypts some data with the RC4 decryptor used in handshaking
		/// </summary>
		/// <param name="buffer">Buffer with the data to decrypt</param>
		/// <param name="offset">Offset to begin decryption</param>
		/// <param name="count">Number of bytes to decrypt</param>
		protected void DoDecrypt(byte[] data, int offset, int length)
		{
			_decryptor.Decrypt(data, offset, data, offset, length);
		}

		/// <summary>
		/// Signal that the cryptor is now in a state ready to encrypt and decrypt payload data
		/// </summary>
		protected void Ready()
		{
			//_handshake.TrySetResult(null);
		}

		protected void SetMinCryptoAllowed(EncryptionTypes allowedEncryption)
		{
			_allowedEncryption = allowedEncryption;

			// EncryptionType is basically a bit position starting from the right.
			// This sets all bits in CryptoProvide 0 that is to the right of minCryptoAllowed.
			CryptoProvide[0] = CryptoProvide[1] = CryptoProvide[2] = CryptoProvide[3] = 0;

			if (Toolbox.HasEncryption(allowedEncryption, EncryptionTypes.RC4Full))
			{
				CryptoProvide[3] |= 1 << 1;
			}

			if (Toolbox.HasEncryption(allowedEncryption, EncryptionTypes.RC4Header))
			{
				CryptoProvide[3] |= 1;
			}
		}
		#endregion

		public void AddPayload(byte[] buffer)
		{
			if (buffer == null)
			{
				throw new ArgumentNullException("buffer");
			}

			AddPayload(buffer, 0, buffer.Length);
		}

		public void AddPayload(byte[] buffer, int offset, int count)
		{
			byte[] newBuffer = new byte[InitialPayload.Length + count];

			Message.Write(newBuffer, 0, InitialPayload);
			Message.Write(newBuffer, InitialPayload.Length, buffer, offset, count);

			InitialPayload = buffer;
		}
	}
}
