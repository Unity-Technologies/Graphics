// custom-begin:
using System.Collections.Generic;
using UnityEngine.VFX;
using System;
using System.Diagnostics;
using System.Linq;
using UnityEngine.Experimental.GlobalIllumination;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.RenderGraphModule;

namespace UnityEngine.Rendering.HighDefinition
{
    public partial class HDRenderPipeline : RenderPipeline
    {
        // This creates a public API for systems in the Hybrid Renderer to append rendering work onto a single, unified command buffer.
        // This command buffer will then be executed and submitted inside of HDRenderPipeline::Render() before any per-camera rendering is performed.
        // When HybridRendererCommandBuffer.RequestImmediateMode(isEnabled: true) is set, commands will be drained immediately at SystemEnd().
        // ImmediateMode exists for debugging purposes.
        public static class HybridRendererCommandBuffer
        {
            public static int GetSystemIDFromName(string name)
            {
                // In lieu of any standard string interning system, we're going to simply use Shader.PropertyToID() to generate stable IDs.
                int id = Shader.PropertyToID(name);
                s_HybridRendererCommandBufferData.EnsureProfilingSampler(name, id);
                return id;
            }

            public static CommandBuffer SystemBegin(int debugSystemID)
            {
                return s_HybridRendererCommandBufferData.Begin(debugSystemID);
            }

            public static void SystemEnd(int debugSystemID)
            {
                s_HybridRendererCommandBufferData.End(debugSystemID);

                var hdrp = (RenderPipelineManager.currentPipeline as HDRenderPipeline);
                if (hdrp == null)
                {
                    // HDRP is not initialized yet, rather than throwing out any work that was requested,
                    //// to be safe, lets allow multiple frames to be queued up, in case any frame-agnostic data is computed in this queue.
                    //s_HybridRendererCommandBuffer.AllowAdditionalFrameCommandBufferQueue();
                    s_HybridRendererCommandBufferData.Clear();
                }
            }

            public static void RequestImmediateMode(bool enabled)
            {
                s_HybridRendererCommandBufferData.RequestImmediateMode(enabled);
            }
        }

        private static HybridRendererCommandBufferData s_HybridRendererCommandBufferData = new HybridRendererCommandBufferData();

        private class HybridRendererCommandBufferData
        {
            private Dictionary<int, bool> debugSubmittedSystemState = new Dictionary<int, bool>();
            private Dictionary<int, ProfilingSampler> profilingSamplers = new Dictionary<int, ProfilingSampler>();
            private CommandBuffer cmd = null;
            private bool immediateModeEnabled = false;
            private bool immediateModeEnabledNext = false;

            public void EnsureProfilingSampler(string name, int id)
            {
                if (!profilingSamplers.TryGetValue(id, out var profilingSampler))
                {
                    profilingSamplers.Add(id, new ProfilingSampler(name));
                }
            }

            public void RequestImmediateMode(bool enabled)
            {
                Debug.Assert(debugSubmittedSystemState.Count == 0);
                immediateModeEnabledNext = enabled;
            }

            public void Clear()
            {
                debugSubmittedSystemState.Clear();
                if (cmd != null) { CommandBufferPool.Release(cmd); cmd = null; }
                immediateModeEnabled = immediateModeEnabledNext;
            }

            public CommandBuffer Begin(int debugSystemID)
            {
                Debug.Assert(!debugSubmittedSystemState.ContainsKey(debugSystemID));
                debugSubmittedSystemState.Add(debugSystemID, false);

                var cmd = EnsureCommandBuffer();
                profilingSamplers[debugSystemID].Begin(cmd);
                return cmd;
            }

            public void End(int debugSystemID)
            {
                Debug.Assert(debugSubmittedSystemState.ContainsKey(debugSystemID));
                if (debugSubmittedSystemState.TryGetValue(debugSystemID, out bool submitted))
                {
                    Debug.Assert(submitted == false);
                }
                else
                {
                    Debug.Assert(false);
                }

                debugSubmittedSystemState[debugSystemID] = true;

                profilingSamplers[debugSystemID].End(cmd);

                if (immediateModeEnabled)
                {
                    Graphics.ExecuteCommandBuffer(cmd);
                    cmd.Clear();
                }
            }

            public void Submit(ScriptableRenderContext renderContext)
            {
                if (cmd == null) { return; }

                renderContext.ExecuteCommandBuffer(cmd);
                renderContext.Submit();
                cmd.Clear();
                Clear();
            }

            private CommandBuffer EnsureCommandBuffer()
            {
                if (cmd == null) { cmd = CommandBufferPool.Get("HybridRendererCommandBuffer"); }
                return cmd;
            }

        }
    }
}