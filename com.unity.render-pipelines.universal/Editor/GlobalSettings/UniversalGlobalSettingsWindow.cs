using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering;
using UnityEngine.Scripting.APIUpdating;
using UnityEditorInternal;
using System.Collections.Generic;
using UnityEngine.UIElements;
using System.Linq;

namespace UnityEditor.Rendering.Universal
{
    using CED = CoreEditorDrawer<SerializedUniversalRenderPipelineGlobalSettings>;

    class UniversalGlobalSettingsPanelProvider
    {
        static UniversalGlobalSettingsPanelIMGUI s_IMGUIImpl = new UniversalGlobalSettingsPanelIMGUI();

        [SettingsProvider]
        public static SettingsProvider CreateSettingsProvider()
        {
            return new SettingsProvider("Project/Graphics/URP Settings", SettingsScope.Project)
            {
                activateHandler = s_IMGUIImpl.OnActivate,
                keywords = SettingsProvider.GetSearchKeywordsFromGUIContentProperties<UniversalGlobalSettingsPanelIMGUI.Styles>().ToArray(),
                guiHandler = s_IMGUIImpl.DoGUI
            };
        }
    }

    internal class UniversalGlobalSettingsPanelIMGUI
    {
        public class Styles
        {
            public const int labelWidth = 220;
            internal static GUIStyle sectionHeaderStyle = new GUIStyle(EditorStyles.largeLabel) { richText = true, fontSize = 18, fixedHeight = 42 };
            internal static GUIStyle subSectionHeaderStyle = new GUIStyle(EditorStyles.boldLabel);

            internal static readonly GUIContent lightLayersLabel = EditorGUIUtility.TrTextContent("Light Layer Names (3D)", "If the Light Layers feature is enabled in the URP Asset, Unity allocates memory for processing Light Layers. In the Deferred Rendering Path, this allocation includes an extra render target in GPU memory, which reduces performance.");
            internal static readonly GUIContent lightLayerName0 = EditorGUIUtility.TrTextContent("Light Layer 0", "The display name for Light Layer 0.");
            internal static readonly GUIContent lightLayerName1 = EditorGUIUtility.TrTextContent("Light Layer 1", "The display name for Light Layer 1.");
            internal static readonly GUIContent lightLayerName2 = EditorGUIUtility.TrTextContent("Light Layer 2", "The display name for Light Layer 2.");
            internal static readonly GUIContent lightLayerName3 = EditorGUIUtility.TrTextContent("Light Layer 3", "The display name for Light Layer 3.");
            internal static readonly GUIContent lightLayerName4 = EditorGUIUtility.TrTextContent("Light Layer 4", "The display name for Light Layer 4.");
            internal static readonly GUIContent lightLayerName5 = EditorGUIUtility.TrTextContent("Light Layer 5", "The display name for Light Layer 5.");
            internal static readonly GUIContent lightLayerName6 = EditorGUIUtility.TrTextContent("Light Layer 6", "The display name for Light Layer 6.");
            internal static readonly GUIContent lightLayerName7 = EditorGUIUtility.TrTextContent("Light Layer 7", "The display name for Light Layer 7.");

            internal static readonly GUIContent miscSettingsLabel = EditorGUIUtility.TrTextContent("Miscellaneous");
            internal static readonly GUIContent supportRuntimeDebugDisplayContentLabel = EditorGUIUtility.TrTextContent("Runtime Debug Shaders", "When disabled, all debug display shader variants are removed when you build for the Unity Player. This decreases build time, but prevents the use of Rendering Debugger in Player builds.");

            internal static readonly string warningUrpNotActive = "Project graphics settings do not refer to a URP Asset. Check the settings: Graphics > Scriptable Render Pipeline Settings, Quality > Render Pipeline Asset.";
            internal static readonly string warningGlobalSettingsMissing = "The URP Settings property does not contain a valid URP Global Settings asset. There might be issues in rendering. Select a valid URP Global Settings asset.";
            internal static readonly string infoGlobalSettingsMissing = "Select a URP Global Settings asset.";
        }

        /// <summary>
        /// Like EditorGUILayout.DrawTextField but for delayed text field
        /// </summary>
        internal static void DrawDelayedTextField(GUIContent label, SerializedProperty property)
        {
            Rect lineRect = GUILayoutUtility.GetRect(1, EditorGUIUtility.singleLineHeight);
            EditorGUI.BeginProperty(lineRect, label, property);
            EditorGUI.BeginChangeCheck();
            string value = EditorGUI.DelayedTextField(lineRect, label, property.stringValue);
            if (EditorGUI.EndChangeCheck())
                property.stringValue = value;
            EditorGUI.EndProperty();
        }

        public static readonly CED.IDrawer Inspector;

        static UniversalGlobalSettingsPanelIMGUI()
        {
            Inspector = CED.Group(
                LightLayerNamesSection,
                MiscSection
            );
        }

        SerializedUniversalRenderPipelineGlobalSettings serializedSettings;
        UniversalRenderPipelineGlobalSettings settingsSerialized;
        public void DoGUI(string searchContext)
        {
            // When the asset being serialized has been deleted before its reconstruction
            if (serializedSettings != null && serializedSettings.serializedObject.targetObject == null)
            {
                serializedSettings = null;
                settingsSerialized = null;
            }

            if (serializedSettings == null || settingsSerialized != UniversalRenderPipelineGlobalSettings.instance)
            {
                if (UniversalRenderPipeline.asset != null || UniversalRenderPipelineGlobalSettings.instance != null)
                {
                    settingsSerialized = UniversalRenderPipelineGlobalSettings.Ensure();
                    var serializedObject = new SerializedObject(settingsSerialized);
                    serializedSettings = new SerializedUniversalRenderPipelineGlobalSettings(serializedObject);
                }
            }
            else if (settingsSerialized != null && serializedSettings != null)
            {
                serializedSettings.serializedObject.Update();
            }

            DrawAssetSelection(ref serializedSettings, null);
            DrawWarnings(ref serializedSettings, null);
            if (settingsSerialized != null && serializedSettings != null)
            {
                EditorGUILayout.Space();
                Inspector.Draw(serializedSettings, null);
                serializedSettings.serializedObject?.ApplyModifiedProperties();
            }
        }

