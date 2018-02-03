using System;

namespace Zen.Trunk.VirtualMemory
{
    /// <summary>
    /// 
    /// </summary>
    [CLSCompliant(false)]
	public class FlushParameters
	{
		#region Public Constructors
		/// <summary>
		/// Initialises an instance of <see cref="T:FlushParameters" />.
		/// </summary>
		public FlushParameters()
		{
			FlushReads = true;
			FlushWrites = true;
		}

		/// <summary>
		/// Initialises an instance of <see cref="T:FlushParameters"/>.
		/// </summary>
		/// <param name="reads">if set to <c>true</c> [reads].</param>
		/// <param name="writes">if set to <c>true</c> [writes].</param>
		public FlushParameters(bool reads, bool writes)
		{
			FlushReads = reads;
			FlushWrites = writes;
		}
		#endregion

		#region Public Properties
		/// <summary>
		/// Gets or sets a value indicating whether to flush read requests.
		/// </summary>
		/// <value>
		/// <c>true</c> if to flush read requests; otherwise, <c>false</c>.
		/// </value>
		public bool FlushReads
		{
			get;
			private set;
		}

		/// <summary>
		/// Gets or sets a value indicating whether to flush write requests.
		/// </summary>
		/// <value>
		/// <c>true</c> to flush write requests; otherwise, <c>false</c>.
		/// </value>
		public bool FlushWrites
		{
			get;
			private set;
		}
		#endregion
	}
}
