using System;

namespace UnityEngine.Rendering
{
    [Serializable, HideInInspector]
    [SupportedOnRenderPipeline]
    [Categorization.CategoryInfo(Name = "R : Rendering Debugger Resources", Order = 100)]
    [Categorization.ElementInfo(Order = 0)]
    class RenderingDebuggerRuntimeResources : IRenderPipelineResources
    {
        enum Version
        {
            Initial,

            Count,
            Last = Count - 1
        }
        [SerializeField, HideInInspector]
        private Version m_version = Version.Last;
        int IRenderPipelineGraphicsSettings.version => (int)m_version;

        [SerializeField, ResourcePath("Runtime/Debugging/Runtime UI Resources/DebugUICanvas.prefab")]
        private GameObject  m_DebugUIHandlerCanvasPrefab;

        /// <summary> Panel Settings </summary>
        public GameObject debugUIHandlerCanvasPrefab
        {
            get => m_DebugUIHandlerCanvasPrefab;
            set => this.SetValueAndNotify(ref m_DebugUIHandlerCanvasPrefab, value, nameof(m_DebugUIHandlerCanvasPrefab));
        }

        [SerializeField, ResourcePath("Runtime/Debugging/Runtime UI Resources/DebugUIPersistentCanvas.prefab")]
        private GameObject  m_DebugUIPersistentCanvasPrefab;

        /// <summary> Panel Settings </summary>
        public GameObject debugUIPersistentCanvasPrefab
        {
            get => m_DebugUIPersistentCanvasPrefab;
            set => this.SetValueAndNotify(ref m_DebugUIPersistentCanvasPrefab, value, nameof(m_DebugUIPersistentCanvasPrefab));
        }
    }
}
