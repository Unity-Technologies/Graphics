using UnityEngine.Rendering.HighDefinition;
using UnityEditor.ShaderGraph;

namespace UnityEditor.Rendering.HighDefinition.ShaderGraph
{
    sealed class HDEyeSubTarget : SubTarget<HDTarget>
    {
        const string kAssetGuid = "864e4e09d6293cf4d98457f740bb3301";
        static string passTemplatePath => $"{HDUtils.GetHDRenderPipelinePath()}Editor/Material/Eye/ShaderGraph/EyePass.template";

        public HDEyeSubTarget()
        {
            displayName = "Eye";
        }

        public override void Setup(ref TargetSetupContext context)
        {
            context.AddAssetDependencyPath(AssetDatabase.GUIDToAssetPath(kAssetGuid));
            context.SetDefaultShaderGUI("Rendering.HighDefinition.EyeGUI");
            context.AddSubShader(SubShaders.Eye);
        }

#region SubShaders
        static class SubShaders
        {
            public static SubShaderDescriptor Eye = new SubShaderDescriptor()
            {
                pipelineTag = HDRenderPipeline.k_ShaderTagName,
                generatesPreview = true,
                passes = new PassCollection
                {
                    { EyePasses.ShadowCaster },
                    { EyePasses.META },
                    { EyePasses.SceneSelection },
                    { EyePasses.DepthForwardOnly },
                    { EyePasses.MotionVectors },
                    { EyePasses.ForwardOnly },
                },
            };
        }
#endregion

#region Passes
        public static class EyePasses
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
                pixelPorts = EyePortMasks.FragmentMETA,

                // Collections
                structs = CoreStructCollections.Default,
                requiredFields = CoreRequiredFields.Meta,
                fieldDependencies = CoreFieldDependencies.Default,
                renderStates = CoreRenderStates.Meta,
                pragmas = CorePragmas.DotsInstancedInV2Only,
                keywords = CoreKeywords.HDBase,
                includes = EyeIncludes.Meta,
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
                vertexPorts = EyePortMasks.Vertex,
                pixelPorts = EyePortMasks.FragmentAlphaDepth,

                // Collections
                structs = CoreStructCollections.Default,
                fieldDependencies = CoreFieldDependencies.Default,
                renderStates = CoreRenderStates.BlendShadowCaster,
                pragmas = CorePragmas.DotsInstancedInV2Only,
                keywords = CoreKeywords.HDBase,
                includes = EyeIncludes.DepthOnly,
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
                vertexPorts = EyePortMasks.Vertex,
                pixelPorts = EyePortMasks.FragmentAlphaDepth,

                // Collections
                structs = CoreStructCollections.Default,
                fieldDependencies = CoreFieldDependencies.Default,
                renderStates = CoreRenderStates.SceneSelection,
                pragmas = CorePragmas.DotsInstancedInV2OnlyEditorSync,
                defines = CoreDefines.SceneSelection,
                keywords = CoreKeywords.HDBase,
                includes = EyeIncludes.DepthOnly,
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
                vertexPorts = EyePortMasks.Vertex,
                pixelPorts = EyePortMasks.FragmentDepthMotionVectors,

                // Collections
                structs = CoreStructCollections.Default,
                requiredFields = CoreRequiredFields.LitFull,
                fieldDependencies = CoreFieldDependencies.Default,
                renderStates = CoreRenderStates.DepthOnly,
                pragmas = CorePragmas.DotsInstancedInV2Only,
                defines = CoreDefines.DepthMotionVectors,
                keywords = CoreKeywords.DepthMotionVectorsNoNormal,
                includes = EyeIncludes.DepthOnly,
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
                vertexPorts = EyePortMasks.Vertex,
                pixelPorts = EyePortMasks.FragmentDepthMotionVectors,

                // Collections
                structs = CoreStructCollections.Default,
                requiredFields = CoreRequiredFields.LitFull,
                fieldDependencies = CoreFieldDependencies.Default,
                renderStates = CoreRenderStates.MotionVectors,
                pragmas = CorePragmas.DotsInstancedInV2Only,
                defines = CoreDefines.DepthMotionVectors,
                keywords = CoreKeywords.DepthMotionVectorsNoNormal,
                includes = EyeIncludes.MotionVectors,
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
                vertexPorts = EyePortMasks.Vertex,
                pixelPorts = EyePortMasks.FragmentForward,

                // Collections
                structs = CoreStructCollections.Default,
                requiredFields = CoreRequiredFields.LitFull,
                fieldDependencies = CoreFieldDependencies.Default,
                renderStates = CoreRenderStates.Forward,
                pragmas = CorePragmas.DotsInstancedInV2Only,
                defines = CoreDefines.Forward,
                keywords = CoreKeywords.Forward,
                includes = EyeIncludes.ForwardOnly,
            };
        }
#endregion

#region PortMasks
        static class EyePortMasks
        {
            public static int[] Vertex = new int[]
            {
                EyeMasterNode.PositionSlotId,
                EyeMasterNode.VertexNormalSlotID,
                EyeMasterNode.VertexTangentSlotID,
            };

            public static int[] FragmentMETA = new int[]
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

            public static int[] FragmentAlphaDepth = new int[]
            {
                EyeMasterNode.AlphaSlotId,
                EyeMasterNode.AlphaClipThresholdSlotId,
                EyeMasterNode.DepthOffsetSlotId,
            };

            public static int[] FragmentDepthMotionVectors = new int[]
            {
                EyeMasterNode.NormalSlotId,
                EyeMasterNode.SmoothnessSlotId,
                EyeMasterNode.AlphaSlotId,
                EyeMasterNode.AlphaClipThresholdSlotId,
                EyeMasterNode.DepthOffsetSlotId,
            };

            public static int[] FragmentForward = new int[]
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
        }
#endregion

#region Includes
        static class EyeIncludes
        {
            const string kEye = "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Eye/Eye.hlsl";

            public static IncludeCollection Common = new IncludeCollection
            {
                { CoreIncludes.CorePregraph },
                { CoreIncludes.kNormalSurfaceGradient, IncludeLocation.Pregraph },
                { kEye, IncludeLocation.Pregraph },
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
                { kEye, IncludeLocation.Pregraph },
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
