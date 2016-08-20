using System;
using System.Collections.Generic;
using System.Text;
using Zen.Trunk.Torrent.Bencoding;
using System.Threading;

namespace Zen.Trunk.Torrent.Dht
{
	internal static class TransactionId
	{
		private static SpinLockClass _lock = new SpinLockClass();
		private static byte[] current = new byte[2];

		public static BEncodedString NextId()
		{
			BEncodedString result = null;
			_lock.Execute(
				() =>
				{
					result = new BEncodedString((byte[])current.Clone());

					// Check whether we are about to overflow
					if (current[0] == 255)
					{
						// Increment high byte
						++current[1];
					}

					// Increment low byte
					++current[0];
				});
			return result;
		}
	}
}
