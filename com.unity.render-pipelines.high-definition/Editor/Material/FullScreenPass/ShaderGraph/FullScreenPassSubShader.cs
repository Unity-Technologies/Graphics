using System.Collections.Generic;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph;
using UnityEngine.Experimental.Rendering.HDPipeline;
using UnityEngine.Rendering;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    class FullScreenPassSubShader : IFullScreenPassSubShader
    {
        Pass m_FullScreenPassPass = new Pass()
        {
            Name = "PostProcess",
            LightMode = "Off",
            TemplateName = "FullScreenPass.template",
            MaterialName = "FullScreenPass",
            ShaderPassName = "SHADERPASS_FULLSCREENPASS",
            PixelShaderSlots = new List<int>()
            {
                FullScreenPassMasterNode.ColorSlotId,
                FullScreenPassMasterNode.DepthSlotId,
            },
            // When we'll have more controls about the shader graph preview, we will be able to show the post process
            // But now we need the pass (as the shader have only this one, either the preview won't compile)
            UseInPreview = true,
        };

        public int GetPreviewPassIndex() { return 0; }

        private static HashSet<string> GetActiveFieldsFromMasterNode(FullScreenPassMasterNode masterNode, Pass pass)
        {
            var activeFields = new HashSet<string>();

            if (masterNode.modifyDepth.isOn)
                activeFields.Add("ModifyDepth");

            return activeFields;
        }

        private static bool GenerateShaderPass(FullScreenPassMasterNode masterNode, Pass pass, GenerationMode mode, ShaderGenerator result, List<string> sourceAssetDependencyPaths)
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
                // FullScreenPassSubShader.cs
                sourceAssetDependencyPaths.Add(AssetDatabase.GUIDToAssetPath("5bae2ab3c8993f940ba4a4a680da1402"));
                // HDSubShaderUtilities.cs
                sourceAssetDependencyPaths.Add(AssetDatabase.GUIDToAssetPath("713ced4e6eef4a44799a4dd59041484b"));
            }

            var masterNode = iMasterNode as FullScreenPassMasterNode;

            var subShader = new ShaderGenerator();
            subShader.AddShaderChunk("SubShader", false);
            subShader.AddShaderChunk("{", false);
            subShader.Indent();
            {
                // Add tags at the SubShader level
                int queue = (int)HDRenderQueue.Priority.Overlay;
                HDSubShaderUtilities.AddTags(subShader, HDRenderPipeline.k_ShaderTagName, HDRenderTypeTags.FullScreen, queue);

                // Assign define here based on opaque or transparent to save some variant
                GenerateShaderPass(masterNode, m_FullScreenPassPass, mode, subShader, sourceAssetDependencyPaths);
            }
            subShader.Deindent();
            subShader.AddShaderChunk("}", false);

            subShader.AddShaderChunk(@"CustomEditor ""UnityEditor.Experimental.Rendering.HDPipeline.FullScreenPassShaderGUI""");

            return subShader.GetShaderString(0);
        }

        public bool IsPipelineCompatible(RenderPipelineAsset renderPipelineAsset)
        {
            return renderPipelineAsset is HDRenderPipelineAsset;
        }
    }
}
