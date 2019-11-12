using System.Collections.Generic;
using UnityEditor.ShaderGraph;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Rendering;
using Data.Util;

namespace UnityEditor.Rendering.HighDefinition
{
    [FormerName("UnityEditor.Experimental.Rendering.HDPipeline.SpeedTreeLitSubShader")]
    class SpeedTreeLitSubShader : ISpeedTreeLitSubShader
    {
        internal static string DefineRaytracingKeyword(RayTracingNode.RaytracingVariant variant)
            => $"#define {RayTracingNode.RaytracingVariantKeyword(variant)}";

        Pass m_PassGBuffer = new Pass()
        {
            Name = "GBuffer",
            LightMode = "GBuffer",
            TemplateName = "SpeedTreeLitPass.template",
            MaterialName = "Lit",
            ShaderPassName = "SHADERPASS_GBUFFER",
            ZTestOverride = HDSubShaderUtilities.zTestGBuffer,
            CullOverride = HDSubShaderUtilities.defaultCullMode,
            ExtraDefines = new List<string>()
            {
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
                SpeedTreeLitMasterNode.AlbedoSlotId,
                SpeedTreeLitMasterNode.NormalSlotId,
                SpeedTreeLitMasterNode.BentNormalSlotId,
                SpeedTreeLitMasterNode.TangentSlotId,
                SpeedTreeLitMasterNode.SubsurfaceMaskSlotId,
                SpeedTreeLitMasterNode.ThicknessSlotId,
                SpeedTreeLitMasterNode.DiffusionProfileHashSlotId,
                SpeedTreeLitMasterNode.IridescenceMaskSlotId,
                SpeedTreeLitMasterNode.IridescenceThicknessSlotId,
                SpeedTreeLitMasterNode.SpecularColorSlotId,
                SpeedTreeLitMasterNode.CoatMaskSlotId,
                SpeedTreeLitMasterNode.MetallicSlotId,
                SpeedTreeLitMasterNode.EmissionSlotId,
                SpeedTreeLitMasterNode.SmoothnessSlotId,
                SpeedTreeLitMasterNode.AmbientOcclusionSlotId,
                SpeedTreeLitMasterNode.SpecularOcclusionSlotId,
                SpeedTreeLitMasterNode.AlphaSlotId,
                SpeedTreeLitMasterNode.AlphaThresholdSlotId,
                SpeedTreeLitMasterNode.AnisotropySlotId,
                SpeedTreeLitMasterNode.SpecularAAScreenSpaceVarianceSlotId,
                SpeedTreeLitMasterNode.SpecularAAThresholdSlotId,
                SpeedTreeLitMasterNode.RefractionIndexSlotId,
                SpeedTreeLitMasterNode.RefractionColorSlotId,
                SpeedTreeLitMasterNode.RefractionDistanceSlotId,
                SpeedTreeLitMasterNode.LightingSlotId,
                SpeedTreeLitMasterNode.BackLightingSlotId,
                SpeedTreeLitMasterNode.DepthOffsetSlotId,
            },
            VertexShaderSlots = new List<int>()
            {
                SpeedTreeLitMasterNode.PositionSlotId,
                SpeedTreeLitMasterNode.VertexNormalSlotID,
                SpeedTreeLitMasterNode.VertexTangentSlotID
            },
            UseInPreview = true,

            OnGeneratePassImpl = (IMasterNode node, ref Pass pass) =>
            {
                var masterNode = node as SpeedTreeLitMasterNode;
                HDSubShaderUtilities.SetStencilStateForGBuffer(ref pass);

                // When we have alpha test, we will force a depth prepass so we always bypass the clip instruction in the GBuffer
                // Don't do it with debug display mode as it is possible there is no depth prepass in this case
                pass.ExtraDefines.Add("#ifndef DEBUG_DISPLAY\n#define SHADERPASS_GBUFFER_BYPASS_ALPHA_TEST\n#endif");
            }
        };

        Pass m_PassMETA = new Pass()
        {
            Name = "META",
            LightMode = "META",
            TemplateName = "SpeedTreeLitPass.template",
            MaterialName = "Lit",
            ShaderPassName = "SHADERPASS_LIGHT_TRANSPORT",
            CullOverride = "Cull Off",
            Includes = new List<string>()
            {
                "#include \"Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassLightTransport.hlsl\"",
            },
            ExtraDefines = new List<string>()
            {
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
                SpeedTreeLitMasterNode.AlbedoSlotId,
                SpeedTreeLitMasterNode.NormalSlotId,
                SpeedTreeLitMasterNode.BentNormalSlotId,
                SpeedTreeLitMasterNode.TangentSlotId,
                SpeedTreeLitMasterNode.SubsurfaceMaskSlotId,
                SpeedTreeLitMasterNode.ThicknessSlotId,
                SpeedTreeLitMasterNode.DiffusionProfileHashSlotId,
                SpeedTreeLitMasterNode.IridescenceMaskSlotId,
                SpeedTreeLitMasterNode.IridescenceThicknessSlotId,
                SpeedTreeLitMasterNode.SpecularColorSlotId,
                SpeedTreeLitMasterNode.CoatMaskSlotId,
                SpeedTreeLitMasterNode.MetallicSlotId,
                SpeedTreeLitMasterNode.EmissionSlotId,
                SpeedTreeLitMasterNode.SmoothnessSlotId,
                SpeedTreeLitMasterNode.AmbientOcclusionSlotId,
                SpeedTreeLitMasterNode.SpecularOcclusionSlotId,
                SpeedTreeLitMasterNode.AlphaSlotId,
                SpeedTreeLitMasterNode.AlphaThresholdSlotId,
                SpeedTreeLitMasterNode.AnisotropySlotId,
                SpeedTreeLitMasterNode.SpecularAAScreenSpaceVarianceSlotId,
                SpeedTreeLitMasterNode.SpecularAAThresholdSlotId,
                SpeedTreeLitMasterNode.RefractionIndexSlotId,
                SpeedTreeLitMasterNode.RefractionColorSlotId,
                SpeedTreeLitMasterNode.RefractionDistanceSlotId,
            },
            VertexShaderSlots = new List<int>()
            {
                //SpeedTreeLitMasterNode.PositionSlotId
            },
            UseInPreview = false
        };

