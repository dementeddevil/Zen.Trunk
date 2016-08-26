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
        private readonly BufferFieldObjectId _objectId;
        private readonly BufferFieldObjectType _objectType;
        private readonly BufferFieldStringFixed _name;
        private readonly BufferFieldLogicalPageId _firstPageId;

        public ObjectRefInfo()
        {
            _objectId = new BufferFieldObjectId();
            _objectType = new BufferFieldObjectType(_objectId);
            _name = new BufferFieldStringFixed(_objectType, 32);
            _firstPageId = new BufferFieldLogicalPageId(_name);
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
                return _objectId.Value;
            }
            set
            {
                _objectId.Value = value;
            }
        }

        public ObjectType ObjectType
        {
            get
            {
                return _objectType.Value;
            }
            set
            {
                _objectType.Value = value;
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
                return _firstPageId.Value;
            }
            set
            {
                _firstPageId.Value = value;
            }
        }
    }
}
