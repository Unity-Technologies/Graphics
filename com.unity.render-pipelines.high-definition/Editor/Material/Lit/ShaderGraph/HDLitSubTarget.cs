using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.UIElements;
using UnityEditor.ShaderGraph;
using UnityEditor.ShaderGraph.Internal;
using UnityEditor.UIElements;
using static UnityEngine.Rendering.HighDefinition.HDMaterialProperties;

namespace UnityEditor.Rendering.HighDefinition.ShaderGraph
{
    sealed class HDLitSubTarget : SubTarget<HDTarget>
    {
        public enum MaterialType
        {
            Standard,
            SubsurfaceScattering,
            Anisotropy,
            Iridescence,
            SpecularColor,
            Translucent
        }

        const string kAssetGuid = "caab952c840878340810cca27417971c";
        static string passTemplatePath => $"{HDUtils.GetHDRenderPipelinePath()}Editor/Material/Lit/ShaderGraph/LitPass.template";

        [SerializeField]
        bool m_RayTracing;

        [SerializeField]
        bool m_BlendPreserveSpecular = true;

        [SerializeField]
        ScreenSpaceRefraction.RefractionModel m_RefractionModel;

        [SerializeField]
        bool m_AlphaTestDepthPrepass;

        [SerializeField]
        bool m_AlphaTestDepthPostpass;

        [SerializeField]
        bool m_TransparentWritesMotionVec; // TODO: Why doesnt HDUnlitSubTarget have this?

        [SerializeField]
        bool m_AlphaTestShadow;

        [SerializeField]
        bool m_BackThenFrontRendering;

        [SerializeField]
        NormalDropOffSpace m_NormalDropOffSpace;

        [SerializeField]
        MaterialType m_MaterialType;

        [SerializeField]
        bool m_SSSTransmission = true;

        [SerializeField]
        bool m_ReceiveDecals = true;

        [SerializeField]
        bool m_ReceivesSSR = true;

        [SerializeField]
        bool m_ReceivesSSRTransparent = true;

        [SerializeField]
        bool m_EnergyConservingSpecular = true;

        [SerializeField]
        bool m_SpecularAA;

        [SerializeField]
        float m_SpecularAAScreenSpaceVariance;

        [SerializeField]
        float m_SpecularAAThreshold;

        [SerializeField]
        SpecularOcclusionMode m_SpecularOcclusionMode;

        [SerializeField]
        int m_DiffusionProfile;

        [SerializeField]
        bool m_OverrideBakedGI;

        [SerializeField]
        bool m_DepthOffset;

        [SerializeField]
        bool m_SupportLodCrossFade;

        [SerializeField]
        int m_MaterialNeedsUpdateHash = 0;
        
        public HDLitSubTarget()
        {
            displayName = "Lit";
        }        

        string renderType => HDRenderTypeTags.HDLitShader.ToString();
        string renderQueue => HDRenderQueue.GetShaderTagValue(HDRenderQueue.ChangeType(target.renderingPass, target.sortPriority, target.alphaTest));

        public bool rayTracing
        {
            get => m_RayTracing;
            set => m_RayTracing = value;
        }

        public bool blendPreserveSpecular
        {
            get => m_BlendPreserveSpecular;
            set => m_BlendPreserveSpecular = value;
        }

        public ScreenSpaceRefraction.RefractionModel refractionModel
        {
            get => m_RefractionModel;
            set => m_RefractionModel = value;
        }

        public bool alphaTestDepthPrepass
        {
            get => m_AlphaTestDepthPrepass;
            set => m_AlphaTestDepthPrepass = value;
        }

        public bool alphaTestDepthPostpass
        {
            get => m_AlphaTestDepthPostpass;
            set => m_AlphaTestDepthPostpass = value;
        }

        public bool transparentWritesMotionVec
        {
            get => m_TransparentWritesMotionVec;
            set => m_TransparentWritesMotionVec = value;
        }

        public bool alphaTestShadow
        {
            get => m_AlphaTestShadow;
            set => m_AlphaTestShadow = value;
        }

        public bool backThenFrontRendering
        {
            get => m_BackThenFrontRendering;
            set => m_BackThenFrontRendering = value;
        }

        public NormalDropOffSpace normalDropOffSpace
        {
            get => m_NormalDropOffSpace;
            set => m_NormalDropOffSpace = value;
        }

        public MaterialType materialType
        {
            get => m_MaterialType;
            set => m_MaterialType = value;
        }

        public bool sssTransmission
        {
            get => m_SSSTransmission;
            set => m_SSSTransmission = value;
        }

        public bool receiveDecals
        {
            get => m_ReceiveDecals;
            set => m_ReceiveDecals = value;
        }

        public bool receiveSSR
        {
            get => m_ReceivesSSR;
            set => m_ReceivesSSR = value;
        }

        public bool receiveSSRTransparent
        {
            get => m_ReceivesSSRTransparent;
            set => m_ReceivesSSRTransparent = value;
        }

        public bool energyConservingSpecular
        {
            get => m_EnergyConservingSpecular;
            set => m_EnergyConservingSpecular = value;
        }

        public bool specularAA
        {
            get => m_SpecularAA;
            set => m_SpecularAA = value;
        }

        public float specularAAScreenSpaceVariance
        {
            get => m_SpecularAAScreenSpaceVariance;
            set => m_SpecularAAScreenSpaceVariance = value;
        }

        public float specularAAThreshold
        {
            get => m_SpecularAAThreshold;
            set => m_SpecularAAThreshold = value;
        }

        public SpecularOcclusionMode specularOcclusionMode
        {
            get => m_SpecularOcclusionMode;
            set => m_SpecularOcclusionMode = value;
        }

        public int diffusionProfile
        {
            get => m_DiffusionProfile;
            set => m_DiffusionProfile = value;
        }

        public bool overrideBakedGI
        {
            get => m_OverrideBakedGI;
            set => m_OverrideBakedGI = value;
        }

        public bool depthOffset
        {
            get => m_DepthOffset;
            set => m_DepthOffset = value;
        }

        public bool supportLodCrossFade
        {
            get => m_SupportLodCrossFade;
            set => m_SupportLodCrossFade = value;
        }

        bool hasRefraction => target.surfaceType == SurfaceType.Transparent && target.renderingPass != HDRenderQueue.RenderQueueType.PreRefraction && m_RefractionModel != ScreenSpaceRefraction.RefractionModel.None;
        bool hasSplitLighting => m_MaterialType == MaterialType.SubsurfaceScattering;
        bool hasAlphaTestShadow => target.alphaTest && m_AlphaTestShadow;
        bool hasTransmission => ((m_MaterialType == MaterialType.SubsurfaceScattering && m_SSSTransmission) || m_MaterialType == MaterialType.Translucent);
        bool hasBlendPreserveSpecular => target.surfaceType != SurfaceType.Opaque && m_BlendPreserveSpecular;
        bool hasTransparentWritesMotionVec => target.surfaceType != SurfaceType.Opaque && m_TransparentWritesMotionVec;
        bool hasBackThenFrontRendering => target.surfaceType != SurfaceType.Opaque && m_BackThenFrontRendering;
        bool hasTransparentDepthPrepass => target.surfaceType != SurfaceType.Opaque && m_AlphaTestDepthPrepass;
        bool hasTransparentDepthPostpass => target.surfaceType != SurfaceType.Opaque && m_AlphaTestDepthPostpass;

        public override bool IsActive() => true;

        public override void Setup(ref TargetSetupContext context)
        {
            context.AddAssetDependencyPath(AssetDatabase.GUIDToAssetPath(kAssetGuid));
            context.SetDefaultShaderGUI("Rendering.HighDefinition.HDLitGUI");

            // We need to validate rendering pass before calculating RenderQueue
            // Therefore we do that here to avoid a multi-line property
            ValidateRenderingPass();

            // Process SubShaders
            SubShaderDescriptor[] subShaders = { SubShaders.Lit, SubShaders.LitRaytracing };
            for(int i = 0; i < subShaders.Length; i++)
            {
                // Update Render State
                subShaders[i].renderType = renderType;
                subShaders[i].renderQueue = renderQueue;

                // Add
                context.AddSubShader(subShaders[i]);
            }
        }

