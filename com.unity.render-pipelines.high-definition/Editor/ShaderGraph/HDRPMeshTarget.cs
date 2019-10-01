using System.Collections.Generic;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEditor.ShaderGraph;
using UnityEditor.ShaderGraph.Internal;
using ShaderPass = UnityEditor.ShaderGraph.Internal.ShaderPass;
using BlendOp = UnityEditor.ShaderGraph.Internal.BlendOp;

namespace UnityEditor.Rendering.HighDefinition
{
    class HDRPMeshTarget : ITargetVariant<MeshTarget>
    {
        public string displayName => "HDRP";
        public string passTemplatePath => string.Empty;
        public string sharedTemplateDirectory => $"{HDUtils.GetHDRenderPipelinePath()}Editor/ShaderGraph";

        public bool Validate(RenderPipelineAsset pipelineAsset)
        {
            return pipelineAsset is HDRenderPipelineAsset;
        }

        public bool TryGetSubShader(IMasterNode masterNode, out ISubShader subShader)
        {
            switch(masterNode)
            {
                case UnlitMasterNode unlitMasterNode:
                    subShader = new UnlitSubShader();
                    return true;
                case PBRMasterNode pbrMasterNode:
                    subShader = new HDPBRSubShader();
                    return true;
                case HDUnlitMasterNode hdUnlitMasterNode:
                    subShader = new HDUnlitSubShader();
                    return true;
                case HDLitMasterNode hdLitMasterNode:
                    subShader = new HDLitSubShader();
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
                case StackLitMasterNode stackLitMasterNode:
                    subShader = new StackLitSubShader();
                    return true;
                default:
                    subShader = null;
                    return false;
            }
        }

        static string GetPassTemplatePath(string materialName)
        {
            return $"{HDUtils.GetHDRenderPipelinePath()}Editor/Material/{materialName}/ShaderGraph/{materialName}Pass.template";
        }

#region Unlit Passes
        public static class UnlitPasses
        {
            public static ShaderPass META = new ShaderPass()
            {
                // Definition
                displayName = "META",
                referenceName = "SHADERPASS_LIGHT_TRANSPORT",
                lightMode = "META",
                passInclude = "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassLightTransport.hlsl",
                useInPreview = false,

                // Port Mask
                pixelPorts = PixelPorts.UnlitDefault,

                // Required Fields
                requiredFields = RequiredFields.Meta,

                // Conditional State
                renderStates = RenderStates.Meta,
                pragmas = Pragmas.Instanced,
                includes = Includes.Unlit,

                // Custom Template
                passTemplatePath = GetPassTemplatePath("Unlit"),
            };

            public static ShaderPass ShadowCaster = new ShaderPass()
            {
                // Definition
                displayName = "ShadowCaster",
                referenceName = "SHADERPASS_SHADOWS",
                lightMode = "ShadowCaster",
                passInclude = "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassDepthOnly.hlsl",
                useInPreview = false,

                // Port Mask
                vertexPorts = VertexPorts.UnlitDefault,
                pixelPorts = PixelPorts.UnlitOnlyAlpha,

                // Conditional State
                renderStates = RenderStates.ShadowCasterUnlit,
                pragmas = Pragmas.Instanced,
                includes = Includes.Unlit,

                // Custom Template
                passTemplatePath = GetPassTemplatePath("Unlit"),
            };

            public static ShaderPass SceneSelection = new ShaderPass()
            {
                // Definition
                displayName = "SceneSelectionPass",
                referenceName = "SHADERPASS_DEPTH_ONLY",
                lightMode = "SceneSelectionPass",
                passInclude = "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassDepthOnly.hlsl",
                useInPreview = false,

                // Port Mask
                vertexPorts = VertexPorts.UnlitDefault,
                pixelPorts = PixelPorts.UnlitOnlyAlpha,

                // Conditional State
                renderStates = RenderStates.SceneSelection,
                pragmas = Pragmas.InstancedEditorSync,
                defines = Defines.SceneSelection,
                includes = Includes.Unlit,

                // Custom Template
                passTemplatePath = GetPassTemplatePath("Unlit"),
            }; 

            public static ShaderPass DepthForwardOnly = new ShaderPass()
            {
                // Definition
                displayName = "DepthForwardOnly",
                referenceName = "SHADERPASS_DEPTH_ONLY",
                lightMode = "DepthForwardOnly",
                passInclude = "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassDepthOnly.hlsl",
                useInPreview = false,

                // Port Mask
                vertexPorts = VertexPorts.UnlitDefault,
                pixelPorts = PixelPorts.UnlitOnlyAlpha,

                // Conditional State
                renderStates = RenderStates.DepthForwardOnly,
                pragmas = Pragmas.Instanced,
                keywords = Keywords.WriteMsaaDepth,
                includes = Includes.Unlit,

                // Custom Template
                passTemplatePath = GetPassTemplatePath("Unlit"),
            };

            public static ShaderPass MotionVectors = new ShaderPass()
            {
                // Definition
                displayName = "MotionVectors",
                referenceName = "SHADERPASS_MOTION_VECTORS",
                lightMode = "MotionVectors",
                passInclude = "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassMotionVectors.hlsl",
                useInPreview = false,

                // Port Mask
                vertexPorts = VertexPorts.UnlitDefault,
                pixelPorts = PixelPorts.UnlitOnlyAlpha,

                // Required fields
                requiredFields = RequiredFields.PositionRWS,

                // Conditional State
                renderStates = RenderStates.UnlitMotionVectors,
                pragmas = Pragmas.Instanced,
                keywords = Keywords.WriteMsaaDepth,
                includes = Includes.Unlit,

                // Custom Template
                passTemplatePath = GetPassTemplatePath("Unlit"),
            };

            public static ShaderPass ForwardOnly = new ShaderPass()
            {
                // Definition
                displayName = "ForwardOnly",
                referenceName = "SHADERPASS_FORWARD_UNLIT",
                lightMode = "ForwardOnly",
                passInclude = "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassForwardUnlit.hlsl",
                useInPreview = true,

                // Port Mask
                vertexPorts = VertexPorts.UnlitDefault,
                pixelPorts = PixelPorts.UnlitDefault,

                // Conditional State
                renderStates = RenderStates.UnlitForward,
                pragmas = Pragmas.Instanced,
                keywords = Keywords.DebugDisplay,
                includes = Includes.Unlit,

                // Custom Template
                passTemplatePath = GetPassTemplatePath("Unlit"),
            };
        }
#endregion

#region PBR Passes
        public static class PBRPasses
        {
            public static ShaderPass GBuffer = new ShaderPass()
            {
                // Definition
                displayName = "GBuffer",
                referenceName = "SHADERPASS_GBUFFER",
                lightMode = "GBuffer",
                passInclude = "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassGBuffer.hlsl",
                useInPreview = true,

                // Port Mask
                vertexPorts = VertexPorts.PBRDefault,
                pixelPorts = PixelPorts.PBRDefault,

                // Required fields
                requiredFields = RequiredFields.LitMinimal,

                // Conditional State
                renderStates = RenderStates.PBRGBuffer,
                pragmas = Pragmas.InstancedRenderingPlayer,
                keywords = Keywords.GBuffer,
                includes = Includes.Lit,

                // Custom Template
                passTemplatePath = GetPassTemplatePath("PBR"),
            };

            public static ShaderPass META = new ShaderPass()
            {
                // Definition
                displayName = "META",
                referenceName = "SHADERPASS_LIGHT_TRANSPORT",
                lightMode = "META",
                passInclude = "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassLightTransport.hlsl",
                useInPreview = false,

                // Port Mask
                pixelPorts = PixelPorts.PBRDefault,

                // Required Fields
                requiredFields = RequiredFields.Meta,

                // Conditional State
                renderStates = RenderStates.Meta,
                pragmas = Pragmas.InstancedRenderingPlayer,
                keywords = Keywords.LodFadeCrossfade,
                includes = Includes.Lit,

                // Custom Template
                passTemplatePath = GetPassTemplatePath("PBR"),
            };

            public static ShaderPass ShadowCaster = new ShaderPass()
            {
                // Definition
                displayName = "ShadowCaster",
                referenceName = "SHADERPASS_SHADOWS",
                lightMode = "ShadowCaster",
                passInclude = "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassDepthOnly.hlsl",
                useInPreview = false,

                // Port Mask
                vertexPorts = VertexPorts.PBRDefault,
                pixelPorts = PixelPorts.PBROnlyAlpha,

                // Conditional State
                renderStates = RenderStates.ShadowCasterPBR,
                pragmas = Pragmas.InstancedRenderingPlayer,
                includes = Includes.Lit,
                keywords = Keywords.LodFadeCrossfade,

                // Custom Template
                passTemplatePath = GetPassTemplatePath("PBR"),
            };

            public static ShaderPass SceneSelection = new ShaderPass()
            {
                // Definition
                displayName = "SceneSelectionPass",
                referenceName = "SHADERPASS_DEPTH_ONLY",
                lightMode = "SceneSelectionPass",
                passInclude = "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassDepthOnly.hlsl",
                useInPreview = false,

                // Port Mask
                vertexPorts = VertexPorts.PBRDefault,
                pixelPorts = PixelPorts.PBROnlyAlpha,

                // Conditional State
                renderStates = RenderStates.SceneSelection,
                pragmas = Pragmas.InstancedRenderingPlayerEditorSync,
                defines = Defines.SceneSelection,
                keywords = Keywords.LodFadeCrossfade,
                includes = Includes.Lit,

                // Custom Template
                passTemplatePath = GetPassTemplatePath("PBR"),
            };

            public static ShaderPass DepthOnly = new ShaderPass()
            {
                // Definition
                displayName = "DepthOnly",
                referenceName = "SHADERPASS_DEPTH_ONLY",
                lightMode = "DepthOnly",
                passInclude = "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassDepthOnly.hlsl",
                useInPreview = true,

                // Port Mask
                vertexPorts = VertexPorts.PBRDefault,
                pixelPorts = PixelPorts.PBRDepthMotionVectors,

                // Required Fields
                requiredFields = RequiredFields.LitFull,

                // Conditional State
                renderStates = RenderStates.DepthOnly,
                pragmas = Pragmas.InstancedRenderingPlayer,
                defines = Defines.ShaderGraphRaytracingHigh,
                keywords = Keywords.DepthMotionVectors,
                includes = Includes.Lit,

                // Custom Template
                passTemplatePath = GetPassTemplatePath("PBR"),
            };

            public static ShaderPass MotionVectors = new ShaderPass()
            {
                // Definition
                displayName = "MotionVectors",
                referenceName = "SHADERPASS_MOTION_VECTORS",
                lightMode = "MotionVectors",
                passInclude = "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassMotionVectors.hlsl",
                useInPreview = false,

                // Port Mask
                vertexPorts = VertexPorts.PBRDefault,
                pixelPorts = PixelPorts.PBRDepthMotionVectors,

                // Required Fields
                requiredFields = RequiredFields.PositionRWS,

                // Conditional State
                renderStates = RenderStates.PBRMotionVectors,
                pragmas = Pragmas.InstancedRenderingPlayer,
                defines = Defines.ShaderGraphRaytracingHigh,
                keywords = Keywords.DepthMotionVectors,
                includes = Includes.Lit,

                // Custom Template
                passTemplatePath = GetPassTemplatePath("PBR"),
            };

            public static ShaderPass Forward = new ShaderPass()
            {
                // Definition
                displayName = "Forward",
                referenceName = "SHADERPASS_FORWARD",
                lightMode = "Forward",
                passInclude = "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassForward.hlsl",
                useInPreview = true,

                // Port Mask
                vertexPorts = VertexPorts.PBRDefault,
                pixelPorts = PixelPorts.PBRDefault,

                // Required Fields
                requiredFields = RequiredFields.LitMinimal,

                // Conditional State
                renderStates = RenderStates.PBRForward,
                pragmas = Pragmas.InstancedRenderingPlayer,
                defines = Defines.Forward,
                keywords = Keywords.Forward,
                includes = Includes.LitForward,

                // Custom Template
                passTemplatePath = GetPassTemplatePath("PBR"),
            };
        }
#endregion

#region HD Unlit Passes
        public static class HDUnlitPasses
        {
            public static ShaderPass META = new ShaderPass()
            {
                // Definition
                displayName = "META",
                referenceName = "SHADERPASS_LIGHT_TRANSPORT",
                lightMode = "META",
                passInclude = "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassLightTransport.hlsl",
                useInPreview = false,

                // Port Mask
                pixelPorts = PixelPorts.HDUnlitDefault,

                // Required Fields
                requiredFields = RequiredFields.Meta,

                // Conditional State
                renderStates = RenderStates.Meta,
                pragmas = Pragmas.Instanced,
                keywords = Keywords.TransparentBlend,
                includes = Includes.Unlit,

                // Custom Template
                passTemplatePath = $"{HDUtils.GetHDRenderPipelinePath()}Editor/Material/Unlit/ShaderGraph/HDUnlitPass.template",
            };

            public static ShaderPass ShadowCaster = new ShaderPass()
            {
                // Definition
                displayName = "ShadowCaster",
                referenceName = "SHADERPASS_SHADOWS",
                lightMode = "ShadowCaster",
                passInclude = "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassDepthOnly.hlsl",
                useInPreview = false,

                // Port Mask
                vertexPorts = VertexPorts.HDUnlitDefault,
                pixelPorts = PixelPorts.HDUnlitOnlyAlpha,

                // Conditional State
                renderStates = RenderStates.HDShadowCaster,
                pragmas = Pragmas.Instanced,
                keywords = Keywords.TransparentBlend,
                includes = Includes.Unlit,

                // Custom Template
                passTemplatePath = $"{HDUtils.GetHDRenderPipelinePath()}Editor/Material/Unlit/ShaderGraph/HDUnlitPass.template",
            };

            public static ShaderPass SceneSelection = new ShaderPass()
            {
                // Definition
                displayName = "SceneSelectionPass",
                referenceName = "SHADERPASS_DEPTH_ONLY",
                lightMode = "SceneSelectionPass",
                passInclude = "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassDepthOnly.hlsl",
                useInPreview = false,

                // Port Mask
                vertexPorts = VertexPorts.HDUnlitDefault,
                pixelPorts = PixelPorts.HDUnlitOnlyAlpha,

                // Conditional State
                renderStates = RenderStates.HDUnlitSceneSelection,
                pragmas = Pragmas.InstancedEditorSync,
                defines = Defines.SceneSelection,
                keywords = Keywords.TransparentBlend,
                includes = Includes.Unlit,

                // Custom Template
                passTemplatePath = $"{HDUtils.GetHDRenderPipelinePath()}Editor/Material/Unlit/ShaderGraph/HDUnlitPass.template",
            };

            public static ShaderPass DepthForwardOnly = new ShaderPass()
            {
                // Definition
                displayName = "DepthForwardOnly",
                referenceName = "SHADERPASS_DEPTH_ONLY",
                lightMode = "DepthForwardOnly",
                passInclude = "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassDepthOnly.hlsl",
                useInPreview = true,

                // Port Mask
                vertexPorts = VertexPorts.HDUnlitDefault,
                pixelPorts = PixelPorts.HDUnlitOnlyAlpha,

                // Conditional State
                renderStates = RenderStates.HDDepthForwardOnly,
                pragmas = Pragmas.Instanced,
                keywords = Keywords.HDDepthMotionVectors,
                includes = Includes.Unlit,

                // Custom Template
                passTemplatePath = $"{HDUtils.GetHDRenderPipelinePath()}Editor/Material/Unlit/ShaderGraph/HDUnlitPass.template",
            };

            public static ShaderPass MotionVectors = new ShaderPass()
            {
                // Definition
                displayName = "MotionVectors",
                referenceName = "SHADERPASS_MOTION_VECTORS",
                lightMode = "MotionVectors",
                passInclude = "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassMotionVectors.hlsl",
                useInPreview = false,

                // Port Mask
                vertexPorts = VertexPorts.HDUnlitDefault,
                pixelPorts = PixelPorts.HDUnlitOnlyAlpha,

                // Required fields
                requiredFields = RequiredFields.PositionRWS,

                // Conditional State
                renderStates = RenderStates.HDUnlitMotionVectors,
                pragmas = Pragmas.Instanced,
                keywords = Keywords.HDDepthMotionVectors,
                includes = Includes.Unlit,

                // Custom Template
                passTemplatePath = $"{HDUtils.GetHDRenderPipelinePath()}Editor/Material/Unlit/ShaderGraph/HDUnlitPass.template",
            };

            public static ShaderPass Distortion = new ShaderPass()
            {
                // Definition
                displayName = "DistortionVectors",
                referenceName = "SHADERPASS_DISTORTION",
                lightMode = "DistortionVectors",
                passInclude = "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassDistortion.hlsl",
                useInPreview = true,

                // Port Mask
                vertexPorts = VertexPorts.HDUnlitDefault,
                pixelPorts = PixelPorts.HDUnlitDistortion,

                // Conditional State
                renderStates = RenderStates.HDUnlitDistortion,
                pragmas = Pragmas.Instanced,
                keywords = Keywords.TransparentBlend,
                includes = Includes.Unlit,

                // Custom Template
                passTemplatePath = $"{HDUtils.GetHDRenderPipelinePath()}Editor/Material/Unlit/ShaderGraph/HDUnlitPass.template",
            };

            public static ShaderPass ForwardOnly = new ShaderPass()
            {
                // Definition
                displayName = "ForwardOnly",
                referenceName = "SHADERPASS_FORWARD_UNLIT",
                lightMode = "ForwardOnly",
                passInclude = "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassForwardUnlit.hlsl",
                useInPreview = true,

                // Port Mask
                vertexPorts = VertexPorts.HDUnlitDefault,
                pixelPorts = PixelPorts.HDUnlitForward,

                // Conditional State
                renderStates = RenderStates.HDUnlitForward,
                pragmas = Pragmas.Instanced,
                keywords = Keywords.HDUnlitForward,
                includes = Includes.Unlit,

                // Custom Template
                passTemplatePath = $"{HDUtils.GetHDRenderPipelinePath()}Editor/Material/Unlit/ShaderGraph/HDUnlitPass.template",
            };
        }
#endregion

#region HD Lit Passes
        public static class HDLitPasses
        {
            public static ShaderPass GBuffer = new ShaderPass()
            {
                // Definition
                displayName = "GBuffer",
                referenceName = "SHADERPASS_GBUFFER",
                lightMode = "GBuffer",
                passInclude = "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassGBuffer.hlsl",
                useInPreview = true,

                // Port Mask
                vertexPorts = VertexPorts.HDLitDefault,
                pixelPorts = PixelPorts.HDLitDefault,

                // Required Fields
                requiredFields = RequiredFields.LitMinimal,

                // Conditional State
                renderStates = RenderStates.HDLitGBuffer,
                pragmas = Pragmas.Instanced,
                defines = Defines.ShaderGraphRaytracingHigh,
                keywords = Keywords.HDGBuffer,
                includes = Includes.Lit,

                // Custom Template
                passTemplatePath = GetPassTemplatePath("Lit"),
            };

