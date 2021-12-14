using System;

namespace UnityEngine.Rendering.HighDefinition
{
    /// <summary>
    /// A volume component that holds settings for the water surface.
    /// </summary>
    [Serializable, VolumeComponentMenuForRenderPipeline("Lighting/WaterRendering", typeof(HDRenderPipeline))]
    [HDRPHelpURLAttribute("Override-Water-Rendering")]
    public sealed partial class WaterRendering : VolumeComponent
    {
        /// <summary>
        /// Defines the maximum resolution of the water surface grids.
        /// </summary>
        public enum WaterGridResolution
        {
            /// <summary>The water surface individual grids will be rendered at maximum resolution of 128x128 quads.</summary>
            VeryLow128 = 128,
            /// <summary>The water surface individual grids will be rendered at maximum resolution of 256x256 quads.</summary>
            Low256 = 256,
            /// <summary>The water surface individual grids will be rendered at maximum resolution of 512x512 quads.</summary>
            Medium512 = 512,
            /// <summary>The water surface individual grids will be rendered at maximum resolution of 1024x1024 quads.</summary>
            High1024 = 1024,
            /// <summary>The water surface individual grids will be rendered at maximum resolution of 2048x2048 quads.</summary>
            Ultra2048 = 2048,
        }

        /// <summary>
        /// A <see cref="VolumeParameter"/> that holds a <see cref="WaterGridResolution"/> value.
        /// </summary>
        [Serializable]
        public sealed class WaterGridResolutionParameter : VolumeParameter<WaterGridResolution>
        {
            /// <summary>
            /// Creates a new <see cref="WaterGridResolutionParameter"/> instance.
            /// </summary>
            /// <param name="value">The initial value to store in the parameter.</param>
            /// <param name="overrideState">The initial override state for the parameter.</param>
            public WaterGridResolutionParameter(WaterGridResolution value, bool overrideState = false) : base(value, overrideState) { }
        }

        /// <summary>
        /// When enabled, the water surfaces are rendered.
        /// </summary>
        [Tooltip("When enabled, the water surfaces are rendered.")]
        public BoolParameter enable = new BoolParameter(false);

        /// <summary>
        /// Specifies the maximum resolution at which the water surface patches are rendered.
        /// </summary>
        [Tooltip("Specifies the maximum resolution at which the water surface patches are rendered.")]
        public WaterGridResolutionParameter gridResolution = new WaterGridResolutionParameter(WaterGridResolution.Medium512);

        /// <summary>
        /// Sets the size of the water grids in meters.
        /// </summary>
        [Tooltip("Sets the size of the water grids in meters.")]
        public MinFloatParameter gridSize = new MinFloatParameter(1000.0f, 100.0f);

        /// <summary>
        /// Controls the number of LOD patches that are rendered.
        /// </summary>
        [Tooltip("Controls the number of LOD patches that are rendered.")]
        public ClampedIntParameter numLevelOfDetails = new ClampedIntParameter(2, 1, 4);

        /// <summary>
        /// Controls the influence of the ambient light probe on the water surfaces.
        /// </summary>
        [Tooltip("Controls the influence of the ambient light probe on the water surfaces.")]
        public ClampedFloatParameter ambientProbeDimmer = new ClampedFloatParameter(1.0f, 0.0f, 1.0f);

        WaterRendering()
        {
            displayName = "Water Rendering";
        }
    }
}
