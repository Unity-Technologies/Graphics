using UnityEngine.Rendering;

namespace UnityEditor.Rendering.HighDefinition
{
    [GenerateHLSL]
    /// <summary>
    /// The type of bias to be applied to Decal Meshes.
    /// </summary>
    public enum DecalMeshDepthBiasType
    {
        /// <summary>
        /// A depth bias to stop the decal's Mesh from overlapping with other Meshes.
        /// </summary>
        DepthBias = 0,
        /// <summary>
        /// A world-space bias alongside the view vector to stop the decal's Mesh from overlapping with other Meshes.
        /// </summary>
        ViewBias = 1
    }
}
