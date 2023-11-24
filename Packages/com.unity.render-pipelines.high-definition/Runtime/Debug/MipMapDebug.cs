using System;

namespace UnityEngine.Rendering.HighDefinition
{
    /// <summary>
    /// Debug mode for texture mipmap streaming.
    /// </summary>
    // Keep in sync with DebugViewEnums.cs in URP's ShaderLibrary/Debug/DebugViewEnums.cs
    [GenerateHLSL]
    public enum DebugMipMapMode
    {
        /// <summary>No mipmap debug.</summary>
        None,
        /// <summary>Display savings and shortage due to streaming.</summary>
        MipStreamingPerformance,
        /// <summary>Display the streaming status of materials and textures.</summary>
        MipStreamingStatus,
        /// <summary>Highlight recently streamed data.</summary>
        MipStreamingActivity,
        /// <summary>Display streaming priorities as set up when importing.</summary>
        MipStreamingPriority,
        /// <summary>Display the amount of uploaded mip levels.</summary>
        MipCount,
        /// <summary>Visualize the pixel density for the highest-resolution uploaded mip level from the camera's point-of-view.</summary>
        MipRatio,
    }

    /// <summary>
    /// Aggregation mode for texture mipmap streaming debugging information.
    /// </summary>
    // Keep in sync with DebugMipMapStatusMode in URP's ShaderLibrary/Debug/DebugViewEnums.cs
    [GenerateHLSL]
    public enum DebugMipMapStatusMode
    {
        /// <summary>Show debug information aggregated per material.</summary>
        Material,
        /// <summary>Show debug information for the selected texture slot.</summary>
        Texture,
    }

    /// <summary>
    /// Terrain layer for texture mipmap streaming debugging.
    /// </summary>
    [GenerateHLSL]
    public enum DebugMipMapModeTerrainTexture
    {
        /// <summary>Control texture debug.</summary>
        Control,
        /// <summary>Layer 0 diffuse texture debug.</summary>
        [InspectorName("Layer 0 - Diffuse")] Layer0,
        /// <summary>Layer 1 diffuse texture debug.</summary>
        [InspectorName("Layer 1 - Diffuse")] Layer1,
        /// <summary>Layer 2 diffuse texture debug.</summary>
        [InspectorName("Layer 2 - Diffuse")] Layer2,
        /// <summary>Layer 3 diffuse texture debug.</summary>
        [InspectorName("Layer 3 - Diffuse")] Layer3,
        /// <summary>Layer 4 diffuse texture debug.</summary>
        [InspectorName("Layer 4 - Diffuse")] Layer4,
        /// <summary>Layer 5 diffuse texture debug.</summary>
        [InspectorName("Layer 5 - Diffuse")] Layer5,
        /// <summary>Layer 6 diffuse texture debug.</summary>
        [InspectorName("Layer 6 - Diffuse")] Layer6,
        /// <summary>Layer 7 diffuse texture debug.</summary>
        [InspectorName("Layer 7 - Diffuse")] Layer7
    }

    /// <summary>
    /// Texture mipmap streaming debug settings.
    /// </summary>
    [Serializable]
    public class MipMapDebugSettings
    {
        /// <summary>Debug mode to visualize.</summary>
        public DebugMipMapMode debugMipMapMode = DebugMipMapMode.None;

        /// <summary>The material texture slot for which debug information is shown.</summary>
        public int materialTextureSlot = 0;
        /// <summary>The terrain layer for which debug information is shown on terrains.</summary>
        public DebugMipMapModeTerrainTexture terrainTexture = DebugMipMapModeTerrainTexture.Control;

        /// <summary>Combine the information over all slots per material.</summary>
        public bool showInfoForAllSlots = true;

        /// <summary>
        /// Whether combining information over texture slots is possible for the current debug mode.
        /// </summary>
        /// <returns>True if combining information over texture slots is possible for the current debug mode.</returns>
        public bool CanAggregateData() { return debugMipMapMode == DebugMipMapMode.MipStreamingStatus || debugMipMapMode == DebugMipMapMode.MipStreamingActivity;  }

        /// <summary>Opacity of texture mipmap streaming debug colors.</summary>
        public float mipmapOpacity = 1.0f;

        /// <summary>How long a texture should be shown as "recently updated".</summary>
        public float recentlyUpdatedCooldown = 3.0f;

        /// <summary>Show detailed status codes for the Mipmap Streaming Status debug view.</summary>
        public bool showStatusCode = false;

        /// <summary>Aggregation mode for showing debug information per texture or aggregated for each material.</summary>
        public DebugMipMapStatusMode statusMode = DebugMipMapStatusMode.Material;

        /// <summary>
        /// Returns true if any texture mipmap streaming debug view is enabled.
        /// </summary>
        /// <returns>True if any texture mipmap streaming debug view is enabled.</returns>
        public bool IsDebugDisplayEnabled()
        {
            return debugMipMapMode != DebugMipMapMode.None;
        }
    }
}
