using System;
using System.Collections.Generic;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEditor.ShaderGraph;
using UnityEditor.ShaderGraph.Internal;
using PassDescriptor = UnityEditor.ShaderGraph.Internal.PassDescriptor;

namespace UnityEditor.Rendering.HighDefinition
{
    class HDRPRaytracingMeshTarget : ITargetImplementation
    {
        public Type targetType => typeof(MeshTarget);
        public string displayName => "HDRP Raytracing";
        public string passTemplatePath => string.Empty;
        public string sharedTemplateDirectory => $"{HDUtils.GetHDRenderPipelinePath()}Editor/ShaderGraph";

        public bool IsValid(IMasterNode masterNode)
        {
            #if ENABLE_RAYTRACING
            return (masterNode is FabricMasterNode ||
                    masterNode is HDLitMasterNode ||
                    masterNode is HDUnlitMasterNode);
            #endif
            return false;
        }

        public void SetupTarget(ref TargetSetupContext context)
        {
            context.AddAssetDependencyPath(AssetDatabase.GUIDToAssetPath("7395c9320da217b42b9059744ceb1de6")); // MeshTarget
            context.AddAssetDependencyPath(AssetDatabase.GUIDToAssetPath("a3b60b90b9eb3e549adfd57a75e77811")); // HDRPRaytracingMeshTarget

            switch(context.masterNode)
            {
                case FabricMasterNode fabricMasterNode:
                    context.SetupSubShader(SubShaders.HDFabric);
                    break;
                case HDLitMasterNode hDLitMasterNode:
                    context.SetupSubShader(SubShaders.HDLit);
                    break;
                case HDUnlitMasterNode hDUnlitMasterNode:
                    context.SetupSubShader(SubShaders.HDUnlit);
                    break;
            }
        }

        static string GetPassTemplatePath(string materialName)
        {
            return $"{HDUtils.GetHDRenderPipelinePath()}Editor/Material/{materialName}/ShaderGraph/{materialName}RaytracingPass.template";
        }

#region SubShaders
        public static class SubShaders
        {
            public static SubShaderDescriptor HDFabric = new SubShaderDescriptor()
            {
                pipelineTag = HDRenderPipeline.k_ShaderTagName,
                passes = new ConditionalPass[]
                {
                    new ConditionalPass(FabricPasses.Indirect, new FieldCondition(DefaultFields.IsPreview, false)),
                    new ConditionalPass(FabricPasses.Visibility, new FieldCondition(DefaultFields.IsPreview, false)),
                    new ConditionalPass(FabricPasses.Forward, new FieldCondition(DefaultFields.IsPreview, false)),
                    new ConditionalPass(FabricPasses.GBuffer, new FieldCondition(DefaultFields.IsPreview, false)),
                },
            };
            public static SubShaderDescriptor HDLit = new SubShaderDescriptor()
            {
                pipelineTag = HDRenderPipeline.k_ShaderTagName,
                passes = new ConditionalPass[]
                {
                    new ConditionalPass(HDLitPasses.Indirect, new FieldCondition(DefaultFields.IsPreview, false)),
                    new ConditionalPass(HDLitPasses.Visibility, new FieldCondition(DefaultFields.IsPreview, false)),
                    new ConditionalPass(HDLitPasses.Forward, new FieldCondition(DefaultFields.IsPreview, false)),
                    new ConditionalPass(HDLitPasses.GBuffer, new FieldCondition(DefaultFields.IsPreview, false)),
                },
            };
            public static SubShaderDescriptor HDUnlit = new SubShaderDescriptor()
            {
                pipelineTag = HDRenderPipeline.k_ShaderTagName,
                passes = new ConditionalPass[]
                {
                    new ConditionalPass(HDUnlitPasses.Indirect, new FieldCondition(DefaultFields.IsPreview, false)),
                    new ConditionalPass(HDUnlitPasses.Visibility, new FieldCondition(DefaultFields.IsPreview, false)),
                    new ConditionalPass(HDUnlitPasses.Forward, new FieldCondition(DefaultFields.IsPreview, false)),
                    new ConditionalPass(HDUnlitPasses.GBuffer, new FieldCondition(DefaultFields.IsPreview, false)),
                },
            };
        }
#endregion

#region HD Lit Passes
        public static class HDLitPasses
        {
            public static PassDescriptor Indirect = new PassDescriptor()
            {
                // Definition
                displayName = "IndirectDXR",
                referenceName = "SHADERPASS_RAYTRACING_INDIRECT",
                lightMode = "IndirectDXR",
                useInPreview = false,

                // Port Mask
                vertexPorts = VertexPorts.HDLit,
                pixelPorts = PixelPorts.HDLit,

                //Fields
                structs = StructDescriptors.Default,
                fieldDependencies = HDRPMeshTarget.FieldDependencies.Default,

                // Pass setup
                pragmas = Pragmas.Instanced,
                defines = Defines.LitForwardIndirect,
                keywords = Keywords.Indirect,
                preGraphIncludes = PreGraphIncludes.Lit,
                postGraphIncludes = PostGraphIncludes.Indirect,

                // Custom Template
                passTemplatePath = GetPassTemplatePath("Lit"),
            };

