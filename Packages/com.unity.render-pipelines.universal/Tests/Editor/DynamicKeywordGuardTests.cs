using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEditor.Shaders;
using UnityEngine;

namespace UnityEditor.Rendering.Universal.Tests
{
    // A compile-time preprocessor check on a keyword declared with variantGenerationMode 3
    // (dynamic branching) makes the shader compiler silently fall back to specialized variants.
    // These tests fail when a converted keyword regains such a check.
    class DynamicKeywordGuardTests
    {
        static readonly string[] k_ConvertibleKeywords =
        {
            "DIRLIGHTMAP_COMBINED",
            "DYNAMICLIGHTMAP_ON",
            "EVALUATE_SH_MIXED",
            "EVALUATE_SH_VERTEX",
            "LIGHTMAP_ON",
            "LIGHTMAP_SHADOW_MIXING",
            "LOD_FADE_CROSSFADE",
            "REFLECTION_PROBE_ROTATION",
            "SHADOWS_SHADOWMASK",
            "_ADDITIONAL_LIGHT_SHADOWS",
            "_ALPHAMODULATE_ON",
            "_ALPHAPREMULTIPLY_ON",
            "_CLEARCOAT",
            "_CLEARCOATMAP",
            "_EMISSION",
            "_ENVIRONMENTREFLECTIONS_OFF",
            "_GBUFFER_NORMALS_OCT",
            "_GLOSSINESS_FROM_BASE_ALPHA",
            "_LIGHT_COOKIES",
            "_LIGHT_LAYERS",
            "_MAIN_LIGHT_SHADOWS",
            "_MAIN_LIGHT_SHADOWS_CASCADE",
            "_MAIN_LIGHT_SHADOWS_SCREEN",
            "_METALLICSPECGLOSSMAP",
            "_NORMALMAP",
            "_OCCLUSIONMAP",
            "_PARALLAXMAP",
            "_RECEIVE_SHADOWS_OFF",
            "_REFLECTION_PROBE_BLENDING",
            "_SCREEN_SPACE_OCCLUSION",
            "_SCREEN_SPACE_REFLECTION",
            "_SHADOWS_SOFT",
            "_SHADOWS_SOFT_HIGH",
            "_SHADOWS_SOFT_LOW",
            "_SHADOWS_SOFT_MEDIUM",
            "_SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A",
            "_SPECGLOSSMAP",
            "_SPECULARHIGHLIGHTS_OFF",
            "_SPECULAR_COLOR",
            "_SPECULAR_SETUP",
            "_SURFACE_TYPE_TRANSPARENT",
        };

        static readonly HashSet<string> k_ConvertibleKeywordSet = new HashSet<string>(k_ConvertibleKeywords);

