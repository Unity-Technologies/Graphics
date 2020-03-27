using System.Collections.Generic;
using UnityEditor.ShaderGraph;
using UnityEditor.ShaderGraph.Internal;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Rendering;
using Data.Util;

namespace UnityEditor.Rendering.HighDefinition
{
    [FormerName("UnityEditor.Experimental.Rendering.HDPipeline.HDLitSubShader")]
    class HDLitSubShader : IHDLitSubShader
    {
        internal static string DefineRaytracingKeyword(RayTracingNode.RaytracingVariant variant)
            => $"#define {RayTracingNode.RaytracingVariantKeyword(variant)}";

        Pass m_PassGBuffer = new Pass()
        {
            Name = "GBuffer",
            LightMode = "GBuffer",
            TemplateName = "HDLitPass.template",
            MaterialName = "Lit",
            ShaderPassName = "SHADERPASS_GBUFFER",
            ZTestOverride = HDSubShaderUtilities.zTestGBuffer,
            CullOverride = HDSubShaderUtilities.defaultCullMode,
            ShaderStages = HDSubShaderUtilities.s_ShaderStagesRasterization,
            ExtraDefines = new List<string>()
            {
                "#pragma only_renderers d3d11 ps4 xboxone vulkan metal switch",
                "#pragma multi_compile _ DEBUG_DISPLAY",
                "#pragma multi_compile _ LIGHTMAP_ON",
                "#pragma multi_compile _ DIRLIGHTMAP_COMBINED",
                "#pragma multi_compile _ DYNAMICLIGHTMAP_ON",
                "#pragma multi_compile _ SHADOWS_SHADOWMASK",
                "#pragma multi_compile DECALS_OFF DECALS_3RT DECALS_4RT",
                "#pragma multi_compile _ LIGHT_LAYERS",
                DefineRaytracingKeyword(RayTracingNode.RaytracingVariant.High),
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
                HDLitMasterNode.AlbedoSlotId,
                HDLitMasterNode.NormalSlotId,
                HDLitMasterNode.BentNormalSlotId,
                HDLitMasterNode.TangentSlotId,
                HDLitMasterNode.SubsurfaceMaskSlotId,
                HDLitMasterNode.ThicknessSlotId,
                HDLitMasterNode.DiffusionProfileHashSlotId,
                HDLitMasterNode.IridescenceMaskSlotId,
                HDLitMasterNode.IridescenceThicknessSlotId,
                HDLitMasterNode.SpecularColorSlotId,
                HDLitMasterNode.CoatMaskSlotId,
                HDLitMasterNode.MetallicSlotId,
                HDLitMasterNode.EmissionSlotId,
                HDLitMasterNode.SmoothnessSlotId,
                HDLitMasterNode.AmbientOcclusionSlotId,
                HDLitMasterNode.SpecularOcclusionSlotId,
                HDLitMasterNode.AlphaSlotId,
                HDLitMasterNode.AlphaThresholdSlotId,
                HDLitMasterNode.AnisotropySlotId,
                HDLitMasterNode.SpecularAAScreenSpaceVarianceSlotId,
                HDLitMasterNode.SpecularAAThresholdSlotId,
                HDLitMasterNode.RefractionIndexSlotId,
                HDLitMasterNode.RefractionColorSlotId,
                HDLitMasterNode.RefractionDistanceSlotId,
                HDLitMasterNode.LightingSlotId,
                HDLitMasterNode.BackLightingSlotId,
                HDLitMasterNode.DepthOffsetSlotId,
            },
            VertexShaderSlots = new List<int>()
            {
                HDLitMasterNode.PositionSlotId,
                HDLitMasterNode.VertexNormalSlotID,
                HDLitMasterNode.VertexTangentSlotID
            },
            UseInPreview = true,

            OnGeneratePassImpl = (IMasterNode node, ref Pass pass) =>
            {
                var masterNode = node as HDLitMasterNode;
                HDSubShaderUtilities.SetStencilStateForGBuffer(ref pass);

                // When we have alpha test, we will force a depth prepass so we always bypass the clip instruction in the GBuffer
                // Don't do it with debug display mode as it is possible there is no depth prepass in this case
                // This remove is required otherwise the code generate several time the define...
                pass.ExtraDefines.Remove("#ifndef DEBUG_DISPLAY\n#define SHADERPASS_GBUFFER_BYPASS_ALPHA_TEST\n#endif");

                if (masterNode.alphaTest.isOn)
                    pass.ExtraDefines.Add("#ifndef DEBUG_DISPLAY\n#define SHADERPASS_GBUFFER_BYPASS_ALPHA_TEST\n#endif");
            }
        };

        Pass m_PassMETA = new Pass()
        {
            Name = "META",
            LightMode = "META",
            TemplateName = "HDLitPass.template",
            MaterialName = "Lit",
            ShaderPassName = "SHADERPASS_LIGHT_TRANSPORT",
            CullOverride = "Cull Off",
            Includes = new List<string>()
            {
                "#include \"Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassLightTransport.hlsl\"",
            },
            ShaderStages = HDSubShaderUtilities.s_ShaderStagesRasterization,
            ExtraDefines = new List<string>()
            {
                "#pragma only_renderers d3d11 ps4 xboxone vulkan metal switch",
                DefineRaytracingKeyword(RayTracingNode.RaytracingVariant.High),
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
                HDLitMasterNode.AlbedoSlotId,
                HDLitMasterNode.NormalSlotId,
                HDLitMasterNode.BentNormalSlotId,
                HDLitMasterNode.TangentSlotId,
                HDLitMasterNode.SubsurfaceMaskSlotId,
                HDLitMasterNode.ThicknessSlotId,
                HDLitMasterNode.DiffusionProfileHashSlotId,
                HDLitMasterNode.IridescenceMaskSlotId,
                HDLitMasterNode.IridescenceThicknessSlotId,
                HDLitMasterNode.SpecularColorSlotId,
                HDLitMasterNode.CoatMaskSlotId,
                HDLitMasterNode.MetallicSlotId,
                HDLitMasterNode.EmissionSlotId,
                HDLitMasterNode.SmoothnessSlotId,
                HDLitMasterNode.AmbientOcclusionSlotId,
                HDLitMasterNode.SpecularOcclusionSlotId,
                HDLitMasterNode.AlphaSlotId,
                HDLitMasterNode.AlphaThresholdSlotId,
                HDLitMasterNode.AnisotropySlotId,
                HDLitMasterNode.SpecularAAScreenSpaceVarianceSlotId,
                HDLitMasterNode.SpecularAAThresholdSlotId,
                HDLitMasterNode.RefractionIndexSlotId,
                HDLitMasterNode.RefractionColorSlotId,
                HDLitMasterNode.RefractionDistanceSlotId,
            },
            VertexShaderSlots = new List<int>()
            {
                //HDLitMasterNode.PositionSlotId
            },
            UseInPreview = false
        };