            public static ShaderPass META = new ShaderPass()
            {
                // Definition
                displayName = "META",
                referenceName = "SHADERPASS_LIGHT_TRANSPORT",
                lightMode = "META",
                passInclude = "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassLightTransport.hlsl",
                useInPreview = false,

                // Port Mask
                pixelPorts = PixelPorts.HDLitMeta,

                // Required Fields
                requiredFields = RequiredFields.Meta,

                // Conditional State
                renderStates = RenderStates.Meta,
                pragmas = Pragmas.Instanced,
                defines = Defines.ShaderGraphRaytracingHigh,
                keywords = Keywords.HDBase,
                includes = Includes.Lit,

                // Custom Template
                passTemplatePath = GetPassTemplatePath("Lit"),
            };

            public static ShaderPass ShadowCaster = new ShaderPass()
            {
                // Definition
                displayName = "ShadowCaster",
                referenceName = "SHADERPASS_SHADOWS",
                lightMode = "ShadowCaster",
                passInclude = "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassDepthOnly.hlsl",
                useInPreview = false,

                // Port Mask
                vertexPorts = VertexPorts.HDLitDefault,
                pixelPorts = PixelPorts.HDLitShadowCaster,

                // Conditional State
                renderStates = RenderStates.HDShadowCaster,
                pragmas = Pragmas.Instanced,
                defines = Defines.ShaderGraphRaytracingHigh,
                keywords = Keywords.HDBase,
                includes = Includes.Lit,
                
                // Custom Template
                passTemplatePath = GetPassTemplatePath("Lit"),
            };

            public static ShaderPass SceneSelection = new ShaderPass()
            {
                // Definition
                displayName = "SceneSelectionPass",
                referenceName = "SHADERPASS_DEPTH_ONLY",
                lightMode = "SceneSelectionPass",
                passInclude = "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassDepthOnly.hlsl",
                useInPreview = false,

                // Port Mask
                vertexPorts = VertexPorts.HDLitDefault,
                pixelPorts = PixelPorts.HDLitSceneSelection,

                // Conditional State
                renderStates = RenderStates.HDSceneSelection,
                pragmas = Pragmas.InstancedEditorSync,
                defines = Defines.SceneSelection,
                keywords = Keywords.HDBase,
                includes = Includes.Lit,

                // Custom Template
                passTemplatePath = GetPassTemplatePath("Lit"),
            };

            public static ShaderPass DepthOnly = new ShaderPass()
            {
                // Definition
                displayName = "DepthOnly",
                referenceName = "SHADERPASS_DEPTH_ONLY",
                lightMode = "DepthOnly",
                passInclude = "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassDepthOnly.hlsl",
                useInPreview = true,

                // Port Mask
                vertexPorts = VertexPorts.HDLitDefault,
                pixelPorts = PixelPorts.HDLitDepthMotionVectors,

                // Required Fields
                requiredFields = RequiredFields.LitFull,

                // Conditional State
                renderStates = RenderStates.HDDepthOnly,
                pragmas = Pragmas.Instanced,
                defines = Defines.ShaderGraphRaytracingHigh,
                keywords = Keywords.HDLitDepthMotionVectors,
                includes = Includes.Lit,

                // Custom Template
                passTemplatePath = GetPassTemplatePath("Lit"),
            };

            public static ShaderPass MotionVectors = new ShaderPass()
            {
                // Definition
                displayName = "MotionVectors",
                referenceName = "SHADERPASS_MOTION_VECTORS",
                lightMode = "MotionVectors",
                passInclude = "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassMotionVectors.hlsl",
                useInPreview = false,

                // Port Mask
                vertexPorts = VertexPorts.HDLitDefault,
                pixelPorts = PixelPorts.HDLitDepthMotionVectors,

                // Required Fields
                requiredFields = RequiredFields.LitFull,

                // Conditional State
                renderStates = RenderStates.HDMotionVectors,
                pragmas = Pragmas.Instanced,
                defines = Defines.ShaderGraphRaytracingHigh,
                keywords = Keywords.HDLitDepthMotionVectors,
                includes = Includes.Lit,

                // Custom Template
                passTemplatePath = GetPassTemplatePath("Lit"),
            };

            public static ShaderPass DistortionVectors = new ShaderPass()
            {
                // Definition
                displayName = "DistortionVectors",
                referenceName = "SHADERPASS_DISTORTION",
                lightMode = "DistortionVectors",
                passInclude = "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassDistortion.hlsl",
                useInPreview = true,

                // Port mask
                vertexPorts = VertexPorts.HDLitDefault,
                pixelPorts = PixelPorts.HDLitDistortion,

                // Conditional State
                renderStates= RenderStates.HDLitDistortion,
                pragmas = Pragmas.Instanced,
                defines = Defines.ShaderGraphRaytracingHigh,
                keywords = Keywords.HDBase,
                includes = Includes.Lit,

                // Custom Template
                passTemplatePath = GetPassTemplatePath("Lit"),
            };

            public static ShaderPass TransparentDepthPrepass = new ShaderPass()
            {
                // Definition
                displayName = "TransparentDepthPrepass",
                referenceName = "SHADERPASS_DEPTH_ONLY",
                lightMode = "TransparentDepthPrepass",
                passInclude = "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassDepthOnly.hlsl",
                useInPreview = true,

                // Port Mask
                vertexPorts = VertexPorts.HDLitDefault,
                pixelPorts = PixelPorts.HDLitTransparentDepthPrepass,

                // Conditional State
                renderStates = RenderStates.HDTransparentDepthPrePostPass,
                pragmas = Pragmas.Instanced,
                defines = Defines.TransparentDepthPrepass,
                keywords = Keywords.HDBase,
                includes = Includes.Lit,

                // Custom Template
                passTemplatePath = GetPassTemplatePath("Lit"),
            };

            public static ShaderPass TransparentBackface = new ShaderPass()
            {
                // Definition
                displayName = "TransparentBackface",
                referenceName = "SHADERPASS_FORWARD",
                lightMode = "TransparentBackface",
                passInclude = "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassForward.hlsl",
                useInPreview = true,

                // Port Mask
                vertexPorts = VertexPorts.HDLitDefault,
                pixelPorts = PixelPorts.HDLitTransparentBackface,

                // Conditional State
                renderStates = RenderStates.HDTransparentBackface,
                pragmas = Pragmas.Instanced,
                defines = Defines.Forward,
                keywords = Keywords.HDForward,
                includes = Includes.LitForward,

                // Custom Template
                passTemplatePath = GetPassTemplatePath("Lit"),
            };

            public static ShaderPass Forward = new ShaderPass()
            {
                // Definition
                displayName = "Forward",
                referenceName = "SHADERPASS_FORWARD",
                lightMode = "Forward",
                passInclude = "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassForward.hlsl",
                useInPreview = true,

                // Port Mask
                vertexPorts = VertexPorts.HDLitDefault,
                pixelPorts = PixelPorts.HDLitDefault,

                // Required Fields
                requiredFields = RequiredFields.LitMinimal,
                
                // Conditional State
                renderStates = RenderStates.HDForwardColorMask,
                pragmas = Pragmas.Instanced,
                defines = Defines.Forward,
                keywords = Keywords.HDForward,
                includes = Includes.LitForward,

                // Custom Template
                passTemplatePath = GetPassTemplatePath("Lit"),
            };

            public static ShaderPass TransparentDepthPostpass = new ShaderPass()
            {
                // Definition
                displayName = "TransparentDepthPostpass",
                referenceName = "SHADERPASS_DEPTH_ONLY",
                lightMode = "TransparentDepthPostpass",
                passInclude = "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassDepthOnly.hlsl",
                useInPreview = true,

                // Port Mask
                vertexPorts = VertexPorts.HDLitDefault,
                pixelPorts = PixelPorts.HDLitTransparentDepthPostpass,

                // Conditional State
                renderStates = RenderStates.HDTransparentDepthPrePostPass,
                pragmas = Pragmas.Instanced,
                defines = Defines.TransparentDepthPostpass,
                keywords = Keywords.HDBase,
                includes = Includes.Lit,

                // Custom Template
                passTemplatePath = GetPassTemplatePath("Lit"),
            };
        }
#endregion

#region Eye Passes
        public static class EyePasses
        {
            public static ShaderPass META = new ShaderPass()
            {
                // Definition
                displayName = "META",
                referenceName = "SHADERPASS_LIGHT_TRANSPORT",
                lightMode = "META",
                passInclude = "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassLightTransport.hlsl",
                useInPreview = false,

                // Port Mask
                pixelPorts = PixelPorts.EyeMETA,

                // Required Fields
                requiredFields = RequiredFields.Meta,            

                // Conditional State
                renderStates = RenderStates.Meta,
                pragmas = Pragmas.InstancedRenderingPlayer,
                keywords = Keywords.HDBase,
                includes = Includes.Eye,

                // Custom Template
                passTemplatePath = GetPassTemplatePath("Eye"),
            };

            public static ShaderPass ShadowCaster = new ShaderPass()
            {
                // Definition
                displayName = "ShadowCaster",
                referenceName = "SHADERPASS_SHADOWS",
                lightMode = "ShadowCaster",
                passInclude = "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassDepthOnly.hlsl",
                useInPreview = false,

                // Port Mask
                vertexPorts = VertexPorts.EyeDefault,
                pixelPorts = PixelPorts.EyeAlphaDepth,

                // Conditional State
                renderStates = RenderStates.HDBlendShadowCaster,
                pragmas = Pragmas.InstancedRenderingPlayer,
                keywords = Keywords.HDBase,
                includes = Includes.Eye,

                // Custom Template
                passTemplatePath = GetPassTemplatePath("Eye"),
            };

            public static ShaderPass SceneSelection = new ShaderPass()
            {
                // Definition
                displayName = "SceneSelectionPass",
                referenceName = "SHADERPASS_DEPTH_ONLY",
                lightMode = "SceneSelectionPass",
                passInclude = "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassDepthOnly.hlsl",
                useInPreview = false,

                // Port Mask
                vertexPorts = VertexPorts.EyeDefault,
                pixelPorts = PixelPorts.EyeAlphaDepth,

                // Conditional State
                renderStates = RenderStates.HDSceneSelection,
                pragmas = Pragmas.InstancedRenderingPlayerEditorSync,
                defines = Defines.SceneSelection,
                keywords = Keywords.HDBase,
                includes = Includes.Eye,

                // Custom Template
                passTemplatePath = GetPassTemplatePath("Eye"),
            };

            public static ShaderPass DepthForwardOnly = new ShaderPass()
            {
                // Definition
                displayName = "DepthForwardOnly",
                referenceName = "SHADERPASS_DEPTH_ONLY",
                lightMode = "DepthForwardOnly",
                passInclude = "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassDepthOnly.hlsl",
                useInPreview = true,

                // Port Mask
                vertexPorts = VertexPorts.EyeDefault,
                pixelPorts = PixelPorts.EyeDepthMotionVectors,

                // Required Fields
                requiredFields = RequiredFields.LitFull,

                // Conditional State
                renderStates = RenderStates.HDDepthOnly,
                pragmas = Pragmas.InstancedRenderingPlayer,
                defines = Defines.DepthMotionVectors,
                keywords = Keywords.HDDepthMotionVectorsNoNormal,
                includes = Includes.Eye,

                // Custom Template
                passTemplatePath = GetPassTemplatePath("Eye"),
            };

            public static ShaderPass MotionVectors = new ShaderPass()
            {
                // Definition
                displayName = "MotionVectors",
                referenceName = "SHADERPASS_MOTION_VECTORS",
                lightMode = "MotionVectors",
                passInclude = "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassMotionVectors.hlsl",
                useInPreview = false,

                // Port Mask
                vertexPorts = VertexPorts.EyeDefault,
                pixelPorts = PixelPorts.EyeDepthMotionVectors,

                // Required Fields
                requiredFields = RequiredFields.LitFull,

                // Conditional State
                renderStates = RenderStates.HDMotionVectors,
                pragmas = Pragmas.InstancedRenderingPlayer,
                defines = Defines.DepthMotionVectors,
                keywords = Keywords.HDDepthMotionVectorsNoNormal,
                includes = Includes.Eye,

                // Custom Template
                passTemplatePath = GetPassTemplatePath("Eye"),
            };

            public static ShaderPass ForwardOnly = new ShaderPass()
            {
                // Definition
                displayName = "ForwardOnly",
                referenceName = "SHADERPASS_FORWARD",
                lightMode = "ForwardOnly",
                passInclude = "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassForward.hlsl",
                useInPreview = true,

                // Port Mask
                vertexPorts = VertexPorts.EyeDefault,
                pixelPorts = PixelPorts.EyeForward,

                // Required Fields
                requiredFields = RequiredFields.LitFull,

                // Conditional State
                renderStates = RenderStates.HDForward,
                pragmas = Pragmas.InstancedRenderingPlayer,
                defines = Defines.Forward,
                includes = Includes.EyeForward,
                keywords = Keywords.HDForward,

                // Custom Template
                passTemplatePath = GetPassTemplatePath("Eye"),
            };
        }
#endregion

#region Fabric Passes
        public static class FabricPasses
        {
            public static ShaderPass META = new ShaderPass()
            {
                // Definition
                displayName = "META",
                referenceName = "SHADERPASS_LIGHT_TRANSPORT",
                lightMode = "META",
                passInclude = "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassLightTransport.hlsl",
                useInPreview = false,

                // Port Mask
                pixelPorts = PixelPorts.FabricMETA,

                // Required Fields
                requiredFields = RequiredFields.Meta,

                // Conditional State
                renderStates = RenderStates.Meta,
                pragmas = Pragmas.InstancedRenderingPlayer,
                keywords = Keywords.HDBase,
                includes = Includes.Fabric,
                
                // Custom Template
                passTemplatePath = GetPassTemplatePath("Fabric"),
            };

            public static ShaderPass ShadowCaster = new ShaderPass()
            {
                // Definition
                displayName = "ShadowCaster",
                referenceName = "SHADERPASS_SHADOWS",
                lightMode = "ShadowCaster",
                passInclude = "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassDepthOnly.hlsl",
                useInPreview = false,

                // Port Mask
                vertexPorts = VertexPorts.FabricDefault,
                pixelPorts = PixelPorts.FabricAlphaDepth,

                // Conditional State
                renderStates = RenderStates.HDBlendShadowCaster,
                pragmas = Pragmas.InstancedRenderingPlayer,
                keywords = Keywords.HDBase,
                includes = Includes.Fabric,

                // Custom Template
                passTemplatePath = GetPassTemplatePath("Fabric"),
            };

            public static ShaderPass SceneSelection = new ShaderPass()
            {
                // Definition
                displayName = "SceneSelectionPass",
                referenceName = "SHADERPASS_DEPTH_ONLY",
                lightMode = "SceneSelectionPass",
                passInclude = "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassDepthOnly.hlsl",
                useInPreview = false,

                // Port Mask
                vertexPorts = VertexPorts.FabricDefault,
                pixelPorts = PixelPorts.FabricAlphaDepth,

                // Conditional State
                renderStates = RenderStates.HDShadowCaster,
                pragmas = Pragmas.InstancedRenderingPlayerEditorSync,
                defines = Defines.SceneSelection,
                keywords = Keywords.HDBase,
                includes = Includes.Fabric,

                // Custom Template
                passTemplatePath = GetPassTemplatePath("Fabric"),
            };

            public static ShaderPass DepthForwardOnly = new ShaderPass()
            {
                // Definition
                displayName = "DepthForwardOnly",
                referenceName = "SHADERPASS_DEPTH_ONLY",
                lightMode = "DepthForwardOnly",
                passInclude = "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassDepthOnly.hlsl",
                useInPreview = true,

                // Port Mask
                vertexPorts = VertexPorts.FabricDefault,
                pixelPorts = PixelPorts.FabricDepthMotionVectors,

                // Required Fields
                requiredFields = RequiredFields.LitFull,

                // Conditional State
                renderStates = RenderStates.HDDepthOnly,
                pragmas = Pragmas.InstancedRenderingPlayer,
                defines = Defines.DepthMotionVectors,
                keywords = Keywords.HDDepthMotionVectorsNoNormal,
                includes = Includes.Fabric,

                // Custom Template
                passTemplatePath = GetPassTemplatePath("Fabric"),
            };

            public static ShaderPass MotionVectors = new ShaderPass()
            {
                // Definition
                displayName = "MotionVectors",
                referenceName = "SHADERPASS_MOTION_VECTORS",
                lightMode = "MotionVectors",
                passInclude = "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassMotionVectors.hlsl",
                useInPreview = false,

                // Port Mask
                vertexPorts = VertexPorts.FabricDefault,
                pixelPorts = PixelPorts.FabricDepthMotionVectors,

                // Required Fields
                requiredFields = RequiredFields.LitFull,

                // Conditional State
                renderStates = RenderStates.HDMotionVectors,
                pragmas = Pragmas.InstancedRenderingPlayer,
                defines = Defines.DepthMotionVectors,
                keywords = Keywords.HDDepthMotionVectorsNoNormal,
                includes = Includes.Fabric,
                
                // Custom Template
                passTemplatePath = GetPassTemplatePath("Fabric"),
            };

            public static ShaderPass FabricForwardOnly = new ShaderPass()
            {
                // Definition
                displayName = "ForwardOnly",
                referenceName = "SHADERPASS_FORWARD",
                lightMode = "ForwardOnly",
                passInclude = "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassForward.hlsl",
                useInPreview = true,

                // Port Mask
                vertexPorts = VertexPorts.FabricDefault,
                pixelPorts = PixelPorts.FabricForward,

                // Required Fields
                requiredFields = RequiredFields.LitFull,

                // Conditional State
                renderStates = RenderStates.HDForward,
                pragmas = Pragmas.InstancedRenderingPlayer,
                defines = Defines.Forward,
                keywords = Keywords.HDForward,
                includes = Includes.FabricForward,

                // Custom Template
                passTemplatePath = GetPassTemplatePath("Fabric"),
            };
        }
#endregion

#region Hair Passes
        public static class HairPasses
        {
            public static ShaderPass META = new ShaderPass()
            {
                // Definition
                displayName = "META",
                referenceName = "SHADERPASS_LIGHT_TRANSPORT",
                lightMode = "META",
                passInclude = "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassLightTransport.hlsl",
                useInPreview = false,

                // Port Mask
                pixelPorts = PixelPorts.HairMETA,

                // Required Fields
                requiredFields = RequiredFields.Meta,

                // Conditional State
                renderStates = RenderStates.Meta,
                pragmas = Pragmas.InstancedRenderingPlayer,
                keywords = Keywords.HDBase,
                includes = Includes.Hair,

                // Custom Template
                passTemplatePath = GetPassTemplatePath("Hair"),
            };

