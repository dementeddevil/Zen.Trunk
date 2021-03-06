﻿using System;
using Zen.Trunk.Storage.BufferFields;

namespace Zen.Trunk.Storage.Data.Index
{
	/// <summary>
	/// RootIndexInfo defines the core root index information.
	/// </summary>
	public class RootIndexInfo : BufferFieldWrapper
	{
		#region Private Fields
        private readonly BufferFieldIndexId _indexId;
		private readonly BufferFieldObjectId _objectId;
		private readonly BufferFieldStringFixed _name;
		private readonly BufferFieldLogicalPageId _rootLogicalPageId;
		private readonly BufferFieldByte _rootIndexDepth;
		private readonly BufferFieldByte _fillFactor;
		#endregion

		#region Public Constructors
		/// <summary>
		/// Initializes a new instance of the <see cref="RootIndexInfo"/> class.
		/// </summary>
		public RootIndexInfo()
			: this(IndexId.Zero)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="RootIndexInfo"/> class.
		/// </summary>
		/// <param name="indexId">The index id.</param>
		public RootIndexInfo(IndexId indexId)
		{
			_indexId = new BufferFieldIndexId(indexId);
			_objectId = new BufferFieldObjectId(_indexId, ObjectId.Zero);
			_name = new BufferFieldStringFixed(_objectId, 16);
			_rootLogicalPageId = new BufferFieldLogicalPageId(_name);
			_rootIndexDepth = new BufferFieldByte(_rootLogicalPageId);
			_fillFactor = new BufferFieldByte(_rootIndexDepth, 90);
		}
		#endregion

		#region Public Properties
	    /// <summary>
	    /// Gets the index id.
	    /// </summary>
	    /// <value>The index id.</value>
	    public IndexId IndexId
	    {
	        get => _indexId.Value;
	        set => _indexId.Value = value;
	    }

	    /// <summary>
		/// Gets or sets the object id.
		/// </summary>
		/// <value>The owner object id.</value>
		public ObjectId ObjectId
		{
			get => _objectId.Value;
	        set => _objectId.Value = value;
	    }

		/// <summary>
		/// Gets or sets the name.
		/// </summary>
		/// <value>The name.</value>
		public string Name
		{
			get => _name.Value;
		    set => _name.Value = value;
		}

	    /// <summary>
		/// Gets or sets the root logical id.
		/// </summary>
		/// <value>The root logical id.</value>
		public LogicalPageId RootLogicalPageId
		{
			get => _rootLogicalPageId.Value;
	        set => _rootLogicalPageId.Value = value;
	    }

		/// <summary>
		/// Gets or sets the root index depth.
		/// </summary>
		/// <value>The root index depth.</value>
		public byte RootIndexDepth
		{
			get => _rootIndexDepth.Value;
		    set => _rootIndexDepth.Value = value;
		}

        /// <summary>
        /// Gets or sets the fill factor.
        /// </summary>
        /// <value>
        /// The fill factor.
        /// </value>
        /// <exception cref="ArgumentOutOfRangeException">value - Fill-factor must be between 0 and 100.</exception>
		/// <remarks>
		/// Determines the amount that index pages are filled.
		/// A value of 0 or 100 will cause index page to be filled to capacity.
		/// </remarks>
        public byte FillFactor
        {
			get => _fillFactor.Value;
			set
			{
				if (value > 100)
                {
					throw new ArgumentOutOfRangeException(
						nameof(value),
						value,
						"Fill-factor must be between 0 and 100.");
                }

				_fillFactor.Value = value;
			}
        }

		/// <summary>
		/// Gets or sets the index file group id.
		/// </summary>
		/// <value>The index file group id.</value>
		public FileGroupId IndexFileGroupId { get; set; }
		#endregion

		#region Protected Properties
		/// <summary>
		/// Gets the first buffer field object.
		/// </summary>
		/// <value>A <see cref="T:BufferField"/> object.</value>
		protected override BufferField FirstField => _indexId;

	    /// <summary>
		/// Gets the last buffer field object.
		/// </summary>
		/// <value>A <see cref="T:BufferField"/> object.</value>
		protected override BufferField LastField => _fillFactor;
	    #endregion
	}
}
