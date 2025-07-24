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

        // TODO Add Rendering Debugger Resources here
    }
}
