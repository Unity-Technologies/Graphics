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
            return CreateVisualElement(
                "Packages/com.unity.render-pipelines.universal/Runtime/Debug/RenderingDebugger/RenderingPanel.uxml");
        }

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
