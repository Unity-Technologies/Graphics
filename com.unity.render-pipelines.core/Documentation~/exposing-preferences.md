# Exposing preferences on the Core Render Pipeline Settings

For exposing preferences on **Edit > Preferences > Core Render Pipeline**, you must specify classes that implement the interface `ICoreRenderPipelinePreferencesProvider`. Unity will automatically insert your preferences on the given page.

For example:

```
    class MyPreference : ICoreRenderPipelinePreferencesProvider
    {
        class Styles
        {
            public static readonly GUIContent myBoolLabel = EditorGUIUtility.TrTextContent("MyBool", "This bool toggles my feature");
        }

        public List<string> keywords => new List<string>() {Styles.myBoolLabel.text};
        public GUIContent header => EditorGUIUtility.TrTextContent("MyPreferenceSection", "Group of my preferences");

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

As a result you will be able to manipulate and see your preferences:

![](Images/core_render_pipeline_preference_provider.png)