            public static PassDescriptor Visibility = new PassDescriptor()
            {
                // Definition
                displayName = "VisibilityDXR",
                referenceName = "SHADERPASS_RAYTRACING_VISIBILITY",
                lightMode = "VisibilityDXR",
                useInPreview = false,

                // Port Mask
                vertexPorts = VertexPorts.HDLit,
                pixelPorts = PixelPorts.HDLit,

                //Fields
                structs = StructDescriptors.Default,
                fieldDependencies = HDRPMeshTarget.FieldDependencies.Default,

                // Conditional State
                pragmas = Pragmas.Instanced,
                defines = Defines.LitVisibility,
                preGraphIncludes = PreGraphIncludes.LitVisibility,
                postGraphIncludes = PostGraphIncludes.Visibility,

                // Custom Template
                passTemplatePath = GetPassTemplatePath("Lit"),
            };

            public static PassDescriptor Forward = new PassDescriptor()
            {
                // Definition
                displayName = "ForwardDXR",
                referenceName = "SHADERPASS_RAYTRACING_FORWARD",
                lightMode = "ForwardDXR",
                useInPreview = false,

                // Port Mask
                vertexPorts = VertexPorts.HDLit,
                pixelPorts = PixelPorts.HDLit,

                //Fields
                structs = StructDescriptors.Default,
                fieldDependencies = HDRPMeshTarget.FieldDependencies.Default,

                // Conditional State
                pragmas = Pragmas.Instanced,
                defines = Defines.LitForwardIndirect,
                keywords = Keywords.GBufferForward,
                preGraphIncludes = PreGraphIncludes.Lit,
                postGraphIncludes = PostGraphIncludes.Forward,

                // Custom Template
                passTemplatePath = GetPassTemplatePath("Lit"),
            };

            public static PassDescriptor GBuffer = new PassDescriptor()
            {
                // Definition
                displayName = "GBufferDXR",
                referenceName = "SHADERPASS_RAYTRACING_GBUFFER",
                lightMode = "GBufferDXR",
                useInPreview = false,

                // Port Mask
                vertexPorts = VertexPorts.HDLit,
                pixelPorts = PixelPorts.HDLit,

                //Fields
                structs = StructDescriptors.Default,
                fieldDependencies = HDRPMeshTarget.FieldDependencies.Default,

                // Conditional State
                pragmas = Pragmas.Instanced,
                defines = Defines.LitGBuffer,
                keywords = Keywords.GBufferForward,
                preGraphIncludes = PreGraphIncludes.LitGBuffer,
                postGraphIncludes = PostGraphIncludes.GBuffer,

                // Custom Template
                passTemplatePath = GetPassTemplatePath("Lit"),
            };
        }
#endregion

#region HD Unlit Passes
        public static class HDUnlitPasses
        {
            public static PassDescriptor Indirect = new PassDescriptor()
            {
                // Definition
                displayName = "IndirectDXR",
                referenceName = "SHADERPASS_RAYTRACING_INDIRECT",
                lightMode = "IndirectDXR",  
                useInPreview = false,

                // Port Mask
                vertexPorts = VertexPorts.HDUnlit,
                pixelPorts = PixelPorts.HDUnlit,

                //Fields
                structs = StructDescriptors.Default,
                fieldDependencies = HDRPMeshTarget.FieldDependencies.Default,

                // Conditional State
                pragmas = Pragmas.Basic,
                keywords = Keywords.Basic,
                preGraphIncludes = PreGraphIncludes.Unlit,
                postGraphIncludes = PostGraphIncludes.Indirect,

                // Custom Template
                passTemplatePath = $"{HDUtils.GetHDRenderPipelinePath()}Editor/Material/Unlit/ShaderGraph/HDUnlitRaytracingPass.template",
            };

            public static PassDescriptor Visibility = new PassDescriptor()
            {
                // Definition
                displayName = "VisibilityDXR",
                referenceName = "SHADERPASS_RAYTRACING_VISIBILITY",
                lightMode = "VisibilityDXR",
                useInPreview = false,

                // Port Mask
                vertexPorts = VertexPorts.HDUnlit,
                pixelPorts = PixelPorts.HDUnlit,

                //Fields
                structs = StructDescriptors.Default,
                fieldDependencies = HDRPMeshTarget.FieldDependencies.Default,

                // Conditional State
                pragmas = Pragmas.Basic,
                keywords = Keywords.Basic,
                preGraphIncludes = PreGraphIncludes.Unlit,
                postGraphIncludes = PostGraphIncludes.Visibility,

                // Custom Template
                passTemplatePath = $"{HDUtils.GetHDRenderPipelinePath()}Editor/Material/Unlit/ShaderGraph/HDUnlitRaytracingPass.template",
            };

