using System;
using System.Diagnostics;
using UnityEngine.Rendering.RenderGraphModule;

namespace UnityEngine.Rendering
{
    /// <summary>
    /// Render graph command buffer types inherit from this base class.
    /// It provides some shared functionality for all command buffer types.
    /// </summary>
    public class BaseCommandBuffer
    {
        /// <summary>
        /// The instance of Unity's CommandBuffer that this class encapsulates, providing access to lower-level rendering commands.
        /// </summary>
        protected internal CommandBuffer m_WrappedCommandBuffer;
        internal RenderGraphPass m_ExecutingPass;

        // Users cannot directly create command buffers. The rendergraph creates them and passes them to callbacks.
        internal BaseCommandBuffer(CommandBuffer wrapped, RenderGraphPass executingPass, bool isAsync)
        {
            m_WrappedCommandBuffer = wrapped;
            m_ExecutingPass = executingPass;
            if (isAsync) m_WrappedCommandBuffer.SetExecutionFlags(CommandBufferExecutionFlags.AsyncCompute);
        }

        ///<summary>See (https://docs.unity3d.com/ScriptReference/Rendering.CommandBuffer-name.html)</summary>
        public string name => m_WrappedCommandBuffer.name;

        ///<summary>See (https://docs.unity3d.com/ScriptReference/Rendering.CommandBuffer-sizeInBytes.html)</summary>
        public int sizeInBytes => m_WrappedCommandBuffer.sizeInBytes;

        /// <summary>
        /// Checks if modifying the global state is permitted by the currently executing render graph pass.
        /// If such modifications are not allowed, an InvalidOperationException is thrown.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Thrown if the current render graph pass does not permit modifications to global state.
        /// </exception>
        [Conditional("DEVELOPMENT_BUILD"), Conditional("UNITY_EDITOR")]
        protected internal void ThrowIfGlobalStateNotAllowed()
        {
            if (m_ExecutingPass != null && !m_ExecutingPass.allowGlobalState) throw new InvalidOperationException($"{m_ExecutingPass.name}: Modifying global state from this command buffer is not allowed. Please ensure your render graph pass allows modifying global state.");
        }

        /// <summary>
        /// Checks if the Raster Command Buffer has set a valid render target.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown if the there are no active render targets.</exception>
        [Conditional("DEVELOPMENT_BUILD"), Conditional("UNITY_EDITOR")]
        protected internal void ThrowIfRasterNotAllowed()
        {
            if (m_ExecutingPass != null && !m_ExecutingPass.HasRenderAttachments()) throw new InvalidOperationException($"{m_ExecutingPass.name}: Using raster commands from a pass with no active render targets is not allowed as it will use an undefined render target state. Please set-up the pass's render targets using SetRenderAttachments.");
        }

        /// <summary>
        /// Ensures that the texture handle being used is valid for the currently executing render graph pass.
        /// This includes checks to ensure that the texture handle is registered for read or write access
        /// and is not being used incorrectly as a render target attachment.
        /// </summary>
        /// <param name="h">The TextureHandle to validate for the current pass.</param>
        /// <exception cref="Exception">
        /// Throws an exception if the texture handle is not properly registered for the pass or being used incorrectly.
        /// </exception>
        [Conditional("DEVELOPMENT_BUILD"), Conditional("UNITY_EDITOR")]
        protected internal void ValidateTextureHandle(TextureHandle h)
        {
            if(RenderGraph.enableValidityChecks)
            {
                if (m_ExecutingPass == null) return;

                if (h.IsBuiltin()) return;

                if (!m_ExecutingPass.IsRead(h.handle) && !m_ExecutingPass.IsWritten(h.handle))
                {
                    throw new Exception("Pass '" + m_ExecutingPass.name + "' is trying to use a texture on the command buffer that was never registered with the pass builder. Please indicate the texture use to the pass builder.");
                }
                if (m_ExecutingPass.IsAttachment(h))
                {
                    throw new Exception("Pass '" + m_ExecutingPass.name + "' is using a texture as a fragment attachment (SetRenderAttachment/SetRenderAttachmentDepth) but is also trying to bind it as regular texture. Please fix this pass. ");
                }
            }
        }

        /// <summary>
        /// Validates that the specified texture handle is registered for read access within the context of the current executing render graph pass.
        /// Throws an exception if the texture is not registered for reading or is used incorrectly as a render target attachment.
        /// </summary>
        /// <param name="h">The TextureHandle to validate for read access.</param>
        /// <exception cref="Exception">
        /// Throws an exception if the texture handle is either not registered as a readable resource or misused as both an attachment and a regular texture.
        /// </exception>
        [Conditional("DEVELOPMENT_BUILD"), Conditional("UNITY_EDITOR")]
        protected internal void ValidateTextureHandleRead(TextureHandle h)
        {
            if(RenderGraph.enableValidityChecks)
            {
                if (m_ExecutingPass == null) return;

                if (!m_ExecutingPass.IsRead(h.handle))
                {
                    throw new Exception("Pass '" + m_ExecutingPass.name + "' is trying to read a texture on the command buffer that was never registered with the pass builder. Please indicate the texture as read to the pass builder.");
                }
                if (m_ExecutingPass.IsAttachment(h))
                {
                    throw new Exception("Pass '" + m_ExecutingPass.name + "' is using a texture as a fragment attachment (SetRenderAttachment/SetRenderAttachmentDepth) but is also trying to bind it as regular texture. Please fix this pass. ");
                }
            }
        }

        /// <summary>
        /// Validates that the specified texture handle is registered for write access within the context of the current executing render graph pass.
        /// Additionally, it checks that built-in textures are not being written to, and that the texture is not incorrectly used as a render target attachment.
        /// An exception is thrown if any of these checks fail.
        /// </summary>
        /// <param name="h">The TextureHandle to validate for write access.</param>
        /// <exception cref="Exception">
        /// Throws an exception if the texture handle is not registered for writing, attempts to write to a built-in texture, or is misused as both a writeable resource and a render target attachment.
        /// </exception>
        [Conditional("DEVELOPMENT_BUILD"), Conditional("UNITY_EDITOR")]
        protected internal void ValidateTextureHandleWrite(TextureHandle h)
        {
            if(RenderGraph.enableValidityChecks)
            {
                if (m_ExecutingPass == null) return;

                if (h.IsBuiltin())
                {
                    throw new Exception("Pass '" + m_ExecutingPass.name + "' is trying to write to a built-in texture. This is not allowed built-in textures are small default resources like `white` or `black` that cannot be written to.");
                }

                if (!m_ExecutingPass.IsWritten(h.handle))
                {
                    throw new Exception("Pass '" + m_ExecutingPass.name + "' is trying to write a texture on the command buffer that was never registered with the pass builder. Please indicate the texture as written to the pass builder.");
                }
                if (m_ExecutingPass.IsAttachment(h))
                {
                    throw new Exception("Pass '" + m_ExecutingPass.name + "' is using a texture as a fragment attachment (SetRenderAttachment/SetRenderAttachmentDepth) but is also trying to bind it as regular texture. Please fix this pass. ");
                }
            }
        }
    }
}