            public static ShaderPass ShadowCaster = new ShaderPass()
            {
                // Definition
                displayName = "ShadowCaster",
                referenceName = "SHADERPASS_SHADOWS",
                lightMode = "ShadowCaster",
                passInclude = "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassDepthOnly.hlsl",
                useInPreview = false,

                // Port Mask
                vertexPorts = VertexPorts.HairDefault,
                pixelPorts = PixelPorts.HairShadowCaster,

                // Conditional State
                renderStates = RenderStates.HDBlendShadowCaster,
                pragmas = Pragmas.InstancedRenderingPlayer,
                keywords = Keywords.HDBase,
                includes = Includes.Hair,

                // Custom Template
                passTemplatePath = GetPassTemplatePath("Hair"),
            };

            public static ShaderPass SceneSelection = new ShaderPass()
            {
                // Definition
                displayName = "SceneSelectionPass",
                referenceName = "SHADERPASS_DEPTH_ONLY",
                lightMode = "SceneSelectionPass",
                passInclude = "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassDepthOnly.hlsl",
                useInPreview = false,

                // Port Mask
                vertexPorts = VertexPorts.HairDefault,
                pixelPorts = PixelPorts.HairAlphaDepth,

                // Conditional State
                renderStates = RenderStates.HDSceneSelection,
                pragmas = Pragmas.InstancedRenderingPlayerEditorSync,
                defines = Defines.SceneSelection,
                keywords = Keywords.HDBase,
                includes = Includes.Hair,

                // Custom Template
                passTemplatePath = GetPassTemplatePath("Hair"),
            };

            public static ShaderPass DepthForwardOnly = new ShaderPass()
            {
                // Definition
                displayName = "DepthForwardOnly",
                referenceName = "SHADERPASS_DEPTH_ONLY",
                lightMode = "DepthForwardOnly",
                passInclude = "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassDepthOnly.hlsl",
                useInPreview = true,

                // Port Mask
                vertexPorts = VertexPorts.HairDefault,
                pixelPorts = PixelPorts.HairDepthMotionVectors,

                // Required Fields
                requiredFields = RequiredFields.LitFull,

                // Conditional State
                renderStates = RenderStates.HairDepthOnly,
                pragmas = Pragmas.InstancedRenderingPlayer,
                defines = Defines.DepthMotionVectors,
                keywords = Keywords.HDDepthMotionVectorsNoNormal,
                includes = Includes.Hair,

                // Custom Template
                passTemplatePath = GetPassTemplatePath("Hair"),
            };

            public static ShaderPass MotionVectors = new ShaderPass()
            {
                // Definition
                displayName = "MotionVectors",
                referenceName = "SHADERPASS_MOTION_VECTORS",
                lightMode = "MotionVectors",
                passInclude = "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassMotionVectors.hlsl",
                useInPreview = false,

                // Port Mask
                vertexPorts = VertexPorts.HairDefault,
                pixelPorts = PixelPorts.HairDepthMotionVectors,

                // Required Fields
                requiredFields = RequiredFields.LitFull,

                // Conditional State
                renderStates = RenderStates.HairMotionVectors,
                pragmas = Pragmas.InstancedRenderingPlayer,
                defines = Defines.DepthMotionVectors,
                keywords = Keywords.HDDepthMotionVectorsNoNormal,
                includes = Includes.Hair,

                // Custom Template
                passTemplatePath = GetPassTemplatePath("Hair"),
            };

            public static ShaderPass TransparentDepthPrepass = new ShaderPass()
            {
                // Definition
                displayName = "TransparentDepthPrepass",
                referenceName = "SHADERPASS_DEPTH_ONLY",
                lightMode = "TransparentDepthPrepass",
                passInclude = "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassDepthOnly.hlsl",
                useInPreview = true,

                // Port Mask
                vertexPorts = VertexPorts.HairDefault,
                pixelPorts = PixelPorts.HairTransparentDepthPrepass,

                // Conditional State
                renderStates = RenderStates.HDTransparentDepthPrePostPass,
                pragmas = Pragmas.InstancedRenderingPlayer,
                defines = Defines.TransparentDepthPrepass,
                keywords = Keywords.HDBase,
                includes = Includes.Hair,

                // Custom Template
                passTemplatePath = GetPassTemplatePath("Hair"),
            };

            public static ShaderPass TransparentBackface = new ShaderPass()
            {
                // Definition
                displayName = "TransparentBackface",
                referenceName = "SHADERPASS_FORWARD",
                lightMode = "TransparentBackface",
                passInclude = "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassForward.hlsl",
                useInPreview = true,

                // Port Mask
                vertexPorts = VertexPorts.HairDefault,
                pixelPorts = PixelPorts.HairTransparentBackface,

                // Required Fields
                requiredFields = RequiredFields.LitMinimal,

                // Conditional State
                renderStates = RenderStates.HDTransparentBackface,
                pragmas = Pragmas.InstancedRenderingPlayer,
                defines = Defines.Forward,
                keywords = Keywords.HDForward,
                includes = Includes.HairForward,

                // Custom Template
                passTemplatePath = GetPassTemplatePath("Hair"),
            };

            public static ShaderPass ForwardOnly = new ShaderPass()
            {
                // Definition
                displayName = "ForwardOnly",
                referenceName = "SHADERPASS_FORWARD",
                lightMode = "ForwardOnly",
                passInclude = "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassForward.hlsl",
                useInPreview = true,

                // Port Mask
                vertexPorts = VertexPorts.HairDefault,
                pixelPorts = PixelPorts.HairForward,

                // Required Fields
                requiredFields = RequiredFields.LitFull,

                // Conditional State
                renderStates = RenderStates.HDForwardColorMask,
                pragmas = Pragmas.InstancedRenderingPlayer,
                defines = Defines.Forward,
                includes = Includes.HairForward,
                keywords = Keywords.HDForward,

                // Custom Template
                passTemplatePath = GetPassTemplatePath("Hair"),
            };

            public static ShaderPass TransparentDepthPostpass = new ShaderPass()
            {
                // Definition
                displayName = "TransparentDepthPostpass",
                referenceName = "SHADERPASS_DEPTH_ONLY",
                lightMode = "TransparentDepthPostpass",
                passInclude = "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassDepthOnly.hlsl",
                useInPreview = true,

                // Port Mask
                vertexPorts = VertexPorts.HairDefault,
                pixelPorts = PixelPorts.HairTransparentDepthPostpass,

                // Conditional State
                renderStates = RenderStates.HDTransparentDepthPrePostPass,
                pragmas = Pragmas.InstancedRenderingPlayer,
                defines = Defines.TransparentDepthPostpass,
                keywords = Keywords.HDBase,
                includes = Includes.Hair,

                // Custom Template
                passTemplatePath = GetPassTemplatePath("Hair"),
            };
        }
#endregion

#region StackLit Passes
        public static class StackLitPasses
        {
            public static ShaderPass META = new ShaderPass()
            {
                // Definition
                displayName = "META",
                referenceName = "SHADERPASS_LIGHT_TRANSPORT",
                lightMode = "META",
                passInclude = "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassLightTransport.hlsl",
                useInPreview = false,

                // Port Mask
                pixelPorts = PixelPorts.StackLitMETA,

                // Required Fields
                requiredFields = RequiredFields.Meta,

                // Conditional State
                renderStates = RenderStates.Meta,
                pragmas = Pragmas.InstancedRenderingPlayer,
                keywords = Keywords.HDBase,
                includes = Includes.StackLit,

                // Custom Template
                passTemplatePath = GetPassTemplatePath("StackLit"),
            };

            public static ShaderPass ShadowCaster = new ShaderPass()
            {
                // Definition
                displayName = "ShadowCaster",
                referenceName = "SHADERPASS_SHADOWS",
                lightMode = "ShadowCaster",
                passInclude = "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassDepthOnly.hlsl",
                useInPreview = false,

                // Port Mask
                vertexPorts = VertexPorts.StackLitPosition,
                pixelPorts = PixelPorts.StackLitAlphaDepth,

                // Conditional State
                renderStates = RenderStates.StackLitShadowCaster,
                pragmas = Pragmas.InstancedRenderingPlayer,
                keywords = Keywords.HDBase,
                includes = Includes.StackLit,

                // Custom Template
                passTemplatePath = GetPassTemplatePath("StackLit"),
            };

            public static ShaderPass SceneSelection = new ShaderPass()
            {
                // Definition
                displayName = "SceneSelectionPass",
                referenceName = "SHADERPASS_DEPTH_ONLY",
                lightMode = "SceneSelectionPass",
                passInclude = "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassDepthOnly.hlsl",
                useInPreview = false,

                // Port Mask
                vertexPorts = VertexPorts.StackLitDefault,
                pixelPorts = PixelPorts.StackLitAlphaDepth,

                // Conditional State
                renderStates = RenderStates.HDSceneSelection,
                pragmas = Pragmas.InstancedRenderingPlayerEditorSync,
                defines = Defines.SceneSelection,
                keywords = Keywords.HDBase,
                includes = Includes.StackLit,

                // Custom Template
                passTemplatePath = GetPassTemplatePath("StackLit"),
            };

            public static ShaderPass DepthForwardOnly = new ShaderPass()
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

                // Port Mask
                vertexPorts = VertexPorts.StackLitDefault,
                pixelPorts = PixelPorts.StackLitDepthMotionVectors,

                // Required Fields
                requiredFields = RequiredFields.LitFull,

                // Conditional State
                renderStates = RenderStates.HDDepthOnly,
                pragmas = Pragmas.InstancedRenderingPlayer,
                defines = Defines.DepthMotionVectors,
                keywords = Keywords.HDDepthMotionVectorsNoNormal,
                includes = Includes.StackLit,

                // Custom Template
                passTemplatePath = GetPassTemplatePath("StackLit"),
            };

            public static ShaderPass MotionVectors = new ShaderPass()
            {
                // Definition
                displayName = "MotionVectors",
                referenceName = "SHADERPASS_MOTION_VECTORS",
                lightMode = "MotionVectors",
                passInclude = "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassMotionVectors.hlsl",
                useInPreview = false,

                // Port Mask
                vertexPorts = VertexPorts.StackLitDefault,
                pixelPorts = PixelPorts.StackLitDepthMotionVectors,

                // Required Fields
                requiredFields = RequiredFields.LitFull,

                // Conditional State
                renderStates = RenderStates.HDMotionVectors,
                pragmas = Pragmas.InstancedRenderingPlayer,
                defines = Defines.DepthMotionVectors,
                keywords = Keywords.HDDepthMotionVectorsNoNormal,
                includes = Includes.StackLit,

                // Custom Template
                passTemplatePath = GetPassTemplatePath("StackLit"),
            };

            public static ShaderPass Distortion = new ShaderPass()
            {
                // Definition
                displayName = "DistortionVectors",
                referenceName = "SHADERPASS_DISTORTION",
                lightMode = "DistortionVectors",
                passInclude = "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassDistortion.hlsl",
                useInPreview = true,

                // Port Mask
                vertexPorts = VertexPorts.StackLitDefault,
                pixelPorts = PixelPorts.StackLitDistortion,

                // Conditional State
                renderStates = RenderStates.StackLitDistortion,
                pragmas = Pragmas.InstancedRenderingPlayer,
                keywords = Keywords.HDBase,
                includes = Includes.StackLit,

                // Custom Template
                passTemplatePath = GetPassTemplatePath("StackLit"),
            };

            public static ShaderPass ForwardOnly = new ShaderPass()
            {
                // Definition
                displayName = "ForwardOnly",
                referenceName = "SHADERPASS_FORWARD",
                lightMode = "ForwardOnly",
                passInclude = "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassForward.hlsl",
                useInPreview = true,

                // Port Mask
                vertexPorts = VertexPorts.StackLitDefault,
                pixelPorts = PixelPorts.StackLitForward,

                // Required Fields
                requiredFields = RequiredFields.LitFull,

                // Conditional State
                renderStates = RenderStates.HDForward,
                pragmas = Pragmas.InstancedRenderingPlayer,
                defines = Defines.Forward,
                keywords = Keywords.HDForward,
                includes = Includes.StackLitForward,

                // Custom Template
                passTemplatePath = GetPassTemplatePath("StackLit"),
            };
        }
#endregion

#region PortMasks
        static class VertexPorts
        {
            public static int[] UnlitDefault = new int[]
            {
                UnlitMasterNode.PositionSlotId,
                UnlitMasterNode.VertNormalSlotId,
                UnlitMasterNode.VertTangentSlotId,
            };

            public static int[] PBRDefault = new int[]
            {
                PBRMasterNode.PositionSlotId,
                PBRMasterNode.VertNormalSlotId,
                PBRMasterNode.VertTangentSlotId,
            };

            public static int[] HDUnlitDefault = new int[]
            {
                HDUnlitMasterNode.PositionSlotId,
                HDUnlitMasterNode.VertexNormalSlotId,
                HDUnlitMasterNode.VertexTangentSlotId,
            };

            public static int[] HDLitDefault = new int[]
            {
                HDLitMasterNode.PositionSlotId,
                HDLitMasterNode.VertexNormalSlotID,
                HDLitMasterNode.VertexTangentSlotID,
            };

            public static int[] EyeDefault = new int[]
            {
                EyeMasterNode.PositionSlotId,
                EyeMasterNode.VertexNormalSlotID,
                EyeMasterNode.VertexTangentSlotID,
            };

            public static int[] FabricDefault = new int[]
            {
                FabricMasterNode.PositionSlotId,
                FabricMasterNode.VertexNormalSlotId,
                FabricMasterNode.VertexTangentSlotId,
            };

            public static int[] HairDefault = new int[]
            {
                HairMasterNode.PositionSlotId,
                HairMasterNode.VertexNormalSlotId,
                HairMasterNode.VertexTangentSlotId,
            };

            public static int[] StackLitDefault = new int[]
            {
                StackLitMasterNode.PositionSlotId,
                StackLitMasterNode.VertexNormalSlotId,
                StackLitMasterNode.VertexTangentSlotId
            };

            public static int[] StackLitPosition = new int[]
            {
                StackLitMasterNode.PositionSlotId,
            };
        }

        static class PixelPorts
        {
            public static int[] UnlitDefault = new int[]
            {
                UnlitMasterNode.ColorSlotId,
                UnlitMasterNode.AlphaSlotId,
                UnlitMasterNode.AlphaThresholdSlotId,
            };

            public static int[] UnlitOnlyAlpha = new int[]
            {
                UnlitMasterNode.AlphaSlotId,
                UnlitMasterNode.AlphaThresholdSlotId,
            };

            public static int[] PBRDefault = new int[]
            {
                PBRMasterNode.AlbedoSlotId,
                PBRMasterNode.NormalSlotId,
                PBRMasterNode.MetallicSlotId,
                PBRMasterNode.SpecularSlotId,
                PBRMasterNode.EmissionSlotId,
                PBRMasterNode.SmoothnessSlotId,
                PBRMasterNode.OcclusionSlotId,
                PBRMasterNode.AlphaSlotId,
                PBRMasterNode.AlphaThresholdSlotId,
            };

            public static int[] PBROnlyAlpha = new int[]
            {
                PBRMasterNode.AlphaSlotId,
                PBRMasterNode.AlphaThresholdSlotId,
            };

            public static int[] PBRDepthMotionVectors = new int[]
            {
                PBRMasterNode.NormalSlotId,
                PBRMasterNode.SmoothnessSlotId,
                PBRMasterNode.AlphaSlotId,
                PBRMasterNode.AlphaThresholdSlotId,
            };

            public static int[] HDUnlitDefault = new int[]
            {
                HDUnlitMasterNode.ColorSlotId,
                HDUnlitMasterNode.AlphaSlotId,
                HDUnlitMasterNode.AlphaThresholdSlotId,
                HDUnlitMasterNode.EmissionSlotId,
            };

            public static int[] HDUnlitOnlyAlpha = new int[]
            {
                HDUnlitMasterNode.AlphaSlotId,
                HDUnlitMasterNode.AlphaThresholdSlotId,
            };

            public static int[] HDUnlitDistortion = new int[]
            {
                HDUnlitMasterNode.AlphaSlotId,
                HDUnlitMasterNode.AlphaThresholdSlotId,
                HDUnlitMasterNode.DistortionSlotId,
                HDUnlitMasterNode.DistortionBlurSlotId,
            };

            public static int[] HDUnlitForward = new int[]
            {
                HDUnlitMasterNode.ColorSlotId,
                HDUnlitMasterNode.AlphaSlotId,
                HDUnlitMasterNode.AlphaThresholdSlotId,
                HDUnlitMasterNode.EmissionSlotId,
            };

            public static int[] HDLitDefault = new int[]
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
            };

