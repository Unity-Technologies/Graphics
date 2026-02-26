
namespace Unity.GraphCommon.LowLevel.Editor
{
    /// <summary>
    /// Represents an attribute data container with a specified capacity.
    /// </summary>
    /*public*/ class AttributeData : IDataDescription
    {
        /// <summary>
        /// The default key for attribute data.
        /// </summary>
        public static readonly UniqueDataKey DefaultKey = new UniqueDataKey("Attributes");

        /// <summary>
        /// Initializes a new instance of the <see cref="AttributeData"/> class with the given capacity.
        /// </summary>
        /// <param name="capacity">The capacity of the attribute data.</param>
        public AttributeData(uint capacity)
        {
            Capacity = capacity;
        }

        /// <summary>
        /// Gets the capacity of the attribute data.
        /// </summary>
        public uint Capacity { get; }

        /// <inheritdoc />
        public IDataDescription GetSubdata(IDataKey dataKey)
        {
            if (dataKey is AttributeKey attributeKey)
            {
                return ValueData.Create(attributeKey.Attribute.DefaultValue.GetType());
            }
            return null;
        }
    }
}
