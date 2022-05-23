using System;
using System.Linq;
using System.Collections.Generic;
using UnityEditor.ShaderGraph;
using UnityEditor.ShaderGraph.Legacy;

using Unity.Rendering.Universal;

namespace UnityEditor.Rendering.Universal.ShaderGraph
{
    sealed class UniversalSpriteUnlitSubTarget : UniversalSubTarget, ILegacyTarget
    {
        static readonly GUID kSourceCodeGuid = new GUID("ed7c0aacec26e9646b45c96fb318e5a3"); // UniversalSpriteUnlitSubTarget.cs

        public UniversalSpriteUnlitSubTarget()
        {
            displayName = "Sprite Unlit";
        }

        protected override ShaderUtils.ShaderID shaderID => ShaderUtils.ShaderID.SG_SpriteUnlit;

        public override bool IsActive() => true;

        public override void Setup(ref TargetSetupContext context)
        {
            base.Setup(ref context);
            context.AddAssetDependency(kSourceCodeGuid, AssetCollection.Flags.SourceDependency);
            context.AddSubShader(PostProcessSubShader(SubShaders.SpriteUnlit(target)));
        }

        public override void GetFields(ref TargetFieldContext context)
        {
            base.GetFields(ref context);

            SpriteSubTargetUtility.AddDefaultFields(ref context, target);
        }

        public override void GetActiveBlocks(ref TargetActiveBlockContext context)
        {
            SpriteSubTargetUtility.GetDefaultActiveBlocks(ref context, target);
        }

        public override void GetPropertiesGUI(ref TargetPropertyGUIContext context, Action onChange, Action<String> registerUndo)
        {
            SpriteSubTargetUtility.AddDefaultPropertiesGUI(ref context, onChange, registerUndo, target);
        }

        public bool TryUpgradeFromMasterNode(IMasterNode1 masterNode, out Dictionary<BlockFieldDescriptor, int> blockMap)
        {
            blockMap = null;
            if (!(masterNode is SpriteUnlitMasterNode1 spriteUnlitMasterNode))
                return false;

            // Set blockmap
            blockMap = new Dictionary<BlockFieldDescriptor, int>()
            {
                { BlockFields.VertexDescription.Position, 9 },
                { BlockFields.VertexDescription.Normal, 10 },
                { BlockFields.VertexDescription.Tangent, 11 },
                { BlockFields.SurfaceDescriptionLegacy.SpriteColor, 0 },
                { BlockFields.SurfaceDescription.AlphaClipThreshold, 8 },
            };

            return true;
        }

        #region SubShader
        static class SubShaders
        {
            public static SubShaderDescriptor SpriteUnlit(UniversalTarget target)
            {
                SubShaderDescriptor result = new SubShaderDescriptor()
                {
                    pipelineTag = UniversalTarget.kPipelineTag,
                    customTags = UniversalTarget.kUnlitMaterialTypeTag,
                    renderType = $"{RenderType.Transparent}",
                    renderQueue = $"{UnityEditor.ShaderGraph.RenderQueue.Transparent}",
                    generatesPreview = true,
                    passes = new PassCollection
                    {
                        { SpriteUnlitPasses.Unlit(target) },
                        // Currently neither of these passes (selection/picking) can be last for the game view for
                        // UI shaders to render correctly. Verify [1352225] before changing this order.
                        { CorePasses._2DSceneSelection(target) },
                        { CorePasses._2DScenePicking(target) },
                        { SpriteUnlitPasses.Forward },
                    },
                };
                return result;
            }
        }
        #endregion

