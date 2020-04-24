using UnityEngine.Rendering.HighDefinition;
using UnityEditor.ShaderGraph;

namespace UnityEditor.Rendering.HighDefinition.ShaderGraph
{
    sealed class UnlitSubTarget : SubTarget<HDTarget>
    {
        const string kAssetGuid = "625d75e9f0cb52546993731fe9ceeb47";
        static string passTemplatePath => $"{HDUtils.GetHDRenderPipelinePath()}Editor/Material/Unlit/ShaderGraph/UnlitPass.template";

        public UnlitSubTarget()
        {
            displayName = "Unlit";
        }

        public override void Setup(ref TargetSetupContext context)
        {
            context.AddAssetDependencyPath(AssetDatabase.GUIDToAssetPath(kAssetGuid));
            context.SetDefaultShaderGUI("Rendering.HighDefinition.UnlitUI");
            context.AddSubShader(SubShaders.Unlit);
        }

#region SubShaders
        static class SubShaders
        {
            public static SubShaderDescriptor Unlit = new SubShaderDescriptor()
            {
                pipelineTag = HDRenderPipeline.k_ShaderTagName,
                renderTypeOverride = HDRenderTypeTags.HDUnlitShader.ToString(),
                generatesPreview = true,
                passes = new PassCollection
                {
                    { UnlitPasses.ShadowCaster },
                    { UnlitPasses.META },
                    { UnlitPasses.SceneSelection },
                    { UnlitPasses.DepthForwardOnly, new FieldCondition(Fields.SurfaceOpaque, true) },
                    { UnlitPasses.MotionVectors, new FieldCondition(Fields.SurfaceOpaque, true) },
                    { UnlitPasses.ForwardOnly },
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
                requiredFields = CoreRequiredFields.Meta,
                fieldDependencies = CoreFieldDependencies.Default,
                renderStates = CoreRenderStates.Meta,
                pragmas = CorePragmas.DotsInstancedInV2Only,
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
                fieldDependencies = CoreFieldDependencies.Default,
                renderStates = UnlitRenderStates.ShadowCaster,
                pragmas = CorePragmas.DotsInstancedInV2Only,
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
                fieldDependencies = CoreFieldDependencies.Default,
                renderStates = UnlitRenderStates.SceneSelection,
                pragmas = CorePragmas.DotsInstancedInV2OnlyEditorSync,
                defines = CoreDefines.SceneSelection,
                includes = UnlitIncludes.DepthOnly,
            };

            public static PassDescriptor DepthForwardOnly = new PassDescriptor()
            {
                // Definition
                displayName = "DepthForwardOnly",
                referenceName = "SHADERPASS_DEPTH_ONLY",
                lightMode = "DepthForwardOnly",
                useInPreview = false,

                // Template
                passTemplatePath = passTemplatePath,
                sharedTemplateDirectory = HDTarget.sharedTemplateDirectory,

                // Port Mask
                vertexPorts = UnlitPortMasks.Vertex,
                pixelPorts = UnlitPortMasks.FragmentOnlyAlpha,

                // Collections
                structs = CoreStructCollections.Default,
                fieldDependencies = CoreFieldDependencies.Default,
                renderStates = UnlitRenderStates.DepthForwardOnly,
                pragmas = CorePragmas.DotsInstancedInV2Only,
                keywords = CoreKeywords.WriteMsaaDepth,
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
                requiredFields = CoreRequiredFields.PositionRWS,
                fieldDependencies = CoreFieldDependencies.Default,
                renderStates = UnlitRenderStates.MotionVectors,
                pragmas = CorePragmas.DotsInstancedInV2Only,
                keywords = CoreKeywords.WriteMsaaDepth,
                includes = UnlitIncludes.MotionVectors,
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
                pixelPorts = UnlitPortMasks.FragmentDefault,

                // Collections
                structs = CoreStructCollections.Default,
                fieldDependencies = CoreFieldDependencies.Default,
                renderStates = UnlitRenderStates.Forward,
                pragmas = CorePragmas.DotsInstancedInV2Only,
                keywords = CoreKeywords.DebugDisplay,
                includes = UnlitIncludes.ForwardOnly,
            };
        }
#endregion

#region PortMasks
        static class UnlitPortMasks
        {
            public static int[] Vertex = new int[]
            {
                UnlitMasterNode.PositionSlotId,
                UnlitMasterNode.VertNormalSlotId,
                UnlitMasterNode.VertTangentSlotId,
            };

