using System;

namespace Zen.Trunk.Storage.Locking
{
    public interface IChildTransactionLock<in TLockTypeEnum, TParentLockTypeEnum> : ITransactionLock<TLockTypeEnum>
        where TLockTypeEnum : struct, IComparable, IConvertible, IFormattable
        where TParentLockTypeEnum : struct, IComparable, IConvertible, IFormattable
    {
        ITransactionLock<TParentLockTypeEnum> Parent { get; set; }
    }
}