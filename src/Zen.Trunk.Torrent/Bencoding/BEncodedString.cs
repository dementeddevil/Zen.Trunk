namespace Zen.Trunk.Torrent.Bencoding
{
	using System;
	using System.IO;
	using System.Collections;
	using System.Text;
	using Zen.Trunk.Torrent.Common;

	/// <summary>
	/// Class representing a BEncoded string
	/// </summary>
	public class BEncodedString : BEncodedValue, IComparable<BEncodedString>
	{
		private byte[] _textBytes;

		#region Public Constructors
		/// <summary>
		/// Create a new BEncodedString using UTF8 encoding
		/// </summary>
		public BEncodedString()
			: this(new byte[0])
		{
		}

		/// <summary>
		/// Create a new BEncodedString using UTF8 encoding
		/// </summary>
		/// <param name="value"></param>
		public BEncodedString(char[] value)
			: this(Encoding.UTF8.GetBytes(value))
		{
		}

		/// <summary>
		/// Create a new BEncodedString using UTF8 encoding
		/// </summary>
		/// <param name="value">Initial value for the string</param>
		public BEncodedString(string value)
			: this(Encoding.UTF8.GetBytes(value))
		{
		}

		/// <summary>
		/// Create a new BEncodedString using UTF8 encoding
		/// </summary>
		/// <param name="value"></param>
		public BEncodedString(byte[] value)
		{
			this._textBytes = value;
		}

		public static implicit operator BEncodedString(string value)
		{
			return new BEncodedString(value);
		}
		public static implicit operator BEncodedString(char[] value)
		{
			return new BEncodedString(value);
		}
		public static implicit operator BEncodedString(byte[] value)
		{
			return new BEncodedString(value);
		}
		#endregion

		#region Public Properties
		/// <summary>
		/// The value of the BEncodedString
		/// </summary>
		public string Text
		{
			get
			{
				return Encoding.UTF8.GetString(_textBytes);
			}
			set
			{
				_textBytes = Encoding.UTF8.GetBytes(value);
			}
		}

		/// <summary>
		/// The underlying byte[] associated with this BEncodedString
		/// </summary>
		public byte[] TextBytes
		{
			get
			{
				return this._textBytes;
			}
		}
		#endregion

		#region Encode/Decode Methods
		/// <summary>
		/// Encodes the BEncodedString to a byte[] using the supplied Encoding
		/// </summary>
		/// <param name="buffer">The buffer to encode the string to</param>
		/// <param name="offset">The offset at which to save the data to</param>
		/// <param name="e">The encoding to use</param>
		/// <returns>The number of bytes encoded</returns>
		public override int Encode(byte[] buffer, int offset)
		{
			string output = _textBytes.Length + ":";
			int written = Encoding.UTF8.GetBytes(output, 0, output.Length, buffer, offset);
			Buffer.BlockCopy(_textBytes, 0, buffer, offset + written, _textBytes.Length);
			return written + _textBytes.Length;
		}


		/// <summary>
		/// Decodes a BEncodedString from the supplied StreamReader
		/// </summary>
		/// <param name="reader">The StreamReader containing the BEncodedString</param>
		internal override void DecodeInternal(RawReader reader)
		{
			if (reader == null)
				throw new ArgumentNullException("reader");

			int letterCount;
			string length = string.Empty;

			try
			{
				// read in how many characters the string is
				// TODO: Impose a max length on the length portion of the string
				while ((reader.PeekChar() != -1) && (reader.PeekChar() != ':'))
				{
					length += (char)reader.ReadChar();
				}

				if (reader.ReadChar() != ':')
				{
					throw new BEncodingException("Invalid data found. Aborting");
				}

				// TODO: Impose a max length on the string we will accept here
				letterCount = int.Parse(length);
				_textBytes = new byte[letterCount];
				if (reader.Read(_textBytes, 0, letterCount) != letterCount)
				{
					throw new BEncodingException("Couldn't decode string");
				}
			}
			catch (Exception ex)
			{
				throw new BEncodingException("Couldn't decode string", ex);
			}
		}
		#endregion

		#region Helper Methods
		public string Hex
		{
			get
			{
				return BitConverter.ToString(TextBytes);
			}
		}

		public override int LengthInBytes()
		{
			string output = _textBytes.Length.ToString() + ":";
			return (output.Length + _textBytes.Length);
		}

		public int CompareTo(object other)
		{
			return CompareTo((BEncodedString)other);
		}

		public int CompareTo(BEncodedString other)
		{
			if (other == null)
			{
				return 1;
			}

			int difference = 0;
			int length = this._textBytes.Length > other._textBytes.Length ? other._textBytes.Length : this._textBytes.Length;

			for (int i = 0; i < length; i++)
			{
				if ((difference = this._textBytes[i].CompareTo(other._textBytes[i])) != 0)
				{
					return difference;
				}
			}

			if (this._textBytes.Length == other._textBytes.Length)
			{
				return 0;
			}

			return this._textBytes.Length > other._textBytes.Length ? 1 : -1;
		}

		#endregion


		#region Overridden Methods

		public override bool Equals(object obj)
		{
			BEncodedString other;
			if (obj is string)
			{
				other = new BEncodedString((string)obj);
			}
			else
			{
				other = obj as BEncodedString;
			}

			if (other == null)
			{
				return false;
			}

			return Toolbox.ByteMatch(this._textBytes, other._textBytes);
		}

		public override int GetHashCode()
		{
			// TODO: Determine a better hash method
			int hash = 0;
			for (int i = 0; i < this._textBytes.Length; i++)
			{
				hash += this._textBytes[i];
			}

			return hash;
		}

		public override string ToString()
		{
			return Encoding.UTF8.GetString(_textBytes);
		}
		#endregion
	}
}
