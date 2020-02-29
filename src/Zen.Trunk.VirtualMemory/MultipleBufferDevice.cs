using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using Zen.Trunk.Extensions;
using Zen.Trunk.Utils;

namespace Zen.Trunk.VirtualMemory
{
    /// <summary>
    /// 
    /// </summary>
    /// <seealso cref="BufferDevice" />
    /// <seealso cref="IMultipleBufferDevice" />
    public class MultipleBufferDevice : BufferDevice, IMultipleBufferDevice
	{
		#region Private Types
		private class BufferDeviceInfo : IBufferDeviceInfo
		{
			public BufferDeviceInfo(DeviceId deviceId, ISingleBufferDevice device)
			{
				DeviceId = deviceId;
				Name = device.Name;
				PageCount = device.PageCount;
			}

			public DeviceId DeviceId { get; }

		    public string Name { get; }

            public uint PageCount { get; }
        }
		#endregion

		#region Private Fields
		private readonly IVirtualBufferFactory _bufferFactory;
		private readonly bool _scatterGatherIoEnabled;
		private readonly ConcurrentDictionary<DeviceId, ISingleBufferDevice> _devices =
			new ConcurrentDictionary<DeviceId, ISingleBufferDevice>();
	    private readonly IBufferDeviceFactory _bufferDeviceFactory;
	    #endregion

		#region Public Constructors
		/// <summary>
		/// Initializes a new instance of the <see cref="MultipleBufferDevice"/> class.
		/// </summary>
		public MultipleBufferDevice(
            IVirtualBufferFactory bufferFactory,
            IBufferDeviceFactory bufferDeviceFactory,
            bool scatterGatherIoEnabled)
		{
		    _bufferFactory = bufferFactory;
		    _bufferDeviceFactory = bufferDeviceFactory;
			_scatterGatherIoEnabled = scatterGatherIoEnabled;
		}
		#endregion

		#region Public Properties
		/// <summary>
		/// Gets the buffer factory.
		/// </summary>
		/// <value>
		/// The buffer factory.
		/// </value>
		public override IVirtualBufferFactory BufferFactory => _bufferFactory;
        #endregion

        #region Public Methods
        /// <summary>
        /// Adds a new child single buffer device to this instance.
        /// </summary>
        /// <param name="name">The device name.</param>
        /// <param name="pathName">The pathname for the device storage.</param>
        /// <returns>
        /// The device id.
        /// </returns>
        /// <remarks>
        /// If the device id is zero (it must be zero for the first device)
        /// then the next available device id is used and the value returned.
        /// If the createPageCount is non-zero then the add device call is
        /// treated as a request to create the underlying storage otherwise
        /// the call is treated as a request to open the underlying storage.
        /// </remarks>
        public Task<DeviceId> AddDeviceAsync(string name, string pathName)
	    {
	        return AddDeviceAsync(name, pathName, DeviceId.Zero);
	    }

        /// <summary>
        /// Adds a new child single buffer device to this instance.
        /// </summary>
        /// <param name="name">The device name.</param>
        /// <param name="pathName">The pathname for the device storage.</param>
        /// <param name="deviceId">The device unique identifier.</param>
        /// <param name="createPageCount">The create page count.</param>
        /// <returns>
        /// The device id.
        /// </returns>
        /// <exception cref="ArgumentException">
        /// Primary device has invalid identifier.
        /// or
        /// Device with same id already added.
        /// </exception>
        /// <remarks>
        /// If the device id is zero (it must be zero for the first device)
        /// then the next available device id is used and the value returned.
        /// If the createPageCount is non-zero then the add device call is
        /// treated as a request to create the underlying storage otherwise
        /// the call is treated as a request to open the underlying storage.
        /// </remarks>
        public async Task<DeviceId> AddDeviceAsync(string name, string pathName, DeviceId deviceId, uint createPageCount = 0)
		{
			// Determine whether this is a primary device add
			var isPrimary = _devices.Count == 0;
            if (isPrimary && deviceId != DeviceId.Zero && deviceId != DeviceId.Primary)
            {
                throw new ArgumentException(
                    "Primary device has invalid identifier.", nameof(deviceId));
            }

			// Create device
			var childDevice = _bufferDeviceFactory.CreateSingleBufferDevice(
                name, pathName, createPageCount, _scatterGatherIoEnabled);

			// Add child device with suitable device id
			if (deviceId == DeviceId.Zero)
			{
				// Attempt to add primary device
				if (isPrimary && _devices.TryAdd(DeviceId.Primary, childDevice))
				{
					deviceId = DeviceId.Primary;
				}
                else
				{
					// Non-primary device with zero id means we look for a suitable id
					for (deviceId = DeviceId.FirstSecondary; ; deviceId = deviceId.Next)
					{
						if (!_devices.ContainsKey(deviceId) &&
							_devices.TryAdd(deviceId, childDevice))
						{
							break;
						}
					}
				}
			}
			else if (!_devices.TryAdd(deviceId, childDevice))
			{
				throw new ArgumentException(
					"Device with same id already added.", nameof(deviceId));
			}

			// If we are mounted then we need to open this device
			if (DeviceState == MountableDeviceState.Open)
			{
				await childDevice.OpenAsync().ConfigureAwait(false);
			}

			return deviceId;
		}

        /// <summary>
        /// Removes a child single-buffer device from this instance.
        /// </summary>
        /// <param name="deviceId">The device unique identifier.</param>
        /// <returns></returns>
        public async Task RemoveDeviceAsync(DeviceId deviceId)
		{
		    if (_devices.TryRemove(deviceId, out var childDevice))
			{
				if (DeviceState == MountableDeviceState.Open)
				{
					await childDevice.CloseAsync().ConfigureAwait(false);
				}

				childDevice.Dispose();
			}
		}

