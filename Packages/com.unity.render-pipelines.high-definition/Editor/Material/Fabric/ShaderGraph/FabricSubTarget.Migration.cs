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
    sealed partial class FabricSubTarget : LightingSubTarget, ILegacyTarget, IRequiresData<FabricData>
    {
        public bool TryUpgradeFromMasterNode(IMasterNode1 masterNode, out Dictionary<BlockFieldDescriptor, int> blockMap)
        {
            blockMap = null;
            if (!(masterNode is FabricMasterNode1 fabricMasterNode))
                return false;

            m_MigrateFromOldSG = true;

            // Set data
            systemData.surfaceType = (SurfaceType)fabricMasterNode.m_SurfaceType;
            systemData.blendMode = HDSubShaderUtilities.UpgradeLegacyAlphaModeToBlendMode((int)fabricMasterNode.m_AlphaMode);
            // Previous master node wasn't having any renderingPass. Assign it correctly now.
            systemData.renderQueueType = systemData.surfaceType == SurfaceType.Opaque ? HDRenderQueue.RenderQueueType.Opaque : HDRenderQueue.RenderQueueType.Transparent;
            systemData.alphaTest = fabricMasterNode.m_AlphaTest;
            systemData.sortPriority = fabricMasterNode.m_SortPriority;
            systemData.doubleSidedMode = fabricMasterNode.m_DoubleSidedMode;
            systemData.transparentZWrite = fabricMasterNode.m_ZWrite;
            systemData.transparentCullMode = fabricMasterNode.m_transparentCullMode;
            systemData.zTest = fabricMasterNode.m_ZTest;
            systemData.dotsInstancing = fabricMasterNode.m_DOTSInstancing;
            systemData.materialNeedsUpdateHash = fabricMasterNode.m_MaterialNeedsUpdateHash;

            builtinData.supportLodCrossFade = fabricMasterNode.m_SupportLodCrossFade;
            builtinData.transparencyFog = fabricMasterNode.m_TransparencyFog;
            builtinData.addPrecomputedVelocity = fabricMasterNode.m_AddPrecomputedVelocity;
            builtinData.depthOffset = fabricMasterNode.m_depthOffset;

            lightingData.blendPreserveSpecular = fabricMasterNode.m_BlendPreserveSpecular;
            lightingData.receiveDecals = fabricMasterNode.m_ReceiveDecals;
            lightingData.receiveSSR = fabricMasterNode.m_ReceivesSSR;
            lightingData.receiveSSRTransparent = fabricMasterNode.m_ReceivesSSRTransparent;
            lightingData.specularOcclusionMode = fabricMasterNode.m_SpecularOcclusionMode;
            lightingData.overrideBakedGI = fabricMasterNode.m_overrideBakedGI;

            fabricData.subsurfaceScattering = fabricMasterNode.m_SubsurfaceScattering;
            fabricData.transmission = fabricMasterNode.m_Transmission;
            fabricData.energyConservingSpecular = fabricMasterNode.m_EnergyConservingSpecular;
            fabricData.materialType = (FabricData.MaterialType)fabricMasterNode.m_MaterialType;
            target.customEditorGUI = fabricMasterNode.m_OverrideEnabled ? fabricMasterNode.m_ShaderGUIOverride : "";


            BlockFieldDescriptor tangentBlock;
            switch (lightingData.normalDropOffSpace)
            {
                case NormalDropOffSpace.Object:
                    tangentBlock = HDBlockFields.SurfaceDescription.TangentOS;
                    break;
                case NormalDropOffSpace.World:
                    tangentBlock = HDBlockFields.SurfaceDescription.TangentWS;
                    break;
                default:
                    tangentBlock = HDBlockFields.SurfaceDescription.TangentTS;
                    break;
            }


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
                { FabricMasterNode1.SlotMask.Tangent, tangentBlock },
                { FabricMasterNode1.SlotMask.Anisotropy, HDBlockFields.SurfaceDescription.Anisotropy },
                { FabricMasterNode1.SlotMask.Emission, BlockFields.SurfaceDescription.Emission },
                { FabricMasterNode1.SlotMask.Alpha, BlockFields.SurfaceDescription.Alpha },
                { FabricMasterNode1.SlotMask.AlphaClipThreshold, BlockFields.SurfaceDescription.AlphaClipThreshold },
            };

            // Legacy master node slots have additional slot conditions, test them here
            bool AdditionalSlotMaskTests(FabricMasterNode1.SlotMask slotMask)
            {
                switch (slotMask)
                {
                    case FabricMasterNode1.SlotMask.SpecularOcclusion:
                        return lightingData.specularOcclusionMode == SpecularOcclusionMode.Custom;
                    case FabricMasterNode1.SlotMask.DiffusionProfile:
                        return fabricData.subsurfaceScattering || fabricData.transmission;
                    case FabricMasterNode1.SlotMask.SubsurfaceMask:
                        return fabricData.subsurfaceScattering;
                    case FabricMasterNode1.SlotMask.Thickness:
                        return fabricData.transmission;
                    case FabricMasterNode1.SlotMask.AlphaClipThreshold:
                        return systemData.alphaTest;
                    default:
                        return true;
                }
            }

            // Set blockmap
            blockMap = new Dictionary<BlockFieldDescriptor, int>();
            foreach (FabricMasterNode1.SlotMask slotMask in Enum.GetValues(typeof(FabricMasterNode1.SlotMask)))
            {
                if (fabricMasterNode.MaterialTypeUsesSlotMask(slotMask))
                {
                    if (!blockMapLookup.TryGetValue(slotMask, out var blockFieldDescriptor))
                        continue;

                    if (!AdditionalSlotMaskTests(slotMask))
                        continue;

                    var slotId = Mathf.Log((int)slotMask, 2);
                    blockMap.Add(blockFieldDescriptor, (int)slotId);
                }
            }

            // Override Baked GI
            if (lightingData.overrideBakedGI)
            {
                blockMap.Add(HDBlockFields.SurfaceDescription.BakedGI, FabricMasterNode1.LightingSlotId);
                blockMap.Add(HDBlockFields.SurfaceDescription.BakedBackGI, FabricMasterNode1.BackLightingSlotId);
            }

            // Depth Offset
            if (builtinData.depthOffset)
            {
                blockMap.Add(HDBlockFields.SurfaceDescription.DepthOffset, FabricMasterNode1.DepthOffsetSlotId);
            }

            return true;
        }
    }
}
