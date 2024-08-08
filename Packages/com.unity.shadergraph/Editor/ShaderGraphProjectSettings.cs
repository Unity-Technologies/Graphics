using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace UnityEditor.ShaderGraph
{
    [FilePath("ProjectSettings/ShaderGraphSettings.asset", FilePathAttribute.Location.ProjectFolder)]
    internal class ShaderGraphProjectSettings : ScriptableSingleton<ShaderGraphProjectSettings>
    {
        [SerializeField]
        internal int shaderVariantLimit = 2048;        
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
        private static int kMaxChannelThreshold = 32;
        private static int kMinChannelThreshold = 8;
        private static string kCustomInterpolatorHelpBox = "Unity uses these options to help ShaderGraph users maintain known compatibilities with target platform(s) when using Custom Interpolators.";
        private static string kCustomInterpolatorDocumentationURL = UnityEngine.Rendering.ShaderGraph.Documentation.GetPageLink("Custom-Interpolators");

        private class Styles
        {
            public static readonly GUIContent shaderVariantLimitLabel = L10n.TextContent("Shader Variant Limit", "");            
            public static readonly GUIContent CustomInterpLabel = L10n.TextContent("Custom Interpolator Channel Settings", "");
            public static readonly GUIContent CustomInterpWarnThresholdLabel = L10n.TextContent("Warning Threshold", $"ShaderGraph displays a warning when the user creates more custom interpolators than permitted by this setting. The number of interpolators that trigger this warning must be between {kMinChannelThreshold} and the Error Threshold.");
            public static readonly GUIContent CustomInterpErrorThresholdLabel = L10n.TextContent("Error Threshold", $"ShaderGraph displays an error message when the user tries to create more custom interpolators than permitted by this setting. The number of interpolators that trigger this error must be between {kMinChannelThreshold} and {kMaxChannelThreshold}.");
            public static readonly GUIContent ReadMore = L10n.TextContent("Read more");
        }

        SerializedObject m_SerializedObject;
        SerializedProperty m_shaderVariantLimit;        
        SerializedProperty m_customInterpWarn;
        SerializedProperty m_customInterpError;

        public ShaderGraphProjectSettingsProvider(string path, SettingsScope scopes, IEnumerable<string> keywords = null) : base(path, scopes, keywords)
        {
            guiHandler = OnGUIHandler;
        }

        public override void OnActivate(string searchContext, VisualElement rootElement)
        {
            ShaderGraphProjectSettings.instance.Save();
            m_SerializedObject = ShaderGraphProjectSettings.instance.GetSerializedObject();
            m_shaderVariantLimit = m_SerializedObject.FindProperty("shaderVariantLimit");            
            m_customInterpWarn = m_SerializedObject.FindProperty("customInterpolatorWarningThreshold");
            m_customInterpError = m_SerializedObject.FindProperty("customInterpolatorErrorThreshold");
        }

        int oldWarningThreshold;
        void OnGUIHandler(string searchContext)
        {
            m_SerializedObject.Update();

            EditorGUI.BeginChangeCheck();
            
            var newValue = EditorGUILayout.DelayedIntField(Styles.shaderVariantLimitLabel, m_shaderVariantLimit.intValue);
            newValue = Mathf.Max(0, newValue);
            if (newValue != m_shaderVariantLimit.intValue)
            {
                m_shaderVariantLimit.intValue = newValue;
                if (ShaderGraphPreferences.onVariantLimitChanged != null)
                    ShaderGraphPreferences.onVariantLimitChanged();
            }

            EditorGUILayout.LabelField(Styles.CustomInterpLabel, EditorStyles.boldLabel);
            EditorGUI.indentLevel++;

            int newError = EditorGUILayout.IntField(Styles.CustomInterpErrorThresholdLabel, m_customInterpError.intValue);
            m_customInterpError.intValue = Mathf.Clamp(newError, kMinChannelThreshold, kMaxChannelThreshold);

            int oldWarn = m_customInterpWarn.intValue;
            int newWarn = EditorGUILayout.IntField(Styles.CustomInterpWarnThresholdLabel, m_customInterpWarn.intValue);

            // If the user did not modify the warning field, restore their previous input and reclamp against the new error threshold.
            if (oldWarn == newWarn)
                newWarn = oldWarningThreshold;
            else
                oldWarningThreshold = newWarn;

            m_customInterpWarn.intValue = Mathf.Clamp(newWarn, kMinChannelThreshold, m_customInterpError.intValue);

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
