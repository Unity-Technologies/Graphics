using System;
using System.Linq;
using System.Collections.Generic;
using UnityEditor.ShaderGraph;
using UnityEngine.Rendering;
using UnityEditor.Experimental.Rendering.Universal;
using UnityEditor.ShaderGraph.Legacy;

namespace UnityEditor.Rendering.Universal.ShaderGraph
{
    sealed class UniversalSpriteUnlitSubTarget : SubTarget<UniversalTarget>, ILegacyTarget
    {
        static readonly GUID kSourceCodeGuid = new GUID("ed7c0aacec26e9646b45c96fb318e5a3"); // UniversalSpriteUnlitSubTarget.cs

        public UniversalSpriteUnlitSubTarget()
        {
            displayName = "Sprite Unlit";
        }

        public override bool IsActive() => true;

        public override void Setup(ref TargetSetupContext context)
        {
            context.AddAssetDependency(kSourceCodeGuid, AssetCollection.Flags.SourceDependency);
            context.AddSubShader(SubShaders.SpriteUnlit);
        }

        public override void GetFields(ref TargetFieldContext context)
        {
            var descs = context.blocks.Select(x => x.descriptor);
            // Only support SpriteColor legacy block if BaseColor/Alpha are not active
            bool useLegacyBlocks = !descs.Contains(BlockFields.SurfaceDescription.BaseColor) && !descs.Contains(BlockFields.SurfaceDescription.Alpha);
            context.AddField(CoreFields.UseLegacySpriteBlocks, useLegacyBlocks);

            // Surface Type & Blend Mode
            context.AddField(UniversalFields.SurfaceTransparent);
            context.AddField(Fields.BlendAlpha);
            context.AddField(Fields.DoubleSided);
        }

        public override void GetActiveBlocks(ref TargetActiveBlockContext context)
        {
            // Only support SpriteColor legacy block if BaseColor/Alpha are not active
            bool useLegacyBlocks = !context.currentBlocks.Contains(BlockFields.SurfaceDescription.BaseColor) && !context.currentBlocks.Contains(BlockFields.SurfaceDescription.Alpha);
            context.AddBlock(BlockFields.SurfaceDescriptionLegacy.SpriteColor, useLegacyBlocks);

            context.AddBlock(BlockFields.SurfaceDescription.Alpha);
        }

        public override void GetPropertiesGUI(ref TargetPropertyGUIContext context, Action onChange, Action<String> registerUndo)
        {
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
            };

            return true;
        }

        #region SubShader
        static class SubShaders
        {
            public static SubShaderDescriptor SpriteUnlit = new SubShaderDescriptor()
            {
                pipelineTag = UniversalTarget.kPipelineTag,
                customTags = UniversalTarget.kUnlitMaterialTypeTag,
                renderType = $"{RenderType.Transparent}",
                renderQueue = $"{UnityEditor.ShaderGraph.RenderQueue.Transparent}",
                generatesPreview = true,
                passes = new PassCollection
                {
                    { SpriteUnlitPasses.Unlit },
                    { SpriteUnlitPasses.Forward },
                },
            };
        }
        #endregion

        #region Passes
        static class SpriteUnlitPasses
        {
            public static PassDescriptor Unlit = new PassDescriptor
            {
                // Definition
                displayName = "Sprite Unlit",
                referenceName = "SHADERPASS_SPRITEUNLIT",
                lightMode = "Universal2D",
                useInPreview = true,


                // Template
                passTemplatePath = GenerationUtils.GetDefaultTemplatePath("PassMesh.template"),
                sharedTemplateDirectories = GenerationUtils.GetDefaultSharedTemplateDirectories(),

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
                includes = SpriteUnlitIncludes.Unlit,
            };

            public static PassDescriptor Forward = new PassDescriptor
            {
                // Definition
                displayName = "Sprite Unlit",
                referenceName = "SHADERPASS_SPRITEFORWARD",
                lightMode = "UniversalForward",
                useInPreview = true,


                // Template
                passTemplatePath = GenerationUtils.GetDefaultTemplatePath("PassMesh.template"),
                sharedTemplateDirectories = GenerationUtils.GetDefaultSharedTemplateDirectories(),

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
                includes = SpriteUnlitIncludes.Unlit,
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
                StructFields.Varyings.color,
                StructFields.Varyings.texCoord0,
            };
        }
        #endregion

        #region Includes
        static class SpriteUnlitIncludes
        {
            const string kSpriteUnlitPass = "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/SpriteUnlitPass.hlsl";

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
