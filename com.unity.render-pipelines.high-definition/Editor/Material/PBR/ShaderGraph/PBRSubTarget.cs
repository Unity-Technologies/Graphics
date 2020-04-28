using UnityEngine.Rendering.HighDefinition;
using UnityEditor.ShaderGraph;

namespace UnityEditor.Rendering.HighDefinition.ShaderGraph
{
    sealed class PBRSubTarget : SubTarget<HDTarget>
    {
        const string kAssetGuid = "c01e45594b63bd8419839b581ee0f601";
        static string passTemplatePath => $"{HDUtils.GetHDRenderPipelinePath()}Editor/Material/PBR/ShaderGraph/PBRPass.template";

        public PBRSubTarget()
        {
            displayName = "PBR";
        }

        public override void Setup(ref TargetSetupContext context)
        {
            context.AddAssetDependencyPath(AssetDatabase.GUIDToAssetPath(kAssetGuid));
            context.SetDefaultShaderGUI("Rendering.HighDefinition.HDPBRLitGUI");
            context.AddSubShader(SubShaders.PBR);
        }

#region SubShaders
        static class SubShaders
        {
            public static SubShaderDescriptor PBR = new SubShaderDescriptor()
            {
                pipelineTag = HDRenderPipeline.k_ShaderTagName,
                renderTypeOverride = HDRenderTypeTags.HDLitShader.ToString(),
                generatesPreview = true,
                passes = new PassCollection
                {
                    { PBRPasses.ShadowCaster },
                    { PBRPasses.META },
                    { PBRPasses.SceneSelection },
                    { PBRPasses.DepthOnly, new FieldCondition(Fields.SurfaceOpaque, true) },
                    { PBRPasses.GBuffer, new FieldCondition(Fields.SurfaceOpaque, true) },
                    { PBRPasses.MotionVectors, new FieldCondition(Fields.SurfaceOpaque, true) },
                    { PBRPasses.Forward },
                },
            };
        }
#endregion

#region Passes
        static class PBRPasses
        {
            public static PassDescriptor GBuffer = new PassDescriptor()
            {
                // Definition
                displayName = "GBuffer",
                referenceName = "SHADERPASS_GBUFFER",
                lightMode = "GBuffer",
                useInPreview = true,

                // Template
                passTemplatePath = passTemplatePath,
                sharedTemplateDirectory = HDTarget.sharedTemplateDirectory,

                // Port Mask
                vertexPorts = PBRPortMasks.Vertex,
                pixelPorts = PBRPortMasks.FragmentDefault,

                // Collections
                structs = CoreStructCollections.Default,
                requiredFields = CoreRequiredFields.LitMinimal,
                fieldDependencies = CoreFieldDependencies.Default,
                renderStates = PBRRenderStates.GBuffer,
                pragmas = CorePragmas.DotsInstancedInV2Only,
                keywords = PBRKeywords.GBuffer,
                includes = PBRIncludes.GBuffer,
            };

            public static PassDescriptor META = new PassDescriptor()
            {
                // Definition
                displayName = "META",
                referenceName = "SHADERPASS_LIGHT_TRANSPORT",
                lightMode = "META",
                useInPreview = false,

                // Template
                passTemplatePath = passTemplatePath,
                sharedTemplateDirectory = HDTarget.sharedTemplateDirectory,

                // Port Mask
                pixelPorts = PBRPortMasks.FragmentDefault,

                // Collections
                structs = CoreStructCollections.Default,
                requiredFields = CoreRequiredFields.Meta,
                fieldDependencies = CoreFieldDependencies.Default,
                renderStates = CoreRenderStates.Meta,
                pragmas = CorePragmas.DotsInstancedInV2Only,
                keywords = PBRKeywords.LodFadeCrossfade,
                includes = PBRIncludes.Meta,
            };

            public static PassDescriptor ShadowCaster = new PassDescriptor()
            {
                // Definition
                displayName = "ShadowCaster",
                referenceName = "SHADERPASS_SHADOWS",
                lightMode = "ShadowCaster",
                useInPreview = false,

                // Template
                passTemplatePath = passTemplatePath,
                sharedTemplateDirectory = HDTarget.sharedTemplateDirectory,

                // Port Mask
                vertexPorts = PBRPortMasks.Vertex,
                pixelPorts = PBRPortMasks.FragmentOnlyAlpha,

                // Collections
                structs = CoreStructCollections.Default,
                fieldDependencies = CoreFieldDependencies.Default,
                renderStates = PBRRenderStates.ShadowCaster,
                pragmas = CorePragmas.DotsInstancedInV2Only,
                keywords = PBRKeywords.LodFadeCrossfade,
                includes = PBRIncludes.DepthOnly,
            };

