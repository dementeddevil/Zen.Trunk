using System;
using Zen.Trunk.Storage.BufferFields;
using NAudio.Wave;
using System.Threading.Tasks;

namespace Zen.Trunk.Storage.Data.Audio
{
    public class AudioSchemaPage : ObjectSchemaPage
    {
        #region Private Fields
        private readonly BufferFieldInt16 _waveFormatTag;
        private readonly BufferFieldInt16 _channelCount;
        private readonly BufferFieldInt32 _sampleRate;
        private readonly BufferFieldInt32 _averageBytesPerSecond;
        private readonly BufferFieldInt16 _blockAlign;
        private readonly BufferFieldInt16 _bitsPerSample;
        private readonly BufferFieldInt16 _extraSize;
        private readonly BufferFieldInt64 _totalBytes;
        private readonly BufferFieldUInt64 _dataFirstLogicalPageId;
        private readonly BufferFieldUInt64 _dataLastLogicalPageId;
        #endregion

        #region Public Constructors
        public AudioSchemaPage()
        {
            _waveFormatTag = new BufferFieldInt16(base.LastHeaderField);
            _channelCount = new BufferFieldInt16(_waveFormatTag);
            _sampleRate = new BufferFieldInt32(_channelCount);
            _averageBytesPerSecond = new BufferFieldInt32(_sampleRate);
            _blockAlign = new BufferFieldInt16(_averageBytesPerSecond);
            _bitsPerSample = new BufferFieldInt16(_blockAlign);
            _extraSize = new BufferFieldInt16(_bitsPerSample);
            _totalBytes = new BufferFieldInt64(_extraSize);
            _dataFirstLogicalPageId = new BufferFieldUInt64(_totalBytes);
            _dataLastLogicalPageId = new BufferFieldUInt64(_dataFirstLogicalPageId);

            IsManagedData = false;
        }
        #endregion

        #region Public Properties
        public override uint MinHeaderSize => base.MinHeaderSize + 26;

        public WaveFormat WaveFormat
        {
            get
            {
                return WaveFormat.CreateCustomFormat(
                    (WaveFormatEncoding)_waveFormatTag.Value,
                    _sampleRate.Value,
                    _channelCount.Value,
                    _averageBytesPerSecond.Value,
                    _blockAlign.Value,
                    _bitsPerSample.Value);
            }
            set
            {
                CheckReadOnly();
                if (_waveFormatTag.Value != (short)value.Encoding)
                {
                    _waveFormatTag.Value = (short)value.Encoding;
                    SetHeaderDirty();
                }
                if (_channelCount.Value != value.Channels)
                {
                    _channelCount.Value = (short)value.Channels;
                    SetHeaderDirty();
                }
                if (_sampleRate.Value != value.SampleRate)
                {
                    _sampleRate.Value = value.SampleRate;
                    SetHeaderDirty();
                }
                if (_averageBytesPerSecond.Value != value.AverageBytesPerSecond)
                {
                    _averageBytesPerSecond.Value = value.AverageBytesPerSecond;
                    SetHeaderDirty();
                }
                if (_blockAlign.Value != value.BlockAlign)
                {
                    _blockAlign.Value = (short)value.BlockAlign;
                    SetHeaderDirty();
                }
                if (_bitsPerSample.Value != value.BitsPerSample)
                {
                    _bitsPerSample.Value = (short)value.BitsPerSample;
                    SetHeaderDirty();
                }
                if (_extraSize.Value != value.ExtraSize)
                {
                    if (value.ExtraSize > DataSize)
                    {
                        throw new NotSupportedException(
                            $"Wave formats that have extra size bigger than {DataSize} are not currently supported.");
                    }

                    _extraSize.Value = (short)value.ExtraSize;
                    SetHeaderDirty();
                }
            }
        }

        public long TotalBytes
        {
            get => _totalBytes.Value;
            set
            {
                CheckReadOnly();
                if (_totalBytes.Value != value)
                {
                    _totalBytes.Value = value;
                    SetHeaderDirty();
                }
            }
        }

        /// <summary>
        /// Gets or sets the logical page identifier of the first data page for the table.
        /// </summary>
        /// <value>
        /// The logical page identifier.
        /// </value>
        public LogicalPageId DataFirstLogicalPageId
        {
            get => new LogicalPageId(_dataFirstLogicalPageId.Value);
            set
            {
                if (_dataFirstLogicalPageId.Value != value.Value)
                {
                    _dataFirstLogicalPageId.Value = value.Value;
                    SetHeaderDirty();
                }
            }
        }

        /// <summary>
        /// Gets or sets the logical page identifier of the last data page for the table.
        /// </summary>
        /// <value>
        /// The logical page identifier.
        /// </value>
        public LogicalPageId DataLastLogicalPageId
        {
            get => new LogicalPageId(_dataLastLogicalPageId.Value);
            set
            {
                if (_dataLastLogicalPageId.Value != value.Value)
                {
                    _dataLastLogicalPageId.Value = value.Value;
                    SetHeaderDirty();
                }
            }
        }
        #endregion

        #region Protected Properties
        /// <summary>
        /// Gets the last header field.
        /// </summary>
        protected override BufferField LastHeaderField => _totalBytes;
        #endregion

        #region Protected Methods
        protected override Task OnInitAsync(EventArgs e)
        {
            PageType = PageType.Audio;
            return base.OnInitAsync(e);
        } 
        #endregion
    }
}