        // Deliberate compile-time references known not to break the dynamic declarations
        static readonly (string path, string keyword, int count)[] k_SanctionedGuards =
        {
            ("ShaderLibrary/GBufferCommon.hlsl", "SHADOWS_SHADOWMASK", 1),
            ("ShaderLibrary/GBufferCommon.hlsl", "LIGHTMAP_SHADOW_MIXING", 1),
            ("ShaderLibrary/GBufferCommon.hlsl", "_LIGHT_LAYERS", 1),
            ("Shaders/2D/Light2D.shader", "_LIGHT_LAYERS", 1),

            // ShaderGraph includes. _SURFACE_TYPE_TRANSPARENT is a per-material #define in
            // generated code (not a declared keyword), so static tests on it are by design
            // (includes the volumetric fog _TRANSPARENT_RECEIVE_FOG sites).
            // The remaining feature-keyword guards are standing migration debt: conversion is
            // blocked on the cross-tree layering decision for keyword-state access in
            // ShaderGraph passes (same debt class as k_SanctionedReferences in
            // DeprecatedSurfaceIsolationTests).
            ("Editor/ShaderGraph/Includes/DepthNormalsOnlyPass.hlsl", "_NORMALMAP", 1),
            ("Editor/ShaderGraph/Includes/MotionVectorPass.hlsl", "_SURFACE_TYPE_TRANSPARENT", 1),
            ("Editor/ShaderGraph/Includes/PBR2DPass.hlsl", "_SURFACE_TYPE_TRANSPARENT", 1),
            ("Editor/ShaderGraph/Includes/PBRForwardPass.hlsl", "_NORMALMAP", 2),
            ("Editor/ShaderGraph/Includes/PBRForwardPass.hlsl", "_SURFACE_TYPE_TRANSPARENT", 4),
            ("Editor/ShaderGraph/Includes/PBRForwardPass.hlsl", "_SPECULAR_SETUP", 1),
            ("Editor/ShaderGraph/Includes/PBRForwardPass.hlsl", "_CLEARCOAT", 1),
            ("Editor/ShaderGraph/Includes/PBRGBufferPass.hlsl", "_NORMALMAP", 1),
            ("Editor/ShaderGraph/Includes/PBRGBufferPass.hlsl", "_SURFACE_TYPE_TRANSPARENT", 1),
            ("Editor/ShaderGraph/Includes/PBRGBufferPass.hlsl", "_SPECULAR_SETUP", 1),
            ("Editor/ShaderGraph/Includes/SixWayForwardPass.hlsl", "_SURFACE_TYPE_TRANSPARENT", 3),
            ("Editor/ShaderGraph/Includes/UnlitPass.hlsl", "_SURFACE_TYPE_TRANSPARENT", 5),
            ("Editor/ShaderGraph/Includes/Varyings.hlsl", "_NORMALMAP", 1),
            ("Editor/ShaderGraph/Includes/Terrain/PBRForwardPass.hlsl", "_NORMALMAP", 1),
            ("Editor/ShaderGraph/Includes/Terrain/PBRForwardPass.hlsl", "_SURFACE_TYPE_TRANSPARENT", 1),
            ("Editor/ShaderGraph/Includes/Terrain/PBRGBufferPass.hlsl", "_NORMALMAP", 1),
        };

        enum RestrictionScope
        {
            // Only where the compile-time consumer lives; the other declaring passes stay convertible
            SomePasses,

            // The keyword only exists on restricted passes
            AllPasses,
        }