            public static PassDescriptor SceneSelection = new PassDescriptor()
            {
                // Definition
                displayName = "SceneSelectionPass",
                referenceName = "SHADERPASS_DEPTH_ONLY",
                lightMode = "SceneSelectionPass",
                useInPreview = false,

                // Template
                passTemplatePath = passTemplatePath,
                sharedTemplateDirectory = HDTarget.sharedTemplateDirectory,

                // Port Mask
                vertexPorts = PBRPortMasks.Vertex,
                pixelPorts = PBRPortMasks.FragmentOnlyAlpha,

                // Collections
                structs = CoreStructCollections.Default,
                fieldDependencies = CoreFieldDependencies.Default,
                renderStates = PBRRenderStates.SceneSelection,
                pragmas = CorePragmas.DotsInstancedInV2OnlyEditorSync,
                defines = CoreDefines.SceneSelection,
                keywords = PBRKeywords.LodFadeCrossfade,
                includes = PBRIncludes.DepthOnly,
            };

            public static PassDescriptor DepthOnly = new PassDescriptor()
            {
                // Definition
                displayName = "DepthOnly",
                referenceName = "SHADERPASS_DEPTH_ONLY",
                lightMode = "DepthOnly",
                useInPreview = true,

                // Template
                passTemplatePath = passTemplatePath,
                sharedTemplateDirectory = HDTarget.sharedTemplateDirectory,

                // Port Mask
                vertexPorts = PBRPortMasks.Vertex,
                pixelPorts = PBRPortMasks.FragmentDepthMotionVectors,

                // Collections
                structs = CoreStructCollections.Default,
                requiredFields = CoreRequiredFields.LitFull,
                fieldDependencies = CoreFieldDependencies.Default,
                renderStates = PBRRenderStates.DepthOnly,
                pragmas = CorePragmas.DotsInstancedInV2Only,
                defines = CoreDefines.ShaderGraphRaytracingHigh,
                keywords = PBRKeywords.DepthMotionVectors,
                includes = PBRIncludes.DepthOnly,
            };

            public static PassDescriptor MotionVectors = new PassDescriptor()
            {
                // Definition
                displayName = "MotionVectors",
                referenceName = "SHADERPASS_MOTION_VECTORS",
                lightMode = "MotionVectors",
                useInPreview = false,

                // Template
                passTemplatePath = passTemplatePath,
                sharedTemplateDirectory = HDTarget.sharedTemplateDirectory,

                // Port Mask
                vertexPorts = PBRPortMasks.Vertex,
                pixelPorts = PBRPortMasks.FragmentDepthMotionVectors,

                // Fields
                structs = CoreStructCollections.Default,
                requiredFields = CoreRequiredFields.PositionRWS,
                fieldDependencies = CoreFieldDependencies.Default,

                // Conditional State
                renderStates = PBRRenderStates.MotionVectors,
                pragmas = CorePragmas.DotsInstancedInV2Only,
                defines = CoreDefines.ShaderGraphRaytracingHigh,
                keywords = PBRKeywords.DepthMotionVectors,
                includes = PBRIncludes.MotionVectors,
            };

            public static PassDescriptor Forward = new PassDescriptor()
            {
                // Definition
                displayName = "Forward",
                referenceName = "SHADERPASS_FORWARD",
                lightMode = "Forward",
                useInPreview = true,

                // Template
                passTemplatePath = passTemplatePath,
                sharedTemplateDirectory = HDTarget.sharedTemplateDirectory,

                // Port Mask
                vertexPorts = PBRPortMasks.Vertex,
                pixelPorts = PBRPortMasks.FragmentDefault,

                // Collections
                structs = CoreStructCollections.Default,
                requiredFields = CoreRequiredFields.LitMinimal,
                fieldDependencies = CoreFieldDependencies.Default,
                renderStates = PBRRenderStates.Forward,
                pragmas = CorePragmas.DotsInstancedInV2Only,
                defines = CoreDefines.Forward,
                keywords = PBRKeywords.Forward,
                includes = PBRIncludes.Forward,
            };
        }
#endregion

#region PortMasks
        static class PBRPortMasks
        {
            public static int[] Vertex = new int[]
            {
                PBRMasterNode.PositionSlotId,
                PBRMasterNode.VertNormalSlotId,
                PBRMasterNode.VertTangentSlotId,
            };

            public static int[] FragmentDefault = new int[]
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

            public static int[] FragmentOnlyAlpha = new int[]
            {
                PBRMasterNode.AlphaSlotId,
                PBRMasterNode.AlphaThresholdSlotId,
            };

