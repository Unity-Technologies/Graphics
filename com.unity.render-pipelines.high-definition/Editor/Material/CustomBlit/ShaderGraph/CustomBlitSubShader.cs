using System.Collections.Generic;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph;
using UnityEngine.Rendering;
using UnityEditor.Rendering.HighDefinition;
using UnityEngine.Rendering.HighDefinition;

namespace UnityEditor.Experimental.Rendering.HighDefinition
{
    class CustomBlitSubShader : ICustomBlitSubShader
    {
        Pass m_PassCustomBlit = new Pass()
        {
            Name = "CustomBlit", 
            LightMode = "CustomBlit", 
            TemplateName = "CustomBlit.template",
            MaterialName = "CustomBlit",
            ShaderPassName = "SHADERPASS_CUSTOMBLIT",

            CullOverride = "Cull Off",
            ZTestOverride = "ZTest Always",
            ZWriteOverride = "ZWrite Off",
          
            ExtraDefines = new List<string>()
            {
                "#define BLIT_PASS"
            },

            Includes = new List<string>()
            {
               "#include \"Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassCustomBlit.hlsl\""
            },

            RequiredFields = new List<string>()
            {
                "AttributesMesh.vertexID",
            },

            PixelShaderSlots = new List<int>()
            {
                CustomBlitMasterNode.BaseColorSlotId
            },

            VertexShaderSlots = new List<int>()
            {
            },

            UseInPreview = false,
            OnGeneratePassImpl = (IMasterNode node, ref Pass pass) =>
            {
                CustomBlitMasterNode customBlitMasterNode = node as CustomBlitMasterNode;
                switch (customBlitMasterNode.blendType)
                {
                    case CustomBlitMasterNode.BlendType.None:
                        pass.BlendOpOverride = "Blend Off";
                        break;
                    case CustomBlitMasterNode.BlendType.Blend:
                        pass.BlendOpOverride = "Blend SrcAlpha OneMinusSrcAlpha";
                        break;
                    case CustomBlitMasterNode.BlendType.Add:
                        pass.BlendOpOverride = "Blend One One";
                        break;
                }               
            }
        };


        Pass m_PassCustomBlitPreview = new Pass()
        {
            Name = "CustomBlitPreview", 
            LightMode = "CustomBlitPreview", 
            TemplateName = "CustomBlit.template",
            MaterialName = "CustomBlit",
            ShaderPassName = "SHADERPASS_CUSTOMBLIT_PREVIEW",

            CullOverride = "Cull Off",
            ZTestOverride = "ZTest Always",
            ZWriteOverride = "ZWrite Off",
       
            ExtraDefines = new List<string>()
            {
                "#define BLIT_PASS"
            },

            Includes = new List<string>()
            {
               "#include \"Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassCustomBlit.hlsl\""
            },

            RequiredFields = new List<string>()
            {
                "AttributesMesh.vertexID",
            },

            PixelShaderSlots = new List<int>()
            {
                CustomBlitMasterNode.BaseColorSlotId
            },

            VertexShaderSlots = new List<int>()
            {
            },

            UseInPreview = true,
            OnGeneratePassImpl = (IMasterNode node, ref Pass pass) =>
            {
                CustomBlitMasterNode customBlitMasterNode = node as CustomBlitMasterNode;
                switch(customBlitMasterNode.blendType)
                {
                    case CustomBlitMasterNode.BlendType.None:
                        pass.BlendOpOverride = "Blend Off";
                        break;
                    case CustomBlitMasterNode.BlendType.Blend:
                        pass.BlendOpOverride = "Blend SrcAlpha OneMinusSrcAlpha";
                        break;
                    case CustomBlitMasterNode.BlendType.Add:
                        pass.BlendOpOverride = "Blend One One";
                        break;
                }
            }
        };
       
        public int GetPreviewPassIndex() { return 1; }

        private static Data.Util.ActiveFields GetActiveFieldsFromMasterNode(AbstractMaterialNode iMasterNode, Pass pass)
        {
            Data.Util.ActiveFields activeFields = new Data.Util.ActiveFields();         
            return activeFields;
        }

        private static bool GenerateShaderPass(CustomBlitMasterNode masterNode, Pass pass, GenerationMode mode, ShaderGenerator result, List<string> sourceAssetDependencyPaths)
        {
            if (mode == GenerationMode.ForReals || pass.UseInPreview)
            {
                pass.OnGeneratePass(masterNode);

                // apply master node options to active fields
                Data.Util.ActiveFields activeFields = GetActiveFieldsFromMasterNode(masterNode, pass);

                // use standard shader pass generation
                return HDSubShaderUtilities.GenerateShaderPass(masterNode, pass, mode, activeFields, result, sourceAssetDependencyPaths, false);
            }
            else
            {
                return false;
            }
        }

        public string GetSubshader(IMasterNode iMasterNode, GenerationMode mode, List<string> sourceAssetDependencyPaths = null)
        {
            if (sourceAssetDependencyPaths != null)
            {
                // CustomBlitSubShader.cs
                sourceAssetDependencyPaths.Add(AssetDatabase.GUIDToAssetPath("a1c640ef3a41b794fa02fcab761192ba"));
                                                                              
                // HDSubShaderUtilities.cs
                sourceAssetDependencyPaths.Add(AssetDatabase.GUIDToAssetPath("713ced4e6eef4a44799a4dd59041484b"));
            }

            var masterNode = iMasterNode as CustomBlitMasterNode;

            var subShader = new ShaderGenerator();
            subShader.AddShaderChunk("SubShader", true);
            subShader.AddShaderChunk("{", true);
            subShader.Indent();
            {                
                GenerateShaderPass(masterNode, m_PassCustomBlit, mode, subShader, sourceAssetDependencyPaths);

                if (mode.IsPreview())
                {
                    GenerateShaderPass(masterNode, m_PassCustomBlitPreview, mode, subShader, sourceAssetDependencyPaths);
                }
            }
            subShader.Deindent();
            subShader.AddShaderChunk("}", true);
            string s = subShader.GetShaderString(0);
            return s;
        }

        public bool IsPipelineCompatible(RenderPipelineAsset renderPipelineAsset)
        {
            return renderPipelineAsset is HDRenderPipelineAsset;
        }
    }
}
