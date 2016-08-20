namespace Zen.Trunk.Torrent.Client.Encryption
{
	using System.Threading.Tasks;
	using Zen.Trunk.Torrent.Client.Connections;

	public interface IEncryptor
	{
		IEncryption Encryptor
		{
			get;
		}

		IEncryption Decryptor
		{
			get;
		}

		byte[] InitialData
		{
			get;
		}

		void AddPayload(byte[] buffer);
	
		void AddPayload(byte[] buffer, int offset, int count);

		Task HandshakeAsync(IConnection socket);

		Task HandshakeAsync(IConnection socket, byte[] initialBuffer, int offset, int count);
	}
}
