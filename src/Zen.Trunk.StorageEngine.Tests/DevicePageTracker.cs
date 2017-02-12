using System.Collections.Generic;
using Autofac;
using Zen.Trunk.Storage.Data;

namespace Zen.Trunk.Storage
{
    public class DevicePageTracker
    {
        private readonly ILifetimeScope _scope;
        private readonly IBufferDevice _bufferDevice;
        private readonly Dictionary<VirtualPageId, PageBuffer> _pages =
            new Dictionary<VirtualPageId, PageBuffer>();

        public DevicePageTracker(ILifetimeScope scope, IBufferDevice bufferDevice)
        {
            _scope = scope;
            _bufferDevice = bufferDevice;
        }

        /// <summary>
        /// Creates a page of the specified type.
        /// </summary>
        /// <typeparam name="TPage">The type of the page.</typeparam>
        /// <returns></returns>
        public TPage CreatePage<TPage>(VirtualPageId pageId)
            where TPage : DataPage, new()
        {
            var treatAsInit = false;
            PageBuffer pageBuffer;
            if (!_pages.TryGetValue(pageId, out pageBuffer))
            {
                pageBuffer = new PageBuffer(_bufferDevice);
                pageBuffer.InitAsync(pageId, LogicalPageId.Zero);
                _pages.Add(pageId, pageBuffer);
                treatAsInit = true;
            }

            var page = new TPage();
            page.SetLifetimeScope(_scope);

            page.VirtualPageId = pageId;

            if (treatAsInit)
            {
                page.PreInitInternal();
                page.DataBuffer = pageBuffer;
                page.OnInitInternal();
            }
            else
            {
                page.PreLoadInternal();
                page.DataBuffer = pageBuffer;
                page.PostLoadInternal();
            }

            return page;
        }
    }
}