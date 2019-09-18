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
                case DecalMasterNode decalMasterNode:
                    subShader = new DecalSubShader();
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
                    "// Stencil setup",
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
                    "// Stencil setup",
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
                    "// Stencil setup",
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
                    "// Stencil setup",
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
                    "// Stencil setup",
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

                    "// Stencil setup",
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

                    "// Stencil setup",
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
                    Keywords.ShadowsShadowmask,
                    Keywords.Decals,
                    Keywords.Shadow,
                },

                // Custom template
                passTemplatePath = $"{HDUtils.GetHDRenderPipelinePath()}Editor/Material/PBR/ShaderGraph/HDPBRPass.template",
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
        }
#endregion
    }
}
