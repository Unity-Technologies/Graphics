using UnityEngine.Rendering.HighDefinition;
using UnityEditor.ShaderGraph;

namespace UnityEditor.Rendering.HighDefinition.ShaderGraph
{
    sealed class HDUnlitSubTarget : SubTarget<HDTarget>
    {
        const string kAssetGuid = "4516595d40fa52047a77940183dc8e74";

        // Why do the raytracing passes use the template for the pipeline agnostic Unlit master node?
        // This should be resolved so we can delete the second pass template
        static string passTemplatePath => $"{HDUtils.GetHDRenderPipelinePath()}Editor/Material/Unlit/ShaderGraph/HDUnlitPass.template";
        static string raytracingPassTemplatePath => $"{HDUtils.GetHDRenderPipelinePath()}Editor/Material/Unlit/ShaderGraph/UnlitPass.template";

        public HDUnlitSubTarget()
        {
            displayName = "Unlit";
        }

        public override void Setup(ref TargetSetupContext context)
        {
            context.AddAssetDependencyPath(AssetDatabase.GUIDToAssetPath(kAssetGuid));
            context.SetDefaultShaderGUI("Rendering.HighDefinition.HDUnlitGUI");
            context.AddSubShader(SubShaders.Unlit);
            context.AddSubShader(SubShaders.UnlitRaytracing);
        }

#region SubShaders
        static class SubShaders
        {
            public static SubShaderDescriptor Unlit = new SubShaderDescriptor()
            {
                pipelineTag = HDRenderPipeline.k_ShaderTagName,
                generatesPreview = true,
                passes = new PassCollection
                {
                    { UnlitPasses.ShadowCaster },
                    { UnlitPasses.META },
                    { UnlitPasses.SceneSelection },
                    { UnlitPasses.DepthForwardOnly },
                    { UnlitPasses.MotionVectors },
                    { UnlitPasses.Distortion, new FieldCondition(HDFields.TransparentDistortion, true) },
                    { UnlitPasses.ForwardOnly },
                },
            };

            public static SubShaderDescriptor UnlitRaytracing = new SubShaderDescriptor()
            {
                pipelineTag = HDRenderPipeline.k_ShaderTagName,
                generatesPreview = false,
                passes = new PassCollection
                {
                    { UnlitPasses.RaytracingIndirect, new FieldCondition(Fields.IsPreview, false) },
                    { UnlitPasses.RaytracingVisibility, new FieldCondition(Fields.IsPreview, false) },
                    { UnlitPasses.RaytracingForward, new FieldCondition(Fields.IsPreview, false) },
                    { UnlitPasses.RaytracingGBuffer, new FieldCondition(Fields.IsPreview, false) },
                    { UnlitPasses.RaytracingPathTracing, new FieldCondition(Fields.IsPreview, false) },
                },
            };
        }
#endregion

#region Passes
        static class UnlitPasses
        {
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
                pixelPorts = UnlitPortMasks.FragmentDefault,

                // Collections
                structs = CoreStructCollections.Default,
                requiredFields = new FieldCollection(){ CoreRequiredFields.Meta, HDFields.SubShader.Unlit },
                fieldDependencies = CoreFieldDependencies.Default,
                renderStates = CoreRenderStates.Meta,
                pragmas = CorePragmas.DotsInstancedInV2Only,
                keywords = CoreKeywords.HDBase,
                includes = UnlitIncludes.Meta,
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
                vertexPorts = UnlitPortMasks.Vertex,
                pixelPorts = UnlitPortMasks.FragmentOnlyAlpha,

                // Collections
                structs = CoreStructCollections.Default,
                requiredFields = new FieldCollection(){ HDFields.SubShader.Unlit },
                fieldDependencies = CoreFieldDependencies.Default,
                renderStates = CoreRenderStates.ShadowCaster,
                pragmas = CorePragmas.DotsInstancedInV2Only,
                keywords = CoreKeywords.HDBase,
                includes = UnlitIncludes.DepthOnly,
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
                vertexPorts = UnlitPortMasks.Vertex,
                pixelPorts = UnlitPortMasks.FragmentOnlyAlpha,

                // Collections
                structs = CoreStructCollections.Default,
                requiredFields = new FieldCollection(){ HDFields.SubShader.Unlit },
                fieldDependencies = CoreFieldDependencies.Default,
                renderStates = UnlitRenderStates.SceneSelection,
                pragmas = CorePragmas.DotsInstancedInV2OnlyEditorSync,
                defines = CoreDefines.SceneSelection,
                keywords = CoreKeywords.HDBase,
                includes = UnlitIncludes.DepthOnly,
            };

