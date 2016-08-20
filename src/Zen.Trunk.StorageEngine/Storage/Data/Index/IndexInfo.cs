namespace Zen.Trunk.Storage.Data.Index
{
	using System;

	public class IndexInfo : BufferFieldWrapper, IComparable
	{
		#region Private Fields
		#endregion

		#region Public Constructors
		/// <summary>
		/// Initializes a new instance of the <see cref="IndexInfo"/> class.
		/// </summary>
		public IndexInfo()
		{
		}
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
		/// Determines whether the specified <see cref="System.Object"/> is equal to this instance.
		/// </summary>
		/// <param name="obj">The <see cref="System.Object"/> to compare with this instance.</param>
		/// <returns>
		/// <c>true</c> if the specified <see cref="System.Object"/> is equal to this instance; otherwise, <c>false</c>.
		/// </returns>
		public override bool Equals(object obj)
		{
			IndexInfo rhs = obj as IndexInfo;
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
		/// Less than zero This instance is less than <paramref name="obj"/>.
		/// </item>
		/// <item>
		/// Zero This instance is equal to <paramref name="obj"/>. 
		/// </item>
		/// <item>
		/// Greater than zero This instance is greater than <paramref name="obj"/>.
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
			return ((IndexInfo)this).CompareTo((IndexInfo)obj);
		}
		#endregion
	}
}
