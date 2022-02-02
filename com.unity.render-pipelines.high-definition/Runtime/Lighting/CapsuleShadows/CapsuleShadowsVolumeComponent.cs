using System;
using System.Diagnostics;
using UnityEngine;

namespace UnityEngine.Rendering.HighDefinition
{
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
        /// The range of indirect shadows from capsules, in multiples of the capsule radius.
        /// </summary>
        public FloatParameter indirectRangeFactor = new MinFloatParameter(4.0f, 0.0f);

        /// <summary>
        /// The method to use for indirect shadowing from capsules.
        /// </summary>
        public CapsuleIndirectShadowMethodParameter indirectShadowMethod = new CapsuleIndirectShadowMethodParameter(CapsuleIndirectShadowMethod.AmbientOcclusion);

        /// <summary>
        /// The method used for ambient occlusion, if selected as the indirect shadowing method.
        /// </summary>
        public CapsuleAmbientOcclusionMethodParameter ambientOcclusionMethod = new CapsuleAmbientOcclusionMethodParameter(CapsuleAmbientOcclusionMethod.LineAndClosestSphere);


        CapsuleShadowsVolumeComponent()
        {
            displayName = "Capsule Shadows";
        }
    }
}
