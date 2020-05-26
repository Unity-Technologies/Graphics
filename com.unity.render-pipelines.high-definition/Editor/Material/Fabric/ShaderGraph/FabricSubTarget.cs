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
    sealed class FabricSubTarget : LightingSubTarget, ILegacyTarget, IRequiresData<FabricData>
    {
        static string passTemplatePath => $"{HDUtils.GetHDRenderPipelinePath()}Editor/Material/Fabric/ShaderGraph/FabricPass.template";

        public FabricSubTarget() => displayName = "Fabric";

        protected override string subTargetAssetGuid => "74f1a4749bab90d429ac01d094be0aeb"; // FabricSubTarget.cs
        protected override string customInspector => "Rendering.HighDefinition.FabricGUI";
        protected override ShaderID shaderID => HDShaderUtils.ShaderID.SG_Fabric;

        FabricData m_FabricData;

        FabricData IRequiresData<FabricData>.data
        {
            get => m_FabricData;
            set => m_FabricData = value;
        }

        public FabricData fabricData
        {
            get => m_FabricData;
            set => m_FabricData = value;
        }

        protected override IEnumerable<SubShaderDescriptor> EnumerateSubShaders()
        {
            yield return SubShaders.Fabric;
            yield return SubShaders.FabricRaytracing;
        }

        public override void GetFields(ref TargetFieldContext context)
        {
            base.GetFields(ref context);

            // Structs
            context.AddField(HDStructFields.FragInputs.IsFrontFace,         systemData.doubleSidedMode != DoubleSidedMode.Disabled && !context.pass.Equals(FabricSubTarget.FabricPasses.MotionVectors));

            // Material
            context.AddField(HDFields.CottonWool,                           fabricData.materialType == FabricData.MaterialType.CottonWool);
            context.AddField(HDFields.Silk,                                 fabricData.materialType == FabricData.MaterialType.Silk);
            context.AddField(HDFields.SubsurfaceScattering,                 lightingData.subsurfaceScattering && systemData.surfaceType != SurfaceType.Transparent);
            context.AddField(HDFields.Transmission,                         lightingData.transmission);

            // Specular Occlusion
            AddSpecularOcclusionFields(ref context);

            // Misc
            AddLitMiscFields(ref context);
            AddSurfaceMiscFields(ref context);
            context.AddField(Fields.AlphaTest,                              systemData.alphaTest && context.pass.validPixelBlocks.Contains(BlockFields.SurfaceDescription.AlphaClipThreshold));
            context.AddField(HDFields.DoAlphaTest,                          systemData.alphaTest && context.pass.validPixelBlocks.Contains(BlockFields.SurfaceDescription.AlphaClipThreshold));
            context.AddField(HDFields.EnergyConservingSpecular,             lightingData.energyConservingSpecular);
        }

        public override void GetActiveBlocks(ref TargetActiveBlockContext context)
        {
            // Vertex
            context.AddBlock(BlockFields.VertexDescription.Position);
            context.AddBlock(BlockFields.VertexDescription.Normal);
            context.AddBlock(BlockFields.VertexDescription.Tangent);

            // Fabric
            context.AddBlock(BlockFields.SurfaceDescription.BaseColor);
            context.AddBlock(HDBlockFields.SurfaceDescription.SpecularOcclusion,    lightingData.specularOcclusionMode == SpecularOcclusionMode.Custom);
            context.AddBlock(BlockFields.SurfaceDescription.NormalTS);
            context.AddBlock(HDBlockFields.SurfaceDescription.BentNormal);
            context.AddBlock(BlockFields.SurfaceDescription.Smoothness);
            context.AddBlock(BlockFields.SurfaceDescription.Occlusion);
            context.AddBlock(BlockFields.SurfaceDescription.Specular);
            context.AddBlock(HDBlockFields.SurfaceDescription.DiffusionProfileHash, lightingData.subsurfaceScattering || lightingData.transmission);
            context.AddBlock(HDBlockFields.SurfaceDescription.SubsurfaceMask,       lightingData.subsurfaceScattering);
            context.AddBlock(HDBlockFields.SurfaceDescription.Thickness,            lightingData.transmission);
            context.AddBlock(BlockFields.SurfaceDescription.Emission);
            context.AddBlock(BlockFields.SurfaceDescription.Alpha);
            context.AddBlock(BlockFields.SurfaceDescription.AlphaClipThreshold,     systemData.alphaTest);
            context.AddBlock(HDBlockFields.SurfaceDescription.BakedGI,              lightingData.overrideBakedGI);
            context.AddBlock(HDBlockFields.SurfaceDescription.BakedBackGI,          lightingData.overrideBakedGI);
            context.AddBlock(HDBlockFields.SurfaceDescription.DepthOffset,          builtinData.depthOffset);

            // Fabric Silk
            if(fabricData.materialType == FabricData.MaterialType.Silk)
            {
                context.AddBlock(HDBlockFields.SurfaceDescription.Tangent);
                context.AddBlock(HDBlockFields.SurfaceDescription.Anisotropy);
            }
        }

        public override void GetPropertiesGUI(ref TargetPropertyGUIContext context, Action onChange, Action<String> registerUndo)
        {
            var settingsView = new FabricSettingsView(this);
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
            HDSubShaderUtilities.AddStencilShaderProperties(collector, lightingData.subsurfaceScattering,
                systemData.surfaceType == SurfaceType.Opaque ? lightingData.receiveSSR : lightingData.receiveSSRTransparent, lightingData.receiveSSR, lightingData.receiveSSRTransparent);
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

            FabricGUI.SetupMaterialKeywordsAndPass(material);
        }

        public bool TryUpgradeFromMasterNode(IMasterNode1 masterNode, out Dictionary<BlockFieldDescriptor, int> blockMap)
        {
            blockMap = null;
            if(!(masterNode is FabricMasterNode1 fabricMasterNode))
                return false;

            // Set data
            systemData.surfaceType = (SurfaceType)fabricMasterNode.m_SurfaceType;
            systemData.blendMode = HDSubShaderUtilities.UpgradeLegacyAlphaModeToBlendMode((int)fabricMasterNode.m_AlphaMode);
            systemData.alphaTest = fabricMasterNode.m_AlphaTest;
            systemData.sortPriority = fabricMasterNode.m_SortPriority;
            systemData.doubleSidedMode = fabricMasterNode.m_DoubleSidedMode;
            systemData.zWrite = fabricMasterNode.m_ZWrite;
            systemData.transparentCullMode = fabricMasterNode.m_transparentCullMode;
            systemData.zTest = fabricMasterNode.m_ZTest;
            systemData.supportLodCrossFade = fabricMasterNode.m_SupportLodCrossFade;
            systemData.dotsInstancing = fabricMasterNode.m_DOTSInstancing;
            systemData.materialNeedsUpdateHash = fabricMasterNode.m_MaterialNeedsUpdateHash;

            builtinData.transparencyFog = fabricMasterNode.m_TransparencyFog;
            builtinData.addPrecomputedVelocity = fabricMasterNode.m_AddPrecomputedVelocity;
            builtinData.depthOffset = fabricMasterNode.m_depthOffset;
            builtinData.alphaToMask = fabricMasterNode.m_AlphaToMask;

            lightingData.blendPreserveSpecular = fabricMasterNode.m_BlendPreserveSpecular;
            lightingData.receiveDecals = fabricMasterNode.m_ReceiveDecals;
            lightingData.receiveSSR = fabricMasterNode.m_ReceivesSSR;
            lightingData.energyConservingSpecular = fabricMasterNode.m_EnergyConservingSpecular;
            lightingData.specularOcclusionMode = fabricMasterNode.m_SpecularOcclusionMode;
            lightingData.overrideBakedGI = fabricMasterNode.m_overrideBakedGI;
            lightingData.transmission = fabricMasterNode.m_Transmission;
            lightingData.subsurfaceScattering = fabricMasterNode.m_SubsurfaceScattering;
            
            fabricData.materialType = (FabricData.MaterialType)fabricMasterNode.m_MaterialType;
            target.customEditorGUI = fabricMasterNode.m_OverrideEnabled ? fabricMasterNode.m_ShaderGUIOverride : "";

            // Convert SlotMask to BlockMap entries
            var blockMapLookup = new Dictionary<FabricMasterNode1.SlotMask, BlockFieldDescriptor>()
            {
                { FabricMasterNode1.SlotMask.Position, BlockFields.VertexDescription.Position },
                { FabricMasterNode1.SlotMask.VertexNormal, BlockFields.VertexDescription.Normal },
                { FabricMasterNode1.SlotMask.VertexTangent, BlockFields.VertexDescription.Tangent },
                { FabricMasterNode1.SlotMask.Albedo, BlockFields.SurfaceDescription.BaseColor },
                { FabricMasterNode1.SlotMask.SpecularOcclusion, HDBlockFields.SurfaceDescription.SpecularOcclusion },
                { FabricMasterNode1.SlotMask.Normal, BlockFields.SurfaceDescription.NormalTS },
                { FabricMasterNode1.SlotMask.BentNormal, HDBlockFields.SurfaceDescription.BentNormal },
                { FabricMasterNode1.SlotMask.Smoothness, BlockFields.SurfaceDescription.Smoothness },
                { FabricMasterNode1.SlotMask.Occlusion, BlockFields.SurfaceDescription.Occlusion },
                { FabricMasterNode1.SlotMask.Specular, BlockFields.SurfaceDescription.Specular },
                { FabricMasterNode1.SlotMask.DiffusionProfile, HDBlockFields.SurfaceDescription.DiffusionProfileHash },
                { FabricMasterNode1.SlotMask.SubsurfaceMask, HDBlockFields.SurfaceDescription.SubsurfaceMask },
                { FabricMasterNode1.SlotMask.Thickness, HDBlockFields.SurfaceDescription.Thickness },
                { FabricMasterNode1.SlotMask.Tangent, HDBlockFields.SurfaceDescription.Tangent },
                { FabricMasterNode1.SlotMask.Anisotropy, HDBlockFields.SurfaceDescription.Anisotropy },
                { FabricMasterNode1.SlotMask.Emission, BlockFields.SurfaceDescription.Emission },
                { FabricMasterNode1.SlotMask.Alpha, BlockFields.SurfaceDescription.Alpha },
                { FabricMasterNode1.SlotMask.AlphaClipThreshold, BlockFields.SurfaceDescription.AlphaClipThreshold },
            };

            // Legacy master node slots have additional slot conditions, test them here
            bool AdditionalSlotMaskTests(FabricMasterNode1.SlotMask slotMask)
            {
                switch(slotMask)
                {
                    case FabricMasterNode1.SlotMask.SpecularOcclusion:
                        return lightingData.specularOcclusionMode == SpecularOcclusionMode.Custom;
                    case FabricMasterNode1.SlotMask.DiffusionProfile:
                        return lightingData.subsurfaceScattering || lightingData.transmission;
                    case FabricMasterNode1.SlotMask.SubsurfaceMask:
                        return lightingData.subsurfaceScattering;
                    case FabricMasterNode1.SlotMask.Thickness:
                        return lightingData.transmission;
                    case FabricMasterNode1.SlotMask.AlphaClipThreshold:
                        return systemData.alphaTest;
                    default:
                        return true;
                }
            }

            // Set blockmap
            blockMap = new Dictionary<BlockFieldDescriptor, int>();
            foreach(FabricMasterNode1.SlotMask slotMask in Enum.GetValues(typeof(FabricMasterNode1.SlotMask)))
            {
                if(fabricMasterNode.MaterialTypeUsesSlotMask(slotMask))
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
                blockMap.Add(HDBlockFields.SurfaceDescription.BakedGI, FabricMasterNode1.LightingSlotId);
                blockMap.Add(HDBlockFields.SurfaceDescription.BakedBackGI, FabricMasterNode1.BackLightingSlotId);
            }

            // Depth Offset
            if(builtinData.depthOffset)
            {
                blockMap.Add(HDBlockFields.SurfaceDescription.DepthOffset, FabricMasterNode1.DepthOffsetSlotId);
            }

            return true;
        }

#region SubShaders
        static class SubShaders
        {
            public static SubShaderDescriptor Fabric = new SubShaderDescriptor()
            {
                pipelineTag = HDRenderPipeline.k_ShaderTagName,
                generatesPreview = true,
                passes = new PassCollection
                {
                    { FabricPasses.ShadowCaster },
                    { FabricPasses.META },
                    { FabricPasses.SceneSelection },
                    { FabricPasses.DepthForwardOnly },
                    { FabricPasses.MotionVectors },
                    { FabricPasses.ForwardOnly },
                },
            };

            public static SubShaderDescriptor FabricRaytracing = new SubShaderDescriptor()
            {
                pipelineTag = HDRenderPipeline.k_ShaderTagName,
                generatesPreview = false,
                passes = new PassCollection
                {
                    { FabricPasses.RaytracingIndirect, new FieldCondition(Fields.IsPreview, false) },
                    { FabricPasses.RaytracingVisibility, new FieldCondition(Fields.IsPreview, false) },
                    { FabricPasses.RaytracingForward, new FieldCondition(Fields.IsPreview, false) },
                    { FabricPasses.RaytracingGBuffer, new FieldCondition(Fields.IsPreview, false) },
                    { FabricPasses.RaytracingSubSurface, new FieldCondition(Fields.IsPreview, false) },
                },
            };
        }
#endregion

#region Passes
        public static class FabricPasses
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
                validPixelBlocks = FabricBlockMasks.FragmentMETA,

                // Collections
                structs = CoreStructCollections.Default,
                requiredFields = CoreRequiredFields.Meta,
                fieldDependencies = CoreFieldDependencies.Default,
                renderStates = CoreRenderStates.Meta,
                pragmas = CorePragmas.DotsInstancedInV2Only,
                keywords = CoreKeywords.HDBase,
                includes = FabricIncludes.Meta,
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
                validPixelBlocks = FabricBlockMasks.FragmentAlphaDepth,

                // Collections
                structs = CoreStructCollections.Default,
                fieldDependencies = CoreFieldDependencies.Default,
                renderStates = CoreRenderStates.BlendShadowCaster,
                pragmas = CorePragmas.DotsInstancedInV2Only,
                keywords = CoreKeywords.HDBase,
                includes = FabricIncludes.DepthOnly,
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
                validPixelBlocks = FabricBlockMasks.FragmentAlphaDepth,

                // Collections
                structs = CoreStructCollections.Default,
                fieldDependencies = CoreFieldDependencies.Default,
                renderStates = CoreRenderStates.ShadowCaster,
                pragmas = CorePragmas.DotsInstancedInV2OnlyEditorSync,
                defines = CoreDefines.SceneSelection,
                keywords = CoreKeywords.HDBase,
                includes = FabricIncludes.DepthOnly,
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
                validPixelBlocks = FabricBlockMasks.FragmentDepthMotionVectors,

                // Collections
                structs = CoreStructCollections.Default,
                requiredFields = CoreRequiredFields.LitFull,
                fieldDependencies = CoreFieldDependencies.Default,
                renderStates = CoreRenderStates.DepthOnly,
                pragmas = CorePragmas.DotsInstancedInV2Only,
                defines = CoreDefines.DepthMotionVectors,
                keywords = CoreKeywords.DepthMotionVectorsNoNormal,
                includes = FabricIncludes.DepthOnly,
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
                validPixelBlocks = FabricBlockMasks.FragmentDepthMotionVectors,

                // Collections
                structs = CoreStructCollections.Default,
                requiredFields = CoreRequiredFields.LitFull,
                fieldDependencies = CoreFieldDependencies.Default,
                renderStates = CoreRenderStates.MotionVectors,
                pragmas = CorePragmas.DotsInstancedInV2Only,
                defines = CoreDefines.DepthMotionVectors,
                keywords = CoreKeywords.DepthMotionVectorsNoNormal,
                includes = FabricIncludes.MotionVectors,
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
                validPixelBlocks = FabricBlockMasks.FragmentForward,

                // Collections
                structs = CoreStructCollections.Default,
                requiredFields = CoreRequiredFields.LitFull,
                fieldDependencies = CoreFieldDependencies.Default,
                renderStates = CoreRenderStates.Forward,
                pragmas = CorePragmas.DotsInstancedInV2Only,
                defines = CoreDefines.Forward,
                keywords = CoreKeywords.Forward,
                includes = FabricIncludes.ForwardOnly,
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
                validVertexBlocks = CoreBlockMasks.Vertex,
                validPixelBlocks = FabricBlockMasks.FragmentForward,

                // Collections
                structs = CoreStructCollections.Default,
                fieldDependencies = CoreFieldDependencies.Default,
                pragmas = CorePragmas.RaytracingBasic,
                defines = FabricDefines.RaytracingForwardIndirect,
                keywords = CoreKeywords.RaytracingIndirect,
                includes = CoreIncludes.Raytracing,
                requiredFields = new FieldCollection(){ HDFields.SubShader.Fabric, HDFields.ShaderPass.RaytracingIndirect },
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
                validVertexBlocks = CoreBlockMasks.Vertex,
                validPixelBlocks = FabricBlockMasks.FragmentForward,

                // Collections
                structs = CoreStructCollections.Default,
                fieldDependencies = CoreFieldDependencies.Default,
                pragmas = CorePragmas.RaytracingBasic,
                keywords = CoreKeywords.HDBase,
                includes = CoreIncludes.Raytracing,
                requiredFields = new FieldCollection(){ HDFields.SubShader.Fabric, HDFields.ShaderPass.RaytracingVisibility },
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
                validVertexBlocks = CoreBlockMasks.Vertex,
                validPixelBlocks = FabricBlockMasks.FragmentForward,

                // Collections
                structs = CoreStructCollections.Default,
                fieldDependencies = CoreFieldDependencies.Default,
                pragmas = CorePragmas.RaytracingBasic,
                defines = FabricDefines.RaytracingForwardIndirect,
                keywords = CoreKeywords.RaytracingGBufferForward,
                includes = CoreIncludes.Raytracing,
                requiredFields = new FieldCollection(){ HDFields.SubShader.Fabric, HDFields.ShaderPass.RaytracingForward },
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
                validVertexBlocks = CoreBlockMasks.Vertex,
                validPixelBlocks = FabricBlockMasks.FragmentForward,

                // Collections
                structs = CoreStructCollections.Default,
                fieldDependencies = CoreFieldDependencies.Default,
                pragmas = CorePragmas.RaytracingBasic,
                defines = FabricDefines.RaytracingGBuffer,
                keywords = CoreKeywords.RaytracingGBufferForward,
                includes = CoreIncludes.Raytracing,
                requiredFields = new FieldCollection(){ HDFields.SubShader.Fabric, HDFields.ShaderPass.RayTracingGBuffer },
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
                validVertexBlocks = CoreBlockMasks.Vertex,
                validPixelBlocks = FabricBlockMasks.FragmentForward,

                //Collections
                structs = CoreStructCollections.Default,
                fieldDependencies = CoreFieldDependencies.Default,
                pragmas = CorePragmas.RaytracingBasic,
                defines = FabricDefines.RaytracingGBuffer,
                keywords = CoreKeywords.RaytracingGBufferForward,
                includes = CoreIncludes.Raytracing,
                requiredFields = new FieldCollection(){ HDFields.SubShader.Fabric, HDFields.ShaderPass.RaytracingSubSurface },
            };
        }
