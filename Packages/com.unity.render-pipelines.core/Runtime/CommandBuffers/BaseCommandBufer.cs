using System;
using System.Diagnostics;
using System.Collections.Generic;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RendererUtils;
using System.Diagnostics.CodeAnalysis;
using UnityEngine.Internal;
using UnityEngine.Experimental.Rendering.RenderGraphModule;

namespace UnityEngine.Experimental.Rendering
{
    /// <summary>
    /// Render graph command buffer types inherit from this base class.
    /// It provides some shared functionality for all command buffer types.
    /// </summary>
    public class BaseCommandBuffer
    {
        internal protected CommandBuffer m_WrappedCommandBuffer;
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

        internal protected void ThrowIfGlobalStateNotAllowed()
        {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            if (m_ExecutingPass != null && !m_ExecutingPass.allowGlobalState) throw new InvalidOperationException("Modifying global state from this command buffer is not allowed. Please ensure your render graph pass allows modifying global state.");
#endif
        }

        // Validation when it is unknown if the texture will be read or written
        internal protected void ValidateTextureHandle(TextureHandle h)
        {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            if (m_ExecutingPass == null) return;

            if (h.IsBuiltin()) return;

            if (!m_ExecutingPass.IsRead(h.handle) && !m_ExecutingPass.IsWritten(h.handle))
            {
                throw new Exception("Pass '" + m_ExecutingPass.name + "' is trying to use a texture on the command buffer that was never registered with the pass builder. Please indicate the texture use to the pass builder.");
            }
            if (m_ExecutingPass.IsAttachment(h))
            {
                throw new Exception("Pass '" + m_ExecutingPass.name + "' is using a texture as a fragment attachment (UseTextureFragment/UseTextureFragmentDepth) but is also trying to bind it as regular texture. Please fix this pass. ");
            }
#endif
        }

        internal protected void ValidateTextureHandleRead(TextureHandle h)
        {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            if (m_ExecutingPass == null) return;

            if (!m_ExecutingPass.IsRead(h.handle))
            {
                throw new Exception("Pass '" + m_ExecutingPass.name + "' is trying to read a texture on the command buffer that was never registered with the pass builder. Please indicate the texture as read to the pass builder.");
            }
            if (m_ExecutingPass.IsAttachment(h))
            {
                throw new Exception("Pass '" + m_ExecutingPass.name + "' is using a texture as a fragment attachment (UseTextureFragment/UseTextureFragmentDepth) but is also trying to bind it as regular texture. Please fix this pass. ");
            }
#endif
        }

        internal protected void ValidateTextureHandleWrite(TextureHandle h)
        {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
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
                throw new Exception("Pass '" + m_ExecutingPass.name + "' is using a texture as a fragment attachment (UseTextureFragment/UseTextureFragmentDepth) but is also trying to bind it as regular texture. Please fix this pass. ");
            }
#endif
        }
    }
}
