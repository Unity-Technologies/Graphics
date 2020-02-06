using System.Collections.Generic;
using System.Linq;
using Data.Util;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph;
using UnityEditor.ShaderGraph.Internal;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Rendering;

namespace UnityEditor.Rendering.HighDefinition
{
    [FormerName("UnityEditor.Experimental.Rendering.HDPipeline.HDPBRSubShader")]
    [FormerName("UnityEditor.ShaderGraph.HDPBRSubShader")]
    class HDPBRSubShader : IPBRSubShader
    {
        Pass m_PassGBuffer = new Pass()
        {
            Name = "GBuffer",
            LightMode = "GBuffer",
            TemplateName = "HDPBRPass.template",
            MaterialName = "PBR",
            ShaderPassName = "SHADERPASS_GBUFFER",

            ExtraDefines = new List<string>()
            {
                "#pragma multi_compile _ DEBUG_DISPLAY",
                "#pragma multi_compile _ LIGHTMAP_ON",
                "#pragma multi_compile _ DIRLIGHTMAP_COMBINED",
                "#pragma multi_compile _ DYNAMICLIGHTMAP_ON",
                "#pragma multi_compile _ SHADOWS_SHADOWMASK",
                "#pragma multi_compile DECALS_OFF DECALS_3RT DECALS_4RT",
                "#pragma multi_compile _ LIGHT_LAYERS",
            },
            Includes = new List<string>()
            {
                "#include \"Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassGBuffer.hlsl\"",
            },
            RequiredFields = new List<string>()
            {
                "FragInputs.tangentToWorld",
                "FragInputs.positionRWS",
                "FragInputs.texCoord1",
                "FragInputs.texCoord2"
            },
            PixelShaderSlots = new List<int>()
            {
                PBRMasterNode.AlbedoSlotId,
                PBRMasterNode.NormalSlotId,
                PBRMasterNode.MetallicSlotId,
                PBRMasterNode.SpecularSlotId,
                PBRMasterNode.EmissionSlotId,
                PBRMasterNode.SmoothnessSlotId,
                PBRMasterNode.OcclusionSlotId,
                PBRMasterNode.AlphaSlotId,
                PBRMasterNode.AlphaThresholdSlotId
            },
            VertexShaderSlots = new List<int>()
            {
                PBRMasterNode.PositionSlotId,
                PBRMasterNode.VertNormalSlotId,
                PBRMasterNode.VertTangentSlotId
            },
            UseInPreview = true,

            OnGeneratePassImpl = (IMasterNode node, ref Pass pass) =>
            {
                var masterNode = node as PBRMasterNode;
                GetStencilStateForGBuffer(true, false, ref pass);
                GetCullMode(masterNode.twoSided.isOn, ref pass);

                // When we have alpha test, we will force a depth prepass so we always bypass the clip instruction in the GBuffer
                // Don't do it with debug display mode as it is possible there is no depth prepass in this case
                // This remove is required otherwise the code generate several time the define...
                pass.ExtraDefines.Remove("#ifndef DEBUG_DISPLAY\n#define SHADERPASS_GBUFFER_BYPASS_ALPHA_TEST\n#endif");

                if (masterNode.surfaceType == UnityEditor.ShaderGraph.SurfaceType.Opaque &&
                    (masterNode.IsSlotConnected(PBRMasterNode.AlphaThresholdSlotId) ||
                     masterNode.GetInputSlots<Vector1MaterialSlot>().First(x => x.id == PBRMasterNode.AlphaThresholdSlotId).value > 0.0f))
                {
                    pass.ExtraDefines.Add("#ifndef DEBUG_DISPLAY\n#define SHADERPASS_GBUFFER_BYPASS_ALPHA_TEST\n#endif");
                    pass.ZTestOverride = "ZTest Equal";
                }
                else
                {
                    pass.ZTestOverride = "ZTest LEqual";
                }
            }
        };

