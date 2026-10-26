using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;

namespace UnityEditor.Rendering.Universal.Tests
{
    // The .deprecated.hlsl files exist only for code outside this package; nothing live may
    // depend on them, or removing the deprecated surface would break the package itself.
    // Deprecated overloads of names that also have live definitions are invisible to this
    // name-level scan; the back-compat suites cover those.
    class DeprecatedSurfaceIsolationTests
    {
        const string k_PackageRoot = "Packages/com.unity.render-pipelines.universal";
        const string k_CorePackageRoot = "Packages/com.unity.render-pipelines.core";

        static readonly string[] k_ScannedFolders =
        {
            "ShaderLibrary",
            "Shaders",
            "Runtime/VFXGraph/Shaders",
            "Editor/ShaderGraph/Includes",
        };

        static readonly string[] k_CoreScannedFolders =
        {
            "ShaderLibrary",
            "Runtime",
        };

        // Deliberate live includes of a .deprecated file that is not the including file's own
        // counterpart.
        static readonly (string includingFile, string includedFile)[] k_SanctionedCrossIncludes =
        {
            ("ShaderLibrary/Core.hlsl", "ShaderLibrary/ForwardPlusKeyword.deprecated.hlsl"),
            ("ShaderLibrary/ShaderVariablesFunctions.hlsl", "ShaderLibrary/Fog.deprecated.hlsl"),
        };

        // Existing live references to deprecated-only symbols, pinned with exact counts so they
        // cannot grow. Each entry is standing migration debt, not an endorsement.
        static readonly (string path, string symbol, int count)[] k_SanctionedReferences =
        {
            // ShaderGraph includes stay on the deprecated wrappers until the cross-tree layering
            // decision for keyword-state access in ShaderGraph passes is made.
            ("Editor/ShaderGraph/Includes/PBRForwardPass.hlsl", "AlphaModulate", 1),
            ("Editor/ShaderGraph/Includes/PBRGBufferPass.hlsl", "ApplyDecal", 1),
            ("Editor/ShaderGraph/Includes/Terrain/PBRForwardPass.hlsl", "AlphaModulate", 1),
            ("Editor/ShaderGraph/Includes/Terrain/PBRGBufferPass.hlsl", "AlphaModulate", 1),
            ("Editor/ShaderGraph/Includes/UnlitGBufferPass.hlsl", "AlphaModulate", 1),
            ("Editor/ShaderGraph/Includes/UnlitPass.hlsl", "AlphaModulate", 1),
        };

        // Deprecated contract macros: defined in live files for back-compat, but nothing live may
        // read them outside their defining file (new code uses the USE_* flags).
        static readonly (string symbol, string definingFile)[] k_DeprecatedContracts =
        {
            ("REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR", "ShaderLibrary/Shadows.hlsl"),
            ("REQUIRES_LIGHTMAP_UV_INTERPOLATOR", "ShaderLibrary/Lightmaps.hlsl"),
            ("REQUIRES_DYNAMICLIGHTMAP_UV_INTERPOLATOR", "ShaderLibrary/Lightmaps.hlsl"),
            ("REQUIRES_VERTEX_SH_INTERPOLATOR", "ShaderLibrary/Lightmaps.hlsl"),
        };

        [Test]
        public void LiveSourcesDoNotReferenceDeprecatedContractMacros()
        {
            var violations = new List<string>();
            foreach (var (relativePath, file) in ScannedFiles(k_PackageRoot, k_ScannedFolders, includeDeprecated: false))
            {
                string[] lines = StripComments(File.ReadAllText(file)).Split('\n');
                for (int i = 0; i < lines.Length; i++)
                {
                    foreach (var (symbol, definingFile) in k_DeprecatedContracts)
                    {
                        if (relativePath == definingFile)
                            continue;
                        if (Regex.IsMatch(lines[i], $@"\b{symbol}\b"))
                            violations.Add($"{relativePath}:{i + 1}: {lines[i].Trim()}");
                    }
                }
            }
            Assert.IsEmpty(violations,
                "Live source(s) reference deprecated contract macro(s):\n" + string.Join("\n", violations) +
                "\nThese names keep their pre-conversion semantics for external code only; read the " +
                "corresponding USE_* interpolator flag instead.");
        }

