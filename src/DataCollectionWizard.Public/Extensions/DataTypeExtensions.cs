using ViciOne.DeviceTree.Contracts;

namespace DataCollectionWizard.Public.Extensions;

public static class DataTypeExtensions
{
    extension(DataType dataType)
    {
        /// <summary>
        /// Whether this data type's values are suitable for compression (see <see cref="IDeviceTreeCompressableDataNode"/>).
        /// </summary>
        public bool SupportsCompression => dataType switch
        {
            DataType.Flag => true,
            DataType.Whole => true,
            DataType.UnsignedWhole => true,
            DataType.Real => true,
            _ => false,
        };

        /// <summary>
        /// Whether this data type's values are suitable for the live view (see <see cref="IDeviceTreeLiveDataNode"/>).
        /// </summary>
        public bool SupportsLiveView => dataType switch
        {
            DataType.Flag => true,
            DataType.Whole => true,
            DataType.UnsignedWhole => true,
            DataType.Real => true,
            DataType.Text => true,
            _ => false,
        };

        /// <summary>
        /// Whether this data type's values are suitable for logging.
        /// </summary>
        public bool SupportsLogging => dataType switch
        {
            DataType.Flag => true,
            DataType.Whole => true,
            DataType.UnsignedWhole => true,
            DataType.Real => true,
            DataType.Blob => true,
            _ => false,
        };
    }
}
