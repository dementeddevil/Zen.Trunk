
namespace Zen.Trunk.Storage.Data.Index
{
    public interface IIndexPage : IObjectDataPage
    {
        byte Depth { get; set; }

        IndexId IndexId { get; set; }

        IndexManager IndexManager { get; }

        IndexType IndexType { get; set; }

        bool IsIntermediateIndex { get; set; }

        bool IsLeafIndex { get; set; }

        bool IsRootIndex { get; set; }

        LogicalPageId LeftLogicalPageId { get; set; }

        ushort MaxIndexEntries { get; }

        LogicalPageId ParentLogicalPageId { get; set; }

        LogicalPageId RightLogicalPageId { get; set; }
    }
}