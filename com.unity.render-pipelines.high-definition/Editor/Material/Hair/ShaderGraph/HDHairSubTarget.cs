using UnityEngine.Rendering.HighDefinition;
using UnityEditor.ShaderGraph;

namespace UnityEditor.Rendering.HighDefinition.ShaderGraph
{
    sealed class HDHairSubTarget : SubTarget<HDTarget>
    {
        const string kAssetGuid = "7e681cc79dd8e6c46ba1e8412d519e26";
        static string passTemplatePath => $"{HDUtils.GetHDRenderPipelinePath()}Editor/Material/Hair/ShaderGraph/HairPass.template";

        public HDHairSubTarget()
        {
            displayName = "Hair";
        }

        public override void Setup(ref TargetSetupContext context)
        {
            context.AddAssetDependencyPath(AssetDatabase.GUIDToAssetPath(kAssetGuid));
            context.SetDefaultShaderGUI("Rendering.HighDefinition.HairGUI");
            context.AddSubShader(SubShaders.Hair);
        }

#region SubShaders
        static class SubShaders
        {
            public static SubShaderDescriptor Hair = new SubShaderDescriptor()
            {
                pipelineTag = HDRenderPipeline.k_ShaderTagName,
                generatesPreview = true,
                passes = new PassCollection
                {
                    { HairPasses.ShadowCaster },
                    { HairPasses.META },
                    { HairPasses.SceneSelection },
                    { HairPasses.DepthForwardOnly },
                    { HairPasses.MotionVectors },
                    { HairPasses.TransparentBackface, new FieldCondition(HDFields.TransparentBackFace, true) },
                    { HairPasses.TransparentDepthPrepass, new FieldCondition(HDFields.TransparentDepthPrePass, true) },
                    { HairPasses.ForwardOnly },
                    { HairPasses.TransparentDepthPostpass, new FieldCondition(HDFields.TransparentDepthPostPass, true) },
                },
            };
        }
#endregion

#region Passes
        public static class HairPasses
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
                pixelPorts = HairPortMasks.FragmentMETA,

                // Collections
                structs = CoreStructCollections.Default,
                requiredFields = CoreRequiredFields.Meta,
                fieldDependencies = CoreFieldDependencies.Default,
                renderStates = CoreRenderStates.Meta,
                pragmas = CorePragmas.InstancedRenderingLayer,
                keywords = CoreKeywords.HDBase,
                includes = HairIncludes.Meta,
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
                vertexPorts = HairPortMasks.Vertex,
                pixelPorts = HairPortMasks.FragmentShadowCaster,

                // Collections
                structs = CoreStructCollections.Default,
                fieldDependencies = CoreFieldDependencies.Default,
                renderStates = CoreRenderStates.BlendShadowCaster,
                pragmas = CorePragmas.InstancedRenderingLayer,
                keywords = CoreKeywords.HDBase,
                includes = HairIncludes.DepthOnly,
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
                vertexPorts = HairPortMasks.Vertex,
                pixelPorts = HairPortMasks.FragmentAlphaDepth,

                // Collections
                structs = CoreStructCollections.Default,
                fieldDependencies = CoreFieldDependencies.Default,
                renderStates = CoreRenderStates.SceneSelection,
                pragmas = CorePragmas.InstancedRenderingLayerEditorSync,
                defines = CoreDefines.SceneSelection,
                keywords = CoreKeywords.HDBase,
                includes = HairIncludes.DepthOnly,
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
                vertexPorts = HairPortMasks.Vertex,
                pixelPorts = HairPortMasks.FragmentDepthMotionVectors,

                // Collections
                structs = CoreStructCollections.Default,
                requiredFields = CoreRequiredFields.LitFull,
                fieldDependencies = CoreFieldDependencies.Default,
                renderStates = HairRenderStates.DepthOnly,
                pragmas = CorePragmas.InstancedRenderingLayer,
                defines = CoreDefines.DepthMotionVectors,
                keywords = CoreKeywords.DepthMotionVectorsNoNormal,
                includes = HairIncludes.DepthOnly,
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
                vertexPorts = HairPortMasks.Vertex,
                pixelPorts = HairPortMasks.FragmentDepthMotionVectors,

