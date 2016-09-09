using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Zen.Trunk.Storage.IO;
using Zen.Trunk.Storage.Locking;

namespace Zen.Trunk.Storage.Data.Table
{
    public class TableDataPage : ObjectDataPage
	{
		#region Private Fields
		/// <summary>
		/// Minimum row length. Rows will be padded to this min size.
		/// </summary>
		public const int MinRowBytes = 8;

		/// <summary>
		/// Maximum number of rows allowed on a given page.
		/// </summary>
		public const int MaxRows = 800;

		/// <summary>
		/// Minimum data-block size accounting for row offset table.
		/// </summary>
		public const int MinDataSize = 6400;

		/// <summary>
		/// Row Offset information
		/// </summary>
		private List<RowInfo> _rowInfo;

		/// <summary>
		/// Raw page data
		/// </summary>
		private byte[] _pageData;

		/// <summary>
		/// The total number of bytes used by row-data on this page.
		/// </summary>
		private readonly BufferFieldUInt16 _totalRowDataSize;
		#endregion

		#region Public Constructors
		/// <summary>
		/// Initializes a new instance of the <see cref="TableDataPage"/> class.
		/// </summary>
		public TableDataPage()
		{
		    // ReSharper disable once RedundantBaseQualifier
			_totalRowDataSize = new BufferFieldUInt16(base.LastHeaderField);
		}
		#endregion

		#region Public Properties
		public uint RowCount
		{
			get
			{
				if (_rowInfo == null)
				{
					return 0;
				}
				return (uint)_rowInfo.Count;
			}
		}

		/// <summary>
		/// Returns the amount of free space on the page
		/// </summary>
		public ushort FreeSpace
		{
			get
			{
				var allocated = (ushort)(_totalRowDataSize.Value + (_rowInfo.Count * 2));

				// Account for row offset table terminator
				if (_rowInfo.Count < MaxRows)
				{
					allocated += 2;
				}

				return (ushort)(DataSize - allocated);
			}
		}
		#endregion

		#region Public Methods

	    /// <summary>
	    /// Returns a <see cref="T:RowReaderWriter"/> for accessing row data
	    /// for the specified row.
	    /// </summary>
	    /// <param name="rowIndex">Zero based row index.</param>
	    /// <param name="rowDef">Complete row column definition.</param>
	    /// <param name="canWrite"></param>
	    /// <returns><see cref="T:RowReaderWriter"/></returns>
	    public RowReaderWriter GetRowReaderWriter(
			uint rowIndex, IList<TableColumnInfo> rowDef, bool canWrite)
		{
			Stream rowStream = new MemoryStream(
				_pageData,
				_rowInfo[(int)rowIndex].Offset,
				_rowInfo[(int)rowIndex].Length,
				canWrite);
			return new RowReaderWriter(rowStream, rowDef);
		}

		/// <summary>
		/// Updates the row and deals with page-splitting if needed.
		/// </summary>
		/// <param name="table">The owner table manager.</param>
		/// <param name="rowIndex">Index of the row to be updated.</param>
		/// <param name="newRowData">The new row data.</param>
		/// <param name="length">The length.</param>
		/// <returns></returns>
		/// <exception cref="System.InvalidOperationException">
		/// Failed to add row to new page.
		/// or
		/// Split/update failed after creating new page.
		/// </exception>
		public async Task<Tuple<LogicalPageId, ushort>> UpdateRowAndSplitIfNeeded(
			DatabaseTable table, ushort rowIndex, byte[] newRowData, ushort length)
		{
			// Attempt: #1
			// If we can update inplace then we're done
			if (UpdateRow(rowIndex, newRowData, length))
			{
				return new Tuple<LogicalPageId, ushort>(LogicalPageId, rowIndex);
			}
			else
			{
				// Can't update in-place; delete existing row and delegate to insert
				DeleteRow(rowIndex);
				return await InsertRowAndSplitIfNeeded(table, rowIndex, newRowData, length);
			}
		}

