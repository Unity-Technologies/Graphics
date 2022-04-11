using System;

namespace UnityEngine.Rendering.HighDefinition
{
    /// <summary>
    /// Decal debug settings.
    /// </summary>
    [Serializable]
    public class DecalsDebugSettings
    {
        /// <summary>Display the decal atlas.</summary>
        public bool displayAtlas = false;
        /// <summary>Displayed decal atlas mip level.</summary>
        public UInt32 mipLevel = 0;
    }
}
