using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph;
using UnityEngine.Rendering;
using UnityEngine.Rendering.LWRP;
    
namespace UnityEditor.Rendering.LWRP
{
    [Serializable]
    [FormerName("UnityEditor.Experimental.Rendering.LightweightPipeline.LightWeightPBRSubShader")]
    [FormerName("UnityEditor.ShaderGraph.LightWeightPBRSubShader")]
    class LightWeightPBRSubShader : IPBRSubShader
    {
        Pass m_ForwardPassMetallic = new Pass
        {
            Name = "LightweightForward",
            TemplatePath = "lightweightPBRForwardPass.template",
            PixelShaderSlots = new List<int>
            {
                PBRMasterNode.AlbedoSlotId,
                PBRMasterNode.NormalSlotId,
                PBRMasterNode.EmissionSlotId,
                PBRMasterNode.MetallicSlotId,
                PBRMasterNode.SmoothnessSlotId,
                PBRMasterNode.OcclusionSlotId,
                PBRMasterNode.AlphaSlotId,
                PBRMasterNode.AlphaThresholdSlotId
            },
            VertexShaderSlots = new List<int>()
            {
                PBRMasterNode.PositionSlotId
            },
            Requirements = new ShaderGraphRequirements()
            {
                requiresNormal = LWRPSubShaderUtilities.k_PixelCoordinateSpace,
                requiresTangent = LWRPSubShaderUtilities.k_PixelCoordinateSpace,
                requiresBitangent = LWRPSubShaderUtilities.k_PixelCoordinateSpace,
                requiresPosition = LWRPSubShaderUtilities.k_PixelCoordinateSpace,
                requiresViewDir = LWRPSubShaderUtilities.k_PixelCoordinateSpace,
                requiresMeshUVs = new List<UVChannel>() { UVChannel.UV1 },
            },
            ExtraDefines = new List<string>(),
            OnGeneratePassImpl = (IMasterNode node, ref Pass pass, ref ShaderGraphRequirements requirements) =>
            {
                var masterNode = node as PBRMasterNode;

                if (masterNode.IsSlotConnected(PBRMasterNode.NormalSlotId))
                    pass.ExtraDefines.Add("#define _NORMALMAP 1");
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

        Pass m_ForwardPassSpecular = new Pass()
        {
            Name = "LightweightForward",
            TemplatePath = "lightweightPBRForwardPass.template",
            PixelShaderSlots = new List<int>()
            {
                PBRMasterNode.AlbedoSlotId,
                PBRMasterNode.NormalSlotId,
                PBRMasterNode.EmissionSlotId,
                PBRMasterNode.SpecularSlotId,
                PBRMasterNode.SmoothnessSlotId,
                PBRMasterNode.OcclusionSlotId,
                PBRMasterNode.AlphaSlotId,
                PBRMasterNode.AlphaThresholdSlotId
            },
            VertexShaderSlots = new List<int>()
            {
                PBRMasterNode.PositionSlotId
            },
            Requirements = new ShaderGraphRequirements()
            {
                requiresNormal = LWRPSubShaderUtilities.k_PixelCoordinateSpace,
                requiresTangent = LWRPSubShaderUtilities.k_PixelCoordinateSpace,
                requiresBitangent = LWRPSubShaderUtilities.k_PixelCoordinateSpace,
                requiresPosition = LWRPSubShaderUtilities.k_PixelCoordinateSpace,
                requiresViewDir = LWRPSubShaderUtilities.k_PixelCoordinateSpace,
                requiresMeshUVs = new List<UVChannel>() { UVChannel.UV1 },
            },
            ExtraDefines = new List<string>(),
            OnGeneratePassImpl = (IMasterNode node, ref Pass pass, ref ShaderGraphRequirements requirements) =>
            {
                var masterNode = node as PBRMasterNode;

                pass.ExtraDefines.Add("#define _SPECULAR_SETUP 1");
                if (masterNode.IsSlotConnected(PBRMasterNode.NormalSlotId))
                    pass.ExtraDefines.Add("#define _NORMALMAP 1");
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

        Pass m_ForwardPassMetallic2D = new Pass
        {
            Name = "LightweightForward",
            TemplatePath = "lightweight2DPBRPass.template",
            PixelShaderSlots = new List<int>
            {
                PBRMasterNode.AlbedoSlotId,
                PBRMasterNode.NormalSlotId,
                PBRMasterNode.EmissionSlotId,
                PBRMasterNode.MetallicSlotId,
                PBRMasterNode.SmoothnessSlotId,
                PBRMasterNode.OcclusionSlotId,
                PBRMasterNode.AlphaSlotId,
                PBRMasterNode.AlphaThresholdSlotId
            },
            VertexShaderSlots = new List<int>()
            {
                PBRMasterNode.PositionSlotId
            },
            Requirements = new ShaderGraphRequirements()
            {
                requiresNormal = LWRPSubShaderUtilities.k_PixelCoordinateSpace,
                requiresTangent = LWRPSubShaderUtilities.k_PixelCoordinateSpace,
                requiresBitangent = LWRPSubShaderUtilities.k_PixelCoordinateSpace,
                requiresPosition = LWRPSubShaderUtilities.k_PixelCoordinateSpace,
                requiresViewDir = LWRPSubShaderUtilities.k_PixelCoordinateSpace,
                requiresMeshUVs = new List<UVChannel>() { UVChannel.UV1 },
            },
            ExtraDefines = new List<string>(),
            OnGeneratePassImpl = (IMasterNode node, ref Pass pass, ref ShaderGraphRequirements requirements) =>
            {
                var masterNode = node as PBRMasterNode;

                if (masterNode.IsSlotConnected(PBRMasterNode.NormalSlotId))
                    pass.ExtraDefines.Add("#define _NORMALMAP 1");
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

        Pass m_ForwardPassSpecular2D = new Pass()
        {
            Name = "LightweightForward",
            TemplatePath = "lightweight2DPBRPass.template",
            PixelShaderSlots = new List<int>()
            {
                PBRMasterNode.AlbedoSlotId,
                PBRMasterNode.NormalSlotId,
                PBRMasterNode.EmissionSlotId,
                PBRMasterNode.SpecularSlotId,
                PBRMasterNode.SmoothnessSlotId,
                PBRMasterNode.OcclusionSlotId,
                PBRMasterNode.AlphaSlotId,
                PBRMasterNode.AlphaThresholdSlotId
            },
            VertexShaderSlots = new List<int>()
            {
                PBRMasterNode.PositionSlotId
            },
            Requirements = new ShaderGraphRequirements()
            {
                requiresNormal = LWRPSubShaderUtilities.k_PixelCoordinateSpace,
                requiresTangent = LWRPSubShaderUtilities.k_PixelCoordinateSpace,
                requiresBitangent = LWRPSubShaderUtilities.k_PixelCoordinateSpace,
                requiresPosition = LWRPSubShaderUtilities.k_PixelCoordinateSpace,
                requiresViewDir = LWRPSubShaderUtilities.k_PixelCoordinateSpace,
                requiresMeshUVs = new List<UVChannel>() { UVChannel.UV1 },
            },
            ExtraDefines = new List<string>(),
            OnGeneratePassImpl = (IMasterNode node, ref Pass pass, ref ShaderGraphRequirements requirements) =>
            {
                var masterNode = node as PBRMasterNode;

                pass.ExtraDefines.Add("#define _SPECULAR_SETUP 1");
                if (masterNode.IsSlotConnected(PBRMasterNode.NormalSlotId))
                    pass.ExtraDefines.Add("#define _NORMALMAP 1");
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

        Pass m_DepthShadowPass = new Pass()
        {
            Name = "",
            TemplatePath = "lightweightPBRExtraPasses.template",
            PixelShaderSlots = new List<int>()
            {
                PBRMasterNode.AlbedoSlotId,
                PBRMasterNode.EmissionSlotId,
                PBRMasterNode.AlphaSlotId,
                PBRMasterNode.AlphaThresholdSlotId
            },
            VertexShaderSlots = new List<int>()
            {
                PBRMasterNode.PositionSlotId
            },
            Requirements = new ShaderGraphRequirements()
            {
                requiresNormal = LWRPSubShaderUtilities.k_PixelCoordinateSpace,
                requiresTangent = LWRPSubShaderUtilities.k_PixelCoordinateSpace,
                requiresBitangent = LWRPSubShaderUtilities.k_PixelCoordinateSpace,
                requiresPosition = LWRPSubShaderUtilities.k_PixelCoordinateSpace,
                requiresViewDir = LWRPSubShaderUtilities.k_PixelCoordinateSpace,
                requiresMeshUVs = new List<UVChannel>() { UVChannel.UV1 },
            },
            ExtraDefines = new List<string>(),
            OnGeneratePassImpl = (IMasterNode node, ref Pass pass, ref ShaderGraphRequirements requirements) =>
            {
                var masterNode = node as PBRMasterNode;

                if (masterNode.model == PBRMasterNode.Model.Specular)
                    pass.ExtraDefines.Add("#define _SPECULAR_SETUP 1");
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
                sourceAssetDependencyPaths.Add(AssetDatabase.GUIDToAssetPath("ca91dbeb78daa054c9bbe15fef76361c"));
            }

            // Master Node data
            var pbrMasterNode = masterNode as PBRMasterNode;
            var tags = ShaderGenerator.BuildMaterialTags(pbrMasterNode.surfaceType);
            var options = ShaderGenerator.GetMaterialOptions(pbrMasterNode.surfaceType, pbrMasterNode.alphaMode, pbrMasterNode.twoSided.isOn);

            // Passes
            var forwardPass = pbrMasterNode.model == PBRMasterNode.Model.Metallic ? m_ForwardPassMetallic : m_ForwardPassSpecular;
            var forward2DPass = pbrMasterNode.model == PBRMasterNode.Model.Metallic ? m_ForwardPassMetallic2D : m_ForwardPassSpecular2D;
            var passes = new Pass[] { forwardPass, m_DepthShadowPass, forward2DPass };

            return LWRPSubShaderUtilities.GetSubShader<PBRMasterNode>(pbrMasterNode, tags, options, 
                passes, mode, "UnityEditor.ShaderGraph.PBRMasterGUI", sourceAssetDependencyPaths);
        }

        public bool IsPipelineCompatible(RenderPipelineAsset renderPipelineAsset)
        {
            return renderPipelineAsset is LightweightRenderPipelineAsset;
        }        
    }
}
