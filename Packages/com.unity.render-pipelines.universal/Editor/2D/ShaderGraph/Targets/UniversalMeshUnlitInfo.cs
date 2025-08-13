using UnityEditor.ShaderGraph;

namespace UnityEditor.Rendering.Universal.ShaderGraph
{
    internal static class MeshUnlitIncludes
    {
        const string kMeshUnlitPass = "Packages/com.unity.render-pipelines.universal/Editor/2D/ShaderGraph/Includes/MeshUnlitPass.hlsl";

        public static IncludeCollection Unlit = new IncludeCollection
        {
            // Pre-graph
            { CoreIncludes.CorePregraph },
            { CoreIncludes.ShaderGraphPregraph },

            // Post-graph
            { CoreIncludes.CorePostgraph },
            { kMeshUnlitPass, IncludeLocation.Postgraph },
        };
    }
}
