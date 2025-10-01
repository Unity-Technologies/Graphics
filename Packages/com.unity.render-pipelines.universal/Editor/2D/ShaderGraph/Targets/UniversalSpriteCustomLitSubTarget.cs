using System;
using UnityEditor.ShaderGraph;
using Unity.Rendering.Universal;

namespace UnityEditor.Rendering.Universal.ShaderGraph
{
    sealed class UniversalSpriteCustomLitSubTarget : UniversalSubTarget
    {
        static readonly GUID kSourceCodeGuid = new GUID("69e608b3e7e0405bbc2f259ad9cfa196"); // UniversalUnlitSubTarget.cs

        public UniversalSpriteCustomLitSubTarget()
        {
            displayName = "Sprite Custom Lit";
        }

        protected override ShaderUtils.ShaderID shaderID => ShaderUtils.ShaderID.SG_SpriteCustomLit;

        public override bool IsActive() => true;

        public override void Setup(ref TargetSetupContext context)
        {
            base.Setup(ref context);
            context.AddAssetDependency(kSourceCodeGuid, AssetCollection.Flags.SourceDependency);

            var universalRPType = typeof(UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset);
            var gui = typeof(ShaderGraphSpriteGUI);
#if HAS_VFX_GRAPH
            if (TargetsVFX())
                gui = typeof(VFXGenericShaderGraphMaterialGUI);
#endif
            context.AddCustomEditorForRenderPipeline(gui.FullName, universalRPType);
            context.AddSubShader(PostProcessSubShader(SubShaders.SpriteLit(target)));
        }

        public override void GetFields(ref TargetFieldContext context)
        {
            base.GetFields(ref context);

            SpriteSubTargetUtility.AddDefaultFields(ref context, target);
        }

        public override void GetActiveBlocks(ref TargetActiveBlockContext context)
        {
            SpriteSubTargetUtility.GetDefaultActiveBlocks(ref context, target);

            context.AddBlock(UniversalBlockFields.SurfaceDescription.SpriteMask);
            context.AddBlock(BlockFields.SurfaceDescription.NormalTS);
        }

        public override void GetPropertiesGUI(ref TargetPropertyGUIContext context, Action onChange, Action<String> registerUndo)
        {
            SpriteSubTargetUtility.AddDefaultPropertiesGUI(ref context, onChange, registerUndo, target);
        }

        public override void CollectShaderProperties(PropertyCollector collector, GenerationMode generationMode)
        {
            base.CollectShaderProperties(collector, generationMode);
            SpriteSubTargetUtility.AddSRPIncompatibility(collector);
        }

        #region SubShader
        static class SubShaders
        {
            public static SubShaderDescriptor SpriteLit(UniversalTarget target)
            {
                SubShaderDescriptor result = new SubShaderDescriptor()
                {
                    pipelineTag = UniversalTarget.kPipelineTag,
                    customTags = UniversalTarget.kLitMaterialTypeTag,
                    renderType = $"{RenderType.Transparent}",
                    renderQueue = $"{UnityEditor.ShaderGraph.RenderQueue.Transparent}",
                    generatesPreview = true,
                    passes = new PassCollection
                    {
                        { SpriteCustomLitPasses.CustomLit(target) },
                        { SpriteCustomLitPasses.Normal(target) },
                        // Currently neither of these passes (selection/picking) can be last for the game view for
                        // UI shaders to render correctly. Verify [1352225] before changing this order.
                        { CorePasses._2DSceneSelection(target) },
                        { CorePasses._2DScenePicking(target) },
                        { SpriteCustomLitPasses.Forward(target) },
                    },
                };
                return result;
            }
        }
        #endregion