		/// <summary>
		/// Inserts the row and deals with page-splitting if needed.
		/// </summary>
		/// <param name="table">The owner table manager.</param>
		/// <param name="rowIndex">Index of the row to be updated.</param>
		/// <param name="newRowData">The new row data.</param>
		/// <param name="length">The length.</param>
		/// <returns></returns>
		/// <exception cref="System.InvalidOperationException">
		/// Failed to add row to new page.
		/// or
		/// Split/update failed after creating new page.
		/// </exception>
		public async Task<Tuple<LogicalPageId, ushort>> InsertRowAndSplitIfNeeded(
			DatabaseTable table, ushort rowIndex, byte[] newRowData, ushort length)
		{
			// If this table doesn't have a clustered index then add the new
			//	row at the of the last data page.
			TableDataPage splitPage;
			ushort newRowIndex;
			if (table.IsHeap)
			{
				// Determine last page and load if needed
				TableDataPage lastPage;
				if (LogicalPageId == table.DataLastLogicalPageId)
				{
					lastPage = this;
				}
				else
				{
				    lastPage = new TableDataPage
				    {
				        LogicalPageId = table.DataLastLogicalPageId,
				        FileGroupId = FileGroupId,
				    };

				    await lastPage.SetObjectLockAsync(ObjectLock).ConfigureAwait(false);

                    await table.Owner
						.LoadDataPageAsync(new LoadDataPageParameters(lastPage, false, true))
						.ConfigureAwait(false);
				}

				// Attempt to add the row to the end of the last page
				if (lastPage.WriteRowIfSpace(ushort.MaxValue, newRowData, length, out newRowIndex))
				{
					return new Tuple<LogicalPageId, ushort>(lastPage.LogicalPageId, newRowIndex);
				}

				// If we get here then we need to add a new page to the end
                // We will need a schema modification lock on the schema root page
			    await table.SchemaRootPage.SetSchemaLockAsync(SchemaLockType.SchemaModification).ConfigureAwait(false);
			    splitPage = new TableDataPage
			    {
			        FileGroupId = FileGroupId,
			    };
			    await splitPage.SetObjectLockAsync(ObjectLock).ConfigureAwait(false);

                await table.Owner
					.InitDataPageAsync(new InitDataPageParameters(splitPage, true, true, true))
					.ConfigureAwait(false);
				splitPage.PrevLogicalPageId = lastPage.LogicalPageId;
				lastPage.NextLogicalPageId = splitPage.LogicalPageId;
				table.DataLastLogicalPageId = splitPage.LogicalPageId;

				if (!splitPage.WriteRowIfSpace(ushort.MaxValue, newRowData, length, out newRowIndex))
				{
					throw new InvalidOperationException("Failed to add row to new page.");
				}
				return new Tuple<LogicalPageId, ushort>(splitPage.LogicalPageId, newRowIndex);
			}

			// Attempt: #1
			// If we can insert inplace then we're done
			if (WriteRowIfSpace(rowIndex, newRowData, length, out newRowIndex))
			{
				return new Tuple<LogicalPageId, ushort>(LogicalPageId, newRowIndex);
			}

			// Counting back from the maximum row on this page - calculate the
			//	number of rows we need to move before we can insert the desired row
			ushort totalSize = FreeSpace, splitAtRowIndex;
			for (splitAtRowIndex = (ushort)(_rowInfo.Count - 1); splitAtRowIndex > rowIndex && totalSize < length; --splitAtRowIndex)
			{
				totalSize += (ushort)(_rowInfo[splitAtRowIndex].Length + 2);
			}

			// Load the next page in preparation for split.
			TableDataPage nextPage = null;
			if (NextLogicalPageId != LogicalPageId.Zero)
			{
			    nextPage = new TableDataPage
			    {
			        LogicalPageId = NextLogicalPageId,
			        FileGroupId = FileGroupId,
			    };
			    await nextPage.SetObjectLockAsync(ObjectLock).ConfigureAwait(false);

                await table.Owner
					.LoadDataPageAsync(new LoadDataPageParameters(nextPage, false, true))
					.ConfigureAwait(false);
			}

			// If the next page is valid then attempt to split data into
			//	the next page and update row in this page
			if (nextPage != null &&
				totalSize >= length &&
				SplitPage(nextPage, splitAtRowIndex) &&
				WriteRowIfSpace(rowIndex, newRowData, length, out newRowIndex))
			{
				return new Tuple<LogicalPageId, ushort>(LogicalPageId, newRowIndex);
			}

			// Create a new page and link to this page
		    splitPage = new TableDataPage
		    {
		        FileGroupId = FileGroupId,
		    };
		    await SetObjectLockAsync(ObjectLock).ConfigureAwait(false);

            await table.Owner
				.InitDataPageAsync(new InitDataPageParameters(splitPage, true, true, true))
				.ConfigureAwait(false);
			splitPage.PrevLogicalPageId = LogicalPageId;
			splitPage.NextLogicalPageId = NextLogicalPageId;
			if (nextPage != null)
			{
				nextPage.PrevLogicalPageId = splitPage.LogicalPageId;
			}
			NextLogicalPageId = splitPage.LogicalPageId;
			if (nextPage == null)
			{
                // We will need a schema modification lock on the schema root page
                await table.SchemaRootPage.SetSchemaLockAsync(SchemaLockType.SchemaModification).ConfigureAwait(false);

                // We must be splitting the last page and therefore the split
                //	page must be the new last page so update schema
                table.DataLastLogicalPageId = splitPage.LogicalPageId;
			}

			if (totalSize >= length)
			{
				// Split the page at the current row and do row update
				if (!SplitPage(splitPage, splitAtRowIndex) ||
					!WriteRowIfSpace(rowIndex, newRowData, length, out newRowIndex))
				{
					throw new InvalidOperationException(
						"Split/insert failed after creating split page.");
				}
				return new Tuple<LogicalPageId, ushort>(LogicalPageId, newRowIndex);
			}
			else
			{
				// Split page at insert point
				if (!SplitPage(splitPage, rowIndex))
				{
					throw new InvalidOperationException(
						"Split failed after creating split page.");
				}

				// Attempt to write row into this page or next page
				if (WriteRowIfSpace(rowIndex, newRowData, length, out newRowIndex))
				{
					return new Tuple<LogicalPageId, ushort>(LogicalPageId, newRowIndex);
				}
				if (splitPage.WriteRowIfSpace(0, newRowData, length, out newRowIndex))
				{
					return new Tuple<LogicalPageId, ushort>(splitPage.LogicalPageId, newRowIndex);
				}

				// Insert new row between this row and the split page
			    var extraPage = new TableDataPage
			    {
			        FileGroupId = FileGroupId,
			    };
			    await extraPage.SetObjectLockAsync(ObjectLock).ConfigureAwait(false);

                await table.Owner
					.InitDataPageAsync(new InitDataPageParameters(extraPage, true, true, true))
					.ConfigureAwait(false);

				// Update linked list
				NextLogicalPageId = extraPage.LogicalPageId;
				extraPage.PrevLogicalPageId = LogicalPageId;
				extraPage.NextLogicalPageId = splitPage.LogicalPageId;
				splitPage.PrevLogicalPageId = extraPage.LogicalPageId;

				// Insert data into new extra page
				if (!extraPage.WriteRowIfSpace(0, newRowData, length, out newRowIndex))
				{
					throw new InvalidOperationException(
						"Insert failed after creating extra page.");
				}
				return new Tuple<LogicalPageId, ushort>(extraPage.LogicalPageId, newRowIndex);
			}
		}

