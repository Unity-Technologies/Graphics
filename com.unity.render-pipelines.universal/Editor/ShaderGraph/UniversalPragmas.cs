using UnityEditor.ShaderGraph;

namespace UnityEditor.Rendering.Universal.ShaderGraph
{
    static class UniversalPragmas
    {
        public static readonly PragmaCollection Default = new PragmaCollection
        {
            { Pragma.Target(2.0) },
            { Pragma.ExcludeRenderers(new Platform[]{ Platform.D3D9 }) },
            { Pragma.PreferHlslCC(new Platform[]{ Platform.GLES }) },
            { Pragma.Vertex("vert") },
            { Pragma.Fragment("frag") },
        };

        public static readonly PragmaCollection Instanced = new PragmaCollection
        {
            { Pragma.Target(2.0) },
            { Pragma.ExcludeRenderers(new Platform[]{ Platform.D3D9 }) },
            { Pragma.MultiCompileInstancing },
            { Pragma.PreferHlslCC(new Platform[]{ Platform.GLES }) },
            { Pragma.Vertex("vert") },
            { Pragma.Fragment("frag") },
        };

        public static readonly PragmaCollection Forward = new PragmaCollection
        {
            { Pragma.Target(2.0) },
            { Pragma.ExcludeRenderers(new Platform[]{ Platform.D3D9 }) },
            { Pragma.MultiCompileInstancing },
            { Pragma.MultiCompileFog },
            { Pragma.PreferHlslCC(new Platform[]{ Platform.GLES }) },
            { Pragma.Vertex("vert") },
            { Pragma.Fragment("frag") },
        };
    }
}
