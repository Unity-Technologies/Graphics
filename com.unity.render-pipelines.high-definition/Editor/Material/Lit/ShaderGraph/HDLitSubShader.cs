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
        Pass m_PassRaytracingIndirect = new Pass()
        {
        };

        Pass m_PassRaytracingVisibility = new Pass()
        {
        };

        Pass m_PassRaytracingForward = new Pass()
        {
        };

        Pass m_PassRaytracingGBuffer = new Pass()
        {
        };

        private static ActiveFields GetActiveFieldsFromMasterNode(HDLitMasterNode masterNode, ShaderPass pass)
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

            // Double Sided
            if (masterNode.doubleSidedMode != DoubleSidedMode.Disabled)
            {
                baseActiveFields.AddAll("DoubleSided");
                if (pass.referenceName != "SHADERPASS_MOTION_VECTORS")  // HACK to get around lack of a good interpolator dependency system
                {                                                       // we need to be able to build interpolators using multiple input structs
                                                                        // also: should only require isFrontFace if Normals are required...
                    if (masterNode.doubleSidedMode == DoubleSidedMode.FlippedNormals)
                    {
                        baseActiveFields.AddAll("DoubleSided.Flip");
                    }
                    else if (masterNode.doubleSidedMode == DoubleSidedMode.MirroredNormals)
                    {
                        baseActiveFields.AddAll("DoubleSided.Mirror");
                    }
                    // Important: the following is used in SharedCode.template.hlsl for determining the normal flip mode
                    baseActiveFields.AddAll("FragInputs.isFrontFace");
                }
            }

            switch (masterNode.materialType)
            {
                case HDLitMasterNode.MaterialType.Anisotropy:
                    baseActiveFields.AddAll("Material.Anisotropy");
                    break;
                case HDLitMasterNode.MaterialType.Iridescence:
                    baseActiveFields.AddAll("Material.Iridescence");
                    break;
                case HDLitMasterNode.MaterialType.SpecularColor:
                    baseActiveFields.AddAll("Material.SpecularColor");
                    break;
                case HDLitMasterNode.MaterialType.Standard:
                    baseActiveFields.AddAll("Material.Standard");
                    break;
                case HDLitMasterNode.MaterialType.SubsurfaceScattering:
                    {
                        if (masterNode.surfaceType != SurfaceType.Transparent)
                        {
                            baseActiveFields.AddAll("Material.SubsurfaceScattering");
                        }
                        if (masterNode.sssTransmission.isOn)
                        {
                            baseActiveFields.AddAll("Material.Transmission");
                        }
                    }
                    break;
                case HDLitMasterNode.MaterialType.Translucent:
                    {
                        baseActiveFields.AddAll("Material.Translucent");
                        baseActiveFields.AddAll("Material.Transmission");
                    }
                    break;
                default:
                    UnityEngine.Debug.LogError("Unknown material type: " + masterNode.materialType);
                    break;
            }

            if (masterNode.alphaTest.isOn)
            {
                int count = 0;
                // If alpha test shadow is enable, we use it, otherwise we use the regular test
                if (pass.pixelPorts.Contains(HDLitMasterNode.AlphaThresholdShadowSlotId) && masterNode.alphaTestShadow.isOn)
                {
                    baseActiveFields.AddAll("AlphaTestShadow");
                    ++count;
                }
                else if (pass.pixelPorts.Contains(HDLitMasterNode.AlphaThresholdSlotId))
                {
                    baseActiveFields.AddAll("AlphaTest");
                    ++count;
                }

                if (pass.pixelPorts.Contains(HDLitMasterNode.AlphaThresholdDepthPrepassSlotId))
                {
                    baseActiveFields.AddAll("AlphaTestPrepass");
                    ++count;
                }
                if (pass.pixelPorts.Contains(HDLitMasterNode.AlphaThresholdDepthPostpassSlotId))
                {
                    baseActiveFields.AddAll("AlphaTestPostpass");
                    ++count;
                }
                UnityEngine.Debug.Assert(count == 1, "Alpha test value not set correctly");
            }

            // Transparent
            if (masterNode.surfaceType != SurfaceType.Opaque)
            {
                // #pragma shader_feature _SURFACE_TYPE_TRANSPARENT
                baseActiveFields.Add("SurfaceType.Transparent");

                if (masterNode.transparencyFog.isOn)
                {
                    baseActiveFields.AddAll("AlphaFog");
                }
                if (masterNode.transparentWritesMotionVec.isOn)
                {
                    baseActiveFields.AddAll("TransparentWritesMotionVec");
                }
                if (masterNode.blendPreserveSpecular.isOn)
                {
                    baseActiveFields.AddAll("BlendMode.PreserveSpecular");
                }

                // #pragma shader_feature _ _BLENDMODE_ALPHA _BLENDMODE_ADD _BLENDMODE_PRE_MULTIPLY
                if (masterNode.alphaMode == AlphaMode.Alpha)
                {
                    baseActiveFields.Add("BlendMode.Alpha");
                }
                else if (masterNode.alphaMode == AlphaMode.Additive)
                {
                    baseActiveFields.Add("BlendMode.Add");
                }
                else if (masterNode.alphaMode == AlphaMode.Additive)
                {
                    baseActiveFields.Add("BlendMode.Multiply");
                }
            }
            // Opaque
            else
            {
                
            }

            if (!masterNode.receiveDecals.isOn)
            {
                baseActiveFields.AddAll("DisableDecals");
            }

            if (!masterNode.receiveSSR.isOn)
            {
                baseActiveFields.AddAll("DisableSSR");
            }

            if (masterNode.addPrecomputedVelocity.isOn)
            {
                baseActiveFields.Add("AddPrecomputedVelocity");
            }

            if (masterNode.specularAA.isOn && pass.pixelPorts.Contains(HDLitMasterNode.SpecularAAThresholdSlotId) && pass.pixelPorts.Contains(HDLitMasterNode.SpecularAAScreenSpaceVarianceSlotId))
            {
                baseActiveFields.AddAll("Specular.AA");
            }

            if (masterNode.energyConservingSpecular.isOn)
            {
                baseActiveFields.AddAll("Specular.EnergyConserving");
            }

            if (masterNode.HasRefraction())
            {
                baseActiveFields.AddAll("Refraction");
                switch (masterNode.refractionModel)
                {
                    case ScreenSpaceRefraction.RefractionModel.Box:
                        baseActiveFields.AddAll("RefractionBox");
                        break;

                    case ScreenSpaceRefraction.RefractionModel.Sphere:
                        baseActiveFields.AddAll("RefractionSphere");
                        break;

                    default:
                        UnityEngine.Debug.LogError("Unknown refraction model: " + masterNode.refractionModel);
                        break;
                }
            }

            if (masterNode.IsSlotConnected(HDLitMasterNode.BentNormalSlotId) && pass.pixelPorts.Contains(HDLitMasterNode.BentNormalSlotId))
            {
                baseActiveFields.AddAll("BentNormal");
            }

            if (masterNode.IsSlotConnected(HDLitMasterNode.TangentSlotId) && pass.pixelPorts.Contains(HDLitMasterNode.TangentSlotId))
            {
                baseActiveFields.AddAll("Tangent");
            }

            switch (masterNode.specularOcclusionMode)
            {
                case SpecularOcclusionMode.Off:
                    break;
                case SpecularOcclusionMode.FromAO:
                    baseActiveFields.AddAll("SpecularOcclusionFromAO");
                    break;
                case SpecularOcclusionMode.FromAOAndBentNormal:
                    baseActiveFields.AddAll("SpecularOcclusionFromAOBentNormal");
                    break;
                case SpecularOcclusionMode.Custom:
                    baseActiveFields.AddAll("SpecularOcclusionCustom");
                    break;

                default:
                    break;
            }

            if (pass.pixelPorts.Contains(HDLitMasterNode.AmbientOcclusionSlotId))
            {
                var occlusionSlot = masterNode.FindSlot<Vector1MaterialSlot>(HDLitMasterNode.AmbientOcclusionSlotId);

                bool connected = masterNode.IsSlotConnected(HDLitMasterNode.AmbientOcclusionSlotId);
                if (connected || occlusionSlot.value != occlusionSlot.defaultValue)
                {
                    baseActiveFields.AddAll("AmbientOcclusion");
                }
            }

            if (pass.pixelPorts.Contains(HDLitMasterNode.CoatMaskSlotId))
            {
                var coatMaskSlot = masterNode.FindSlot<Vector1MaterialSlot>(HDLitMasterNode.CoatMaskSlotId);

                bool connected = masterNode.IsSlotConnected(HDLitMasterNode.CoatMaskSlotId);
                if (connected || coatMaskSlot.value > 0.0f)
                {
                    baseActiveFields.AddAll("CoatMask");
                }
            }

            if (masterNode.IsSlotConnected(HDLitMasterNode.LightingSlotId) && pass.pixelPorts.Contains(HDLitMasterNode.LightingSlotId))
            {
                baseActiveFields.AddAll("LightingGI");
            }
            if (masterNode.IsSlotConnected(HDLitMasterNode.BackLightingSlotId) && pass.pixelPorts.Contains(HDLitMasterNode.LightingSlotId))
            {
                baseActiveFields.AddAll("BackLightingGI");
            }

            if (masterNode.depthOffset.isOn && pass.pixelPorts.Contains(HDLitMasterNode.DepthOffsetSlotId))
                baseActiveFields.AddAll("DepthOffset");

            return activeFields;
        }

        private static bool GenerateShaderPassLit(HDLitMasterNode masterNode, ITarget target, ShaderPass pass, GenerationMode mode, ShaderGenerator result, List<string> sourceAssetDependencyPaths)
        {
            if(mode == GenerationMode.Preview && !pass.useInPreview)
                return false;
            
            // Render state
            if(pass.Equals(HDRPMeshTarget.Passes.HDLitDistortion))
            {
                if (masterNode.distortionDepthTest.isOn)
                {
                    pass.ZTestOverride = "ZTest LEqual";
                }
                else
                {
                    pass.ZTestOverride = "ZTest Always";
                }
                if (masterNode.distortionMode == DistortionMode.Add)
                {
                    pass.BlendOverride = "Blend One One, One One";
                    pass.BlendOpOverride = "BlendOp Add, Add";
                }
                else if (masterNode.distortionMode == DistortionMode.Multiply)
                {
                    pass.BlendOverride = "Blend DstColor Zero, DstAlpha Zero";
                    pass.BlendOpOverride = "BlendOp Add, Add";
                }
                else // (masterNode.distortionMode == DistortionMode.Replace)
                {
                    pass.BlendOverride = "Blend One Zero, One Zero";
                    pass.BlendOpOverride = "BlendOp Add, Add";
                }
            }
            else if(pass.Equals(HDRPMeshTarget.Passes.HDLitForwardOpaque) || pass.Equals(HDRPMeshTarget.Passes.HDLitForwardOpaque))
            {
                if (masterNode.surfaceType == SurfaceType.Opaque && masterNode.alphaTest.isOn)
                {
                    pass.ZTestOverride = "ZTest Equal";
                }
            }

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
            var activeFields = GetActiveFieldsFromMasterNode(masterNode, pass);
            
            // Generate
            return GenerationUtils.GenerateShaderPass(masterNode, target, pass, mode, activeFields, result, sourceAssetDependencyPaths,
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
                        GenerateShaderPassLit(masterNode, target, HDRPRaytracingMeshTarget.Passes.HDLitIndirect, mode, subShader, sourceAssetDependencyPaths);
                        GenerateShaderPassLit(masterNode, target, HDRPRaytracingMeshTarget.Passes.HDLitVisibility, mode, subShader, sourceAssetDependencyPaths);
                        GenerateShaderPassLit(masterNode, target, HDRPRaytracingMeshTarget.Passes.HDLitForward, mode, subShader, sourceAssetDependencyPaths);
                        GenerateShaderPassLit(masterNode, target, HDRPRaytracingMeshTarget.Passes.HDLitGBuffer, mode, subShader, sourceAssetDependencyPaths);
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

                    GenerateShaderPassLit(masterNode, target, HDRPMeshTarget.Passes.HDLitShadowCaster, mode, subShader, sourceAssetDependencyPaths);
                    GenerateShaderPassLit(masterNode, target, HDRPMeshTarget.Passes.HDLitMETA, mode, subShader, sourceAssetDependencyPaths);
                    GenerateShaderPassLit(masterNode, target, HDRPMeshTarget.Passes.HDLitSceneSelection, mode, subShader, sourceAssetDependencyPaths);
                    GenerateShaderPassLit(masterNode, target, HDRPMeshTarget.Passes.HDLitDepthOnly, mode, subShader, sourceAssetDependencyPaths);
                    GenerateShaderPassLit(masterNode, target, HDRPMeshTarget.Passes.HDLitGBuffer, mode, subShader, sourceAssetDependencyPaths);
                    GenerateShaderPassLit(masterNode, target, HDRPMeshTarget.Passes.HDLitMotionVectors, mode, subShader, sourceAssetDependencyPaths);

                    if (distortionActive)
                    {
                        GenerateShaderPassLit(masterNode, target, HDRPMeshTarget.Passes.HDLitDistortion, mode, subShader, sourceAssetDependencyPaths);
                    }

                    if (transparentBackfaceActive)
                    {
                        GenerateShaderPassLit(masterNode, target, HDRPMeshTarget.Passes.HDLitTransparentBackface, mode, subShader, sourceAssetDependencyPaths);
                    }

                    if(opaque)
                    {
                        GenerateShaderPassLit(masterNode, target, HDRPMeshTarget.Passes.HDLitForwardOpaque, mode, subShader, sourceAssetDependencyPaths);
                    }
                    else
                    {
                        GenerateShaderPassLit(masterNode, target, HDRPMeshTarget.Passes.HDLitForwardTransparent, mode, subShader, sourceAssetDependencyPaths);
                    }
                    
                    if (transparentDepthPrepassActive)
                    {
                        GenerateShaderPassLit(masterNode, target, HDRPMeshTarget.Passes.HDLitTransparentDepthPrepass, mode, subShader, sourceAssetDependencyPaths);
                    }

                    if (transparentDepthPostpassActive)
                    {
                        GenerateShaderPassLit(masterNode, target, HDRPMeshTarget.Passes.HDLitTransparentDepthPostpass, mode, subShader, sourceAssetDependencyPaths);
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
