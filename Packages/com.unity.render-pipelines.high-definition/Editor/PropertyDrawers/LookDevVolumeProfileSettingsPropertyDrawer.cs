using System;
using UnityEditor.UIElements;
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
        int m_LookDevVolumeProfileHash = -1;
        SerializedObject m_SettingsSerializedObject;
        SerializedProperty m_VolumeProfileSerializedProperty;
        EditorPrefBool m_DefaultVolumeProfileFoldoutExpanded;
        VisualElement m_EditorContainer;

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

            m_EditorContainer = new VisualElement();
            var assetFieldUI = CreateAssetFieldUI();
            assetFieldUI.style.marginBottom = k_AssetFieldBottomMargin;
            m_Root.Add(assetFieldUI);

            m_EditorContainer.Add(CreateVolumeProfileEditorUI());
            m_EditorContainer.style.marginLeft = -k_ImguiContainerLeftMargin;
            m_Root.Add(m_EditorContainer);

            return m_Root;
        }

        Editor GetLookDevDefaultVolumeProfileEditor(VolumeProfile lookDevAsset)
        {
            int currentHash = (lookDevAsset != null) ? lookDevAsset.GetHashCode() : -1;
            if (currentHash != m_LookDevVolumeProfileHash)
            {
                Editor.DestroyImmediate(m_LookDevVolumeProfileEditor);
                m_LookDevVolumeProfileEditor = null;
                m_LookDevVolumeProfileHash = currentHash;
            }

            Editor.CreateCachedEditor(lookDevAsset, typeof(VolumeProfileEditor), ref m_LookDevVolumeProfileEditor);
            return m_LookDevVolumeProfileEditor;
        }

        VisualElement CreateAssetFieldUI()
        {
            var lookDevVolumeProfileSettings = GraphicsSettings.GetRenderPipelineSettings<LookDevVolumeProfileSettings>();

            VisualElement profileLine = new();
            var toggle = new Toggle();
            toggle.AddToClassList(Foldout.toggleUssClassName);
            var checkmark = toggle.Q(className: Toggle.checkmarkUssClassName);
            checkmark.AddToClassList(Foldout.checkmarkUssClassName);
            var field = new ObjectField(k_LookDevVolumeProfileAssetLabel.text)
            {
                tooltip = k_LookDevVolumeProfileAssetLabel.tooltip,
                objectType = typeof(VolumeProfile),
                style =
                {
                    flexShrink = 1,
                }
            };
            field.BindProperty(m_VolumeProfileSerializedProperty);
            field.AddToClassList("unity-base-field__aligned"); //Align with other BaseField<T>
            field.Q<Label>().RegisterCallback<ClickEvent>(evt => toggle.value ^= true);

            toggle.RegisterValueChangedCallback(evt =>
            {
                m_EditorContainer.style.display = evt.newValue ? DisplayStyle.Flex : DisplayStyle.None;
                m_DefaultVolumeProfileFoldoutExpanded.value = evt.newValue;
            });
            toggle.SetValueWithoutNotify(m_DefaultVolumeProfileFoldoutExpanded.value);
            m_EditorContainer.style.display = m_DefaultVolumeProfileFoldoutExpanded.value ? DisplayStyle.Flex : DisplayStyle.None;

            profileLine.Add(toggle);
            profileLine.Add(field);
            profileLine.style.flexDirection = FlexDirection.Row;
            field.style.flexGrow = 1;

            field.RegisterValueChangedCallback(evt =>
            {
                if (evt.newValue == evt.previousValue)
                    return;

                if (GraphicsSettings.TryGetRenderPipelineSettings<LookDevVolumeProfileSettings>(out var lookDevVolumeProfileSettings))
                {
                    lookDevVolumeProfileSettings.volumeProfile = evt.newValue as VolumeProfile;
                    m_VolumeProfileSerializedProperty.objectReferenceValue = lookDevVolumeProfileSettings.volumeProfile;
                    m_VolumeProfileSerializedProperty.serializedObject.ApplyModifiedProperties();
                    m_SettingsSerializedObject.Update();
                }

                m_LookDevVolumeProfileHash = -1; // Invalidate the hash, to allow the IMGUI container recreate the editor
            });

            return profileLine;
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
                else
                {
                    EditorGUILayout.HelpBox("No Look Dev Volume Profile assigned. A default profile will be created automatically when you open the Look Dev tool (Window > Rendering > Look Dev).", MessageType.Info);
                }
            });
        }
    }
}