            public static PassDescriptor Forward = new PassDescriptor()
            {
                // Definition
                displayName = "ForwardDXR",
                referenceName = "SHADERPASS_RAYTRACING_FORWARD",
                lightMode = "ForwardDXR",
                useInPreview = false,

                // Port Mask
                vertexPorts = VertexPorts.HDUnlit,
                pixelPorts = PixelPorts.HDUnlit,

                //Fields
                structs = StructDescriptors.Default,
                fieldDependencies = HDRPMeshTarget.FieldDependencies.Default,

                // Conditional State
                pragmas = Pragmas.Basic,
                keywords = Keywords.Basic,
                preGraphIncludes = PreGraphIncludes.Unlit,
                postGraphIncludes = PostGraphIncludes.Forward,

                // Custom Template
                passTemplatePath = $"{HDUtils.GetHDRenderPipelinePath()}Editor/Material/Unlit/ShaderGraph/HDUnlitRaytracingPass.template",
            };

            public static PassDescriptor GBuffer = new PassDescriptor()
            {
                // Definition
                displayName = "GBufferDXR",
                referenceName = "SHADERPASS_RAYTRACING_GBUFFER",
                lightMode = "GBufferDXR",
                useInPreview = false,

                // Port Mask
                vertexPorts = VertexPorts.HDUnlit,
                pixelPorts = PixelPorts.HDUnlit,

                //Fields
                structs = StructDescriptors.Default,
                fieldDependencies = HDRPMeshTarget.FieldDependencies.Default,

                // Conditional State
                pragmas = Pragmas.Basic,
                keywords = Keywords.Basic,
                preGraphIncludes = PreGraphIncludes.UnlitGBuffer,
                postGraphIncludes = PostGraphIncludes.GBuffer,

                // Custom Template
                passTemplatePath = $"{HDUtils.GetHDRenderPipelinePath()}Editor/Material/Unlit/ShaderGraph/HDUnlitRaytracingPass.template",
            };
        }
#endregion

#region Fabric Passes
        public static class FabricPasses
        {
            public static PassDescriptor Indirect = new PassDescriptor()
            {
                // Definition
                displayName = "IndirectDXR",
                referenceName = "SHADERPASS_RAYTRACING_INDIRECT",
                lightMode = "IndirectDXR",
                useInPreview = false,

                // Port Mask
                vertexPorts = VertexPorts.Fabric,
                pixelPorts = PixelPorts.Fabric,

                //Fields
                structs = StructDescriptors.Default,
                fieldDependencies = HDRPMeshTarget.FieldDependencies.Default,

                // Conditional State
                pragmas = Pragmas.Instanced,
                defines = Defines.FabricForwardIndirect,
                keywords = Keywords.Indirect,
                preGraphIncludes = PreGraphIncludes.Fabric,
                postGraphIncludes = PostGraphIncludes.Indirect,

                // Custom Template
                passTemplatePath = GetPassTemplatePath("Fabric"),
            };

            public static PassDescriptor Visibility = new PassDescriptor()
            {
                // Definition
                displayName = "VisibilityDXR",
                referenceName = "SHADERPASS_RAYTRACING_VISIBILITY",
                lightMode = "VisibilityDXR",
                useInPreview = false,

                // Port Mask
                vertexPorts = VertexPorts.Fabric,
                pixelPorts = PixelPorts.Fabric,

                //Fields
                structs = StructDescriptors.Default,
                fieldDependencies = HDRPMeshTarget.FieldDependencies.Default,

                // Conditional State
                pragmas = Pragmas.Instanced,
                keywords = Keywords.Basic,
                preGraphIncludes = PreGraphIncludes.FabricVisibility,
                postGraphIncludes = PostGraphIncludes.Visibility,

                // Custom Template
                passTemplatePath = GetPassTemplatePath("Fabric"),
            };

            public static PassDescriptor Forward = new PassDescriptor()
            {
                // Definition
                displayName = "ForwardDXR",
                referenceName = "SHADERPASS_RAYTRACING_FORWARD",
                lightMode = "ForwardDXR",
                useInPreview = false,

                // Port Mask
                vertexPorts = VertexPorts.Fabric,
                pixelPorts = PixelPorts.Fabric,

                //Fields
                structs = StructDescriptors.Default,
                fieldDependencies = HDRPMeshTarget.FieldDependencies.Default,

                // Conditional State
                pragmas = Pragmas.Instanced,
                defines = Defines.FabricForwardIndirect,
                keywords = Keywords.GBufferForward,
                preGraphIncludes = PreGraphIncludes.Fabric,
                postGraphIncludes = PostGraphIncludes.Forward,

                // Custom Template
                passTemplatePath = GetPassTemplatePath("Fabric"),
            };