        #region Passes
        static class SpriteCustomLitPasses
        {
            public static PassDescriptor CustomLit(UniversalTarget target)
            {
                var result = new PassDescriptor()
                {
                    // Definition
                    displayName = "Sprite Lit",
                    referenceName = "SHADERPASS_SPRITELIT",
                    lightMode = "Universal2D",
                    useInPreview = true,

                    // Template
                    passTemplatePath = UniversalTarget.kUberTemplatePath,
                    sharedTemplateDirectories = UniversalTarget.kSharedTemplateDirectories,

                    // Port Mask
                    validVertexBlocks = CoreBlockMasks.Vertex,
                    validPixelBlocks = SpriteLitBlockMasks.FragmentLit,

                    // Fields
                    structs = CoreStructCollections.Default,
                    requiredFields = SpriteLitRequiredFields.Lit,
                    fieldDependencies = CoreFieldDependencies.Default,

                    // Conditional State
                    renderStates = target.sort3DAs2DCompatible ? Universal2DSubTargetDescriptors.RenderStateCollections.Sort3DAs2DCompatible : CoreRenderStates.Default,
                    pragmas = CorePragmas._2DDefault,
                    defines = new DefineCollection(),
                    keywords = SpriteCustomLitKeywords.Lit,
                    includes = target.sort3DAs2DCompatible ? MeshUnlitIncludes.Unlit : SpriteCustomLitIncludes.Unlit,

                    // Custom Interpolator Support
                    customInterpolators = CoreCustomInterpDescriptors.Common
                };

                if (target.disableTint)
                    result.defines.Add(Canvas.ShaderGraph.CanvasSubTarget<Target>.CanvasKeywords.DisableTint, 1);

                SpriteSubTargetUtility.AddAlphaClipControlToPass(ref result, target);

                return result;
            }

            public static PassDescriptor Normal(UniversalTarget target)
            {
                var result = new PassDescriptor()
                {
                    // Definition
                    displayName = "Sprite Normal",
                    referenceName = "SHADERPASS_SPRITENORMAL",
                    lightMode = "NormalsRendering",
                    useInPreview = true,

                    // Template
                    passTemplatePath = UniversalTarget.kUberTemplatePath,
                    sharedTemplateDirectories = UniversalTarget.kSharedTemplateDirectories,

                    // Port Mask
                    validVertexBlocks = CoreBlockMasks.Vertex,
                    validPixelBlocks = SpriteLitBlockMasks.FragmentNormal,

                    // Fields
                    structs = CoreStructCollections.Default,
                    requiredFields = SpriteLitRequiredFields.Normal,
                    fieldDependencies = CoreFieldDependencies.Default,

                    // Conditional State

                    renderStates = target.sort3DAs2DCompatible ? Universal2DSubTargetDescriptors.RenderStateCollections.Sort3DAs2DCompatible : CoreRenderStates.Default,
                    pragmas = CorePragmas._2DDefault,
                    defines = new DefineCollection(),
                    includes = target.sort3DAs2DCompatible ? UniversalMeshLitInfo.Includes.Normal : SpriteCustomLitIncludes.Normal,

                    // Custom Interpolator Support
                    customInterpolators = CoreCustomInterpDescriptors.Common

                };

                if (target.disableTint)
                    result.defines.Add(Canvas.ShaderGraph.CanvasSubTarget<Target>.CanvasKeywords.DisableTint, 1);

                SpriteSubTargetUtility.AddAlphaClipControlToPass(ref result, target);

                return result;
            }

            public static PassDescriptor Forward(UniversalTarget target)
            {
                var result = new PassDescriptor
                {
                    // Definition
                    displayName = "Sprite Forward",
                    referenceName = "SHADERPASS_SPRITEFORWARD",
                    lightMode = "UniversalForward",
                    useInPreview = true,

                    // Template
                    passTemplatePath = UniversalTarget.kUberTemplatePath,
                    sharedTemplateDirectories = UniversalTarget.kSharedTemplateDirectories,

                    // Port Mask
                    validVertexBlocks = CoreBlockMasks.Vertex,
                    validPixelBlocks = SpriteLitBlockMasks.FragmentForward,

                    // Fields
                    structs = CoreStructCollections.Default,
                    requiredFields = SpriteLitRequiredFields.Forward,
                    keywords = SpriteCustomLitKeywords.Forward,
                    fieldDependencies = CoreFieldDependencies.Default,

                    // Conditional State
                    renderStates = CoreRenderStates.Default,
                    pragmas = CorePragmas._2DDefault,
                    defines = new DefineCollection(),
                    includes = SpriteCustomLitIncludes.Forward,

                    // Custom Interpolator Support
                    customInterpolators = CoreCustomInterpDescriptors.Common
                };

                if (target.disableTint)
                    result.defines.Add(Canvas.ShaderGraph.CanvasSubTarget<Target>.CanvasKeywords.DisableTint, 1);

                return result;

            }
        }
        #endregion