            public static PassDescriptor DepthForwardOnly = new PassDescriptor()
            {
                // Definition
                displayName = "DepthForwardOnly",
                referenceName = "SHADERPASS_DEPTH_ONLY",
                lightMode = "DepthForwardOnly",
                useInPreview = true,

                // Template
                passTemplatePath = passTemplatePath,
                sharedTemplateDirectory = HDTarget.sharedTemplateDirectory,

                // Port Mask
                vertexPorts = UnlitPortMasks.Vertex,
                pixelPorts = UnlitPortMasks.FragmentOnlyAlpha,

                // Collections
                structs = CoreStructCollections.Default,
                requiredFields = new FieldCollection(){ HDFields.SubShader.Unlit },
                fieldDependencies = CoreFieldDependencies.Default,
                renderStates = UnlitRenderStates.DepthForwardOnly,
                pragmas = CorePragmas.DotsInstancedInV2Only,
                keywords = UnlitKeywords.DepthMotionVectors,
                includes = UnlitIncludes.DepthOnly,
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
                vertexPorts = UnlitPortMasks.Vertex,
                pixelPorts = UnlitPortMasks.FragmentOnlyAlpha,

                // Collections
                structs = CoreStructCollections.Default,
                requiredFields = new FieldCollection(){ CoreRequiredFields.PositionRWS, HDFields.SubShader.Unlit },
                fieldDependencies = CoreFieldDependencies.Default,
                renderStates = UnlitRenderStates.MotionVectors,
                pragmas = CorePragmas.DotsInstancedInV2Only,
                keywords = UnlitKeywords.DepthMotionVectors,
                includes = UnlitIncludes.MotionVectors,
            };

            public static PassDescriptor Distortion = new PassDescriptor()
            {
                // Definition
                displayName = "DistortionVectors",
                referenceName = "SHADERPASS_DISTORTION",
                lightMode = "DistortionVectors",
                useInPreview = true,

                // Template
                passTemplatePath = passTemplatePath,
                sharedTemplateDirectory = HDTarget.sharedTemplateDirectory,

                // Port Mask
                vertexPorts = UnlitPortMasks.Vertex,
                pixelPorts = UnlitPortMasks.FragmentDistortion,

                // Collections
                structs = CoreStructCollections.Default,
                requiredFields = new FieldCollection(){ HDFields.SubShader.Unlit },
                fieldDependencies = CoreFieldDependencies.Default,
                renderStates = UnlitRenderStates.Distortion,
                pragmas = CorePragmas.DotsInstancedInV2Only,
                keywords = CoreKeywords.HDBase,
                includes = UnlitIncludes.Distortion,
            };

            public static PassDescriptor ForwardOnly = new PassDescriptor()
            {
                // Definition
                displayName = "ForwardOnly",
                referenceName = "SHADERPASS_FORWARD_UNLIT",
                lightMode = "ForwardOnly",
                useInPreview = true,

                // Template
                passTemplatePath = passTemplatePath,
                sharedTemplateDirectory = HDTarget.sharedTemplateDirectory,

                // Port Mask
                vertexPorts = UnlitPortMasks.Vertex,
                pixelPorts = UnlitPortMasks.FragmentForward,

                // Collections
                structs = CoreStructCollections.Default,
                requiredFields = new FieldCollection(){ HDFields.SubShader.Unlit },
                fieldDependencies = CoreFieldDependencies.Default,
                renderStates = CoreRenderStates.Forward,
                pragmas = CorePragmas.DotsInstancedInV2Only,
                keywords = UnlitKeywords.Forward,
                includes = UnlitIncludes.ForwardOnly,

                virtualTextureFeedback = true,
            };

            public static PassDescriptor RaytracingIndirect = new PassDescriptor()
            {
                // Definition
                displayName = "IndirectDXR",
                referenceName = "SHADERPASS_RAYTRACING_INDIRECT",
                lightMode = "IndirectDXR",
                useInPreview = false,

                // Template
                passTemplatePath = raytracingPassTemplatePath,
                sharedTemplateDirectory = HDTarget.sharedTemplateDirectory,

                // Port Mask
                vertexPorts = UnlitPortMasks.Vertex,
                pixelPorts = UnlitPortMasks.FragmentDefault,

                // Collections
                structs = CoreStructCollections.Default,
                fieldDependencies = CoreFieldDependencies.Default,
                pragmas = CorePragmas.RaytracingBasic,
                keywords = CoreKeywords.HDBase,
                includes = CoreIncludes.Raytracing,
                requiredFields = new FieldCollection(){ HDFields.SubShader.Unlit, HDFields.ShaderPass.RaytracingIndirect },
            };

