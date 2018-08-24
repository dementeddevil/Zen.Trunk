using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Zen.Trunk.Storage.Locking
{
    public interface IDatabaseLock : ITransactionLock<DatabaseLockType>
    {
    }
}
