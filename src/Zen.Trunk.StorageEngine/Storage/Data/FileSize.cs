using System;

namespace Zen.Trunk.Storage.Data
{
    /// <summary>
    /// <c>FileSize</c> is a value-type that represents the size of a file or
    /// the dynamics on how a file can be expanded.
    /// </summary>
    public struct FileSize
    {
        /// <summary>
        /// <c>FileSizeUnit</c> defines the size units in the value exposed by
        /// <see cref="FileSize.Value" /> property.
        /// </summary>
        public enum FileSizeUnit
        {
            /// <summary>
            /// The value is in units of kilobytes (Kb)
            /// </summary>
            KiloBytes,

            /// <summary>
            /// The value is in units of megabytes (Mb)
            /// </summary>
            MegaBytes,

            /// <summary>
            /// The value is in units of gigabytes (Gb)
            /// </summary>
            GigaBytes,

            /// <summary>
            /// The value is in units of terabytes (Tb)
            /// </summary>
            TeraBytes,

            /// <summary>
            /// The value is a percentage (only applicable to usage as growth value)
            /// </summary>
            Percentage,

            /// <summary>
            /// The value is unlimited (only applicable to usage as a maximum size value)
            /// </summary>
            Unlimited
        }

        /// <summary>
        /// A static value representing an unlimited filesize.
        /// </summary>
        public static readonly FileSize Unlimited = new FileSize(0, FileSizeUnit.Unlimited);

        /// <summary>
        /// Initializes a new instance of the <see cref="FileSize"/> struct.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="unit">The unit.</param>
        /// <exception cref="ArgumentException">
        /// Thrown if the unit is set to percentage and the value is outside the allowed range.
        /// </exception>
        public FileSize(double value, FileSizeUnit unit)
        {
            if (unit == FileSizeUnit.Percentage &&
                (value < 0.0 || value > 100.0))
            {
                throw new ArgumentException(nameof(value));
            }

            Value = value;
            Unit = unit;
        }

        /// <summary>
        /// Gets the value.
        /// </summary>
        /// <value>
        /// The value.
        /// </value>
        public double Value { get; }

        /// <summary>
        /// Gets the value units.
        /// </summary>
        /// <value>
        /// The unit.
        /// </value>
        public FileSizeUnit Unit { get; }

        /// <summary>
        /// Gets a value indicating whether this instance represents an unlimited size.
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance represents an unlimited size; otherwise, <c>false</c>.
        /// </value>
        public bool IsUnlimited => Unit == FileSizeUnit.Unlimited;

        /// <summary>
        /// Gets a value indicating whether this instance represents a percentage value.
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance represents a percentage value; otherwise, <c>false</c>.
        /// </value>
        public bool IsPercentage => Unit == FileSizeUnit.Percentage;

        /// <summary>
        /// Gets the file size expressed as a quantity pages based on the
        /// specified page size.
        /// </summary>
        /// <param name="pageSize">Size of the page.</param>
        /// <returns></returns>
        public uint GetSizeAsPages(uint pageSize)
        {
            ulong actualSize;
            switch (Unit)
            {
                case FileSizeUnit.KiloBytes:
                    actualSize = (ulong)(Value * 1024);
                    break;
                case FileSizeUnit.MegaBytes:
                    actualSize = (ulong)(Value * 1024 * 1024);
                    break;
                case FileSizeUnit.GigaBytes:
                    actualSize = (ulong)(Value * 1024 * 1024 * 1024);
                    break;
                case FileSizeUnit.TeraBytes:
                    actualSize = (ulong)(Value * 1024 * 1024 * 1024 * 1024);
                    break;
                default:
                    return 0;
            }

            // 1MB = 128 pages @ 8192 bytes per page
            // 1GB = 131,072 pages @ 8192 bytes per page
            // 1TB = 134,217,728 pages @ 8192 bytes per page
            uint pages = (uint)(actualSize / pageSize);
            if ((actualSize % pageSize) != 0)
            {
                ++pages;
            }
            return pages;
        }
    }
}