            public static PassDescriptor RaytracingVisibility = new PassDescriptor()
            {
                // Definition
                displayName = "VisibilityDXR",
                referenceName = "SHADERPASS_RAYTRACING_VISIBILITY",
                lightMode = "VisibilityDXR",
                useInPreview = false,

                // Template
                passTemplatePath = raytracingPassTemplatePath,
                sharedTemplateDirectory = HDTarget.sharedTemplateDirectory,

                // Port Mask
                vertexPorts = UnlitPortMasks.Vertex,
                pixelPorts = UnlitPortMasks.FragmentDefault,

                // Collections
                structs = CoreStructCollections.Default,
                fieldDependencies = CoreFieldDependencies.Default,
                pragmas = CorePragmas.RaytracingBasic,
                includes = CoreIncludes.Raytracing,
                keywords = CoreKeywords.RaytracingVisiblity,
                requiredFields = new FieldCollection(){ HDFields.SubShader.Unlit, HDFields.ShaderPass.RaytracingVisibility },
            };

            public static PassDescriptor RaytracingForward = new PassDescriptor()
            {
                // Definition
                displayName = "ForwardDXR",
                referenceName = "SHADERPASS_RAYTRACING_FORWARD",
                lightMode = "ForwardDXR",
                useInPreview = false,

                // Template
                passTemplatePath = raytracingPassTemplatePath,
                sharedTemplateDirectory = HDTarget.sharedTemplateDirectory,

                // Port Mask
                vertexPorts = UnlitPortMasks.Vertex,
                pixelPorts = UnlitPortMasks.FragmentDefault,

                // Collections
                structs = CoreStructCollections.Default,
                fieldDependencies = CoreFieldDependencies.Default,
                pragmas = CorePragmas.RaytracingBasic,
                keywords = CoreKeywords.HDBase,
                includes = CoreIncludes.Raytracing,
                requiredFields = new FieldCollection(){ HDFields.SubShader.Unlit, HDFields.ShaderPass.RaytracingForward },
            };

            public static PassDescriptor RaytracingGBuffer = new PassDescriptor()
            {
                // Definition
                displayName = "GBufferDXR",
                referenceName = "SHADERPASS_RAYTRACING_GBUFFER",
                lightMode = "GBufferDXR",
                useInPreview = false,

                // Template
                passTemplatePath = raytracingPassTemplatePath,
                sharedTemplateDirectory = HDTarget.sharedTemplateDirectory,

                // Port Mask
                vertexPorts = UnlitPortMasks.Vertex,
                pixelPorts = UnlitPortMasks.FragmentDefault,

                // Collections
                structs = CoreStructCollections.Default,
                fieldDependencies = CoreFieldDependencies.Default,
                pragmas = CorePragmas.RaytracingBasic,
                keywords = CoreKeywords.HDBase,
                includes = CoreIncludes.Raytracing,
                requiredFields = new FieldCollection(){ HDFields.SubShader.Unlit, HDFields.ShaderPass.RayTracingGBuffer },
            };

            public static PassDescriptor RaytracingPathTracing = new PassDescriptor()
            {
                //Definition
                displayName = "PathTracingDXR",
                referenceName = "SHADERPASS_PATH_TRACING",
                lightMode = "PathTracingDXR",
                useInPreview = false,

                // Template
                passTemplatePath = raytracingPassTemplatePath,
                sharedTemplateDirectory = HDTarget.sharedTemplateDirectory,

                // Port Mask
                vertexPorts = UnlitPortMasks.Vertex,
                pixelPorts = UnlitPortMasks.FragmentDefault,

                // Collections
                structs = CoreStructCollections.Default,
                fieldDependencies = CoreFieldDependencies.Default,
                pragmas = CorePragmas.RaytracingBasic,
                keywords = CoreKeywords.HDBaseNoCrossFade,
                includes = CoreIncludes.Raytracing,
                requiredFields = new FieldCollection(){ HDFields.SubShader.Unlit, HDFields.ShaderPass.RaytracingPathTracing },
            };
        }
#endregion

#region PortMasks
        static class UnlitPortMasks
        {
            public static int[] Vertex = new int[]
            {
                HDUnlitMasterNode.PositionSlotId,
                HDUnlitMasterNode.VertexNormalSlotId,
                HDUnlitMasterNode.VertexTangentSlotId,
            };

            public static int[] FragmentDefault = new int[]
            {
                HDUnlitMasterNode.ColorSlotId,
                HDUnlitMasterNode.AlphaSlotId,
                HDUnlitMasterNode.AlphaThresholdSlotId,
                HDUnlitMasterNode.EmissionSlotId,
            };