            public static int[] HDLitMeta = new int[]
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
            };

            public static int[] HDLitShadowCaster = new int[]
            {
                HDLitMasterNode.AlphaSlotId,
                HDLitMasterNode.AlphaThresholdSlotId,
                HDLitMasterNode.AlphaThresholdShadowSlotId,
                HDLitMasterNode.DepthOffsetSlotId,
            };

            public static int[] HDLitSceneSelection = new int[]
            {
                HDLitMasterNode.AlphaSlotId,
                HDLitMasterNode.AlphaThresholdSlotId,
                HDLitMasterNode.DepthOffsetSlotId,
            };

            public static int[] HDLitDepthMotionVectors = new int[]
            {
                HDLitMasterNode.NormalSlotId,
                HDLitMasterNode.SmoothnessSlotId,
                HDLitMasterNode.AlphaSlotId,
                HDLitMasterNode.AlphaThresholdSlotId,
                HDLitMasterNode.DepthOffsetSlotId,
            };

            public static int[] HDLitDistortion = new int[]
            {
                HDLitMasterNode.AlphaSlotId,
                HDLitMasterNode.AlphaThresholdSlotId,
                HDLitMasterNode.DistortionSlotId,
                HDLitMasterNode.DistortionBlurSlotId,
            };

            public static int[] HDLitTransparentDepthPrepass = new int[]
            {
                HDLitMasterNode.AlphaSlotId,
                HDLitMasterNode.AlphaThresholdDepthPrepassSlotId,
                HDLitMasterNode.DepthOffsetSlotId,
            };

            public static int[] HDLitTransparentBackface = new int[]
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
            };

            public static int[] HDLitTransparentDepthPostpass = new int[]
            {
                HDLitMasterNode.AlphaSlotId,
                HDLitMasterNode.AlphaThresholdDepthPrepassSlotId,
                HDLitMasterNode.DepthOffsetSlotId,
            };

            public static int[] EyeMETA = new int[]
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
            };

            public static int[] EyeAlphaDepth = new int[]
            {
                EyeMasterNode.AlphaSlotId,
                EyeMasterNode.AlphaClipThresholdSlotId,
                EyeMasterNode.DepthOffsetSlotId,
            };

            public static int[] EyeDepthMotionVectors = new int[]
            {
                EyeMasterNode.NormalSlotId,
                EyeMasterNode.SmoothnessSlotId,
                EyeMasterNode.AlphaSlotId,
                EyeMasterNode.AlphaClipThresholdSlotId,
                EyeMasterNode.DepthOffsetSlotId,
            };

            public static int[] EyeForward = new int[]
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
            };

            public static int[] FabricMETA = new int[]
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
            };

            public static int[] FabricAlphaDepth = new int[]
            {
                FabricMasterNode.AlphaSlotId,
                FabricMasterNode.AlphaClipThresholdSlotId,
                FabricMasterNode.DepthOffsetSlotId,
            };

            public static int[] FabricDepthMotionVectors = new int[]
            {
                FabricMasterNode.NormalSlotId,
                FabricMasterNode.SmoothnessSlotId,
                FabricMasterNode.AlphaSlotId,
                FabricMasterNode.AlphaClipThresholdSlotId,
                FabricMasterNode.DepthOffsetSlotId,
            };

            public static int[] FabricForward = new int[]
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
            };

            public static int[] HairMETA = new int[]
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
            };

            public static int[] HairShadowCaster = new int[]
            {
                HairMasterNode.AlphaSlotId,
                HairMasterNode.AlphaClipThresholdSlotId,
                HairMasterNode.AlphaClipThresholdShadowSlotId,
                HairMasterNode.DepthOffsetSlotId,
            };

            public static int[] HairAlphaDepth = new int[]
            {
                HairMasterNode.AlphaSlotId,
                HairMasterNode.AlphaClipThresholdSlotId,
                HairMasterNode.DepthOffsetSlotId,
            };

            public static int[] HairDepthMotionVectors = new int[]
            {
                HairMasterNode.NormalSlotId,
                HairMasterNode.SmoothnessSlotId,
                HairMasterNode.AlphaSlotId,
                HairMasterNode.AlphaClipThresholdSlotId,
                HairMasterNode.DepthOffsetSlotId,
            };

            public static int[] HairTransparentDepthPrepass = new int[]
            {
                HairMasterNode.AlphaSlotId,
                HairMasterNode.AlphaClipThresholdDepthPrepassSlotId,
                HairMasterNode.DepthOffsetSlotId,
            };

            public static int[] HairTransparentBackface = new int[]
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
            };

            public static int[] HairForward = new int[]
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
            };

            public static int[] HairTransparentDepthPostpass = new int[]
            {
                HairMasterNode.AlphaSlotId,
                HairMasterNode.AlphaClipThresholdDepthPostpassSlotId,
                HairMasterNode.DepthOffsetSlotId,
            };

            public static int[] StackLitMETA = new int[]
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
            };

            public static int[] StackLitAlphaDepth = new int[]
            {
                StackLitMasterNode.AlphaSlotId,
                StackLitMasterNode.AlphaClipThresholdSlotId,
                StackLitMasterNode.DepthOffsetSlotId,
            };

            public static int[] StackLitDepthMotionVectors = new int[]
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
            };

            public static int[] StackLitDistortion = new int[]
            {
                StackLitMasterNode.AlphaSlotId,
                StackLitMasterNode.AlphaClipThresholdSlotId,
                StackLitMasterNode.DistortionSlotId,
                StackLitMasterNode.DistortionBlurSlotId,
                StackLitMasterNode.DepthOffsetSlotId,
            };

            public static int[] StackLitForward = new int[]
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
            };
        }
#endregion

#region RequiredFields
        static class RequiredFields
        {
            public static string[] Meta = new string[]
            {
                "AttributesMesh.normalOS",
                "AttributesMesh.tangentOS",     // Always present as we require it also in case of anisotropic lighting
                "AttributesMesh.uv0",
                "AttributesMesh.uv1",
                "AttributesMesh.color",
                "AttributesMesh.uv2",           // SHADERPASS_LIGHT_TRANSPORT always uses uv2
            };

            public static string[] PositionRWS = new string[]
            {
                "VaryingsMeshToPS.positionRWS",
            };

            public static string[] LitMinimal = new string[]
            {
                "FragInputs.tangentToWorld",
                "FragInputs.positionRWS",
                "FragInputs.texCoord1",
                "FragInputs.texCoord2",
            };

            public static string[] LitFull = new string[]
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
            };
        }
#endregion

#region RenderStates
        static class RenderStates
        {
            static class Uniforms
            { 
                public static readonly string srcBlend = "[_SrcBlend]";
                public static readonly string dstBlend = "[_DstBlend]";
                public static readonly string alphaSrcBlend = "[_AlphaSrcBlend]";
                public static readonly string alphaDstBlend = "[_AlphaDstBlend]";
                public static readonly string cullMode = "[_CullMode]";
                public static readonly string cullModeForward = "[_CullModeForward]";
                public static readonly string zTestDepthEqualForOpaque = "[_ZTestDepthEqualForOpaque]";
                public static readonly string zTestTransparent = "[_ZTestTransparent]";
                public static readonly string zTestGBuffer = "[_ZTestGBuffer]";
                public static readonly string zWrite = "[_ZWrite]";
                public static readonly string zClip = "[_ZClip]";
                public static readonly string stencilWriteMaskDepth = "[_StencilWriteMaskDepth]";
                public static readonly string stencilRefDepth = "[_StencilRefDepth]";
                public static readonly string stencilWriteMaskMV = "[_StencilWriteMaskMV]";
                public static readonly string stencilRefMV = "[_StencilRefMV]";
                public static readonly string stencilWriteMask = "[_StencilWriteMask]";
                public static readonly string stencilRef = "[_StencilRef]";
                public static readonly string stencilWriteMaskGBuffer = "[_StencilWriteMaskGBuffer]";
                public static readonly string stencilRefGBuffer = "[_StencilRefGBuffer]";
                public static readonly string stencilRefDistortionVec = "[_StencilRefDistortionVec]";
            }

            // --------------------------------------------------
            // META

            public static ConditionalRenderState[] Meta = new ConditionalRenderState[]
            {
                new ConditionalRenderState(RenderState.Cull(Cull.Off)),
            };

            // --------------------------------------------------
            // Shadow Caster

            public static ConditionalRenderState[] ShadowCasterUnlit = new ConditionalRenderState[]
            {
                new ConditionalRenderState(RenderState.Cull(Cull.Off), new FieldCondition(DefaultFields.DoubleSided, true)),
                new ConditionalRenderState(RenderState.ZWrite(ZWrite.On)),
                new ConditionalRenderState(RenderState.ColorMask("ColorMask 0")),
            };

            public static ConditionalRenderState[] ShadowCasterPBR = new ConditionalRenderState[]
            {
                new ConditionalRenderState(RenderState.Blend(Blend.One, Blend.Zero)),
                new ConditionalRenderState(RenderState.ZWrite(ZWrite.On)),
                new ConditionalRenderState(RenderState.ColorMask("ColorMask 0")),
            };

            public static ConditionalRenderState[] HDShadowCaster = new ConditionalRenderState[]
            {
                new ConditionalRenderState(RenderState.Cull(Uniforms.cullMode)),
                new ConditionalRenderState(RenderState.ZWrite(ZWrite.On)),
                new ConditionalRenderState(RenderState.ZClip(Uniforms.zClip)),
                new ConditionalRenderState(RenderState.ColorMask("ColorMask 0")),
            };

            public static ConditionalRenderState[] HDBlendShadowCaster = new ConditionalRenderState[]
            {
                new ConditionalRenderState(RenderState.Blend(Blend.One, Blend.Zero)),
                new ConditionalRenderState(RenderState.Cull(Uniforms.cullMode)),
                new ConditionalRenderState(RenderState.ZWrite(ZWrite.On)),
                new ConditionalRenderState(RenderState.ZClip(Uniforms.zClip)),
                new ConditionalRenderState(RenderState.ColorMask("ColorMask 0")),
            };

            public static ConditionalRenderState[] StackLitShadowCaster = new ConditionalRenderState[]
            {
                new ConditionalRenderState(RenderState.Blend(Blend.One, Blend.Zero)),
                new ConditionalRenderState(RenderState.ZWrite(ZWrite.On)),
                new ConditionalRenderState(RenderState.ZClip(Uniforms.zClip)),
                new ConditionalRenderState(RenderState.ColorMask("ColorMask 0")),
            };

            // --------------------------------------------------
            // Scene Selection

            public static ConditionalRenderState[] SceneSelection = new ConditionalRenderState[]
            {
                new ConditionalRenderState(RenderState.Cull(Cull.Off), new FieldCondition(DefaultFields.DoubleSided, true)),
                new ConditionalRenderState(RenderState.ZWrite(ZWrite.On), new FieldCondition(DefaultFields.SurfaceOpaque, true)),
                new ConditionalRenderState(RenderState.ZWrite(ZWrite.Off), new FieldCondition(DefaultFields.SurfaceTransparent, true)),
                new ConditionalRenderState(RenderState.ColorMask("ColorMask 0")),
            };

            public static ConditionalRenderState[] HDSceneSelection = new ConditionalRenderState[]
            {
                new ConditionalRenderState(RenderState.ColorMask("ColorMask 0")),
            };

            public static ConditionalRenderState[] HDUnlitSceneSelection = new ConditionalRenderState[]
            {
                new ConditionalRenderState(RenderState.Cull(Uniforms.cullMode)),
                new ConditionalRenderState(RenderState.ZWrite(ZWrite.On)),
                new ConditionalRenderState(RenderState.ColorMask("ColorMask 0")),
            };

            // --------------------------------------------------
            // Depth Forward Only

            // Caution: When using MSAA we have normal and depth buffer bind.
            // Mean unlit object need to not write in it (or write 0) - Disable color mask for this RT
            // This is not a problem in no MSAA mode as there is no buffer bind
            public static ConditionalRenderState[] DepthForwardOnly = new ConditionalRenderState[]
            {
                new ConditionalRenderState(RenderState.Cull(Cull.Off), new FieldCondition(DefaultFields.DoubleSided, true)),
                new ConditionalRenderState(RenderState.ZWrite(ZWrite.On), new FieldCondition(DefaultFields.SurfaceOpaque, true)),
                new ConditionalRenderState(RenderState.ZWrite(ZWrite.Off), new FieldCondition(DefaultFields.SurfaceTransparent, true)),
                new ConditionalRenderState(RenderState.ColorMask("ColorMask 0 0")),
            };

            // Caution: When using MSAA we have normal and depth buffer bind.
            // Mean unlit object need to not write in it (or write 0) - Disable color mask for this RT
            // This is not a problem in no MSAA mode as there is no buffer bind
            public static ConditionalRenderState[] HDDepthForwardOnly = new ConditionalRenderState[]
            {
                new ConditionalRenderState(RenderState.Cull(Uniforms.cullMode)),
                new ConditionalRenderState(RenderState.ZWrite(ZWrite.On)),
                new ConditionalRenderState(RenderState.ColorMask("ColorMask 0 0")),
                new ConditionalRenderState(RenderState.Stencil(new Stencil()
                {
                    WriteMask = Uniforms.stencilWriteMaskDepth,
                    Ref = Uniforms.stencilRefDepth,
                    Comp = "Always",
                    Pass = "Replace",
                })),
            };

            // --------------------------------------------------
            // Depth Only

            public static ConditionalRenderState[] DepthOnly = new ConditionalRenderState[]
            {
                new ConditionalRenderState(RenderState.ZWrite(ZWrite.On)),
                new ConditionalRenderState(RenderState.Stencil(new Stencil()
                {
                    WriteMask = $"{(int)HDRenderPipeline.StencilBitMask.DecalsForwardOutputNormalBuffer | (int)HDRenderPipeline.StencilBitMask.DoesntReceiveSSR}",
                    Ref = "0",
                    Comp = "Always",
                    Pass = "Replace",
                })),
            };

            public static ConditionalRenderState[] HDDepthOnly = new ConditionalRenderState[]
            {
                new ConditionalRenderState(RenderState.Cull(Uniforms.cullMode)),
                new ConditionalRenderState(RenderState.ZWrite(ZWrite.On)),
                new ConditionalRenderState(RenderState.Stencil(new Stencil()
                {
                    WriteMask = Uniforms.stencilWriteMaskDepth,
                    Ref = Uniforms.stencilRefDepth,
                    Comp = "Always",
                    Pass = "Replace",
                })),
            };

            public static ConditionalRenderState[] HairDepthOnly = new ConditionalRenderState[]
            {
                new ConditionalRenderState(RenderState.ZWrite(ZWrite.On)),
                new ConditionalRenderState(RenderState.Stencil(new Stencil()
                {
                    WriteMask = Uniforms.stencilWriteMaskDepth,
                    Ref = Uniforms.stencilRefDepth,
                    Comp = "Always",
                    Pass = "Replace",
                })),
            };

            // --------------------------------------------------
            // Motion Vectors

            // Caution: When using MSAA we have motion vector, normal and depth buffer bind.
            // Mean unlit object need to not write in it (or write 0) - Disable color mask for this RT
            // This is not a problem in no MSAA mode as there is no buffer bind
            public static ConditionalRenderState[] UnlitMotionVectors = new ConditionalRenderState[]
            {
                new ConditionalRenderState(RenderState.Cull(Cull.Off), new FieldCondition(DefaultFields.DoubleSided, true)),
                new ConditionalRenderState(RenderState.ColorMask("ColorMask 0 1")),
                new ConditionalRenderState(RenderState.Stencil(new Stencil()
                {
                    WriteMask = $"{(int)HDRenderPipeline.StencilBitMask.ObjectMotionVectors}",
                    Ref = $"{(int)HDRenderPipeline.StencilBitMask.ObjectMotionVectors}",
                    Comp = "Always",
                    Pass = "Replace",
                })),
            };

            public static ConditionalRenderState[] PBRMotionVectors = new ConditionalRenderState[]
            {
                new ConditionalRenderState(RenderState.Blend(Blend.One, Blend.Zero)),
                new ConditionalRenderState(RenderState.ZWrite(ZWrite.On)),
                new ConditionalRenderState(RenderState.ColorMask("ColorMask 0")),
                new ConditionalRenderState(RenderState.Stencil(new Stencil()
                {
                    WriteMask = $"{(int)HDRenderPipeline.StencilBitMask.DecalsForwardOutputNormalBuffer | (int)HDRenderPipeline.StencilBitMask.DoesntReceiveSSR | (int)HDRenderPipeline.StencilBitMask.ObjectMotionVectors}",
                    Ref = $"{(int)HDRenderPipeline.StencilBitMask.ObjectMotionVectors}",
                    Comp = "Always",
                    Pass = "Replace",
                })),
            };

            public static ConditionalRenderState[] HDMotionVectors = new ConditionalRenderState[]
            {
                new ConditionalRenderState(RenderState.Cull(Uniforms.cullMode)),
                new ConditionalRenderState(RenderState.ZWrite(ZWrite.On)),
                new ConditionalRenderState(RenderState.Stencil(new Stencil()
                {
                    WriteMask = Uniforms.stencilWriteMaskMV,
                    Ref = Uniforms.stencilRefMV,
                    Comp = "Always",
                    Pass = "Replace",
                })),
            };

            // Caution: When using MSAA we have motion vector, normal and depth buffer bind.
            // Mean unlit object need to not write in it (or write 0) - Disable color mask for this RT
            // This is not a problem in no MSAA mode as there is no buffer bind
            public static ConditionalRenderState[] HDUnlitMotionVectors = new ConditionalRenderState[]
            {
                new ConditionalRenderState(RenderState.Cull(Uniforms.cullMode)),
                new ConditionalRenderState(RenderState.ZWrite(ZWrite.On)),
                new ConditionalRenderState(RenderState.ColorMask("ColorMask 0 1")),
                new ConditionalRenderState(RenderState.Stencil(new Stencil()
                {
                    WriteMask = Uniforms.stencilWriteMaskMV,
                    Ref = Uniforms.stencilRefMV,
                    Comp = "Always",
                    Pass = "Replace",
                })),
            };            

            public static ConditionalRenderState[] HairMotionVectors = new ConditionalRenderState[]
            {
                new ConditionalRenderState(RenderState.Stencil(new Stencil()
                {
                    WriteMask = Uniforms.stencilWriteMaskMV,
                    Ref = Uniforms.stencilRefMV,
                    Comp = "Always",
                    Pass = "Replace",
                })),
            };

            // --------------------------------------------------
            // Forward

            public static ConditionalRenderState[] UnlitForward = new ConditionalRenderState[]
            {
                new ConditionalRenderState(RenderState.Blend(Blend.One, Blend.Zero, Blend.One, Blend.Zero), new FieldCondition(DefaultFields.SurfaceOpaque, true)),
                new ConditionalRenderState(RenderState.Blend(Blend.One, Blend.OneMinusSrcAlpha, Blend.One, Blend.OneMinusSrcAlpha), new FieldCondition[] { 
                    new FieldCondition(DefaultFields.SurfaceTransparent, true), 
                    new FieldCondition(DefaultFields.BlendAlpha, true) }),
                new ConditionalRenderState(RenderState.Blend(Blend.One, Blend.One, Blend.One, Blend.One), new FieldCondition[] { 
                    new FieldCondition(DefaultFields.SurfaceTransparent, true), 
                    new FieldCondition(DefaultFields.BlendAdd, true) }),
                new ConditionalRenderState(RenderState.Blend(Blend.One, Blend.OneMinusSrcAlpha, Blend.One, Blend.OneMinusSrcAlpha), new FieldCondition[] { 
                    new FieldCondition(DefaultFields.SurfaceTransparent, true), 
                    new FieldCondition(DefaultFields.BlendPremultiply, true) }),
                new ConditionalRenderState(RenderState.Blend(Blend.One, Blend.OneMinusSrcAlpha, Blend.One, Blend.OneMinusSrcAlpha), new FieldCondition[] { 
                    new FieldCondition(DefaultFields.SurfaceTransparent, true), 
                    new FieldCondition(DefaultFields.BlendMultiply, true) }),

                new ConditionalRenderState(RenderState.Cull(Cull.Off), new FieldCondition(DefaultFields.DoubleSided, true)),
                new ConditionalRenderState(RenderState.ZWrite(ZWrite.On), new FieldCondition(DefaultFields.SurfaceOpaque, true)),
                new ConditionalRenderState(RenderState.ZWrite(ZWrite.Off), new FieldCondition(DefaultFields.SurfaceTransparent, true)),
                new ConditionalRenderState(RenderState.Stencil(new Stencil()
                {
                    WriteMask = $"{(int)HDRenderPipeline.StencilBitMask.LightingMask}",
                    Ref = $"{(int)StencilLightingUsage.NoLighting}",
                    Comp = "Always",
                    Pass = "Replace",
                })),
            };

            public static ConditionalRenderState[] PBRForward = new ConditionalRenderState[]
            {
                new ConditionalRenderState(RenderState.Blend(Blend.One, Blend.Zero, Blend.One, Blend.Zero), new FieldCondition(DefaultFields.SurfaceOpaque, true)),
                new ConditionalRenderState(RenderState.Blend(Blend.One, Blend.OneMinusSrcAlpha, Blend.One, Blend.OneMinusSrcAlpha), new FieldCondition[] { 
                    new FieldCondition(DefaultFields.SurfaceTransparent, true), 
                    new FieldCondition(DefaultFields.BlendAlpha, true) }),
                new ConditionalRenderState(RenderState.Blend(Blend.One, Blend.One, Blend.One, Blend.One), new FieldCondition[] { 
                    new FieldCondition(DefaultFields.SurfaceTransparent, true), 
                    new FieldCondition(DefaultFields.BlendAdd, true) }),
                new ConditionalRenderState(RenderState.Blend(Blend.One, Blend.OneMinusSrcAlpha, Blend.One, Blend.OneMinusSrcAlpha), new FieldCondition[] { 
                    new FieldCondition(DefaultFields.SurfaceTransparent, true), 
                    new FieldCondition(DefaultFields.BlendPremultiply, true) }),
                new ConditionalRenderState(RenderState.Blend(Blend.One, Blend.OneMinusSrcAlpha, Blend.One, Blend.OneMinusSrcAlpha), new FieldCondition[] { 
                    new FieldCondition(DefaultFields.SurfaceTransparent, true), 
                    new FieldCondition(DefaultFields.BlendMultiply, true) }),
                
                new ConditionalRenderState(RenderState.ZTest(ZTest.Equal), new FieldCondition[] { 
                    new FieldCondition(DefaultFields.SurfaceOpaque, true), 
                    new FieldCondition(DefaultFields.AlphaTest, true) }),
                new ConditionalRenderState(RenderState.Stencil(new Stencil()
                {
                    WriteMask = $"{(int)HDRenderPipeline.StencilBitMask.LightingMask}",
                    Ref = $"{(int)StencilLightingUsage.NoLighting}",
                    Comp = "Always",
                    Pass = "Replace",
                })),
            };

            public static ConditionalRenderState[] HDUnlitForward = new ConditionalRenderState[]
            {
                new ConditionalRenderState(RenderState.Blend(Uniforms.srcBlend, Uniforms.dstBlend, Uniforms.alphaSrcBlend, Uniforms.alphaDstBlend)),
                new ConditionalRenderState(RenderState.Cull(Uniforms.cullMode)),
                new ConditionalRenderState(RenderState.ZWrite(Uniforms.zWrite)),
                new ConditionalRenderState(RenderState.ZTest(Uniforms.zTestTransparent)),
                new ConditionalRenderState(RenderState.Stencil(new Stencil()
                {
                    WriteMask = Uniforms.stencilWriteMask,
                    Ref = Uniforms.stencilRef,
                    Comp = "Always",
                    Pass = "Replace",
                })),
            };

            public static ConditionalRenderState[] HDForward = new ConditionalRenderState[]
            {
                new ConditionalRenderState(RenderState.Blend(Uniforms.srcBlend, Uniforms.dstBlend, Uniforms.alphaSrcBlend, Uniforms.alphaDstBlend)),
                new ConditionalRenderState(RenderState.Cull(Uniforms.cullModeForward)),
                new ConditionalRenderState(RenderState.ZWrite(Uniforms.zWrite)),
                new ConditionalRenderState(RenderState.ZTest(Uniforms.zTestDepthEqualForOpaque), new FieldCondition[] {
                    new FieldCondition(DefaultFields.SurfaceOpaque, true),
                    new FieldCondition(DefaultFields.AlphaTest, false)
                }),
                new ConditionalRenderState(RenderState.ZTest(Uniforms.zTestDepthEqualForOpaque), new FieldCondition[] {
                    new FieldCondition(DefaultFields.SurfaceOpaque, false),
                    new FieldCondition(DefaultFields.AlphaTest, true)
                }),
                new ConditionalRenderState(RenderState.ZTest(ZTest.Equal), new FieldCondition[] {
                    new FieldCondition(DefaultFields.SurfaceOpaque, true),
                    new FieldCondition(DefaultFields.AlphaTest, true)
                }),
                new ConditionalRenderState(RenderState.Stencil(new Stencil()
                {
                    WriteMask = Uniforms.stencilWriteMask,
                    Ref = Uniforms.stencilRef,
                    Comp = "Always",
                    Pass = "Replace",
                })),
            };

            public static ConditionalRenderState[] HDForwardColorMask = new ConditionalRenderState[]
            {
                new ConditionalRenderState(RenderState.Blend(Uniforms.srcBlend, Uniforms.dstBlend, Uniforms.alphaSrcBlend, Uniforms.alphaDstBlend)),
                new ConditionalRenderState(RenderState.Cull(Uniforms.cullModeForward)),
                new ConditionalRenderState(RenderState.ZWrite(Uniforms.zWrite)),
                new ConditionalRenderState(RenderState.ZTest(Uniforms.zTestDepthEqualForOpaque), new FieldCondition[] {
                    new FieldCondition(DefaultFields.SurfaceOpaque, true),
                    new FieldCondition(DefaultFields.AlphaTest, false)
                }),
                new ConditionalRenderState(RenderState.ZTest(Uniforms.zTestDepthEqualForOpaque), new FieldCondition[] {
                    new FieldCondition(DefaultFields.SurfaceOpaque, false),
                    new FieldCondition(DefaultFields.AlphaTest, true)
                }),
                new ConditionalRenderState(RenderState.ZTest(ZTest.Equal), new FieldCondition[] {
                    new FieldCondition(DefaultFields.SurfaceOpaque, true),
                    new FieldCondition(DefaultFields.AlphaTest, true)
                }),
                new ConditionalRenderState(RenderState.ColorMask("ColorMask [_ColorMaskTransparentVel] 1")),
                new ConditionalRenderState(RenderState.Stencil(new Stencil()
                {
                    WriteMask = Uniforms.stencilWriteMask,
                    Ref = Uniforms.stencilRef,
                    Comp = "Always",
                    Pass = "Replace",
                })),
            };

            // --------------------------------------------------
            // GBuffer

            public static ConditionalRenderState[] PBRGBuffer = new ConditionalRenderState[]
            {
                new ConditionalRenderState(RenderState.ZTest(ZTest.Equal), new FieldCondition[] { 
                    new FieldCondition(DefaultFields.SurfaceOpaque, true), 
                    new FieldCondition(DefaultFields.AlphaTest, true) }),
                new ConditionalRenderState(RenderState.Stencil(new Stencil()
                {
                    WriteMask = $"{(int)HDRenderPipeline.StencilBitMask.LightingMask | (int)HDRenderPipeline.StencilBitMask.DoesntReceiveSSR | (int)HDRenderPipeline.StencilBitMask.DecalsForwardOutputNormalBuffer}",
                    Ref = $"{(int)StencilLightingUsage.RegularLighting}",
                    Comp = "Always",
                    Pass = "Replace",
                })),
            };

            public static ConditionalRenderState[] HDLitGBuffer = new ConditionalRenderState[]
            {
                new ConditionalRenderState(RenderState.Cull(Uniforms.cullMode)),
                new ConditionalRenderState(RenderState.ZTest(Uniforms.zTestGBuffer)),
                new ConditionalRenderState(RenderState.Stencil(new Stencil()
                {
                    WriteMask = Uniforms.stencilWriteMaskGBuffer,
                    Ref = Uniforms.stencilRefGBuffer,
                    Comp = "Always",
                    Pass = "Replace",
                })),
            };

            // --------------------------------------------------
            // Distortion

            public static ConditionalRenderState[] HDUnlitDistortion = new ConditionalRenderState[]
            {
                new ConditionalRenderState(RenderState.Blend(Blend.One, Blend.One, Blend.One, Blend.One), new FieldCondition(HDRPShaderGraphFields.DistortionAdd, true)),
                new ConditionalRenderState(RenderState.Blend(Blend.DstColor, Blend.Zero, Blend.DstAlpha, Blend.Zero), new FieldCondition(HDRPShaderGraphFields.DistortionMultiply, true)),
                new ConditionalRenderState(RenderState.Blend(Blend.One, Blend.Zero, Blend.One, Blend.Zero), new FieldCondition(HDRPShaderGraphFields.DistortionReplace, true)),
                new ConditionalRenderState(RenderState.BlendOp(BlendOp.Add, BlendOp.Add)),
                new ConditionalRenderState(RenderState.Cull(Uniforms.cullMode)),
                new ConditionalRenderState(RenderState.ZWrite(ZWrite.Off)),
                new ConditionalRenderState(RenderState.ZTest(ZTest.Always), new FieldCondition(HDRPShaderGraphFields.DistortionDepthTest, false)),
                new ConditionalRenderState(RenderState.ZTest(ZTest.LEqual), new FieldCondition(HDRPShaderGraphFields.DistortionDepthTest, true)),
                new ConditionalRenderState(RenderState.Stencil(new Stencil()
                {
                    WriteMask = Uniforms.stencilRefDistortionVec,
                    Ref = Uniforms.stencilRefDistortionVec,
                    Comp = "Always",
                    Pass = "Replace",
                })),
            };

            public static ConditionalRenderState[] HDLitDistortion = new ConditionalRenderState[]
            {
                new ConditionalRenderState(RenderState.Blend(Blend.One, Blend.One, Blend.One, Blend.One), new FieldCondition(HDRPShaderGraphFields.DistortionAdd, true)),
                new ConditionalRenderState(RenderState.Blend(Blend.DstColor, Blend.Zero, Blend.DstAlpha, Blend.Zero), new FieldCondition(HDRPShaderGraphFields.DistortionMultiply, true)),
                new ConditionalRenderState(RenderState.Blend(Blend.One, Blend.Zero, Blend.One, Blend.Zero), new FieldCondition(HDRPShaderGraphFields.DistortionReplace, true)),
                new ConditionalRenderState(RenderState.BlendOp(BlendOp.Add, BlendOp.Add)),
                new ConditionalRenderState(RenderState.ZWrite(ZWrite.Off)),
                new ConditionalRenderState(RenderState.ZTest(ZTest.Always), new FieldCondition(HDRPShaderGraphFields.DistortionDepthTest, false)),
                new ConditionalRenderState(RenderState.ZTest(ZTest.LEqual), new FieldCondition(HDRPShaderGraphFields.DistortionDepthTest, true)),
                new ConditionalRenderState(RenderState.Stencil(new Stencil()
                {
                    WriteMask = Uniforms.stencilRefDistortionVec,
                    Ref = Uniforms.stencilRefDistortionVec,
                    Comp = "Always",
                    Pass = "Replace",
                })),
            };

            public static ConditionalRenderState[] StackLitDistortion = new ConditionalRenderState[]
            {
                new ConditionalRenderState(RenderState.Blend(Blend.One, Blend.One, Blend.One, Blend.One), new FieldCondition(HDRPShaderGraphFields.DistortionAdd, true)),
                new ConditionalRenderState(RenderState.Blend(Blend.DstColor, Blend.Zero, Blend.DstAlpha, Blend.Zero), new FieldCondition(HDRPShaderGraphFields.DistortionMultiply, true)),
                new ConditionalRenderState(RenderState.Blend(Blend.One, Blend.Zero, Blend.One, Blend.Zero), new FieldCondition(HDRPShaderGraphFields.DistortionReplace, true)),
                new ConditionalRenderState(RenderState.BlendOp(BlendOp.Add, BlendOp.Add)),
                new ConditionalRenderState(RenderState.Cull(Uniforms.cullMode)),
                new ConditionalRenderState(RenderState.ZWrite(ZWrite.Off)),
                new ConditionalRenderState(RenderState.ZTest(ZTest.Always), new FieldCondition(HDRPShaderGraphFields.DistortionDepthTest, false)),
                new ConditionalRenderState(RenderState.ZTest(ZTest.LEqual), new FieldCondition(HDRPShaderGraphFields.DistortionDepthTest, true)),
                new ConditionalRenderState(RenderState.Stencil(new Stencil()
                {
                    WriteMask = $"{(int)HDRenderPipeline.StencilBitMask.DistortionVectors}",
                    Ref = $"{(int)HDRenderPipeline.StencilBitMask.DistortionVectors}",
                    Comp = "Always",
                    Pass = "Replace",
                })),
            };

            // --------------------------------------------------
            // Transparent Depth Prepass & Postpass           

            public static ConditionalRenderState[] HDTransparentDepthPrePostPass = new ConditionalRenderState[]
            {
                new ConditionalRenderState(RenderState.Blend(Blend.One, Blend.Zero)),
                new ConditionalRenderState(RenderState.Cull(Uniforms.cullMode)),
                new ConditionalRenderState(RenderState.ZWrite(ZWrite.On)),
                new ConditionalRenderState(RenderState.ColorMask("ColorMask 0")),
            };

            // --------------------------------------------------
            // Transparent Backface

            public static ConditionalRenderState[] HDTransparentBackface = new ConditionalRenderState[]
            {
                new ConditionalRenderState(RenderState.Blend(Uniforms.srcBlend, Uniforms.dstBlend, Uniforms.alphaSrcBlend, Uniforms.alphaDstBlend)),
                new ConditionalRenderState(RenderState.Cull(Cull.Front)),
                new ConditionalRenderState(RenderState.ZWrite(Uniforms.zWrite)),
                new ConditionalRenderState(RenderState.ZTest(Uniforms.zTestTransparent)),
                new ConditionalRenderState(RenderState.ColorMask("ColorMask [_ColorMaskTransparentVel] 1")),
            };
        }
