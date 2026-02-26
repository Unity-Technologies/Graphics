using Unity.GraphCommon.LowLevel.Editor;
using UnityEngine;
using UnityEngine.VFX;

namespace UnityEditor.VFX
{
    /// <summary>
    /// A data type representing a set of particle tied to a simulation space and bounds.
    /// </summary>
    /*public*/ class ParticleData : IDataDescription
    {
        /// <summary>
        /// The key used to identify the particle attribute data.
        /// </summary>
        public static readonly UniqueDataKey AttributeDataKey = new UniqueDataKey("AttributeData");

        /// <summary>
        /// The key used to identify the dead list.
        /// </summary>
        public static readonly UniqueDataKey DeadlistKey = new UniqueDataKey("Deadlist");

        /// <inheritdoc cref="IDataModel"/>
        public string Name { get; }

        /// <summary>
        /// The maximum number of particles held by this ParticleData.
        /// </summary>
        ///
        public uint Capacity { get; }
        /// <summary>
        /// The simulation space of the particles.
        /// </summary>
        public VFXSpace Space { get; }
        /// <summary>
        /// The initial bounds of the particles.
        /// </summary>
        public Bounds Bounds { get; }


        /// <summary>
        /// Constructs a named ParticleData with initial bounds, a space and a capacity.
        /// </summary>
        /// <param name="name">The name of the ParticleData</param>
        /// <param name="bounds">The initial bounds of the particles.</param>
        /// <param name="space">The simulation space of the particles.</param>
        /// <param name="capacity">The maximum number of particles handled by the ParticleData.</param>
        public ParticleData(string name, Bounds bounds, VFXSpace space, uint capacity)
        {
            Name = name;
            Bounds = bounds;
            Space = space;
            Capacity = capacity;
        }

        /// <inheritdoc />
        public IDataDescription GetSubdata(IDataKey dataKey)
        {
            if (dataKey.Equals(AttributeDataKey))
                return new AttributeData(Capacity);

            if (dataKey.Equals(DeadlistKey))
                return new IdGeneratorData();

            return null;
        }
    }
}
