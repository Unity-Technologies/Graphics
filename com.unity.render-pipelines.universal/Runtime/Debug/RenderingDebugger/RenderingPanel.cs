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
            return CreateVisualElement(panelVisualTreeAsset);
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

        private void UpdateDebugSceneOverrideMode()
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

        /// <summary>
        /// Current debug wireframe mode.
        /// </summary>
        public DebugWireframeMode wireframeMode;

        internal DebugSceneOverrideMode sceneOverrideMode { get; set; } = DebugSceneOverrideMode.None;

        /// <summary>
        /// Whether debug overdraw mode is active.
        /// </summary>
        public bool overdraw;

        protected override void RegisterCallbacks(VisualElement element)
        {
            RegisterCallback<Enum>(element, nameof(fullScreenDebugMode), fullScreenDebugMode, OnFullScreenDebugModeChanged);
            RegisterCallback<Enum>(element, nameof(wireframeMode), wireframeMode, _ => UpdateDebugSceneOverrideMode());
            RegisterCallback<bool>(element, nameof(overdraw), overdraw, _ => UpdateDebugSceneOverrideMode());
        }

#if !UNITY_EDITOR
        protected override void BindToInternal(VisualElement targetElement)
        {
            RegisterChange<Enum>(targetElement, nameof(fullScreenDebugMode), fullScreenDebugMode, evt => fullScreenDebugMode = (DebugFullScreenMode)evt.newValue);
            RegisterChange<Enum>(targetElement, nameof(postProcessingDebugMode), postProcessingDebugMode, evt => postProcessingDebugMode = (DebugPostProcessingMode)evt.newValue);
            RegisterChange<Enum>(targetElement, nameof(wireframeMode), wireframeMode, evt => wireframeMode = (DebugWireframeMode)evt.newValue);
            RegisterChange<bool>(targetElement, nameof(enableHDR), enableHDR, evt => enableHDR = evt.newValue);
            RegisterChange<bool>(targetElement, nameof(enableMsaa), enableMsaa, evt => enableMsaa = evt.newValue);
            RegisterChange<int>(targetElement, nameof(fullScreenDebugModeOutputSizeScreenPercent), fullScreenDebugModeOutputSizeScreenPercent, evt => fullScreenDebugModeOutputSizeScreenPercent = evt.newValue);
        }
#endif

    }
}
