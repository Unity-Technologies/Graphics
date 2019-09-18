using System.Collections.Generic;
using System.Linq;
using Data.Util;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Rendering;
using UnityEditor.ShaderGraph.Internal;
using ShaderPass = UnityEditor.ShaderGraph.Internal.ShaderPass;

namespace UnityEditor.Rendering.HighDefinition
{
    [FormerName("UnityEditor.Experimental.Rendering.HDPipeline.UnlitSubShader")]
    class UnlitSubShader : ISubShader
    {
        public static void GetBlendMode(ShaderGraph.SurfaceType surfaceType, AlphaMode alphaMode, ref ShaderPass pass)
        {
            if (surfaceType == ShaderGraph.SurfaceType.Opaque)
            {
                pass.BlendOverride = "Blend One Zero, One Zero";
            }
            else
            {
                switch (alphaMode)
                {
                    case AlphaMode.Alpha:
                        pass.BlendOverride = "Blend One OneMinusSrcAlpha, One OneMinusSrcAlpha";
                        break;
                    case AlphaMode.Additive:
                        pass.BlendOverride = "Blend One One, One One";
                        break;
                    case AlphaMode.Premultiply:
                        pass.BlendOverride = "Blend One OneMinusSrcAlpha, One OneMinusSrcAlpha";
                        break;
                    // This isn't supported in HDRP.
                    case AlphaMode.Multiply:
                    default:
                        pass.BlendOverride = "Blend One OneMinusSrcAlpha, One OneMinusSrcAlpha";
                        break;
                }
            }
        }

        public static void GetCullMode(bool doubleSided, ref ShaderPass pass)
        {
            if (doubleSided)
                pass.CullOverride = "Cull Off";
        }

        public static void GetZWrite(ShaderGraph.SurfaceType surfaceType, ref ShaderPass pass)
        {
            if (surfaceType == ShaderGraph.SurfaceType.Opaque)
            {
                pass.ZWriteOverride = "ZWrite On";
            }
            else
            {
                pass.ZWriteOverride = "ZWrite Off";
            }
        }

        private static ActiveFields GetActiveFieldsFromMasterNode(AbstractMaterialNode iMasterNode, ShaderPass pass)
        {
            var activeFields = new ActiveFields();
            var baseActiveFields = activeFields.baseInstance;
            UnlitMasterNode masterNode = iMasterNode as UnlitMasterNode;

            // Graph Vertex
            if(masterNode.IsSlotConnected(PBRMasterNode.PositionSlotId) || 
               masterNode.IsSlotConnected(PBRMasterNode.VertNormalSlotId) || 
               masterNode.IsSlotConnected(PBRMasterNode.VertTangentSlotId))
            {
                baseActiveFields.Add("features.graphVertex");
            }

            // Graph Pixel (always enabled)
            baseActiveFields.Add("features.graphPixel");

            // Alpha Test
            if (masterNode.IsSlotConnected(UnlitMasterNode.AlphaThresholdSlotId) ||
                masterNode.GetInputSlots<Vector1MaterialSlot>().First(x => x.id == UnlitMasterNode.AlphaThresholdSlotId).value > 0.0f)
            {
                baseActiveFields.Add("AlphaTest");
            }

            // Transparent
            if (masterNode.surfaceType != ShaderGraph.SurfaceType.Opaque)
            {
                // #pragma shader_feature _SURFACE_TYPE_TRANSPARENT
                baseActiveFields.Add("SurfaceType.Transparent");

                // #pragma shader_feature _ _BLENDMODE_ALPHA _BLENDMODE_ADD _BLENDMODE_PRE_MULTIPLY
                if (masterNode.alphaMode == AlphaMode.Alpha)
                {
                    baseActiveFields.Add("BlendMode.Alpha");
                }
                else if (masterNode.alphaMode == AlphaMode.Additive)
                {
                    baseActiveFields.Add("BlendMode.Add");
                }
            }
            // Opaque
            else
            {
                
            }

            // Precomputed Velocity
            if (masterNode.addPrecomputedVelocity.isOn)
            {
                baseActiveFields.Add("AddPrecomputedVelocity");
            }

            return activeFields;
        }

