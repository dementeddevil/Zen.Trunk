// -----------------------------------------------------------------------
// <copyright file="ObjectRefInfo.cs" company="Zen Design Corp">
// TODO: Update copyright text.
// </copyright>
// -----------------------------------------------------------------------

namespace Zen.Trunk.Storage.Data
{
    /// <summary>
    /// TODO: Update summary.
    /// </summary>
    public class ObjectRefInfo : BufferFieldWrapper
    {
        private readonly BufferFieldUInt32 _objectId;
        private readonly BufferFieldByte _objectType;
        private readonly BufferFieldStringFixed _name;
        private readonly BufferFieldUInt64 _firstPageId;

        public ObjectRefInfo()
        {
            _objectId = new BufferFieldUInt32();
            _objectType = new BufferFieldByte(_objectId);
            _name = new BufferFieldStringFixed(_objectType, 32);
            _firstPageId = new BufferFieldUInt64(_name);
        }

        protected override BufferField FirstField => _objectId;

        protected override BufferField LastField => _firstPageId;

        /// <summary>
        /// Gets or sets the file group id.
        /// </summary>
        /// <value>The file group id.</value>
        /// <remarks>
        /// This value is not persisted.
        /// </remarks>
        public FileGroupId FileGroupId
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the root page virtual page id.
        /// </summary>
        /// <value>The root page virtual page id.</value>
        /// <remarks>
        /// This value is not persisted.
        /// </remarks>
        public VirtualPageId RootPageVirtualPageId
        {
            get;
            set;
        }

        public ObjectId ObjectId
        {
            get
            {
                return new ObjectId(_objectId.Value);
            }
            set
            {
                _objectId.Value = value.Value;
            }
        }

        public ObjectType ObjectType
        {
            get
            {
                return new ObjectType(_objectType.Value);
            }
            set
            {
                _objectType.Value = value.Value;
            }
        }

        public string Name
        {
            get
            {
                return _name.Value;
            }
            set
            {
                _name.Value = value;
            }
        }

        public LogicalPageId FirstPageId
        {
            get
            {
                return new LogicalPageId(_firstPageId.Value);
            }
            set
            {
                _firstPageId.Value = value.Value;
            }
        }
    }
}
