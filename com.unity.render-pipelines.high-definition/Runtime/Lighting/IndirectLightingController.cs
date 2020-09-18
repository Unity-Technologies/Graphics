using System;
using System.Diagnostics;

namespace UnityEngine.Rendering.HighDefinition
{
    /// <summary>
    /// Component that allow you to control the indirect specular and diffuse intensity
    /// </summary>
    [Serializable, VolumeComponentMenu("Lighting/Indirect Lighting Controller")]
    public class IndirectLightingController : VolumeComponent
    {
        /// <summary>Indirect diffuse lighting multiplier, between 0 and 1</summary>
        public MinFloatParameter indirectDiffuseIntensity = new MinFloatParameter(1.0f, 0.0f);
        /// Controls which layer will be affected by the indirect diffuse lighting multiplier 
        public LightLayerEnumParameter indirectDiffuseLightingLayers = new LightLayerEnumParameter(LightLayerEnum.Everything); // Default to everything to not have migration issue

        /// <summary>Reflection lighting multiplier, between 0 and 1</summary>
        public MinFloatParameter reflectionLightingMultiplier = new MinFloatParameter(1.0f, 0.0f);
        /// Controls which layer will be affected by the reflection lighting multiplier 
        public LightLayerEnumParameter reflectionLightingLayers = new LightLayerEnumParameter(LightLayerEnum.Everything); // Default to everything to not have migration issue

        /// <summary>Reflection probe intensity multiplier, between 0 and 1</summary>
        public MinFloatParameter indirectSpecularIntensity = new MinFloatParameter(1.0f, 0.0f);

        /// <summary>
        /// Returns a mask of reflection lighting layers as uint and handle the case of Everything as being 0xFF and not -1
        /// </summary>
        /// <returns></returns>
        public uint GetReflectionLightingLayers()
        {
            int value = (int)reflectionLightingLayers.GetValue<LightLayerEnum>();
            return value < 0 ? (uint)LightLayerEnum.Everything : (uint)value;
        }

        /// <summary>
        /// Returns a mask of indirect diffuse lighting layers as uint and handle the case of Everything as being 0xFF and not -1
        /// </summary>
        /// <returns></returns>
        public uint GetIndirectDiffuseLightingLayers()
        {
            int value = (int)indirectDiffuseLightingLayers.GetValue<LightLayerEnum>();
            return value < 0 ? (uint)LightLayerEnum.Everything : (uint)value;
        }

        /// <summary>
        /// Sky Ambient Mode volume parameter.
        /// </summary>
        [Serializable, DebuggerDisplay(k_DebuggerDisplay)]
        public sealed class LightLayerEnumParameter : VolumeParameter<LightLayerEnum>
        {
            /// <summary>
            /// Light Layer Enum parameterconstructor.
            /// </summary>
            /// <param name="value">Light Layer Enum parameter.</param>
            /// <param name="overrideState">Initial override value.</param>
            public LightLayerEnumParameter(LightLayerEnum value, bool overrideState = false)
                : base(value, overrideState) { }
        }
    }
}