            public static PassDescriptor GBuffer = new PassDescriptor()
            {
                // Definition
                displayName = "GBufferDXR",
                referenceName = "SHADERPASS_RAYTRACING_GBUFFER",
                lightMode = "GBufferDXR",
                useInPreview = false,

                // Port Mask
                vertexPorts = VertexPorts.Fabric,
                pixelPorts = PixelPorts.Fabric,

                //Fields
                structs = StructDescriptors.Default,
                fieldDependencies = HDRPMeshTarget.FieldDependencies.Default,

                // Conditional State
                pragmas = Pragmas.Instanced,
                defines = Defines.FabricGBuffer,
                keywords = Keywords.GBufferForward,
                preGraphIncludes = PreGraphIncludes.FabricGBuffer,
                postGraphIncludes = PostGraphIncludes.GBuffer,

                // Custom Template
                passTemplatePath = GetPassTemplatePath("Fabric"),
            };
        }
#endregion

#region PortMasks
        static class VertexPorts
        {
            public static int[] HDLit = new int[]
            {
                HDLitMasterNode.PositionSlotId,
                HDLitMasterNode.VertexNormalSlotID,
                HDLitMasterNode.VertexTangentSlotID,
            };

            public static int[] HDUnlit = new int[]
            {
                HDUnlitMasterNode.PositionSlotId,
                HDUnlitMasterNode.VertexNormalSlotId,
                HDUnlitMasterNode.VertexTangentSlotId,
            };

            public static int[] Fabric = new int[]
            {
                FabricMasterNode.PositionSlotId,
                FabricMasterNode.VertexNormalSlotId,
                FabricMasterNode.VertexTangentSlotId,
            };
        }

        static class PixelPorts
        {
            public static int[] HDLit = new int[]
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

            public static int[] HDUnlit = new int[]
            {
                HDUnlitMasterNode.ColorSlotId,
                HDUnlitMasterNode.AlphaSlotId,
                HDUnlitMasterNode.AlphaThresholdSlotId,
                HDUnlitMasterNode.EmissionSlotId,
            };

            public static int[] Fabric = new int[]
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
        }
#endregion

#region StructDescriptors
        static class StructDescriptors
        {
            public static StructDescriptor[] Default = new StructDescriptor[]
            {
                HDRPMeshTarget.AttributesMesh,
                HDRPMeshTarget.VaryingsMeshToPS,
                HDRPMeshTarget.SurfaceDescriptionInputs,
                HDRPMeshTarget.VertexDescriptionInputs,
            };
        }
#endregion

#region Pragmas
        public static class Pragmas
        {
            public static ConditionalPragma[] Basic = new ConditionalPragma[]
            {
                new ConditionalPragma(Pragma.Target(4.5)),
                new ConditionalPragma(Pragma.Custom("raytracing test")),
                new ConditionalPragma(Pragma.OnlyRenderers(new Platform[] {Platform.D3D11, Platform.PS4, Platform.XboxOne, Platform.Vulkan, Platform.Metal, Platform.Switch})),
            };

            public static ConditionalPragma[] Instanced = new ConditionalPragma[]
            {
                new ConditionalPragma(Pragma.Target(4.5)),
                new ConditionalPragma(Pragma.Custom("raytracing test")),
                new ConditionalPragma(Pragma.OnlyRenderers(new Platform[] {Platform.D3D11, Platform.PS4, Platform.XboxOne, Platform.Vulkan, Platform.Metal, Platform.Switch})),
                new ConditionalPragma(Pragma.MultiCompileInstancing),
            };
        }
#endregion

#region Defines
        static class Defines
        {
            public static ConditionalDefine[] LitForwardIndirect = new ConditionalDefine[]
            {
                new ConditionalDefine(HDRPMeshTarget.KeywordDescriptors.Shadow, 0),
                new ConditionalDefine(KeywordDescriptors.SkipRasterizedShadows, 1),
                new ConditionalDefine(RayTracingNode.GetRayTracingKeyword(), 1),
                new ConditionalDefine(HDRPMeshTarget.KeywordDescriptors.HasLightloop, 1),
            };

            public static ConditionalDefine[] LitGBuffer = new ConditionalDefine[]
            {
                new ConditionalDefine(HDRPMeshTarget.KeywordDescriptors.Shadow, 0),
                new ConditionalDefine(RayTracingNode.GetRayTracingKeyword(), 1),
            };

            public static ConditionalDefine[] LitVisibility = new ConditionalDefine[]
            {
                new ConditionalDefine(RayTracingNode.GetRayTracingKeyword(), 1),
            };

            public static ConditionalDefine[] FabricForwardIndirect = new ConditionalDefine[]
            {
                new ConditionalDefine(HDRPMeshTarget.KeywordDescriptors.Shadow, 0),
                new ConditionalDefine(HDRPMeshTarget.KeywordDescriptors.HasLightloop, 1),
            };

