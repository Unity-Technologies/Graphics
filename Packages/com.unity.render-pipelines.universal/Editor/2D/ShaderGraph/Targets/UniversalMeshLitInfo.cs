using UnityEditor.ShaderGraph;

namespace UnityEditor.Rendering.Universal.ShaderGraph
{
    internal static class UniversalMeshLitInfo
    {
        public static class Includes
        {
            const string k2DNormal = "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/NormalsRenderingShared.hlsl";
            const string kMeshLitPass = "Packages/com.unity.render-pipelines.universal/Editor/2D/ShaderGraph/Includes/Mesh2DLitPass.hlsl";
            const string kMeshNormalPass = "Packages/com.unity.render-pipelines.universal/Editor/2D/ShaderGraph/Includes/MeshNormalPass.hlsl";
            const string kMeshForwardPass = "Packages/com.unity.render-pipelines.universal/Editor/2D/ShaderGraph/Includes/SpriteForwardPass.hlsl";

            public static IncludeCollection Lit = new IncludeCollection
            {
                // Pre-graph
                { CoreIncludes.CorePregraph },
                { CoreIncludes.ShaderGraphPregraph },

                // Post-graph
                { CoreIncludes.CorePostgraph },
                { kMeshLitPass, IncludeLocation.Postgraph },
            };

            public static IncludeCollection Normal = new IncludeCollection
            {
                // Pre-graph
                { CoreIncludes.CorePregraph },
                { CoreIncludes.ShaderGraphPregraph },
                { k2DNormal, IncludeLocation.Pregraph },

                // Post-graph
                { CoreIncludes.CorePostgraph },
                { kMeshNormalPass, IncludeLocation.Postgraph },
            };

            public static IncludeCollection Forward = new IncludeCollection
            {
                // Pre-graph
                { CoreIncludes.CorePregraph },
                { CoreIncludes.ShaderGraphPregraph },

                // Post-graph
                { CoreIncludes.CorePostgraph },
                { kMeshForwardPass, IncludeLocation.Postgraph },
            };
        }
    }
}