        Pass m_PassMETA = new Pass()
        {
            Name = "META",
            LightMode = "META",
            TemplateName = "HDPBRPass.template",
            MaterialName = "PBR",
            ShaderPassName = "SHADERPASS_LIGHT_TRANSPORT",
            CullOverride = "Cull Off",
            Includes = new List<string>()
            {
                "#include \"Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassLightTransport.hlsl\"",
            },
            RequiredFields = new List<string>()
            {
                "AttributesMesh.normalOS",
                "AttributesMesh.tangentOS",     // Always present as we require it also in case of anisotropic lighting
                "AttributesMesh.uv0",
                "AttributesMesh.uv1",
                "AttributesMesh.color",
                "AttributesMesh.uv2",           // SHADERPASS_LIGHT_TRANSPORT always uses uv2
            },
            PixelShaderSlots = new List<int>()
            {
                PBRMasterNode.AlbedoSlotId,
                PBRMasterNode.NormalSlotId,
                PBRMasterNode.MetallicSlotId,
                PBRMasterNode.SpecularSlotId,
                PBRMasterNode.EmissionSlotId,
                PBRMasterNode.SmoothnessSlotId,
                PBRMasterNode.OcclusionSlotId,
                PBRMasterNode.AlphaSlotId,
                PBRMasterNode.AlphaThresholdSlotId
            },
            VertexShaderSlots = new List<int>()
            {
                //PBRMasterNode.PositionSlotId
            },
            UseInPreview = false
        };

        Pass m_PassShadowCaster = new Pass()
        {
            Name = "ShadowCaster",
            LightMode = "ShadowCaster",
            TemplateName = "HDPBRPass.template",
            MaterialName = "PBR",
            ShaderPassName = "SHADERPASS_SHADOWS",
            BlendOverride = "Blend One Zero",
            ZWriteOverride = "ZWrite On",
            ColorMaskOverride = "ColorMask 0",
            Includes = new List<string>()
            {
                "#include \"Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassDepthOnly.hlsl\"",
            },
            PixelShaderSlots = new List<int>()
            {
                PBRMasterNode.AlphaSlotId,
                PBRMasterNode.AlphaThresholdSlotId
            },
            VertexShaderSlots = new List<int>()
            {
                PBRMasterNode.PositionSlotId,
                PBRMasterNode.VertNormalSlotId,
                PBRMasterNode.VertTangentSlotId
            },
            UseInPreview = false,
            OnGeneratePassImpl = (IMasterNode node, ref Pass pass) =>
            {
                var masterNode = node as PBRMasterNode;
                GetCullMode(masterNode.twoSided.isOn, ref pass);
            }
        };

        Pass m_SceneSelectionPass = new Pass()
        {
            Name = "SceneSelectionPass",
            LightMode = "SceneSelectionPass",
            TemplateName = "HDPBRPass.template",
            MaterialName = "PBR",
            ShaderPassName = "SHADERPASS_DEPTH_ONLY",
            ColorMaskOverride = "ColorMask 0",
            ExtraDefines = new List<string>()
            {
                "#define SCENESELECTIONPASS",
                "#pragma editor_sync_compilation",
            },
            Includes = new List<string>()
            {
                "#include \"Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassDepthOnly.hlsl\"",
            },
            PixelShaderSlots = new List<int>()
            {
                PBRMasterNode.AlphaSlotId,
                PBRMasterNode.AlphaThresholdSlotId
            },
            VertexShaderSlots = new List<int>()
            {
                PBRMasterNode.PositionSlotId,
                PBRMasterNode.VertNormalSlotId,
                PBRMasterNode.VertTangentSlotId
            },
            UseInPreview = false,
            OnGeneratePassImpl = (IMasterNode node, ref Pass pass) =>
            {
                var masterNode = node as PBRMasterNode;
                GetCullMode(masterNode.twoSided.isOn, ref pass);
                GetZWrite(masterNode.surfaceType, ref pass);
            }
        };

