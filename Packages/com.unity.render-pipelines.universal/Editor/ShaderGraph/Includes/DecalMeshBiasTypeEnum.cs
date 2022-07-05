using UnityEngine.Rendering;

namespace UnityEditor.Rendering.Universal
{
    /// <summary>
    /// Options for mesh bias type.
    /// The Mesh bias lets you prevent z-fighting between the Decal GameObject and the GameObject it overlaps.
    /// This property is only applicable for GameObjects with a Decal Material type assigned directly.
    /// </summary>
    [GenerateHLSL]
    public enum DecalMeshDepthBiasType
    {
        /// <summary>
        /// When drawing the decal gameObject, Unity changes the depth value of each pixel of the GameObject by this value.
        /// A negative value shifts pixels closer to the Camera, so that Unity draws the Decal GameObject on top of the overlapping Mesh, which prevents z-fighting.
        /// Decal projectors ignore this property.
        /// </summary>
        DepthBias = 0,

        /// <summary>
        /// A world-space bias (in meters).
        /// When drawing the Decal GameObject, Unity shifts each pixel of the GameObject by this value along the view vector.
        /// A positive value shifts pixels closer to the Camera, so that Unity draws the decal GameObject on top of the overlapping mesh, which prevents z-fighting.
        /// Decal projectors ignore this property.
        /// </summary>
        ViewBias = 1
    }
}