        static readonly Regex k_Include = new Regex(
            @"^\s*#\s*include(?:_with_pragmas)?\s+""([^""]+\.deprecated\.hlsl)""",
            RegexOptions.Compiled);
        // Return type = builtin scalar/vector/matrix, or a struct name (PascalCase with at least
        // one lowercase letter, so all-caps macros like UNITY_BRANCH never parse as a type).
        static readonly Regex k_FunctionDefinition = new Regex(
            @"^\s*(?:inline\s+)?(?:(?:half|float|real|void|bool|int|uint|min16float)(?:[1-4](?:x[1-4])?)?|[A-Z]\w*[a-z]\w*)\s+(\w+)\s*\(",
            RegexOptions.Compiled | RegexOptions.Multiline);
        static readonly Regex k_MacroDefinition = new Regex(
            @"^\s*#\s*define\s+(\w+)", RegexOptions.Compiled | RegexOptions.Multiline);
        static readonly Regex k_MacroUndef = new Regex(
            @"^\s*#\s*undef\s+(\w+)", RegexOptions.Compiled | RegexOptions.Multiline);
        static readonly Regex k_StringLiteral = new Regex(@"""[^""]*""", RegexOptions.Compiled);

        [Test]
        public void DeprecatedIncludesComeOnlyFromTheirLiveCounterparts()
        {
            var violations = new List<string>();
            var seenSanctioned = new HashSet<(string, string)>();
            foreach (var (relativePath, file) in ScannedFiles(k_PackageRoot, k_ScannedFolders, includeDeprecated: true))
            {
                bool includerIsDeprecated = relativePath.Contains(".deprecated.");
                int lineNumber = 0;
                foreach (string line in StripComments(File.ReadAllText(file)).Split('\n'))
                {
                    lineNumber++;
                    var match = k_Include.Match(line);
                    if (!match.Success)
                        continue;
                    if (includerIsDeprecated)
                        continue;

                    string included = match.Groups[1].Value.Replace('\\', '/');
                    if (included.StartsWith(k_PackageRoot + "/"))
                        included = included.Substring(k_PackageRoot.Length + 1);
                    else if (!included.Contains("/"))
                        included = Path.GetDirectoryName(relativePath).Replace('\\', '/') + "/" + included;

                    string ownCounterpart = relativePath.Substring(0, relativePath.Length - ".hlsl".Length) + ".deprecated.hlsl";
                    if (relativePath.EndsWith(".hlsl") && included == ownCounterpart)
                        continue;
                    if (System.Array.IndexOf(k_SanctionedCrossIncludes, (relativePath, included)) >= 0)
                    {
                        seenSanctioned.Add((relativePath, included));
                        continue;
                    }
                    violations.Add($"{relativePath}:{lineNumber}: #include \"{included}\"");
                }
            }

            Assert.IsEmpty(violations,
                "Live source(s) include a .deprecated.hlsl file that is not their own counterpart:\n" +
                string.Join("\n", violations) + "\n" +
                "The deprecated surface exists only for old code outside this package. Use the live " +
                "API instead, or add the pair to k_SanctionedCrossIncludes with a justification.");
            Assert.AreEqual(k_SanctionedCrossIncludes.Length, seenSanctioned.Count,
                "k_SanctionedCrossIncludes no longer matches the sources (include removed or moved) - " +
                "update the table.");
        }

