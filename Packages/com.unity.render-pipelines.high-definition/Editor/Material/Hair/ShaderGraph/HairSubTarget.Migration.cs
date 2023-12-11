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
    sealed partial class HairSubTarget : LightingSubTarget, ILegacyTarget, IRequiresData<HairData>
    {
        public bool TryUpgradeFromMasterNode(IMasterNode1 masterNode, out Dictionary<BlockFieldDescriptor, int> blockMap)
        {
            blockMap = null;
            if (!(masterNode is HairMasterNode1 hairMasterNode))
                return false;

            m_MigrateFromOldSG = true;

            // Set data
            systemData.surfaceType = (SurfaceType)hairMasterNode.m_SurfaceType;
            systemData.blendingMode = HDSubShaderUtilities.UpgradeLegacyAlphaModeToBlendMode((int)hairMasterNode.m_AlphaMode);
            // Previous master node wasn't having any renderingPass. Assign it correctly now.
            systemData.renderQueueType = systemData.surfaceType == SurfaceType.Opaque ? HDRenderQueue.RenderQueueType.Opaque : HDRenderQueue.RenderQueueType.Transparent;
            systemData.alphaTest = hairMasterNode.m_AlphaTest;
            systemData.sortPriority = hairMasterNode.m_SortPriority;
            systemData.doubleSidedMode = hairMasterNode.m_DoubleSidedMode;
            systemData.transparentZWrite = hairMasterNode.m_ZWrite;
            systemData.transparentCullMode = hairMasterNode.m_transparentCullMode;
            systemData.zTest = hairMasterNode.m_ZTest;
            systemData.dotsInstancing = hairMasterNode.m_DOTSInstancing;
            systemData.materialNeedsUpdateHash = hairMasterNode.m_MaterialNeedsUpdateHash;

            builtinData.supportLodCrossFade = hairMasterNode.m_SupportLodCrossFade;
            builtinData.transparentDepthPrepass = hairMasterNode.m_AlphaTestDepthPrepass;
            builtinData.transparentDepthPostpass = hairMasterNode.m_AlphaTestDepthPostpass;
            builtinData.transparencyFog = hairMasterNode.m_TransparencyFog;
            builtinData.transparentWritesMotionVec = hairMasterNode.m_TransparentWritesMotionVec;
            builtinData.addPrecomputedVelocity = hairMasterNode.m_AddPrecomputedVelocity;
            builtinData.depthOffset = hairMasterNode.m_depthOffset;

            builtinData.alphaTestShadow = hairMasterNode.m_AlphaTestShadow;
            builtinData.backThenFrontRendering = hairMasterNode.m_BackThenFrontRendering;
            lightingData.blendPreserveSpecular = hairMasterNode.m_BlendPreserveSpecular;
            lightingData.receiveDecals = hairMasterNode.m_ReceiveDecals;
            lightingData.receiveSSR = hairMasterNode.m_ReceivesSSR;
            lightingData.receiveSSRTransparent = hairMasterNode.m_ReceivesSSRTransparent;
            lightingData.specularAA = hairMasterNode.m_SpecularAA;
            lightingData.specularOcclusionMode = hairMasterNode.m_SpecularOcclusionMode;
            lightingData.overrideBakedGI = hairMasterNode.m_overrideBakedGI;

            hairData.materialType = (HairData.MaterialType)hairMasterNode.m_MaterialType;
            hairData.geometryType = hairMasterNode.m_UseLightFacingNormal ? HairData.GeometryType.Strands : HairData.GeometryType.Cards;
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
                switch (slotMask)
                {
                    case HairMasterNode1.SlotMask.SpecularOcclusion:
                        return lightingData.specularOcclusionMode == SpecularOcclusionMode.Custom;
                    case HairMasterNode1.SlotMask.AlphaClipThreshold:
                        return systemData.alphaTest;
                    case HairMasterNode1.SlotMask.AlphaClipThresholdDepthPrepass:
                        return systemData.surfaceType == SurfaceType.Transparent && systemData.alphaTest && builtinData.transparentDepthPrepass;
                    case HairMasterNode1.SlotMask.AlphaClipThresholdDepthPostpass:
                        return systemData.surfaceType == SurfaceType.Transparent && systemData.alphaTest && builtinData.transparentDepthPostpass;
                    case HairMasterNode1.SlotMask.AlphaClipThresholdShadow:
                        return systemData.alphaTest && builtinData.alphaTestShadow;
                    default:
                        return true;
                }
            }

            // Set blockmap
            blockMap = new Dictionary<BlockFieldDescriptor, int>();
            foreach (HairMasterNode1.SlotMask slotMask in Enum.GetValues(typeof(HairMasterNode1.SlotMask)))
            {
                if (hairMasterNode.MaterialTypeUsesSlotMask(slotMask))
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
                blockMap.Add(HDBlockFields.SurfaceDescription.SpecularAAScreenSpaceVariance, HairMasterNode1.SpecularAAScreenSpaceVarianceSlotId);
                blockMap.Add(HDBlockFields.SurfaceDescription.SpecularAAThreshold, HairMasterNode1.SpecularAAThresholdSlotId);
            }

            // Override Baked GI
            if (lightingData.overrideBakedGI)
            {
                blockMap.Add(HDBlockFields.SurfaceDescription.BakedGI, HairMasterNode1.LightingSlotId);
                blockMap.Add(HDBlockFields.SurfaceDescription.BakedBackGI, HairMasterNode1.BackLightingSlotId);
            }

            // Depth Offset
            if (builtinData.depthOffset)
            {
                blockMap.Add(HDBlockFields.SurfaceDescription.DepthOffset, HairMasterNode1.DepthOffsetSlotId);
            }

            return true;
        }
    }
}
