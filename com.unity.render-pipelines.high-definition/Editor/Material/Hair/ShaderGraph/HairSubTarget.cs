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

namespace UnityEditor.Rendering.HighDefinition.ShaderGraph
{
    sealed class HairSubTarget : SubTarget<HDTarget>, IHasMetadata, ILegacyTarget,
        IRequiresData<SystemData>, IRequiresData<BuiltinData>, IRequiresData<LightingData>, IRequiresData<HairData>
    {
        const string kAssetGuid = "7e681cc79dd8e6c46ba1e8412d519e26";
        static string passTemplatePath => $"{HDUtils.GetHDRenderPipelinePath()}Editor/Material/Hair/ShaderGraph/HairPass.template";

        public HairSubTarget()
        {
            displayName = "Hair";
        }

        // Render State
        string renderType => HDRenderTypeTags.HDLitShader.ToString();
        string renderQueue
        {
            get
            {
                var renderingPass = systemData.surfaceType == SurfaceType.Opaque ? HDRenderQueue.RenderQueueType.Opaque : HDRenderQueue.RenderQueueType.Transparent;
                int queue = HDRenderQueue.ChangeType(renderingPass, systemData.sortPriority, systemData.alphaTest);
                return HDRenderQueue.GetShaderTagValue(queue);
            }
        }

        // Material Data
        SystemData m_SystemData;
        BuiltinData m_BuiltinData;
        LightingData m_LightingData;
        HairData m_HairData;

        // Interface Properties
        SystemData IRequiresData<SystemData>.data
        {
            get => m_SystemData;
            set => m_SystemData = value;
        }
        BuiltinData IRequiresData<BuiltinData>.data
        {
            get => m_BuiltinData;
            set => m_BuiltinData = value;
        }
        LightingData IRequiresData<LightingData>.data
        {
            get => m_LightingData;
            set => m_LightingData = value;
        }
        HairData IRequiresData<HairData>.data
        {
            get => m_HairData;
            set => m_HairData = value;
        }

        // Public properties
        public SystemData systemData
        {
            get => m_SystemData;
            set => m_SystemData = value;
        }
        public BuiltinData builtinData
        {
            get => m_BuiltinData;
            set => m_BuiltinData = value;
        }
        public LightingData lightingData
        {
            get => m_LightingData;
            set => m_LightingData = value;
        }
        public HairData hairData
        {
            get => m_HairData;
            set => m_HairData = value;
        }

        public override bool IsActive() => true;

        public override void Setup(ref TargetSetupContext context)
        {
            context.AddAssetDependencyPath(AssetDatabase.GUIDToAssetPath(kAssetGuid));
            context.SetDefaultShaderGUI("Rendering.HighDefinition.HairGUI");

            // Process SubShaders
            SubShaderDescriptor[] subShaders = { SubShaders.Hair, SubShaders.HairRaytracing };
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
            // Features
            context.AddField(Fields.LodCrossFade,                           systemData.supportLodCrossFade);

            // Surface Type
            context.AddField(Fields.SurfaceOpaque,                          systemData.surfaceType == SurfaceType.Opaque);
            context.AddField(Fields.SurfaceTransparent,                     systemData.surfaceType != SurfaceType.Opaque);

            // Structs
            context.AddField(HDStructFields.FragInputs.IsFrontFace,         systemData.doubleSidedMode != DoubleSidedMode.Disabled && !context.pass.Equals(HairSubTarget.HairPasses.MotionVectors));

            // Material
            context.AddField(HDFields.KajiyaKay,                            hairData.materialType == HairData.MaterialType.KajiyaKay);

            // Specular Occlusion
            context.AddField(HDFields.SpecularOcclusionFromAO,              lightingData.specularOcclusionMode == SpecularOcclusionMode.FromAO);
            context.AddField(HDFields.SpecularOcclusionFromAOBentNormal,    lightingData.specularOcclusionMode == SpecularOcclusionMode.FromAOAndBentNormal);
            context.AddField(HDFields.SpecularOcclusionCustom,              lightingData.specularOcclusionMode == SpecularOcclusionMode.Custom);

            // AlphaTest
            // We always generate the keyword ALPHATEST_ON
            context.AddField(Fields.AlphaTest,                              systemData.alphaTest && (context.pass.pixelBlocks.Contains(BlockFields.SurfaceDescription.AlphaClipThreshold) || context.pass.pixelBlocks.Contains(HDBlockFields.SurfaceDescription.AlphaClipThresholdShadow) ||
                                                                                context.pass.pixelBlocks.Contains(HDBlockFields.SurfaceDescription.AlphaClipThresholdDepthPrepass) || context.pass.pixelBlocks.Contains(HDBlockFields.SurfaceDescription.AlphaClipThresholdDepthPostpass)));
            // All the DoAlphaXXX field drive the generation of which code to use for alpha test in the template
            // Do alpha test only if we aren't using the TestShadow one
            context.AddField(HDFields.DoAlphaTest,                          systemData.alphaTest && (context.pass.pixelBlocks.Contains(BlockFields.SurfaceDescription.AlphaClipThreshold) &&
                                                                                !(lightingData.alphaTestShadow && context.pass.pixelBlocks.Contains(HDBlockFields.SurfaceDescription.AlphaClipThresholdShadow))));
            context.AddField(HDFields.DoAlphaTestShadow,                    systemData.alphaTest && lightingData.alphaTestShadow && context.pass.pixelBlocks.Contains(HDBlockFields.SurfaceDescription.AlphaClipThresholdShadow));
            context.AddField(HDFields.DoAlphaTestPrepass,                   systemData.alphaTest && systemData.alphaTestDepthPrepass && context.pass.pixelBlocks.Contains(HDBlockFields.SurfaceDescription.AlphaClipThresholdDepthPrepass));
            context.AddField(HDFields.DoAlphaTestPostpass,                  systemData.alphaTest && systemData.alphaTestDepthPostpass && context.pass.pixelBlocks.Contains(HDBlockFields.SurfaceDescription.AlphaClipThresholdDepthPostpass));

            // Misc
            context.AddField(Fields.AlphaToMask,                            systemData.alphaTest && context.pass.pixelBlocks.Contains(BlockFields.SurfaceDescription.AlphaClipThreshold) && builtinData.alphaToMask);
            context.AddField(HDFields.AlphaFog,                             systemData.surfaceType != SurfaceType.Opaque && builtinData.transparencyFog);
            context.AddField(HDFields.BlendPreserveSpecular,                systemData.surfaceType != SurfaceType.Opaque && lightingData.blendPreserveSpecular);
            context.AddField(HDFields.TransparentWritesMotionVec,           systemData.surfaceType != SurfaceType.Opaque && builtinData.transparentWritesMotionVec);
            context.AddField(HDFields.DisableDecals,                        !lightingData.receiveDecals);
            context.AddField(HDFields.DisableSSR,                           !lightingData.receiveSSR);
            context.AddField(Fields.VelocityPrecomputed,                    builtinData.addPrecomputedVelocity);
            context.AddField(HDFields.BentNormal,                           context.blocks.Contains(HDBlockFields.SurfaceDescription.BentNormal) && context.pass.pixelBlocks.Contains(HDBlockFields.SurfaceDescription.BentNormal));
            context.AddField(HDFields.AmbientOcclusion,                     context.blocks.Contains(BlockFields.SurfaceDescription.Occlusion) && context.pass.pixelBlocks.Contains(BlockFields.SurfaceDescription.Occlusion));
            context.AddField(HDFields.LightingGI,                           context.blocks.Contains(HDBlockFields.SurfaceDescription.BakedGI) && context.pass.pixelBlocks.Contains(HDBlockFields.SurfaceDescription.BakedGI));
            context.AddField(HDFields.BackLightingGI,                       context.blocks.Contains(HDBlockFields.SurfaceDescription.BakedBackGI) && context.pass.pixelBlocks.Contains(HDBlockFields.SurfaceDescription.BakedBackGI));
            context.AddField(HDFields.DepthOffset,                          builtinData.depthOffset && context.pass.pixelBlocks.Contains(HDBlockFields.SurfaceDescription.DepthOffset));
            
            context.AddField(HDFields.SpecularAA,                           lightingData.specularAA &&
                                                                                context.pass.pixelBlocks.Contains(HDBlockFields.SurfaceDescription.SpecularAAThreshold) &&
                                                                                context.pass.pixelBlocks.Contains(HDBlockFields.SurfaceDescription.SpecularAAScreenSpaceVariance));
            context.AddField(HDFields.HairStrandDirection,                  context.blocks.Contains(HDBlockFields.SurfaceDescription.HairStrandDirection) && context.pass.pixelBlocks.Contains(HDBlockFields.SurfaceDescription.HairStrandDirection));
            context.AddField(HDFields.Transmittance,                        context.blocks.Contains(HDBlockFields.SurfaceDescription.Transmittance) && context.pass.pixelBlocks.Contains(HDBlockFields.SurfaceDescription.Transmittance));
            context.AddField(HDFields.RimTransmissionIntensity,             context.blocks.Contains(HDBlockFields.SurfaceDescription.RimTransmissionIntensity) && context.pass.pixelBlocks.Contains(HDBlockFields.SurfaceDescription.RimTransmissionIntensity));
            context.AddField(HDFields.UseLightFacingNormal,                 hairData.useLightFacingNormal);
            context.AddField(HDFields.TransparentBackFace,                  systemData.surfaceType != SurfaceType.Opaque && lightingData.backThenFrontRendering);
            context.AddField(HDFields.TransparentDepthPrePass,              systemData.surfaceType != SurfaceType.Opaque && systemData.alphaTestDepthPrepass);
            context.AddField(HDFields.TransparentDepthPostPass,             systemData.surfaceType != SurfaceType.Opaque && systemData.alphaTestDepthPrepass);
        }

        public override void GetActiveBlocks(ref TargetActiveBlockContext context)
        {
            // Vertex
            context.AddBlock(BlockFields.VertexDescription.Position);
            context.AddBlock(BlockFields.VertexDescription.Normal);
            context.AddBlock(BlockFields.VertexDescription.Tangent);

            // Hair
            context.AddBlock(BlockFields.SurfaceDescription.BaseColor);
            context.AddBlock(HDBlockFields.SurfaceDescription.SpecularOcclusion,                lightingData.specularOcclusionMode == SpecularOcclusionMode.Custom);
            context.AddBlock(BlockFields.SurfaceDescription.NormalTS);
            context.AddBlock(HDBlockFields.SurfaceDescription.BentNormal);
            context.AddBlock(BlockFields.SurfaceDescription.Smoothness);
            context.AddBlock(BlockFields.SurfaceDescription.Occlusion);
            context.AddBlock(HDBlockFields.SurfaceDescription.Transmittance);
            context.AddBlock(HDBlockFields.SurfaceDescription.RimTransmissionIntensity);
            context.AddBlock(HDBlockFields.SurfaceDescription.HairStrandDirection);
            context.AddBlock(BlockFields.SurfaceDescription.Emission);
            context.AddBlock(BlockFields.SurfaceDescription.Alpha);
            context.AddBlock(BlockFields.SurfaceDescription.AlphaClipThreshold,                 systemData.alphaTest);
            context.AddBlock(HDBlockFields.SurfaceDescription.AlphaClipThresholdDepthPrepass,   systemData.surfaceType == SurfaceType.Transparent && systemData.alphaTest && systemData.alphaTestDepthPrepass);
            context.AddBlock(HDBlockFields.SurfaceDescription.AlphaClipThresholdDepthPostpass,  systemData.surfaceType == SurfaceType.Transparent && systemData.alphaTest && systemData.alphaTestDepthPostpass);
            context.AddBlock(HDBlockFields.SurfaceDescription.AlphaClipThresholdShadow,         systemData.alphaTest && lightingData.alphaTestShadow);
            context.AddBlock(HDBlockFields.SurfaceDescription.SpecularAAScreenSpaceVariance,    lightingData.specularAA);
            context.AddBlock(HDBlockFields.SurfaceDescription.SpecularAAThreshold,              lightingData.specularAA);
            context.AddBlock(HDBlockFields.SurfaceDescription.SpecularTint);
            context.AddBlock(HDBlockFields.SurfaceDescription.SpecularShift);
            context.AddBlock(HDBlockFields.SurfaceDescription.SecondarySpecularTint);
            context.AddBlock(HDBlockFields.SurfaceDescription.SecondarySmoothness);
            context.AddBlock(HDBlockFields.SurfaceDescription.SecondarySpecularShift);
            context.AddBlock(HDBlockFields.SurfaceDescription.BakedGI,              lightingData.overrideBakedGI);
            context.AddBlock(HDBlockFields.SurfaceDescription.BakedBackGI,          lightingData.overrideBakedGI);
            context.AddBlock(HDBlockFields.SurfaceDescription.DepthOffset,          builtinData.depthOffset);
        }

        public override void GetPropertiesGUI(ref TargetPropertyGUIContext context, Action onChange, Action<String> registerUndo)
        {
            var settingsView = new HairSettingsView(this);
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
            HDSubShaderUtilities.AddStencilShaderProperties(collector, false, lightingData.receiveSSR);
            HDSubShaderUtilities.AddBlendingStatesShaderProperties(
                collector,
                systemData.surfaceType,
                systemData.blendMode,
                systemData.sortPriority,
                builtinData.alphaToMask,
                systemData.zWrite,
                systemData.transparentCullMode,
                systemData.zTest,
                lightingData.backThenFrontRendering,
                builtinData.transparencyFog
            );
            HDSubShaderUtilities.AddAlphaCutoffShaderProperties(collector, systemData.alphaTest, lightingData.alphaTestShadow);
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

            HairGUI.SetupMaterialKeywordsAndPass(material);
        }

        int ComputeMaterialNeedsUpdateHash()
        {
            int hash = 0;
            hash |= (systemData.alphaTest ? 0 : 1) << 0;
            hash |= (lightingData.alphaTestShadow ? 0 : 1) << 1;
            hash |= (lightingData.receiveSSR ? 0 : 1) << 2;
            return hash;
        }

        public override object saveContext
        {
            get
            {
                int hash = ComputeMaterialNeedsUpdateHash();
                bool needsUpdate = hash != systemData.materialNeedsUpdateHash;
                if (needsUpdate)
                    systemData.materialNeedsUpdateHash = hash;

                return new HDSaveContext{ updateMaterials = needsUpdate };
            }
        }

        // IHasMetaData
        public string identifier => "HDHairSubTarget";

        public ScriptableObject GetMetadataObject()
        {
            var hdMetadata = ScriptableObject.CreateInstance<HDMetadata>();
            hdMetadata.shaderID = HDShaderUtils.ShaderID.SG_Hair;
            return hdMetadata;
        }

        public bool TryUpgradeFromMasterNode(IMasterNode1 masterNode, out Dictionary<BlockFieldDescriptor, int> blockMap)
        {
            blockMap = null;
            if(!(masterNode is HairMasterNode1 hairMasterNode))
                return false;

            // Set data
            systemData.surfaceType = (SurfaceType)hairMasterNode.m_SurfaceType;
            systemData.blendMode = HDSubShaderUtilities.UpgradeLegacyAlphaModeToBlendMode((int)hairMasterNode.m_AlphaMode);
            systemData.alphaTest = hairMasterNode.m_AlphaTest;
            systemData.alphaTestDepthPrepass = hairMasterNode.m_AlphaTestDepthPrepass;
            systemData.alphaTestDepthPostpass = hairMasterNode.m_AlphaTestDepthPostpass;
            systemData.sortPriority = hairMasterNode.m_SortPriority;
            systemData.doubleSidedMode = hairMasterNode.m_DoubleSidedMode;
            systemData.zWrite = hairMasterNode.m_ZWrite;
            systemData.transparentCullMode = hairMasterNode.m_transparentCullMode;
            systemData.zTest = hairMasterNode.m_ZTest;
            systemData.supportLodCrossFade = hairMasterNode.m_SupportLodCrossFade;
            systemData.dotsInstancing = hairMasterNode.m_DOTSInstancing;
            systemData.materialNeedsUpdateHash = hairMasterNode.m_MaterialNeedsUpdateHash;

            builtinData.transparencyFog = hairMasterNode.m_TransparencyFog;
            builtinData.transparentWritesMotionVec = hairMasterNode.m_TransparentWritesMotionVec;
            builtinData.addPrecomputedVelocity = hairMasterNode.m_AddPrecomputedVelocity;
            builtinData.depthOffset = hairMasterNode.m_depthOffset;
            builtinData.alphaToMask = hairMasterNode.m_AlphaToMask;

            lightingData.alphaTestShadow = hairMasterNode.m_AlphaTestShadow;
            lightingData.backThenFrontRendering = hairMasterNode.m_BackThenFrontRendering;
            lightingData.blendPreserveSpecular = hairMasterNode.m_BlendPreserveSpecular;
            lightingData.receiveDecals = hairMasterNode.m_ReceiveDecals;
            lightingData.receiveSSR = hairMasterNode.m_ReceivesSSR;
            lightingData.specularAA = hairMasterNode.m_SpecularAA;
            lightingData.specularOcclusionMode = hairMasterNode.m_SpecularOcclusionMode;
            lightingData.overrideBakedGI = hairMasterNode.m_overrideBakedGI;
            
            hairData.materialType = (HairData.MaterialType)hairMasterNode.m_MaterialType;
            hairData.useLightFacingNormal = hairMasterNode.m_UseLightFacingNormal;
            target.customEditorGUI = hairMasterNode.m_OverrideEnabled ? hairMasterNode.m_ShaderGUIOverride : "";

            // Convert SlotMask to BlockMap entries
            var blockMapLookup = new Dictionary<HairMasterNode1.SlotMask, BlockFieldDescriptor>()
            {
                { HairMasterNode1.SlotMask.Position, BlockFields.VertexDescription.Position },
                { HairMasterNode1.SlotMask.VertexNormal, BlockFields.VertexDescription.Normal },
                { HairMasterNode1.SlotMask.VertexTangent, BlockFields.VertexDescription.Tangent },
                { HairMasterNode1.SlotMask.Albedo, BlockFields.SurfaceDescription.BaseColor },
                { HairMasterNode1.SlotMask.SpecularOcclusion, HDBlockFields.SurfaceDescription.SpecularOcclusion },
                { HairMasterNode1.SlotMask.Normal, BlockFields.SurfaceDescription.NormalTS },
                { HairMasterNode1.SlotMask.BentNormal, HDBlockFields.SurfaceDescription.BentNormal },
                { HairMasterNode1.SlotMask.Smoothness, BlockFields.SurfaceDescription.Smoothness },
                { HairMasterNode1.SlotMask.Occlusion, BlockFields.SurfaceDescription.Occlusion },
                { HairMasterNode1.SlotMask.Transmittance, HDBlockFields.SurfaceDescription.Transmittance },
                { HairMasterNode1.SlotMask.RimTransmissionIntensity, HDBlockFields.SurfaceDescription.RimTransmissionIntensity },
                { HairMasterNode1.SlotMask.HairStrandDirection, HDBlockFields.SurfaceDescription.HairStrandDirection },
                { HairMasterNode1.SlotMask.Emission, BlockFields.SurfaceDescription.Emission },
                { HairMasterNode1.SlotMask.Alpha, BlockFields.SurfaceDescription.Alpha },
                { HairMasterNode1.SlotMask.AlphaClipThreshold, BlockFields.SurfaceDescription.AlphaClipThreshold },
                { HairMasterNode1.SlotMask.AlphaClipThresholdDepthPrepass, HDBlockFields.SurfaceDescription.AlphaClipThresholdDepthPrepass },
                { HairMasterNode1.SlotMask.AlphaClipThresholdDepthPostpass, HDBlockFields.SurfaceDescription.AlphaClipThresholdDepthPostpass },
                { HairMasterNode1.SlotMask.AlphaClipThresholdShadow, HDBlockFields.SurfaceDescription.AlphaClipThresholdShadow },
                { HairMasterNode1.SlotMask.SpecularTint, HDBlockFields.SurfaceDescription.SpecularTint },
                { HairMasterNode1.SlotMask.SpecularShift, HDBlockFields.SurfaceDescription.SpecularShift },
                { HairMasterNode1.SlotMask.SecondarySpecularTint, HDBlockFields.SurfaceDescription.SecondarySpecularTint },
                { HairMasterNode1.SlotMask.SecondarySmoothness, HDBlockFields.SurfaceDescription.SecondarySmoothness },
                { HairMasterNode1.SlotMask.SecondarySpecularShift, HDBlockFields.SurfaceDescription.SecondarySpecularShift },
            };

            // Legacy master node slots have additional slot conditions, test them here
            bool AdditionalSlotMaskTests(HairMasterNode1.SlotMask slotMask)
            {
                switch(slotMask)
                {
                    case HairMasterNode1.SlotMask.SpecularOcclusion:
                        return lightingData.specularOcclusionMode == SpecularOcclusionMode.Custom;
                    case HairMasterNode1.SlotMask.AlphaClipThreshold:
                        return systemData.alphaTest;
                    case HairMasterNode1.SlotMask.AlphaClipThresholdDepthPrepass:
                        return systemData.surfaceType == SurfaceType.Transparent && systemData.alphaTest && systemData.alphaTestDepthPrepass;
                    case HairMasterNode1.SlotMask.AlphaClipThresholdDepthPostpass:
                        return systemData.surfaceType == SurfaceType.Transparent && systemData.alphaTest && systemData.alphaTestDepthPostpass;
                    case HairMasterNode1.SlotMask.AlphaClipThresholdShadow:
                        return systemData.alphaTest && lightingData.alphaTestShadow;
                    default:
                        return true;
                }
            }

            // Set blockmap
            blockMap = new Dictionary<BlockFieldDescriptor, int>();
            foreach(HairMasterNode1.SlotMask slotMask in Enum.GetValues(typeof(HairMasterNode1.SlotMask)))
            {
                if(hairMasterNode.MaterialTypeUsesSlotMask(slotMask))
                {
                    if(!blockMapLookup.TryGetValue(slotMask, out var blockFieldDescriptor))
                        continue;

                    if(!AdditionalSlotMaskTests(slotMask))
                        continue;
                    
                    var slotId = Mathf.Log((int)slotMask, 2);
                    blockMap.Add(blockFieldDescriptor, (int)slotId);
                }
            }

            // Specular AA
            if(lightingData.specularAA)
            {
                blockMap.Add(HDBlockFields.SurfaceDescription.SpecularAAScreenSpaceVariance, HairMasterNode1.SpecularAAScreenSpaceVarianceSlotId);
                blockMap.Add(HDBlockFields.SurfaceDescription.SpecularAAThreshold, HairMasterNode1.SpecularAAThresholdSlotId);
            }

            // Override Baked GI
            if(lightingData.overrideBakedGI)
            {
                blockMap.Add(HDBlockFields.SurfaceDescription.BakedGI, HairMasterNode1.LightingSlotId);
                blockMap.Add(HDBlockFields.SurfaceDescription.BakedBackGI, HairMasterNode1.BackLightingSlotId);
            }

            // Depth Offset
            if(builtinData.depthOffset)
            {
                blockMap.Add(HDBlockFields.SurfaceDescription.DepthOffset, HairMasterNode1.DepthOffsetSlotId);
            }

            return true;
        }

#region SubShaders
        static class SubShaders
        {
            public static SubShaderDescriptor Hair = new SubShaderDescriptor()
            {
                pipelineTag = HDRenderPipeline.k_ShaderTagName,
                generatesPreview = true,
                passes = new PassCollection
                {
                    { HairPasses.ShadowCaster },
                    { HairPasses.META },
                    { HairPasses.SceneSelection },
                    { HairPasses.DepthForwardOnly },
                    { HairPasses.MotionVectors },
                    { HairPasses.TransparentBackface, new FieldCondition(HDFields.TransparentBackFace, true) },
                    { HairPasses.TransparentDepthPrepass, new FieldCondition(HDFields.TransparentDepthPrePass, true) },
                    { HairPasses.ForwardOnly },
                    { HairPasses.TransparentDepthPostpass, new FieldCondition(HDFields.TransparentDepthPostPass, true) },
                },
            };

            public static SubShaderDescriptor HairRaytracing = new SubShaderDescriptor()
            {
                pipelineTag = HDRenderPipeline.k_ShaderTagName,
                generatesPreview = false,
                passes = new PassCollection
                {
                    { HairPasses.RaytracingIndirect, new FieldCondition(Fields.IsPreview, false) },
                    { HairPasses.RaytracingVisibility, new FieldCondition(Fields.IsPreview, false) },
                    { HairPasses.RaytracingForward, new FieldCondition(Fields.IsPreview, false) },
                    { HairPasses.RaytracingGBuffer, new FieldCondition(Fields.IsPreview, false) },
                    { HairPasses.RaytracingSubSurface, new FieldCondition(Fields.IsPreview, false) },
                },
            };
        }
#endregion

#region Passes
        public static class HairPasses
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
                pixelBlocks = HairBlockMasks.FragmentMETA,

                // Collections
                structs = CoreStructCollections.Default,
                requiredFields = CoreRequiredFields.Meta,
                fieldDependencies = CoreFieldDependencies.Default,
                renderStates = CoreRenderStates.Meta,
                pragmas = CorePragmas.DotsInstancedInV2Only,
                keywords = CoreKeywords.HDBase,
                includes = HairIncludes.Meta,
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
                pixelBlocks = HairBlockMasks.FragmentShadowCaster,

                // Collections
                structs = CoreStructCollections.Default,
                fieldDependencies = CoreFieldDependencies.Default,
                renderStates = CoreRenderStates.BlendShadowCaster,
                pragmas = CorePragmas.DotsInstancedInV2Only,
                keywords = CoreKeywords.HDBase,
                includes = HairIncludes.DepthOnly,
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
                pixelBlocks = HairBlockMasks.FragmentAlphaDepth,

                // Collections
                structs = CoreStructCollections.Default,
                fieldDependencies = CoreFieldDependencies.Default,
                renderStates = CoreRenderStates.SceneSelection,
                pragmas = CorePragmas.DotsInstancedInV2OnlyEditorSync,
                defines = CoreDefines.SceneSelection,
                keywords = CoreKeywords.HDBase,
                includes = HairIncludes.DepthOnly,
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
                vertexBlocks = CoreBlockMasks.Vertex,
                pixelBlocks = HairBlockMasks.FragmentDepthMotionVectors,

                // Collections
                structs = CoreStructCollections.Default,
                requiredFields = CoreRequiredFields.LitFull,
                fieldDependencies = CoreFieldDependencies.Default,
                renderStates = CoreRenderStates.DepthOnly,
                pragmas = CorePragmas.DotsInstancedInV2Only,
                defines = CoreDefines.DepthMotionVectors,
                keywords = CoreKeywords.DepthMotionVectorsNoNormal,
                includes = HairIncludes.DepthOnly,
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
                pixelBlocks = HairBlockMasks.FragmentDepthMotionVectors,

                // Collections
                structs = CoreStructCollections.Default,
                requiredFields = CoreRequiredFields.LitFull,
                fieldDependencies = CoreFieldDependencies.Default,
                renderStates = HairRenderStates.MotionVectors,
                pragmas = CorePragmas.DotsInstancedInV2Only,
                defines = CoreDefines.DepthMotionVectors,
                keywords = CoreKeywords.DepthMotionVectorsNoNormal,
                includes = HairIncludes.MotionVectors,
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
                pixelBlocks = HairBlockMasks.FragmentTransparentDepthPrepass,

                // Collections
                structs = CoreStructCollections.Default,
                fieldDependencies = CoreFieldDependencies.Default,
                renderStates = CoreRenderStates.TransparentDepthPrePostPass,
                pragmas = CorePragmas.DotsInstancedInV2Only,
                defines = CoreDefines.TransparentDepthPrepass,
                keywords = CoreKeywords.HDBase,
                includes = HairIncludes.DepthOnly,
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
                pixelBlocks = HairBlockMasks.FragmentTransparentBackface,

                // Collections
                structs = CoreStructCollections.Default,
                requiredFields = CoreRequiredFields.LitMinimal,
                fieldDependencies = CoreFieldDependencies.Default,
                renderStates = CoreRenderStates.TransparentBackface,
                pragmas = CorePragmas.DotsInstancedInV2Only,
                defines = CoreDefines.Forward,
                keywords = CoreKeywords.Forward,
                includes = HairIncludes.ForwardOnly,
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
                vertexBlocks = CoreBlockMasks.Vertex,
                pixelBlocks = HairBlockMasks.FragmentForward,

                // Collections
                structs = CoreStructCollections.Default,
                requiredFields = CoreRequiredFields.LitFull,
                fieldDependencies = CoreFieldDependencies.Default,
                renderStates = CoreRenderStates.ForwardColorMask,
                pragmas = CorePragmas.DotsInstancedInV2Only,
                defines = CoreDefines.Forward,
                keywords = CoreKeywords.Forward,
                includes = HairIncludes.ForwardOnly,
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
                pixelBlocks = HairBlockMasks.FragmentTransparentDepthPostpass,

                // Collections
                structs = CoreStructCollections.Default,
                fieldDependencies = CoreFieldDependencies.Default,
                renderStates = CoreRenderStates.TransparentDepthPrePostPass,
                pragmas = CorePragmas.DotsInstancedInV2Only,
                defines = CoreDefines.ShaderGraphRaytracingHigh,
                keywords = CoreKeywords.HDBase,
                includes = HairIncludes.DepthOnly,
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
                pixelBlocks = HairBlockMasks.FragmentForward,

                // Collections
                structs = CoreStructCollections.Default,
                fieldDependencies = CoreFieldDependencies.Default,
                pragmas = CorePragmas.RaytracingBasic,
                defines = HairDefines.RaytracingForwardIndirect,
                keywords = CoreKeywords.RaytracingIndirect,
                includes = CoreIncludes.Raytracing,
                requiredFields = new FieldCollection(){ HDFields.SubShader.Hair, HDFields.ShaderPass.RaytracingIndirect },
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
                pixelBlocks = HairBlockMasks.FragmentForward,

                // Collections
                structs = CoreStructCollections.Default,
                fieldDependencies = CoreFieldDependencies.Default,
                pragmas = CorePragmas.RaytracingBasic,
                keywords = CoreKeywords.HDBase,
                includes = CoreIncludes.Raytracing,
                requiredFields = new FieldCollection(){ HDFields.SubShader.Hair, HDFields.ShaderPass.RaytracingVisibility },
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
                pixelBlocks = HairBlockMasks.FragmentForward,

                // Collections
                structs = CoreStructCollections.Default,
                fieldDependencies = CoreFieldDependencies.Default,
                pragmas = CorePragmas.RaytracingBasic,
                defines = HairDefines.RaytracingForwardIndirect,
                keywords = CoreKeywords.RaytracingGBufferForward,
                includes = CoreIncludes.Raytracing,
                requiredFields = new FieldCollection(){ HDFields.SubShader.Hair, HDFields.ShaderPass.RaytracingForward },
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
                pixelBlocks = HairBlockMasks.FragmentForward,

                // Collections
                structs = CoreStructCollections.Default,
                fieldDependencies = CoreFieldDependencies.Default,
                pragmas = CorePragmas.RaytracingBasic,
                defines = HairDefines.RaytracingGBuffer,
                keywords = CoreKeywords.RaytracingGBufferForward,
                includes = CoreIncludes.Raytracing,
                requiredFields = new FieldCollection(){ HDFields.SubShader.Hair, HDFields.ShaderPass.RayTracingGBuffer },
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

                // Port Mask
                vertexBlocks = CoreBlockMasks.Vertex,
                pixelBlocks = HairBlockMasks.FragmentForward,

                //Collections
                structs = CoreStructCollections.Default,
                fieldDependencies = CoreFieldDependencies.Default,
                pragmas = CorePragmas.RaytracingBasic,
                defines = HairDefines.RaytracingGBuffer,
                keywords = CoreKeywords.RaytracingGBufferForward,
                includes = CoreIncludes.Raytracing,
                requiredFields = new FieldCollection(){ HDFields.SubShader.Hair, HDFields.ShaderPass.RaytracingSubSurface },
            };
        }
