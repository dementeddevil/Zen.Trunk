namespace Zen.Trunk.Torrent.Bencoding
{
	using System;
	using System.IO;
	using System.Text;
	using System.Collections.Generic;

	/// <summary>
	/// Class representing a BEncoded number
	/// </summary>
	public class BEncodedNumber : BEncodedValue, IComparable<BEncodedNumber>
	{
		#region Member Variables
		/// <summary>
		/// The value of the BEncodedNumber
		/// </summary>
		public long Number
		{
			get { return number; }
			set { number = value; }
		}
		internal long number;
		#endregion


		#region Constructors
		public BEncodedNumber()
			: this(0)
		{
		}

		/// <summary>
		/// Create a new BEncoded number with the given value
		/// </summary>
		/// <param name="initialValue">The inital value of the BEncodedNumber</param>
		public BEncodedNumber(long value)
		{
			this.number = value;
		}

		public static implicit operator BEncodedNumber(long value)
		{
			return new BEncodedNumber(value);
		}
		#endregion


		#region Encode/Decode Methods

		/// <summary>
		/// Encodes this number to the supplied byte[] starting at the supplied offset
		/// </summary>
		/// <param name="buffer">The buffer to write the data to</param>
		/// <param name="offset">The offset to start writing the data at</param>
		/// <returns></returns>
		public override int Encode(byte[] buffer, int offset)
		{
			int written = 0;
			buffer[offset + written] = (byte)'i';
			written++;
			written += System.Text.Encoding.UTF8.GetBytes(this.number.ToString(), 0, this.number.ToString().Length, buffer, offset + written);
			buffer[offset + written] = (byte)'e';
			written++;
			return written;
		}


		/// <summary>
		/// Decodes a BEncoded number from the supplied RawReader
		/// </summary>
		/// <param name="reader">RawReader containing a BEncoded Number</param>
		internal override void DecodeInternal(RawReader reader)
		{
			if (reader == null)
				throw new ArgumentNullException("reader");

			StringBuilder sb;
			try
			{
				sb = new StringBuilder(8);
				if (reader.ReadByte() != 'i')              // remove the leading 'i'
					throw new BEncodingException("Invalid data found. Aborting.");

				while ((reader.PeekChar() != -1) && ((char)reader.PeekChar() != 'e'))
					sb.Append((char)reader.ReadByte());

				if (reader.ReadByte() != 'e')        //remove the trailing 'e'
					throw new BEncodingException("Invalid data found. Aborting.");

				this.number = long.Parse(sb.ToString());
			}
			catch (BEncodingException ex)
			{
				throw new BEncodingException("Couldn't decode number", ex);
			}
			catch
			{
				throw new BEncodingException("Couldn't decode number");
			}
		}
		#endregion


		#region Helper Methods
		/// <summary>
		/// Returns the length of the encoded string in bytes
		/// </summary>
		/// <returns></returns>
		public override int LengthInBytes()
		{
			return System.Text.Encoding.UTF8.GetByteCount('i' + this.number.ToString() + 'e');
		}


		public int CompareTo(object other)
		{
			if (other is BEncodedNumber || other is long || other is int)
				return CompareTo((BEncodedNumber)other);

			return -1;
		}

		public int CompareTo(BEncodedNumber other)
		{
			if (other == null)
				throw new ArgumentNullException("other");

			return this.number.CompareTo(other.number);
		}


		public int CompareTo(long other)
		{
			return this.number.CompareTo(other);
		}
		#endregion


		#region Overridden Methods
		/// <summary>
		/// 
		/// </summary>
		/// <param name="obj"></param>
		/// <returns></returns>
		public override bool Equals(object obj)
		{
			BEncodedNumber obj2 = obj as BEncodedNumber;
			if (obj2 == null)
				return false;

			return (this.number == obj2.number);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <returns></returns>
		public override int GetHashCode()
		{
			return this.number.GetHashCode();
		}

		/// <summary>
		/// 
		/// </summary>
		/// <returns></returns>
		public override string ToString()
		{
			return (this.number.ToString());
		}
		#endregion
	}
}
