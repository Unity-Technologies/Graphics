using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using UnityEditor.ShaderGraph;
using UnityEditor.ShaderGraph.Internal;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Legacy;
using UnityEditor.Rendering.HighDefinition.ShaderGraph.Legacy;
using static UnityEngine.Rendering.HighDefinition.HDMaterialProperties;
using static UnityEditor.Rendering.HighDefinition.HDShaderUtils;

namespace UnityEditor.Rendering.HighDefinition.ShaderGraph
{
    sealed class EyeSubTarget : LightingSubTarget, ILegacyTarget, IRequiresData<EyeData>
    {
        static string passTemplatePath => $"{HDUtils.GetHDRenderPipelinePath()}Editor/Material/Eye/ShaderGraph/EyePass.template";
        protected override string customInspector => "Rendering.HighDefinition.EyeGUI";
        protected override string subTargetAssetGuid => "864e4e09d6293cf4d98457f740bb3301";
        protected override ShaderID shaderID => HDShaderUtils.ShaderID.SG_Eye;

        public EyeSubTarget() => displayName = "Eye";

        EyeData m_EyeData;

        EyeData IRequiresData<EyeData>.data
        {
            get => m_EyeData;
            set => m_EyeData = value;
        }

        public EyeData eyeData
        {
            get => m_EyeData;
            set => m_EyeData = value;
        }

        protected override IEnumerable<SubShaderDescriptor> EnumerateSubShaders()
        {
            yield return SubShaders.Eye; 
        }

        public override void GetFields(ref TargetFieldContext context)
        {
            base.GetFields(ref context);

            // Structs
            context.AddField(HDStructFields.FragInputs.IsFrontFace,         systemData.doubleSidedMode != DoubleSidedMode.Disabled && !context.pass.Equals(EyeSubTarget.EyePasses.MotionVectors));

            // Material
            context.AddField(HDFields.Eye,                                  eyeData.materialType == EyeData.MaterialType.Eye);
            context.AddField(HDFields.EyeCinematic,                         eyeData.materialType == EyeData.MaterialType.EyeCinematic);
            context.AddField(HDFields.SubsurfaceScattering,                 lightingData.subsurfaceScattering && systemData.surfaceType != SurfaceType.Transparent);

            AddSpecularOcclusionFields(ref context);

            // Misc
            AddLitMiscFields(ref context);
            AddSurfaceMiscFields(ref context);
            context.AddField(HDFields.DoAlphaTest,                          systemData.alphaTest && context.pass.validPixelBlocks.Contains(BlockFields.SurfaceDescription.AlphaClipThreshold));
        }

        public override void GetActiveBlocks(ref TargetActiveBlockContext context)
        {
            // Vertex
            context.AddBlock(BlockFields.VertexDescription.Position);
            context.AddBlock(BlockFields.VertexDescription.Normal);
            context.AddBlock(BlockFields.VertexDescription.Tangent);

            // Eye
            context.AddBlock(BlockFields.SurfaceDescription.BaseColor);
            context.AddBlock(HDBlockFields.SurfaceDescription.SpecularOcclusion,        lightingData.specularOcclusionMode == SpecularOcclusionMode.Custom);
            context.AddBlock(BlockFields.SurfaceDescription.NormalTS);
            context.AddBlock(HDBlockFields.SurfaceDescription.IrisNormal);
            context.AddBlock(HDBlockFields.SurfaceDescription.BentNormal);
            context.AddBlock(BlockFields.SurfaceDescription.Smoothness);
            context.AddBlock(HDBlockFields.SurfaceDescription.IOR);
            context.AddBlock(BlockFields.SurfaceDescription.Occlusion);
            context.AddBlock(HDBlockFields.SurfaceDescription.Mask);
            context.AddBlock(HDBlockFields.SurfaceDescription.DiffusionProfileHash,     lightingData.subsurfaceScattering);
            context.AddBlock(HDBlockFields.SurfaceDescription.SubsurfaceMask,           lightingData.subsurfaceScattering);
            context.AddBlock(BlockFields.SurfaceDescription.Emission);
            context.AddBlock(BlockFields.SurfaceDescription.Alpha);
            context.AddBlock(BlockFields.SurfaceDescription.AlphaClipThreshold,         systemData.alphaTest);
            context.AddBlock(HDBlockFields.SurfaceDescription.BakedGI,                  lightingData.overrideBakedGI);
            context.AddBlock(HDBlockFields.SurfaceDescription.BakedBackGI,              lightingData.overrideBakedGI);
            context.AddBlock(HDBlockFields.SurfaceDescription.DepthOffset,              builtinData.depthOffset);
        }

