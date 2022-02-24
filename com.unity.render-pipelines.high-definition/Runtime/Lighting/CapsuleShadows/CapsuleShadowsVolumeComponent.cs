using System;
using System.Diagnostics;
using UnityEngine;

namespace UnityEngine.Rendering.HighDefinition
{
    [Serializable]
    public sealed class CapsuleShadowPipelineParameter : VolumeParameter<CapsuleShadowPipeline>
    {
        public CapsuleShadowPipelineParameter(CapsuleShadowPipeline value, bool overrideState = false) : base(value, overrideState) { }
    }

    [Serializable]
    public sealed class CapsuleShadowResolutionParameter : VolumeParameter<CapsuleShadowResolution>
    {
        public CapsuleShadowResolutionParameter(CapsuleShadowResolution value, bool overrideState = false) : base(value, overrideState) { }
    }

    [Serializable]
    public sealed class CapsuleShadowTextureFormatParameter : VolumeParameter<CapsuleShadowTextureFormat>
    {
        public CapsuleShadowTextureFormatParameter(CapsuleShadowTextureFormat value, bool overrideState = false) : base(value, overrideState) { }
    }

    [Serializable]
    public sealed class CapsuleShadowMethodParameter : VolumeParameter<CapsuleShadowMethod>
    {
        public CapsuleShadowMethodParameter(CapsuleShadowMethod value, bool overrideState = false) : base(value, overrideState) { }
    }

    [Serializable]
    public sealed class CapsuleIndirectShadowMethodParameter : VolumeParameter<CapsuleIndirectShadowMethod>
    {
        public CapsuleIndirectShadowMethodParameter(CapsuleIndirectShadowMethod value, bool overrideState = false) : base(value, overrideState) { }
    }

    [Serializable]
    public sealed class CapsuleAmbientOcclusionMethodParameter : VolumeParameter<CapsuleAmbientOcclusionMethod>
    {
        public CapsuleAmbientOcclusionMethodParameter(CapsuleAmbientOcclusionMethod value, bool overrideState = false) : base(value, overrideState) { }
    }

    [Serializable, VolumeComponentMenuForRenderPipeline("Shadowing/Capsule Shadows", typeof(HDRenderPipeline))]
    public class CapsuleShadowsVolumeComponent : VolumeComponent
    {
        /// <summary>
        /// Choose a pipeline for how capsule shadowing interacts with HDRP.
        /// </summary>
        public CapsuleShadowPipelineParameter pipeline = new CapsuleShadowPipelineParameter(CapsuleShadowPipeline.AfterDepthPrePass);

        /// <summary>
        /// Choose what resolution to use when rendering capsules shadows after the depth pre-pass.
        /// </summary>
        public CapsuleShadowResolutionParameter resolution = new CapsuleShadowResolutionParameter(CapsuleShadowResolution.Half);

        /// <summary>
        /// Skip storing and upscaling results in tiles with no shadowing.
        /// </summary>
        public BoolParameter skipEmptyTiles = new BoolParameter(true);

        /// <summary>
        /// Choose a texture format for capsule shadows to be resolved to.
        /// </summary>
        public CapsuleShadowTextureFormatParameter textureFormat = new CapsuleShadowTextureFormatParameter(CapsuleShadowTextureFormat.U8);

        /// <summary>
        /// Allow tiles that skip a lot of depth range to split their depth range (test capsules twice).
        /// </summary>
        public BoolParameter useSplitDepthRange = new BoolParameter(true);

        /// <summary>
        /// When enabled, capsules cast shadows for supported lights.
        /// </summary>
        public BoolParameter enableDirectShadows = new BoolParameter(true);

        /// <summary>
        /// The method to use for direct shadowing from capsules.
        /// </summary>
        public CapsuleShadowMethodParameter directShadowMethod = new CapsuleShadowMethodParameter(CapsuleShadowMethod.FlattenThenClosestSphere);

        /// <summary>
        /// Whether to fade out self-shadowing artifacts from capsules.
        /// </summary>
        public BoolParameter fadeDirectSelfShadow = new BoolParameter(true);

        /// <summary>
        /// When enabled, capsules produce indirect shadows or ambient occlusion.
        /// </summary>
        public BoolParameter enableIndirectShadows = new BoolParameter(true);

        /// <summary>
        /// The minimium amount of visibility that must remain after indirect shadows.
        /// </summary>
        public FloatParameter indirectMinVisibility = new ClampedFloatParameter(0.1f, 0.0f, 1.0f);

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
        public CapsuleAmbientOcclusionMethodParameter ambientOcclusionMethod = new CapsuleAmbientOcclusionMethodParameter(CapsuleAmbientOcclusionMethod.ClosestSphere);

        /// <summary>
        /// The angular diameter of the virtual light source when using directional indirect shadows.
        /// </summary>
        public ClampedFloatParameter indirectAngularDiameter = new ClampedFloatParameter(45.0f, 1.0f, 90.0f);

        /// <summary>
        /// A world space bias to add to the indirect light direction when using directional indirect shadows.
        /// </summary>
        public Vector3Parameter indirectDirectionBias = new Vector3Parameter(new Vector3(0.0f, 0.5f, 0.0f));

        CapsuleShadowsVolumeComponent()
        {
            displayName = "Capsule Shadows";
        }
    }
}
