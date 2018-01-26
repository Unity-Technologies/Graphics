using System.Collections.Generic;
using System.IO;
using UnityEditor.Experimental.Rendering.HDPipeline;
using UnityEditor.Experimental.ShaderTools.Internal;
using UnityEngine;

namespace UnityEditor.Experimental.ShaderTools.PSSLInternal
{
    static class PS4Utility
    {
        
        static readonly HashSet<string> k_DefaultCompileDefines = new HashSet<string>
        {
            "UNITY_VERSION=" + ShaderCompilationUtility.unityVersionForShader,
            "SHADER_API_PSSL=1"
        };
        static readonly HashSet<string> k_DefaultIncludes = new HashSet<string>()
        {
            
        };
        static HashSet<string> k_RecursiveIncludes = new HashSet<string>
        {
            new DirectoryInfo(HDEditorUtils.GetHDRenderPipelinePath()).Parent.ToString(),
            new DirectoryInfo(HDEditorUtils.GetCorePath()).Parent.ToString()
        };
        static Dictionary<string, HashSet<string>> s_ResolvedRecursiveIncludes = new Dictionary<string, HashSet<string>>();

        public static DirectoryInfo currentSourceDir { get { return new DirectoryInfo(Application.dataPath).Parent;} }

        public const string k_DefineFragment = "SHADER_STAGE_FRAGMENT=1";
        public const string k_DefineCompute = "SHADER_STAGE_COMPUTE=1";

        public static ShaderCompilerOptions DefaultHDRPCompileOptions(IEnumerable<string> defines, string entry, DirectoryInfo sourceDir, string shaderModel = null)
        {
            if (!s_ResolvedRecursiveIncludes.ContainsKey(sourceDir.FullName))
            {
                var resolvedIncludes = new HashSet<string>();
                s_ResolvedRecursiveIncludes[sourceDir.FullName] = resolvedIncludes;

                foreach (var include in k_DefaultIncludes)
                {
                    var d = Path.IsPathRooted(include)
                        ? new DirectoryInfo(include).FullName
                        : new DirectoryInfo(Path.Combine(sourceDir.FullName, include)).FullName;
                    resolvedIncludes.Add(d);
                }

                var stack = new Stack<string>();
                foreach (var recursiveInclude in k_RecursiveIncludes)
                {
                    var d = Path.IsPathRooted(recursiveInclude)
                        ? new DirectoryInfo(recursiveInclude).FullName
                        : new DirectoryInfo(Path.Combine(sourceDir.FullName, recursiveInclude)).FullName;
                    stack.Push(d);
                }
                while (stack.Count > 0)
                {
                    var dir = stack.Pop();
                    resolvedIncludes.Add(dir);
                    foreach (var subdir in Directory.GetDirectories(dir))
                        stack.Push(subdir);
                }
            }

            var compileOptions = new ShaderCompilerOptions
            {
                includeFolders = new HashSet<string>(s_ResolvedRecursiveIncludes[sourceDir.FullName]),
                defines = new HashSet<string>(),
                entry = entry
            };
            compileOptions.defines.UnionWith(k_DefaultCompileDefines);
            compileOptions.defines.UnionWith(defines);
            if (!string.IsNullOrEmpty(shaderModel))
                compileOptions.defines.Add(string.Format("SHADER_TARGET={0}", shaderModel));

            var path = Path.Combine(EditorApplication.applicationContentsPath, "CGIncludes");
            if (Directory.Exists(path))
                compileOptions.includeFolders.Add(path);

            // TODO: Use a robust way to find the PS4Player directory
            path = Path.Combine(EditorApplication.applicationContentsPath, "../../PS4Player/CgBatchPlugins64/include");
            if (Directory.Exists(path))
                compileOptions.includeFolders.Add(path);

            return compileOptions;
        }

        public static void OpenInSCUI(this ShaderBuildReport.CompileUnit cu)
        {
            var scui = new SCUI();
            scui.Initialize();

            var tmpSourceFile = cu.CreateTemporarySourceCodeFile();
            OrbisWavePSSLC.FixupSource(tmpSourceFile, tmpSourceFile);

            var options = DefaultHDRPCompileOptions(cu.defines, cu.entry, currentSourceDir);
            scui.Open(tmpSourceFile, currentSourceDir, options, cu.profile);
        }

        public static void DiffCUInSCUI(ShaderBuildReport.CompileUnit firstCU, DirectoryInfo firstSourceDir, ShaderBuildReport.CompileUnit secondCU, DirectoryInfo secondSourceDir)
        {
            var scui = new SCUI();
            scui.Initialize();

            var firstSourceFile = firstCU.CreateTemporarySourceCodeFile("source");
            var secondSourceFile = secondCU.CreateTemporarySourceCodeFile("reference");
            OrbisWavePSSLC.FixupSource(firstSourceFile, firstSourceFile);
            OrbisWavePSSLC.FixupSource(secondSourceFile, secondSourceFile);

            var firstOptions = DefaultHDRPCompileOptions(firstCU.defines, firstCU.entry, firstSourceDir);
            var secondOptions = DefaultHDRPCompileOptions(secondCU.defines, secondCU.entry, secondSourceDir);

            scui.OpenDiff(firstSourceFile, firstOptions, firstSourceDir, secondSourceFile, secondOptions, secondSourceDir, firstCU.profile);
        }
    }
}
