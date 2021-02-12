using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NAudio.Wave;
using Zen.Trunk.IO;
using Zen.Trunk.Storage.BufferFields;

namespace Zen.Trunk.Storage.Data.Audio
{
    public class AudioSchemaPage : ObjectSchemaPage
    {
        #region Private Fields
        // Header fields
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
        private readonly BufferFieldByte _indexCount;

        // Data fields
        private PageItemCollection<RootAudioIndexInfo> _indices;
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
            _indexCount = new BufferFieldByte(_dataLastLogicalPageId);
        }
        #endregion

        #region Public Properties
        /// <summary>
        /// Gets the minimum number of bytes required for the header block.
        /// </summary>
        public override uint MinHeaderSize => base.MinHeaderSize + 43;

        /// <summary>
        /// Gets or sets the wave format.
        /// </summary>
        /// <value>
        /// The wave format.
        /// </value>
        /// <exception cref="NotSupportedException">Wave formats that have extra size bigger than {DataSize} are not currently supported.</exception>
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

        /// <summary>
        /// Gets or sets the total bytes.
        /// </summary>
        /// <value>
        /// The total bytes.
        /// </value>
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

        /// <summary>
        /// Gets the samples per page.
        /// </summary>
        /// <value>
        /// The samples per page.
        /// </value>
        public int SamplesPerPage => (int)(DataSize / WaveFormat.BlockAlign);

        /// <summary>
        /// Gets the sample bytes per page.
        /// </summary>
        /// <value>
        /// The sample bytes per page.
        /// </value>
        public int SampleBytesPerPage => SamplesPerPage * WaveFormat.BlockAlign;

        /// <summary>
        /// Gets the total samples.
        /// </summary>
        /// <value>
        /// The total samples.
        /// </value>
        public long TotalSamples =>  TotalBytes / WaveFormat.BlockAlign;

        /// <summary>
        /// Gets the root index collection for this page.
        /// </summary>
        public IList<RootAudioIndexInfo> Indices
        {
            get
            {
                if (_indices == null)
                {
                    _indices = new PageItemCollection<RootAudioIndexInfo>(this);
                }
                return _indices;
            }
        }
        #endregion

        #region Protected Properties
        /// <summary>
        /// Gets the last header field.
        /// </summary>
        protected override BufferField LastHeaderField => _indexCount;
        #endregion

        #region Protected Methods
        /// <summary>
        /// Raises the <see cref="E:Init" /> event.
        /// </summary>
        /// <returns></returns>
        protected override Task OnInitAsync()
        {
            PageType = PageType.Audio;
            return base.OnInitAsync();
        }

        /// <summary>
        /// Writes the page header block to the specified buffer writer.
        /// </summary>
        /// <param name="streamManager">The stream manager.</param>
        protected override void WriteHeader(SwitchingBinaryWriter streamManager)
        {
            _indexCount.Value = (byte)(_indices?.Count ?? 0);
            base.WriteHeader(streamManager);
        }

        /// <summary>
        /// Reads the page data block from the specified buffer reader.
        /// </summary>
        /// <param name="streamManager">The stream manager.</param>
        protected override void ReadData(SwitchingBinaryReader streamManager)
        {
            _indices?.Clear();
            for (byte index = 0; index < _indexCount.Value; ++index)
            {
                var rootIndex = new RootAudioIndexInfo();
                rootIndex.Read(streamManager);
                Indices.Add(rootIndex);
            }
        }

        /// <summary>
        /// Writes the page data block to the specified buffer writer.
        /// </summary>
        /// <param name="streamManager">The stream manager.</param>
        protected override void WriteData(SwitchingBinaryWriter streamManager)
        {
            if (_indices != null)
            {
                foreach (var index in _indices)
                {
                    index.Write(streamManager);
                }
            }
        }
        #endregion
    }
}
