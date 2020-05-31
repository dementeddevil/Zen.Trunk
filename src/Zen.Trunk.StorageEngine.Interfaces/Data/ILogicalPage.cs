
namespace Zen.Trunk.Storage.Data
{
    public interface ILogicalPage : IDataPage
    {
        LogicalPageId LogicalPageId { get; set; }

        LogicalPageId PrevLogicalPageId { get; set; }

        LogicalPageId NextLogicalPageId { get; set; }
    }
}