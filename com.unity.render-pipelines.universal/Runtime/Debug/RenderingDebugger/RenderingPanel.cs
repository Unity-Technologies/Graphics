using System;
using UnityEditor;
using UnityEngine.UIElements;

namespace UnityEngine.Rendering.Universal
{
    [Serializable]
    class RenderingPanel : RenderingDebuggerPanel
    {
        public override string panelName => "Rendering";

        public override VisualElement CreatePanel()
        {
            var panelVisualTreeAsset = Resources.Load<VisualTreeAsset>("RenderingPanel");
            var panel = CreateVisualElement(panelVisualTreeAsset);

            RegisterCallback<Enum>(panel, nameof(fullScreenDebugMode), fullScreenDebugMode, OnFullScreenDebugModeChanged);

            return panel;
        }

        public override bool AreAnySettingsActive =>
            (postProcessingDebugMode != DebugPostProcessingMode.Auto) ||
            (fullScreenDebugMode != DebugFullScreenMode.None) ||
            (sceneOverrideMode != DebugSceneOverrideMode.None) ||
            //(validationMode != DebugValidationMode.None) || // TODO
            !enableMsaa ||
            !enableHDR;

        public override bool IsPostProcessingAllowed =>
            (postProcessingDebugMode != DebugPostProcessingMode.Disabled) &&
            (sceneOverrideMode == DebugSceneOverrideMode.None);

        public override bool IsLightingActive => (sceneOverrideMode == DebugSceneOverrideMode.None);

        public override bool TryGetScreenClearColor(ref Color color)
        {
            switch (sceneOverrideMode)
            {
                case DebugSceneOverrideMode.None:
                case DebugSceneOverrideMode.ShadedWireframe:
                    return false;

                case DebugSceneOverrideMode.Overdraw:
                    color = Color.black;
                    return true;

                case DebugSceneOverrideMode.Wireframe:
                case DebugSceneOverrideMode.SolidWireframe:
                    color = new Color(0.1f, 0.1f, 0.1f, 1.0f);
                    return true;

                default:
                    throw new ArgumentOutOfRangeException(nameof(color));
            }
        }

        /// <summary>
        /// Current debug fullscreen overlay mode.
        /// </summary>
        public DebugFullScreenMode fullScreenDebugMode = DebugFullScreenMode.None;

        private void OnFullScreenDebugModeChanged(ChangeEvent<Enum> evt)
        {
            SetElementHidden(nameof(fullScreenDebugModeOutputSizeScreenPercent), (DebugFullScreenMode)evt.newValue == DebugFullScreenMode.None);
        }

        /// <summary>
        /// Size of the debug fullscreen overlay, as percentage of the screen size.
        /// </summary>
        public int fullScreenDebugModeOutputSizeScreenPercent = 50;

        /// <summary>
        /// Whether HDR is enabled.
        /// </summary>
        public bool enableHDR = true;

        /// <summary>
        /// Whether MSAA is enabled.
        /// </summary>
        public bool enableMsaa = true;

        /// <summary>
        /// Current debug post processing mode.
        /// </summary>
        public DebugPostProcessingMode postProcessingDebugMode = DebugPostProcessingMode.Auto;

        // Under the hood, the implementation uses a single enum (DebugSceneOverrideMode). For UI & public API,
        // we have split this enum into WireframeMode and a separate Overdraw boolean.

        // NOTE: Should be private, but must be public for data binding.
        // TODO binding to this doesn't work because the property setter updating SceneOverrideMode never gets called. Need to rethink.
        public DebugWireframeMode m_WireframeMode = DebugWireframeMode.None;

        /// <summary>
        /// Current debug wireframe mode.
        /// </summary>
        public DebugWireframeMode wireframeMode
        {
            get => m_WireframeMode;
            set
            {
                m_WireframeMode = value;
                UpdateDebugSceneOverrideMode();
            }
        }

        void UpdateDebugSceneOverrideMode()
        {
            switch (wireframeMode)
            {
                case DebugWireframeMode.Wireframe:
                    sceneOverrideMode = DebugSceneOverrideMode.Wireframe;
                    break;
                case DebugWireframeMode.SolidWireframe:
                    sceneOverrideMode = DebugSceneOverrideMode.SolidWireframe;
                    break;
                case DebugWireframeMode.ShadedWireframe:
                    sceneOverrideMode = DebugSceneOverrideMode.ShadedWireframe;
                    break;
                default:
                    sceneOverrideMode = overdraw ? DebugSceneOverrideMode.Overdraw : DebugSceneOverrideMode.None;
                    break;
            }
        }

        internal DebugSceneOverrideMode sceneOverrideMode { get; set; } = DebugSceneOverrideMode.None;

        // NOTE: Should be private, but must be public for data binding.
        // TODO binding to this doesn't work because the property setter updating SceneOverrideMode never gets called. Need to rethink.
        public bool m_Overdraw = false;

        /// <summary>
        /// Whether debug overdraw mode is active.
        /// </summary>
        public bool overdraw
        {
            get => m_Overdraw;
            set
            {
                m_Overdraw = value;
                UpdateDebugSceneOverrideMode();
            }
        }
    }
}