        public override void GetFields(ref TargetFieldContext context)
        {
            // TODO: Figure this out...
            // We need this to know if there are any Dots properties active
            // Ideally we do this another way but HDLit needs this for conditional pragmas
            // var shaderProperties = new PropertyCollector();
            // owner.CollectShaderProperties(shaderProperties, GenerationMode.ForReals);
            bool hasDotsProperties = false; //shaderProperties.GetDotsInstancingPropertiesCount(GenerationMode.ForReals) > 0;

            // Structs
            context.AddField(HDStructFields.FragInputs.IsFrontFace,         target.doubleSidedMode != DoubleSidedMode.Disabled && !context.pass.Equals(LitPasses.MotionVectors));

            // Material
            context.AddField(HDFields.Anisotropy,                           m_MaterialType == MaterialType.Anisotropy);
            context.AddField(HDFields.Iridescence,                          m_MaterialType == MaterialType.Iridescence);
            context.AddField(HDFields.SpecularColor,                        m_MaterialType == MaterialType.SpecularColor);
            context.AddField(HDFields.Standard,                             m_MaterialType == MaterialType.Standard);
            context.AddField(HDFields.SubsurfaceScattering,                 m_MaterialType == MaterialType.SubsurfaceScattering && target.surfaceType != SurfaceType.Transparent);
            context.AddField(HDFields.Translucent,                          m_MaterialType == MaterialType.Translucent);
            context.AddField(HDFields.Transmission,                         hasTransmission);

            // Specular Occlusion
            context.AddField(HDFields.SpecularOcclusionFromAO,              m_SpecularOcclusionMode == SpecularOcclusionMode.FromAO);
            context.AddField(HDFields.SpecularOcclusionFromAOBentNormal,    m_SpecularOcclusionMode == SpecularOcclusionMode.FromAOAndBentNormal);
            context.AddField(HDFields.SpecularOcclusionCustom,              m_SpecularOcclusionMode == SpecularOcclusionMode.Custom);

            // Refraction
            context.AddField(HDFields.Refraction,                           hasRefraction);
            context.AddField(HDFields.RefractionBox,                        hasRefraction && m_RefractionModel == ScreenSpaceRefraction.RefractionModel.Box);
            context.AddField(HDFields.RefractionSphere,                     hasRefraction && m_RefractionModel == ScreenSpaceRefraction.RefractionModel.Sphere);

            // Normal Drop Off Space
            context.AddField(Fields.NormalDropOffOS,                        m_NormalDropOffSpace == NormalDropOffSpace.Object);
            context.AddField(Fields.NormalDropOffTS,                        m_NormalDropOffSpace == NormalDropOffSpace.Tangent);
            context.AddField(Fields.NormalDropOffWS,                        m_NormalDropOffSpace == NormalDropOffSpace.World);

            // Dots
            context.AddField(HDFields.DotsInstancing,                       target.dotsInstancing); // TODO: Why doesnt Unlit have this?
            context.AddField(HDFields.DotsProperties,                       hasDotsProperties); // TODO: Why doesnt Unlit have this?

            // Misc
            context.AddField(Fields.LodCrossFade,                           supportLodCrossFade);
            context.AddField(HDFields.DoAlphaTestShadow,                    hasAlphaTestShadow && context.pass.pixelBlocks.Contains(HDBlockFields.SurfaceDescription.AlphaClipThresholdShadow));
            context.AddField(HDFields.DoAlphaTestPrepass,                   target.alphaTest && context.pass.pixelBlocks.Contains(HDBlockFields.SurfaceDescription.AlphaClipThresholdDepthPrepass));
            context.AddField(HDFields.DoAlphaTestPostpass,                  target.alphaTest && context.pass.pixelBlocks.Contains(HDBlockFields.SurfaceDescription.AlphaClipThresholdDepthPostpass));
            context.AddField(HDFields.BlendPreserveSpecular,                hasBlendPreserveSpecular);
            context.AddField(HDFields.TransparentWritesMotionVec,           hasTransparentWritesMotionVec);
            context.AddField(HDFields.DisableDecals,                        !receiveDecals);
            context.AddField(HDFields.DisableSSR,                           !receiveSSR);
            context.AddField(HDFields.DisableSSRTransparent,                !receiveSSRTransparent);
            context.AddField(HDFields.SpecularAA,                           specularAA && context.pass.pixelBlocks.Contains(HDBlockFields.SurfaceDescription.SpecularAAThreshold) && context.pass.pixelBlocks.Contains(HDBlockFields.SurfaceDescription.SpecularAAScreenSpaceVariance));
            context.AddField(HDFields.DepthOffset,                          depthOffset && context.pass.pixelBlocks.Contains(HDBlockFields.SurfaceDescription.DepthOffset));
            context.AddField(HDFields.TransparentBackFace,                  hasBackThenFrontRendering);
            context.AddField(HDFields.TransparentDepthPrePass,              hasTransparentDepthPrepass);
            context.AddField(HDFields.TransparentDepthPostPass,             hasTransparentDepthPostpass);
            context.AddField(HDFields.EnergyConservingSpecular,             energyConservingSpecular);
            context.AddField(HDFields.BentNormal,                           context.blocks.Contains(HDBlockFields.SurfaceDescription.BentNormal) && context.pass.pixelBlocks.Contains(HDBlockFields.SurfaceDescription.BentNormal));
            context.AddField(HDFields.AmbientOcclusion,                     context.blocks.Contains(BlockFields.SurfaceDescription.Occlusion) && context.pass.pixelBlocks.Contains(BlockFields.SurfaceDescription.Occlusion));
            context.AddField(HDFields.CoatMask,                             context.blocks.Contains(HDBlockFields.SurfaceDescription.CoatMask) && context.pass.pixelBlocks.Contains(HDBlockFields.SurfaceDescription.CoatMask));
            context.AddField(HDFields.Tangent,                              context.blocks.Contains(HDBlockFields.SurfaceDescription.Tangent) && context.pass.pixelBlocks.Contains(HDBlockFields.SurfaceDescription.Tangent));
            context.AddField(HDFields.LightingGI,                           context.blocks.Contains(HDBlockFields.SurfaceDescription.BakedGI) && context.pass.pixelBlocks.Contains(HDBlockFields.SurfaceDescription.BakedGI));
            context.AddField(HDFields.BackLightingGI,                       context.blocks.Contains(HDBlockFields.SurfaceDescription.BakedBackGI) && context.pass.pixelBlocks.Contains(HDBlockFields.SurfaceDescription.BakedBackGI));

            // --------------------------------------------------
            // Legacy?
            // TODO: It seems that these are no longer needed. Confirm then remove.

            // Surface Type
            context.AddField(Fields.SurfaceOpaque,                          target.surfaceType == SurfaceType.Opaque);
            context.AddField(Fields.SurfaceTransparent,                     target.surfaceType != SurfaceType.Opaque);

            // Blend Mode
            context.AddField(Fields.BlendAdd,                               target.surfaceType != SurfaceType.Opaque && target.alphaMode == AlphaMode.Additive);
            context.AddField(Fields.BlendAlpha,                             target.surfaceType != SurfaceType.Opaque && target.alphaMode == AlphaMode.Alpha);
            context.AddField(Fields.BlendPremultiply,                       target.surfaceType != SurfaceType.Opaque && target.alphaMode == AlphaMode.Premultiply);

            // Double Sided
            context.AddField(HDFields.DoubleSided,                          target.doubleSidedMode != DoubleSidedMode.Disabled);
            context.AddField(HDFields.DoubleSidedFlip,                      target.doubleSidedMode == DoubleSidedMode.FlippedNormals && !context.pass.Equals(LitPasses.MotionVectors));
            context.AddField(HDFields.DoubleSidedMirror,                    target.doubleSidedMode == DoubleSidedMode.MirroredNormals && !context.pass.Equals(LitPasses.MotionVectors));
        }

