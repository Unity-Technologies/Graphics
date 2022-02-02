using System;
using System.Diagnostics;

namespace UnityEngine.Rendering.HighDefinition
{
    public enum CapsuleIndirectShadowMethod
    {
        AmbientOcclusion,
        //Directional,
    }

    [Serializable]
    public sealed class CapsuleAmbientOcclusionMethodParameter : VolumeParameter<CapsuleAmbientOcclusionMethod>
    {
        public CapsuleAmbientOcclusionMethodParameter(CapsuleAmbientOcclusionMethod value, bool overrideState = false) : base(value, overrideState) { }
    }

    [Serializable]
    public sealed class CapsuleIndirectShadowMethodParameter : VolumeParameter<CapsuleIndirectShadowMethod>
    {
        public CapsuleIndirectShadowMethodParameter(CapsuleIndirectShadowMethod value, bool overrideState = false) : base(value, overrideState) { }
    }

    [Serializable, VolumeComponentMenuForRenderPipeline("Shadowing/Capsule Shadows", typeof(HDRenderPipeline))]
    public class CapsuleShadowsVolumeComponent : VolumeComponent
    {
        /// <summary>
        /// When enabled, capsules cast shadows for supported lights.
        /// </summary>
        public BoolParameter enableDirectShadows = new BoolParameter(true);

        /// <summary>
        /// When enabled, capsules cast shadows for supported lights.
        /// </summary>
        public BoolParameter enableIndirectShadows = new BoolParameter(false);

        /// <summary>
        /// The method to use for indirect shadowing from capsules.
        /// </summary>
        public CapsuleIndirectShadowMethodParameter indirectShadowMethod = new CapsuleIndirectShadowMethodParameter(CapsuleIndirectShadowMethod.AmbientOcclusion);

        /// <summary>
        /// Controls the range of ambient occlusion from capsules, in multiples of the capsule radius.
        /// </summary>
        public FloatParameter ambientOcclusionRangeFactor = new MinFloatParameter(4.0f, 0.0f);

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