#endregion

#region BlockMasks
        static class FabricBlockMasks
        {
            public static BlockFieldDescriptor[] FragmentMETA = new BlockFieldDescriptor[]
            {
                BlockFields.SurfaceDescription.BaseColor,
                HDBlockFields.SurfaceDescription.SpecularOcclusion,
                BlockFields.SurfaceDescription.NormalTS,
                BlockFields.SurfaceDescription.Smoothness,
                BlockFields.SurfaceDescription.Occlusion,
                BlockFields.SurfaceDescription.Specular,
                HDBlockFields.SurfaceDescription.DiffusionProfileHash,
                HDBlockFields.SurfaceDescription.SubsurfaceMask,
                HDBlockFields.SurfaceDescription.Thickness,
                HDBlockFields.SurfaceDescription.Tangent,
                HDBlockFields.SurfaceDescription.Anisotropy,
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
                HDBlockFields.SurfaceDescription.BentNormal,
                BlockFields.SurfaceDescription.Smoothness,
                BlockFields.SurfaceDescription.Occlusion,
                BlockFields.SurfaceDescription.Specular,
                HDBlockFields.SurfaceDescription.DiffusionProfileHash,
                HDBlockFields.SurfaceDescription.SubsurfaceMask,
                HDBlockFields.SurfaceDescription.Thickness,
                HDBlockFields.SurfaceDescription.Tangent,
                HDBlockFields.SurfaceDescription.Anisotropy,
                BlockFields.SurfaceDescription.Emission,
                BlockFields.SurfaceDescription.Alpha,
                BlockFields.SurfaceDescription.AlphaClipThreshold,
                HDBlockFields.SurfaceDescription.BakedGI,
                HDBlockFields.SurfaceDescription.BakedBackGI,
                HDBlockFields.SurfaceDescription.DepthOffset,
            };
        }
#endregion

#region Defines
        static class FabricDefines
        {
            public static DefineCollection RaytracingForwardIndirect = new DefineCollection
            {
                { CoreKeywordDescriptors.Shadow, 0 },
                { CoreKeywordDescriptors.HasLightloop, 1 },
            };

            public static DefineCollection RaytracingGBuffer = new DefineCollection
            {
                { CoreKeywordDescriptors.Shadow, 0 },
            };
        }
#endregion

#region Includes
        static class FabricIncludes
        {
            const string kFabric = "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Fabric/Fabric.hlsl";

            public static IncludeCollection Common = new IncludeCollection
            {
                { CoreIncludes.CorePregraph },
                { CoreIncludes.kNormalSurfaceGradient, IncludeLocation.Pregraph },
                { kFabric, IncludeLocation.Pregraph },
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
                { kFabric, IncludeLocation.Pregraph },
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
