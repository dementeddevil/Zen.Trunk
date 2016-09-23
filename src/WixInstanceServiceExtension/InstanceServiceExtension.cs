using System.Reflection;
using Microsoft.Tools.WindowsInstallerXml;

namespace Zen.WindowsInstallerXml.Extensions
{
	/// <summary>
	/// The Windows Installer XML Toolset SUI Extension.
	/// </summary>
	/// <remarks>
	/// Extends the default WiX UI templates by providing support for 
	/// installing Windows Services.
	/// </remarks>
	public sealed class InstanceServiceExtension : WixExtension
	{
		#region Private Fields
		private InstanceServiceCompiler _compilerExtension;
        //private InstanceServiceDecompiler _decompilerExtension;
        private Library _library;
		private TableDefinitionCollection _tableDefinitions;
		#endregion

		#region Public Properties
		/// <summary>
		/// Gets the default culture.
		/// </summary>
		/// <value>The default culture.</value>
		public override string DefaultCulture => "en-us";

	    /// <summary>
		/// Gets the optional table definitions for this extension.
		/// </summary>
		/// <value>The optional table definitions for this extension.</value>
		public override TableDefinitionCollection TableDefinitions => 
            _tableDefinitions ?? (_tableDefinitions =
		        LoadTableDefinitionHelper(Assembly.GetExecutingAssembly(),
		            "Zen.WindowsInstallerXml.Extensions.Data.tables.xml"));

	    /// <summary>
		/// Gets the compiler extension.
		/// </summary>
		/// <value>The compiler extension.</value>
		public override CompilerExtension CompilerExtension => _compilerExtension ?? (_compilerExtension = new InstanceServiceCompiler());

	    /*/// <summary>
		/// Gets the optional decompiler extension.
		/// </summary>
		/// <value>The optional decompiler extension.</value>
		public override DecompilerExtension DecompilerExtension
		{
			get
			{
				if (_decompilerExtension == null)
				{
					_decompilerExtension = new InstanceServiceDecompiler ();
				}

				return _decompilerExtension;
			}
		}*/
        #endregion

        #region Public Methods
        /// <summary>
        /// Gets the library associated with this extension.
        /// </summary>
        /// <param name="tableDefinitions">The table definitions to use while loading the library.</param>
        /// <returns>The library for this extension.</returns>
        public override Library GetLibrary(TableDefinitionCollection tableDefinitions)
        {
            return _library ?? (_library =
                LoadLibraryHelper(
                    Assembly.GetExecutingAssembly(),
                    "Zen.WindowsInstallerXml.Extensions.Data.InstanceService.wixlib",
                    tableDefinitions));
        }
		#endregion
	}
}
