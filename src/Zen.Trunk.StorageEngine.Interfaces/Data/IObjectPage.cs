using System.Threading.Tasks;
using Zen.Trunk.Storage.Locking;

namespace Zen.Trunk.Storage.Data
{
    public interface IObjectPage : ILogicalPage
    {
        ObjectId ObjectId { get; set; }

        ObjectLockType ObjectLock { get; }

        Task SetObjectLockAsync(ObjectLockType value);
    }
}