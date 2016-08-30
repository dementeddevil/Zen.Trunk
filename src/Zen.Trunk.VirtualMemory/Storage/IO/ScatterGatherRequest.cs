﻿namespace Zen.Trunk.Storage.IO
{
	using System;
	using System.Threading.Tasks.Dataflow;

	[CLSCompliant(false)]
	public class ScatterGatherRequest : TaskRequest<object>
	{
		[CLSCompliant(false)]
		public ScatterGatherRequest(uint physicalPageId, IVirtualBuffer buffer)
		{
			PhysicalPageId = physicalPageId;
			Buffer = buffer;
		}

		public uint PhysicalPageId
		{
			get;
			private set;
		}

		public IVirtualBuffer Buffer
		{
			get;
			private set;
		}
	}
}
