using System.Collections.Generic;
using System.Linq;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Rendering;
using Data.Util;

namespace UnityEditor.Rendering.HighDefinition
{
    class SpeedTreeLitSubShader : ISpeedTreeLitSubShader
    {
        internal static string DefineRaytracingKeyword(RayTracingNode.RaytracingVariant variant)
            => $"#define {RayTracingNode.RaytracingVariantKeyword(variant)}";

        Pass m_PassGBuffer = new Pass()
        {
            Name = "GBuffer",
            LightMode = "GBuffer",
            TemplateName = "SpeedTreeLitPass.template",
            MaterialName = "SpeedTreeLit",
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
                "#pragma multi_compile _ LOD_FADE_CROSSFADE",
                "#define SPEEDTREE_Y_UP",
                DefineRaytracingKeyword(RayTracingNode.RaytracingVariant.High),
                // When we have alpha test, we will force a depth prepass so we always bypass the clip instruction in the GBuffer
                // Don't do it with debug display mode as it is possible there is no depth prepass in this case
                // This remove is required otherwise the code generate several time the define...
                "#ifndef DEBUG_DISPLAY\n#define SHADERPASS_GBUFFER_BYPASS_ALPHA_TEST\n#endif"
            },
            Includes = new List<string>()
            {
                "#include \"Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassGBuffer.hlsl\"",
            },
            RequiredFields = new List<string>()
            {
                "FragInputs.tangentToWorld",
                "FragInputs.positionRWS",
                "FragInputs.texCoord0",
                "FragInputs.texCoord1",
                "FragInputs.texCoord2",
                "FragInputs.texCoord3",
                "FragInputs.color"
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
                SpeedTreeLitMasterNode.VertexTangentSlotID,
            },
            UseInPreview = true,

            OnGeneratePassImpl = (IMasterNode node, ref Pass pass) =>
            {
                var masterNode = node as SpeedTreeLitMasterNode;
                HDSubShaderUtilities.SetStencilStateForGBuffer(ref pass);

                masterNode.AddBasicGeometryDefines(ref pass.ExtraDefines);
            }
        };

        Pass m_PassMETA = new Pass()
        {
            Name = "META",
            LightMode = "META",
            TemplateName = "SpeedTreeLitPass.template",
            MaterialName = "SpeedTreeLit",
            ShaderPassName = "SHADERPASS_LIGHT_TRANSPORT",
            CullOverride = "Cull Off",
            Includes = new List<string>()
            {
                "#include \"Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassLightTransport.hlsl\"",
            },
            ExtraDefines = new List<string>()
            {
                DefineRaytracingKeyword(RayTracingNode.RaytracingVariant.High),
                "#define SPEEDTREE_Y_UP",
            },
            RequiredFields = new List<string>()
            {
                "AttributesMesh.normalOS",
                "AttributesMesh.tangentOS",     // Always present as we require it also in case of Variants lighting
                "AttributesMesh.uv0",
                "AttributesMesh.uv1",
                "AttributesMesh.color",
                "AttributesMesh.uv2",           // SHADERPASS_LIGHT_TRANSPORT always uses uv2
                "AttributesMesh.uv3",           // Wind animation can potentially use uv3
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
            },
            UseInPreview = false,

            OnGeneratePassImpl = (IMasterNode node, ref Pass pass) =>
            {
                var masterNode = node as SpeedTreeLitMasterNode;

                masterNode.AddBasicGeometryDefines(ref pass.ExtraDefines);
            }
        };