            public static ConditionalDefine[] FabricGBuffer = new ConditionalDefine[]
            {
                new ConditionalDefine(HDRPMeshTarget.KeywordDescriptors.Shadow, 0),
            };
        }
#endregion

#region Keywords
        static class Keywords
        {
            public static ConditionalKeyword[] Basic = new ConditionalKeyword[]
            {
                new ConditionalKeyword(HDRPMeshTarget.KeywordDescriptors.SurfaceTypeTransparent),
                new ConditionalKeyword(HDRPMeshTarget.KeywordDescriptors.DoubleSided),
                new ConditionalKeyword(HDRPMeshTarget.KeywordDescriptors.BlendMode),
            };

            public static ConditionalKeyword[] Indirect = new ConditionalKeyword[]
            {
                new ConditionalKeyword(HDRPMeshTarget.KeywordDescriptors.SurfaceTypeTransparent),
                new ConditionalKeyword(HDRPMeshTarget.KeywordDescriptors.DoubleSided),
                new ConditionalKeyword(HDRPMeshTarget.KeywordDescriptors.BlendMode),
                new ConditionalKeyword(HDRPMeshTarget.KeywordDescriptors.DiffuseLightingOnly),
                new ConditionalKeyword(HDRPMeshTarget.KeywordDescriptors.Lightmap),
                new ConditionalKeyword(HDRPMeshTarget.KeywordDescriptors.DirectionalLightmapCombined),
                new ConditionalKeyword(HDRPMeshTarget.KeywordDescriptors.DynamicLightmap),
            };

            public static ConditionalKeyword[] GBufferForward = new ConditionalKeyword[]
            {
                new ConditionalKeyword(HDRPMeshTarget.KeywordDescriptors.SurfaceTypeTransparent),
                new ConditionalKeyword(HDRPMeshTarget.KeywordDescriptors.DoubleSided),
                new ConditionalKeyword(HDRPMeshTarget.KeywordDescriptors.BlendMode),
                new ConditionalKeyword(HDRPMeshTarget.KeywordDescriptors.Lightmap),
                new ConditionalKeyword(HDRPMeshTarget.KeywordDescriptors.DirectionalLightmapCombined),
                new ConditionalKeyword(HDRPMeshTarget.KeywordDescriptors.DynamicLightmap),
            };
        }
#endregion

#region Includes
        static class PreGraphIncludes
        {
            public static ConditionalInclude[] Lit = new ConditionalInclude[]
            {
                new ConditionalInclude(IncludeDescriptor.File("Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl")),
                new ConditionalInclude(IncludeDescriptor.File("Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/FragInputs.hlsl")),
                new ConditionalInclude(IncludeDescriptor.File("Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPass.cs.hlsl")),
                new ConditionalInclude(IncludeDescriptor.File("Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/Raytracing/Shaders/RaytracingMacros.hlsl")),
                new ConditionalInclude(IncludeDescriptor.File("Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl")),
                new ConditionalInclude(IncludeDescriptor.File("Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Material.hlsl")),
                new ConditionalInclude(IncludeDescriptor.File("Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/Raytracing/Shaders/ShaderVariablesRaytracing.hlsl")),
                new ConditionalInclude(IncludeDescriptor.File("Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/Raytracing/Shaders/ShaderVariablesRaytracingLightLoop.hlsl")),
                new ConditionalInclude(IncludeDescriptor.File("Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/Raytracing/Shaders/RaytracingIntersection.hlsl")),
                new ConditionalInclude(IncludeDescriptor.File("Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/Lighting.hlsl")),
                new ConditionalInclude(IncludeDescriptor.File("Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/LightLoop/LightLoopDef.hlsl")),
                new ConditionalInclude(IncludeDescriptor.File("Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Lit/Lit.hlsl")),
                new ConditionalInclude(IncludeDescriptor.File("Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Lit/LitRaytracing.hlsl")),
                new ConditionalInclude(IncludeDescriptor.File("Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/Raytracing/Shaders/RaytracingLightLoop.hlsl")),
                new ConditionalInclude(IncludeDescriptor.File("Packages/com.unity.render-pipelines.high-definition/Runtime/Material/MaterialUtilities.hlsl")),
                new ConditionalInclude(IncludeDescriptor.File("Packages/com.unity.render-pipelines.high-definition/Runtime/Material/BuiltinUtilities.hlsl")),
                new ConditionalInclude(IncludeDescriptor.File("Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/Raytracing/Shaders/RaytracingCommon.hlsl")),
                new ConditionalInclude(IncludeDescriptor.File("Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderGraphFunctions.hlsl")),
            };

