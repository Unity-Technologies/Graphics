using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEditor;

// Validates that every $include("...") directive inside URP shader-template files resolves to an
// actual file under that template's search path. The shadergraph splice processor silently
// skips includes that cannot be found (it appends an error comment but does not fail the build),
// which is what allowed UUM-117270 to slip in: DecalPass.template referenced a non-existent
// VertexAnimation.template.hlsl, so custom interpolators silently went un-wired in URP decals.
class ShaderTemplateIncludeTests
{
    // One entry per template subtree: where to look for *.template files, and which directories
    // includes in those files should resolve against. Kept in sync by hand with each subtarget's
    // sharedTemplateDirectories setting (those symbols are internal to shadergraph/URP).
    // Public so it can appear in the public test method signature.
    public class Scope
    {
        public string Name;
        public string TemplateRoot;
        public string[] SearchDirectories;
    }

    const string kShaderGraphTemplates = "Packages/com.unity.shadergraph/Editor/Generation/Templates";
    const string kUrpTemplates = "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Templates";
    const string kUrpTerrain = "Packages/com.unity.render-pipelines.universal/Editor/Terrain";
    const string kUrpDecal = "Packages/com.unity.render-pipelines.universal/Editor/Decal";
    const string kVfxTemplates = "Packages/com.unity.visualeffectgraph/Editor/ShaderGraph/Templates";

    // The Foundation test project does not depend on VFX, but other downstream test projects can,
    // so detect it dynamically rather than assuming. When VFX is installed, its templates dir
    // joins the URP main search path; when it isn't, $features.graphVFX-conditional includes are
    // unreachable at runtime and we skip them rather than report a false positive.
    static readonly bool s_HasVfx = Directory.Exists(FileUtil.PathToAbsolutePath("Packages/com.unity.visualeffectgraph"));

    static Scope[] BuildScopes()
    {
        // UniversalDecalSubTarget uses GenerationUtils.GetDefaultSharedTemplateDirectories() only.
        var decal = new Scope
        {
            Name = "Decal",
            TemplateRoot = kUrpDecal,
            SearchDirectories = new[] { kShaderGraphTemplates },
        };

        // UniversalTerrainLitSubTarget.TerrainLitTemplate.kSharedTemplateDirectories =
        //   default + URP templates + URP terrain (see UniversalTerrainLitSubTarget.cs).
        var terrain = new Scope
        {
            Name = "Terrain",
            TemplateRoot = kUrpTerrain,
            SearchDirectories = new[] { kShaderGraphTemplates, kUrpTemplates, kUrpTerrain },
        };

        // UniversalTarget.kSharedTemplateDirectories = default + URP templates (+ VFX if present).
        // Used by Lit/Unlit/SixWay/etc.
        string[] urpMainSearch;
        if (s_HasVfx)
            urpMainSearch = new[] { kShaderGraphTemplates, kUrpTemplates, kVfxTemplates };
        else
            urpMainSearch = new[] { kShaderGraphTemplates, kUrpTemplates };
        var urpMain = new Scope
        {
            Name = "URP",
            TemplateRoot = kUrpTemplates,
            SearchDirectories = urpMainSearch,
        };

        return new[] { decal, terrain, urpMain };
    }

    static readonly Scope[] s_Scopes = BuildScopes();

    static readonly Regex s_IncludeRegex = new Regex(
        @"\$include\(\s*""(?<path>[^""]+)""\s*\)",
        RegexOptions.Compiled);

    // Matches a "$features.X:" prefix anywhere on the line. Used to skip includes whose feature
    // can never activate in the current project configuration (e.g. graphVFX without VFX).
    static readonly Regex s_FeaturePrefixRegex = new Regex(
        @"\$features\.(?<feature>\w+):",
        RegexOptions.Compiled);

