using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    internal class LabelWidthScope : GUI.Scope
    {
        float m_previewLabelWidth;
        internal LabelWidthScope(int labelPadding = 10, int labelWidth = 251)
        {
            m_previewLabelWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = labelWidth;
            GUILayout.BeginHorizontal();
            GUILayout.Space(labelPadding);
            GUILayout.BeginVertical();
        }

        protected override void CloseScope()
        {
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();
            EditorGUIUtility.labelWidth = m_previewLabelWidth;
        }
    }

    static class ShaderGraphPreferences
    {
        static class Keys
        {
            internal const string variantLimit = "UnityEditor.ShaderGraph.VariantLimit";
            internal const string autoAddRemoveBlocks = "UnityEditor.ShaderGraph.AutoAddRemoveBlocks";
            internal const string allowDeprecatedBehaviors = "UnityEditor.ShaderGraph.AllowDeprecatedBehaviors";
            internal const string zoomStepSize = "UnityEditor.ShaderGraph.ZoomStepSize";
            internal const string graphTemplateWorkflow = "UnityEditor.ShaderGraph.GraphTemplateWorkflow";
            internal const string openNewGraphOnCreation = "UnityEditor.ShaderGraph.OpenNewGraphOnCreation";
            internal const string newNodesPreview = "UnityEditor.ShaderGraph.NewNodesPreview";
        }

        static bool m_Loaded = false;
        internal delegate void PreferenceChangedDelegate();

        internal static PreferenceChangedDelegate onVariantLimitChanged;
        static int m_PreviewVariantLimit = 2048;
        internal static int previewVariantLimit
        {
            get { return m_PreviewVariantLimit; }
            set
            {
                if (onVariantLimitChanged != null)
                    onVariantLimitChanged();
                TrySave(ref m_PreviewVariantLimit, value, Keys.variantLimit);
            }
        }

        static bool m_AutoAddRemoveBlocks = true;
        internal static bool autoAddRemoveBlocks
        {
            get => m_AutoAddRemoveBlocks;
            set => TrySave(ref m_AutoAddRemoveBlocks, value, Keys.autoAddRemoveBlocks);
        }

        internal static PreferenceChangedDelegate onAllowDeprecatedChanged;
        static bool m_AllowDeprecatedBehaviors = false;
        internal static bool allowDeprecatedBehaviors
        {
            get => m_AllowDeprecatedBehaviors;
            set
            {
                TrySave(ref m_AllowDeprecatedBehaviors, value, Keys.allowDeprecatedBehaviors);
                if (onAllowDeprecatedChanged != null)
                {
                    onAllowDeprecatedChanged();
                }
            }
        }

        internal static PreferenceChangedDelegate onZoomStepSizeChanged;
        const float defaultZoomStepSize = 0.5f;
        static float m_ZoomStepSize = defaultZoomStepSize;
        internal static float zoomStepSize
        {
            get => m_ZoomStepSize;
            set
            {
                TrySave(ref m_ZoomStepSize, value, Keys.zoomStepSize);
                if (onZoomStepSizeChanged != null)
                {
                    onZoomStepSizeChanged();
                }
            }
        }

        internal enum GraphTemplateWorkflow { MaterialVariant, Material }
        const GraphTemplateWorkflow defaultGraphTemplateWorkflow = GraphTemplateWorkflow.MaterialVariant;
        static GraphTemplateWorkflow m_GraphTemplateWorkflow = defaultGraphTemplateWorkflow;
        static GraphTemplateWorkflow graphTemplateWorkflow
        {
            get => m_GraphTemplateWorkflow;
            set => TrySave(ref m_GraphTemplateWorkflow, value, Keys.graphTemplateWorkflow);
        }

        internal static GraphTemplateWorkflow GetOrPromptGraphTemplateWorkflow()
        {
            if (!EditorPrefs.HasKey(Keys.graphTemplateWorkflow))
            {
                bool userPref = EditorUtility.DisplayDialog("Shader Graph Preferences", "Should the new Material be made a variant of the Shader Graph sub asset, or a Material using the Shader?\nThis can be changed later in Editor Preferences.", "Material Variant", "Material");
                TrySave(ref m_GraphTemplateWorkflow, userPref ? GraphTemplateWorkflow.MaterialVariant : GraphTemplateWorkflow.Material, Keys.graphTemplateWorkflow, true);
            }
            return m_GraphTemplateWorkflow;
        }

        static bool m_OpenNewGraphOnCreation = true;
        static bool openNewGraphOnCreation
        {
            get => m_OpenNewGraphOnCreation;
            set => TrySave(ref m_OpenNewGraphOnCreation, value, Keys.openNewGraphOnCreation);
        }

        static bool m_NewNodesPreview = true;
        internal static bool newNodesPreview
        {
            get => m_NewNodesPreview;
            private set => TrySave(ref m_NewNodesPreview, value, Keys.newNodesPreview);
        }

        internal static bool GetOrPromptOpenNewGraphOnCreation()
        {
            if (!EditorPrefs.HasKey(Keys.openNewGraphOnCreation))
            {
                bool userPref = EditorUtility.DisplayDialog("Shader Graph Preferences", "Should Shader Graph assets open upon creation?\nThis can be changed later in Editor Preferences.", "Yes", "No");
                TrySave(ref m_OpenNewGraphOnCreation, userPref, Keys.openNewGraphOnCreation, true);
            }
            return m_OpenNewGraphOnCreation;
        }

        static ShaderGraphPreferences()
        {
            Load();
        }

        [SettingsProvider]
        static SettingsProvider PreferenceGUI()
        {
            return new SettingsProvider("Preferences/Shader Graph", SettingsScope.User)
            {
                guiHandler = searchContext => OpenGUI()
            };
        }

        static void OpenGUI()
        {
            if (!m_Loaded)
                Load();

            using (var scope = new LabelWidthScope(10, 300))
            {
                var actualLimit = ShaderGraphProjectSettings.instance.overrideShaderVariantLimit
                    ? ShaderGraphProjectSettings.instance.shaderVariantLimit
                    : ShaderGraphProjectSettings.defaultVariantLimit;
                var willPreviewVariantBeIgnored = ShaderGraphPreferences.previewVariantLimit > actualLimit || ShaderGraphProjectSettings.instance.overrideShaderVariantLimit;

                var variantLimitLabel = willPreviewVariantBeIgnored
                    ? new GUIContent("Preview Variant Limit", EditorGUIUtility.IconContent("console.infoicon").image, $"The Preview Variant Limit is higher than the Shader Variant Limit in Project Settings: {actualLimit}. The Preview Variant Limit will be ignored.")
                    : new GUIContent("Preview Variant Limit");

                EditorGUI.BeginChangeCheck();
                var variantLimitValue = EditorGUILayout.DelayedIntField(variantLimitLabel, previewVariantLimit);
                variantLimitValue = Mathf.Max(0, variantLimitValue);
                if (EditorGUI.EndChangeCheck())
                {
                    previewVariantLimit = variantLimitValue;
                }

                EditorGUI.BeginChangeCheck();
                var autoAddRemoveBlocksValue = EditorGUILayout.Toggle("Automatically Add and Remove Block Nodes", autoAddRemoveBlocks);
                if (EditorGUI.EndChangeCheck())
                {
                    autoAddRemoveBlocks = autoAddRemoveBlocksValue;
                }

                EditorGUI.BeginChangeCheck();
                var allowDeprecatedBehaviorsValue = EditorGUILayout.Toggle("Enable Deprecated Nodes", allowDeprecatedBehaviors);
                if (EditorGUI.EndChangeCheck())
                {
                    allowDeprecatedBehaviors = allowDeprecatedBehaviorsValue;
                }

                EditorGUI.BeginChangeCheck();
                var zoomStepSizeValue = EditorGUILayout.Slider(new GUIContent("Zoom Step Size", $"Default is 0.5"), zoomStepSize, 0.0f, 1f);
                if (EditorGUI.EndChangeCheck())
                {
                    zoomStepSize = zoomStepSizeValue;
                }

                EditorGUI.BeginChangeCheck();
                var graphTemplateWorkflowValue = EditorGUILayout.EnumPopup(new GUIContent("Graph Template Workflow", "When creating a new Shadergraph asset from specific menu items, determine if a reference should use a variant of the newly created material sub-asset or the shader itself."), graphTemplateWorkflow);
                if (EditorGUI.EndChangeCheck())
                {
                    graphTemplateWorkflow = (GraphTemplateWorkflow)graphTemplateWorkflowValue;
                }

                EditorGUI.BeginChangeCheck();
                var openNewGraphOnCreationValue = EditorGUILayout.Toggle(new GUIContent("Open new Shader Graphs automatically", "Choose whether new ShaderGraph assets should automatically open for editing."), openNewGraphOnCreation);
                if (EditorGUI.EndChangeCheck())
                {
                    openNewGraphOnCreation = openNewGraphOnCreationValue;
                }

                EditorGUI.BeginChangeCheck();
                var newNodesPreviewValue = EditorGUILayout.Toggle(new GUIContent("Expand Node Preview on Node creation", "Choose whether newly added Nodes' Previews should be expanded."), newNodesPreview);
                if (EditorGUI.EndChangeCheck())
                {
                    newNodesPreview = newNodesPreviewValue;
                }
            }
        }

        static void Load()
        {
            m_PreviewVariantLimit = EditorPrefs.GetInt(Keys.variantLimit, 128);
            m_AutoAddRemoveBlocks = EditorPrefs.GetBool(Keys.autoAddRemoveBlocks, true);
            m_AllowDeprecatedBehaviors = EditorPrefs.GetBool(Keys.allowDeprecatedBehaviors, false);
            m_ZoomStepSize = EditorPrefs.GetFloat(Keys.zoomStepSize, defaultZoomStepSize);
            m_GraphTemplateWorkflow = (GraphTemplateWorkflow)EditorPrefs.GetInt(Keys.graphTemplateWorkflow, (int)defaultGraphTemplateWorkflow);
            m_OpenNewGraphOnCreation = EditorPrefs.GetBool(Keys.openNewGraphOnCreation, true);
            m_NewNodesPreview = EditorPrefs.GetBool(Keys.newNodesPreview, true);
            m_Loaded = true;
        }

        static void TrySave<T>(ref T field, T newValue, string key, bool forceSave = false)
        {
            if (field.Equals(newValue) && !forceSave)
                return;

            if (typeof(T) == typeof(float))
                EditorPrefs.SetFloat(key, (float)(object)newValue);
            else if (typeof(T) == typeof(int))
                EditorPrefs.SetInt(key, (int)(object)newValue);
            else if (typeof(T) == typeof(bool))
                EditorPrefs.SetBool(key, (bool)(object)newValue);
            else if (typeof(T) == typeof(string))
                EditorPrefs.SetString(key, (string)(object)newValue);
            else if (typeof(T).IsEnum)
                EditorPrefs.SetInt(key, (int)(object)newValue);

            field = newValue;
        }
    }
}
