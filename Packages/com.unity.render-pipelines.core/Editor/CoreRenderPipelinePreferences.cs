using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine.Rendering;

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
        public static readonly string corePreferencePath = "Preferences/Graphics";

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

            s_Providers.Sort((x, y) => GetDisplayInfoOrder(x.GetType()).CompareTo(GetDisplayInfoOrder(y.GetType())));
        }

        static int GetDisplayInfoOrder(Type type)
        {
            var attribute = type.GetCustomAttribute<DisplayInfoAttribute>();
            return attribute?.order ?? int.MaxValue;
        }

        [SettingsProvider]
        static SettingsProvider PreferenceGUI()
        {
            var provider = new SettingsProvider(corePreferencePath, SettingsScope.User)
            {
                guiHandler = searchContext =>
                {
                    using (new SettingsProviderGUIScope())
                    {
                        foreach (var providers in s_Providers)
                        {
                            if (providers.header != null)
                            {
                                EditorGUILayout.LabelField(providers.header, EditorStyles.boldLabel);
                                providers.PreferenceGUI();
                            }
                        }
                    }
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