		/// <summary>
		/// Attempts to copy rows from the current page at the given split
		/// point into the specified page.
		/// The split will fail if the destination page does not contain room
		/// for the source rows either due to maximum row restrictions or space.
		/// It is assumed that <paramref name="nextPage"/> refers to the page
		/// that follows this page instance.
		/// </summary>
		/// <param name="nextPage">
		/// The table data page that follows this page.
		/// </param>
		/// <param name="splitRowIndex"></param>
		/// <returns></returns>
		public bool SplitPage(TableDataPage nextPage, ushort splitRowIndex)
		{
			var splitSize = (ushort)(_totalRowDataSize.Value - _rowInfo[splitRowIndex].Offset);
			var splitRows = (ushort)(_rowInfo.Count - splitRowIndex);
			if (nextPage._rowInfo.Count == 0)
			{
				// Copy page data to other page
				Array.Copy(_pageData, _rowInfo[splitRowIndex].Offset,
					nextPage._pageData, 0,
					splitSize);

				// Move row information
				nextPage._rowInfo.AddRange(_rowInfo.GetRange(splitRowIndex,
					splitRows));
				_rowInfo.RemoveRange(splitRowIndex, splitRows);

				// Clear source page
				Array.Clear(_pageData, _rowInfo[splitRowIndex].Offset,
					splitSize);
			}
			else
			{
				// Partial splits are only supported if the split contents 
				//	will fit on the other page
				if (!nextPage.CanAddRowBlock(splitSize, splitRows))
				{
					return false;
				}

				// Make space for entry
				Array.Copy(nextPage._pageData, 0,
					nextPage._pageData, splitSize,
					nextPage._totalRowDataSize.Value);

				// Copy row data into place
				Array.Copy(_pageData, _rowInfo[splitRowIndex].Offset,
					nextPage._pageData, 0, splitSize);

				// Move split row information
				nextPage._rowInfo.InsertRange(0, _rowInfo.GetRange(
					splitRowIndex, splitRows));
				_rowInfo.RemoveRange(splitRowIndex, splitRows);

				// Adjust row offsets for other page rows
				ushort offset = 0;
				for (var index = 0; index < nextPage._rowInfo.Count; ++index)
				{
					nextPage._rowInfo[index].Offset = offset;
					offset += nextPage._rowInfo[index].Length;
				}
			}

			// Adjust total row data size information for both pages
			_totalRowDataSize.Value -= splitSize;
			nextPage._totalRowDataSize.Value += splitSize;
			nextPage.SetHeaderDirty();
			nextPage.SetDataDirty();
			SetHeaderDirty();
			SetDataDirty();
			return true;
		}