        public override void GetActiveBlocks(ref TargetActiveBlockContext context)
        {
            bool hasMetallic = m_MaterialType == MaterialType.Standard || m_MaterialType == MaterialType.Anisotropy || m_MaterialType == MaterialType.Iridescence;
            bool hasDiffusionProfile = m_MaterialType == MaterialType.SubsurfaceScattering || m_MaterialType == MaterialType.Translucent;
            bool hasThickness = (m_MaterialType == MaterialType.SubsurfaceScattering && m_SSSTransmission) || m_MaterialType == MaterialType.Translucent || hasRefraction;

            // Base Lit
            context.AddBlock(HDBlockFields.SurfaceDescription.BentNormal);
            context.AddBlock(HDBlockFields.SurfaceDescription.CoatMask);
            context.AddBlock(BlockFields.SurfaceDescription.Smoothness);
            context.AddBlock(BlockFields.SurfaceDescription.Occlusion);

            // Normal
            context.AddBlock(BlockFields.SurfaceDescription.NormalTS,                           m_NormalDropOffSpace == NormalDropOffSpace.Tangent);
            context.AddBlock(BlockFields.SurfaceDescription.NormalWS,                           m_NormalDropOffSpace == NormalDropOffSpace.World);
            context.AddBlock(BlockFields.SurfaceDescription.NormalOS,                           m_NormalDropOffSpace == NormalDropOffSpace.Object);
            
            // Material Type
            context.AddBlock(BlockFields.SurfaceDescription.Metallic,                           hasMetallic);
            context.AddBlock(HDBlockFields.SurfaceDescription.DiffusionProfileHash,             hasDiffusionProfile);
            context.AddBlock(HDBlockFields.SurfaceDescription.Thickness,                        hasThickness);
            context.AddBlock(BlockFields.SurfaceDescription.Specular,                           m_MaterialType == MaterialType.SpecularColor);
            context.AddBlock(HDBlockFields.SurfaceDescription.SubsurfaceMask,                   m_MaterialType == MaterialType.SubsurfaceScattering);
            context.AddBlock(HDBlockFields.SurfaceDescription.Anisotropy,                       m_MaterialType == MaterialType.Anisotropy);
            context.AddBlock(HDBlockFields.SurfaceDescription.Tangent,                          m_MaterialType == MaterialType.Anisotropy);
            context.AddBlock(HDBlockFields.SurfaceDescription.IridescenceMask,                  m_MaterialType == MaterialType.Iridescence);
            context.AddBlock(HDBlockFields.SurfaceDescription.IridescenceThickness,             m_MaterialType == MaterialType.Iridescence);

            // Alpha Test
            context.AddBlock(HDBlockFields.SurfaceDescription.AlphaClipThresholdDepthPrepass,   hasTransparentDepthPrepass && target.alphaTest);
            context.AddBlock(HDBlockFields.SurfaceDescription.AlphaClipThresholdDepthPostpass,  hasTransparentDepthPostpass && target.alphaTest);
            context.AddBlock(HDBlockFields.SurfaceDescription.AlphaClipThresholdShadow,         m_AlphaTestShadow && target.alphaTest);

            // Refraction
            context.AddBlock(HDBlockFields.SurfaceDescription.RefractionIndex,                  hasRefraction);
            context.AddBlock(HDBlockFields.SurfaceDescription.RefractionColor,                  hasRefraction);
            context.AddBlock(HDBlockFields.SurfaceDescription.RefractionDistance,               hasRefraction);

            // Misc
            context.AddBlock(HDBlockFields.SurfaceDescription.SpecularOcclusion,                specularOcclusionMode == SpecularOcclusionMode.Custom);
            context.AddBlock(HDBlockFields.SurfaceDescription.SpecularAAScreenSpaceVariance,    m_SpecularAA);
            context.AddBlock(HDBlockFields.SurfaceDescription.SpecularAAThreshold,              m_SpecularAA);
            context.AddBlock(HDBlockFields.SurfaceDescription.BakedGI,                          m_OverrideBakedGI);
            context.AddBlock(HDBlockFields.SurfaceDescription.BakedBackGI,                      m_OverrideBakedGI);
            context.AddBlock(HDBlockFields.SurfaceDescription.DepthOffset,                      m_DepthOffset);
        }

        public override void GetPropertiesGUI(ref TargetPropertyGUIContext context, Action onChange)
        {
            context.AddProperty("Ray Tracing (Preview)", 0, new Toggle() { value = rayTracing }, (evt) =>
            {
                if (Equals(rayTracing, evt.newValue))
                    return;

                rayTracing = evt.newValue;
                onChange();
            });

            context.AddProperty("Surface Type", 0, new EnumField(SurfaceType.Opaque) { value = target.surfaceType }, (evt) =>
            {
                if (Equals(target.surfaceType, evt.newValue))
                    return;

                target.surfaceType = (SurfaceType)evt.newValue;
                target.UpdateRenderingPassValue(target.renderingPass);
                onChange();
            });

            var renderingPassList = HDSubShaderUtilities.GetRenderingPassList(target.surfaceType == SurfaceType.Opaque, true);
            var renderingPassValue = target.surfaceType == SurfaceType.Opaque ? HDRenderQueue.GetOpaqueEquivalent(target.renderingPass) : HDRenderQueue.GetTransparentEquivalent(target.renderingPass);
            var renderQueueType = target.surfaceType == SurfaceType.Opaque ? HDRenderQueue.RenderQueueType.Opaque : HDRenderQueue.RenderQueueType.Transparent;
            context.AddProperty("Rendering Pass", 1, new PopupField<HDRenderQueue.RenderQueueType>(renderingPassList, renderQueueType, HDSubShaderUtilities.RenderQueueName, HDSubShaderUtilities.RenderQueueName) { value = renderingPassValue }, (evt) =>
            {
                if(target.ChangeRenderingPass(evt.newValue))
                {
                    onChange();
                }
            });

            context.AddProperty("Blending Mode", 1, new EnumField(AlphaMode.Alpha) { value = target.alphaMode }, target.surfaceType == SurfaceType.Transparent && !hasRefraction, (evt) =>
            {
                if (Equals(target.alphaMode, evt.newValue))
                    return;

                target.alphaMode = (AlphaMode)evt.newValue;
                onChange();
            });

            context.AddProperty("Preserve Specular Lighting", 2, new Toggle() { value = blendPreserveSpecular }, target.surfaceType == SurfaceType.Transparent && !hasRefraction, (evt) =>
            {
                if (Equals(blendPreserveSpecular, evt.newValue))
                    return;

                blendPreserveSpecular = evt.newValue;
                onChange();
            });

            context.AddProperty("Depth Test", 1, new EnumField(target.zTest) { value = target.zTest }, target.surfaceType == SurfaceType.Transparent, (evt) =>
            {
                if (Equals(target.zTest, evt.newValue))
                    return;

                target.zTest = (CompareFunction)evt.newValue;
                onChange();
            });

            context.AddProperty("Depth Write", 1, new Toggle() { value = target.zWrite }, target.surfaceType == SurfaceType.Transparent, (evt) =>
            {
                if (Equals(target.zWrite, evt.newValue))
                    return;

                target.zWrite = evt.newValue;
                onChange();
            });

            context.AddProperty("Cull Mode", 1, new EnumField(target.transparentCullMode) { value = target.transparentCullMode }, target.surfaceType == SurfaceType.Transparent && target.doubleSidedMode != DoubleSidedMode.Disabled, (evt) =>
            {
                if (Equals(target.transparentCullMode, evt.newValue))
                    return;

                target.transparentCullMode = (TransparentCullMode)evt.newValue;
                onChange();
            });

            context.AddProperty("Sorting Priority", 1, new IntegerField() { value = target.sortPriority }, target.surfaceType == SurfaceType.Transparent, (evt) =>
            {
                if (Equals(target.sortPriority, evt.newValue))
                    return;

                target.sortPriority = evt.newValue;
                onChange();
            });

            context.AddProperty("Receive Fog", 1, new Toggle() { value = target.transparencyFog }, target.surfaceType == SurfaceType.Transparent, (evt) =>
            {
                if (Equals(target.transparencyFog, evt.newValue))
                    return;

                target.transparencyFog = evt.newValue;
                onChange();
            });

            context.AddProperty("Back Then Front Rendering", 1, new Toggle() { value = backThenFrontRendering }, target.surfaceType == SurfaceType.Transparent, (evt) =>
            {
                if (Equals(backThenFrontRendering, evt.newValue))
                    return;

                backThenFrontRendering = evt.newValue;
                onChange();
            });

            context.AddProperty("Transparent Depth Prepass", 1, new Toggle() { value = alphaTestDepthPrepass }, target.surfaceType == SurfaceType.Transparent, (evt) =>
            {
                if (Equals(alphaTestDepthPrepass, evt.newValue))
                    return;

                alphaTestDepthPrepass = evt.newValue;
                onChange();
            });

            context.AddProperty("Transparent Depth Postpass", 1, new Toggle() { value = alphaTestDepthPostpass }, target.surfaceType == SurfaceType.Transparent, (evt) =>
            {
                if (Equals(alphaTestDepthPostpass, evt.newValue))
                    return;

                alphaTestDepthPostpass = evt.newValue;
                onChange();
            });

            context.AddProperty("Transparent Writes Motion Vector", 1, new Toggle() { value = transparentWritesMotionVec }, target.surfaceType == SurfaceType.Transparent, (evt) =>
            {
                if (Equals(transparentWritesMotionVec, evt.newValue))
                    return;

                transparentWritesMotionVec = evt.newValue;
                onChange();
            });

            context.AddProperty("Refraction Model", 1, new EnumField(ScreenSpaceRefraction.RefractionModel.None) { value = refractionModel }, target.surfaceType == SurfaceType.Transparent && target.renderingPass != HDRenderQueue.RenderQueueType.PreRefraction, (evt) =>
            {
                if (Equals(refractionModel, evt.newValue))
                    return;

                refractionModel = (ScreenSpaceRefraction.RefractionModel)evt.newValue;
                onChange();
            });

            context.AddProperty("Distortion", 1, new Toggle() { value = target.distortion }, target.surfaceType == SurfaceType.Transparent, (evt) =>
            {
                if (Equals(target.distortion, evt.newValue))
                    return;

                target.distortion = evt.newValue;
                onChange();
            });

            context.AddProperty("Distortion Blend Mode", 2, new EnumField(DistortionMode.Add) { value = target.distortionMode }, target.surfaceType == SurfaceType.Transparent && target.distortion, (evt) =>
            {
                if (Equals(target.distortionMode, evt.newValue))
                    return;

                target.distortionMode = (DistortionMode)evt.newValue;
                onChange();
            });

            context.AddProperty("Distortion Depth Test", 2, new Toggle() { value = target.distortionDepthTest }, target.surfaceType == SurfaceType.Transparent && target.distortion, (evt) =>
            {
                if (Equals(target.distortionDepthTest, evt.newValue))
                    return;

                target.distortionDepthTest = evt.newValue;
                onChange();
            });

            context.AddProperty("Double-Sided", 0, new EnumField(DoubleSidedMode.Disabled) { value = target.doubleSidedMode }, (evt) =>
            {
                if (Equals(target.doubleSidedMode, evt.newValue))
                    return;

                target.doubleSidedMode = (DoubleSidedMode)evt.newValue;
                onChange();
            });

            context.AddProperty("Fragment Normal Space", 0, new EnumField(NormalDropOffSpace.Tangent) { value = normalDropOffSpace }, (evt) =>
            {
                if (Equals(normalDropOffSpace, evt.newValue))
                    return;

                normalDropOffSpace = (NormalDropOffSpace)evt.newValue;
                onChange();
            });

            context.AddProperty("Alpha Clipping", 0, new Toggle() { value = target.alphaTest }, (evt) =>
            {
                if (Equals(target.alphaTest, evt.newValue))
                    return;

                target.alphaTest = evt.newValue;
                onChange();
            });

            context.AddProperty("Use Shadow Threshold", 1, new Toggle() { value = alphaTestShadow }, target.alphaTest, (evt) =>
            {
                if (Equals(alphaTestShadow, evt.newValue))
                    return;

                alphaTestShadow = evt.newValue;
                onChange();
            });

            context.AddProperty("Material Type", 0, new EnumField(MaterialType.Standard) { value = materialType }, (evt) =>
            {
                if (Equals(materialType, evt.newValue))
                    return;

                materialType = (MaterialType)evt.newValue;
                onChange();
            });

            context.AddProperty("Transmission", 1, new Toggle() { value = sssTransmission }, materialType == MaterialType.SubsurfaceScattering, (evt) =>
            {
                if (Equals(sssTransmission, evt.newValue))
                    return;

                sssTransmission = evt.newValue;
                onChange();
            });

            context.AddProperty("Energy Conserving Specular", 1, new Toggle() { value = energyConservingSpecular }, materialType == MaterialType.SpecularColor, (evt) =>
            {
                if (Equals(energyConservingSpecular, evt.newValue))
                    return;

                energyConservingSpecular = evt.newValue;
                onChange();
            });

            context.AddProperty("Receive Decals", 0, new Toggle() { value = receiveDecals }, (evt) =>
            {
                if (Equals(receiveDecals, evt.newValue))
                    return;

                receiveDecals = evt.newValue;
                onChange();
            });
            
            bool receiveSSRValue = target.surfaceType == SurfaceType.Opaque ? receiveSSR : receiveSSRTransparent;
            context.AddProperty("Receive SSR", 0, new Toggle() { value = receiveSSRValue }, (evt) =>
            {
                if (Equals(receiveSSRValue, evt.newValue))
                    return;

                receiveSSRValue = evt.newValue;
                onChange();
            });

            context.AddProperty("Add Precomputed Velocity", 0, new Toggle() { value = target.addPrecomputedVelocity }, (evt) =>
            {
                if (Equals(target.addPrecomputedVelocity, evt.newValue))
                    return;

                target.addPrecomputedVelocity = evt.newValue;
                onChange();
            });

            context.AddProperty("Geometric Specular AA", 0, new Toggle() { value = specularAA }, (evt) =>
            {
                if (Equals(specularAA, evt.newValue))
                    return;

                specularAA = evt.newValue;
                onChange();
            });

            context.AddProperty("Specular Occlusion Mode", 0, new EnumField(SpecularOcclusionMode.Off) { value = specularOcclusionMode }, (evt) =>
            {
                if (Equals(specularOcclusionMode, evt.newValue))
                    return;

                specularOcclusionMode = (SpecularOcclusionMode)evt.newValue;
                onChange();
            });

            context.AddProperty("Override Baked GI", 0, new Toggle() { value = overrideBakedGI }, (evt) =>
            {
                if (Equals(overrideBakedGI, evt.newValue))
                    return;

                overrideBakedGI = evt.newValue;
                onChange();
            });

            context.AddProperty("Depth Offset", 0, new Toggle() { value = depthOffset }, (evt) =>
            {
                if (Equals(depthOffset, evt.newValue))
                    return;

                depthOffset = evt.newValue;
                onChange();
            });

            context.AddProperty("Support LOD CrossFade", 0, new Toggle() { value = supportLodCrossFade }, (evt) =>
            {
                if (Equals(supportLodCrossFade, evt.newValue))
                    return;

                supportLodCrossFade = evt.newValue;
                onChange();
            });
        }

