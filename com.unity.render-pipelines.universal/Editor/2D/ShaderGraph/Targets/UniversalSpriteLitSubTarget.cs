using System;
using System.Collections.Generic;
using UnityEditor.ShaderGraph;
using UnityEditor.ShaderGraph.Legacy;

using Unity.Rendering.Universal;
using UnityEngine;

namespace UnityEditor.Rendering.Universal.ShaderGraph
{
    sealed class UniversalSpriteLitSubTarget : UniversalSubTarget, ILegacyTarget
    {
        static readonly GUID kSourceCodeGuid = new GUID("ea1514729d7120344b27dcd67fbf34de"); // UniversalSpriteLitSubTarget.cs

        public UniversalSpriteLitSubTarget()
        {
            displayName = "Sprite Lit";
        }

        public override bool IsActive() => true;

        protected override ShaderUtils.ShaderID shaderID => ShaderUtils.ShaderID.SG_SpriteLit;

        public override void Setup(ref TargetSetupContext context)
        {
            base.Setup(ref context);
            context.AddAssetDependency(kSourceCodeGuid, AssetCollection.Flags.SourceDependency);
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

        public bool TryUpgradeFromMasterNode(IMasterNode1 masterNode, out Dictionary<BlockFieldDescriptor, int> blockMap)
        {
            blockMap = null;
            if (!(masterNode is SpriteLitMasterNode1 spriteLitMasterNode))
                return false;

            // Set blockmap
            blockMap = new Dictionary<BlockFieldDescriptor, int>()
            {
                { BlockFields.VertexDescription.Position, 9 },
                { BlockFields.VertexDescription.Normal, 10 },
                { BlockFields.VertexDescription.Tangent, 11 },
                { BlockFields.SurfaceDescriptionLegacy.SpriteColor, 0 },
                { UniversalBlockFields.SurfaceDescription.SpriteMask, 1 },
                { BlockFields.SurfaceDescription.NormalTS, 2 },
                { BlockFields.SurfaceDescription.AlphaClipThreshold, 8 },
            };

            return true;
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
                        { SpriteLitPasses.Lit(target) },
                        { SpriteLitPasses.Normal(target) },
                        // Currently neither of these passes (selection/picking) can be last for the game view for
                        // UI shaders to render correctly. Verify [1352225] before changing this order.
                        { CorePasses._2DSceneSelection(target) },
                        { CorePasses._2DScenePicking(target) },
                        { SpriteLitPasses.Forward },
                    },
                };
                return result;
            }
        }
        #endregion

        #region Passes
        static class SpriteLitPasses
        {
            public static PassDescriptor Lit(UniversalTarget target)
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
                    renderStates = CoreRenderStates.Default,
                    pragmas = CorePragmas._2DDefault,
                    defines = new DefineCollection(),
                    keywords = SpriteLitKeywords.Lit,
                    includes = SpriteLitIncludes.Lit,

                    // Custom Interpolator Support
                    customInterpolators = CoreCustomInterpDescriptors.Common
                };

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
                    validPixelBlocks = SpriteLitBlockMasks.FragmentForwardNormal,

                    // Fields
                    structs = CoreStructCollections.Default,
                    requiredFields = SpriteLitRequiredFields.Normal,
                    fieldDependencies = CoreFieldDependencies.Default,

                    // Conditional State
                    renderStates = CoreRenderStates.Default,
                    pragmas = CorePragmas._2DDefault,
                    defines = new DefineCollection(),
                    includes = SpriteLitIncludes.Normal,

                    // Custom Interpolator Support
                    customInterpolators = CoreCustomInterpDescriptors.Common
                };

                SpriteSubTargetUtility.AddAlphaClipControlToPass(ref result, target);

                return result;
            }

            public static PassDescriptor Forward = new PassDescriptor
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
                validPixelBlocks = SpriteLitBlockMasks.FragmentForwardNormal,

                // Fields
                structs = CoreStructCollections.Default,
                requiredFields = SpriteLitRequiredFields.Forward,
                keywords = SpriteLitKeywords.Forward,
                fieldDependencies = CoreFieldDependencies.Default,

                // Conditional State
                renderStates = CoreRenderStates.Default,
                pragmas = CorePragmas._2DDefault,
                includes = SpriteLitIncludes.Forward,

                // Custom Interpolator Support
                customInterpolators = CoreCustomInterpDescriptors.Common
            };
        }
        #endregion

        #region PortMasks
        static class SpriteLitBlockMasks
        {
            public static BlockFieldDescriptor[] FragmentLit = new BlockFieldDescriptor[]
            {
                BlockFields.SurfaceDescription.BaseColor,
                BlockFields.SurfaceDescriptionLegacy.SpriteColor,
                BlockFields.SurfaceDescription.Alpha,
                UniversalBlockFields.SurfaceDescription.SpriteMask,
                BlockFields.SurfaceDescription.AlphaClipThreshold,
            };

            public static BlockFieldDescriptor[] FragmentForwardNormal = new BlockFieldDescriptor[]
            {
                BlockFields.SurfaceDescription.BaseColor,
                BlockFields.SurfaceDescriptionLegacy.SpriteColor,
                BlockFields.SurfaceDescription.Alpha,
                BlockFields.SurfaceDescription.NormalTS,
                BlockFields.SurfaceDescription.AlphaClipThreshold,
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
            };

            public static FieldCollection Normal = new FieldCollection()
            {
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
        static class SpriteLitKeywords
        {
            public static KeywordCollection Lit = new KeywordCollection
            {
                { CoreKeywordDescriptors.ShapeLightType0 },
                { CoreKeywordDescriptors.ShapeLightType1 },
                { CoreKeywordDescriptors.ShapeLightType2 },
                { CoreKeywordDescriptors.ShapeLightType3 },
                { CoreKeywordDescriptors.DebugDisplay },
            };

            public static KeywordCollection Forward = new KeywordCollection
            {
                { CoreKeywordDescriptors.DebugDisplay },
            };
        }
        #endregion

        #region Includes
        static class SpriteLitIncludes
        {
            const string k2DLightingUtil = "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/LightingUtility.hlsl";
            const string k2DNormal = "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/NormalsRenderingShared.hlsl";
            const string kSpriteLitPass = "Packages/com.unity.render-pipelines.universal/Editor/2D/ShaderGraph/Includes/SpriteLitPass.hlsl";
            const string kSpriteNormalPass = "Packages/com.unity.render-pipelines.universal/Editor/2D/ShaderGraph/Includes/SpriteNormalPass.hlsl";
            const string kSpriteForwardPass = "Packages/com.unity.render-pipelines.universal/Editor/2D/ShaderGraph/Includes/SpriteForwardPass.hlsl";

            public static IncludeCollection Lit = new IncludeCollection
            {
                // Pre-graph
                { CoreIncludes.CorePregraph },
                { CoreIncludes.ShaderGraphPregraph },
                { k2DLightingUtil, IncludeLocation.Pregraph },

                // Post-graph
                { CoreIncludes.CorePostgraph },
                { kSpriteLitPass, IncludeLocation.Postgraph },
            };

            public static IncludeCollection Normal = new IncludeCollection
            {
                // Pre-graph
                { CoreIncludes.CorePregraph },
                { CoreIncludes.ShaderGraphPregraph },
                { k2DNormal, IncludeLocation.Pregraph },

                // Post-graph
                { CoreIncludes.CorePostgraph },
                { kSpriteNormalPass, IncludeLocation.Postgraph },
            };

            public static IncludeCollection Forward = new IncludeCollection
            {
                // Pre-graph
                { CoreIncludes.CorePregraph },
                { CoreIncludes.ShaderGraphPregraph },

                // Post-graph
                { CoreIncludes.CorePostgraph },
                { kSpriteForwardPass, IncludeLocation.Postgraph },
            };
        }
        #endregion
    }
}
