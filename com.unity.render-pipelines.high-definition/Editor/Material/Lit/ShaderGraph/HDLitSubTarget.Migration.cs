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
    sealed partial class HDLitSubTarget : LightingSubTarget, ILegacyTarget, IRequiresData<HDLitData>
    {
        public bool TryUpgradeFromMasterNode(IMasterNode1 masterNode, out Dictionary<BlockFieldDescriptor, int> blockMap)
        {
            m_MigrateFromOldSG = true;

            blockMap = null;
            switch (masterNode)
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
            m_MigrateFromOldCrossPipelineSG = true;

            // Set data
            systemData.surfaceType = (SurfaceType)pbrMasterNode.m_SurfaceType;
            systemData.blendMode = HDSubShaderUtilities.UpgradeLegacyAlphaModeToBlendMode((int)pbrMasterNode.m_AlphaMode);
            systemData.doubleSidedMode = pbrMasterNode.m_TwoSided ? DoubleSidedMode.Enabled : DoubleSidedMode.Disabled;
            // Previous master node wasn't having any renderingPass. Assign it correctly now.
            systemData.renderQueueType = systemData.surfaceType == SurfaceType.Opaque ? HDRenderQueue.RenderQueueType.Opaque : HDRenderQueue.RenderQueueType.Transparent;
            systemData.dotsInstancing = false;
            systemData.alphaTest = HDSubShaderUtilities.UpgradeLegacyAlphaClip(pbrMasterNode);
            builtinData.addPrecomputedVelocity = false;
            lightingData.blendPreserveSpecular = false;
            lightingData.normalDropOffSpace = pbrMasterNode.m_NormalDropOffSpace;
            lightingData.receiveDecals = false;
            lightingData.receiveSSR = true;
            lightingData.receiveSSRTransparent = false;
            litData.materialType = pbrMasterNode.m_Model == PBRMasterNode1.Model.Specular ? HDLitData.MaterialType.SpecularColor : HDLitData.MaterialType.Standard;
            litData.energyConservingSpecular = false;
            litData.clearCoat = false;
            target.customEditorGUI = pbrMasterNode.m_OverrideEnabled ? pbrMasterNode.m_ShaderGUIOverride : "";
            // Handle mapping of Normal block specifically
            BlockFieldDescriptor normalBlock;
            switch (lightingData.normalDropOffSpace)
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
            if (litData.materialType == HDLitData.MaterialType.SpecularColor)
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
            systemData.renderQueueType = HDRenderQueue.MigrateRenderQueueToHDRP10(hdLitMasterNode.m_RenderingPass);
            if (systemData.renderQueueType == HDRenderQueue.RenderQueueType.PreRefraction && !hdLitMasterNode.m_DrawBeforeRefraction)
                systemData.renderQueueType = HDRenderQueue.RenderQueueType.Transparent;
            // Patch rendering pass in case the master node had an old configuration
            if (systemData.renderQueueType == HDRenderQueue.RenderQueueType.Background)
                systemData.renderQueueType = HDRenderQueue.RenderQueueType.Opaque;
            systemData.alphaTest = hdLitMasterNode.m_AlphaTest;
            systemData.sortPriority = hdLitMasterNode.m_SortPriority;
            systemData.doubleSidedMode = hdLitMasterNode.m_DoubleSidedMode;
            systemData.transparentZWrite = hdLitMasterNode.m_ZWrite;
            systemData.transparentCullMode = hdLitMasterNode.m_transparentCullMode;
            systemData.zTest = hdLitMasterNode.m_ZTest;
            systemData.dotsInstancing = hdLitMasterNode.m_DOTSInstancing;
            systemData.materialNeedsUpdateHash = hdLitMasterNode.m_MaterialNeedsUpdateHash;

            builtinData.transparentDepthPrepass = hdLitMasterNode.m_AlphaTestDepthPrepass;
            builtinData.transparentDepthPostpass = hdLitMasterNode.m_AlphaTestDepthPostpass;
            builtinData.supportLodCrossFade = hdLitMasterNode.m_SupportLodCrossFade;
            builtinData.transparencyFog = hdLitMasterNode.m_TransparencyFog;
            builtinData.distortion = hdLitMasterNode.m_Distortion;
            builtinData.distortionMode = hdLitMasterNode.m_DistortionMode;
            builtinData.distortionDepthTest = hdLitMasterNode.m_DistortionDepthTest;
            builtinData.transparentWritesMotionVec = hdLitMasterNode.m_TransparentWritesMotionVec;
            builtinData.addPrecomputedVelocity = hdLitMasterNode.m_AddPrecomputedVelocity;
            builtinData.depthOffset = hdLitMasterNode.m_depthOffset;
            builtinData.alphaToMask = hdLitMasterNode.m_AlphaToMask;

            builtinData.alphaTestShadow = hdLitMasterNode.m_AlphaTestShadow;
            builtinData.backThenFrontRendering = hdLitMasterNode.m_BackThenFrontRendering;
            lightingData.normalDropOffSpace = hdLitMasterNode.m_NormalDropOffSpace;
            lightingData.blendPreserveSpecular = hdLitMasterNode.m_BlendPreserveSpecular;
            lightingData.receiveDecals = hdLitMasterNode.m_ReceiveDecals;
            lightingData.receiveSSR = hdLitMasterNode.m_ReceivesSSR;
            lightingData.receiveSSRTransparent = hdLitMasterNode.m_ReceivesSSRTransparent;
            lightingData.specularAA = hdLitMasterNode.m_SpecularAA;
            lightingData.specularOcclusionMode = hdLitMasterNode.m_SpecularOcclusionMode;
            lightingData.overrideBakedGI = hdLitMasterNode.m_overrideBakedGI;
            HDLitData.MaterialType materialType = (HDLitData.MaterialType)hdLitMasterNode.m_MaterialType;

            litData.clearCoat = UpgradeCoatMask(hdLitMasterNode);
            litData.energyConservingSpecular = hdLitMasterNode.m_EnergyConservingSpecular;
            litData.rayTracing = hdLitMasterNode.m_RayTracing;
            litData.refractionModel = hdLitMasterNode.m_RefractionModel;
            litData.materialType = materialType;
            litData.sssTransmission = hdLitMasterNode.m_SSSTransmission;

            target.customEditorGUI = hdLitMasterNode.m_OverrideEnabled ? hdLitMasterNode.m_ShaderGUIOverride : "";

            // Handle mapping of Normal block specifically
            BlockFieldDescriptor normalBlock;
            BlockFieldDescriptor tangentBlock;
            switch (lightingData.normalDropOffSpace)
            {
                case NormalDropOffSpace.Object:
                    normalBlock = BlockFields.SurfaceDescription.NormalOS;
                    tangentBlock = HDBlockFields.SurfaceDescription.TangentOS;
                    break;
                case NormalDropOffSpace.World:
                    normalBlock = BlockFields.SurfaceDescription.NormalWS;
                    tangentBlock = HDBlockFields.SurfaceDescription.TangentWS;
                    break;
                default:
                    normalBlock = BlockFields.SurfaceDescription.NormalTS;
                    tangentBlock = HDBlockFields.SurfaceDescription.TangentTS;
                    break;
            }

            // Convert SlotMask to BlockMap entries
            var blockMapLookup = new Dictionary<HDLitMasterNode1.SlotMask, BlockFieldDescriptor>()
            {
                { HDLitMasterNode1.SlotMask.Albedo, BlockFields.SurfaceDescription.BaseColor },
                { HDLitMasterNode1.SlotMask.Normal, normalBlock },
                { HDLitMasterNode1.SlotMask.BentNormal, HDBlockFields.SurfaceDescription.BentNormal },
                { HDLitMasterNode1.SlotMask.Tangent, tangentBlock },
                { HDLitMasterNode1.SlotMask.Anisotropy, HDBlockFields.SurfaceDescription.Anisotropy },
                { HDLitMasterNode1.SlotMask.SubsurfaceMask, HDBlockFields.SurfaceDescription.SubsurfaceMask },
                { HDLitMasterNode1.SlotMask.Thickness, HDBlockFields.SurfaceDescription.Thickness },
                { HDLitMasterNode1.SlotMask.DiffusionProfile, HDBlockFields.SurfaceDescription.DiffusionProfileHash },
                { HDLitMasterNode1.SlotMask.IridescenceMask, HDBlockFields.SurfaceDescription.IridescenceMask },
                { HDLitMasterNode1.SlotMask.IridescenceLayerThickness, HDBlockFields.SurfaceDescription.IridescenceThickness },
                { HDLitMasterNode1.SlotMask.Specular, BlockFields.SurfaceDescription.Specular },
                { HDLitMasterNode1.SlotMask.CoatMask, BlockFields.SurfaceDescription.CoatMask },
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
                switch (slotMask)
                {
                    case HDLitMasterNode1.SlotMask.Thickness:
                        return litData.sssTransmission || litData.materialType == HDLitData.MaterialType.Translucent;
                    case HDLitMasterNode1.SlotMask.SpecularOcclusion:
                        return lightingData.specularOcclusionMode == SpecularOcclusionMode.Custom;
                    case HDLitMasterNode1.SlotMask.AlphaThreshold:
                        return systemData.alphaTest;
                    case HDLitMasterNode1.SlotMask.AlphaThresholdDepthPrepass:
                        return systemData.surfaceType == SurfaceType.Transparent && systemData.alphaTest && builtinData.transparentDepthPrepass;
                    case HDLitMasterNode1.SlotMask.AlphaThresholdDepthPostpass:
                        return systemData.surfaceType == SurfaceType.Transparent && systemData.alphaTest && builtinData.transparentDepthPostpass;
                    case HDLitMasterNode1.SlotMask.AlphaThresholdShadow:
                        return systemData.alphaTest && builtinData.alphaTestShadow;
                    default:
                        return true;
                }
            }

            bool UpgradeCoatMask(HDLitMasterNode1 masterNode)
            {
                var coatMaskSlotId = HDLitMasterNode1.CoatMaskSlotId;

                var node = masterNode as AbstractMaterialNode;
                var coatMaskSlot = node.FindSlot<Vector1MaterialSlot>(coatMaskSlotId);
                if (coatMaskSlot == null)
                    return false;

                coatMaskSlot.owner = node;
                return (coatMaskSlot.isConnected || coatMaskSlot.value > 0.0f);
            }

            // Set blockmap
            blockMap = new Dictionary<BlockFieldDescriptor, int>();

            // First handle vertex blocks. We ran out of SlotMask bits for VertexNormal and VertexTangent
            // so do all Vertex blocks here to maintain correct block order (Position is not in blockMapLookup)
            blockMap.Add(BlockFields.VertexDescription.Position, HDLitMasterNode1.PositionSlotId);
            blockMap.Add(BlockFields.VertexDescription.Normal, HDLitMasterNode1.VertexNormalSlotID);
            blockMap.Add(BlockFields.VertexDescription.Tangent, HDLitMasterNode1.VertexTangentSlotID);

            // Now handle the SlotMask cases
            foreach (HDLitMasterNode1.SlotMask slotMask in Enum.GetValues(typeof(HDLitMasterNode1.SlotMask)))
            {
                if (hdLitMasterNode.MaterialTypeUsesSlotMask(slotMask))
                {
                    if (!blockMapLookup.TryGetValue(slotMask, out var blockFieldDescriptor))
                        continue;

                    if (!AdditionalSlotMaskTests(slotMask))
                        continue;

                    var slotId = Mathf.Log((int)slotMask, 2);
                    blockMap.Add(blockFieldDescriptor, (int)slotId);
                }
            }

            // Specular AA
            if (lightingData.specularAA)
            {
                blockMap.Add(HDBlockFields.SurfaceDescription.SpecularAAScreenSpaceVariance, HDLitMasterNode1.SpecularAAScreenSpaceVarianceSlotId);
                blockMap.Add(HDBlockFields.SurfaceDescription.SpecularAAThreshold, HDLitMasterNode1.SpecularAAThresholdSlotId);
            }

            // Refraction
            bool hasRefraction = (systemData.surfaceType == SurfaceType.Transparent && systemData.renderQueueType != HDRenderQueue.RenderQueueType.PreRefraction && litData.refractionModel != ScreenSpaceRefraction.RefractionModel.None);
            if (hasRefraction)
            {
                if (!blockMap.TryGetValue(HDBlockFields.SurfaceDescription.Thickness, out _))
                {
                    blockMap.Add(HDBlockFields.SurfaceDescription.Thickness, HDLitMasterNode1.ThicknessSlotId);
                }

                blockMap.Add(HDBlockFields.SurfaceDescription.RefractionIndex, HDLitMasterNode1.RefractionIndexSlotId);
                blockMap.Add(HDBlockFields.SurfaceDescription.RefractionColor, HDLitMasterNode1.RefractionColorSlotId);
                blockMap.Add(HDBlockFields.SurfaceDescription.RefractionDistance, HDLitMasterNode1.RefractionDistanceSlotId);
            }

            // Distortion
            bool hasDistortion = (systemData.surfaceType == SurfaceType.Transparent && builtinData.distortion);
            if (hasDistortion)
            {
                blockMap.Add(HDBlockFields.SurfaceDescription.Distortion, HDLitMasterNode1.DistortionSlotId);
                blockMap.Add(HDBlockFields.SurfaceDescription.DistortionBlur, HDLitMasterNode1.DistortionBlurSlotId);
            }

            // Override Baked GI
            if (lightingData.overrideBakedGI)
            {
                blockMap.Add(HDBlockFields.SurfaceDescription.BakedGI, HDLitMasterNode1.LightingSlotId);
                blockMap.Add(HDBlockFields.SurfaceDescription.BakedBackGI, HDLitMasterNode1.BackLightingSlotId);
            }

            // Depth Offset (Removed from SlotMask because of missing bits)
            if (builtinData.depthOffset)
            {
                blockMap.Add(HDBlockFields.SurfaceDescription.DepthOffset, HDLitMasterNode1.DepthOffsetSlotId);
            }
        }
    }
}
