using System.Collections.Generic;
using UnityEditor.ShaderGraph;
using UnityEditor.ShaderGraph.Internal;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Rendering;
using Data.Util;
using ShaderPass = UnityEditor.ShaderGraph.Internal.ShaderPass;

namespace UnityEditor.Rendering.HighDefinition
{
    [FormerName("UnityEditor.Experimental.Rendering.HDPipeline.HDLitSubShader")]
    class HDLitSubShader : ISubShader
    {
        private static bool GenerateShaderPassLit(HDLitMasterNode masterNode, ITarget target, ShaderPass pass, GenerationMode mode, ShaderGenerator result, List<string> sourceAssetDependencyPaths)
        {
            if(mode == GenerationMode.Preview && !pass.useInPreview)
                return false;
            
            // Instancing options
            if (masterNode.dotsInstancing.isOn)
            {
                pass.defaultDotsInstancingOptions = new List<string>()
                {
                    "#pragma instancing_options nolightprobe",
                    "#pragma instancing_options nolodfade",
                };
            }
            else
            {
                pass.defaultDotsInstancingOptions = new List<string>()
                {
                    "#pragma instancing_options renderinglayer"
                };
            }

            // Active Fields
            var activeFields = GenerationUtils.GetActiveFieldsFromConditionals(masterNode.GetConditionalFields(pass));
            
            // Generate
            return GenerationUtils.GenerateShaderPass(masterNode, target, pass, mode, result, sourceAssetDependencyPaths,
                HDRPShaderStructs.s_Dependencies, HDRPShaderStructs.s_ResourceClassName, HDRPShaderStructs.s_AssemblyName);
        }