        public override void GetPropertiesGUI(ref TargetPropertyGUIContext context, Action onChange, Action<String> registerUndo)
        {
            // TODO: refactor this
            var settingsView = new EyeSettingsView(this);
            settingsView.GetPropertiesGUI(ref context, onChange, registerUndo);
        }

        public override void CollectShaderProperties(PropertyCollector collector, GenerationMode generationMode)
        {
            // Trunk currently relies on checking material property "_EmissionColor" to allow emissive GI. If it doesn't find that property, or it is black, GI is forced off.
            // ShaderGraph doesn't use this property, so currently it inserts a dummy color (white). This dummy color may be removed entirely once the following PR has been merged in trunk: Pull request #74105
            // The user will then need to explicitly disable emissive GI if it is not needed.
            // To be able to automatically disable emission based on the ShaderGraph config when emission is black,
            // we will need a more general way to communicate this to the engine (not directly tied to a material property).
            collector.AddShaderProperty(new ColorShaderProperty()
            {
                overrideReferenceName = "_EmissionColor",
                hidden = true,
                value = new Color(1.0f, 1.0f, 1.0f, 1.0f)
            });

            //See SG-ADDITIONALVELOCITY-NOTE
            if (builtinData.addPrecomputedVelocity)
            {
                collector.AddShaderProperty(new BooleanShaderProperty
                {
                    value = true,
                    hidden = true,
                    overrideReferenceName = kAddPrecomputedVelocity,
                });
            }

            // Add all shader properties required by the inspector
            HDSubShaderUtilities.AddStencilShaderProperties(collector, lightingData.subsurfaceScattering, lightingData.receiveSSR, lightingData.receiveSSR, false);
            HDSubShaderUtilities.AddBlendingStatesShaderProperties(
                collector,
                systemData.surfaceType,
                systemData.blendMode,
                systemData.sortPriority,
                builtinData.alphaToMask,
                systemData.zWrite,
                systemData.transparentCullMode,
                systemData.zTest,
                false,
                builtinData.transparencyFog
            );
            HDSubShaderUtilities.AddAlphaCutoffShaderProperties(collector, systemData.alphaTest, false);
            HDSubShaderUtilities.AddDoubleSidedProperty(collector, systemData.doubleSidedMode);
        }

        public override void ProcessPreviewMaterial(Material material)
        {
            // Fixup the material settings:
            material.SetFloat(kSurfaceType, (int)systemData.surfaceType);
            material.SetFloat(kDoubleSidedNormalMode, (int)systemData.doubleSidedMode);
            material.SetFloat(kDoubleSidedEnable, systemData.doubleSidedMode != DoubleSidedMode.Disabled ? 1.0f : 0.0f);
            material.SetFloat(kAlphaCutoffEnabled, systemData.alphaTest ? 1 : 0);
            material.SetFloat(kBlendMode, (int)systemData.blendMode);
            material.SetFloat(kEnableFogOnTransparent, builtinData.transparencyFog ? 1.0f : 0.0f);
            material.SetFloat(kZTestTransparent, (int)systemData.zTest);
            material.SetFloat(kTransparentCullMode, (int)systemData.transparentCullMode);
            material.SetFloat(kZWrite, systemData.zWrite ? 1.0f : 0.0f);

            // No sorting priority for shader graph preview
            var renderingPass = systemData.surfaceType == SurfaceType.Opaque ? HDRenderQueue.RenderQueueType.Opaque : HDRenderQueue.RenderQueueType.Transparent;
            material.renderQueue = (int)HDRenderQueue.ChangeType(renderingPass, offset: 0, alphaTest: systemData.alphaTest);

            EyeGUI.SetupMaterialKeywordsAndPass(material);
        }