        void ValidateRenderingPass()
        {
            if(target.renderingPass != HDRenderQueue.RenderQueueType.Unknown)
                return;

            switch(target.surfaceType)
            {
                case SurfaceType.Opaque:
                    target.renderingPass = HDRenderQueue.RenderQueueType.Opaque;
                    break;
                case SurfaceType.Transparent:
                    target.renderingPass = HDRenderQueue.RenderQueueType.Transparent;
                    break;
            }
        }

        public override void CollectShaderProperties(PropertyCollector collector, GenerationMode generationMode)
        {
            // Add all shader properties required by the inspector
            HDSubShaderUtilities.AddStencilShaderProperties(collector, hasSplitLighting, receiveSSR, receiveSSRTransparent);
            HDSubShaderUtilities.AddBlendingStatesShaderProperties(
                collector,
                target.surfaceType,
                HDSubShaderUtilities.ConvertAlphaModeToBlendMode(target.alphaMode),
                target.sortPriority,
                target.zWrite,
                target.transparentCullMode,
                target.zTest,
                backThenFrontRendering,
                target.transparencyFog
            );
            HDSubShaderUtilities.AddAlphaCutoffShaderProperties(collector, target.alphaTest, alphaTestShadow);
            HDSubShaderUtilities.AddRayTracingProperty(collector, rayTracing);
        }

#region SubShaders
        static class SubShaders
        {
            public static SubShaderDescriptor Lit = new SubShaderDescriptor()
            {
                pipelineTag = HDRenderPipeline.k_ShaderTagName,
                generatesPreview = true,
                passes = new PassCollection
                {
                    { LitPasses.ShadowCaster },
                    { LitPasses.META },
                    { LitPasses.SceneSelection },
                    { LitPasses.DepthOnly },
                    { LitPasses.GBuffer },
                    { LitPasses.MotionVectors },
                    { LitPasses.DistortionVectors, new FieldCondition(HDFields.TransparentDistortion, true) },
                    { LitPasses.TransparentBackface, new FieldCondition(HDFields.TransparentBackFace, true) },
                    { LitPasses.TransparentDepthPrepass, new FieldCondition(HDFields.TransparentDepthPrePass, true) },
                    { LitPasses.Forward },
                    { LitPasses.TransparentDepthPostpass, new FieldCondition(HDFields.TransparentDepthPostPass, true) },
                    { LitPasses.RayTracingPrepass, new FieldCondition(HDFields.RayTracing, true) },
                },
            };