        Pass m_PassShadowCaster = new Pass()
        {
            Name = "ShadowCaster",
            LightMode = "ShadowCaster",
            TemplateName = "SpeedTreeLitPass.template",
            MaterialName = "SpeedTreeLit",
            ShaderPassName = "SHADERPASS_SHADOWS",
            ColorMaskOverride = "ColorMask 0",
            ZClipOverride = HDSubShaderUtilities.zClipShadowCaster,
            CullOverride = HDSubShaderUtilities.defaultCullMode,
            ZWriteOverride = HDSubShaderUtilities.zWriteOn,
            Includes = new List<string>()
            {
                "#include \"Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassDepthOnly.hlsl\"",
            },
            ExtraDefines = new List<string>()
            {
                DefineRaytracingKeyword(RayTracingNode.RaytracingVariant.High),
                "#pragma multi_compile_vertex LOD_FADE_PERCENTAGE",
                "#define SPEEDTREE_Y_UP",
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
            UseInPreview = false,

            OnGeneratePassImpl = (IMasterNode node, ref Pass pass) =>
            {
                var masterNode = node as SpeedTreeLitMasterNode;

                masterNode.AddBasicGeometryDefines(ref pass.ExtraDefines);
            }
        };

        Pass m_SceneSelectionPass = new Pass()
        {
            Name = "SceneSelectionPass",
            LightMode = "SceneSelectionPass",
            TemplateName = "SpeedTreeLitPass.template",
            MaterialName = "SpeedTreeLit",
            ShaderPassName = "SHADERPASS_DEPTH_ONLY",
            ColorMaskOverride = "ColorMask 0",
            ExtraDefines = new List<string>()
            {
                "#define SCENESELECTIONPASS",
                "#pragma editor_sync_compilation",
                "#pragma multi_compile_vertex LOD_FADE_PERCENTAGE",
                "#define SPEEDTREE_Y_UP",
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
            UseInPreview = false,

            OnGeneratePassImpl = (IMasterNode node, ref Pass pass) =>
            {
                var masterNode = node as SpeedTreeLitMasterNode;

                masterNode.AddBasicGeometryDefines(ref pass.ExtraDefines);
            }
        };

        Pass m_PassDepthOnly = new Pass()
        {
            Name = "DepthOnly",
            LightMode = "DepthOnly",
            TemplateName = "SpeedTreeLitPass.template",
            MaterialName = "SpeedTreeLit",
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

                pass.ExtraDefines.Add("#define SPEEDTREE_Y_UP");
                masterNode.AddBasicGeometryDefines(ref pass.ExtraDefines);
            }
        };

        Pass m_PassMotionVectors = new Pass()
        {
            Name = "MotionVectors",
            LightMode = "MotionVectors",
            TemplateName = "SpeedTreeLitPass.template",
            MaterialName = "SpeedTreeLit",
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

                pass.ExtraDefines.Add("#define SPEEDTREE_Y_UP");

                masterNode.AddBasicGeometryDefines(ref pass.ExtraDefines);
            }
        };

        Pass m_PassDistortion = new Pass()
        {
            Name = "DistortionVectors",
            LightMode = "DistortionVectors",
            TemplateName = "SpeedTreeLitPass.template",
            MaterialName = "SpeedTreeLit",
            ShaderPassName = "SHADERPASS_DISTORTION",
            ZWriteOverride = HDSubShaderUtilities.zWriteOff,
            Includes = new List<string>()
            {
                "#include \"Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassDistortion.hlsl\"",
            },
            ExtraDefines = new List<string>()
            {
                "#pragma multi_compile_vertex LOD_FADE_PERCENTAGE",
                "#define SPEEDTREE_Y_UP",
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

                masterNode.AddBasicGeometryDefines(ref pass.ExtraDefines);
            }
        };

        Pass m_PassTransparentDepthPrepass = new Pass()
        {
            Name = "TransparentDepthPrepass",
            LightMode = "TransparentDepthPrepass",
            TemplateName = "SpeedTreeLitPass.template",
            MaterialName = "SpeedTreeLit",
            ShaderPassName = "SHADERPASS_DEPTH_ONLY",
            BlendOverride = "Blend One Zero",
            ZWriteOverride = "ZWrite On",
            ColorMaskOverride = "ColorMask 0",
            CullOverride = HDSubShaderUtilities.defaultCullMode,
            ExtraDefines = new List<string>()
            {
                "#define CUTOFF_TRANSPARENT_DEPTH_PREPASS",
                "#define SPEEDTREE_Y_UP",
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
            UseInPreview = true,

            OnGeneratePassImpl = (IMasterNode node, ref Pass pass) =>
            {
                var masterNode = node as SpeedTreeLitMasterNode;

                masterNode.AddBasicGeometryDefines(ref pass.ExtraDefines);
            }
        };

        Pass m_PassTransparentBackface = new Pass()
        {
            Name = "TransparentBackface",
            LightMode = "TransparentBackface",
            TemplateName = "SpeedTreeLitPass.template",
            MaterialName = "SpeedTreeLit",
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
                "FragInputs.texCoord0",
                "FragInputs.texCoord1",
                "FragInputs.texCoord2",
                "FragInputs.texCoord3"
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
                masterNode.AddBasicGeometryDefines(ref pass.ExtraDefines);
            }
        };

