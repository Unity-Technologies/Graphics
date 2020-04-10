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

        public override void Setup(ref TargetSetupContext context)
        {
            context.AddAssetDependencyPath(AssetDatabase.GUIDToAssetPath(kAssetGuid));
            context.AddSubShader(SubShaders.SpriteUnlit);
        }

#region SubShader
        static class SubShaders
        {
            public static SubShaderDescriptor SpriteUnlit = new SubShaderDescriptor()
            {
                pipelineTag = UniversalTarget.kPipelineTag,
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
                vertexPorts = SpriteUnlitPortMasks.Vertex,
                pixelPorts = SpriteUnlitPortMasks.Fragment,

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
        static class SpriteUnlitPortMasks
        {
            public static int[] Vertex = new int[]
            {
                SpriteUnlitMasterNode.PositionSlotId,
                SpriteUnlitMasterNode.VertNormalSlotId,
                SpriteUnlitMasterNode.VertTangentSlotId
            };

            public static int[] Fragment = new int[]
            {
                SpriteUnlitMasterNode.ColorSlotId,
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
