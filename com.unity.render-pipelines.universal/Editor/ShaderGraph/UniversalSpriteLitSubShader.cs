using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEditor.Rendering.Universal;

namespace UnityEditor.Experimental.Rendering.Universal
{
    [Serializable]
    [FormerName("UnityEditor.Experimental.Rendering.LWRP.LightWeightSpriteLitSubShader")]
    class UniversalSpriteLitSubShader : ISpriteLitSubShader
    {
        Pass m_LitPass = new Pass
        {
            Name = "Lit Pass",
            TemplatePath = "universalSpriteLitPass.template",
            PixelShaderSlots = new List<int>
            {
                SpriteLitMasterNode.ColorSlotId,
                SpriteLitMasterNode.MaskSlotId,
            },
            VertexShaderSlots = new List<int>()
            {
                SpriteLitMasterNode.PositionSlotId,
            },
            Requirements = new ShaderGraphRequirements()
            {
                requiresVertexColor = true,
                requiresMeshUVs = new List<UVChannel>() { UVChannel.UV0 },
            },
            ExtraDefines = new List<string>(),
            OnGeneratePassImpl = (IMasterNode node, ref Pass pass, ref ShaderGraphRequirements requirements) =>
            {
                var masterNode = node as SpriteUnlitMasterNode;

                if (requirements.requiresDepthTexture)
                    pass.ExtraDefines.Add("#define REQUIRE_DEPTH_TEXTURE");
                if (requirements.requiresCameraOpaqueTexture)
                    pass.ExtraDefines.Add("#define REQUIRE_OPAQUE_TEXTURE");
            }
        };

        Pass m_NormalPass = new Pass
        {
            Name = "Sprite Normal",
            TemplatePath = "universalSpriteNormalPass.template",
            PixelShaderSlots = new List<int>
            {
                SpriteLitMasterNode.ColorSlotId,
                SpriteLitMasterNode.NormalSlotId
            },
            VertexShaderSlots = new List<int>()
            {
                SpriteLitMasterNode.PositionSlotId
            },
            Requirements = new ShaderGraphRequirements()
            {
                requiresVertexColor = true,
            },
            ExtraDefines = new List<string>(),
            OnGeneratePassImpl = (IMasterNode node, ref Pass pass, ref ShaderGraphRequirements requirements) =>
            {
                var masterNode = node as SpriteUnlitMasterNode;

                if (requirements.requiresDepthTexture)
                    pass.ExtraDefines.Add("#define REQUIRE_DEPTH_TEXTURE");
                if (requirements.requiresCameraOpaqueTexture)
                    pass.ExtraDefines.Add("#define REQUIRE_OPAQUE_TEXTURE");
            }
        };

        Pass m_ForwardPass = new Pass
        {
            Name = "Sprite Forward",
            TemplatePath = "universalSpriteForwardPass.template",
            PixelShaderSlots = new List<int>
            {
                SpriteLitMasterNode.ColorSlotId,
                SpriteLitMasterNode.NormalSlotId
            },
            VertexShaderSlots = new List<int>()
            {
                SpriteLitMasterNode.PositionSlotId
            },
            Requirements = new ShaderGraphRequirements()
            {
                requiresVertexColor = true,
            },
            ExtraDefines = new List<string>(),
            OnGeneratePassImpl = (IMasterNode node, ref Pass pass, ref ShaderGraphRequirements requirements) =>
            {
                var masterNode = node as SpriteLitMasterNode;

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
                // LightWeightSpriteLitSubShader.cs
                sourceAssetDependencyPaths.Add(AssetDatabase.GUIDToAssetPath("62511ee827d14492a8c78ba0ef167e7f"));
            }

            // Master Node data
            var litMasterNode = masterNode as SpriteLitMasterNode;
            var tags = ShaderGenerator.BuildMaterialTags(SurfaceType.Transparent);
            var options = ShaderGenerator.GetMaterialOptions(SurfaceType.Transparent, AlphaMode.Alpha, true);

            // Passes
            var passes = new Pass[] { m_LitPass, m_NormalPass, m_ForwardPass };

            return UniversalSubShaderUtilities.GetSubShader<SpriteLitMasterNode>(litMasterNode, tags, options, 
                passes, mode, sourceAssetDependencyPaths: sourceAssetDependencyPaths);
        }

        public bool IsPipelineCompatible(RenderPipelineAsset renderPipelineAsset)
        {
            return renderPipelineAsset is UniversalRenderPipelineAsset;
        }

        public UniversalSpriteLitSubShader () { }
    }
}
