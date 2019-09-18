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
                    Keywords.MsaaDepth
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
                    $"   WriteMask {(int)HDRenderPipeline.StencilBitMask.ObjectMotionVectors}",
                    $"   Ref  {(int)HDRenderPipeline.StencilBitMask.ObjectMotionVectors}",
                    "   Comp Always",
                    "   Pass Replace",
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
                    Keywords.MsaaDepth
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
                    $"   WriteMask {(int) HDRenderPipeline.StencilBitMask.LightingMask}",
                    $"   Ref  {(int)StencilLightingUsage.NoLighting}",
                    "   Comp Always",
                    "   Pass Replace",
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
        }
#endregion

#region Keywords
        public static class Keywords
        {
            public static KeywordDescriptor MsaaDepth = new KeywordDescriptor()
            {
                displayName = "MSAA Depth",
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
        }
#endregion
    }
}
