// -----------------------------------------------------------------------
// <copyright file="ObjectReferenceBufferFieldWrapper.cs" company="Zen Design Software">
// © Zen Design Software 2009 - 2018
// </copyright>
// -----------------------------------------------------------------------

using Zen.Trunk.VirtualMemory;

namespace Zen.Trunk.Storage.BufferFields
{
    /// <summary>
    /// Contains information used to track object references.
    /// </summary>
    /// <seealso cref="BufferFieldWrapper" />
    public class ObjectReferenceBufferFieldWrapper : BufferFieldWrapper
    {
        private readonly BufferFieldObjectId _objectId;
        private readonly BufferFieldObjectType _objectType;
        private readonly BufferFieldFileGroupId _fileGroupId;
        private readonly BufferFieldStringFixed _name;
        private readonly BufferFieldLogicalPageId _firstPageId;

        /// <summary>
        /// Initializes a new instance of the <see cref="ObjectReferenceBufferFieldWrapper"/> class.
        /// </summary>
        public ObjectReferenceBufferFieldWrapper()
        {
            _objectId = new BufferFieldObjectId();
            _objectType = new BufferFieldObjectType(_objectId);
            _fileGroupId = new BufferFieldFileGroupId(_objectType);
            _name = new BufferFieldStringFixed(_fileGroupId, 32);
            _firstPageId = new BufferFieldLogicalPageId(_name);
        }

        /// <summary>
        /// Gets the first buffer field object.
        /// </summary>
        /// <value>
        /// A <see cref="T:BufferField" /> object.
        /// </value>
        protected override BufferField FirstField => _objectId;

        /// <summary>
        /// Gets the last buffer field object.
        /// </summary>
        /// <value>
        /// A <see cref="T:BufferField" /> object.
        /// </value>
        protected override BufferField LastField => _firstPageId;

        /// <summary>
        /// Gets or sets the root page virtual page id.
        /// </summary>
        /// <value>The root page virtual page id.</value>
        /// <remarks>
        /// This value is not persisted.
        /// </remarks>
        public VirtualPageId RootPageVirtualPageId { get; set; }

        /// <summary>
        /// Gets or sets the object identifier.
        /// </summary>
        /// <value>
        /// The object identifier.
        /// </value>
        public ObjectId ObjectId
        {
            get => _objectId.Value;
            set => _objectId.Value = value;
        }

        /// <summary>
        /// Gets or sets the type of the object.
        /// </summary>
        /// <value>
        /// The type of the object.
        /// </value>
        public ObjectType ObjectType
        {
            get => _objectType.Value;
            set => _objectType.Value = value;
        }

        /// <summary>
        /// Gets or sets the file group identifier.
        /// </summary>
        /// <value>The file group identifier.</value>
        public FileGroupId FileGroupId
        {
            get => _fileGroupId.Value;
            set => _fileGroupId.Value = value;
        }

        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        /// <value>
        /// The name.
        /// </value>
        public string Name
        {
            get => _name.Value;
            set => _name.Value = value;
        }

        /// <summary>
        /// Gets or sets the first logical page identifier.
        /// </summary>
        /// <value>
        /// The first logical page identifier.
        /// </value>
        public LogicalPageId FirstLogicalPageId
        {
            get => _firstPageId.Value;
            set => _firstPageId.Value = value;
        }
    }
}