        // Expected: the shadowmask and rendering-layer keywords select the GBuffer render target
        // layout (GBufferCommon.hlsl), so the GBuffer pass can never convert them to dynamic
        // branches; passes without that dependency can, which is what a SomePasses scope pins.
        static readonly (string shader, string keyword, RestrictionScope scope)[] k_ExpectedRestrictions =
        {
            ("Lit.shader", "SHADOWS_SHADOWMASK", RestrictionScope.SomePasses),
            ("Lit.shader", "LIGHTMAP_SHADOW_MIXING", RestrictionScope.SomePasses),
            ("SimpleLit.shader", "SHADOWS_SHADOWMASK", RestrictionScope.SomePasses),
            ("SimpleLit.shader", "LIGHTMAP_SHADOW_MIXING", RestrictionScope.SomePasses),
            ("Unlit.shader", "SHADOWS_SHADOWMASK", RestrictionScope.AllPasses),
            ("BakedLit.shader", "SHADOWS_SHADOWMASK", RestrictionScope.AllPasses),
            ("Terrain/TerrainLit.shader", "SHADOWS_SHADOWMASK", RestrictionScope.AllPasses),
            ("Terrain/TerrainLit.shader", "LIGHTMAP_SHADOW_MIXING", RestrictionScope.AllPasses),
            ("Terrain/TerrainLit.shader", "_LIGHT_LAYERS", RestrictionScope.AllPasses),
            ("Particles/ParticlesLit.shader", "LIGHTMAP_SHADOW_MIXING", RestrictionScope.SomePasses),
            ("Particles/ParticlesSimpleLit.shader", "LIGHTMAP_SHADOW_MIXING", RestrictionScope.SomePasses),
            ("2D/Light2D.shader", "_LIGHT_LAYERS", RestrictionScope.AllPasses),
            ("ComplexLit.shader", "SHADOWS_SHADOWMASK", RestrictionScope.SomePasses),
            ("ComplexLit.shader", "LIGHTMAP_SHADOW_MIXING", RestrictionScope.SomePasses),
            ("Nature/SpeedTree7.shader", "_LIGHT_LAYERS", RestrictionScope.AllPasses),
            ("Nature/SpeedTree8.shader", "_LIGHT_LAYERS", RestrictionScope.AllPasses),
            ("Terrain/TerrainDetailLit.shader", "SHADOWS_SHADOWMASK", RestrictionScope.AllPasses),
            ("Terrain/TerrainDetailLit.shader", "LIGHTMAP_SHADOW_MIXING", RestrictionScope.AllPasses),
            ("Terrain/TerrainLitAdd.shader", "SHADOWS_SHADOWMASK", RestrictionScope.AllPasses),
            ("Terrain/TerrainLitAdd.shader", "LIGHTMAP_SHADOW_MIXING", RestrictionScope.AllPasses),
            ("Terrain/TerrainLitAdd.shader", "_LIGHT_LAYERS", RestrictionScope.AllPasses),
            ("Terrain/TerrainLitBase.shader", "SHADOWS_SHADOWMASK", RestrictionScope.AllPasses),
            ("Terrain/TerrainLitBase.shader", "LIGHTMAP_SHADOW_MIXING", RestrictionScope.AllPasses),
            ("Terrain/TerrainLitBase.shader", "_LIGHT_LAYERS", RestrictionScope.AllPasses),
            ("Terrain/WavingGrass.shader", "SHADOWS_SHADOWMASK", RestrictionScope.AllPasses),
            ("Terrain/WavingGrass.shader", "LIGHTMAP_SHADOW_MIXING", RestrictionScope.AllPasses),
            ("Terrain/WavingGrass.shader", "_LIGHT_LAYERS", RestrictionScope.AllPasses),
            ("Terrain/WavingGrassBillboard.shader", "SHADOWS_SHADOWMASK", RestrictionScope.AllPasses),
            ("Terrain/WavingGrassBillboard.shader", "LIGHTMAP_SHADOW_MIXING", RestrictionScope.AllPasses),
            ("Utils/ClusterDeferred.shader", "SHADOWS_SHADOWMASK", RestrictionScope.AllPasses),
            ("Utils/ClusterDeferred.shader", "LIGHTMAP_SHADOW_MIXING", RestrictionScope.AllPasses),
            ("Utils/ClusterDeferred.shader", "_LIGHT_LAYERS", RestrictionScope.AllPasses),
            ("Utils/StencilDeferred.shader", "SHADOWS_SHADOWMASK", RestrictionScope.AllPasses),
            ("Utils/StencilDeferred.shader", "LIGHTMAP_SHADOW_MIXING", RestrictionScope.AllPasses),
            ("Utils/StencilDeferred.shader", "_LIGHT_LAYERS", RestrictionScope.AllPasses),
        };

        const string k_PackageRoot = "Packages/com.unity.render-pipelines.universal";
        const string k_ShippedShaderFolder = "Shaders";
        static readonly string[] k_ScannedFolders = { "ShaderLibrary", "Shaders", "Editor/ShaderGraph/Includes" };

        static readonly Regex k_Directive = new Regex(@"^\s*#\s*(if|elif|else|endif|ifdef|ifndef)\b(.*)$", RegexOptions.Compiled);
        static readonly Regex k_AnyConvertibleKeyword = new Regex(
            @"\b(" + string.Join("|", k_ConvertibleKeywords) + @")\b", RegexOptions.Compiled);
        static readonly Regex k_DeclaredSymbol = new Regex(@"\b(\w+)_KEYWORD_DECLARED\b", RegexOptions.Compiled);
        static readonly Regex k_DefinedNonZeroArm = new Regex(@"DEFINED_NONZERO\s*\(\s*$", RegexOptions.Compiled);

        static IEnumerable<string> ShippedShaders()
        {
            string root = Path.GetFullPath(k_PackageRoot + "/" + k_ShippedShaderFolder);
            var paths = new List<string>();
            foreach (string file in Directory.EnumerateFiles(root, "*.shader", SearchOption.AllDirectories))
                paths.Add(file.Substring(root.Length + 1).Replace('\\', '/'));
            paths.Sort();
            return paths;
        }

