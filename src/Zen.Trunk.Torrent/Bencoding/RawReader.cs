namespace Zen.Trunk.Torrent.Bencoding
{
	using System;
	using System.Collections.Generic;
	using System.Text;
	using System.IO;

	public class RawReader : BinaryReader
	{
		public RawReader(Stream input)
			: this(input, true)
		{
		}

		public RawReader(Stream input, bool strictDecoding)
			: base(input)
		{
			StrictDecoding = strictDecoding;
		}

		public bool StrictDecoding
		{
			get;
			private set;
		}
	}
}