            public static ConditionalInclude[] LitVisibility = new ConditionalInclude[]
            {
                new ConditionalInclude(IncludeDescriptor.File("Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl")),
                new ConditionalInclude(IncludeDescriptor.File("Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/FragInputs.hlsl")),
                new ConditionalInclude(IncludeDescriptor.File("Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPass.cs.hlsl")),
                new ConditionalInclude(IncludeDescriptor.File("Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/Raytracing/Shaders/RaytracingMacros.hlsl")),
                new ConditionalInclude(IncludeDescriptor.File("Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl")),
                new ConditionalInclude(IncludeDescriptor.File("Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Material.hlsl")),
                new ConditionalInclude(IncludeDescriptor.File("Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/Raytracing/Shaders/ShaderVariablesRaytracing.hlsl")),
                new ConditionalInclude(IncludeDescriptor.File("Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/Raytracing/Shaders/ShaderVariablesRaytracingLightLoop.hlsl")),
                new ConditionalInclude(IncludeDescriptor.File("Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/Raytracing/Shaders/RaytracingIntersection.hlsl")),
                new ConditionalInclude(IncludeDescriptor.File("Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Lit/Lit.hlsl")),
                new ConditionalInclude(IncludeDescriptor.File("Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Lit/LitRaytracing.hlsl")),
                new ConditionalInclude(IncludeDescriptor.File("Packages/com.unity.render-pipelines.high-definition/Runtime/Material/MaterialUtilities.hlsl")),
                new ConditionalInclude(IncludeDescriptor.File("Packages/com.unity.render-pipelines.high-definition/Runtime/Material/BuiltinUtilities.hlsl")),
                new ConditionalInclude(IncludeDescriptor.File("Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/Raytracing/Shaders/RaytracingCommon.hlsl")),
                new ConditionalInclude(IncludeDescriptor.File("Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderGraphFunctions.hlsl")),
            };

            public static ConditionalInclude[] LitGBuffer = new ConditionalInclude[]
            {
                new ConditionalInclude(IncludeDescriptor.File("Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl")),
                new ConditionalInclude(IncludeDescriptor.File("Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/FragInputs.hlsl")),
                new ConditionalInclude(IncludeDescriptor.File("Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPass.cs.hlsl")),
                new ConditionalInclude(IncludeDescriptor.File("Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/Raytracing/Shaders/RaytracingMacros.hlsl")),
                new ConditionalInclude(IncludeDescriptor.File("Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl")),
                new ConditionalInclude(IncludeDescriptor.File("Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Material.hlsl")),
                new ConditionalInclude(IncludeDescriptor.File("Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/Raytracing/Shaders/ShaderVariablesRaytracing.hlsl")),
                new ConditionalInclude(IncludeDescriptor.File("Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/Raytracing/Shaders/ShaderVariablesRaytracingLightLoop.hlsl")),
                new ConditionalInclude(IncludeDescriptor.File("Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/Raytracing/Shaders/Deferred/RaytracingIntersectonGBuffer.hlsl")),
                new ConditionalInclude(IncludeDescriptor.File("Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Lit/Lit.hlsl")),
                new ConditionalInclude(IncludeDescriptor.File("Packages/com.unity.render-pipelines.high-definition/Runtime/Material/StandardLit/StandardLit.hlsl")),
                new ConditionalInclude(IncludeDescriptor.File("Packages/com.unity.render-pipelines.high-definition/Runtime/Material/MaterialUtilities.hlsl")),
                new ConditionalInclude(IncludeDescriptor.File("Packages/com.unity.render-pipelines.high-definition/Runtime/Material/BuiltinUtilities.hlsl")),
                new ConditionalInclude(IncludeDescriptor.File("Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/Raytracing/Shaders/RaytracingCommon.hlsl")),
                new ConditionalInclude(IncludeDescriptor.File("Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderGraphFunctions.hlsl")),
            };

            public static ConditionalInclude[] Unlit = new ConditionalInclude[]
            {
                new ConditionalInclude(IncludeDescriptor.File("Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl")),
                new ConditionalInclude(IncludeDescriptor.File("Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/FragInputs.hlsl")),
                new ConditionalInclude(IncludeDescriptor.File("Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPass.cs.hlsl")),
                new ConditionalInclude(IncludeDescriptor.File("Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/Raytracing/Shaders/RaytracingMacros.hlsl")),
                new ConditionalInclude(IncludeDescriptor.File("Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl")),
                new ConditionalInclude(IncludeDescriptor.File("Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Material.hlsl")),
                new ConditionalInclude(IncludeDescriptor.File("Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/Raytracing/Shaders/ShaderVariablesRaytracing.hlsl")),
                new ConditionalInclude(IncludeDescriptor.File("Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/Raytracing/Shaders/ShaderVariablesRaytracingLightLoop.hlsl")),
                new ConditionalInclude(IncludeDescriptor.File("Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/Raytracing/Shaders/RaytracingIntersection.hlsl")),
                new ConditionalInclude(IncludeDescriptor.File("Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Unlit/Unlit.hlsl")),
                new ConditionalInclude(IncludeDescriptor.File("Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Unlit/UnlitRaytracing.hlsl")),
                new ConditionalInclude(IncludeDescriptor.File("Packages/com.unity.render-pipelines.high-definition/Runtime/Material/MaterialUtilities.hlsl")),
                new ConditionalInclude(IncludeDescriptor.File("Packages/com.unity.render-pipelines.high-definition/Runtime/Material/BuiltinUtilities.hlsl")),
                new ConditionalInclude(IncludeDescriptor.File("Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/Raytracing/Shaders/RaytracingCommon.hlsl")),
                new ConditionalInclude(IncludeDescriptor.File("Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderGraphFunctions.hlsl")),
            };

