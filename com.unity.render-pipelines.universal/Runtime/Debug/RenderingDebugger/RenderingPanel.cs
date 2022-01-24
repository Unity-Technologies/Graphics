using System;

namespace UnityEngine.Rendering.Universal
{
    [Serializable]
    [RenderingDebuggerPanel("Rendering", "Packages/com.unity.render-pipelines.universal/Runtime/Debug/RenderingDebugger/RenderingPanel.uxml")]
    class RenderingPanel : ScriptableObject
    {
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