                // Collections
                structs = CoreStructCollections.Default,
                requiredFields = CoreRequiredFields.LitFull,
                fieldDependencies = CoreFieldDependencies.Default,
                renderStates = HairRenderStates.MotionVectors,
                pragmas = CorePragmas.InstancedRenderingLayer,
                defines = CoreDefines.DepthMotionVectors,
                keywords = CoreKeywords.DepthMotionVectorsNoNormal,
                includes = HairIncludes.MotionVectors,
            };

            public static PassDescriptor TransparentDepthPrepass = new PassDescriptor()
            {
                // Definition
                displayName = "TransparentDepthPrepass",
                referenceName = "SHADERPASS_DEPTH_ONLY",
                lightMode = "TransparentDepthPrepass",
                useInPreview = true,

                // Template
                passTemplatePath = passTemplatePath,
                sharedTemplateDirectory = HDTarget.sharedTemplateDirectory,

                // Port Mask
                vertexPorts = HairPortMasks.Vertex,
                pixelPorts = HairPortMasks.FragmentTransparentDepthPrepass,

                // Collections
                structs = CoreStructCollections.Default,
                fieldDependencies = CoreFieldDependencies.Default,
                renderStates = CoreRenderStates.TransparentDepthPrePostPass,
                pragmas = CorePragmas.InstancedRenderingLayer,
                defines = CoreDefines.TransparentDepthPrepass,
                keywords = CoreKeywords.HDBase,
                includes = HairIncludes.DepthOnly,
            };

            public static PassDescriptor TransparentBackface = new PassDescriptor()
            {
                // Definition
                displayName = "TransparentBackface",
                referenceName = "SHADERPASS_FORWARD",
                lightMode = "TransparentBackface",
                useInPreview = true,

                // Template
                passTemplatePath = passTemplatePath,
                sharedTemplateDirectory = HDTarget.sharedTemplateDirectory,

                // Port Mask
                vertexPorts = HairPortMasks.Vertex,
                pixelPorts = HairPortMasks.FragmentTransparentBackface,

                // Collections
                structs = CoreStructCollections.Default,
                requiredFields = CoreRequiredFields.LitMinimal,
                fieldDependencies = CoreFieldDependencies.Default,
                renderStates = CoreRenderStates.TransparentBackface,
                pragmas = CorePragmas.InstancedRenderingLayer,
                defines = CoreDefines.Forward,
                keywords = CoreKeywords.Forward,
                includes = HairIncludes.ForwardOnly,
            };

            public static PassDescriptor ForwardOnly = new PassDescriptor()
            {
                // Definition
                displayName = "ForwardOnly",
                referenceName = "SHADERPASS_FORWARD",
                lightMode = "ForwardOnly",
                useInPreview = true,

                // Template
                passTemplatePath = passTemplatePath,
                sharedTemplateDirectory = HDTarget.sharedTemplateDirectory,

                // Port Mask
                vertexPorts = HairPortMasks.Vertex,
                pixelPorts = HairPortMasks.FragmentForward,

                // Collections
                structs = CoreStructCollections.Default,
                requiredFields = CoreRequiredFields.LitFull,
                fieldDependencies = CoreFieldDependencies.Default,
                renderStates = CoreRenderStates.ForwardColorMask,
                pragmas = CorePragmas.InstancedRenderingLayer,
                defines = CoreDefines.Forward,
                keywords = CoreKeywords.Forward,
                includes = HairIncludes.ForwardOnly,
            };

            public static PassDescriptor TransparentDepthPostpass = new PassDescriptor()
            {
                // Definition
                displayName = "TransparentDepthPostpass",
                referenceName = "SHADERPASS_DEPTH_ONLY",
                lightMode = "TransparentDepthPostpass",
                useInPreview = true,

                // Template
                passTemplatePath = passTemplatePath,
                sharedTemplateDirectory = HDTarget.sharedTemplateDirectory,

                // Port Mask
                vertexPorts = HairPortMasks.Vertex,
                pixelPorts = HairPortMasks.FragmentTransparentDepthPostpass,

                // Collections
                structs = CoreStructCollections.Default,
                fieldDependencies = CoreFieldDependencies.Default,
                renderStates = CoreRenderStates.TransparentDepthPrePostPass,
                pragmas = CorePragmas.InstancedRenderingLayer,
                defines = CoreDefines.ShaderGraphRaytracingHigh,
                keywords = CoreKeywords.HDBase,
                includes = HairIncludes.DepthOnly,
            };
        }
