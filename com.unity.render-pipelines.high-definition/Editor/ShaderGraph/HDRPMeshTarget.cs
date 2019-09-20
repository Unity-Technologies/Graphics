using System.Collections.Generic;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEditor.ShaderGraph;
using UnityEditor.ShaderGraph.Internal;
using ShaderPass = UnityEditor.ShaderGraph.Internal.ShaderPass;

namespace UnityEditor.Rendering.HighDefinition
{
    class HDRPMeshTarget : ITargetVariant<MeshTarget>
    {
        public string displayName => "HDRP";
        public string passTemplatePath => string.Empty;
        public string sharedTemplateDirectory => string.Empty;

        public bool Validate(RenderPipelineAsset pipelineAsset)
        {
            return pipelineAsset is HDRenderPipelineAsset;
        }

        public bool TryGetSubShader(IMasterNode masterNode, out ISubShader subShader)
        {
            switch(masterNode)
            {
                case PBRMasterNode pbrMasterNode:
                    subShader = new HDPBRSubShader();
                    return true;
                case UnlitMasterNode unlitMasterNode:
                    subShader = new UnlitSubShader();
                    return true;
                case EyeMasterNode eyeMasterNode:
                    subShader = new EyeSubShader();
                    return true;
                case FabricMasterNode fabricMasterNode:
                    subShader = new FabricSubShader();
                    return true;
                case HairMasterNode hairMasterNode:
                    subShader = new HairSubShader();
                    return true;
                case HDLitMasterNode hdLitMasterNode:
                    subShader = new HDLitSubShader();
                    return true;
                case HDUnlitMasterNode hdUnlitMasterNode:
                    subShader = new HDUnlitSubShader();
                    return true;
                case StackLitMasterNode stackLitMasterNode:
                    subShader = new StackLitSubShader();
                    return true;
                default:
                    subShader = null;
                    return false;
            }
        }

#region Passes
        public static class Passes
        {
            public static ShaderPass UnlitMETA = new ShaderPass()
            {
                // Definition
                displayName = "META",
                referenceName = "SHADERPASS_LIGHT_TRANSPORT",
                lightMode = "META",
                passInclude = "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassLightTransport.hlsl",
                useInPreview = false,

                // Port mask
                pixelPorts = new List<int>
                {
                    UnlitMasterNode.ColorSlotId,
                    UnlitMasterNode.AlphaSlotId,
                    UnlitMasterNode.AlphaThresholdSlotId
                },

                // Render State Overrides
                CullOverride = "Cull Off",

                // Required fields
                requiredAttributes = new List<string>()
                {
                    "AttributesMesh.normalOS",
                    "AttributesMesh.tangentOS",     // Always present as we require it also in case of anisotropic lighting
                    "AttributesMesh.uv0",
                    "AttributesMesh.uv1",
                    "AttributesMesh.color",
                    "AttributesMesh.uv2",           // SHADERPASS_LIGHT_TRANSPORT always uses uv2
                },

                // Pass setup
                pragmas = new List<string>()
                {
                    "#pragma target 4.5",
                    "only_renderers d3d11 ps4 xboxone vulkan metal switch",
                    "multi_compile_instancing"
                },
                includes = new List<string>()
                {
                    "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/FragInputs.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPass.cs.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Material.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Unlit/Unlit.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/BuiltinUtilities.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/MaterialUtilities.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderGraphFunctions.hlsl",
                },

                // Custom template
                passTemplatePath = $"{HDUtils.GetHDRenderPipelinePath()}Editor/Material/Unlit/ShaderGraph/UnlitPass.template",
                sharedTemplateDirectory = $"{HDUtils.GetHDRenderPipelinePath()}Editor/ShaderGraph",
            };

            public static ShaderPass UnlitShadowCaster = new ShaderPass()
            {
                // Definition
                displayName = "ShadowCaster",
                referenceName = "SHADERPASS_SHADOWS",
                lightMode = "ShadowCaster",
                passInclude = "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassDepthOnly.hlsl",
                useInPreview = false,

                // Port mask
                vertexPorts = new List<int>()
                {
                    UnlitMasterNode.PositionSlotId,
                    UnlitMasterNode.VertNormalSlotId,
                    UnlitMasterNode.VertTangentSlotId
                },
                pixelPorts = new List<int>
                {
                    UnlitMasterNode.AlphaSlotId,
                    UnlitMasterNode.AlphaThresholdSlotId
                },

                // Render State Overrides
                ColorMaskOverride = "ColorMask 0",
                ZWriteOverride = "ZWrite On",

                // Pass setup
                pragmas = new List<string>()
                {
                    "#pragma target 4.5",
                    "only_renderers d3d11 ps4 xboxone vulkan metal switch",
                    "multi_compile_instancing"
                },
                includes = new List<string>()
                {
                    "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/FragInputs.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPass.cs.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Material.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Unlit/Unlit.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/BuiltinUtilities.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/MaterialUtilities.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderGraphFunctions.hlsl",
                },

                // Custom template
                passTemplatePath = $"{HDUtils.GetHDRenderPipelinePath()}Editor/Material/Unlit/ShaderGraph/UnlitPass.template",
                sharedTemplateDirectory = $"{HDUtils.GetHDRenderPipelinePath()}Editor/ShaderGraph",
            };

            public static ShaderPass UnlitSceneSelection = new ShaderPass()
            {
                // Definition
                displayName = "SceneSelectionPass",
                referenceName = "SHADERPASS_DEPTH_ONLY",
                lightMode = "SceneSelectionPass",
                passInclude = "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassDepthOnly.hlsl",
                useInPreview = false,

                // Port mask
                vertexPorts = new List<int>()
                {
                    UnlitMasterNode.PositionSlotId,
                    UnlitMasterNode.VertNormalSlotId,
                    UnlitMasterNode.VertTangentSlotId
                },
                pixelPorts = new List<int>
                {
                    UnlitMasterNode.AlphaSlotId,
                    UnlitMasterNode.AlphaThresholdSlotId
                },

                // Render State Overrides
                ColorMaskOverride = "ColorMask 0",

                // Pass setup
                pragmas = new List<string>()
                {
                    "#pragma target 4.5",
                    "only_renderers d3d11 ps4 xboxone vulkan metal switch",
                    "multi_compile_instancing",
                    "editor_sync_compilation"
                },
                includes = new List<string>()
                {
                    "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/FragInputs.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPass.cs.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Material.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Unlit/Unlit.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/BuiltinUtilities.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/MaterialUtilities.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderGraphFunctions.hlsl",
                },
                defines = new List<string>()
                {
                    "SCENESELECTIONPASS"
                },

                // Custom template
                passTemplatePath = $"{HDUtils.GetHDRenderPipelinePath()}Editor/Material/Unlit/ShaderGraph/UnlitPass.template",
                sharedTemplateDirectory = $"{HDUtils.GetHDRenderPipelinePath()}Editor/ShaderGraph",
            }; 

            public static ShaderPass UnlitDepthForwardOnly = new ShaderPass()
            {
                // Definition
                displayName = "DepthForwardOnly",
                referenceName = "SHADERPASS_DEPTH_ONLY",
                lightMode = "DepthForwardOnly",
                passInclude = "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassDepthOnly.hlsl",
                useInPreview = false,

                // Port mask
                vertexPorts = new List<int>()
                {
                    UnlitMasterNode.PositionSlotId,
                    UnlitMasterNode.VertNormalSlotId,
                    UnlitMasterNode.VertTangentSlotId
                },
                pixelPorts = new List<int>
                {
                    UnlitMasterNode.AlphaSlotId,
                    UnlitMasterNode.AlphaThresholdSlotId
                },

                // Render State Overrides
                // Caution: When using MSAA we have normal and depth buffer bind.
                // Mean unlit object need to not write in it (or write 0) - Disable color mask for this RT
                // This is not a problem in no MSAA mode as there is no buffer bind
                ColorMaskOverride = "ColorMask 0 0",
                ZWriteOverride = "ZWrite On",

                // Pass setup
                pragmas = new List<string>()
                {
                    "#pragma target 4.5",
                    "only_renderers d3d11 ps4 xboxone vulkan metal switch",
                    "multi_compile_instancing"
                },
                includes = new List<string>()
                {
                    "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/FragInputs.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPass.cs.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Material.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Unlit/Unlit.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/BuiltinUtilities.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/MaterialUtilities.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderGraphFunctions.hlsl",
                },
                keywords = new List<KeywordDescriptor>()
                {
                    // Note we don't need to define WRITE_NORMAL_BUFFER
                    Keywords.WriteMsaaDepth
                },

                // Custom template
                passTemplatePath = $"{HDUtils.GetHDRenderPipelinePath()}Editor/Material/Unlit/ShaderGraph/UnlitPass.template",
                sharedTemplateDirectory = $"{HDUtils.GetHDRenderPipelinePath()}Editor/ShaderGraph",
            };

            public static ShaderPass UnlitMotionVectors = new ShaderPass()
            {
                // Definition
                displayName = "MotionVectors",
                referenceName = "SHADERPASS_MOTION_VECTORS",
                lightMode = "MotionVectors",
                passInclude = "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassMotionVectors.hlsl",
                useInPreview = false,

                // Port mask
                vertexPorts = new List<int>()
                {
                    UnlitMasterNode.PositionSlotId,
                    UnlitMasterNode.VertNormalSlotId,
                    UnlitMasterNode.VertTangentSlotId
                },
                pixelPorts = new List<int>
                {
                    UnlitMasterNode.AlphaSlotId,
                    UnlitMasterNode.AlphaThresholdSlotId
                },

                // Render State Overrides
                // Caution: When using MSAA we have motion vector, normal and depth buffer bind.
                // Mean unlit object need to not write in it (or write 0) - Disable color mask for this RT
                // This is not a problem in no MSAA mode as there is no buffer bind
                ColorMaskOverride = "ColorMask 0 1",
                StencilOverride = new List<string>()
                {
                    "Stencil",
                    "{",
                    $"    WriteMask {(int)HDRenderPipeline.StencilBitMask.ObjectMotionVectors}",
                    $"    Ref  {(int)HDRenderPipeline.StencilBitMask.ObjectMotionVectors}",
                    "    Comp Always",
                    "    Pass Replace",
                    "}"
                },

                // Required fields
                requiredVaryings = new List<string>()
                {
                    "VaryingsMeshToPS.positionRWS",
                },

                // Pass setup
                pragmas = new List<string>()
                {
                    "#pragma target 4.5",
                    "only_renderers d3d11 ps4 xboxone vulkan metal switch",
                    "multi_compile_instancing"
                },
                includes = new List<string>()
                {
                    "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/FragInputs.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPass.cs.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Material.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Unlit/Unlit.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/BuiltinUtilities.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/MaterialUtilities.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderGraphFunctions.hlsl",
                },
                keywords = new List<KeywordDescriptor>()
                {
                    // Note we don't need to define WRITE_NORMAL_BUFFER
                    Keywords.WriteMsaaDepth
                },

                // Custom template
                passTemplatePath = $"{HDUtils.GetHDRenderPipelinePath()}Editor/Material/Unlit/ShaderGraph/UnlitPass.template",
                sharedTemplateDirectory = $"{HDUtils.GetHDRenderPipelinePath()}Editor/ShaderGraph",
            };

            public static ShaderPass UnlitForwardOnly = new ShaderPass()
            {
                // Definition
                displayName = "ForwardOnly",
                referenceName = "SHADERPASS_FORWARD_UNLIT",
                lightMode = "ForwardOnly",
                passInclude = "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassForwardUnlit.hlsl",
                useInPreview = true,

                // Port mask
                vertexPorts = new List<int>()
                {
                    UnlitMasterNode.PositionSlotId,
                    UnlitMasterNode.VertNormalSlotId,
                    UnlitMasterNode.VertTangentSlotId
                },
                pixelPorts = new List<int>
                {
                    UnlitMasterNode.ColorSlotId,
                    UnlitMasterNode.AlphaSlotId,
                    UnlitMasterNode.AlphaThresholdSlotId
                },

                // Render State Overrides
                StencilOverride = new List<string>()
                {
                    "Stencil",
                    "{",
                    $"    WriteMask {(int) HDRenderPipeline.StencilBitMask.LightingMask}",
                    $"    Ref  {(int)StencilLightingUsage.NoLighting}",
                    "    Comp Always",
                    "    Pass Replace",
                    "}"
                },

                // Pass setup
                pragmas = new List<string>()
                {
                    "#pragma target 4.5",
                    "only_renderers d3d11 ps4 xboxone vulkan metal switch",
                    "multi_compile_instancing"
                },
                includes = new List<string>()
                {
                    "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/FragInputs.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPass.cs.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Material.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Unlit/Unlit.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/BuiltinUtilities.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/MaterialUtilities.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderGraphFunctions.hlsl",
                },
                keywords = new List<KeywordDescriptor>()
                {
                    Keywords.DebugDisplay
                },

                // Custom template
                passTemplatePath = $"{HDUtils.GetHDRenderPipelinePath()}Editor/Material/Unlit/ShaderGraph/UnlitPass.template",
                sharedTemplateDirectory = $"{HDUtils.GetHDRenderPipelinePath()}Editor/ShaderGraph",
            };

            public static ShaderPass PBRGBuffer = new ShaderPass()
            {
                // Definition
                displayName = "GBuffer",
                referenceName = "SHADERPASS_GBUFFER",
                lightMode = "GBuffer",
                passInclude = "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassGBuffer.hlsl",
                useInPreview = true,

                // Port mask
                vertexPorts = new List<int>()
                {
                    PBRMasterNode.PositionSlotId,
                    PBRMasterNode.VertNormalSlotId,
                    PBRMasterNode.VertTangentSlotId
                },
                pixelPorts = new List<int>
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

                // Render state overrides
                StencilOverride = new List<string>
                {
                    "Stencil",
                    "{",
                    $"    WriteMask {(int)HDRenderPipeline.StencilBitMask.LightingMask | (int)HDRenderPipeline.StencilBitMask.DoesntReceiveSSR | (int)HDRenderPipeline.StencilBitMask.DecalsForwardOutputNormalBuffer}",
                    $"    Ref  {(int)StencilLightingUsage.RegularLighting}",
                    "    Comp Always",
                    "    Pass Replace",
                    "}"
                },

                // Required fields
                requiredVaryings = new List<string>()
                {
                    "FragInputs.tangentToWorld",
                    "FragInputs.positionRWS",
                    "FragInputs.texCoord1",
                    "FragInputs.texCoord2"
                },

                // Pass setup
                pragmas = new List<string>()
                {
                    "#pragma target 4.5",
                    "only_renderers d3d11 ps4 xboxone vulkan metal switch",
                    "multi_compile_instancing",
                    "instancing_options renderinglayer",
                },
                includes = new List<string>()
                {
                    "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl",
                    "Packages/com.unity.render-pipelines.core/ShaderLibrary/NormalSurfaceGradient.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/FragInputs.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPass.cs.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Material.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Lit/Lit.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/BuiltinUtilities.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/MaterialUtilities.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Decal/DecalUtilities.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Lit/LitDecalData.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderGraphFunctions.hlsl",
                },
                keywords = new List<KeywordDescriptor>()
                {
                    Keywords.LodFadeCrossfade,
                    Keywords.DebugDisplay,
                    Keywords.Lightmap,
                    Keywords.DirectionalLightmapCombined,
                    Keywords.DynamicLightmap,
                    Keywords.ShadowsShadowmask,
                    Keywords.LightLayers,
                    Keywords.Decals,
                },

                // Custom template
                passTemplatePath = $"{HDUtils.GetHDRenderPipelinePath()}Editor/Material/PBR/ShaderGraph/HDPBRPass.template",
                sharedTemplateDirectory = $"{HDUtils.GetHDRenderPipelinePath()}Editor/ShaderGraph",
            };

            public static ShaderPass PBRMETA = new ShaderPass()
            {
                // Definition
                displayName = "META",
                referenceName = "SHADERPASS_LIGHT_TRANSPORT",
                lightMode = "META",
                passInclude = "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassLightTransport.hlsl",
                useInPreview = false,

                // Port mask
                pixelPorts = new List<int>
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

                // Render State Overrides
                CullOverride = "Cull Off",

                // Required fields
                requiredAttributes = new List<string>()
                {
                    "AttributesMesh.normalOS",
                    "AttributesMesh.tangentOS",     // Always present as we require it also in case of anisotropic lighting
                    "AttributesMesh.uv0",
                    "AttributesMesh.uv1",
                    "AttributesMesh.color",
                    "AttributesMesh.uv2",           // SHADERPASS_LIGHT_TRANSPORT always uses uv2
                },

                // Pass setup
                pragmas = new List<string>()
                {
                    "#pragma target 4.5",
                    "only_renderers d3d11 ps4 xboxone vulkan metal switch",
                    "multi_compile_instancing",
                    "instancing_options renderinglayer",
                },
                includes = new List<string>()
                {
                    "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl",
                    "Packages/com.unity.render-pipelines.core/ShaderLibrary/NormalSurfaceGradient.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/FragInputs.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPass.cs.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Material.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Lit/Lit.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/BuiltinUtilities.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/MaterialUtilities.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Decal/DecalUtilities.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Lit/LitDecalData.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderGraphFunctions.hlsl",
                },
                keywords = new List<KeywordDescriptor>()
                {
                    Keywords.LodFadeCrossfade,
                },

                // Custom template
                passTemplatePath = $"{HDUtils.GetHDRenderPipelinePath()}Editor/Material/PBR/ShaderGraph/HDPBRPass.template",
                sharedTemplateDirectory = $"{HDUtils.GetHDRenderPipelinePath()}Editor/ShaderGraph",
            };

            public static ShaderPass PBRShadowCaster = new ShaderPass()
            {
                // Definition
                displayName = "ShadowCaster",
                referenceName = "SHADERPASS_SHADOWS",
                lightMode = "ShadowCaster",
                passInclude = "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassDepthOnly.hlsl",
                useInPreview = false,

                // Port mask
                vertexPorts = new List<int>()
                {
                    PBRMasterNode.PositionSlotId,
                    PBRMasterNode.VertNormalSlotId,
                    PBRMasterNode.VertTangentSlotId
                },
                pixelPorts = new List<int>
                {
                    PBRMasterNode.AlphaSlotId,
                    PBRMasterNode.AlphaThresholdSlotId
                },

                // Render State Overrides
                BlendOverride = "Blend One Zero",
                ZWriteOverride = "ZWrite On",
                ColorMaskOverride = "ColorMask 0",

                // Pass setup
                pragmas = new List<string>()
                {
                    "#pragma target 4.5",
                    "only_renderers d3d11 ps4 xboxone vulkan metal switch",
                    "multi_compile_instancing",
                    "instancing_options renderinglayer",
                },
                includes = new List<string>()
                {
                    "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl",
                    "Packages/com.unity.render-pipelines.core/ShaderLibrary/NormalSurfaceGradient.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/FragInputs.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPass.cs.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Material.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Lit/Lit.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/BuiltinUtilities.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/MaterialUtilities.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Decal/DecalUtilities.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Lit/LitDecalData.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderGraphFunctions.hlsl",
                },
                keywords = new List<KeywordDescriptor>()
                {
                    Keywords.LodFadeCrossfade,
                },

                // Custom template
                passTemplatePath = $"{HDUtils.GetHDRenderPipelinePath()}Editor/Material/PBR/ShaderGraph/HDPBRPass.template",
                sharedTemplateDirectory = $"{HDUtils.GetHDRenderPipelinePath()}Editor/ShaderGraph",
            };

            public static ShaderPass PBRSceneSelection = new ShaderPass()
            {
                // Definition
                displayName = "SceneSelectionPass",
                referenceName = "SHADERPASS_DEPTH_ONLY",
                lightMode = "SceneSelectionPass",
                passInclude = "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassDepthOnly.hlsl",
                useInPreview = false,

                // Port mask
                vertexPorts = new List<int>()
                {
                    PBRMasterNode.PositionSlotId,
                    PBRMasterNode.VertNormalSlotId,
                    PBRMasterNode.VertTangentSlotId
                },
                pixelPorts = new List<int>
                {
                    PBRMasterNode.AlphaSlotId,
                    PBRMasterNode.AlphaThresholdSlotId
                },

                // Render State Overrides
                ColorMaskOverride = "ColorMask 0",

                // Pass setup
                pragmas = new List<string>()
                {
                    "#pragma target 4.5",
                    "only_renderers d3d11 ps4 xboxone vulkan metal switch",
                    "multi_compile_instancing",
                    "instancing_options renderinglayer",
                    "editor_sync_compilation",
                },
                defines = new List<string>()
                {
                    "SCENESELECTIONPASS",
                },
                includes = new List<string>()
                {
                    "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl",
                    "Packages/com.unity.render-pipelines.core/ShaderLibrary/NormalSurfaceGradient.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/FragInputs.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPass.cs.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Material.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Lit/Lit.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/BuiltinUtilities.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/MaterialUtilities.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Decal/DecalUtilities.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Lit/LitDecalData.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderGraphFunctions.hlsl",
                },
                keywords = new List<KeywordDescriptor>()
                {
                    Keywords.LodFadeCrossfade,
                },

                // Custom template
                passTemplatePath = $"{HDUtils.GetHDRenderPipelinePath()}Editor/Material/PBR/ShaderGraph/HDPBRPass.template",
                sharedTemplateDirectory = $"{HDUtils.GetHDRenderPipelinePath()}Editor/ShaderGraph",
            };

