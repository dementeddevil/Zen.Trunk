using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autofac;
using Moq;
using Xunit;
using Zen.Trunk.Storage.Data;
using Zen.Trunk.VirtualMemory;

namespace Zen.Trunk.Storage
{
    public class DistributionPageDevice_should : IClassFixture<StorageEngineTestFixture>
    {
        private class FakeDistributionPageDevice : PrimaryDistributionPageDevice
        {
            private readonly IFileGroupDevice _fileGroupDevice;

            public FakeDistributionPageDevice(
                DeviceId deviceId,
                IFileGroupDevice fileGroupDevice
                ) : base(deviceId)
            {
                _fileGroupDevice = fileGroupDevice;
            }

            public override uint DistributionPageOffset => 0;

            protected override void BuildDeviceLifetimeScope(ContainerBuilder builder)
            {
                base.BuildDeviceLifetimeScope(builder);
                builder.RegisterInstance(_fileGroupDevice);
            }
        }

        private readonly StorageEngineTestFixture _fixture;

        public DistributionPageDevice_should(StorageEngineTestFixture fixture)
        {
            _fixture = fixture;
        }

        [Theory(DisplayName = nameof(DistributionPageDevice_should) + "_" + nameof(open_all_distribution_pages))]
        [InlineData(3)]
        public async Task open_all_distribution_pages(int numberOfDistPages)
        {
            var container = _fixture.Scope;
            var fileGroupDevice = FileGroupDeviceBuilder
                .Create(container)
                .WithRootPage(
                    new PrimaryFileGroupRootPage
                    {
                        VirtualPageId = new VirtualPageId(DeviceId.Primary, 0),
                        AllocatedPages = (uint)(numberOfDistPages * 513 + 10),
                    })
                .Build();

            var sut = new FakeDistributionPageDevice(
                DeviceId.Primary,
                fileGroupDevice);
            sut.InitialiseDeviceLifetimeScope(container);

            await sut.OpenAsync(false);

        }
    }

    public class FileGroupDeviceBuilder
    {
        private readonly ISingleBufferDevice _singleBufferDevice = Mock.Of<ISingleBufferDevice>();
        private readonly IFileGroupDevice _fileGroupDevice = Mock.Of<IFileGroupDevice>();
        private readonly IDictionary<VirtualPageId, IDataPage> _pages = new Dictionary<VirtualPageId, IDataPage>();
        private readonly ILifetimeScope _lifetimeScope;

        private FileGroupDeviceBuilder(ILifetimeScope lifetimeScope)
        {
            _lifetimeScope = lifetimeScope;
            Mock.Get(_singleBufferDevice)
                .SetupGet(d => d.BufferFactory)
                .Returns(lifetimeScope.Resolve<IVirtualBufferFactory>());

            Mock.Get(_fileGroupDevice)
                .Setup(d => d.CreateRootPage())
                .Returns(() => new PrimaryFileGroupRootPage());
            Mock.Get(_fileGroupDevice)
                .Setup(d => d.LoadDataPageAsync(It.IsAny<LoadDataPageParameters>()))
                .Callback<LoadDataPageParameters>(
                    r =>
                    {
                        if (r.VirtualPageIdValid)
                        {
                            var knownPage = _pages[r.Page.VirtualPageId];
                            r.Page.PreLoadInternal();
                            r.Page.DataBuffer = knownPage.DataBuffer;
                            r.Page.PostLoadInternal();
                        }
                    })
                .Returns(Task.CompletedTask);
        }

        public static FileGroupDeviceBuilder Create(ILifetimeScope lifetimeScope)
        {
            return new FileGroupDeviceBuilder(lifetimeScope);
        }

        public IFileGroupDevice Build()
        {
            return _fileGroupDevice;
        }

        public FileGroupDeviceBuilder WithPage(VirtualPageId virtualPageId, IDataPage page)
        {
            // Assign buffer and force save
            page.DataBuffer = new PageBuffer(_singleBufferDevice);
            page.Save();

            _pages.Add(virtualPageId, page);
            return this;
        }

        public FileGroupDeviceBuilder WithRootPage(IRootPage rootPage)
        {
            return WithPage(new VirtualPageId(DeviceId.Primary, 0), rootPage);
        }
    }
}