        [Test]
        public void LiveSourcesDoNotReferenceDeprecatedOnlySymbols()
        {
            GatherDefinitions(k_PackageRoot, k_ScannedFolders, deprecated: true,
                out var deprecatedFunctions, out var deprecatedMacros, out var deprecatedUndefs);
            GatherDefinitions(k_PackageRoot, k_ScannedFolders, deprecated: false,
                out var liveFunctions, out var liveMacros, out _);
            GatherDefinitions(k_CorePackageRoot, k_CoreScannedFolders, deprecated: false,
                out var coreFunctions, out var coreMacros, out _);

            deprecatedFunctions.ExceptWith(liveFunctions);
            deprecatedFunctions.ExceptWith(coreFunctions);
            deprecatedMacros.ExceptWith(liveMacros);
            deprecatedMacros.ExceptWith(coreMacros);
            deprecatedMacros.ExceptWith(deprecatedUndefs);
            deprecatedMacros.RemoveWhere(m => m.EndsWith("_INCLUDED") || m.EndsWith("_DEFINED_LOCALLY"));
            Assert.NotZero(deprecatedFunctions.Count + deprecatedMacros.Count,
                "Found no deprecated-only symbols; the extraction is broken or the deprecated files moved.");

            var functionReference = new Regex(@"\b(" + string.Join("|", deprecatedFunctions) + @")\s*\(");
            var macroReference = new Regex(@"\b(" + string.Join("|", deprecatedMacros) + @")\b");

            var found = new Dictionary<(string path, string symbol), List<string>>();
            foreach (var (relativePath, file) in ScannedFiles(k_PackageRoot, k_ScannedFolders, includeDeprecated: false))
            {
                string[] lines = StripComments(File.ReadAllText(file)).Split('\n');
                for (int i = 0; i < lines.Length; i++)
                {
                    string line = k_StringLiteral.Replace(lines[i], "\"\"");
                    foreach (Match match in functionReference.Matches(line))
                        AddSite(found, relativePath, match.Groups[1].Value, i + 1, lines[i]);
                    foreach (Match match in macroReference.Matches(line))
                        AddSite(found, relativePath, match.Groups[1].Value, i + 1, lines[i]);
                }
            }

            var sanctioned = new Dictionary<(string path, string symbol), int>();
            foreach (var (path, symbol, count) in k_SanctionedReferences)
                sanctioned.Add((path, symbol), count);

            var violations = new List<string>();
            foreach (var (key, sites) in found)
            {
                sanctioned.TryGetValue(key, out int allowed);
                if (sites.Count > allowed)
                    violations.AddRange(sites);
            }
            var stale = new List<string>();
            foreach (var (key, count) in sanctioned)
            {
                if (!found.TryGetValue(key, out var sites) || sites.Count != count)
                    stale.Add($"{key.path} ({key.symbol}, expected {count})");
            }

            Assert.IsEmpty(violations,
                "Live source(s) reference symbol(s) that are only defined in .deprecated.hlsl files:\n" +
                string.Join("\n", violations) + "\n" +
                "These definitions disappear when the deprecated surface is removed. Use the live " +
                "replacement instead, or add the site to k_SanctionedReferences with a justification.");
            Assert.IsEmpty(stale,
                "k_SanctionedReferences no longer matches the sources (reference removed or moved) - " +
                "update the table:\n" + string.Join("\n", stale));
        }

        static void AddSite(Dictionary<(string, string), List<string>> found, string path, string symbol,
            int lineNumber, string line)
        {
            if (!found.TryGetValue((path, symbol), out var sites))
                found[(path, symbol)] = sites = new List<string>();
            sites.Add($"{path}:{lineNumber}: {line.Trim()}");
        }

        static IEnumerable<(string relativePath, string file)> ScannedFiles(
            string packageRoot, string[] folders, bool includeDeprecated)
        {
            foreach (string folder in folders)
            {
                string root = Path.GetFullPath(packageRoot + "/" + folder);
                if (!Directory.Exists(root))
                    continue;
                foreach (string file in Directory.EnumerateFiles(root, "*.*", SearchOption.AllDirectories))
                {
                    if (!file.EndsWith(".hlsl") && !file.EndsWith(".shader"))
                        continue;
                    if (!includeDeprecated && file.Contains(".deprecated."))
                        continue;
                    yield return (folder + "/" + file.Substring(root.Length + 1).Replace('\\', '/'), file);
                }
            }
        }

        static void GatherDefinitions(string packageRoot, string[] folders, bool deprecated,
            out HashSet<string> functions, out HashSet<string> macros, out HashSet<string> undefs)
        {
            functions = new HashSet<string>();
            macros = new HashSet<string>();
            undefs = new HashSet<string>();
            foreach (var (relativePath, file) in ScannedFiles(packageRoot, folders, includeDeprecated: true))
            {
                if (relativePath.Contains(".deprecated.") != deprecated)
                    continue;
                string source = StripComments(File.ReadAllText(file));
                foreach (Match match in k_FunctionDefinition.Matches(source))
                    functions.Add(match.Groups[1].Value);
                foreach (Match match in k_MacroDefinition.Matches(source))
                    macros.Add(match.Groups[1].Value);
                foreach (Match match in k_MacroUndef.Matches(source))
                    undefs.Add(match.Groups[1].Value);
            }
        }

        static string StripComments(string source)
        {
            return DynamicKeywordGuardTests.StripComments(source);
        }
    }
}
