using System.Collections.Generic;

namespace UnityEngine.Rendering.LWRP
{
    public abstract class RendererData : ScriptableObject
    {
        public abstract ScriptableRenderer Create();

        [SerializeField] List<RenderPassFeature> m_RenderPassFeatures = new List<RenderPassFeature>(10);
        
        public List<RenderPassFeature> renderPassFeatures
        {
            get => m_RenderPassFeatures;
            set => m_RenderPassFeatures = value;
        }
    }
}

