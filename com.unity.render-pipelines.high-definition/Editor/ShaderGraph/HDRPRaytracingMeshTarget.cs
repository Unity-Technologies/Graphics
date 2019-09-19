using System.Collections.Generic;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEditor.ShaderGraph;
using UnityEditor.ShaderGraph.Internal;
using ShaderPass = UnityEditor.ShaderGraph.Internal.ShaderPass;

namespace UnityEditor.Rendering.HighDefinition
{
    class HDRPRaytracingMeshTarget : ITargetVariant<MeshTarget>
    {
        public string displayName => "HDRP Raytracing";
        public string passTemplatePath => string.Empty;
        public string sharedTemplateDirectory => string.Empty;

        public bool Validate(RenderPipelineAsset pipelineAsset)
        {
            #if ENABLE_RAYTRACING
            return pipelineAsset is HDRenderPipelineAsset;
            #endif

            return false;
        }

        public bool TryGetSubShader(IMasterNode masterNode, out ISubShader subShader)
        {
            switch(masterNode)
            {
                case FabricMasterNode fabricMasterNode:
                    subShader = new FabricSubShader();
                    return true;
                case HDLitMasterNode hdLitMasterNode:
                    subShader = new HDLitSubShader();
                    return true;
                default:
                    subShader = null;
                    return false;
            }
        }

#region Passes
        public static class Passes
        {
            public static ShaderPass HDLitIndirect = new ShaderPass()
            {
                // Definition
                displayName = "IndirectDXR",
                referenceName = "SHADERPASS_RAYTRACING_INDIRECT",
                lightMode = "IndirectDXR",
                passInclude = "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassRaytracingIndirect.hlsl",
                useInPreview = false,

                // Port mask
                vertexPorts = new List<int>()
                {
                    HDLitMasterNode.PositionSlotId,
                    HDLitMasterNode.VertexNormalSlotID,
                    HDLitMasterNode.VertexTangentSlotID
                },
                pixelPorts = new List<int>()
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

                // Pass setup
                pragmas = new List<string>()
                {
                    "#pragma target 4.5",
                    "only_renderers d3d11 ps4 xboxone vulkan metal switch",
                    "multi_compile_instancing"
                },
                defines = new List<string>()
                {
                    "SHADOW_LOW",
                    "SKIP_RASTERIZED_SHADOWS",
                    "RAYTRACING_SHADER_GRAPH_LOW",
                    "HAS_LIGHTLOOP", // FORWARD & INDIRECT
                },
                keywords = new List<KeywordDescriptor>()
                {
                    HDRPMeshTarget.Keywords.Lightmap,
                    HDRPMeshTarget.Keywords.DirectionalLightmapCombined,
                    HDRPMeshTarget.Keywords.DynamicLightmap,
                    HDRPMeshTarget.Keywords.DiffuseLightingOnly,
                    HDRPMeshTarget.Keywords.SurfaceTypeTransparent,
                    HDRPMeshTarget.Keywords.DoubleSided,
                    HDRPMeshTarget.Keywords.BlendMode,
                },
                includes = new List<string>()
                {
                    "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/FragInputs.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPass.cs.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/Raytracing/Shaders/RaytracingMacros.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Material.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/Raytracing/Shaders/ShaderVariablesRaytracing.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/Raytracing/Shaders/ShaderVariablesRaytracingLightLoop.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/Raytracing/Shaders/RaytracingIntersection.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/Lighting.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/LightLoop/LightLoopDef.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Lit/Lit.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Lit/LitRaytracing.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/Raytracing/Shaders/RaytracingLightLoop.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/MaterialUtilities.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/BuiltinUtilities.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/Raytracing/Shaders/RaytracingCommon.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderGraphFunctions.hlsl",
                },

                // Custom template
                passTemplatePath = $"{HDUtils.GetHDRenderPipelinePath()}Editor/Material/Lit/ShaderGraph/HDLitRaytracingPass.template",
                sharedTemplateDirectory = $"{HDUtils.GetHDRenderPipelinePath()}Editor/ShaderGraph",
            };

            public static ShaderPass HDLitVisibility = new ShaderPass()
            {
                // Definition
                displayName = "VisibilityDXR",
                referenceName = "SHADERPASS_RAYTRACING_VISIBILITY",
                lightMode = "VisibilityDXR",
                passInclude = "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassRaytracingVisibility.hlsl",
                useInPreview = false,

                // Port mask
                vertexPorts = new List<int>()
                {
                    HDLitMasterNode.PositionSlotId,
                    HDLitMasterNode.VertexNormalSlotID,
                    HDLitMasterNode.VertexTangentSlotID
                },
                pixelPorts = new List<int>()
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

                // Pass setup
                pragmas = new List<string>()
                {
                    "#pragma target 4.5",
                    "only_renderers d3d11 ps4 xboxone vulkan metal switch",
                    "multi_compile_instancing"
                },
                defines = new List<string>()
                {
                    "RAYTRACING_SHADER_GRAPH_LOW",
                },
                keywords = new List<KeywordDescriptor>()
                {
                    HDRPMeshTarget.Keywords.SurfaceTypeTransparent,
                    HDRPMeshTarget.Keywords.DoubleSided,
                    HDRPMeshTarget.Keywords.BlendMode,
                },
                includes = new List<string>()
                {
                    "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/FragInputs.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPass.cs.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/Raytracing/Shaders/RaytracingMacros.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Material.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/Raytracing/Shaders/ShaderVariablesRaytracing.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/Raytracing/Shaders/ShaderVariablesRaytracingLightLoop.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/Raytracing/Shaders/RaytracingIntersection.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Lit/Lit.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Lit/LitRaytracing.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/MaterialUtilities.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/BuiltinUtilities.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/Raytracing/Shaders/RaytracingCommon.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderGraphFunctions.hlsl",
                },

                // Custom template
                passTemplatePath = $"{HDUtils.GetHDRenderPipelinePath()}Editor/Material/Lit/ShaderGraph/HDLitRaytracingPass.template",
                sharedTemplateDirectory = $"{HDUtils.GetHDRenderPipelinePath()}Editor/ShaderGraph",
            };