        Pass m_PassShadowCaster = new Pass()
        {
            Name = "ShadowCaster",
            LightMode = "ShadowCaster",
            TemplateName = "HDLitPass.template",
            MaterialName = "Lit",
            ShaderPassName = "SHADERPASS_SHADOWS",
            ColorMaskOverride = "ColorMask 0",
            ZClipOverride = HDSubShaderUtilities.zClipShadowCaster,
            CullOverride = HDSubShaderUtilities.defaultCullMode,
            ZWriteOverride = HDSubShaderUtilities.zWriteOn,
            ShaderStages = HDSubShaderUtilities.s_ShaderStagesRasterization,
            ExtraDefines = new List<string>()
            {
                "#pragma only_renderers d3d11 ps4 xboxone vulkan metal switch",
                DefineRaytracingKeyword(RayTracingNode.RaytracingVariant.High),
            },
            Includes = new List<string>()
            {
                "#include \"Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassDepthOnly.hlsl\"",
            },
            PixelShaderSlots = new List<int>()
            {
                HDLitMasterNode.AlphaSlotId,
                HDLitMasterNode.AlphaThresholdSlotId,
                HDLitMasterNode.AlphaThresholdShadowSlotId,
                HDLitMasterNode.DepthOffsetSlotId,
            },
            VertexShaderSlots = new List<int>()
            {
                HDLitMasterNode.PositionSlotId,
                HDLitMasterNode.VertexNormalSlotID,
                HDLitMasterNode.VertexTangentSlotID
            },
            UseInPreview = false,
        };

        Pass m_SceneSelectionPass = new Pass()
        {
            Name = "SceneSelectionPass",
            LightMode = "SceneSelectionPass",
            TemplateName = "HDLitPass.template",
            MaterialName = "Lit",
            ShaderPassName = "SHADERPASS_DEPTH_ONLY",
            ColorMaskOverride = "ColorMask 0",
            ShaderStages = HDSubShaderUtilities.s_ShaderStagesRasterization,
            ExtraDefines = new List<string>()
            {
                "#pragma only_renderers d3d11 ps4 xboxone vulkan metal switch",
                "#define SCENESELECTIONPASS",
                "#pragma editor_sync_compilation",
                DefineRaytracingKeyword(RayTracingNode.RaytracingVariant.High),
            },
            Includes = new List<string>()
            {
                "#include \"Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassDepthOnly.hlsl\"",
            },
            PixelShaderSlots = new List<int>()
            {
                HDLitMasterNode.AlphaSlotId,
                HDLitMasterNode.AlphaThresholdSlotId,
                HDLitMasterNode.DepthOffsetSlotId,
            },
            VertexShaderSlots = new List<int>()
            {
                HDLitMasterNode.PositionSlotId,
                HDLitMasterNode.VertexNormalSlotID,
                HDLitMasterNode.VertexTangentSlotID
            },
            UseInPreview = false
        };

        Pass m_PassDepthOnly = new Pass()
        {
            Name = "DepthOnly",
            LightMode = "DepthOnly",
            TemplateName = "HDLitPass.template",
            MaterialName = "Lit",
            ShaderPassName = "SHADERPASS_DEPTH_ONLY",
            CullOverride = HDSubShaderUtilities.defaultCullMode,
            ShaderStages = HDSubShaderUtilities.s_ShaderStagesRasterization,
            ExtraDefines = HDSubShaderUtilities.s_ExtraDefinesDepthOrMotion,
            ZWriteOverride = HDSubShaderUtilities.zWriteOn,

            Includes = new List<string>()
            {
                "#include \"Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassDepthOnly.hlsl\"",
            },
            PixelShaderSlots = new List<int>()
            {
                HDLitMasterNode.NormalSlotId,
                HDLitMasterNode.SmoothnessSlotId,
                HDLitMasterNode.AlphaSlotId,
                HDLitMasterNode.AlphaThresholdSlotId,
                HDLitMasterNode.DepthOffsetSlotId,
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
                HDLitMasterNode.PositionSlotId,
                HDLitMasterNode.VertexNormalSlotID,
                HDLitMasterNode.VertexTangentSlotID
            },
            UseInPreview = true,

            OnGeneratePassImpl = (IMasterNode node, ref Pass pass) =>
            {
                var masterNode = node as HDLitMasterNode;
                HDSubShaderUtilities.SetStencilStateForDepth(ref pass);
            }
        };