            public static int[] FragmentDefault = new int[]
            {
                UnlitMasterNode.ColorSlotId,
                UnlitMasterNode.AlphaSlotId,
                UnlitMasterNode.AlphaThresholdSlotId,
            };

            public static int[] FragmentOnlyAlpha = new int[]
            {
                UnlitMasterNode.AlphaSlotId,
                UnlitMasterNode.AlphaThresholdSlotId,
            };
        }
#endregion

#region RenderStates
        static class UnlitRenderStates
        {
            public static RenderStateCollection ShadowCaster = new RenderStateCollection
            {
                { RenderState.Cull(Cull.Off), new FieldCondition(Fields.DoubleSided, true) },
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

            // Caution: When using MSAA we have normal and depth buffer bind.
            // Unlit objects need to NOT write in normal buffer (or write 0) - Disable color mask for this RT
            // Note: ShaderLab doesn't allow to have a variable on the second parameter of ColorMask
            // - When MSAA: disable target 1 (normal buffer)
            // - When no MSAA: disable target 0 (normal buffer) and 1 (unused)
            public static RenderStateCollection DepthForwardOnly = new RenderStateCollection
            {
                { RenderState.Cull(Cull.Off), new FieldCondition(Fields.DoubleSided, true) },
                { RenderState.ZWrite(ZWrite.On), new FieldCondition(Fields.SurfaceOpaque, true) },
                { RenderState.ZWrite(ZWrite.Off), new FieldCondition(Fields.SurfaceTransparent, true) },
                { RenderState.ColorMask("ColorMask [_ColorMaskNormal]") },
                { RenderState.ColorMask("ColorMask 0 1") },
            };

            // Caution: When using MSAA we have motion vector, normal and depth buffer bind.
            // Mean unlit object need to not write in it (or write 0) - Disable color mask for this RT
            // This is not a problem in no MSAA mode as there is no buffer bind
            public static RenderStateCollection MotionVectors = new RenderStateCollection
            {
                { RenderState.Cull(Cull.Off), new FieldCondition(Fields.DoubleSided, true) },
                { RenderState.ColorMask("ColorMask [_ColorMaskNormal] 1") },
                { RenderState.ColorMask("ColorMask 0 2") },
                { RenderState.Stencil(new StencilDescriptor()
                {
                    WriteMask = $"{(int)StencilUsage.ObjectMotionVector}",
                    Ref = $"{(int)StencilUsage.ObjectMotionVector}",
                    Comp = "Always",
                    Pass = "Replace",
                }) },
            };

            public static RenderStateCollection Forward = new RenderStateCollection
            {
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

                { RenderState.Cull(Cull.Off), new FieldCondition(Fields.DoubleSided, true) },
                { RenderState.ZWrite(ZWrite.On), new FieldCondition(Fields.SurfaceOpaque, true) },
                { RenderState.ZWrite(ZWrite.Off), new FieldCondition(Fields.SurfaceTransparent, true) },
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

#region Includes
        static class UnlitIncludes
        {
            // These are duplicated from HDUnlitSubTarget
            // We avoid moving these to CoreIncludes because this SubTarget will be removed with Stacks

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

            public static IncludeCollection ForwardOnly = new IncludeCollection
            {
                { CoreIncludes.CorePregraph },
                { CoreIncludes.kUnlit, IncludeLocation.Pregraph },
                { CoreIncludes.CoreUtility },
                { CoreIncludes.kShaderGraphFunctions, IncludeLocation.Pregraph },
                { CoreIncludes.kCommonLighting, IncludeLocation.Pregraph, new FieldCondition(HDFields.EnableShadowMatte, true) },
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
