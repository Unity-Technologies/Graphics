using Unity.GraphCommon.LowLevel.Editor;
using UnityEngine.VFX;
using UnityEngine;

namespace UnityEditor.VFX
{
    /// <summary>
    /// A data type representing a set of particle strips tied to a simulation space and bounds.
    /// </summary>
    /*public*/ class ParticleStripData : IDataDescription
    {
        /// <inheritdoc cref="IDataModel"/>
        public string Name { get; }
        /// <summary>
        /// The maximum number of strips held by this ParticleStripData.
        /// </summary>
        public uint StripCapacity { get; }
        /// <summary>
        /// The maximum number of particle composing each strip.
        /// </summary>
        public uint PerStripCapacity { get; }

        /// <summary>
        /// The initial bounds of the particles.
        /// </summary>
        public Bounds Bounds { get; }

        /// <summary>
        /// The simulation space of the particles.
        /// </summary>
        public VFXSpace Space { get; }

        /// <summary>
        /// Constructs a named ParticleData with initial bounds, a space and a capacity.
        /// </summary>
        /// <param name="name">The name of the ParticleStripData</param>
        /// <param name="bounds">The initial bounds of the particles.</param>
        /// <param name="space">The simulation space of the particles.</param>
        /// <param name="stripCapacity">The maximum number of strips held by this ParticleStripData.</param>
        /// <param name="perStripCapacity">The maximum number of particle composing each strip.</param>
        public ParticleStripData(string name, Bounds bounds, VFXSpace space, uint stripCapacity, uint perStripCapacity)
        {
            Name = name;
            Bounds = bounds;
            Space = space;
            StripCapacity = stripCapacity;
            PerStripCapacity = perStripCapacity;
        }

    }
}
