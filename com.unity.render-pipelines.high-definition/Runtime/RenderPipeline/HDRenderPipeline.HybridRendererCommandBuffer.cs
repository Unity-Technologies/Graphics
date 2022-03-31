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
            public static HybridRendererCommandBufferSystemHandle CreateSystemHandle(string name, string worldName)
            {
                // In lieu of any standard string interning system, we're going to simply use Shader.PropertyToID() to generate stable IDs.
                var handle = new HybridRendererCommandBufferSystemHandle()
                {
                    systemID = Shader.PropertyToID(name),
                    worldID = Shader.PropertyToID(worldName)
                };
                s_HybridRendererCommandBufferData.EnsureProfilingSampler(name, handle);
                return handle;
            }

            public static CommandBuffer SystemBegin(HybridRendererCommandBufferSystemHandle handle)
            {
                return s_HybridRendererCommandBufferData.Begin(handle);
            }

            public static void SystemEnd(HybridRendererCommandBufferSystemHandle handle)
            {
                s_HybridRendererCommandBufferData.End(handle);

                var hdrp = (RenderPipelineManager.currentPipeline as HDRenderPipeline);
                if (hdrp == null)
                {
                    // HDRP is not initialized yet, rather than throwing out any work that was requested,
                    // to be safe, lets immediately submit the work, to ensure any global frame agnostic state changes get setup correctly.
                    s_HybridRendererCommandBufferData.SubmitImmediate();
                }
            }

            public static void RequestImmediateMode(bool enabled)
            {
                s_HybridRendererCommandBufferData.RequestImmediateMode(enabled);
            }
        }

        private static HybridRendererCommandBufferData s_HybridRendererCommandBufferData = new HybridRendererCommandBufferData();

        public struct HybridRendererCommandBufferSystemHandle : IEquatable<HybridRendererCommandBufferSystemHandle>
        {
            public int systemID;
            public int worldID;

            public bool Equals(HybridRendererCommandBufferSystemHandle keyOther)
            {
                return (this.systemID == keyOther.systemID)
                    && (this.worldID == keyOther.worldID);
            }

            public override bool Equals(object other)
            {
                return other is HybridRendererCommandBufferSystemHandle key && Equals(key);
            }

            public override int GetHashCode()
            {
                var hash = systemID.GetHashCode();
                hash = hash * 23 + worldID.GetHashCode();

                return hash;
            }
        }

        private class HybridRendererCommandBufferData
        {
            private Dictionary<HybridRendererCommandBufferSystemHandle, bool> debugSubmittedSystemState = new Dictionary<HybridRendererCommandBufferSystemHandle, bool>();
            private Dictionary<int, ProfilingSampler> profilingSamplers = new Dictionary<int, ProfilingSampler>();
            private CommandBuffer cmd = null;
            private bool immediateModeEnabled = false;
            private bool immediateModeEnabledNext = false;

            public void EnsureProfilingSampler(string name, HybridRendererCommandBufferSystemHandle handle)
            {
                if (!profilingSamplers.TryGetValue(handle.systemID, out var profilingSampler))
                {
                    profilingSamplers.Add(handle.systemID, new ProfilingSampler(name));
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

            public CommandBuffer Begin(HybridRendererCommandBufferSystemHandle handle)
            {
                if (debugSubmittedSystemState.ContainsKey(handle))
                {
                    // Encountered previous frame data, or a system from another world updating.
                    // In the editor, there are a few edge cases where this can happen, such as changes to Project Settings (oddly enough).
                    // Simply submit the command buffer immediately, and do not log any warnings.
                    // If we are in a build, this case is unexpected, so we log a warning.
                    SubmitImmediate();

#if !UNITY_EDITOR
                    Debug.LogWarning("Warning: HybridRendererCommandBuffer: Encountered unexpected case of a command buffer having not been submitted between Simulation Update loops. It should have been submitted in the HDRenderPipeline::Render() loop.");
#endif
                }
                debugSubmittedSystemState.Add(handle, false);

                var cmd = EnsureCommandBuffer();
                profilingSamplers[handle.systemID].Begin(cmd);
                return cmd;
            }

            public void End(HybridRendererCommandBufferSystemHandle handle)
            {
                if (debugSubmittedSystemState.TryGetValue(handle, out bool submitted))
                {
                    Debug.AssertFormat(submitted == false, "Error: Encountered Hybrid Rendering System {0} with an already submitted command buffer. Was End() already called in this Simulation Update?", profilingSamplers[handle.systemID].name);
                }
                else
                {
                    Debug.AssertFormat(false, "Error: Encountered Hybrid Rendering System {0} End() call with no Begin() call.", profilingSamplers[handle.systemID].name);
                }

                debugSubmittedSystemState[handle] = true;

                profilingSamplers[handle.systemID].End(cmd);

                if (immediateModeEnabled)
                {
                    SubmitImmediate();
                }
            }

            public void Submit(ScriptableRenderContext renderContext)
            {
                if (cmd != null)
                {
                    renderContext.ExecuteCommandBuffer(cmd);
                    renderContext.Submit();
                    cmd.Clear();
                }

                Clear();
            }

            public void SubmitImmediate()
            {
                if (cmd != null)
                {
                    Graphics.ExecuteCommandBuffer(cmd);
                    cmd.Clear();
                }

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