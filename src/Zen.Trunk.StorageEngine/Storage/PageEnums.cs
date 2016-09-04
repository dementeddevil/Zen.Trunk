namespace Zen.Trunk.Storage
{
	using System;
	using System.Collections.Generic;
	using System.Text;

	/// <summary>
	/// Define the page status.
	/// </summary>
	public enum PageType : byte
	{
		/// <summary>
		/// Indicates the page is new and uninitialised.
		/// </summary>
		New = 0,

		/// <summary>
		/// Indicates the page is a generic data page.
		/// </summary>
		Data = 1,

		/// <summary>
		/// Indicates the page is a distribution page.
		/// </summary>
		Distribution = 2,

		/// <summary>
		/// Indicates the page is a sample page.
		/// </summary>
		Sample = 3,

		/// <summary>
		/// Indicates the page is a table page.
		/// </summary>
		Table = 4,

		/// <summary>
		/// Indicates the page is an index page.
		/// </summary>
		Index = 5,

		/// <summary>
		/// Indicates the page is a root page.
		/// </summary>
		Root = 6,
	}

	/// <summary>
	/// IndexType defines the type of index page.
	/// </summary>
	[Flags]
	public enum IndexType
	{
		/// <summary>
		/// Page is an index root page.
		/// </summary>
		/// <remarks>
		/// Every index contains a single root page.
		/// When the index is first created, the root page is also a leaf page.
		/// When the root page becomes full it is split into two leaf pages and
		/// a new root page is generated.
		/// </remarks>
		Root = 1,

		/// <summary>
		/// Page is an intermediate link page.
		/// </summary>
		/// <remarks>
		/// Intermediate pages are internal pages that allow traversal from
		/// the root page to the desired leaf page containing the data required.
		/// </remarks>
		Intermediate = 2,

		/// <summary>
		/// Page is a leaf page.
		/// </summary>
		/// <remarks>
		/// Leaf pages are not used with clustered indices. In this case the
		/// actual table data page is the leaf page of the index.
		/// </remarks>
		Leaf = 4,
	}
}
