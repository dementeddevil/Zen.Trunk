using System;
using System.Runtime.InteropServices;
using System.Text;

namespace Zen.Tasks.Wix.InstanceService.InstallerDatabase
{
    /// <summary>
    /// 
    /// </summary>
    /// <seealso cref="System.IDisposable" />
    public class MsiSummaryInformation : IDisposable
	{
		#region Private Fields
		private SafeMsiHandle _summaryInformation;
		#endregion

		#region Public Constructors
		/// <summary>
		/// Initializes a new instance of the <see cref="MsiSummaryInformation"/> class.
		/// </summary>
		/// <param name="summaryInformation">The summary information.</param>
		public MsiSummaryInformation(SafeMsiHandle summaryInformation)
		{
			_summaryInformation = summaryInformation;
		}
		#endregion

		#region Public Properties
		/// <summary>
		/// Gets the property count.
		/// </summary>
		/// <value>The property count.</value>
		public int PropertyCount => Win32.MsiGetPropertyCount(_summaryInformation);
	    #endregion

		#region Public Methods
		/// <summary>
		/// Sets the int16.
		/// </summary>
		/// <param name="property">The property.</param>
		/// <param name="value">The value.</param>
		public void SetInt16(SummaryProperty property, short value)
		{
			CheckSummaryHandle();
			var dummy =
				new System.Runtime.InteropServices.ComTypes.FILETIME();
			Win32.MsiSummaryInfoSetProperty(_summaryInformation, property,
				VarEnum.VT_I2, (int)value, dummy, null);
		}

		/// <summary>
		/// Sets the int32.
		/// </summary>
		/// <param name="property">The property.</param>
		/// <param name="value">The value.</param>
		public void SetInt32(SummaryProperty property, int value)
		{
			CheckSummaryHandle();
			Win32.MsiSummaryInfoSetProperty(_summaryInformation, property,
				VarEnum.VT_I4, value, null, null);
		}

		/// <summary>
		/// Sets the file time.
		/// </summary>
		/// <param name="property">The property.</param>
		/// <param name="value">The value.</param>
		public void SetFileTime(SummaryProperty property,
			System.Runtime.InteropServices.ComTypes.FILETIME value)
		{
			CheckSummaryHandle();
			Win32.MsiSummaryInfoSetProperty(_summaryInformation, property,
				VarEnum.VT_FILETIME, null, value, null);
		}

		/// <summary>
		/// Sets the string.
		/// </summary>
		/// <param name="property">The property.</param>
		/// <param name="value">The value.</param>
		public void SetString(SummaryProperty property, string value)
		{
			CheckSummaryHandle();
			Win32.MsiSummaryInfoSetProperty(_summaryInformation, property,
				VarEnum.VT_LPSTR, null, null, value);
		}

		/// <summary>
		/// Gets the int16.
		/// </summary>
		/// <param name="property">The property.</param>
		/// <returns></returns>
		public short GetInt16(SummaryProperty property)
		{
			CheckSummaryHandle();
			VarEnum dataType;
			int intValue;
			System.Runtime.InteropServices.ComTypes.FILETIME timeValue;
			StringBuilder textValue;
			var result = Win32.MsiSummaryInfoGetProperty(_summaryInformation, property,
				out dataType, out intValue, out timeValue, out textValue, 2);
			if (dataType != VarEnum.VT_I2)
			{
				throw new ArgumentException();
			}
			return (short)intValue;
		}

		/// <summary>
		/// Gets the int32.
		/// </summary>
		/// <param name="property">The property.</param>
		/// <returns></returns>
		public int GetInt32(SummaryProperty property)
		{
			CheckSummaryHandle();
			VarEnum dataType;
			int intValue;
			System.Runtime.InteropServices.ComTypes.FILETIME timeValue;
			StringBuilder textValue;
			var result = Win32.MsiSummaryInfoGetProperty(_summaryInformation, property,
				out dataType, out intValue, out timeValue, out textValue, 4);
			if (dataType != VarEnum.VT_I4)
			{
				throw new ArgumentException();
			}
			return intValue;
		}

		/// <summary>
		/// Gets the file time.
		/// </summary>
		/// <param name="property">The property.</param>
		/// <returns></returns>
		public System.Runtime.InteropServices.ComTypes.FILETIME GetFileTime(SummaryProperty property)
		{
			CheckSummaryHandle();
			VarEnum dataType;
			int intValue;
			System.Runtime.InteropServices.ComTypes.FILETIME timeValue;
			StringBuilder textValue;
			var result = Win32.MsiSummaryInfoGetProperty(_summaryInformation, property,
				out dataType, out intValue, out timeValue, out textValue, 16);
			if (dataType != VarEnum.VT_FILETIME)
			{
				throw new ArgumentException();
			}
			return timeValue;
		}

		/// <summary>
		/// Gets the string.
		/// </summary>
		/// <param name="property">The property.</param>
		/// <param name="bufferSize">Size of the buffer.</param>
		/// <returns></returns>
		public string GetString(SummaryProperty property, int bufferSize)
		{
			CheckSummaryHandle();
			VarEnum dataType;
			int intValue;
			System.Runtime.InteropServices.ComTypes.FILETIME timeValue;
			StringBuilder textValue;
			var result = Win32.MsiSummaryInfoGetProperty(_summaryInformation, property,
				out dataType, out intValue, out timeValue, out textValue, bufferSize);
			if (dataType != VarEnum.VT_LPWSTR &&
				dataType != VarEnum.VT_LPSTR)
			{
				throw new ArgumentException();
			}
			return textValue.ToString();
		}

		/// <summary>
		/// Persists this instance.
		/// </summary>
		public void Persist()
		{
			CheckSummaryHandle();
			Win32.MsiSummaryInfoPersist(_summaryInformation);
		}
		#endregion

		#region Protected Methods
		#endregion

		#region Private Methods
		private void CheckSummaryHandle()
		{
			if (_summaryInformation == null || _summaryInformation.IsClosed ||
				_summaryInformation.IsInvalid)
			{
				throw new ObjectDisposedException(GetType().FullName);
			}
		}
		#endregion

		#region IDisposable Members
		/// <summary>
		/// Releases unmanaged resources and performs other cleanup operations before the
		/// <see cref="MsiSummaryInformation"/> is reclaimed by garbage collection.
		/// </summary>
		~MsiSummaryInformation()
		{
			Dispose(false);
		}

		/// <summary>
		/// Performs application-defined tasks associated with freeing, 
		/// releasing, or resetting unmanaged resources.
		/// </summary>
		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		/// <summary>
		/// Releases unmanaged and - optionally - managed resources
		/// </summary>
		/// <param name="disposing">
		/// <c>true</c> to release both managed and unmanaged resources; 
		/// <c>false</c> to release only unmanaged resources.
		/// </param>
		protected virtual void Dispose(bool disposing)
		{
			if (disposing)
			{
			    _summaryInformation?.Dispose();
			}
			_summaryInformation = null;
		}
		#endregion
	}
}