#endregion

#region PortMasks
        static class HairPortMasks
        {
            public static int[] Vertex = new int[]
            {
                HairMasterNode.PositionSlotId,
                HairMasterNode.VertexNormalSlotId,
                HairMasterNode.VertexTangentSlotId,
            };

            public static int[] FragmentMETA = new int[]
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

            public static int[] FragmentShadowCaster = new int[]
            {
                HairMasterNode.AlphaSlotId,
                HairMasterNode.AlphaClipThresholdSlotId,
                HairMasterNode.AlphaClipThresholdShadowSlotId,
                HairMasterNode.DepthOffsetSlotId,
            };

            public static int[] FragmentAlphaDepth = new int[]
            {
                HairMasterNode.AlphaSlotId,
                HairMasterNode.AlphaClipThresholdSlotId,
                HairMasterNode.DepthOffsetSlotId,
            };

            public static int[] FragmentDepthMotionVectors = new int[]
            {
                HairMasterNode.NormalSlotId,
                HairMasterNode.SmoothnessSlotId,
                HairMasterNode.AlphaSlotId,
                HairMasterNode.AlphaClipThresholdSlotId,
                HairMasterNode.DepthOffsetSlotId,
            };

            public static int[] FragmentTransparentDepthPrepass = new int[]
            {
                HairMasterNode.AlphaSlotId,
                HairMasterNode.AlphaClipThresholdDepthPrepassSlotId,
                HairMasterNode.DepthOffsetSlotId,
            };

            public static int[] FragmentTransparentBackface = new int[]
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

            public static int[] FragmentForward = new int[]
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

            public static int[] FragmentTransparentDepthPostpass = new int[]
            {
                HairMasterNode.AlphaSlotId,
                HairMasterNode.AlphaClipThresholdDepthPostpassSlotId,
                HairMasterNode.DepthOffsetSlotId,
            };
        }
#endregion

#region RenderStates
        static class HairRenderStates
        {
            public static RenderStateCollection DepthOnly = new RenderStateCollection
            {
                { RenderState.Cull(CoreRenderStates.Uniforms.cullMode) },
                { RenderState.ZWrite(ZWrite.On) },
                { RenderState.Stencil(new StencilDescriptor()
                {
                    WriteMask = CoreRenderStates.Uniforms.stencilWriteMaskDepth,
                    Ref = CoreRenderStates.Uniforms.stencilRefDepth,
                    Comp = "Always",
                    Pass = "Replace",
                }) },
            };

            public static RenderStateCollection MotionVectors = new RenderStateCollection
            {
                { RenderState.Stencil(new StencilDescriptor()
                {
                    WriteMask = CoreRenderStates.Uniforms.stencilWriteMaskMV,
                    Ref = CoreRenderStates.Uniforms.stencilRefMV,
                    Comp = "Always",
                    Pass = "Replace",
                }) },
            };
        }
#endregion

#region Includes
        static class HairIncludes
        {
            const string kHair = "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Hair/Hair.hlsl";
            
            public static IncludeCollection Common = new IncludeCollection
            {
                { CoreIncludes.CorePregraph },
                { CoreIncludes.kNormalSurfaceGradient, IncludeLocation.Pregraph },
                { kHair, IncludeLocation.Pregraph },
                { CoreIncludes.CoreUtility },
                { CoreIncludes.kDecalUtilities, IncludeLocation.Pregraph },
                { CoreIncludes.kShaderGraphFunctions, IncludeLocation.Pregraph },
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

            public static IncludeCollection ForwardOnly = new IncludeCollection
            {
                { CoreIncludes.CorePregraph },
                { CoreIncludes.kNormalSurfaceGradient, IncludeLocation.Pregraph },
                { CoreIncludes.kLighting, IncludeLocation.Pregraph },
                { CoreIncludes.kLightLoopDef, IncludeLocation.Pregraph },
                { kHair, IncludeLocation.Pregraph },
                { CoreIncludes.kLightLoop, IncludeLocation.Pregraph },
                { CoreIncludes.CoreUtility },
                { CoreIncludes.kDecalUtilities, IncludeLocation.Pregraph },
                { CoreIncludes.kShaderGraphFunctions, IncludeLocation.Pregraph },
                { CoreIncludes.kPassForward, IncludeLocation.Postgraph },
            };
        }
#endregion
    }
}
