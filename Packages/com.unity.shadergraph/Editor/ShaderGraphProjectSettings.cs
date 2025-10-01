using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Serialization;
using UnityEngine.UIElements;
using static UnityEngine.GraphicsBuffer;

namespace UnityEditor.ShaderGraph
{
    [FilePath("ProjectSettings/ShaderGraphSettings.asset", FilePathAttribute.Location.ProjectFolder)]
    internal class ShaderGraphProjectSettings : ScriptableSingleton<ShaderGraphProjectSettings>
    {
        static internal readonly int defaultVariantLimit = 2048;

        [SerializeField]
        internal int shaderVariantLimit = defaultVariantLimit;
        [SerializeField]
        internal bool overrideShaderVariantLimit = false;
        [SerializeField]
        internal int customInterpolatorErrorThreshold = 32;
        [SerializeField]
        internal int customInterpolatorWarningThreshold = 16;
        [SerializeField]
        internal ShaderGraphHeatmapValues customHeatmapValues;

        internal SerializedObject GetSerializedObject() { return new SerializedObject(this); }
        internal void Save() { Save(true); }
        private void OnDisable() { Save(); }

        public ShaderGraphHeatmapValues GetHeatValues()
        {
            return customHeatmapValues != null ? customHeatmapValues : ShaderGraphHeatmapValues.GetPackageDefault();
        }
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
            public static readonly GUIContent overrideShaderVariantLimitLabel = L10n.TextContent("Override Variant Limit", $"The Shader Graph internal default limit Shader Variant Limit is {ShaderGraphProjectSettings.defaultVariantLimit}.");
            public static readonly GUIContent CustomInterpLabel = L10n.TextContent("Custom Interpolator Channel Settings", "");
            public static readonly GUIContent CustomInterpWarnThresholdLabel = L10n.TextContent("Warning Threshold", $"ShaderGraph displays a warning when the user creates more custom interpolators than permitted by this setting. The number of interpolators that trigger this warning must be between {kMinChannelThreshold} and the Error Threshold.");
            public static readonly GUIContent CustomInterpErrorThresholdLabel = L10n.TextContent("Error Threshold", $"ShaderGraph displays an error message when the user tries to create more custom interpolators than permitted by this setting. The number of interpolators that trigger this error must be between {kMinChannelThreshold} and {kMaxChannelThreshold}.");
            public static readonly GUIContent ReadMore = L10n.TextContent("Read more");

            public static readonly GUIContent HeatmapSectionLabel = L10n.TextContent("Heatmap Color Mode Settings", "");
            public static readonly GUIContent HeatmapAssetLabel = L10n.TextContent("Custom Values", "Specifies a custom Heatmap Values asset with data to display in the Heatmap color mode. If empty, a set of default values will be used.");
        }

        SerializedObject m_SerializedObject;
        SerializedProperty m_shaderVariantLimit;
        SerializedProperty m_overrideShaderVariantLimit;
        SerializedProperty m_customInterpWarn;
        SerializedProperty m_customInterpError;
        SerializedProperty m_HeatValues;

        public ShaderGraphProjectSettingsProvider(string path, SettingsScope scopes, IEnumerable<string> keywords = null) : base(path, scopes, keywords)
        {
            guiHandler = OnGUIHandler;
        }

        public override void OnActivate(string searchContext, VisualElement rootElement)
        {
            ShaderGraphProjectSettings.instance.Save();
            m_SerializedObject = ShaderGraphProjectSettings.instance.GetSerializedObject();
            m_shaderVariantLimit = m_SerializedObject.FindProperty("shaderVariantLimit");
            m_overrideShaderVariantLimit = m_SerializedObject.FindProperty("overrideShaderVariantLimit");
            m_customInterpWarn = m_SerializedObject.FindProperty("customInterpolatorWarningThreshold");
            m_customInterpError = m_SerializedObject.FindProperty("customInterpolatorErrorThreshold");
            m_HeatValues = m_SerializedObject.FindProperty(nameof(ShaderGraphProjectSettings.customHeatmapValues));
        }

        int oldWarningThreshold;
        void OnGUIHandler(string searchContext)
        {
            m_SerializedObject.Update();

            EditorGUI.BeginChangeCheck();

            var doOverride = EditorGUILayout.Toggle(Styles.overrideShaderVariantLimitLabel, m_overrideShaderVariantLimit.boolValue);
            if (doOverride != m_overrideShaderVariantLimit.boolValue)
            {
                m_overrideShaderVariantLimit.boolValue = doOverride;
                if (ShaderGraphPreferences.onVariantLimitChanged != null)
                    ShaderGraphPreferences.onVariantLimitChanged();
            }

            using (new EditorGUI.DisabledScope(!doOverride))
            {
                var newVariantLimitValue = EditorGUILayout.DelayedIntField(Styles.shaderVariantLimitLabel, m_shaderVariantLimit.intValue);
                newVariantLimitValue = Mathf.Max(0, newVariantLimitValue);
                if (newVariantLimitValue != m_shaderVariantLimit.intValue)
                {
                    m_shaderVariantLimit.intValue = newVariantLimitValue;
                    if (ShaderGraphPreferences.onVariantLimitChanged != null)
                        ShaderGraphPreferences.onVariantLimitChanged();
                }
            }

            EditorGUILayout.LabelField(Styles.CustomInterpLabel, EditorStyles.boldLabel);
            EditorGUI.indentLevel++;

            int oldError = m_customInterpError.intValue;
            int oldWarn = m_customInterpWarn.intValue;

            int newError = EditorGUILayout.DelayedIntField(Styles.CustomInterpErrorThresholdLabel, oldError);
            int newWarn = EditorGUILayout.DelayedIntField(Styles.CustomInterpWarnThresholdLabel, oldWarn);

            m_customInterpError.intValue = Mathf.Clamp(newError, oldWarn, kMaxChannelThreshold);
            m_customInterpWarn.intValue = Mathf.Clamp(newWarn, kMinChannelThreshold, oldError);

            GUILayout.BeginHorizontal(EditorStyles.helpBox);
            GUILayout.Label(EditorGUIUtility.IconContent("console.infoicon"), GUILayout.ExpandWidth(true));
            GUILayout.Box(kCustomInterpolatorHelpBox, EditorStyles.wordWrappedLabel);
            if (EditorGUILayout.LinkButton(Styles.ReadMore))
            {
                System.Diagnostics.Process.Start(kCustomInterpolatorDocumentationURL);
            }
            GUILayout.EndHorizontal();
            EditorGUI.indentLevel--;

            EditorGUILayout.LabelField(Styles.HeatmapSectionLabel, EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            var oldHeatValues = (ShaderGraphHeatmapValues) m_HeatValues.objectReferenceValue;
            var newHeatValues = EditorGUILayout.ObjectField(Styles.HeatmapAssetLabel, oldHeatValues, typeof(ShaderGraphHeatmapValues), false);
            if (oldHeatValues != newHeatValues)
            {
                m_HeatValues.objectReferenceValue = newHeatValues;
            }

            EditorGUI.indentLevel--;

            if (EditorGUI.EndChangeCheck())
            {
                m_SerializedObject.ApplyModifiedProperties();
                ShaderGraphProjectSettings.instance.Save();

                if (oldHeatValues != newHeatValues)
                {
                    ShaderGraphHeatmapValuesEditor.UpdateShaderGraphWindows();
                }
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
