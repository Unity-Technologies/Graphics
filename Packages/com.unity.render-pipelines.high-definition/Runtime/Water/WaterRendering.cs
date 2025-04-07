using System;

namespace UnityEngine.Rendering.HighDefinition
{
    /// <summary>
    /// A volume component that holds settings for the water surface.
    /// </summary>
    [Serializable, VolumeComponentMenu("Lighting/Water Rendering")]
    [SupportedOnRenderPipeline(typeof(HDRenderPipelineAsset))]
    [HDRPHelpURL("water-use-the-water-system-in-your-project")]
    public sealed partial class WaterRendering : VolumeComponent
    {
        /// <summary>
        /// When enabled, the water surfaces are rendered.
        /// </summary>
        [Tooltip("When enabled, the water surfaces are rendered.")]
        public BoolParameter enable = new BoolParameter(false, BoolParameter.DisplayType.EnumPopup);

        /// <summary>
        /// Sets the elevation at which the max grid size is reached.
        /// </summary>
        [Tooltip("Sets the size of a triangle edge in screen space. Smaller values result in smaller triangles.")]
        public ClampedFloatParameter triangleSize = new ClampedFloatParameter(30.0f, 15.0f, 100.0f);

        /// <summary>
        /// Controls the influence of the ambient light probe on the water surfaces.
        /// </summary>
        [Tooltip("Controls the influence of the ambient light probe on the water surfaces.")]
        public ClampedFloatParameter ambientProbeDimmer = new ClampedFloatParameter(0.5f, 0.0f, 1.0f);

        WaterRendering()
        {
            displayName = "Water Rendering";
        }
    }
}
