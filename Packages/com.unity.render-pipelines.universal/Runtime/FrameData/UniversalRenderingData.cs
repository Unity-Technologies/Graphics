namespace UnityEngine.Rendering.Universal
{
    /// <summary>
    /// Contains the data for general renderer settings.
    /// </summary>
    public class UniversalRenderingData : ContextItem
    {
        // Non-rendergraph path only. Do NOT use with rendergraph! (RG execution timeline breaks.)
        // NOTE: internal for a ref return in legacy RenderingData.commandBuffer.
        internal CommandBuffer m_CommandBuffer;

        // Non-rendergraph path only. Do NOT use with rendergraph! (RG execution timeline breaks.)
        internal CommandBuffer commandBuffer
        {
            get
            {
                if (m_CommandBuffer == null)
                    Debug.LogError("UniversalRenderingData.commandBuffer is null. RenderGraph does not support this property. Please use the command buffer provided by the RenderGraphContext.");

                return m_CommandBuffer;
            }
        }

        /// <summary>
        /// Returns culling results that exposes handles to visible objects, lights and probes.
        /// You can use this to draw objects with <c>ScriptableRenderContext.DrawRenderers</c>
        /// <see cref="CullingResults"/>
        /// <seealso cref="ScriptableRenderContext"/>
        /// </summary>
        public CullingResults cullResults;

        /// <summary>
        /// True if the pipeline supports dynamic batching.
        /// This settings doesn't apply when drawing shadow casters. Dynamic batching is always disabled when drawing shadow casters.
        /// </summary>
        public bool supportsDynamicBatching;

        /// <summary>
        /// Holds per-object data that are requested when drawing
        /// <see cref="PerObjectData"/>
        /// </summary>
        public PerObjectData perObjectData;

        /// <summary>
        /// The Rendering mode used by the renderer in the current frame.
        /// Note that this may sometimes be different from what is set in the Renderer asset,
        /// for example when the hardware not capable of deferred rendering or when doing wireframe rendering.
        /// </summary>
        public RenderingMode renderingMode { get; internal set; }

        /// <summary>
        /// The layer mask set on the renderer to filter prepass objects.
        /// </summary>
        public LayerMask prepassLayerMask { get; internal set; }

        /// <summary>
        /// The layer mask set on the renderer to filter opaque objects.
        /// </summary>
        public LayerMask opaqueLayerMask { get; internal set; }

        /// <summary>
        /// The layer mask set on the renderer to filter transparent objects.
        /// </summary>
        public LayerMask transparentLayerMask { get; internal set; }

        /// <summary>
        /// True if the Stencil Lod Cross fade is enabled.
        /// </summary>
        public bool stencilLodCrossFadeEnabled { get; internal set; }

        /// <inheritdoc/>
        public override void Reset()
        {
            m_CommandBuffer = default;
            cullResults = default;
            supportsDynamicBatching = default;
            perObjectData = default;
            renderingMode = default;
            stencilLodCrossFadeEnabled = default;
            prepassLayerMask = -1;
            opaqueLayerMask = -1;
            transparentLayerMask = -1;
        }
    }
}