            public static SubShaderDescriptor LitRaytracing = new SubShaderDescriptor()
            {
                pipelineTag = HDRenderPipeline.k_ShaderTagName,
                generatesPreview = false,
                passes = new PassCollection
                {
                    { LitPasses.RaytracingIndirect, new FieldCondition(Fields.IsPreview, false) },
                    { LitPasses.RaytracingVisibility, new FieldCondition(Fields.IsPreview, false) },
                    { LitPasses.RaytracingForward, new FieldCondition(Fields.IsPreview, false) },
                    { LitPasses.RaytracingGBuffer, new FieldCondition(Fields.IsPreview, false) },
                    { LitPasses.RaytracingSubSurface, new FieldCondition(Fields.IsPreview, false) },
                    { LitPasses.RaytracingPathTracing, new FieldCondition(Fields.IsPreview, false) },
                },
            };
        }
#endregion

#region Passes
        public static class LitPasses
        {
            public static PassDescriptor GBuffer = new PassDescriptor()
            {
                // Definition
                displayName = "GBuffer",
                referenceName = "SHADERPASS_GBUFFER",
                lightMode = "GBuffer",
                useInPreview = true,

                // Template
                passTemplatePath = passTemplatePath,
                sharedTemplateDirectory = HDTarget.sharedTemplateDirectory,

                // Port Mask
                vertexBlocks = CoreBlockMasks.Vertex,
                pixelBlocks = LitPortMasks.FragmentDefault,

                // Collections
                structs = CoreStructCollections.Default,
                requiredFields = CoreRequiredFields.LitMinimal,
                fieldDependencies = CoreFieldDependencies.Default,
                renderStates = LitRenderStates.GBuffer,
                pragmas = CorePragmas.DotsInstancedInV1AndV2,
                defines = CoreDefines.ShaderGraphRaytracingHigh,
                keywords = LitKeywords.GBuffer,
                includes = LitIncludes.GBuffer,
            };

            public static PassDescriptor META = new PassDescriptor()
            {
                // Definition
                displayName = "META",
                referenceName = "SHADERPASS_LIGHT_TRANSPORT",
                lightMode = "META",
                useInPreview = false,

                // Template
                passTemplatePath = passTemplatePath,
                sharedTemplateDirectory = HDTarget.sharedTemplateDirectory,

                // Port Mask
                pixelBlocks = LitPortMasks.FragmentMeta,

                // Collections
                structs = CoreStructCollections.Default,
                requiredFields = CoreRequiredFields.Meta,
                fieldDependencies = CoreFieldDependencies.Default,
                renderStates = CoreRenderStates.Meta,
                pragmas = CorePragmas.DotsInstancedInV1AndV2,
                defines = CoreDefines.ShaderGraphRaytracingHigh,
                keywords = CoreKeywords.HDBase,
                includes = LitIncludes.Meta,
            };

            public static PassDescriptor ShadowCaster = new PassDescriptor()
            {
                // Definition
                displayName = "ShadowCaster",
                referenceName = "SHADERPASS_SHADOWS",
                lightMode = "ShadowCaster",
                useInPreview = false,

                // Template
                passTemplatePath = passTemplatePath,
                sharedTemplateDirectory = HDTarget.sharedTemplateDirectory,

                // Port Mask
                vertexBlocks = CoreBlockMasks.Vertex,
                pixelBlocks = LitPortMasks.FragmentShadowCaster,

                // Collections
                structs = CoreStructCollections.Default,
                fieldDependencies = CoreFieldDependencies.Default,
                renderStates = CoreRenderStates.BlendShadowCaster,
                pragmas = CorePragmas.DotsInstancedInV1AndV2,
                defines = CoreDefines.ShaderGraphRaytracingHigh,
                keywords = CoreKeywords.HDBase,
                includes = LitIncludes.DepthOnly,
            };

            public static PassDescriptor SceneSelection = new PassDescriptor()
            {
                // Definition
                displayName = "SceneSelectionPass",
                referenceName = "SHADERPASS_DEPTH_ONLY",
                lightMode = "SceneSelectionPass",
                useInPreview = false,

                // Template
                passTemplatePath = passTemplatePath,
                sharedTemplateDirectory = HDTarget.sharedTemplateDirectory,

                // Port Mask
                vertexBlocks = CoreBlockMasks.Vertex,
                pixelBlocks = LitPortMasks.FragmentSceneSelection,

                // Collections
                structs = CoreStructCollections.Default,
                fieldDependencies = CoreFieldDependencies.Default,
                renderStates = CoreRenderStates.SceneSelection,
                pragmas = CorePragmas.DotsInstancedInV1AndV2EditorSync,
                defines = CoreDefines.SceneSelection,
                keywords = CoreKeywords.HDBase,
                includes = LitIncludes.DepthOnly,
            };

            public static PassDescriptor DepthOnly = new PassDescriptor()
            {
                // Definition
                displayName = "DepthOnly",
                referenceName = "SHADERPASS_DEPTH_ONLY",
                lightMode = "DepthOnly",
                useInPreview = true,

                // Template
                passTemplatePath = passTemplatePath,
                sharedTemplateDirectory = HDTarget.sharedTemplateDirectory,

                // Port Mask
                vertexBlocks = CoreBlockMasks.Vertex,
                pixelBlocks = LitPortMasks.FragmentDepthMotionVectors,

                // Collections
                structs = CoreStructCollections.Default,
                requiredFields = CoreRequiredFields.LitFull,
                fieldDependencies = CoreFieldDependencies.Default,
                renderStates = CoreRenderStates.DepthOnly,
                pragmas = CorePragmas.DotsInstancedInV1AndV2,
                defines = CoreDefines.ShaderGraphRaytracingHigh,
                keywords = LitKeywords.DepthMotionVectors,
                includes = LitIncludes.DepthOnly,
            };

            public static PassDescriptor MotionVectors = new PassDescriptor()
            {
                // Definition
                displayName = "MotionVectors",
                referenceName = "SHADERPASS_MOTION_VECTORS",
                lightMode = "MotionVectors",
                useInPreview = false,

                // Template
                passTemplatePath = passTemplatePath,
                sharedTemplateDirectory = HDTarget.sharedTemplateDirectory,

                // Port Mask
                vertexBlocks = CoreBlockMasks.Vertex,
                pixelBlocks = LitPortMasks.FragmentDepthMotionVectors,

                // Collections
                structs = CoreStructCollections.Default,
                requiredFields = CoreRequiredFields.LitFull,
                fieldDependencies = CoreFieldDependencies.Default,
                renderStates = CoreRenderStates.MotionVectors,
                pragmas = CorePragmas.DotsInstancedInV1AndV2,
                defines = CoreDefines.ShaderGraphRaytracingHigh,
                keywords = LitKeywords.DepthMotionVectors,
                includes = LitIncludes.MotionVectors,
            };

            public static PassDescriptor DistortionVectors = new PassDescriptor()
            {
                // Definition
                displayName = "DistortionVectors",
                referenceName = "SHADERPASS_DISTORTION",
                lightMode = "DistortionVectors",
                useInPreview = true,

                // Template
                passTemplatePath = passTemplatePath,
                sharedTemplateDirectory = HDTarget.sharedTemplateDirectory,

                // Port mask
                vertexBlocks = CoreBlockMasks.Vertex,
                pixelBlocks = LitPortMasks.FragmentDistortion,

                // Collections
                structs = CoreStructCollections.Default,
                fieldDependencies = CoreFieldDependencies.Default,
                renderStates = LitRenderStates.Distortion,
                pragmas = CorePragmas.DotsInstancedInV1AndV2,
                defines = CoreDefines.ShaderGraphRaytracingHigh,
                keywords = CoreKeywords.HDBase,
                includes = LitIncludes.Distortion,
            };

            public static PassDescriptor TransparentDepthPrepass = new PassDescriptor()
            {
                // Definition
                displayName = "TransparentDepthPrepass",
                referenceName = "SHADERPASS_DEPTH_ONLY",
                lightMode = "TransparentDepthPrepass",
                useInPreview = true,

                // Template
                passTemplatePath = passTemplatePath,
                sharedTemplateDirectory = HDTarget.sharedTemplateDirectory,

                // Port Mask
                vertexBlocks = CoreBlockMasks.Vertex,
                pixelBlocks = LitPortMasks.FragmentTransparentDepthPrepass,

                // Collections
                structs = CoreStructCollections.Default,
                requiredFields = CoreRequiredFields.LitFull,
                fieldDependencies = CoreFieldDependencies.Default,
                renderStates = LitRenderStates.TransparentDepthPrePostPass,
                pragmas = CorePragmas.DotsInstancedInV1AndV2,
                defines = CoreDefines.TransparentDepthPrepass,
                keywords = CoreKeywords.HDBase,
                includes = LitIncludes.DepthOnly,
            };

