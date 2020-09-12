using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Assertions;
using Object = UnityEngine.Object;

namespace UnityEditor.ShaderAnalysis.Internal
{
    public static class ShaderAnalysisUtils
    {
        ////#pragma target 4.5
        static readonly Regex k_RegexShaderModel = new Regex(@"^[\s\r]*#pragma\s+target\s+([\d\.]+)\s+[\s\r]*$");
        static readonly Regex k_RegexMultiCompile = new Regex(@"^[\s\r]*(#pragma\s+multi_compile\s+(?:([\w_]+)\s+)+)[\s\r]*$");
        static readonly Regex k_RegexPragmaKernel = new Regex(@"#pragma kernel ([\w_]+)(?:\s+(\S+))*$");

        static Dictionary<string, Dictionary<BuildTarget, AssetMetadata>> s_AssetMetadatas = new Dictionary<string, Dictionary<BuildTarget, AssetMetadata>>();

        #region Folder Layout

        public static DirectoryInfo GetTemporaryDirectory(Object asset, BuildTarget target)
        {
            var guid = CalculateGUIDFor(asset);
            return GetTemporaryDirectory(guid, target);
        }

        public static DirectoryInfo GetTemporaryDirectory(string guid, BuildTarget target)
        {
            return new DirectoryInfo(string.Format("Temp/ShaderAnalysis/{0}/{1}", guid, target));
        }

        public static FileInfo GetTemporaryProgramSourceCodeFile(DirectoryInfo folder, int variantNumber)
        {
            return new FileInfo(Path.Combine(folder.FullName, string.Format("Variant{0:D3}.shader", variantNumber)));
        }

        public static FileInfo GetTemporaryProgramCompiledFile(FileInfo sourceFile, DirectoryInfo genDir, string multicompile)
        {
            return new FileInfo(Path.Combine(genDir.FullName, string.Format("{0}.{1}.sb", Path.GetFileNameWithoutExtension(sourceFile.Name), multicompile)));
        }

        public static FileInfo GetTemporaryDisassemblyFile(FileInfo binaryFile)
        {
            return new FileInfo(
                Path.Combine(
                    binaryFile.DirectoryName,
                    Path.GetFileNameWithoutExtension(binaryFile.Name) + ".disassemble.txt"
                )
            );
        }

        public static FileInfo GetTemporaryPerformanceReportFile(FileInfo binaryFile)
        {
            return new FileInfo(
                Path.Combine(
                    binaryFile.DirectoryName,
                    Path.GetFileNameWithoutExtension(binaryFile.Name) + ".perf.txt"
                )
            );
        }

        public static FileInfo GetTemporaryReportFile(Object asset, BuildTarget target)
        {
            return new FileInfo(Path.Combine(
                GetTemporaryDirectory(asset, target).FullName,
                Path.GetFileNameWithoutExtension(asset.name) + ".perf.txt"
            ));
        }

        public static FileInfo GetTemporaryDiffFile(string assetGUID, BuildTarget target)
        {
            return new FileInfo(Path.Combine(
                GetTemporaryDirectory(assetGUID, target).FullName,
                "Diff.txt"
            ));
        }

        public static string CalculateGUIDFor(Object asset)
        {
            if (asset == null || asset.Equals(null)) return string.Empty;
            var assetPath = AssetDatabase.GetAssetPath(asset);
            var guid = string.IsNullOrEmpty(assetPath)
                ? asset.name
                : AssetDatabase.AssetPathToGUID(assetPath);
            return guid;
        }

        public static FileInfo GetWorkingAssetMetaDataBaseFile(DirectoryInfo sourceDir)
        {
            var fileInfo = new FileInfo(Path.Combine(sourceDir.FullName, "AssetMetaDataBase.asset"));
            return fileInfo;
        }
        #endregion

        #region Shader Code

        public static void ParseVariantMultiCompiles(string variantBody, List<HashSet<string>> multicompiles)
        {
            var lines = variantBody.Split('\n');
            for (var i = 0; i < lines.Length; i++)
            {
                var m = k_RegexMultiCompile.Match(lines[i]);
                if (m.Success)
                {
                    var entry = new HashSet<string>();
                    multicompiles.Add(entry);

                    var captures = m.Groups[2].Captures;
                    for (var j = 0; j < captures.Count; j++)
                        entry.Add(captures[j].Value);
                }
            }
        }

