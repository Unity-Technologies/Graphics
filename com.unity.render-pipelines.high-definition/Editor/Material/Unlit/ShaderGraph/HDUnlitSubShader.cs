using System.Collections.Generic;
using Data.Util;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Rendering;

namespace UnityEditor.Rendering.HighDefinition
{
    [FormerName("UnityEditor.Experimental.Rendering.HDPipeline.HDUnlitSubShader")]
    class HDUnlitSubShader : IHDUnlitSubShader
    {
        Pass m_PassMETA = new Pass()
        {
            Name = "META",
            LightMode = "META",
            TemplateName = "HDUnlitPass.template",
            MaterialName = "Unlit",
            ShaderPassName = "SHADERPASS_LIGHT_TRANSPORT",
            CullOverride = "Cull Off",
            ShaderStages = HDSubShaderUtilities.s_ShaderStagesRasterization,
            ExtraDefines = new List<string>()
            {
                "#pragma only_renderers d3d11 ps4 xboxone vulkan metal switch"
            },
            Includes = new List<string>()
            {
                "#include \"Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassLightTransport.hlsl\"",
            },
            RequiredFields = new List<string>()
            {
                "AttributesMesh.normalOS",
                "AttributesMesh.tangentOS",     // Always present as we require it also in case of Variants lighting
                "AttributesMesh.uv0",
                "AttributesMesh.uv1",
                "AttributesMesh.color",
                "AttributesMesh.uv2",           // SHADERPASS_LIGHT_TRANSPORT always uses uv2
            },
            PixelShaderSlots = new List<int>()
            {
                HDUnlitMasterNode.ColorSlotId,
                HDUnlitMasterNode.AlphaSlotId,
                HDUnlitMasterNode.AlphaThresholdSlotId,
                HDUnlitMasterNode.EmissionSlotId
            },
            VertexShaderSlots = new List<int>()
            {
                //HDUnlitMasterNode.PositionSlotId
            },
            UseInPreview = false
        };

        Pass m_SceneSelectionPass = new Pass()
        {
            Name = "SceneSelectionPass",
            LightMode = "SceneSelectionPass",
            TemplateName = "HDUnlitPass.template",
            MaterialName = "Unlit",
            ShaderPassName = "SHADERPASS_DEPTH_ONLY",
            ColorMaskOverride = "ColorMask 0",
            CullOverride = HDSubShaderUtilities.defaultCullMode,
            ZWriteOverride = HDSubShaderUtilities.zWriteOn,
            ShaderStages = HDSubShaderUtilities.s_ShaderStagesRasterization,
            ExtraDefines = new List<string>()
            {
                "#pragma only_renderers d3d11 ps4 xboxone vulkan metal switch",
                "#define SCENESELECTIONPASS",
                "#pragma editor_sync_compilation",
            },
            Includes = new List<string>()
            {
                "#include \"Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassDepthOnly.hlsl\"",
            },
            PixelShaderSlots = new List<int>()
            {
                HDUnlitMasterNode.AlphaSlotId,
                HDUnlitMasterNode.AlphaThresholdSlotId
            },
            VertexShaderSlots = new List<int>()
            {
                HDUnlitMasterNode.PositionSlotId,
                HDUnlitMasterNode.VertexNormalSlotId,
                HDUnlitMasterNode.VertexTangentSlotId
            },
            UseInPreview = false,
        };

