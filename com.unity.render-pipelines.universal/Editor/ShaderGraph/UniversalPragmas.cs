using UnityEditor.ShaderGraph;

namespace UnityEditor.Rendering.Universal.ShaderGraph
{
    static class UniversalPragmas
    {
        public static readonly PragmaCollection Default = new PragmaCollection
        {
            { Pragma.Target(ShaderModel.Target20) },
            { Pragma.OnlyRenderers(new[]{ Platform.GLES }) },
            { Pragma.PreferHlslCC(new[]{ Platform.GLES }) },
            { Pragma.Vertex("vert") },
            { Pragma.Fragment("frag") },
        };

        public static readonly PragmaCollection _2DDefault = new PragmaCollection
        {
            { Pragma.Target(ShaderModel.Target20) },
            { Pragma.ExcludeRenderers(new[]{ Platform.D3D9 }) },
            { Pragma.PreferHlslCC(new[]{ Platform.GLES }) },
            { Pragma.Vertex("vert") },
            { Pragma.Fragment("frag") },
        };

        public static readonly PragmaCollection Instanced = new PragmaCollection
        {
            { Pragma.Target(ShaderModel.Target20) },
            { Pragma.OnlyRenderers(new[]{ Platform.GLES }) },
            { Pragma.MultiCompileInstancing },
            { Pragma.PreferHlslCC(new[]{ Platform.GLES }) },
            { Pragma.Vertex("vert") },
            { Pragma.Fragment("frag") },
        };

        public static readonly PragmaCollection Forward = new PragmaCollection
        {
            { Pragma.Target(ShaderModel.Target20) },
            { Pragma.OnlyRenderers(new[]{ Platform.GLES }) },
            { Pragma.MultiCompileInstancing },
            { Pragma.MultiCompileFog },
            { Pragma.PreferHlslCC(new[]{ Platform.GLES }) },
            { Pragma.Vertex("vert") },
            { Pragma.Fragment("frag") },
        };

        public static readonly PragmaCollection DOTSDefault = new PragmaCollection
        {
            { Pragma.Target(ShaderModel.Target45) },
            { Pragma.ExcludeRenderers(new[]{ Platform.D3D9, Platform.GLES }) },
            { Pragma.Vertex("vert") },
            { Pragma.Fragment("frag") },
        };

        public static readonly PragmaCollection DOTSInstanced = new PragmaCollection
        {
            { Pragma.Target(ShaderModel.Target45) },
            { Pragma.ExcludeRenderers(new[]{ Platform.D3D9, Platform.GLES }) },
            { Pragma.MultiCompileInstancing },
            { Pragma.DOTSInstancing },
            { Pragma.Vertex("vert") },
            { Pragma.Fragment("frag") },
        };

        public static readonly PragmaCollection DOTSForward = new PragmaCollection
        {
            { Pragma.Target(ShaderModel.Target45) },
            { Pragma.ExcludeRenderers(new[]{ Platform.D3D9, Platform.GLES }) },
            { Pragma.MultiCompileInstancing },
            { Pragma.MultiCompileFog },
            { Pragma.DOTSInstancing },
            { Pragma.Vertex("vert") },
            { Pragma.Fragment("frag") },
        };
    }
}
