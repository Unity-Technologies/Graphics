using System.Collections.Generic;

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

        [SettingsProvider]
        static SettingsProvider PreferenceGUI()
        {
            var provider = new SettingsProvider(corePreferencePath, SettingsScope.User)
            {
                guiHandler = searchContext =>
                {
                    AdditionalPropertiesPreferences.PreferenceGUI();
                }
            };

            List<string> keywords = new List<string>();
            foreach (var keyword in AdditionalPropertiesPreferences.GetPreferenceSearchKeywords())
                keywords.Add(keyword);
            provider.keywords = keywords;
            return provider;
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
