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
    sealed partial class StackLitSubTarget : LightingSubTarget, ILegacyTarget, IRequiresData<StackLitData>
    {
        public bool TryUpgradeFromMasterNode(IMasterNode1 masterNode, out Dictionary<BlockFieldDescriptor, int> blockMap)
        {
            blockMap = null;
            if (!(masterNode is StackLitMasterNode1 stackLitMasterNode))
                return false;

            m_MigrateFromOldSG = true;

            // Set data
            systemData.surfaceType = (SurfaceType)stackLitMasterNode.m_SurfaceType;
            systemData.blendMode = HDSubShaderUtilities.UpgradeLegacyAlphaModeToBlendMode((int)stackLitMasterNode.m_AlphaMode);
            // Previous master node wasn't having any renderingPass. Assign it correctly now.
            systemData.renderQueueType = systemData.surfaceType == SurfaceType.Opaque ? HDRenderQueue.RenderQueueType.Opaque : HDRenderQueue.RenderQueueType.Transparent;
            systemData.alphaTest = stackLitMasterNode.m_AlphaTest;
            systemData.sortPriority = stackLitMasterNode.m_SortPriority;
            systemData.doubleSidedMode = stackLitMasterNode.m_DoubleSidedMode;
            systemData.transparentZWrite = stackLitMasterNode.m_ZWrite;
            systemData.transparentCullMode = stackLitMasterNode.m_transparentCullMode;
            systemData.zTest = stackLitMasterNode.m_ZTest;
            systemData.dotsInstancing = stackLitMasterNode.m_DOTSInstancing;
            systemData.materialNeedsUpdateHash = stackLitMasterNode.m_MaterialNeedsUpdateHash;

            builtinData.supportLodCrossFade = stackLitMasterNode.m_SupportLodCrossFade;
            builtinData.transparencyFog = stackLitMasterNode.m_TransparencyFog;
            builtinData.distortion = stackLitMasterNode.m_Distortion;
            builtinData.distortionMode = stackLitMasterNode.m_DistortionMode;
            builtinData.distortionDepthTest = stackLitMasterNode.m_DistortionDepthTest;
            builtinData.addPrecomputedVelocity = stackLitMasterNode.m_AddPrecomputedVelocity;
            builtinData.depthOffset = stackLitMasterNode.m_depthOffset;
            builtinData.alphaToMask = stackLitMasterNode.m_AlphaToMask;

            lightingData.normalDropOffSpace = stackLitMasterNode.m_NormalDropOffSpace;
            lightingData.blendPreserveSpecular = stackLitMasterNode.m_BlendPreserveSpecular;
            lightingData.receiveDecals = stackLitMasterNode.m_ReceiveDecals;
            lightingData.receiveSSR = stackLitMasterNode.m_ReceiveSSR;
            lightingData.receiveSSRTransparent = stackLitMasterNode.m_ReceivesSSRTransparent;
            lightingData.overrideBakedGI = stackLitMasterNode.m_overrideBakedGI;
            lightingData.specularAA = stackLitMasterNode.m_GeometricSpecularAA;

            stackLitData.subsurfaceScattering = stackLitMasterNode.m_SubsurfaceScattering;
            stackLitData.transmission = stackLitMasterNode.m_Transmission;
            stackLitData.energyConservingSpecular = stackLitMasterNode.m_EnergyConservingSpecular;
            stackLitData.baseParametrization = stackLitMasterNode.m_BaseParametrization;
            stackLitData.dualSpecularLobeParametrization = stackLitMasterNode.m_DualSpecularLobeParametrization;
            stackLitData.anisotropy = stackLitMasterNode.m_Anisotropy;
            stackLitData.coat = stackLitMasterNode.m_Coat;
            stackLitData.coatNormal = stackLitMasterNode.m_CoatNormal;
            stackLitData.dualSpecularLobe = stackLitMasterNode.m_DualSpecularLobe;
            stackLitData.capHazinessWrtMetallic = stackLitMasterNode.m_CapHazinessWrtMetallic;
            stackLitData.iridescence = stackLitMasterNode.m_Iridescence;
            stackLitData.screenSpaceSpecularOcclusionBaseMode = (StackLitData.SpecularOcclusionBaseMode)stackLitMasterNode.m_ScreenSpaceSpecularOcclusionBaseMode;
            stackLitData.dataBasedSpecularOcclusionBaseMode = (StackLitData.SpecularOcclusionBaseMode)stackLitMasterNode.m_DataBasedSpecularOcclusionBaseMode;
            stackLitData.screenSpaceSpecularOcclusionAOConeSize = (StackLitData.SpecularOcclusionAOConeSize)stackLitMasterNode.m_ScreenSpaceSpecularOcclusionAOConeSize;
            stackLitData.screenSpaceSpecularOcclusionAOConeDir = (StackLitData.SpecularOcclusionAOConeDir)stackLitMasterNode.m_ScreenSpaceSpecularOcclusionAOConeDir;
            stackLitData.dataBasedSpecularOcclusionAOConeSize = (StackLitData.SpecularOcclusionAOConeSize)stackLitMasterNode.m_DataBasedSpecularOcclusionAOConeSize;
            stackLitData.specularOcclusionConeFixupMethod = (StackLitData.SpecularOcclusionConeFixupMethod)stackLitMasterNode.m_SpecularOcclusionConeFixupMethod;
            stackLitData.anisotropyForAreaLights = stackLitMasterNode.m_AnisotropyForAreaLights;
            stackLitData.recomputeStackPerLight = stackLitMasterNode.m_RecomputeStackPerLight;
            stackLitData.honorPerLightMinRoughness = stackLitMasterNode.m_HonorPerLightMinRoughness;
            stackLitData.shadeBaseUsingRefractedAngles = stackLitMasterNode.m_ShadeBaseUsingRefractedAngles;
            stackLitData.debug = stackLitMasterNode.m_Debug;
            stackLitData.devMode = stackLitMasterNode.m_DevMode;

            target.customEditorGUI = stackLitMasterNode.m_OverrideEnabled ? stackLitMasterNode.m_ShaderGUIOverride : "";

            // Set blockmap
            blockMap = new Dictionary<BlockFieldDescriptor, int>();
            blockMap.Add(BlockFields.VertexDescription.Position, StackLitMasterNode1.PositionSlotId);
            blockMap.Add(BlockFields.VertexDescription.Normal, StackLitMasterNode1.VertexNormalSlotId);
            blockMap.Add(BlockFields.VertexDescription.Tangent, StackLitMasterNode1.VertexTangentSlotId);

            // Handle mapping of Normal and Tangent block specifically
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
            blockMap.Add(normalBlock, StackLitMasterNode1.NormalSlotId);

            blockMap.Add(HDBlockFields.SurfaceDescription.BentNormal, StackLitMasterNode1.BentNormalSlotId);
            blockMap.Add(tangentBlock, StackLitMasterNode1.TangentSlotId);
            blockMap.Add(BlockFields.SurfaceDescription.BaseColor, StackLitMasterNode1.BaseColorSlotId);

            if (stackLitData.baseParametrization == StackLit.BaseParametrization.BaseMetallic)
            {
                blockMap.Add(BlockFields.SurfaceDescription.Metallic, StackLitMasterNode1.MetallicSlotId);
                blockMap.Add(HDBlockFields.SurfaceDescription.DielectricIor, StackLitMasterNode1.DielectricIorSlotId);
            }
            else if (stackLitData.baseParametrization == StackLit.BaseParametrization.SpecularColor)
            {
                blockMap.Add(BlockFields.SurfaceDescription.Specular, StackLitMasterNode1.SpecularColorSlotId);
            }

            blockMap.Add(BlockFields.SurfaceDescription.Smoothness, StackLitMasterNode1.SmoothnessASlotId);

            if (stackLitData.anisotropy)
            {
                blockMap.Add(HDBlockFields.SurfaceDescription.Anisotropy, StackLitMasterNode1.AnisotropyASlotId);
            }

            blockMap.Add(BlockFields.SurfaceDescription.Occlusion, StackLitMasterNode1.AmbientOcclusionSlotId);

            if (stackLitData.dataBasedSpecularOcclusionBaseMode == StackLitData.SpecularOcclusionBaseMode.Custom)
            {
                blockMap.Add(HDBlockFields.SurfaceDescription.SpecularOcclusion, StackLitMasterNode1.SpecularOcclusionSlotId);
            }

            if (SpecularOcclusionUsesBentNormal(stackLitData) && stackLitData.specularOcclusionConeFixupMethod != StackLitData.SpecularOcclusionConeFixupMethod.Off)
            {
                blockMap.Add(HDBlockFields.SurfaceDescription.SOFixupVisibilityRatioThreshold, StackLitMasterNode1.SOFixupVisibilityRatioThresholdSlotId);
                blockMap.Add(HDBlockFields.SurfaceDescription.SOFixupStrengthFactor, StackLitMasterNode1.SOFixupStrengthFactorSlotId);

                if (SpecularOcclusionConeFixupMethodModifiesRoughness(stackLitData.specularOcclusionConeFixupMethod))
                {
                    blockMap.Add(HDBlockFields.SurfaceDescription.SOFixupMaxAddedRoughness, StackLitMasterNode1.SOFixupMaxAddedRoughnessSlotId);
                }
            }

            if (stackLitData.coat)
            {
                blockMap.Add(BlockFields.SurfaceDescription.CoatSmoothness, StackLitMasterNode1.CoatSmoothnessSlotId);
                blockMap.Add(HDBlockFields.SurfaceDescription.CoatIor, StackLitMasterNode1.CoatIorSlotId);
                blockMap.Add(HDBlockFields.SurfaceDescription.CoatThickness, StackLitMasterNode1.CoatThicknessSlotId);
                blockMap.Add(HDBlockFields.SurfaceDescription.CoatExtinction, StackLitMasterNode1.CoatExtinctionSlotId);

                if (stackLitData.coatNormal)
                {
                    blockMap.Add(HDBlockFields.SurfaceDescription.CoatNormalTS, StackLitMasterNode1.CoatNormalSlotId);
                }

                blockMap.Add(BlockFields.SurfaceDescription.CoatMask, StackLitMasterNode1.CoatMaskSlotId);
            }

            if (stackLitData.dualSpecularLobe)
            {
                if (stackLitData.dualSpecularLobeParametrization == StackLit.DualSpecularLobeParametrization.Direct)
                {
                    blockMap.Add(HDBlockFields.SurfaceDescription.SmoothnessB, StackLitMasterNode1.SmoothnessBSlotId);
                    blockMap.Add(HDBlockFields.SurfaceDescription.LobeMix, StackLitMasterNode1.LobeMixSlotId);
                }
                else if (stackLitData.dualSpecularLobeParametrization == StackLit.DualSpecularLobeParametrization.HazyGloss)
                {
                    blockMap.Add(HDBlockFields.SurfaceDescription.Haziness, StackLitMasterNode1.HazinessSlotId);
                    blockMap.Add(HDBlockFields.SurfaceDescription.HazeExtent, StackLitMasterNode1.HazeExtentSlotId);

                    if (stackLitData.capHazinessWrtMetallic && stackLitData.baseParametrization == StackLit.BaseParametrization.BaseMetallic) // the later should be an assert really
                    {
                        blockMap.Add(HDBlockFields.SurfaceDescription.HazyGlossMaxDielectricF0, StackLitMasterNode1.HazyGlossMaxDielectricF0SlotId);
                    }
                }

                if (stackLitData.anisotropy)
                {
                    blockMap.Add(HDBlockFields.SurfaceDescription.AnisotropyB, StackLitMasterNode1.AnisotropyBSlotId);
                }
            }

            if (stackLitData.iridescence)
            {
                blockMap.Add(HDBlockFields.SurfaceDescription.IridescenceMask, StackLitMasterNode1.IridescenceMaskSlotId);
                blockMap.Add(HDBlockFields.SurfaceDescription.IridescenceThickness, StackLitMasterNode1.IridescenceThicknessSlotId);

                if (stackLitData.coat)
                {
                    blockMap.Add(HDBlockFields.SurfaceDescription.IridescenceCoatFixupTIR, StackLitMasterNode1.IridescenceCoatFixupTIRSlotId);
                    blockMap.Add(HDBlockFields.SurfaceDescription.IridescenceCoatFixupTIRClamp, StackLitMasterNode1.IridescenceCoatFixupTIRClampSlotId);
                }
            }

            if (stackLitData.subsurfaceScattering)
            {
                blockMap.Add(HDBlockFields.SurfaceDescription.SubsurfaceMask, StackLitMasterNode1.SubsurfaceMaskSlotId);
            }

            if (stackLitData.transmission)
            {
                blockMap.Add(HDBlockFields.SurfaceDescription.Thickness, StackLitMasterNode1.ThicknessSlotId);
            }

            if (stackLitData.subsurfaceScattering || stackLitData.transmission)
            {
                blockMap.Add(HDBlockFields.SurfaceDescription.DiffusionProfileHash, StackLitMasterNode1.DiffusionProfileHashSlotId);
            }

            blockMap.Add(BlockFields.SurfaceDescription.Alpha, StackLitMasterNode1.AlphaSlotId);

            if (systemData.alphaTest)
            {
                blockMap.Add(BlockFields.SurfaceDescription.AlphaClipThreshold, StackLitMasterNode1.AlphaClipThresholdSlotId);
            }

            blockMap.Add(BlockFields.SurfaceDescription.Emission, StackLitMasterNode1.EmissionSlotId);

            if (systemData.surfaceType == SurfaceType.Transparent && builtinData.distortion)
            {
                blockMap.Add(HDBlockFields.SurfaceDescription.Distortion, StackLitMasterNode1.DistortionSlotId);
                blockMap.Add(HDBlockFields.SurfaceDescription.DistortionBlur, StackLitMasterNode1.DistortionBlurSlotId);
            }

            if (lightingData.specularAA)
            {
                blockMap.Add(HDBlockFields.SurfaceDescription.SpecularAAScreenSpaceVariance, StackLitMasterNode1.SpecularAAScreenSpaceVarianceSlotId);
                blockMap.Add(HDBlockFields.SurfaceDescription.SpecularAAThreshold, StackLitMasterNode1.SpecularAAThresholdSlotId);
            }

            if (lightingData.overrideBakedGI)
            {
                blockMap.Add(HDBlockFields.SurfaceDescription.BakedGI, StackLitMasterNode1.LightingSlotId);
                blockMap.Add(HDBlockFields.SurfaceDescription.BakedBackGI, StackLitMasterNode1.BackLightingSlotId);
            }

            if (builtinData.depthOffset)
            {
                blockMap.Add(HDBlockFields.SurfaceDescription.DepthOffset, StackLitMasterNode1.DepthOffsetSlotId);
            }

            return true;
        }
    }
}
