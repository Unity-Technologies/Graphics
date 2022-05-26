using System;
using System.Collections.Generic;
using UnityEditor.ShaderGraph;
using UnityEngine;

namespace UnityEditor.Rendering.Foundry
{
    sealed class FoundryTestUnlitSubTarget : FoundryTestSubTarget
    {
        public override int latestVersion => 1;
        static readonly GUID kSourceCodeGuid = new GUID("f098c3fcb5349d34fa55e6cb4e64eaab"); // FoundryTestUnlitSubTarget.cs

        public FoundryTestUnlitSubTarget()
        {
            displayName = "Unlit";
        }

        public override bool IsActive() => true;

        public override void Setup(ref TargetSetupContext context)
        {
            base.Setup(ref context);

            context.AddAssetDependency(kSourceCodeGuid, AssetCollection.Flags.SourceDependency);

            var subShaderDescriptor = SubShaders.Unlit(target, target.renderType, target.renderQueue);
            if (modifySubShaderCallback != null)
                subShaderDescriptor = modifySubShaderCallback(subShaderDescriptor);
            context.AddSubShader(subShaderDescriptor);
        }

        public override void ProcessPreviewMaterial(Material material) {}
        public override void GetFields(ref TargetFieldContext context) {}
        public override void CollectShaderProperties(PropertyCollector collector, GenerationMode generationMode) {}
        public override void GetPropertiesGUI(ref TargetPropertyGUIContext context, Action onChange, Action<String> registerUndo) {}

        public override void GetActiveBlocks(ref TargetActiveBlockContext context)
        {
            context.AddBlock(BlockFields.SurfaceDescription.Alpha);
        }

        #region SubShader
        static class SubShaders
        {
            public static SubShaderDescriptor Unlit(FoundryTestTarget target, string renderType, string renderQueue)
            {
                var result = new SubShaderDescriptor()
                {
                    renderType = renderType,
                    renderQueue = renderQueue,
                    generatesPreview = true,
                    passes = new PassCollection(),
                    shaderCustomEditor = "MyCustomEditor",
                    shaderCustomEditors = new List<ShaderCustomEditor>() { }
                };

                result.passes.Add(UnlitPasses.Forward(target));

                return result;
            }
        }
        #endregion

        #region Pass
        static class UnlitPasses
        {
            public static PassDescriptor Forward(FoundryTestTarget target)
            {
                var result = new PassDescriptor
                {
                    // Definition
                    displayName = "Foundry Unlit Forward",
                    referenceName = "SHADERPASS_UNLIT",
                    useInPreview = true,

                    lightMode = "Foundry Unlit Forward",

                    // Template
                    passTemplatePath = FoundryTestTarget.kUberTemplatePath,
                    sharedTemplateDirectories = FoundryTestTarget.kSharedTemplateDirectories,

                    // Port Mask
                    validVertexBlocks = CoreBlockMasks.Vertex,
                    validPixelBlocks = CoreBlockMasks.Fragment,

                    // Fields
                    structs = CoreStructCollections.Default,
                    requiredFields = UnlitRequiredFields.Unlit,
                    fieldDependencies = CoreFieldDependencies.Default,

                    // Conditional State
                    renderStates = CoreRenderStates.Default,
                    pragmas = CorePragmas.Forward,
                    keywords = new KeywordCollection() { UnlitKeywords.UnlitBaseKeywords },
                    includes = UnlitIncludes.Unlit,

                    // Custom Interpolator Support
                    customInterpolators = CoreCustomInterpDescriptors.Common
                };

                return result;
            }

            #region RequiredFields
            static class UnlitRequiredFields
            {
                public static readonly FieldCollection Unlit = new FieldCollection()
                {
                    StructFields.Varyings.positionWS,
                    StructFields.Varyings.normalWS
                };
            }
            #endregion
        }
        #endregion

        #region Keywords
        static class UnlitKeywords
        {
            public static readonly KeywordCollection UnlitBaseKeywords = new KeywordCollection()
            {
                CoreKeywordDescriptors.DebugDisplay,
            };
        }
        #endregion

        #region Includes
        static class UnlitIncludes
        {
            const string kUnlitPass = "Assets/CommonAssets/Editor/Targets/Shaders/UnlitPass.hlsl";

            public static IncludeCollection Unlit = new IncludeCollection
            {
                // Pre-graph
                { CoreIncludes.CorePregraph },

                // Post-graph
                { CoreIncludes.CorePostgraph },
                { kUnlitPass, IncludeLocation.Postgraph },
            };
        }
        #endregion
    }
}