        Pass m_PassDepthForwardOnly = new Pass()
        {
            Name = "DepthForwardOnly",
            LightMode = "DepthForwardOnly",
            TemplateName = "HDUnlitPass.template",
            MaterialName = "Unlit",
            ShaderPassName = "SHADERPASS_DEPTH_ONLY",
            ZWriteOverride = HDSubShaderUtilities.zWriteOn,
            // Caution: When using MSAA we have normal and depth buffer bind.
            // Mean unlit object need to not write in it (or write 0) - Disable color mask for this RT
            // This is not a problem in no MSAA mode as there is no buffer bind
            ColorMaskOverride = "ColorMask 0 0",
            CullOverride = HDSubShaderUtilities.defaultCullMode,

            ShaderStages = HDSubShaderUtilities.s_ShaderStagesRasterization,
            ExtraDefines = new List<string>()
            {
                "#pragma only_renderers d3d11 ps4 xboxone vulkan metal switch",
                "#pragma multi_compile _ WRITE_MSAA_DEPTH"
                // Note we don't need to define WRITE_NORMAL_BUFFER
            },

            Includes = new List<string>()
            {
                "#include \"Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassDepthOnly.hlsl\"",
            },
            PixelShaderSlots = new List<int>()
            {
                HDUnlitMasterNode.AlphaSlotId,
                HDUnlitMasterNode.AlphaThresholdSlotId
            },

            VertexShaderSlots = new List<int>()
            {
                HDUnlitMasterNode.PositionSlotId,
                HDUnlitMasterNode.VertexNormalSlotId,
                HDUnlitMasterNode.VertexTangentSlotId
            },
            UseInPreview = false,

            OnGeneratePassImpl = (IMasterNode node, ref Pass pass) =>
            {
                HDSubShaderUtilities.SetStencilStateForDepth(ref pass);
            }
        };

        Pass m_PassMotionVectors = new Pass()
        {
            Name = "MotionVectors",
            LightMode = "MotionVectors",
            TemplateName = "HDUnlitPass.template",
            MaterialName = "Unlit",
            ShaderPassName = "SHADERPASS_MOTION_VECTORS",
            CullOverride = HDSubShaderUtilities.defaultCullMode,
            ZWriteOverride = HDSubShaderUtilities.zWriteOn,
            // Caution: When using MSAA we have motion vector, normal and depth buffer bind.
            // Mean unlit object need to not write in it (or write 0) - Disable color mask for this RT
            // This is not a problem in no MSAA mode as there is no buffer bind
            ColorMaskOverride = "ColorMask 0 1",

            ShaderStages = HDSubShaderUtilities.s_ShaderStagesRasterization,
            ExtraDefines = new List<string>()
            {
                "#pragma only_renderers d3d11 ps4 xboxone vulkan metal switch",
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
                HDUnlitMasterNode.AlphaSlotId,
                HDUnlitMasterNode.AlphaThresholdSlotId
            },
            VertexShaderSlots = new List<int>()
            {
                HDUnlitMasterNode.PositionSlotId,
                HDUnlitMasterNode.VertexNormalSlotId,
                HDUnlitMasterNode.VertexTangentSlotId
            },
            UseInPreview = false,

            OnGeneratePassImpl = (IMasterNode node, ref Pass pass) =>
            {
                HDSubShaderUtilities.SetStencilStateForMotionVector(ref pass);
            }
        };

