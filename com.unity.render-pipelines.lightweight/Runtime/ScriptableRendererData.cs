using System.Collections.Generic;

namespace UnityEngine.Rendering.LWRP
{
    /// <summary>
    /// Class <c>ScriptableRendererData</c> contains resources for a <c>ScriptableRenderer</c>.
    /// <seealso cref="ScriptableRenderer"/>
    /// </summary>
    public abstract class ScriptableRendererData : ScriptableObject
    {
        /// <summary>
        /// Creates the instance of the ScriptableRenderer.
        /// </summary>
        /// <returns>The instance of ScriptableRenderer</returns>
        public abstract ScriptableRenderer Create();

        [SerializeField] List<ScriptableRendererFeature> m_RendererFeatures = new List<ScriptableRendererFeature>(10);

        /// <summary>
        /// List of additional render pass features for this renderer.
        /// </summary>
        public List<ScriptableRendererFeature> rendererFeatures
        {
            get => m_RendererFeatures;
        }
    }
}

