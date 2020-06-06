using System;
using System.Threading.Tasks;
using Zen.Trunk.VirtualMemory;

namespace Zen.Trunk.Storage.Data
{
    public interface IFileGroupDevice : IMountableDevice
    {
        FileGroupId FileGroupId { get; }

        string FileGroupName { get; }

        bool IsPrimaryFileGroup { get; }

        DeviceId PrimaryDeviceId { get; }

        Task<Tuple<DeviceId, string>> AddDataDeviceAsync(AddDataDeviceParameters deviceParams);

        Task<ObjectId> AddAudioAsync(AddAudioParameters audioParams);

        Task<ObjectId> AddTableAsync(AddTableParameters tableParams);

        Task<IndexId> AddTableIndexAsync(AddTableIndexParameters indexParams);

        Task<VirtualPageId> AllocateDataPageAsync(AllocateDataPageParameters allocParams);

        Task CreateDistributionPagesAsync(DeviceId deviceId, uint startPhysicalId, uint endPhysicalId);

        IRootPage CreateRootPage();

        Task DeallocateDataPageAsync(DeallocateDataPageParameters deallocParams);

        Task ExpandDataDeviceAsync(ExpandDataDeviceParameters parameters);

        Task InitDataPageAsync(InitDataPageParameters initParams);

        Task InsertReferenceInformationAsync(InsertReferenceInformationRequestParameters parameters);

        Task<bool> InsertTableData(InsertTableDataParameters tableDataParams);

        Task LoadDataPageAsync(LoadDataPageParameters loadParams);

        Task<TPageType> LoadOrCreateNextLinkedPageAsync<TPageType>(TPageType previousPage, Func<TPageType> pageCreationFunc) where TPageType : ILogicalPage;

        Task ProcessDistributionPageAsync(IDistributionPage page);

        Task<bool> RemoveDataDeviceAsync(RemoveDataDeviceParameters deviceParams);
    }
}