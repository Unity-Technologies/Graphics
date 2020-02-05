using System;

namespace UnityEngine.Rendering.HighDefinition
{
    /// <summary>
    /// Context used when executing custom passes
    /// </summary>
    public struct CustomPassContext
    {
        /// <summary>
        /// Scriptable Render Context, used for any SRP related operations.
        /// </summary>
        public ScriptableRenderContext  renderContext;

        /// <summary>
        /// Command Buffer, used to enqueue graphic commands to the GPU.
        /// </summary>
        public CommandBuffer            cmd;

        /// <summary>
        /// HDCamera, HDRP data related to the rendering camera. Use the camera property to access the Camera class.
        /// </summary>
        public HDCamera                 hdCamera;

        /// <summary>
        /// Result of the culling either of the camera or the custom pass if AggregateCullingParameters is used.
        /// </summary>
        public CullingResults           cullingResult;

        /// <summary>
        /// Camera color buffer.
        /// </summary>
        public RTHandle                 cameraColorBuffer;

        /// <summary>
        /// Camera depth buffer.
        /// </summary>
        public RTHandle                 cameraDepthBuffer;

        /// <summary>
        /// Camera normal buffer.
        /// </summary>
        public RTHandle                 cameraNormalBuffer;

        /// <summary>
        /// Lazy handle to the custom color buffer, not allocated if not used.
        /// </summary>
        public Lazy<RTHandle>           customColorBuffer;

        /// <summary>
        /// Lazy handle to the custom depth buffer, not allocated if not used.
        /// </summary>
        public Lazy<RTHandle>           customDepthBuffer;

        /// <summary>
        /// Material Property Block, unique for each custom pass instance.
        /// </summary>
        public MaterialPropertyBlock    propertyBlock;
    }
}