        [Test]
        public void ShaderKeywordsRemainDynamicBranchConvertible(
            [ValueSource(nameof(ShippedShaders))] string shaderPath)
        {
            string assetPath = k_PackageRoot + "/" + k_ShippedShaderFolder + "/" + shaderPath;
            var shader = AssetDatabase.LoadAssetAtPath<Shader>(assetPath);
            Assert.IsNotNull(shader, $"Failed to load shader at path '{assetPath}'");

            var expected = new Dictionary<string, RestrictionScope>();
            foreach (var (entryShader, keyword, scope) in k_ExpectedRestrictions)
            {
                if (entryShader == shaderPath)
                    expected.Add(keyword, scope);
            }

            var unexpected = new List<string>();
            var spread = new List<string>();
            var narrowed = new List<string>();
            var unusedExpected = new HashSet<string>(expected.Keys);
            foreach (var declaration in ShaderKeywordDeclarations.GatherFromShader(shader))
            {
                bool restrictedAny = declaration.IsRestrictedInAnyInstance(ShaderKeywordOverrideRestriction.NoDynamicBranch);
                if (!restrictedAny)
                    continue;

                bool restrictedEvery = declaration.IsRestrictedInEveryInstance(ShaderKeywordOverrideRestriction.NoDynamicBranch);
                foreach (string keyword in declaration.keywords)
                {
                    if (!k_ConvertibleKeywordSet.Contains(keyword))
                        continue;

                    if (!expected.TryGetValue(keyword, out var scope))
                        unexpected.Add(keyword);
                    else if (scope == RestrictionScope.SomePasses && restrictedEvery)
                        spread.Add(keyword);
                    else if (scope == RestrictionScope.AllPasses && !restrictedEvery)
                        narrowed.Add(keyword);
                    unusedExpected.Remove(keyword);
                }
            }

            Assert.IsEmpty(unexpected,
                $"Shader '{shader.name}' no longer supports dynamic branching (variantGenerationMode 3) for: " +
                string.Join(", ", unexpected) + ". A compile-time preprocessor test on the keyword was " +
                "introduced somewhere in the shader's include graph. Read the keyword at runtime " +
                "(if (KW)) instead, or extend k_ExpectedRestrictions with a justification.");
            Assert.IsEmpty(spread,
                $"Shader '{shader.name}' is now restricted in EVERY pass declaring: " +
                string.Join(", ", spread) + ". The sanctioned restriction covers only some passes; " +
                "a compile-time preprocessor check on the keyword was introduced in a pass " +
                "that must stay dynamic-branch convertible.");
            Assert.IsEmpty(narrowed,
                $"Shader '{shader.name}' is no longer restricted in every pass declaring: " +
                string.Join(", ", narrowed) + ". Change the scope to SomePasses in " +
                "k_ExpectedRestrictions so the improvement stays locked in.");
            Assert.IsEmpty(unusedExpected,
                $"Shader '{shader.name}' is dynamic-branch convertible again for: " +
                string.Join(", ", unusedExpected) + ". Remove the pair(s) from k_ExpectedRestrictions " +
                "so the improvement stays locked in.");
        }

        [Test]
        public void PackageSourcesHaveNoUnsanctionedStaticGuardsOnConvertibleKeywords()
        {
            var found = new Dictionary<(string path, string keyword), List<string>>();
            int scannedFileCount = 0;
            foreach (string folder in k_ScannedFolders)
            {
                string root = Path.GetFullPath(k_PackageRoot + "/" + folder);
                foreach (string file in Directory.EnumerateFiles(root, "*.*", SearchOption.AllDirectories))
                {
                    if (!file.EndsWith(".hlsl") && !file.EndsWith(".shader"))
                        continue;
                    if (file.Contains(".deprecated."))
                        continue;

                    scannedFileCount++;
                    string relativePath = folder + "/" +
                        file.Substring(root.Length + 1).Replace('\\', '/');
                    foreach (var (lineNumber, keyword, line) in FindStaticGuards(file))
                    {
                        if (!found.TryGetValue((relativePath, keyword), out var sites))
                            found[(relativePath, keyword)] = sites = new List<string>();
                        sites.Add($"{relativePath}:{lineNumber}: {line}");
                    }
                }
            }
            Assert.NotZero(scannedFileCount, "Scanned no shader source files; package layout changed?");

            var sanctioned = new Dictionary<(string path, string keyword), int>();
            foreach (var (path, keyword, count) in k_SanctionedGuards)
                sanctioned.Add((path, keyword), count);

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
                    stale.Add($"{key.path} ({key.keyword}, expected {count})");
            }

