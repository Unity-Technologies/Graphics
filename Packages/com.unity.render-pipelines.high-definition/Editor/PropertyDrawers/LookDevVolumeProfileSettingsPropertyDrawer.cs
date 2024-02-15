using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.UIElements;

namespace UnityEditor.Rendering.HighDefinition
{
    [CustomPropertyDrawer(typeof(LookDevVolumeProfileSettings))]
    [SupportedOnRenderPipeline(typeof(HDRenderPipelineAsset))]
    class LookDevVolumeProfileSettingsPropertyDrawer : PropertyDrawer
    {
        VisualElement m_Root;
        Editor m_LookDevVolumeProfileEditor;
        SerializedObject m_SettingsSerializedObject;
        SerializedProperty m_VolumeProfileSerializedProperty;
        EditorPrefBool m_DefaultVolumeProfileFoldoutExpanded;

        const int k_DefaultVolumeLabelWidth = 260;
        static readonly GUIContent k_LookDevVolumeProfileAssetLabel = EditorGUIUtility.TrTextContent("LookDev Profile");

        const int k_ImguiContainerLeftMargin = 32;
        const int k_AssetFieldBottomMargin = 6;

        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            m_Root = new VisualElement();
            
            var label = new Label("LookDev");
            label.style.unityFontStyleAndWeight = FontStyle.Bold;
            label.style.paddingTop = 6;
            m_Root.Add(label);

            m_SettingsSerializedObject = property.serializedObject;
            m_VolumeProfileSerializedProperty = property.FindPropertyRelative("m_VolumeProfile");
            m_DefaultVolumeProfileFoldoutExpanded = new EditorPrefBool($"{GetType()}.LookDevVolumeProfileFoldoutExpanded", false);

            var assetFieldUI = CreateAssetFieldUI();
            assetFieldUI.style.marginBottom = k_AssetFieldBottomMargin;
            m_Root.Add(assetFieldUI);

            var volumeEditorUI = CreateVolumeProfileEditorUI();
            volumeEditorUI.style.marginLeft = -k_ImguiContainerLeftMargin;
            m_Root.Add(volumeEditorUI);

            return m_Root;
        }

        Editor GetLookDevDefaultVolumeProfileEditor(VolumeProfile lookDevAsset)
        {
            Editor.CreateCachedEditor(lookDevAsset, typeof(VolumeProfileEditor), ref m_LookDevVolumeProfileEditor);
            return m_LookDevVolumeProfileEditor;
        }

        VisualElement CreateAssetFieldUI()
        {
            IMGUIContainer container = null;
            container = new IMGUIContainer(() =>
            {
                using var indentLevelScope = new EditorGUI.IndentLevelScope();
                using var changeScope = new EditorGUI.ChangeCheckScope();
                
                /* values adapted to the ProjectSettings > Graphics */
                var minWidth = 91;
                var indent = 94;
                var ratio = 0.45f;
                EditorGUIUtility.labelWidth = Mathf.Max(minWidth, (int)((container.worldBound.width - indent) * ratio));

                var lookDevVolumeProfileSettings = GraphicsSettings.GetRenderPipelineSettings<LookDevVolumeProfileSettings>();

                bool expanded = m_DefaultVolumeProfileFoldoutExpanded.value;
                VolumeProfile lookDevAsset = RenderPipelineGlobalSettingsUI.DrawVolumeProfileAssetField(
                    m_VolumeProfileSerializedProperty,
                    k_LookDevVolumeProfileAssetLabel,
                    getOrCreateVolumeProfile: () =>
                    {
                        if (lookDevVolumeProfileSettings.volumeProfile == null)
                        {
                            lookDevVolumeProfileSettings.volumeProfile = VolumeUtils.CopyVolumeProfileFromResourcesToAssets(GraphicsSettings
                                .GetRenderPipelineSettings<HDRenderPipelineEditorAssets>().lookDevVolumeProfile);
                        }

                        // When the built-in Reset context action is used, the asset becomes null outside of this scope.
                        // This is required to apply the new value to the serialized property.
                        GUI.changed = true;

                        return lookDevVolumeProfileSettings.volumeProfile;
                    },
                    ref expanded
                );
                m_DefaultVolumeProfileFoldoutExpanded.value = expanded;

                if (changeScope.changed)
                    m_SettingsSerializedObject.ApplyModifiedProperties();
            });
            return container;
        }

        static Lazy<GUIStyle> s_ImguiContainerScopeStyle = new(() => new GUIStyle
        {
            padding = new RectOffset(k_ImguiContainerLeftMargin, 0, 0, 0)
        });

        VisualElement CreateVolumeProfileEditorUI()
        {
            return new IMGUIContainer(() =>
            {
                using var imguiContainerScope = new EditorGUILayout.VerticalScope(s_ImguiContainerScopeStyle.Value);
                using var indentLevelScope = new EditorGUI.IndentLevelScope();

                var lookDevAsset = m_VolumeProfileSerializedProperty.objectReferenceValue as VolumeProfile;
                if (lookDevAsset != null && m_DefaultVolumeProfileFoldoutExpanded.value)
                {
                    var editor = GetLookDevDefaultVolumeProfileEditor(lookDevAsset) as VolumeProfileEditor;

                    bool oldEnabled = GUI.enabled;
                    GUI.enabled = AssetDatabase.IsOpenForEdit(lookDevAsset);
                    editor.OnInspectorGUI();
                    GUI.enabled = oldEnabled;

                    if (lookDevAsset.Has<VisualEnvironment>())
                        EditorGUILayout.HelpBox("VisualEnvironment is not modifiable and will be overridden by the LookDev", MessageType.Warning);
                    if (lookDevAsset.Has<HDRISky>())
                        EditorGUILayout.HelpBox("HDRISky is not modifiable and will be overridden by the LookDev", MessageType.Warning);
                }
            });
        }

        public class LookDevVolumeProfileSettingsContextMenu : IRenderPipelineGraphicsSettingsContextMenu<LookDevVolumeProfileSettings>
        {
            public void PopulateContextMenu(LookDevVolumeProfileSettings setting, PropertyDrawer drawer, ref GenericMenu menu)
            {
                menu.AddSeparator("");

                LookDevVolumeProfileSettingsPropertyDrawer lookDevDrawer = drawer as LookDevVolumeProfileSettingsPropertyDrawer;
                VolumeProfileEditor lookDevVolumeProfileEditor = lookDevDrawer?.m_LookDevVolumeProfileEditor as VolumeProfileEditor;
                var componentEditors = lookDevVolumeProfileEditor != null ? lookDevVolumeProfileEditor.componentList.editors : null;
                VolumeProfileUtils.AddVolumeProfileContextMenuItems(ref menu,
                    setting.volumeProfile,
                    componentEditors,
                    overrideStateOnReset: true,
                    defaultVolumeProfilePath: $"Assets/{HDProjectSettings.projectSettingsFolderPath}/LookDevProfile_Default.asset",
                    onNewVolumeProfileCreated: createdProfile =>
                    {
                        lookDevDrawer.m_VolumeProfileSerializedProperty.objectReferenceValue = createdProfile;
                        lookDevDrawer.m_VolumeProfileSerializedProperty.serializedObject.ApplyModifiedProperties();
                    });
            }
        }
    }
}
