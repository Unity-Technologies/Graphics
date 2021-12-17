using System;
using UnityEngine.Experimental.Rendering.RenderGraphModule;

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
        [Obsolete("renderContext is now only available from within the render graph pass.")]
        public readonly ScriptableRenderContext renderContext;

        /// <summary>
        /// Command Buffer, used to enqueue graphic commands to the GPU.
        /// </summary>
        [Obsolete("cmd is now only available from within the render graph pass.")]
        public readonly CommandBuffer cmd;

        /// <summary>
        /// HdCamera, HDRP data related to the rendering camera. Use the camera property to access the Camera class.
        /// </summary>
        public readonly HDCamera hdCamera;

        /// <summary>
        /// Result of the culling either of the camera or the custom pass if AggregateCullingParameters is used.
        /// </summary>
        public CullingResults cullingResults;

        /// <summary>
        /// Camera culling results, not modified by the custom pass culling.
        /// </summary>
        public readonly CullingResults cameraCullingResults;

        /// <summary>
        /// Camera color buffer.
        /// </summary>
        [Obsolete("Use cameraColorBufferHandle instead.")]
        public readonly RTHandle cameraColorBuffer;

        /// <summary>
        /// Camera color buffer.
        /// </summary>
        public readonly TextureHandle cameraColorBufferHandle;

        /// <summary>
        /// Camera depth buffer.
        /// </summary>
        [Obsolete("Use cameraDepthBufferHandle instead.")]
        public readonly RTHandle cameraDepthBuffer;
        /// <summary>
        /// Camera depth buffer.
        /// </summary>
        public readonly TextureHandle cameraDepthBufferHandle;

        /// <summary>
        /// Camera normal buffer.
        /// </summary>
        [Obsolete("Use cameraNormalBufferHandle instead.")]
        public readonly RTHandle cameraNormalBuffer;
        /// <summary>
        /// Camera normal buffer.
        /// </summary>
        public readonly TextureHandle cameraNormalBufferHandle;

        /// <summary>
        /// Camera motion vectors buffer.
        /// </summary>
        [Obsolete("Use cameraMotionVectorsBufferHandle instead.")]
        public readonly RTHandle cameraMotionVectorsBuffer;
        /// <summary>
        /// Camera motion vectors buffer.
        /// </summary>
        public readonly TextureHandle cameraMotionVectorsBufferHandle;

        /// <summary>
        /// Lazy handle to the custom color buffer, not allocated if not used.
        /// </summary>
        [Obsolete("Custom buffers are now allocated from within render graph passes.")]
        public readonly Lazy<RTHandle> customColorBuffer;

        /// <summary>
        /// Lazy handle to the custom depth buffer, not allocated if not used.
        /// </summary>
        [Obsolete("Custom buffers are now allocated from within render graph passes.")]
        public readonly Lazy<RTHandle> customDepthBuffer;

        /// <summary>
        /// Material Property Block, unique for each custom pass instance.
        /// </summary>
        [Obsolete("MaterialPropertyBlocks are available through render graph passes pooling system.")]
        public readonly MaterialPropertyBlock propertyBlock;

        internal CustomPassContext(
            ScriptableRenderContext renderContext, CommandBuffer cmd,
            HDCamera hdCamera, CullingResults cullingResults,
            CullingResults cameraCullingResults,
            RTHandle cameraColorBuffer, RTHandle cameraDepthBuffer,
            RTHandle cameraNormalBuffer, RTHandle cameraMotionVectorsBuffer,
            Lazy<RTHandle> customColorBuffer,
            Lazy<RTHandle> customDepthBuffer, MaterialPropertyBlock propertyBlock)
        {
#pragma warning disable CS0618 // Member is obsolete
            this.renderContext = renderContext;
            this.cmd = cmd;
            this.hdCamera = hdCamera;
            this.cullingResults = cullingResults;
            this.cameraCullingResults = cameraCullingResults;
            this.cameraColorBuffer = cameraColorBuffer;
            this.cameraDepthBuffer = cameraDepthBuffer;
            this.customColorBuffer = customColorBuffer;
            this.cameraNormalBuffer = cameraNormalBuffer;
            this.cameraMotionVectorsBuffer = cameraMotionVectorsBuffer;
            this.customDepthBuffer = customDepthBuffer;
            this.propertyBlock = propertyBlock;
#pragma warning restore CS0618

            cameraColorBufferHandle = TextureHandle.nullHandle;
            cameraDepthBufferHandle = TextureHandle.nullHandle;
            cameraNormalBufferHandle = TextureHandle.nullHandle;
            cameraMotionVectorsBufferHandle = TextureHandle.nullHandle;
        }

        internal CustomPassContext(
            HDCamera hdCamera, CullingResults cullingResults,
            CullingResults cameraCullingResults)
        {
#pragma warning disable CS0618 // Member is obsolete
            renderContext = default;
            cmd = null;
            cameraColorBuffer = null;
            cameraDepthBuffer = null;
            customColorBuffer = null;
            cameraNormalBuffer = null;
            cameraMotionVectorsBuffer = null;
            customDepthBuffer = null;
            propertyBlock = null;
#pragma warning restore CS0618

            this.hdCamera = hdCamera;
            this.cullingResults = cullingResults;
            this.cameraCullingResults = cameraCullingResults;

            cameraColorBufferHandle = TextureHandle.nullHandle;
            cameraDepthBufferHandle = TextureHandle.nullHandle;
            cameraNormalBufferHandle = TextureHandle.nullHandle;
            cameraMotionVectorsBufferHandle = TextureHandle.nullHandle;
        }
    }
}
