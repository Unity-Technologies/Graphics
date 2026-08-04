using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.Rendering;
using UnityEditor.Shaders;

namespace UnityEngine.TestTools.Graphics.Universal
{
    // Applies a project's ShaderKeywordPlatformOverrides asset during the player build.
    //
    // Keyword declaration overrides are a global project setting with no per-target dimension. Build profiles
    // carry per-target overrides, but UTR test builds switch the build target without activating a profile,
    // leaving a build hook as the only per-target lever. For a target a keyword is not whitelisted on, its
    // override is dropped and the keyword reverts to specialized variants.
    //
    // Shader build settings are an import dependency. Editing them here reimports the affected shaders before
    // the build compiles them. The originals are restored afterwards.
    class PerPlatformKeywordOverrides : IPreprocessBuildWithReport, IPostprocessBuildWithReport
    {
        ShaderBuildSettings m_Saved;
        bool m_Applied;

        public int callbackOrder => -10000;

        // This hook must never be the reason a build fails: swallow any error and proceed with unmodified settings.
        public void OnPreprocessBuild(BuildReport report)
        {
            try
            {
                Apply(report.summary.platform);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[{nameof(PerPlatformKeywordOverrides)}] Skipped applying platform keyword overrides: {e}");
            }
        }

        public void OnPostprocessBuild(BuildReport report)
        {
            try
            {
                Restore();
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[{nameof(PerPlatformKeywordOverrides)}] Failed to restore shader build settings: {e}");
            }
        }

        void Apply(BuildTarget target)
        {
            var config = LoadConfig();
            if (config == null || config.entries == null)
                return;

            var saved = EditorGraphicsSettings.GetShaderBuildSettings();
            var current = saved.GetKeywordDeclarationOverridesCopy();

            var kept = new List<ShaderBuildSettings.KeywordDeclarationOverride>(current.Length);
            foreach (var o in current)
            {
                if (config.IsEnabledOnTarget(Names(o.keywords), target))
                    kept.Add(o);
            }
            if (kept.Count == current.Length)
                return;

            // Mark applied before mutating so a throw mid-apply still triggers Restore in postprocess.
            m_Saved = saved;
            m_Applied = true;

            var settings = saved;
            settings.KeywordDeclarationOverrides = kept.ToArray();
            EditorGraphicsSettings.SetShaderBuildSettings(settings);
        }

        void Restore()
        {
            if (!m_Applied)
                return;

            // Clear only after the restore succeeds, so a throw leaves the flag set rather than marking dirty settings clean.
            EditorGraphicsSettings.SetShaderBuildSettings(m_Saved);
            m_Applied = false;
        }

        static string[] Names(ShaderBuildSettings.KeywordOverrideInfo[] keywords)
        {
            if (keywords == null)
                return Array.Empty<string>();
            var names = new string[keywords.Length];
            for (int i = 0; i < keywords.Length; i++)
                names[i] = keywords[i].name;
            return names;
        }

        static ShaderKeywordPlatformOverrides LoadConfig()
        {
            var guids = AssetDatabase.FindAssets($"t:{nameof(ShaderKeywordPlatformOverrides)}");
            if (guids.Length == 0)
                return null;
            // FindAssets order is nondeterministic, so a stray duplicate could silently apply the wrong restrictions; fail closed instead of guessing.
            if (guids.Length > 1)
                throw new InvalidOperationException($"Found {guids.Length} {nameof(ShaderKeywordPlatformOverrides)} assets; expected exactly one.");

            var path = AssetDatabase.GUIDToAssetPath(guids[0]);
            return AssetDatabase.LoadAssetAtPath<ShaderKeywordPlatformOverrides>(path);
        }
    }
}
