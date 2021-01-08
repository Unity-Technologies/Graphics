namespace UnityEditor.Rendering
{
    /// <summary>
    /// Core Render Pipeline preferences.
    /// </summary>
    public static class CoreRenderPipelinePreferences
    {
        public static readonly string corePreferencePath = "Preferences/Core Render Pipeline";

        [SettingsProvider]
        static SettingsProvider PreferenceGUI()
        {
            return new SettingsProvider(corePreferencePath, SettingsScope.User)
            {
                guiHandler = searchContext =>
                {
                    AdditionalPropertiesPreferences.PreferenceGUI();
                }
            };
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
