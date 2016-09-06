using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using Zen.Trunk.Extensions;
using Zen.Trunk.Utils;

namespace Zen.Trunk.Storage.IO
{
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
		public MultipleBufferDevice(IVirtualBufferFactory bufferFactory, IBufferDeviceFactory bufferDeviceFactory, bool scatterGatherIoEnabled)
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

	    public Task<DeviceId> AddDeviceAsync(string name, string pathName)
	    {
	        return AddDeviceAsync(name, pathName, DeviceId.Zero);
	    }

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

		public async Task RemoveDeviceAsync(DeviceId deviceId)
		{
			ISingleBufferDevice childDevice;
			if (_devices.TryRemove(deviceId, out childDevice))
			{
				if (DeviceState == MountableDeviceState.Open)
				{
					await childDevice.CloseAsync();
				}

				childDevice.Dispose();
			}
		}

		public uint ExpandDevice(DeviceId deviceId, int pageCount)
		{
			var device = GetDevice(deviceId);
			return device.ExpandDevice(pageCount);
		}

		public Task LoadBufferAsync(VirtualPageId pageId, IVirtualBuffer buffer)
		{
			var device = GetDevice(pageId.DeviceId);
			return device.LoadBufferAsync(pageId.PhysicalPageId, buffer);
		}

		public Task SaveBufferAsync(VirtualPageId pageId, IVirtualBuffer buffer)
		{
			var device = GetDevice(pageId.DeviceId);
			return device.SaveBufferAsync(pageId.PhysicalPageId, buffer);
		}

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

		public IEnumerable<IBufferDeviceInfo> GetDeviceInfo()
		{
			var result = new List<IBufferDeviceInfo>();
			foreach (var entry in _devices.ToArray())
			{
				result.Add(new BufferDeviceInfo(entry.Key, entry.Value));
			}
			return result;
		}

		public IBufferDeviceInfo GetDeviceInfo(DeviceId deviceId)
		{
			var device = GetDevice(deviceId);
			return new BufferDeviceInfo(deviceId, device);
		}
		#endregion

		#region Protected Methods
		protected override Task OnOpen()
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

		protected override Task OnClose()
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

			ISingleBufferDevice device;
			if (!_devices.TryGetValue(deviceId, out device))
			{
				throw new ArgumentException("Device not found", nameof(deviceId));
			}

            return device;
		}
		#endregion
	}
}
