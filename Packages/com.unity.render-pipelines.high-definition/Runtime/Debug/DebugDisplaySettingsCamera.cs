using System;
using System.Collections.Generic;
using UnityEngine.Rendering.HighDefinition;

namespace UnityEngine.Rendering
{
    [Serializable]
    class DebugDisplaySettingsCamera : IDebugDisplaySettingsData, ISerializedDebugDisplaySettings
    {
        [Serializable]
        public class RegisteredCameraEntry
        {
            [SerializeField] public Camera camera;

            // We use this container to serialize the the "debug column" of FrameSettingsHistory, which is the only editable thing.
            [SerializeField] public FrameSettings debugSettings;
        }

        [Serializable]
        public class FrameSettingsDebugData
        {
            [SerializeField]
            Camera m_SelectedCamera;

            public Camera selectedCamera
            {
                get
                {
#if UNITY_EDITOR
                    if (m_SelectedCamera == null && UnityEditor.SceneView.lastActiveSceneView != null)
                    {
                        var sceneCamera = UnityEditor.SceneView.lastActiveSceneView.camera;
                        if (sceneCamera != null)
                            m_SelectedCamera = sceneCamera;
                    }
#endif
                    return m_SelectedCamera;
                }
                set
                {
                    if (value != null && value != m_SelectedCamera)
                    {
                        m_SelectedCamera = value;
                    }
                }
            }

            [SerializeField]
            public List<RegisteredCameraEntry> registeredCameras = new ();
        }

        [SerializeField]
        FrameSettingsDebugData m_FrameSettingsData = new();

        public bool RegisterCameraIfNeeded(Camera camera)
        {
            foreach (var entry in m_FrameSettingsData.registeredCameras)
            {
                if (entry.camera == camera)
                {
                    // Restore debug settings to the camera
#if UNITY_EDITOR
                    if (camera.cameraType == CameraType.SceneView)
                    {
                        FrameSettingsHistory.RegisterDebug(null, true);
                        var container = FrameSettingsHistory.sceneViewFrameSettingsContainer;
                        var history = container.frameSettingsHistory;
                        history.debug = entry.debugSettings;
                        container.frameSettingsHistory = history;
                        return true;
                    }
#endif

                    if (camera.TryGetComponent<HDAdditionalCameraData>(out var additionalCameraData))
                    {
                        var container = additionalCameraData as IFrameSettingsHistoryContainer;
                        var history = container.frameSettingsHistory;
                        history.debug = entry.debugSettings;
                        container.frameSettingsHistory = history;
                    }

                    return true;
                }
            }

            // All scene view will share the same debug FrameSettings, even if HDAdditionalCameraData is present
            // Check for SceneView FIRST before checking HDAdditionalCameraData to ensure correct container registration
#if UNITY_EDITOR
            if (camera.cameraType == CameraType.SceneView)
            {
                var debugData = FrameSettingsHistory.RegisterDebug(null, true);
                m_FrameSettingsData.registeredCameras.Add(new RegisteredCameraEntry
                {
                    camera = camera,
                    debugSettings = FrameSettingsHistory.sceneViewFrameSettingsContainer.frameSettingsHistory.debug
                });
                return true;
            }
#endif

            if (camera.TryGetComponent<HDAdditionalCameraData>(out var hdAdditionalCameraData))
            {
                var debugData = FrameSettingsHistory.RegisterDebug(hdAdditionalCameraData);
                m_FrameSettingsData.registeredCameras.Add(new RegisteredCameraEntry { camera = camera });
                DebugManager.instance.RegisterData(debugData);
                return true;
            }

            Debug.LogWarning($"[Rendering Debugger] Unable to register camera {camera.name} due to missing {nameof(HDAdditionalCameraData)} component");
            return false;
        }

        void IDebugDisplaySettingsData.Reset()
        {
            FrameSettingsHistory.Clear();
            m_FrameSettingsData.registeredCameras.Clear();
        }

        const string k_PanelTitle = "Camera";

        static class Strings
        {
            public static readonly string camera = "Frame Settings";
        }

        internal static class WidgetFactory
        {
            public static DebugUI.CameraSelector CreateCameraSelector(SettingsPanel panel,
                Action<DebugUI.Field<Object>, Object> refresh)
            {
                return new DebugUI.CameraSelector()
                {
                    displayName = Strings.camera,
                    getter = () => panel.data.m_FrameSettingsData.selectedCamera,
                    setter = value =>
                    {
                        if (value is Camera cam && value != panel.data.m_FrameSettingsData.selectedCamera)
                            panel.data.m_FrameSettingsData.selectedCamera = cam;
                    },
                    onValueChanged = refresh
                };
            }
        }