        Pass m_PassDepthOnly = new Pass()
        {
            Name = "DepthOnly",
            LightMode = "DepthOnly",
            TemplateName = "HDPBRPass.template",
            MaterialName = "PBR",

            ZWriteOverride = "ZWrite On",

            ExtraDefines = HDSubShaderUtilities.s_ExtraDefinesDepthOrMotion,

            ShaderPassName = "SHADERPASS_DEPTH_ONLY",

            Includes = new List<string>()
            {
                "#include \"Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassDepthOnly.hlsl\"",
            },
            PixelShaderSlots = new List<int>()
            {
                PBRMasterNode.NormalSlotId,
                PBRMasterNode.SmoothnessSlotId,
                PBRMasterNode.AlphaSlotId,
                PBRMasterNode.AlphaThresholdSlotId
            },

            RequiredFields = new List<string>()
            {
                "AttributesMesh.normalOS",
                "AttributesMesh.tangentOS",     // Always present as we require it also in case of Variants lighting
                "AttributesMesh.uv0",
                "AttributesMesh.uv1",
                "AttributesMesh.color",
                "AttributesMesh.uv2",           // SHADERPASS_LIGHT_TRANSPORT always uses uv2
                "AttributesMesh.uv3",           // DEBUG_DISPLAY

                "FragInputs.tangentToWorld",
                "FragInputs.positionRWS",
                "FragInputs.texCoord0",
                "FragInputs.texCoord1",
                "FragInputs.texCoord2",
                "FragInputs.texCoord3",
                "FragInputs.color",
            },
            VertexShaderSlots = new List<int>()
            {
                PBRMasterNode.PositionSlotId,
                PBRMasterNode.VertNormalSlotId,
                PBRMasterNode.VertTangentSlotId
            },
            UseInPreview = true,

            OnGeneratePassImpl = (IMasterNode node, ref Pass pass) =>
            {
                var masterNode = node as PBRMasterNode;
                GetStencilStateForDepthOrMV(false, true, false, ref pass);
                GetCullMode(masterNode.twoSided.isOn, ref pass);
            }
        };

        Pass m_PassMotionVectors = new Pass()
        {
            Name = "MotionVectors",
            LightMode = "MotionVectors",
            TemplateName = "HDPBRPass.template",
            MaterialName = "PBR",
            ShaderPassName = "SHADERPASS_MOTION_VECTORS",
            ExtraDefines = HDSubShaderUtilities.s_ExtraDefinesDepthOrMotion,
            Includes = new List<string>()
            {
                "#include \"Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassMotionVectors.hlsl\"",
            },
            RequiredFields = new List<string>()
            {
                "FragInputs.positionRWS",
            },
            PixelShaderSlots = new List<int>()
            {
                PBRMasterNode.NormalSlotId,
                PBRMasterNode.SmoothnessSlotId,
                PBRMasterNode.AlphaSlotId,
                PBRMasterNode.AlphaThresholdSlotId
            },
            VertexShaderSlots = new List<int>()
            {
                PBRMasterNode.PositionSlotId,
                PBRMasterNode.VertNormalSlotId,
                PBRMasterNode.VertTangentSlotId
            },
            UseInPreview = false,

            OnGeneratePassImpl = (IMasterNode node, ref Pass pass) =>
            {
                var masterNode = node as PBRMasterNode;
                GetStencilStateForDepthOrMV(false, true, true, ref pass);
                GetCullMode(masterNode.twoSided.isOn, ref pass);
            }
        };

