namespace Zen.Trunk.Storage.Log
{
	internal class CheckPointInfo : BufferFieldWrapper
	{
		private BufferFieldUInt32 _beginFileId;
		private BufferFieldUInt32 _beginOffset;
		private BufferFieldUInt32 _endFileId;
		private BufferFieldUInt32 _endOffset;
		private BufferFieldBitVector8 _status;

		public CheckPointInfo()
		{
			_beginFileId = new BufferFieldUInt32();
			_beginOffset = new BufferFieldUInt32(_beginFileId);
			_endFileId = new BufferFieldUInt32(_beginOffset);
			_endOffset = new BufferFieldUInt32(_endFileId);
			_status = new BufferFieldBitVector8(_endOffset);
		}

		protected override BufferField FirstField
		{
			get
			{
				return _beginFileId;
			}
		}

		protected override BufferField LastField
		{
			get
			{
				return _status;
			}
		}

		internal uint BeginFileId
		{
			get
			{
				return _beginFileId.Value;
			}
			set
			{
				_beginFileId.Value = value;
			}
		}

		internal uint BeginOffset
		{
			get
			{
				return _beginOffset.Value;
			}
			set
			{
				_beginOffset.Value = value;
			}
		}

		internal uint EndFileId
		{
			get
			{
				return _endFileId.Value;
			}
			set
			{
				_endFileId.Value = value;
			}
		}

		internal uint EndOffset
		{
			get
			{
				return _endOffset.Value;
			}
			set
			{
				_endOffset.Value = value;
			}
		}

		internal bool Valid
		{
			get
			{
				return _status.GetBit(1);
			}
			set
			{
				_status.SetBit(1, value);
			}
		}
	}
}