        Pass m_PassDistortion = new Pass()
        {
            Name = "DistortionVectors",
            LightMode = "DistortionVectors",
            TemplateName = "HDUnlitPass.template",
            MaterialName = "Unlit",
            ShaderPassName = "SHADERPASS_DISTORTION",
            CullOverride = HDSubShaderUtilities.defaultCullMode,
            ZWriteOverride = HDSubShaderUtilities.zWriteOff,
            ShaderStages = HDSubShaderUtilities.s_ShaderStagesRasterization,
            ExtraDefines = new List<string>()
            {
                "#pragma only_renderers d3d11 ps4 xboxone vulkan metal switch"
            },
            Includes = new List<string>()
            {
                "#include \"Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassDistortion.hlsl\"",
            },
            PixelShaderSlots = new List<int>()
            {
                HDUnlitMasterNode.AlphaSlotId,
                HDUnlitMasterNode.AlphaThresholdSlotId,
                HDUnlitMasterNode.DistortionSlotId,
                HDUnlitMasterNode.DistortionBlurSlotId,
            },
            VertexShaderSlots = new List<int>()
            {
                HDUnlitMasterNode.PositionSlotId,
                HDUnlitMasterNode.VertexNormalSlotId,
                HDUnlitMasterNode.VertexTangentSlotId
            },
            UseInPreview = true,

            OnGeneratePassImpl = (IMasterNode node, ref Pass pass) =>
            {
                HDSubShaderUtilities.SetStencilStateForDistortionVector(ref pass);
                var masterNode = node as HDUnlitMasterNode;
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
        };

        Pass m_PassShadowCaster = new Pass()
        {
            Name = "ShadowCaster",
            LightMode = "ShadowCaster",
            TemplateName = "HDUnlitPass.template",
            MaterialName = "Unlit",
            ShaderPassName = "SHADERPASS_SHADOWS",
            ColorMaskOverride = "ColorMask 0",
            ZClipOverride = HDSubShaderUtilities.zClipShadowCaster,
            CullOverride = HDSubShaderUtilities.defaultCullMode,
            ZWriteOverride = HDSubShaderUtilities.zWriteOn,
            ShaderStages = HDSubShaderUtilities.s_ShaderStagesRasterization,
            ExtraDefines = new List<string>()
            {
                "#pragma only_renderers d3d11 ps4 xboxone vulkan metal switch"
            },
            Includes = new List<string>()
            {
                "#include \"Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassDepthOnly.hlsl\"",
            },
            PixelShaderSlots = new List<int>()
            {
                HDUnlitMasterNode.AlphaSlotId,
                HDUnlitMasterNode.AlphaThresholdSlotId,
            },
            VertexShaderSlots = new List<int>()
            {
                HDUnlitMasterNode.PositionSlotId,
                HDUnlitMasterNode.VertexNormalSlotId,
                HDUnlitMasterNode.VertexTangentSlotId
            },
            UseInPreview = false,
        };

        Pass m_PassForwardOnly = new Pass()
        {
            Name = "ForwardOnly",
            LightMode = "ForwardOnly",
            TemplateName = "HDUnlitPass.template",
            MaterialName = "Unlit",
            ShaderPassName = "SHADERPASS_FORWARD_UNLIT",
            CullOverride = HDSubShaderUtilities.defaultCullMode,
            ZTestOverride = HDSubShaderUtilities.zTestTransparent,
            ZWriteOverride = HDSubShaderUtilities.ZWriteDefault,
            ShaderStages = HDSubShaderUtilities.s_ShaderStagesRasterization,
            ExtraDefines = new List<string>()
            {
                "#pragma only_renderers d3d11 ps4 xboxone vulkan metal switch",
                "#pragma multi_compile _ DEBUG_DISPLAY"
            },
            Includes = new List<string>()
            {
                "#include \"Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassForwardUnlit.hlsl\"",
            },
            PixelShaderSlots = new List<int>()
            {
                HDUnlitMasterNode.ColorSlotId,
                HDUnlitMasterNode.AlphaSlotId,
                HDUnlitMasterNode.AlphaThresholdSlotId,
                HDUnlitMasterNode.EmissionSlotId,
                HDUnlitMasterNode.ShadowTintSlotId
            },
            VertexShaderSlots = new List<int>()
            {
                HDUnlitMasterNode.PositionSlotId,
                HDUnlitMasterNode.VertexNormalSlotId,
                HDUnlitMasterNode.VertexTangentSlotId
            },
            UseInPreview = true,

            OnGeneratePassImpl = (IMasterNode node, ref Pass pass) =>
            {
                HDSubShaderUtilities.SetStencilStateForForward(ref pass);
                HDSubShaderUtilities.SetBlendModeForForward(ref pass);
            }
        };

        Pass m_PassRaytracingIndirect = new Pass()
        {
            Name = "IndirectDXR",
            LightMode = "IndirectDXR",
            TemplateName = "HDUnlitPass.template",
            MaterialName = "Unlit",
            ShaderPassName = "SHADERPASS_RAYTRACING_INDIRECT",
            ShaderStages = HDSubShaderUtilities.s_ShaderStagesRayTracing,
            ExtraDefines = new List<string>()
            {
                "#pragma only_renderers d3d11",
            },
            Includes = new List<string>()
            {
                "#include \"Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassRaytracingIndirect.hlsl\"",
            },
            PixelShaderSlots = new List<int>()
            {
                HDUnlitMasterNode.ColorSlotId,
                HDUnlitMasterNode.AlphaSlotId,
                HDUnlitMasterNode.AlphaThresholdSlotId,
                HDUnlitMasterNode.EmissionSlotId
            },
            VertexShaderSlots = new List<int>()
            {
                HDLitMasterNode.PositionSlotId,
                HDUnlitMasterNode.VertexNormalSlotId,
                HDUnlitMasterNode.VertexTangentSlotId
            },
            UseInPreview = false
        };

        Pass m_PassRaytracingVisibility = new Pass()
        {
            Name = "VisibilityDXR",
            LightMode = "VisibilityDXR",
            TemplateName = "HDUnlitPass.template",
            MaterialName = "Unlit",
            ShaderPassName = "SHADERPASS_RAYTRACING_VISIBILITY",
            ShaderStages = HDSubShaderUtilities.s_ShaderStagesRayTracing,
            ExtraDefines = new List<string>()
            {
                "#pragma only_renderers d3d11",
                "#pragma multi_compile _ TRANSPARENT_COLOR_SHADOW",
            },
            Includes = new List<string>()
            {
                "#include \"Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassRaytracingVisibility.hlsl\"",
            },
            PixelShaderSlots = new List<int>()
            {
                HDUnlitMasterNode.ColorSlotId,
                HDUnlitMasterNode.AlphaSlotId,
                HDUnlitMasterNode.AlphaThresholdSlotId,
                HDUnlitMasterNode.EmissionSlotId
            },
            VertexShaderSlots = new List<int>()
            {
                HDLitMasterNode.PositionSlotId,
                HDUnlitMasterNode.VertexNormalSlotId,
                HDUnlitMasterNode.VertexTangentSlotId
            },
            UseInPreview = false
        };

        Pass m_PassRaytracingForward = new Pass()
        {
            Name = "ForwardDXR",
            LightMode = "ForwardDXR",
            TemplateName = "HDUnlitPass.template",
            MaterialName = "Unlit",
            ShaderPassName = "SHADERPASS_RAYTRACING_FORWARD",
            ShaderStages = HDSubShaderUtilities.s_ShaderStagesRayTracing,
            ExtraDefines = new List<string>()
            {
                "#pragma only_renderers d3d11",
            },
            Includes = new List<string>()
            {
                "#include \"Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassRaytracingForward.hlsl\"",
            },
            PixelShaderSlots = new List<int>()
            {
                HDUnlitMasterNode.ColorSlotId,
                HDUnlitMasterNode.AlphaSlotId,
                HDUnlitMasterNode.AlphaThresholdSlotId,
                HDUnlitMasterNode.EmissionSlotId
            },
            VertexShaderSlots = new List<int>()
            {
                HDLitMasterNode.PositionSlotId,
                HDUnlitMasterNode.VertexNormalSlotId,
                HDUnlitMasterNode.VertexTangentSlotId
            },
            UseInPreview = false
        };

        Pass m_PassRaytracingGBuffer = new Pass()
        {
            Name = "GBufferDXR",
            LightMode = "GBufferDXR",
            TemplateName = "HDUnlitPass.template",
            MaterialName = "Unlit",
            ShaderPassName = "SHADERPASS_RAYTRACING_GBUFFER",
            ShaderStages = HDSubShaderUtilities.s_ShaderStagesRayTracing,
            ExtraDefines = new List<string>()
            {
                "#pragma only_renderers d3d11",
            },
            Includes = new List<string>()
            {
                "#include \"Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderpassRaytracingGBuffer.hlsl\"",
            },
            PixelShaderSlots = new List<int>()
            {
                HDUnlitMasterNode.ColorSlotId,
                HDUnlitMasterNode.AlphaSlotId,
                HDUnlitMasterNode.AlphaThresholdSlotId,
                HDUnlitMasterNode.EmissionSlotId
            },
            VertexShaderSlots = new List<int>()
            {
                HDLitMasterNode.PositionSlotId,
                HDUnlitMasterNode.VertexNormalSlotId,
                HDUnlitMasterNode.VertexTangentSlotId
            },
            UseInPreview = false
        };

        Pass m_PassPathTracing = new Pass()
        {
            Name = "PathTracingDXR",
            LightMode = "PathTracingDXR",
            TemplateName = "HDUnlitPass.template",
            MaterialName = "Unlit",
            ShaderPassName = "SHADERPASS_PATH_TRACING",
            ShaderStages = HDSubShaderUtilities.s_ShaderStagesRayTracing,
            ExtraDefines = new List<string>()
            {
                "#pragma only_renderers d3d11",
            },
            Includes = new List<string>()
            {
                "#include \"Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassPathTracing.hlsl\"",
            },
            PixelShaderSlots = new List<int>()
            {
                HDUnlitMasterNode.ColorSlotId,
                HDUnlitMasterNode.AlphaSlotId,
                HDUnlitMasterNode.AlphaThresholdSlotId,
                HDUnlitMasterNode.EmissionSlotId
            },
            VertexShaderSlots = new List<int>()
            {
                HDLitMasterNode.PositionSlotId,
                HDUnlitMasterNode.VertexNormalSlotId,
                HDUnlitMasterNode.VertexTangentSlotId
            },
            UseInPreview = false
        };

        public int GetPreviewPassIndex() { return 0; }

        private static ActiveFields GetActiveFieldsFromMasterNode(AbstractMaterialNode iMasterNode, Pass pass)
        {
            var activeFields = new ActiveFields();
            var baseActiveFields = activeFields.baseInstance;

            HDUnlitMasterNode masterNode = iMasterNode as HDUnlitMasterNode;
            if (masterNode == null)
            {
                return activeFields;
            }

            if (masterNode.IsSlotConnected(HDUnlitMasterNode.VertexNormalSlotId))
            {
                baseActiveFields.Add("AttributesMesh.normalOS");
            }

            if (masterNode.alphaTest.isOn && pass.PixelShaderUsesSlot(HDUnlitMasterNode.AlphaThresholdSlotId))
            {
                baseActiveFields.Add("AlphaTest");
            }

            if (masterNode.surfaceType != SurfaceType.Opaque)
            {
                if (masterNode.transparencyFog.isOn)
                {
                    baseActiveFields.Add("AlphaFog");
                }
            }

            if (masterNode.addPrecomputedVelocity.isOn)
            {
                baseActiveFields.Add("AddPrecomputedVelocity");
            }

            if (masterNode.enableShadowMatte.isOn)
            {
                baseActiveFields.Add("EnableShadowMatte");
            }

            return activeFields;
        }

        private static bool GenerateShaderPassUnlit(HDUnlitMasterNode masterNode, Pass pass, GenerationMode mode, ShaderGenerator result, List<string> sourceAssetDependencyPaths, bool instancingFlag = true)
        {
            if (mode == GenerationMode.ForReals || pass.UseInPreview)
            {
                pass.OnGeneratePass(masterNode);

                // apply master node options to active fields
                var activeFields = GetActiveFieldsFromMasterNode(masterNode, pass);

                // use standard shader pass generation
                bool vertexActive = false;
                if (masterNode.IsSlotConnected(HDUnlitMasterNode.PositionSlotId) ||
                    masterNode.IsSlotConnected(HDUnlitMasterNode.VertexNormalSlotId) ||
                    masterNode.IsSlotConnected(HDUnlitMasterNode.VertexTangentSlotId) )
                {
                    vertexActive = true;
                }
                return HDSubShaderUtilities.GenerateShaderPass(masterNode, pass, mode, activeFields, result, sourceAssetDependencyPaths, vertexActive, isLit:false, instancingFlag: instancingFlag);
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
                // HDUnlitSubShader.cs
                sourceAssetDependencyPaths.Add(AssetDatabase.GUIDToAssetPath("1c44ec077faa54145a89357de68e5d26"));
                // HDSubShaderUtilities.cs
                sourceAssetDependencyPaths.Add(AssetDatabase.GUIDToAssetPath("713ced4e6eef4a44799a4dd59041484b"));
            }

            var masterNode = iMasterNode as HDUnlitMasterNode;

            var subShader = new ShaderGenerator();
            subShader.AddShaderChunk("SubShader", true);
            subShader.AddShaderChunk("{", true);
            subShader.Indent();
            {
                // Add tags at the SubShader level
                int queue = HDRenderQueue.ChangeType(masterNode.renderingPass, masterNode.sortPriority, masterNode.alphaTest.isOn);
                HDSubShaderUtilities.AddTags(subShader, HDRenderPipeline.k_ShaderTagName, HDRenderTypeTags.HDUnlitShader, queue);

                // For preview only we generate the passes that are enabled
                bool opaque = (masterNode.surfaceType == SurfaceType.Opaque);
                bool transparent = !opaque;
                bool distortionActive = transparent && masterNode.distortion.isOn;

                GenerateShaderPassUnlit(masterNode, m_PassShadowCaster, mode, subShader, sourceAssetDependencyPaths);
                GenerateShaderPassUnlit(masterNode, m_PassMETA, mode, subShader, sourceAssetDependencyPaths);
                GenerateShaderPassUnlit(masterNode, m_SceneSelectionPass, mode, subShader, sourceAssetDependencyPaths);

                GenerateShaderPassUnlit(masterNode, m_PassDepthForwardOnly, mode, subShader, sourceAssetDependencyPaths);
                GenerateShaderPassUnlit(masterNode, m_PassMotionVectors, mode, subShader, sourceAssetDependencyPaths);

                if (distortionActive)
                {
                    GenerateShaderPassUnlit(masterNode, m_PassDistortion, mode, subShader, sourceAssetDependencyPaths);
                }

                GenerateShaderPassUnlit(masterNode, m_PassForwardOnly, mode, subShader, sourceAssetDependencyPaths);
            }
            subShader.Deindent();
            subShader.AddShaderChunk("}", false);

            if (mode == GenerationMode.ForReals)
            {
                subShader.AddShaderChunk("SubShader", false);
                subShader.AddShaderChunk("{", false);
                subShader.Indent();
                HDSubShaderUtilities.AddTags(subShader, HDRenderPipeline.k_ShaderTagName);
                {
                    GenerateShaderPassUnlit(masterNode, m_PassRaytracingIndirect, mode, subShader, sourceAssetDependencyPaths, instancingFlag: false);
                    GenerateShaderPassUnlit(masterNode, m_PassRaytracingVisibility, mode, subShader, sourceAssetDependencyPaths, instancingFlag: false);
                    GenerateShaderPassUnlit(masterNode, m_PassRaytracingForward, mode, subShader, sourceAssetDependencyPaths, instancingFlag: false);
                    GenerateShaderPassUnlit(masterNode, m_PassRaytracingGBuffer, mode, subShader, sourceAssetDependencyPaths, instancingFlag: false);
                    GenerateShaderPassUnlit(masterNode, m_PassPathTracing, mode, subShader, sourceAssetDependencyPaths, instancingFlag: false);
                }
                subShader.Deindent();
                subShader.AddShaderChunk("}", false);
            }

            if (!masterNode.OverrideEnabled)
            {
                subShader.AddShaderChunk(@"CustomEditor ""UnityEditor.Rendering.HighDefinition.HDUnlitGUI""");
            }

            return subShader.GetShaderString(0);
        }

        public bool IsPipelineCompatible(RenderPipelineAsset renderPipelineAsset)
        {
            return renderPipelineAsset is HDRenderPipelineAsset;
        }
    }
}