        Pass m_PassForward = new Pass()
        {
            Name = "Forward",
            LightMode = "Forward",
            TemplateName = "HDPBRPass.template",
            MaterialName = "PBR",
            ShaderPassName = "SHADERPASS_FORWARD",
            // ExtraDefines are set when the pass is generated
            Includes = new List<string>()
            {
                "#include \"Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassForward.hlsl\"",
            },
            RequiredFields = new List<string>()
            {
                "FragInputs.tangentToWorld",
                "FragInputs.positionRWS",	// NOTE : world-space pos is necessary for any lighting pass
                "FragInputs.texCoord1",
                "FragInputs.texCoord2"
            },
            StencilOverride = new List<string>
            {

                "// Stencil setup",
                "Stencil",
                "{",
                string.Format("   WriteMask {0}", (int) StencilUsage.RequiresDeferredLighting | (int) StencilUsage.SubsurfaceScattering),
                string.Format("   Ref  {0}", (int)StencilUsage.Clear),
                "   Comp Always",
                "   Pass Replace",
                "}"
            },
            PixelShaderSlots = new List<int>()
            {
                PBRMasterNode.AlbedoSlotId,
                PBRMasterNode.NormalSlotId,
                PBRMasterNode.MetallicSlotId,
                PBRMasterNode.SpecularSlotId,
                PBRMasterNode.EmissionSlotId,
                PBRMasterNode.SmoothnessSlotId,
                PBRMasterNode.OcclusionSlotId,
                PBRMasterNode.AlphaSlotId,
                PBRMasterNode.AlphaThresholdSlotId
            },
            VertexShaderSlots = new List<int>()
            {
                PBRMasterNode.PositionSlotId,
                PBRMasterNode.VertNormalSlotId,
                PBRMasterNode.VertTangentSlotId
            },

            OnGeneratePassImpl = (IMasterNode node, ref Pass pass) =>
            {
                var masterNode = node as PBRMasterNode;

                GetBlendMode(masterNode.surfaceType, masterNode.alphaMode, ref pass);
                GetCullMode(masterNode.twoSided.isOn, ref pass);

                pass.ExtraDefines.Remove("#ifndef DEBUG_DISPLAY\n#define SHADERPASS_FORWARD_BYPASS_ALPHA_TEST\n#endif");

                if (masterNode.surfaceType == UnityEditor.ShaderGraph.SurfaceType.Opaque &&
                    (masterNode.IsSlotConnected(PBRMasterNode.AlphaThresholdSlotId) ||
                     masterNode.GetInputSlots<Vector1MaterialSlot>().First(x => x.id == PBRMasterNode.AlphaThresholdSlotId).value > 0.0f))
                {
                    // In case of opaque we don't want to perform the alpha test, it is done in depth prepass and we use depth equal for ztest (setup from UI)
                    // Don't do it with debug display mode as it is possible there is no depth prepass in this case
                    pass.ExtraDefines.Add("#ifndef DEBUG_DISPLAY\n#define SHADERPASS_FORWARD_BYPASS_ALPHA_TEST\n#endif");
                    pass.ZTestOverride = "ZTest Equal";
                }
                else
                {
                    pass.ZTestOverride = null;
                }
            },
            UseInPreview = true
        };

        // These functions are still required because for the PBR shader use hardcoded stencil and render queues
        public static void GetStencilStateForGBuffer(bool receiveSSR, bool useSplitLighting, ref Pass pass)
        {
            int stencilWriteMask = (int)StencilUsage.RequiresDeferredLighting | (int)StencilUsage.SubsurfaceScattering;
            int stencilRef = (int)StencilUsage.RequiresDeferredLighting;
            if(useSplitLighting)
            {
                stencilRef |= (int)StencilUsage.SubsurfaceScattering;
            }

            stencilWriteMask |= (int)StencilUsage.TraceReflectionRay;
            stencilRef |= receiveSSR ? (int)StencilUsage.TraceReflectionRay : 0;

            pass.StencilOverride = new List<string>()
            {
                "// Stencil setup",
                "Stencil",
                "{",
                string.Format("   WriteMask {0}", stencilWriteMask),
                string.Format("   Ref  {0}", stencilRef),
                "   Comp Always",
                "   Pass Replace",
                "}"
            };
        }

        public static void GetStencilStateForDepthOrMV(bool receiveDecals, bool receiveSSR, bool useObjectMotionVector, ref Pass pass)
        {
            int stencilWriteMask = 0;
            int stencilRef = 0;

            stencilWriteMask |= (int)StencilUsage.TraceReflectionRay;
            stencilRef |= receiveSSR ? (int)StencilUsage.TraceReflectionRay : 0;

            stencilWriteMask |= useObjectMotionVector ? (int)StencilUsage.ObjectMotionVector : 0;
            stencilRef |= useObjectMotionVector ? (int)StencilUsage.ObjectMotionVector : 0;

            if (stencilWriteMask != 0)
            {
                pass.StencilOverride = new List<string>()
                {
                    "// Stencil setup",
                    "Stencil",
                    "{",
                    string.Format("   WriteMask {0}", stencilWriteMask),
                    string.Format("   Ref  {0}", stencilRef),
                    "   Comp Always",
                    "   Pass Replace",
                    "}"
                };
            }
        }

