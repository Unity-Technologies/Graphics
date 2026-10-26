using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Rendering;
using UnityEditor.Shaders;

namespace UnityEngine.TestTools.Graphics.Universal
{
    [CustomEditor(typeof(ShaderKeywordPlatformOverrides))]
    class ShaderKeywordPlatformOverridesEditor : UnityEditor.Editor
    {
        static string[] s_TargetNames;

        public override void OnInspectorGUI()
        {
            var config = (ShaderKeywordPlatformOverrides)target;
            var selectable = ShaderKeywordPlatformOverrides.SelectableTargets;
            int fullMask = (1 << selectable.Length) - 1;

            if (s_TargetNames == null)
            {
                s_TargetNames = new string[selectable.Length];
                for (int i = 0; i < selectable.Length; i++)
                    s_TargetNames[i] = selectable[i].ToString();
            }

            EditorGUILayout.HelpBox(
                "Each override is dynamic on the selected targets. Unselected targets fall back to specialized "
                + "(multi_compile) variants for that override. The list comes from this project's Graphics Settings.",
                MessageType.Info);

            var overrides = EditorGraphicsSettings.GetShaderBuildSettings().GetKeywordDeclarationOverridesCopy();

            var kept = new List<ShaderKeywordPlatformOverrides.Entry>();
            bool anyShown = false;
            EditorGUI.BeginChangeCheck();

            foreach (var o in overrides)
            {
                // Default-mode overrides are already specialized; there is nothing to restrict per target.
                if (o.variantGenerationMode == ShaderBuildSettings.ShaderVariantGenerationMode.Default)
                    continue;

                anyShown = true;
                var names = Names(o.keywords);
                int mask = MaskFor(config, names, fullMask);
                int newMask = EditorGUILayout.MaskField(Label(names), mask, s_TargetNames) & fullMask;
                if (newMask != fullMask)
                    kept.Add(new ShaderKeywordPlatformOverrides.Entry { keywords = names, enabledTargetsMask = newMask });
            }

            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(config, "Edit Shader Keyword Platform Overrides");
                config.entries = kept.ToArray();
                EditorUtility.SetDirty(config);
            }

            if (!anyShown)
                EditorGUILayout.HelpBox("This project's Graphics Settings define no dynamic keyword overrides.",
                    MessageType.None);
        }

        static int MaskFor(ShaderKeywordPlatformOverrides config, string[] names, int fullMask)
        {
            foreach (var e in config.entries)
            {
                if (ShaderKeywordPlatformOverrides.KeywordsEqual(e.keywords, names))
                    return e.enabledTargetsMask & fullMask;
            }
            return fullMask;
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

        static string Label(string[] names)
        {
            var parts = new List<string>();
            foreach (var n in names)
            {
                if (!string.IsNullOrEmpty(n) && n != "_")
                    parts.Add(n);
            }
            return parts.Count > 0 ? string.Join(", ", parts) : "_";
        }
    }
}