        public bool TryUpgradeFromMasterNode(IMasterNode1 masterNode, out Dictionary<BlockFieldDescriptor, int> blockMap)
        {
            blockMap = null;
            if(!(masterNode is EyeMasterNode1 eyeMasterNode))
                return false;

            // Set data
            systemData.surfaceType = (SurfaceType)eyeMasterNode.m_SurfaceType;
            systemData.blendMode = HDSubShaderUtilities.UpgradeLegacyAlphaModeToBlendMode((int)eyeMasterNode.m_AlphaMode);
            systemData.alphaTest = eyeMasterNode.m_AlphaTest;
            systemData.alphaTestDepthPrepass = eyeMasterNode.m_AlphaTestDepthPrepass;
            systemData.alphaTestDepthPostpass = eyeMasterNode.m_AlphaTestDepthPostpass;
            systemData.sortPriority = eyeMasterNode.m_SortPriority;
            systemData.doubleSidedMode = eyeMasterNode.m_DoubleSidedMode;
            systemData.zWrite = eyeMasterNode.m_ZWrite;
            systemData.transparentCullMode = eyeMasterNode.m_transparentCullMode;
            systemData.zTest = eyeMasterNode.m_ZTest;
            systemData.supportLodCrossFade = eyeMasterNode.m_SupportLodCrossFade;
            systemData.dotsInstancing = eyeMasterNode.m_DOTSInstancing;
            systemData.materialNeedsUpdateHash = eyeMasterNode.m_MaterialNeedsUpdateHash;

            builtinData.transparencyFog = eyeMasterNode.m_TransparencyFog;
            builtinData.addPrecomputedVelocity = eyeMasterNode.m_AddPrecomputedVelocity;
            builtinData.depthOffset = eyeMasterNode.m_depthOffset;
            builtinData.alphaToMask = eyeMasterNode.m_AlphaToMask;

            lightingData.blendPreserveSpecular = eyeMasterNode.m_BlendPreserveSpecular;
            lightingData.receiveDecals = eyeMasterNode.m_ReceiveDecals;
            lightingData.receiveSSR = eyeMasterNode.m_ReceivesSSR;
            lightingData.specularOcclusionMode = eyeMasterNode.m_SpecularOcclusionMode;
            lightingData.overrideBakedGI = eyeMasterNode.m_overrideBakedGI;
            lightingData.subsurfaceScattering = eyeMasterNode.m_SubsurfaceScattering;
            
            eyeData.materialType = (EyeData.MaterialType)eyeMasterNode.m_MaterialType;
            target.customEditorGUI = eyeMasterNode.m_OverrideEnabled ? eyeMasterNode.m_ShaderGUIOverride : "";

            // Convert SlotMask to BlockMap entries
            var blockMapLookup = new Dictionary<EyeMasterNode1.SlotMask, BlockFieldDescriptor>()
            {
                { EyeMasterNode1.SlotMask.Position, BlockFields.VertexDescription.Position },
                { EyeMasterNode1.SlotMask.VertexNormal, BlockFields.VertexDescription.Normal },
                { EyeMasterNode1.SlotMask.VertexTangent, BlockFields.VertexDescription.Tangent },
                { EyeMasterNode1.SlotMask.Albedo, BlockFields.SurfaceDescription.BaseColor },
                { EyeMasterNode1.SlotMask.SpecularOcclusion, HDBlockFields.SurfaceDescription.SpecularOcclusion },
                { EyeMasterNode1.SlotMask.Normal, BlockFields.SurfaceDescription.NormalTS }, 
                { EyeMasterNode1.SlotMask.IrisNormal, HDBlockFields.SurfaceDescription.IrisNormal }, 
                { EyeMasterNode1.SlotMask.BentNormal, HDBlockFields.SurfaceDescription.BentNormal },
                { EyeMasterNode1.SlotMask.Smoothness, BlockFields.SurfaceDescription.Smoothness }, 
                { EyeMasterNode1.SlotMask.IOR, HDBlockFields.SurfaceDescription.IOR },
                { EyeMasterNode1.SlotMask.Occlusion, BlockFields.SurfaceDescription.Occlusion },
                { EyeMasterNode1.SlotMask.Mask, HDBlockFields.SurfaceDescription.Mask },
                { EyeMasterNode1.SlotMask.DiffusionProfile, HDBlockFields.SurfaceDescription.DiffusionProfileHash },
                { EyeMasterNode1.SlotMask.SubsurfaceMask, HDBlockFields.SurfaceDescription.SubsurfaceMask },
                { EyeMasterNode1.SlotMask.Emission, BlockFields.SurfaceDescription.Emission },
                { EyeMasterNode1.SlotMask.Alpha, BlockFields.SurfaceDescription.Alpha },
                { EyeMasterNode1.SlotMask.AlphaClipThreshold, BlockFields.SurfaceDescription.AlphaClipThreshold },
            };

            // Legacy master node slots have additional slot conditions, test them here
            bool AdditionalSlotMaskTests(EyeMasterNode1.SlotMask slotMask)
            {
                switch(slotMask)
                {
                    case EyeMasterNode1.SlotMask.SpecularOcclusion:
                        return lightingData.specularOcclusionMode == SpecularOcclusionMode.Custom;
                    case EyeMasterNode1.SlotMask.DiffusionProfile:
                        return lightingData.subsurfaceScattering;
                    case EyeMasterNode1.SlotMask.SubsurfaceMask:
                        return lightingData.subsurfaceScattering;
                    case EyeMasterNode1.SlotMask.AlphaClipThreshold:
                        return systemData.alphaTest;
                    default:
                        return true;
                }
            }

            // Set blockmap
            blockMap = new Dictionary<BlockFieldDescriptor, int>();
            foreach(EyeMasterNode1.SlotMask slotMask in Enum.GetValues(typeof(EyeMasterNode1.SlotMask)))
            {
                if(eyeMasterNode.MaterialTypeUsesSlotMask(slotMask))
                {
                    if(!blockMapLookup.TryGetValue(slotMask, out var blockFieldDescriptor))
                        continue;

                    if(!AdditionalSlotMaskTests(slotMask))
                        continue;
                    
                    var slotId = Mathf.Log((int)slotMask, 2);
                    blockMap.Add(blockFieldDescriptor, (int)slotId);
                }
            }

            // Override Baked GI
            if(lightingData.overrideBakedGI)
            {
                blockMap.Add(HDBlockFields.SurfaceDescription.BakedGI, EyeMasterNode1.LightingSlotId);
                blockMap.Add(HDBlockFields.SurfaceDescription.BakedBackGI, EyeMasterNode1.BackLightingSlotId);
            }

            // Depth Offset
            if(builtinData.depthOffset)
            {
                blockMap.Add(HDBlockFields.SurfaceDescription.DepthOffset, EyeMasterNode1.DepthOffsetSlotId);
            }

            return true;
        }

#region SubShaders
        static class SubShaders
        {
            public static SubShaderDescriptor Eye = new SubShaderDescriptor()
            {
                pipelineTag = HDRenderPipeline.k_ShaderTagName,
                generatesPreview = true,
                passes = new PassCollection
                {
                    { EyePasses.ShadowCaster },
                    { EyePasses.META },
                    { EyePasses.SceneSelection },
                    { EyePasses.DepthForwardOnly },
                    { EyePasses.MotionVectors },
                    { EyePasses.ForwardOnly },
                },
            };
        }
#endregion

#region Passes
        public static class EyePasses
        {
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
                validPixelBlocks = EyeBlockMasks.FragmentMETA,

