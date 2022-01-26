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
            var panel = CreateVisualElement(
                "Packages/com.unity.render-pipelines.universal/Runtime/Debug/RenderingDebugger/RenderingPanel.uxml");

            RegisterCallback<Enum>(panel, nameof(fullScreenDebugMode), OnFullScreenDebugModeChanged);

            return panel;
        }

        /// <summary>
        /// Current debug fullscreen overlay mode.
        /// </summary>
        public DebugFullScreenMode fullScreenDebugMode = DebugFullScreenMode.None;

        private void OnFullScreenDebugModeChanged(ChangeEvent<Enum> evt)
        {
            var mapSize = panelElement.Q(nameof(fullScreenDebugModeOutputSizeScreenPercent));
            SetElementHidden(mapSize, (DebugFullScreenMode)evt.newValue == DebugFullScreenMode.None);
        }

        /// <summary>
        /// Size of the debug fullscreen overlay, as percentage of the screen size.
        /// </summary>
        public int fullScreenDebugModeOutputSizeScreenPercent = 50;

        /// <summary>
        /// Whether MSAA is enabled.
        /// </summary>
        public bool enableMsaa = true;

        /// <summary>
        /// Whether HDR is enabled.
        /// </summary>
        public bool enableHDR = true;

    }
}
