using System.Collections.Generic;
using System.Linq;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Rendering;

namespace UnityEditor.Rendering.HighDefinition
{
    class SpeedTreeLitSubShader : ISpeedTreeLitSubShader
    {
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
                "#define ENABLE_WIND",
                "#define EFFECT_BACKSIDE_NORMALS",
                "#define SPEEDTREE_Y_UP",
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
                SpeedTreeLitMasterNode.NormalSlotId,
                SpeedTreeLitMasterNode.TangentSlotId,
            },
            UseInPreview = true,

            OnGeneratePassImpl = (IMasterNode node, ref Pass pass) =>
            {
                var masterNode = node as SpeedTreeLitMasterNode;
                HDSubShaderUtilities.SetStencilStateForGBuffer(ref pass);

                // When we have alpha test, we will force a depth prepass so we always bypass the clip instruction in the GBuffer
                // Don't do it with debug display mode as it is possible there is no depth prepass in this case
                // This remove is required otherwise the code generate several time the define...
                pass.ExtraDefines.Remove("#ifndef DEBUG_DISPLAY\n#define SHADERPASS_GBUFFER_BYPASS_ALPHA_TEST\n#endif");

                if (masterNode.alphaTest.isOn)
                {
                    pass.ExtraDefines.Add("#ifndef DEBUG_DISPLAY\n#define SHADERPASS_GBUFFER_BYPASS_ALPHA_TEST\n#endif");
                }

                if (masterNode.billboard.isOn)
                {
                    pass.ExtraDefines.Add("#define SPEEDTREE_BILLBOARD");
                    pass.ExtraDefines.Add("#define EFFECT_BILLBOARD");
                }

                if (masterNode.speedTreeAssetVersion == SpeedTreeLitMasterNode.SpeedTreeVersion.SpeedTree7)
                {
                    switch (masterNode.speedTreeGeomType)
                    {
                        case SpeedTreeLitMasterNode.TreeGeomType.BranchDetail:
                            pass.ExtraDefines.Add("#define GEOM_TYPE_BRANCH_DETAIL");
                            pass.ExtraDefines.Add("#define GEOM_TYPE_BRANCH");
                            break;
                        case SpeedTreeLitMasterNode.TreeGeomType.Branch:
                            pass.ExtraDefines.Add("#define GEOM_TYPE_BRANCH");
                            break;
                        case SpeedTreeLitMasterNode.TreeGeomType.Frond:
                            pass.ExtraDefines.Add("#define GEOM_TYPE_FROND");
                            break;
                        case SpeedTreeLitMasterNode.TreeGeomType.Leaf:
                            pass.ExtraDefines.Add("#define GEOM_TYPE_LEAF");
                            break;
                        case SpeedTreeLitMasterNode.TreeGeomType.Mesh:
                            pass.ExtraDefines.Add("#define GEOM_TYPE_MESH");
                            break;
                    }
                }
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
            RequiredFields = new List<string>()
            {
                "AttributesMesh.normalOS",
                "AttributesMesh.tangentOS",     // Always present as we require it also in case of Variants lighting
                "AttributesMesh.uv0",
                "AttributesMesh.uv1",
                "AttributesMesh.color",
                "AttributesMesh.uv2",           // SHADERPASS_LIGHT_TRANSPORT always uses uv2
            },

            ExtraDefines = new List<string>()
            {
                "#define ENABLE_WIND",
                "#define EFFECT_BACKSIDE_NORMALS",
                "#define SPEEDTREE_Y_UP",
            },
            PixelShaderSlots = new List<int>()
            {
                SpeedTreeLitMasterNode.AlbedoSlotId,
                SpeedTreeLitMasterNode.NormalSlotId,
                SpeedTreeLitMasterNode.BentNormalSlotId,
                SpeedTreeLitMasterNode.TangentSlotId,
                //SpeedTreeLitMasterNode.SubsurfaceMaskSlotId,
                //SpeedTreeLitMasterNode.ThicknessSlotId,
                //SpeedTreeLitMasterNode.DiffusionProfileHashSlotId,
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

                if (masterNode.speedTreeAssetVersion == SpeedTreeLitMasterNode.SpeedTreeVersion.SpeedTree7)
                {
                    switch (masterNode.speedTreeGeomType)
                    {
                        case SpeedTreeLitMasterNode.TreeGeomType.BranchDetail:
                            pass.ExtraDefines.Add("#define GEOM_TYPE_BRANCH_DETAIL");
                            pass.ExtraDefines.Add("#define GEOM_TYPE_BRANCH");
                            break;
                        case SpeedTreeLitMasterNode.TreeGeomType.Branch:
                            pass.ExtraDefines.Add("#define GEOM_TYPE_BRANCH");
                            break;
                        case SpeedTreeLitMasterNode.TreeGeomType.Frond:
                            pass.ExtraDefines.Add("#define GEOM_TYPE_FROND");
                            break;
                        case SpeedTreeLitMasterNode.TreeGeomType.Leaf:
                            pass.ExtraDefines.Add("#define GEOM_TYPE_LEAF");
                            break;
                        case SpeedTreeLitMasterNode.TreeGeomType.Mesh:
                            pass.ExtraDefines.Add("#define GEOM_TYPE_MESH");
                            break;
                    }
                }
                else
                {
                    pass.PixelShaderSlots.Add(SpeedTreeLitMasterNode.SubsurfaceMaskSlotId);
                    pass.PixelShaderSlots.Add(SpeedTreeLitMasterNode.ThicknessSlotId);
                    pass.PixelShaderSlots.Add(SpeedTreeLitMasterNode.DiffusionProfileHashSlotId);
                }
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
                "#pragma multi_compile_vertex LOD_FADE_PERCENTAGE",
                "#define ENABLE_WIND",
                "#define EFFECT_BACKSIDE_NORMALS",
                "#define SPEEDTREE_Y_UP",
            },
            PixelShaderSlots = new List<int>()
            {
                SpeedTreeLitMasterNode.AlphaSlotId,
                SpeedTreeLitMasterNode.AlphaThresholdSlotId,
                SpeedTreeLitMasterNode.AlphaThresholdShadowSlotId,
                SpeedTreeLitMasterNode.DepthOffsetSlotId,
                SpeedTreeLitMasterNode.DepthBiasSlotId,
            },
            VertexShaderSlots = new List<int>()
            {
                SpeedTreeLitMasterNode.PositionSlotId,
            },
            UseInPreview = false,
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
                "#define ENABLE_WIND",
                "#define EFFECT_BACKSIDE_NORMALS",
                "#define SPEEDTREE_Y_UP",
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
            },
            UseInPreview = false
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
                SpeedTreeLitMasterNode.DepthBiasSlotId,
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
                SpeedTreeLitMasterNode.PositionSlotId
            },
            UseInPreview = true,

            OnGeneratePassImpl = (IMasterNode node, ref Pass pass) =>
            {
                var masterNode = node as SpeedTreeLitMasterNode;
                HDSubShaderUtilities.SetStencilStateForDepth(ref pass);

                pass.ExtraDefines.Add("#define ENABLE_WIND");
                pass.ExtraDefines.Add("#define EFFECT_BACKSIDE_NORMALS");
                pass.ExtraDefines.Add("#define SPEEDTREE_Y_UP");
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
                SpeedTreeLitMasterNode.PositionSlotId
            },
            UseInPreview = false,

            OnGeneratePassImpl = (IMasterNode node, ref Pass pass) =>
            {
                var masterNode = node as SpeedTreeLitMasterNode;
                HDSubShaderUtilities.SetStencilStateForMotionVector(ref pass);

                pass.ExtraDefines.Add("#define ENABLE_WIND");
                pass.ExtraDefines.Add("#define EFFECT_BACKSIDE_NORMALS");
                pass.ExtraDefines.Add("#define SPEEDTREE_Y_UP");
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
            MaterialName = "SpeedTreeLit",
            ShaderPassName = "SHADERPASS_DEPTH_ONLY",
            BlendOverride = "Blend One Zero",
            ZWriteOverride = "ZWrite On",
            ColorMaskOverride = "ColorMask 0",
            CullOverride = HDSubShaderUtilities.defaultCullMode,
            ExtraDefines = new List<string>()
            {
                "#define CUTOFF_TRANSPARENT_DEPTH_PREPASS",
                "#define ENABLE_WIND",
                "#define EFFECT_BACKSIDE_NORMALS",
                "#define SPEEDTREE_Y_UP",
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
                SpeedTreeLitMasterNode.DepthBiasSlotId
            },
            VertexShaderSlots = new List<int>()
            {
                SpeedTreeLitMasterNode.PositionSlotId
            },
            UseInPreview = true,
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
                SpeedTreeLitMasterNode.PositionSlotId
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
            TemplateName = "SpeedTreeLitPass.template",
            MaterialName = "SpeedTreeLit",
            ShaderPassName = "SHADERPASS_FORWARD",
            CullOverride = HDSubShaderUtilities.cullModeForward,
            ZTestOverride = HDSubShaderUtilities.zTestDepthEqualForOpaque,
            ZWriteOverride = HDSubShaderUtilities.ZWriteDefault,
            // ExtraDefines are set when the pass is generated
            ExtraDefines = new List<string>()
            {
                "#define ENABLE_WIND",
                "#define EFFECT_BACKSIDE_NORMALS",
                "#define SPEEDTREE_Y_UP",
            },
            Includes = new List<string>()
            {
                "#include \"Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassForward.hlsl\"",
            },
            RequiredFields = new List<string>()
            {
                "FragInputs.tangentToWorld",
                "FragInputs.positionRWS",
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
                SpeedTreeLitMasterNode.PositionSlotId
            },
            UseInPreview = true,

            OnGeneratePassImpl = (IMasterNode node, ref Pass pass) =>
            {
                var masterNode = node as SpeedTreeLitMasterNode;
                HDSubShaderUtilities.SetStencilStateForForward(ref pass);
                HDSubShaderUtilities.SetBlendModeForForward(ref pass);

                if (masterNode.speedTreeAssetVersion == SpeedTreeLitMasterNode.SpeedTreeVersion.SpeedTree8)
                {
                    pass.ExtraDefines.Add("#ifdef GEOM_TYPE_LEAF\n#define _SURFACE_TYPE_TRANSPARENT\n#endif");
                    pass.ExtraDefines.Add("#ifndef _SURFACE_TYPE_TRANSPARENT\n#define SHADERPASS_FORWARD_BYPASS_ALPHA_TEST\n#endif");
                }

                pass.ExtraDefines.Remove("#ifndef DEBUG_DISPLAY\n#define SHADERPASS_FORWARD_BYPASS_ALPHA_TEST\n#endif");

                pass.ColorMaskOverride = "ColorMask [_ColorMaskTransparentVel] 1";
                if (masterNode.alphaTest.isOn)
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
            MaterialName = "SpeedTreeLit",
            ShaderPassName = "SHADERPASS_DEPTH_ONLY",
            BlendOverride = "Blend One Zero",
            ZWriteOverride = "ZWrite On",
            ColorMaskOverride = "ColorMask 0",
            CullOverride = HDSubShaderUtilities.defaultCullMode,
            ExtraDefines = new List<string>()
            {
                "#define CUTOFF_TRANSPARENT_DEPTH_POSTPASS",
                "#define ENABLE_WIND",
                "#define EFFECT_BACKSIDE_NORMALS",
                "#define SPEEDTREE_Y_UP",
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
                SpeedTreeLitMasterNode.PositionSlotId
            },
            UseInPreview = true,
        };

        /*

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
                "#define SKIP_RASTERIZED_SHADOWS",
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
                SpeedTreeLitMasterNode.PositionSlotId
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
                SpeedTreeLitMasterNode.PositionSlotId
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
                "#define SKIP_RASTERIZED_SHADOWS",
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
                SpeedTreeLitMasterNode.PositionSlotId
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
                SpeedTreeLitMasterNode.PositionSlotId
            },
            UseInPreview = false
        };
        */

        public int GetPreviewPassIndex() { return 0; }

        public bool IsPipelineCompatible(RenderPipelineAsset renderPipelineAsset)
        {
            return renderPipelineAsset is HDRenderPipelineAsset;
        }

        private static HashSet<string> GetActiveFieldsFromMasterNode(AbstractMaterialNode iMasterNode, Pass pass)
        {
            HashSet<string> activeFields = new HashSet<string>();

            SpeedTreeLitMasterNode masterNode = iMasterNode as SpeedTreeLitMasterNode;
            if (masterNode == null)
            {
                return activeFields;
            }

            // TODO

            return activeFields;
        }

        private static bool GenerateShaderPassLit(SpeedTreeLitMasterNode masterNode, Pass pass, GenerationMode mode, ShaderGenerator result, List<string> sourceAssetDependencyPaths)
        {
            if (mode == GenerationMode.ForReals || pass.UseInPreview)
            {
                pass.OnGeneratePass(masterNode);

                // apply master node options to active fields
                HashSet<string> activeFields = GetActiveFieldsFromMasterNode(masterNode, pass);

                //pass.ExtraInstancingOptions = GetInstancingOptionsFromMasterNode(masterNode);

                // use standard shader pass generation
                bool vertexActive = masterNode.IsSlotConnected(SpeedTreeLitMasterNode.PositionSlotId);
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
            // TODO
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
                bool transparentDepthPrepassActive = transparent && masterNode.alphaTest.isOn && masterNode.alphaTestDepthPrepass.isOn;
                bool transparentDepthPostpassActive = transparent && masterNode.alphaTest.isOn && masterNode.alphaTestDepthPostpass.isOn;

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

            //subShader.AddShaderChunk(@"CustomEditor ""UnityEditor.Rendering.HighDefinition.HDLitGUI""");

            return subShader.GetShaderString(0);
        }
    }
}