            public static PassDescriptor TransparentBackface = new PassDescriptor()
            {
                // Definition
                displayName = "TransparentBackface",
                referenceName = "SHADERPASS_FORWARD",
                lightMode = "TransparentBackface",
                useInPreview = true,

                // Template
                passTemplatePath = passTemplatePath,
                sharedTemplateDirectory = HDTarget.sharedTemplateDirectory,

                // Port Mask
                vertexBlocks = CoreBlockMasks.Vertex,
                pixelBlocks = LitPortMasks.FragmentTransparentBackface,

                // Collections
                structs = CoreStructCollections.Default,
                fieldDependencies = CoreFieldDependencies.Default,
                renderStates = CoreRenderStates.TransparentBackface,
                pragmas = CorePragmas.DotsInstancedInV1AndV2,
                defines = CoreDefines.Forward,
                keywords = CoreKeywords.Forward,
                includes = LitIncludes.Forward,
            };

            public static PassDescriptor Forward = new PassDescriptor()
            {
                // Definition
                displayName = "Forward",
                referenceName = "SHADERPASS_FORWARD",
                lightMode = "Forward",
                useInPreview = true,

                // Template
                passTemplatePath = passTemplatePath,
                sharedTemplateDirectory = HDTarget.sharedTemplateDirectory,

                // Port Mask
                vertexBlocks = CoreBlockMasks.Vertex,
                pixelBlocks = LitPortMasks.FragmentDefault,

                // Collections
                structs = CoreStructCollections.Default,
                requiredFields = CoreRequiredFields.LitMinimal,
                fieldDependencies = CoreFieldDependencies.Default,
                renderStates = CoreRenderStates.ForwardColorMask,
                pragmas = CorePragmas.DotsInstancedInV1AndV2,
                defines = CoreDefines.Forward,
                keywords = CoreKeywords.Forward,
                includes = LitIncludes.Forward,
            };

            public static PassDescriptor TransparentDepthPostpass = new PassDescriptor()
            {
                // Definition
                displayName = "TransparentDepthPostpass",
                referenceName = "SHADERPASS_DEPTH_ONLY",
                lightMode = "TransparentDepthPostpass",
                useInPreview = true,

                // Template
                passTemplatePath = passTemplatePath,
                sharedTemplateDirectory = HDTarget.sharedTemplateDirectory,

                // Port Mask
                vertexBlocks = CoreBlockMasks.Vertex,
                pixelBlocks = LitPortMasks.FragmentTransparentDepthPostpass,

                // Collections
                structs = CoreStructCollections.Default,
                fieldDependencies = CoreFieldDependencies.Default,
                renderStates = CoreRenderStates.TransparentDepthPrePostPass,
                pragmas = CorePragmas.DotsInstancedInV1AndV2,
                defines = CoreDefines.ShaderGraphRaytracingHigh,
                keywords = CoreKeywords.HDBase,
                includes = LitIncludes.DepthOnly,
            };

            public static PassDescriptor RayTracingPrepass = new PassDescriptor()
            {
                // Definition
                displayName = "RayTracingPrepass",
                referenceName = "SHADERPASS_CONSTANT",
                lightMode = "RayTracingPrepass",
                useInPreview = false,

                // Template
                passTemplatePath = passTemplatePath,
                sharedTemplateDirectory = HDTarget.sharedTemplateDirectory,

                // Port Mask
                vertexBlocks = CoreBlockMasks.Vertex,
                pixelBlocks = LitPortMasks.FragmentRayTracingPrepass,

                // Collections
                structs = CoreStructCollections.Default,
                fieldDependencies = CoreFieldDependencies.Default,
                renderStates = LitRenderStates.RayTracingPrepass,
                pragmas = LitPragmas.RaytracingBasic,
                defines = CoreDefines.ShaderGraphRaytracingHigh,
                keywords = CoreKeywords.HDBase,
                includes = LitIncludes.RayTracingPrepass,
            };

            public static PassDescriptor RaytracingIndirect = new PassDescriptor()
            {
                // Definition
                displayName = "IndirectDXR",
                referenceName = "SHADERPASS_RAYTRACING_INDIRECT",
                lightMode = "IndirectDXR",
                useInPreview = false,

                // Template
                passTemplatePath = passTemplatePath,
                sharedTemplateDirectory = HDTarget.sharedTemplateDirectory,

                // Port Mask
                vertexBlocks = CoreBlockMasks.Vertex,
                pixelBlocks = LitPortMasks.FragmentDefault,

                // Collections
                structs = CoreStructCollections.Default,
                fieldDependencies = CoreFieldDependencies.Default,
                pragmas = CorePragmas.RaytracingBasic,
                defines = LitDefines.RaytracingForwardIndirect,
                keywords = CoreKeywords.RaytracingIndirect,
                includes = CoreIncludes.Raytracing,
                requiredFields = new FieldCollection(){ HDFields.SubShader.Lit, HDFields.ShaderPass.RaytracingIndirect },
            };

            public static PassDescriptor RaytracingVisibility = new PassDescriptor()
            {
                // Definition
                displayName = "VisibilityDXR",
                referenceName = "SHADERPASS_RAYTRACING_VISIBILITY",
                lightMode = "VisibilityDXR",
                useInPreview = false,

                // Template
                passTemplatePath = passTemplatePath,
                sharedTemplateDirectory = HDTarget.sharedTemplateDirectory,

                // Port Mask
                vertexBlocks = CoreBlockMasks.Vertex,
                pixelBlocks = LitPortMasks.FragmentDefault,

                // Collections
                structs = CoreStructCollections.Default,
                fieldDependencies = CoreFieldDependencies.Default,
                pragmas = CorePragmas.RaytracingBasic,
                defines = LitDefines.RaytracingVisibility,
                includes = CoreIncludes.Raytracing,
                requiredFields = new FieldCollection(){ HDFields.SubShader.Lit, HDFields.ShaderPass.RaytracingVisibility },
            };

            public static PassDescriptor RaytracingForward = new PassDescriptor()
            {
                // Definition
                displayName = "ForwardDXR",
                referenceName = "SHADERPASS_RAYTRACING_FORWARD",
                lightMode = "ForwardDXR",
                useInPreview = false,

                // Template
                passTemplatePath = passTemplatePath,
                sharedTemplateDirectory = HDTarget.sharedTemplateDirectory,

                // Port Mask
                vertexBlocks = CoreBlockMasks.Vertex,
                pixelBlocks = LitPortMasks.FragmentDefault,

                // Collections
                structs = CoreStructCollections.Default,
                fieldDependencies = CoreFieldDependencies.Default,
                pragmas = CorePragmas.RaytracingBasic,
                defines = LitDefines.RaytracingForwardIndirect,
                keywords = CoreKeywords.RaytracingGBufferForward,
                includes = CoreIncludes.Raytracing,
                requiredFields = new FieldCollection(){ HDFields.SubShader.Lit, HDFields.ShaderPass.RaytracingForward },
            };

            public static PassDescriptor RaytracingGBuffer = new PassDescriptor()
            {
                // Definition
                displayName = "GBufferDXR",
                referenceName = "SHADERPASS_RAYTRACING_GBUFFER",
                lightMode = "GBufferDXR",
                useInPreview = false,

                // Template
                passTemplatePath = passTemplatePath,
                sharedTemplateDirectory = HDTarget.sharedTemplateDirectory,

                // Port Mask
                vertexBlocks = CoreBlockMasks.Vertex,
                pixelBlocks = LitPortMasks.FragmentDefault,

                // Collections
                structs = CoreStructCollections.Default,
                fieldDependencies = CoreFieldDependencies.Default,
                pragmas = CorePragmas.RaytracingBasic,
                defines = LitDefines.RaytracingGBuffer,
                keywords = CoreKeywords.RaytracingGBufferForward,
                includes = CoreIncludes.Raytracing,
                requiredFields = new FieldCollection(){ HDFields.SubShader.Lit, HDFields.ShaderPass.RayTracingGBuffer },
            };

            public static PassDescriptor RaytracingPathTracing = new PassDescriptor()
            {
                //Definition
                displayName = "PathTracingDXR",
                referenceName = "SHADERPASS_PATH_TRACING",
                lightMode = "PathTracingDXR",
                useInPreview = false,

                // Template
                passTemplatePath = passTemplatePath,
                sharedTemplateDirectory = HDTarget.sharedTemplateDirectory,

                //Port mask
                vertexBlocks = CoreBlockMasks.Vertex,
                pixelBlocks = LitPortMasks.FragmentDefault,

                //Collections
                structs = CoreStructCollections.Default,
                fieldDependencies = CoreFieldDependencies.Default,
                pragmas = CorePragmas.RaytracingBasic,
                defines = LitDefines.RaytracingPathTracing,
                includes = CoreIncludes.Raytracing,
                requiredFields = new FieldCollection(){ HDFields.SubShader.Lit, HDFields.ShaderPass.RaytracingPathTracing },
            };

