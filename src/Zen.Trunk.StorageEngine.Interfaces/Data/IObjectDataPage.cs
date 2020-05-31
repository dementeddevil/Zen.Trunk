using System.Threading.Tasks;
using Zen.Trunk.Storage.Locking;

namespace Zen.Trunk.Storage.Data
{
    public interface IObjectDataPage : IObjectPage
    {
        DataLockType PageLock { get; }

        void SetDirtyState();

        Task SetPageLockAsync(DataLockType value);
    }
}