using System.Collections.Generic;
using UnityEditor.ShaderGraph;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using Data.Util;

namespace UnityEditor.Rendering.HighDefinition
{
    [FormerName("UnityEditor.Experimental.Rendering.HDPipeline.FabricSubShader")]
    class FabricSubShader : IFabricSubShader
    {
        Pass m_PassMETA = new Pass()
        {
            Name = "META",
            LightMode = "META",
            TemplateName = "FabricPass.template",
            MaterialName = "Fabric",
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
                FabricMasterNode.AlbedoSlotId,
                FabricMasterNode.SpecularOcclusionSlotId,
                FabricMasterNode.NormalSlotId,
                FabricMasterNode.SmoothnessSlotId,
                FabricMasterNode.AmbientOcclusionSlotId,
                FabricMasterNode.SpecularColorSlotId,
                FabricMasterNode.DiffusionProfileHashSlotId,
                FabricMasterNode.SubsurfaceMaskSlotId,
                FabricMasterNode.ThicknessSlotId,
                FabricMasterNode.TangentSlotId,
                FabricMasterNode.AnisotropySlotId,
                FabricMasterNode.EmissionSlotId,
                FabricMasterNode.AlphaSlotId,
                FabricMasterNode.AlphaClipThresholdSlotId,
            },
            VertexShaderSlots = new List<int>()
            {
                //FabricMasterNode.PositionSlotId
            },
            UseInPreview = false,
        };