        Pass m_PassForward = new Pass()
        {
            Name = "Forward",
            LightMode = "Forward",
            TemplateName = "SpeedTreeLitPass.template",
            MaterialName = "SpeedTreeLit",
            ShaderPassName = "SHADERPASS_FORWARD",
            CullOverride = HDSubShaderUtilities.cullModeForward,
            ZTestOverride = HDSubShaderUtilities.zTestDepthEqualForOpaque,
            ZWriteOverride = HDSubShaderUtilities.ZWriteDefault,
            // ExtraDefines are set when the pass is generated
            ExtraDefines = new List<string>()
            {
                "#ifndef DEBUG_DISPLAY\n#define SHADERPASS_FORWARD_BYPASS_ALPHA_TEST\n#endif",
                "#define SPEEDTREE_Y_UP",
                DefineRaytracingKeyword(RayTracingNode.RaytracingVariant.High),
            },
            Includes = new List<string>()
            {
                "#include \"Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassForward.hlsl\"",
            },
            RequiredFields = new List<string>()
            {
                "FragInputs.tangentToWorld",
                "FragInputs.positionRWS",
                "FragInputs.texCoord0",
                "FragInputs.texCoord1",
                "FragInputs.texCoord2",
                "FragInputs.texCoord3",
                "FragInputs.color"
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

                masterNode.AddBasicGeometryDefines(ref pass.ExtraDefines);
                if (masterNode.speedTreeAssetVersion == SpeedTreeLitMasterNode.SpeedTreeVersion.SpeedTree7)
                {
                    pass.ExtraDefines.Add("#if defined(GEOM_TYPE_LEAF) || defined(GEOM_TYPE_FROND)\n#define _SURFACE_TYPE_TRANSPARENT\n#endif");
                }
                pass.ColorMaskOverride = "ColorMask [_ColorMaskTransparentVel] 1";
                pass.ZTestOverride = "ZTest Equal";
            }
        };

