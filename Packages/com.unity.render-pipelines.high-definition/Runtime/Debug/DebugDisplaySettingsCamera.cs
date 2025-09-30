using System;
using System.Collections.Generic;
using UnityEngine.Rendering.HighDefinition;
using static UnityEngine.Rendering.DebugUI;

namespace UnityEngine.Rendering
{
    class DebugDisplaySettingsCamera : IDebugDisplaySettingsData
    {
        [Serializable]
        public class FrameSettingsDebugData
        {
            public Camera selectedCamera { get; set; }

            public Dictionary<Camera, (HDAdditionalCameraData, IDebugData)> registeredCameras = new ();
        }

        public FrameSettingsDebugData frameSettingsData { get; }

        public bool IsCameraRegistered(Camera camera) => frameSettingsData.registeredCameras.ContainsKey(camera);

        public bool RegisterCamera(Camera camera)
        {
            if (!frameSettingsData.registeredCameras.TryGetValue(camera, out var data))
            {
                if (camera.TryGetComponent<HDAdditionalCameraData>(out var hdAdditionalCameraData))
                {
                    var debugData = FrameSettingsHistory.RegisterDebug(hdAdditionalCameraData);
                    frameSettingsData.registeredCameras.Add(camera, (hdAdditionalCameraData, debugData));
                    DebugManager.instance.RegisterData(debugData);
                }
                else
                {
                    // All scene view will share the same debug FrameSettings as the HDAdditionalData might not be present
                    if (camera.cameraType == CameraType.SceneView)
                    {
                        var debugData = FrameSettingsHistory.RegisterDebug(null, true);
                        frameSettingsData.registeredCameras.Add(camera, (null, debugData));
                    }
                    else
                    {
                        Debug.LogWarning($"[Rendering Debugger] Unable to register camera {camera.name} due to missing {nameof(HDAdditionalCameraData)} component,");
                        return false;
                    }
                }
            }

            return true;
        }

        void IDebugDisplaySettingsData.Reset()
        {
            FrameSettingsHistory.Clear();
            frameSettingsData.registeredCameras.Clear();
        }

        public DebugDisplaySettingsCamera()
        {
            this.frameSettingsData = new ();
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
                    getter = () => panel.data.frameSettingsData.selectedCamera,
                    setter = value =>
                    {
                        if (value != panel.data.frameSettingsData.selectedCamera)
                            panel.data.frameSettingsData.selectedCamera = value as Camera;
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
                // Unregister all the cameras from the history
                foreach(var registeredCamera in data.frameSettingsData.registeredCameras)
                {
                    FrameSettingsHistory.UnRegisterDebug(registeredCamera.Value.Item1); 
                }

                var panel = DebugManager.instance.GetPanel(PanelName);
                if (panel != null)
                {
                    panel.children.Clear();
                    m_FrameSettingsWidgets.Clear();
                }

                base.Dispose();
            }

            DebugUI.CameraSelector m_CameraSelector;
            Dictionary<Camera, List<DebugUI.Widget>> m_FrameSettingsWidgets = new ();
            public SettingsPanel(DebugDisplaySettingsCamera data)
                : base(data)
            {
                m_CameraSelector = WidgetFactory.CreateCameraSelector(this, (_, __) => Refresh());

                // Select first camera if none is selected
                var availableCameras = m_CameraSelector.getObjects() as List<Camera>;
                if (data.frameSettingsData.selectedCamera == null && availableCameras is { Count: > 0 })
                    data.frameSettingsData.selectedCamera = availableCameras[0];
                
                AddWidget(m_CameraSelector);

                if (GetOrCreateFrameSettingsWidgets(out var frameSettingsWidgets))
                {
                    foreach (var c in frameSettingsWidgets)
                        AddWidget(c);
                }
            }

            bool GetOrCreateFrameSettingsWidgets(out List<DebugUI.Widget> widgets)
            {
                widgets = new List<DebugUI.Widget>();

                if (data.frameSettingsData.selectedCamera == null)
                    return false;

                if (!data.IsCameraRegistered(data.frameSettingsData.selectedCamera))
                {
                    if (!data.RegisterCamera(data.frameSettingsData.selectedCamera))
                        return false;
                }

                if (!m_FrameSettingsWidgets.TryGetValue(data.frameSettingsData.selectedCamera, out widgets))
                {
                    widgets ??= new List<DebugUI.Widget>();
                    var cameraInfo = data.frameSettingsData.registeredCameras[data.frameSettingsData.selectedCamera];
                    var panelContent = FrameSettingsHistory.GenerateFrameSettingsPanelContent(cameraInfo.Item1);
                    foreach (var foldout in panelContent)
                    {
                        widgets.Add(foldout);
                    }

                    m_FrameSettingsWidgets[data.frameSettingsData.selectedCamera] = widgets;
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

                bool needsRefresh = GetOrCreateFrameSettingsWidgets(out var frameSettingsWidgets);
                if (needsRefresh)
                {
                    foreach (var c in frameSettingsWidgets)
                    {
                        AddWidget(c);
                        panel.children.Add(c);
                    }

                    DebugManager.instance.ReDrawOnScreenDebug();
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