        public string GetSubshader(AbstractMaterialNode outputNode, ITarget target, GenerationMode mode, List<string> sourceAssetDependencyPaths = null)
        {
            if (sourceAssetDependencyPaths != null)
            {
                // HDLitSubShader.cs
                sourceAssetDependencyPaths.Add(AssetDatabase.GUIDToAssetPath("bac1a9627cfec924fa2ea9c65af8eeca"));
                // HDSubShaderUtilities.cs
                sourceAssetDependencyPaths.Add(AssetDatabase.GUIDToAssetPath("713ced4e6eef4a44799a4dd59041484b"));
            }

            var masterNode = outputNode as HDLitMasterNode;
            var subShader = new ShaderGenerator();
            
            // TODO: For now this SubShader is used for both MDRPMeshTarget and HDRPMeshRaytracingTarget
            if(target is HDRPRaytracingMeshTarget)
            {
                if(mode == GenerationMode.ForReals)
                {
                    subShader.AddShaderChunk("SubShader", false);
                    subShader.AddShaderChunk("{", false);
                    subShader.Indent();
                    {
                        GenerateShaderPassLit(masterNode, target, HDRPRaytracingMeshTarget.HDLitPasses.Indirect, mode, subShader, sourceAssetDependencyPaths);
                        GenerateShaderPassLit(masterNode, target, HDRPRaytracingMeshTarget.HDLitPasses.Visibility, mode, subShader, sourceAssetDependencyPaths);
                        GenerateShaderPassLit(masterNode, target, HDRPRaytracingMeshTarget.HDLitPasses.Forward, mode, subShader, sourceAssetDependencyPaths);
                        GenerateShaderPassLit(masterNode, target, HDRPRaytracingMeshTarget.HDLitPasses.GBuffer, mode, subShader, sourceAssetDependencyPaths);
                    }
                    subShader.Deindent();
                    subShader.AddShaderChunk("}", false);
                }
            }
            else // HDRPMeshTarget
            {
                subShader.AddShaderChunk("SubShader", false);
                subShader.AddShaderChunk("{", false);
                subShader.Indent();
                {
                    //Handle data migration here as we need to have a renderingPass already set with accurate data at this point.
                    if (masterNode.renderingPass == HDRenderQueue.RenderQueueType.Unknown)
                    {
                        switch (masterNode.surfaceType)
                        {
                            case SurfaceType.Opaque:
                                masterNode.renderingPass = HDRenderQueue.RenderQueueType.Opaque;
                                break;
                            case SurfaceType.Transparent:
#pragma warning disable CS0618  // Type or member is obsolete
                                if (masterNode.m_DrawBeforeRefraction)
                                {
                                    masterNode.m_DrawBeforeRefraction = false;
#pragma warning restore CS0618  // Type or member is obsolete
                                    masterNode.renderingPass = HDRenderQueue.RenderQueueType.PreRefraction;
                                }
                                else
                                {
                                    masterNode.renderingPass = HDRenderQueue.RenderQueueType.Transparent;
                                }
                                break;
                            default:
                                throw new System.ArgumentException("Unknown SurfaceType");
                        }
                    }

                    // Add tags at the SubShader level
                    int queue = HDRenderQueue.ChangeType(masterNode.renderingPass, masterNode.sortPriority, masterNode.alphaTest.isOn);
                    HDSubShaderUtilities.AddTags(subShader, HDRenderPipeline.k_ShaderTagName, HDRenderTypeTags.HDLitShader, queue);

                    // generate the necessary shader passes
                    bool opaque = (masterNode.surfaceType == SurfaceType.Opaque);
                    bool distortionActive = !opaque && masterNode.distortion.isOn;
                    bool transparentBackfaceActive = !opaque && masterNode.backThenFrontRendering.isOn;
                    bool transparentDepthPrepassActive = !opaque && masterNode.alphaTestDepthPrepass.isOn;
                    bool transparentDepthPostpassActive = !opaque && masterNode.alphaTestDepthPostpass.isOn;

                    GenerateShaderPassLit(masterNode, target, HDRPMeshTarget.HDLitPasses.ShadowCaster, mode, subShader, sourceAssetDependencyPaths);
                    GenerateShaderPassLit(masterNode, target, HDRPMeshTarget.HDLitPasses.META, mode, subShader, sourceAssetDependencyPaths);
                    GenerateShaderPassLit(masterNode, target, HDRPMeshTarget.HDLitPasses.SceneSelection, mode, subShader, sourceAssetDependencyPaths);
                    GenerateShaderPassLit(masterNode, target, HDRPMeshTarget.HDLitPasses.DepthOnly, mode, subShader, sourceAssetDependencyPaths);
                    GenerateShaderPassLit(masterNode, target, HDRPMeshTarget.HDLitPasses.GBuffer, mode, subShader, sourceAssetDependencyPaths);
                    GenerateShaderPassLit(masterNode, target, HDRPMeshTarget.HDLitPasses.MotionVectors, mode, subShader, sourceAssetDependencyPaths);

                    if (distortionActive)
                    {
                        GenerateShaderPassLit(masterNode, target, HDRPMeshTarget.HDLitPasses.DistortionVectors, mode, subShader, sourceAssetDependencyPaths);
                    }

                    if (transparentBackfaceActive)
                    {
                        GenerateShaderPassLit(masterNode, target, HDRPMeshTarget.HDLitPasses.TransparentBackface, mode, subShader, sourceAssetDependencyPaths);
                    }

                    GenerateShaderPassLit(masterNode, target, HDRPMeshTarget.HDLitPasses.Forward, mode, subShader, sourceAssetDependencyPaths);
                    
                    if (transparentDepthPrepassActive)
                    {
                        GenerateShaderPassLit(masterNode, target, HDRPMeshTarget.HDLitPasses.TransparentDepthPrepass, mode, subShader, sourceAssetDependencyPaths);
                    }

                    if (transparentDepthPostpassActive)
                    {
                        GenerateShaderPassLit(masterNode, target, HDRPMeshTarget.HDLitPasses.TransparentDepthPostpass, mode, subShader, sourceAssetDependencyPaths);
                    }
                }
                subShader.Deindent();
                subShader.AddShaderChunk("}", false);
            }
            subShader.AddShaderChunk(@"CustomEditor ""UnityEditor.Rendering.HighDefinition.HDLitGUI""");

            return subShader.GetShaderString(0);
        }
    }
}
