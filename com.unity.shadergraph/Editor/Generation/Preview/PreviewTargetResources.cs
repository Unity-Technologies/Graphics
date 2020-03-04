using UnityEditor.ShaderGraph;

namespace UnityEditor.ShaderGraph
{
    static class PreviewTargetResources
    {
        static KeywordDescriptor s_PreviewKeyword = new KeywordDescriptor()
        {
            displayName = "Preview",
            referenceName = "SHADERGRAPH_PREVIEW",
            type = KeywordType.Boolean,
            definition = KeywordDefinition.MultiCompile,
            scope = KeywordScope.Global,
        };

        static StructDescriptor s_PreviewVaryings = new StructDescriptor()
        {
            name = "Varyings",
            packFields = true,
            fields = new[]
            {
                StructFields.Varyings.positionCS,
                StructFields.Varyings.positionWS,
                StructFields.Varyings.normalWS,
                StructFields.Varyings.tangentWS,
                StructFields.Varyings.texCoord0,
                StructFields.Varyings.texCoord1,
                StructFields.Varyings.texCoord2,
                StructFields.Varyings.texCoord3,
                StructFields.Varyings.color,
                StructFields.Varyings.viewDirectionWS,
                StructFields.Varyings.screenPosition,
                StructFields.Varyings.instanceID,
                StructFields.Varyings.cullFace,
            }
        };

        static PassDescriptor s_PreviewPass = new PassDescriptor()
        {
            // Definition
            referenceName = "SHADERPASS_PREVIEW",
            useInPreview = true,

            // Collections
            structs = new StructCollection
            {
                { Structs.Attributes },
                { s_PreviewVaryings },
                { Structs.SurfaceDescriptionInputs },
                { Structs.VertexDescriptionInputs },
            },
            fieldDependencies = FieldDependencies.Default,
            pragmas = new PragmaCollection
            {
                { Pragma.Vertex("vert") },
                { Pragma.Fragment("frag") },
            },
            defines = new DefineCollection
            {
                { s_PreviewKeyword, 1 },
            },
            includes = new IncludeCollection
            {
                // Pre-graph
                { "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl", IncludeLocation.Pregraph },
                { "Packages/com.unity.render-pipelines.core/ShaderLibrary/Packing.hlsl", IncludeLocation.Pregraph },
                { "Packages/com.unity.render-pipelines.core/ShaderLibrary/NormalSurfaceGradient.hlsl", IncludeLocation.Pregraph },
                { "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl", IncludeLocation.Pregraph },
                { "Packages/com.unity.render-pipelines.core/ShaderLibrary/UnityInstancing.hlsl", IncludeLocation.Pregraph },
                { "Packages/com.unity.render-pipelines.core/ShaderLibrary/EntityLighting.hlsl", IncludeLocation.Pregraph },
                { "Packages/com.unity.shadergraph/ShaderGraphLibrary/ShaderVariables.hlsl", IncludeLocation.Pregraph },
                { "Packages/com.unity.shadergraph/ShaderGraphLibrary/ShaderVariablesFunctions.hlsl", IncludeLocation.Pregraph },
                { "Packages/com.unity.shadergraph/ShaderGraphLibrary/Functions.hlsl", IncludeLocation.Pregraph },

                // Post-graph
                { "Packages/com.unity.shadergraph/ShaderGraphLibrary/PreviewVaryings.hlsl", IncludeLocation.Postgraph },
                { "Packages/com.unity.shadergraph/ShaderGraphLibrary/PreviewPass.hlsl", IncludeLocation.Postgraph },
            }
        };

        public static SubShaderDescriptor PreviewSubShader = new SubShaderDescriptor()
        {
            renderQueueOverride = "Geometry",
            renderTypeOverride = "Opaque",
            generatesPreview = true,
            passes = new PassCollection { s_PreviewPass },
        };
    }
}