#endregion

#region Pragmas
        public static class Pragmas
        {
            public static ConditionalPragma[] Basic = new ConditionalPragma[]
            {
                new ConditionalPragma(Pragma.Target(4.5)),
                new ConditionalPragma(Pragma.OnlyRenderers(new Platform[] {Platform.D3D11, Platform.PS4, Platform.XboxOne, Platform.Vulkan, Platform.Metal, Platform.Switch})),
            };

            public static ConditionalPragma[] Instanced = new ConditionalPragma[]
            {
                new ConditionalPragma(Pragma.Target(4.5)),
                new ConditionalPragma(Pragma.OnlyRenderers(new Platform[] {Platform.D3D11, Platform.PS4, Platform.XboxOne, Platform.Vulkan, Platform.Metal, Platform.Switch})),
                new ConditionalPragma(Pragma.MultiCompileInstancing),
            };

            public static ConditionalPragma[] InstancedEditorSync = new ConditionalPragma[]
            {
                new ConditionalPragma(Pragma.Target(4.5)),
                new ConditionalPragma(Pragma.OnlyRenderers(new Platform[] {Platform.D3D11, Platform.PS4, Platform.XboxOne, Platform.Vulkan, Platform.Metal, Platform.Switch})),
                new ConditionalPragma(Pragma.MultiCompileInstancing),
                new ConditionalPragma(Pragma.Custom("editor_sync_compilation")),
            };

            public static ConditionalPragma[] InstancedRenderingPlayer = new ConditionalPragma[]
            {
                new ConditionalPragma(Pragma.Target(4.5)),
                new ConditionalPragma(Pragma.OnlyRenderers(new Platform[] {Platform.D3D11, Platform.PS4, Platform.XboxOne, Platform.Vulkan, Platform.Metal, Platform.Switch})),
                new ConditionalPragma(Pragma.MultiCompileInstancing),
                new ConditionalPragma(Pragma.Custom("instancing_options renderinglayer")),
            };

            public static ConditionalPragma[] InstancedRenderingPlayerEditorSync = new ConditionalPragma[]
            {
                new ConditionalPragma(Pragma.Target(4.5)),
                new ConditionalPragma(Pragma.OnlyRenderers(new Platform[] {Platform.D3D11, Platform.PS4, Platform.XboxOne, Platform.Vulkan, Platform.Metal, Platform.Switch})),
                new ConditionalPragma(Pragma.MultiCompileInstancing),
                new ConditionalPragma(Pragma.Custom("instancing_options renderinglayer")),
                new ConditionalPragma(Pragma.Custom("editor_sync_compilation")),
            };
        }
#endregion

#region Defines
        static class Defines
        {
            public static ConditionalDefine[] SceneSelection = new ConditionalDefine[]
            {
                new ConditionalDefine(KeywordDescriptors.SceneSelectionPass, 1),
            };

            public static ConditionalDefine[] ShaderGraphRaytracingHigh = new ConditionalDefine[]
            {
                new ConditionalDefine(RayTracingNode.GetRayTracingKeyword(), 0),
            };

            public static ConditionalDefine[] Forward = new ConditionalDefine[]
            {
                new ConditionalDefine(KeywordDescriptors.HasLightloop, 1),
                new ConditionalDefine(KeywordDescriptors.LightList, 1, new FieldCondition(DefaultFields.SurfaceTransparent, true)),
                new ConditionalDefine(RayTracingNode.GetRayTracingKeyword(), 0, new FieldCondition(DefaultFields.SurfaceTransparent, true)),
            };

            public static ConditionalDefine[] TransparentDepthPrepass = new ConditionalDefine[]
            {
                new ConditionalDefine(RayTracingNode.GetRayTracingKeyword(), 0),
                new ConditionalDefine(KeywordDescriptors.TransparentDepthPrepass, 1),
            };

            public static ConditionalDefine[] TransparentDepthPostpass = new ConditionalDefine[]
            {
                new ConditionalDefine(RayTracingNode.GetRayTracingKeyword(), 0),
                new ConditionalDefine(KeywordDescriptors.TransparentDepthPostpass, 1),
            };

            public static ConditionalDefine[] DepthMotionVectors = new ConditionalDefine[]
            {
                new ConditionalDefine(RayTracingNode.GetRayTracingKeyword(), 0),
                new ConditionalDefine(KeywordDescriptors.WriteNormalBuffer, 1),
            };
        }
#endregion