            public static int[] FragmentDepthMotionVectors = new int[]
            {
                PBRMasterNode.NormalSlotId,
                PBRMasterNode.SmoothnessSlotId,
                PBRMasterNode.AlphaSlotId,
                PBRMasterNode.AlphaThresholdSlotId,
            };
        }
#endregion

#region RenderStates
        static class PBRRenderStates
        {
            public static RenderStateCollection GBuffer = new RenderStateCollection
            {
                { RenderState.Cull(Cull.Off), new FieldCondition(Fields.DoubleSided, true) },
                { RenderState.ZTest(CoreRenderStates.Uniforms.zTestGBuffer) },
                { RenderState.Stencil(new StencilDescriptor()
                {
                    WriteMask = $"{ 0 | (int)StencilUsage.RequiresDeferredLighting | (int)StencilUsage.SubsurfaceScattering | (int)StencilUsage.TraceReflectionRay}",
                    Ref = $"{0 | (int)StencilUsage.RequiresDeferredLighting | (int)StencilUsage.TraceReflectionRay}",
                    Comp = "Always",
                    Pass = "Replace",
                }) },
            };

            public static RenderStateCollection ShadowCaster = new RenderStateCollection
            {
                { RenderState.Cull(Cull.Off), new FieldCondition(Fields.DoubleSided, true) },
                { RenderState.Blend(Blend.One, Blend.Zero) },
                { RenderState.ZWrite(ZWrite.On) },
                { RenderState.ColorMask("ColorMask 0") },
            };

            public static RenderStateCollection SceneSelection = new RenderStateCollection
            {
                { RenderState.Cull(Cull.Off), new FieldCondition(Fields.DoubleSided, true) },
                { RenderState.ZWrite(ZWrite.On), new FieldCondition(Fields.SurfaceOpaque, true) },
                { RenderState.ZWrite(ZWrite.Off), new FieldCondition(Fields.SurfaceTransparent, true) },
                { RenderState.ColorMask("ColorMask 0") },
            };
                
            public static RenderStateCollection DepthOnly = new RenderStateCollection
            {
                { RenderState.Cull(Cull.Off), new FieldCondition(Fields.DoubleSided, true) },
                { RenderState.ZWrite(ZWrite.On) },
                { RenderState.Stencil(new StencilDescriptor()
                {
                    WriteMask = $"{ 0 | (int)StencilUsage.TraceReflectionRay}",
                    Ref = $"{0 | (int)StencilUsage.TraceReflectionRay}",
                    Comp = "Always",
                    Pass = "Replace",
                }) },
            };

            public static RenderStateCollection MotionVectors = new RenderStateCollection
            {
                { RenderState.Cull(Cull.Off), new FieldCondition(Fields.DoubleSided, true) },
                { RenderState.Stencil(new StencilDescriptor()
                {
                    WriteMask = $"{0 | (int)StencilUsage.TraceReflectionRay | (int)StencilUsage.ObjectMotionVector}",
                    Ref = $"{ 0 | (int)StencilUsage.TraceReflectionRay | (int)StencilUsage.ObjectMotionVector}",
                    Comp = "Always",
                    Pass = "Replace",
                }) },
            };

            public static RenderStateCollection Forward = new RenderStateCollection
            {
                { RenderState.Cull(Cull.Off), new FieldCondition(Fields.DoubleSided, true) },
                { RenderState.Blend(Blend.One, Blend.Zero, Blend.One, Blend.Zero), new FieldCondition(Fields.SurfaceOpaque, true) },
                { RenderState.Blend(Blend.One, Blend.OneMinusSrcAlpha, Blend.One, Blend.OneMinusSrcAlpha), new FieldCondition[] {
                    new FieldCondition(Fields.SurfaceTransparent, true),
                    new FieldCondition(Fields.BlendAlpha, true) } },
                { RenderState.Blend(Blend.One, Blend.One, Blend.One, Blend.One), new FieldCondition[] {
                    new FieldCondition(Fields.SurfaceTransparent, true),
                    new FieldCondition(Fields.BlendAdd, true) } },
                { RenderState.Blend(Blend.One, Blend.OneMinusSrcAlpha, Blend.One, Blend.OneMinusSrcAlpha), new FieldCondition[] {
                    new FieldCondition(Fields.SurfaceTransparent, true),
                    new FieldCondition(Fields.BlendPremultiply, true) } },
                { RenderState.Blend(Blend.One, Blend.OneMinusSrcAlpha, Blend.One, Blend.OneMinusSrcAlpha), new FieldCondition[] {
                    new FieldCondition(Fields.SurfaceTransparent, true),
                    new FieldCondition(Fields.BlendMultiply, true) } },

                { RenderState.ZTest(ZTest.Equal), new FieldCondition[] {
                    new FieldCondition(Fields.SurfaceOpaque, true),
                    new FieldCondition(Fields.AlphaTest, true) } },
                { RenderState.Stencil(new StencilDescriptor()
                {
                    WriteMask = $"{(int)StencilUsage.RequiresDeferredLighting | (int)StencilUsage.SubsurfaceScattering}",
                    Ref = $"{(int)StencilUsage.Clear}",
                    Comp = "Always",
                    Pass = "Replace",
                }) },
            };
        }
#endregion

#region Keywords
        static class PBRKeywords
        {
            public static KeywordCollection GBuffer = new KeywordCollection
            {
                { CoreKeywordDescriptors.LodFadeCrossfade },
                { CoreKeywordDescriptors.DebugDisplay },
                { CoreKeywords.Lightmaps },
                { CoreKeywordDescriptors.ShadowsShadowmask },
                { CoreKeywordDescriptors.LightLayers },
                { CoreKeywordDescriptors.Decals },
                { CoreKeywordDescriptors.AlphaTest, new FieldCondition(Fields.AlphaTest, true) },
            };

