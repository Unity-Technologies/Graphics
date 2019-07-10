using System;

namespace UnityEngine.Rendering.HighDefinition
{
    [Serializable, VolumeComponentMenu("Lighting/Indirect Lighting Controller")]
    class IndirectLightingController : VolumeComponent
    {
        public MinFloatParameter    indirectSpecularIntensity = new MinFloatParameter(1.0f, 0.0f);
        public MinFloatParameter    indirectDiffuseIntensity = new MinFloatParameter(1.0f, 0.0f);
    }
}
