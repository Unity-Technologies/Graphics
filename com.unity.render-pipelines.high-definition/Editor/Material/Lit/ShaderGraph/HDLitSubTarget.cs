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
    sealed class HDLitSubTarget : SubTarget<HDTarget>, IHasMetadata, ILegacyTarget,
        IRequiresData<SystemData>, IRequiresData<BuiltinData>, IRequiresData<LightingData>, IRequiresData<HDLitData>
    {
        const string kAssetGuid = "caab952c840878340810cca27417971c";
        static string passTemplatePath => $"{HDUtils.GetHDRenderPipelinePath()}Editor/Material/Lit/ShaderGraph/LitPass.template";

        public HDLitSubTarget()
        {
            displayName = "Lit";
        }

        // Render State
        string renderType => HDRenderTypeTags.HDLitShader.ToString();
        string renderQueue
        {
            get
            {
                if(systemData.renderingPass == HDRenderQueue.RenderQueueType.Unknown)
                {
                    switch(systemData.surfaceType)
                    {
                        case SurfaceType.Opaque:
                            systemData.renderingPass = HDRenderQueue.RenderQueueType.Opaque;
                            break;
                        case SurfaceType.Transparent:
                        #pragma warning disable CS0618 // Type or member is obsolete
                            if (litData.drawBeforeRefraction)
                            {
                                litData.drawBeforeRefraction = false;
                        #pragma warning restore CS0618 // Type or member is obsolete
                                systemData.renderingPass = HDRenderQueue.RenderQueueType.PreRefraction;
                            }
                            else
                            {
                                systemData.renderingPass = HDRenderQueue.RenderQueueType.Transparent;
                            }
                            break;
                    }
                }
                int queue = HDRenderQueue.ChangeType(systemData.renderingPass, systemData.sortPriority, systemData.alphaTest);
                return HDRenderQueue.GetShaderTagValue(queue);
            }
        }

        // Material Data
        SystemData m_SystemData;
        BuiltinData m_BuiltinData;
        LightingData m_LightingData;
        HDLitData m_LitData;

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
        HDLitData IRequiresData<HDLitData>.data
        {
            get => m_LitData;
            set => m_LitData = value;
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
        public HDLitData litData
        {
            get => m_LitData;
            set => m_LitData = value;
        }

        public override bool IsActive() => true;

        public override void Setup(ref TargetSetupContext context)
        {
            context.AddAssetDependencyPath(AssetDatabase.GUIDToAssetPath(kAssetGuid));
            context.SetDefaultShaderGUI("Rendering.HighDefinition.HDLitGUI");

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
            bool hasRefraction = (systemData.surfaceType == SurfaceType.Transparent && systemData.renderingPass != HDRenderQueue.RenderQueueType.PreRefraction && litData.refractionModel != ScreenSpaceRefraction.RefractionModel.None);

            // Features
            context.AddField(Fields.LodCrossFade,                           systemData.supportLodCrossFade);

            // Surface Type
            context.AddField(Fields.SurfaceOpaque,                          systemData.surfaceType == SurfaceType.Opaque);
            context.AddField(Fields.SurfaceTransparent,                     systemData.surfaceType != SurfaceType.Opaque);

            // Structs
            context.AddField(HDStructFields.FragInputs.IsFrontFace,         systemData.doubleSidedMode != DoubleSidedMode.Disabled && !context.pass.Equals(HDLitSubTarget.LitPasses.MotionVectors));

            // Dots
            context.AddField(HDFields.DotsInstancing,                       systemData.dotsInstancing);
            context.AddField(HDFields.DotsProperties,                       context.hasDotsProperties);

            // Material
            context.AddField(HDFields.Anisotropy,                           litData.materialType == HDLitData.MaterialType.Anisotropy);
            context.AddField(HDFields.Iridescence,                          litData.materialType == HDLitData.MaterialType.Iridescence);
            context.AddField(HDFields.SpecularColor,                        litData.materialType == HDLitData.MaterialType.SpecularColor);
            context.AddField(HDFields.Standard,                             litData.materialType == HDLitData.MaterialType.Standard);
            context.AddField(HDFields.SubsurfaceScattering,                 litData.materialType == HDLitData.MaterialType.SubsurfaceScattering && systemData.surfaceType != SurfaceType.Transparent);
            context.AddField(HDFields.Transmission,                         (litData.materialType == HDLitData.MaterialType.SubsurfaceScattering && litData.sssTransmission) ||
                                                                                (litData.materialType == HDLitData.MaterialType.Translucent));
            context.AddField(HDFields.Translucent,                          litData.materialType == HDLitData.MaterialType.Translucent);

            // Blend Mode
            context.AddField(Fields.BlendAdd,                               systemData.surfaceType != SurfaceType.Opaque && systemData.blendMode == BlendMode.Additive);
            context.AddField(Fields.BlendAlpha,                             systemData.surfaceType != SurfaceType.Opaque && systemData.blendMode == BlendMode.Alpha);
            context.AddField(Fields.BlendPremultiply,                       systemData.surfaceType != SurfaceType.Opaque && systemData.blendMode == BlendMode.Premultiply);

            // Double Sided
            context.AddField(HDFields.DoubleSided,                          systemData.doubleSidedMode != DoubleSidedMode.Disabled);
            context.AddField(HDFields.DoubleSidedFlip,                      systemData.doubleSidedMode == DoubleSidedMode.FlippedNormals && !context.pass.Equals(HDLitSubTarget.LitPasses.MotionVectors));
            context.AddField(HDFields.DoubleSidedMirror,                    systemData.doubleSidedMode == DoubleSidedMode.MirroredNormals && !context.pass.Equals(HDLitSubTarget.LitPasses.MotionVectors));

            // Specular Occlusion
            context.AddField(HDFields.SpecularOcclusionFromAO,              lightingData.specularOcclusionMode == SpecularOcclusionMode.FromAO);
            context.AddField(HDFields.SpecularOcclusionFromAOBentNormal,    lightingData.specularOcclusionMode == SpecularOcclusionMode.FromAOAndBentNormal);
            context.AddField(HDFields.SpecularOcclusionCustom,              lightingData.specularOcclusionMode == SpecularOcclusionMode.Custom);

            // Distortion
            context.AddField(HDFields.DistortionDepthTest,                  builtinData.distortionDepthTest);
            context.AddField(HDFields.DistortionAdd,                        builtinData.distortionMode == DistortionMode.Add);
            context.AddField(HDFields.DistortionMultiply,                   builtinData.distortionMode == DistortionMode.Multiply);
            context.AddField(HDFields.DistortionReplace,                    builtinData.distortionMode == DistortionMode.Replace);
            context.AddField(HDFields.TransparentDistortion,                systemData.surfaceType != SurfaceType.Opaque && builtinData.distortion);

            // Refraction
            context.AddField(HDFields.Refraction,                           hasRefraction);
            context.AddField(HDFields.RefractionBox,                        hasRefraction && litData.refractionModel == ScreenSpaceRefraction.RefractionModel.Box);
            context.AddField(HDFields.RefractionSphere,                     hasRefraction && litData.refractionModel == ScreenSpaceRefraction.RefractionModel.Sphere);

            // Normal Drop Off Space
            context.AddField(Fields.NormalDropOffOS,                        lightingData.normalDropOffSpace == NormalDropOffSpace.Object);
            context.AddField(Fields.NormalDropOffTS,                        lightingData.normalDropOffSpace == NormalDropOffSpace.Tangent);
            context.AddField(Fields.NormalDropOffWS,                        lightingData.normalDropOffSpace == NormalDropOffSpace.World);


            // AlphaTest
            // We always generate the keyword ALPHATEST_ON
            context.AddField(Fields.AlphaTest,                              systemData.alphaTest && (context.pass.validPixelBlocks.Contains(BlockFields.SurfaceDescription.AlphaClipThreshold) || context.pass.validPixelBlocks.Contains(HDBlockFields.SurfaceDescription.AlphaClipThresholdShadow) ||
                                                                                context.pass.validPixelBlocks.Contains(HDBlockFields.SurfaceDescription.AlphaClipThresholdDepthPrepass) || context.pass.validPixelBlocks.Contains(HDBlockFields.SurfaceDescription.AlphaClipThresholdDepthPostpass)));
            // All the DoAlphaXXX field drive the generation of which code to use for alpha test in the template
            // Do alpha test only if we aren't using the TestShadow one
            context.AddField(HDFields.DoAlphaTest,                          systemData.alphaTest && (context.pass.validPixelBlocks.Contains(BlockFields.SurfaceDescription.AlphaClipThreshold) &&
                                                                                !(lightingData.alphaTestShadow && context.pass.validPixelBlocks.Contains(HDBlockFields.SurfaceDescription.AlphaClipThresholdShadow))));
            context.AddField(HDFields.DoAlphaTestShadow,                    systemData.alphaTest && lightingData.alphaTestShadow && context.pass.validPixelBlocks.Contains(HDBlockFields.SurfaceDescription.AlphaClipThresholdShadow));
            context.AddField(HDFields.DoAlphaTestPrepass,                   systemData.alphaTest && systemData.alphaTestDepthPrepass && context.pass.validPixelBlocks.Contains(HDBlockFields.SurfaceDescription.AlphaClipThresholdDepthPrepass));
            context.AddField(HDFields.DoAlphaTestPostpass,                  systemData.alphaTest && systemData.alphaTestDepthPostpass && context.pass.validPixelBlocks.Contains(HDBlockFields.SurfaceDescription.AlphaClipThresholdDepthPostpass));

            // Misc
            context.AddField(Fields.AlphaToMask,                            systemData.alphaTest && context.pass.validPixelBlocks.Contains(BlockFields.SurfaceDescription.AlphaClipThreshold) && builtinData.alphaToMask);
            context.AddField(HDFields.AlphaFog,                             systemData.surfaceType != SurfaceType.Opaque && builtinData.transparencyFog);
            context.AddField(HDFields.BlendPreserveSpecular,                systemData.surfaceType != SurfaceType.Opaque && lightingData.blendPreserveSpecular);
            context.AddField(HDFields.TransparentWritesMotionVec,           systemData.surfaceType != SurfaceType.Opaque && builtinData.transparentWritesMotionVec);
            context.AddField(HDFields.DisableDecals,                        !lightingData.receiveDecals);
            context.AddField(HDFields.DisableSSR,                           !lightingData.receiveSSR);
            context.AddField(HDFields.DisableSSRTransparent,                !litData.receiveSSRTransparent);
            context.AddField(Fields.VelocityPrecomputed,                    builtinData.addPrecomputedVelocity);
            context.AddField(HDFields.SpecularAA,                           lightingData.specularAA &&
                                                                                context.pass.validPixelBlocks.Contains(HDBlockFields.SurfaceDescription.SpecularAAThreshold) &&
                                                                                context.pass.validPixelBlocks.Contains(HDBlockFields.SurfaceDescription.SpecularAAScreenSpaceVariance));
            context.AddField(HDFields.EnergyConservingSpecular,             lightingData.energyConservingSpecular);
            context.AddField(HDFields.BentNormal,                           context.blocks.Contains(HDBlockFields.SurfaceDescription.BentNormal) && context.pass.validPixelBlocks.Contains(HDBlockFields.SurfaceDescription.BentNormal));
            context.AddField(HDFields.AmbientOcclusion,                     context.blocks.Contains(BlockFields.SurfaceDescription.Occlusion) && context.pass.validPixelBlocks.Contains(BlockFields.SurfaceDescription.Occlusion));
            context.AddField(HDFields.CoatMask,                             context.blocks.Contains(HDBlockFields.SurfaceDescription.CoatMask) && context.pass.validPixelBlocks.Contains(HDBlockFields.SurfaceDescription.CoatMask));
            context.AddField(HDFields.Tangent,                              context.blocks.Contains(HDBlockFields.SurfaceDescription.Tangent) && context.pass.validPixelBlocks.Contains(HDBlockFields.SurfaceDescription.Tangent));
            context.AddField(HDFields.LightingGI,                           context.blocks.Contains(HDBlockFields.SurfaceDescription.BakedGI) && context.pass.validPixelBlocks.Contains(HDBlockFields.SurfaceDescription.BakedGI));
            context.AddField(HDFields.BackLightingGI,                       context.blocks.Contains(HDBlockFields.SurfaceDescription.BakedBackGI) && context.pass.validPixelBlocks.Contains(HDBlockFields.SurfaceDescription.BakedBackGI));
            context.AddField(HDFields.DepthOffset,                          builtinData.depthOffset && context.pass.validPixelBlocks.Contains(HDBlockFields.SurfaceDescription.DepthOffset));
            context.AddField(HDFields.TransparentBackFace,                  systemData.surfaceType != SurfaceType.Opaque && lightingData.backThenFrontRendering);
            context.AddField(HDFields.TransparentDepthPrePass,              systemData.surfaceType != SurfaceType.Opaque && systemData.alphaTestDepthPrepass);
            context.AddField(HDFields.TransparentDepthPostPass,             systemData.surfaceType != SurfaceType.Opaque && systemData.alphaTestDepthPrepass);
            context.AddField(HDFields.RayTracing,                           litData.rayTracing);
        }

        public override void GetActiveBlocks(ref TargetActiveBlockContext context)
        {
            bool hasRefraction = (systemData.surfaceType == SurfaceType.Transparent && systemData.renderingPass != HDRenderQueue.RenderQueueType.PreRefraction && litData.refractionModel != ScreenSpaceRefraction.RefractionModel.None);
            bool hasDistortion = (systemData.surfaceType == SurfaceType.Transparent && builtinData.distortion);

            // Vertex
            context.AddBlock(BlockFields.VertexDescription.Position);
            context.AddBlock(BlockFields.VertexDescription.Normal);
            context.AddBlock(BlockFields.VertexDescription.Tangent);

            // Common
            context.AddBlock(BlockFields.SurfaceDescription.BaseColor);
            context.AddBlock(HDBlockFields.SurfaceDescription.BentNormal);
            context.AddBlock(HDBlockFields.SurfaceDescription.CoatMask);
            context.AddBlock(BlockFields.SurfaceDescription.Emission);
            context.AddBlock(BlockFields.SurfaceDescription.Smoothness);
            context.AddBlock(BlockFields.SurfaceDescription.Occlusion);
            context.AddBlock(BlockFields.SurfaceDescription.Alpha);

            // Alpha Test
            context.AddBlock(BlockFields.SurfaceDescription.AlphaClipThreshold,     systemData.alphaTest);
            context.AddBlock(HDBlockFields.SurfaceDescription.AlphaClipThresholdDepthPrepass, systemData.surfaceType == SurfaceType.Transparent && systemData.alphaTest && systemData.alphaTestDepthPrepass);
            context.AddBlock(HDBlockFields.SurfaceDescription.AlphaClipThresholdDepthPostpass, systemData.surfaceType == SurfaceType.Transparent && systemData.alphaTest && systemData.alphaTestDepthPostpass);
            context.AddBlock(HDBlockFields.SurfaceDescription.AlphaClipThresholdShadow, systemData.alphaTest && lightingData.alphaTestShadow);

            // Specular AA
            context.AddBlock(HDBlockFields.SurfaceDescription.SpecularAAScreenSpaceVariance, lightingData.specularAA);
            context.AddBlock(HDBlockFields.SurfaceDescription.SpecularAAThreshold,  lightingData.specularAA);

            // Refraction
            context.AddBlock(HDBlockFields.SurfaceDescription.RefractionIndex,      hasRefraction);
            context.AddBlock(HDBlockFields.SurfaceDescription.RefractionColor,      hasRefraction);
            context.AddBlock(HDBlockFields.SurfaceDescription.RefractionDistance,   hasRefraction);

            // Distortion
            context.AddBlock(HDBlockFields.SurfaceDescription.Distortion,           hasDistortion);
            context.AddBlock(HDBlockFields.SurfaceDescription.DistortionBlur,       hasDistortion);

            // Baked GI
            context.AddBlock(HDBlockFields.SurfaceDescription.BakedGI,              lightingData.overrideBakedGI);
            context.AddBlock(HDBlockFields.SurfaceDescription.BakedBackGI,          lightingData.overrideBakedGI);

            // Normal
            context.AddBlock(BlockFields.SurfaceDescription.NormalOS,               lightingData.normalDropOffSpace == NormalDropOffSpace.Object);
            context.AddBlock(BlockFields.SurfaceDescription.NormalTS,               lightingData.normalDropOffSpace == NormalDropOffSpace.Tangent);
            context.AddBlock(BlockFields.SurfaceDescription.NormalWS,               lightingData.normalDropOffSpace == NormalDropOffSpace.World);

            // Material
            context.AddBlock(HDBlockFields.SurfaceDescription.Tangent,              litData.materialType == HDLitData.MaterialType.Anisotropy);
            context.AddBlock(HDBlockFields.SurfaceDescription.Anisotropy,           litData.materialType == HDLitData.MaterialType.Anisotropy);
            context.AddBlock(HDBlockFields.SurfaceDescription.SubsurfaceMask,       litData.materialType == HDLitData.MaterialType.SubsurfaceScattering);
            context.AddBlock(HDBlockFields.SurfaceDescription.Thickness,            ((litData.materialType == HDLitData.MaterialType.SubsurfaceScattering || litData.materialType == HDLitData.MaterialType.Translucent) &&
                                                                                        (litData.sssTransmission || litData.materialType == HDLitData.MaterialType.Translucent)) || hasRefraction);
            context.AddBlock(HDBlockFields.SurfaceDescription.DiffusionProfileHash, litData.materialType == HDLitData.MaterialType.SubsurfaceScattering || litData.materialType == HDLitData.MaterialType.Translucent);
            context.AddBlock(HDBlockFields.SurfaceDescription.IridescenceMask,      litData.materialType == HDLitData.MaterialType.Iridescence);
            context.AddBlock(HDBlockFields.SurfaceDescription.IridescenceThickness, litData.materialType == HDLitData.MaterialType.Iridescence);
            context.AddBlock(BlockFields.SurfaceDescription.Specular,               litData.materialType == HDLitData.MaterialType.SpecularColor);
            context.AddBlock(BlockFields.SurfaceDescription.Metallic,               litData.materialType == HDLitData.MaterialType.Standard || 
                                                                                        litData.materialType == HDLitData.MaterialType.Anisotropy ||
                                                                                        litData.materialType == HDLitData.MaterialType.Iridescence);

            // Misc
            context.AddBlock(HDBlockFields.SurfaceDescription.SpecularOcclusion,    lightingData.specularOcclusionMode == SpecularOcclusionMode.Custom);
            context.AddBlock(HDBlockFields.SurfaceDescription.DepthOffset,          builtinData.depthOffset);
        }

        public override void GetPropertiesGUI(ref TargetPropertyGUIContext context, Action onChange, Action<String> registerUndo)
        {
            var settingsView = new HDLitSettingsView(this);
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
            // ShaderGraph only property used to send the RenderQueueType to the material
            collector.AddShaderProperty(new Vector1ShaderProperty
            {
                overrideReferenceName = "_RenderQueueType",
                hidden = true,
                value = (int)systemData.renderingPass,
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
            HDSubShaderUtilities.AddStencilShaderProperties(collector, litData.materialType == HDLitData.MaterialType.SubsurfaceScattering, lightingData.receiveSSR, litData.receiveSSRTransparent);
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
            HDSubShaderUtilities.AddRayTracingProperty(collector, litData.rayTracing);
        }

        public override void ProcessPreviewMaterial(Material material)
        {
            // Fixup the material settings:
            material.SetFloat(kSurfaceType, (int)systemData.surfaceType);
            material.SetFloat(kDoubleSidedNormalMode, (int)systemData.doubleSidedMode);
            material.SetFloat(kAlphaCutoffEnabled, systemData.alphaTest ? 1 : 0);
            material.SetFloat(kBlendMode, (int)systemData.blendMode);
            material.SetFloat(kEnableFogOnTransparent, builtinData.transparencyFog ? 1.0f : 0.0f);
            material.SetFloat(kZTestTransparent, (int)systemData.zTest);
            material.SetFloat(kTransparentCullMode, (int)systemData.transparentCullMode);
            material.SetFloat(kZWrite, systemData.zWrite ? 1.0f : 0.0f);

            // No sorting priority for shader graph preview
            material.renderQueue = (int)HDRenderQueue.ChangeType(systemData.renderingPass, offset: 0, alphaTest: systemData.alphaTest);

            HDLitGUI.SetupMaterialKeywordsAndPass(material);
        }

        int ComputeMaterialNeedsUpdateHash()
        {
            int hash = 0;
            hash |= (systemData.alphaTest ? 0 : 1) << 0;
            hash |= (lightingData.alphaTestShadow ? 0 : 1) << 1;
            hash |= (lightingData.receiveSSR ? 0 : 1) << 2;
            hash |= (litData.receiveSSRTransparent ? 0 : 1) << 3;
            hash |= (litData.materialType == HDLitData.MaterialType.SubsurfaceScattering ? 0 : 1) << 4;
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
        public string identifier => "HDLitSubTarget";

        public ScriptableObject GetMetadataObject()
        {
            var hdMetadata = ScriptableObject.CreateInstance<HDMetadata>();
            hdMetadata.shaderID = HDShaderUtils.ShaderID.SG_Lit;
            return hdMetadata;
        }

        public bool TryUpgradeFromMasterNode(IMasterNode1 masterNode, out Dictionary<BlockFieldDescriptor, int> blockMap)
        {
            blockMap = null;
            switch(masterNode)
            {
                case PBRMasterNode1 pbrMasterNode:
                    UpgradePBRMasterNode(pbrMasterNode, out blockMap);
                    return true;
                case HDLitMasterNode1 hdLitMasterNode:
                    UpgradeHDLitMasterNode(hdLitMasterNode, out blockMap);
                    return true;
                default:
                    return false;
            }
        }

        void UpgradePBRMasterNode(PBRMasterNode1 pbrMasterNode, out Dictionary<BlockFieldDescriptor, int> blockMap)
        {
            // Set data
            systemData.surfaceType = (SurfaceType)pbrMasterNode.m_SurfaceType;
            systemData.blendMode = HDSubShaderUtilities.UpgradeLegacyAlphaModeToBlendMode((int)pbrMasterNode.m_AlphaMode);
            systemData.doubleSidedMode = pbrMasterNode.m_TwoSided ? DoubleSidedMode.Enabled : DoubleSidedMode.Disabled;
            systemData.alphaTest = HDSubShaderUtilities.UpgradeLegacyAlphaClip(pbrMasterNode);
            systemData.dotsInstancing = pbrMasterNode.m_DOTSInstancing;
            builtinData.addPrecomputedVelocity = false;
            lightingData.normalDropOffSpace = pbrMasterNode.m_NormalDropOffSpace;
            litData.materialType = pbrMasterNode.m_Model == PBRMasterNode1.Model.Specular ? HDLitData.MaterialType.SpecularColor : HDLitData.MaterialType.Standard;
            target.customEditorGUI = pbrMasterNode.m_OverrideEnabled ? pbrMasterNode.m_ShaderGUIOverride : "";

            // Handle mapping of Normal block specifically
            BlockFieldDescriptor normalBlock;
            switch(lightingData.normalDropOffSpace)
            {
                case NormalDropOffSpace.Object:
                    normalBlock = BlockFields.SurfaceDescription.NormalOS;
                    break;
                case NormalDropOffSpace.World:
                    normalBlock = BlockFields.SurfaceDescription.NormalWS;
                    break;
                default:
                    normalBlock = BlockFields.SurfaceDescription.NormalTS;
                    break;
            }

            // PBRMasterNode adds/removes Metallic/Specular based on settings
            BlockFieldDescriptor specularMetallicBlock;
            int specularMetallicId;
            if(litData.materialType == HDLitData.MaterialType.SpecularColor)
            {
                specularMetallicBlock = BlockFields.SurfaceDescription.Specular;
                specularMetallicId = 3;
            }
            else
            {
                specularMetallicBlock = BlockFields.SurfaceDescription.Metallic;
                specularMetallicId = 2;
            }

            // Set blockmap
            blockMap = new Dictionary<BlockFieldDescriptor, int>()
            {
                { BlockFields.VertexDescription.Position, 9 },
                { BlockFields.VertexDescription.Normal, 10 },
                { BlockFields.VertexDescription.Tangent, 11 },
                { BlockFields.SurfaceDescription.BaseColor, 0 },
                { normalBlock, 1 },
                { specularMetallicBlock, specularMetallicId },
                { BlockFields.SurfaceDescription.Emission, 4 },
                { BlockFields.SurfaceDescription.Smoothness, 5 },
                { BlockFields.SurfaceDescription.Occlusion, 6 },
                { BlockFields.SurfaceDescription.Alpha, 7 },
                { BlockFields.SurfaceDescription.AlphaClipThreshold, 8 },
            };
        }

        void UpgradeHDLitMasterNode(HDLitMasterNode1 hdLitMasterNode, out Dictionary<BlockFieldDescriptor, int> blockMap)
        {
            // Set data
            systemData.surfaceType = (SurfaceType)hdLitMasterNode.m_SurfaceType;
            systemData.blendMode = HDSubShaderUtilities.UpgradeLegacyAlphaModeToBlendMode((int)hdLitMasterNode.m_AlphaMode);
            systemData.renderingPass = hdLitMasterNode.m_RenderingPass;
            systemData.alphaTest = hdLitMasterNode.m_AlphaTest;
            systemData.alphaTestDepthPrepass = hdLitMasterNode.m_AlphaTestDepthPrepass;
            systemData.alphaTestDepthPostpass = hdLitMasterNode.m_AlphaTestDepthPostpass;
            systemData.sortPriority = hdLitMasterNode.m_SortPriority;
            systemData.doubleSidedMode = hdLitMasterNode.m_DoubleSidedMode;
            systemData.zWrite = hdLitMasterNode.m_ZWrite;
            systemData.transparentCullMode = hdLitMasterNode.m_transparentCullMode;
            systemData.zTest = hdLitMasterNode.m_ZTest;
            systemData.supportLodCrossFade = hdLitMasterNode.m_SupportLodCrossFade;
            systemData.dotsInstancing = hdLitMasterNode.m_DOTSInstancing;
            systemData.materialNeedsUpdateHash = hdLitMasterNode.m_MaterialNeedsUpdateHash;

            builtinData.transparencyFog = hdLitMasterNode.m_TransparencyFog;
            builtinData.distortion = hdLitMasterNode.m_Distortion;
            builtinData.distortionMode = hdLitMasterNode.m_DistortionMode;
            builtinData.distortionDepthTest = hdLitMasterNode.m_DistortionDepthTest;
            builtinData.transparentWritesMotionVec = hdLitMasterNode.m_TransparentWritesMotionVec;
            builtinData.addPrecomputedVelocity = hdLitMasterNode.m_AddPrecomputedVelocity;
            builtinData.depthOffset = hdLitMasterNode.m_depthOffset;
            builtinData.alphaToMask = hdLitMasterNode.m_AlphaToMask;

            lightingData.alphaTestShadow = hdLitMasterNode.m_AlphaTestShadow;
            lightingData.backThenFrontRendering = hdLitMasterNode.m_BackThenFrontRendering;
            lightingData.normalDropOffSpace = hdLitMasterNode.m_NormalDropOffSpace;
            lightingData.blendPreserveSpecular = hdLitMasterNode.m_BlendPreserveSpecular;
            lightingData.receiveDecals = hdLitMasterNode.m_ReceiveDecals;
            lightingData.receiveSSR = hdLitMasterNode.m_ReceivesSSR;
            lightingData.energyConservingSpecular = hdLitMasterNode.m_EnergyConservingSpecular;
            lightingData.specularAA = hdLitMasterNode.m_SpecularAA;
            lightingData.specularOcclusionMode = hdLitMasterNode.m_SpecularOcclusionMode;
            lightingData.overrideBakedGI = hdLitMasterNode.m_overrideBakedGI;
            
            litData.rayTracing = hdLitMasterNode.m_RayTracing;
            litData.refractionModel = hdLitMasterNode.m_RefractionModel; 
            litData.materialType = (HDLitData.MaterialType)hdLitMasterNode.m_MaterialType;
            litData.sssTransmission = hdLitMasterNode.m_SSSTransmission;
            litData.receiveSSRTransparent = hdLitMasterNode.m_ReceivesSSRTransparent;
            
            target.customEditorGUI = hdLitMasterNode.m_OverrideEnabled ? hdLitMasterNode.m_ShaderGUIOverride : "";

            // Handle mapping of Normal block specifically
            BlockFieldDescriptor normalBlock;
            switch(lightingData.normalDropOffSpace)
            {
                case NormalDropOffSpace.Object:
                    normalBlock = BlockFields.SurfaceDescription.NormalOS;
                    break;
                case NormalDropOffSpace.World:
                    normalBlock = BlockFields.SurfaceDescription.NormalWS;
                    break;
                default:
                    normalBlock = BlockFields.SurfaceDescription.NormalTS;
                    break;
            }

            // Convert SlotMask to BlockMap entries
            var blockMapLookup = new Dictionary<HDLitMasterNode1.SlotMask, BlockFieldDescriptor>()
            {
                { HDLitMasterNode1.SlotMask.Albedo, BlockFields.SurfaceDescription.BaseColor },
                { HDLitMasterNode1.SlotMask.Normal, normalBlock },
                { HDLitMasterNode1.SlotMask.BentNormal, HDBlockFields.SurfaceDescription.BentNormal },
                { HDLitMasterNode1.SlotMask.Tangent, HDBlockFields.SurfaceDescription.Tangent },
                { HDLitMasterNode1.SlotMask.Anisotropy, HDBlockFields.SurfaceDescription.Anisotropy },
                { HDLitMasterNode1.SlotMask.SubsurfaceMask, HDBlockFields.SurfaceDescription.SubsurfaceMask },
                { HDLitMasterNode1.SlotMask.Thickness, HDBlockFields.SurfaceDescription.Thickness },
                { HDLitMasterNode1.SlotMask.DiffusionProfile, HDBlockFields.SurfaceDescription.DiffusionProfileHash },
                { HDLitMasterNode1.SlotMask.IridescenceMask, HDBlockFields.SurfaceDescription.IridescenceMask },
                { HDLitMasterNode1.SlotMask.IridescenceLayerThickness, HDBlockFields.SurfaceDescription.IridescenceThickness },
                { HDLitMasterNode1.SlotMask.Specular, BlockFields.SurfaceDescription.Specular },
                { HDLitMasterNode1.SlotMask.CoatMask, HDBlockFields.SurfaceDescription.CoatMask },
                { HDLitMasterNode1.SlotMask.Metallic, BlockFields.SurfaceDescription.Metallic },
                { HDLitMasterNode1.SlotMask.Smoothness, BlockFields.SurfaceDescription.Smoothness },
                { HDLitMasterNode1.SlotMask.Occlusion, BlockFields.SurfaceDescription.Occlusion },
                { HDLitMasterNode1.SlotMask.SpecularOcclusion, HDBlockFields.SurfaceDescription.SpecularOcclusion },
                { HDLitMasterNode1.SlotMask.Emission, BlockFields.SurfaceDescription.Emission },
                { HDLitMasterNode1.SlotMask.Alpha, BlockFields.SurfaceDescription.Alpha },
                { HDLitMasterNode1.SlotMask.AlphaThreshold, BlockFields.SurfaceDescription.AlphaClipThreshold },
                { HDLitMasterNode1.SlotMask.AlphaThresholdDepthPrepass, HDBlockFields.SurfaceDescription.AlphaClipThresholdDepthPrepass },
                { HDLitMasterNode1.SlotMask.AlphaThresholdDepthPostpass, HDBlockFields.SurfaceDescription.AlphaClipThresholdDepthPostpass },
                { HDLitMasterNode1.SlotMask.AlphaThresholdShadow, HDBlockFields.SurfaceDescription.AlphaClipThresholdShadow },
            };

            // Legacy master node slots have additional slot conditions, test them here
            bool AdditionalSlotMaskTests(HDLitMasterNode1.SlotMask slotMask)
            {
                switch(slotMask)
                {
                    case HDLitMasterNode1.SlotMask.Thickness:
                        return litData.sssTransmission || litData.materialType == HDLitData.MaterialType.Translucent;
                    case HDLitMasterNode1.SlotMask.SpecularOcclusion:
                        return lightingData.specularOcclusionMode == SpecularOcclusionMode.Custom;
                    case HDLitMasterNode1.SlotMask.AlphaThreshold:
                        return systemData.alphaTest;
                    case HDLitMasterNode1.SlotMask.AlphaThresholdDepthPrepass:
                        return systemData.surfaceType == SurfaceType.Transparent && systemData.alphaTest && systemData.alphaTestDepthPrepass;
                    case HDLitMasterNode1.SlotMask.AlphaThresholdDepthPostpass:
                        return systemData.surfaceType == SurfaceType.Transparent && systemData.alphaTest && systemData.alphaTestDepthPostpass;
                    case HDLitMasterNode1.SlotMask.AlphaThresholdShadow:
                        return systemData.alphaTest && lightingData.alphaTestShadow;
                    default:
                        return true;
                }
            }

            // Set blockmap
            blockMap = new Dictionary<BlockFieldDescriptor, int>();

            // First handle vertex blocks. We ran out of SlotMask bits for VertexNormal and VertexTangent
            // so do all Vertex blocks here to maintain correct block order (Position is not in blockMapLookup)
            blockMap.Add(BlockFields.VertexDescription.Position, HDLitMasterNode1.PositionSlotId);
            blockMap.Add(BlockFields.VertexDescription.Normal, HDLitMasterNode1.VertexNormalSlotID);
            blockMap.Add(BlockFields.VertexDescription.Tangent, HDLitMasterNode1.VertexTangentSlotID);

            // Now handle the SlotMask cases
            foreach(HDLitMasterNode1.SlotMask slotMask in Enum.GetValues(typeof(HDLitMasterNode1.SlotMask)))
            {
                if(hdLitMasterNode.MaterialTypeUsesSlotMask(slotMask))
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
                blockMap.Add(HDBlockFields.SurfaceDescription.SpecularAAScreenSpaceVariance, HDLitMasterNode1.SpecularAAScreenSpaceVarianceSlotId);
                blockMap.Add(HDBlockFields.SurfaceDescription.SpecularAAThreshold, HDLitMasterNode1.SpecularAAThresholdSlotId);
            }

            // Refraction
            bool hasRefraction = (systemData.surfaceType == SurfaceType.Transparent && systemData.renderingPass != HDRenderQueue.RenderQueueType.PreRefraction && litData.refractionModel != ScreenSpaceRefraction.RefractionModel.None);
            if(hasRefraction)
            {
                if(!blockMap.TryGetValue(HDBlockFields.SurfaceDescription.Thickness, out _))
                {
                    blockMap.Add(HDBlockFields.SurfaceDescription.Thickness, HDLitMasterNode1.ThicknessSlotId);
                }

                blockMap.Add(HDBlockFields.SurfaceDescription.RefractionIndex, HDLitMasterNode1.RefractionIndexSlotId);
                blockMap.Add(HDBlockFields.SurfaceDescription.RefractionColor, HDLitMasterNode1.RefractionColorSlotId);
                blockMap.Add(HDBlockFields.SurfaceDescription.RefractionDistance, HDLitMasterNode1.RefractionDistanceSlotId);
            }

            // Distortion
            bool hasDistortion = (systemData.surfaceType == SurfaceType.Transparent && builtinData.distortion);
            if(hasDistortion)
            {
                blockMap.Add(HDBlockFields.SurfaceDescription.Distortion, HDLitMasterNode1.DistortionSlotId);
                blockMap.Add(HDBlockFields.SurfaceDescription.DistortionBlur, HDLitMasterNode1.DistortionBlurSlotId);
            }

            // Override Baked GI
            if(lightingData.overrideBakedGI)
            {
                blockMap.Add(HDBlockFields.SurfaceDescription.BakedGI, HDLitMasterNode1.LightingSlotId);
                blockMap.Add(HDBlockFields.SurfaceDescription.BakedBackGI, HDLitMasterNode1.BackLightingSlotId);
            }

            // Depth Offset (Removed from SlotMask because of missing bits)
            if(builtinData.depthOffset)
            {
                blockMap.Add(HDBlockFields.SurfaceDescription.DepthOffset, HDLitMasterNode1.DepthOffsetSlotId);
            }
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
                validVertexBlocks = CoreBlockMasks.Vertex,
                validPixelBlocks = LitBlockMasks.FragmentDefault,

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
                validPixelBlocks = LitBlockMasks.FragmentMeta,

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
                validVertexBlocks = CoreBlockMasks.Vertex,
                validPixelBlocks = LitBlockMasks.FragmentShadowCaster,

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
                validVertexBlocks = CoreBlockMasks.Vertex,
                validPixelBlocks = LitBlockMasks.FragmentSceneSelection,

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
                validVertexBlocks = CoreBlockMasks.Vertex,
                validPixelBlocks = LitBlockMasks.FragmentDepthMotionVectors,

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
                validVertexBlocks = CoreBlockMasks.Vertex,
                validPixelBlocks = LitBlockMasks.FragmentDepthMotionVectors,

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
                validVertexBlocks = CoreBlockMasks.Vertex,
                validPixelBlocks = LitBlockMasks.FragmentDistortion,

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
                validVertexBlocks = CoreBlockMasks.Vertex,
                validPixelBlocks = LitBlockMasks.FragmentTransparentDepthPrepass,

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
                validVertexBlocks = CoreBlockMasks.Vertex,
                validPixelBlocks = LitBlockMasks.FragmentTransparentBackface,

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
                validVertexBlocks = CoreBlockMasks.Vertex,
                validPixelBlocks = LitBlockMasks.FragmentDefault,

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
                validVertexBlocks = CoreBlockMasks.Vertex,
                validPixelBlocks = LitBlockMasks.FragmentTransparentDepthPostpass,

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
                validVertexBlocks = CoreBlockMasks.Vertex,
                validPixelBlocks = LitBlockMasks.FragmentRayTracingPrepass,

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
                validVertexBlocks = CoreBlockMasks.Vertex,
                validPixelBlocks = LitBlockMasks.FragmentDefault,

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
                validVertexBlocks = CoreBlockMasks.Vertex,
                validPixelBlocks = LitBlockMasks.FragmentDefault,

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
                validVertexBlocks = CoreBlockMasks.Vertex,
                validPixelBlocks = LitBlockMasks.FragmentDefault,

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
                validVertexBlocks = CoreBlockMasks.Vertex,
                validPixelBlocks = LitBlockMasks.FragmentDefault,

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
                validVertexBlocks = CoreBlockMasks.Vertex,
                validPixelBlocks = LitBlockMasks.FragmentDefault,

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
                validVertexBlocks = CoreBlockMasks.Vertex,
                validPixelBlocks = LitBlockMasks.FragmentDefault,

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

#region BlockMasks
        static class LitBlockMasks
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
                { RenderState.BlendOp(BlendOp.Add, BlendOp.Add) },
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
                { CoreKeywordDescriptors.AlphaToMask, new FieldCondition(Fields.AlphaToMask, true) },
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