        Pass m_PassTransparentDepthPostpass = new Pass()
        {
            Name = "TransparentDepthPostpass",
            LightMode = "TransparentDepthPostpass",
            TemplateName = "SpeedTreeLitPass.template",
            MaterialName = "SpeedTreeLit",
            ShaderPassName = "SHADERPASS_DEPTH_ONLY",
            BlendOverride = "Blend One Zero",
            ZWriteOverride = "ZWrite On",
            ColorMaskOverride = "ColorMask 0",
            CullOverride = HDSubShaderUtilities.defaultCullMode,
            ExtraDefines = new List<string>()
            {
                "#define CUTOFF_TRANSPARENT_DEPTH_POSTPASS",
                "#define SPEEDTREE_Y_UP",
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
            UseInPreview = true,

            OnGeneratePassImpl = (IMasterNode node, ref Pass pass) =>
            {
                var masterNode = node as SpeedTreeLitMasterNode;

                masterNode.AddBasicGeometryDefines(ref pass.ExtraDefines);
            }
        };

        Pass m_PassRaytracingIndirect = new Pass()
        {
            Name = "IndirectDXR",
            LightMode = "IndirectDXR",
            TemplateName = "SpeedTreeLitRaytracingPass.template",
            MaterialName = "SpeedTreeLit",
            ShaderPassName = "SHADERPASS_RAYTRACING_INDIRECT",
            ExtraDefines = new List<string>()
            {
                "#pragma multi_compile _ LIGHTMAP_ON",
                "#pragma multi_compile _ DIRLIGHTMAP_COMBINED",
                "#pragma multi_compile _ DYNAMICLIGHTMAP_ON",
                "#pragma multi_compile _ DIFFUSE_LIGHTING_ONLY",
                "#define SHADOW_LOW",
                "#define SPEEDTREE_Y_UP",
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
            MaterialName = "SpeedTreeLit",
            ShaderPassName = "SHADERPASS_RAYTRACING_VISIBILITY",
            Includes = new List<string>()
            {
                "#include \"Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassRaytracingVisibility.hlsl\"",
            },
            ExtraDefines = new List<string>()
            {
                "#define SPEEDTREE_Y_UP",
                DefineRaytracingKeyword(RayTracingNode.RaytracingVariant.Low)
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
            MaterialName = "SpeedTreeLit",
            ShaderPassName = "SHADERPASS_RAYTRACING_FORWARD",
            ExtraDefines = new List<string>()
            {
                "#pragma multi_compile _ LIGHTMAP_ON",
                "#pragma multi_compile _ DIRLIGHTMAP_COMBINED",
                "#pragma multi_compile _ DYNAMICLIGHTMAP_ON",
                "#define SHADOW_LOW",
                "#define SPEEDTREE_Y_UP",
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
            MaterialName = "SpeedTreeLit",
            ShaderPassName = "SHADERPASS_RAYTRACING_GBUFFER",
            ExtraDefines = new List<string>()
            {
                "#pragma multi_compile _ LIGHTMAP_ON",
                "#pragma multi_compile _ DIRLIGHTMAP_COMBINED",
                "#pragma multi_compile _ DYNAMICLIGHTMAP_ON",
                "#define SHADOW_LOW",
                "#define SPEEDTREE_Y_UP",
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

        public bool IsPipelineCompatible(RenderPipelineAsset renderPipelineAsset)
        {
            return renderPipelineAsset is HDRenderPipelineAsset;
        }

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
            {
                return activeFields;
            }

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

            // Alpha test is always on.
            int AlphaCount = 0;
            // If alpha test shadow is enable, we use it, otherwise we use the regular test
            if (pass.PixelShaderUsesSlot(SpeedTreeLitMasterNode.AlphaThresholdShadowSlotId) && masterNode.alphaTestShadow.isOn)
            {
                baseActiveFields.AddAll("AlphaTestShadow");
                ++AlphaCount;
            }
            else if (pass.PixelShaderUsesSlot(SpeedTreeLitMasterNode.AlphaThresholdSlotId))
            {
                baseActiveFields.AddAll("AlphaTest");
                ++AlphaCount;
            }

            if (pass.PixelShaderUsesSlot(SpeedTreeLitMasterNode.AlphaThresholdDepthPrepassSlotId))
            {
                baseActiveFields.AddAll("AlphaTestPrepass");
                ++AlphaCount;
            }
            if (pass.PixelShaderUsesSlot(SpeedTreeLitMasterNode.AlphaThresholdDepthPostpassSlotId))
            {
                baseActiveFields.AddAll("AlphaTestPostpass");
                ++AlphaCount;
            }
            UnityEngine.Debug.Assert(AlphaCount == 1, "Alpha test value not set correctly");


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

            // Speedtree-specific stuff
            if (masterNode.speedTreeAssetVersion == SpeedTreeLitMasterNode.SpeedTreeVersion.SpeedTree7)
            {
                baseActiveFields.AddAll("SpeedtreeVersion7");
            }
            else
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

                // apply master node options to active fields
                var activeFields = GetActiveFieldsFromMasterNode(masterNode, pass);

                pass.ExtraInstancingOptions = GetInstancingOptionsFromMasterNode(masterNode);

                // use standard shader pass generation
                bool vertexActive = false;
                if (masterNode.IsSlotConnected(HDLitMasterNode.PositionSlotId) ||
                    masterNode.IsSlotConnected(HDLitMasterNode.VertexNormalSlotID) || 
                    masterNode.IsSlotConnected(HDLitMasterNode.VertexTangentSlotID))
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
                sourceAssetDependencyPaths.Add(AssetDatabase.GUIDToAssetPath("2c5fbb8147a1e744eb355b2a2532f1a0"));
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

#if ENABLE_RAYTRACING
            if(mode == GenerationMode.ForReals)
            {
                subShader.AddShaderChunk("SubShader", false);
                subShader.AddShaderChunk("{", false);
                subShader.Indent();
                {
                    GenerateShaderPassLit(masterNode, m_PassRaytracingIndirect, mode, subShader, sourceAssetDependencyPaths);
                    GenerateShaderPassLit(masterNode, m_PassRaytracingVisibility, mode, subShader, sourceAssetDependencyPaths);
                    GenerateShaderPassLit(masterNode, m_PassRaytracingForward, mode, subShader, sourceAssetDependencyPaths);
                    GenerateShaderPassLit(masterNode, m_PassRaytracingGBuffer, mode, subShader, sourceAssetDependencyPaths);
                }
                subShader.Deindent();
                subShader.AddShaderChunk("}", false);
            }
#endif

            subShader.AddShaderChunk(@"CustomEditor ""UnityEditor.Rendering.HighDefinition.SpeedTreeLitGUI""");

            return subShader.GetShaderString(0);
        }
    }
}
