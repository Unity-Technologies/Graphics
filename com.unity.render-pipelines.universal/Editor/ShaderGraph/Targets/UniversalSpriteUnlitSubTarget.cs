using System;
using System.Linq;
using System.Collections.Generic;
using UnityEditor.ShaderGraph;
using UnityEngine.Rendering;
using UnityEditor.Experimental.Rendering.Universal;

namespace UnityEditor.Rendering.Universal.ShaderGraph
{
    sealed class UniversalSpriteUnlitSubTarget : SubTarget<UniversalTarget>
    {
        const string kAssetGuid = "ed7c0aacec26e9646b45c96fb318e5a3";

        public UniversalSpriteUnlitSubTarget()
        {
            displayName = "Sprite Unlit";
        }

        public override bool IsActive() => true;
        
        public override void Setup(ref TargetSetupContext context)
        {
            context.AddAssetDependencyPath(AssetDatabase.GUIDToAssetPath(kAssetGuid));
            context.AddSubShader(SubShaders.SpriteUnlit);
        }

        public override void GetFields(ref TargetFieldContext context)
        {
            // Surface Type & Blend Mode
            context.AddField(Fields.SurfaceTransparent);
            context.AddField(Fields.BlendAlpha);
        }

        public override void GetActiveBlocks(ref TargetActiveBlockContext context)
        {
            context.AddBlock(BlockFields.SurfaceDescription.Alpha);
        }

        public override void GetPropertiesGUI(ref TargetPropertyGUIContext context, Action onChange, Action<String> registerUndo)
        {
        }
        
#region SubShader
        static class SubShaders
        {
            public static SubShaderDescriptor SpriteUnlit = new SubShaderDescriptor()
            {
                pipelineTag = UniversalTarget.kPipelineTag,
                renderType = $"{RenderType.Transparent}",
                renderQueue = $"{UnityEditor.ShaderGraph.RenderQueue.Transparent}",
                generatesPreview = true,
                passes = new PassCollection
                {
                    { SpriteUnlitPasses.Unlit },
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
                referenceName = "SHADERPASS_SPRITEUNLIT",
                useInPreview = true,

                // Template
                passTemplatePath = GenerationUtils.GetDefaultTemplatePath("PassMesh.template"),
                sharedTemplateDirectory = GenerationUtils.GetDefaultSharedTemplateDirectory(),

                // Port Mask
                vertexBlocks = CoreBlockMasks.Vertex,
                pixelBlocks = SpriteUnlitBlockMasks.Fragment,

                // Fields
                structs = CoreStructCollections.Default,
                requiredFields = SpriteUnlitRequiredFields.Unlit,
                fieldDependencies = CoreFieldDependencies.Default,

                // Conditional State
                renderStates = CoreRenderStates.Default,
                pragmas = CorePragmas._2DDefault,
                keywords = SpriteUnlitKeywords.ETCExternalAlpha,
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

#region Keywords
        static class SpriteUnlitKeywords
        {
            public static KeywordCollection ETCExternalAlpha = new KeywordCollection
            {
                { CoreKeywordDescriptors.ETCExternalAlpha },
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