        /// <summary>
        /// Resizes the specified device to the soecified number of pages.
        /// </summary>
        /// <param name="deviceId">The device identifier.</param>
        /// <param name="pageCount">The page count.</param>
        /// <remarks>
        /// If the <paramref name="pageCount"/> is negative then the device
        /// will be shrunk.
        /// </remarks>
        public void ResizeDevice(DeviceId deviceId, uint pageCount)
		{
			var device = GetDevice(deviceId);
			device.Resize(pageCount);
		}

        /// <summary>
        /// Loads the page data from the physical page into the supplied buffer.
        /// </summary>
        /// <param name="pageId">The virtual page identifier.</param>
        /// <param name="buffer">The buffer.</param>
        /// <returns>
        /// A <see cref="Task" /> representing the asynchronous operation.
        /// </returns>
        /// <remarks>
        /// When scatter/gather I/O is enabled then the save is deferred until
        /// pending requests are flushed via <see cref="FlushBuffersAsync" />.
        /// </remarks>
        public override Task LoadBufferAsync(VirtualPageId pageId, IVirtualBuffer buffer)
		{
			var device = GetDevice(pageId.DeviceId);
			return device.LoadBufferAsync(pageId, buffer);
		}

        /// <summary>
        /// Saves the page data from the supplied buffer to the physical page.
        /// </summary>
        /// <param name="pageId">The virtual page identifier.</param>
        /// <param name="buffer">The buffer.</param>
        /// <returns></returns>
        public override Task SaveBufferAsync(VirtualPageId pageId, IVirtualBuffer buffer)
		{
			var device = GetDevice(pageId.DeviceId);
			return device.SaveBufferAsync(pageId, buffer);
		}

        /// <summary>
        /// Flushes pending buffer operations.
        /// </summary>
        /// <param name="flushReads">if set to <c>true</c> then read operations are flushed.</param>
        /// <param name="flushWrites">if set to <c>true</c> then write operations are flushed.</param>
        /// <param name="deviceIds">An optional list of device identifiers to restrict flush operation.</param>
        /// <returns>
        /// A <see cref="Task" /> representing the asynchronous operation.
        /// </returns>
        public async Task FlushBuffersAsync(bool flushReads, bool flushWrites, params DeviceId[] deviceIds)
		{
			var subTasks = new List<Task>();

			if (deviceIds == null || deviceIds.Length == 0)
			{
				foreach (var device in _devices.Values)
				{
					subTasks.Add(device.FlushBuffersAsync(flushReads, flushWrites));
				}
			}
			else
			{
				foreach (var deviceId in deviceIds)
				{
					var device = GetDevice(deviceId);
					subTasks.Add(device.FlushBuffersAsync(flushReads, flushWrites));
				}
			}

			// Wait for sub-actions to complete
			await TaskExtra
				.WhenAllOrEmpty(subTasks.ToArray())
				.ConfigureAwait(false);
		}

        /// <summary>
        /// Gets the device information for all child devices.
        /// </summary>
        /// <returns></returns>
        public IEnumerable<IBufferDeviceInfo> GetDeviceInfo()
		{
			var result = new List<IBufferDeviceInfo>();
			foreach (var entry in _devices.ToArray())
			{
				result.Add(new BufferDeviceInfo(entry.Key, entry.Value));
			}
			return result;
		}

        /// <summary>
        /// Gets the device information for a single child device.
        /// </summary>
        /// <param name="deviceId">The device unique identifier.</param>
        /// <returns></returns>
        public IBufferDeviceInfo GetDeviceInfo(DeviceId deviceId)
		{
			var device = GetDevice(deviceId);
			return new BufferDeviceInfo(deviceId, device);
		}
        #endregion

        #region Protected Methods
        /// <summary>
        /// Called when opening the device.
        /// </summary>
        /// <returns></returns>
        protected override Task OnOpenAsync()
		{
			Parallel.ForEach(
				_devices.Values,
				new ParallelOptions
				{
					MaxDegreeOfParallelism = 2
				},
				device =>
				{
					device.OpenAsync();
				});
			return CompletedTask.Default;
		}

        /*protected virtual async Task GetDeviceStatusHandler(GetDeviceStatusRequest request)
		{
			try
			{
				Dictionary<ushort, GetStatusResponse> responseMap =
					new Dictionary<ushort, GetStatusResponse>();
				foreach (var entry in _devices)
				{
					if (!request.IsDeviceIdValid || request.DeviceId == entry.Key)
					{
						GetStatusRequest deviceStatus = new GetStatusRequest();
						GetStatusResponse deviceResponse = await entry.Value.RequestPort
							.PostAndWaitAsync<GetStatusResponse>(deviceStatus);
						responseMap.Add(entry.Key, deviceResponse);
					}
				}
				request.TrySetResult(new GetDeviceStatusResponse(responseMap));
			}
			catch (Exception error)
			{
				request.TrySetException(error);
			}
		}*/

        /// <summary>
        /// Raises the Close event.
        /// </summary>
        /// <returns></returns>
        protected override Task OnCloseAsync()
		{
			Parallel.ForEach(
				_devices.Values,
				new ParallelOptions
				{
					MaxDegreeOfParallelism = 2
				},
				device =>
				{
					device.CloseAsync();
				});
			return CompletedTask.Default;
		}
		#endregion

		#region Private Methods
		private ISingleBufferDevice GetDevice(DeviceId deviceId)
		{
			CheckDisposed();

		    if (!_devices.TryGetValue(deviceId, out var device))
			{
				throw new ArgumentException("Device not found", nameof(deviceId));
			}

            return device;
		}
		#endregion
	}
}
