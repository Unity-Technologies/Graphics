using System.Collections.Generic;
using Data.Util;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph;
using UnityEditor.ShaderGraph.Internal;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using ShaderPass = UnityEditor.ShaderGraph.Internal.ShaderPass;

namespace UnityEditor.Rendering.HighDefinition
{
    [FormerName("UnityEditor.Experimental.Rendering.HDPipeline.DecalSubShader")]
    class DecalSubShader : ISubShader
    {
        private static ActiveFields GetActiveFieldsFromMasterNode(DecalMasterNode masterNode, ShaderPass pass)
        {
            var activeFields = new ActiveFields();
            var baseActiveFields = activeFields.baseInstance;

            // Graph Vertex
            if(masterNode.IsSlotConnected(PBRMasterNode.PositionSlotId) || 
               masterNode.IsSlotConnected(PBRMasterNode.VertNormalSlotId) || 
               masterNode.IsSlotConnected(PBRMasterNode.VertTangentSlotId))
            {
                baseActiveFields.Add("features.graphVertex");
            }

            // Graph Pixel (always enabled)
            baseActiveFields.Add("features.graphPixel");
            
            if(masterNode.affectsAlbedo.isOn)
            {
                baseActiveFields.Add("Material.AffectsAlbedo");
            }
            if (masterNode.affectsNormal.isOn)
            {
                baseActiveFields.Add("Material.AffectsNormal");
            }
            if (masterNode.affectsEmission.isOn)
            {
                baseActiveFields.Add("Material.AffectsEmission");
            }
            if (masterNode.affectsSmoothness.isOn || masterNode.affectsMetal.isOn || masterNode.affectsAO.isOn)
            {
                baseActiveFields.Add("Material.AffectsMaskMap");
            }

            return activeFields;
        }

        private static bool GenerateShaderPass(DecalMasterNode masterNode, ITarget target, ShaderPass pass, GenerationMode mode, ShaderGenerator result, List<string> sourceAssetDependencyPaths)
        {
            if(mode == GenerationMode.Preview && !pass.useInPreview)
                return false;
            
            if(pass.Equals(HDRPDecalTarget.Passes.Projector4RT) || pass.Equals(HDRPDecalTarget.Passes.Mesh4RT))
            {
                int colorMaskIndex = (masterNode.affectsMetal.isOn ? 1 : 0);
                colorMaskIndex |= (masterNode.affectsAO.isOn ? 2 : 0);
                colorMaskIndex |= (masterNode.affectsSmoothness.isOn ? 4 : 0);
                pass.ColorMaskOverride = HDRPDecalTarget.ColorMasks[colorMaskIndex];
            }

            // Active Fields
            var activeFields = GetActiveFieldsFromMasterNode(masterNode, pass);
            
            // Generate
            return GenerationUtils.GenerateShaderPass(masterNode, target, pass, mode, activeFields, result, sourceAssetDependencyPaths,
                HDRPShaderStructs.s_Dependencies, HDRPShaderStructs.s_ResourceClassName, HDRPShaderStructs.s_AssemblyName);
        }

        public string GetSubshader(AbstractMaterialNode outputNode, ITarget target, GenerationMode mode, List<string> sourceAssetDependencyPaths = null)
        {
            if (sourceAssetDependencyPaths != null)
            {
                // DecalSubShader.cs
                sourceAssetDependencyPaths.Add(AssetDatabase.GUIDToAssetPath("3b523fb79ded88842bb5195be78e0354"));
                // HDSubShaderUtilities.cs
                sourceAssetDependencyPaths.Add(AssetDatabase.GUIDToAssetPath("713ced4e6eef4a44799a4dd59041484b"));
            }

            var masterNode = outputNode as DecalMasterNode;

            var subShader = new ShaderGenerator();
            subShader.AddShaderChunk("SubShader", true);
            subShader.AddShaderChunk("{", true);
            subShader.Indent();
            {
                // Add tags at the SubShader level
                int queue = HDRenderQueue.ChangeType(HDRenderQueue.RenderQueueType.Opaque, masterNode.drawOrder, false);
                HDSubShaderUtilities.AddTags(subShader, HDRenderPipeline.k_ShaderTagName, HDRenderTypeTags.Opaque, queue);

                // Caution: Order of GenerateShaderPass matter. Only generate required pass
                if (masterNode.affectsAlbedo.isOn || masterNode.affectsNormal.isOn || masterNode.affectsMetal.isOn || masterNode.affectsAO.isOn || masterNode.affectsSmoothness.isOn)
                {
                    GenerateShaderPass(masterNode, target, HDRPDecalTarget.Passes.Projector3RT, mode, subShader, sourceAssetDependencyPaths);
                    GenerateShaderPass(masterNode, target, HDRPDecalTarget.Passes.Projector4RT, mode, subShader, sourceAssetDependencyPaths);
                }
                if (masterNode.affectsEmission.isOn)
                {
                    GenerateShaderPass(masterNode, target, HDRPDecalTarget.Passes.ProjectorEmissive, mode, subShader, sourceAssetDependencyPaths);
                }
                if (masterNode.affectsAlbedo.isOn || masterNode.affectsNormal.isOn || masterNode.affectsMetal.isOn || masterNode.affectsAO.isOn || masterNode.affectsSmoothness.isOn)
                {
                    GenerateShaderPass(masterNode, target, HDRPDecalTarget.Passes.Mesh3RT, mode, subShader, sourceAssetDependencyPaths);
                    GenerateShaderPass(masterNode, target, HDRPDecalTarget.Passes.Mesh4RT, mode, subShader, sourceAssetDependencyPaths);
                }
                if (masterNode.affectsEmission.isOn)
                {
                    GenerateShaderPass(masterNode, target, HDRPDecalTarget.Passes.MeshEmissive, mode, subShader, sourceAssetDependencyPaths);
                }

                if (mode.IsPreview())
                {
                    GenerateShaderPass(masterNode, target, HDRPDecalTarget.Passes.Preview, mode, subShader, sourceAssetDependencyPaths);
                }
            }
            subShader.Deindent();
            subShader.AddShaderChunk("}", true);
            subShader.AddShaderChunk(@"CustomEditor ""UnityEditor.Rendering.HighDefinition.DecalGUI""");
            string s = subShader.GetShaderString(0);
            return s;
        }
    }
}
