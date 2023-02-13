using System;

namespace UnityEngine.Rendering.HighDefinition
{
    // Deprecated, kept for migration
    [Obsolete()]
    [SupportedOnRenderPipeline(typeof(HDRenderPipelineAsset))]
    class ExponentialFog : AtmosphericScattering
    {
        private readonly static int m_ExpFogParam = Shader.PropertyToID("_ExpFogParameters");

        [Tooltip("Sets the distance from the Camera at which the fog reaches its maximum thickness.")]
        public MinFloatParameter fogDistance = new MinFloatParameter(200.0f, 0.0f);
        [Tooltip("Sets the height, in world space, at which HDRP begins to decrease the fog density from 1.0.")]
        public FloatParameter fogBaseHeight = new FloatParameter(0.0f);
        [Tooltip("Controls the falloff of height fog attenuation, larger values result in sharper attenuation.")]
        public ClampedFloatParameter fogHeightAttenuation = new ClampedFloatParameter(0.2f, 0.0f, 1.0f);

        internal override void PushShaderParameters(HDCamera hdCamera, CommandBuffer cmd)
        {
        }

        ExponentialFog() => displayName = "Exponential Fog (Deprecated)";
    }
}