            public static int[] FragmentOnlyAlpha = new int[]
            {
                HDUnlitMasterNode.AlphaSlotId,
                HDUnlitMasterNode.AlphaThresholdSlotId,
            };

            public static int[] FragmentDistortion = new int[]
            {
                HDUnlitMasterNode.AlphaSlotId,
                HDUnlitMasterNode.AlphaThresholdSlotId,
                HDUnlitMasterNode.DistortionSlotId,
                HDUnlitMasterNode.DistortionBlurSlotId,
            };

            public static int[] FragmentForward = new int[]
            {
                HDUnlitMasterNode.ColorSlotId,
                HDUnlitMasterNode.AlphaSlotId,
                HDUnlitMasterNode.AlphaThresholdSlotId,
                HDUnlitMasterNode.EmissionSlotId,
                HDUnlitMasterNode.ShadowTintSlotId,
            };
        }
#endregion

#region RenderStates
        static class UnlitRenderStates
        {
            public static RenderStateCollection SceneSelection = new RenderStateCollection
            {
                { RenderState.Cull(CoreRenderStates.Uniforms.cullMode) },
                { RenderState.ZWrite(ZWrite.On) },
                { RenderState.ColorMask("ColorMask 0") },
            };

            // Caution: When using MSAA we have normal and depth buffer bind.
            // Unlit objects need to NOT write in normal buffer (or write 0) - Disable color mask for this RT
            // Note: ShaderLab doesn't allow to have a variable on the second parameter of ColorMask
            // - When MSAA: disable target 1 (normal buffer)
            // - When no MSAA: disable target 0 (normal buffer) and 1 (unused)
            public static RenderStateCollection DepthForwardOnly = new RenderStateCollection
            {
                { RenderState.Cull(CoreRenderStates.Uniforms.cullMode) },
                { RenderState.ZWrite(ZWrite.On) },
                { RenderState.ColorMask("ColorMask [_ColorMaskNormal]") },
                { RenderState.ColorMask("ColorMask 0 1") },
                { RenderState.AlphaToMask(CoreRenderStates.Uniforms.alphaToMask), new FieldCondition(Fields.AlphaToMask, true) },
                { RenderState.Stencil(new StencilDescriptor()
                {
                    WriteMask = CoreRenderStates.Uniforms.stencilWriteMaskDepth,
                    Ref = CoreRenderStates.Uniforms.stencilRefDepth,
                    Comp = "Always",
                    Pass = "Replace",
                }) },
            };

            // Caution: When using MSAA we have motion vector, normal and depth buffer bind.
            // Mean unlit object need to not write in it (or write 0) - Disable color mask for this RT
            // This is not a problem in no MSAA mode as there is no buffer bind
            public static RenderStateCollection MotionVectors = new RenderStateCollection
            {
                { RenderState.Cull(CoreRenderStates.Uniforms.cullMode) },
                { RenderState.ZWrite(ZWrite.On) },
                { RenderState.ColorMask("ColorMask [_ColorMaskNormal] 1") },
                { RenderState.ColorMask("ColorMask 0 2") },
                { RenderState.AlphaToMask(CoreRenderStates.Uniforms.alphaToMask), new FieldCondition(Fields.AlphaToMask, true) },
                { RenderState.Stencil(new StencilDescriptor()
                {
                    WriteMask = CoreRenderStates.Uniforms.stencilWriteMaskMV,
                    Ref = CoreRenderStates.Uniforms.stencilRefMV,
                    Comp = "Always",
                    Pass = "Replace",
                }) },
            };

            public static RenderStateCollection Distortion = new RenderStateCollection
            {
                { RenderState.Blend(Blend.One, Blend.One, Blend.One, Blend.One), new FieldCondition(HDFields.DistortionAdd, true) },
                { RenderState.Blend(Blend.DstColor, Blend.Zero, Blend.DstAlpha, Blend.Zero), new FieldCondition(HDFields.DistortionMultiply, true) },
                { RenderState.Blend(Blend.One, Blend.Zero, Blend.One, Blend.Zero), new FieldCondition(HDFields.DistortionReplace, true) },
                { RenderState.BlendOp(BlendOp.Add, BlendOp.Add) },
                { RenderState.Cull(CoreRenderStates.Uniforms.cullMode) },
                { RenderState.ZWrite(ZWrite.Off) },
                { RenderState.ZTest(ZTest.Always), new FieldCondition(HDFields.DistortionDepthTest, false) },
                { RenderState.ZTest(ZTest.LEqual), new FieldCondition(HDFields.DistortionDepthTest, true) },
                { RenderState.Stencil(new StencilDescriptor()
                {
                    WriteMask = CoreRenderStates.Uniforms.stencilWriteMaskDistortionVec,
                    Ref = CoreRenderStates.Uniforms.stencilRefDistortionVec,
                    Comp = "Always",
                    Pass = "Replace",
                }) },
            };
        }
        #endregion

#region Defines
        static class UnlitDefines
        {
            public static DefineCollection RaytracingForward = new DefineCollection
            {
                { RayTracingNode.GetRayTracingKeyword(), 0 },
            };

