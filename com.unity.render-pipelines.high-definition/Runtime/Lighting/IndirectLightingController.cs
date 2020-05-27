using System;

namespace UnityEngine.Rendering.HighDefinition
{
    /// <summary>
    /// Component that allow you to control the indirect specular and diffuse intensity
    /// </summary>
    [Serializable, VolumeComponentMenu("Lighting/Indirect Lighting Controller")]
    public class IndirectLightingController : VolumeComponent
    {
        /// <summary>Indirect specular intensity multiplier, between 0 and 1</summary>
        public MinFloatParameter    indirectSpecularIntensity = new MinFloatParameter(1.0f, 0.0f);
        /// <summary>Indirect diffuse intensity multiplier, between 0 and 1</summary>
        public MinFloatParameter    indirectDiffuseIntensity = new MinFloatParameter(1.0f, 0.0f);
    }
}
