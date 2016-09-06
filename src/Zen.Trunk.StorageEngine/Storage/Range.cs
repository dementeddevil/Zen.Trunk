namespace Zen.Trunk.Storage
{
	using System;
	using System.Globalization;
	using System.Runtime.InteropServices;

    /// <summary>
    /// 
    /// </summary>
    [Serializable, StructLayout(LayoutKind.Sequential)]
	public struct InclusiveRange
	{
        /// <summary>
        /// The empty
        /// </summary>
        public static InclusiveRange Empty = new InclusiveRange(0, 0);

		#region Private Fields
		private int _min;
		private int _max;
        #endregion

        #region Public Constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="InclusiveRange"/> struct.
        /// </summary>
        /// <param name="range">The range.</param>
        public InclusiveRange(InclusiveRange range)
		{
			_min = range._min;
			_max = range._max;
		}

        /// <summary>
        /// Initializes a new instance of the <see cref="InclusiveRange"/> struct.
        /// </summary>
        /// <param name="min">The minimum.</param>
        /// <param name="max">The maximum.</param>
        public InclusiveRange(int min, int max)
		{
			_min = min;
			_max = max;
		}
        #endregion

        #region Public Properties
        /// <summary>
        /// Gets or sets the minimum.
        /// </summary>
        /// <value>
        /// The minimum.
        /// </value>
        public int Min
		{
			get
			{
				return _min;
			}
			set
			{
				_min = value;
			}
		}

        /// <summary>
        /// Gets or sets the maximum.
        /// </summary>
        /// <value>
        /// The maximum.
        /// </value>
        public int Max
		{
			get
			{
				return _max;
			}
			set
			{
				_max = value;
			}
		}
        #endregion

        #region Public Methods
        /// <summary>
        /// Determines whether the specified value is in range.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>
        /// <c>true</c> if value is in range; otherwise, <c>false</c>.
        /// </returns>
        public bool IsInRange(int value)
		{
			var result = true;
			EnsureValid();
			if (value < Min || value > Max)
			{
				result = false;
			}
			return result;
		}

        /// <summary>
        /// Ensures the this instance is valid.
        /// </summary>
        public void EnsureValid()
		{
			if (Min > Max)
			{
				var temp = Min;
				Min = Max;
				Max = temp;
			}
		}

        /// <summary>
        /// Returns a hash code for this instance.
        /// </summary>
        /// <returns>
        /// A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table. 
        /// </returns>
        public override int GetHashCode()
		{
			return (Min ^ Max);
		}

        /// <summary>
        /// Determines whether the specified <see cref="System.Object" />, is equal to this instance.
        /// </summary>
        /// <param name="obj">The <see cref="System.Object" /> to compare with this instance.</param>
        /// <returns>
        ///   <c>true</c> if the specified <see cref="System.Object" /> is equal to this instance; otherwise, <c>false</c>.
        /// </returns>
        public override bool Equals(object obj)
		{
			if (obj is InclusiveRange)
			{
				var range = (InclusiveRange)obj;
				if (range._min == _min)
				{
					return (range._max == _max);
				}
			}
			return false;
		}

        /// <summary>
        /// Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String" /> that represents this instance.
        /// </returns>
        public override string ToString()
		{
			return string.Concat(new[]
				{
					"{Min=", Min.ToString (CultureInfo.CurrentCulture),
					",Max=", Max.ToString (CultureInfo.CurrentCulture),
					"}" 
				});
		}
        #endregion

        #region Operators
        /// <summary>
        /// Implements the operator ==.
        /// </summary>
        /// <param name="lhs">The LHS.</param>
        /// <param name="rhs">The RHS.</param>
        /// <returns>
        /// The result of the operator.
        /// </returns>
        public static bool operator ==(InclusiveRange lhs, InclusiveRange rhs)
		{
			if (lhs._min == rhs._min)
			{
				return rhs._max == lhs._max;
			}
			return false;
		}

        /// <summary>
        /// Implements the operator !=.
        /// </summary>
        /// <param name="lhs">The LHS.</param>
        /// <param name="rhs">The RHS.</param>
        /// <returns>
        /// The result of the operator.
        /// </returns>
        public static bool operator !=(InclusiveRange lhs, InclusiveRange rhs)
		{
			return !(lhs == rhs);
		}

        /// <summary>
        /// Implements the operator +.
        /// </summary>
        /// <param name="lhs">The LHS.</param>
        /// <param name="rhs">The RHS.</param>
        /// <returns>
        /// The result of the operator.
        /// </returns>
        public static InclusiveRange operator +(InclusiveRange lhs, InclusiveRange rhs)
		{
			var result = new InclusiveRange();
			result.Min = lhs.Min + rhs.Min;
			result.Max = lhs.Max + rhs.Max;
			return result;
		}

        /// <summary>
        /// Implements the operator -.
        /// </summary>
        /// <param name="lhs">The LHS.</param>
        /// <param name="rhs">The RHS.</param>
        /// <returns>
        /// The result of the operator.
        /// </returns>
        public static InclusiveRange operator -(InclusiveRange lhs, InclusiveRange rhs)
		{
			var result = new InclusiveRange();
			result.Min = lhs.Min - rhs.Min;
			result.Max = lhs.Max - rhs.Max;
			return result;
		}
		#endregion
	}

    /// <summary>
    /// 
    /// </summary>
    [Serializable, StructLayout(LayoutKind.Sequential)]
	public struct ExclusiveRange
	{
        /// <summary>
        /// The empty
        /// </summary>
        public static ExclusiveRange Empty = new ExclusiveRange(0, 0);

		#region Private Fields
		private int _min;
		private int _max;
		#endregion

		#region Public Constructors
		/// <summary>
		/// Initializes a new instance of the <see cref="ExclusiveRange"/> struct.
		/// </summary>
		/// <param name="range">The range.</param>
		public ExclusiveRange(ExclusiveRange range)
		{
			_min = range._min;
			_max = range._max;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="ExclusiveRange"/> struct.
		/// </summary>
		/// <param name="min">The min.</param>
		/// <param name="max">The max.</param>
		public ExclusiveRange(int min, int max)
		{
			_min = min;
			_max = max;
		}
        #endregion

        #region Public Properties
        /// <summary>
        /// Gets or sets the minimum.
        /// </summary>
        /// <value>
        /// The minimum.
        /// </value>
        public int Min
		{
			get
			{
				return _min;
			}
			set
			{
				_min = value;
			}
		}

        /// <summary>
        /// Gets or sets the maximum.
        /// </summary>
        /// <value>
        /// The maximum.
        /// </value>
        public int Max
		{
			get
			{
				return _max;
			}
			set
			{
				_max = value;
			}
		}
        #endregion

        #region Public Methods
        /// <summary>
        /// Determines whether [is in range] [the specified value].
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>
        ///   <c>true</c> if [is in range] [the specified value]; otherwise, <c>false</c>.
        /// </returns>
        public bool IsInRange(int value)
		{
			var result = true;
			EnsureValid();
			if (value <= Min || value >= Max)
			{
				result = false;
			}
			return result;
		}

        /// <summary>
        /// Ensures the valid.
        /// </summary>
        public void EnsureValid()
		{
			if (Min > Max)
			{
				var temp = Min;
				Min = Max;
				Max = temp;
			}
		}

        /// <summary>
        /// Returns a hash code for this instance.
        /// </summary>
        /// <returns>
        /// A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table. 
        /// </returns>
        public override int GetHashCode()
		{
			return (Min ^ Max);
		}

        /// <summary>
        /// Determines whether the specified <see cref="System.Object" />, is equal to this instance.
        /// </summary>
        /// <param name="obj">The <see cref="System.Object" /> to compare with this instance.</param>
        /// <returns>
        ///   <c>true</c> if the specified <see cref="System.Object" /> is equal to this instance; otherwise, <c>false</c>.
        /// </returns>
        public override bool Equals(object obj)
		{
			if (obj is ExclusiveRange)
			{
				var range = (ExclusiveRange)obj;
				if (range._min == _min)
				{
					return (range._max == _max);
				}
			}
			return false;
		}

        /// <summary>
        /// Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String" /> that represents this instance.
        /// </returns>
        public override string ToString()
		{
			return string.Concat(new[]
				{
					"{Min=", Min.ToString (CultureInfo.CurrentCulture),
					",Max=", Max.ToString (CultureInfo.CurrentCulture),
					"}" 
				});
		}
        #endregion

        #region Operators
        /// <summary>
        /// Implements the operator ==.
        /// </summary>
        /// <param name="lhs">The LHS.</param>
        /// <param name="rhs">The RHS.</param>
        /// <returns>
        /// The result of the operator.
        /// </returns>
        public static bool operator ==(ExclusiveRange lhs, ExclusiveRange rhs)
		{
			if (lhs._min == rhs._min)
			{
				return rhs._max == lhs._max;
			}
			return false;
		}
        
        /// <summary>
        /// Implements the operator !=.
        /// </summary>
        /// <param name="lhs">The LHS.</param>
        /// <param name="rhs">The RHS.</param>
        /// <returns>
        /// The result of the operator.
        /// </returns>
        public static bool operator !=(ExclusiveRange lhs, ExclusiveRange rhs)
		{
			return !(lhs == rhs);
		}
        
        /// <summary>
        /// Implements the operator +.
        /// </summary>
        /// <param name="lhs">The LHS.</param>
        /// <param name="rhs">The RHS.</param>
        /// <returns>
        /// The result of the operator.
        /// </returns>
        public static ExclusiveRange operator +(ExclusiveRange lhs, ExclusiveRange rhs)
		{
			var result = new ExclusiveRange();
			result.Min = lhs.Min + rhs.Min;
			result.Max = lhs.Max + rhs.Max;
			return result;
		}

        /// <summary>
        /// Implements the operator -.
        /// </summary>
        /// <param name="lhs">The LHS.</param>
        /// <param name="rhs">The RHS.</param>
        /// <returns>
        /// The result of the operator.
        /// </returns>
        public static ExclusiveRange operator -(ExclusiveRange lhs, ExclusiveRange rhs)
		{
			var result = new ExclusiveRange();
			result.Min = lhs.Min - rhs.Min;
			result.Max = lhs.Max - rhs.Max;
			return result;
		}
		#endregion
	}
}