        #region PortMasks
        static class SpriteLitBlockMasks
        {
            public static BlockFieldDescriptor[] FragmentLit = new BlockFieldDescriptor[]
            {
                BlockFields.SurfaceDescription.BaseColor,
                BlockFields.SurfaceDescriptionLegacy.SpriteColor,
                UniversalBlockFields.SurfaceDescription.SpriteMask,
                BlockFields.SurfaceDescription.NormalTS,
                BlockFields.SurfaceDescription.Alpha,
                BlockFields.SurfaceDescription.AlphaClipThreshold,
            };

            public static BlockFieldDescriptor[] FragmentNormal = new BlockFieldDescriptor[]
            {
                BlockFields.SurfaceDescription.NormalTS,
                BlockFields.SurfaceDescription.Alpha,
                BlockFields.SurfaceDescription.AlphaClipThreshold,
            };

            public static BlockFieldDescriptor[] FragmentForward = new BlockFieldDescriptor[]
            {
                BlockFields.SurfaceDescription.BaseColor,
                BlockFields.SurfaceDescriptionLegacy.SpriteColor,
                BlockFields.SurfaceDescription.Alpha,
                BlockFields.SurfaceDescription.NormalTS,
            };
        }
        #endregion

        #region RequiredFields
        static class SpriteLitRequiredFields
        {
            public static FieldCollection Lit = new FieldCollection()
            {
                StructFields.Varyings.color,
                StructFields.Varyings.positionWS,
                StructFields.Varyings.texCoord0,
                StructFields.Varyings.screenPosition,
                StructFields.Varyings.normalWS,
            };

            public static FieldCollection Normal = new FieldCollection()
            {
                StructFields.Varyings.color,
                StructFields.Varyings.normalWS,
                StructFields.Varyings.tangentWS,
            };

            public static FieldCollection Forward = new FieldCollection()
            {
                StructFields.Varyings.color,
                StructFields.Varyings.positionWS,
                StructFields.Varyings.texCoord0,
            };
        }
        #endregion

        #region Keywords
        static class SpriteCustomLitKeywords
        {
            public static KeywordCollection Lit = new KeywordCollection
            {
                { CoreKeywordDescriptors.DebugDisplay },
                { CoreKeywordDescriptors.UseSkinnedSprite },
            };

            public static KeywordCollection Forward = new KeywordCollection
            {
                { CoreKeywordDescriptors.DebugDisplay },
                { CoreKeywordDescriptors.UseSkinnedSprite },
            };
        }
        #endregion

        #region Includes
        static class SpriteCustomLitIncludes
        {
            const string kSpriteCore2D = "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Core2D.hlsl";
            const string kSpriteUnlitPass = "Packages/com.unity.render-pipelines.universal/Editor/2D/ShaderGraph/Includes/SpriteUnlitPass.hlsl";
            const string k2DNormal = "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/NormalsRenderingShared.hlsl";
            const string kSpriteNormalPass = "Packages/com.unity.render-pipelines.universal/Editor/2D/ShaderGraph/Includes/SpriteNormalPass.hlsl";
            const string kSpriteForwardPass = "Packages/com.unity.render-pipelines.universal/Editor/2D/ShaderGraph/Includes/SpriteForwardPass.hlsl";

            public static IncludeCollection Unlit = new IncludeCollection
            {
                // Pre-graph
                { CoreIncludes.FogPregraph },
                { CoreIncludes.CorePregraph },
                { CoreIncludes.ShaderGraphPregraph },
                { kSpriteCore2D, IncludeLocation.Pregraph },

                // Post-graph
                { CoreIncludes.CorePostgraph },
                { kSpriteUnlitPass, IncludeLocation.Postgraph },
            };

            public static IncludeCollection Normal = new IncludeCollection
            {
                // Pre-graph
                { CoreIncludes.CorePregraph },
                { CoreIncludes.ShaderGraphPregraph },
                { k2DNormal, IncludeLocation.Pregraph },
                { kSpriteCore2D, IncludeLocation.Pregraph },

                // Post-graph
                { CoreIncludes.CorePostgraph },
                { kSpriteNormalPass, IncludeLocation.Postgraph },
            };

            public static IncludeCollection Forward = new IncludeCollection
            {
                // Pre-graph
                { CoreIncludes.FogPregraph },
                { CoreIncludes.CorePregraph },
                { CoreIncludes.ShaderGraphPregraph },
                { kSpriteCore2D, IncludeLocation.Pregraph },

                // Post-graph
                { CoreIncludes.CorePostgraph },
                { kSpriteForwardPass, IncludeLocation.Postgraph },
            };
        }
        #endregion
    }
}
