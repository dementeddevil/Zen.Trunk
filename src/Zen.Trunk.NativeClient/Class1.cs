using System;
using SuperSocket.ClientEngine;
using SuperSocket.ProtoBase;

namespace Zen.Trunk.NativeClient
{
    public class FixedHeaderReceiveFilter : IReceiveFilter<PackageInfo<string, byte[]>>
    {
        public PackageInfo<string, byte[]> Filter(BufferList data, out int rest)
        {
            //data.
            throw new NotImplementedException();
        }

        public void Reset()
        {
            throw new NotImplementedException();
        }

        public IReceiveFilter<PackageInfo<string, byte[]>> NextReceiveFilter { get; }
        public FilterState State { get; }
    }

    public class ClientCore : EasyClient<PackageInfo<string, byte[]>>
    {
        public void Initialise()
        {
            //var filter = new SuperSocket.ProtoBase.FixedHeaderReceiveFilter
            //base.Initialize();
        }

        public override void Initialize(IReceiveFilter<PackageInfo<string, byte[]>> receiveFilter)
        {
            base.Initialize(receiveFilter);
        }

        protected override void HandlePackage(IPackageInfo package)
        {
            base.HandlePackage(package);
        }
    }
}
