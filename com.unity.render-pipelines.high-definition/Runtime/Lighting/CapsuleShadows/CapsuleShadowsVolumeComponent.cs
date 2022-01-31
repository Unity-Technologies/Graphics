using System;
using System.Diagnostics;

namespace UnityEngine.Rendering.HighDefinition
{
    [Serializable]
    public sealed class CapsuleAmbientOcclusionMethodParameter : VolumeParameter<CapsuleAmbientOcclusionMethod>
    {
        public CapsuleAmbientOcclusionMethodParameter(CapsuleAmbientOcclusionMethod value, bool overrideState = false) : base(value, overrideState) { }
    }

    [Serializable, VolumeComponentMenuForRenderPipeline("Shadowing/Capsule Shadows", typeof(HDRenderPipeline))]
    public class CapsuleShadowsVolumeComponent : VolumeComponent
    {
        /// <summary>
        /// When enabled, HDRP processes Capsule Shadows for this Volume.
        /// </summary>
        public BoolParameter enable = new BoolParameter(true);

        /// <summary>
        /// When enabled, HDRP processes Capsule Ambient Occlusion for this Volume.
        /// </summary>
        public BoolParameter enableAmbientOcclusion = new BoolParameter(true);

        /// <summary>
        /// Controls the range of ambient occlusion from capsules.
        /// </summary>
        public FloatParameter ambientOcclusionRange = new MinFloatParameter(1.0f, 0.0f);

        /// <summary>
        /// Controls the method used to apply ambient occlusion from capsules.
        /// </summary>
        public CapsuleAmbientOcclusionMethodParameter ambientOcclusionMethod = new CapsuleAmbientOcclusionMethodParameter(CapsuleAmbientOcclusionMethod.LineIntegral);

        CapsuleShadowsVolumeComponent()
        {
            displayName = "Capsule Shadows";
        }
    }
}