		/// <summary>
		/// Updates a row on a given data page.
		/// </summary>
		/// <param name="rowIndex">Row index being updated</param>
		/// <param name="newRowData">New row data</param>
		/// <param name="length">Row data length</param>
		/// <returns>True if row was updated, false if not.</returns>
		/// <remarks>
		/// This method will fail if the page does not contain enough free 
		/// space to accommodate the new row data in which case the caller
		/// will have to initiate a page-split operation before attempting
		/// the operation again.
		/// </remarks>
		public bool UpdateRow(ushort rowIndex, byte[] newRowData,
			ushort length)
		{
			// Determine reservation space
			var reservationLength = Math.Max(length, (ushort)MinRowBytes);

			// If current row is longer (or the same length) as the new
			//	data then perform an in-place update.
			if (_rowInfo[rowIndex].Length >= reservationLength)
			{
				// Clear page area
				Array.Clear(_pageData, _rowInfo[rowIndex].Offset,
					reservationLength);

				// Copy row data into place
				Array.Copy(newRowData, 0, _pageData,
					_rowInfo[rowIndex].Offset, length);

				// More work to do when row is different size and modified
				//	row is not the last row of the page.
				if (_rowInfo[rowIndex].Length > reservationLength)
				{
					var difference = (ushort)(_rowInfo[rowIndex].Length - reservationLength);
					if (rowIndex < (_rowInfo.Count - 1))
					{
						// Reclaim unused space
						var reclaim = (ushort)(_rowInfo[rowIndex].Length - reservationLength);
						Array.Copy(_pageData, _rowInfo[rowIndex + 1].Offset,
							_pageData, _rowInfo[rowIndex].Offset + reservationLength,
							_totalRowDataSize.Value - _rowInfo[rowIndex + 1].Offset);

						// Adjust row length
						_rowInfo[rowIndex].Length = reservationLength;

						// Adjust row offsets for following rows
						for (var index = (ushort)(rowIndex + 1); index < _rowInfo.Count; ++index)
						{
							_rowInfo[rowIndex].Offset -= difference;
						}
					}

					// Adjust total row-data size
					_totalRowDataSize.Value -= difference;
				}
			}
			else
			{
				// Determine whether updated row will fit on page
				if ((reservationLength - _rowInfo[rowIndex].Length) > FreeSpace)
				{
					// Page must be split on following row if this is
					//	not the last row in the page
					return false;
				}

				// Need to perform delete followed by add
				DeleteRow(rowIndex);

				// Followed by insert
				var result = WriteRowIfSpace(rowIndex, newRowData, length,
					out rowIndex);

				// Sanity check
				if (!result)
				{
					throw new InvalidOperationException(
						"Row removed but failed to insert new row on existing page!");
				}
			}

			return true;
		}