            Assert.IsEmpty(violations,
                "Static preprocessor guard(s) found on dynamic-branch convertible keyword(s):\n" +
                string.Join("\n", violations) + "\n" +
                "These keywords are declared with variantGenerationMode 3 (dynamic branch) by URP " +
                "test projects; a compile-time #if/#ifdef on them makes the compiler silently fall " +
                "back to specialized variants. Read the keyword at runtime (if (KW)) instead, or " +
                "add the site to k_SanctionedGuards with a justification.");
            Assert.IsEmpty(stale,
                "k_SanctionedGuards no longer matches the sources (guard removed or moved) - " +
                "update the table:\n" + string.Join("\n", stale));
        }

        static IEnumerable<(int lineNumber, string keyword, string line)> FindStaticGuards(string file)
        {
            var reportedKeywords = new HashSet<string>();
            string[] lines = StripComments(File.ReadAllText(file)).Split('\n');

            // Conditional-block stack. Each entry holds the keywords whose <KW>_KEYWORD_DECLARED
            // symbol the block's condition tests: inside such a block, compile-time tests on <KW>
            // itself are the sanctioned declaration-aware normalization shape, not a restriction.
            var declaredScopes = new List<HashSet<string>>();

            for (int i = 0; i < lines.Length; i++)
            {
                int lineNumber = i + 1;
                string line = lines[i];
                while (line.TrimEnd().EndsWith(@"\") && i + 1 < lines.Length)
                    line = line.TrimEnd().TrimEnd('\\') + " " + lines[++i];

                var match = k_Directive.Match(line);
                if (!match.Success)
                    continue;

                string directive = match.Groups[1].Value;
                string expr = match.Groups[2].Value;

                if (directive == "endif")
                {
                    if (declaredScopes.Count > 0)
                        declaredScopes.RemoveAt(declaredScopes.Count - 1);
                    continue;
                }
                if (directive == "else")
                    continue;

                var exprDeclared = new HashSet<string>();
                foreach (Match declared in k_DeclaredSymbol.Matches(expr))
                    exprDeclared.Add(declared.Groups[1].Value);

                bool Exempt(string keyword)
                {
                    if (exprDeclared.Contains(keyword))
                        return true;
                    foreach (var scope in declaredScopes)
                    {
                        if (scope.Contains(keyword))
                            return true;
                    }
                    return false;
                }

                if (directive == "elif")
                {
                    // An #elif arm belongs to the enclosing #if block; fold its declared
                    // symbols into that block's exemption set.
                    if (declaredScopes.Count > 0)
                        declaredScopes[declaredScopes.Count - 1].UnionWith(exprDeclared);
                }

                reportedKeywords.Clear();
                foreach (Match occurrence in k_AnyConvertibleKeyword.Matches(expr))
                {
                    // DEFINED_NONZERO(KW) is the sanctioned value-less normalization arm.
                    if (k_DefinedNonZeroArm.IsMatch(expr.Substring(0, occurrence.Index)))
                        continue;
                    if (!Exempt(occurrence.Value) && reportedKeywords.Add(occurrence.Value))
                        yield return (lineNumber, occurrence.Value, line.Trim());
                }

                if (directive == "if" || directive == "ifdef" || directive == "ifndef")
                    declaredScopes.Add(exprDeclared);
            }
        }

        // Removes // and /* */ comments while preserving newlines, so reported line numbers
        // match the source file.
        internal static string StripComments(string source)
        {
            var result = new System.Text.StringBuilder(source.Length);
            for (int i = 0; i < source.Length; i++)
            {
                if (source[i] == '/' && i + 1 < source.Length && source[i + 1] == '/')
                {
                    while (i < source.Length && source[i] != '\n')
                        i++;
                    if (i < source.Length)
                        result.Append('\n');
                }
                else if (source[i] == '/' && i + 1 < source.Length && source[i + 1] == '*')
                {
                    i += 2;
                    while (i + 1 < source.Length && !(source[i] == '*' && source[i + 1] == '/'))
                    {
                        if (source[i] == '\n')
                            result.Append('\n');
                        i++;
                    }
                    i++;
                }
                else
                {
                    result.Append(source[i]);
                }
            }
            return result.ToString();
        }
    }
}