    static TestCaseData MakeCase(string path, Scope scope)
    {
        // Stable, repo-relative name for the test report.
        var name = path.Replace('\\', '/');
        var marker = "/Packages/";
        var idx = name.IndexOf(marker, System.StringComparison.Ordinal);
        if (idx >= 0)
            name = name.Substring(idx + 1);
        return new TestCaseData(path, scope).SetName($"EveryShaderTemplateIncludeExists({name})");
    }

    public static IEnumerable<TestCaseData> TemplateFileCases()
    {
        foreach (var scope in s_Scopes)
        {
            // FileUtil.PathToAbsolutePath resolves Unity's virtual "Packages/..." paths to the
            // real on-disk location. The shadergraph splice processor uses the same call
            // (ShaderSpliceUtil.ProcessIncludeCommand) — matching it keeps us honest.
            var abs = FileUtil.PathToAbsolutePath(scope.TemplateRoot);
            if (!Directory.Exists(abs))
                continue;

            // Two passes because Directory.EnumerateFiles can't take multiple patterns.
            foreach (var path in Directory.EnumerateFiles(abs, "*.template", SearchOption.AllDirectories))
                yield return MakeCase(path, scope);
            foreach (var path in Directory.EnumerateFiles(abs, "*.template.hlsl", SearchOption.AllDirectories))
                yield return MakeCase(path, scope);
        }
    }

    // Returns true if the $include is gated by a $features.X: prefix whose feature cannot be
    // activated in this project (e.g. graphVFX when the VFX package isn't installed). Such an
    // include is unreachable at runtime, so a missing target file is not a real defect.
    static bool IsIncludeUnreachable(string text, int matchIndex)
    {
        // Find the start of the line containing the match.
        int lineStart = matchIndex;
        while (lineStart > 0 && text[lineStart - 1] != '\n')
            lineStart--;
        var prefixSpan = text.Substring(lineStart, matchIndex - lineStart);

        var featureMatch = s_FeaturePrefixRegex.Match(prefixSpan);
        if (!featureMatch.Success)
            return false;

        var feature = featureMatch.Groups["feature"].Value;
        switch (feature)
        {
            case "graphVFX":
                return !s_HasVfx;
            default:
                // graphVertex and graphPixel are universally reachable; new features default to
                // "reachable" so that missing search-path coverage gets reported, not silenced.
                return false;
        }
    }

    static bool ResolvesUnderSearchPath(string includeName, string[] searchDirectories)
    {
        foreach (var dir in searchDirectories)
        {
            var candidate = FileUtil.PathToAbsolutePath(Path.Combine(dir, includeName));
            if (File.Exists(candidate))
                return true;
        }
        return false;
    }

    static int LineNumberAt(string text, int charIndex)
    {
        int line = 1;
        for (int i = 0; i < charIndex && i < text.Length; i++)
        {
            if (text[i] == '\n')
                line++;
        }
        return line;
    }

    [Test, TestCaseSource(nameof(TemplateFileCases))]
    public void EveryShaderTemplateIncludeExists(string templatePath, Scope scope)
    {
        var text = File.ReadAllText(templatePath);
        var unresolved = new List<string>();

        foreach (Match match in s_IncludeRegex.Matches(text))
        {
            var includeName = match.Groups["path"].Value;

            if (IsIncludeUnreachable(text, match.Index))
                continue;

            if (ResolvesUnderSearchPath(includeName, scope.SearchDirectories))
                continue;

            var line = LineNumberAt(text, match.Index);
            unresolved.Add($"  line {line}: $include(\"{includeName}\")");
        }

        if (unresolved.Count == 0)
            return;

        var sb = new StringBuilder();
        sb.Append("Unresolved $include directive(s) in ").Append(templatePath).Append(":\n");
        foreach (var entry in unresolved)
            sb.Append(entry).Append('\n');
        sb.Append("Searched:\n");
        foreach (var dir in scope.SearchDirectories)
            sb.Append("  ").Append(dir).Append('\n');
        Assert.Fail(sb.ToString());
    }
}
