using System;
using System.Collections.Generic;
using System.Reflection;

namespace UnityEditor.Rendering
{
    /// <summary>
    /// Core Render Pipeline preferences.
    /// </summary>
    public static class CoreRenderPipelinePreferences
    {
        /// <summary>
        /// Path to the Render Pipeline Preferences
        /// </summary>
        public static readonly string corePreferencePath = "Preferences/Core Render Pipeline";

        private static readonly List<ICoreRenderPipelinePreferencesProvider> s_Providers = new();

        [InitializeOnLoadMethod]
        static void InitPreferenceProviders()
        {
            foreach (var provider in TypeCache.GetTypesDerivedFrom<ICoreRenderPipelinePreferencesProvider>())
            {
                if (provider.GetConstructor(BindingFlags.Public | BindingFlags.Instance, null, Type.EmptyTypes, null) == null)
                    continue;
                s_Providers.Add(Activator.CreateInstance(provider) as ICoreRenderPipelinePreferencesProvider);
            }
        }

        [SettingsProvider]
        static SettingsProvider PreferenceGUI()
        {
            var provider = new SettingsProvider(corePreferencePath, SettingsScope.User)
            {
                guiHandler = searchContext =>
                {
                    var labelWidth = EditorGUIUtility.labelWidth;
                    EditorGUIUtility.labelWidth = 251;

                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUILayout.Space(10, false);
                        using (new EditorGUILayout.VerticalScope())
                        {
                            foreach (var providers in s_Providers)
                            {
                                EditorGUILayout.LabelField(providers.header, EditorStyles.boldLabel);
                                providers.PreferenceGUI();
                            }
                        }
                    }

                    EditorGUIUtility.labelWidth = labelWidth;
                }
            };

            FillKeywords(provider);

            return provider;
        }

        private static void FillKeywords(SettingsProvider provider)
        {
            List<string> keywords = new List<string>();
            foreach (var providers in s_Providers)
                keywords.AddRange(providers.keywords);
            provider.keywords = keywords;
        }

        /// <summary>
        /// Open the Core Rendering Pipeline preference window.
        /// </summary>
        public static void Open()
        {
            SettingsService.OpenUserPreferences(corePreferencePath);
        }
    }
}