                // Collections
                structs = CoreStructCollections.Default,
                requiredFields = CoreRequiredFields.Meta,
                fieldDependencies = CoreFieldDependencies.Default,
                renderStates = CoreRenderStates.Meta,
                pragmas = CorePragmas.DotsInstancedInV2Only,
                keywords = CoreKeywords.HDBase,
                includes = EyeIncludes.Meta,
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
                validVertexBlocks = CoreBlockMasks.Vertex,
                validPixelBlocks = EyeBlockMasks.FragmentAlphaDepth,

                // Collections
                structs = CoreStructCollections.Default,
                fieldDependencies = CoreFieldDependencies.Default,
                renderStates = CoreRenderStates.BlendShadowCaster,
                pragmas = CorePragmas.DotsInstancedInV2Only,
                keywords = CoreKeywords.HDBase,
                includes = EyeIncludes.DepthOnly,
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
                validVertexBlocks = CoreBlockMasks.Vertex,
                validPixelBlocks = EyeBlockMasks.FragmentAlphaDepth,

                // Collections
                structs = CoreStructCollections.Default,
                fieldDependencies = CoreFieldDependencies.Default,
                renderStates = CoreRenderStates.SceneSelection,
                pragmas = CorePragmas.DotsInstancedInV2OnlyEditorSync,
                defines = CoreDefines.SceneSelection,
                keywords = CoreKeywords.HDBase,
                includes = EyeIncludes.DepthOnly,
            };

