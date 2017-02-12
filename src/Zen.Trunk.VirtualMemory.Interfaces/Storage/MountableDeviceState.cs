namespace Zen.Trunk.Storage
{
	/// <summary>
	/// Device state enumeration
	/// </summary>
	public enum MountableDeviceState
	{
		/// <summary>
		/// Device is closed
		/// </summary>
		Closed = 0,

		/// <summary>
		/// Device is being opened
		/// </summary>
		Opening = 1,

		/// <summary>
		/// Device is open and ready for use
		/// </summary>
		Open = 2,

		/// <summary>
		/// Device is closing
		/// </summary>
		Closing = 3
	}
}