		/// <summary>
		/// Deletes a row from a table page.
		/// </summary>
		/// <param name="rowIndex"></param>
		public void DeleteRow(ushort rowIndex)
		{
			if (rowIndex == (_rowInfo.Count - 1))
			{
				_totalRowDataSize.Value -= _rowInfo[rowIndex].Length;
				Array.Clear(_pageData,
					_rowInfo[rowIndex].Offset,
					_rowInfo[rowIndex].Length);
				_rowInfo.RemoveAt(rowIndex);
			}
			else
			{
				// Move row data into place
				Array.Copy(
					_pageData, _rowInfo[rowIndex + 1].Offset,
					_pageData, _rowInfo[rowIndex].Offset,
					_totalRowDataSize.Value - _rowInfo[rowIndex].Length);
				for (var index = rowIndex + 1; index < _rowInfo.Count; ++index)
				{
					_rowInfo[index].Offset -= _rowInfo[rowIndex].Length;
				}
				_totalRowDataSize.Value -= _rowInfo[rowIndex].Length;
				_rowInfo.RemoveAt(rowIndex);
			}
		}

		/// <summary>
		/// Writes row data to the given insertion point.
		/// </summary>
		/// <param name="insertRow"></param>
		/// <param name="rowData"></param>
		/// <param name="length"></param>
		/// <param name="rowIndex"></param>
		/// <returns></returns>
		public bool WriteRowIfSpace(ushort insertRow, byte[] rowData,
			ushort length, out ushort rowIndex)
		{
			if (!CanAddRow(length))
			{
				rowIndex = 0;
				return false;
			}

			// Create row information and determine minimum reservation size
			var newInfo = new RowInfo();
			newInfo.Length = Math.Max((ushort)MinRowBytes, length);

			if (insertRow >= _rowInfo.Count)
			{
				// Add
				// Determine row offset
				newInfo.Offset = _totalRowDataSize.Value;

				// Clear page area
				Array.Clear(_pageData, newInfo.Offset, newInfo.Length);

				// Copy row data into place
				Array.Copy(rowData, 0, _pageData, newInfo.Offset,
					length);

				// Add row information and update return value
				rowIndex = (ushort)_rowInfo.Count;
				_rowInfo.Add(newInfo);
			}
			else
			{
				// Insert
				// Make space for entry
				Array.Copy(_pageData, _rowInfo[insertRow].Offset,
					_pageData, _rowInfo[insertRow].Offset + newInfo.Length,
					_totalRowDataSize.Value - _rowInfo[insertRow].Offset);

				// Zero rowspace for security
				Array.Clear(_pageData, _rowInfo[insertRow].Offset,
					newInfo.Length);

				// Copy row data into place
				Array.Copy(rowData, 0, _pageData, _rowInfo[insertRow].Offset,
					length);

				// Adjust row offsets for following rows
				newInfo.Offset = _rowInfo[insertRow].Offset;
				for (int index = insertRow; index < _rowInfo.Count; ++index)
				{
					_rowInfo[index].Offset += newInfo.Length;
				}

				// Add row and setup return value
				_rowInfo.Insert(insertRow, newInfo);
				rowIndex = insertRow;
			}
			_totalRowDataSize.Value += newInfo.Length;
			SetHeaderDirty();
			SetDataDirty();
			return true;
		}
		#endregion

