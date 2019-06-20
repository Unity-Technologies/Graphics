using System.Collections.Generic;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph;
using UnityEngine.Experimental.Rendering.HDPipeline;
using UnityEngine.Rendering;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    class HairSubShader : IHairSubShader
    {
        Pass m_PassMETA = new Pass()
        {
            Name = "META",
            LightMode = "META",
            TemplateName = "HairPass.template",
            MaterialName = "Hair",
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
            PixelShaderSlots = new List<int>()
            {
                HairMasterNode.AlbedoSlotId,
                HairMasterNode.NormalSlotId,
                HairMasterNode.SpecularOcclusionSlotId,
                HairMasterNode.BentNormalSlotId,
                HairMasterNode.HairStrandDirectionSlotId,
                HairMasterNode.TransmittanceSlotId,
                HairMasterNode.RimTransmissionIntensitySlotId,
                HairMasterNode.SmoothnessSlotId,
                HairMasterNode.AmbientOcclusionSlotId,
                HairMasterNode.EmissionSlotId,
                HairMasterNode.AlphaSlotId,
                HairMasterNode.AlphaClipThresholdSlotId,
                HairMasterNode.SpecularAAScreenSpaceVarianceSlotId,
                HairMasterNode.SpecularAAThresholdSlotId,
                HairMasterNode.SpecularTintSlotId,
                HairMasterNode.SpecularShiftSlotId,
                HairMasterNode.SecondarySpecularTintSlotId,
                HairMasterNode.SecondarySmoothnessSlotId,
                HairMasterNode.SecondarySpecularShiftSlotId,
            },
            VertexShaderSlots = new List<int>()
            {
                //HairMasterNode.PositionSlotId
            },
            UseInPreview = false
        };

        Pass m_PassShadowCaster = new Pass()
        {
            Name = "ShadowCaster",
            LightMode = "ShadowCaster",
            TemplateName = "HairPass.template",
            MaterialName = "Hair",
            ShaderPassName = "SHADERPASS_SHADOWS",
            BlendOverride = "Blend One Zero",
            ZWriteOverride = "ZWrite On",
            ColorMaskOverride = "ColorMask 0",
            ZClipOverride = HDSubShaderUtilities.zClipShadowCaster,
            CullOverride = HDSubShaderUtilities.defaultCullMode,
            Includes = new List<string>()
            {
                "#include \"Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassDepthOnly.hlsl\"",
            },
            PixelShaderSlots = new List<int>()
            {
                HairMasterNode.AlphaSlotId,
                HairMasterNode.AlphaClipThresholdSlotId,
                HairMasterNode.AlphaClipThresholdShadowSlotId,
                HairMasterNode.DepthOffsetSlotId,
            },
            VertexShaderSlots = new List<int>()
            {
                HairMasterNode.PositionSlotId
            },
            UseInPreview = false
        };

        Pass m_SceneSelectionPass = new Pass()
        {
            Name = "SceneSelectionPass",
            LightMode = "SceneSelectionPass",
            TemplateName = "HairPass.template",
            MaterialName = "Hair",
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
                HairMasterNode.AlphaSlotId,
                HairMasterNode.AlphaClipThresholdSlotId,
                HairMasterNode.DepthOffsetSlotId,
            },
            VertexShaderSlots = new List<int>()
            {
                HairMasterNode.PositionSlotId
            },
            UseInPreview = false
        };

        Pass m_PassDepthForwardOnly = new Pass()
        {
            Name = "DepthForwardOnly",
            LightMode = "DepthForwardOnly",
            TemplateName = "HairPass.template",
            MaterialName = "Hair",
            ShaderPassName = "SHADERPASS_DEPTH_ONLY",
            ZWriteOverride = "ZWrite On",

            ExtraDefines = HDSubShaderUtilities.s_ExtraDefinesForwardMaterialDepthOrMotion,

            Includes = new List<string>()
            {
                "#include \"Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassDepthOnly.hlsl\"",
            },
            PixelShaderSlots = new List<int>()
            {
                HairMasterNode.NormalSlotId,
                HairMasterNode.SmoothnessSlotId,
                HairMasterNode.AlphaSlotId,
                HairMasterNode.AlphaClipThresholdSlotId,
                HairMasterNode.DepthOffsetSlotId,
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
                HairMasterNode.PositionSlotId
            },
            UseInPreview = true,

            OnGeneratePassImpl = (IMasterNode node, ref Pass pass) =>
            {
                var masterNode = node as HairMasterNode;
                HDSubShaderUtilities.SetStencilStateForDepth(ref pass);
            }
        };

        Pass m_PassMotionVectors = new Pass()
        {
            Name = "MotionVectors",
            LightMode = "MotionVectors",
            TemplateName = "HairPass.template",
            MaterialName = "Hair",
            ShaderPassName = "SHADERPASS_MOTION_VECTORS",
            ExtraDefines = HDSubShaderUtilities.s_ExtraDefinesForwardMaterialDepthOrMotion,
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
                HairMasterNode.NormalSlotId,
                HairMasterNode.SmoothnessSlotId,
                HairMasterNode.AlphaSlotId,
                HairMasterNode.AlphaClipThresholdSlotId,
                HairMasterNode.DepthOffsetSlotId,
            },
            VertexShaderSlots = new List<int>()
            {
                HairMasterNode.PositionSlotId
            },
            UseInPreview = false,

            OnGeneratePassImpl = (IMasterNode node, ref Pass pass) =>
            {
                var masterNode = node as HairMasterNode;
                HDSubShaderUtilities.SetStencilStateForMotionVector(ref pass);
            }
        };

        Pass m_PassTransparentDepthPrepass = new Pass()
        {
            Name = "TransparentDepthPrepass",
            LightMode = "TransparentDepthPrepass",
            TemplateName = "HairPass.template",
            MaterialName = "Hair",
            ShaderPassName = "SHADERPASS_DEPTH_ONLY",
            BlendOverride = "Blend One Zero",
            ZWriteOverride = "ZWrite On",
            ColorMaskOverride = "ColorMask 0",
            CullOverride = HDSubShaderUtilities.defaultCullMode,
            ExtraDefines = new List<string>()
            {
                "#define CUTOFF_TRANSPARENT_DEPTH_PREPASS",
            },
            Includes = new List<string>()
            {
                "#include \"Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassDepthOnly.hlsl\"",
            },
            PixelShaderSlots = new List<int>()
            {
                HairMasterNode.AlphaSlotId,
                HairMasterNode.AlphaClipThresholdDepthPrepassSlotId,
                HairMasterNode.DepthOffsetSlotId,
            },
            VertexShaderSlots = new List<int>()
            {
                HairMasterNode.PositionSlotId
            },
            UseInPreview = true
        };

        Pass m_PassTransparentBackface = new Pass()
        {
            Name = "TransparentBackface",
            LightMode = "TransparentBackface",
            TemplateName = "HairPass.template",
            MaterialName = "Hair",
            ShaderPassName = "SHADERPASS_FORWARD",
            CullOverride = "Cull Front",
            ColorMaskOverride = "ColorMask [_ColorMaskTransparentVel] 1",
            ExtraDefines = HDSubShaderUtilities.s_ExtraDefinesForwardTransparent,
            ZTestOverride = HDSubShaderUtilities.zTestTransparent,
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
                HairMasterNode.AlbedoSlotId,
                HairMasterNode.NormalSlotId,
                HairMasterNode.SpecularOcclusionSlotId,
                HairMasterNode.BentNormalSlotId,
                HairMasterNode.HairStrandDirectionSlotId,
                HairMasterNode.TransmittanceSlotId,
                HairMasterNode.RimTransmissionIntensitySlotId,
                HairMasterNode.SmoothnessSlotId,
                HairMasterNode.AmbientOcclusionSlotId,
                HairMasterNode.EmissionSlotId,
                HairMasterNode.AlphaSlotId,
                HairMasterNode.AlphaClipThresholdSlotId,
                HairMasterNode.SpecularAAScreenSpaceVarianceSlotId,
                HairMasterNode.SpecularAAThresholdSlotId,
                HairMasterNode.SpecularTintSlotId,
                HairMasterNode.SpecularShiftSlotId,
                HairMasterNode.SecondarySpecularTintSlotId,
                HairMasterNode.SecondarySmoothnessSlotId,
                HairMasterNode.SecondarySpecularShiftSlotId,
                HairMasterNode.DepthOffsetSlotId,
            },
            VertexShaderSlots = new List<int>()
            {
                HairMasterNode.PositionSlotId
            },
            UseInPreview = true,
            OnGeneratePassImpl = (IMasterNode node, ref Pass pass) =>
            {
                HDSubShaderUtilities.SetBlendModeForTransparentBackface(ref pass);
            }
        };

        Pass m_PassForwardOnly = new Pass()
        {
            Name = "ForwardOnly",
            LightMode = "ForwardOnly",
            TemplateName = "HairPass.template",
            MaterialName = "Hair",
            ShaderPassName = "SHADERPASS_FORWARD",
            CullOverride = HDSubShaderUtilities.cullModeForward,
            ZWriteOverride = HDSubShaderUtilities.ZWriteDefault,
            ZTestOverride = HDSubShaderUtilities.zTestDepthEqualForOpaque,
            // ExtraDefines are set when the pass is generated
            Includes = new List<string>()
            {
                "#include \"Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassForward.hlsl\"",
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
                HairMasterNode.AlbedoSlotId,
                HairMasterNode.NormalSlotId,
                HairMasterNode.SpecularOcclusionSlotId,
                HairMasterNode.BentNormalSlotId,
                HairMasterNode.HairStrandDirectionSlotId,
                HairMasterNode.TransmittanceSlotId,
                HairMasterNode.RimTransmissionIntensitySlotId,
                HairMasterNode.SmoothnessSlotId,
                HairMasterNode.AmbientOcclusionSlotId,
                HairMasterNode.EmissionSlotId,
                HairMasterNode.AlphaSlotId,
                HairMasterNode.AlphaClipThresholdSlotId,
                HairMasterNode.SpecularAAScreenSpaceVarianceSlotId,
                HairMasterNode.SpecularAAThresholdSlotId,
                HairMasterNode.SpecularTintSlotId,
                HairMasterNode.SpecularShiftSlotId,
                HairMasterNode.SecondarySpecularTintSlotId,
                HairMasterNode.SecondarySmoothnessSlotId,
                HairMasterNode.SecondarySpecularShiftSlotId,
                HairMasterNode.LightingSlotId,
                HairMasterNode.BackLightingSlotId,
                HairMasterNode.DepthOffsetSlotId,
            },
            VertexShaderSlots = new List<int>()
            {
                HairMasterNode.PositionSlotId
            },
            UseInPreview = true,

            OnGeneratePassImpl = (IMasterNode node, ref Pass pass) =>
            {
                var masterNode = node as HairMasterNode;
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
            TemplateName = "HairPass.template",
            MaterialName = "Hair",
            ShaderPassName = "SHADERPASS_DEPTH_ONLY",
            BlendOverride = "Blend One Zero",
            ZWriteOverride = "ZWrite On",
            ColorMaskOverride = "ColorMask 0",
            ZTestOverride = "ZTest LEqual",
            CullOverride = HDSubShaderUtilities.defaultCullMode,
            ExtraDefines = new List<string>()
            {
                "#define CUTOFF_TRANSPARENT_DEPTH_POSTPASS",
            },
            Includes = new List<string>()
            {
                "#include \"Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassDepthOnly.hlsl\"",
            },
            PixelShaderSlots = new List<int>()
            {
                HairMasterNode.AlphaSlotId,
                HairMasterNode.AlphaClipThresholdDepthPostpassSlotId,
                HairMasterNode.DepthOffsetSlotId,
            },
            VertexShaderSlots = new List<int>()
            {
                HairMasterNode.PositionSlotId
            },
            UseInPreview = true,
        };

        public int GetPreviewPassIndex() { return 0; }

        private static HashSet<string> GetActiveFieldsFromMasterNode(AbstractMaterialNode iMasterNode, Pass pass)
        {
            HashSet<string> activeFields = new HashSet<string>();

            HairMasterNode masterNode = iMasterNode as HairMasterNode;
            if (masterNode == null)
            {
                return activeFields;
            }

            if (masterNode.doubleSidedMode != DoubleSidedMode.Disabled)
            {
                if (pass.ShaderPassName != "SHADERPASS_MOTION_VECTORS")   // HACK to get around lack of a good interpolator dependency system
                {                                                   // we need to be able to build interpolators using multiple input structs
                                                                    // also: should only require isFrontFace if Normals are required...
                    // Important: the following is used in SharedCode.template.hlsl for determining the normal flip mode
                    activeFields.Add("FragInputs.isFrontFace");
                }
            }

            switch (masterNode.materialType)
            {
                case HairMasterNode.MaterialType.KajiyaKay:
                    activeFields.Add("Material.KajiyaKay");
                    break;

                default:
                    UnityEngine.Debug.LogError("Unknown material type: " + masterNode.materialType);
                    break;
            }

            if (masterNode.alphaTest.isOn)
            {
                int count = 0;

                // If alpha test shadow is enable, we use it, otherwise we use the regular test
                if (pass.PixelShaderUsesSlot(HairMasterNode.AlphaClipThresholdShadowSlotId) && masterNode.alphaTestShadow.isOn)
                {
                    activeFields.Add("AlphaTestShadow");
                    ++count;
                }
                else if (pass.PixelShaderUsesSlot(HairMasterNode.AlphaClipThresholdSlotId))
                {
                    activeFields.Add("AlphaTest");
                    ++count;
                }
                // Other alpha test are suppose to be alone
                else if (pass.PixelShaderUsesSlot(HairMasterNode.AlphaClipThresholdDepthPrepassSlotId))
                {
                    activeFields.Add("AlphaTestPrepass");
                    ++count;
                }
                else if (pass.PixelShaderUsesSlot(HairMasterNode.AlphaClipThresholdDepthPostpassSlotId))
                {
                    activeFields.Add("AlphaTestPostpass");
                    ++count;
                }
                UnityEngine.Debug.Assert(count == 1, "Alpha test value not set correctly");
            }

            if (masterNode.surfaceType != SurfaceType.Opaque)
            {
                if (masterNode.transparencyFog.isOn)
                {
                    activeFields.Add("AlphaFog");
                }

                if (masterNode.transparentWritesMotionVec.isOn)
                {
                    activeFields.Add("TransparentWritesMotionVec");
                }

                if (masterNode.blendPreserveSpecular.isOn)
                {
                    activeFields.Add("BlendMode.PreserveSpecular");
                }
            }

            if (!masterNode.receiveDecals.isOn)
            {
                activeFields.Add("DisableDecals");
            }

            if (!masterNode.receiveSSR.isOn)
            {
                activeFields.Add("DisableSSR");
            }

            if (masterNode.specularAA.isOn && pass.PixelShaderUsesSlot(HairMasterNode.SpecularAAThresholdSlotId) && pass.PixelShaderUsesSlot(HairMasterNode.SpecularAAScreenSpaceVarianceSlotId))
            {
                activeFields.Add("Specular.AA");
            }

            if (masterNode.IsSlotConnected(HairMasterNode.BentNormalSlotId) && pass.PixelShaderUsesSlot(HairMasterNode.BentNormalSlotId))
            {
                activeFields.Add("BentNormal");
            }

            if (masterNode.IsSlotConnected(HairMasterNode.HairStrandDirectionSlotId) && pass.PixelShaderUsesSlot(HairMasterNode.HairStrandDirectionSlotId))
            {
                activeFields.Add("HairStrandDirection");
            }

            if (masterNode.IsSlotConnected(HairMasterNode.TransmittanceSlotId) && pass.PixelShaderUsesSlot(HairMasterNode.TransmittanceSlotId))
            {
                activeFields.Add(HairMasterNode.TransmittanceSlotName);
            }

            if (masterNode.IsSlotConnected(HairMasterNode.RimTransmissionIntensitySlotId) && pass.PixelShaderUsesSlot(HairMasterNode.RimTransmissionIntensitySlotId))
            {
                activeFields.Add(HairMasterNode.RimTransmissionIntensitySlotName);
            }

            if (masterNode.useLightFacingNormal.isOn)
            {
                activeFields.Add("UseLightFacingNormal");
            }

            switch (masterNode.specularOcclusionMode)
            {
                case SpecularOcclusionMode.Off:
                    break;
                case SpecularOcclusionMode.FromAO:
                    activeFields.Add("SpecularOcclusionFromAO");
                    break;
                case SpecularOcclusionMode.FromAOAndBentNormal:
                    activeFields.Add("SpecularOcclusionFromAOBentNormal");
                    break;
                case SpecularOcclusionMode.Custom:
                    activeFields.Add("SpecularOcclusionCustom");
                    break;

                default:
                    break;
            }

            if (pass.PixelShaderUsesSlot(HairMasterNode.AmbientOcclusionSlotId))
            {
                var occlusionSlot = masterNode.FindSlot<Vector1MaterialSlot>(HairMasterNode.AmbientOcclusionSlotId);

                bool connected = masterNode.IsSlotConnected(HairMasterNode.AmbientOcclusionSlotId);
                if (connected || occlusionSlot.value != occlusionSlot.defaultValue)
                {
                    activeFields.Add("AmbientOcclusion");
                }
            }

            if (masterNode.IsSlotConnected(HairMasterNode.LightingSlotId) && pass.PixelShaderUsesSlot(HairMasterNode.LightingSlotId))
            {
                activeFields.Add("LightingGI");
            }
            if (masterNode.IsSlotConnected(HairMasterNode.BackLightingSlotId) && pass.PixelShaderUsesSlot(HairMasterNode.LightingSlotId))
            {
                activeFields.Add("BackLightingGI");
            }

            if (masterNode.depthOffset.isOn && pass.PixelShaderUsesSlot(HairMasterNode.DepthOffsetSlotId))
                activeFields.Add("DepthOffset");

            return activeFields;
        }

        private static bool GenerateShaderPassHair(HairMasterNode masterNode, Pass pass, GenerationMode mode, ShaderGenerator result, List<string> sourceAssetDependencyPaths)
        {
            if (mode == GenerationMode.ForReals || pass.UseInPreview)
            {
                pass.OnGeneratePass(masterNode);

                // apply master node options to active fields
                HashSet<string> activeFields = GetActiveFieldsFromMasterNode(masterNode, pass);

                // use standard shader pass generation
                bool vertexActive = masterNode.IsSlotConnected(HairMasterNode.PositionSlotId);
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
                // HairSubShader.cs
                sourceAssetDependencyPaths.Add(AssetDatabase.GUIDToAssetPath("c3f20efb64673e0488a2c8e986a453fa"));
                // HDSubShaderUtilities.cs
                sourceAssetDependencyPaths.Add(AssetDatabase.GUIDToAssetPath("713ced4e6eef4a44799a4dd59041484b"));
            }

            var masterNode = iMasterNode as HairMasterNode;

            var subShader = new ShaderGenerator();
            subShader.AddShaderChunk("SubShader", true);
            subShader.AddShaderChunk("{", true);
            subShader.Indent();
            {
                // Add tags at the SubShader level
                var renderingPass = masterNode.surfaceType == SurfaceType.Opaque ? HDRenderQueue.RenderQueueType.Opaque : HDRenderQueue.RenderQueueType.Transparent;
                int queue = HDRenderQueue.ChangeType(renderingPass, masterNode.sortPriority, masterNode.alphaTest.isOn);
                HDSubShaderUtilities.AddTags(subShader, HDRenderPipeline.k_ShaderTagName, HDRenderTypeTags.HDLitShader, queue);

                // generate the necessary shader passes
                bool opaque = (masterNode.surfaceType == SurfaceType.Opaque);
                bool transparent = !opaque;

                bool transparentBackfaceActive = transparent && masterNode.backThenFrontRendering.isOn;
                bool transparentDepthPrepassActive = transparent && masterNode.alphaTest.isOn && masterNode.alphaTestDepthPrepass.isOn;
                bool transparentDepthPostpassActive = transparent && masterNode.alphaTest.isOn && masterNode.alphaTestDepthPostpass.isOn;

                GenerateShaderPassHair(masterNode, m_PassMETA, mode, subShader, sourceAssetDependencyPaths);
                GenerateShaderPassHair(masterNode, m_PassShadowCaster, mode, subShader, sourceAssetDependencyPaths);
                GenerateShaderPassHair(masterNode, m_SceneSelectionPass, mode, subShader, sourceAssetDependencyPaths);

                GenerateShaderPassHair(masterNode, m_PassDepthForwardOnly, mode, subShader, sourceAssetDependencyPaths);
                GenerateShaderPassHair(masterNode, m_PassMotionVectors, mode, subShader, sourceAssetDependencyPaths);

                if (transparentBackfaceActive)
                {
                    GenerateShaderPassHair(masterNode, m_PassTransparentBackface, mode, subShader, sourceAssetDependencyPaths);
                }

                if (transparentDepthPrepassActive)
                {
                    GenerateShaderPassHair(masterNode, m_PassTransparentDepthPrepass, mode, subShader, sourceAssetDependencyPaths);
                }

                // Assign define here based on opaque or transparent to save some variant
                m_PassForwardOnly.ExtraDefines = opaque ? HDSubShaderUtilities.s_ExtraDefinesForwardOpaque : HDSubShaderUtilities.s_ExtraDefinesForwardTransparent;
                GenerateShaderPassHair(masterNode, m_PassForwardOnly, mode, subShader, sourceAssetDependencyPaths);

                if (transparentDepthPostpassActive)
                {
                    GenerateShaderPassHair(masterNode, m_PassTransparentDepthPostpass, mode, subShader, sourceAssetDependencyPaths);
                }
            }
            subShader.Deindent();
            subShader.AddShaderChunk("}", true);
            subShader.AddShaderChunk(@"CustomEditor ""UnityEditor.Experimental.Rendering.HDPipeline.HairGUI""");

            return subShader.GetShaderString(0);
        }

        public bool IsPipelineCompatible(RenderPipelineAsset renderPipelineAsset)
        {
            return renderPipelineAsset is HDRenderPipelineAsset;
        }
    }
}
