using System;
using Microsoft.Tools.WindowsInstallerXml;
using Wix = Microsoft.Tools.WindowsInstallerXml.Serialize;

namespace Zen.WindowsInstallerXml.Extensions
{
	/// <summary>
	/// The decompiler for the Windows Installer XML Toolset UI Extension.
	/// </summary>
	public sealed class InstanceServiceDecompiler : DecompilerExtension
	{
		#region Private Fields
		private bool _removeLibraryRows;
		#endregion

		#region Public Constructors
		/// <summary>
		/// Initialises an instance of <see cref="T:UIDecompiler" />.
		/// </summary>
		public InstanceServiceDecompiler()
		{
		}
		#endregion

		#region Public Properties
		/// <summary>
		/// Gets the option to remove the rows from this extension's library.
		/// </summary>
		/// <value>The option to remove the rows from this extension's library.</value>
		public override bool RemoveLibraryRows
		{
			get
			{
				return _removeLibraryRows;
			}
		}
		#endregion

		#region Public Methods
		/// <summary>
		/// Called at the beginning of the decompilation of a database.
		/// </summary>
		/// <param name="tables">The collection of all tables.</param>
		public override void InitializeDecompile(TableCollection tables)
		{
			var propertyTable = tables["Property"];

			if (propertyTable != null)
			{
				foreach (Row row in propertyTable.Rows)
				{
					if ("WixUI_Mode" == (string)row[0])
					{
						var uiRef = new Wix.UIRef();
						uiRef.Id = String.Concat("WixUI_", (string)row[1]);

						Core.RootElement.AddChild(uiRef);
						_removeLibraryRows = true;
						break;
					}
				}
			}
		}
		#endregion

		#region Protected Methods
		#endregion

		#region Private Methods
		#endregion
	}
}
