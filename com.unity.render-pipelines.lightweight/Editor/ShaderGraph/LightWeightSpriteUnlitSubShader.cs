using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph;
using UnityEngine.Rendering;
using UnityEngine.Rendering.LWRP;

namespace UnityEditor.Experimental.Rendering.LWRP
{
    [Serializable]
    class LightWeightSpriteUnlitSubShader : ISpriteUnlitSubShader
    {
        Pass m_UnlitPass = new Pass
        {
            Name = "Pass",
            TemplatePath = "lightweightSpriteUnlitPass.template",
            PixelShaderSlots = new List<int>
            {
                SpriteUnlitMasterNode.ColorSlotId,
            },
            VertexShaderSlots = new List<int>()
            {
                SpriteUnlitMasterNode.PositionSlotId,
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

        public int GetPreviewPassIndex() { return 0; }

        public string GetSubshader(IMasterNode masterNode, GenerationMode mode, List<string> sourceAssetDependencyPaths = null)
        {
            if (sourceAssetDependencyPaths != null)
            {
                // LightWeightSpriteUnlitSubShader.cs
                sourceAssetDependencyPaths.Add(AssetDatabase.GUIDToAssetPath("f2df349d00ec920488971bb77440b7bc"));
            }

            // Master Node data
            var unlitMasterNode = masterNode as SpriteUnlitMasterNode;
            var tags = ShaderGenerator.BuildMaterialTags(SurfaceType.Transparent);
            var options = ShaderGenerator.GetMaterialOptions(SurfaceType.Transparent, AlphaMode.Alpha, true);

            // Passes
            var passes = new Pass[] { m_UnlitPass };

            return LWRPSubShaderUtilities.GetSubShader<SpriteUnlitMasterNode>(unlitMasterNode, tags, options, 
                passes, mode, sourceAssetDependencyPaths: sourceAssetDependencyPaths);
        }

        public bool IsPipelineCompatible(RenderPipelineAsset renderPipelineAsset)
        {
            return renderPipelineAsset is LightweightRenderPipelineAsset;
        }
    }
}
