using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;

namespace UnityEngine.Rendering.HighDefinition
{
    // To avoid polluting HDCamera with state, this helper container can keep track, and garbage
    // collect view data from cameras.
    internal class UpscalerCameras
    {
        public class State : IDisposable
        {
            internal WeakReference<Camera> m_CamReference = new WeakReference<Camera>(null);

            public int key;
            public object data;
            public UInt64 lastFrameId { set; get; }

            public static int GetKey(Camera camera) => camera.GetInstanceID();

            public void Init(Camera camera)
            {
                m_CamReference.SetTarget(camera);
                key = GetKey(camera);
            }

            public bool IsAlive()
            {
                return m_CamReference.TryGetTarget(out _);
            }

            public void Invalidate()
            {
                m_CamReference.SetTarget(null);
            }

            public void Dispose()
            {
                Invalidate();
            }
        }

        //Amount of inactive frames dlss has rendered before we clean / destroy the plugin state.
        private static UInt64 sMaximumFrameExpiration = 400;

        private Dictionary<int, State> m_CameraStates = new Dictionary<int, State>();
        private List<int> m_InvalidCameraKeys = new List<int>();
        private UInt64 m_FrameId = 0;

        public Dictionary<int, State> cameras => m_CameraStates;

        public State GetState(Camera camera)
        {
            if (camera == null)
                return null;

            if (!m_CameraStates.TryGetValue(State.GetKey(camera), out var cameraState))
                return null;

            return cameraState;
        }

        public void TagUsed(State state)
        {
            state.lastFrameId = m_FrameId;
        }

        public State CreateState(Camera camera)
        {
            State state = GenericPool<State>.Get();
            state.Init(camera);
            m_CameraStates.Add(state.key, state);
            return state;
        }

        public bool HasCameraStateExpired(State cameraState)
        {
            return !cameraState.IsAlive() || (m_FrameId - cameraState.lastFrameId) >= sMaximumFrameExpiration;
        }

        public void InvalidateState(State state)
        {
            state.Invalidate();
            m_InvalidCameraKeys.Add(state.key);
        }

        public void ProcessExpiredCameras()
        {
            foreach (KeyValuePair<int, State> kv in m_CameraStates)
            {
                if (!HasCameraStateExpired(kv.Value))
                    continue;

                InvalidateState(kv.Value);
            }
        }

        public void NextFrame()
        {
            ++m_FrameId;
        }

        public void CleanupCameraStates()
        {
            if (m_InvalidCameraKeys.Count == 0)
                return;

            foreach (var invalidKey in m_InvalidCameraKeys)
            {
                if (!m_CameraStates.TryGetValue(invalidKey, out var cameraState))
                    continue;

                m_CameraStates.Remove(invalidKey);
                cameraState.Dispose();
                GenericPool<State>.Release(cameraState);
            }

            m_InvalidCameraKeys.Clear();
        }
    }

    internal struct UpscalerResolution
    {
        public uint width;
        public uint height;

        public static bool operator==(UpscalerResolution a, UpscalerResolution b) =>
            a.width == b.width && a.height == b.height;

        public static bool operator!=(UpscalerResolution a, UpscalerResolution b) =>
            !(a == b);

        public override bool Equals(object obj)
        {
            if (obj is UpscalerResolution)
                return (UpscalerResolution)obj == this;
            return false;
        }

        public override int GetHashCode()
        {
            return (int)(width ^ height);
        }
    }


    #region Render Graph Helper
    // Upscaler resource helpers for render graph integration.
    internal static class UpscalerResources
    {
        public struct ViewResources
        {
            public Texture source;
            public Texture output;
            public Texture depth;
            public Texture motionVectors;
            public Texture biasColorMask;
        }

        public struct CameraResources
        {
            internal ViewResources resources;
            internal bool copyToViews;
            internal ViewResources tmpView0;
            internal ViewResources tmpView1;
        }

        public struct ViewResourceHandles
        {
            public TextureHandle source;
            public TextureHandle output;
            public TextureHandle depth;
            public TextureHandle motionVectors;
            public TextureHandle biasColorMask;
            public void WriteResources(RenderGraphBuilder builder)
            {
                source = builder.WriteTexture(source);
                output = builder.WriteTexture(output);
                depth = builder.WriteTexture(depth);
                motionVectors = builder.WriteTexture(motionVectors);

                if (biasColorMask.IsValid())
                    biasColorMask = builder.WriteTexture(biasColorMask);
            }
        }

        public struct CameraResourcesHandles
        {
            internal ViewResourceHandles resources;
            internal bool copyToViews;
            internal ViewResourceHandles tmpView0;
            internal ViewResourceHandles tmpView1;
        }

        public static ViewResources GetViewResources(in ViewResourceHandles handles)
        {
            var resources = new ViewResources
            {
                source = (Texture)handles.source,
                output = (Texture)handles.output,
                depth = (Texture)handles.depth,
                motionVectors = (Texture)handles.motionVectors
            };

            resources.biasColorMask = (handles.biasColorMask.IsValid()) ? (Texture)handles.biasColorMask : (Texture)null;

            return resources;
        }

        public static UpscalerResources.CameraResourcesHandles CreateCameraResources(HDCamera camera, RenderGraph renderGraph, RenderGraphBuilder builder, in UpscalerResources.ViewResourceHandles resources)
        {
            var camResources = new UpscalerResources.CameraResourcesHandles();
            camResources.resources = resources;
            camResources.copyToViews = camera.xr.enabled && camera.xr.singlePassEnabled && camera.xr.viewCount > 1;

            if (camResources.copyToViews)
            {
                TextureHandle GetTmpViewXrTex(in TextureHandle handle)
                {
                    if (!handle.IsValid())
                        return TextureHandle.nullHandle;

                    var newTexDesc = renderGraph.GetTextureDesc(handle);
                    newTexDesc.slices = 1;
                    newTexDesc.dimension = TextureDimension.Tex2D;
                    return renderGraph.CreateTexture(newTexDesc);
                }

                void CreateCopyNoXR(in UpscalerResources.ViewResourceHandles input, out UpscalerResources.ViewResourceHandles newResources)
                {
                    newResources.source = GetTmpViewXrTex(input.source);
                    newResources.output = GetTmpViewXrTex(input.output);
                    newResources.depth = GetTmpViewXrTex(input.depth);
                    newResources.motionVectors = GetTmpViewXrTex(input.motionVectors);
                    newResources.biasColorMask = GetTmpViewXrTex(input.biasColorMask);
                    newResources.WriteResources(builder);
                }

                CreateCopyNoXR(resources, out camResources.tmpView0);
                CreateCopyNoXR(resources, out camResources.tmpView1);
            }

            return camResources;
        }

        public static UpscalerResources.CameraResources GetCameraResources(in UpscalerResources.CameraResourcesHandles handles)
        {
            var camResources = new UpscalerResources.CameraResources
            {
                resources = UpscalerResources.GetViewResources(handles.resources),
                copyToViews = handles.copyToViews
            };

            if (camResources.copyToViews)
            {
                camResources.tmpView0 = GetViewResources(handles.tmpView0);
                camResources.tmpView1 = GetViewResources(handles.tmpView1);
            }

            return camResources;
        }


    }
    #endregion
}
