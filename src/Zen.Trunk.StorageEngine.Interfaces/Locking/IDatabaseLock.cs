using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Zen.Trunk.Storage.Locking
{
    public interface IDatabaseLock : ITransactionLock<DatabaseLockType>
    {
    }
}
