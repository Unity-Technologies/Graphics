using System.Collections.Generic;
using System.Linq;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph;
using UnityEngine.Experimental.Rendering.HDPipeline;
using UnityEngine.Rendering;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    class UnlitSubShader : IUnlitSubShader
    {
        Pass m_PassMETA = new Pass()
        {
            Name = "META",
            LightMode = "META",
            TemplateName = "UnlitPass.template",
            MaterialName = "Unlit",
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
                UnlitMasterNode.ColorSlotId,
                UnlitMasterNode.AlphaSlotId,
                UnlitMasterNode.AlphaThresholdSlotId
            },
            VertexShaderSlots = new List<int>()
            {
                //UnlitMasterNode.PositionSlotId
            },
            UseInPreview = false
        };

        Pass m_PassShadowCaster = new Pass()
        {
            Name = "ShadowCaster",
            LightMode = "ShadowCaster",
            TemplateName = "UnlitPass.template",
            MaterialName = "Unlit",
            ShaderPassName = "SHADERPASS_SHADOWS",
            ColorMaskOverride = "ColorMask 0",
            ExtraDefines = new List<string>(),
            Includes = new List<string>()
            {
                "#include \"Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassDepthOnly.hlsl\"",
            },
            RequiredFields = new List<string>()
            {
            },
            PixelShaderSlots = new List<int>()
            {
                UnlitMasterNode.AlphaSlotId,
                UnlitMasterNode.AlphaThresholdSlotId
            },
            VertexShaderSlots = new List<int>()
            {
                UnlitMasterNode.PositionSlotId
            },
            UseInPreview = false,
            OnGeneratePassImpl = (IMasterNode node, ref Pass pass) =>
            {
                var masterNode = node as UnlitMasterNode;
                GetCullMode(masterNode.twoSided.isOn, ref pass);
                GetZWrite(masterNode.surfaceType, ref pass);
            }
        };

        Pass m_SceneSelectionPass = new Pass()
        {
            Name = "SceneSelectionPass",
            LightMode = "SceneSelectionPass",
            TemplateName = "UnlitPass.template",
            MaterialName = "Unlit",
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
                UnlitMasterNode.AlphaSlotId,
                UnlitMasterNode.AlphaThresholdSlotId
            },
            VertexShaderSlots = new List<int>()
            {
                UnlitMasterNode.PositionSlotId
            },
            UseInPreview = false,
            OnGeneratePassImpl = (IMasterNode node, ref Pass pass) =>
            {
                var masterNode = node as UnlitMasterNode;
                GetCullMode(masterNode.twoSided.isOn, ref pass);
                GetZWrite(masterNode.surfaceType, ref pass);
            }
        };

        Pass m_PassDepthForwardOnly = new Pass()
        {
            Name = "DepthForwardOnly",
            LightMode = "DepthForwardOnly",
            TemplateName = "UnlitPass.template",
            MaterialName = "Unlit",
            ShaderPassName = "SHADERPASS_DEPTH_ONLY",
            ZWriteOverride = "ZWrite On",
            // Caution: When using MSAA we have normal and depth buffer bind.
            // Mean unlit object need to not write in it (or write 0) - Disable color mask for this RT
            // This is not a problem in no MSAA mode as there is no buffer bind
            ColorMaskOverride = "ColorMask 0 0",

            ExtraDefines = new List<string>()
            {
                "#pragma multi_compile _ WRITE_MSAA_DEPTH"
                // Note we don't need to define WRITE_NORMAL_BUFFER
            },

            Includes = new List<string>()
            {
                "#include \"Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassDepthOnly.hlsl\"",
            },
            PixelShaderSlots = new List<int>()
            {
                UnlitMasterNode.AlphaSlotId,
                UnlitMasterNode.AlphaThresholdSlotId
            },
            VertexShaderSlots = new List<int>()
            {
                UnlitMasterNode.PositionSlotId
            },
            UseInPreview = false,

            OnGeneratePassImpl = (IMasterNode node, ref Pass pass) =>
            {
                var masterNode = node as UnlitMasterNode;
                GetStencilStateForDepthOrMV(false, ref pass);
                GetCullMode(masterNode.twoSided.isOn, ref pass);
                GetZWrite(masterNode.surfaceType, ref pass);
            }
        };

        Pass m_PassMotionVectors = new Pass()
        {
            Name = "MotionVectors",
            LightMode = "MotionVectors",
            TemplateName = "UnlitPass.template",
            MaterialName = "Unlit",
            ShaderPassName = "SHADERPASS_MOTION_VECTORS",

            // Caution: When using MSAA we have motion vector, normal and depth buffer bind.
            // Mean unlit object need to not write in it (or write 0) - Disable color mask for this RT
            // This is not a problem in no MSAA mode as there is no buffer bind
            ColorMaskOverride = "ColorMask 0 1",

            ExtraDefines = new List<string>()
            {
                "#pragma multi_compile _ WRITE_MSAA_DEPTH"
                // Note we don't need to define WRITE_NORMAL_BUFFER
            },
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
                UnlitMasterNode.AlphaSlotId,
                UnlitMasterNode.AlphaThresholdSlotId
            },
            VertexShaderSlots = new List<int>()
            {
                UnlitMasterNode.PositionSlotId
            },
            UseInPreview = false,

            OnGeneratePassImpl = (IMasterNode node, ref Pass pass) =>
            {
                var masterNode = node as UnlitMasterNode;
                GetStencilStateForDepthOrMV(true, ref pass);
                GetCullMode(masterNode.twoSided.isOn, ref pass);
            }
        };

        Pass m_PassForwardOnly = new Pass()
        {
            Name = "ForwardOnly",
            LightMode = "ForwardOnly",
            TemplateName = "UnlitPass.template",
            MaterialName = "Unlit",
            ShaderPassName = "SHADERPASS_FORWARD_UNLIT",
            ExtraDefines = new List<string>()
            {
                "#pragma multi_compile _ DEBUG_DISPLAY"
            },
            Includes = new List<string>()
            {
                "#include \"Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassForwardUnlit.hlsl\"",
            },
            PixelShaderSlots = new List<int>()
            {
                UnlitMasterNode.ColorSlotId,
                UnlitMasterNode.AlphaSlotId,
                UnlitMasterNode.AlphaThresholdSlotId
            },
            VertexShaderSlots = new List<int>()
            {
                UnlitMasterNode.PositionSlotId
            },
            UseInPreview = true,

            OnGeneratePassImpl = (IMasterNode node, ref Pass pass) =>
            {
                var masterNode = node as UnlitMasterNode;
                GetStencilStateForForwardUnlit(ref pass);
                GetBlendMode(masterNode.surfaceType, masterNode.alphaMode, ref pass);
                GetCullMode(masterNode.twoSided.isOn, ref pass);
                GetZWrite(masterNode.surfaceType, ref pass);
            }
        };

        public int GetPreviewPassIndex() { return 0; }

        // For Unlit we keep the hardcoded stencils as the shader does not have properties to setup the stencil
        public static void GetStencilStateForForwardUnlit(ref Pass pass)
        {
            pass.StencilOverride = new List<string>()
            {
                "// Stencil setup",
                "Stencil",
                "{",
                string.Format("   WriteMask {0}", (int) HDRenderPipeline.StencilBitMask.LightingMask),
                string.Format("   Ref  {0}", (int)StencilLightingUsage.NoLighting),
                "   Comp Always",
                "   Pass Replace",
                "}"
            };
        }

        public static void GetStencilStateForDepthOrMV(bool useObjectMotionVector, ref Pass pass)
        {
            int stencilWriteMask = useObjectMotionVector ? (int)HDRenderPipeline.StencilBitMask.ObjectMotionVectors : 0;
            int stencilRef = useObjectMotionVector ? (int)HDRenderPipeline.StencilBitMask.ObjectMotionVectors : 0;

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

        private static HashSet<string> GetActiveFieldsFromMasterNode(AbstractMaterialNode iMasterNode, Pass pass)
        {
            HashSet<string> activeFields = new HashSet<string>();

            UnlitMasterNode masterNode = iMasterNode as UnlitMasterNode;
            if (masterNode == null)
            {
                return activeFields;
            }

            if (masterNode.IsSlotConnected(UnlitMasterNode.AlphaThresholdSlotId) ||
                masterNode.GetInputSlots<Vector1MaterialSlot>().First(x => x.id == UnlitMasterNode.AlphaThresholdSlotId).value > 0.0f)
            {
                activeFields.Add("AlphaTest");
            }

            // Keywords for transparent
            // #pragma shader_feature _SURFACE_TYPE_TRANSPARENT
            if (masterNode.surfaceType != ShaderGraph.SurfaceType.Opaque)
            {
                // transparent-only defines
                activeFields.Add("SurfaceType.Transparent");

                // #pragma shader_feature _ _BLENDMODE_ALPHA _BLENDMODE_ADD _BLENDMODE_PRE_MULTIPLY
                if (masterNode.alphaMode == AlphaMode.Alpha)
                {
                    activeFields.Add("BlendMode.Alpha");
                }
                else if (masterNode.alphaMode == AlphaMode.Additive)
                {
                    activeFields.Add("BlendMode.Add");
                }
            }
            else
            {
                // opaque-only defines
            }

            return activeFields;
        }

        private static bool GenerateShaderPassUnlit(UnlitMasterNode masterNode, Pass pass, GenerationMode mode, ShaderGenerator result, List<string> sourceAssetDependencyPaths)
        {
            pass.OnGeneratePass(masterNode);

            // apply master node options to active fields
            HashSet<string> activeFields = GetActiveFieldsFromMasterNode(masterNode, pass);

            // use standard shader pass generation
            bool vertexActive = masterNode.IsSlotConnected(UnlitMasterNode.PositionSlotId);
            return HDSubShaderUtilities.GenerateShaderPass(masterNode, pass, mode, activeFields, result, sourceAssetDependencyPaths, vertexActive);
        }

        public string GetSubshader(IMasterNode iMasterNode, GenerationMode mode, List<string> sourceAssetDependencyPaths = null)
        {
            if (sourceAssetDependencyPaths != null)
            {
                // UnlitSubShader.cs
                sourceAssetDependencyPaths.Add(AssetDatabase.GUIDToAssetPath("a32a2cf536cae8e478ca1bbb7b9c493b"));
                // HDSubShaderUtilities.cs
                sourceAssetDependencyPaths.Add(AssetDatabase.GUIDToAssetPath("713ced4e6eef4a44799a4dd59041484b"));
            }

            var masterNode = iMasterNode as UnlitMasterNode;
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

                GenerateShaderPassUnlit(masterNode, m_PassShadowCaster, mode, subShader, sourceAssetDependencyPaths);
                GenerateShaderPassUnlit(masterNode, m_PassMETA, mode, subShader, sourceAssetDependencyPaths);
                GenerateShaderPassUnlit(masterNode, m_SceneSelectionPass, mode, subShader, sourceAssetDependencyPaths);

                if (opaque)
                {
                    GenerateShaderPassUnlit(masterNode, m_PassDepthForwardOnly, mode, subShader, sourceAssetDependencyPaths);
                    GenerateShaderPassUnlit(masterNode, m_PassMotionVectors, mode, subShader, sourceAssetDependencyPaths);
                }

                GenerateShaderPassUnlit(masterNode, m_PassForwardOnly, mode, subShader, sourceAssetDependencyPaths);
            }
            subShader.Deindent();
            subShader.AddShaderChunk("}", true);

            subShader.AddShaderChunk(@"CustomEditor ""UnityEditor.Experimental.Rendering.HDPipeline.UnlitUI""");

            return subShader.GetShaderString(0);
        }

        public bool IsPipelineCompatible(RenderPipelineAsset renderPipelineAsset)
        {
            return renderPipelineAsset is HDRenderPipelineAsset;
        }
    }
}