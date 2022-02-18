using System;

namespace UnityEngine.Rendering.HighDefinition
{
    // Deprecated, kept for migration
    [Obsolete()]
    class VolumetricFog : AtmosphericScattering
    {
        public ColorParameter albedo = new ColorParameter(Color.white);
        public MinFloatParameter meanFreePath = new MinFloatParameter(1000000.0f, 1.0f);
        public FloatParameter baseHeight = new FloatParameter(0.0f);
        public FloatParameter maximumHeight = new FloatParameter(10.0f);
        public ClampedFloatParameter anisotropy = new ClampedFloatParameter(0.0f, -1.0f, 1.0f);
        public ClampedFloatParameter globalLightProbeDimmer = new ClampedFloatParameter(1.0f, 0.0f, 1.0f);
        public BoolParameter enableDistantFog = new BoolParameter(false);

        internal override void PushShaderParameters(HDCamera hdCamera, CommandBuffer cmd)
        {
        }

        VolumetricFog() => displayName = "Volumetric Fog (Deprecated)";
    }
}
