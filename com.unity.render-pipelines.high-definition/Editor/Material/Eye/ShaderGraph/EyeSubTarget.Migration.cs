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
    sealed partial class EyeSubTarget : LightingSubTarget, ILegacyTarget, IRequiresData<EyeData>
    {
        public bool TryUpgradeFromMasterNode(IMasterNode1 masterNode, out Dictionary<BlockFieldDescriptor, int> blockMap)
        {
            blockMap = null;
            if(!(masterNode is EyeMasterNode1 eyeMasterNode))
                return false;

            m_MigrateFromOldSG = true;

            // Set data
            systemData.surfaceType = (SurfaceType)eyeMasterNode.m_SurfaceType;
            systemData.blendMode = HDSubShaderUtilities.UpgradeLegacyAlphaModeToBlendMode((int)eyeMasterNode.m_AlphaMode);
            // Previous master node wasn't having any renderingPass. Assign it correctly now.
            systemData.renderQueueType = systemData.surfaceType == SurfaceType.Opaque ? HDRenderQueue.RenderQueueType.Opaque : HDRenderQueue.RenderQueueType.Transparent;
            systemData.alphaTest = eyeMasterNode.m_AlphaTest;
            systemData.sortPriority = eyeMasterNode.m_SortPriority;
            systemData.doubleSidedMode = eyeMasterNode.m_DoubleSidedMode;
            systemData.transparentZWrite = eyeMasterNode.m_ZWrite;
            systemData.transparentCullMode = eyeMasterNode.m_transparentCullMode;
            systemData.zTest = eyeMasterNode.m_ZTest;
            systemData.dotsInstancing = eyeMasterNode.m_DOTSInstancing;
            systemData.materialNeedsUpdateHash = eyeMasterNode.m_MaterialNeedsUpdateHash;

            builtinData.transparentDepthPrepass = eyeMasterNode.m_AlphaTestDepthPrepass;
            builtinData.transparentDepthPostpass = eyeMasterNode.m_AlphaTestDepthPostpass;
            builtinData.supportLodCrossFade = eyeMasterNode.m_SupportLodCrossFade;
            builtinData.transparencyFog = eyeMasterNode.m_TransparencyFog;
            builtinData.addPrecomputedVelocity = eyeMasterNode.m_AddPrecomputedVelocity;
            builtinData.depthOffset = eyeMasterNode.m_depthOffset;
            builtinData.alphaToMask = eyeMasterNode.m_AlphaToMask;

            lightingData.blendPreserveSpecular = eyeMasterNode.m_BlendPreserveSpecular;
            lightingData.receiveDecals = eyeMasterNode.m_ReceiveDecals;
            lightingData.receiveSSR = eyeMasterNode.m_ReceivesSSR;
            lightingData.receiveSSRTransparent = eyeMasterNode.m_ReceivesSSRTransparent;
            lightingData.specularOcclusionMode = eyeMasterNode.m_SpecularOcclusionMode;
            lightingData.overrideBakedGI = eyeMasterNode.m_overrideBakedGI;
            
            eyeData.subsurfaceScattering = eyeMasterNode.m_SubsurfaceScattering;
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
                { EyeMasterNode1.SlotMask.IrisNormal, HDBlockFields.SurfaceDescription.IrisNormalTS }, 
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
                        return eyeData.subsurfaceScattering;
                    case EyeMasterNode1.SlotMask.SubsurfaceMask:
                        return eyeData.subsurfaceScattering;
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
    }
}
