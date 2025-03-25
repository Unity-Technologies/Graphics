using System;

namespace UnityEngine.Rendering.Universal
{
    /// <summary>
    /// Options to control the renderer override.
    /// This enum is no longer in use.
    /// </summary>
    [Obsolete("Renderer override is no longer used, renderers are referenced by index on the pipeline asset. #from(2023.1)")]
    public enum RendererOverrideOption
    {
        /// <summary>
        /// Use this to choose a custom override.
        /// </summary>
        Custom,

        /// <summary>
        /// Use this to choose the setting set on the pipeline asset.
        /// </summary>
        UsePipelineSettings,
    }
    
    public partial class UniversalAdditionalCameraData
    {
        /// <summary>
        /// The serialized version of the class. Used for upgrading.
        /// </summary>
        [Obsolete("This field has been deprecated. #from(6000.2)", false)]
        public float version => (int)m_Version;
    }
}