        Pass m_PassShadowCaster = new Pass()
        {
            Name = "ShadowCaster",
            LightMode = "ShadowCaster",
            TemplateName = "SpeedTreeLitPass.template",
            MaterialName = "Lit",
            ShaderPassName = "SHADERPASS_SHADOWS",
            ColorMaskOverride = "ColorMask 0",
            ZClipOverride = HDSubShaderUtilities.zClipShadowCaster,
            CullOverride = HDSubShaderUtilities.defaultCullMode,
            ZWriteOverride = HDSubShaderUtilities.zWriteOn,
            ExtraDefines = new List<string>()
            {
                DefineRaytracingKeyword(RayTracingNode.RaytracingVariant.High),
            },
            Includes = new List<string>()
            {
                "#include \"Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassDepthOnly.hlsl\"",
            },
            PixelShaderSlots = new List<int>()
            {
                SpeedTreeLitMasterNode.AlphaSlotId,
                SpeedTreeLitMasterNode.AlphaThresholdSlotId,
                SpeedTreeLitMasterNode.AlphaThresholdShadowSlotId,
                SpeedTreeLitMasterNode.DepthOffsetSlotId,
            },
            VertexShaderSlots = new List<int>()
            {
                SpeedTreeLitMasterNode.PositionSlotId,
                SpeedTreeLitMasterNode.VertexNormalSlotID,
                SpeedTreeLitMasterNode.VertexTangentSlotID
            },
            UseInPreview = false
        };

        Pass m_SceneSelectionPass = new Pass()
        {
            Name = "SceneSelectionPass",
            LightMode = "SceneSelectionPass",
            TemplateName = "SpeedTreeLitPass.template",
            MaterialName = "Lit",
            ShaderPassName = "SHADERPASS_DEPTH_ONLY",
            ColorMaskOverride = "ColorMask 0",
            ExtraDefines = new List<string>()
            {
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
                SpeedTreeLitMasterNode.AlphaSlotId,
                SpeedTreeLitMasterNode.AlphaThresholdSlotId,
                SpeedTreeLitMasterNode.DepthOffsetSlotId,
            },
            VertexShaderSlots = new List<int>()
            {
                SpeedTreeLitMasterNode.PositionSlotId,
                SpeedTreeLitMasterNode.VertexNormalSlotID,
                SpeedTreeLitMasterNode.VertexTangentSlotID
            },
            UseInPreview = false
        };

        Pass m_PassDepthOnly = new Pass()
        {
            Name = "DepthOnly",
            LightMode = "DepthOnly",
            TemplateName = "SpeedTreeLitPass.template",
            MaterialName = "Lit",
            ShaderPassName = "SHADERPASS_DEPTH_ONLY",
            CullOverride = HDSubShaderUtilities.defaultCullMode,
            ExtraDefines = HDSubShaderUtilities.s_ExtraDefinesDepthOrMotion,
            ZWriteOverride = HDSubShaderUtilities.zWriteOn,

            Includes = new List<string>()
            {
                "#include \"Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassDepthOnly.hlsl\"",
            },
            PixelShaderSlots = new List<int>()
            {
                SpeedTreeLitMasterNode.NormalSlotId,
                SpeedTreeLitMasterNode.SmoothnessSlotId,
                SpeedTreeLitMasterNode.AlphaSlotId,
                SpeedTreeLitMasterNode.AlphaThresholdSlotId,
                SpeedTreeLitMasterNode.DepthOffsetSlotId,
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
                SpeedTreeLitMasterNode.PositionSlotId,
                SpeedTreeLitMasterNode.VertexNormalSlotID,
                SpeedTreeLitMasterNode.VertexTangentSlotID
            },
            UseInPreview = true,

            OnGeneratePassImpl = (IMasterNode node, ref Pass pass) =>
            {
                var masterNode = node as SpeedTreeLitMasterNode;
                HDSubShaderUtilities.SetStencilStateForDepth(ref pass);
            }
        };