            public static PassDescriptor DepthForwardOnly = new PassDescriptor()
            {
                // Definition
                displayName = "DepthForwardOnly",
                referenceName = "SHADERPASS_DEPTH_ONLY",
                lightMode = "DepthForwardOnly",
                useInPreview = true,

                // Template
                passTemplatePath = passTemplatePath,
                sharedTemplateDirectory = HDTarget.sharedTemplateDirectory,

                // Port Mask
                validVertexBlocks = CoreBlockMasks.Vertex,
                validPixelBlocks = EyeBlockMasks.FragmentDepthMotionVectors,

                // Collections
                structs = CoreStructCollections.Default,
                requiredFields = CoreRequiredFields.LitFull,
                fieldDependencies = CoreFieldDependencies.Default,
                renderStates = CoreRenderStates.DepthOnly,
                pragmas = CorePragmas.DotsInstancedInV2Only,
                defines = CoreDefines.DepthMotionVectors,
                keywords = CoreKeywords.DepthMotionVectorsNoNormal,
                includes = EyeIncludes.DepthOnly,
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
                validVertexBlocks = CoreBlockMasks.Vertex,
                validPixelBlocks = EyeBlockMasks.FragmentDepthMotionVectors,

                // Collections
                structs = CoreStructCollections.Default,
                requiredFields = CoreRequiredFields.LitFull,
                fieldDependencies = CoreFieldDependencies.Default,
                renderStates = CoreRenderStates.MotionVectors,
                pragmas = CorePragmas.DotsInstancedInV2Only,
                defines = CoreDefines.DepthMotionVectors,
                keywords = CoreKeywords.DepthMotionVectorsNoNormal,
                includes = EyeIncludes.MotionVectors,
            };

            public static PassDescriptor ForwardOnly = new PassDescriptor()
            {
                // Definition
                displayName = "ForwardOnly",
                referenceName = "SHADERPASS_FORWARD",
                lightMode = "ForwardOnly",
                useInPreview = true,

                // Template
                passTemplatePath = passTemplatePath,
                sharedTemplateDirectory = HDTarget.sharedTemplateDirectory,

                // Port Mask
                validVertexBlocks = CoreBlockMasks.Vertex,
                validPixelBlocks = EyeBlockMasks.FragmentForward,

                // Collections
                structs = CoreStructCollections.Default,
                requiredFields = CoreRequiredFields.LitFull,
                fieldDependencies = CoreFieldDependencies.Default,
                renderStates = CoreRenderStates.Forward,
                pragmas = CorePragmas.DotsInstancedInV2Only,
                defines = CoreDefines.Forward,
                keywords = CoreKeywords.Forward,
                includes = EyeIncludes.ForwardOnly,
            };
        }
#endregion

#region BlockMasks
        static class EyeBlockMasks
        {
            public static BlockFieldDescriptor[] FragmentMETA = new BlockFieldDescriptor[]
            {
                BlockFields.SurfaceDescription.BaseColor,
                HDBlockFields.SurfaceDescription.SpecularOcclusion,
                BlockFields.SurfaceDescription.NormalTS,
                HDBlockFields.SurfaceDescription.IrisNormal,
                BlockFields.SurfaceDescription.Smoothness,
                HDBlockFields.SurfaceDescription.IOR,
                BlockFields.SurfaceDescription.Occlusion,
                HDBlockFields.SurfaceDescription.Mask,
                HDBlockFields.SurfaceDescription.DiffusionProfileHash,
                HDBlockFields.SurfaceDescription.SubsurfaceMask,
                BlockFields.SurfaceDescription.Emission,
                BlockFields.SurfaceDescription.Alpha,
                BlockFields.SurfaceDescription.AlphaClipThreshold,
            };