        #region Passes
        static class SpriteUnlitPasses
        {
            public static PassDescriptor Unlit(UniversalTarget target)
            {
                var result = new PassDescriptor()
                {

                    // Definition
                    displayName = "Sprite Unlit",
                    referenceName = "SHADERPASS_SPRITEUNLIT",
                    lightMode = "Universal2D",
                    useInPreview = true,

                    // Template
                    passTemplatePath = UniversalTarget.kUberTemplatePath,
                    sharedTemplateDirectories = UniversalTarget.kSharedTemplateDirectories,

                    // Port Mask
                    validVertexBlocks = CoreBlockMasks.Vertex,
                    validPixelBlocks = SpriteUnlitBlockMasks.Fragment,

                    // Fields
                    structs = CoreStructCollections.Default,
                    requiredFields = SpriteUnlitRequiredFields.Unlit,
                    fieldDependencies = CoreFieldDependencies.Default,

                    // Conditional State
                    renderStates = CoreRenderStates.Default,
                    pragmas = CorePragmas._2DDefault,
                    defines = new DefineCollection(),
                    keywords = SpriteUnlitKeywords.Unlit,
                    includes = SpriteUnlitIncludes.Unlit,

                    // Custom Interpolator Support
                    customInterpolators = CoreCustomInterpDescriptors.Common
                };

                SpriteSubTargetUtility.AddAlphaClipControlToPass(ref result, target);

                return result;
            }

            public static PassDescriptor Forward = new PassDescriptor
            {
                // Definition
                displayName = "Sprite Unlit",
                referenceName = "SHADERPASS_SPRITEFORWARD",
                lightMode = "UniversalForward",
                useInPreview = true,

                // Template
                passTemplatePath = UniversalTarget.kUberTemplatePath,
                sharedTemplateDirectories = UniversalTarget.kSharedTemplateDirectories,

                // Port Mask
                validVertexBlocks = CoreBlockMasks.Vertex,
                validPixelBlocks = SpriteUnlitBlockMasks.Fragment,

                // Fields
                structs = CoreStructCollections.Default,
                requiredFields = SpriteUnlitRequiredFields.Unlit,
                fieldDependencies = CoreFieldDependencies.Default,

                // Conditional State
                renderStates = CoreRenderStates.Default,
                pragmas = CorePragmas._2DDefault,
                keywords = SpriteUnlitKeywords.Unlit,
                includes = SpriteUnlitIncludes.Unlit,

                // Custom Interpolator Support
                customInterpolators = CoreCustomInterpDescriptors.Common
            };
        }
        #endregion

        #region PortMasks
        static class SpriteUnlitBlockMasks
        {
            public static BlockFieldDescriptor[] Fragment = new BlockFieldDescriptor[]
            {
                BlockFields.SurfaceDescription.BaseColor,
                BlockFields.SurfaceDescriptionLegacy.SpriteColor,
                BlockFields.SurfaceDescription.Alpha,
                BlockFields.SurfaceDescription.AlphaClipThreshold,
            };
        }
        #endregion

        #region RequiredFields
        static class SpriteUnlitRequiredFields
        {
            public static FieldCollection Unlit = new FieldCollection()
            {
                StructFields.Attributes.color,
                StructFields.Attributes.uv0,
                StructFields.Varyings.positionWS,
                StructFields.Varyings.color,
                StructFields.Varyings.texCoord0,
            };
        }
        #endregion

        #region Keywords
        static class SpriteUnlitKeywords
        {
            public static KeywordCollection Unlit = new KeywordCollection
            {
                { CoreKeywordDescriptors.DebugDisplay },
            };
        }
        #endregion

        #region Includes
        static class SpriteUnlitIncludes
        {
            const string kSpriteUnlitPass = "Packages/com.unity.render-pipelines.universal/Editor/2D/ShaderGraph/Includes/SpriteUnlitPass.hlsl";

            public static IncludeCollection Unlit = new IncludeCollection
            {
                // Pre-graph
                { CoreIncludes.CorePregraph },
                { CoreIncludes.ShaderGraphPregraph },

                // Post-graph
                { CoreIncludes.CorePostgraph },
                { kSpriteUnlitPass, IncludeLocation.Postgraph },
            };
        }
        #endregion
    }
}
