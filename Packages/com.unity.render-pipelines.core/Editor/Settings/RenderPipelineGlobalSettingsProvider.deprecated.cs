using System;
using System.Reflection;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UIElements;

namespace UnityEditor.Rendering
{
    /// <summary>
    /// Render Pipeline Settings provider
    /// </summary>
    /// <typeparam name="TRenderPipeline"><see cref="RenderPipeline"/></typeparam>
    /// <typeparam name="TGlobalSettings"><see cref="RenderPipelineGlobalSettings"/></typeparam>
    [Obsolete("This is obsolete. GlobalSettingsAsset content are transformed in IRenderPipelineGraphicsSettings and they are directly embedded in Projectsettings > Graphics #from(2023.3)")]
    public abstract class RenderPipelineGlobalSettingsProvider<TRenderPipeline, TGlobalSettings> : SettingsProvider
        where TRenderPipeline : RenderPipeline
        where TGlobalSettings : RenderPipelineGlobalSettings
    {
        static class Styles
        {
            public static readonly string warningGlobalSettingsMissing = "Select a valid {0} asset.";
            public static readonly string warningSRPNotActive = "Current Render Pipeline is {0}. Check the settings: Graphics > Scriptable Render Pipeline Settings, Quality > Render Pipeline Asset.";
            public static readonly string settingNullRPSettings = "Invalid Global Settings asset. Rollback to the previous value";

            public static readonly GUIContent newAssetButtonLabel = EditorGUIUtility.TrTextContent("New", "Create a Global Settings asset in the Assets folder.");
            public static readonly GUIContent cloneAssetButtonLabel = EditorGUIUtility.TrTextContent("Clone", "Clone a Global Settings asset in the Assets folder.");
            public static readonly GUILayoutOption[] buttonOptions = new GUILayoutOption[] { GUILayout.Width(45), GUILayout.Height(18) };
        }

        private const string k_TemplatePath = "Packages/com.unity.render-pipelines.core/Editor/UXML/RenderPipelineGlobalSettings.uxml";

        Editor m_Editor;
        SupportedOnRenderPipelineAttribute m_SupportedOnRenderPipeline;
        RenderPipelineGlobalSettings renderPipelineSettings => GraphicsSettings.GetSettingsForRenderPipeline<TRenderPipeline>();

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="v">The path of the settings</param>
        public RenderPipelineGlobalSettingsProvider(string v)
            : base(v, SettingsScope.Project)
        {
            m_SupportedOnRenderPipeline = GetType().GetCustomAttribute<SupportedOnRenderPipelineAttribute>();
        }

        /// <summary>
        /// Method called when the title bar is being rendered
        /// </summary>
        public override void OnTitleBarGUI()
        {
            if (GUILayout.Button(CoreEditorStyles.iconHelp, CoreEditorStyles.iconHelpStyle))
                Help.BrowseURL(Help.GetHelpURLForObject(renderPipelineSettings));
        }

        void DestroyEditor()
        {
            CoreUtils.Destroy(m_Editor);
            m_Editor = null;
        }

        /// <summary>
        /// This method is being called when the provider is activated
        /// </summary>
        /// <param name="searchContext">The context with the search</param>
        /// <param name="rootElement">The <see cref="VisualElement"/> with the root</param>
        public override void OnActivate(string searchContext, VisualElement rootElement)
        {
            DestroyEditor();
            base.OnActivate(searchContext, rootElement);

            var template = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(k_TemplatePath);
            var settingsRoot = template.Instantiate();

            settingsRoot.Q<Label>("srp-global-settings__header-label").text = label;
            settingsRoot.Q<Image>("srp-global-settings__help-button-image").image = CoreEditorStyles.iconHelp;
            settingsRoot.Q<Button>("srp-global-settings__help-button").clicked += () => Help.BrowseURL(Help.GetHelpURLForObject(renderPipelineSettings));

            var contentContainer = settingsRoot.Q("srp-global-settings__content-container");

            if (m_SupportedOnRenderPipeline is { isSupportedOnCurrentPipeline: false })
            {
                contentContainer.Add(new HelpBox("These settings are currently not available due to the active Render Pipeline.", HelpBoxMessageType.Warning));
            }
            else
            {
                VisualElement renderPipelineGraphicsSettingsWidget = new VisualElement
                {
                    name = nameof(renderPipelineGraphicsSettingsWidget),
                    style =
                    {
                        flexDirection = FlexDirection.Row,
                        marginLeft = 10,
                        marginRight = 10,
                    }
                };
                var picker = new ObjectField()
                {
                    value = renderPipelineSettings,
                    objectType = typeof(TGlobalSettings),
                    style =
                    {
                        flexGrow = 1.0f
                    }
                };
                picker.RegisterValueChangedCallback(evt =>
                {
                    if (evt.newValue == null)
                    {
                        Debug.LogWarning(Styles.settingNullRPSettings);
                        picker.SetValueWithoutNotify(renderPipelineSettings);
                    }
                    else
                    {
                        EditorGraphicsSettings.SetRenderPipelineGlobalSettingsAsset<TRenderPipeline>(
                            evt.newValue as RenderPipelineGlobalSettings);
                        // As the SetRenderPipelineGlobalSettingsAsset call is reloading the ProjectSettings window
                        // We need to close the ObjectSelector as we have lost this picker object.
                        EditorWindow[] windows = Resources.FindObjectsOfTypeAll<EditorWindow>();
                        foreach (EditorWindow window in windows)
                        {
                            if (window.GetType().Name.Contains("ObjectSelector"))
                            {
                                window.Close();
                                break;
                            }
                        }
                    }
                });
                renderPipelineGraphicsSettingsWidget.Add(picker);
                renderPipelineGraphicsSettingsWidget.Add(new Button(
                    () => Create(useProjectSettingsFolder: true, activateAsset: true))
                {
                    text = "New"
                });
                renderPipelineGraphicsSettingsWidget.Add(new Button(
                    () => Clone(renderPipelineSettings, activateAsset: true))
                {
                    text = "Clone"
                });
                contentContainer.Add(renderPipelineGraphicsSettingsWidget);

                m_Editor = Editor.CreateEditor(renderPipelineSettings);
                contentContainer.Add(new InspectorElement(m_Editor)
                {
                    style = { paddingLeft = 0}
                });
            }

            rootElement.Add(settingsRoot);
        }

        /// <summary>
        /// This method is being called when the provider is deactivated
        /// </summary>
        public override void OnDeactivate()
        {
            DestroyEditor();
            base.OnDeactivate();
        }

        /// <summary>
        /// Ensures that the <see cref="RenderPipelineGlobalSettings"/> asset is correct
        /// </summary>
        protected abstract void Ensure();

        /// <summary>
        /// Creates a new <see cref="RenderPipelineGlobalSettings"/> asset
        /// </summary>
        /// <param name="useProjectSettingsFolder">If the asset should be created on the project settings folder</param>
        /// <param name="activateAsset">if the asset should be shown on the inspector</param>
        protected virtual void Create(bool useProjectSettingsFolder, bool activateAsset)
        {
            RenderPipelineGlobalSettingsEndNameEditAction.CreateNew<TRenderPipeline, TGlobalSettings>(useProjectSettingsFolder, activateAsset);
        }

        /// <summary>
        /// Clones the <see cref="RenderPipelineGlobalSettings"/> asset
        /// </summary>
        /// <param name="source">The <see cref="RenderPipelineGlobalSettings"/> to clone.</param>
        /// <param name="activateAsset">if the asset should be shown on the inspector.</param>
        protected virtual void Clone(RenderPipelineGlobalSettings source, bool activateAsset)
        {
            RenderPipelineGlobalSettingsEndNameEditAction.CloneFrom<TRenderPipeline, TGlobalSettings>(source, activateAsset);
        }

        bool DrawImguiContent(out bool assetChanged)
        {
            assetChanged = false;

            if (m_SupportedOnRenderPipeline is { isSupportedOnCurrentPipeline: false })
            {
                EditorGUILayout.HelpBox("These settings are currently not available due to the active Render Pipeline.", MessageType.Warning);
                return false;
            }

            if (renderPipelineSettings == null)
            {
                CoreEditorUtils.DrawFixMeBox(string.Format(Styles.warningGlobalSettingsMissing, ObjectNames.NicifyVariableName(typeof(TGlobalSettings).Name)), () => Ensure());
                return false;
            }

            DrawAssetSelection(out assetChanged);

            if (RenderPipelineManager.currentPipeline != null && !(RenderPipelineManager.currentPipeline is TRenderPipeline))
            {
                EditorGUILayout.HelpBox(string.Format(Styles.warningSRPNotActive, ObjectNames.NicifyVariableName(RenderPipelineManager.currentPipeline.GetType().Name)), MessageType.Warning);
            }

            return true;
        }

        /// <summary>
        /// Method called to render the IMGUI of the settings provider
        /// </summary>
        /// <param name="searchContext">The search content</param>
        public override void OnGUI(string searchContext)
        {
            using (new SettingsProviderGUIScope())
            {
                if (DrawImguiContent(out var assetChanged))
                {
                    if (assetChanged || m_Editor != null && (m_Editor.target == null || m_Editor.target != renderPipelineSettings))
                        DestroyEditor();

                    if (m_Editor == null)
                        m_Editor = Editor.CreateEditor(renderPipelineSettings);

                    if (m_Editor != null)
                        m_Editor.OnInspectorGUI();
                }
            }

            base.OnGUI(searchContext);
        }

        void DrawAssetSelection(out bool assetChanged)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                var selectedGlobalSettings = renderPipelineSettings;

                selectedGlobalSettings = (TGlobalSettings)EditorGUILayout.ObjectField(selectedGlobalSettings, typeof(TGlobalSettings), false);

                if (selectedGlobalSettings == null && renderPipelineSettings != null)
                {
                    Debug.LogWarning(Styles.settingNullRPSettings);
                    selectedGlobalSettings = renderPipelineSettings;
                }

                if (GUILayout.Button(Styles.newAssetButtonLabel, Styles.buttonOptions))
                {
                    Create(useProjectSettingsFolder: true, activateAsset: true);
                }

                bool guiEnabled = GUI.enabled;
                GUI.enabled = guiEnabled && (renderPipelineSettings != null);
                if (GUILayout.Button(Styles.cloneAssetButtonLabel, Styles.buttonOptions))
                {
                    Clone(renderPipelineSettings, activateAsset: true);
                }
                GUI.enabled = guiEnabled;

                assetChanged = selectedGlobalSettings != renderPipelineSettings;
                if (assetChanged)
                {
                    DestroyEditor();
                    EditorGraphicsSettings.SetRenderPipelineGlobalSettingsAsset<TRenderPipeline>(selectedGlobalSettings);
                }
            }
        }
    }
}