        /// <summary>
        /// Executed when activate is called from the settings provider.
        /// </summary>
        /// <param name="searchContext"></param>
        /// <param name="rootElement"></param>
        public void OnActivate(string searchContext, VisualElement rootElement)
        {
        }

        void DrawWarnings(ref SerializedUniversalRenderPipelineGlobalSettings serialized, Editor owner)
        {
            bool isURPinUse = UniversalRenderPipeline.asset != null;
            if (isURPinUse && serialized != null)
                return;

            if (isURPinUse)
            {
                EditorGUILayout.HelpBox(Styles.warningGlobalSettingsMissing, MessageType.Warning);
            }
            else
            {
                EditorGUILayout.HelpBox(Styles.warningUrpNotActive, MessageType.Warning);
                if (serialized == null)
                    EditorGUILayout.HelpBox(Styles.infoGlobalSettingsMissing, MessageType.Info);
            }
        }

        #region Universal Global Settings asset selection
        void DrawAssetSelection(ref SerializedUniversalRenderPipelineGlobalSettings serialized, Editor owner)
        {
            var oldWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = Styles.labelWidth;
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUI.BeginChangeCheck();
                var newAsset = (UniversalRenderPipelineGlobalSettings)EditorGUILayout.ObjectField(settingsSerialized, typeof(UniversalRenderPipelineGlobalSettings), false);
                if (EditorGUI.EndChangeCheck())
                {
                    UniversalRenderPipelineGlobalSettings.UpdateGraphicsSettings(newAsset);
                    if (settingsSerialized != null && !settingsSerialized.Equals(null))
                        EditorUtility.SetDirty(settingsSerialized);
                }

                if (GUILayout.Button(EditorGUIUtility.TrTextContent("New", "Create a URP Global Settings asset in the Assets folder."), GUILayout.Width(45), GUILayout.Height(18)))
                {
                    UniversalGlobalSettingsCreator.Create(activateAsset: true);
                }

                bool guiEnabled = GUI.enabled;
                GUI.enabled = guiEnabled && (settingsSerialized != null);
                if (GUILayout.Button(EditorGUIUtility.TrTextContent("Clone", "Clone a URP Global Settings asset in the Assets folder."), GUILayout.Width(45), GUILayout.Height(18)))
                {
                    UniversalGlobalSettingsCreator.Clone(settingsSerialized, activateAsset: true);
                }
                GUI.enabled = guiEnabled;
            }
            EditorGUIUtility.labelWidth = oldWidth;
            EditorGUILayout.Space();
        }

        #endregion
        #region Rendering Layer Names

        static readonly CED.IDrawer LightLayerNamesSection = CED.Group(
            CED.Group((serialized, owner) => EditorGUILayout.LabelField(Styles.lightLayersLabel, Styles.sectionHeaderStyle)),
            CED.Group((serialized, owner) => EditorGUILayout.Space()),
            CED.Group(DrawLightLayerNames),
            CED.Group((serialized, owner) => EditorGUILayout.Space())
        );

        static void DrawLightLayerNames(SerializedUniversalRenderPipelineGlobalSettings serialized, Editor owner)
        {
            var oldWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = Styles.labelWidth;

            using (new EditorGUI.IndentLevelScope())
            {
                DrawDelayedTextField(Styles.lightLayerName0, serialized.lightLayerName0);
                GUILayout.Space(2);
                DrawDelayedTextField(Styles.lightLayerName1, serialized.lightLayerName1);
                GUILayout.Space(2);
                DrawDelayedTextField(Styles.lightLayerName2, serialized.lightLayerName2);
                GUILayout.Space(2);
                DrawDelayedTextField(Styles.lightLayerName3, serialized.lightLayerName3);
                GUILayout.Space(2);
                DrawDelayedTextField(Styles.lightLayerName4, serialized.lightLayerName4);
                GUILayout.Space(2);
                DrawDelayedTextField(Styles.lightLayerName5, serialized.lightLayerName5);
                GUILayout.Space(2);
                DrawDelayedTextField(Styles.lightLayerName6, serialized.lightLayerName6);
                GUILayout.Space(2);
                DrawDelayedTextField(Styles.lightLayerName7, serialized.lightLayerName7);
                EditorGUILayout.Space();
            }

            EditorGUIUtility.labelWidth = oldWidth;
        }

        #endregion

        #region Misc Settings

        static readonly CED.IDrawer MiscSection = CED.Group(
            CED.Group((serialized, owner) => EditorGUILayout.LabelField(Styles.miscSettingsLabel, Styles.sectionHeaderStyle)),
            CED.Group((serialized, owner) => EditorGUILayout.Space()),
            CED.Group(DrawMiscSettings),
            CED.Group((serialized, owner) => EditorGUILayout.Space())
        );

        static void DrawMiscSettings(SerializedUniversalRenderPipelineGlobalSettings serialized, Editor owner)
        {
            var oldWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = Styles.labelWidth;

            using (new EditorGUI.IndentLevelScope())
            {
                EditorGUILayout.PropertyField(serialized.supportRuntimeDebugDisplay, Styles.supportRuntimeDebugDisplayContentLabel);
            }

            EditorGUIUtility.labelWidth = oldWidth;
        }

        #endregion
    }
}
