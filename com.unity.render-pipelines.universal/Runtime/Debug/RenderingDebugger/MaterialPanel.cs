using System;

namespace UnityEngine.Rendering.Universal
{
    [Serializable]
    class MaterialPanel : RenderingDebuggerPanel
    {
        public override string panelName => "Material";

        public override string uiDocument =>
            "Packages/com.unity.render-pipelines.universal/Runtime/Debug/RenderingDebugger/MaterialPanel.uxml";

        /// <summary>
        /// Current debug material mode.
        /// </summary>
        public DebugMaterialMode materialDebugMode;
    }
}