            public static KeywordCollection LodFadeCrossfade = new KeywordCollection
            {
                { CoreKeywordDescriptors.LodFadeCrossfade },
                { CoreKeywordDescriptors.AlphaTest, new FieldCondition(Fields.AlphaTest, true) },
            };

            public static KeywordCollection DepthMotionVectors = new KeywordCollection
            {
                { CoreKeywordDescriptors.WriteMsaaDepth },
                { CoreKeywordDescriptors.WriteNormalBuffer },
                { CoreKeywordDescriptors.LodFadeCrossfade },
                { CoreKeywordDescriptors.AlphaTest, new FieldCondition(Fields.AlphaTest, true) },
            };

            public static KeywordCollection Forward = new KeywordCollection
            {
                { CoreKeywordDescriptors.LodFadeCrossfade },
                { CoreKeywordDescriptors.DebugDisplay },
                { CoreKeywords.Lightmaps },
                { CoreKeywordDescriptors.ShadowsShadowmask },
                { CoreKeywordDescriptors.Decals },
                { CoreKeywordDescriptors.Shadow },
                { CoreKeywordDescriptors.LightList, new FieldCondition(Fields.SurfaceOpaque, true) },
                { CoreKeywordDescriptors.AlphaTest, new FieldCondition(Fields.AlphaTest, true) },
            };
        }
#endregion

#region Includes
        static class PBRIncludes
        {
            // These are duplicated from HDLitSubTarget
            // We avoid moving these to CoreIncludes because this SubTarget will be removed with Stacks

            const string kLitDecalData = "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Lit/LitDecalData.hlsl";
            const string kPassGBuffer = "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassGBuffer.hlsl";

            public static IncludeCollection Common = new IncludeCollection
            {
                { CoreIncludes.CorePregraph },
                { CoreIncludes.kNormalSurfaceGradient, IncludeLocation.Pregraph },
                { CoreIncludes.kLit, IncludeLocation.Pregraph },
                { CoreIncludes.CoreUtility },
                { CoreIncludes.kDecalUtilities, IncludeLocation.Pregraph },
                { kLitDecalData, IncludeLocation.Pregraph },
                { CoreIncludes.kShaderGraphFunctions, IncludeLocation.Pregraph },
            };

            public static IncludeCollection GBuffer = new IncludeCollection
            {
                { Common },
                { kPassGBuffer, IncludeLocation.Postgraph },
            };

            public static IncludeCollection Meta = new IncludeCollection
            {
                { Common },
                { CoreIncludes.kPassLightTransport, IncludeLocation.Postgraph },
            };

            public static IncludeCollection DepthOnly = new IncludeCollection
            {
                { Common },
                { CoreIncludes.kPassDepthOnly, IncludeLocation.Postgraph },
            };

            public static IncludeCollection MotionVectors = new IncludeCollection
            {
                { Common },
                { CoreIncludes.kPassMotionVectors, IncludeLocation.Postgraph },
            };

            public static IncludeCollection Forward = new IncludeCollection
            {
                { CoreIncludes.CorePregraph },
                { CoreIncludes.kNormalSurfaceGradient, IncludeLocation.Pregraph },
                { CoreIncludes.kLighting, IncludeLocation.Pregraph },
                { CoreIncludes.kLightLoopDef, IncludeLocation.Pregraph },
                { CoreIncludes.kLit, IncludeLocation.Pregraph },
                { CoreIncludes.kLightLoop, IncludeLocation.Pregraph },
                { CoreIncludes.CoreUtility },
                { CoreIncludes.kDecalUtilities, IncludeLocation.Pregraph },
                { kLitDecalData, IncludeLocation.Pregraph },
                { CoreIncludes.kShaderGraphFunctions, IncludeLocation.Pregraph },
                { CoreIncludes.kPassForward, IncludeLocation.Postgraph },
            };
        }
#endregion
    }
}
