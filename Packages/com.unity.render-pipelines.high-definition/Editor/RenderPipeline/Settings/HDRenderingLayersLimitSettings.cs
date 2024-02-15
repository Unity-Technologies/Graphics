using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace UnityEditor.Rendering.HighDefinition
{
    /// <summary>
    /// Rendering Layers settings class.
    /// </summary>
    [Serializable]
    [SupportedOnRenderPipeline(typeof(HDRenderPipelineAsset))]
    [UnityEngine.Categorization.CategoryInfo(Name = "H: Rendering Layer Settings", Order = 980), HideInInspector]
    public class HDRenderingLayersLimitSettings : RenderingLayersLimitSettings
    {
        /// <summary>
        /// Maximum number of the supported Rendering Layers on HDRP.
        /// </summary>
        protected override int maxRenderingLayersForPipeline => 16;
    }
}