        public static void ParseShaderModel(string sourceCode, ref string shaderModel)
        {
            var lines = sourceCode.Split('\n');
            for (var i = 0; i < lines.Length; i++)
            {
                var m = k_RegexShaderModel.Match(lines[i]);
                if (m.Success)
                {
                    shaderModel = m.Groups[1].Value.Replace(".", string.Empty);
                    break;
                }
            }
        }

        public static IEnumerator BuildDefinesFromMultiCompiles(List<HashSet<string>> multicompiles, List<HashSet<string>> defines, ShaderProgramFilter filter)
        {
            if (multicompiles.Count == 0)
            {
                defines.Add(new HashSet<string>());
                yield break;
            }

            var mc = new string[multicompiles.Count][];
            for (var i = 0; i < mc.Length; i++)
            {
                mc[i] = new string[multicompiles[i].Count];
                multicompiles[i].CopyTo(mc[i]);
            }

            // current multicompile index
            var indices = new int[multicompiles.Count];

            while (true)
            {
                // Add an entry with current indices
                var entry = new HashSet<string>();

                for (var i = 0; i < indices.Length; i++)
                {
                    var token = mc[i][indices[i]];
                    if (token == "_")
                        continue;
                    entry.Add(token);
                }

                var success = false;
                if (filter != null && filter.includedKeywords.Count != 0)
                {
                    foreach (var keywordSet in filter.includedKeywords)
                    {
                        if (keywordSet.IsSubsetOf(entry))
                        {
                            success = true;
                            break;
                        }
                    }
                }
                else
                    success = true;

                if (success)
                {
                    defines.Add(entry);
                    yield return defines.Count;
                }

                var incrementIndex = indices.Length - 1;
                while (true)
                {
                    // increment last index
                    ++indices[incrementIndex];
                    if (indices[incrementIndex] >= multicompiles[incrementIndex].Count)
                    {
                        // We have done all multicompiles at this index
                        // So increment previous indices
                        indices[incrementIndex] = 0;
                        --incrementIndex;
                    }
                    else
                        break;

                    // Loop complete
                    if (incrementIndex < 0)
                        break;
                }

                // Loop complete
                if (incrementIndex < 0)
                    break;
            }
        }

        public static void ParseComputeShaderKernels(string computeBody, Dictionary<string, HashSet<string>> kernels)
        {
            var lines = computeBody.Split('\n', '\r');
            for (var i = 0; i < lines.Length; i++)
            {
                var m = k_RegexPragmaKernel.Match(lines[i]);
                if (m.Success)
                {
                    var defines = new HashSet<string>();

                    for (var j = 0; j < m.Groups[2].Captures.Count; j++)
                        defines.Add(m.Groups[2].Captures[j].Value);

                    kernels.Add(m.Groups[1].Value, defines);
                }
            }
        }

        #endregion

        #region Asset Metadata
        static readonly DirectoryInfo k_WorkingAssetMetadataFolder = new DirectoryInfo("Library/ShaderAnalysis");

        public static AssetMetadata LoadAssetMetadatasFor(BuildTarget target, DirectoryInfo rootFolder = null)
        {
            rootFolder = rootFolder ?? k_WorkingAssetMetadataFolder;

            var rootHash = rootFolder.FullName.Replace("\\\\", "/");
            Dictionary<BuildTarget, AssetMetadata> metadatabase;
            if (!s_AssetMetadatas.TryGetValue(rootHash, out metadatabase))
            {
                metadatabase = new Dictionary<BuildTarget, AssetMetadata>();
                s_AssetMetadatas[rootHash] = metadatabase;
            }

            if (metadatabase.TryGetValue(target, out var value) && value != null && !value.Equals(null))
                return value;

            AssetMetadata result = null;
            var file = GetAssetMetadataFileFor(target, rootFolder);
            if (file.Exists)
            {
                var objs = InternalEditorUtility.LoadSerializedFileAndForget(file.FullName);
                if (objs.Length > 0)
                {
                    result = objs[0] as AssetMetadata;
                    result.OnAfterDeserialize();
                }
            }
            if (result == null)
            {
                result = ScriptableObject.CreateInstance<AssetMetadata>();
                result.target = target;
            }

            metadatabase[target] = result;

            return result;
        }

        public static void SaveAssetMetadata(AssetMetadata metadatas, DirectoryInfo rootFolder = null)
        {
            Assert.IsNotNull(metadatas);

            rootFolder = rootFolder ?? k_WorkingAssetMetadataFolder;

            var file = GetAssetMetadataFileFor(metadatas.target, rootFolder);
            if (!file.Directory.Exists)
                file.Directory.Create();
            if (file.Exists)
                file.Delete();
            InternalEditorUtility.SaveToSerializedFileAndForget(new Object[] { metadatas }, file.FullName, true);
        }