            public static BlockFieldDescriptor[] FragmentAlphaDepth = new BlockFieldDescriptor[]
            {
                BlockFields.SurfaceDescription.Alpha,
                BlockFields.SurfaceDescription.AlphaClipThreshold,
                HDBlockFields.SurfaceDescription.DepthOffset,
            };

            public static BlockFieldDescriptor[] FragmentDepthMotionVectors = new BlockFieldDescriptor[]
            {
                BlockFields.SurfaceDescription.NormalTS,
                BlockFields.SurfaceDescription.Smoothness,
                BlockFields.SurfaceDescription.Alpha,
                BlockFields.SurfaceDescription.AlphaClipThreshold,
                HDBlockFields.SurfaceDescription.DepthOffset,
            };

            public static BlockFieldDescriptor[] FragmentForward = new BlockFieldDescriptor[]
            {
                BlockFields.SurfaceDescription.BaseColor,
                HDBlockFields.SurfaceDescription.SpecularOcclusion,
                BlockFields.SurfaceDescription.NormalTS,
                HDBlockFields.SurfaceDescription.IrisNormal,
                BlockFields.SurfaceDescription.Smoothness,
                HDBlockFields.SurfaceDescription.IOR,
                BlockFields.SurfaceDescription.Occlusion,
                HDBlockFields.SurfaceDescription.Mask,
                HDBlockFields.SurfaceDescription.DiffusionProfileHash,
                HDBlockFields.SurfaceDescription.SubsurfaceMask,
                BlockFields.SurfaceDescription.Emission,
                BlockFields.SurfaceDescription.Alpha,
                BlockFields.SurfaceDescription.AlphaClipThreshold,
                HDBlockFields.SurfaceDescription.BakedGI,
                HDBlockFields.SurfaceDescription.BakedBackGI,
                HDBlockFields.SurfaceDescription.DepthOffset,
            };
        }
#endregion

#region Includes
        static class EyeIncludes
        {
            const string kEye = "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Eye/Eye.hlsl";

            public static IncludeCollection Common = new IncludeCollection
            {
                { CoreIncludes.CorePregraph },
                { CoreIncludes.kNormalSurfaceGradient, IncludeLocation.Pregraph },
                { kEye, IncludeLocation.Pregraph },
                { CoreIncludes.CoreUtility },
                { CoreIncludes.kDecalUtilities, IncludeLocation.Pregraph },
                { CoreIncludes.kShaderGraphFunctions, IncludeLocation.Pregraph },
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

            public static IncludeCollection MotionVectors = new IncludeCollection
            {
                { Common },
                { CoreIncludes.kPassMotionVectors, IncludeLocation.Postgraph },
            };

            public static IncludeCollection ForwardOnly = new IncludeCollection
            {
                { CoreIncludes.CorePregraph },
                { CoreIncludes.kNormalSurfaceGradient, IncludeLocation.Pregraph },
                { CoreIncludes.kLighting, IncludeLocation.Pregraph },
                { CoreIncludes.kLightLoopDef, IncludeLocation.Pregraph },
                { kEye, IncludeLocation.Pregraph },
                { CoreIncludes.kLightLoop, IncludeLocation.Pregraph },
                { CoreIncludes.CoreUtility },
                { CoreIncludes.kDecalUtilities, IncludeLocation.Pregraph },
                { CoreIncludes.kShaderGraphFunctions, IncludeLocation.Pregraph },
                { CoreIncludes.kPassForward, IncludeLocation.Postgraph },
            };
        }
#endregion
    }
}
