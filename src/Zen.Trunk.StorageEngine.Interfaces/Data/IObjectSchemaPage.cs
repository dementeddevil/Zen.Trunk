using System.Threading.Tasks;
using Zen.Trunk.Storage.Locking;

namespace Zen.Trunk.Storage.Data
{
    public interface IObjectSchemaPage : IObjectPage
    {
        SchemaLockType SchemaLock { get; }

        Task SetSchemaLockAsync(SchemaLockType value);
    }
}