        static FileInfo GetAssetMetadataFileFor(BuildTarget target, DirectoryInfo rootFolder)
        {
            return new FileInfo(Path.Combine(rootFolder.FullName, string.Format("AssetMetaData_{0}.asset", target)));
        }
        #endregion

        #region Basic Utils
        public static string Join(this HashSet<string> strings, string separator)
        {
            var builder = new StringBuilder();
            var first = true;
            foreach (var s in strings)
            {
                if (!first)
                    builder.Append(separator);
                first = false;
                builder.Append(s);
            }

            return builder.ToString();
        }

        public static string Join(this List<string> strings, string separator)
        {
            if (strings == null)
                return string.Empty;

            var builder = new StringBuilder();
            var first = true;
            foreach (var s in strings)
            {
                if (!first)
                    builder.Append(separator);
                first = false;
                builder.Append(s);
            }

            return builder.ToString();
        }
        #endregion

        public static ShaderBuildReportDiff DiffReports(ShaderBuildReport source, ShaderBuildReport reference)
        {
            var diff = new ShaderBuildReportDiff
            {
                source = source,
                reference = reference
            };

            var spos = source.programs;
            for (var i = 0; i < spos.Count; i++)
            {
                var spo = spos[i];
                var rpo = reference.GetProgramByName(spo.name);

                if (rpo == null)
                    continue;

                foreach (var scu in spo.compileUnits)
                {
                    var rcu = rpo.GetCompileUnitByDefines(scu.defines);
                    if (rcu == null)
                        continue;

                    var spu = scu.performanceUnit;
                    var rpu = rcu.performanceUnit;

                    if (spu == null || rpu == null)
                        continue;

                    var perfdiff = new ShaderBuildReportDiff.PerfDiff
                    {
                        programName = spo.name,
                        defines = scu.defines,
                        multicompiles = scu.multicompileDefines,
                        metrics = ShaderBuildReport.ProgramPerformanceMetrics.Diff(spu.parsedReport, rpu.parsedReport),
                        sourceMultiCompileIndex = scu.multicompileIndex,
                        sourceProgramIndex = scu.programIndex,
                        refMultiCompileIndex = rcu.multicompileIndex,
                        refProgramIndex = rcu.programIndex
                    };
                    diff.perfDiffs.Add(perfdiff);
                }
            }

            return diff;
        }

        public const string DefineFragment = "SHADER_STAGE_FRAGMENT=1";
        public const string DefineCompute = "SHADER_STAGE_COMPUTE=1";

        public static ShaderCompilerOptions DefaultCompileOptions(
            IEnumerable<string> defines, string entry, DirectoryInfo sourceDir, string shaderModel = null
        )
        {
            var includes = new HashSet<string>
            {
                sourceDir.FullName
            };

            var compileOptions = new ShaderCompilerOptions
            {
                includeFolders = includes,
                defines = new HashSet<string>(),
                entry = entry
            };

            compileOptions.defines.UnionWith(defines);
            if (!string.IsNullOrEmpty(shaderModel))
                compileOptions.defines.Add($"SHADER_TARGET={shaderModel}");

            // Add default unity includes
            var path = Path.Combine(EditorApplication.applicationContentsPath, "CGIncludes");
            if (Directory.Exists(path))
                compileOptions.includeFolders.Add(path);

            // Add package symlinks folder
            // So shader compiler will find include files with "Package/<package_id>/..."
            compileOptions.includeFolders.Add(Path.GetFullPath(Path.Combine(Application.dataPath,
                $"../{PackagesUtilities.PackageSymbolicLinkFolder}")));

            // add include folders of playback engines
            foreach (var playbackEnginePath in new[]
            {
                "../..",
                "PlaybackEngines"
            })
            {
                var directory = new DirectoryInfo(Path.Combine(EditorApplication.applicationContentsPath, playbackEnginePath));
                if (directory.Exists)
                {
                    foreach (var engineDir in directory.EnumerateDirectories())
                    {
                        var includeDir = new DirectoryInfo(Path.Combine(engineDir.FullName, "CgBatchPlugins64/include"));
                        if (includeDir.Exists)
                            compileOptions.includeFolders.Add(includeDir.FullName);
                    }
                }
            }

            return compileOptions;
        }
    }
}
