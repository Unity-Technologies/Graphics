using System;

namespace UnityEngine.Rendering.HighDefinition
{
    /// <summary>
    /// Component that allow you to control the indirect specular and diffuse intensity
    /// </summary>
    [Serializable, VolumeComponentMenu("Lighting/Indirect Lighting Controller")]
    public class IndirectLightingController : VolumeComponent
    {
        [FormerlySerializedAs("indirectSpecularIntensity")]
        /// <summary>Reflection probe intensity multiplier, between 0 and 1</summary>
        public MinFloatParameter reflectionProbeIntensityMultiplier = new MinFloatParameter(1.0f, 0.0f);
        /// <summary>Reflection lighting multiplier, between 0 and 1</summary>
        public MinFloatParameter reflectionLightingMultiplier = new MinFloatParameter(1.0f, 0.0f);
        /// Controls which layer will be affected by the reflection lighting multiplier 
        public LightLayerEnum reflectionLightinglayersMask = LightLayerEnum.LightLayerDefault;
        /// <summary>Indirect diffuse lighting multiplier, between 0 and 1</summary>
        public MinFloatParameter indirectDiffuseLightingMultiplier = new MinFloatParameter(1.0f, 0.0f);
        /// Controls which layer will be affected by the indirect diffuse lighting multiplier 
        public LightLayerEnum indirectDiffuseLightinglayersMask = LightLayerEnum.LightLayerDefault;
    }
}
