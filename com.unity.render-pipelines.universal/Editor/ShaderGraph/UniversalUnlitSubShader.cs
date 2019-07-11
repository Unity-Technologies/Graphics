using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace UnityEditor.Rendering.Universal
{
    [Serializable]
    [FormerName("UnityEngine.Experimental.Rendering.LightweightPipeline.LightWeightUnlitSubShader")]
    [FormerName("UnityEditor.ShaderGraph.LightWeightUnlitSubShader")]
    [FormerName("UnityEditor.Rendering.LWRP.LightWeightUnlitSubShader")]
    [FormerName("UnityEngine.Rendering.LWRP.LightWeightUnlitSubShader")]
    class UniversalUnlitSubShader : IUnlitSubShader
    {
        Pass m_UnlitPass = new Pass
        {
            Name = "Pass",
            TemplatePath = "universalUnlitPass.template",
            PixelShaderSlots = new List<int>
            {
                UnlitMasterNode.ColorSlotId,
                UnlitMasterNode.AlphaSlotId,
                UnlitMasterNode.AlphaThresholdSlotId
            },
            VertexShaderSlots = new List<int>()
            {
                UnlitMasterNode.PositionSlotId
            },
            Requirements = new ShaderGraphRequirements()
            {
                requiresNormal = UniversalSubShaderUtilities.k_PixelCoordinateSpace,
                requiresTangent = UniversalSubShaderUtilities.k_PixelCoordinateSpace,
                requiresBitangent = UniversalSubShaderUtilities.k_PixelCoordinateSpace,
                requiresPosition = UniversalSubShaderUtilities.k_PixelCoordinateSpace,
                requiresViewDir = UniversalSubShaderUtilities.k_PixelCoordinateSpace,
                requiresMeshUVs = new List<UVChannel>() { UVChannel.UV1 },
            },
            ExtraDefines = new List<string>(),
            OnGeneratePassImpl = (IMasterNode node, ref Pass pass, ref ShaderGraphRequirements requirements) =>
            {
                var masterNode = node as UnlitMasterNode;

                if (masterNode.IsSlotConnected(PBRMasterNode.AlphaThresholdSlotId))
                    pass.ExtraDefines.Add("#define _AlphaClip 1");
                if (masterNode.surfaceType == SurfaceType.Transparent && masterNode.alphaMode == AlphaMode.Premultiply)
                    pass.ExtraDefines.Add("#define _ALPHAPREMULTIPLY_ON 1");
                if (requirements.requiresDepthTexture)
                    pass.ExtraDefines.Add("#define REQUIRE_DEPTH_TEXTURE");
                if (requirements.requiresCameraOpaqueTexture)
                    pass.ExtraDefines.Add("#define REQUIRE_OPAQUE_TEXTURE");
            }
        };

        Pass m_ShadowPass = new Pass()
        {
            Name = "",
            TemplatePath = "universalUnlitShadowPass.template",
            PixelShaderSlots = new List<int>()
            {
                PBRMasterNode.AlphaSlotId,
                PBRMasterNode.AlphaThresholdSlotId
            },
            VertexShaderSlots = new List<int>()
            {
                PBRMasterNode.PositionSlotId
            },
            Requirements = new ShaderGraphRequirements()
            {
                requiresNormal = UniversalSubShaderUtilities.k_PixelCoordinateSpace,
                requiresTangent = UniversalSubShaderUtilities.k_PixelCoordinateSpace,
                requiresBitangent = UniversalSubShaderUtilities.k_PixelCoordinateSpace,
                requiresPosition = UniversalSubShaderUtilities.k_PixelCoordinateSpace,
                requiresViewDir = UniversalSubShaderUtilities.k_PixelCoordinateSpace,
                requiresMeshUVs = new List<UVChannel>() { UVChannel.UV1 },
            },
            ExtraDefines = new List<string>(),
            OnGeneratePassImpl = (IMasterNode node, ref Pass pass, ref ShaderGraphRequirements requirements) =>
            {
                var masterNode = node as UnlitMasterNode;

                if (masterNode.IsSlotConnected(PBRMasterNode.AlphaThresholdSlotId))
                    pass.ExtraDefines.Add("#define _AlphaClip 1");
                if (masterNode.surfaceType == SurfaceType.Transparent && masterNode.alphaMode == AlphaMode.Premultiply)
                    pass.ExtraDefines.Add("#define _ALPHAPREMULTIPLY_ON 1");
                if (requirements.requiresDepthTexture)
                    pass.ExtraDefines.Add("#define REQUIRE_DEPTH_TEXTURE");
                if (requirements.requiresCameraOpaqueTexture)
                    pass.ExtraDefines.Add("#define REQUIRE_OPAQUE_TEXTURE");
            }
        };

        Pass m_DepthPass = new Pass()
        {
            Name = "",
            TemplatePath = "universalUnlitDepthPass.template",
            PixelShaderSlots = new List<int>()
            {
                PBRMasterNode.AlphaSlotId,
                PBRMasterNode.AlphaThresholdSlotId
            },
            VertexShaderSlots = new List<int>()
            {
                PBRMasterNode.PositionSlotId
            },
            Requirements = new ShaderGraphRequirements()
            {
                requiresNormal = UniversalSubShaderUtilities.k_PixelCoordinateSpace,
                requiresTangent = UniversalSubShaderUtilities.k_PixelCoordinateSpace,
                requiresBitangent = UniversalSubShaderUtilities.k_PixelCoordinateSpace,
                requiresPosition = UniversalSubShaderUtilities.k_PixelCoordinateSpace,
                requiresViewDir = UniversalSubShaderUtilities.k_PixelCoordinateSpace,
                requiresMeshUVs = new List<UVChannel>() { UVChannel.UV1 },
            },
            ExtraDefines = new List<string>(),
            OnGeneratePassImpl = (IMasterNode node, ref Pass pass, ref ShaderGraphRequirements requirements) =>
            {
                var masterNode = node as UnlitMasterNode;

                if (masterNode.IsSlotConnected(PBRMasterNode.AlphaThresholdSlotId))
                    pass.ExtraDefines.Add("#define _AlphaClip 1");
                if (masterNode.surfaceType == SurfaceType.Transparent && masterNode.alphaMode == AlphaMode.Premultiply)
                    pass.ExtraDefines.Add("#define _ALPHAPREMULTIPLY_ON 1");
                if (requirements.requiresDepthTexture)
                    pass.ExtraDefines.Add("#define REQUIRE_DEPTH_TEXTURE");
                if (requirements.requiresCameraOpaqueTexture)
                    pass.ExtraDefines.Add("#define REQUIRE_OPAQUE_TEXTURE");
            }
        };

        public int GetPreviewPassIndex() { return 0; }

        public string GetSubshader(IMasterNode masterNode, GenerationMode mode, List<string> sourceAssetDependencyPaths = null)
        {
            if (sourceAssetDependencyPaths != null)
            {
                // LightWeightPBRSubShader.cs
                sourceAssetDependencyPaths.Add(AssetDatabase.GUIDToAssetPath("3ef30c5c1d5fc412f88511ef5818b654"));
            }

            // Master Node data
            var unlitMasterNode = masterNode as UnlitMasterNode;
            var tags = ShaderGenerator.BuildMaterialTags(unlitMasterNode.surfaceType);
            var options = ShaderGenerator.GetMaterialOptions(unlitMasterNode.surfaceType, unlitMasterNode.alphaMode, unlitMasterNode.twoSided.isOn);

            // Optional passes
            bool includeShadowPass = true;
            IOptionalShadowPass optionalShadow = masterNode as IOptionalShadowPass;
            if (optionalShadow != null)
            {
                includeShadowPass = optionalShadow.ShadowPassActive();
            }

            // Passes
            var passes = includeShadowPass ? new Pass[] { m_UnlitPass, m_ShadowPass, m_DepthPass } : new Pass[] { m_UnlitPass, m_DepthPass };

            return UniversalSubShaderUtilities.GetSubShader<UnlitMasterNode>(unlitMasterNode, tags, options,
                passes, mode, sourceAssetDependencyPaths: sourceAssetDependencyPaths);
        }

        public bool IsPipelineCompatible(RenderPipelineAsset renderPipelineAsset)
        {
            return renderPipelineAsset is UniversalRenderPipelineAsset;
        }
    }
}
