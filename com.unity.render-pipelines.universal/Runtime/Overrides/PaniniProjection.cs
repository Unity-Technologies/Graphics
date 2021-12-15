using System;

namespace UnityEngine.Rendering.Universal
{
    [Serializable, VolumeComponentMenuForRenderPipeline("Post-processing/Panini Projection", typeof(UniversalRenderPipeline))]
    public sealed class PaniniProjection : VolumeComponent, IPostProcessComponent
    {
        [Tooltip("Panini projection distance.")]
        public ClampedFloatParameter distance = new ClampedFloatParameter(0f, 0f, 1f);

        [Tooltip("Panini projection crop to fit.")]
        public ClampedFloatParameter cropToFit = new ClampedFloatParameter(1f, 0f, 1f);

        /// <inheritdoc/>
        public bool IsActive() => distance.value > 0f;

        /// <inheritdoc/>
        public bool IsTileCompatible() => false;
    }
}
