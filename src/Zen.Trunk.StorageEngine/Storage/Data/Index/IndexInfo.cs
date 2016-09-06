namespace Zen.Trunk.Storage.Data.Index
{
	using System;

    /// <summary>
    /// 
    /// </summary>
    /// <seealso cref="Zen.Trunk.Storage.BufferFieldWrapper" />
    /// <seealso cref="System.IComparable" />
    public class IndexInfo : BufferFieldWrapper, IComparable
	{
		#region Private Fields
		#endregion

		#region Public Operators
		/// <summary>
		/// Implements the operator !=.
		/// </summary>
		/// <param name="lhs">The LHS.</param>
		/// <param name="rhs">The RHS.</param>
		/// <returns>The result of the operator.</returns>
		public static bool operator !=(IndexInfo lhs, IndexInfo rhs)
		{
			return (lhs.CompareTo(rhs) != 0);
		}

		/// <summary>
		/// Implements the operator ==.
		/// </summary>
		/// <param name="lhs">The LHS.</param>
		/// <param name="rhs">The RHS.</param>
		/// <returns>The result of the operator.</returns>
		public static bool operator ==(IndexInfo lhs, IndexInfo rhs)
		{
			return (lhs.CompareTo(rhs) == 0);
		}

		/// <summary>
		/// Implements the operator &gt;.
		/// </summary>
		/// <param name="lhs">The LHS.</param>
		/// <param name="rhs">The RHS.</param>
		/// <returns>The result of the operator.</returns>
		public static bool operator >(IndexInfo lhs, IndexInfo rhs)
		{
			return (lhs.CompareTo(rhs) > 0);
		}

		/// <summary>
		/// Implements the operator &lt;.
		/// </summary>
		/// <param name="lhs">The LHS.</param>
		/// <param name="rhs">The RHS.</param>
		/// <returns>The result of the operator.</returns>
		public static bool operator <(IndexInfo lhs, IndexInfo rhs)
		{
			return (lhs.CompareTo(rhs) < 0);
		}
        #endregion

        #region Public Methods
        /// <summary>
        /// Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String" /> that represents this instance.
        /// </returns>
        public override string ToString()
	    {
	        return base.ToString();
	    }

        /// <summary>
        /// Returns a hash code for this instance.
        /// </summary>
        /// <returns>
        /// A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table. 
        /// </returns>
        public override int GetHashCode()
	    {
	        return base.GetHashCode();
	    }

	    /// <summary>
		/// Determines whether the specified <see cref="System.Object"/> is equal to this instance.
		/// </summary>
		/// <param name="obj">The <see cref="System.Object"/> to compare with this instance.</param>
		/// <returns>
		/// <c>true</c> if the specified <see cref="System.Object"/> is equal to this instance; otherwise, <c>false</c>.
		/// </returns>
		public override bool Equals(object obj)
		{
			var rhs = obj as IndexInfo;
			if (rhs != null)
			{
				return (CompareTo(rhs) == 0);
			}
			else
			{
				return base.Equals(obj);
			}
		}

        /// <summary>
        /// Compares the current instance with another object of the same type
        /// and returns an integer that indicates whether the current instance
        /// precedes, follows, or occurs in the same position in the sort order
        /// as the other object.
        /// </summary>
        /// <param name="rhs">An object to compare with this instance.</param>
        /// <returns>
        /// A value that indicates the relative order of the objects being
        /// compared. The return value has these meanings: 
        /// <list type="bulleted">
        /// <listheader>
        /// Value Meaning 
        /// </listheader>
        /// <item>
        /// Less than zero This instance is less than <paramref name="rhs"/>.
        /// </item>
        /// <item>
        /// Zero This instance is equal to <paramref name="rhs"/>. 
        /// </item>
        /// <item>
        /// Greater than zero This instance is greater than <paramref name="rhs"/>.
        /// </item>
        /// </list>
        /// </returns>
        /// <exception cref="T:System.ArgumentException">
        /// <paramref name="rhs"/> is not the same type as this instance.
        /// </exception>
        public virtual int CompareTo(IndexInfo rhs)
		{
			return 0;
		}
		#endregion

		#region IComparable Members
		/// <summary>
		/// Compares the current instance with another object of the same type and returns an integer that indicates whether the current instance precedes, follows, or occurs in the same position in the sort order as the other object.
		/// </summary>
		/// <param name="obj">An object to compare with this instance.</param>
		/// <returns>
		/// A value that indicates the relative order of the objects being compared. The return value has these meanings: Value Meaning Less than zero This instance is less than <paramref name="obj"/>. Zero This instance is equal to <paramref name="obj"/>. Greater than zero This instance is greater than <paramref name="obj"/>.
		/// </returns>
		/// <exception cref="T:System.ArgumentException">
		/// 	<paramref name="obj"/> is not the same type as this instance. </exception>
		int IComparable.CompareTo(object obj)
		{
			if (!(obj is IndexInfo))
			{
				return -1;
			}
			return CompareTo((IndexInfo)obj);
		}
		#endregion
	}
}
