using System.Collections.Generic;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering.RenderGraphModule;

using RendererList = UnityEngine.Rendering.RendererList;
using RendererListDesc = UnityEngine.Rendering.RendererUtils.RendererListDesc;

namespace UnityEngine.Rendering.HighDefinition
{
    /// <summary>
    /// Enum that defines the sets of scale which Compute Thickness.
    /// </summary>
    public enum ComputeThicknessResolution
    {
        /// <summary>
        /// The Compute Thickness will be rendered on Full resolution.
        /// </summary>
        Full,
        /// <summary>
        /// The Compute Thickness will be rendered on Half resolution.
        /// </summary>
        Half,
        /// <summary>
        /// The Compute Thickness will be rendered on Quarter resolution.
        /// </summary>
        Quarter
    }

    /// <summary>
    /// Class handling the generation of fullscreen thickness
    /// </summary>
    public sealed class HDComputeThickness
    {
        private static HDComputeThickness m_Instance = null;

        private TextureHandle m_ThicknessArrayRT;
        private GraphicsBuffer m_ReindexMapCB;
        private uint m_UsedLayersCountCurrentFrame;

        /// <summary>
        /// Max RT of Thickness we can computed in a single frame
        /// </summary>
        // Should be paired with: COMPUTE_THICKNESS_MAX_LAYER_COUNT in
        // 'Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl'
        public static uint computeThicknessMaxLayer = 32; // bitscount(LayerMask)

        /// <summary>
        /// Current unique instance
        /// </summary>
        public static HDComputeThickness Instance
        {
            get
            {
                if (m_Instance == null)
                {
                    m_Instance = new HDComputeThickness();
                }
                return m_Instance;
            }
        }

        private HDComputeThickness()
        {
        }

        /// <summary>
        /// TextureArray of thicknesses computed.
        /// </summary>
        /// <param name="rt">TextureArray of thicknesses computed.</param>
        public void SetTextureArray(TextureHandle rt)
        {
            m_ThicknessArrayRT = rt;
        }

        /// <summary>
        /// Return a TextureArrayHandle of thicknesses computed, the slices count are the layer needed.
        /// </summary>
        /// <returns>TextureArray of thicknesses computed.</returns>
        public TextureHandle GetThicknessTextureArray()
        {
            return m_ThicknessArrayRT;
        }

        /// <summary>
        /// Set a ComputeBuffer To reindex from LayerIndex to SliceIndex of the TextureArray of Thicknesses.
        /// </summary>
        /// <param name="cb">The ComputeBuffer (StructuredArray&lt;uint&gt;[computeThicknessMaxLayer]</param>
        public void SetReindexMap(GraphicsBuffer cb)
        {
            m_ReindexMapCB = cb;
        }

        /// <summary>
        /// Get a GraphicsBuffer To reindex from LayerIndex to SliceIndex of the TextureArray of Thicknesses.
        /// </summary>
        /// <returns>The GraphicsBuffer (StructuredArray&lt;uint&gt;[computeThicknessMaxLayer]</returns>
        public GraphicsBuffer GetReindexMap()
        {
            return m_ReindexMapCB;
        }
    }
}