#region Keywords
        public static class Keywords
        {
            public static ConditionalKeyword[] WriteMsaaDepth = new ConditionalKeyword[]
            {
                new ConditionalKeyword(KeywordDescriptors.WriteMsaaDepth),
            };

            public static ConditionalKeyword[] DebugDisplay = new ConditionalKeyword[]
            {
                new ConditionalKeyword(KeywordDescriptors.DebugDisplay),
            };

            public static ConditionalKeyword[] LodFadeCrossfade = new ConditionalKeyword[]
            {
                new ConditionalKeyword(KeywordDescriptors.LodFadeCrossfade),
            };

            public static ConditionalKeyword[] GBuffer = new ConditionalKeyword[]
            {
                new ConditionalKeyword(KeywordDescriptors.LodFadeCrossfade),
                new ConditionalKeyword(KeywordDescriptors.DebugDisplay),
                new ConditionalKeyword(KeywordDescriptors.Lightmap),
                new ConditionalKeyword(KeywordDescriptors.DirectionalLightmapCombined),
                new ConditionalKeyword(KeywordDescriptors.DynamicLightmap),
                new ConditionalKeyword(KeywordDescriptors.ShadowsShadowmask),
                new ConditionalKeyword(KeywordDescriptors.LightLayers),
                new ConditionalKeyword(KeywordDescriptors.Decals),
            };

            public static ConditionalKeyword[] DepthMotionVectors = new ConditionalKeyword[]
            {
                new ConditionalKeyword(KeywordDescriptors.WriteMsaaDepth),
                new ConditionalKeyword(KeywordDescriptors.WriteNormalBuffer),
                new ConditionalKeyword(KeywordDescriptors.LodFadeCrossfade),
            };

            public static ConditionalKeyword[] Forward = new ConditionalKeyword[]
            {
                new ConditionalKeyword(KeywordDescriptors.LodFadeCrossfade),
                new ConditionalKeyword(KeywordDescriptors.DebugDisplay),
                new ConditionalKeyword(KeywordDescriptors.Lightmap),
                new ConditionalKeyword(KeywordDescriptors.DirectionalLightmapCombined),
                new ConditionalKeyword(KeywordDescriptors.DynamicLightmap),
                new ConditionalKeyword(KeywordDescriptors.ShadowsShadowmask),
                new ConditionalKeyword(KeywordDescriptors.Decals),
                new ConditionalKeyword(KeywordDescriptors.Shadow),
                new ConditionalKeyword(KeywordDescriptors.LightList, new FieldCondition(DefaultFields.SurfaceOpaque, true)),
            };

            public static ConditionalKeyword[] TransparentBlend = new ConditionalKeyword[]
            {
                new ConditionalKeyword(KeywordDescriptors.SurfaceTypeTransparent),
                new ConditionalKeyword(KeywordDescriptors.BlendMode),
            };

            public static ConditionalKeyword[] HDDepthMotionVectors = new ConditionalKeyword[]
            {
                new ConditionalKeyword(KeywordDescriptors.WriteMsaaDepth),
                new ConditionalKeyword(KeywordDescriptors.SurfaceTypeTransparent),
                new ConditionalKeyword(KeywordDescriptors.BlendMode),
            };

            public static ConditionalKeyword[] HDUnlitForward = new ConditionalKeyword[]
            {
                new ConditionalKeyword(KeywordDescriptors.DebugDisplay),
                new ConditionalKeyword(KeywordDescriptors.SurfaceTypeTransparent),
                new ConditionalKeyword(KeywordDescriptors.BlendMode),
            };

            public static ConditionalKeyword[] HDGBuffer = new ConditionalKeyword[]
            {
                new ConditionalKeyword(KeywordDescriptors.LodFadeCrossfade),
                new ConditionalKeyword(KeywordDescriptors.SurfaceTypeTransparent),
                new ConditionalKeyword(KeywordDescriptors.DoubleSided),
                new ConditionalKeyword(KeywordDescriptors.BlendMode),
                new ConditionalKeyword(KeywordDescriptors.DebugDisplay),
                new ConditionalKeyword(KeywordDescriptors.Lightmap),
                new ConditionalKeyword(KeywordDescriptors.DirectionalLightmapCombined),
                new ConditionalKeyword(KeywordDescriptors.DynamicLightmap),
                new ConditionalKeyword(KeywordDescriptors.ShadowsShadowmask),
                new ConditionalKeyword(KeywordDescriptors.LightLayers),
                new ConditionalKeyword(KeywordDescriptors.Decals),
            };

            public static ConditionalKeyword[] HDBase = new ConditionalKeyword[]
            {
                new ConditionalKeyword(KeywordDescriptors.LodFadeCrossfade),
                new ConditionalKeyword(KeywordDescriptors.SurfaceTypeTransparent),
                new ConditionalKeyword(KeywordDescriptors.BlendMode),
                new ConditionalKeyword(KeywordDescriptors.DoubleSided),
            };

            public static ConditionalKeyword[] HDLitDepthMotionVectors = new ConditionalKeyword[]
            {
                new ConditionalKeyword(KeywordDescriptors.WriteMsaaDepth),
                new ConditionalKeyword(KeywordDescriptors.WriteNormalBuffer),
                new ConditionalKeyword(KeywordDescriptors.LodFadeCrossfade),
                new ConditionalKeyword(KeywordDescriptors.SurfaceTypeTransparent),
                new ConditionalKeyword(KeywordDescriptors.BlendMode),
                new ConditionalKeyword(KeywordDescriptors.DoubleSided),
            };

            public static ConditionalKeyword[] HDForward = new ConditionalKeyword[]
            {
                new ConditionalKeyword(KeywordDescriptors.LodFadeCrossfade),
                new ConditionalKeyword(KeywordDescriptors.SurfaceTypeTransparent),
                new ConditionalKeyword(KeywordDescriptors.DoubleSided),
                new ConditionalKeyword(KeywordDescriptors.BlendMode),
                new ConditionalKeyword(KeywordDescriptors.DebugDisplay),
                new ConditionalKeyword(KeywordDescriptors.Lightmap),
                new ConditionalKeyword(KeywordDescriptors.DirectionalLightmapCombined),
                new ConditionalKeyword(KeywordDescriptors.DynamicLightmap),
                new ConditionalKeyword(KeywordDescriptors.ShadowsShadowmask),
                new ConditionalKeyword(KeywordDescriptors.Shadow),
                new ConditionalKeyword(KeywordDescriptors.Decals),
                new ConditionalKeyword(KeywordDescriptors.LightList, new FieldCondition(DefaultFields.SurfaceOpaque, true)),
            };

            public static ConditionalKeyword[] HDDepthMotionVectorsNoNormal = new ConditionalKeyword[]
            {
                new ConditionalKeyword(KeywordDescriptors.WriteMsaaDepth),
                new ConditionalKeyword(KeywordDescriptors.LodFadeCrossfade),
                new ConditionalKeyword(KeywordDescriptors.SurfaceTypeTransparent),
                new ConditionalKeyword(KeywordDescriptors.BlendMode),
                new ConditionalKeyword(KeywordDescriptors.DoubleSided),
            };
        }
#endregion

#region Includes
        static class Includes
        {
            public static ConditionalInclude[] Unlit = new ConditionalInclude[]
            {
                new ConditionalInclude(Include.File("Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl")),
                new ConditionalInclude(Include.File("Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl")),
                new ConditionalInclude(Include.File("Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/FragInputs.hlsl")),
                new ConditionalInclude(Include.File("Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPass.cs.hlsl")),
                new ConditionalInclude(Include.File("Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Material.hlsl")),
                new ConditionalInclude(Include.File("Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Unlit/Unlit.hlsl")),
                new ConditionalInclude(Include.File("Packages/com.unity.render-pipelines.high-definition/Runtime/Material/BuiltinUtilities.hlsl")),
                new ConditionalInclude(Include.File("Packages/com.unity.render-pipelines.high-definition/Runtime/Material/MaterialUtilities.hlsl")),
                new ConditionalInclude(Include.File("Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderGraphFunctions.hlsl")),
            };

            public static ConditionalInclude[] Lit = new ConditionalInclude[]
            {
                new ConditionalInclude(Include.File("Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl")),
                new ConditionalInclude(Include.File("Packages/com.unity.render-pipelines.core/ShaderLibrary/NormalSurfaceGradient.hlsl")),
                new ConditionalInclude(Include.File("Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl")),
                new ConditionalInclude(Include.File("Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/FragInputs.hlsl")),
                new ConditionalInclude(Include.File("Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPass.cs.hlsl")),
                new ConditionalInclude(Include.File("Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Material.hlsl")),
                new ConditionalInclude(Include.File("Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Lit/Lit.hlsl")),
                new ConditionalInclude(Include.File("Packages/com.unity.render-pipelines.high-definition/Runtime/Material/BuiltinUtilities.hlsl")),
                new ConditionalInclude(Include.File("Packages/com.unity.render-pipelines.high-definition/Runtime/Material/MaterialUtilities.hlsl")),
                new ConditionalInclude(Include.File("Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Decal/DecalUtilities.hlsl")),
                new ConditionalInclude(Include.File("Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Lit/LitDecalData.hlsl")),
                new ConditionalInclude(Include.File("Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderGraphFunctions.hlsl")),
            };

            public static ConditionalInclude[] LitForward = new ConditionalInclude[]
            {
                new ConditionalInclude(Include.File("Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl")),
                new ConditionalInclude(Include.File("Packages/com.unity.render-pipelines.core/ShaderLibrary/NormalSurfaceGradient.hlsl")),
                new ConditionalInclude(Include.File("Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl")),
                new ConditionalInclude(Include.File("Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/FragInputs.hlsl")),
                new ConditionalInclude(Include.File("Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPass.cs.hlsl")),
                new ConditionalInclude(Include.File("Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Material.hlsl")),
                new ConditionalInclude(Include.File("Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/Lighting.hlsl")),
                new ConditionalInclude(Include.File("Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/LightLoop/LightLoopDef.hlsl")),
                new ConditionalInclude(Include.File("Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Lit/Lit.hlsl")),
                new ConditionalInclude(Include.File("Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/LightLoop/LightLoop.hlsl")),
                new ConditionalInclude(Include.File("Packages/com.unity.render-pipelines.high-definition/Runtime/Material/BuiltinUtilities.hlsl")),
                new ConditionalInclude(Include.File("Packages/com.unity.render-pipelines.high-definition/Runtime/Material/MaterialUtilities.hlsl")),
                new ConditionalInclude(Include.File("Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Decal/DecalUtilities.hlsl")),
                new ConditionalInclude(Include.File("Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Lit/LitDecalData.hlsl")),
                new ConditionalInclude(Include.File("Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderGraphFunctions.hlsl")),
            };

            public static ConditionalInclude[] Eye = new ConditionalInclude[]
            {
                new ConditionalInclude(Include.File("Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl")),
                new ConditionalInclude(Include.File("Packages/com.unity.render-pipelines.core/ShaderLibrary/NormalSurfaceGradient.hlsl")),
                new ConditionalInclude(Include.File("Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl")),
                new ConditionalInclude(Include.File("Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/FragInputs.hlsl")),
                new ConditionalInclude(Include.File("Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPass.cs.hlsl")),
                new ConditionalInclude(Include.File("Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Material.hlsl")),
                new ConditionalInclude(Include.File("Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Eye/Eye.hlsl")),
                new ConditionalInclude(Include.File("Packages/com.unity.render-pipelines.high-definition/Runtime/Material/BuiltinUtilities.hlsl")),
                new ConditionalInclude(Include.File("Packages/com.unity.render-pipelines.high-definition/Runtime/Material/MaterialUtilities.hlsl")),
                new ConditionalInclude(Include.File("Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Decal/DecalUtilities.hlsl")),
                new ConditionalInclude(Include.File("Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderGraphFunctions.hlsl")),
            };

            public static ConditionalInclude[] EyeForward = new ConditionalInclude[]
            {
                new ConditionalInclude(Include.File("Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl")),
                new ConditionalInclude(Include.File("Packages/com.unity.render-pipelines.core/ShaderLibrary/NormalSurfaceGradient.hlsl")),
                new ConditionalInclude(Include.File("Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl")),
                new ConditionalInclude(Include.File("Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/FragInputs.hlsl")),
                new ConditionalInclude(Include.File("Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPass.cs.hlsl")),
                new ConditionalInclude(Include.File("Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Material.hlsl")),
                new ConditionalInclude(Include.File("Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/Lighting.hlsl")),
                new ConditionalInclude(Include.File("Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/LightLoop/LightLoopDef.hlsl")),
                new ConditionalInclude(Include.File("Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Eye/Eye.hlsl")),
                new ConditionalInclude(Include.File("Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/LightLoop/LightLoop.hlsl")),
                new ConditionalInclude(Include.File("Packages/com.unity.render-pipelines.high-definition/Runtime/Material/BuiltinUtilities.hlsl")),
                new ConditionalInclude(Include.File("Packages/com.unity.render-pipelines.high-definition/Runtime/Material/MaterialUtilities.hlsl")),
                new ConditionalInclude(Include.File("Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Decal/DecalUtilities.hlsl")),
                new ConditionalInclude(Include.File("Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderGraphFunctions.hlsl")),
            };

            public static ConditionalInclude[] Fabric = new ConditionalInclude[]
            {
                new ConditionalInclude(Include.File("Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl")),
                new ConditionalInclude(Include.File("Packages/com.unity.render-pipelines.core/ShaderLibrary/NormalSurfaceGradient.hlsl")),
                new ConditionalInclude(Include.File("Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl")),
                new ConditionalInclude(Include.File("Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/FragInputs.hlsl")),
                new ConditionalInclude(Include.File("Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPass.cs.hlsl")),
                new ConditionalInclude(Include.File("Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Material.hlsl")),
                new ConditionalInclude(Include.File("Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Fabric/Fabric.hlsl")),
                new ConditionalInclude(Include.File("Packages/com.unity.render-pipelines.high-definition/Runtime/Material/BuiltinUtilities.hlsl")),
                new ConditionalInclude(Include.File("Packages/com.unity.render-pipelines.high-definition/Runtime/Material/MaterialUtilities.hlsl")),
                new ConditionalInclude(Include.File("Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Decal/DecalUtilities.hlsl")),
                new ConditionalInclude(Include.File("Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderGraphFunctions.hlsl")),
            };

            public static ConditionalInclude[] FabricForward = new ConditionalInclude[]
            {
                new ConditionalInclude(Include.File("Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl")),
                new ConditionalInclude(Include.File("Packages/com.unity.render-pipelines.core/ShaderLibrary/NormalSurfaceGradient.hlsl")),
                new ConditionalInclude(Include.File("Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl")),
                new ConditionalInclude(Include.File("Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/FragInputs.hlsl")),
                new ConditionalInclude(Include.File("Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPass.cs.hlsl")),
                new ConditionalInclude(Include.File("Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Material.hlsl")),
                new ConditionalInclude(Include.File("Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/Lighting.hlsl")),
                new ConditionalInclude(Include.File("Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/LightLoop/LightLoopDef.hlsl")),
                new ConditionalInclude(Include.File("Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Fabric/Fabric.hlsl")),
                new ConditionalInclude(Include.File("Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/LightLoop/LightLoop.hlsl")),
                new ConditionalInclude(Include.File("Packages/com.unity.render-pipelines.high-definition/Runtime/Material/BuiltinUtilities.hlsl")),
                new ConditionalInclude(Include.File("Packages/com.unity.render-pipelines.high-definition/Runtime/Material/MaterialUtilities.hlsl")),
                new ConditionalInclude(Include.File("Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Decal/DecalUtilities.hlsl")),
                new ConditionalInclude(Include.File("Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderGraphFunctions.hlsl")),
            };

            public static ConditionalInclude[] Hair = new ConditionalInclude[]
            {
                new ConditionalInclude(Include.File("Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl")),
                new ConditionalInclude(Include.File("Packages/com.unity.render-pipelines.core/ShaderLibrary/NormalSurfaceGradient.hlsl")),
                new ConditionalInclude(Include.File("Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl")),
                new ConditionalInclude(Include.File("Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/FragInputs.hlsl")),
                new ConditionalInclude(Include.File("Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPass.cs.hlsl")),
                new ConditionalInclude(Include.File("Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Material.hlsl")),
                new ConditionalInclude(Include.File("Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Hair/Hair.hlsl")),
                new ConditionalInclude(Include.File("Packages/com.unity.render-pipelines.high-definition/Runtime/Material/BuiltinUtilities.hlsl")),
                new ConditionalInclude(Include.File("Packages/com.unity.render-pipelines.high-definition/Runtime/Material/MaterialUtilities.hlsl")),
                new ConditionalInclude(Include.File("Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Decal/DecalUtilities.hlsl")),
                new ConditionalInclude(Include.File("Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderGraphFunctions.hlsl")),
            };

            public static ConditionalInclude[] HairForward = new ConditionalInclude[]
            {
                new ConditionalInclude(Include.File("Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl")),
                new ConditionalInclude(Include.File("Packages/com.unity.render-pipelines.core/ShaderLibrary/NormalSurfaceGradient.hlsl")),
                new ConditionalInclude(Include.File("Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl")),
                new ConditionalInclude(Include.File("Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/FragInputs.hlsl")),
                new ConditionalInclude(Include.File("Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPass.cs.hlsl")),
                new ConditionalInclude(Include.File("Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Material.hlsl")),
                new ConditionalInclude(Include.File("Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/Lighting.hlsl")),
                new ConditionalInclude(Include.File("Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/LightLoop/LightLoopDef.hlsl")),
                new ConditionalInclude(Include.File("Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Hair/Hair.hlsl")),
                new ConditionalInclude(Include.File("Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/LightLoop/LightLoop.hlsl")),
                new ConditionalInclude(Include.File("Packages/com.unity.render-pipelines.high-definition/Runtime/Material/BuiltinUtilities.hlsl")),
                new ConditionalInclude(Include.File("Packages/com.unity.render-pipelines.high-definition/Runtime/Material/MaterialUtilities.hlsl")),
                new ConditionalInclude(Include.File("Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Decal/DecalUtilities.hlsl")),
                new ConditionalInclude(Include.File("Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderGraphFunctions.hlsl")),
            };

            public static ConditionalInclude[] StackLit = new ConditionalInclude[]
            {
                new ConditionalInclude(Include.File("Packages/com.unity.render-pipelines.high-definition/Runtime/Material/SphericalCapPivot/SpecularOcclusionDef.hlsl")),
                new ConditionalInclude(Include.File("Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl")),
                new ConditionalInclude(Include.File("Packages/com.unity.render-pipelines.core/ShaderLibrary/NormalSurfaceGradient.hlsl")),
                new ConditionalInclude(Include.File("Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl")),
                new ConditionalInclude(Include.File("Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/FragInputs.hlsl")),
                new ConditionalInclude(Include.File("Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPass.cs.hlsl")),
                new ConditionalInclude(Include.File("Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Material.hlsl")),
                new ConditionalInclude(Include.File("Packages/com.unity.render-pipelines.high-definition/Runtime/Material/StackLit/StackLit.hlsl")),
                new ConditionalInclude(Include.File("Packages/com.unity.render-pipelines.high-definition/Runtime/Material/BuiltinUtilities.hlsl")),
                new ConditionalInclude(Include.File("Packages/com.unity.render-pipelines.high-definition/Runtime/Material/MaterialUtilities.hlsl")),
                new ConditionalInclude(Include.File("Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Decal/DecalUtilities.hlsl")),
                new ConditionalInclude(Include.File("Packages/com.unity.render-pipelines.high-definition/Runtime/Material/StackLit/StackLitDecalData.hlsl")),
                new ConditionalInclude(Include.File("Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderGraphFunctions.hlsl")),
            };

            public static ConditionalInclude[] StackLitForward = new ConditionalInclude[]
            {
                new ConditionalInclude(Include.File("Packages/com.unity.render-pipelines.high-definition/Runtime/Material/SphericalCapPivot/SpecularOcclusionDef.hlsl")),
                new ConditionalInclude(Include.File("Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl")),
                new ConditionalInclude(Include.File("Packages/com.unity.render-pipelines.core/ShaderLibrary/NormalSurfaceGradient.hlsl")),
                new ConditionalInclude(Include.File("Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl")),
                new ConditionalInclude(Include.File("Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/FragInputs.hlsl")),
                new ConditionalInclude(Include.File("Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPass.cs.hlsl")),
                new ConditionalInclude(Include.File("Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Material.hlsl")),
                new ConditionalInclude(Include.File("Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/Lighting.hlsl")),
                new ConditionalInclude(Include.File("Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/LightLoop/LightLoopDef.hlsl")),
                new ConditionalInclude(Include.File("Packages/com.unity.render-pipelines.high-definition/Runtime/Material/StackLit/StackLit.hlsl")),
                new ConditionalInclude(Include.File("Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/LightLoop/LightLoop.hlsl")),
                new ConditionalInclude(Include.File("Packages/com.unity.render-pipelines.high-definition/Runtime/Material/BuiltinUtilities.hlsl")),
                new ConditionalInclude(Include.File("Packages/com.unity.render-pipelines.high-definition/Runtime/Material/MaterialUtilities.hlsl")),
                new ConditionalInclude(Include.File("Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Decal/DecalUtilities.hlsl")),
                new ConditionalInclude(Include.File("Packages/com.unity.render-pipelines.high-definition/Runtime/Material/StackLit/StackLitDecalData.hlsl")),
                new ConditionalInclude(Include.File("Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderGraphFunctions.hlsl")),
            };
        }