        public static void GetBlendMode(ShaderGraph.SurfaceType surfaceType, AlphaMode alphaMode, ref Pass pass)
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

        public static void GetCullMode(bool doubleSided, ref Pass pass)
        {
            if (doubleSided)
                pass.CullOverride = "Cull Off";
        }
        
        public static void GetZWrite(ShaderGraph.SurfaceType surfaceType, ref Pass pass)
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

        public int GetPreviewPassIndex() { return 0; }

        private static ActiveFields GetActiveFieldsFromMasterNode(AbstractMaterialNode iMasterNode, Pass pass)
        {
            var activeFields = new ActiveFields();
            var baseActiveFields = activeFields.baseInstance;

            PBRMasterNode masterNode = iMasterNode as PBRMasterNode;
            if (masterNode == null)
            {
                return activeFields;
            }

            if (masterNode.twoSided.isOn)
            {
                baseActiveFields.Add("DoubleSided");
                if (pass.ShaderPassName != "SHADERPASS_MOTION_VECTORS")   // HACK to get around lack of a good interpolator dependency system
                {                                                   // we need to be able to build interpolators using multiple input structs
                                                                    // also: should only require isFrontFace if Normals are required...
                    baseActiveFields.Add("DoubleSided.Mirror");         // TODO: change this depending on what kind of normal flip you want..
                    baseActiveFields.Add("FragInputs.isFrontFace");     // will need this for determining normal flip mode
                }
            }

            switch (masterNode.model)
            {
                case PBRMasterNode.Model.Metallic:
                    break;
                case PBRMasterNode.Model.Specular:
                    baseActiveFields.Add("Material.SpecularColor");
                    break;
                default:
                    // TODO: error!
                    break;
            }

            if (masterNode.IsSlotConnected(PBRMasterNode.AlphaThresholdSlotId) ||
                masterNode.GetInputSlots<Vector1MaterialSlot>().First(x => x.id == PBRMasterNode.AlphaThresholdSlotId).value > 0.0f)
            {
                baseActiveFields.Add("AlphaTest");
            }
            
            switch(masterNode.normalDropOffSpace)
            {
                case NormalDropOffSpace.Tangent:
                    baseActiveFields.AddAll("features.NormalDropOffTS");
                    break;
                case NormalDropOffSpace.Object:
                    baseActiveFields.AddAll("features.NormalDropOffOS");
                    break;
                case NormalDropOffSpace.World:
                    baseActiveFields.AddAll("features.NormalDropOffWS");
                    break;
                default:
                    UnityEngine.Debug.LogError("Unknown normal drop off space: " + masterNode.normalDropOffSpace);
                    break;
            }

            if (masterNode.surfaceType != UnityEditor.ShaderGraph.SurfaceType.Opaque)
            {
                baseActiveFields.Add("SurfaceType.Transparent");

                if (masterNode.alphaMode == AlphaMode.Alpha)
                {
                    baseActiveFields.Add("BlendMode.Alpha");
                }
                else if (masterNode.alphaMode == AlphaMode.Additive)
                {
                    baseActiveFields.Add("BlendMode.Add");
                }

                // By default PBR node will take the fog
                baseActiveFields.Add("AlphaFog");
            }
            else
            {
                // opaque-only defines
            }

            return activeFields;
        }

        private static bool GenerateShaderPassLit(AbstractMaterialNode masterNode, Pass pass, GenerationMode mode, ShaderGenerator result, List<string> sourceAssetDependencyPaths)
        {
            if (mode == GenerationMode.ForReals || pass.UseInPreview)
            {
                pass.OnGeneratePass(masterNode as PBRMasterNode);

                // apply master node options to active fields
                var activeFields = GetActiveFieldsFromMasterNode(masterNode, pass);

                // use standard shader pass generation
                bool vertexActive = false;
                if (masterNode.IsSlotConnected(PBRMasterNode.PositionSlotId) ||
                    masterNode.IsSlotConnected(PBRMasterNode.VertNormalSlotId) ||
                    masterNode.IsSlotConnected(PBRMasterNode.VertTangentSlotId))
                {
                    vertexActive = true;
                }
                return HDSubShaderUtilities.GenerateShaderPass(masterNode, pass, mode, activeFields, result, sourceAssetDependencyPaths, vertexActive);
            }
            else
            {
                return false;
            }
        }