            public static ShaderPass PBRDepthOnly = new ShaderPass()
            {
                // Definition
                displayName = "DepthOnly",
                referenceName = "SHADERPASS_DEPTH_ONLY",
                lightMode = "DepthOnly",
                passInclude = "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassDepthOnly.hlsl",
                useInPreview = true,

                // Port mask
                vertexPorts = new List<int>()
                {
                    PBRMasterNode.PositionSlotId,
                    PBRMasterNode.VertNormalSlotId,
                    PBRMasterNode.VertTangentSlotId
                },
                pixelPorts = new List<int>
                {
                    PBRMasterNode.NormalSlotId,
                    PBRMasterNode.SmoothnessSlotId,
                    PBRMasterNode.AlphaSlotId,
                    PBRMasterNode.AlphaThresholdSlotId
                },

                // Render State Overrides
                ZWriteOverride = "ZWrite On",
                StencilOverride = new List<string>
                {
                    "Stencil",
                    "{",
                    $"    WriteMask {(int)HDRenderPipeline.StencilBitMask.DecalsForwardOutputNormalBuffer | (int)HDRenderPipeline.StencilBitMask.DoesntReceiveSSR}",
                    $"    Ref  0",
                    "    Comp Always",
                    "    Pass Replace",
                    "}"
                },

                // Required fields
                requiredAttributes = new List<string>()
                {
                    "AttributesMesh.normalOS",
                    "AttributesMesh.tangentOS",     // Always present as we require it also in case of Variants lighting
                    "AttributesMesh.uv0",
                    "AttributesMesh.uv1",
                    "AttributesMesh.color",
                    "AttributesMesh.uv2",           // SHADERPASS_LIGHT_TRANSPORT always uses uv2
                    "AttributesMesh.uv3",           // DEBUG_DISPLAY
                },
                requiredVaryings = new List<string>()
                {
                    "FragInputs.tangentToWorld",
                    "FragInputs.positionRWS",
                    "FragInputs.texCoord0",
                    "FragInputs.texCoord1",
                    "FragInputs.texCoord2",
                    "FragInputs.texCoord3",
                    "FragInputs.color",
                },

                // Pass setup
                pragmas = new List<string>()
                {
                    "#pragma target 4.5",
                    "only_renderers d3d11 ps4 xboxone vulkan metal switch",
                    "multi_compile_instancing",
                    "instancing_options renderinglayer",
                },
                includes = new List<string>()
                {
                    "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl",
                    "Packages/com.unity.render-pipelines.core/ShaderLibrary/NormalSurfaceGradient.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/FragInputs.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPass.cs.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Material.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Lit/Lit.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/BuiltinUtilities.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/MaterialUtilities.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Decal/DecalUtilities.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Lit/LitDecalData.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderGraphFunctions.hlsl",
                },
                defines = new List<string>()
                {
                    "RAYTRACING_SHADER_GRAPH_HIGH",
                },
                keywords = new List<KeywordDescriptor>()
                {
                    Keywords.WriteMsaaDepth,
                    Keywords.WriteNormalBuffer,
                    Keywords.LodFadeCrossfade,
                },

                // Custom template
                passTemplatePath = $"{HDUtils.GetHDRenderPipelinePath()}Editor/Material/PBR/ShaderGraph/HDPBRPass.template",
                sharedTemplateDirectory = $"{HDUtils.GetHDRenderPipelinePath()}Editor/ShaderGraph",
            };

            public static ShaderPass PBRMotionVectors = new ShaderPass()
            {
                // Definition
                displayName = "MotionVectors",
                referenceName = "SHADERPASS_MOTION_VECTORS",
                lightMode = "MotionVectors",
                passInclude = "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassMotionVectors.hlsl",
                useInPreview = false,

                // Port mask
                vertexPorts = new List<int>()
                {
                    PBRMasterNode.PositionSlotId,
                    PBRMasterNode.VertNormalSlotId,
                    PBRMasterNode.VertTangentSlotId
                },
                pixelPorts = new List<int>
                {
                    PBRMasterNode.NormalSlotId,
                    PBRMasterNode.SmoothnessSlotId,
                    PBRMasterNode.AlphaSlotId,
                    PBRMasterNode.AlphaThresholdSlotId
                },

                // Render State Overrides
                BlendOverride = "Blend One Zero",
                ZWriteOverride = "ZWrite On",
                ColorMaskOverride = "ColorMask 0",
                StencilOverride = new List<string>
                {
                    "Stencil",
                    "{",
                    $"    WriteMask {(int)HDRenderPipeline.StencilBitMask.DecalsForwardOutputNormalBuffer | (int)HDRenderPipeline.StencilBitMask.DoesntReceiveSSR | (int)HDRenderPipeline.StencilBitMask.ObjectMotionVectors}",
                    $"    Ref  {(int)HDRenderPipeline.StencilBitMask.ObjectMotionVectors}",
                    "    Comp Always",
                    "    Pass Replace",
                    "}"
                },

                // Required fields
                requiredVaryings = new List<string>()
                {
                    "FragInputs.positionRWS",
                },

                // Pass setup
                pragmas = new List<string>()
                {
                    "#pragma target 4.5",
                    "only_renderers d3d11 ps4 xboxone vulkan metal switch",
                    "multi_compile_instancing",
                    "instancing_options renderinglayer",
                },
                includes = new List<string>()
                {
                    "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl",
                    "Packages/com.unity.render-pipelines.core/ShaderLibrary/NormalSurfaceGradient.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/FragInputs.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPass.cs.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Material.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Lit/Lit.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/BuiltinUtilities.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/MaterialUtilities.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Decal/DecalUtilities.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Lit/LitDecalData.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderGraphFunctions.hlsl",
                },
                defines = new List<string>()
                {
                    "RAYTRACING_SHADER_GRAPH_HIGH",
                },
                keywords = new List<KeywordDescriptor>()
                {
                    Keywords.WriteMsaaDepth,
                    Keywords.WriteNormalBuffer,
                    Keywords.LodFadeCrossfade,
                },

                // Custom template
                passTemplatePath = $"{HDUtils.GetHDRenderPipelinePath()}Editor/Material/PBR/ShaderGraph/HDPBRPass.template",
                sharedTemplateDirectory = $"{HDUtils.GetHDRenderPipelinePath()}Editor/ShaderGraph",
            };

            public static ShaderPass PBRForwardOpaque = new ShaderPass()
            {
                // Definition
                displayName = "Forward",
                referenceName = "SHADERPASS_FORWARD",
                lightMode = "Forward",
                passInclude = "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassForward.hlsl",
                useInPreview = true,

                // Port mask
                vertexPorts = new List<int>()
                {
                    PBRMasterNode.PositionSlotId,
                    PBRMasterNode.VertNormalSlotId,
                    PBRMasterNode.VertTangentSlotId
                },
                pixelPorts = new List<int>
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

                // Render State Overrides
                StencilOverride = new List<string>
                {
                    "Stencil",
                    "{",
                    $"    WriteMask {(int)HDRenderPipeline.StencilBitMask.LightingMask}",
                    $"    Ref  {(int)StencilLightingUsage.RegularLighting}",
                    "    Comp Always",
                    "    Pass Replace",
                    "}"
                },

                // Required fields
                requiredVaryings = new List<string>()
                {
                    "FragInputs.tangentToWorld",
                    "FragInputs.positionRWS",	// NOTE : world-space pos is necessary for any lighting pass
                    "FragInputs.texCoord1",
                    "FragInputs.texCoord2"
                },

                // Pass setup
                pragmas = new List<string>()
                {
                    "#pragma target 4.5",
                    "only_renderers d3d11 ps4 xboxone vulkan metal switch",
                    "multi_compile_instancing",
                    "instancing_options renderinglayer",
                },
                defines = new List<string>()
                {
                    "HAS_LIGHTLOOP",
                },
                includes = new List<string>()
                {
                    "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl",
                    "Packages/com.unity.render-pipelines.core/ShaderLibrary/NormalSurfaceGradient.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/FragInputs.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPass.cs.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Material.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/Lighting.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/LightLoop/LightLoopDef.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Lit/Lit.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/LightLoop/LightLoop.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/BuiltinUtilities.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/MaterialUtilities.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Decal/DecalUtilities.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Lit/LitDecalData.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderGraphFunctions.hlsl",
                },
                keywords = new List<KeywordDescriptor>()
                {
                    Keywords.LodFadeCrossfade,
                    Keywords.DebugDisplay,
                    Keywords.Lightmap,
                    Keywords.DirectionalLightmapCombined,
                    Keywords.DynamicLightmap,
                    Keywords.ShadowsShadowmask,
                    Keywords.Decals,
                    Keywords.LightList,
                    Keywords.Shadow,
                },

                // Custom template
                passTemplatePath = $"{HDUtils.GetHDRenderPipelinePath()}Editor/Material/PBR/ShaderGraph/HDPBRPass.template",
                sharedTemplateDirectory = $"{HDUtils.GetHDRenderPipelinePath()}Editor/ShaderGraph",
            };

            public static ShaderPass PBRForwardTransparent = new ShaderPass()
            {
                // Definition
                displayName = "Forward",
                referenceName = "SHADERPASS_FORWARD",
                lightMode = "Forward",
                passInclude = "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassForward.hlsl",
                useInPreview = true,

                // Port mask
                vertexPorts = new List<int>()
                {
                    PBRMasterNode.PositionSlotId,
                    PBRMasterNode.VertNormalSlotId,
                    PBRMasterNode.VertTangentSlotId
                },
                pixelPorts = new List<int>
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

                // Render State Overrides
                StencilOverride = new List<string>
                {
                    "Stencil",
                    "{",
                    $"    WriteMask {(int)HDRenderPipeline.StencilBitMask.LightingMask}",
                    $"    Ref  {(int)StencilLightingUsage.RegularLighting}",
                    "    Comp Always",
                    "    Pass Replace",
                    "}"
                },

                // Required fields
                requiredVaryings = new List<string>()
                {
                    "FragInputs.tangentToWorld",
                    "FragInputs.positionRWS",	// NOTE : world-space pos is necessary for any lighting pass
                    "FragInputs.texCoord1",
                    "FragInputs.texCoord2"
                },

                // Pass setup
                pragmas = new List<string>()
                {
                    "#pragma target 4.5",
                    "only_renderers d3d11 ps4 xboxone vulkan metal switch",
                    "multi_compile_instancing",
                    "instancing_options renderinglayer",
                },
                defines = new List<string>()
                {
                    "HAS_LIGHTLOOP",
                    "USE_CLUSTERED_LIGHTLIST",
                    "RAYTRACING_SHADER_GRAPH_HIGH",
                },
                includes = new List<string>()
                {
                    "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl",
                    "Packages/com.unity.render-pipelines.core/ShaderLibrary/NormalSurfaceGradient.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/FragInputs.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPass.cs.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Material.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/Lighting.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/LightLoop/LightLoopDef.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Lit/Lit.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/LightLoop/LightLoop.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/BuiltinUtilities.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/MaterialUtilities.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Decal/DecalUtilities.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Lit/LitDecalData.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderGraphFunctions.hlsl",
                },
                keywords = new List<KeywordDescriptor>()
                {
                    Keywords.LodFadeCrossfade,
                    Keywords.DebugDisplay,
                    Keywords.Lightmap,
                    Keywords.DirectionalLightmapCombined,
                    Keywords.DynamicLightmap,
                    Keywords.ShadowsShadowmask,
                    Keywords.Decals,
                    Keywords.Shadow,
                },

                // Custom template
                passTemplatePath = $"{HDUtils.GetHDRenderPipelinePath()}Editor/Material/PBR/ShaderGraph/HDPBRPass.template",
                sharedTemplateDirectory = $"{HDUtils.GetHDRenderPipelinePath()}Editor/ShaderGraph",
            };

            public static ShaderPass HDLitGBuffer = new ShaderPass()
            {
                // Definition
                displayName = "GBuffer",
                referenceName = "SHADERPASS_GBUFFER",
                lightMode = "GBuffer",
                passInclude = "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassGBuffer.hlsl",
                useInPreview = true,

                // Port mask
                vertexPorts = new List<int>()
                {
                    HDLitMasterNode.PositionSlotId,
                    HDLitMasterNode.VertexNormalSlotID,
                    HDLitMasterNode.VertexTangentSlotID
                },
                pixelPorts = new List<int>
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

                // Render state overrides
                ZTestOverride = HDSubShaderUtilities.zTestGBuffer,
                CullOverride = HDSubShaderUtilities.defaultCullMode,
                StencilOverride = new List<string>()
                {
                    "Stencil",
                    "{",
                    "    WriteMask [_StencilWriteMaskGBuffer]",
                    "    Ref [_StencilRefGBuffer]",
                    "    Comp Always",
                    "    Pass Replace",
                    "}",
                },

                // Required fields
                requiredVaryings = new List<string>()
                {
                    "FragInputs.tangentToWorld",
                    "FragInputs.positionRWS",
                    "FragInputs.texCoord1",
                    "FragInputs.texCoord2"
                },

                // Pass setup
                pragmas = new List<string>()
                {
                    "#pragma target 4.5",
                    "only_renderers d3d11 ps4 xboxone vulkan metal switch",
                    "multi_compile_instancing",
                },
                defines = new List<string>()
                {
                    "RAYTRACING_SHADER_GRAPH_HIGH",
                },
                includes = new List<string>()
                {
                    "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl",
                    "Packages/com.unity.render-pipelines.core/ShaderLibrary/NormalSurfaceGradient.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/FragInputs.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPass.cs.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Material.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Lit/Lit.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/BuiltinUtilities.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/MaterialUtilities.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Decal/DecalUtilities.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Lit/LitDecalData.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderGraphFunctions.hlsl",
                },
                keywords = new List<KeywordDescriptor>()
                {
                    Keywords.LodFadeCrossfade,
                    Keywords.SurfaceTypeTransparent,
                    Keywords.DoubleSided,
                    Keywords.BlendMode,
                    Keywords.DebugDisplay,
                    Keywords.Lightmap,
                    Keywords.DirectionalLightmapCombined,
                    Keywords.DynamicLightmap,
                    Keywords.ShadowsShadowmask,
                    Keywords.LightLayers,
                    Keywords.Decals,
                },

                // Custom template
                passTemplatePath = $"{HDUtils.GetHDRenderPipelinePath()}Editor/Material/Lit/ShaderGraph/HDLitPass.template",
                sharedTemplateDirectory = $"{HDUtils.GetHDRenderPipelinePath()}Editor/ShaderGraph",
            };

            public static ShaderPass HDLitMETA = new ShaderPass()
            {
                // Definition
                displayName = "META",
                referenceName = "SHADERPASS_LIGHT_TRANSPORT",
                lightMode = "META",
                passInclude = "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassLightTransport.hlsl",
                useInPreview = false,

                // Port mask
                pixelPorts = new List<int>
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

                // Render State Overrides
                CullOverride = "Cull Off",

                // Required fields
                requiredAttributes = new List<string>()
                {
                    "AttributesMesh.normalOS",
                    "AttributesMesh.tangentOS",     // Always present as we require it also in case of anisotropic lighting
                    "AttributesMesh.uv0",
                    "AttributesMesh.uv1",
                    "AttributesMesh.color",
                    "AttributesMesh.uv2",           // SHADERPASS_LIGHT_TRANSPORT always uses uv2
                },

                // Pass setup
                pragmas = new List<string>()
                {
                    "#pragma target 4.5",
                    "only_renderers d3d11 ps4 xboxone vulkan metal switch",
                    "multi_compile_instancing",
                },
                defines = new List<string>()
                {
                    "RAYTRACING_SHADER_GRAPH_HIGH",
                },
                includes = new List<string>()
                {
                    "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl",
                    "Packages/com.unity.render-pipelines.core/ShaderLibrary/NormalSurfaceGradient.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/FragInputs.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPass.cs.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Material.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Lit/Lit.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/BuiltinUtilities.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/MaterialUtilities.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Decal/DecalUtilities.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Lit/LitDecalData.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderGraphFunctions.hlsl",
                },
                keywords = new List<KeywordDescriptor>()
                {
                    Keywords.LodFadeCrossfade,
                    Keywords.SurfaceTypeTransparent,
                    Keywords.DoubleSided,
                    Keywords.BlendMode,
                },

                // Custom template
                passTemplatePath = $"{HDUtils.GetHDRenderPipelinePath()}Editor/Material/Lit/ShaderGraph/HDLitPass.template",
                sharedTemplateDirectory = $"{HDUtils.GetHDRenderPipelinePath()}Editor/ShaderGraph",
            };

            public static ShaderPass HDLitShadowCaster = new ShaderPass()
            {
                // Definition
                displayName = "ShadowCaster",
                referenceName = "SHADERPASS_SHADOWS",
                lightMode = "ShadowCaster",
                passInclude = "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassDepthOnly.hlsl",
                useInPreview = false,

                // Port mask
                vertexPorts = new List<int>()
                {
                    HDLitMasterNode.PositionSlotId,
                    HDLitMasterNode.VertexNormalSlotID,
                    HDLitMasterNode.VertexTangentSlotID
                },
                pixelPorts = new List<int>
                {
                    HDLitMasterNode.AlphaSlotId,
                    HDLitMasterNode.AlphaThresholdSlotId,
                    HDLitMasterNode.AlphaThresholdShadowSlotId,
                    HDLitMasterNode.DepthOffsetSlotId,
                },

                // Render State Overrides
                ZClipOverride = HDSubShaderUtilities.zClipShadowCaster,
                CullOverride = HDSubShaderUtilities.defaultCullMode,
                ZWriteOverride = HDSubShaderUtilities.zWriteOn,
                ColorMaskOverride = "ColorMask 0",

                // Pass setup
                pragmas = new List<string>()
                {
                    "#pragma target 4.5",
                    "only_renderers d3d11 ps4 xboxone vulkan metal switch",
                    "multi_compile_instancing",
                },
                defines = new List<string>()
                {
                    "RAYTRACING_SHADER_GRAPH_HIGH",
                },
                includes = new List<string>()
                {
                    "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl",
                    "Packages/com.unity.render-pipelines.core/ShaderLibrary/NormalSurfaceGradient.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/FragInputs.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPass.cs.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Material.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Lit/Lit.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/BuiltinUtilities.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/MaterialUtilities.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Decal/DecalUtilities.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Lit/LitDecalData.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderGraphFunctions.hlsl",
                },
                keywords = new List<KeywordDescriptor>()
                {
                    Keywords.LodFadeCrossfade,
                    Keywords.SurfaceTypeTransparent,
                    Keywords.DoubleSided,
                    Keywords.BlendMode,
                },

                // Custom template
                passTemplatePath = $"{HDUtils.GetHDRenderPipelinePath()}Editor/Material/Lit/ShaderGraph/HDLitPass.template",
                sharedTemplateDirectory = $"{HDUtils.GetHDRenderPipelinePath()}Editor/ShaderGraph",
            };

            public static ShaderPass HDLitSceneSelection = new ShaderPass()
            {
                // Definition
                displayName = "SceneSelectionPass",
                referenceName = "SHADERPASS_DEPTH_ONLY",
                lightMode = "SceneSelectionPass",
                passInclude = "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassDepthOnly.hlsl",
                useInPreview = false,

                // Port mask
                vertexPorts = new List<int>()
                {
                    HDLitMasterNode.PositionSlotId,
                    HDLitMasterNode.VertexNormalSlotID,
                    HDLitMasterNode.VertexTangentSlotID
                },
                pixelPorts = new List<int>
                {
                    HDLitMasterNode.AlphaSlotId,
                    HDLitMasterNode.AlphaThresholdSlotId,
                    HDLitMasterNode.DepthOffsetSlotId,
                },

                // Render State Overrides
                ColorMaskOverride = "ColorMask 0",

                // Pass setup
                pragmas = new List<string>()
                {
                    "#pragma target 4.5",
                    "only_renderers d3d11 ps4 xboxone vulkan metal switch",
                    "multi_compile_instancing",
                    "editor_sync_compilation",
                },
                defines = new List<string>()
                {
                    "SCENESELECTIONPASS",
                },
                includes = new List<string>()
                {
                    "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl",
                    "Packages/com.unity.render-pipelines.core/ShaderLibrary/NormalSurfaceGradient.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/FragInputs.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPass.cs.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Material.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Lit/Lit.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/BuiltinUtilities.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/MaterialUtilities.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Decal/DecalUtilities.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Lit/LitDecalData.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderGraphFunctions.hlsl",
                },
                keywords = new List<KeywordDescriptor>()
                {
                    Keywords.LodFadeCrossfade,
                    Keywords.SurfaceTypeTransparent,
                    Keywords.DoubleSided,
                    Keywords.BlendMode,
                },

                // Custom template
                passTemplatePath = $"{HDUtils.GetHDRenderPipelinePath()}Editor/Material/Lit/ShaderGraph/HDLitPass.template",
                sharedTemplateDirectory = $"{HDUtils.GetHDRenderPipelinePath()}Editor/ShaderGraph",
            };