#endregion

#region KeywordDescriptors
        public static class KeywordDescriptors
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

            public static KeywordDescriptor HasLightloop = new KeywordDescriptor()
            {
                displayName = "Has Lightloop",
                referenceName = "HAS_LIGHTLOOP",
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

            public static KeywordDescriptor SceneSelectionPass = new KeywordDescriptor()
            {
                displayName = "Scene Selection Pass",
                referenceName = "SCENESELECTIONPASS",
                type = KeywordType.Boolean,
                definition = KeywordDefinition.ShaderFeature,
                scope = KeywordScope.Local,
            };

            public static KeywordDescriptor TransparentDepthPrepass = new KeywordDescriptor()
            {
                displayName = "Transparent Depth Prepass",
                referenceName = "CUTOFF_TRANSPARENT_DEPTH_PREPASS",
                type = KeywordType.Boolean,
                definition = KeywordDefinition.ShaderFeature,
                scope = KeywordScope.Local,
            };

            public static KeywordDescriptor TransparentDepthPostpass = new KeywordDescriptor()
            {
                displayName = "Transparent Depth Postpass",
                referenceName = "CUTOFF_TRANSPARENT_DEPTH_POSTPASS",
                type = KeywordType.Boolean,
                definition = KeywordDefinition.ShaderFeature,
                scope = KeywordScope.Local,
            };
        }
#endregion
#region ShaderStructs
        public static class ShaderStructs
        {
            public struct AttributesMesh
            {
                public static string name = "AttributesMesh";
                public static SubscriptDescriptor positionOS = new SubscriptDescriptor(Attributes.name, "positionOS", "", ShaderValueType.Float3, "POSITION");
                public static SubscriptDescriptor normalOS = new SubscriptDescriptor(Attributes.name, "normalOS", "ATTRIBUTES_NEED_NORMAL", ShaderValueType.Float3,
                    "NORMAL", subscriptOptions : SubscriptOptions.Optional);
                public static SubscriptDescriptor tangentOS = new SubscriptDescriptor(Attributes.name, "tangentOS", "ATTRIBUTES_NEED_TANGENT", ShaderValueType.Float4,
                    "TANGENT", subscriptOptions : SubscriptOptions.Optional);
                public static SubscriptDescriptor uv0 = new SubscriptDescriptor(Attributes.name, "uv0", "ATTRIBUTES_NEED_TEXCOORD0", ShaderValueType.Float4,
                    "TEXCOORD0", subscriptOptions : SubscriptOptions.Optional);
                public static SubscriptDescriptor uv1 = new SubscriptDescriptor(Attributes.name, "uv1", "ATTRIBUTES_NEED_TEXCOORD1", ShaderValueType.Float4,
                    "TEXCOORD1", subscriptOptions : SubscriptOptions.Optional);
                public static SubscriptDescriptor uv2 = new SubscriptDescriptor(Attributes.name, "uv2", "ATTRIBUTES_NEED_TEXCOORD2", ShaderValueType.Float4,
                    "TEXCOORD2", subscriptOptions : SubscriptOptions.Optional);
                public static SubscriptDescriptor uv3 = new SubscriptDescriptor(Attributes.name, "uv3", "ATTRIBUTES_NEED_TEXCOORD3", ShaderValueType.Float4,
                    "TEXCOORD3", subscriptOptions : SubscriptOptions.Optional);
                public static SubscriptDescriptor color = new SubscriptDescriptor(Attributes.name, "color", "ATTRIBUTES_NEED_COLOR", ShaderValueType.Float4,
                    "COLOR", subscriptOptions : SubscriptOptions.Optional);
                public static SubscriptDescriptor instanceID = new SubscriptDescriptor(Attributes.name, "instanceID", "", ShaderValueType.UnsignedInteger,
                    "INSTANCEID_SEMANTIC", "UNITY_ANY_INSTANCING_ENABLED");
            }

            public struct VaryingsMeshToPS
            {
                public static string name = "VaryingsMeshToPS";
                public static SubscriptDescriptor positionCS = new SubscriptDescriptor(Varyings.name, "positionCS", "", ShaderValueType.Float4, "Sv_Position");
                public static SubscriptDescriptor positionRWS = new SubscriptDescriptor(Varyings.name, "positionRWS", "VARYINGS_NEED_POSITION_WS", ShaderValueType.Float3,
                    subscriptOptions : SubscriptOptions.Optional);
                public static SubscriptDescriptor normalWS = new SubscriptDescriptor(Varyings.name, "normalWS", "VARYINGS_NEED_NORMAL_WS", ShaderValueType.Float3,
                    subscriptOptions : SubscriptOptions.Optional);
                public static SubscriptDescriptor tangentWS = new SubscriptDescriptor(Varyings.name, "tangentWS", "VARYINGS_NEED_TANGENT_WS", ShaderValueType.Float4,
                    subscriptOptions : SubscriptOptions.Optional);
                public static SubscriptDescriptor texCoord0 = new SubscriptDescriptor(Varyings.name, "texCoord0", "VARYINGS_NEED_TEXCOORD0", ShaderValueType.Float4,
                    subscriptOptions : SubscriptOptions.Optional);
                public static SubscriptDescriptor texCoord1 = new SubscriptDescriptor(Varyings.name, "texCoord1", "VARYINGS_NEED_TEXCOORD1", ShaderValueType.Float4,
                    subscriptOptions : SubscriptOptions.Optional);
                public static SubscriptDescriptor texCoord2 = new SubscriptDescriptor(Varyings.name, "texCoord2", "VARYINGS_NEED_TEXCOORD2", ShaderValueType.Float4,
                    subscriptOptions : SubscriptOptions.Optional);
                public static SubscriptDescriptor texCoord3 = new SubscriptDescriptor(Varyings.name, "texCoord3", "VARYINGS_NEED_TEXCOORD3", ShaderValueType.Float4,
                    subscriptOptions : SubscriptOptions.Optional);
                public static SubscriptDescriptor color = new SubscriptDescriptor(Varyings.name, "color", "VARYINGS_NEED_COLOR", ShaderValueType.Float4,
                    subscriptOptions : SubscriptOptions.Optional);
                public static SubscriptDescriptor instanceID = new SubscriptDescriptor(Varyings.name, "instanceID", "", ShaderValueType.UnsignedInteger,
                    "INSTANCEID_SEMANTIC", "UNITY_ANY_INSTANCING_ENABLED");
                public static SubscriptDescriptor cullFace = new SubscriptDescriptor(Varyings.name, "cullFace", "VARYINGS_NEED_CULLFACE", "FRONT_FACE_TYPE",
                    "FRONT_FACE_SEMANTIC", "defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)", SubscriptOptions.Generated & SubscriptOptions.Optional);
            }

            public struct VaryingsMeshToDS
            {
                public static string name = "VaryingsMeshToDS";
                public static SubscriptDescriptor positionWS = new SubscriptDescriptor(Varyings.name, "positionWS", "VARYINGS_NEED_POSITION_WS", ShaderValueType.Float3);
                public static SubscriptDescriptor normalWS = new SubscriptDescriptor(Varyings.name, "normalWS", "VARYINGS_NEED_NORMAL_WS", ShaderValueType.Float3);
                public static SubscriptDescriptor tangentWS = new SubscriptDescriptor(Varyings.name, "tangentWS", "VARYINGS_NEED_TANGENT_WS", ShaderValueType.Float4,
                    subscriptOptions : SubscriptOptions.Optional);
                public static SubscriptDescriptor texCoord0 = new SubscriptDescriptor(Varyings.name, "texCoord0", "VARYINGS_NEED_TEXCOORD0", ShaderValueType.Float4,
                    subscriptOptions : SubscriptOptions.Optional);
                public static SubscriptDescriptor texCoord1 = new SubscriptDescriptor(Varyings.name, "texCoord1", "VARYINGS_NEED_TEXCOORD1", ShaderValueType.Float4,
                    subscriptOptions : SubscriptOptions.Optional);
                public static SubscriptDescriptor texCoord2 = new SubscriptDescriptor(Varyings.name, "texCoord2", "VARYINGS_NEED_TEXCOORD2", ShaderValueType.Float4,
                    subscriptOptions : SubscriptOptions.Optional);
                public static SubscriptDescriptor texCoord3 = new SubscriptDescriptor(Varyings.name, "texCoord3", "VARYINGS_NEED_TEXCOORD3", ShaderValueType.Float4,
                    subscriptOptions : SubscriptOptions.Optional);
                public static SubscriptDescriptor color = new SubscriptDescriptor(Varyings.name, "color", "VARYINGS_NEED_COLOR", ShaderValueType.Float4,
                    subscriptOptions : SubscriptOptions.Optional);
                public static SubscriptDescriptor instanceID = new SubscriptDescriptor(Varyings.name, "instanceID", "", ShaderValueType.UnsignedInteger,
                    "INSTANCEID_SEMANTIC", "UNITY_ANY_INSTANCING_ENABLED");
            }
            public struct FragInputs
            {
                public static string name = "FragInputs";
                public static SubscriptDescriptor positionRWS = new SubscriptDescriptor(FragInputs.name, "positionRWS", "", ShaderValueType.Float3, 
                    subscriptOptions : SubscriptOptions.Optional);
                public static SubscriptDescriptor tangentToWorld = new SubscriptDescriptor(FragInputs.name, "tangentToWorld", "", ShaderValueType.Float4, 
                    subscriptOptions : SubscriptOptions.Optional);
                public static SubscriptDescriptor texCoord0 = new SubscriptDescriptor(FragInputs.name, "texCoord0", "", ShaderValueType.Float4, 
                    subscriptOptions : SubscriptOptions.Optional);
                public static SubscriptDescriptor texCoord1 = new SubscriptDescriptor(FragInputs.name, "texCoord1", "", ShaderValueType.Float4, 
                    subscriptOptions : SubscriptOptions.Optional);
                public static SubscriptDescriptor texCoord2 = new SubscriptDescriptor(FragInputs.name, "texCoord2", "", ShaderValueType.Float4, 
                    subscriptOptions : SubscriptOptions.Optional);
                public static SubscriptDescriptor texCoord3 = new SubscriptDescriptor(FragInputs.name, "texCoord3", "", ShaderValueType.Float4, 
                    subscriptOptions : SubscriptOptions.Optional);
                public static SubscriptDescriptor color = new SubscriptDescriptor(FragInputs.name, "color", "", ShaderValueType.Float4, 
                    subscriptOptions : SubscriptOptions.Optional);
                public static SubscriptDescriptor IsFrontFace = new SubscriptDescriptor(FragInputs.name, "isFrontFace", "", ShaderValueType.Boolean, 
                    subscriptOptions : SubscriptOptions.Optional);
            }
        }
        public static StructDescriptor AttributesMesh = new StructDescriptor()
        {
            name = "AttributesMesh",
            interpolatorPack = false,
            subscripts = new SubscriptDescriptor[]
            {
                ShaderStructs.AttributesMesh.positionOS,
                ShaderStructs.AttributesMesh.normalOS,
                ShaderStructs.AttributesMesh.tangentOS,
                ShaderStructs.AttributesMesh.uv0,
                ShaderStructs.AttributesMesh.uv1,
                ShaderStructs.AttributesMesh.uv2,
                ShaderStructs.AttributesMesh.uv3,
                ShaderStructs.AttributesMesh.color,
                ShaderStructs.AttributesMesh.instanceID,
            }
        };
        public static StructDescriptor VaryingsMeshToPS = new StructDescriptor()
        {
            name = "VaryingsMeshToPS",
            interpolatorPack = true,
            subscripts = new SubscriptDescriptor[]
            {
                ShaderStructs.VaryingsMeshToPS.positionCS,
                ShaderStructs.VaryingsMeshToPS.positionRWS,
                ShaderStructs.VaryingsMeshToPS.normalWS,
                ShaderStructs.VaryingsMeshToPS.tangentWS,
                ShaderStructs.VaryingsMeshToPS.texCoord0,
                ShaderStructs.VaryingsMeshToPS.texCoord1,
                ShaderStructs.VaryingsMeshToPS.texCoord2,
                ShaderStructs.VaryingsMeshToPS.texCoord3,
                ShaderStructs.VaryingsMeshToPS.color,
                ShaderStructs.VaryingsMeshToPS.instanceID,
                ShaderStructs.VaryingsMeshToPS.cullFace,
            }
        };
        public static StructDescriptor VaryingsMeshToDS = new StructDescriptor()
        {
            name = "VaryingsMeshToDS",
            interpolatorPack = true,
            subscripts = new SubscriptDescriptor[]
            {
                ShaderStructs.VaryingsMeshToDS.positionWS,
                ShaderStructs.VaryingsMeshToDS.normalWS,
                ShaderStructs.VaryingsMeshToDS.tangentWS,
                ShaderStructs.VaryingsMeshToDS.texCoord0,
                ShaderStructs.VaryingsMeshToDS.texCoord1,
                ShaderStructs.VaryingsMeshToDS.texCoord2,
                ShaderStructs.VaryingsMeshToDS.texCoord3,
                ShaderStructs.VaryingsMeshToDS.color,
                ShaderStructs.VaryingsMeshToDS.instanceID,
            }
        };

        public static StructDescriptor VertexDescriptionInputs = new StructDescriptor()
        {
            name = "VertexDescriptionInputs",
            interpolatorPack = false,
            subscripts = new SubscriptDescriptor[]
            {
                MeshTarget.ShaderStructs.VertexDescriptionInputs.ObjectSpaceNormal,
                MeshTarget.ShaderStructs.VertexDescriptionInputs.ViewSpaceNormal,
                MeshTarget.ShaderStructs.VertexDescriptionInputs.WorldSpaceNormal,
                MeshTarget.ShaderStructs.VertexDescriptionInputs.TangentSpaceNormal,

                MeshTarget.ShaderStructs.VertexDescriptionInputs.ObjectSpaceTangent,
                MeshTarget.ShaderStructs.VertexDescriptionInputs.ViewSpaceTangent,
                MeshTarget.ShaderStructs.VertexDescriptionInputs.WorldSpaceTangent,
                MeshTarget.ShaderStructs.VertexDescriptionInputs.TangentSpaceTangent,

                MeshTarget.ShaderStructs.VertexDescriptionInputs.ObjectSpaceBiTangent,
                MeshTarget.ShaderStructs.VertexDescriptionInputs.ViewSpaceBiTangent,
                MeshTarget.ShaderStructs.VertexDescriptionInputs.WorldSpaceBiTangent,
                MeshTarget.ShaderStructs.VertexDescriptionInputs.TangentSpaceBiTangent,

                MeshTarget.ShaderStructs.VertexDescriptionInputs.ObjectSpaceViewDirection,
                MeshTarget.ShaderStructs.VertexDescriptionInputs.ViewSpaceViewDirection,
                MeshTarget.ShaderStructs.VertexDescriptionInputs.WorldSpaceViewDirection,
                MeshTarget.ShaderStructs.VertexDescriptionInputs.TangentSpaceViewDirection,

                MeshTarget.ShaderStructs.VertexDescriptionInputs.ObjectSpacePosition,
                MeshTarget.ShaderStructs.VertexDescriptionInputs.ViewSpacePosition,
                MeshTarget.ShaderStructs.VertexDescriptionInputs.WorldSpacePosition,
                MeshTarget.ShaderStructs.VertexDescriptionInputs.TangentSpacePosition,
                MeshTarget.ShaderStructs.VertexDescriptionInputs.AbsoluteWorldSpacePosition,

                MeshTarget.ShaderStructs.VertexDescriptionInputs.ScreenPosition,
                MeshTarget.ShaderStructs.VertexDescriptionInputs.uv0,
                MeshTarget.ShaderStructs.VertexDescriptionInputs.uv1,
                MeshTarget.ShaderStructs.VertexDescriptionInputs.uv2,
                MeshTarget.ShaderStructs.VertexDescriptionInputs.uv3,
                MeshTarget.ShaderStructs.VertexDescriptionInputs.VertexColor,
                MeshTarget.ShaderStructs.VertexDescriptionInputs.TimeParameters,
            }
        };

        public static StructDescriptor SurfaceDescriptionInputs = new StructDescriptor()
        {
            name = "SurfaceDescriptionInputs",
            interpolatorPack = false,
            subscripts = new SubscriptDescriptor[]
            {
                MeshTarget.ShaderStructs.SurfaceDescriptionInputs.ObjectSpaceNormal,
                MeshTarget.ShaderStructs.SurfaceDescriptionInputs.ViewSpaceNormal,
                MeshTarget.ShaderStructs.SurfaceDescriptionInputs.WorldSpaceNormal,
                MeshTarget.ShaderStructs.SurfaceDescriptionInputs.TangentSpaceNormal,

                MeshTarget.ShaderStructs.SurfaceDescriptionInputs.ObjectSpaceTangent,
                MeshTarget.ShaderStructs.SurfaceDescriptionInputs.ViewSpaceTangent,
                MeshTarget.ShaderStructs.SurfaceDescriptionInputs.WorldSpaceTangent,
                MeshTarget.ShaderStructs.SurfaceDescriptionInputs.TangentSpaceTangent,

                MeshTarget.ShaderStructs.SurfaceDescriptionInputs.ObjectSpaceBiTangent,
                MeshTarget.ShaderStructs.SurfaceDescriptionInputs.ViewSpaceBiTangent,
                MeshTarget.ShaderStructs.SurfaceDescriptionInputs.WorldSpaceBiTangent,
                MeshTarget.ShaderStructs.SurfaceDescriptionInputs.TangentSpaceBiTangent,

                MeshTarget.ShaderStructs.SurfaceDescriptionInputs.ObjectSpaceViewDirection,
                MeshTarget.ShaderStructs.SurfaceDescriptionInputs.ViewSpaceViewDirection,
                MeshTarget.ShaderStructs.SurfaceDescriptionInputs.WorldSpaceViewDirection,
                MeshTarget.ShaderStructs.SurfaceDescriptionInputs.TangentSpaceViewDirection,

                MeshTarget.ShaderStructs.SurfaceDescriptionInputs.ObjectSpacePosition,
                MeshTarget.ShaderStructs.SurfaceDescriptionInputs.ViewSpacePosition,
                MeshTarget.ShaderStructs.SurfaceDescriptionInputs.WorldSpacePosition,
                MeshTarget.ShaderStructs.SurfaceDescriptionInputs.TangentSpacePosition,
                MeshTarget.ShaderStructs.SurfaceDescriptionInputs.AbsoluteWorldSpacePosition,

                MeshTarget.ShaderStructs.SurfaceDescriptionInputs.ScreenPosition,
                MeshTarget.ShaderStructs.SurfaceDescriptionInputs.uv0,
                MeshTarget.ShaderStructs.SurfaceDescriptionInputs.uv1,
                MeshTarget.ShaderStructs.SurfaceDescriptionInputs.uv2,
                MeshTarget.ShaderStructs.SurfaceDescriptionInputs.uv3,
                MeshTarget.ShaderStructs.SurfaceDescriptionInputs.VertexColor,
                MeshTarget.ShaderStructs.SurfaceDescriptionInputs.TimeParameters,
                MeshTarget.ShaderStructs.SurfaceDescriptionInputs.FaceSign,
            }
        };