        private static bool GenerateShaderPassUnlit(UnlitMasterNode masterNode, ITarget target, ShaderPass pass, GenerationMode mode, ShaderGenerator result, List<string> sourceAssetDependencyPaths)
        {
            if(pass.Equals(HDRPMeshTarget.Passes.UnlitShadowCaster))
            {
                GetCullMode(masterNode.twoSided.isOn, ref pass);
            }
            else if(pass.Equals(HDRPMeshTarget.Passes.UnlitSceneSelection))
            {
                GetCullMode(masterNode.twoSided.isOn, ref pass);
                GetZWrite(masterNode.surfaceType, ref pass);
            }
            else if(pass.Equals(HDRPMeshTarget.Passes.UnlitDepthForwardOnly))
            {
                GetCullMode(masterNode.twoSided.isOn, ref pass);
                GetZWrite(masterNode.surfaceType, ref pass);
            }
            else if(pass.Equals(HDRPMeshTarget.Passes.UnlitMotionVectors))
            {
                GetCullMode(masterNode.twoSided.isOn, ref pass);
            }
            else if(pass.Equals(HDRPMeshTarget.Passes.UnlitForwardOnly))
            {
                GetBlendMode(masterNode.surfaceType, masterNode.alphaMode, ref pass);
                GetCullMode(masterNode.twoSided.isOn, ref pass);
                GetZWrite(masterNode.surfaceType, ref pass);
            }

            // apply master node options to active fields
            var activeFields = GetActiveFieldsFromMasterNode(masterNode, pass);

            return GenerationUtils.GenerateShaderPass(masterNode, target, pass, mode, activeFields, result, sourceAssetDependencyPaths,
                HDRPShaderStructs.s_Dependencies, HDRPShaderStructs.s_ResourceClassName, HDRPShaderStructs.s_AssemblyName);
        }

        public string GetSubshader(AbstractMaterialNode outputNode, ITarget target, GenerationMode mode, List<string> sourceAssetDependencyPaths = null)
        {
            if (sourceAssetDependencyPaths != null)
            {
                // UnlitSubShader.cs
                sourceAssetDependencyPaths.Add(AssetDatabase.GUIDToAssetPath("a32a2cf536cae8e478ca1bbb7b9c493b"));
                // HDSubShaderUtilities.cs
                sourceAssetDependencyPaths.Add(AssetDatabase.GUIDToAssetPath("713ced4e6eef4a44799a4dd59041484b"));
            }

            var masterNode = outputNode as UnlitMasterNode;
            var subShader = new ShaderGenerator();
            subShader.AddShaderChunk("SubShader", true);
            subShader.AddShaderChunk("{", true);
            subShader.Indent();
            {
                var renderingPass = masterNode.surfaceType == ShaderGraph.SurfaceType.Opaque ? HDRenderQueue.RenderQueueType.Opaque : HDRenderQueue.RenderQueueType.Transparent;
                int queue = HDRenderQueue.ChangeType(renderingPass, 0, true);
                HDSubShaderUtilities.AddTags(subShader, HDRenderPipeline.k_ShaderTagName, HDRenderTypeTags.HDUnlitShader, queue);

                // generate the necessary shader passes
                bool opaque = (masterNode.surfaceType == ShaderGraph.SurfaceType.Opaque);

                GenerateShaderPassUnlit(masterNode, target, HDRPMeshTarget.Passes.UnlitShadowCaster, mode, subShader, sourceAssetDependencyPaths);
                GenerateShaderPassUnlit(masterNode, target, HDRPMeshTarget.Passes.UnlitMETA, mode, subShader, sourceAssetDependencyPaths);
                GenerateShaderPassUnlit(masterNode, target, HDRPMeshTarget.Passes.UnlitSceneSelection, mode, subShader, sourceAssetDependencyPaths);

                if (opaque)
                {
                    GenerateShaderPassUnlit(masterNode, target, HDRPMeshTarget.Passes.UnlitDepthForwardOnly, mode, subShader, sourceAssetDependencyPaths);
                    GenerateShaderPassUnlit(masterNode, target, HDRPMeshTarget.Passes.UnlitMotionVectors, mode, subShader, sourceAssetDependencyPaths);
                }

                GenerateShaderPassUnlit(masterNode, target, HDRPMeshTarget.Passes.UnlitForwardOnly, mode, subShader, sourceAssetDependencyPaths);
            }
            subShader.Deindent();
            subShader.AddShaderChunk("}", true);

            subShader.AddShaderChunk(@"CustomEditor ""UnityEditor.Rendering.HighDefinition.UnlitUI""");

            return subShader.GetShaderString(0);
        }
    }
}