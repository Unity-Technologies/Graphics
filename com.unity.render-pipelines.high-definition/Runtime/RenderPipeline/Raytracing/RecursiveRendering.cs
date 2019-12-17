using System;
using UnityEngine.Experimental.Rendering;

namespace UnityEngine.Rendering.HighDefinition
{
    [Serializable, VolumeComponentMenu("Ray Tracing/Recursive Rendering (Preview)")]
    public sealed class RecursiveRendering : VolumeComponent
    {
        [Tooltip("Enable. Enables recursive rendering.")]
        public BoolParameter enable = new BoolParameter(false);

        [Tooltip("Layer Mask. Layer mask used to include the objects for recursive rendering.")]
        public LayerMaskParameter layerMask = new LayerMaskParameter(-1);

        [Tooltip("Max Depth. Defines the maximal recursion for rays.")]
        public ClampedIntParameter maxDepth = new ClampedIntParameter(4, 1, 10);

        [Tooltip("Ray Length. This defines the maximal travel distance of rays.")]
        public ClampedFloatParameter rayLength = new ClampedFloatParameter(10f, 0f, 50f);
    }
}
