using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace UnityEditor.ShaderGraph
{
    [FilePath("ProjectSettings/ShaderGraphProjectSettings.asset", FilePathAttribute.Location.ProjectFolder)]
    internal class ShaderGraphProjectSettings : ScriptableSingleton<ShaderGraphProjectSettings>
    {
        [SerializeField]
        internal int customInterpolatorErrorThreshold = 32;
        [SerializeField]
        internal int customInterpolatorWarningThreshold = 16;
        internal SerializedObject GetSerializedObject() { return new SerializedObject(this); }
        internal void Save() { Save(true); }
        private void OnDisable() { Save(); }
    }

    class ShaderGraphProjectSettingsProvider : SettingsProvider
    {
        private static string kCustomInterpolatorHelpBox = "These options are used to help ShaderGraph users maintain known compatibilities with target platform(s) when using Custom Interpolators.";
        private static string kCustomInterpolatorDocumentationURL = "https://docs.unity3d.com/Packages/com.unity.shadergraph@latest/index.html?preview=1&subfolder=/manual/";

        private class Styles
        {
            public static readonly GUIContent CustomInterpLabel = L10n.TextContent("Custom Interpolator Channel Settings", "");
            public static readonly GUIContent CustomInterpWarnThresholdLabel = L10n.TextContent("Warning Threshold", "ShaderGraph will warn when this channel limitation is exceeded by a custom interpolator. Range must be between 8 and Error Threshold.");
            public static readonly GUIContent CustomInterpErrorThresholdLabel = L10n.TextContent("Error Threshold", "ShaderGraph will error when this channel limitation is exceeded by a custom interpolator. Range must be between 8 and 32.");
            public static readonly GUIContent ReadMore = L10n.TextContent("Read more");
        }

        SerializedObject m_SerializedObject;
        SerializedProperty m_customInterpWarn;
        SerializedProperty m_customInterpError;

        public ShaderGraphProjectSettingsProvider(string path, SettingsScope scopes, IEnumerable<string> keywords = null)
        : base(path, scopes, keywords) { }

        public override void OnActivate(string searchContext, VisualElement rootElement)
        {
            ShaderGraphProjectSettings.instance.Save();
            m_SerializedObject = ShaderGraphProjectSettings.instance.GetSerializedObject();
            m_customInterpWarn = m_SerializedObject.FindProperty("customInterpolatorWarningThreshold");
            m_customInterpError = m_SerializedObject.FindProperty("customInterpolatorErrorThreshold");
        }

        int oldWarningThreshold;
        public override void OnGUI(string searchContext)
        {
            m_SerializedObject.Update();
            EditorGUI.BeginChangeCheck();

            EditorGUILayout.LabelField(Styles.CustomInterpLabel, EditorStyles.boldLabel);
            EditorGUI.indentLevel++;

            int newError = EditorGUILayout.IntField(Styles.CustomInterpErrorThresholdLabel, m_customInterpError.intValue);
            m_customInterpError.intValue = Mathf.Clamp(newError, 8, 32);

            int oldWarn = m_customInterpWarn.intValue;
            int newWarn = EditorGUILayout.IntField(Styles.CustomInterpWarnThresholdLabel, m_customInterpWarn.intValue);

            // If the user did not modify the field, restore their previous input and reclamp against the new error threshold.
            if (oldWarn == newWarn) newWarn = oldWarningThreshold;
            else oldWarningThreshold = newWarn;
            m_customInterpWarn.intValue = Mathf.Clamp(newWarn, 8, m_customInterpError.intValue);

            GUILayout.BeginHorizontal(EditorStyles.helpBox);
            GUILayout.Label(EditorGUIUtility.IconContent("console.infoicon"), GUILayout.ExpandWidth(true));
            GUILayout.Box(kCustomInterpolatorHelpBox, EditorStyles.wordWrappedLabel);
            if (EditorGUILayout.LinkButton(Styles.ReadMore))
            {
                System.Diagnostics.Process.Start(kCustomInterpolatorDocumentationURL);
            }
            GUILayout.EndHorizontal();
            EditorGUI.indentLevel--;

            if (EditorGUI.EndChangeCheck())
            {
                m_SerializedObject.ApplyModifiedProperties();
                ShaderGraphProjectSettings.instance.Save();
            }
        }

        [SettingsProvider]
        public static SettingsProvider CreateShaderGraphProjectSettingsProvider()
        {
            var provider = new ShaderGraphProjectSettingsProvider("Project/ShaderGraph", SettingsScope.Project);
            return provider;
        }
    }
}