            public static ShaderPass HDLitDepthOnly = new ShaderPass()
            {
                // Definition
                displayName = "DepthOnly",
                referenceName = "SHADERPASS_DEPTH_ONLY",
                lightMode = "DepthOnly",
                passInclude = "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassDepthOnly.hlsl",
                useInPreview = true,

                // Port mask
                vertexPorts = new List<int>()
                {
                    HDLitMasterNode.PositionSlotId,
                    HDLitMasterNode.VertexNormalSlotID,
                    HDLitMasterNode.VertexTangentSlotID
                },
                pixelPorts = new List<int>
                {
                    HDLitMasterNode.NormalSlotId,
                    HDLitMasterNode.SmoothnessSlotId,
                    HDLitMasterNode.AlphaSlotId,
                    HDLitMasterNode.AlphaThresholdSlotId,
                    HDLitMasterNode.DepthOffsetSlotId,
                },

                // Render State Overrides
                CullOverride = HDSubShaderUtilities.defaultCullMode,
                ZWriteOverride = HDSubShaderUtilities.zWriteOn,
                StencilOverride = new List<string>
                {
                    "Stencil",
                    "{",
                    "    WriteMask [_StencilWriteMaskDepth]",
                    "    Ref [_StencilRefDepth]",
                    "    Comp Always",
                    "    Pass Replace",
                    "}",
                },

                // Required fields
                requiredAttributes = new List<string>()
                {
                    "AttributesMesh.normalOS",
                    "AttributesMesh.tangentOS",     // Always present as we require it also in case of Variants lighting
                    "AttributesMesh.uv0",
                    "AttributesMesh.uv1",
                    "AttributesMesh.color",
                    "AttributesMesh.uv2",           // SHADERPASS_LIGHT_TRANSPORT always uses uv2
                    "AttributesMesh.uv3",           // DEBUG_DISPLAY
                },
                requiredVaryings = new List<string>()
                {
                    "FragInputs.tangentToWorld",
                    "FragInputs.positionRWS",
                    "FragInputs.texCoord0",
                    "FragInputs.texCoord1",
                    "FragInputs.texCoord2",
                    "FragInputs.texCoord3",
                    "FragInputs.color",
                },

                // Pass setup
                pragmas = new List<string>()
                {
                    "#pragma target 4.5",
                    "only_renderers d3d11 ps4 xboxone vulkan metal switch",
                    "multi_compile_instancing",
                },
                includes = new List<string>()
                {
                    "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl",
                    "Packages/com.unity.render-pipelines.core/ShaderLibrary/NormalSurfaceGradient.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/FragInputs.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPass.cs.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Material.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Lit/Lit.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/BuiltinUtilities.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/MaterialUtilities.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Decal/DecalUtilities.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Lit/LitDecalData.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderGraphFunctions.hlsl",
                },
                defines = new List<string>()
                {
                    "RAYTRACING_SHADER_GRAPH_HIGH",
                },
                keywords = new List<KeywordDescriptor>()
                {
                    Keywords.WriteMsaaDepth,
                    Keywords.WriteNormalBuffer,
                    Keywords.LodFadeCrossfade,
                    Keywords.SurfaceTypeTransparent,
                    Keywords.DoubleSided,
                    Keywords.BlendMode,
                },

                // Custom template
                passTemplatePath = $"{HDUtils.GetHDRenderPipelinePath()}Editor/Material/Lit/ShaderGraph/HDLitPass.template",
                sharedTemplateDirectory = $"{HDUtils.GetHDRenderPipelinePath()}Editor/ShaderGraph",
            };

            public static ShaderPass HDLitMotionVectors = new ShaderPass()
            {
                // Definition
                displayName = "MotionVectors",
                referenceName = "SHADERPASS_MOTION_VECTORS",
                lightMode = "MotionVectors",
                passInclude = "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassMotionVectors.hlsl",
                useInPreview = false,

                // Port mask
                vertexPorts = new List<int>()
                {
                    HDLitMasterNode.PositionSlotId,
                    HDLitMasterNode.VertexNormalSlotID,
                    HDLitMasterNode.VertexTangentSlotID
                },
                pixelPorts = new List<int>
                {
                    HDLitMasterNode.NormalSlotId,
                    HDLitMasterNode.SmoothnessSlotId,
                    HDLitMasterNode.AlphaSlotId,
                    HDLitMasterNode.AlphaThresholdSlotId,
                    HDLitMasterNode.DepthOffsetSlotId,
                },

                // Render State Overrides
                CullOverride = HDSubShaderUtilities.defaultCullMode,
                ZWriteOverride = HDSubShaderUtilities.zWriteOn,
                StencilOverride = new List<string>
                {
                    "Stencil",
                    "{",
                    "    WriteMask [_StencilWriteMaskMV]",
                    "    Ref [_StencilRefMV]",
                    "    Comp Always",
                    "    Pass Replace",
                    "}",
                },

                // Required fields
                requiredAttributes = new List<string>()
                {
                    "AttributesMesh.normalOS",
                    "AttributesMesh.tangentOS",     // Always present as we require it also in case of Variants lighting
                    "AttributesMesh.uv0",
                    "AttributesMesh.uv1",
                    "AttributesMesh.color",
                    "AttributesMesh.uv2",           // SHADERPASS_LIGHT_TRANSPORT always uses uv2
                    "AttributesMesh.uv3",           // DEBUG_DISPLAY
                },
                requiredVaryings = new List<string>()
                {
                    "FragInputs.tangentToWorld",
                    "FragInputs.positionRWS",
                    "FragInputs.texCoord0",
                    "FragInputs.texCoord1",
                    "FragInputs.texCoord2",
                    "FragInputs.texCoord3",
                    "FragInputs.color",
                },

                // Pass setup
                pragmas = new List<string>()
                {
                    "#pragma target 4.5",
                    "only_renderers d3d11 ps4 xboxone vulkan metal switch",
                    "multi_compile_instancing",
                },
                includes = new List<string>()
                {
                    "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl",
                    "Packages/com.unity.render-pipelines.core/ShaderLibrary/NormalSurfaceGradient.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/FragInputs.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPass.cs.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Material.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Lit/Lit.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/BuiltinUtilities.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/MaterialUtilities.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Decal/DecalUtilities.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Lit/LitDecalData.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderGraphFunctions.hlsl",
                },
                defines = new List<string>()
                {
                    "RAYTRACING_SHADER_GRAPH_HIGH",
                },
                keywords = new List<KeywordDescriptor>()
                {
                    Keywords.WriteMsaaDepth,
                    Keywords.WriteNormalBuffer,
                    Keywords.LodFadeCrossfade,
                    Keywords.SurfaceTypeTransparent,
                    Keywords.DoubleSided,
                    Keywords.BlendMode,
                },

                // Custom template
                passTemplatePath = $"{HDUtils.GetHDRenderPipelinePath()}Editor/Material/Lit/ShaderGraph/HDLitPass.template",
                sharedTemplateDirectory = $"{HDUtils.GetHDRenderPipelinePath()}Editor/ShaderGraph",
            };

            public static ShaderPass HDLitDistortion = new ShaderPass()
            {
                // Definition
                displayName = "DistortionVectors",
                referenceName = "SHADERPASS_DISTORTION",
                lightMode = "DistortionVectors",
                passInclude = "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassDistortion.hlsl",
                useInPreview = true,

                // Port mask
                vertexPorts = new List<int>()
                {
                    HDLitMasterNode.PositionSlotId,
                    HDLitMasterNode.VertexNormalSlotID,
                    HDLitMasterNode.VertexTangentSlotID
                },
                pixelPorts = new List<int>
                {
                    HDLitMasterNode.AlphaSlotId,
                    HDLitMasterNode.AlphaThresholdSlotId,
                    HDLitMasterNode.DistortionSlotId,
                    HDLitMasterNode.DistortionBlurSlotId,
                },

                // Render State Overrides
                ZWriteOverride = HDSubShaderUtilities.zWriteOff,
                StencilOverride = new List<string>()
                {
                    "// Stencil setup",
                    "Stencil",
                    "{",
                    "    WriteMask [_StencilRefDistortionVec]",
                    "    Ref [_StencilRefDistortionVec]",
                    "    Comp Always",
                    "    Pass Replace",
                    "}"
                },

                // Pass setup
                pragmas = new List<string>()
                {
                    "#pragma target 4.5",
                    "only_renderers d3d11 ps4 xboxone vulkan metal switch",
                    "multi_compile_instancing",
                },
                includes = new List<string>()
                {
                    "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl",
                    "Packages/com.unity.render-pipelines.core/ShaderLibrary/NormalSurfaceGradient.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/FragInputs.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPass.cs.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Material.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Lit/Lit.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/BuiltinUtilities.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/MaterialUtilities.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Decal/DecalUtilities.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Lit/LitDecalData.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderGraphFunctions.hlsl",
                },
                defines = new List<string>()
                {
                    "RAYTRACING_SHADER_GRAPH_HIGH",
                },
                keywords = new List<KeywordDescriptor>()
                {
                    Keywords.LodFadeCrossfade,
                    Keywords.SurfaceTypeTransparent,
                    Keywords.DoubleSided,
                    Keywords.BlendMode,
                },

                // Custom template
                passTemplatePath = $"{HDUtils.GetHDRenderPipelinePath()}Editor/Material/Lit/ShaderGraph/HDLitPass.template",
                sharedTemplateDirectory = $"{HDUtils.GetHDRenderPipelinePath()}Editor/ShaderGraph",
            };

            public static ShaderPass HDLitTransparentDepthPrepass = new ShaderPass()
            {
                // Definition
                displayName = "TransparentDepthPrepass",
                referenceName = "SHADERPASS_DEPTH_ONLY",
                lightMode = "TransparentDepthPrepass",
                passInclude = "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassDepthOnly.hlsl",
                useInPreview = true,

                // Port mask
                vertexPorts = new List<int>()
                {
                    HDLitMasterNode.PositionSlotId,
                    HDLitMasterNode.VertexNormalSlotID,
                    HDLitMasterNode.VertexTangentSlotID
                },
                pixelPorts = new List<int>
                {
                    HDLitMasterNode.AlphaSlotId,
                    HDLitMasterNode.AlphaThresholdDepthPrepassSlotId,
                    HDLitMasterNode.DepthOffsetSlotId,
                },

                // Render State Overrides
                BlendOverride = "Blend One Zero",
                ZWriteOverride = "ZWrite On",
                ColorMaskOverride = "ColorMask 0",
                CullOverride = HDSubShaderUtilities.defaultCullMode,

                // Pass setup
                pragmas = new List<string>()
                {
                    "#pragma target 4.5",
                    "only_renderers d3d11 ps4 xboxone vulkan metal switch",
                    "multi_compile_instancing",
                },
                includes = new List<string>()
                {
                    "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl",
                    "Packages/com.unity.render-pipelines.core/ShaderLibrary/NormalSurfaceGradient.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/FragInputs.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPass.cs.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Material.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Lit/Lit.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/BuiltinUtilities.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/MaterialUtilities.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Decal/DecalUtilities.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Lit/LitDecalData.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderGraphFunctions.hlsl",
                },
                defines = new List<string>()
                {
                    "CUTOFF_TRANSPARENT_DEPTH_PREPASS",
                    "RAYTRACING_SHADER_GRAPH_HIGH",
                },
                keywords = new List<KeywordDescriptor>()
                {
                    Keywords.LodFadeCrossfade,
                    Keywords.SurfaceTypeTransparent,
                    Keywords.DoubleSided,
                    Keywords.BlendMode,
                },

                // Custom template
                passTemplatePath = $"{HDUtils.GetHDRenderPipelinePath()}Editor/Material/Lit/ShaderGraph/HDLitPass.template",
                sharedTemplateDirectory = $"{HDUtils.GetHDRenderPipelinePath()}Editor/ShaderGraph",
            };

            public static ShaderPass HDLitTransparentBackface = new ShaderPass()
            {
                // Definition
                displayName = "TransparentBackface",
                referenceName = "SHADERPASS_FORWARD",
                lightMode = "TransparentBackface",
                passInclude = "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassForward.hlsl",
                useInPreview = true,

                // Port mask
                vertexPorts = new List<int>()
                {
                    HDLitMasterNode.PositionSlotId,
                    HDLitMasterNode.VertexNormalSlotID,
                    HDLitMasterNode.VertexTangentSlotID
                },
                pixelPorts = new List<int>
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

                // Render State Overrides
                BlendOverride = "Blend [_SrcBlend] [_DstBlend], [_AlphaSrcBlend] [_AlphaDstBlend]",
                CullOverride = "Cull Front",
                ZTestOverride = HDSubShaderUtilities.zTestTransparent,
                ColorMaskOverride = "ColorMask [_ColorMaskTransparentVel] 1",
                ZWriteOverride = HDSubShaderUtilities.ZWriteDefault,

                // Pass setup
                pragmas = new List<string>()
                {
                    "#pragma target 4.5",
                    "only_renderers d3d11 ps4 xboxone vulkan metal switch",
                    "multi_compile_instancing",
                },
                defines = new List<string>()
                {
                    "HAS_LIGHTLOOP",
                    "USE_CLUSTERED_LIGHTLIST",
                    "RAYTRACING_SHADER_GRAPH_HIGH",
                },
                includes = new List<string>()
                {
                    "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl",
                    "Packages/com.unity.render-pipelines.core/ShaderLibrary/NormalSurfaceGradient.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/FragInputs.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPass.cs.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Material.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/Lighting.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/LightLoop/LightLoopDef.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Lit/Lit.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/LightLoop/LightLoop.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/BuiltinUtilities.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/MaterialUtilities.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Decal/DecalUtilities.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Lit/LitDecalData.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderGraphFunctions.hlsl",
                },
                keywords = new List<KeywordDescriptor>()
                {
                    Keywords.LodFadeCrossfade,
                    Keywords.SurfaceTypeTransparent,
                    Keywords.DoubleSided,
                    Keywords.BlendMode,
                    Keywords.DebugDisplay,
                    Keywords.Lightmap,
                    Keywords.DynamicLightmap,
                    Keywords.DirectionalLightmapCombined,
                    Keywords.ShadowsShadowmask,
                    Keywords.Decals,
                    Keywords.Shadow,
                },

                // Custom template
                passTemplatePath = $"{HDUtils.GetHDRenderPipelinePath()}Editor/Material/Lit/ShaderGraph/HDLitPass.template",
                sharedTemplateDirectory = $"{HDUtils.GetHDRenderPipelinePath()}Editor/ShaderGraph",
            };

            public static ShaderPass HDLitForwardOpaque = new ShaderPass()
            {
                // Definition
                displayName = "Forward",
                referenceName = "SHADERPASS_FORWARD",
                lightMode = "Forward",
                passInclude = "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassForward.hlsl",
                useInPreview = true,

                // Port mask
                vertexPorts = new List<int>()
                {
                    HDLitMasterNode.PositionSlotId,
                    HDLitMasterNode.VertexNormalSlotID,
                    HDLitMasterNode.VertexTangentSlotID
                },
                pixelPorts = new List<int>
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

                // Render State Overrides
                BlendOverride = "Blend [_SrcBlend] [_DstBlend], [_AlphaSrcBlend] [_AlphaDstBlend]",
                CullOverride = HDSubShaderUtilities.cullModeForward,
                ZTestOverride = HDSubShaderUtilities.zTestDepthEqualForOpaque,
                ZWriteOverride = HDSubShaderUtilities.ZWriteDefault,
                ColorMaskOverride = "ColorMask [_ColorMaskTransparentVel] 1",
                StencilOverride = new List<string>()
                {
                    "Stencil",
                    "{",
                    "    WriteMask [_StencilWriteMask]",
                    "    Ref [_StencilRef]",
                    "    Comp Always",
                    "    Pass Replace",
                    "}"
                },

                // Required fields
                requiredVaryings = new List<string>()
                {
                    "FragInputs.tangentToWorld",
                    "FragInputs.positionRWS",
                    "FragInputs.texCoord1",
                    "FragInputs.texCoord2"
                },

                // Pass setup
                pragmas = new List<string>()
                {
                    "#pragma target 4.5",
                    "only_renderers d3d11 ps4 xboxone vulkan metal switch",
                    "multi_compile_instancing",
                },
                defines = new List<string>()
                {
                    "HAS_LIGHTLOOP",
                },
                includes = new List<string>()
                {
                    "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl",
                    "Packages/com.unity.render-pipelines.core/ShaderLibrary/NormalSurfaceGradient.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/FragInputs.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPass.cs.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Material.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/Lighting.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/LightLoop/LightLoopDef.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Lit/Lit.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/LightLoop/LightLoop.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/BuiltinUtilities.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/MaterialUtilities.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Decal/DecalUtilities.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Lit/LitDecalData.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderGraphFunctions.hlsl",
                },
                keywords = new List<KeywordDescriptor>()
                {
                    Keywords.LodFadeCrossfade,
                    Keywords.SurfaceTypeTransparent,
                    Keywords.DoubleSided,
                    Keywords.BlendMode,
                    Keywords.DebugDisplay,
                    Keywords.Lightmap,
                    Keywords.DynamicLightmap,
                    Keywords.DirectionalLightmapCombined,
                    Keywords.ShadowsShadowmask,
                    Keywords.Decals,
                    Keywords.Shadow,
                    Keywords.LightList,
                },

                // Custom template
                passTemplatePath = $"{HDUtils.GetHDRenderPipelinePath()}Editor/Material/Lit/ShaderGraph/HDLitPass.template",
                sharedTemplateDirectory = $"{HDUtils.GetHDRenderPipelinePath()}Editor/ShaderGraph",
            };

            public static ShaderPass HDLitForwardTransparent = new ShaderPass()
            {
                // Definition
                displayName = "Forward",
                referenceName = "SHADERPASS_FORWARD",
                lightMode = "Forward",
                passInclude = "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassForward.hlsl",
                useInPreview = true,

                // Port mask
                vertexPorts = new List<int>()
                {
                    HDLitMasterNode.PositionSlotId,
                    HDLitMasterNode.VertexNormalSlotID,
                    HDLitMasterNode.VertexTangentSlotID
                },
                pixelPorts = new List<int>
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

                // Render State Overrides
                BlendOverride = "Blend [_SrcBlend] [_DstBlend], [_AlphaSrcBlend] [_AlphaDstBlend]",
                CullOverride = HDSubShaderUtilities.cullModeForward,
                ZTestOverride = HDSubShaderUtilities.zTestDepthEqualForOpaque,
                ZWriteOverride = HDSubShaderUtilities.ZWriteDefault,
                ColorMaskOverride = "ColorMask [_ColorMaskTransparentVel] 1",
                StencilOverride = new List<string>()
                {
                    "Stencil",
                    "{",
                    "    WriteMask [_StencilWriteMask]",
                    "    Ref [_StencilRef]",
                    "    Comp Always",
                    "    Pass Replace",
                    "}"
                },

                // Required fields
                requiredVaryings = new List<string>()
                {
                    "FragInputs.tangentToWorld",
                    "FragInputs.positionRWS",
                    "FragInputs.texCoord1",
                    "FragInputs.texCoord2"
                },

                // Pass setup
                pragmas = new List<string>()
                {
                    "#pragma target 4.5",
                    "only_renderers d3d11 ps4 xboxone vulkan metal switch",
                    "multi_compile_instancing",
                },
                defines = new List<string>()
                {
                    "HAS_LIGHTLOOP",
                    "USE_CLUSTERED_LIGHTLIST",
                    "RAYTRACING_SHADER_GRAPH_HIGH",
                },
                includes = new List<string>()
                {
                    "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl",
                    "Packages/com.unity.render-pipelines.core/ShaderLibrary/NormalSurfaceGradient.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/FragInputs.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPass.cs.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Material.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/Lighting.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/LightLoop/LightLoopDef.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Lit/Lit.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/LightLoop/LightLoop.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/BuiltinUtilities.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/MaterialUtilities.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Decal/DecalUtilities.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Lit/LitDecalData.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderGraphFunctions.hlsl",
                },
                keywords = new List<KeywordDescriptor>()
                {
                    Keywords.LodFadeCrossfade,
                    Keywords.SurfaceTypeTransparent,
                    Keywords.DoubleSided,
                    Keywords.BlendMode,
                    Keywords.DebugDisplay,
                    Keywords.Lightmap,
                    Keywords.DynamicLightmap,
                    Keywords.DirectionalLightmapCombined,
                    Keywords.ShadowsShadowmask,
                    Keywords.Decals,
                    Keywords.Shadow,
                },

                // Custom template
                passTemplatePath = $"{HDUtils.GetHDRenderPipelinePath()}Editor/Material/Lit/ShaderGraph/HDLitPass.template",
                sharedTemplateDirectory = $"{HDUtils.GetHDRenderPipelinePath()}Editor/ShaderGraph",
            };

            public static ShaderPass HDLitTransparentDepthPostpass = new ShaderPass()
            {
                // Definition
                displayName = "TransparentDepthPostpass",
                referenceName = "SHADERPASS_DEPTH_ONLY",
                lightMode = "TransparentDepthPostpass",
                passInclude = "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassDepthOnly.hlsl",
                useInPreview = true,

                // Port mask
                vertexPorts = new List<int>()
                {
                    HDLitMasterNode.PositionSlotId,
                    HDLitMasterNode.VertexNormalSlotID,
                    HDLitMasterNode.VertexTangentSlotID
                },
                pixelPorts = new List<int>
                {
                    HDLitMasterNode.AlphaSlotId,
                    HDLitMasterNode.AlphaThresholdDepthPrepassSlotId,
                    HDLitMasterNode.DepthOffsetSlotId,
                },

                // Render State Overrides
                BlendOverride = "Blend One Zero",
                ZWriteOverride = "ZWrite On",
                ColorMaskOverride = "ColorMask 0",
                CullOverride = HDSubShaderUtilities.defaultCullMode,

                // Pass setup
                pragmas = new List<string>()
                {
                    "#pragma target 4.5",
                    "only_renderers d3d11 ps4 xboxone vulkan metal switch",
                    "multi_compile_instancing",
                },
                includes = new List<string>()
                {
                    "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl",
                    "Packages/com.unity.render-pipelines.core/ShaderLibrary/NormalSurfaceGradient.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/FragInputs.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPass.cs.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Material.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Lit/Lit.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/BuiltinUtilities.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/MaterialUtilities.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Decal/DecalUtilities.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Lit/LitDecalData.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderGraphFunctions.hlsl",
                },
                defines = new List<string>()
                {
                    "CUTOFF_TRANSPARENT_DEPTH_POSTPASS",
                    "RAYTRACING_SHADER_GRAPH_HIGH",
                },
                keywords = new List<KeywordDescriptor>()
                {
                    Keywords.LodFadeCrossfade,
                    Keywords.SurfaceTypeTransparent,
                    Keywords.DoubleSided,
                    Keywords.BlendMode,
                },

                // Custom template
                passTemplatePath = $"{HDUtils.GetHDRenderPipelinePath()}Editor/Material/Lit/ShaderGraph/HDLitPass.template",
                sharedTemplateDirectory = $"{HDUtils.GetHDRenderPipelinePath()}Editor/ShaderGraph",
            };