        void AddTags(ShaderGenerator generator, string pipeline, HDRenderTypeTags renderType, PBRMasterNode masterNode)
        {
            var type = masterNode.surfaceType == ShaderGraph.SurfaceType.Opaque ? HDRenderQueue.RenderQueueType.Opaque : HDRenderQueue.RenderQueueType.Transparent; 
            string queue = HDRenderQueue.GetShaderTagValue(HDRenderQueue.ChangeType(type, 0, true));
            ShaderStringBuilder builder = new ShaderStringBuilder();
            builder.AppendLine("Tags");
            using (builder.BlockScope())
            {
                builder.AppendLine("\"RenderPipeline\"=\"{0}\"", pipeline);
                builder.AppendLine("\"RenderType\"=\"{0}\"", renderType);
                builder.AppendLine("\"Queue\"=\"{0}\"", queue);
            }

            generator.AddShaderChunk(builder.ToString());
        }

        public string GetSubshader(IMasterNode iMasterNode, GenerationMode mode, List<string> sourceAssetDependencyPaths = null)
        {
            if (sourceAssetDependencyPaths != null)
            {
                // HDPBRSubShader.cs
                sourceAssetDependencyPaths.Add(AssetDatabase.GUIDToAssetPath("8a6369cac4d1faf45b8715adbd364f13"));
                // HDSubShaderUtilities.cs
                sourceAssetDependencyPaths.Add(AssetDatabase.GUIDToAssetPath("713ced4e6eef4a44799a4dd59041484b"));
            }

            var masterNode = iMasterNode as PBRMasterNode;
            var subShader = new ShaderGenerator();
            subShader.AddShaderChunk("SubShader", true);
            subShader.AddShaderChunk("{", true);
            subShader.Indent();
            {
                // Add tags at the SubShader level
                AddTags(subShader, HDRenderPipeline.k_ShaderTagName, HDRenderTypeTags.HDLitShader, masterNode);

                // generate the necessary shader passes
                bool opaque = (masterNode.surfaceType == UnityEditor.ShaderGraph.SurfaceType.Opaque);

                GenerateShaderPassLit(masterNode, m_PassShadowCaster, mode, subShader, sourceAssetDependencyPaths);
                GenerateShaderPassLit(masterNode, m_PassMETA, mode, subShader, sourceAssetDependencyPaths);
                GenerateShaderPassLit(masterNode, m_SceneSelectionPass, mode, subShader, sourceAssetDependencyPaths);

                if (opaque)
                {
                    GenerateShaderPassLit(masterNode, m_PassDepthOnly, mode, subShader, sourceAssetDependencyPaths);
                    GenerateShaderPassLit(masterNode, m_PassGBuffer, mode, subShader, sourceAssetDependencyPaths);
                    GenerateShaderPassLit(masterNode, m_PassMotionVectors, mode, subShader, sourceAssetDependencyPaths);
                }

                // Assign define here based on opaque or transparent to save some variant
                m_PassForward.ExtraDefines = opaque ? HDSubShaderUtilities.s_ExtraDefinesForwardOpaque : HDSubShaderUtilities.s_ExtraDefinesForwardTransparent;
                GenerateShaderPassLit(masterNode, m_PassForward, mode, subShader, sourceAssetDependencyPaths);
            }
            subShader.Deindent();
            subShader.AddShaderChunk("}", true);

            subShader.AddShaderChunk(@"CustomEditor ""UnityEditor.Rendering.HighDefinition.HDPBRLitGUI""");

            return subShader.GetShaderString(0);
        }

        public bool IsPipelineCompatible(RenderPipelineAsset renderPipelineAsset)
        {
            return renderPipelineAsset is HDRenderPipelineAsset;
        }
    }
}
