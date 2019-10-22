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
            ShaderPassName = "SHADERPASS_DBUFFER_PROJECTOR",

            CullOverride = "Cull Front",
            ZTestOverride = "ZTest Greater",
            ZWriteOverride = "ZWrite Off",
            BlendOverride = "Blend 0 SrcAlpha OneMinusSrcAlpha, Zero OneMinusSrcAlpha Blend 1 SrcAlpha OneMinusSrcAlpha, Zero OneMinusSrcAlpha Blend 2 SrcAlpha OneMinusSrcAlpha, Zero OneMinusSrcAlpha",

            ExtraDefines = new List<string>()
            {
               // "#define PostProcessS_3RT",
            },

            Includes = new List<string>()
            {
               // "#include \"Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassPostProcess.hlsl\""
            },

            RequiredFields = new List<string>()
            {
            },

            PixelShaderSlots = new List<int>()
            {
                PostProcessMasterNode.AlbedoSlotId,
                PostProcessMasterNode.BaseColorOpacitySlotId,
                PostProcessMasterNode.NormalSlotId,
                PostProcessMasterNode.NormaOpacitySlotId,
                PostProcessMasterNode.MetallicSlotId,
                PostProcessMasterNode.AmbientOcclusionSlotId,
                PostProcessMasterNode.SmoothnessSlotId,
                PostProcessMasterNode.MAOSOpacitySlotId,
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
            MaterialName = "PostProcess",
            ShaderPassName = "SHADERPASS_DBUFFER_PROJECTOR",

            CullOverride = "Cull Front",
            ZTestOverride = "ZTest Greater",
            ZWriteOverride = "ZWrite Off",
            BlendOverride = "Blend 0 SrcAlpha OneMinusSrcAlpha, Zero OneMinusSrcAlpha Blend 1 SrcAlpha OneMinusSrcAlpha, Zero OneMinusSrcAlpha Blend 2 SrcAlpha OneMinusSrcAlpha, Zero OneMinusSrcAlpha",

            ExtraDefines = new List<string>()
            {
               // "#define PostProcessS_3RT",
            },

            Includes = new List<string>()
            {
               // "#include \"Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassPostProcess.hlsl\""
            },

            RequiredFields = new List<string>()
            {
            },

            PixelShaderSlots = new List<int>()
            {
                PostProcessMasterNode.AlbedoSlotId,
                PostProcessMasterNode.BaseColorOpacitySlotId,
                PostProcessMasterNode.NormalSlotId,
                PostProcessMasterNode.NormaOpacitySlotId,
                PostProcessMasterNode.MetallicSlotId,
                PostProcessMasterNode.AmbientOcclusionSlotId,
                PostProcessMasterNode.SmoothnessSlotId,
                PostProcessMasterNode.MAOSOpacitySlotId,
            },

            VertexShaderSlots = new List<int>()
            {
            },

            UseInPreview = false,
            OnGeneratePassImpl = (IMasterNode node, ref Pass pass) =>
            {         
            }
        };
       
        public int GetPreviewPassIndex() { return 0; }

        private static string[] m_ColorMasks = new string[8]
        {
            "ColorMask 0 2 ColorMask 0 3",     // nothing
            "ColorMask R 2 ColorMask R 3",     // metal
            "ColorMask G 2 ColorMask G 3",     // AO
            "ColorMask RG 2 ColorMask RG 3",    // metal + AO
            "ColorMask BA 2 ColorMask 0 3",     // smoothness
            "ColorMask RBA 2 ColorMask R 3",     // metal + smoothness
            "ColorMask GBA 2 ColorMask G 3",     // AO + smoothness
            "ColorMask RGBA 2 ColorMask RG 3",   // metal + AO + smoothness
        };


        private static HashSet<string> GetActiveFieldsFromMasterNode(AbstractMaterialNode iMasterNode, Pass pass)
        {
            HashSet<string> activeFields = new HashSet<string>();

            PostProcessMasterNode masterNode = iMasterNode as PostProcessMasterNode;
            if (masterNode == null)
            {
                return activeFields;
            }
            if(masterNode.affectsAlbedo.isOn)
            {
                activeFields.Add("Material.AffectsAlbedo");
            }
            if (masterNode.affectsNormal.isOn)
            {
                activeFields.Add("Material.AffectsNormal");
            }
            if (masterNode.affectsEmission.isOn)
            {
                activeFields.Add("Material.AffectsEmission");
            }
            if (masterNode.affectsSmoothness.isOn || masterNode.affectsMetal.isOn || masterNode.affectsAO.isOn)
            {
                activeFields.Add("Material.AffectsMaskMap");
            }

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
                bool vertexActive = masterNode.IsSlotConnected(PostProcessMasterNode.PositionSlotId);
                return HDSubShaderUtilities.GenerateShaderPass(masterNode, pass, mode, activeFields, result, sourceAssetDependencyPaths, vertexActive);
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
                sourceAssetDependencyPaths.Add(AssetDatabase.GUIDToAssetPath("3b523fb79ded88842bb5195be78e0354"));
                // HDSubShaderUtilities.cs
                sourceAssetDependencyPaths.Add(AssetDatabase.GUIDToAssetPath("713ced4e6eef4a44799a4dd59041484b"));
            }

            var masterNode = iMasterNode as PostProcessMasterNode;

            var subShader = new ShaderGenerator();
            subShader.AddShaderChunk("SubShader", true);
            subShader.AddShaderChunk("{", true);
            subShader.Indent();
            {
                // Add tags at the SubShader level
                int queue = HDRenderQueue.ChangeType(HDRenderQueue.RenderQueueType.Opaque, masterNode.drawOrder, false);
                HDSubShaderUtilities.AddTags(subShader, HDRenderPipeline.k_ShaderTagName, HDRenderTypeTags.Opaque, queue);
                GenerateShaderPass(masterNode, m_PassPostProcess, mode, subShader, sourceAssetDependencyPaths);

                if (mode.IsPreview())
                {
                    GenerateShaderPass(masterNode, m_PassPostProcessPreview, mode, subShader, sourceAssetDependencyPaths);
                }
            }
            subShader.Deindent();
            subShader.AddShaderChunk("}", true);
            subShader.AddShaderChunk(@"CustomEditor ""UnityEditor.Experimental.Rendering.HDPipeline.PostProcessGUI""");
            string s = subShader.GetShaderString(0);
            return s;
        }

        public bool IsPipelineCompatible(RenderPipelineAsset renderPipelineAsset)
        {
            return renderPipelineAsset is HDRenderPipelineAsset;
        }
    }
}