            public static ShaderPass EyeMETA = new ShaderPass()
            {
                // Definition
                displayName = "META",
                referenceName = "SHADERPASS_LIGHT_TRANSPORT",
                lightMode = "META",
                passInclude = "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassLightTransport.hlsl",
                useInPreview = false,

                // Port mask
                pixelPorts = new List<int>
                {
                    EyeMasterNode.AlbedoSlotId,
                    EyeMasterNode.SpecularOcclusionSlotId,
                    EyeMasterNode.NormalSlotId,
                    EyeMasterNode.IrisNormalSlotId,
                    EyeMasterNode.SmoothnessSlotId,
                    EyeMasterNode.IORSlotId,
                    EyeMasterNode.AmbientOcclusionSlotId,
                    EyeMasterNode.MaskSlotId,
                    EyeMasterNode.DiffusionProfileHashSlotId,
                    EyeMasterNode.SubsurfaceMaskSlotId,
                    EyeMasterNode.EmissionSlotId,
                    EyeMasterNode.AlphaSlotId,
                    EyeMasterNode.AlphaClipThresholdSlotId,
                },

                // Render State Overrides
                CullOverride = "Cull Off",

                // Required fields
                requiredAttributes = new List<string>()
                {
                    "AttributesMesh.normalOS",
                    "AttributesMesh.tangentOS",     // Always present as we require it also in case of anisotropic lighting
                    "AttributesMesh.uv0",
                    "AttributesMesh.uv1",
                    "AttributesMesh.color",
                    "AttributesMesh.uv2",           // SHADERPASS_LIGHT_TRANSPORT always uses uv2
                },

                // Pass setup
                pragmas = new List<string>()
                {
                    "#pragma target 4.5",
                    "only_renderers d3d11 ps4 xboxone vulkan metal switch",
                    "multi_compile_instancing",
                    "instancing_options renderinglayer",
                },
                includes = new List<string>()
                {
                    "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl",
                    "Packages/com.unity.render-pipelines.core/ShaderLibrary/NormalSurfaceGradient.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/FragInputs.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPass.cs.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Material.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Eye/Eye.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/BuiltinUtilities.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/MaterialUtilities.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Decal/DecalUtilities.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderGraphFunctions.hlsl",
                },
                keywords = new List<KeywordDescriptor>()
                {
                    Keywords.LodFadeCrossfade,
                    Keywords.SurfaceTypeTransparent,
                    Keywords.DoubleSided,
                    Keywords.BlendMode,
                },

                // Custom template
                passTemplatePath = $"{HDUtils.GetHDRenderPipelinePath()}Editor/Material/Eye/ShaderGraph/EyePass.template",
                sharedTemplateDirectory = $"{HDUtils.GetHDRenderPipelinePath()}Editor/ShaderGraph",
            };

            public static ShaderPass EyeShadowCaster = new ShaderPass()
            {
                // Definition
                displayName = "ShadowCaster",
                referenceName = "SHADERPASS_SHADOWS",
                lightMode = "ShadowCaster",
                passInclude = "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassDepthOnly.hlsl",
                useInPreview = false,

                // Port mask
                vertexPorts = new List<int>()
                {
                    EyeMasterNode.PositionSlotId,
                    EyeMasterNode.VertexNormalSlotID,
                    EyeMasterNode.VertexTangentSlotID
                },
                pixelPorts = new List<int>
                {
                    EyeMasterNode.AlphaSlotId,
                    EyeMasterNode.AlphaClipThresholdSlotId,
                    EyeMasterNode.DepthOffsetSlotId,
                },

                // Render State Overrides
                BlendOverride = "Blend One Zero",
                ZClipOverride = HDSubShaderUtilities.zClipShadowCaster,
                CullOverride = HDSubShaderUtilities.defaultCullMode,
                ZWriteOverride = "ZWrite On",
                ColorMaskOverride = "ColorMask 0",

                // Pass setup
                pragmas = new List<string>()
                {
                    "#pragma target 4.5",
                    "only_renderers d3d11 ps4 xboxone vulkan metal switch",
                    "multi_compile_instancing",
                    "instancing_options renderinglayer",
                },
                includes = new List<string>()
                {
                    "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl",
                    "Packages/com.unity.render-pipelines.core/ShaderLibrary/NormalSurfaceGradient.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/FragInputs.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPass.cs.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Material.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Eye/Eye.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/BuiltinUtilities.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/MaterialUtilities.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Decal/DecalUtilities.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderGraphFunctions.hlsl",
                },
                keywords = new List<KeywordDescriptor>()
                {
                    Keywords.LodFadeCrossfade,
                    Keywords.SurfaceTypeTransparent,
                    Keywords.DoubleSided,
                    Keywords.BlendMode,
                },

                // Custom template
                passTemplatePath = $"{HDUtils.GetHDRenderPipelinePath()}Editor/Material/Eye/ShaderGraph/EyePass.template",
                sharedTemplateDirectory = $"{HDUtils.GetHDRenderPipelinePath()}Editor/ShaderGraph",
            };

            public static ShaderPass EyeSceneSelection = new ShaderPass()
            {
                // Definition
                displayName = "SceneSelectionPass",
                referenceName = "SHADERPASS_DEPTH_ONLY",
                lightMode = "SceneSelectionPass",
                passInclude = "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassDepthOnly.hlsl",
                useInPreview = false,

                // Port mask
                vertexPorts = new List<int>()
                {
                    EyeMasterNode.PositionSlotId,
                    EyeMasterNode.VertexNormalSlotID,
                    EyeMasterNode.VertexTangentSlotID
                },
                pixelPorts = new List<int>
                {
                    EyeMasterNode.AlphaSlotId,
                    EyeMasterNode.AlphaClipThresholdSlotId,
                    EyeMasterNode.DepthOffsetSlotId,
                },

                // Render State Overrides
                ColorMaskOverride = "ColorMask 0",

                // Pass setup
                pragmas = new List<string>()
                {
                    "#pragma target 4.5",
                    "only_renderers d3d11 ps4 xboxone vulkan metal switch",
                    "multi_compile_instancing",
                    "editor_sync_compilation",
                    "instancing_options renderinglayer",
                },
                defines = new List<string>()
                {
                    "SCENESELECTIONPASS",
                },
                includes = new List<string>()
                {
                    "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl",
                    "Packages/com.unity.render-pipelines.core/ShaderLibrary/NormalSurfaceGradient.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/FragInputs.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPass.cs.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Material.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Eye/Eye.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/BuiltinUtilities.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/MaterialUtilities.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Decal/DecalUtilities.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderGraphFunctions.hlsl",
                },
                keywords = new List<KeywordDescriptor>()
                {
                    Keywords.LodFadeCrossfade,
                    Keywords.SurfaceTypeTransparent,
                    Keywords.DoubleSided,
                    Keywords.BlendMode,
                },

                // Custom template
                passTemplatePath = $"{HDUtils.GetHDRenderPipelinePath()}Editor/Material/Eye/ShaderGraph/EyePass.template",
                sharedTemplateDirectory = $"{HDUtils.GetHDRenderPipelinePath()}Editor/ShaderGraph",
            };

            public static ShaderPass EyeDepthForwardOnly = new ShaderPass()
            {
                // Definition
                displayName = "DepthForwardOnly",
                referenceName = "SHADERPASS_DEPTH_ONLY",
                lightMode = "DepthForwardOnly",
                passInclude = "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassDepthOnly.hlsl",
                useInPreview = true,

                // Port mask
                vertexPorts = new List<int>()
                {
                    EyeMasterNode.PositionSlotId,
                    EyeMasterNode.VertexNormalSlotID,
                    EyeMasterNode.VertexTangentSlotID
                },
                pixelPorts = new List<int>
                {
                    EyeMasterNode.NormalSlotId,
                    EyeMasterNode.SmoothnessSlotId,
                    EyeMasterNode.AlphaSlotId,
                    EyeMasterNode.AlphaClipThresholdSlotId,
                    EyeMasterNode.DepthOffsetSlotId,
                },

                // Render State Overrides
                CullOverride = HDSubShaderUtilities.defaultCullMode,
                ZWriteOverride = "ZWrite On",
                StencilOverride = new List<string>
                {
                    "Stencil",
                    "{",
                    "    WriteMask [_StencilWriteMaskDepth]",
                    "    Ref [_StencilRefDepth]",
                    "    Comp Always",
                    "    Pass Replace",
                    "}",
                },

                // Required fields
                requiredAttributes = new List<string>()
                {
                    "AttributesMesh.normalOS",
                    "AttributesMesh.tangentOS",     // Always present as we require it also in case of Variants lighting
                    "AttributesMesh.uv0",
                    "AttributesMesh.uv1",
                    "AttributesMesh.color",
                    "AttributesMesh.uv2",           // SHADERPASS_LIGHT_TRANSPORT always uses uv2
                    "AttributesMesh.uv3",           // DEBUG_DISPLAY
                },
                requiredVaryings = new List<string>()
                {
                    "FragInputs.tangentToWorld",
                    "FragInputs.positionRWS",
                    "FragInputs.texCoord0",
                    "FragInputs.texCoord1",
                    "FragInputs.texCoord2",
                    "FragInputs.texCoord3",
                    "FragInputs.color",
                },

                // Pass setup
                pragmas = new List<string>()
                {
                    "#pragma target 4.5",
                    "only_renderers d3d11 ps4 xboxone vulkan metal switch",
                    "multi_compile_instancing",
                    "instancing_options renderinglayer",
                },
                includes = new List<string>()
                {
                    "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl",
                    "Packages/com.unity.render-pipelines.core/ShaderLibrary/NormalSurfaceGradient.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/FragInputs.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPass.cs.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Material.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Eye/Eye.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/BuiltinUtilities.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/MaterialUtilities.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Decal/DecalUtilities.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderGraphFunctions.hlsl",
                },
                defines = new List<string>()
                {
                    "WRITE_NORMAL_BUFFER",
                    "RAYTRACING_SHADER_GRAPH_HIGH",
                },
                keywords = new List<KeywordDescriptor>()
                {
                    Keywords.WriteMsaaDepth,
                    Keywords.LodFadeCrossfade,
                    Keywords.SurfaceTypeTransparent,
                    Keywords.DoubleSided,
                    Keywords.BlendMode,
                },

                // Custom template
                passTemplatePath = $"{HDUtils.GetHDRenderPipelinePath()}Editor/Material/Eye/ShaderGraph/EyePass.template",
                sharedTemplateDirectory = $"{HDUtils.GetHDRenderPipelinePath()}Editor/ShaderGraph",
            };

            public static ShaderPass EyeMotionVectors = new ShaderPass()
            {
                // Definition
                displayName = "MotionVectors",
                referenceName = "SHADERPASS_MOTION_VECTORS",
                lightMode = "MotionVectors",
                passInclude = "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassMotionVectors.hlsl",
                useInPreview = false,

                // Port mask
                vertexPorts = new List<int>()
                {
                    EyeMasterNode.PositionSlotId,
                    EyeMasterNode.VertexNormalSlotID,
                    EyeMasterNode.VertexTangentSlotID
                },
                pixelPorts = new List<int>
                {
                    EyeMasterNode.NormalSlotId,
                    EyeMasterNode.SmoothnessSlotId,
                    EyeMasterNode.AlphaSlotId,
                    EyeMasterNode.AlphaClipThresholdSlotId,
                    EyeMasterNode.DepthOffsetSlotId,
                },

                // Render State Overrides
                CullOverride = HDSubShaderUtilities.defaultCullMode,
                StencilOverride = new List<string>
                {
                    "Stencil",
                    "{",
                    "    WriteMask [_StencilWriteMaskMV]",
                    "    Ref [_StencilRefMV]",
                    "    Comp Always",
                    "    Pass Replace",
                    "}",
                },

                // Required fields
                requiredAttributes = new List<string>()
                {
                    "AttributesMesh.normalOS",
                    "AttributesMesh.tangentOS",     // Always present as we require it also in case of Variants lighting
                    "AttributesMesh.uv0",
                    "AttributesMesh.uv1",
                    "AttributesMesh.color",
                    "AttributesMesh.uv2",           // SHADERPASS_LIGHT_TRANSPORT always uses uv2
                    "AttributesMesh.uv3",           // DEBUG_DISPLAY
                },
                requiredVaryings = new List<string>()
                {
                    "FragInputs.tangentToWorld",
                    "FragInputs.positionRWS",
                    "FragInputs.texCoord0",
                    "FragInputs.texCoord1",
                    "FragInputs.texCoord2",
                    "FragInputs.texCoord3",
                    "FragInputs.color",
                },

                // Pass setup
                pragmas = new List<string>()
                {
                    "#pragma target 4.5",
                    "only_renderers d3d11 ps4 xboxone vulkan metal switch",
                    "multi_compile_instancing",
                    "instancing_options renderinglayer",
                },
                includes = new List<string>()
                {
                    "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl",
                    "Packages/com.unity.render-pipelines.core/ShaderLibrary/NormalSurfaceGradient.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/FragInputs.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPass.cs.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Material.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Eye/Eye.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/BuiltinUtilities.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/MaterialUtilities.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Decal/DecalUtilities.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderGraphFunctions.hlsl",
                },
                defines = new List<string>()
                {
                    "WRITE_NORMAL_BUFFER",
                    "RAYTRACING_SHADER_GRAPH_HIGH",
                },
                keywords = new List<KeywordDescriptor>()
                {
                    Keywords.WriteMsaaDepth,
                    Keywords.LodFadeCrossfade,
                    Keywords.SurfaceTypeTransparent,
                    Keywords.DoubleSided,
                    Keywords.BlendMode,
                },

                // Custom template
                passTemplatePath = $"{HDUtils.GetHDRenderPipelinePath()}Editor/Material/Eye/ShaderGraph/EyePass.template",
                sharedTemplateDirectory = $"{HDUtils.GetHDRenderPipelinePath()}Editor/ShaderGraph",
            };

            public static ShaderPass EyeForwardOnlyOpaque = new ShaderPass()
            {
                // Definition
                displayName = "ForwardOnly",
                referenceName = "SHADERPASS_FORWARD",
                lightMode = "ForwardOnly",
                passInclude = "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassForward.hlsl",
                useInPreview = true,

                // Port mask
                vertexPorts = new List<int>()
                {
                    EyeMasterNode.PositionSlotId,
                    EyeMasterNode.VertexNormalSlotID,
                    EyeMasterNode.VertexTangentSlotID
                },
                pixelPorts = new List<int>
                {
                    EyeMasterNode.AlbedoSlotId,
                    EyeMasterNode.SpecularOcclusionSlotId,
                    EyeMasterNode.NormalSlotId,
                    EyeMasterNode.IrisNormalSlotId,
                    EyeMasterNode.SmoothnessSlotId,
                    EyeMasterNode.IORSlotId,
                    EyeMasterNode.AmbientOcclusionSlotId,
                    EyeMasterNode.MaskSlotId,
                    EyeMasterNode.DiffusionProfileHashSlotId,
                    EyeMasterNode.SubsurfaceMaskSlotId,
                    EyeMasterNode.EmissionSlotId,
                    EyeMasterNode.AlphaSlotId,
                    EyeMasterNode.AlphaClipThresholdSlotId,
                    EyeMasterNode.LightingSlotId,
                    EyeMasterNode.BackLightingSlotId,
                    EyeMasterNode.DepthOffsetSlotId,
                },

                // Render State Overrides
                BlendOverride = "Blend [_SrcBlend] [_DstBlend], [_AlphaSrcBlend] [_AlphaDstBlend]",
                CullOverride = HDSubShaderUtilities.cullModeForward,
                ZTestOverride = HDSubShaderUtilities.zTestDepthEqualForOpaque,
                ZWriteOverride = HDSubShaderUtilities.ZWriteDefault,
                StencilOverride = new List<string>()
                {
                    "Stencil",
                    "{",
                    "    WriteMask [_StencilWriteMask]",
                    "    Ref [_StencilRef]",
                    "    Comp Always",
                    "    Pass Replace",
                    "}"
                },

                // Required fields
                requiredAttributes = new List<string>()
                {
                    "AttributesMesh.normalOS",
                    "AttributesMesh.tangentOS",     // Always present as we require it also in case of Variants lighting
                    "AttributesMesh.uv0",
                    "AttributesMesh.uv1",
                    "AttributesMesh.color",
                    "AttributesMesh.uv2",           // SHADERPASS_LIGHT_TRANSPORT always uses uv2
                    "AttributesMesh.uv3",           // DEBUG_DISPLAY
                },
                requiredVaryings = new List<string>()
                {
                    "FragInputs.tangentToWorld",
                    "FragInputs.positionRWS",
                    "FragInputs.texCoord0",
                    "FragInputs.texCoord1",
                    "FragInputs.texCoord2",
                    "FragInputs.texCoord3",
                    "FragInputs.color",
                },

                // Pass setup
                pragmas = new List<string>()
                {
                    "#pragma target 4.5",
                    "only_renderers d3d11 ps4 xboxone vulkan metal switch",
                    "multi_compile_instancing",
                    "instancing_options renderinglayer",
                },
                defines = new List<string>()
                {
                    "HAS_LIGHTLOOP",
                },
                includes = new List<string>()
                {
                    "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl",
                    "Packages/com.unity.render-pipelines.core/ShaderLibrary/NormalSurfaceGradient.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/FragInputs.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPass.cs.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Material.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/Lighting.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/LightLoop/LightLoopDef.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Eye/Eye.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/LightLoop/LightLoop.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/BuiltinUtilities.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/MaterialUtilities.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Decal/DecalUtilities.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderGraphFunctions.hlsl",
                },
                keywords = new List<KeywordDescriptor>()
                {
                    Keywords.LodFadeCrossfade,
                    Keywords.SurfaceTypeTransparent,
                    Keywords.DoubleSided,
                    Keywords.BlendMode,
                    Keywords.DebugDisplay,
                    Keywords.Lightmap,
                    Keywords.DynamicLightmap,
                    Keywords.DirectionalLightmapCombined,
                    Keywords.ShadowsShadowmask,
                    Keywords.Decals,
                    Keywords.Shadow,
                    Keywords.LightList,
                },

                // Custom template
                passTemplatePath = $"{HDUtils.GetHDRenderPipelinePath()}Editor/Material/Eye/ShaderGraph/EyePass.template",
                sharedTemplateDirectory = $"{HDUtils.GetHDRenderPipelinePath()}Editor/ShaderGraph",
            };

            public static ShaderPass EyeForwardOnlyTransparent = new ShaderPass()
            {
                // Definition
                displayName = "ForwardOnly",
                referenceName = "SHADERPASS_FORWARD",
                lightMode = "ForwardOnly",
                passInclude = "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassForward.hlsl",
                useInPreview = true,

                // Port mask
                vertexPorts = new List<int>()
                {
                    EyeMasterNode.PositionSlotId,
                    EyeMasterNode.VertexNormalSlotID,
                    EyeMasterNode.VertexTangentSlotID
                },
                pixelPorts = new List<int>
                {
                    EyeMasterNode.AlbedoSlotId,
                    EyeMasterNode.SpecularOcclusionSlotId,
                    EyeMasterNode.NormalSlotId,
                    EyeMasterNode.IrisNormalSlotId,
                    EyeMasterNode.SmoothnessSlotId,
                    EyeMasterNode.IORSlotId,
                    EyeMasterNode.AmbientOcclusionSlotId,
                    EyeMasterNode.MaskSlotId,
                    EyeMasterNode.DiffusionProfileHashSlotId,
                    EyeMasterNode.SubsurfaceMaskSlotId,
                    EyeMasterNode.EmissionSlotId,
                    EyeMasterNode.AlphaSlotId,
                    EyeMasterNode.AlphaClipThresholdSlotId,
                    EyeMasterNode.LightingSlotId,
                    EyeMasterNode.BackLightingSlotId,
                    EyeMasterNode.DepthOffsetSlotId,
                },

                // Render State Overrides
                BlendOverride = "Blend [_SrcBlend] [_DstBlend], [_AlphaSrcBlend] [_AlphaDstBlend]",
                CullOverride = HDSubShaderUtilities.cullModeForward,
                ZTestOverride = HDSubShaderUtilities.zTestDepthEqualForOpaque,
                ZWriteOverride = HDSubShaderUtilities.ZWriteDefault,
                StencilOverride = new List<string>()
                {
                    "Stencil",
                    "{",
                    "    WriteMask [_StencilWriteMask]",
                    "    Ref [_StencilRef]",
                    "    Comp Always",
                    "    Pass Replace",
                    "}"
                },

                // Required fields
                requiredAttributes = new List<string>()
                {
                    "AttributesMesh.normalOS",
                    "AttributesMesh.tangentOS",     // Always present as we require it also in case of Variants lighting
                    "AttributesMesh.uv0",
                    "AttributesMesh.uv1",
                    "AttributesMesh.color",
                    "AttributesMesh.uv2",           // SHADERPASS_LIGHT_TRANSPORT always uses uv2
                    "AttributesMesh.uv3",           // DEBUG_DISPLAY
                },
                requiredVaryings = new List<string>()
                {
                    "FragInputs.tangentToWorld",
                    "FragInputs.positionRWS",
                    "FragInputs.texCoord0",
                    "FragInputs.texCoord1",
                    "FragInputs.texCoord2",
                    "FragInputs.texCoord3",
                    "FragInputs.color",
                },

                // Pass setup
                pragmas = new List<string>()
                {
                    "#pragma target 4.5",
                    "only_renderers d3d11 ps4 xboxone vulkan metal switch",
                    "multi_compile_instancing",
                    "instancing_options renderinglayer",
                },
                defines = new List<string>()
                {
                    "HAS_LIGHTLOOP",
                    "USE_CLUSTERED_LIGHTLIST",
                    "RAYTRACING_SHADER_GRAPH_HIGH",
                },
                includes = new List<string>()
                {
                    "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl",
                    "Packages/com.unity.render-pipelines.core/ShaderLibrary/NormalSurfaceGradient.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/FragInputs.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPass.cs.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Material.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/Lighting.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/LightLoop/LightLoopDef.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Eye/Eye.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/LightLoop/LightLoop.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/BuiltinUtilities.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/MaterialUtilities.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Decal/DecalUtilities.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderGraphFunctions.hlsl",
                },
                keywords = new List<KeywordDescriptor>()
                {
                    Keywords.LodFadeCrossfade,
                    Keywords.SurfaceTypeTransparent,
                    Keywords.DoubleSided,
                    Keywords.BlendMode,
                    Keywords.DebugDisplay,
                    Keywords.Lightmap,
                    Keywords.DynamicLightmap,
                    Keywords.DirectionalLightmapCombined,
                    Keywords.ShadowsShadowmask,
                    Keywords.Decals,
                    Keywords.Shadow,
                },

                // Custom template
                passTemplatePath = $"{HDUtils.GetHDRenderPipelinePath()}Editor/Material/Eye/ShaderGraph/EyePass.template",
                sharedTemplateDirectory = $"{HDUtils.GetHDRenderPipelinePath()}Editor/ShaderGraph",
            };