        Pass m_PassMotionVectors = new Pass()
        {
            Name = "MotionVectors",
            LightMode = "MotionVectors",
            TemplateName = "HDLitPass.template",
            MaterialName = "Lit",
            ShaderPassName = "SHADERPASS_MOTION_VECTORS",
            ShaderStages = HDSubShaderUtilities.s_ShaderStagesRasterization,
            ExtraDefines = HDSubShaderUtilities.s_ExtraDefinesDepthOrMotion,
            CullOverride = HDSubShaderUtilities.defaultCullMode,
            ZWriteOverride = HDSubShaderUtilities.zWriteOn,
            Includes = new List<string>()
            {
                "#include \"Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassMotionVectors.hlsl\"",
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
            PixelShaderSlots = new List<int>()
            {
                HDLitMasterNode.NormalSlotId,
                HDLitMasterNode.SmoothnessSlotId,
                HDLitMasterNode.AlphaSlotId,
                HDLitMasterNode.AlphaThresholdSlotId,
                HDLitMasterNode.DepthOffsetSlotId,
            },
            VertexShaderSlots = new List<int>()
            {
                HDLitMasterNode.PositionSlotId,
                HDLitMasterNode.VertexNormalSlotID,
                HDLitMasterNode.VertexTangentSlotID
            },
            UseInPreview = false,

            OnGeneratePassImpl = (IMasterNode node, ref Pass pass) =>
            {
                var masterNode = node as HDLitMasterNode;
                HDSubShaderUtilities.SetStencilStateForMotionVector(ref pass);
            }
        };

        Pass m_PassDistortion = new Pass()
        {
            Name = "DistortionVectors",
            LightMode = "DistortionVectors",
            TemplateName = "HDLitPass.template",
            MaterialName = "Lit",
            ShaderPassName = "SHADERPASS_DISTORTION",
            ZWriteOverride = HDSubShaderUtilities.zWriteOff,
            ShaderStages = HDSubShaderUtilities.s_ShaderStagesRasterization,
            ExtraDefines = new List<string>()
            {
                "#pragma only_renderers d3d11 ps4 xboxone vulkan metal switch",
                DefineRaytracingKeyword(RayTracingNode.RaytracingVariant.High),
            },
            Includes = new List<string>()
            {
                "#include \"Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassDistortion.hlsl\"",
            },
            PixelShaderSlots = new List<int>()
            {
                HDLitMasterNode.AlphaSlotId,
                HDLitMasterNode.AlphaThresholdSlotId,
                HDLitMasterNode.DistortionSlotId,
                HDLitMasterNode.DistortionBlurSlotId,
            },
            VertexShaderSlots = new List<int>()
            {
                HDLitMasterNode.PositionSlotId,
                HDLitMasterNode.VertexNormalSlotID,
                HDLitMasterNode.VertexTangentSlotID
            },
            UseInPreview = true,

            OnGeneratePassImpl = (IMasterNode node, ref Pass pass) =>
            {
                HDSubShaderUtilities.SetStencilStateForDistortionVector(ref pass);
                var masterNode = node as HDLitMasterNode;
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


        Pass m_PassTransparentDepthPrepass = new Pass()
        {
            Name = "TransparentDepthPrepass",
            LightMode = "TransparentDepthPrepass",
            TemplateName = "HDLitPass.template",
            MaterialName = "Lit",
            ShaderPassName = "SHADERPASS_DEPTH_ONLY",
            BlendOverride = "Blend One Zero",
            ZWriteOverride = "ZWrite On",
            ColorMaskOverride = "ColorMask 0",
            CullOverride = HDSubShaderUtilities.defaultCullMode,
            ShaderStages = HDSubShaderUtilities.s_ShaderStagesRasterization,
            ExtraDefines = new List<string>()
            {
                "#pragma only_renderers d3d11 ps4 xboxone vulkan metal switch",
                "#define CUTOFF_TRANSPARENT_DEPTH_PREPASS",
                DefineRaytracingKeyword(RayTracingNode.RaytracingVariant.High),
            },
            Includes = new List<string>()
            {
                "#include \"Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassDepthOnly.hlsl\"",
            },
            PixelShaderSlots = new List<int>()
            {
                HDLitMasterNode.AlphaSlotId,
                HDLitMasterNode.AlphaThresholdDepthPrepassSlotId,
                HDLitMasterNode.DepthOffsetSlotId,
            },
            VertexShaderSlots = new List<int>()
            {
                HDLitMasterNode.PositionSlotId,
                HDLitMasterNode.VertexNormalSlotID,
                HDLitMasterNode.VertexTangentSlotID
            },
            UseInPreview = true,
        };

        Pass m_PassTransparentBackface = new Pass()
        {
            Name = "TransparentBackface",
            LightMode = "TransparentBackface",
            TemplateName = "HDLitPass.template",
            MaterialName = "Lit",
            ShaderPassName = "SHADERPASS_FORWARD",
            CullOverride = "Cull Front",
            ShaderStages = HDSubShaderUtilities.s_ShaderStagesRasterization,
            ExtraDefines = HDSubShaderUtilities.s_ExtraDefinesForwardTransparent,
            ZTestOverride = HDSubShaderUtilities.zTestTransparent,
            ColorMaskOverride = "ColorMask [_ColorMaskTransparentVel] 1",
            ZWriteOverride = HDSubShaderUtilities.ZWriteDefault,
            Includes = new List<string>()
            {
                "#include \"Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassForward.hlsl\"",
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
                HDLitMasterNode.AlbedoSlotId,
                HDLitMasterNode.NormalSlotId,
                HDLitMasterNode.BentNormalSlotId,
                HDLitMasterNode.TangentSlotId,
                HDLitMasterNode.SubsurfaceMaskSlotId,
                HDLitMasterNode.ThicknessSlotId,
                HDLitMasterNode.DiffusionProfileHashSlotId,
                HDLitMasterNode.IridescenceMaskSlotId,
                HDLitMasterNode.IridescenceThicknessSlotId,
                HDLitMasterNode.SpecularColorSlotId,
                HDLitMasterNode.CoatMaskSlotId,
                HDLitMasterNode.MetallicSlotId,
                HDLitMasterNode.EmissionSlotId,
                HDLitMasterNode.SmoothnessSlotId,
                HDLitMasterNode.AmbientOcclusionSlotId,
                HDLitMasterNode.SpecularOcclusionSlotId,
                HDLitMasterNode.AlphaSlotId,
                HDLitMasterNode.AlphaThresholdSlotId,
                HDLitMasterNode.AnisotropySlotId,
                HDLitMasterNode.SpecularAAScreenSpaceVarianceSlotId,
                HDLitMasterNode.SpecularAAThresholdSlotId,
                HDLitMasterNode.RefractionIndexSlotId,
                HDLitMasterNode.RefractionColorSlotId,
                HDLitMasterNode.RefractionDistanceSlotId,
                HDLitMasterNode.DepthOffsetSlotId,
            },
            VertexShaderSlots = new List<int>()
            {
                HDLitMasterNode.PositionSlotId,
                HDLitMasterNode.VertexNormalSlotID,
                HDLitMasterNode.VertexTangentSlotID
            },
            UseInPreview = true,
            OnGeneratePassImpl = (IMasterNode node, ref Pass pass) =>
            {
                HDSubShaderUtilities.SetBlendModeForTransparentBackface(ref pass);
            }
        };

        Pass m_PassForward = new Pass()
        {
            Name = "Forward",
            LightMode = "Forward",
            TemplateName = "HDLitPass.template",
            MaterialName = "Lit",
            ShaderPassName = "SHADERPASS_FORWARD",
            CullOverride = HDSubShaderUtilities.cullModeForward,
            ZTestOverride = HDSubShaderUtilities.zTestDepthEqualForOpaque,
            ZWriteOverride = HDSubShaderUtilities.ZWriteDefault,
            // ExtraDefines are set when the pass is generated
            Includes = new List<string>()
            {
                "#include \"Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassForward.hlsl\"",
            },
            ShaderStages = HDSubShaderUtilities.s_ShaderStagesRasterization,
            ExtraDefines = new List<string>()
            {
                "#pragma only_renderers d3d11 ps4 xboxone vulkan metal switch",
                DefineRaytracingKeyword(RayTracingNode.RaytracingVariant.High),
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
                HDLitMasterNode.AlbedoSlotId,
                HDLitMasterNode.NormalSlotId,
                HDLitMasterNode.BentNormalSlotId,
                HDLitMasterNode.TangentSlotId,
                HDLitMasterNode.SubsurfaceMaskSlotId,
                HDLitMasterNode.ThicknessSlotId,
                HDLitMasterNode.DiffusionProfileHashSlotId,
                HDLitMasterNode.IridescenceMaskSlotId,
                HDLitMasterNode.IridescenceThicknessSlotId,
                HDLitMasterNode.SpecularColorSlotId,
                HDLitMasterNode.CoatMaskSlotId,
                HDLitMasterNode.MetallicSlotId,
                HDLitMasterNode.EmissionSlotId,
                HDLitMasterNode.SmoothnessSlotId,
                HDLitMasterNode.AmbientOcclusionSlotId,
                HDLitMasterNode.SpecularOcclusionSlotId,
                HDLitMasterNode.AlphaSlotId,
                HDLitMasterNode.AlphaThresholdSlotId,
                HDLitMasterNode.AnisotropySlotId,
                HDLitMasterNode.SpecularAAScreenSpaceVarianceSlotId,
                HDLitMasterNode.SpecularAAThresholdSlotId,
                HDLitMasterNode.RefractionIndexSlotId,
                HDLitMasterNode.RefractionColorSlotId,
                HDLitMasterNode.RefractionDistanceSlotId,
                HDLitMasterNode.LightingSlotId,
                HDLitMasterNode.BackLightingSlotId,
                HDLitMasterNode.DepthOffsetSlotId,
            },
            VertexShaderSlots = new List<int>()
            {
                HDLitMasterNode.PositionSlotId,
                HDLitMasterNode.VertexNormalSlotID,
                HDLitMasterNode.VertexTangentSlotID
            },
            UseInPreview = true,

            OnGeneratePassImpl = (IMasterNode node, ref Pass pass) =>
            {
                var masterNode = node as HDLitMasterNode;
                HDSubShaderUtilities.SetStencilStateForForward(ref pass);
                HDSubShaderUtilities.SetBlendModeForForward(ref pass);

                pass.ExtraDefines.Remove("#ifndef DEBUG_DISPLAY\n#define SHADERPASS_FORWARD_BYPASS_ALPHA_TEST\n#endif");

                pass.ColorMaskOverride = "ColorMask [_ColorMaskTransparentVel] 1";
                if (masterNode.surfaceType == SurfaceType.Opaque && masterNode.alphaTest.isOn)
                {
                    // In case of opaque we don't want to perform the alpha test, it is done in depth prepass and we use depth equal for ztest (setup from UI)
                    // Don't do it with debug display mode as it is possible there is no depth prepass in this case
                    pass.ExtraDefines.Add("#ifndef DEBUG_DISPLAY\n#define SHADERPASS_FORWARD_BYPASS_ALPHA_TEST\n#endif");
                    pass.ZTestOverride = "ZTest Equal";
                }
            }
        };

        Pass m_PassTransparentDepthPostpass = new Pass()
        {
            Name = "TransparentDepthPostpass",
            LightMode = "TransparentDepthPostpass",
            TemplateName = "HDLitPass.template",
            MaterialName = "Lit",
            ShaderPassName = "SHADERPASS_DEPTH_ONLY",
            BlendOverride = "Blend One Zero",
            ZWriteOverride = "ZWrite On",
            ColorMaskOverride = "ColorMask 0",
            CullOverride = HDSubShaderUtilities.defaultCullMode,
            ShaderStages = HDSubShaderUtilities.s_ShaderStagesRasterization,
            ExtraDefines = new List<string>()
            {
                "#pragma only_renderers d3d11 ps4 xboxone vulkan metal switch",
                "#define CUTOFF_TRANSPARENT_DEPTH_POSTPASS",
                DefineRaytracingKeyword(RayTracingNode.RaytracingVariant.High),
            },
            Includes = new List<string>()
            {
                "#include \"Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassDepthOnly.hlsl\"",
            },
            PixelShaderSlots = new List<int>()
            {
                HDLitMasterNode.AlphaSlotId,
                HDLitMasterNode.AlphaThresholdDepthPostpassSlotId,
                HDLitMasterNode.DepthOffsetSlotId,
            },
            VertexShaderSlots = new List<int>()
            {
                HDLitMasterNode.PositionSlotId,
                HDLitMasterNode.VertexNormalSlotID,
                HDLitMasterNode.VertexTangentSlotID
            },
            UseInPreview = false,
        };

        Pass m_PassRaytracingIndirect = new Pass()
        {
            Name = "IndirectDXR",
            LightMode = "IndirectDXR",
            TemplateName = "HDLitPass.template",
            MaterialName = "Lit",
            ShaderPassName = "SHADERPASS_RAYTRACING_INDIRECT",
            ShaderStages = HDSubShaderUtilities.s_ShaderStagesRayTracing,
            ExtraDefines = new List<string>()
            {
                "#pragma only_renderers d3d11",
                "#pragma multi_compile _ LIGHTMAP_ON",
                "#pragma multi_compile _ DIRLIGHTMAP_COMBINED",
                "#pragma multi_compile _ DYNAMICLIGHTMAP_ON",
                "#pragma multi_compile _ DIFFUSE_LIGHTING_ONLY",
                "#define SHADOW_LOW",
                DefineRaytracingKeyword(RayTracingNode.RaytracingVariant.Low)
            },
            Includes = new List<string>()
            {
                "#include \"Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassRaytracingIndirect.hlsl\"",
            },
            PixelShaderSlots = new List<int>()
            {
                HDLitMasterNode.AlbedoSlotId,
                HDLitMasterNode.NormalSlotId,
                HDLitMasterNode.BentNormalSlotId,
                HDLitMasterNode.TangentSlotId,
                HDLitMasterNode.SubsurfaceMaskSlotId,
                HDLitMasterNode.ThicknessSlotId,
                HDLitMasterNode.DiffusionProfileHashSlotId,
                HDLitMasterNode.IridescenceMaskSlotId,
                HDLitMasterNode.IridescenceThicknessSlotId,
                HDLitMasterNode.SpecularColorSlotId,
                HDLitMasterNode.CoatMaskSlotId,
                HDLitMasterNode.MetallicSlotId,
                HDLitMasterNode.EmissionSlotId,
                HDLitMasterNode.SmoothnessSlotId,
                HDLitMasterNode.AmbientOcclusionSlotId,
                HDLitMasterNode.SpecularOcclusionSlotId,
                HDLitMasterNode.AlphaSlotId,
                HDLitMasterNode.AlphaThresholdSlotId,
                HDLitMasterNode.AnisotropySlotId,
                HDLitMasterNode.SpecularAAScreenSpaceVarianceSlotId,
                HDLitMasterNode.SpecularAAThresholdSlotId,
                HDLitMasterNode.RefractionIndexSlotId,
                HDLitMasterNode.RefractionColorSlotId,
                HDLitMasterNode.RefractionDistanceSlotId,
            },
            VertexShaderSlots = new List<int>()
            {
                HDLitMasterNode.PositionSlotId,
                HDLitMasterNode.VertexNormalSlotID,
                HDLitMasterNode.VertexTangentSlotID
            },
            UseInPreview = false
        };

        Pass m_PassRaytracingVisibility = new Pass()
        {
            Name = "VisibilityDXR",
            LightMode = "VisibilityDXR",
            TemplateName = "HDLitPass.template",
            MaterialName = "Lit",
            ShaderPassName = "SHADERPASS_RAYTRACING_VISIBILITY",
            ShaderStages = HDSubShaderUtilities.s_ShaderStagesRayTracing,
            ExtraDefines = new List<string>()
            {
                "#pragma only_renderers d3d11",
                "#pragma multi_compile _ TRANSPARENT_COLOR_SHADOW",
                DefineRaytracingKeyword(RayTracingNode.RaytracingVariant.Low)
            },
            Includes = new List<string>()
            {
                "#include \"Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassRaytracingVisibility.hlsl\"",
            },
            PixelShaderSlots = new List<int>()
            {
                HDLitMasterNode.AlbedoSlotId,
                HDLitMasterNode.NormalSlotId,
                HDLitMasterNode.BentNormalSlotId,
                HDLitMasterNode.TangentSlotId,
                HDLitMasterNode.SubsurfaceMaskSlotId,
                HDLitMasterNode.ThicknessSlotId,
                HDLitMasterNode.DiffusionProfileHashSlotId,
                HDLitMasterNode.IridescenceMaskSlotId,
                HDLitMasterNode.IridescenceThicknessSlotId,
                HDLitMasterNode.SpecularColorSlotId,
                HDLitMasterNode.CoatMaskSlotId,
                HDLitMasterNode.MetallicSlotId,
                HDLitMasterNode.EmissionSlotId,
                HDLitMasterNode.SmoothnessSlotId,
                HDLitMasterNode.AmbientOcclusionSlotId,
                HDLitMasterNode.SpecularOcclusionSlotId,
                HDLitMasterNode.AlphaSlotId,
                HDLitMasterNode.AlphaThresholdSlotId,
                HDLitMasterNode.AnisotropySlotId,
                HDLitMasterNode.SpecularAAScreenSpaceVarianceSlotId,
                HDLitMasterNode.SpecularAAThresholdSlotId,
                HDLitMasterNode.RefractionIndexSlotId,
                HDLitMasterNode.RefractionColorSlotId,
                HDLitMasterNode.RefractionDistanceSlotId,
            },
            VertexShaderSlots = new List<int>()
            {
                HDLitMasterNode.PositionSlotId,
                HDLitMasterNode.VertexNormalSlotID,
                HDLitMasterNode.VertexTangentSlotID
            },
            UseInPreview = false
        };

        Pass m_PassRaytracingForward = new Pass()
        {
            Name = "ForwardDXR",
            LightMode = "ForwardDXR",
            TemplateName = "HDLitPass.template",
            MaterialName = "Lit",
            ShaderPassName = "SHADERPASS_RAYTRACING_FORWARD",
            ShaderStages = HDSubShaderUtilities.s_ShaderStagesRayTracing,
            ExtraDefines = new List<string>()
            {
                "#pragma only_renderers d3d11",
                "#pragma multi_compile _ LIGHTMAP_ON",
                "#pragma multi_compile _ DIRLIGHTMAP_COMBINED",
                "#pragma multi_compile _ DYNAMICLIGHTMAP_ON",
                "#define SHADOW_LOW",
                DefineRaytracingKeyword(RayTracingNode.RaytracingVariant.High)
            },
            Includes = new List<string>()
            {
                "#include \"Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassRaytracingForward.hlsl\"",
            },
            PixelShaderSlots = new List<int>()
            {
                HDLitMasterNode.AlbedoSlotId,
                HDLitMasterNode.NormalSlotId,
                HDLitMasterNode.BentNormalSlotId,
                HDLitMasterNode.TangentSlotId,
                HDLitMasterNode.SubsurfaceMaskSlotId,
                HDLitMasterNode.ThicknessSlotId,
                HDLitMasterNode.DiffusionProfileHashSlotId,
                HDLitMasterNode.IridescenceMaskSlotId,
                HDLitMasterNode.IridescenceThicknessSlotId,
                HDLitMasterNode.SpecularColorSlotId,
                HDLitMasterNode.CoatMaskSlotId,
                HDLitMasterNode.MetallicSlotId,
                HDLitMasterNode.EmissionSlotId,
                HDLitMasterNode.SmoothnessSlotId,
                HDLitMasterNode.AmbientOcclusionSlotId,
                HDLitMasterNode.SpecularOcclusionSlotId,
                HDLitMasterNode.AlphaSlotId,
                HDLitMasterNode.AlphaThresholdSlotId,
                HDLitMasterNode.AnisotropySlotId,
                HDLitMasterNode.SpecularAAScreenSpaceVarianceSlotId,
                HDLitMasterNode.SpecularAAThresholdSlotId,
                HDLitMasterNode.RefractionIndexSlotId,
                HDLitMasterNode.RefractionColorSlotId,
                HDLitMasterNode.RefractionDistanceSlotId,
            },
            VertexShaderSlots = new List<int>()
            {
                HDLitMasterNode.PositionSlotId,
                HDLitMasterNode.VertexNormalSlotID,
                HDLitMasterNode.VertexTangentSlotID
            },
            UseInPreview = false
        };

        Pass m_PassRaytracingGBuffer = new Pass()
        {
            Name = "GBufferDXR",
            LightMode = "GBufferDXR",
            TemplateName = "HDLitPass.template",
            MaterialName = "Lit",
            ShaderPassName = "SHADERPASS_RAYTRACING_GBUFFER",
            ShaderStages = HDSubShaderUtilities.s_ShaderStagesRayTracing,
            ExtraDefines = new List<string>()
            {
                "#pragma only_renderers d3d11",
                "#pragma multi_compile _ LIGHTMAP_ON",
                "#pragma multi_compile _ DIRLIGHTMAP_COMBINED",
                "#pragma multi_compile _ DYNAMICLIGHTMAP_ON",
                "#define SHADOW_LOW",
                DefineRaytracingKeyword(RayTracingNode.RaytracingVariant.High)
            },
            Includes = new List<string>()
            {
                "#include \"Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderpassRaytracingGBuffer.hlsl\"",
            },
            PixelShaderSlots = new List<int>()
            {
                HDLitMasterNode.AlbedoSlotId,
                HDLitMasterNode.NormalSlotId,
                HDLitMasterNode.BentNormalSlotId,
                HDLitMasterNode.TangentSlotId,
                HDLitMasterNode.SubsurfaceMaskSlotId,
                HDLitMasterNode.ThicknessSlotId,
                HDLitMasterNode.DiffusionProfileHashSlotId,
                HDLitMasterNode.IridescenceMaskSlotId,
                HDLitMasterNode.IridescenceThicknessSlotId,
                HDLitMasterNode.SpecularColorSlotId,
                HDLitMasterNode.CoatMaskSlotId,
                HDLitMasterNode.MetallicSlotId,
                HDLitMasterNode.EmissionSlotId,
                HDLitMasterNode.SmoothnessSlotId,
                HDLitMasterNode.AmbientOcclusionSlotId,
                HDLitMasterNode.SpecularOcclusionSlotId,
                HDLitMasterNode.AlphaSlotId,
                HDLitMasterNode.AlphaThresholdSlotId,
                HDLitMasterNode.AnisotropySlotId,
                HDLitMasterNode.SpecularAAScreenSpaceVarianceSlotId,
                HDLitMasterNode.SpecularAAThresholdSlotId,
                HDLitMasterNode.RefractionIndexSlotId,
                HDLitMasterNode.RefractionColorSlotId,
                HDLitMasterNode.RefractionDistanceSlotId,
            },
            VertexShaderSlots = new List<int>()
            {
                HDLitMasterNode.PositionSlotId,
                HDLitMasterNode.VertexNormalSlotID,
                HDLitMasterNode.VertexTangentSlotID
            },
            UseInPreview = false
        };

        Pass m_PassRaytracingSubSurface = new Pass()
        {
            Name = "SubSurfaceDXR",
            LightMode = "SubSurfaceDXR",
            TemplateName = "HDLitPass.template",
            MaterialName = "Lit",
            ShaderPassName = "SHADERPASS_RAYTRACING_SUB_SURFACE",
            ShaderStages = HDSubShaderUtilities.s_ShaderStagesRayTracing,
            ExtraDefines = new List<string>()
            {
                "#pragma only_renderers d3d11",
                "#pragma multi_compile _ LIGHTMAP_ON",
                "#pragma multi_compile _ DIRLIGHTMAP_COMBINED",
                "#pragma multi_compile _ DYNAMICLIGHTMAP_ON",
                "#define SHADOW_LOW",
                DefineRaytracingKeyword(RayTracingNode.RaytracingVariant.High)
            },
            Includes = new List<string>()
            {
                "#include \"Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderpassRaytracingSubSurface.hlsl\"",
            },
            PixelShaderSlots = new List<int>()
            {
                HDLitMasterNode.AlbedoSlotId,
                HDLitMasterNode.NormalSlotId,
                HDLitMasterNode.BentNormalSlotId,
                HDLitMasterNode.TangentSlotId,
                HDLitMasterNode.SubsurfaceMaskSlotId,
                HDLitMasterNode.ThicknessSlotId,
                HDLitMasterNode.DiffusionProfileHashSlotId,
                HDLitMasterNode.IridescenceMaskSlotId,
                HDLitMasterNode.IridescenceThicknessSlotId,
                HDLitMasterNode.SpecularColorSlotId,
                HDLitMasterNode.CoatMaskSlotId,
                HDLitMasterNode.MetallicSlotId,
                HDLitMasterNode.EmissionSlotId,
                HDLitMasterNode.SmoothnessSlotId,
                HDLitMasterNode.AmbientOcclusionSlotId,
                HDLitMasterNode.SpecularOcclusionSlotId,
                HDLitMasterNode.AlphaSlotId,
                HDLitMasterNode.AlphaThresholdSlotId,
                HDLitMasterNode.AnisotropySlotId,
                HDLitMasterNode.SpecularAAScreenSpaceVarianceSlotId,
                HDLitMasterNode.SpecularAAThresholdSlotId,
                HDLitMasterNode.RefractionIndexSlotId,
                HDLitMasterNode.RefractionColorSlotId,
                HDLitMasterNode.RefractionDistanceSlotId,
            },
            VertexShaderSlots = new List<int>()
            {
                HDLitMasterNode.PositionSlotId,
                HDLitMasterNode.VertexNormalSlotID,
                HDLitMasterNode.VertexTangentSlotID
            },
            UseInPreview = false
        };

        Pass m_PassPathTracing = new Pass()
        {
            Name = "PathTracingDXR",
            LightMode = "PathTracingDXR",
            TemplateName = "HDLitPass.template",
            MaterialName = "Lit",
            ShaderPassName = "SHADERPASS_PATH_TRACING",
            ShaderStages = HDSubShaderUtilities.s_ShaderStagesRayTracing,
            ExtraDefines = new List<string>()
            {
                "#pragma only_renderers d3d11",
                "#define SHADOW_LOW",
                DefineRaytracingKeyword(RayTracingNode.RaytracingVariant.High)
            },
            Includes = new List<string>()
            {
                "#include \"Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassPathTracing.hlsl\"",
            },
            PixelShaderSlots = new List<int>()
            {
                HDLitMasterNode.AlbedoSlotId,
                HDLitMasterNode.NormalSlotId,
                HDLitMasterNode.BentNormalSlotId,
                HDLitMasterNode.TangentSlotId,
                HDLitMasterNode.SubsurfaceMaskSlotId,
                HDLitMasterNode.ThicknessSlotId,
                HDLitMasterNode.DiffusionProfileHashSlotId,
                HDLitMasterNode.IridescenceMaskSlotId,
                HDLitMasterNode.IridescenceThicknessSlotId,
                HDLitMasterNode.SpecularColorSlotId,
                HDLitMasterNode.CoatMaskSlotId,
                HDLitMasterNode.MetallicSlotId,
                HDLitMasterNode.EmissionSlotId,
                HDLitMasterNode.SmoothnessSlotId,
                HDLitMasterNode.AmbientOcclusionSlotId,
                HDLitMasterNode.SpecularOcclusionSlotId,
                HDLitMasterNode.AlphaSlotId,
                HDLitMasterNode.AlphaThresholdSlotId,
                HDLitMasterNode.AnisotropySlotId,
                HDLitMasterNode.SpecularAAScreenSpaceVarianceSlotId,
                HDLitMasterNode.SpecularAAThresholdSlotId,
                HDLitMasterNode.RefractionIndexSlotId,
                HDLitMasterNode.RefractionColorSlotId,
                HDLitMasterNode.RefractionDistanceSlotId,
            },
            VertexShaderSlots = new List<int>()
            {
                HDLitMasterNode.PositionSlotId,
                HDLitMasterNode.VertexNormalSlotID,
                HDLitMasterNode.VertexTangentSlotID
            },
            UseInPreview = false
        };
        public int GetPreviewPassIndex() { return 0; }

        private static ActiveFields GetActiveFieldsFromMasterNode(AbstractMaterialNode iMasterNode, Pass pass)
        {
            var activeFields = new ActiveFields();
            var baseActiveFields = activeFields.baseInstance;

            HDLitMasterNode masterNode = iMasterNode as HDLitMasterNode;
            if (masterNode == null)
                return activeFields;

            if (masterNode.doubleSidedMode != DoubleSidedMode.Disabled)
            {
                baseActiveFields.AddAll("DoubleSided");
                if (pass.ShaderPassName != "SHADERPASS_MOTION_VECTORS")   // HACK to get around lack of a good interpolator dependency system
                {                                                   // we need to be able to build interpolators using multiple input structs
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

            switch(masterNode.normalDropOffSpace)
            {
                case NormalDropOffSpace.Tangent:
                    baseActiveFields.AddAll("NormalDropOffTS");
                    break;
                case NormalDropOffSpace.Object:
                    baseActiveFields.AddAll("NormalDropOffOS");
                    break;
                case NormalDropOffSpace.World:
                    baseActiveFields.AddAll("NormalDropOffWS");
                    break;
                default:
                    UnityEngine.Debug.LogError("Unknown normal drop off space: " + masterNode.normalDropOffSpace);
                    break;
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
                if (pass.PixelShaderUsesSlot(HDLitMasterNode.AlphaThresholdShadowSlotId) && masterNode.alphaTestShadow.isOn)
                {
                    baseActiveFields.AddAll("AlphaTestShadow");
                    ++count;
                }
                else if (pass.PixelShaderUsesSlot(HDLitMasterNode.AlphaThresholdSlotId))
                {
                    baseActiveFields.AddAll("AlphaTest");
                    ++count;
                }

                if (pass.PixelShaderUsesSlot(HDLitMasterNode.AlphaThresholdDepthPrepassSlotId))
                {
                    baseActiveFields.AddAll("AlphaTestPrepass");
                    ++count;
                }
                if (pass.PixelShaderUsesSlot(HDLitMasterNode.AlphaThresholdDepthPostpassSlotId))
                {
                    baseActiveFields.AddAll("AlphaTestPostpass");
                    ++count;
                }
                UnityEngine.Debug.Assert(count == 1, "Alpha test value not set correctly");
            }

            if (masterNode.surfaceType != SurfaceType.Opaque)
            {
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

            if (masterNode.specularAA.isOn && pass.PixelShaderUsesSlot(HDLitMasterNode.SpecularAAThresholdSlotId) && pass.PixelShaderUsesSlot(HDLitMasterNode.SpecularAAScreenSpaceVarianceSlotId))
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

                    case ScreenSpaceRefraction.RefractionModel.Thin:
                        baseActiveFields.AddAll("RefractionThin");
                        break;

                    default:
                        UnityEngine.Debug.LogError("Unknown refraction model: " + masterNode.refractionModel);
                        break;
                }
            }

            if (masterNode.IsSlotConnected(HDLitMasterNode.BentNormalSlotId) && pass.PixelShaderUsesSlot(HDLitMasterNode.BentNormalSlotId))
            {
                baseActiveFields.AddAll("BentNormal");
            }

            if (masterNode.IsSlotConnected(HDLitMasterNode.TangentSlotId) && pass.PixelShaderUsesSlot(HDLitMasterNode.TangentSlotId))
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

            if (pass.PixelShaderUsesSlot(HDLitMasterNode.AmbientOcclusionSlotId))
            {
                var occlusionSlot = masterNode.FindSlot<Vector1MaterialSlot>(HDLitMasterNode.AmbientOcclusionSlotId);

                bool connected = masterNode.IsSlotConnected(HDLitMasterNode.AmbientOcclusionSlotId);
                if (connected || occlusionSlot.value != occlusionSlot.defaultValue)
                {
                    baseActiveFields.AddAll("AmbientOcclusion");
                }
            }

            if (pass.PixelShaderUsesSlot(HDLitMasterNode.CoatMaskSlotId))
            {
                var coatMaskSlot = masterNode.FindSlot<Vector1MaterialSlot>(HDLitMasterNode.CoatMaskSlotId);

                bool connected = masterNode.IsSlotConnected(HDLitMasterNode.CoatMaskSlotId);
                if (connected || coatMaskSlot.value > 0.0f)
                {
                    baseActiveFields.AddAll("CoatMask");
                }
            }

            if (masterNode.IsSlotConnected(HDLitMasterNode.LightingSlotId) && pass.PixelShaderUsesSlot(HDLitMasterNode.LightingSlotId))
            {
                baseActiveFields.AddAll("LightingGI");
            }
            if (masterNode.IsSlotConnected(HDLitMasterNode.BackLightingSlotId) && pass.PixelShaderUsesSlot(HDLitMasterNode.LightingSlotId))
            {
                baseActiveFields.AddAll("BackLightingGI");
            }

            if (masterNode.depthOffset.isOn && pass.PixelShaderUsesSlot(HDLitMasterNode.DepthOffsetSlotId))
                baseActiveFields.AddAll("DepthOffset");

            if (masterNode.supportLodCrossFade.isOn)
                baseActiveFields.AddAll("LodCrossFade");

            return activeFields;
        }

        private static bool GenerateShaderPassLit(HDLitMasterNode masterNode, Pass pass, GenerationMode mode, ShaderGenerator result, List<string> sourceAssetDependencyPaths, bool instancingFlag = true)
        {
            if (mode == GenerationMode.ForReals || pass.UseInPreview)
            {
                pass.OnGeneratePass(masterNode);

                // apply master node options to active fields
                var activeFields = GetActiveFieldsFromMasterNode(masterNode, pass);

                // use standard shader pass generation
                bool vertexActive = false;
                if (masterNode.IsSlotConnected(HDLitMasterNode.PositionSlotId) ||
                    masterNode.IsSlotConnected(HDLitMasterNode.VertexNormalSlotID) ||
                    masterNode.IsSlotConnected(HDLitMasterNode.VertexTangentSlotID) )
                {
                    vertexActive = true;
                }
                return HDSubShaderUtilities.GenerateShaderPass(masterNode, pass, mode, activeFields, result, sourceAssetDependencyPaths, vertexActive, instancingFlag: instancingFlag);
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
                // HDLitSubShader.cs
                sourceAssetDependencyPaths.Add(AssetDatabase.GUIDToAssetPath("bac1a9627cfec924fa2ea9c65af8eeca"));
                // HDSubShaderUtilities.cs
                sourceAssetDependencyPaths.Add(AssetDatabase.GUIDToAssetPath("713ced4e6eef4a44799a4dd59041484b"));
            }

            var masterNode = iMasterNode as HDLitMasterNode;

            var subShader = new ShaderGenerator();
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
#pragma warning disable CS0618 // Type or member is obsolete
                            if (masterNode.m_DrawBeforeRefraction)
                            {
                                masterNode.m_DrawBeforeRefraction = false;
#pragma warning restore CS0618 // Type or member is obsolete
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
                bool transparent = !opaque;

                bool distortionActive = transparent && masterNode.distortion.isOn;
                bool transparentBackfaceActive = transparent && masterNode.backThenFrontRendering.isOn;
                bool transparentDepthPrepassActive = transparent && masterNode.alphaTestDepthPrepass.isOn;
                bool transparentDepthPostpassActive = transparent && masterNode.alphaTestDepthPostpass.isOn;

                GenerateShaderPassLit(masterNode, m_PassShadowCaster, mode, subShader, sourceAssetDependencyPaths);
                GenerateShaderPassLit(masterNode, m_PassMETA, mode, subShader, sourceAssetDependencyPaths);
                GenerateShaderPassLit(masterNode, m_SceneSelectionPass, mode, subShader, sourceAssetDependencyPaths);

                GenerateShaderPassLit(masterNode, m_PassDepthOnly, mode, subShader, sourceAssetDependencyPaths);
                GenerateShaderPassLit(masterNode, m_PassGBuffer, mode, subShader, sourceAssetDependencyPaths);
                GenerateShaderPassLit(masterNode, m_PassMotionVectors, mode, subShader, sourceAssetDependencyPaths);

                if (distortionActive)
                {
                    GenerateShaderPassLit(masterNode, m_PassDistortion, mode, subShader, sourceAssetDependencyPaths);
                }

                if (transparentBackfaceActive)
                {
                    GenerateShaderPassLit(masterNode, m_PassTransparentBackface, mode, subShader, sourceAssetDependencyPaths);
                }

                // Assign define here based on opaque or transparent to save some variant
                m_PassForward.ExtraDefines = opaque ? HDSubShaderUtilities.s_ExtraDefinesForwardOpaque : HDSubShaderUtilities.s_ExtraDefinesForwardTransparent;
                GenerateShaderPassLit(masterNode, m_PassForward, mode, subShader, sourceAssetDependencyPaths);

                if (transparentDepthPrepassActive)
                {
                    GenerateShaderPassLit(masterNode, m_PassTransparentDepthPrepass, mode, subShader, sourceAssetDependencyPaths);
                }

                if (transparentDepthPostpassActive)
                {
                    GenerateShaderPassLit(masterNode, m_PassTransparentDepthPostpass, mode, subShader, sourceAssetDependencyPaths);
                }
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
                    GenerateShaderPassLit(masterNode, m_PassRaytracingIndirect, mode, subShader, sourceAssetDependencyPaths, instancingFlag: false);
                    GenerateShaderPassLit(masterNode, m_PassRaytracingVisibility, mode, subShader, sourceAssetDependencyPaths, instancingFlag: false);
                    GenerateShaderPassLit(masterNode, m_PassRaytracingForward, mode, subShader, sourceAssetDependencyPaths, instancingFlag: false);
                    GenerateShaderPassLit(masterNode, m_PassRaytracingGBuffer, mode, subShader, sourceAssetDependencyPaths, instancingFlag: false);
                    GenerateShaderPassLit(masterNode, m_PassRaytracingSubSurface, mode, subShader, sourceAssetDependencyPaths, instancingFlag: false);
                    GenerateShaderPassLit(masterNode, m_PassPathTracing, mode, subShader, sourceAssetDependencyPaths, instancingFlag: false);
                }
                subShader.Deindent();
                subShader.AddShaderChunk("}", false);
            }

            if (!masterNode.OverrideEnabled)
            {
                subShader.AddShaderChunk(@"CustomEditor ""UnityEditor.Rendering.HighDefinition.HDLitGUI""");
            }

            return subShader.GetShaderString(0);
        }

        public bool IsPipelineCompatible(RenderPipelineAsset renderPipelineAsset)
        {
            return renderPipelineAsset is HDRenderPipelineAsset;
        }
    }
}