            public static ShaderPass HDLitForward = new ShaderPass()
            {
                // Definition
                displayName = "ForwardDXR",
                referenceName = "SHADERPASS_RAYTRACING_FORWARD",
                lightMode = "ForwardDXR",
                passInclude = "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassRaytracingForward.hlsl",
                useInPreview = false,

                // Port mask
                vertexPorts = new List<int>()
                {
                    HDLitMasterNode.PositionSlotId,
                    HDLitMasterNode.VertexNormalSlotID,
                    HDLitMasterNode.VertexTangentSlotID
                },
                pixelPorts = new List<int>()
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

                // Pass setup
                pragmas = new List<string>()
                {
                    "#pragma target 4.5",
                    "only_renderers d3d11 ps4 xboxone vulkan metal switch",
                    "multi_compile_instancing"
                },
                defines = new List<string>()
                {
                    "SHADOW_LOW",
                    "SKIP_RASTERIZED_SHADOWS",
                    "RAYTRACING_SHADER_GRAPH_LOW",
                    "HAS_LIGHTLOOP",
                },
                keywords = new List<KeywordDescriptor>()
                {
                    HDRPMeshTarget.Keywords.Lightmap,
                    HDRPMeshTarget.Keywords.DirectionalLightmapCombined,
                    HDRPMeshTarget.Keywords.DynamicLightmap,
                    HDRPMeshTarget.Keywords.SurfaceTypeTransparent,
                    HDRPMeshTarget.Keywords.DoubleSided,
                    HDRPMeshTarget.Keywords.BlendMode,
                },
                includes = new List<string>()
                {
                    "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/FragInputs.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPass.cs.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/Raytracing/Shaders/RaytracingMacros.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Material.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/Raytracing/Shaders/ShaderVariablesRaytracing.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/Raytracing/Shaders/ShaderVariablesRaytracingLightLoop.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/Raytracing/Shaders/RaytracingIntersection.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/Lighting.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/LightLoop/LightLoopDef.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Lit/Lit.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Lit/LitRaytracing.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/Raytracing/Shaders/RaytracingLightLoop.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/MaterialUtilities.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/BuiltinUtilities.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/Raytracing/Shaders/RaytracingCommon.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderGraphFunctions.hlsl",
                },

                // Custom template
                passTemplatePath = $"{HDUtils.GetHDRenderPipelinePath()}Editor/Material/Lit/ShaderGraph/HDLitRaytracingPass.template",
                sharedTemplateDirectory = $"{HDUtils.GetHDRenderPipelinePath()}Editor/ShaderGraph",
            };

            public static ShaderPass HDLitGBuffer = new ShaderPass()
            {
                // Definition
                displayName = "GBufferDXR",
                referenceName = "SHADERPASS_RAYTRACING_GBUFFER",
                lightMode = "GBufferDXR",
                passInclude = "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderpassRaytracingGBuffer.hlsl",
                useInPreview = false,

                // Port mask
                vertexPorts = new List<int>()
                {
                    HDLitMasterNode.PositionSlotId,
                    HDLitMasterNode.VertexNormalSlotID,
                    HDLitMasterNode.VertexTangentSlotID
                },
                pixelPorts = new List<int>()
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

                // Pass setup
                pragmas = new List<string>()
                {
                    "#pragma target 4.5",
                    "only_renderers d3d11 ps4 xboxone vulkan metal switch",
                    "multi_compile_instancing"
                },
                defines = new List<string>()
                {
                    "SHADOW_LOW",
                    "RAYTRACING_SHADER_GRAPH_LOW",
                },
                keywords = new List<KeywordDescriptor>()
                {
                    HDRPMeshTarget.Keywords.Lightmap,
                    HDRPMeshTarget.Keywords.DirectionalLightmapCombined,
                    HDRPMeshTarget.Keywords.DynamicLightmap,
                    HDRPMeshTarget.Keywords.SurfaceTypeTransparent,
                    HDRPMeshTarget.Keywords.DoubleSided,
                    HDRPMeshTarget.Keywords.BlendMode,
                },
                includes = new List<string>()
                {
                    "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/FragInputs.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPass.cs.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/Raytracing/Shaders/RaytracingMacros.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Material.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/Raytracing/Shaders/ShaderVariablesRaytracing.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/Raytracing/Shaders/ShaderVariablesRaytracingLightLoop.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/Raytracing/Shaders/Deferred/RaytracingIntersectonGBuffer.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Lit/Lit.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/StandardLit/StandardLit.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/MaterialUtilities.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/BuiltinUtilities.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/Raytracing/Shaders/RaytracingCommon.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderGraphFunctions.hlsl",
                },

                // Custom template
                passTemplatePath = $"{HDUtils.GetHDRenderPipelinePath()}Editor/Material/Lit/ShaderGraph/HDLitRaytracingPass.template",
                sharedTemplateDirectory = $"{HDUtils.GetHDRenderPipelinePath()}Editor/ShaderGraph",
            };
        }
#endregion
    }
}
