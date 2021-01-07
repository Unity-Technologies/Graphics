namespace UnityEditor.Rendering
{
    static class CoreRenderPipelinePreferences
    {
        [SettingsProvider]
        static SettingsProvider PreferenceGUI()
        {
            return new SettingsProvider("Preferences/Core Render Pipeline", SettingsScope.User)
            {
                guiHandler = searchContext =>
                {
                    UnityEngine.Rendering.CoreRenderPipelinePreferences.PreferenceGUI();
                    AdditionalPropertiesPreferences.PreferenceGUI();
                }
            };
        }
    }
}