            public static ConditionalInclude[] UnlitGBuffer = new ConditionalInclude[]
            {
                new ConditionalInclude(IncludeDescriptor.File("Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl")),
                new ConditionalInclude(IncludeDescriptor.File("Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/FragInputs.hlsl")),
                new ConditionalInclude(IncludeDescriptor.File("Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPass.cs.hlsl")),
                new ConditionalInclude(IncludeDescriptor.File("Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/Raytracing/Shaders/RaytracingMacros.hlsl")),
                new ConditionalInclude(IncludeDescriptor.File("Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl")),
                new ConditionalInclude(IncludeDescriptor.File("Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Material.hlsl")),
                new ConditionalInclude(IncludeDescriptor.File("Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/Raytracing/Shaders/ShaderVariablesRaytracing.hlsl")),
                new ConditionalInclude(IncludeDescriptor.File("Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/Raytracing/Shaders/ShaderVariablesRaytracingLightLoop.hlsl")),
                new ConditionalInclude(IncludeDescriptor.File("Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/Raytracing/Shaders/Deferred/RaytracingIntersectonGBuffer.hlsl")),
                new ConditionalInclude(IncludeDescriptor.File("Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Unlit/Unlit.hlsl")),
                new ConditionalInclude(IncludeDescriptor.File("Packages/com.unity.render-pipelines.high-definition/Runtime/Material/NormalBuffer.hlsl")),
                new ConditionalInclude(IncludeDescriptor.File("Packages/com.unity.render-pipelines.high-definition/Runtime/Material/StandardLit/StandardLit.hlsl")),
                new ConditionalInclude(IncludeDescriptor.File("Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Unlit/UnlitRaytracing.hlsl")),
                new ConditionalInclude(IncludeDescriptor.File("Packages/com.unity.render-pipelines.high-definition/Runtime/Material/MaterialUtilities.hlsl")),
                new ConditionalInclude(IncludeDescriptor.File("Packages/com.unity.render-pipelines.high-definition/Runtime/Material/BuiltinUtilities.hlsl")),
                new ConditionalInclude(IncludeDescriptor.File("Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/Raytracing/Shaders/RaytracingCommon.hlsl")),
                new ConditionalInclude(IncludeDescriptor.File("Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderGraphFunctions.hlsl")),
            };

            public static ConditionalInclude[] Fabric = new ConditionalInclude[]
            {
                new ConditionalInclude(IncludeDescriptor.File("Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl")),
                new ConditionalInclude(IncludeDescriptor.File("Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/FragInputs.hlsl")),
                new ConditionalInclude(IncludeDescriptor.File("Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPass.cs.hlsl")),
                new ConditionalInclude(IncludeDescriptor.File("Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/Raytracing/Shaders/RaytracingMacros.hlsl")),
                new ConditionalInclude(IncludeDescriptor.File("Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl")),
                new ConditionalInclude(IncludeDescriptor.File("Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Material.hlsl")),
                new ConditionalInclude(IncludeDescriptor.File("Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/Raytracing/Shaders/ShaderVariablesRaytracing.hlsl")),
                new ConditionalInclude(IncludeDescriptor.File("Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/Raytracing/Shaders/ShaderVariablesRaytracingLightLoop.hlsl")),
                new ConditionalInclude(IncludeDescriptor.File("Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/Raytracing/Shaders/RaytracingIntersection.hlsl")),
                new ConditionalInclude(IncludeDescriptor.File("Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/Lighting.hlsl")),
                new ConditionalInclude(IncludeDescriptor.File("Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/LightLoop/LightLoopDef.hlsl")),
                new ConditionalInclude(IncludeDescriptor.File("Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Fabric/Fabric.hlsl")),
                new ConditionalInclude(IncludeDescriptor.File("Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Fabric/FabricRaytracing.hlsl")),
                new ConditionalInclude(IncludeDescriptor.File("Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/Raytracing/Shaders/RaytracingLightLoop.hlsl")),
                new ConditionalInclude(IncludeDescriptor.File("Packages/com.unity.render-pipelines.high-definition/Runtime/Material/MaterialUtilities.hlsl")),
                new ConditionalInclude(IncludeDescriptor.File("Packages/com.unity.render-pipelines.high-definition/Runtime/Material/BuiltinUtilities.hlsl")),
                new ConditionalInclude(IncludeDescriptor.File("Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/Raytracing/Shaders/RaytracingCommon.hlsl")),
                new ConditionalInclude(IncludeDescriptor.File("Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderGraphFunctions.hlsl")),
            };