		#region Protected Methods
		protected override Task OnInitAsync(EventArgs e)
		{
			PageType = PageType.Table;
			return base.OnInitAsync(e);
		}
		protected override Task OnPreLoadAsync(EventArgs e)
		{
			_pageData = null;
			_rowInfo = null;
			_totalRowDataSize.Value = 0;
			return base.OnPreLoadAsync(e);
		}

		protected override void ReadData(BufferReaderWriter streamManager)
		{
			_pageData = streamManager.ReadBytes((int)DataSize);
		}

		protected override Task OnPostLoadAsync(EventArgs e)
		{
			// Setup page row information
			ReadPageInfo();
			return base.OnPostLoadAsync(e);
		}

		protected override void OnPreSave(EventArgs e)
		{
			// Update row offset block
			WritePageInfo();
			base.OnPreSave(e);
		}

		protected override void WriteData(BufferReaderWriter streamManager)
		{
			if (_pageData != null)
			{
				streamManager.Write(_pageData);
			}
		}
		#endregion

		#region Private Methods
		private void ReadPageInfo()
		{
			_rowInfo = new List<RowInfo>();

			RowInfo lastRow = null;
			var offset = DataSize;
			for (var index = 0; index < MaxRows; ++index)
			{
				// Read row data
				ushort rowDataOffset = _pageData[--offset];
				rowDataOffset |= (ushort)(_pageData[--offset] << 8);
				if (rowDataOffset == 0)
				{
					break;
				}

				// Build row information block
				var newInfo = new RowInfo();
				newInfo.Offset = rowDataOffset;

				// Make provisional assumption about length
				//	this value will only be valid for the last row
				newInfo.Length = (ushort)(_totalRowDataSize.Value - rowDataOffset);

				// If we have a previous row then adjust it's length
				if (lastRow != null)
				{
					lastRow.Length = (ushort)(newInfo.Offset - lastRow.Offset);
				}

				// Add row and update last known row
				_rowInfo.Add(newInfo);
				lastRow = newInfo;
			}
		}

		private void WritePageInfo()
		{
			// Build row offset table
			var offset = DataSize;
			for (var index = 0; index < _rowInfo.Count; ++index)
			{
				// Write row data
				_pageData[--offset] = (byte)(_rowInfo[index].Offset & 0xff);
				_pageData[--offset] = (byte)(_rowInfo[index].Offset >> 8);
			}

			// Account for terminator
			if (_rowInfo.Count < MaxRows)
			{
				_pageData[--offset] = 0;
				_pageData[--offset] = 0;
			}
		}

		/// <summary>
		/// Tests whether the page can accommodate a row of specified size.
		/// </summary>
		/// <param name="rowSize"></param>
		/// <returns></returns>
		private bool CanAddRow(ushort rowSize)
		{
			if (_rowInfo.Count == MaxRows)
			{
				return false;
			}

			// Ensure minimum row size for check
			rowSize = Math.Max(rowSize, (ushort)8);

			// Account for row offset
			if (_rowInfo.Count < MaxRows)
			{
				rowSize += 2;
			}

			// See if it fits
			return (FreeSpace >= rowSize);
		}

	    /// <summary>
	    /// Tests whether the page can accommodate a row of specified size.
	    /// </summary>
	    /// <param name="totalSize"></param>
	    /// <param name="numberOfRows"></param>
	    /// <returns></returns>
	    private bool CanAddRowBlock(ushort totalSize, ushort numberOfRows)
		{
			if ((_rowInfo.Count + numberOfRows) > MaxRows)
			{
				return false;
			}

			// Account for row offsets
			totalSize += (ushort)(numberOfRows * 2);

			// See if it fits
			return (FreeSpace >= totalSize);
		}
		#endregion
	}
}
