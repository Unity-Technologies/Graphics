using System;
using UnityEngine.Experimental.Rendering;

namespace UnityEngine.Rendering.HighDefinition
{
    /// <summary>
    /// Recursive Rendering Volume Component.
    /// This component setups recursive rendering.
    /// </summary>
    [Serializable, VolumeComponentMenu("Ray Tracing/Recursive Rendering (Preview)")]
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
        [Tooltip("Ray Length. This defines the maximal travel distance of rays.")]
        public ClampedFloatParameter rayLength = new ClampedFloatParameter(10f, 0f, 50f);

        public RecursiveRendering()
        {
            displayName = "Recursive Rendering (Preview)";
        }
    }
}
