using System;

namespace UnityEngine.Rendering.HighDefinition
{
    /// <summary>
    /// Mip Map Debug Mode.
    /// </summary>
    [GenerateHLSL]
    public enum DebugMipMapMode
    {
        /// <summary>No Mip Map debug mode.</summary>
        None,
        /// <summary>Mip ratio debug mode.</summary>
        MipRatio,
        /// <summary>Mip count debug mode.</summary>
        MipCount,
        /// <summary>Mip count reduction debug mode.</summary>
        MipCountReduction,
        /// <summary>Streaming budget debug mode.</summary>
        StreamingMipBudget,
        /// <summary>Streaming mip debug mode.</summary>
        StreamingMip
    }

    /// <summary>
    /// Terrain mip map debug mode.
    /// </summary>
    [GenerateHLSL]
    public enum DebugMipMapModeTerrainTexture
    {
        /// <summary>Control debug.</summary>
        Control,
        /// <summary>Layer 0 debug.</summary>
        Layer0,
        /// <summary>Layer 1 debug.</summary>
        Layer1,
        /// <summary>Layer 2 debug.</summary>
        Layer2,
        /// <summary>Layer 3 debug.</summary>
        Layer3,
        /// <summary>Layer 4 debug.</summary>
        Layer4,
        /// <summary>Layer 5 debug.</summary>
        Layer5,
        /// <summary>Layer 6 debug.</summary>
        Layer6,
        /// <summary>Layer 7 debug.</summary>
        Layer7
    }

    /// <summary>
    /// Mîp map debug Settings.
    /// </summary>
    [Serializable]
    public class MipMapDebugSettings
    {
        /// <summary>Mip maps debug mode.</summary>
        public DebugMipMapMode debugMipMapMode = DebugMipMapMode.None;
        /// <summary>Terrain texture mip map debug mode.</summary>
        public DebugMipMapModeTerrainTexture terrainTexture = DebugMipMapModeTerrainTexture.Control;

        /// <summary>
        /// Returns true if any mip map debug is enabled.
        /// </summary>
        /// <returns>True if any mip map debug is enabled.</returns>
        public bool IsDebugDisplayEnabled()
        {
            return debugMipMapMode != DebugMipMapMode.None;
        }
    }
}
