# Adding properties to the Core Render Pipeline settings section

To add properties in the **Core Render Pipeline** settings section (**Edit > Preferences > Core Render Pipeline**), create a class that implements the interface `ICoreRenderPipelinePreferencesProvider`.

For example:

```
    class MyPreference : ICoreRenderPipelinePreferencesProvider
    {
        class Styles
        {
            public static readonly GUIContent myBoolLabel = L10n.TextContent("My check box", "The description of the property.", null, null);
        }

        public List<string> keywords => new List<string>() {Styles.myBoolLabel.text};
        public GUIContent header => L10n.TextContent("My property section", "The description of my property section.", null, null);

        public static bool s_MyBoolPreference;
        public void PreferenceGUI()
        {
            EditorGUI.BeginChangeCheck();
            var newValue = EditorGUILayout.Toggle(Styles.myBoolLabel, s_MyBoolPreference);
            if (EditorGUI.EndChangeCheck())
            {
                s_MyBoolPreference = newValue;
            }
        }
    }
```

Unity shows the new properties in the **Core Render Pipeline** settings section:

![Screenshot of the Preferences window showing the new properties in the Core Render Pipeline settings section.](Images/core_render_pipeline_preference_provider.png)