#endregion

#region BlockMasks
        static class HairBlockMasks
        {
            public static BlockFieldDescriptor[] FragmentMETA = new BlockFieldDescriptor[]
            {
                BlockFields.SurfaceDescription.BaseColor,
                HDBlockFields.SurfaceDescription.SpecularOcclusion,
                BlockFields.SurfaceDescription.NormalTS,
                HDBlockFields.SurfaceDescription.BentNormal,
                HDBlockFields.SurfaceDescription.HairStrandDirection,
                HDBlockFields.SurfaceDescription.Transmittance,
                HDBlockFields.SurfaceDescription.RimTransmissionIntensity,
                BlockFields.SurfaceDescription.Smoothness,
                BlockFields.SurfaceDescription.Occlusion,
                BlockFields.SurfaceDescription.Emission,
                BlockFields.SurfaceDescription.Alpha,
                BlockFields.SurfaceDescription.AlphaClipThreshold,
                HDBlockFields.SurfaceDescription.SpecularAAScreenSpaceVariance,
                HDBlockFields.SurfaceDescription.SpecularAAThreshold,
                HDBlockFields.SurfaceDescription.SpecularTint,
                HDBlockFields.SurfaceDescription.SpecularShift,
                HDBlockFields.SurfaceDescription.SecondarySpecularTint,
                HDBlockFields.SurfaceDescription.SecondarySmoothness,
                HDBlockFields.SurfaceDescription.SecondarySpecularShift,
            };

            public static BlockFieldDescriptor[] FragmentShadowCaster = new BlockFieldDescriptor[]
            {
                BlockFields.SurfaceDescription.Alpha,
                BlockFields.SurfaceDescription.AlphaClipThreshold,
                HDBlockFields.SurfaceDescription.AlphaClipThresholdShadow,
                HDBlockFields.SurfaceDescription.DepthOffset,
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

            public static BlockFieldDescriptor[] FragmentTransparentDepthPrepass = new BlockFieldDescriptor[]
            {
                BlockFields.SurfaceDescription.Alpha,
                HDBlockFields.SurfaceDescription.AlphaClipThresholdDepthPrepass,
                HDBlockFields.SurfaceDescription.DepthOffset,
            };

            public static BlockFieldDescriptor[] FragmentTransparentBackface = new BlockFieldDescriptor[]
            {
                BlockFields.SurfaceDescription.BaseColor,
                HDBlockFields.SurfaceDescription.SpecularOcclusion,
                BlockFields.SurfaceDescription.NormalTS,
                HDBlockFields.SurfaceDescription.BentNormal,
                HDBlockFields.SurfaceDescription.HairStrandDirection,
                HDBlockFields.SurfaceDescription.Transmittance,
                HDBlockFields.SurfaceDescription.RimTransmissionIntensity,
                BlockFields.SurfaceDescription.Smoothness,
                BlockFields.SurfaceDescription.Occlusion,
                BlockFields.SurfaceDescription.Emission,
                BlockFields.SurfaceDescription.Alpha,
                BlockFields.SurfaceDescription.AlphaClipThreshold,
                HDBlockFields.SurfaceDescription.SpecularAAScreenSpaceVariance,
                HDBlockFields.SurfaceDescription.SpecularAAThreshold,
                HDBlockFields.SurfaceDescription.SpecularTint,
                HDBlockFields.SurfaceDescription.SpecularShift,
                HDBlockFields.SurfaceDescription.SecondarySpecularTint,
                HDBlockFields.SurfaceDescription.SecondarySmoothness,
                HDBlockFields.SurfaceDescription.SecondarySpecularShift,
                HDBlockFields.SurfaceDescription.DepthOffset,
            };

            public static BlockFieldDescriptor[] FragmentForward = new BlockFieldDescriptor[]
            {
                BlockFields.SurfaceDescription.BaseColor,
                HDBlockFields.SurfaceDescription.SpecularOcclusion,
                BlockFields.SurfaceDescription.NormalTS,
                HDBlockFields.SurfaceDescription.BentNormal,
                HDBlockFields.SurfaceDescription.HairStrandDirection,
                HDBlockFields.SurfaceDescription.Transmittance,
                HDBlockFields.SurfaceDescription.RimTransmissionIntensity,
                BlockFields.SurfaceDescription.Smoothness,
                BlockFields.SurfaceDescription.Occlusion,
                BlockFields.SurfaceDescription.Emission,
                BlockFields.SurfaceDescription.Alpha,
                BlockFields.SurfaceDescription.AlphaClipThreshold,
                HDBlockFields.SurfaceDescription.SpecularAAScreenSpaceVariance,
                HDBlockFields.SurfaceDescription.SpecularAAThreshold,
                HDBlockFields.SurfaceDescription.SpecularTint,
                HDBlockFields.SurfaceDescription.SpecularShift,
                HDBlockFields.SurfaceDescription.SecondarySpecularTint,
                HDBlockFields.SurfaceDescription.SecondarySmoothness,
                HDBlockFields.SurfaceDescription.SecondarySpecularShift,
                HDBlockFields.SurfaceDescription.BakedGI,
                HDBlockFields.SurfaceDescription.BakedBackGI,
                HDBlockFields.SurfaceDescription.DepthOffset,
            };

            public static BlockFieldDescriptor[] FragmentTransparentDepthPostpass = new BlockFieldDescriptor[]
            {
                BlockFields.SurfaceDescription.Alpha,
                HDBlockFields.SurfaceDescription.AlphaClipThresholdDepthPostpass,
                HDBlockFields.SurfaceDescription.DepthOffset,
            };
        }
#endregion

#region RenderStates
        static class HairRenderStates
        {
            public static RenderStateCollection MotionVectors = new RenderStateCollection
            {
                { RenderState.AlphaToMask(CoreRenderStates.Uniforms.alphaToMask), new FieldCondition(Fields.AlphaToMask, true) },
                { RenderState.Stencil(new StencilDescriptor()
                {
                    WriteMask = CoreRenderStates.Uniforms.stencilWriteMaskMV,
                    Ref = CoreRenderStates.Uniforms.stencilRefMV,
                    Comp = "Always",
                    Pass = "Replace",
                }) },
            };
        }
#endregion

#region Defines
        static class HairDefines
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
        static class HairIncludes
        {
            public static IncludeCollection Common = new IncludeCollection
            {
                { CoreIncludes.CorePregraph },
                { CoreIncludes.kNormalSurfaceGradient, IncludeLocation.Pregraph },
                { CoreIncludes.kHair, IncludeLocation.Pregraph },
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
                { CoreIncludes.kHair, IncludeLocation.Pregraph },
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
