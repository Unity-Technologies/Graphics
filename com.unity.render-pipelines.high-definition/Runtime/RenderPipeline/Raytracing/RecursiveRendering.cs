using System;

namespace UnityEngine.Rendering.HighDefinition
{
    /// <summary>
    /// Recursive Rendering Volume Component.
    /// This component setups recursive rendering.
    /// </summary>
    [Serializable, VolumeComponentMenu("Ray Tracing/Recursive Rendering (Preview)")]
    [HelpURL(Documentation.baseURL + Documentation.version + Documentation.subURL + "Ray-Tracing-Recursive-Rendering" + Documentation.endURL)]
    public sealed class RecursiveRendering : VolumeComponent
    {
        /// <summary>
        /// Enables recursive rendering.
        /// </summary>
        [Tooltip("Enable. Enables recursive rendering.")]
        public BoolParameter enable = new BoolParameter(false);

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
        /// This defines the maximal travel distance of rays.
        /// </summary>
        [Tooltip("Ray Length. This defines the maximal travel distance of rays. High value have performance impact.")]
        public ClampedFloatParameter rayLength = new ClampedFloatParameter(10f, 0f, 500f);

        /// <summary>
        /// Minmal smoothness for reflection rays. If the surface has a smoothness value below this threshold, a reflection ray will not be case and it will fallback on other techniques.
        /// </summary>
        [Tooltip("Minmal Smoothness for Reflection. If the surface has a smoothness value below this threshold, a reflection ray will not be case and it will fallback on other techniques.")]
        public ClampedFloatParameter minSmoothness = new ClampedFloatParameter(0.5f, 0.0f, 1.0f);
        /// <summary>
        /// Default constructor for the recursive rendering volume component.
        /// </summary>
        public RecursiveRendering()
        {
            displayName = "Recursive Rendering (Preview)";
        }
    }
}
