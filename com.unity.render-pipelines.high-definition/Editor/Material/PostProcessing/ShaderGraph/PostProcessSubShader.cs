using System.Collections.Generic;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering.HDPipeline;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    class PostProcessSubShader : IPostProcessSubShader
    {
        // CAUTION: c# code relies on the order in which the passes are declared, any change will need to be reflected in PostProcesssystem.cs - s_MaterialPostProcessNames and s_MaterialPostProcessSGNames array
        // and PostProcessSet.InitializeMaterialValues()

        Pass m_PassPostProcess = new Pass()
        {
            Name = "PostProcess", 
            LightMode = "PostProcess", 
            TemplateName = "PostProcessPass.template",
            MaterialName = "PostProcessing",
            ShaderPassName = "SHADERPASS_POSTPROCESS",

            CullOverride = "Cull Off",
            ZTestOverride = "ZTest Always",
            ZWriteOverride = "ZWrite Off",
          

            ExtraDefines = new List<string>()
            {
                "#define BLIT_PASS"
            },

            Includes = new List<string>()
            {
               "#include \"Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassPostProcess.hlsl\""
            },

            RequiredFields = new List<string>()
            {
                "AttributesMesh.vertexID",
            },

            PixelShaderSlots = new List<int>()
            {
                PostProcessMasterNode.BaseColorSlotId
            },

            VertexShaderSlots = new List<int>()
            {
            },

            UseInPreview = false,
            OnGeneratePassImpl = (IMasterNode node, ref Pass pass) =>
            {         
            }
        };


        Pass m_PassPostProcessPreview = new Pass()
        {
            Name = "PostProcessPreview", 
            LightMode = "PostProcessPreview", 
            TemplateName = "PostProcessPass.template",
            MaterialName = "PostProcessing",
            ShaderPassName = "SHADERPASS_POSTPROCESS_PREVIEW",

            CullOverride = "Cull Off",
            ZTestOverride = "ZTest Always",
            ZWriteOverride = "ZWrite Off",
       
            ExtraDefines = new List<string>()
            {
                "#define BLIT_PASS"
            },

            Includes = new List<string>()
            {
               "#include \"Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassPostProcess.hlsl\""
            },

            RequiredFields = new List<string>()
            {
                "AttributesMesh.vertexID",
            },

            PixelShaderSlots = new List<int>()
            {
                PostProcessMasterNode.BaseColorSlotId
            },

            VertexShaderSlots = new List<int>()
            {
            },

            UseInPreview = true,
            OnGeneratePassImpl = (IMasterNode node, ref Pass pass) =>
            {         
            }
        };
       
        public int GetPreviewPassIndex() { return 1; }

        private static HashSet<string> GetActiveFieldsFromMasterNode(AbstractMaterialNode iMasterNode, Pass pass)
        {
            HashSet<string> activeFields = new HashSet<string>();          
            return activeFields;
        }

        private static bool GenerateShaderPass(PostProcessMasterNode masterNode, Pass pass, GenerationMode mode, ShaderGenerator result, List<string> sourceAssetDependencyPaths)
        {
            if (mode == GenerationMode.ForReals || pass.UseInPreview)
            {
                pass.OnGeneratePass(masterNode);

                // apply master node options to active fields
                HashSet<string> activeFields = GetActiveFieldsFromMasterNode(masterNode, pass);

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
                // PostProcessSubShader.cs
                sourceAssetDependencyPaths.Add(AssetDatabase.GUIDToAssetPath("9479058f49a0c45439570b0e30882800"));
                // HDSubShaderUtilities.cs
                sourceAssetDependencyPaths.Add(AssetDatabase.GUIDToAssetPath("713ced4e6eef4a44799a4dd59041484b"));
            }

            var masterNode = iMasterNode as PostProcessMasterNode;

            var subShader = new ShaderGenerator();
            subShader.AddShaderChunk("SubShader", true);
            subShader.AddShaderChunk("{", true);
            subShader.Indent();
            {                
                GenerateShaderPass(masterNode, m_PassPostProcess, mode, subShader, sourceAssetDependencyPaths);

                if (mode.IsPreview())
                {
                    GenerateShaderPass(masterNode, m_PassPostProcessPreview, mode, subShader, sourceAssetDependencyPaths);
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