            public static ShaderPass FabricMETA = new ShaderPass()
            {
                // Definition
                displayName = "META",
                referenceName = "SHADERPASS_LIGHT_TRANSPORT",
                lightMode = "META",
                passInclude = "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassLightTransport.hlsl",
                useInPreview = false,

                // Port mask
                pixelPorts = new List<int>
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

                // Render State Overrides
                CullOverride = "Cull Off",

                // Required fields
                requiredAttributes = new List<string>()
                {
                    "AttributesMesh.normalOS",
                    "AttributesMesh.tangentOS",     // Always present as we require it also in case of anisotropic lighting
                    "AttributesMesh.uv0",
                    "AttributesMesh.uv1",
                    "AttributesMesh.color",
                    "AttributesMesh.uv2",           // SHADERPASS_LIGHT_TRANSPORT always uses uv2
                },

                // Pass setup
                pragmas = new List<string>()
                {
                    "#pragma target 4.5",
                    "only_renderers d3d11 ps4 xboxone vulkan metal switch",
                    "multi_compile_instancing",
                    "instancing_options renderinglayer",
                },
                includes = new List<string>()
                {
                    "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl",
                    "Packages/com.unity.render-pipelines.core/ShaderLibrary/NormalSurfaceGradient.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/FragInputs.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPass.cs.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Material.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Eye/Eye.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/BuiltinUtilities.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/MaterialUtilities.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Decal/DecalUtilities.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderGraphFunctions.hlsl",
                },
                keywords = new List<KeywordDescriptor>()
                {
                    Keywords.LodFadeCrossfade,
                    Keywords.SurfaceTypeTransparent,
                    Keywords.DoubleSided,
                    Keywords.BlendMode,
                },

                // Custom template
                passTemplatePath = $"{HDUtils.GetHDRenderPipelinePath()}Editor/Material/Fabric/ShaderGraph/FabricPass.template",
                sharedTemplateDirectory = $"{HDUtils.GetHDRenderPipelinePath()}Editor/ShaderGraph",
            };

            public static ShaderPass FabricShadowCaster = new ShaderPass()
            {
                // Definition
                displayName = "ShadowCaster",
                referenceName = "SHADERPASS_SHADOWS",
                lightMode = "ShadowCaster",
                passInclude = "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassDepthOnly.hlsl",
                useInPreview = false,

                // Port mask
                vertexPorts = new List<int>()
                {
                    FabricMasterNode.PositionSlotId,
                    FabricMasterNode.VertexNormalSlotId,
                    FabricMasterNode.VertexTangentSlotId
                },
                pixelPorts = new List<int>
                {
                    FabricMasterNode.AlphaSlotId,
                    FabricMasterNode.AlphaClipThresholdSlotId,
                    FabricMasterNode.DepthOffsetSlotId,
                },

                // Render State Overrides
                BlendOverride = "Blend One Zero",
                ZClipOverride = HDSubShaderUtilities.zClipShadowCaster,
                CullOverride = HDSubShaderUtilities.defaultCullMode,
                ZWriteOverride = "ZWrite On",
                ColorMaskOverride = "ColorMask 0",

                // Pass setup
                pragmas = new List<string>()
                {
                    "#pragma target 4.5",
                    "only_renderers d3d11 ps4 xboxone vulkan metal switch",
                    "multi_compile_instancing",
                    "instancing_options renderinglayer",
                },
                includes = new List<string>()
                {
                    "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl",
                    "Packages/com.unity.render-pipelines.core/ShaderLibrary/NormalSurfaceGradient.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/FragInputs.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPass.cs.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Material.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Fabric/Fabric.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/BuiltinUtilities.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/MaterialUtilities.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Decal/DecalUtilities.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderGraphFunctions.hlsl",
                },
                keywords = new List<KeywordDescriptor>()
                {
                    Keywords.LodFadeCrossfade,
                    Keywords.SurfaceTypeTransparent,
                    Keywords.DoubleSided,
                    Keywords.BlendMode,
                },

                // Custom template
                passTemplatePath = $"{HDUtils.GetHDRenderPipelinePath()}Editor/Material/Fabric/ShaderGraph/FabricPass.template",
                sharedTemplateDirectory = $"{HDUtils.GetHDRenderPipelinePath()}Editor/ShaderGraph",
            };

            public static ShaderPass FabricSceneSelection = new ShaderPass()
            {
                // Definition
                displayName = "SceneSelectionPass",
                referenceName = "SHADERPASS_DEPTH_ONLY",
                lightMode = "SceneSelectionPass",
                passInclude = "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassDepthOnly.hlsl",
                useInPreview = false,

                // Port mask
                vertexPorts = new List<int>()
                {
                    FabricMasterNode.PositionSlotId,
                    FabricMasterNode.VertexNormalSlotId,
                    FabricMasterNode.VertexTangentSlotId
                },
                pixelPorts = new List<int>
                {
                    FabricMasterNode.AlphaSlotId,
                    FabricMasterNode.AlphaClipThresholdSlotId,
                    FabricMasterNode.DepthOffsetSlotId,
                },

                // Render State Overrides
                ColorMaskOverride = "ColorMask 0",

                // Pass setup
                pragmas = new List<string>()
                {
                    "#pragma target 4.5",
                    "only_renderers d3d11 ps4 xboxone vulkan metal switch",
                    "multi_compile_instancing",
                    "editor_sync_compilation",
                    "instancing_options renderinglayer",
                },
                defines = new List<string>()
                {
                    "SCENESELECTIONPASS",
                },
                includes = new List<string>()
                {
                    "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl",
                    "Packages/com.unity.render-pipelines.core/ShaderLibrary/NormalSurfaceGradient.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/FragInputs.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPass.cs.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Material.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Fabric/Fabric.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/BuiltinUtilities.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/MaterialUtilities.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Decal/DecalUtilities.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderGraphFunctions.hlsl",
                },
                keywords = new List<KeywordDescriptor>()
                {
                    Keywords.LodFadeCrossfade,
                    Keywords.SurfaceTypeTransparent,
                    Keywords.DoubleSided,
                    Keywords.BlendMode,
                },

                // Custom template
                passTemplatePath = $"{HDUtils.GetHDRenderPipelinePath()}Editor/Material/Fabric/ShaderGraph/FabricPass.template",
                sharedTemplateDirectory = $"{HDUtils.GetHDRenderPipelinePath()}Editor/ShaderGraph",
            };

            public static ShaderPass FabricDepthForwardOnly = new ShaderPass()
            {
                // Definition
                displayName = "DepthForwardOnly",
                referenceName = "SHADERPASS_DEPTH_ONLY",
                lightMode = "DepthForwardOnly",
                passInclude = "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassDepthOnly.hlsl",
                useInPreview = true,

                // Port mask
                vertexPorts = new List<int>()
                {
                    FabricMasterNode.PositionSlotId,
                    FabricMasterNode.VertexNormalSlotId,
                    FabricMasterNode.VertexTangentSlotId
                },
                pixelPorts = new List<int>
                {
                    FabricMasterNode.NormalSlotId,
                    FabricMasterNode.SmoothnessSlotId,
                    FabricMasterNode.AlphaSlotId,
                    FabricMasterNode.AlphaClipThresholdSlotId,
                    FabricMasterNode.DepthOffsetSlotId,
                },

                // Render State Overrides
                CullOverride = HDSubShaderUtilities.defaultCullMode,
                ZWriteOverride = "ZWrite On",
                StencilOverride = new List<string>
                {
                    "Stencil",
                    "{",
                    "    WriteMask [_StencilWriteMaskDepth]",
                    "    Ref [_StencilRefDepth]",
                    "    Comp Always",
                    "    Pass Replace",
                    "}",
                },

                // Required fields
                requiredAttributes = new List<string>()
                {
                    "AttributesMesh.normalOS",
                    "AttributesMesh.tangentOS",     // Always present as we require it also in case of Variants lighting
                    "AttributesMesh.uv0",
                    "AttributesMesh.uv1",
                    "AttributesMesh.color",
                    "AttributesMesh.uv2",           // SHADERPASS_LIGHT_TRANSPORT always uses uv2
                    "AttributesMesh.uv3",           // DEBUG_DISPLAY
                },
                requiredVaryings = new List<string>()
                {
                    "FragInputs.tangentToWorld",
                    "FragInputs.positionRWS",
                    "FragInputs.texCoord0",
                    "FragInputs.texCoord1",
                    "FragInputs.texCoord2",
                    "FragInputs.texCoord3",
                    "FragInputs.color",
                },

                // Pass setup
                pragmas = new List<string>()
                {
                    "#pragma target 4.5",
                    "only_renderers d3d11 ps4 xboxone vulkan metal switch",
                    "multi_compile_instancing",
                    "instancing_options renderinglayer",
                },
                includes = new List<string>()
                {
                    "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl",
                    "Packages/com.unity.render-pipelines.core/ShaderLibrary/NormalSurfaceGradient.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/FragInputs.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPass.cs.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Material.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Fabric/Fabric.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/BuiltinUtilities.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/MaterialUtilities.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Decal/DecalUtilities.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderGraphFunctions.hlsl",
                },
                defines = new List<string>()
                {
                    "WRITE_NORMAL_BUFFER",
                    "RAYTRACING_SHADER_GRAPH_HIGH",
                },
                keywords = new List<KeywordDescriptor>()
                {
                    Keywords.WriteMsaaDepth,
                    Keywords.LodFadeCrossfade,
                    Keywords.SurfaceTypeTransparent,
                    Keywords.DoubleSided,
                    Keywords.BlendMode,
                },

                // Custom template
                passTemplatePath = $"{HDUtils.GetHDRenderPipelinePath()}Editor/Material/Fabric/ShaderGraph/FabricPass.template",
                sharedTemplateDirectory = $"{HDUtils.GetHDRenderPipelinePath()}Editor/ShaderGraph",
            };

            public static ShaderPass FabricMotionVectors = new ShaderPass()
            {
                // Definition
                displayName = "MotionVectors",
                referenceName = "SHADERPASS_MOTION_VECTORS",
                lightMode = "MotionVectors",
                passInclude = "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassMotionVectors.hlsl",
                useInPreview = false,

                // Port mask
                vertexPorts = new List<int>()
                {
                    FabricMasterNode.PositionSlotId,
                    FabricMasterNode.VertexNormalSlotId,
                    FabricMasterNode.VertexTangentSlotId
                },
                pixelPorts = new List<int>
                {
                    FabricMasterNode.NormalSlotId,
                    FabricMasterNode.SmoothnessSlotId,
                    FabricMasterNode.AlphaSlotId,
                    FabricMasterNode.AlphaClipThresholdSlotId,
                    FabricMasterNode.DepthOffsetSlotId,
                },

                // Render State Overrides
                CullOverride = HDSubShaderUtilities.defaultCullMode,
                StencilOverride = new List<string>
                {
                    "Stencil",
                    "{",
                    "    WriteMask [_StencilWriteMaskMV]",
                    "    Ref [_StencilRefMV]",
                    "    Comp Always",
                    "    Pass Replace",
                    "}",
                },

                // Required fields
                requiredAttributes = new List<string>()
                {
                    "AttributesMesh.normalOS",
                    "AttributesMesh.tangentOS",     // Always present as we require it also in case of Variants lighting
                    "AttributesMesh.uv0",
                    "AttributesMesh.uv1",
                    "AttributesMesh.color",
                    "AttributesMesh.uv2",           // SHADERPASS_LIGHT_TRANSPORT always uses uv2
                    "AttributesMesh.uv3",           // DEBUG_DISPLAY
                },
                requiredVaryings = new List<string>()
                {
                    "FragInputs.tangentToWorld",
                    "FragInputs.positionRWS",
                    "FragInputs.texCoord0",
                    "FragInputs.texCoord1",
                    "FragInputs.texCoord2",
                    "FragInputs.texCoord3",
                    "FragInputs.color",
                },

                // Pass setup
                pragmas = new List<string>()
                {
                    "#pragma target 4.5",
                    "only_renderers d3d11 ps4 xboxone vulkan metal switch",
                    "multi_compile_instancing",
                    "instancing_options renderinglayer",
                },
                includes = new List<string>()
                {
                    "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl",
                    "Packages/com.unity.render-pipelines.core/ShaderLibrary/NormalSurfaceGradient.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/FragInputs.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPass.cs.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Material.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Fabric/Fabric.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/BuiltinUtilities.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/MaterialUtilities.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Decal/DecalUtilities.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderGraphFunctions.hlsl",
                },
                defines = new List<string>()
                {
                    "WRITE_NORMAL_BUFFER",
                    "RAYTRACING_SHADER_GRAPH_HIGH",
                },
                keywords = new List<KeywordDescriptor>()
                {
                    Keywords.WriteMsaaDepth,
                    Keywords.LodFadeCrossfade,
                    Keywords.SurfaceTypeTransparent,
                    Keywords.DoubleSided,
                    Keywords.BlendMode,
                },

                // Custom template
                passTemplatePath = $"{HDUtils.GetHDRenderPipelinePath()}Editor/Material/Fabric/ShaderGraph/FabricPass.template",
                sharedTemplateDirectory = $"{HDUtils.GetHDRenderPipelinePath()}Editor/ShaderGraph",
            };

            public static ShaderPass FabricForwardOnlyOpaque = new ShaderPass()
            {
                // Definition
                displayName = "ForwardOnly",
                referenceName = "SHADERPASS_FORWARD",
                lightMode = "ForwardOnly",
                passInclude = "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassForward.hlsl",
                useInPreview = true,

                // Port mask
                vertexPorts = new List<int>()
                {
                    FabricMasterNode.PositionSlotId,
                    FabricMasterNode.VertexNormalSlotId,
                    FabricMasterNode.VertexTangentSlotId
                },
                pixelPorts = new List<int>
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

                // Render State Overrides
                BlendOverride = "Blend [_SrcBlend] [_DstBlend], [_AlphaSrcBlend] [_AlphaDstBlend]",
                CullOverride = HDSubShaderUtilities.cullModeForward,
                ZTestOverride = HDSubShaderUtilities.zTestDepthEqualForOpaque,
                ZWriteOverride = HDSubShaderUtilities.ZWriteDefault,
                StencilOverride = new List<string>()
                {
                    "Stencil",
                    "{",
                    "    WriteMask [_StencilWriteMask]",
                    "    Ref [_StencilRef]",
                    "    Comp Always",
                    "    Pass Replace",
                    "}"
                },

                // Required fields
                requiredAttributes = new List<string>()
                {
                    "AttributesMesh.normalOS",
                    "AttributesMesh.tangentOS",     // Always present as we require it also in case of Variants lighting
                    "AttributesMesh.uv0",
                    "AttributesMesh.uv1",
                    "AttributesMesh.color",
                    "AttributesMesh.uv2",           // SHADERPASS_LIGHT_TRANSPORT always uses uv2
                    "AttributesMesh.uv3",           // DEBUG_DISPLAY
                },
                requiredVaryings = new List<string>()
                {
                    "FragInputs.tangentToWorld",
                    "FragInputs.positionRWS",
                    "FragInputs.texCoord0",
                    "FragInputs.texCoord1",
                    "FragInputs.texCoord2",
                    "FragInputs.texCoord3",
                    "FragInputs.color",
                },

                // Pass setup
                pragmas = new List<string>()
                {
                    "#pragma target 4.5",
                    "only_renderers d3d11 ps4 xboxone vulkan metal switch",
                    "multi_compile_instancing",
                    "instancing_options renderinglayer",
                },
                defines = new List<string>()
                {
                    "HAS_LIGHTLOOP",
                },
                includes = new List<string>()
                {
                    "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl",
                    "Packages/com.unity.render-pipelines.core/ShaderLibrary/NormalSurfaceGradient.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/FragInputs.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPass.cs.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Material.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/Lighting.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/LightLoop/LightLoopDef.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Fabric/Fabric.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/LightLoop/LightLoop.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/BuiltinUtilities.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/MaterialUtilities.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Decal/DecalUtilities.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderGraphFunctions.hlsl",
                },
                keywords = new List<KeywordDescriptor>()
                {
                    Keywords.LodFadeCrossfade,
                    Keywords.SurfaceTypeTransparent,
                    Keywords.DoubleSided,
                    Keywords.BlendMode,
                    Keywords.DebugDisplay,
                    Keywords.Lightmap,
                    Keywords.DynamicLightmap,
                    Keywords.DirectionalLightmapCombined,
                    Keywords.ShadowsShadowmask,
                    Keywords.Decals,
                    Keywords.Shadow,
                    Keywords.LightList,
                },

                // Custom template
                passTemplatePath = $"{HDUtils.GetHDRenderPipelinePath()}Editor/Material/Fabric/ShaderGraph/FabricPass.template",
                sharedTemplateDirectory = $"{HDUtils.GetHDRenderPipelinePath()}Editor/ShaderGraph",
            };

            public static ShaderPass FabricForwardOnlyTransparent = new ShaderPass()
            {
                // Definition
                displayName = "ForwardOnly",
                referenceName = "SHADERPASS_FORWARD",
                lightMode = "ForwardOnly",
                passInclude = "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassForward.hlsl",
                useInPreview = true,

                // Port mask
                vertexPorts = new List<int>()
                {
                    FabricMasterNode.PositionSlotId,
                    FabricMasterNode.VertexNormalSlotId,
                    FabricMasterNode.VertexTangentSlotId
                },
                pixelPorts = new List<int>
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

                // Render State Overrides
                BlendOverride = "Blend [_SrcBlend] [_DstBlend], [_AlphaSrcBlend] [_AlphaDstBlend]",
                CullOverride = HDSubShaderUtilities.cullModeForward,
                ZTestOverride = HDSubShaderUtilities.zTestDepthEqualForOpaque,
                ZWriteOverride = HDSubShaderUtilities.ZWriteDefault,
                StencilOverride = new List<string>()
                {
                    "Stencil",
                    "{",
                    "    WriteMask [_StencilWriteMask]",
                    "    Ref [_StencilRef]",
                    "    Comp Always",
                    "    Pass Replace",
                    "}"
                },

                // Required fields
                requiredAttributes = new List<string>()
                {
                    "AttributesMesh.normalOS",
                    "AttributesMesh.tangentOS",     // Always present as we require it also in case of Variants lighting
                    "AttributesMesh.uv0",
                    "AttributesMesh.uv1",
                    "AttributesMesh.color",
                    "AttributesMesh.uv2",           // SHADERPASS_LIGHT_TRANSPORT always uses uv2
                    "AttributesMesh.uv3",           // DEBUG_DISPLAY
                },
                requiredVaryings = new List<string>()
                {
                    "FragInputs.tangentToWorld",
                    "FragInputs.positionRWS",
                    "FragInputs.texCoord0",
                    "FragInputs.texCoord1",
                    "FragInputs.texCoord2",
                    "FragInputs.texCoord3",
                    "FragInputs.color",
                },

                // Pass setup
                pragmas = new List<string>()
                {
                    "#pragma target 4.5",
                    "only_renderers d3d11 ps4 xboxone vulkan metal switch",
                    "multi_compile_instancing",
                    "instancing_options renderinglayer",
                },
                defines = new List<string>()
                {
                    "HAS_LIGHTLOOP",
                    "USE_CLUSTERED_LIGHTLIST",
                    "RAYTRACING_SHADER_GRAPH_HIGH",
                },
                includes = new List<string>()
                {
                    "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl",
                    "Packages/com.unity.render-pipelines.core/ShaderLibrary/NormalSurfaceGradient.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/FragInputs.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPass.cs.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Material.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/Lighting.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/LightLoop/LightLoopDef.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Fabric/Fabric.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/LightLoop/LightLoop.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/BuiltinUtilities.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/MaterialUtilities.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Decal/DecalUtilities.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderGraphFunctions.hlsl",
                },
                keywords = new List<KeywordDescriptor>()
                {
                    Keywords.LodFadeCrossfade,
                    Keywords.SurfaceTypeTransparent,
                    Keywords.DoubleSided,
                    Keywords.BlendMode,
                    Keywords.DebugDisplay,
                    Keywords.Lightmap,
                    Keywords.DynamicLightmap,
                    Keywords.DirectionalLightmapCombined,
                    Keywords.ShadowsShadowmask,
                    Keywords.Decals,
                    Keywords.Shadow,
                },

                // Custom template
                passTemplatePath = $"{HDUtils.GetHDRenderPipelinePath()}Editor/Material/Fabric/ShaderGraph/FabricPass.template",
                sharedTemplateDirectory = $"{HDUtils.GetHDRenderPipelinePath()}Editor/ShaderGraph",
            };

            public static ShaderPass HairMETA = new ShaderPass()
            {
                // Definition
                displayName = "META",
                referenceName = "SHADERPASS_LIGHT_TRANSPORT",
                lightMode = "META",
                passInclude = "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassLightTransport.hlsl",
                useInPreview = false,

                // Port mask
                pixelPorts = new List<int>
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

                // Render State Overrides
                CullOverride = "Cull Off",

                // Required fields
                requiredAttributes = new List<string>()
                {
                    "AttributesMesh.normalOS",
                    "AttributesMesh.tangentOS",     // Always present as we require it also in case of anisotropic lighting
                    "AttributesMesh.uv0",
                    "AttributesMesh.uv1",
                    "AttributesMesh.color",
                    "AttributesMesh.uv2",           // SHADERPASS_LIGHT_TRANSPORT always uses uv2
                },

                // Pass setup
                pragmas = new List<string>()
                {
                    "#pragma target 4.5",
                    "only_renderers d3d11 ps4 xboxone vulkan metal switch",
                    "multi_compile_instancing",
                    "instancing_options renderinglayer",
                },
                includes = new List<string>()
                {
                    "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl",
                    "Packages/com.unity.render-pipelines.core/ShaderLibrary/NormalSurfaceGradient.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/FragInputs.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPass.cs.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Material.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Hair/Hair.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/BuiltinUtilities.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/MaterialUtilities.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Decal/DecalUtilities.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderGraphFunctions.hlsl",
                },
                keywords = new List<KeywordDescriptor>()
                {
                    Keywords.LodFadeCrossfade,
                    Keywords.SurfaceTypeTransparent,
                    Keywords.DoubleSided,
                    Keywords.BlendMode,
                },

                // Custom template
                passTemplatePath = $"{HDUtils.GetHDRenderPipelinePath()}Editor/Material/Hair/ShaderGraph/HairPass.template",
                sharedTemplateDirectory = $"{HDUtils.GetHDRenderPipelinePath()}Editor/ShaderGraph",
            };