        [DisplayInfo(name = k_PanelTitle, order = 40)]
        [HDRPHelpURL("rendering-debugger-window-reference", "CameraPanel")]
        internal class SettingsPanel : DebugDisplaySettingsPanel<DebugDisplaySettingsCamera>
        {
            public override void Dispose()
            {
                // Store debug settings for serialization
                foreach (var entry in data.m_FrameSettingsData.registeredCameras)
                {
                    if (entry.camera != null)
                    {
#if UNITY_EDITOR
                        if (entry.camera.cameraType == CameraType.SceneView)
                        {
                            var container = FrameSettingsHistory.sceneViewFrameSettingsContainer;
                            entry.debugSettings = container.frameSettingsHistory.debug;
                            continue;
                        }
#endif

                        if (entry.camera.TryGetComponent<HDAdditionalCameraData>(out var hdAdditionalCameraData))
                        {
                            var container = hdAdditionalCameraData as IFrameSettingsHistoryContainer;
                            entry.debugSettings = container.frameSettingsHistory.debug;
                        }
                    }
                }

                // Unregister all the cameras from the history
                FrameSettingsHistory.Clear();

                var panel = DebugManager.instance.GetPanel(PanelName);
                if (panel != null)
                {
                    panel.children.Clear();
                    m_FrameSettingsWidgets.Clear();
                }

                base.Dispose();
            }

            static readonly string k_DebugOverridesMessage = "Debug overrides cannot force-enable unsupported features - they can only override values within the constraints of your current pipeline configuration.";
            static readonly string k_CameraNeedsToRenderMessage = "Values might not be fully updated if the camera you are inspecting is not rendered.";

            DebugUI.CameraSelector m_CameraSelector;
            DebugUI.MessageBox m_HelpBox;
            DebugUI.MessageBox m_HelpBoxCameraNeedsToRender;
            Dictionary<Camera, List<DebugUI.Widget>> m_FrameSettingsWidgets = new();
            public SettingsPanel(DebugDisplaySettingsCamera data)
                : base(data)
            {
                m_CameraSelector = WidgetFactory.CreateCameraSelector(this, (_, __) => Refresh());
                AddWidget(m_CameraSelector);

                m_HelpBox = new DebugUI.MessageBox
                {
                    displayName = k_DebugOverridesMessage,
                    style = DebugUI.MessageBox.Style.Warning,
                    isHiddenCallback = () => data.m_FrameSettingsData.selectedCamera == null
                };
                AddWidget(m_HelpBox);

                m_HelpBoxCameraNeedsToRender = new DebugUI.MessageBox
                {
                    displayName = k_CameraNeedsToRenderMessage,
                    style = DebugUI.MessageBox.Style.Warning,
                    isHiddenCallback = () => data.m_FrameSettingsData.selectedCamera == null
                };
                AddWidget(m_HelpBoxCameraNeedsToRender);

                if (GetOrCreateFrameSettingsWidgets(out var frameSettingsWidgets))
                {
                    foreach (var c in frameSettingsWidgets)
                        AddWidget(c);
                }
            }

            bool GetOrCreateFrameSettingsWidgets(out List<DebugUI.Widget> widgets)
            {
                widgets = new List<DebugUI.Widget>();

                var camera = data.m_FrameSettingsData.selectedCamera;
                if (camera == null)
                    return false;

                bool registered = data.RegisterCameraIfNeeded(camera);
                if (!registered)
                    return false;

                if (!m_FrameSettingsWidgets.TryGetValue(camera, out widgets))
                {
                    widgets ??= new List<DebugUI.Widget>();
                    var container = camera.GetComponent<HDAdditionalCameraData>() as IFrameSettingsHistoryContainer;

#if UNITY_EDITOR
                    if (camera.cameraType == CameraType.SceneView)
                        container = FrameSettingsHistory.sceneViewFrameSettingsContainer;
#endif

                    var panelContent = FrameSettingsHistory.GenerateFrameSettingsPanelContent(container);
                    foreach (var foldout in panelContent)
                    {
                        widgets.Add(foldout);
                    }

                    m_FrameSettingsWidgets[camera] = widgets;
                }

                return widgets.Count != 0;
            }

            void Refresh()
            {
                var panel = DebugManager.instance.GetPanel(PanelName);
                if (panel == null)
                    return;

                panel.children.Clear();
                AddWidget(m_CameraSelector);
                panel.children.Add(m_CameraSelector);

                AddWidget(m_HelpBox);
                panel.children.Add(m_HelpBox);

                AddWidget(m_HelpBoxCameraNeedsToRender);
                panel.children.Add(m_HelpBoxCameraNeedsToRender);

                bool needsRefresh = GetOrCreateFrameSettingsWidgets(out var frameSettingsWidgets);
                if (needsRefresh)
                {
                    foreach (var c in frameSettingsWidgets)
                    {
                        AddWidget(c);
                        panel.children.Add(c);
                    }
                }
            }
        }

        #region IDebugDisplaySettingsData
        /// <summary>
        /// Checks whether ANY of the debug settings are currently active.
        /// </summary>
        public bool AreAnySettingsActive => false; // This Panel doesn't need to modify the renderer data, therefore this property returns false

        /// <inheritdoc/>
        public IDebugDisplaySettingsPanelDisposable CreatePanel()
        {
            return new SettingsPanel(this);
        }

        #endregion
    }
}
