using System;
using System.Diagnostics;

namespace UnityEngine.Rendering.HighDefinition
{
    /// <summary>
    /// Component that allow you to control the indirect specular and diffuse intensity
    /// </summary>
    [Serializable, VolumeComponentMenu("Lighting/Indirect Lighting Controller")]
    [SupportedOnRenderPipeline(typeof(HDRenderPipelineAsset))]
    [HDRPHelpURL("Override-Indirect-Lighting-Controller")]
    public class IndirectLightingController : VolumeComponent
    {
        /// <summary>Indirect diffuse lighting multiplier, between 0 and 1</summary>
        [Serialization.FormerlySerializedAs("indirectDiffuseIntensity")]
        public MinFloatParameter indirectDiffuseLightingMultiplier = new MinFloatParameter(1.0f, 0.0f);
        /// <summary>Controls which layer will be affected by the indirect diffuse lighting multiplier </summary>
        public LightLayerEnumParameter indirectDiffuseLightingLayers = new LightLayerEnumParameter(RenderingLayerMask.Everything); // Default to everything to not have migration issue

        /// <summary>Reflection lighting multiplier, between 0 and 1</summary>
        public MinFloatParameter reflectionLightingMultiplier = new MinFloatParameter(1.0f, 0.0f);
        /// <summary>Controls which layer will be affected by the reflection lighting multiplier </summary>
        public LightLayerEnumParameter reflectionLightingLayers = new LightLayerEnumParameter(RenderingLayerMask.Everything); // Default to everything to not have migration issue

        /// <summary>Reflection probe and Planar reflection intensity multiplier, between 0 and 1</summary>
        [Serialization.FormerlySerializedAs("indirectSpecularIntensity")]
        public MinFloatParameter reflectionProbeIntensityMultiplier = new MinFloatParameter(1.0f, 0.0f);

        /// <summary>
        /// Returns a mask of reflection lighting layers as uint and handle the case of Everything as being 0xFF and not -1
        /// </summary>
        /// <returns></returns>
        public uint GetReflectionLightingLayers()
        {
            int value = (int)reflectionLightingLayers.GetValue<RenderingLayerMask>();
            return value < 0 ? (uint)RenderingLayerMask.Everything : (uint)value;
        }

        /// <summary>
        /// Returns a mask of indirect diffuse lighting layers as uint and handle the case of Everything as being 0xFF and not -1
        /// </summary>
        /// <returns></returns>
        public uint GetIndirectDiffuseLightingLayers()
        {
            int value = (int)indirectDiffuseLightingLayers.GetValue<RenderingLayerMask>();
            return value < 0 ? (uint)RenderingLayerMask.Everything : (uint)value;
        }

        /// <summary>
        /// Sky Ambient Mode volume parameter.
        /// </summary>
        [Serializable, DebuggerDisplay(k_DebuggerDisplay)]
        public sealed class LightLayerEnumParameter : VolumeParameter<RenderingLayerMask>
        {
            /// <summary>
            /// Light Layer Enum parameterconstructor.
            /// </summary>
            /// <param name="value">Light Layer Enum parameter.</param>
            /// <param name="overrideState">Initial override value.</param>
            public LightLayerEnumParameter(RenderingLayerMask value, bool overrideState = false)
                : base(value, overrideState) { }
        }
    }
}
