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
    sealed partial class HDUnlitSubTarget : ILegacyTarget
    {
        public bool TryUpgradeFromMasterNode(IMasterNode1 masterNode, out Dictionary<BlockFieldDescriptor, int> blockMap)
        {
            blockMap = null;
            switch (masterNode)
            {
                case UnlitMasterNode1 unlitMasterNode:
                    UpgradeUnlitMasterNode(unlitMasterNode, out blockMap);
                    return true;
                case HDUnlitMasterNode1 hdUnlitMasterNode:
                    UpgradeHDUnlitMasterNode(hdUnlitMasterNode, out blockMap);
                    return true;
                default:
                    return false;
            }
        }

        void UpgradeUnlitMasterNode(UnlitMasterNode1 unlitMasterNode, out Dictionary<BlockFieldDescriptor, int> blockMap)
        {
            m_MigrateFromOldCrossPipelineSG = true;
            m_MigrateFromOldSG = true;

            // Set data
            systemData.surfaceType = (SurfaceType)unlitMasterNode.m_SurfaceType;
            systemData.blendMode = HDSubShaderUtilities.UpgradeLegacyAlphaModeToBlendMode((int)unlitMasterNode.m_AlphaMode);
            // Previous master node wasn't having any renderingPass. Assign it correctly now.
            systemData.renderQueueType = systemData.surfaceType == SurfaceType.Opaque ? HDRenderQueue.RenderQueueType.Opaque : HDRenderQueue.RenderQueueType.Transparent;
            systemData.doubleSidedMode = unlitMasterNode.m_TwoSided ? DoubleSidedMode.Enabled : DoubleSidedMode.Disabled;
            systemData.alphaTest = HDSubShaderUtilities.UpgradeLegacyAlphaClip(unlitMasterNode);
            systemData.dotsInstancing = false;
            systemData.transparentZWrite = false;
            builtinData.addPrecomputedVelocity = false;
            target.customEditorGUI = unlitMasterNode.m_OverrideEnabled ? unlitMasterNode.m_ShaderGUIOverride : "";

            // Set blockmap
            blockMap = new Dictionary<BlockFieldDescriptor, int>()
            {
                { BlockFields.VertexDescription.Position, 9 },
                { BlockFields.VertexDescription.Normal, 10 },
                { BlockFields.VertexDescription.Tangent, 11 },
                { BlockFields.SurfaceDescription.BaseColor, 0 },
                { BlockFields.SurfaceDescription.Alpha, 7 },
                { BlockFields.SurfaceDescription.AlphaClipThreshold, 8 },
            };
        }

        void UpgradeHDUnlitMasterNode(HDUnlitMasterNode1 hdUnlitMasterNode, out Dictionary<BlockFieldDescriptor, int> blockMap)
        {
            m_MigrateFromOldSG = true;

            // Set data
            systemData.surfaceType = (SurfaceType)hdUnlitMasterNode.m_SurfaceType;
            systemData.blendMode = HDSubShaderUtilities.UpgradeLegacyAlphaModeToBlendMode((int)hdUnlitMasterNode.m_AlphaMode);
            systemData.renderQueueType = HDRenderQueue.MigrateRenderQueueToHDRP10(hdUnlitMasterNode.m_RenderingPass);
            // Patch rendering pass in case the master node had an old configuration
            if (systemData.renderQueueType == HDRenderQueue.RenderQueueType.Background)
                systemData.renderQueueType = HDRenderQueue.RenderQueueType.Opaque;
            systemData.alphaTest = hdUnlitMasterNode.m_AlphaTest;
            systemData.sortPriority = hdUnlitMasterNode.m_SortPriority;
            systemData.doubleSidedMode = hdUnlitMasterNode.m_DoubleSided ? DoubleSidedMode.Enabled : DoubleSidedMode.Disabled;
            systemData.transparentZWrite = hdUnlitMasterNode.m_ZWrite;
            systemData.transparentCullMode = hdUnlitMasterNode.m_transparentCullMode;
            systemData.zTest = hdUnlitMasterNode.m_ZTest;
            systemData.dotsInstancing = hdUnlitMasterNode.m_DOTSInstancing;

            builtinData.transparencyFog = hdUnlitMasterNode.m_TransparencyFog;
            builtinData.distortion = hdUnlitMasterNode.m_Distortion;
            builtinData.distortionMode = hdUnlitMasterNode.m_DistortionMode;
            builtinData.distortionDepthTest = hdUnlitMasterNode.m_DistortionDepthTest;
            builtinData.addPrecomputedVelocity = hdUnlitMasterNode.m_AddPrecomputedVelocity;

            unlitData.distortionOnly = hdUnlitMasterNode.m_DistortionOnly;
            unlitData.enableShadowMatte = hdUnlitMasterNode.m_EnableShadowMatte;
            target.customEditorGUI = hdUnlitMasterNode.m_OverrideEnabled ? hdUnlitMasterNode.m_ShaderGUIOverride : "";

            // Set blockmap
            blockMap = new Dictionary<BlockFieldDescriptor, int>()
            {
                { BlockFields.VertexDescription.Position, 9 },
                { BlockFields.VertexDescription.Normal, 13 },
                { BlockFields.VertexDescription.Tangent, 14 },
                { BlockFields.SurfaceDescription.BaseColor, 0 },
                { BlockFields.SurfaceDescription.Alpha, 7 },
                { BlockFields.SurfaceDescription.AlphaClipThreshold, 8 },
                { BlockFields.SurfaceDescription.Emission, 12 },
            };

            // Distortion
            if (systemData.surfaceType == SurfaceType.Transparent && builtinData.distortion)
            {
                blockMap.Add(HDBlockFields.SurfaceDescription.Distortion, 10);
                blockMap.Add(HDBlockFields.SurfaceDescription.DistortionBlur, 11);
            }

            // Shadow Matte
            if (unlitData.enableShadowMatte)
            {
                blockMap.Add(HDBlockFields.SurfaceDescription.ShadowTint, 15);
            }
        }
    }
}