            public static ShaderPass HairShadowCaster = new ShaderPass()
            {
                // Definition
                displayName = "ShadowCaster",
                referenceName = "SHADERPASS_SHADOWS",
                lightMode = "ShadowCaster",
                passInclude = "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassDepthOnly.hlsl",
                useInPreview = false,

                // Port mask
                vertexPorts = new List<int>()
                {
                    HairMasterNode.PositionSlotId,
                    HairMasterNode.VertexNormalSlotId,
                    HairMasterNode.VertexTangentSlotId
                },
                pixelPorts = new List<int>
                {
                    HairMasterNode.AlphaSlotId,
                    HairMasterNode.AlphaClipThresholdSlotId,
                    HairMasterNode.AlphaClipThresholdShadowSlotId,
                    HairMasterNode.DepthOffsetSlotId,
                },

                // Render State Overrides
                BlendOverride = "Blend One Zero",
                ZWriteOverride = "ZWrite On",
                ColorMaskOverride = "ColorMask 0",
                ZClipOverride = HDSubShaderUtilities.zClipShadowCaster,
                CullOverride = HDSubShaderUtilities.defaultCullMode,

                // Pass setup
                pragmas = new List<string>()
                {
                    "#pragma target 4.5",
                    "only_renderers d3d11 ps4 xboxone vulkan metal switch",
                    "multi_compile_instancing",
                    "instancing_options renderinglayer",
                },
                includes = new List<string>()
                {
                    "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl",
                    "Packages/com.unity.render-pipelines.core/ShaderLibrary/NormalSurfaceGradient.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/FragInputs.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPass.cs.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Material.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Hair/Hair.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/BuiltinUtilities.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/MaterialUtilities.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Decal/DecalUtilities.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderGraphFunctions.hlsl",
                },
                keywords = new List<KeywordDescriptor>()
                {
                    Keywords.LodFadeCrossfade,
                    Keywords.SurfaceTypeTransparent,
                    Keywords.DoubleSided,
                    Keywords.BlendMode,
                },

                // Custom template
                passTemplatePath = $"{HDUtils.GetHDRenderPipelinePath()}Editor/Material/Hair/ShaderGraph/HairPass.template",
                sharedTemplateDirectory = $"{HDUtils.GetHDRenderPipelinePath()}Editor/ShaderGraph",
            };

            public static ShaderPass HairSceneSelection = new ShaderPass()
            {
                // Definition
                displayName = "SceneSelectionPass",
                referenceName = "SHADERPASS_DEPTH_ONLY",
                lightMode = "SceneSelectionPass",
                passInclude = "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassDepthOnly.hlsl",
                useInPreview = false,

                // Port mask
                vertexPorts = new List<int>()
                {
                    HairMasterNode.PositionSlotId,
                    HairMasterNode.VertexNormalSlotId,
                    HairMasterNode.VertexTangentSlotId
                },
                pixelPorts = new List<int>
                {
                    HairMasterNode.AlphaSlotId,
                    HairMasterNode.AlphaClipThresholdSlotId,
                    HairMasterNode.DepthOffsetSlotId,
                },

                // Render State Overrides
                ColorMaskOverride = "ColorMask 0",

                // Pass setup
                pragmas = new List<string>()
                {
                    "#pragma target 4.5",
                    "only_renderers d3d11 ps4 xboxone vulkan metal switch",
                    "multi_compile_instancing",
                    "instancing_options renderinglayer",
                    "editor_sync_compilation",
                },
                defines = new List<string>()
                {
                    "SCENESELECTIONPASS",
                },
                includes = new List<string>()
                {
                    "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl",
                    "Packages/com.unity.render-pipelines.core/ShaderLibrary/NormalSurfaceGradient.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/FragInputs.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPass.cs.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Material.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Hair/Hair.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/BuiltinUtilities.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/MaterialUtilities.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Decal/DecalUtilities.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderGraphFunctions.hlsl",
                },
                keywords = new List<KeywordDescriptor>()
                {
                    Keywords.LodFadeCrossfade,
                    Keywords.SurfaceTypeTransparent,
                    Keywords.DoubleSided,
                    Keywords.BlendMode,
                },

                // Custom template
                passTemplatePath = $"{HDUtils.GetHDRenderPipelinePath()}Editor/Material/Hair/ShaderGraph/HairPass.template",
                sharedTemplateDirectory = $"{HDUtils.GetHDRenderPipelinePath()}Editor/ShaderGraph",
            };

            public static ShaderPass HairDepthForwardOnly = new ShaderPass()
            {
                // Definition
                displayName = "DepthForwardOnly",
                referenceName = "SHADERPASS_DEPTH_ONLY",
                lightMode = "DepthForwardOnly",
                passInclude = "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassDepthOnly.hlsl",
                useInPreview = true,

                // Port mask
                vertexPorts = new List<int>()
                {
                    HairMasterNode.PositionSlotId,
                    HairMasterNode.VertexNormalSlotId,
                    HairMasterNode.VertexTangentSlotId
                },
                pixelPorts = new List<int>
                {
                    HairMasterNode.NormalSlotId,
                    HairMasterNode.SmoothnessSlotId,
                    HairMasterNode.AlphaSlotId,
                    HairMasterNode.AlphaClipThresholdSlotId,
                    HairMasterNode.DepthOffsetSlotId,
                },

                // Render State Overrides
                ZWriteOverride = "ZWrite On",
                StencilOverride = new List<string>
                {
                    "Stencil",
                    "{",
                    "    WriteMask [_StencilWriteMaskDepth]",
                    "    Ref [_StencilRefDepth]",
                    "    Comp Always",
                    "    Pass Replace",
                    "}",
                },

                // Required fields
                requiredAttributes = new List<string>()
                {
                    "AttributesMesh.normalOS",
                    "AttributesMesh.tangentOS",     // Always present as we require it also in case of Variants lighting
                    "AttributesMesh.uv0",
                    "AttributesMesh.uv1",
                    "AttributesMesh.color",
                    "AttributesMesh.uv2",           // SHADERPASS_LIGHT_TRANSPORT always uses uv2
                    "AttributesMesh.uv3",           // DEBUG_DISPLAY
                },
                requiredVaryings = new List<string>()
                {
                    "FragInputs.tangentToWorld",
                    "FragInputs.positionRWS",
                    "FragInputs.texCoord0",
                    "FragInputs.texCoord1",
                    "FragInputs.texCoord2",
                    "FragInputs.texCoord3",
                    "FragInputs.color",
                },

                // Pass setup
                pragmas = new List<string>()
                {
                    "#pragma target 4.5",
                    "only_renderers d3d11 ps4 xboxone vulkan metal switch",
                    "multi_compile_instancing",
                    "instancing_options renderinglayer",
                },
                defines = new List<string>()
                {
                    "WRITE_NORMAL_BUFFER",
                    "RAYTRACING_SHADER_GRAPH_HIGH",
                },
                includes = new List<string>()
                {
                    "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl",
                    "Packages/com.unity.render-pipelines.core/ShaderLibrary/NormalSurfaceGradient.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/FragInputs.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPass.cs.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Material.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Hair/Hair.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/BuiltinUtilities.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/MaterialUtilities.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Decal/DecalUtilities.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderGraphFunctions.hlsl",
                },
                keywords = new List<KeywordDescriptor>()
                {
                    Keywords.WriteMsaaDepth,
                    Keywords.LodFadeCrossfade,
                    Keywords.SurfaceTypeTransparent,
                    Keywords.DoubleSided,
                    Keywords.BlendMode,
                },

                // Custom template
                passTemplatePath = $"{HDUtils.GetHDRenderPipelinePath()}Editor/Material/Hair/ShaderGraph/HairPass.template",
                sharedTemplateDirectory = $"{HDUtils.GetHDRenderPipelinePath()}Editor/ShaderGraph",
            };

            public static ShaderPass HairMotionVectors = new ShaderPass()
            {
                // Definition
                displayName = "MotionVectors",
                referenceName = "SHADERPASS_MOTION_VECTORS",
                lightMode = "MotionVectors",
                passInclude = "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassMotionVectors.hlsl",
                useInPreview = false,

                // Port mask
                vertexPorts = new List<int>()
                {
                    HairMasterNode.PositionSlotId,
                    HairMasterNode.VertexNormalSlotId,
                    HairMasterNode.VertexTangentSlotId
                },
                pixelPorts = new List<int>
                {
                    HairMasterNode.NormalSlotId,
                    HairMasterNode.SmoothnessSlotId,
                    HairMasterNode.AlphaSlotId,
                    HairMasterNode.AlphaClipThresholdSlotId,
                    HairMasterNode.DepthOffsetSlotId,
                },

                // Render State Overrides
                StencilOverride = new List<string>
                {
                    "Stencil",
                    "{",
                    "    WriteMask [_StencilWriteMaskMV]",
                    "    Ref [_StencilRefMV]",
                    "    Comp Always",
                    "    Pass Replace",
                    "}",
                },

                // Required fields
                requiredAttributes = new List<string>()
                {
                    "AttributesMesh.normalOS",
                    "AttributesMesh.tangentOS",     // Always present as we require it also in case of Variants lighting
                    "AttributesMesh.uv0",
                    "AttributesMesh.uv1",
                    "AttributesMesh.color",
                    "AttributesMesh.uv2",           // SHADERPASS_LIGHT_TRANSPORT always uses uv2
                    "AttributesMesh.uv3",           // DEBUG_DISPLAY
                },
                requiredVaryings = new List<string>()
                {
                    "FragInputs.tangentToWorld",
                    "FragInputs.positionRWS",
                    "FragInputs.texCoord0",
                    "FragInputs.texCoord1",
                    "FragInputs.texCoord2",
                    "FragInputs.texCoord3",
                    "FragInputs.color",
                },

                // Pass setup
                pragmas = new List<string>()
                {
                    "#pragma target 4.5",
                    "only_renderers d3d11 ps4 xboxone vulkan metal switch",
                    "multi_compile_instancing",
                    "instancing_options renderinglayer",
                },
                includes = new List<string>()
                {
                    "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl",
                    "Packages/com.unity.render-pipelines.core/ShaderLibrary/NormalSurfaceGradient.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/FragInputs.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPass.cs.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Material.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Hair/Hair.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/BuiltinUtilities.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/MaterialUtilities.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Decal/DecalUtilities.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderGraphFunctions.hlsl",
                },
                defines = new List<string>()
                {
                    "WRITE_NORMAL_BUFFER",
                    "RAYTRACING_SHADER_GRAPH_HIGH",
                },
                keywords = new List<KeywordDescriptor>()
                {
                    Keywords.WriteMsaaDepth,
                    Keywords.LodFadeCrossfade,
                    Keywords.SurfaceTypeTransparent,
                    Keywords.DoubleSided,
                    Keywords.BlendMode,
                },

                // Custom template
                passTemplatePath = $"{HDUtils.GetHDRenderPipelinePath()}Editor/Material/Hair/ShaderGraph/HairPass.template",
                sharedTemplateDirectory = $"{HDUtils.GetHDRenderPipelinePath()}Editor/ShaderGraph",
            };

            public static ShaderPass HairTransparentDepthPrepass = new ShaderPass()
            {
                // Definition
                displayName = "TransparentDepthPrepass",
                referenceName = "SHADERPASS_DEPTH_ONLY",
                lightMode = "TransparentDepthPrepass",
                passInclude = "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassDepthOnly.hlsl",
                useInPreview = true,

                // Port mask
                vertexPorts = new List<int>()
                {
                    HairMasterNode.PositionSlotId,
                    HairMasterNode.VertexNormalSlotId,
                    HairMasterNode.VertexTangentSlotId
                },
                pixelPorts = new List<int>
                {
                    HairMasterNode.AlphaSlotId,
                    HairMasterNode.AlphaClipThresholdDepthPrepassSlotId,
                    HairMasterNode.DepthOffsetSlotId,
                },

                // Render State Overrides
                BlendOverride = "Blend One Zero",
                ZWriteOverride = "ZWrite On",
                ColorMaskOverride = "ColorMask 0",
                CullOverride = HDSubShaderUtilities.defaultCullMode,

                // Pass setup
                pragmas = new List<string>()
                {
                    "#pragma target 4.5",
                    "only_renderers d3d11 ps4 xboxone vulkan metal switch",
                    "multi_compile_instancing",
                    "instancing_options renderinglayer",
                },
                includes = new List<string>()
                {
                    "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl",
                    "Packages/com.unity.render-pipelines.core/ShaderLibrary/NormalSurfaceGradient.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/FragInputs.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPass.cs.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Material.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Hair/Hair.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/BuiltinUtilities.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/MaterialUtilities.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Decal/DecalUtilities.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderGraphFunctions.hlsl",
                },
                defines = new List<string>()
                {
                    "CUTOFF_TRANSPARENT_DEPTH_PREPASS",
                },
                keywords = new List<KeywordDescriptor>()
                {
                    Keywords.LodFadeCrossfade,
                    Keywords.SurfaceTypeTransparent,
                    Keywords.DoubleSided,
                    Keywords.BlendMode,
                },

                // Custom template
                passTemplatePath = $"{HDUtils.GetHDRenderPipelinePath()}Editor/Material/Hair/ShaderGraph/HairPass.template",
                sharedTemplateDirectory = $"{HDUtils.GetHDRenderPipelinePath()}Editor/ShaderGraph",
            };

            public static ShaderPass HairTransparentBackface = new ShaderPass()
            {
                // Definition
                displayName = "TransparentBackface",
                referenceName = "SHADERPASS_FORWARD",
                lightMode = "TransparentBackface",
                passInclude = "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassForward.hlsl",
                useInPreview = true,

                // Port mask
                vertexPorts = new List<int>()
                {
                    HairMasterNode.PositionSlotId,
                    HairMasterNode.VertexNormalSlotId,
                    HairMasterNode.VertexTangentSlotId
                },
                pixelPorts = new List<int>
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

                // Render State Overrides
                BlendOverride = "Blend [_SrcBlend] [_DstBlend], [_AlphaSrcBlend] [_AlphaDstBlend]",
                CullOverride = "Cull Front",
                ZTestOverride = HDSubShaderUtilities.zTestTransparent,
                ColorMaskOverride = "ColorMask [_ColorMaskTransparentVel] 1",
                ZWriteOverride = HDSubShaderUtilities.ZWriteDefault,

                // Required fields
                requiredVaryings = new List<string>()
                {
                    "FragInputs.tangentToWorld",
                    "FragInputs.positionRWS",
                    "FragInputs.texCoord1",
                    "FragInputs.texCoord2",
                },

                // Pass setup
                pragmas = new List<string>()
                {
                    "#pragma target 4.5",
                    "only_renderers d3d11 ps4 xboxone vulkan metal switch",
                    "multi_compile_instancing",
                    "instancing_options renderinglayer",
                },
                defines = new List<string>()
                {
                    "HAS_LIGHTLOOP",
                    "USE_CLUSTERED_LIGHTLIST",
                    "RAYTRACING_SHADER_GRAPH_HIGH",
                },
                includes = new List<string>()
                {
                    "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl",
                    "Packages/com.unity.render-pipelines.core/ShaderLibrary/NormalSurfaceGradient.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/FragInputs.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPass.cs.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Material.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/Lighting.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/LightLoop/LightLoopDef.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Hair/Hair.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/LightLoop/LightLoop.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/BuiltinUtilities.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/MaterialUtilities.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Decal/DecalUtilities.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderGraphFunctions.hlsl",
                },
                keywords = new List<KeywordDescriptor>()
                {
                    Keywords.LodFadeCrossfade,
                    Keywords.SurfaceTypeTransparent,
                    Keywords.DoubleSided,
                    Keywords.BlendMode,
                    Keywords.DebugDisplay,
                    Keywords.Lightmap,
                    Keywords.DynamicLightmap,
                    Keywords.DirectionalLightmapCombined,
                    Keywords.ShadowsShadowmask,
                    Keywords.Decals,
                    Keywords.Shadow,
                },

                // Custom template
                passTemplatePath = $"{HDUtils.GetHDRenderPipelinePath()}Editor/Material/Hair/ShaderGraph/HairPass.template",
                sharedTemplateDirectory = $"{HDUtils.GetHDRenderPipelinePath()}Editor/ShaderGraph",
            };

            public static ShaderPass HairForwardOnlyOpaque = new ShaderPass()
            {
                // Definition
                displayName = "ForwardOnly",
                referenceName = "SHADERPASS_FORWARD",
                lightMode = "ForwardOnly",
                passInclude = "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassForward.hlsl",
                useInPreview = true,

                // Port mask
                vertexPorts = new List<int>()
                {
                    HairMasterNode.PositionSlotId,
                    HairMasterNode.VertexNormalSlotId,
                    HairMasterNode.VertexTangentSlotId
                },
                pixelPorts = new List<int>
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

                // Render State Overrides
                BlendOverride = "Blend [_SrcBlend] [_DstBlend], [_AlphaSrcBlend] [_AlphaDstBlend]",
                CullOverride = HDSubShaderUtilities.cullModeForward,
                ZTestOverride = HDSubShaderUtilities.zTestDepthEqualForOpaque,
                ZWriteOverride = HDSubShaderUtilities.ZWriteDefault,
                ColorMaskOverride = "ColorMask [_ColorMaskTransparentVel] 1",
                StencilOverride = new List<string>()
                {
                    "Stencil",
                    "{",
                    "    WriteMask [_StencilWriteMask]",
                    "    Ref [_StencilRef]",
                    "    Comp Always",
                    "    Pass Replace",
                    "}"
                },

                // Required fields
                requiredAttributes = new List<string>()
                {
                    "AttributesMesh.normalOS",
                    "AttributesMesh.tangentOS",     // Always present as we require it also in case of Variants lighting
                    "AttributesMesh.uv0",
                    "AttributesMesh.uv1",
                    "AttributesMesh.color",
                    "AttributesMesh.uv2",           // SHADERPASS_LIGHT_TRANSPORT always uses uv2
                    "AttributesMesh.uv3",           // DEBUG_DISPLAY
                },
                requiredVaryings = new List<string>()
                {
                    "FragInputs.tangentToWorld",
                    "FragInputs.positionRWS",
                    "FragInputs.texCoord0",
                    "FragInputs.texCoord1",
                    "FragInputs.texCoord2",
                    "FragInputs.texCoord3",
                    "FragInputs.color",
                },

                // Pass setup
                pragmas = new List<string>()
                {
                    "#pragma target 4.5",
                    "only_renderers d3d11 ps4 xboxone vulkan metal switch",
                    "multi_compile_instancing",
                    "instancing_options renderinglayer",
                },
                defines = new List<string>()
                {
                    "HAS_LIGHTLOOP",
                },
                includes = new List<string>()
                {
                    "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl",
                    "Packages/com.unity.render-pipelines.core/ShaderLibrary/NormalSurfaceGradient.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/FragInputs.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPass.cs.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Material.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/Lighting.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/LightLoop/LightLoopDef.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Hair/Hair.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/LightLoop/LightLoop.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/BuiltinUtilities.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/MaterialUtilities.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Decal/DecalUtilities.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderGraphFunctions.hlsl",
                },
                keywords = new List<KeywordDescriptor>()
                {
                    Keywords.LodFadeCrossfade,
                    Keywords.SurfaceTypeTransparent,
                    Keywords.DoubleSided,
                    Keywords.BlendMode,
                    Keywords.DebugDisplay,
                    Keywords.Lightmap,
                    Keywords.DynamicLightmap,
                    Keywords.DirectionalLightmapCombined,
                    Keywords.ShadowsShadowmask,
                    Keywords.Decals,
                    Keywords.Shadow,
                    Keywords.LightList,
                },

                // Custom template
                passTemplatePath = $"{HDUtils.GetHDRenderPipelinePath()}Editor/Material/Hair/ShaderGraph/HairPass.template",
                sharedTemplateDirectory = $"{HDUtils.GetHDRenderPipelinePath()}Editor/ShaderGraph",
            };