            public static PassDescriptor RaytracingSubSurface = new PassDescriptor()
            {
                //Definition
                displayName = "SubSurfaceDXR",
                referenceName = "SHADERPASS_RAYTRACING_SUB_SURFACE",
                lightMode = "SubSurfaceDXR",
                useInPreview = false,

                // Template
                passTemplatePath = passTemplatePath,
                sharedTemplateDirectory = HDTarget.sharedTemplateDirectory,

                //Port mask
                vertexBlocks = CoreBlockMasks.Vertex,
                pixelBlocks = LitPortMasks.FragmentDefault,

                //Collections
                structs = CoreStructCollections.Default,
                fieldDependencies = CoreFieldDependencies.Default,
                pragmas = CorePragmas.RaytracingBasic,
                defines = LitDefines.RaytracingGBuffer,
                keywords = CoreKeywords.RaytracingGBufferForward,
                includes = CoreIncludes.Raytracing,
                requiredFields = new FieldCollection(){ HDFields.SubShader.Lit, HDFields.ShaderPass.RaytracingSubSurface },
            };
        }
#endregion

#region PortMasks
        static class LitPortMasks
        {
            public static BlockFieldDescriptor[] FragmentDefault = new BlockFieldDescriptor[]
            {
                BlockFields.SurfaceDescription.BaseColor,
                BlockFields.SurfaceDescription.NormalTS,
                BlockFields.SurfaceDescription.NormalWS,
                BlockFields.SurfaceDescription.NormalOS,
                HDBlockFields.SurfaceDescription.BentNormal,
                HDBlockFields.SurfaceDescription.Tangent,
                HDBlockFields.SurfaceDescription.SubsurfaceMask,
                HDBlockFields.SurfaceDescription.Thickness,
                HDBlockFields.SurfaceDescription.DiffusionProfileHash,
                HDBlockFields.SurfaceDescription.IridescenceMask,
                HDBlockFields.SurfaceDescription.IridescenceThickness,
                BlockFields.SurfaceDescription.Specular,
                HDBlockFields.SurfaceDescription.CoatMask,
                BlockFields.SurfaceDescription.Metallic,
                BlockFields.SurfaceDescription.Emission,
                BlockFields.SurfaceDescription.Smoothness,
                BlockFields.SurfaceDescription.Occlusion,
                HDBlockFields.SurfaceDescription.SpecularOcclusion,
                BlockFields.SurfaceDescription.Alpha,
                BlockFields.SurfaceDescription.AlphaClipThreshold,
                HDBlockFields.SurfaceDescription.Anisotropy,
                HDBlockFields.SurfaceDescription.SpecularAAScreenSpaceVariance,
                HDBlockFields.SurfaceDescription.SpecularAAThreshold,
                HDBlockFields.SurfaceDescription.RefractionIndex,
                HDBlockFields.SurfaceDescription.RefractionColor,
                HDBlockFields.SurfaceDescription.RefractionDistance,
                HDBlockFields.SurfaceDescription.BakedGI,
                HDBlockFields.SurfaceDescription.BakedBackGI,
                HDBlockFields.SurfaceDescription.DepthOffset,
            };

            public static BlockFieldDescriptor[] FragmentMeta = new BlockFieldDescriptor[]
            {
                BlockFields.SurfaceDescription.BaseColor,
                BlockFields.SurfaceDescription.NormalTS,
                BlockFields.SurfaceDescription.NormalWS,
                BlockFields.SurfaceDescription.NormalOS,
                HDBlockFields.SurfaceDescription.BentNormal,
                HDBlockFields.SurfaceDescription.Tangent,
                HDBlockFields.SurfaceDescription.SubsurfaceMask,
                HDBlockFields.SurfaceDescription.Thickness,
                HDBlockFields.SurfaceDescription.DiffusionProfileHash,
                HDBlockFields.SurfaceDescription.IridescenceMask,
                HDBlockFields.SurfaceDescription.IridescenceThickness,
                BlockFields.SurfaceDescription.Specular,
                HDBlockFields.SurfaceDescription.CoatMask,
                BlockFields.SurfaceDescription.Metallic,
                BlockFields.SurfaceDescription.Emission,
                BlockFields.SurfaceDescription.Smoothness,
                BlockFields.SurfaceDescription.Occlusion,
                HDBlockFields.SurfaceDescription.SpecularOcclusion,
                BlockFields.SurfaceDescription.Alpha,
                BlockFields.SurfaceDescription.AlphaClipThreshold,
                HDBlockFields.SurfaceDescription.Anisotropy,
                HDBlockFields.SurfaceDescription.SpecularAAScreenSpaceVariance,
                HDBlockFields.SurfaceDescription.SpecularAAThreshold,
                HDBlockFields.SurfaceDescription.RefractionIndex,
                HDBlockFields.SurfaceDescription.RefractionColor,
                HDBlockFields.SurfaceDescription.RefractionDistance,
            };

            public static BlockFieldDescriptor[] FragmentShadowCaster = new BlockFieldDescriptor[]
            {
                BlockFields.SurfaceDescription.Alpha,
                BlockFields.SurfaceDescription.AlphaClipThreshold,
                HDBlockFields.SurfaceDescription.AlphaClipThresholdShadow,
                HDBlockFields.SurfaceDescription.DepthOffset,
            };

            public static BlockFieldDescriptor[] FragmentSceneSelection = new BlockFieldDescriptor[]
            {
                BlockFields.SurfaceDescription.Alpha,
                BlockFields.SurfaceDescription.AlphaClipThreshold,
                HDBlockFields.SurfaceDescription.DepthOffset,
            };

            public static BlockFieldDescriptor[] FragmentDepthMotionVectors = new BlockFieldDescriptor[]
            {
                BlockFields.SurfaceDescription.NormalTS,
                BlockFields.SurfaceDescription.NormalWS,
                BlockFields.SurfaceDescription.NormalOS,
                BlockFields.SurfaceDescription.Smoothness,
                BlockFields.SurfaceDescription.Alpha,
                BlockFields.SurfaceDescription.AlphaClipThreshold,
                HDBlockFields.SurfaceDescription.DepthOffset,
            };

            public static BlockFieldDescriptor[] FragmentDistortion = new BlockFieldDescriptor[]
            {
                BlockFields.SurfaceDescription.Alpha,
                BlockFields.SurfaceDescription.AlphaClipThreshold,
                HDBlockFields.SurfaceDescription.Distortion,
                HDBlockFields.SurfaceDescription.DistortionBlur,
            };

            public static BlockFieldDescriptor[] FragmentTransparentDepthPrepass = new BlockFieldDescriptor[]
            {
                BlockFields.SurfaceDescription.Alpha,
                HDBlockFields.SurfaceDescription.AlphaClipThresholdDepthPrepass,
                HDBlockFields.SurfaceDescription.DepthOffset,
                BlockFields.SurfaceDescription.NormalTS,
                BlockFields.SurfaceDescription.NormalWS,
                BlockFields.SurfaceDescription.NormalOS,
                BlockFields.SurfaceDescription.Smoothness,
            };

            public static BlockFieldDescriptor[] FragmentTransparentBackface = new BlockFieldDescriptor[]
            {
                BlockFields.SurfaceDescription.BaseColor,
                BlockFields.SurfaceDescription.NormalTS,
                BlockFields.SurfaceDescription.NormalWS,
                BlockFields.SurfaceDescription.NormalOS,
                HDBlockFields.SurfaceDescription.BentNormal,
                HDBlockFields.SurfaceDescription.Tangent,
                HDBlockFields.SurfaceDescription.SubsurfaceMask,
                HDBlockFields.SurfaceDescription.Thickness,
                HDBlockFields.SurfaceDescription.DiffusionProfileHash,
                HDBlockFields.SurfaceDescription.IridescenceMask,
                HDBlockFields.SurfaceDescription.IridescenceThickness,
                BlockFields.SurfaceDescription.Specular,
                HDBlockFields.SurfaceDescription.CoatMask,
                BlockFields.SurfaceDescription.Metallic,
                BlockFields.SurfaceDescription.Emission,
                BlockFields.SurfaceDescription.Smoothness,
                BlockFields.SurfaceDescription.Occlusion,
                HDBlockFields.SurfaceDescription.SpecularOcclusion,
                BlockFields.SurfaceDescription.Alpha,
                BlockFields.SurfaceDescription.AlphaClipThreshold,
                HDBlockFields.SurfaceDescription.Anisotropy,
                HDBlockFields.SurfaceDescription.SpecularAAScreenSpaceVariance,
                HDBlockFields.SurfaceDescription.SpecularAAThreshold,
                HDBlockFields.SurfaceDescription.RefractionIndex,
                HDBlockFields.SurfaceDescription.RefractionColor,
                HDBlockFields.SurfaceDescription.RefractionDistance,
                HDBlockFields.SurfaceDescription.DepthOffset,
            };

