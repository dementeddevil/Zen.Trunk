using System;
using Zen.Trunk.IO;
using Zen.Trunk.Storage.BufferFields;
using Zen.Trunk.Storage.Data.Index;

namespace Zen.Trunk.Storage.Data.Audio
{

    /// <summary>
    /// <c>AudioIndexInfo</c> defines an index entry for an audio sample.
    /// </summary>
    /// <remarks>
    /// Each entry consists of the ordinal index of the sample in the entire audio file.
    /// </remarks>
    public class AudioIndexInfo : IndexInfo
    {
        #region Private Fields
        private RootAudioIndexInfo _rootInfo;
        private readonly BufferFieldInt64 _sampleIndex;
        #endregion

        #region Public Constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="AudioIndexInfo"/> class.
        /// </summary>
        public AudioIndexInfo()
        {
            _sampleIndex = new BufferFieldInt64();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AudioIndexInfo" /> class.
        /// </summary>
        /// <param name="value">The value.</param>
        public AudioIndexInfo(long value)
        {
            _sampleIndex = new BufferFieldInt64(value);
        }
        #endregion

        #region Public Properties
        public long SampleIndex
        {
            get => _sampleIndex.Value;
            set => _sampleIndex.Value = value;
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Sets the context.
        /// </summary>
        /// <param name="def">The definition.</param>
        /// <param name="rootInfo">The root information.</param>
        /// <exception cref="ArgumentException">Root information of differing key length.</exception>
        /// <exception cref="InvalidOperationException">Column ID not found in index.</exception>
        public virtual void SetContext(DatabaseAudio def, RootAudioIndexInfo rootInfo)
        {
            _rootInfo = rootInfo;
        }

        /// <summary>
        /// Compares the current instance with another object of the same type
        /// and returns an integer that indicates whether the current instance
        /// precedes, follows, or occurs in the same position in the sort order
        /// as the other object.
        /// </summary>
        /// <param name="rhs">An object to compare with this instance.</param>
        /// <returns>
        /// A value that indicates the relative order of the objects being
        /// compared. The return value has these meanings:
        /// <list type="bulleted"><listheader>
        /// Value Meaning
        /// </listheader><item>
        /// Less than zero This instance is less than <paramref name="rhs" />.
        /// </item><item>
        /// Zero This instance is equal to <paramref name="rhs" />.
        /// </item><item>
        /// Greater than zero This instance is greater than <paramref name="rhs" />.
        /// </item></list>
        /// </returns>
        /// <exception cref="ArgumentException">Key length mismatch.</exception>
        public override int CompareTo(IndexInfo rhs)
        {
            var aiRhs = (AudioIndexInfo)rhs;
            return SampleIndex.CompareTo(aiRhs.SampleIndex);
        }
        #endregion

        #region Protected Methods
        /// <summary>
        /// Reads the field chain from the specified stream manager.
        /// </summary>
        /// <param name="reader">A <see cref="T:SwitchingBinaryReader" /> object.</param>
        protected override void OnRead(SwitchingBinaryReader reader)
        {
            // Wire up columns
            base.OnRead(reader);
            _sampleIndex.Read(reader);
        }

        /// <summary>
        /// Writes the field chain to the specified stream manager.
        /// </summary>
        /// <param name="writer">A <see cref="T:SwitchingBinaryWriter" /> object.</param>
        protected override void OnWrite(SwitchingBinaryWriter writer)
        {
            base.OnWrite(writer);
            _sampleIndex.Write(writer);
        }
        #endregion
    }
}