        Pass m_PassMotionVectors = new Pass()
        {
            Name = "MotionVectors",
            LightMode = "MotionVectors",
            TemplateName = "SpeedTreeLitPass.template",
            MaterialName = "Lit",
            ShaderPassName = "SHADERPASS_MOTION_VECTORS",
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
                SpeedTreeLitMasterNode.NormalSlotId,
                SpeedTreeLitMasterNode.SmoothnessSlotId,
                SpeedTreeLitMasterNode.AlphaSlotId,
                SpeedTreeLitMasterNode.AlphaThresholdSlotId,
                SpeedTreeLitMasterNode.DepthOffsetSlotId,
            },
            VertexShaderSlots = new List<int>()
            {
                SpeedTreeLitMasterNode.PositionSlotId,
                SpeedTreeLitMasterNode.VertexNormalSlotID,
                SpeedTreeLitMasterNode.VertexTangentSlotID
            },
            UseInPreview = false,

            OnGeneratePassImpl = (IMasterNode node, ref Pass pass) =>
            {
                var masterNode = node as SpeedTreeLitMasterNode;
                HDSubShaderUtilities.SetStencilStateForMotionVector(ref pass);
            }
        };

        Pass m_PassDistortion = new Pass()
        {
            Name = "DistortionVectors",
            LightMode = "DistortionVectors",
            TemplateName = "SpeedTreeLitPass.template",
            MaterialName = "Lit",
            ShaderPassName = "SHADERPASS_DISTORTION",
            ZWriteOverride = HDSubShaderUtilities.zWriteOff,
            ExtraDefines = new List<string>()
            {
                DefineRaytracingKeyword(RayTracingNode.RaytracingVariant.High),
            },
            Includes = new List<string>()
            {
                "#include \"Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassDistortion.hlsl\"",
            },
            StencilOverride = new List<string>()
            {
                "// Stencil setup",
                "Stencil",
                "{",
                string.Format("   WriteMask {0}", (int)HDRenderPipeline.StencilBitMask.DistortionVectors),
                string.Format("   Ref  {0}", (int)HDRenderPipeline.StencilBitMask.DistortionVectors),
                "   Comp Always",
                "   Pass Replace",
                "}"
            },
            PixelShaderSlots = new List<int>()
            {
                SpeedTreeLitMasterNode.AlphaSlotId,
                SpeedTreeLitMasterNode.AlphaThresholdSlotId,
                SpeedTreeLitMasterNode.DistortionSlotId,
                SpeedTreeLitMasterNode.DistortionBlurSlotId,
            },
            VertexShaderSlots = new List<int>()
            {
                SpeedTreeLitMasterNode.PositionSlotId,
                SpeedTreeLitMasterNode.VertexNormalSlotID,
                SpeedTreeLitMasterNode.VertexTangentSlotID
            },
            UseInPreview = true,

            OnGeneratePassImpl = (IMasterNode node, ref Pass pass) =>
            {
                HDSubShaderUtilities.SetStencilStateForDistortionVector(ref pass);
                var masterNode = node as SpeedTreeLitMasterNode;
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
            TemplateName = "SpeedTreeLitPass.template",
            MaterialName = "Lit",
            ShaderPassName = "SHADERPASS_DEPTH_ONLY",
            BlendOverride = "Blend One Zero",
            ZWriteOverride = "ZWrite On",
            ColorMaskOverride = "ColorMask 0",
            CullOverride = HDSubShaderUtilities.defaultCullMode,
            ExtraDefines = new List<string>()
            {
                "#define CUTOFF_TRANSPARENT_DEPTH_PREPASS",
                DefineRaytracingKeyword(RayTracingNode.RaytracingVariant.High),
            },
            Includes = new List<string>()
            {
                "#include \"Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassDepthOnly.hlsl\"",
            },
            PixelShaderSlots = new List<int>()
            {
                SpeedTreeLitMasterNode.AlphaSlotId,
                SpeedTreeLitMasterNode.AlphaThresholdDepthPrepassSlotId,
                SpeedTreeLitMasterNode.DepthOffsetSlotId,
            },
            VertexShaderSlots = new List<int>()
            {
                SpeedTreeLitMasterNode.PositionSlotId,
                SpeedTreeLitMasterNode.VertexNormalSlotID,
                SpeedTreeLitMasterNode.VertexTangentSlotID
            },
            UseInPreview = true
        };

        Pass m_PassTransparentBackface = new Pass()
        {
            Name = "TransparentBackface",
            LightMode = "TransparentBackface",
            TemplateName = "SpeedTreeLitPass.template",
            MaterialName = "Lit",
            ShaderPassName = "SHADERPASS_FORWARD",
            CullOverride = "Cull Front",
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
                SpeedTreeLitMasterNode.AlbedoSlotId,
                SpeedTreeLitMasterNode.NormalSlotId,
                SpeedTreeLitMasterNode.BentNormalSlotId,
                SpeedTreeLitMasterNode.TangentSlotId,
                SpeedTreeLitMasterNode.SubsurfaceMaskSlotId,
                SpeedTreeLitMasterNode.ThicknessSlotId,
                SpeedTreeLitMasterNode.DiffusionProfileHashSlotId,
                SpeedTreeLitMasterNode.IridescenceMaskSlotId,
                SpeedTreeLitMasterNode.IridescenceThicknessSlotId,
                SpeedTreeLitMasterNode.SpecularColorSlotId,
                SpeedTreeLitMasterNode.CoatMaskSlotId,
                SpeedTreeLitMasterNode.MetallicSlotId,
                SpeedTreeLitMasterNode.EmissionSlotId,
                SpeedTreeLitMasterNode.SmoothnessSlotId,
                SpeedTreeLitMasterNode.AmbientOcclusionSlotId,
                SpeedTreeLitMasterNode.SpecularOcclusionSlotId,
                SpeedTreeLitMasterNode.AlphaSlotId,
                SpeedTreeLitMasterNode.AlphaThresholdSlotId,
                SpeedTreeLitMasterNode.AnisotropySlotId,
                SpeedTreeLitMasterNode.SpecularAAScreenSpaceVarianceSlotId,
                SpeedTreeLitMasterNode.SpecularAAThresholdSlotId,
                SpeedTreeLitMasterNode.RefractionIndexSlotId,
                SpeedTreeLitMasterNode.RefractionColorSlotId,
                SpeedTreeLitMasterNode.RefractionDistanceSlotId,
                SpeedTreeLitMasterNode.DepthOffsetSlotId,
            },
            VertexShaderSlots = new List<int>()
            {
                SpeedTreeLitMasterNode.PositionSlotId,
                SpeedTreeLitMasterNode.VertexNormalSlotID,
                SpeedTreeLitMasterNode.VertexTangentSlotID
            },
            UseInPreview = true,
            OnGeneratePassImpl = (IMasterNode node, ref Pass pass) =>
            {
                var masterNode = node as SpeedTreeLitMasterNode;
                HDSubShaderUtilities.SetBlendModeForTransparentBackface(ref pass);
            }
        };