            public static BlockFieldDescriptor[] FragmentTransparentDepthPostpass = new BlockFieldDescriptor[]
            {
                BlockFields.SurfaceDescription.Alpha,
                HDBlockFields.SurfaceDescription.AlphaClipThresholdDepthPostpass,
                HDBlockFields.SurfaceDescription.DepthOffset,
            };

            public static BlockFieldDescriptor[] FragmentRayTracingPrepass = new BlockFieldDescriptor[]
            {
                BlockFields.SurfaceDescription.Alpha,
                BlockFields.SurfaceDescription.AlphaClipThreshold,
                HDBlockFields.SurfaceDescription.DepthOffset,
            };
        }
#endregion

#region RenderStates
        static class LitRenderStates
        {
            public static RenderStateCollection GBuffer = new RenderStateCollection
            {
                { RenderState.Cull(CoreRenderStates.Uniforms.cullMode) },
                { RenderState.ZTest(CoreRenderStates.Uniforms.zTestGBuffer) },
                { RenderState.Stencil(new StencilDescriptor()
                {
                    WriteMask = CoreRenderStates.Uniforms.stencilWriteMaskGBuffer,
                    Ref = CoreRenderStates.Uniforms.stencilRefGBuffer,
                    Comp = "Always",
                    Pass = "Replace",
                }) },
            };

            public static RenderStateCollection Distortion = new RenderStateCollection
            {
                { RenderState.Blend(Blend.One, Blend.One, Blend.One, Blend.One), new FieldCondition(HDFields.DistortionAdd, true) },
                { RenderState.Blend(Blend.DstColor, Blend.Zero, Blend.DstAlpha, Blend.Zero), new FieldCondition(HDFields.DistortionMultiply, true) },
                { RenderState.Blend(Blend.One, Blend.Zero, Blend.One, Blend.Zero), new FieldCondition(HDFields.DistortionReplace, true) },
                { RenderState.BlendOp(UnityEditor.ShaderGraph.BlendOp.Add, UnityEditor.ShaderGraph.BlendOp.Add) },
                { RenderState.ZWrite(ZWrite.Off) },
                { RenderState.ZTest(ZTest.Always), new FieldCondition(HDFields.DistortionDepthTest, false) },
                { RenderState.ZTest(ZTest.LEqual), new FieldCondition(HDFields.DistortionDepthTest, true) },
                { RenderState.Stencil(new StencilDescriptor()
                {
                    WriteMask = CoreRenderStates.Uniforms.stencilWriteMaskDistortionVec,
                    Ref = CoreRenderStates.Uniforms.stencilRefDistortionVec,
                    Comp = "Always",
                    Pass = "Replace",
                }) },
            };

            public static RenderStateCollection TransparentDepthPrePostPass = new RenderStateCollection
            {
                { RenderState.Blend(Blend.One, Blend.Zero) },
                { RenderState.Cull(CoreRenderStates.Uniforms.cullMode) },
                { RenderState.ZWrite(ZWrite.On) },
                { RenderState.Stencil(new StencilDescriptor()
                {
                    WriteMask = CoreRenderStates.Uniforms.stencilWriteMaskDepth,
                    Ref = CoreRenderStates.Uniforms.stencilRefDepth,
                    Comp = "Always",
                    Pass = "Replace",
                }) },
            };

            public static RenderStateCollection RayTracingPrepass = new RenderStateCollection
            {
                { RenderState.Blend(Blend.One, Blend.Zero) },
                { RenderState.Cull(CoreRenderStates.Uniforms.cullMode) },
                { RenderState.ZWrite(ZWrite.On) },
                // Note: we use default ZTest LEqual so if the object have already been render in depth prepass, it will re-render to tag stencil
            };
        }
#endregion

#region Pragmas
        static class LitPragmas
        {
            public static PragmaCollection RaytracingBasic = new PragmaCollection
            {
                { Pragma.Target(ShaderModel.Target45) },
                { Pragma.Vertex("Vert") },
                { Pragma.Fragment("Frag") },
                { Pragma.OnlyRenderers(new Platform[] {Platform.D3D11}) },
            };
        }
#endregion

#region Defines
        static class LitDefines
        {
            public static DefineCollection RaytracingForwardIndirect = new DefineCollection
            {
                { CoreKeywordDescriptors.Shadow, 0 },
                { RayTracingNode.GetRayTracingKeyword(), 1 },
                { CoreKeywordDescriptors.HasLightloop, 1 },
            };

            public static DefineCollection RaytracingGBuffer = new DefineCollection
            {
                { CoreKeywordDescriptors.Shadow, 0 },
                { RayTracingNode.GetRayTracingKeyword(), 1 },
            };

            public static DefineCollection RaytracingVisibility = new DefineCollection
            {
                { RayTracingNode.GetRayTracingKeyword(), 1 },
            };

            public static DefineCollection RaytracingPathTracing = new DefineCollection
            {
                { CoreKeywordDescriptors.Shadow, 0 },
                { RayTracingNode.GetRayTracingKeyword(), 0 },
            };
        }
#endregion

#region Keywords
        static class LitKeywords
        {
            public static KeywordCollection GBuffer = new KeywordCollection
            {
                { CoreKeywords.HDBase },
                { CoreKeywordDescriptors.DebugDisplay },
                { CoreKeywords.Lightmaps },
                { CoreKeywordDescriptors.ShadowsShadowmask },
                { CoreKeywordDescriptors.LightLayers },
                { CoreKeywordDescriptors.Decals },
            };

            public static KeywordCollection DepthMotionVectors = new KeywordCollection
            {
                { CoreKeywords.HDBase },
                { CoreKeywordDescriptors.WriteMsaaDepth },
                { CoreKeywordDescriptors.WriteNormalBuffer },
            };
        }
#endregion

#region Includes
        static class LitIncludes
        {
            const string kLitDecalData = "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Lit/LitDecalData.hlsl";
            const string kPassGBuffer = "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassGBuffer.hlsl";
            const string kPassConstant = "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassConstant.hlsl";
            
            public static IncludeCollection Common = new IncludeCollection
            {
                { CoreIncludes.CorePregraph },
                { CoreIncludes.kNormalSurfaceGradient, IncludeLocation.Pregraph },
                { CoreIncludes.kLit, IncludeLocation.Pregraph },
                { CoreIncludes.CoreUtility },
                { CoreIncludes.kDecalUtilities, IncludeLocation.Pregraph },
                { kLitDecalData, IncludeLocation.Pregraph },
                { CoreIncludes.kShaderGraphFunctions, IncludeLocation.Pregraph },
            };

            public static IncludeCollection GBuffer = new IncludeCollection
            {
                { Common },
                { kPassGBuffer, IncludeLocation.Postgraph },
            };

            public static IncludeCollection Meta = new IncludeCollection
            {
                { Common },
                { CoreIncludes.kPassLightTransport, IncludeLocation.Postgraph },
            };

            public static IncludeCollection DepthOnly = new IncludeCollection
            {
                { Common },
                { CoreIncludes.kPassDepthOnly, IncludeLocation.Postgraph },
            };

            public static IncludeCollection RayTracingPrepass = new IncludeCollection
            {
                { Common },
                { kPassConstant, IncludeLocation.Postgraph },
            };

            public static IncludeCollection MotionVectors = new IncludeCollection
            {
                { Common },
                { CoreIncludes.kPassMotionVectors, IncludeLocation.Postgraph },
            };

            public static IncludeCollection Forward = new IncludeCollection
            {
                { CoreIncludes.CorePregraph },
                { CoreIncludes.kNormalSurfaceGradient, IncludeLocation.Pregraph },
                { CoreIncludes.kLighting, IncludeLocation.Pregraph },
                { CoreIncludes.kLightLoopDef, IncludeLocation.Pregraph },
                { CoreIncludes.kLit, IncludeLocation.Pregraph },
                { CoreIncludes.kLightLoop, IncludeLocation.Pregraph },
                { CoreIncludes.CoreUtility },
                { CoreIncludes.kDecalUtilities, IncludeLocation.Pregraph },
                { kLitDecalData, IncludeLocation.Pregraph },
                { CoreIncludes.kShaderGraphFunctions, IncludeLocation.Pregraph },
                { CoreIncludes.kPassForward, IncludeLocation.Postgraph },
            };

            public static IncludeCollection Distortion = new IncludeCollection
            {
                { Common },
                { CoreIncludes.kDisortionVectors, IncludeLocation.Postgraph },
            };
        }
#endregion
    }
}
