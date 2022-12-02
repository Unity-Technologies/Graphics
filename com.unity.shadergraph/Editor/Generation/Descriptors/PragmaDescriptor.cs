using System.Linq;

namespace UnityEditor.ShaderGraph
{
    [GenerationAPI]
    internal struct PragmaDescriptor
    {
        public string value;
    }

    [GenerationAPI]
    internal static class Pragma
    {
        static string GetPlatformList(Platform[] platforms)
        {
            var rendererStrings = platforms.Select(x => x.ToShaderString());
            return string.Join(" ", rendererStrings);
        }

        public static PragmaDescriptor Target(ShaderModel value) => new PragmaDescriptor { value = $"target {value.ToShaderString()}" };
        public static PragmaDescriptor TargetForKeyword(ShaderModel value, string keyword) => new PragmaDescriptor { value = $"target {value.ToShaderString()} {keyword}" };
        public static PragmaDescriptor Vertex(string value) => new PragmaDescriptor { value = $"vertex {value}" };
        public static PragmaDescriptor Fragment(string value) => new PragmaDescriptor { value = $"fragment {value}" };
        public static PragmaDescriptor Geometry(string value) => new PragmaDescriptor { value = $"geometry {value}" };
        public static PragmaDescriptor Hull(string value) => new PragmaDescriptor { value = $"hull {value}" };
        public static PragmaDescriptor Domain(string value) => new PragmaDescriptor { value = $"domain {value}" };
        public static PragmaDescriptor Raytracing(string value) => new PragmaDescriptor { value = $"raytracing {value}" };
        public static PragmaDescriptor Kernel(string value) => new PragmaDescriptor {value = $"kernel {value}"};
        public static PragmaDescriptor OnlyRenderers(Platform[] renderers) => new PragmaDescriptor { value = $"only_renderers {GetPlatformList(renderers)}" };
        public static PragmaDescriptor NeverUseDXC(Platform[] renderers) => new PragmaDescriptor { value = $"never_use_dxc {GetPlatformList(renderers)}" };
        public static PragmaDescriptor ExcludeRenderers(Platform[] renderers) => new PragmaDescriptor { value = $"exclude_renderers {GetPlatformList(renderers)}" };
        public static PragmaDescriptor PreferHlslCC(Platform[] renderers) => new PragmaDescriptor { value = $"prefer_hlslcc {GetPlatformList(renderers)}" };
        public static PragmaDescriptor InstancingOptions(InstancingOptions value) => new PragmaDescriptor { value = $"instancing_options {value.ToShaderString()}" };
        public static PragmaDescriptor MultiCompileInstancing => new PragmaDescriptor { value = "multi_compile_instancing" };
        public static PragmaDescriptor MultiCompileForwardBase => new PragmaDescriptor { value = "multi_compile_fwdbase" };
        public static PragmaDescriptor MultiCompileForwardAddFullShadowsBase => new PragmaDescriptor { value = "multi_compile_fwdadd_fullshadows" };
        public static PragmaDescriptor MultiCompilePrePassFinal => new PragmaDescriptor { value = "multi_compile_prepassfinal" };
        public static PragmaDescriptor MultiCompileShadowCaster => new PragmaDescriptor { value = "multi_compile_shadowcaster" };
        public static PragmaDescriptor DOTSInstancing => new PragmaDescriptor { value = "multi_compile _ DOTS_INSTANCING_ON" };
        public static PragmaDescriptor MultiCompileFog => new PragmaDescriptor { value = "multi_compile_fog" };
        public static PragmaDescriptor EditorSyncCompilation => new PragmaDescriptor { value = "editor_sync_compilation" };
        public static PragmaDescriptor DebugSymbols => new PragmaDescriptor { value = "enable_d3d11_debug_symbols" };
        public static PragmaDescriptor SkipVariants(string[] variants) => new PragmaDescriptor { value = $"skip_variants {string.Join(" ", variants)}" };
    }
}
