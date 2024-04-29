using System;
using System.ComponentModel;

namespace UnityEngine.Rendering.RenderGraphModule.Util
{
    [Serializable]
    [HideInInspector]
    [Category("Resources/Render Graph Helper Function Resources")]
    [SupportedOnRenderPipeline]
    class RenderGraphUtilsResources : IRenderPipelineResources
    {
        public enum Version
        {
            Initial,

            Count,
            Latest = Count - 1
        }
        [SerializeField, HideInInspector] Version m_Version = Version.Latest;
        int IRenderPipelineGraphicsSettings.version => (int)m_Version;

        [SerializeField, ResourcePath("Shaders/CoreCopy.shader")]
        internal Shader m_CoreCopyPS;

        /// <summary>
        /// Core Copy shader.
        /// </summary>
        public Shader coreCopyPS
        {
            get => m_CoreCopyPS;
            set => this.SetValueAndNotify(ref m_CoreCopyPS, value, nameof(m_CoreCopyPS));
        }
    }
}