            public static DefineCollection RaytracingIndirect = new DefineCollection
            {
                { RayTracingNode.GetRayTracingKeyword(), 1 },
            };

            public static DefineCollection RaytracingVisibility = new DefineCollection
            {
                { RayTracingNode.GetRayTracingKeyword(), 1 },
            };

            public static DefineCollection RaytracingGBuffer = new DefineCollection
            {
                { RayTracingNode.GetRayTracingKeyword(), 1 },
            };
        }
#endregion

#region Keywords
        static class UnlitKeywords
        {
            public static KeywordCollection DepthMotionVectors = new KeywordCollection
            {
                { CoreKeywords.HDBase },
                { CoreKeywordDescriptors.WriteMsaaDepth },
                { CoreKeywordDescriptors.AlphaToMask, new FieldCondition(Fields.AlphaToMask, true) },
            };

            public static KeywordCollection Forward = new KeywordCollection
            {
                { CoreKeywords.HDBase },
                { CoreKeywordDescriptors.DebugDisplay },
                { CoreKeywordDescriptors.Shadow, new FieldCondition(HDFields.EnableShadowMatte, true) },
            };
        }
#endregion

#region Includes
        static class UnlitIncludes
        {
            const string kPassForwardUnlit = "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassForwardUnlit.hlsl";
            
            public static IncludeCollection Meta = new IncludeCollection
            {
                { CoreIncludes.CorePregraph },
                { CoreIncludes.kUnlit, IncludeLocation.Pregraph },
                { CoreIncludes.CoreUtility },
                { CoreIncludes.kShaderGraphFunctions, IncludeLocation.Pregraph },
                { CoreIncludes.kPassLightTransport, IncludeLocation.Postgraph },
            };

            public static IncludeCollection DepthOnly = new IncludeCollection
            {
                { CoreIncludes.CorePregraph },
                { CoreIncludes.kUnlit, IncludeLocation.Pregraph },
                { CoreIncludes.CoreUtility },
                { CoreIncludes.kShaderGraphFunctions, IncludeLocation.Pregraph },
                { CoreIncludes.kPassDepthOnly, IncludeLocation.Postgraph },
            };

            public static IncludeCollection MotionVectors = new IncludeCollection
            {
                { CoreIncludes.CorePregraph },
                { CoreIncludes.kUnlit, IncludeLocation.Pregraph },
                { CoreIncludes.CoreUtility },
                { CoreIncludes.kShaderGraphFunctions, IncludeLocation.Pregraph },
                { CoreIncludes.kPassMotionVectors, IncludeLocation.Postgraph },
            };

            public static IncludeCollection Distortion = new IncludeCollection
            {
                { CoreIncludes.CorePregraph },
                { CoreIncludes.kUnlit, IncludeLocation.Pregraph },
                { CoreIncludes.CoreUtility },
                { CoreIncludes.kShaderGraphFunctions, IncludeLocation.Pregraph },
                { CoreIncludes.kDisortionVectors, IncludeLocation.Postgraph },
            };

            public static IncludeCollection ForwardOnly = new IncludeCollection
            {
                { CoreIncludes.CorePregraph },
                { CoreIncludes.kUnlit, IncludeLocation.Pregraph },
                { CoreIncludes.CoreUtility },
                { CoreIncludes.kShaderGraphFunctions, IncludeLocation.Pregraph },
                { CoreIncludes.kHDShadow, IncludeLocation.Pregraph, new FieldCondition(HDFields.EnableShadowMatte, true) },
                { CoreIncludes.kLightLoopDef, IncludeLocation.Pregraph, new FieldCondition(HDFields.EnableShadowMatte, true) },
                { CoreIncludes.kPunctualLightCommon, IncludeLocation.Pregraph, new FieldCondition(HDFields.EnableShadowMatte, true) },
                { CoreIncludes.kHDShadowLoop, IncludeLocation.Pregraph, new FieldCondition(HDFields.EnableShadowMatte, true) },
                { kPassForwardUnlit, IncludeLocation.Postgraph },
            };
        }
#endregion
    }
}
