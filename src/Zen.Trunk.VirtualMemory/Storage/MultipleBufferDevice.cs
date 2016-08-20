namespace Zen.Trunk.Storage
{
	using System;
	using System.Collections.Concurrent;
	using System.Collections.Generic;
	using System.Threading.Tasks;
	using Zen.Trunk.Storage.IO;

	[CLSCompliant(false)]
	public class MultipleBufferDevice : BufferDevice, IMultipleBufferDevice
	{
		#region Private Types
		private class BufferDeviceInfo : IBufferDeviceInfo
		{
			private readonly ushort _deviceId;
			private readonly string _name;
			private readonly uint _pageCount;

			public BufferDeviceInfo(ushort deviceId, ISingleBufferDevice device)
			{
				_deviceId = deviceId;
				_name = device.Name;
				_pageCount = device.PageCount;
			}

			public ushort DeviceId => _deviceId;

		    public string Name => _name;

		    public uint PageCount => _pageCount;
		}
		#endregion

		#region Private Fields
		private readonly IVirtualBufferFactory _bufferFactory;
		private readonly bool _scatterGatherIoEnabled;
		private readonly ConcurrentDictionary<ushort, ISingleBufferDevice> _devices =
			new ConcurrentDictionary<ushort, ISingleBufferDevice>();
		#endregion

		#region Public Constructors
		/// <summary>
		/// Initializes a new instance of the <see cref="MultipleBufferDevice"/> class.
		/// </summary>
		public MultipleBufferDevice(IVirtualBufferFactory bufferFactory, bool scatterGatherIoEnabled)
		{
			_bufferFactory = bufferFactory;
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
		public async Task<ushort> AddDeviceAsync(string name, string pathName, ushort deviceId = 0, uint createPageCount = 0)
		{
			// Determine whether this is a primary device add
			var isPrimary = false;
			if (_devices.Count == 0)
			{
				isPrimary = true;
			}

			// Create device
			ISingleBufferDevice childDevice = null;
			if (createPageCount > 0)
			{
				childDevice = new SingleBufferDevice(
					_bufferFactory,
					isPrimary,
					name,
					pathName,
					_scatterGatherIoEnabled,
					createPageCount);
			}
			else
			{
				childDevice = new SingleBufferDevice(
					_bufferFactory,
					isPrimary,
					name,
					pathName,
					_scatterGatherIoEnabled);
			}

			// Add child device with suitable device id
			if (deviceId == 0)
			{
				// Attempt to add primary device
				if (isPrimary && _devices.TryAdd(1, childDevice))
				{
					deviceId = 1;
				}

				if (deviceId == 0)
				{
					// Non-primary device with zero id means we look for a suitable id
					for (deviceId = 2; ; ++deviceId)
					{
						if (!_devices.ContainsKey(deviceId) &&
							_devices.TryAdd(deviceId, childDevice))
						{
							break;
						}
					}
				}
			}
			else if (isPrimary && deviceId != 1)
			{
				throw new ArgumentException(
					"Primary device must have a device id of one.");
			}
			else if (!_devices.TryAdd(deviceId, childDevice))
			{
				throw new ArgumentException(
					"Device with same id already added.");
			}

			// If we are mounted then we need to open this device
			if (DeviceState == MountableDeviceState.Open)
			{
				await childDevice.OpenAsync().ConfigureAwait(false);
			}

			return deviceId;
		}

		public async Task RemoveDeviceAsync(ushort deviceId)
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

		public uint ExpandDevice(ushort deviceId, int pageCount)
		{
			var device = GetDevice(deviceId);
			return device.ExpandDevice(pageCount);
		}

		public Task LoadBufferAsync(VirtualPageId pageId, VirtualBuffer buffer)
		{
			var device = GetDevice(pageId.DeviceId);
			return device.LoadBufferAsync(pageId.PhysicalPageId, buffer);
		}

		public Task SaveBufferAsync(VirtualPageId pageId, VirtualBuffer buffer)
		{
			var device = GetDevice(pageId.DeviceId);
			return device.SaveBufferAsync(pageId.PhysicalPageId, buffer);
		}

		public async Task FlushBuffersAsync(bool flushReads, bool flushWrites, params ushort[] deviceIds)
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

		public IBufferDeviceInfo GetDeviceInfo(ushort deviceId)
		{
			var device = GetDevice(deviceId);
			return new BufferDeviceInfo(deviceId, device);
		}
		#endregion

		#region Protected Methods
		protected override Task OnOpen()
		{
			var subTasks = new List<Task>();
			var result = Parallel.ForEach(
				_devices.Values,
				new ParallelOptions
				{
					MaxDegreeOfParallelism = 2
				},
				(device) =>
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
			var subTasks = new List<Task>();
			var result = Parallel.ForEach(
				_devices.Values,
				new ParallelOptions
				{
					MaxDegreeOfParallelism = 2
				},
				(device) =>
				{
					device.CloseAsync();
				});
			return CompletedTask.Default;
		}
		#endregion

		#region Private Methods
		private ISingleBufferDevice GetDevice(ushort deviceId)
		{
			CheckDisposed();
			ISingleBufferDevice device = null;
			if (!_devices.TryGetValue(deviceId, out device))
			{
				throw new ArgumentException("Device not found");
			}
			return device;
		}
		#endregion
	}
}