            public static ConditionalInclude[] FabricVisibility = new ConditionalInclude[]
            {
                new ConditionalInclude(IncludeDescriptor.File("Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl")),
                new ConditionalInclude(IncludeDescriptor.File("Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/FragInputs.hlsl")),
                new ConditionalInclude(IncludeDescriptor.File("Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPass.cs.hlsl")),
                new ConditionalInclude(IncludeDescriptor.File("Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/Raytracing/Shaders/RaytracingMacros.hlsl")),
                new ConditionalInclude(IncludeDescriptor.File("Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl")),
                new ConditionalInclude(IncludeDescriptor.File("Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Material.hlsl")),
                new ConditionalInclude(IncludeDescriptor.File("Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/Raytracing/Shaders/ShaderVariablesRaytracing.hlsl")),
                new ConditionalInclude(IncludeDescriptor.File("Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/Raytracing/Shaders/ShaderVariablesRaytracingLightLoop.hlsl")),
                new ConditionalInclude(IncludeDescriptor.File("Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/Raytracing/Shaders/RaytracingIntersection.hlsl")),
                new ConditionalInclude(IncludeDescriptor.File("Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Fabric/Fabric.hlsl")),
                new ConditionalInclude(IncludeDescriptor.File("Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Fabric/FabricRaytracing.hlsl")),
                new ConditionalInclude(IncludeDescriptor.File("Packages/com.unity.render-pipelines.high-definition/Runtime/Material/MaterialUtilities.hlsl")),
                new ConditionalInclude(IncludeDescriptor.File("Packages/com.unity.render-pipelines.high-definition/Runtime/Material/BuiltinUtilities.hlsl")),
                new ConditionalInclude(IncludeDescriptor.File("Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/Raytracing/Shaders/RaytracingCommon.hlsl")),
                new ConditionalInclude(IncludeDescriptor.File("Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderGraphFunctions.hlsl")),
            };

            public static ConditionalInclude[] FabricGBuffer = new ConditionalInclude[]
            {
                new ConditionalInclude(IncludeDescriptor.File("Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl")),
                new ConditionalInclude(IncludeDescriptor.File("Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/FragInputs.hlsl")),
                new ConditionalInclude(IncludeDescriptor.File("Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPass.cs.hlsl")),
                new ConditionalInclude(IncludeDescriptor.File("Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/Raytracing/Shaders/RaytracingMacros.hlsl")),
                new ConditionalInclude(IncludeDescriptor.File("Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl")),
                new ConditionalInclude(IncludeDescriptor.File("Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Material.hlsl")),
                new ConditionalInclude(IncludeDescriptor.File("Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/Raytracing/Shaders/ShaderVariablesRaytracing.hlsl")),
                new ConditionalInclude(IncludeDescriptor.File("Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/Raytracing/Shaders/ShaderVariablesRaytracingLightLoop.hlsl")),
                new ConditionalInclude(IncludeDescriptor.File("Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/Raytracing/Shaders/Deferred/RaytracingIntersectonGBuffer.hlsl")),
                new ConditionalInclude(IncludeDescriptor.File("Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Fabric/Fabric.hlsl")),
                new ConditionalInclude(IncludeDescriptor.File("Packages/com.unity.render-pipelines.high-definition/Runtime/Material/StandardLit/StandardLit.hlsl")),
                new ConditionalInclude(IncludeDescriptor.File("Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Fabric/FabricRaytracing.hlsl")),
                new ConditionalInclude(IncludeDescriptor.File("Packages/com.unity.render-pipelines.high-definition/Runtime/Material/MaterialUtilities.hlsl")),
                new ConditionalInclude(IncludeDescriptor.File("Packages/com.unity.render-pipelines.high-definition/Runtime/Material/BuiltinUtilities.hlsl")),
                new ConditionalInclude(IncludeDescriptor.File("Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/Raytracing/Shaders/RaytracingCommon.hlsl")),
                new ConditionalInclude(IncludeDescriptor.File("Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderGraphFunctions.hlsl")),
            };
        }

        static class PostGraphIncludes
        {
            public static ConditionalInclude[] GBuffer = new ConditionalInclude[]
            {
                new ConditionalInclude(IncludeDescriptor.File("Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderpassRaytracingGBuffer.hlsl")),
            };
            public static ConditionalInclude[] Forward = new ConditionalInclude[]
            {
                new ConditionalInclude(IncludeDescriptor.File("Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassRaytracingForward.hlsl")),
            };
            public static ConditionalInclude[] Visibility = new ConditionalInclude[]
            {
                new ConditionalInclude(IncludeDescriptor.File("Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassRaytracingVisibility.hlsl")),
            };
            public static ConditionalInclude[] Indirect = new ConditionalInclude[]
            {
                new ConditionalInclude(IncludeDescriptor.File("Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassRaytracingIndirect.hlsl")),
            };
        }
#endregion

#region KeywordDescriptors
        public static class KeywordDescriptors
        {
            public static KeywordDescriptor SkipRasterizedShadows = new KeywordDescriptor()
            {
                displayName = "Skip Rasterized Shadows",
                referenceName = "SKIP_RASTERIZED_SHADOWS",
                type = KeywordType.Boolean,
                definition = KeywordDefinition.MultiCompile,
                scope = KeywordScope.Global,
            };
        }
#endregion
    }
}
