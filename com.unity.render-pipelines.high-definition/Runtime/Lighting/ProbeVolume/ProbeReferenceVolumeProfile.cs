using System.Collections.Generic;
using UnityEditor;

namespace UnityEngine.Rendering.HighDefinition
{
    /// <summary>
    /// An Asset which holds a set of settings to use with a <see cref="Probe Reference Volume"/>.
    /// </summary>
    public sealed class ProbeReferenceVolumeProfile : ScriptableObject
    {
        // TODO: Better documentation, the one here is not really doc!

            /// <summary>
        /// The size of a Cell. 
        /// </summary>
        public int CellSize = 64;
        /// <summary>
        /// The size of a Brick. 
        /// </summary>
        public int BrickSize = 4;
        /// <summary>
        /// Max subdivision. 
        /// </summary>
        public int MaxSubdivision = 2;
        /// <summary>
        /// The normal bias to apply during shading. 
        /// </summary>
        public float NormalBias = 0.2f;
        /// <summary>
        /// Index field dimensions. 
        /// </summary>
        public Vector3Int IndexDimensions = new Vector3Int(1024, 64, 1024);

        /// <summary>
        /// A custom hashing function that Unity uses to compare the state of parameters.
        /// </summary>
        /// <returns>A computed hash code for the current instance.</returns>
        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;

                hash = hash * 23 + CellSize.GetHashCode();
                hash = hash * 23 + BrickSize.GetHashCode();
                hash = hash * 23 + MaxSubdivision.GetHashCode();
                hash = hash * 23 + NormalBias.GetHashCode();
                hash = hash * 23 + IndexDimensions.GetHashCode();

                return hash;
            }
        }
    }
}
