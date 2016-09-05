namespace Zen.Trunk.Storage
{
	using System;
	using System.Globalization;
	using System.Runtime.InteropServices;

	[Serializable, StructLayout(LayoutKind.Sequential)]
	public struct InclusiveRange
	{
		public static InclusiveRange Empty = new InclusiveRange(0, 0);

		#region Private Fields
		private int _min;
		private int _max;
		#endregion

		#region Public Constructors
		public InclusiveRange(InclusiveRange range)
		{
			_min = range._min;
			_max = range._max;
		}
		public InclusiveRange(int min, int max)
		{
			_min = min;
			_max = max;
		}
		#endregion

		#region Public Properties
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

		public void EnsureValid()
		{
			if (Min > Max)
			{
				var temp = Min;
				Min = Max;
				Max = temp;
			}
		}

		public override int GetHashCode()
		{
			return (_min ^ _max);
		}

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
		public static bool operator ==(InclusiveRange lhs, InclusiveRange rhs)
		{
			if (lhs._min == rhs._min)
			{
				return rhs._max == lhs._max;
			}
			return false;
		}
		public static bool operator !=(InclusiveRange lhs, InclusiveRange rhs)
		{
			return !(lhs == rhs);
		}
		public static InclusiveRange operator +(InclusiveRange lhs, InclusiveRange rhs)
		{
			var result = new InclusiveRange();
			result.Min = lhs.Min + rhs.Min;
			result.Max = lhs.Max + rhs.Max;
			return result;
		}
		public static InclusiveRange operator -(InclusiveRange lhs, InclusiveRange rhs)
		{
			var result = new InclusiveRange();
			result.Min = lhs.Min - rhs.Min;
			result.Max = lhs.Max - rhs.Max;
			return result;
		}
		#endregion
	}

	[Serializable, StructLayout(LayoutKind.Sequential)]
	public struct ExclusiveRange
	{
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

		public void EnsureValid()
		{
			if (Min > Max)
			{
				var temp = Min;
				Min = Max;
				Max = temp;
			}
		}

		public override int GetHashCode()
		{
			return (_min ^ _max);
		}

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
		public static bool operator ==(ExclusiveRange lhs, ExclusiveRange rhs)
		{
			if (lhs._min == rhs._min)
			{
				return rhs._max == lhs._max;
			}
			return false;
		}
		public static bool operator !=(ExclusiveRange lhs, ExclusiveRange rhs)
		{
			return !(lhs == rhs);
		}
		public static ExclusiveRange operator +(ExclusiveRange lhs, ExclusiveRange rhs)
		{
			var result = new ExclusiveRange();
			result.Min = lhs.Min + rhs.Min;
			result.Max = lhs.Max + rhs.Max;
			return result;
		}
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