#endregion

#region Dependencies
        public static List<FieldDependency[]> fieldDependencies = new List<FieldDependency[]>()
        {
            //Standard Varying Dependencies
            new FieldDependency[]
            {
                new FieldDependency(ShaderStructs.VaryingsMeshToPS.positionRWS,   ShaderStructs.AttributesMesh.positionOS),
                new FieldDependency(ShaderStructs.VaryingsMeshToPS.normalWS,      ShaderStructs.AttributesMesh.normalOS),
                new FieldDependency(ShaderStructs.VaryingsMeshToPS.tangentWS,     ShaderStructs.AttributesMesh.tangentOS),
                new FieldDependency(ShaderStructs.VaryingsMeshToPS.texCoord0,     ShaderStructs.AttributesMesh.uv0),
                new FieldDependency(ShaderStructs.VaryingsMeshToPS.texCoord1,     ShaderStructs.AttributesMesh.uv1),
                new FieldDependency(ShaderStructs.VaryingsMeshToPS.texCoord2,     ShaderStructs.AttributesMesh.uv2),
                new FieldDependency(ShaderStructs.VaryingsMeshToPS.texCoord3,     ShaderStructs.AttributesMesh.uv3),
                new FieldDependency(ShaderStructs.VaryingsMeshToPS.color,         ShaderStructs.AttributesMesh.color),
                new FieldDependency(ShaderStructs.VaryingsMeshToPS.instanceID,    ShaderStructs.AttributesMesh.instanceID),
            }, 

            //Tessellation Varying Dependencies
            new FieldDependency[]
            {
                new FieldDependency(ShaderStructs.VaryingsMeshToPS.positionRWS,   ShaderStructs.VaryingsMeshToDS.positionRWS),
                new FieldDependency(ShaderStructs.VaryingsMeshToPS.normalWS,      ShaderStructs.VaryingsMeshToDS.normalWS),
                new FieldDependency(ShaderStructs.VaryingsMeshToPS.tangentWS,     ShaderStructs.VaryingsMeshToDS.tangentWS),
                new FieldDependency(ShaderStructs.VaryingsMeshToPS.texCoord0,     ShaderStructs.VaryingsMeshToDS.texCoord0),
                new FieldDependency(ShaderStructs.VaryingsMeshToPS.texCoord1,     ShaderStructs.VaryingsMeshToDS.texCoord1),
                new FieldDependency(ShaderStructs.VaryingsMeshToPS.texCoord2,     ShaderStructs.VaryingsMeshToDS.texCoord2),
                new FieldDependency(ShaderStructs.VaryingsMeshToPS.texCoord3,     ShaderStructs.VaryingsMeshToDS.texCoord3),
                new FieldDependency(ShaderStructs.VaryingsMeshToPS.color,         ShaderStructs.VaryingsMeshToDS.color),
                new FieldDependency(ShaderStructs.VaryingsMeshToPS.instanceID,    ShaderStructs.VaryingsMeshToDS.instanceID),
            },

            //Tessellation Varying Dependencies, TODO: Why is this loop created?
            new FieldDependency[]
            {
                new FieldDependency(ShaderStructs.VaryingsMeshToDS.tangentWS,     ShaderStructs.VaryingsMeshToPS.tangentWS),
                new FieldDependency(ShaderStructs.VaryingsMeshToDS.texCoord0,     ShaderStructs.VaryingsMeshToPS.texCoord0),
                new FieldDependency(ShaderStructs.VaryingsMeshToDS.texCoord1,     ShaderStructs.VaryingsMeshToPS.texCoord1),
                new FieldDependency(ShaderStructs.VaryingsMeshToDS.texCoord2,     ShaderStructs.VaryingsMeshToPS.texCoord2),
                new FieldDependency(ShaderStructs.VaryingsMeshToDS.texCoord3,     ShaderStructs.VaryingsMeshToPS.texCoord3),
                new FieldDependency(ShaderStructs.VaryingsMeshToDS.color,         ShaderStructs.VaryingsMeshToPS.color),
                new FieldDependency(ShaderStructs.VaryingsMeshToDS.instanceID,    ShaderStructs.VaryingsMeshToPS.instanceID),
            },

            //FragInput dependencies
            new FieldDependency[]
            {
                new FieldDependency(ShaderStructs.FragInputs.positionRWS,        ShaderStructs.VaryingsMeshToPS.positionRWS),
                new FieldDependency(ShaderStructs.FragInputs.tangentToWorld,     ShaderStructs.VaryingsMeshToPS.tangentWS),
                new FieldDependency(ShaderStructs.FragInputs.tangentToWorld,     ShaderStructs.VaryingsMeshToPS.normalWS),
                new FieldDependency(ShaderStructs.FragInputs.texCoord0,          ShaderStructs.VaryingsMeshToPS.texCoord0),
                new FieldDependency(ShaderStructs.FragInputs.texCoord1,          ShaderStructs.VaryingsMeshToPS.texCoord1),
                new FieldDependency(ShaderStructs.FragInputs.texCoord2,          ShaderStructs.VaryingsMeshToPS.texCoord2),
                new FieldDependency(ShaderStructs.FragInputs.texCoord3,          ShaderStructs.VaryingsMeshToPS.texCoord3),
                new FieldDependency(ShaderStructs.FragInputs.color,              ShaderStructs.VaryingsMeshToPS.color),
                new FieldDependency(ShaderStructs.FragInputs.IsFrontFace,        ShaderStructs.VaryingsMeshToPS.cullFace),
            },

            //Vertex Description Dependencies
            new FieldDependency[]
            {
                new FieldDependency(MeshTarget.ShaderStructs.VertexDescriptionInputs.ObjectSpaceNormal,            ShaderStructs.AttributesMesh.normalOS),
                new FieldDependency(MeshTarget.ShaderStructs.VertexDescriptionInputs.WorldSpaceNormal,             ShaderStructs.AttributesMesh.normalOS),
                new FieldDependency(MeshTarget.ShaderStructs.VertexDescriptionInputs.ViewSpaceNormal,              MeshTarget.ShaderStructs.VertexDescriptionInputs.WorldSpaceNormal),
    
                new FieldDependency(MeshTarget.ShaderStructs.VertexDescriptionInputs.ObjectSpaceTangent,           ShaderStructs.AttributesMesh.tangentOS),
                new FieldDependency(MeshTarget.ShaderStructs.VertexDescriptionInputs.WorldSpaceTangent,            ShaderStructs.AttributesMesh.tangentOS),
                new FieldDependency(MeshTarget.ShaderStructs.VertexDescriptionInputs.ViewSpaceTangent,             MeshTarget.ShaderStructs.VertexDescriptionInputs.WorldSpaceTangent),
    
                new FieldDependency(MeshTarget.ShaderStructs.VertexDescriptionInputs.ObjectSpaceBiTangent,         ShaderStructs.AttributesMesh.normalOS),
                new FieldDependency(MeshTarget.ShaderStructs.VertexDescriptionInputs.ObjectSpaceBiTangent,         ShaderStructs.AttributesMesh.tangentOS),
                new FieldDependency(MeshTarget.ShaderStructs.VertexDescriptionInputs.WorldSpaceBiTangent,          MeshTarget.ShaderStructs.VertexDescriptionInputs.ObjectSpaceBiTangent),
                new FieldDependency(MeshTarget.ShaderStructs.VertexDescriptionInputs.ViewSpaceBiTangent,           MeshTarget.ShaderStructs.VertexDescriptionInputs.WorldSpaceBiTangent),
    
                new FieldDependency(MeshTarget.ShaderStructs.VertexDescriptionInputs.ObjectSpacePosition,          ShaderStructs.AttributesMesh.positionOS),
                new FieldDependency(MeshTarget.ShaderStructs.VertexDescriptionInputs.WorldSpacePosition,           ShaderStructs.AttributesMesh.positionOS),
                new FieldDependency(MeshTarget.ShaderStructs.VertexDescriptionInputs.AbsoluteWorldSpacePosition,   ShaderStructs.AttributesMesh.positionOS),
                new FieldDependency(MeshTarget.ShaderStructs.VertexDescriptionInputs.ViewSpacePosition,            MeshTarget.ShaderStructs.VertexDescriptionInputs.WorldSpacePosition),

                new FieldDependency(MeshTarget.ShaderStructs.VertexDescriptionInputs.WorldSpaceViewDirection,      MeshTarget.ShaderStructs.VertexDescriptionInputs.WorldSpacePosition),
                new FieldDependency(MeshTarget.ShaderStructs.VertexDescriptionInputs.ObjectSpaceViewDirection,     MeshTarget.ShaderStructs.VertexDescriptionInputs.WorldSpaceViewDirection),
                new FieldDependency(MeshTarget.ShaderStructs.VertexDescriptionInputs.ViewSpaceViewDirection,       MeshTarget.ShaderStructs.VertexDescriptionInputs.WorldSpaceViewDirection),
                new FieldDependency(MeshTarget.ShaderStructs.VertexDescriptionInputs.TangentSpaceViewDirection,    MeshTarget.ShaderStructs.VertexDescriptionInputs.WorldSpaceViewDirection),
                new FieldDependency(MeshTarget.ShaderStructs.VertexDescriptionInputs.TangentSpaceViewDirection,    MeshTarget.ShaderStructs.VertexDescriptionInputs.WorldSpaceTangent),
                new FieldDependency(MeshTarget.ShaderStructs.VertexDescriptionInputs.TangentSpaceViewDirection,    MeshTarget.ShaderStructs.VertexDescriptionInputs.WorldSpaceBiTangent),
                new FieldDependency(MeshTarget.ShaderStructs.VertexDescriptionInputs.TangentSpaceViewDirection,    MeshTarget.ShaderStructs.VertexDescriptionInputs.WorldSpaceNormal),

                new FieldDependency(MeshTarget.ShaderStructs.VertexDescriptionInputs.ScreenPosition,               MeshTarget.ShaderStructs.VertexDescriptionInputs.WorldSpacePosition),
                new FieldDependency(MeshTarget.ShaderStructs.VertexDescriptionInputs.uv0,                          ShaderStructs.AttributesMesh.uv0),
                new FieldDependency(MeshTarget.ShaderStructs.VertexDescriptionInputs.uv1,                          ShaderStructs.AttributesMesh.uv1),
                new FieldDependency(MeshTarget.ShaderStructs.VertexDescriptionInputs.uv2,                          ShaderStructs.AttributesMesh.uv2),
                new FieldDependency(MeshTarget.ShaderStructs.VertexDescriptionInputs.uv3,                          ShaderStructs.AttributesMesh.uv3),
                new FieldDependency(MeshTarget.ShaderStructs.VertexDescriptionInputs.VertexColor,                  ShaderStructs.AttributesMesh.color),
            },

            //Surface Description Dependencies
            new FieldDependency[]
            {
                new FieldDependency(MeshTarget.ShaderStructs.SurfaceDescriptionInputs.WorldSpaceNormal,             ShaderStructs.FragInputs.tangentToWorld),
                new FieldDependency(MeshTarget.ShaderStructs.SurfaceDescriptionInputs.ObjectSpaceNormal,            MeshTarget.ShaderStructs.SurfaceDescriptionInputs.WorldSpaceNormal),
                new FieldDependency(MeshTarget.ShaderStructs.SurfaceDescriptionInputs.ViewSpaceNormal,              MeshTarget.ShaderStructs.SurfaceDescriptionInputs.WorldSpaceNormal),

                new FieldDependency(MeshTarget.ShaderStructs.SurfaceDescriptionInputs.WorldSpaceTangent,            ShaderStructs.FragInputs.tangentToWorld),
                new FieldDependency(MeshTarget.ShaderStructs.SurfaceDescriptionInputs.ObjectSpaceTangent,           MeshTarget.ShaderStructs.SurfaceDescriptionInputs.WorldSpaceTangent),
                new FieldDependency(MeshTarget.ShaderStructs.SurfaceDescriptionInputs.ViewSpaceTangent,             MeshTarget.ShaderStructs.SurfaceDescriptionInputs.WorldSpaceTangent),
    
                new FieldDependency(MeshTarget.ShaderStructs.SurfaceDescriptionInputs.WorldSpaceBiTangent,          ShaderStructs.FragInputs.tangentToWorld),
                new FieldDependency(MeshTarget.ShaderStructs.SurfaceDescriptionInputs.ObjectSpaceBiTangent,         MeshTarget.ShaderStructs.SurfaceDescriptionInputs.WorldSpaceBiTangent),
                new FieldDependency(MeshTarget.ShaderStructs.SurfaceDescriptionInputs.ViewSpaceBiTangent,           MeshTarget.ShaderStructs.SurfaceDescriptionInputs.WorldSpaceBiTangent),
    
                new FieldDependency(MeshTarget.ShaderStructs.SurfaceDescriptionInputs.WorldSpacePosition,           ShaderStructs.FragInputs.positionRWS),
                new FieldDependency(MeshTarget.ShaderStructs.SurfaceDescriptionInputs.AbsoluteWorldSpacePosition,   ShaderStructs.FragInputs.positionRWS),
                new FieldDependency(MeshTarget.ShaderStructs.SurfaceDescriptionInputs.ObjectSpacePosition,          ShaderStructs.FragInputs.positionRWS),
                new FieldDependency(MeshTarget.ShaderStructs.SurfaceDescriptionInputs.ViewSpacePosition,            ShaderStructs.FragInputs.positionRWS),

                new FieldDependency(MeshTarget.ShaderStructs.SurfaceDescriptionInputs.WorldSpaceViewDirection,      ShaderStructs.FragInputs.positionRWS),
                new FieldDependency(MeshTarget.ShaderStructs.SurfaceDescriptionInputs.ObjectSpaceViewDirection,     MeshTarget.ShaderStructs.SurfaceDescriptionInputs.WorldSpaceViewDirection),
                new FieldDependency(MeshTarget.ShaderStructs.SurfaceDescriptionInputs.ViewSpaceViewDirection,       MeshTarget.ShaderStructs.SurfaceDescriptionInputs.WorldSpaceViewDirection),
                new FieldDependency(MeshTarget.ShaderStructs.SurfaceDescriptionInputs.TangentSpaceViewDirection,    MeshTarget.ShaderStructs.SurfaceDescriptionInputs.WorldSpaceViewDirection),
                new FieldDependency(MeshTarget.ShaderStructs.SurfaceDescriptionInputs.TangentSpaceViewDirection,    MeshTarget.ShaderStructs.SurfaceDescriptionInputs.WorldSpaceTangent),
                new FieldDependency(MeshTarget.ShaderStructs.SurfaceDescriptionInputs.TangentSpaceViewDirection,    MeshTarget.ShaderStructs.SurfaceDescriptionInputs.WorldSpaceBiTangent),
                new FieldDependency(MeshTarget.ShaderStructs.SurfaceDescriptionInputs.TangentSpaceViewDirection,    MeshTarget.ShaderStructs.SurfaceDescriptionInputs.WorldSpaceNormal),

                new FieldDependency(MeshTarget.ShaderStructs.SurfaceDescriptionInputs.ScreenPosition,               MeshTarget.ShaderStructs.SurfaceDescriptionInputs.WorldSpacePosition),
                new FieldDependency(MeshTarget.ShaderStructs.SurfaceDescriptionInputs.uv0,                          ShaderStructs.FragInputs.texCoord0),
                new FieldDependency(MeshTarget.ShaderStructs.SurfaceDescriptionInputs.uv1,                          ShaderStructs.FragInputs.texCoord1),
                new FieldDependency(MeshTarget.ShaderStructs.SurfaceDescriptionInputs.uv2,                          ShaderStructs.FragInputs.texCoord2),
                new FieldDependency(MeshTarget.ShaderStructs.SurfaceDescriptionInputs.uv3,                          ShaderStructs.FragInputs.texCoord3),
                new FieldDependency(MeshTarget.ShaderStructs.SurfaceDescriptionInputs.VertexColor,                  ShaderStructs.FragInputs.color),
                new FieldDependency(MeshTarget.ShaderStructs.SurfaceDescriptionInputs.FaceSign,                     ShaderStructs.FragInputs.IsFrontFace),
            }
        };
#endregion

    }
}