            public static ShaderPass HairForwardOnlyTransparent = new ShaderPass()
            {
                // Definition
                displayName = "ForwardOnly",
                referenceName = "SHADERPASS_FORWARD",
                lightMode = "ForwardOnly",
                passInclude = "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassForward.hlsl",
                useInPreview = true,

                // Port mask
                vertexPorts = new List<int>()
                {
                    HairMasterNode.PositionSlotId,
                    HairMasterNode.VertexNormalSlotId,
                    HairMasterNode.VertexTangentSlotId
                },
                pixelPorts = new List<int>
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

                // Render State Overrides
                BlendOverride = "Blend [_SrcBlend] [_DstBlend], [_AlphaSrcBlend] [_AlphaDstBlend]",
                CullOverride = HDSubShaderUtilities.cullModeForward,
                ZTestOverride = HDSubShaderUtilities.zTestDepthEqualForOpaque,
                ZWriteOverride = HDSubShaderUtilities.ZWriteDefault,
                ColorMaskOverride = "ColorMask [_ColorMaskTransparentVel] 1",
                StencilOverride = new List<string>()
                {
                    "Stencil",
                    "{",
                    "    WriteMask [_StencilWriteMask]",
                    "    Ref [_StencilRef]",
                    "    Comp Always",
                    "    Pass Replace",
                    "}"
                },

                // Required fields
                requiredAttributes = new List<string>()
                {
                    "AttributesMesh.normalOS",
                    "AttributesMesh.tangentOS",     // Always present as we require it also in case of Variants lighting
                    "AttributesMesh.uv0",
                    "AttributesMesh.uv1",
                    "AttributesMesh.color",
                    "AttributesMesh.uv2",           // SHADERPASS_LIGHT_TRANSPORT always uses uv2
                    "AttributesMesh.uv3",           // DEBUG_DISPLAY
                },
                requiredVaryings = new List<string>()
                {
                    "FragInputs.tangentToWorld",
                    "FragInputs.positionRWS",
                    "FragInputs.texCoord0",
                    "FragInputs.texCoord1",
                    "FragInputs.texCoord2",
                    "FragInputs.texCoord3",
                    "FragInputs.color",
                },

                // Pass setup
                pragmas = new List<string>()
                {
                    "#pragma target 4.5",
                    "only_renderers d3d11 ps4 xboxone vulkan metal switch",
                    "multi_compile_instancing",
                    "instancing_options renderinglayer",
                },
                defines = new List<string>()
                {
                    "HAS_LIGHTLOOP",
                    "USE_CLUSTERED_LIGHTLIST",
                    "RAYTRACING_SHADER_GRAPH_HIGH",
                },
                includes = new List<string>()
                {
                    "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl",
                    "Packages/com.unity.render-pipelines.core/ShaderLibrary/NormalSurfaceGradient.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/FragInputs.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPass.cs.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Material.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/Lighting.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/LightLoop/LightLoopDef.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Hair/Hair.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/LightLoop/LightLoop.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/BuiltinUtilities.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/MaterialUtilities.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Decal/DecalUtilities.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderGraphFunctions.hlsl",
                },
                keywords = new List<KeywordDescriptor>()
                {
                    Keywords.LodFadeCrossfade,
                    Keywords.SurfaceTypeTransparent,
                    Keywords.DoubleSided,
                    Keywords.BlendMode,
                    Keywords.DebugDisplay,
                    Keywords.Lightmap,
                    Keywords.DynamicLightmap,
                    Keywords.DirectionalLightmapCombined,
                    Keywords.ShadowsShadowmask,
                    Keywords.Decals,
                    Keywords.Shadow,
                },

                // Custom template
                passTemplatePath = $"{HDUtils.GetHDRenderPipelinePath()}Editor/Material/Hair/ShaderGraph/HairPass.template",
                sharedTemplateDirectory = $"{HDUtils.GetHDRenderPipelinePath()}Editor/ShaderGraph",
            };

            public static ShaderPass HairTransparentDepthPostpass = new ShaderPass()
            {
                // Definition
                displayName = "TransparentDepthPostpass",
                referenceName = "SHADERPASS_DEPTH_ONLY",
                lightMode = "TransparentDepthPostpass",
                passInclude = "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassDepthOnly.hlsl",
                useInPreview = true,

                // Port mask
                vertexPorts = new List<int>()
                {
                    HairMasterNode.PositionSlotId,
                    HairMasterNode.VertexNormalSlotId,
                    HairMasterNode.VertexTangentSlotId
                },
                pixelPorts = new List<int>
                {
                    HairMasterNode.AlphaSlotId,
                    HairMasterNode.AlphaClipThresholdDepthPostpassSlotId,
                    HairMasterNode.DepthOffsetSlotId,
                },

                // Render State Overrides
                BlendOverride = "Blend One Zero",
                ZWriteOverride = "ZWrite On",
                ZTestOverride = "ZTest LEqual",
                ColorMaskOverride = "ColorMask 0",
                CullOverride = HDSubShaderUtilities.defaultCullMode,

                // Pass setup
                pragmas = new List<string>()
                {
                    "#pragma target 4.5",
                    "only_renderers d3d11 ps4 xboxone vulkan metal switch",
                    "multi_compile_instancing",
                    "instancing_options renderinglayer",
                },
                includes = new List<string>()
                {
                    "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl",
                    "Packages/com.unity.render-pipelines.core/ShaderLibrary/NormalSurfaceGradient.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/FragInputs.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPass.cs.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Material.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Hair/Hair.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/BuiltinUtilities.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/MaterialUtilities.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Decal/DecalUtilities.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderGraphFunctions.hlsl",
                },
                defines = new List<string>()
                {
                    "CUTOFF_TRANSPARENT_DEPTH_POSTPASS",
                    "RAYTRACING_SHADER_GRAPH_HIGH",
                },
                keywords = new List<KeywordDescriptor>()
                {
                    Keywords.LodFadeCrossfade,
                    Keywords.SurfaceTypeTransparent,
                    Keywords.DoubleSided,
                    Keywords.BlendMode,
                },

                // Custom template
                passTemplatePath = $"{HDUtils.GetHDRenderPipelinePath()}Editor/Material/Hair/ShaderGraph/HairPass.template",
                sharedTemplateDirectory = $"{HDUtils.GetHDRenderPipelinePath()}Editor/ShaderGraph",
            };

            public static ShaderPass StackLitMETA = new ShaderPass()
            {
                // Definition
                displayName = "META",
                referenceName = "SHADERPASS_LIGHT_TRANSPORT",
                lightMode = "META",
                passInclude = "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassLightTransport.hlsl",
                useInPreview = false,

                // Port mask
                pixelPorts = new List<int>
                {
                    StackLitMasterNode.BaseColorSlotId,
                    StackLitMasterNode.NormalSlotId,
                    StackLitMasterNode.BentNormalSlotId,
                    StackLitMasterNode.TangentSlotId,
                    StackLitMasterNode.SubsurfaceMaskSlotId,
                    StackLitMasterNode.ThicknessSlotId,
                    StackLitMasterNode.DiffusionProfileHashSlotId,
                    StackLitMasterNode.IridescenceMaskSlotId,
                    StackLitMasterNode.IridescenceThicknessSlotId,
                    StackLitMasterNode.IridescenceCoatFixupTIRSlotId,
                    StackLitMasterNode.IridescenceCoatFixupTIRClampSlotId,
                    StackLitMasterNode.SpecularColorSlotId,
                    StackLitMasterNode.DielectricIorSlotId,
                    StackLitMasterNode.MetallicSlotId,
                    StackLitMasterNode.EmissionSlotId,
                    StackLitMasterNode.SmoothnessASlotId,
                    StackLitMasterNode.SmoothnessBSlotId,
                    StackLitMasterNode.AmbientOcclusionSlotId,
                    StackLitMasterNode.AlphaSlotId,
                    StackLitMasterNode.AlphaClipThresholdSlotId,
                    StackLitMasterNode.AnisotropyASlotId,
                    StackLitMasterNode.AnisotropyBSlotId,
                    StackLitMasterNode.SpecularAAScreenSpaceVarianceSlotId,
                    StackLitMasterNode.SpecularAAThresholdSlotId,
                    StackLitMasterNode.CoatSmoothnessSlotId,
                    StackLitMasterNode.CoatIorSlotId,
                    StackLitMasterNode.CoatThicknessSlotId,
                    StackLitMasterNode.CoatExtinctionSlotId,
                    StackLitMasterNode.CoatNormalSlotId,
                    StackLitMasterNode.CoatMaskSlotId,
                    StackLitMasterNode.LobeMixSlotId,
                    StackLitMasterNode.HazinessSlotId,
                    StackLitMasterNode.HazeExtentSlotId,
                    StackLitMasterNode.HazyGlossMaxDielectricF0SlotId,
                    StackLitMasterNode.SpecularOcclusionSlotId,
                    StackLitMasterNode.SOFixupVisibilityRatioThresholdSlotId,
                    StackLitMasterNode.SOFixupStrengthFactorSlotId,
                    StackLitMasterNode.SOFixupMaxAddedRoughnessSlotId,
                },

                // Render State Overrides
                CullOverride = "Cull Off",

                // Required fields
                requiredAttributes = new List<string>()
                {
                    "AttributesMesh.normalOS",
                    "AttributesMesh.tangentOS",     // Always present as we require it also in case of anisotropic lighting
                    "AttributesMesh.uv0",
                    "AttributesMesh.uv1",
                    "AttributesMesh.color",
                    "AttributesMesh.uv2",           // SHADERPASS_LIGHT_TRANSPORT always uses uv2
                },

                // Pass setup
                pragmas = new List<string>()
                {
                    "#pragma target 4.5",
                    "only_renderers d3d11 ps4 xboxone vulkan metal switch",
                    "multi_compile_instancing",
                    "instancing_options renderinglayer",
                },
                includes = new List<string>()
                {
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/SphericalCapPivot/SpecularOcclusionDef.hlsl",
                    "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl",
                    "Packages/com.unity.render-pipelines.core/ShaderLibrary/NormalSurfaceGradient.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/FragInputs.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPass.cs.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Material.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/StackLit/StackLit.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/BuiltinUtilities.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/MaterialUtilities.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Decal/DecalUtilities.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/StackLit/StackLitDecalData.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderGraphFunctions.hlsl",
                },
                keywords = new List<KeywordDescriptor>()
                {
                    Keywords.LodFadeCrossfade,
                    Keywords.SurfaceTypeTransparent,
                    Keywords.DoubleSided,
                    Keywords.BlendMode,
                },

                // Custom template
                passTemplatePath = $"{HDUtils.GetHDRenderPipelinePath()}Editor/Material/StackLit/ShaderGraph/StackLitPass.template",
                sharedTemplateDirectory = $"{HDUtils.GetHDRenderPipelinePath()}Editor/ShaderGraph",
            };

            public static ShaderPass StackLitShadowCaster = new ShaderPass()
            {
                // Definition
                displayName = "ShadowCaster",
                referenceName = "SHADERPASS_SHADOWS",
                lightMode = "ShadowCaster",
                passInclude = "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassDepthOnly.hlsl",
                useInPreview = false,

                // Port mask
                vertexPorts = new List<int>()
                {
                    StackLitMasterNode.PositionSlotId,
                },
                pixelPorts = new List<int>
                {
                    StackLitMasterNode.AlphaSlotId,
                    StackLitMasterNode.AlphaClipThresholdSlotId,
                    StackLitMasterNode.DepthOffsetSlotId,
                },

                // Render State Overrides
                BlendOverride = "Blend One Zero",
                ZWriteOverride = "ZWrite On",
                ColorMaskOverride = "ColorMask 0",
                ZClipOverride = HDSubShaderUtilities.zClipShadowCaster,

                // Pass setup
                pragmas = new List<string>()
                {
                    "#pragma target 4.5",
                    "only_renderers d3d11 ps4 xboxone vulkan metal switch",
                    "multi_compile_instancing",
                    "instancing_options renderinglayer",
                },
                includes = new List<string>()
                {
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/SphericalCapPivot/SpecularOcclusionDef.hlsl",
                    "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl",
                    "Packages/com.unity.render-pipelines.core/ShaderLibrary/NormalSurfaceGradient.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/FragInputs.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPass.cs.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Material.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/StackLit/StackLit.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/BuiltinUtilities.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/MaterialUtilities.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Decal/DecalUtilities.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/StackLit/StackLitDecalData.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderGraphFunctions.hlsl",
                },
                keywords = new List<KeywordDescriptor>()
                {
                    Keywords.LodFadeCrossfade,
                    Keywords.SurfaceTypeTransparent,
                    Keywords.DoubleSided,
                    Keywords.BlendMode,
                },

                // Custom template
                passTemplatePath = $"{HDUtils.GetHDRenderPipelinePath()}Editor/Material/StackLit/ShaderGraph/StackLitPass.template",
                sharedTemplateDirectory = $"{HDUtils.GetHDRenderPipelinePath()}Editor/ShaderGraph",
            };

            public static ShaderPass StackLitSceneSelection = new ShaderPass()
            {
                // Definition
                displayName = "SceneSelectionPass",
                referenceName = "SHADERPASS_DEPTH_ONLY",
                lightMode = "SceneSelectionPass",
                passInclude = "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassDepthOnly.hlsl",
                useInPreview = false,

                // Port mask
                vertexPorts = new List<int>()
                {
                    StackLitMasterNode.PositionSlotId,
                    StackLitMasterNode.VertexNormalSlotId,
                    StackLitMasterNode.VertexTangentSlotId
                },
                pixelPorts = new List<int>
                {
                    StackLitMasterNode.AlphaSlotId,
                    StackLitMasterNode.AlphaClipThresholdSlotId,
                    StackLitMasterNode.DepthOffsetSlotId,
                },

                // Render State Overrides
                ColorMaskOverride = "ColorMask 0",

                // Pass setup
                pragmas = new List<string>()
                {
                    "#pragma target 4.5",
                    "only_renderers d3d11 ps4 xboxone vulkan metal switch",
                    "multi_compile_instancing",
                    "instancing_options renderinglayer",
                    "editor_sync_compilation",
                },
                defines = new List<string>()
                {
                    "SCENESELECTIONPASS",
                },
                includes = new List<string>()
                {
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/SphericalCapPivot/SpecularOcclusionDef.hlsl",
                    "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl",
                    "Packages/com.unity.render-pipelines.core/ShaderLibrary/NormalSurfaceGradient.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/FragInputs.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPass.cs.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Material.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/StackLit/StackLit.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/BuiltinUtilities.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/MaterialUtilities.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Decal/DecalUtilities.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/StackLit/StackLitDecalData.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderGraphFunctions.hlsl",
                },
                keywords = new List<KeywordDescriptor>()
                {
                    Keywords.LodFadeCrossfade,
                    Keywords.SurfaceTypeTransparent,
                    Keywords.DoubleSided,
                    Keywords.BlendMode,
                },

                // Custom template
                passTemplatePath = $"{HDUtils.GetHDRenderPipelinePath()}Editor/Material/StackLit/ShaderGraph/StackLitPass.template",
                sharedTemplateDirectory = $"{HDUtils.GetHDRenderPipelinePath()}Editor/ShaderGraph",
            };

            public static ShaderPass StackLitDepthForwardOnly = new ShaderPass()
            {
                // // Code path for WRITE_NORMAL_BUFFER
                // See StackLit.hlsl:ConvertSurfaceDataToNormalData()
                // which ShaderPassDepthOnly uses: we need to add proper interpolators dependencies depending on WRITE_NORMAL_BUFFER.
                // In our case WRITE_NORMAL_BUFFER is always enabled here.
                // Also, we need to add PixelShaderSlots dependencies for everything potentially used there.
                // See AddPixelShaderSlotsForWriteNormalBufferPasses()

                // Definition
                displayName = "DepthForwardOnly",
                referenceName = "SHADERPASS_DEPTH_ONLY",
                lightMode = "DepthForwardOnly",
                passInclude = "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassDepthOnly.hlsl",
                useInPreview = true,

                // Port mask
                vertexPorts = new List<int>()
                {
                    StackLitMasterNode.PositionSlotId,
                    StackLitMasterNode.VertexNormalSlotId,
                    StackLitMasterNode.VertexTangentSlotId
                },
                pixelPorts = new List<int>
                {
                    StackLitMasterNode.AlphaSlotId,
                    StackLitMasterNode.AlphaClipThresholdSlotId,
                    StackLitMasterNode.DepthOffsetSlotId,
                    // StackLitMasterNode.coat
                    StackLitMasterNode.CoatSmoothnessSlotId,
                    StackLitMasterNode.CoatNormalSlotId,
                    // !StackLitMasterNode.coat
                    StackLitMasterNode.NormalSlotId,
                    StackLitMasterNode.LobeMixSlotId,
                    StackLitMasterNode.SmoothnessASlotId,
                    StackLitMasterNode.SmoothnessBSlotId,
                    // StackLitMasterNode.geometricSpecularAA
                    StackLitMasterNode.SpecularAAScreenSpaceVarianceSlotId,
                    StackLitMasterNode.SpecularAAThresholdSlotId,
                },

                // Render State Overrides
                CullOverride = HDSubShaderUtilities.defaultCullMode,
                ZWriteOverride = "ZWrite On",
                StencilOverride = new List<string>
                {
                    "Stencil",
                    "{",
                    "    WriteMask [_StencilWriteMaskDepth]",
                    "    Ref [_StencilRefDepth]",
                    "    Comp Always",
                    "    Pass Replace",
                    "}",
                },

                // Required fields
                requiredAttributes = new List<string>()
                {
                    "AttributesMesh.normalOS",
                    "AttributesMesh.tangentOS",     // Always present as we require it also in case of Variants lighting
                    "AttributesMesh.uv0",
                    "AttributesMesh.uv1",
                    "AttributesMesh.color",
                    "AttributesMesh.uv2",           // SHADERPASS_LIGHT_TRANSPORT always uses uv2
                    "AttributesMesh.uv3",           // DEBUG_DISPLAY
                },
                requiredVaryings = new List<string>()
                {
                    "FragInputs.tangentToWorld",
                    "FragInputs.positionRWS",
                    "FragInputs.texCoord0",
                    "FragInputs.texCoord1",
                    "FragInputs.texCoord2",
                    "FragInputs.texCoord3",
                    "FragInputs.color",
                },

                // Pass setup
                pragmas = new List<string>()
                {
                    "#pragma target 4.5",
                    "only_renderers d3d11 ps4 xboxone vulkan metal switch",
                    "multi_compile_instancing",
                    "instancing_options renderinglayer",
                },
                defines = new List<string>()
                {
                    "WRITE_NORMAL_BUFFER",
                    "RAYTRACING_SHADER_GRAPH_HIGH",
                },
                includes = new List<string>()
                {
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/SphericalCapPivot/SpecularOcclusionDef.hlsl",
                    "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl",
                    "Packages/com.unity.render-pipelines.core/ShaderLibrary/NormalSurfaceGradient.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/FragInputs.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPass.cs.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Material.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/StackLit/StackLit.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/BuiltinUtilities.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/MaterialUtilities.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Decal/DecalUtilities.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/StackLit/StackLitDecalData.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderGraphFunctions.hlsl",
                },
                keywords = new List<KeywordDescriptor>()
                {
                    Keywords.WriteMsaaDepth,
                    Keywords.LodFadeCrossfade,
                    Keywords.SurfaceTypeTransparent,
                    Keywords.DoubleSided,
                    Keywords.BlendMode,
                },

                // Custom template
                passTemplatePath = $"{HDUtils.GetHDRenderPipelinePath()}Editor/Material/StackLit/ShaderGraph/StackLitPass.template",
                sharedTemplateDirectory = $"{HDUtils.GetHDRenderPipelinePath()}Editor/ShaderGraph",
            };

            public static ShaderPass StackLitMotionVectors = new ShaderPass()
            {
                // Definition
                displayName = "MotionVectors",
                referenceName = "SHADERPASS_MOTION_VECTORS",
                lightMode = "MotionVectors",
                passInclude = "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassMotionVectors.hlsl",
                useInPreview = false,

                // Port mask
                vertexPorts = new List<int>()
                {
                    StackLitMasterNode.PositionSlotId,
                    StackLitMasterNode.VertexNormalSlotId,
                    StackLitMasterNode.VertexTangentSlotId
                },
                pixelPorts = new List<int>
                {
                    StackLitMasterNode.AlphaSlotId,
                    StackLitMasterNode.AlphaClipThresholdSlotId,
                    StackLitMasterNode.DepthOffsetSlotId,
                    // StackLitMasterNode.coat
                    StackLitMasterNode.CoatSmoothnessSlotId,
                    StackLitMasterNode.CoatNormalSlotId,
                    // !StackLitMasterNode.coat
                    StackLitMasterNode.NormalSlotId,
                    StackLitMasterNode.LobeMixSlotId,
                    StackLitMasterNode.SmoothnessASlotId,
                    StackLitMasterNode.SmoothnessBSlotId,
                    // StackLitMasterNode.geometricSpecularAA
                    StackLitMasterNode.SpecularAAScreenSpaceVarianceSlotId,
                    StackLitMasterNode.SpecularAAThresholdSlotId,
                },

                // Render State Overrides
                CullOverride = HDSubShaderUtilities.defaultCullMode,
                StencilOverride = new List<string>
                {
                    "Stencil",
                    "{",
                    "    WriteMask [_StencilWriteMaskMV]",
                    "    Ref [_StencilRefMV]",
                    "    Comp Always",
                    "    Pass Replace",
                    "}",
                },

                // Required fields
                requiredAttributes = new List<string>()
                {
                    "AttributesMesh.normalOS",
                    "AttributesMesh.tangentOS",     // Always present as we require it also in case of Variants lighting
                    "AttributesMesh.uv0",
                    "AttributesMesh.uv1",
                    "AttributesMesh.color",
                    "AttributesMesh.uv2",           // SHADERPASS_LIGHT_TRANSPORT always uses uv2
                    "AttributesMesh.uv3",           // DEBUG_DISPLAY
                },
                requiredVaryings = new List<string>()
                {
                    "FragInputs.tangentToWorld",
                    "FragInputs.positionRWS",
                    "FragInputs.texCoord0",
                    "FragInputs.texCoord1",
                    "FragInputs.texCoord2",
                    "FragInputs.texCoord3",
                    "FragInputs.color",
                },

                // Pass setup
                pragmas = new List<string>()
                {
                    "#pragma target 4.5",
                    "only_renderers d3d11 ps4 xboxone vulkan metal switch",
                    "multi_compile_instancing",
                    "instancing_options renderinglayer",
                },
                defines = new List<string>()
                {
                    "WRITE_NORMAL_BUFFER",
                    "RAYTRACING_SHADER_GRAPH_HIGH",
                },
                includes = new List<string>()
                {
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/SphericalCapPivot/SpecularOcclusionDef.hlsl",
                    "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl",
                    "Packages/com.unity.render-pipelines.core/ShaderLibrary/NormalSurfaceGradient.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/FragInputs.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPass.cs.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Material.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/StackLit/StackLit.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/BuiltinUtilities.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/MaterialUtilities.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Decal/DecalUtilities.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/StackLit/StackLitDecalData.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderGraphFunctions.hlsl",
                },
                keywords = new List<KeywordDescriptor>()
                {
                    Keywords.WriteMsaaDepth,
                    Keywords.LodFadeCrossfade,
                    Keywords.SurfaceTypeTransparent,
                    Keywords.DoubleSided,
                    Keywords.BlendMode,
                },

                // Custom template
                passTemplatePath = $"{HDUtils.GetHDRenderPipelinePath()}Editor/Material/StackLit/ShaderGraph/StackLitPass.template",
                sharedTemplateDirectory = $"{HDUtils.GetHDRenderPipelinePath()}Editor/ShaderGraph",
            };

