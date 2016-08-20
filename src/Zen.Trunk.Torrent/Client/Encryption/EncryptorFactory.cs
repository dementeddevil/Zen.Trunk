//
// EncryptorFactory.cs
//
// Authors:
//   Alan McGovern alan.mcgovern@gmail.com
//
// Copyright (C) 2008 Alan McGovern
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
using System.Collections.Generic;
using System.Text;
using Zen.Trunk.Torrent.Client;
using Zen.Trunk.Torrent.Common;
using System.Threading;
using Zen.Trunk.Torrent.Client.Connections;
using Zen.Trunk.Torrent.Client.Messages.Standard;
using System.Threading.Tasks;

namespace Zen.Trunk.Torrent.Client.Encryption
{
	internal static class EncryptorFactory
	{
		internal static Task<byte[]> CheckEncryptionAsync(PeerId id)
		{
			return CheckEncryptionAsync(id, null);
		}

		internal static async Task<byte[]> CheckEncryptionAsync(PeerId id, byte[][] sKeys)
		{
			IEncryptor encryptorConnection = null;
			IEncryption encryptor = new PlainTextEncryption();
			IEncryption decryptor = new PlainTextEncryption();
			byte[] buffer;
			byte[] initialData = null;
			int available = 0;

			IConnection c = id.Connection;
			try
			{
				// If the connection is incoming, receive the handshake before
				// trying to decide what encryption to use
				if (id.Connection.IsIncoming)
				{
					// Get the incoming message
					buffer = new byte[id.BytesToReceive];
					while (available < id.BytesToReceive)
					{
						int count = 0;
						try
						{
							count = await NetworkIO.EnqueueReceive(
								c,
								buffer,
								available,
								buffer.Length - available);
						}
						catch
						{
							throw new EncryptionException("Couldn't receive the handshake");
						}

						available += count;
					}

					HandshakeMessage message = new HandshakeMessage();
					message.Decode(buffer, 0, buffer.Length);
					bool valid = message.ProtocolString == VersionInfo.ProtocolStringV100;
					bool canUseRC4 = CheckRC4(id);

					if (valid)
					{
						initialData = buffer;
					}
					else
					{
						// If encryption is disabled and we received an invalid handshake - abort!
						if (!canUseRC4 && !valid)
						{
							throw new EncryptionException("Invalid handshake received and no decryption works");
						}

						System.Diagnostics.Debug.Assert(canUseRC4);

						// The data we just received was part of an encrypted handshake and was *not* the BitTorrent handshake
						encryptorConnection = new PeerBEncryption(sKeys, EncryptionTypes.All);
						await encryptorConnection.HandshakeAsync(c, buffer, 0, buffer.Length);
					}
				}
				else
				{
					bool hasRC4 = CheckRC4(id);
					bool hasPlainText = Toolbox.HasEncryption(id.Engine.Settings.AllowedEncryption, EncryptionTypes.PlainText);

					if (id.Engine.Settings.PreferEncryption)
					{
						if (hasRC4)
						{
							encryptorConnection = new PeerAEncryption(
								id.TorrentManager.Torrent.infoHash,
								EncryptionTypes.All);
							await encryptorConnection.HandshakeAsync(id.Connection);
						}
					}
					else
					{
						if (!hasPlainText)
						{
							encryptorConnection = new PeerAEncryption(
								id.TorrentManager.Torrent.infoHash,
								EncryptionTypes.All);
							await encryptorConnection.HandshakeAsync(id.Connection);
						}
					}
				}

				// At this point we should be ready to process the handshake
				if (encryptorConnection != null &&
					encryptorConnection.Encryptor != null &&
					encryptorConnection.Decryptor != null)
				{
					encryptor = encryptorConnection.Encryptor;
					decryptor = encryptorConnection.Decryptor;
					initialData = encryptorConnection.InitialData;
				}

				id.Encryptor = encryptor;
				id.Decryptor = decryptor;
				return initialData;
			}
			catch (Exception)
			{
				//result.Complete(ex);
				throw;
			}
		}

		private static bool CheckRC4(PeerId id)
		{
			// By default we assume all encryption levels are allowed. This is
			// needed when we receive an incoming connection, because that is not
			// associated with an engine and so we cannot check the engines settings
			EncryptionTypes t = EncryptionTypes.All;

			// If the connection is *not* incoming, then it will be associated with an Engine
			// so we can check what encryption levels the engine allows.
			if (!id.Connection.IsIncoming)
			{
				t = id.TorrentManager.Engine.Settings.AllowedEncryption;
			}

			// We're allowed use encryption if the engine settings allow it and the peer supports it
			// Binary AND both the engine encryption and peer encryption and check what levels are supported
			t = t & id.Peer.Encryption;
			return ClientEngine.SupportsEncryption &&
				   (Toolbox.HasEncryption(t, EncryptionTypes.RC4Full) || Toolbox.HasEncryption(t, EncryptionTypes.RC4Header));
		}
	}
}