        Pass m_PassForward = new Pass()
        {
            Name = "Forward",
            LightMode = "Forward",
            TemplateName = "SpeedTreeLitPass.template",
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
            ExtraDefines = new List<string>()
            {
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
                SpeedTreeLitMasterNode.AlbedoSlotId,
                SpeedTreeLitMasterNode.NormalSlotId,
                SpeedTreeLitMasterNode.BentNormalSlotId,
                SpeedTreeLitMasterNode.TangentSlotId,
                SpeedTreeLitMasterNode.SubsurfaceMaskSlotId,
                SpeedTreeLitMasterNode.ThicknessSlotId,
                SpeedTreeLitMasterNode.DiffusionProfileHashSlotId,
                SpeedTreeLitMasterNode.IridescenceMaskSlotId,
                SpeedTreeLitMasterNode.IridescenceThicknessSlotId,
                SpeedTreeLitMasterNode.SpecularColorSlotId,
                SpeedTreeLitMasterNode.CoatMaskSlotId,
                SpeedTreeLitMasterNode.MetallicSlotId,
                SpeedTreeLitMasterNode.EmissionSlotId,
                SpeedTreeLitMasterNode.SmoothnessSlotId,
                SpeedTreeLitMasterNode.AmbientOcclusionSlotId,
                SpeedTreeLitMasterNode.SpecularOcclusionSlotId,
                SpeedTreeLitMasterNode.AlphaSlotId,
                SpeedTreeLitMasterNode.AlphaThresholdSlotId,
                SpeedTreeLitMasterNode.AnisotropySlotId,
                SpeedTreeLitMasterNode.SpecularAAScreenSpaceVarianceSlotId,
                SpeedTreeLitMasterNode.SpecularAAThresholdSlotId,
                SpeedTreeLitMasterNode.RefractionIndexSlotId,
                SpeedTreeLitMasterNode.RefractionColorSlotId,
                SpeedTreeLitMasterNode.RefractionDistanceSlotId,
                SpeedTreeLitMasterNode.LightingSlotId,
                SpeedTreeLitMasterNode.BackLightingSlotId,
                SpeedTreeLitMasterNode.DepthOffsetSlotId,
            },
            VertexShaderSlots = new List<int>()
            {
                SpeedTreeLitMasterNode.PositionSlotId,
                SpeedTreeLitMasterNode.VertexNormalSlotID,
                SpeedTreeLitMasterNode.VertexTangentSlotID
            },
            UseInPreview = true,

            OnGeneratePassImpl = (IMasterNode node, ref Pass pass) =>
            {
                var masterNode = node as SpeedTreeLitMasterNode;
                HDSubShaderUtilities.SetStencilStateForForward(ref pass);
                HDSubShaderUtilities.SetBlendModeForForward(ref pass);

                pass.ExtraDefines.Remove("#ifndef DEBUG_DISPLAY\n#define SHADERPASS_FORWARD_BYPASS_ALPHA_TEST\n#endif");

                pass.ColorMaskOverride = "ColorMask [_ColorMaskTransparentVel] 1";
                if (masterNode.surfaceType == SurfaceType.Opaque)
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
            TemplateName = "SpeedTreeLitPass.template",
            MaterialName = "Lit",
            ShaderPassName = "SHADERPASS_DEPTH_ONLY",
            BlendOverride = "Blend One Zero",
            ZWriteOverride = "ZWrite On",
            ColorMaskOverride = "ColorMask 0",
            CullOverride = HDSubShaderUtilities.defaultCullMode,
            ExtraDefines = new List<string>()
            {
                "#define CUTOFF_TRANSPARENT_DEPTH_POSTPASS",
                DefineRaytracingKeyword(RayTracingNode.RaytracingVariant.High),
            },
            Includes = new List<string>()
            {
                "#include \"Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassDepthOnly.hlsl\"",
            },
            PixelShaderSlots = new List<int>()
            {
                SpeedTreeLitMasterNode.AlphaSlotId,
                SpeedTreeLitMasterNode.AlphaThresholdDepthPostpassSlotId,
                SpeedTreeLitMasterNode.DepthOffsetSlotId,
            },
            VertexShaderSlots = new List<int>()
            {
                SpeedTreeLitMasterNode.PositionSlotId,
                SpeedTreeLitMasterNode.VertexNormalSlotID,
                SpeedTreeLitMasterNode.VertexTangentSlotID
            },
            UseInPreview = true
        };

        Pass m_PassRaytracingIndirect = new Pass()
        {
            Name = "IndirectDXR",
            LightMode = "IndirectDXR",
            TemplateName = "SpeedTreeLitRaytracingPass.template",
            MaterialName = "Lit",
            ShaderPassName = "SHADERPASS_RAYTRACING_INDIRECT",
            ExtraDefines = new List<string>()
            {
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
                SpeedTreeLitMasterNode.AlbedoSlotId,
                SpeedTreeLitMasterNode.NormalSlotId,
                SpeedTreeLitMasterNode.BentNormalSlotId,
                SpeedTreeLitMasterNode.TangentSlotId,
                SpeedTreeLitMasterNode.SubsurfaceMaskSlotId,
                SpeedTreeLitMasterNode.ThicknessSlotId,
                SpeedTreeLitMasterNode.DiffusionProfileHashSlotId,
                SpeedTreeLitMasterNode.IridescenceMaskSlotId,
                SpeedTreeLitMasterNode.IridescenceThicknessSlotId,
                SpeedTreeLitMasterNode.SpecularColorSlotId,
                SpeedTreeLitMasterNode.CoatMaskSlotId,
                SpeedTreeLitMasterNode.MetallicSlotId,
                SpeedTreeLitMasterNode.EmissionSlotId,
                SpeedTreeLitMasterNode.SmoothnessSlotId,
                SpeedTreeLitMasterNode.AmbientOcclusionSlotId,
                SpeedTreeLitMasterNode.SpecularOcclusionSlotId,
                SpeedTreeLitMasterNode.AlphaSlotId,
                SpeedTreeLitMasterNode.AlphaThresholdSlotId,
                SpeedTreeLitMasterNode.AnisotropySlotId,
                SpeedTreeLitMasterNode.SpecularAAScreenSpaceVarianceSlotId,
                SpeedTreeLitMasterNode.SpecularAAThresholdSlotId,
                SpeedTreeLitMasterNode.RefractionIndexSlotId,
                SpeedTreeLitMasterNode.RefractionColorSlotId,
                SpeedTreeLitMasterNode.RefractionDistanceSlotId,
            },
            VertexShaderSlots = new List<int>()
            {
                SpeedTreeLitMasterNode.PositionSlotId,
                SpeedTreeLitMasterNode.VertexNormalSlotID,
                SpeedTreeLitMasterNode.VertexTangentSlotID
            },
            UseInPreview = false
        };

        Pass m_PassRaytracingVisibility = new Pass()
        {
            Name = "VisibilityDXR",
            LightMode = "VisibilityDXR",
            TemplateName = "SpeedTreeLitRaytracingPass.template",
            MaterialName = "Lit",
            ShaderPassName = "SHADERPASS_RAYTRACING_VISIBILITY",
            ExtraDefines = new List<string>()
            {
                DefineRaytracingKeyword(RayTracingNode.RaytracingVariant.Low)
            },
            Includes = new List<string>()
            {
                "#include \"Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassRaytracingVisibility.hlsl\"",
            },
            PixelShaderSlots = new List<int>()
            {
                SpeedTreeLitMasterNode.AlbedoSlotId,
                SpeedTreeLitMasterNode.NormalSlotId,
                SpeedTreeLitMasterNode.BentNormalSlotId,
                SpeedTreeLitMasterNode.TangentSlotId,
                SpeedTreeLitMasterNode.SubsurfaceMaskSlotId,
                SpeedTreeLitMasterNode.ThicknessSlotId,
                SpeedTreeLitMasterNode.DiffusionProfileHashSlotId,
                SpeedTreeLitMasterNode.IridescenceMaskSlotId,
                SpeedTreeLitMasterNode.IridescenceThicknessSlotId,
                SpeedTreeLitMasterNode.SpecularColorSlotId,
                SpeedTreeLitMasterNode.CoatMaskSlotId,
                SpeedTreeLitMasterNode.MetallicSlotId,
                SpeedTreeLitMasterNode.EmissionSlotId,
                SpeedTreeLitMasterNode.SmoothnessSlotId,
                SpeedTreeLitMasterNode.AmbientOcclusionSlotId,
                SpeedTreeLitMasterNode.SpecularOcclusionSlotId,
                SpeedTreeLitMasterNode.AlphaSlotId,
                SpeedTreeLitMasterNode.AlphaThresholdSlotId,
                SpeedTreeLitMasterNode.AnisotropySlotId,
                SpeedTreeLitMasterNode.SpecularAAScreenSpaceVarianceSlotId,
                SpeedTreeLitMasterNode.SpecularAAThresholdSlotId,
                SpeedTreeLitMasterNode.RefractionIndexSlotId,
                SpeedTreeLitMasterNode.RefractionColorSlotId,
                SpeedTreeLitMasterNode.RefractionDistanceSlotId,
            },
            VertexShaderSlots = new List<int>()
            {
                SpeedTreeLitMasterNode.PositionSlotId,
                SpeedTreeLitMasterNode.VertexNormalSlotID,
                SpeedTreeLitMasterNode.VertexTangentSlotID
            },
            UseInPreview = false
        };

        Pass m_PassRaytracingForward = new Pass()
        {
            Name = "ForwardDXR",
            LightMode = "ForwardDXR",
            TemplateName = "SpeedTreeLitRaytracingPass.template",
            MaterialName = "Lit",
            ShaderPassName = "SHADERPASS_RAYTRACING_FORWARD",
            ExtraDefines = new List<string>()
            {
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
                SpeedTreeLitMasterNode.AlbedoSlotId,
                SpeedTreeLitMasterNode.NormalSlotId,
                SpeedTreeLitMasterNode.BentNormalSlotId,
                SpeedTreeLitMasterNode.TangentSlotId,
                SpeedTreeLitMasterNode.SubsurfaceMaskSlotId,
                SpeedTreeLitMasterNode.ThicknessSlotId,
                SpeedTreeLitMasterNode.DiffusionProfileHashSlotId,
                SpeedTreeLitMasterNode.IridescenceMaskSlotId,
                SpeedTreeLitMasterNode.IridescenceThicknessSlotId,
                SpeedTreeLitMasterNode.SpecularColorSlotId,
                SpeedTreeLitMasterNode.CoatMaskSlotId,
                SpeedTreeLitMasterNode.MetallicSlotId,
                SpeedTreeLitMasterNode.EmissionSlotId,
                SpeedTreeLitMasterNode.SmoothnessSlotId,
                SpeedTreeLitMasterNode.AmbientOcclusionSlotId,
                SpeedTreeLitMasterNode.SpecularOcclusionSlotId,
                SpeedTreeLitMasterNode.AlphaSlotId,
                SpeedTreeLitMasterNode.AlphaThresholdSlotId,
                SpeedTreeLitMasterNode.AnisotropySlotId,
                SpeedTreeLitMasterNode.SpecularAAScreenSpaceVarianceSlotId,
                SpeedTreeLitMasterNode.SpecularAAThresholdSlotId,
                SpeedTreeLitMasterNode.RefractionIndexSlotId,
                SpeedTreeLitMasterNode.RefractionColorSlotId,
                SpeedTreeLitMasterNode.RefractionDistanceSlotId,
            },
            VertexShaderSlots = new List<int>()
            {
                SpeedTreeLitMasterNode.PositionSlotId,
                SpeedTreeLitMasterNode.VertexNormalSlotID,
                SpeedTreeLitMasterNode.VertexTangentSlotID
            },
            UseInPreview = false
        };

        Pass m_PassRaytracingGBuffer = new Pass()
        {
            Name = "GBufferDXR",
            LightMode = "GBufferDXR",
            TemplateName = "SpeedTreeLitRaytracingPass.template",
            MaterialName = "Lit",
            ShaderPassName = "SHADERPASS_RAYTRACING_GBUFFER",
            ExtraDefines = new List<string>()
            {
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
                SpeedTreeLitMasterNode.AlbedoSlotId,
                SpeedTreeLitMasterNode.NormalSlotId,
                SpeedTreeLitMasterNode.BentNormalSlotId,
                SpeedTreeLitMasterNode.TangentSlotId,
                SpeedTreeLitMasterNode.SubsurfaceMaskSlotId,
                SpeedTreeLitMasterNode.ThicknessSlotId,
                SpeedTreeLitMasterNode.DiffusionProfileHashSlotId,
                SpeedTreeLitMasterNode.IridescenceMaskSlotId,
                SpeedTreeLitMasterNode.IridescenceThicknessSlotId,
                SpeedTreeLitMasterNode.SpecularColorSlotId,
                SpeedTreeLitMasterNode.CoatMaskSlotId,
                SpeedTreeLitMasterNode.MetallicSlotId,
                SpeedTreeLitMasterNode.EmissionSlotId,
                SpeedTreeLitMasterNode.SmoothnessSlotId,
                SpeedTreeLitMasterNode.AmbientOcclusionSlotId,
                SpeedTreeLitMasterNode.SpecularOcclusionSlotId,
                SpeedTreeLitMasterNode.AlphaSlotId,
                SpeedTreeLitMasterNode.AlphaThresholdSlotId,
                SpeedTreeLitMasterNode.AnisotropySlotId,
                SpeedTreeLitMasterNode.SpecularAAScreenSpaceVarianceSlotId,
                SpeedTreeLitMasterNode.SpecularAAThresholdSlotId,
                SpeedTreeLitMasterNode.RefractionIndexSlotId,
                SpeedTreeLitMasterNode.RefractionColorSlotId,
                SpeedTreeLitMasterNode.RefractionDistanceSlotId,
            },
            VertexShaderSlots = new List<int>()
            {
                SpeedTreeLitMasterNode.PositionSlotId,
                SpeedTreeLitMasterNode.VertexNormalSlotID,
                SpeedTreeLitMasterNode.VertexTangentSlotID
            },
            UseInPreview = false
        };
        public int GetPreviewPassIndex() { return 0; }

        private static List<string> GetInstancingOptionsFromMasterNode(AbstractMaterialNode iMasterNode)
        {
            List<string> instancingOption = new List<string>();

            SpeedTreeLitMasterNode masterNode = iMasterNode as SpeedTreeLitMasterNode;

            if (masterNode.dotsInstancing.isOn)
            {
                instancingOption.Add("#pragma instancing_options nolightprobe");
                instancingOption.Add("#pragma instancing_options nolodfade");
            }
            else
            {
                instancingOption.Add("#pragma instancing_options renderinglayer");
            }

            return instancingOption;
        }


        private static ActiveFields GetActiveFieldsFromMasterNode(AbstractMaterialNode iMasterNode, Pass pass)
        {
            var activeFields = new ActiveFields();
            var baseActiveFields = activeFields.baseInstance;

            SpeedTreeLitMasterNode masterNode = iMasterNode as SpeedTreeLitMasterNode;
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

            switch (masterNode.materialType)
            {
                case SpeedTreeLitMasterNode.MaterialType.Anisotropy:
                    baseActiveFields.AddAll("Material.Anisotropy");
                    break;
                case SpeedTreeLitMasterNode.MaterialType.Iridescence:
                    baseActiveFields.AddAll("Material.Iridescence");
                    break;
                case SpeedTreeLitMasterNode.MaterialType.SpecularColor:
                    baseActiveFields.AddAll("Material.SpecularColor");
                    break;
                case SpeedTreeLitMasterNode.MaterialType.Standard:
                    baseActiveFields.AddAll("Material.Standard");
                    break;
                case SpeedTreeLitMasterNode.MaterialType.SubsurfaceScattering:
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
                case SpeedTreeLitMasterNode.MaterialType.Translucent:
                    {
                        baseActiveFields.AddAll("Material.Translucent");
                        baseActiveFields.AddAll("Material.Transmission");
                    }
                    break;
                default:
                    UnityEngine.Debug.LogError("Unknown material type: " + masterNode.materialType);
                    break;
            }


            int count = 0;
            // If alpha test shadow is enable, we use it, otherwise we use the regular test
            if (pass.PixelShaderUsesSlot(SpeedTreeLitMasterNode.AlphaThresholdShadowSlotId) && masterNode.alphaTestShadow.isOn)
            {
                baseActiveFields.AddAll("AlphaTestShadow");
                ++count;
            }
            else if (pass.PixelShaderUsesSlot(SpeedTreeLitMasterNode.AlphaThresholdSlotId))
            {
                baseActiveFields.AddAll("AlphaTest");
                ++count;
            }

            if (pass.PixelShaderUsesSlot(SpeedTreeLitMasterNode.AlphaThresholdDepthPrepassSlotId))
            {
                baseActiveFields.AddAll("AlphaTestPrepass");
                ++count;
            }
            if (pass.PixelShaderUsesSlot(SpeedTreeLitMasterNode.AlphaThresholdDepthPostpassSlotId))
            {
                baseActiveFields.AddAll("AlphaTestPostpass");
                ++count;
            }
            UnityEngine.Debug.Assert(count == 1, "Alpha test value not set correctly");


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

            if (masterNode.specularAA.isOn && pass.PixelShaderUsesSlot(SpeedTreeLitMasterNode.SpecularAAThresholdSlotId) && pass.PixelShaderUsesSlot(SpeedTreeLitMasterNode.SpecularAAScreenSpaceVarianceSlotId))
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

            if (masterNode.IsSlotConnected(SpeedTreeLitMasterNode.BentNormalSlotId) && pass.PixelShaderUsesSlot(SpeedTreeLitMasterNode.BentNormalSlotId))
            {
                baseActiveFields.AddAll("BentNormal");
            }

            if (masterNode.IsSlotConnected(SpeedTreeLitMasterNode.TangentSlotId) && pass.PixelShaderUsesSlot(SpeedTreeLitMasterNode.TangentSlotId))
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

            if (pass.PixelShaderUsesSlot(SpeedTreeLitMasterNode.AmbientOcclusionSlotId))
            {
                var occlusionSlot = masterNode.FindSlot<Vector1MaterialSlot>(SpeedTreeLitMasterNode.AmbientOcclusionSlotId);

                bool connected = masterNode.IsSlotConnected(SpeedTreeLitMasterNode.AmbientOcclusionSlotId);
                if (connected || occlusionSlot.value != occlusionSlot.defaultValue)
                {
                    baseActiveFields.AddAll("AmbientOcclusion");
                }
            }

            if (pass.PixelShaderUsesSlot(SpeedTreeLitMasterNode.CoatMaskSlotId))
            {
                var coatMaskSlot = masterNode.FindSlot<Vector1MaterialSlot>(SpeedTreeLitMasterNode.CoatMaskSlotId);

                bool connected = masterNode.IsSlotConnected(SpeedTreeLitMasterNode.CoatMaskSlotId);
                if (connected || coatMaskSlot.value > 0.0f)
                {
                    baseActiveFields.AddAll("CoatMask");
                }
            }

            if (masterNode.IsSlotConnected(SpeedTreeLitMasterNode.LightingSlotId) && pass.PixelShaderUsesSlot(SpeedTreeLitMasterNode.LightingSlotId))
            {
                baseActiveFields.AddAll("LightingGI");
            }
            if (masterNode.IsSlotConnected(SpeedTreeLitMasterNode.BackLightingSlotId) && pass.PixelShaderUsesSlot(SpeedTreeLitMasterNode.LightingSlotId))
            {
                baseActiveFields.AddAll("BackLightingGI");
            }

            if (masterNode.depthOffset.isOn && pass.PixelShaderUsesSlot(SpeedTreeLitMasterNode.DepthOffsetSlotId))
                baseActiveFields.AddAll("DepthOffset");

            if (masterNode.supportLodCrossFade.isOn)
                baseActiveFields.AddAll("LodCrossFade");

            // Speedtree asset version
            if (masterNode.speedTreeVersion == SpeedTreeLitMasterNode.SpeedTreeVersion.SpeedTree7)
            {
                baseActiveFields.AddAll("SpeedtreeVersion7");
            }
            else if (masterNode.speedTreeVersion == SpeedTreeLitMasterNode.SpeedTreeVersion.SpeedTree8)
            {
                baseActiveFields.AddAll("SpeedtreeVersion8");
            }

            return activeFields;
        }

        private static bool GenerateShaderPassLit(SpeedTreeLitMasterNode masterNode, Pass pass, GenerationMode mode, ShaderGenerator result, List<string> sourceAssetDependencyPaths)
        {
            if (mode == GenerationMode.ForReals || pass.UseInPreview)
            {
                pass.OnGeneratePass(masterNode);

                // Make sure Speedtree-specific stuff is added 
                masterNode.AddSpeedTreeGeometryDefines(ref pass.ExtraDefines);

                // apply master node options to active fields
                var activeFields = GetActiveFieldsFromMasterNode(masterNode, pass);

                pass.ExtraInstancingOptions = GetInstancingOptionsFromMasterNode(masterNode);

                // use standard shader pass generation
                bool vertexActive = false;
                if (masterNode.IsSlotConnected(SpeedTreeLitMasterNode.PositionSlotId) ||
                    masterNode.IsSlotConnected(SpeedTreeLitMasterNode.VertexNormalSlotID) ||
                    masterNode.IsSlotConnected(SpeedTreeLitMasterNode.VertexTangentSlotID) )
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

        public string GetSubshader(IMasterNode iMasterNode, GenerationMode mode, List<string> sourceAssetDependencyPaths = null)
        {
            if (sourceAssetDependencyPaths != null)
            {
                // SpeedTreeLitSubShader.cs
                sourceAssetDependencyPaths.Add(AssetDatabase.GUIDToAssetPath("1157bab7d33bbe44eb86b4326a02cf73"));
                // HDSubShaderUtilities.cs
                sourceAssetDependencyPaths.Add(AssetDatabase.GUIDToAssetPath("713ced4e6eef4a44799a4dd59041484b"));
            }

            var masterNode = iMasterNode as SpeedTreeLitMasterNode;

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
                int queue = HDRenderQueue.ChangeType(masterNode.renderingPass, masterNode.sortPriority, true);
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
                    GenerateShaderPassLit(masterNode, m_PassRaytracingIndirect, mode, subShader, sourceAssetDependencyPaths);
                    GenerateShaderPassLit(masterNode, m_PassRaytracingVisibility, mode, subShader, sourceAssetDependencyPaths);
                    GenerateShaderPassLit(masterNode, m_PassRaytracingForward, mode, subShader, sourceAssetDependencyPaths);
                    GenerateShaderPassLit(masterNode, m_PassRaytracingGBuffer, mode, subShader, sourceAssetDependencyPaths);
                }
                subShader.Deindent();
                subShader.AddShaderChunk("}", false);
            }
            
            subShader.AddShaderChunk(@"CustomEditor ""UnityEditor.Rendering.HighDefinition.SpeedTreeLitGUI""");

            return subShader.GetShaderString(0);
        }

        public bool IsPipelineCompatible(RenderPipelineAsset renderPipelineAsset)
        {
            return renderPipelineAsset is HDRenderPipelineAsset;
        }
    }
}
