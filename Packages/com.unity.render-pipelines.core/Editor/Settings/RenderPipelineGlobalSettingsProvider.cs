using System.Reflection;
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
    public abstract class RenderPipelineGlobalSettingsProvider<TRenderPipeline, TGlobalSettings> : SettingsProvider
        where TRenderPipeline : RenderPipeline
        where TGlobalSettings : RenderPipelineGlobalSettings
    {
        static class Styles
        {
            public static readonly string warningGlobalSettingsMissing = "Select a valid {0} asset.";
            public static readonly string warningSRPNotActive = "Current Render Pipeline is {0}. Check the settings: Graphics > Scriptable Render Pipeline Settings, Quality > Render Pipeline Asset.";
            public static readonly string settingNullRPSettings = "Are you sure you want to unregister the Render Pipeline Settings? There might be issues with rendering.";

            public static readonly GUIContent newAssetButtonLabel = EditorGUIUtility.TrTextContent("New", "Create a Global Settings asset in the Assets folder.");
            public static readonly GUIContent cloneAssetButtonLabel = EditorGUIUtility.TrTextContent("Clone", "Clone a Global Settings asset in the Assets folder.");
            public static readonly GUILayoutOption[] buttonOptions = new GUILayoutOption[] { GUILayout.Width(45), GUILayout.Height(18) };
        }

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
            if (m_Editor == null)
                return;

            UnityEngine.Object.DestroyImmediate(m_Editor);
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
        protected abstract void Create(bool useProjectSettingsFolder, bool activateAsset);

        /// <summary>
        /// Clones the <see cref="RenderPipelineGlobalSettings"/> asset
        /// </summary>
        /// <param name="src">The <see cref="RenderPipelineGlobalSettings"/> to clone.</param>
        /// <param name="activateAsset">if the asset should be shown on the inspector.</param>
        protected abstract void Clone(RenderPipelineGlobalSettings src, bool activateAsset);

        /// <summary>
        /// Method called to render the IMGUI of the settings provider
        /// </summary>
        /// <param name="searchContext">The search content</param>
        public override void OnGUI(string searchContext)
        {
            if (m_SupportedOnRenderPipeline is { isSupportedOnCurrentPipeline: false })
            {
                EditorGUILayout.HelpBox("These settings are currently not available due to the active Render Pipeline.", MessageType.Warning);
                return;
            }

            using (new SettingsProviderGUIScope())
            {
                if (renderPipelineSettings == null)
                {
                    CoreEditorUtils.DrawFixMeBox(string.Format(Styles.warningGlobalSettingsMissing, ObjectNames.NicifyVariableName(typeof(TGlobalSettings).Name)), () => Ensure());
                    DestroyEditor();
                }
                else
                {
                    DrawAssetSelection();

                    if (RenderPipelineManager.currentPipeline != null && !(RenderPipelineManager.currentPipeline is TRenderPipeline))
                    {
                        EditorGUILayout.HelpBox(string.Format(Styles.warningSRPNotActive, ObjectNames.NicifyVariableName(RenderPipelineManager.currentPipeline.GetType().Name)), MessageType.Warning);
                    }

                    if (m_Editor != null && (m_Editor.target == null || m_Editor.target != renderPipelineSettings))
                        DestroyEditor();

                    if (m_Editor == null)
                        m_Editor = Editor.CreateEditor(renderPipelineSettings);

                    m_Editor?.OnInspectorGUI();
                }
            }

            base.OnGUI(searchContext);
        }

        void DrawAssetSelection()
        {
            var oldRenderPipelineSettings = renderPipelineSettings;

            using (new EditorGUILayout.HorizontalScope())
            {
                var newSettings = (TGlobalSettings)EditorGUILayout.ObjectField(renderPipelineSettings, typeof(TGlobalSettings), false);

                if (renderPipelineSettings != newSettings)
                {
                    if (newSettings != null)
                        GraphicsSettings.RegisterRenderPipelineSettings<TRenderPipeline>(newSettings);
                    else
                    {
                        if (EditorUtility.DisplayDialog($"Invalid {ObjectNames.NicifyVariableName(typeof(TGlobalSettings).Name)}", Styles.settingNullRPSettings, "Yes", "No"))
                            GraphicsSettings.UnregisterRenderPipelineSettings<TRenderPipeline>();
                    }

                    if (renderPipelineSettings != null && !renderPipelineSettings.Equals(null))
                        EditorUtility.SetDirty(renderPipelineSettings);
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
            }

            if (oldRenderPipelineSettings != renderPipelineSettings)
            {
                DestroyEditor();
            }
        }
    }
}
