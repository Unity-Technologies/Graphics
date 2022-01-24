using System;

namespace UnityEngine.Rendering.Universal
{
    [Serializable]
    [RenderingDebuggerPanel("Material", "Packages/com.unity.render-pipelines.universal/Runtime/Debug/RenderingDebugger/MaterialPanel.uxml")]
    class MaterialPanel : ScriptableObject
    {
        /// <summary>
        /// Current debug material mode.
        /// </summary>
        public DebugMaterialMode materialDebugMode;
    }
}
