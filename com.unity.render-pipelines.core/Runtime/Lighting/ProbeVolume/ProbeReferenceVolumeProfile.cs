using System.Collections.Generic;
using UnityEditor;

namespace UnityEngine.Rendering
{
    /// <summary>
    /// An Asset which holds a set of settings to use with a <see cref="Probe Reference Volume"/>.
    /// </summary>
    public sealed class ProbeReferenceVolumeProfile : ScriptableObject
    {
        /// <summary>
        /// The default dimensions for APV's index data structure.
        /// </summary>
        public static Vector3Int s_DefaultIndexDimensions = new Vector3Int(1024, 64, 1024);

        /// <summary>
        /// The size of a Cell.
        /// </summary>
        public int cellSize = 64;
        /// <summary>
        /// The size of a Brick.
        /// </summary>
        public int brickSize = 4;
        /// <summary>
        /// Max subdivision.
        /// </summary>
        public int maxSubdivision = 2;
        /// <summary>
        /// The normal bias to apply during shading.
        /// </summary>
        public float normalBias = 0.2f;
        /// <summary>
        /// Index field dimensions.
        /// </summary>
        public Vector3Int indexDimensions = s_DefaultIndexDimensions;

        /// <summary>
        /// Determines if the Probe Reference Volume Profile is equivalent to another one.
        /// </summary>
        /// <param name ="otherProfile">The profile to compare with.</param>
        /// <returns>Whether the Probe Reference Volume Profile is equivalent to another one.</returns>
        public bool IsEquivalent(ProbeReferenceVolumeProfile otherProfile)
        {
            return brickSize == otherProfile.brickSize &&
                cellSize == otherProfile.cellSize &&
                maxSubdivision == otherProfile.maxSubdivision &&
                normalBias == otherProfile.normalBias;
        }
    }
}