        Pass m_PassShadowCaster = new Pass()
        {
            Name = "ShadowCaster",
            LightMode = "ShadowCaster",
            TemplateName = "FabricPass.template",
            MaterialName = "Fabric",
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
                FabricMasterNode.AlphaSlotId,
                FabricMasterNode.AlphaClipThresholdSlotId,
                FabricMasterNode.DepthOffsetSlotId,
            },
            VertexShaderSlots = new List<int>()
            {
                FabricMasterNode.PositionSlotId,
                FabricMasterNode.VertexNormalSlotId,
                FabricMasterNode.VertexTangentSlotId
            },
            UseInPreview = false,
        };

        Pass m_SceneSelectionPass = new Pass()
        {
            Name = "SceneSelectionPass",
            LightMode = "SceneSelectionPass",
            TemplateName = "FabricPass.template",
            MaterialName = "Fabric",
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
                FabricMasterNode.AlphaSlotId,
                FabricMasterNode.AlphaClipThresholdSlotId,
                FabricMasterNode.DepthOffsetSlotId,
            },
            VertexShaderSlots = new List<int>()
            {
                FabricMasterNode.PositionSlotId,
                FabricMasterNode.VertexNormalSlotId,
                FabricMasterNode.VertexTangentSlotId
            },
            UseInPreview = false
        };

        Pass m_PassDepthForwardOnly = new Pass()
        {
            Name = "DepthForwardOnly",
            LightMode = "DepthForwardOnly",
            TemplateName = "FabricPass.template",
            MaterialName = "Fabric",
            ShaderPassName = "SHADERPASS_DEPTH_ONLY",
            ZWriteOverride = "ZWrite On",
            CullOverride = HDSubShaderUtilities.defaultCullMode,
            ExtraDefines = HDSubShaderUtilities.s_ExtraDefinesForwardMaterialDepthOrMotion,

            Includes = new List<string>()
            {
                "#include \"Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassDepthOnly.hlsl\"",
            },
            PixelShaderSlots = new List<int>()
            {
                FabricMasterNode.NormalSlotId,
                FabricMasterNode.SmoothnessSlotId,
                FabricMasterNode.AlphaSlotId,
                FabricMasterNode.AlphaClipThresholdSlotId,
                FabricMasterNode.DepthOffsetSlotId,
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
                FabricMasterNode.PositionSlotId,
                FabricMasterNode.VertexNormalSlotId,
                FabricMasterNode.VertexTangentSlotId
            },
            UseInPreview = true,

            OnGeneratePassImpl = (IMasterNode node, ref Pass pass) =>
            {
                var masterNode = node as FabricMasterNode;
                HDSubShaderUtilities.SetStencilStateForDepth(ref pass);
            }
        };

        Pass m_PassMotionVectors = new Pass()
        {
            Name = "MotionVectors",
            LightMode = "MotionVectors",
            TemplateName = "FabricPass.template",
            MaterialName = "Fabric",
            ShaderPassName = "SHADERPASS_MOTION_VECTORS",
            ExtraDefines = HDSubShaderUtilities.s_ExtraDefinesForwardMaterialDepthOrMotion,
            CullOverride = HDSubShaderUtilities.defaultCullMode,
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
                FabricMasterNode.NormalSlotId,
                FabricMasterNode.SmoothnessSlotId,
                FabricMasterNode.AlphaSlotId,
                FabricMasterNode.AlphaClipThresholdSlotId,
                FabricMasterNode.DepthOffsetSlotId,
            },
            VertexShaderSlots = new List<int>()
            {
                FabricMasterNode.PositionSlotId,
                FabricMasterNode.VertexNormalSlotId,
                FabricMasterNode.VertexTangentSlotId
            },
            UseInPreview = false,

            OnGeneratePassImpl = (IMasterNode node, ref Pass pass) =>
            {
                var masterNode = node as FabricMasterNode;
                HDSubShaderUtilities.SetStencilStateForMotionVector(ref pass);
            }
        };

        Pass m_PassForwardOnly = new Pass()
        {
            Name = "ForwardOnly",
            LightMode = "ForwardOnly",
            TemplateName = "FabricPass.template",
            MaterialName = "Fabric",
            ShaderPassName = "SHADERPASS_FORWARD",
            CullOverride = HDSubShaderUtilities.cullModeForward,
            ZTestOverride = HDSubShaderUtilities.zTestDepthEqualForOpaque,
            ZWriteOverride = HDSubShaderUtilities.ZWriteDefault,
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
                FabricMasterNode.AlbedoSlotId,
                FabricMasterNode.SpecularOcclusionSlotId,
                FabricMasterNode.NormalSlotId,
                FabricMasterNode.BentNormalSlotId,
                FabricMasterNode.SmoothnessSlotId,
                FabricMasterNode.AmbientOcclusionSlotId,
                FabricMasterNode.SpecularColorSlotId,
                FabricMasterNode.DiffusionProfileHashSlotId,
                FabricMasterNode.SubsurfaceMaskSlotId,
                FabricMasterNode.ThicknessSlotId,
                FabricMasterNode.TangentSlotId,
                FabricMasterNode.AnisotropySlotId,
                FabricMasterNode.EmissionSlotId,
                FabricMasterNode.AlphaSlotId,
                FabricMasterNode.AlphaClipThresholdSlotId,
                FabricMasterNode.LightingSlotId,
                FabricMasterNode.BackLightingSlotId,
                FabricMasterNode.DepthOffsetSlotId,
            },
            VertexShaderSlots = new List<int>()
            {
                FabricMasterNode.PositionSlotId,
                FabricMasterNode.VertexNormalSlotId,
                FabricMasterNode.VertexTangentSlotId
            },
            UseInPreview = true,

            OnGeneratePassImpl = (IMasterNode node, ref Pass pass) =>
            {
                var masterNode = node as FabricMasterNode;
                HDSubShaderUtilities.SetStencilStateForForward(ref pass);
                HDSubShaderUtilities.SetBlendModeForForward(ref pass);

                pass.ExtraDefines.Remove("#ifndef DEBUG_DISPLAY\n#define SHADERPASS_FORWARD_BYPASS_ALPHA_TEST\n#endif");

                if (masterNode.surfaceType == SurfaceType.Opaque)
                {
                    if (masterNode.alphaTest.isOn)
                    {
                        // In case of opaque we don't want to perform the alpha test, it is done in depth prepass and we use depth equal for ztest (setup from UI)
                        // Don't do it with debug display mode as it is possible there is no depth prepass in this case
                        pass.ExtraDefines.Add("#ifndef DEBUG_DISPLAY\n#define SHADERPASS_FORWARD_BYPASS_ALPHA_TEST\n#endif");
                        pass.ZTestOverride = "ZTest Equal";
                    }
                    else
                        pass.ZTestOverride = null;
                }
            }
        };

        Pass m_PassRaytracingIndirect = new Pass()
        {
            Name = "IndirectDXR",
            LightMode = "IndirectDXR",
            TemplateName = "FabricRaytracingPass.template",
            MaterialName = "Fabric",
            ShaderPassName = "SHADERPASS_RAYTRACING_INDIRECT",
            ExtraDefines = new List<string>()
            {
                "#pragma multi_compile _ LIGHTMAP_ON",
                "#pragma multi_compile _ DIRLIGHTMAP_COMBINED",
                "#pragma multi_compile _ DYNAMICLIGHTMAP_ON",
                "#pragma multi_compile _ DIFFUSE_LIGHTING_ONLY",
                "#define SHADOW_LOW",
            },
            Includes = new List<string>()
            {
                "#include \"Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassRaytracingIndirect.hlsl\"",
            },
            PixelShaderSlots = new List<int>()
            {
                FabricMasterNode.AlbedoSlotId,
                FabricMasterNode.SpecularOcclusionSlotId,
                FabricMasterNode.NormalSlotId,
                FabricMasterNode.BentNormalSlotId,
                FabricMasterNode.SmoothnessSlotId,
                FabricMasterNode.AmbientOcclusionSlotId,
                FabricMasterNode.SpecularColorSlotId,
                FabricMasterNode.DiffusionProfileHashSlotId,
                FabricMasterNode.SubsurfaceMaskSlotId,
                FabricMasterNode.ThicknessSlotId,
                FabricMasterNode.TangentSlotId,
                FabricMasterNode.AnisotropySlotId,
                FabricMasterNode.EmissionSlotId,
                FabricMasterNode.AlphaSlotId,
                FabricMasterNode.AlphaClipThresholdSlotId,
                FabricMasterNode.LightingSlotId,
                FabricMasterNode.BackLightingSlotId,
                FabricMasterNode.DepthOffsetSlotId,
            },
            VertexShaderSlots = new List<int>()
            {
                FabricMasterNode.PositionSlotId,
                FabricMasterNode.VertexNormalSlotId,
                FabricMasterNode.VertexTangentSlotId
            },
            UseInPreview = false
        };

        Pass m_PassRaytracingVisibility = new Pass()
        {
            Name = "VisibilityDXR",
            LightMode = "VisibilityDXR",
            TemplateName = "FabricRaytracingPass.template",
            MaterialName = "Fabric",
            ShaderPassName = "SHADERPASS_RAYTRACING_VISIBILITY",
            Includes = new List<string>()
            {
                "#include \"Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassRaytracingVisibility.hlsl\"",
            },
            PixelShaderSlots = new List<int>()
            {
                FabricMasterNode.AlbedoSlotId,
                FabricMasterNode.SpecularOcclusionSlotId,
                FabricMasterNode.NormalSlotId,
                FabricMasterNode.BentNormalSlotId,
                FabricMasterNode.SmoothnessSlotId,
                FabricMasterNode.AmbientOcclusionSlotId,
                FabricMasterNode.SpecularColorSlotId,
                FabricMasterNode.DiffusionProfileHashSlotId,
                FabricMasterNode.SubsurfaceMaskSlotId,
                FabricMasterNode.ThicknessSlotId,
                FabricMasterNode.TangentSlotId,
                FabricMasterNode.AnisotropySlotId,
                FabricMasterNode.EmissionSlotId,
                FabricMasterNode.AlphaSlotId,
                FabricMasterNode.AlphaClipThresholdSlotId,
                FabricMasterNode.LightingSlotId,
                FabricMasterNode.BackLightingSlotId,
                FabricMasterNode.DepthOffsetSlotId,
            },
            VertexShaderSlots = new List<int>()
            {
                FabricMasterNode.PositionSlotId,
                FabricMasterNode.VertexNormalSlotId,
                FabricMasterNode.VertexTangentSlotId
            },
            UseInPreview = false
        };

        Pass m_PassRaytracingForward = new Pass()
        {
            Name = "ForwardDXR",
            LightMode = "ForwardDXR",
            TemplateName = "FabricRaytracingPass.template",
            MaterialName = "Fabric",
            ShaderPassName = "SHADERPASS_RAYTRACING_FORWARD",
            ExtraDefines = new List<string>()
            {
                "#pragma multi_compile _ LIGHTMAP_ON",
                "#pragma multi_compile _ DIRLIGHTMAP_COMBINED",
                "#pragma multi_compile _ DYNAMICLIGHTMAP_ON",
                "#define SHADOW_LOW",
            },
            Includes = new List<string>()
            {
                "#include \"Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassRaytracingForward.hlsl\"",
            },
            PixelShaderSlots = new List<int>()
            {
                FabricMasterNode.AlbedoSlotId,
                FabricMasterNode.SpecularOcclusionSlotId,
                FabricMasterNode.NormalSlotId,
                FabricMasterNode.BentNormalSlotId,
                FabricMasterNode.SmoothnessSlotId,
                FabricMasterNode.AmbientOcclusionSlotId,
                FabricMasterNode.SpecularColorSlotId,
                FabricMasterNode.DiffusionProfileHashSlotId,
                FabricMasterNode.SubsurfaceMaskSlotId,
                FabricMasterNode.ThicknessSlotId,
                FabricMasterNode.TangentSlotId,
                FabricMasterNode.AnisotropySlotId,
                FabricMasterNode.EmissionSlotId,
                FabricMasterNode.AlphaSlotId,
                FabricMasterNode.AlphaClipThresholdSlotId,
                FabricMasterNode.LightingSlotId,
                FabricMasterNode.BackLightingSlotId,
                FabricMasterNode.DepthOffsetSlotId,
            },
            VertexShaderSlots = new List<int>()
            {
                FabricMasterNode.PositionSlotId,
                FabricMasterNode.VertexNormalSlotId,
                FabricMasterNode.VertexTangentSlotId
            },
            UseInPreview = false
        };

        Pass m_PassRaytracingGBuffer = new Pass()
        {
            Name = "GBufferDXR",
            LightMode = "GBufferDXR",
            TemplateName = "FabricRaytracingPass.template",
            MaterialName = "Fabric",
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
                FabricMasterNode.AlbedoSlotId,
                FabricMasterNode.SpecularOcclusionSlotId,
                FabricMasterNode.NormalSlotId,
                FabricMasterNode.BentNormalSlotId,
                FabricMasterNode.SmoothnessSlotId,
                FabricMasterNode.AmbientOcclusionSlotId,
                FabricMasterNode.SpecularColorSlotId,
                FabricMasterNode.DiffusionProfileHashSlotId,
                FabricMasterNode.SubsurfaceMaskSlotId,
                FabricMasterNode.ThicknessSlotId,
                FabricMasterNode.TangentSlotId,
                FabricMasterNode.AnisotropySlotId,
                FabricMasterNode.EmissionSlotId,
                FabricMasterNode.AlphaSlotId,
                FabricMasterNode.AlphaClipThresholdSlotId,
                FabricMasterNode.LightingSlotId,
                FabricMasterNode.BackLightingSlotId,
                FabricMasterNode.DepthOffsetSlotId,
            },
            VertexShaderSlots = new List<int>()
            {
                FabricMasterNode.PositionSlotId,
                FabricMasterNode.VertexNormalSlotId,
                FabricMasterNode.VertexTangentSlotId
            },
            UseInPreview = false
        };

        public int GetPreviewPassIndex() { return 0; }

        private static ActiveFields GetActiveFieldsFromMasterNode(AbstractMaterialNode iMasterNode, Pass pass)
        {
            var activeFields = new ActiveFields();
            var baseActiveFields = activeFields.baseInstance;

            FabricMasterNode masterNode = iMasterNode as FabricMasterNode;
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
                    baseActiveFields.Add("FragInputs.isFrontFace");
                }
            }

            switch (masterNode.materialType)
            {
                case FabricMasterNode.MaterialType.CottonWool:
                    baseActiveFields.Add("Material.CottonWool");
                    break;
                case FabricMasterNode.MaterialType.Silk:
                    baseActiveFields.Add("Material.Silk");
                    break;
                default:
                    UnityEngine.Debug.LogError("Unknown material type: " + masterNode.materialType);
                    break;
            }

            if (masterNode.alphaTest.isOn)
            {
                if (pass.PixelShaderUsesSlot(FabricMasterNode.AlphaClipThresholdSlotId))
                {
                    baseActiveFields.Add("AlphaTest");
                }
            }

            if (masterNode.surfaceType != SurfaceType.Opaque)
            {
                if (masterNode.transparencyFog.isOn)
                {
                    baseActiveFields.Add("AlphaFog");
                }

                if (masterNode.blendPreserveSpecular.isOn)
                {
                    baseActiveFields.Add("BlendMode.PreserveSpecular");
                }
            }

            if (!masterNode.receiveDecals.isOn)
            {
                baseActiveFields.Add("DisableDecals");
            }

            if (!masterNode.receiveSSR.isOn)
            {
                baseActiveFields.Add("DisableSSR");
            }

            if (masterNode.addPrecomputedVelocity.isOn)
            {
                baseActiveFields.Add("AddPrecomputedVelocity");
            }

            if (masterNode.energyConservingSpecular.isOn)
            {
                baseActiveFields.Add("Specular.EnergyConserving");
            }

            if (masterNode.transmission.isOn)
            {
                baseActiveFields.Add("Material.Transmission");
            }

            if (masterNode.subsurfaceScattering.isOn && masterNode.surfaceType != SurfaceType.Transparent)
            {
                baseActiveFields.Add("Material.SubsurfaceScattering");
            }

            if (masterNode.IsSlotConnected(FabricMasterNode.BentNormalSlotId) && pass.PixelShaderUsesSlot(FabricMasterNode.BentNormalSlotId))
            {
                baseActiveFields.Add("BentNormal");
            }

            if (masterNode.IsSlotConnected(FabricMasterNode.TangentSlotId) && pass.PixelShaderUsesSlot(FabricMasterNode.TangentSlotId))
            {
                baseActiveFields.Add("Tangent");
            }

            switch (masterNode.specularOcclusionMode)
            {
                case SpecularOcclusionMode.Off:
                    break;
                case SpecularOcclusionMode.FromAO:
                    baseActiveFields.Add("SpecularOcclusionFromAO");
                    break;
                case SpecularOcclusionMode.FromAOAndBentNormal:
                    baseActiveFields.Add("SpecularOcclusionFromAOBentNormal");
                    break;
                case SpecularOcclusionMode.Custom:
                    baseActiveFields.Add("SpecularOcclusionCustom");
                    break;
                default:
                    break;
            }

            if (pass.PixelShaderUsesSlot(FabricMasterNode.AmbientOcclusionSlotId))
            {
                var occlusionSlot = masterNode.FindSlot<Vector1MaterialSlot>(FabricMasterNode.AmbientOcclusionSlotId);

                bool connected = masterNode.IsSlotConnected(FabricMasterNode.AmbientOcclusionSlotId);
                if (connected || occlusionSlot.value != occlusionSlot.defaultValue)
                {
                    baseActiveFields.Add("AmbientOcclusion");
                }
            }

            if (masterNode.IsSlotConnected(FabricMasterNode.LightingSlotId) && pass.PixelShaderUsesSlot(FabricMasterNode.LightingSlotId))
            {
                baseActiveFields.Add("LightingGI");
            }
            if (masterNode.IsSlotConnected(FabricMasterNode.BackLightingSlotId) && pass.PixelShaderUsesSlot(FabricMasterNode.BackLightingSlotId))
            {
                baseActiveFields.Add("BackLightingGI");
            }

            if (masterNode.depthOffset.isOn && pass.PixelShaderUsesSlot(FabricMasterNode.DepthOffsetSlotId))
                baseActiveFields.Add("DepthOffset");

            return activeFields;
        }

        private static bool GenerateShaderPassLit(FabricMasterNode masterNode, Pass pass, GenerationMode mode, ShaderGenerator result, List<string> sourceAssetDependencyPaths)
        {
            if (mode == GenerationMode.ForReals || pass.UseInPreview)
            {
                pass.OnGeneratePass(masterNode);

                // apply master node options to active fields
                var activeFields = GetActiveFieldsFromMasterNode(masterNode, pass);

                // use standard shader pass generation
                bool vertexActive = false;
                if (masterNode.IsSlotConnected(FabricMasterNode.PositionSlotId) ||
                    masterNode.IsSlotConnected(FabricMasterNode.VertexNormalSlotId) ||
                    masterNode.IsSlotConnected(FabricMasterNode.VertexNormalSlotId) )
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
                // FabricSubShader.cs
                sourceAssetDependencyPaths.Add(AssetDatabase.GUIDToAssetPath("059cc3132f0336e40886300f3d2d7f12"));
                // HDSubShaderUtilities.cs
                sourceAssetDependencyPaths.Add(AssetDatabase.GUIDToAssetPath("713ced4e6eef4a44799a4dd59041484b"));
            }

            var masterNode = iMasterNode as FabricMasterNode;

            var subShader = new ShaderGenerator();
            subShader.AddShaderChunk("SubShader", true);
            subShader.AddShaderChunk("{", true);
            subShader.Indent();
            {
                // generate the necessary shader passes
                bool opaque = (masterNode.surfaceType == SurfaceType.Opaque);
                bool transparent = !opaque;

                // Add tags at the SubShader level
                var renderingPass = masterNode.surfaceType == SurfaceType.Opaque ? HDRenderQueue.RenderQueueType.Opaque : HDRenderQueue.RenderQueueType.Transparent;
                int queue = HDRenderQueue.ChangeType(renderingPass, masterNode.sortPriority, masterNode.alphaTest.isOn);
                HDSubShaderUtilities.AddTags(subShader, HDRenderPipeline.k_ShaderTagName, HDRenderTypeTags.HDLitShader, queue);

                GenerateShaderPassLit(masterNode, m_PassShadowCaster, mode, subShader, sourceAssetDependencyPaths);
                GenerateShaderPassLit(masterNode, m_PassMETA, mode, subShader, sourceAssetDependencyPaths);
                GenerateShaderPassLit(masterNode, m_SceneSelectionPass, mode, subShader, sourceAssetDependencyPaths);

                GenerateShaderPassLit(masterNode, m_PassDepthForwardOnly, mode, subShader, sourceAssetDependencyPaths);
                GenerateShaderPassLit(masterNode, m_PassMotionVectors, mode, subShader, sourceAssetDependencyPaths);

                // Assign define here based on opaque or transparent to save some variant
                m_PassForwardOnly.ExtraDefines = opaque ? HDSubShaderUtilities.s_ExtraDefinesForwardOpaque : HDSubShaderUtilities.s_ExtraDefinesForwardTransparent;
                GenerateShaderPassLit(masterNode, m_PassForwardOnly, mode, subShader, sourceAssetDependencyPaths);
            }
            subShader.Deindent();
            subShader.AddShaderChunk("}", true);

            if (mode == GenerationMode.ForReals)
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

            subShader.AddShaderChunk(@"CustomEditor ""UnityEditor.Rendering.HighDefinition.FabricGUI""");

            return subShader.GetShaderString(0);
        }

        public bool IsPipelineCompatible(RenderPipelineAsset renderPipelineAsset)
        {
            return renderPipelineAsset is HDRenderPipelineAsset;
        }
    }
}