            public static ShaderPass StackLitDistortion = new ShaderPass()
            {
                // Definition
                displayName = "DistortionVectors",
                referenceName = "SHADERPASS_DISTORTION",
                lightMode = "DistortionVectors",
                passInclude = "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassDistortion.hlsl",
                useInPreview = true,

                // Port mask
                vertexPorts = new List<int>()
                {
                    StackLitMasterNode.PositionSlotId,
                    StackLitMasterNode.VertexNormalSlotId,
                    StackLitMasterNode.VertexTangentSlotId
                },
                pixelPorts = new List<int>
                {
                    StackLitMasterNode.AlphaSlotId,
                    StackLitMasterNode.AlphaClipThresholdSlotId,
                    StackLitMasterNode.DistortionSlotId,
                    StackLitMasterNode.DistortionBlurSlotId,
                    StackLitMasterNode.DepthOffsetSlotId,
                },

                // Render State Overrides
                ZWriteOverride = "ZWrite Off",
                CullOverride = HDSubShaderUtilities.defaultCullMode,
                StencilOverride = new List<string>()
                {
                    "Stencil",
                    "{",
                    $"    WriteMask {(int)HDRenderPipeline.StencilBitMask.DistortionVectors}",
                    $"    Ref  {(int)HDRenderPipeline.StencilBitMask.DistortionVectors}",
                    "    Comp Always",
                    "    Pass Replace",
                    "}"
                },

                // Pass setup
                pragmas = new List<string>()
                {
                    "#pragma target 4.5",
                    "only_renderers d3d11 ps4 xboxone vulkan metal switch",
                    "multi_compile_instancing",
                    "instancing_options renderinglayer",
                },
                includes = new List<string>()
                {
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/SphericalCapPivot/SpecularOcclusionDef.hlsl",
                    "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl",
                    "Packages/com.unity.render-pipelines.core/ShaderLibrary/NormalSurfaceGradient.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/FragInputs.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPass.cs.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Material.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/StackLit/StackLit.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/BuiltinUtilities.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/MaterialUtilities.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Decal/DecalUtilities.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/StackLit/StackLitDecalData.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderGraphFunctions.hlsl",
                },
                keywords = new List<KeywordDescriptor>()
                {
                    Keywords.LodFadeCrossfade,
                    Keywords.SurfaceTypeTransparent,
                    Keywords.DoubleSided,
                    Keywords.BlendMode,
                },

                // Custom template
                passTemplatePath = $"{HDUtils.GetHDRenderPipelinePath()}Editor/Material/StackLit/ShaderGraph/StackLitPass.template",
                sharedTemplateDirectory = $"{HDUtils.GetHDRenderPipelinePath()}Editor/ShaderGraph",
            };

            public static ShaderPass StackLitForwardOnlyOpaque = new ShaderPass()
            {
                // Definition
                displayName = "ForwardOnly",
                referenceName = "SHADERPASS_FORWARD",
                lightMode = "ForwardOnly",
                passInclude = "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassForward.hlsl",
                useInPreview = true,

                // Port mask
                vertexPorts = new List<int>()
                {
                    StackLitMasterNode.PositionSlotId,
                    StackLitMasterNode.VertexNormalSlotId,
                    StackLitMasterNode.VertexTangentSlotId
                },
                pixelPorts = new List<int>
                {
                    StackLitMasterNode.BaseColorSlotId,
                    StackLitMasterNode.NormalSlotId,
                    StackLitMasterNode.BentNormalSlotId,
                    StackLitMasterNode.TangentSlotId,
                    StackLitMasterNode.SubsurfaceMaskSlotId,
                    StackLitMasterNode.ThicknessSlotId,
                    StackLitMasterNode.DiffusionProfileHashSlotId,
                    StackLitMasterNode.IridescenceMaskSlotId,
                    StackLitMasterNode.IridescenceThicknessSlotId,
                    StackLitMasterNode.IridescenceCoatFixupTIRSlotId,
                    StackLitMasterNode.IridescenceCoatFixupTIRClampSlotId,
                    StackLitMasterNode.SpecularColorSlotId,
                    StackLitMasterNode.DielectricIorSlotId,
                    StackLitMasterNode.MetallicSlotId,
                    StackLitMasterNode.EmissionSlotId,
                    StackLitMasterNode.SmoothnessASlotId,
                    StackLitMasterNode.SmoothnessBSlotId,
                    StackLitMasterNode.AmbientOcclusionSlotId,
                    StackLitMasterNode.AlphaSlotId,
                    StackLitMasterNode.AlphaClipThresholdSlotId,
                    StackLitMasterNode.AnisotropyASlotId,
                    StackLitMasterNode.AnisotropyBSlotId,
                    StackLitMasterNode.SpecularAAScreenSpaceVarianceSlotId,
                    StackLitMasterNode.SpecularAAThresholdSlotId,
                    StackLitMasterNode.CoatSmoothnessSlotId,
                    StackLitMasterNode.CoatIorSlotId,
                    StackLitMasterNode.CoatThicknessSlotId,
                    StackLitMasterNode.CoatExtinctionSlotId,
                    StackLitMasterNode.CoatNormalSlotId,
                    StackLitMasterNode.CoatMaskSlotId,
                    StackLitMasterNode.LobeMixSlotId,
                    StackLitMasterNode.HazinessSlotId,
                    StackLitMasterNode.HazeExtentSlotId,
                    StackLitMasterNode.HazyGlossMaxDielectricF0SlotId,
                    StackLitMasterNode.SpecularOcclusionSlotId,
                    StackLitMasterNode.SOFixupVisibilityRatioThresholdSlotId,
                    StackLitMasterNode.SOFixupStrengthFactorSlotId,
                    StackLitMasterNode.SOFixupMaxAddedRoughnessSlotId,
                    StackLitMasterNode.LightingSlotId,
                    StackLitMasterNode.BackLightingSlotId,
                    StackLitMasterNode.DepthOffsetSlotId,
                },

                // Render State Overrides
                BlendOverride = "Blend [_SrcBlend] [_DstBlend], [_AlphaSrcBlend] [_AlphaDstBlend]",
                CullOverride = HDSubShaderUtilities.cullModeForward,
                ZTestOverride = HDSubShaderUtilities.zTestDepthEqualForOpaque,
                ZWriteOverride = HDSubShaderUtilities.ZWriteDefault,
                StencilOverride = new List<string>()
                {
                    "Stencil",
                    "{",
                    "    WriteMask [_StencilWriteMask]",
                    "    Ref [_StencilRef]",
                    "    Comp Always",
                    "    Pass Replace",
                    "}"
                },

                // Required fields
                requiredAttributes = new List<string>()
                {
                    "AttributesMesh.normalOS",
                    "AttributesMesh.tangentOS",     // Always present as we require it also in case of Variants lighting
                    "AttributesMesh.uv0",
                    "AttributesMesh.uv1",
                    "AttributesMesh.color",
                    "AttributesMesh.uv2",           // SHADERPASS_LIGHT_TRANSPORT always uses uv2
                    "AttributesMesh.uv3",           // DEBUG_DISPLAY
                },
                requiredVaryings = new List<string>()
                {
                    "FragInputs.tangentToWorld",
                    "FragInputs.positionRWS",
                    "FragInputs.texCoord0",
                    "FragInputs.texCoord1",
                    "FragInputs.texCoord2",
                    "FragInputs.texCoord3",
                    "FragInputs.color",
                },

                // Pass setup
                pragmas = new List<string>()
                {
                    "#pragma target 4.5",
                    "only_renderers d3d11 ps4 xboxone vulkan metal switch",
                    "multi_compile_instancing",
                    "instancing_options renderinglayer",
                },
                defines = new List<string>()
                {
                    "HAS_LIGHTLOOP",
                },
                includes = new List<string>()
                {
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/SphericalCapPivot/SpecularOcclusionDef.hlsl",
                    "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl",
                    "Packages/com.unity.render-pipelines.core/ShaderLibrary/NormalSurfaceGradient.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/FragInputs.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPass.cs.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Material.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/Lighting.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/LightLoop/LightLoopDef.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/StackLit/StackLit.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/LightLoop/LightLoop.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/BuiltinUtilities.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/MaterialUtilities.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Decal/DecalUtilities.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/StackLit/StackLitDecalData.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderGraphFunctions.hlsl",
                },
                keywords = new List<KeywordDescriptor>()
                {
                    Keywords.LodFadeCrossfade,
                    Keywords.SurfaceTypeTransparent,
                    Keywords.DoubleSided,
                    Keywords.BlendMode,
                    Keywords.DebugDisplay,
                    Keywords.Lightmap,
                    Keywords.DynamicLightmap,
                    Keywords.DirectionalLightmapCombined,
                    Keywords.ShadowsShadowmask,
                    Keywords.Decals,
                    Keywords.Shadow,
                    Keywords.LightList,
                },

                // Custom template
                passTemplatePath = $"{HDUtils.GetHDRenderPipelinePath()}Editor/Material/StackLit/ShaderGraph/StackLitPass.template",
                sharedTemplateDirectory = $"{HDUtils.GetHDRenderPipelinePath()}Editor/ShaderGraph",
            };

            public static ShaderPass StackLitForwardOnlyTransparent = new ShaderPass()
            {
                // Definition
                displayName = "ForwardOnly",
                referenceName = "SHADERPASS_FORWARD",
                lightMode = "ForwardOnly",
                passInclude = "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassForward.hlsl",
                useInPreview = true,

                // Port mask
                vertexPorts = new List<int>()
                {
                    StackLitMasterNode.PositionSlotId,
                    StackLitMasterNode.VertexNormalSlotId,
                    StackLitMasterNode.VertexTangentSlotId
                },
                pixelPorts = new List<int>
                {
                    StackLitMasterNode.BaseColorSlotId,
                    StackLitMasterNode.NormalSlotId,
                    StackLitMasterNode.BentNormalSlotId,
                    StackLitMasterNode.TangentSlotId,
                    StackLitMasterNode.SubsurfaceMaskSlotId,
                    StackLitMasterNode.ThicknessSlotId,
                    StackLitMasterNode.DiffusionProfileHashSlotId,
                    StackLitMasterNode.IridescenceMaskSlotId,
                    StackLitMasterNode.IridescenceThicknessSlotId,
                    StackLitMasterNode.IridescenceCoatFixupTIRSlotId,
                    StackLitMasterNode.IridescenceCoatFixupTIRClampSlotId,
                    StackLitMasterNode.SpecularColorSlotId,
                    StackLitMasterNode.DielectricIorSlotId,
                    StackLitMasterNode.MetallicSlotId,
                    StackLitMasterNode.EmissionSlotId,
                    StackLitMasterNode.SmoothnessASlotId,
                    StackLitMasterNode.SmoothnessBSlotId,
                    StackLitMasterNode.AmbientOcclusionSlotId,
                    StackLitMasterNode.AlphaSlotId,
                    StackLitMasterNode.AlphaClipThresholdSlotId,
                    StackLitMasterNode.AnisotropyASlotId,
                    StackLitMasterNode.AnisotropyBSlotId,
                    StackLitMasterNode.SpecularAAScreenSpaceVarianceSlotId,
                    StackLitMasterNode.SpecularAAThresholdSlotId,
                    StackLitMasterNode.CoatSmoothnessSlotId,
                    StackLitMasterNode.CoatIorSlotId,
                    StackLitMasterNode.CoatThicknessSlotId,
                    StackLitMasterNode.CoatExtinctionSlotId,
                    StackLitMasterNode.CoatNormalSlotId,
                    StackLitMasterNode.CoatMaskSlotId,
                    StackLitMasterNode.LobeMixSlotId,
                    StackLitMasterNode.HazinessSlotId,
                    StackLitMasterNode.HazeExtentSlotId,
                    StackLitMasterNode.HazyGlossMaxDielectricF0SlotId,
                    StackLitMasterNode.SpecularOcclusionSlotId,
                    StackLitMasterNode.SOFixupVisibilityRatioThresholdSlotId,
                    StackLitMasterNode.SOFixupStrengthFactorSlotId,
                    StackLitMasterNode.SOFixupMaxAddedRoughnessSlotId,
                    StackLitMasterNode.LightingSlotId,
                    StackLitMasterNode.BackLightingSlotId,
                    StackLitMasterNode.DepthOffsetSlotId,
                },

                // Render State Overrides
                BlendOverride = "Blend [_SrcBlend] [_DstBlend], [_AlphaSrcBlend] [_AlphaDstBlend]",
                CullOverride = HDSubShaderUtilities.cullModeForward,
                ZTestOverride = HDSubShaderUtilities.zTestDepthEqualForOpaque,
                ZWriteOverride = HDSubShaderUtilities.ZWriteDefault,
                StencilOverride = new List<string>()
                {
                    "Stencil",
                    "{",
                    "    WriteMask [_StencilWriteMask]",
                    "    Ref [_StencilRef]",
                    "    Comp Always",
                    "    Pass Replace",
                    "}"
                },

                // Required fields
                requiredAttributes = new List<string>()
                {
                    "AttributesMesh.normalOS",
                    "AttributesMesh.tangentOS",     // Always present as we require it also in case of Variants lighting
                    "AttributesMesh.uv0",
                    "AttributesMesh.uv1",
                    "AttributesMesh.color",
                    "AttributesMesh.uv2",           // SHADERPASS_LIGHT_TRANSPORT always uses uv2
                    "AttributesMesh.uv3",           // DEBUG_DISPLAY
                },
                requiredVaryings = new List<string>()
                {
                    "FragInputs.tangentToWorld",
                    "FragInputs.positionRWS",
                    "FragInputs.texCoord0",
                    "FragInputs.texCoord1",
                    "FragInputs.texCoord2",
                    "FragInputs.texCoord3",
                    "FragInputs.color",
                },

                // Pass setup
                pragmas = new List<string>()
                {
                    "#pragma target 4.5",
                    "only_renderers d3d11 ps4 xboxone vulkan metal switch",
                    "multi_compile_instancing",
                    "instancing_options renderinglayer",
                },
                defines = new List<string>()
                {
                    "HAS_LIGHTLOOP",
                    "USE_CLUSTERED_LIGHTLIST",
                    "RAYTRACING_SHADER_GRAPH_HIGH",
                },
                includes = new List<string>()
                {
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/SphericalCapPivot/SpecularOcclusionDef.hlsl",
                    "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl",
                    "Packages/com.unity.render-pipelines.core/ShaderLibrary/NormalSurfaceGradient.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/FragInputs.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPass.cs.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Material.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/Lighting.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/LightLoop/LightLoopDef.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/StackLit/StackLit.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/LightLoop/LightLoop.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/BuiltinUtilities.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/MaterialUtilities.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Decal/DecalUtilities.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/StackLit/StackLitDecalData.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderGraphFunctions.hlsl",
                },
                keywords = new List<KeywordDescriptor>()
                {
                    Keywords.LodFadeCrossfade,
                    Keywords.SurfaceTypeTransparent,
                    Keywords.DoubleSided,
                    Keywords.BlendMode,
                    Keywords.DebugDisplay,
                    Keywords.Lightmap,
                    Keywords.DynamicLightmap,
                    Keywords.DirectionalLightmapCombined,
                    Keywords.ShadowsShadowmask,
                    Keywords.Decals,
                    Keywords.Shadow,
                },

                // Custom template
                passTemplatePath = $"{HDUtils.GetHDRenderPipelinePath()}Editor/Material/StackLit/ShaderGraph/StackLitPass.template",
                sharedTemplateDirectory = $"{HDUtils.GetHDRenderPipelinePath()}Editor/ShaderGraph",
            };
        }
#endregion

#region Keywords
        public static class Keywords
        {
            public static KeywordDescriptor WriteNormalBuffer = new KeywordDescriptor()
            {
                displayName = "Write Normal Buffer",
                referenceName = "WRITE_NORMAL_BUFFER",
                type = KeywordType.Boolean,
                definition = KeywordDefinition.MultiCompile,
                scope = KeywordScope.Global,
            };

            public static KeywordDescriptor WriteMsaaDepth = new KeywordDescriptor()
            {
                displayName = "Write MSAA Depth",
                referenceName = "WRITE_MSAA_DEPTH",
                type = KeywordType.Boolean,
                definition = KeywordDefinition.MultiCompile,
                scope = KeywordScope.Global,
            };

            public static KeywordDescriptor DebugDisplay = new KeywordDescriptor()
            {
                displayName = "Debug Display",
                referenceName = "DEBUG_DISPLAY",
                type = KeywordType.Boolean,
                definition = KeywordDefinition.MultiCompile,
                scope = KeywordScope.Global,
            };

            public static KeywordDescriptor Lightmap = new KeywordDescriptor()
            {
                displayName = "Lightmap",
                referenceName = "LIGHTMAP_ON",
                type = KeywordType.Boolean,
                definition = KeywordDefinition.MultiCompile,
                scope = KeywordScope.Global,
            };

            public static KeywordDescriptor DirectionalLightmapCombined = new KeywordDescriptor()
            {
                displayName = "Directional Lightmap Combined",
                referenceName = "DIRLIGHTMAP_COMBINED",
                type = KeywordType.Boolean,
                definition = KeywordDefinition.MultiCompile,
                scope = KeywordScope.Global,
            };

            public static KeywordDescriptor DynamicLightmap = new KeywordDescriptor()
            {
                displayName = "Dynamic Lightmap",
                referenceName = "DYNAMICLIGHTMAP_ON",
                type = KeywordType.Boolean,
                definition = KeywordDefinition.MultiCompile,
                scope = KeywordScope.Global,
            };

            public static KeywordDescriptor ShadowsShadowmask = new KeywordDescriptor()
            {
                displayName = "Shadows Shadowmask",
                referenceName = "SHADOWS_SHADOWMASK",
                type = KeywordType.Boolean,
                definition = KeywordDefinition.MultiCompile,
                scope = KeywordScope.Global,
            };

            public static KeywordDescriptor DiffuseLightingOnly = new KeywordDescriptor()
            {
                displayName = "Diffuse Lighting Only",
                referenceName = "DIFFUSE_LIGHTING_ONLY",
                type = KeywordType.Boolean,
                definition = KeywordDefinition.MultiCompile,
                scope = KeywordScope.Global,
            };

            public static KeywordDescriptor LightLayers = new KeywordDescriptor()
            {
                displayName = "Light Layers",
                referenceName = "LIGHT_LAYERS",
                type = KeywordType.Boolean,
                definition = KeywordDefinition.MultiCompile,
                scope = KeywordScope.Global,
            };

            public static KeywordDescriptor Decals = new KeywordDescriptor()
            {
                displayName = "Decals",
                referenceName = "DECALS",
                type = KeywordType.Enum,
                definition = KeywordDefinition.MultiCompile,
                scope = KeywordScope.Global,
                entries = new KeywordEntry[]
                {
                    new KeywordEntry() { displayName = "Off", referenceName = "OFF" },
                    new KeywordEntry() { displayName = "3RT", referenceName = "3RT" },
                    new KeywordEntry() { displayName = "4RT", referenceName = "4RT" },
                }
            };

            public static KeywordDescriptor LodFadeCrossfade = new KeywordDescriptor()
            {
                displayName = "LOD Fade Crossfade",
                referenceName = "LOD_FADE_CROSSFADE",
                type = KeywordType.Boolean,
                definition = KeywordDefinition.MultiCompile,
                scope = KeywordScope.Global,
            };

            public static KeywordDescriptor LightList = new KeywordDescriptor()
            {
                displayName = "Light List",
                referenceName = "USE",
                type = KeywordType.Enum,
                definition = KeywordDefinition.MultiCompile,
                scope = KeywordScope.Global,
                entries = new KeywordEntry[]
                {
                    new KeywordEntry() { displayName = "FPTL", referenceName = "FPTL_LIGHTLIST" },
                    new KeywordEntry() { displayName = "Clustered", referenceName = "CLUSTERED_LIGHTLIST" },
                }
            };

            public static KeywordDescriptor Shadow = new KeywordDescriptor()
            {
                displayName = "Shadow",
                referenceName = "SHADOW",
                type = KeywordType.Enum,
                definition = KeywordDefinition.MultiCompile,
                scope = KeywordScope.Global,
                entries = new KeywordEntry[]
                {
                    new KeywordEntry() { displayName = "Low", referenceName = "LOW" },
                    new KeywordEntry() { displayName = "Medium", referenceName = "MEDIUM" },
                    new KeywordEntry() { displayName = "High", referenceName = "HIGH" },
                }
            };

            public static KeywordDescriptor SurfaceTypeTransparent = new KeywordDescriptor()
            {
                displayName = "Surface Type Transparent",
                referenceName = "_SURFACE_TYPE_TRANSPARENT",
                type = KeywordType.Boolean,
                definition = KeywordDefinition.ShaderFeature,
                scope = KeywordScope.Global,
            };

            public static KeywordDescriptor DoubleSided = new KeywordDescriptor()
            {
                displayName = "Double Sided",
                referenceName = "_DOUBLESIDED_ON",
                type = KeywordType.Boolean,
                definition = KeywordDefinition.ShaderFeature,
                scope = KeywordScope.Local,
            };

            public static KeywordDescriptor BlendMode = new KeywordDescriptor()
            {
                displayName = "Blend Mode",
                referenceName = "_BLENDMODE",
                type = KeywordType.Enum,
                definition = KeywordDefinition.ShaderFeature,
                scope = KeywordScope.Local,
                entries = new KeywordEntry[]
                {
                    new KeywordEntry() { displayName = "Off", referenceName = "OFF" },
                    new KeywordEntry() { displayName = "Alpha", referenceName = "ALPHA" },
                    new KeywordEntry() { displayName = "Add", referenceName = "ADD" },
                    new KeywordEntry() { displayName = "Multiply", referenceName = "MULTIPLY" },
                }
            };
        }
#endregion
    }
}
