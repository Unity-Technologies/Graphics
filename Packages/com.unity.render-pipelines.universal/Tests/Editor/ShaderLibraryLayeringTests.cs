using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;

namespace UnityEditor.Rendering.Universal.Tests
{
    // ShaderLibrary/ must not depend on the material-level Shaders/ tree.
    class ShaderLibraryLayeringTests
    {
        const string k_PackageRoot = "Packages/com.unity.render-pipelines.universal";

        static readonly Regex k_ShadersInclude = new Regex(
            @"^\s*#\s*include(_with_pragmas)?\s+""Packages/com\.unity\.render-pipelines\.universal/Shaders/",
            RegexOptions.Compiled);

        static readonly (string path, int count)[] k_SanctionedIncludes =
        {
            // Pre-existing 2D debt: InputData2D/SurfaceData2D structs live in Shaders/2D/Include.
            ("ShaderLibrary/Debug/Debugging2D.hlsl", 2),
        };

        [Test]
        public void ShaderLibraryDoesNotIncludeShadersTree()
        {
            var found = new Dictionary<string, List<string>>();
            int scannedFileCount = 0;
            string root = Path.GetFullPath(k_PackageRoot + "/ShaderLibrary");
            foreach (string file in Directory.EnumerateFiles(root, "*.hlsl", SearchOption.AllDirectories))
            {
                if (file.Contains(".deprecated."))
                    continue;

                scannedFileCount++;
                string relativePath = "ShaderLibrary/" + file.Substring(root.Length + 1).Replace('\\', '/');
                string[] lines = File.ReadAllLines(file);
                for (int i = 0; i < lines.Length; i++)
                {
                    if (!k_ShadersInclude.IsMatch(lines[i]))
                        continue;
                    if (!found.TryGetValue(relativePath, out var sites))
                        found[relativePath] = sites = new List<string>();
                    sites.Add($"{relativePath}:{i + 1}: {lines[i].Trim()}");
                }
            }
            Assert.NotZero(scannedFileCount, "Scanned no ShaderLibrary files; package layout changed?");

            var sanctioned = new Dictionary<string, int>();
            foreach (var (path, count) in k_SanctionedIncludes)
                sanctioned.Add(path, count);

            var violations = new List<string>();
            foreach (var (path, sites) in found)
            {
                sanctioned.TryGetValue(path, out int allowed);
                if (sites.Count > allowed)
                    violations.AddRange(sites);
            }
            var stale = new List<string>();
            foreach (var (path, count) in sanctioned)
            {
                if (!found.TryGetValue(path, out var sites) || sites.Count != count)
                    stale.Add($"{path} (expected {count})");
            }

            Assert.IsEmpty(violations,
                "ShaderLibrary file(s) include the Shaders/ tree:\n" + string.Join("\n", violations) + "\n" +
                "ShaderLibrary must not depend on material-level modules; pass shader_feature " +
                "state into library functions as parameters instead.");
            Assert.IsEmpty(stale,
                "k_SanctionedIncludes no longer matches the sources - update the table:\n" +
                string.Join("\n", stale));
        }
    }
}
