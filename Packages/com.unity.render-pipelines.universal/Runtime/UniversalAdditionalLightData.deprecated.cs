using System;

namespace UnityEngine.Rendering.Universal
{
    /// <summary>Light Layers.</summary>
    [Flags]
    [Obsolete("Use RenderingLayerMask instead. #from(6000.2)")]
    public enum LightLayerEnum
    {
        /// <summary>The light will no affect any object.</summary>
        Nothing = 0, // Custom name for "Nothing" option

        /// <summary>Light Layer 0.</summary>
        LightLayerDefault = 1 << 0,

        /// <summary>Light Layer 1.</summary>
        LightLayer1 = 1 << 1,

        /// <summary>Light Layer 2.</summary>
        LightLayer2 = 1 << 2,

        /// <summary>Light Layer 3.</summary>
        LightLayer3 = 1 << 3,

        /// <summary>Light Layer 4.</summary>
        LightLayer4 = 1 << 4,

        /// <summary>Light Layer 5.</summary>
        LightLayer5 = 1 << 5,

        /// <summary>Light Layer 6.</summary>
        LightLayer6 = 1 << 6,

        /// <summary>Light Layer 7.</summary>
        LightLayer7 = 1 << 7,

        /// <summary>Everything.</summary>
        Everything = 0xFF, // Custom name for "Everything" option
    }

    public partial class UniversalAdditionalLightData
    {
        // The layer(s) this light belongs too.
        [Obsolete("This is obsolete, please use m_RenderingLayerMask instead. #from(2023.1)", false)] [SerializeField]
        LightLayerEnum m_LightLayerMask = LightLayerEnum.LightLayerDefault;


        /// <summary>
        /// The layer(s) this light belongs to.
        /// </summary>
        [Obsolete("This is obsolete, please use renderingLayerMask instead. #from(2023.1)", true)]
        public LightLayerEnum lightLayerMask
        {
            get { return m_LightLayerMask; }
            set { m_LightLayerMask = value; }
        }

        // The layer(s) used for shadow casting.
        [Obsolete("This is obsolete, please use m_RenderingLayerMask instead. #from(2023.1)", false)] [SerializeField]
        LightLayerEnum m_ShadowLayerMask = LightLayerEnum.LightLayerDefault;

        /// <summary>
        /// The layer(s) for shadow.
        /// </summary>
        [Obsolete("This is obsolete, please use shadowRenderingLayerMask instead. #from(2023.1)", true)]
        public LightLayerEnum shadowLayerMask
        {
            get { return m_ShadowLayerMask; }
            set { m_ShadowLayerMask = value; }
        }
        
        [SerializeField]
        [Obsolete("This is obsolete, please use m_RenderingLayersMask instead. #from(6000.2)", false)]
        uint m_RenderingLayers = 1;
        
        [SerializeField] 
        [Obsolete("This is obsolete, please use renderingLayersMask instead. #from(6000.2)", false)]
        uint m_ShadowRenderingLayers = 1;
    }
}