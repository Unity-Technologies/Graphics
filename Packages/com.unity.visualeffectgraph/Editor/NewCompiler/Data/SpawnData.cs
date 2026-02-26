using Unity.GraphCommon.LowLevel.Editor;

namespace UnityEditor.VFX
{
    /// <summary>
    /// A data type representing data driving the spawning in other data types.
    /// </summary>
    /*public*/ class SpawnData : IDataDescription
    {

        /// <summary>
        /// The key used to identify the source attribute data.
        /// </summary>
        public static readonly UniqueDataKey SourceAttributeDataKey = new UniqueDataKey("SourceAttributeData");

        /// <inheritdoc cref="IDataDescription"/>
        public string Name { get; }

        /// <summary>
        /// Constructs a named SpawnData.
        /// </summary>
        /// <param name="name">The name of the SpawnData.</param>
        public SpawnData(string name)
        {
            Name = name;
        }

        /// <inheritdoc />
        public IDataDescription GetSubdata(IDataKey dataKey)
        {
            if (dataKey.Equals(SourceAttributeDataKey))
                return new AttributeData(1);

            return null;
        }
    }
}
