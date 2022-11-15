using System;

namespace UnityEngine.Rendering.HighDefinition
{
    /// <summary>
    /// Recursive Rendering Volume Component.
    /// This component setups recursive rendering.
    /// </summary>
    [Serializable, VolumeComponentMenuForRenderPipeline("Ray Tracing/Recursive Rendering (Preview)", typeof(HDRenderPipeline))]
    [HDRPHelpURLAttribute("Ray-Tracing-Recursive-Rendering")]
    public sealed class RecursiveRendering : VolumeComponent
    {
        /// <summary>
        /// Enables recursive rendering.
        /// </summary>
        [Tooltip("Enable. Enables recursive rendering.")]
        public BoolParameter enable = new BoolParameter(false, BoolParameter.DisplayType.EnumPopup);

        /// <summary>
        /// Layer mask used to include the objects for recursive rendering.
        /// </summary>
        [Tooltip("Layer Mask. Layer mask used to include the objects for recursive rendering.")]
        public LayerMaskParameter layerMask = new LayerMaskParameter(-1);

        /// <summary>
        /// Defines the maximal recursion for rays.
        /// </summary>
        [Tooltip("Max Depth. Defines the maximal recursion for rays.")]
        public ClampedIntParameter maxDepth = new ClampedIntParameter(4, 1, 10);

        /// <summary>
        /// This defines the maximal travel distance of rays in meters.
        /// </summary>
        public MinFloatParameter rayLength = new MinFloatParameter(10.0f, 0.0f);

        /// <summary>
        /// Minmal smoothness for reflection rays. If the surface has a smoothness value below this threshold, a reflection ray will not be case and it will fallback on other techniques.
        /// </summary>
        [Tooltip("Minmal Smoothness for Reflection. If the surface has a smoothness value below this threshold, a reflection ray will not be case and it will fallback on other techniques.")]
        public ClampedFloatParameter minSmoothness = new ClampedFloatParameter(0.5f, 0.0f, 1.0f);

        /// <summary>
        /// Controls which sources are used to fallback on when the traced ray misses.
        /// </summary>
        [AdditionalProperty]
        [Tooltip("Controls which sources are used to fallback on when the traced ray misses.")]
        public RayTracingFallbackHierachyParameter rayMiss = new RayTracingFallbackHierachyParameter(RayTracingFallbackHierachy.ReflectionProbesAndSky);

        /// <summary>
        /// Controls the fallback hierarchy for lighting the last bounce.
        /// </summary>
        [AdditionalProperty]
        [Tooltip("Controls the fallback hierarchy for lighting the last bounce.")]
        public RayTracingFallbackHierachyParameter lastBounce = new RayTracingFallbackHierachyParameter(RayTracingFallbackHierachy.ReflectionProbesAndSky);

        /// <summary>
        /// Controls the dimmer applied to the ambient and legacy light probes.
        /// </summary>
        [Tooltip("Controls the dimmer applied to the ambient and legacy light probes.")]
        [AdditionalProperty]
        public ClampedFloatParameter ambientProbeDimmer = new ClampedFloatParameter(1.0f, 0.0f, 1.0f);

        /// <summary>
        /// Default constructor for the recursive rendering volume component.
        /// </summary>
        public RecursiveRendering()
        {
            displayName = "Recursive Rendering (Preview)";
        }
    }
}
