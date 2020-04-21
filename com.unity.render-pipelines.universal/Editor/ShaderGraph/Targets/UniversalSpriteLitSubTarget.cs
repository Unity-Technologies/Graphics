using System;
using System.Linq;
using System.Collections.Generic;
using UnityEditor.ShaderGraph;
using UnityEngine.Rendering;
using UnityEditor.Experimental.Rendering.Universal;

namespace UnityEditor.Rendering.Universal.ShaderGraph
{
    sealed class UniversalSpriteLitSubTarget : SubTarget<UniversalTarget>
    {
        const string kAssetGuid = "ea1514729d7120344b27dcd67fbf34de";

        public override void Setup(ref TargetSetupContext context)
        {
            context.AddAssetDependencyPath(AssetDatabase.GUIDToAssetPath(kAssetGuid));
            context.AddSubShader(SubShaders.SpriteLit);
        }

#region SubShader
        static class SubShaders
        {
            public static SubShaderDescriptor SpriteLit = new SubShaderDescriptor()
            {
                pipelineTag = UniversalTarget.kPipelineTag,
                generatesPreview = true,
                passes = new PassCollection
                {
                    { SpriteLitPasses.Lit },
                    { SpriteLitPasses.Normal },
                    { SpriteLitPasses.Forward },
                },
            };
        }
#endregion

#region Passes
        static class SpriteLitPasses
        {
            public static PassDescriptor Lit = new PassDescriptor
            {
                // Definition
                displayName = "Sprite Lit",
                referenceName = "SHADERPASS_SPRITELIT",
                lightMode = "Universal2D",
                useInPreview = true,

                // Template
                passTemplatePath = GenerationUtils.GetDefaultTemplatePath("PassMesh.template"),
                sharedTemplateDirectory = GenerationUtils.GetDefaultSharedTemplateDirectory(),

                // Port Mask
                vertexPorts = SpriteLitPortMasks.Vertex,
                pixelPorts = SpriteLitPortMasks.FragmentLit,

                // Fields
                structs = CoreStructCollections.Default,
                requiredFields = SpriteLitRequiredFields.Lit,
                fieldDependencies = CoreFieldDependencies.Default,

                // Conditional State
                renderStates = CoreRenderStates.Default,
                pragmas = CorePragmas._2DDefault,
                keywords = SpriteLitKeywords.Lit,
                includes = SpriteLitIncludes.Lit,
            };

            public static PassDescriptor Normal = new PassDescriptor
            {
                // Definition
                displayName = "Sprite Normal",
                referenceName = "SHADERPASS_SPRITENORMAL",
                lightMode = "NormalsRendering",
                useInPreview = true,

                // Template
                passTemplatePath = GenerationUtils.GetDefaultTemplatePath("PassMesh.template"),
                sharedTemplateDirectory = GenerationUtils.GetDefaultSharedTemplateDirectory(),

                // Port Mask
                vertexPorts = SpriteLitPortMasks.Vertex,
                pixelPorts = SpriteLitPortMasks.FragmentForwardNormal,

                // Fields
                structs = CoreStructCollections.Default,
                requiredFields = SpriteLitRequiredFields.Normal,
                fieldDependencies = CoreFieldDependencies.Default,

                // Conditional State
                renderStates = CoreRenderStates.Default,
                pragmas = CorePragmas._2DDefault,
                includes = SpriteLitIncludes.Normal,
            };

            public static PassDescriptor Forward = new PassDescriptor
            {
                // Definition
                displayName = "Sprite Forward",
                referenceName = "SHADERPASS_SPRITEFORWARD",
                lightMode = "UniversalForward",
                useInPreview = true,

                // Template
                passTemplatePath = GenerationUtils.GetDefaultTemplatePath("PassMesh.template"),
                sharedTemplateDirectory = GenerationUtils.GetDefaultSharedTemplateDirectory(),

                // Port Mask
                vertexPorts = SpriteLitPortMasks.Vertex,
                pixelPorts = SpriteLitPortMasks.FragmentForwardNormal,

                // Fields
                structs = CoreStructCollections.Default,
                requiredFields = SpriteLitRequiredFields.Forward,
                fieldDependencies = CoreFieldDependencies.Default,

                // Conditional State
                renderStates = CoreRenderStates.Default,
                pragmas = CorePragmas._2DDefault,
                keywords = SpriteLitKeywords.ETCExternalAlpha,
                includes = SpriteLitIncludes.Forward,
            };
        }
#endregion

#region PortMasks
        static class SpriteLitPortMasks
        {
            public static int[] Vertex = new int[]
            {
                SpriteLitMasterNode.PositionSlotId,
                SpriteLitMasterNode.VertNormalSlotId,
                SpriteLitMasterNode.VertTangentSlotId,
            };

            public static int[] FragmentLit = new int[]
            {
                SpriteLitMasterNode.ColorSlotId,
                SpriteLitMasterNode.MaskSlotId,
            };

            public static int[] FragmentForwardNormal = new int[]
            {
                SpriteLitMasterNode.ColorSlotId,
                SpriteLitMasterNode.NormalSlotId,
            };
        }
#endregion

#region RequiredFields
        static class SpriteLitRequiredFields
        {
            public static FieldCollection Lit = new FieldCollection()
            {
                StructFields.Varyings.color,
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
                StructFields.Varyings.texCoord0,
            };
        }
#endregion

#region Keywords
        static class SpriteLitKeywords
        {
            public static KeywordCollection Lit = new KeywordCollection
            {
                { CoreKeywordDescriptors.ETCExternalAlpha },
                { CoreKeywordDescriptors.ShapeLightType0 },
                { CoreKeywordDescriptors.ShapeLightType1 },
                { CoreKeywordDescriptors.ShapeLightType2 },
                { CoreKeywordDescriptors.ShapeLightType3 },
            };

            public static KeywordCollection ETCExternalAlpha = new KeywordCollection
            {
                { CoreKeywordDescriptors.ETCExternalAlpha },
            };
        }
#endregion

#region Includes
        static class SpriteLitIncludes
        {
            const string k2DLightingUtil = "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/LightingUtility.hlsl";
            const string k2DNormal = "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/NormalsRenderingShared.hlsl";
            const string kSpriteLitPass = "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/SpriteLitPass.hlsl";
            const string kSpriteNormalPass = "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/SpriteNormalPass.hlsl";
            const string kSpriteForwardPass = "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/SpriteForwardPass.hlsl";

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
