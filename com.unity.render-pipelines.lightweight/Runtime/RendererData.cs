using System.Collections.Generic;

namespace UnityEngine.Experimental.Rendering.LWRP
{
    public abstract class RendererData : ScriptableObject
    {
        public abstract RendererSetup Create();

        [SerializeField] List<RenderPassFeature> m_RenderPassFeatures = new List<RenderPassFeature>(10);
        [SerializeField] List<DrawGroup> m_DrawGroups = new List<DrawGroup>(3);
        
        public List<RenderPassFeature> renderPassFeatures
        {
            get => m_RenderPassFeatures;
            set => m_RenderPassFeatures = value;
        }

        public List<DrawGroup> drawGroups
        {
            get => m_DrawGroups;
            set => m_DrawGroups = value;
        }
    }
}

