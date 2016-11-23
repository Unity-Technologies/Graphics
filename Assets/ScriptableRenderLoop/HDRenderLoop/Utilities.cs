using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;

using UnityObject = UnityEngine.Object;

namespace UnityEngine.Experimental.ScriptableRenderLoop
{
    [Flags]
    public enum ClearFlag
    {
        ClearNone = 0,
        ClearColor = 1,
        ClearDepth = 2
    }

    public class Utilities
    {
        public const RendererConfiguration kRendererConfigurationBakedLighting = RendererConfiguration.PerObjectLightProbe | RendererConfiguration.PerObjectReflectionProbes | RendererConfiguration.PerObjectLightmaps | RendererConfiguration.PerObjectLightProbeProxyVolume;


        // Render Target Management.
        public const ClearFlag kClearAll = ClearFlag.ClearDepth | ClearFlag.ClearColor;

        public static void SetRenderTarget(RenderLoop renderLoop, RenderTargetIdentifier buffer, int miplevel = 0, CubemapFace cubemapFace = CubemapFace.Unknown)
        {
            var cmd = new CommandBuffer();
            cmd.name = "";
            cmd.SetRenderTarget(buffer, miplevel, cubemapFace);
            renderLoop.ExecuteCommandBuffer(cmd);
            cmd.Dispose();
        }

        public static void SetRenderTarget(RenderLoop renderLoop, RenderTargetIdentifier colorBuffer, RenderTargetIdentifier depthBuffer, int miplevel = 0, CubemapFace cubemapFace = CubemapFace.Unknown)
        {
            SetRenderTarget(renderLoop, colorBuffer, depthBuffer, ClearFlag.ClearNone, new Color(0.0f, 0.0f, 0.0f, 0.0f), miplevel, cubemapFace);
        }

        public static void SetRenderTarget(RenderLoop renderLoop, RenderTargetIdentifier colorBuffer, RenderTargetIdentifier depthBuffer, ClearFlag clearFlag, int miplevel = 0, CubemapFace cubemapFace = CubemapFace.Unknown)
        {
            SetRenderTarget(renderLoop, colorBuffer, depthBuffer, clearFlag, new Color(0.0f, 0.0f, 0.0f, 0.0f), miplevel, cubemapFace);
        }

        public static void SetRenderTarget(RenderLoop renderLoop, RenderTargetIdentifier colorBuffer, RenderTargetIdentifier depthBuffer, ClearFlag clearFlag, Color clearColor, int miplevel = 0, CubemapFace cubemapFace = CubemapFace.Unknown)
        {
            var cmd = new CommandBuffer();
            cmd.name = "";
            cmd.SetRenderTarget(colorBuffer, depthBuffer, miplevel, cubemapFace);
            if (clearFlag != ClearFlag.ClearNone)
                cmd.ClearRenderTarget((clearFlag & ClearFlag.ClearDepth) != 0, (clearFlag & ClearFlag.ClearColor) != 0, clearColor);
            renderLoop.ExecuteCommandBuffer(cmd);
            cmd.Dispose();
        }

        public static void SetRenderTarget(RenderLoop renderLoop, RenderTargetIdentifier[] colorBuffers, RenderTargetIdentifier depthBuffer)
        {
            SetRenderTarget(renderLoop, colorBuffers, depthBuffer, ClearFlag.ClearNone, Color.black);
        }

        public static void SetRenderTarget(RenderLoop renderLoop, RenderTargetIdentifier[] colorBuffers, RenderTargetIdentifier depthBuffer, ClearFlag clearFlag = ClearFlag.ClearNone)
        {
            SetRenderTarget(renderLoop, colorBuffers, depthBuffer, clearFlag, Color.black);
        }

        public static void SetRenderTarget(RenderLoop renderLoop, RenderTargetIdentifier[] colorBuffers, RenderTargetIdentifier depthBuffer, ClearFlag clearFlag, Color clearColor)
        {
            var cmd = new CommandBuffer();
            cmd.name = "";
            cmd.SetRenderTarget(colorBuffers, depthBuffer);
            if (clearFlag != ClearFlag.ClearNone)
                cmd.ClearRenderTarget((clearFlag & ClearFlag.ClearDepth) != 0, (clearFlag & ClearFlag.ClearColor) != 0, clearColor);
            renderLoop.ExecuteCommandBuffer(cmd);
            cmd.Dispose();
        }

        // Miscellanous
        public static Material CreateEngineMaterial(string shaderPath)
        {
            var mat = new Material(Shader.Find(shaderPath))
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            return mat;
        }

        public static void Destroy(UnityObject obj)
        {
            if (obj != null)
            {
#if UNITY_EDITOR
                if (Application.isPlaying)
                    UnityObject.Destroy(obj);
                else
                    UnityObject.DestroyImmediate(obj);
#else
                    UnityObject.Destroy(obj);
#endif
            }
        }


        public class ProfilingSample
            : IDisposable
        {
            bool        disposed = false;
            RenderLoop  renderLoop;
            string      name;

            public ProfilingSample(string _name, RenderLoop _renderloop)
            {
                renderLoop = _renderloop;
                name = _name;

                CommandBuffer cmd = new CommandBuffer();
                cmd.name = "";
                cmd.BeginSample(name);
                renderLoop.ExecuteCommandBuffer(cmd);
                cmd.Dispose();
            }

            ~ProfilingSample()
            {
                Dispose(false);
            }

            public void Dispose()
            { 
                Dispose(true);
            }

            // Protected implementation of Dispose pattern.
            protected virtual void Dispose(bool disposing)
            {
                if (disposed)
                    return; 

                if (disposing)
                {
                    CommandBuffer cmd = new CommandBuffer();
                    cmd.name = "";
                    cmd.EndSample(name);
                    renderLoop.ExecuteCommandBuffer(cmd);
                    cmd.Dispose();
                }

                disposed = true;
            }
        }

        public static Matrix4x4 GetViewProjectionMatrix(Camera camera)
        {
            // The actual projection matrix used in shaders is actually massaged a bit to work across all platforms
            // (different Z value ranges etc.)
            var gpuProj = GL.GetGPUProjectionMatrix(camera.projectionMatrix, false);
            var gpuVP = gpuProj * camera.worldToCameraMatrix;

            return gpuVP;
        }

        public static Vector4 ComputeScreenSize(Camera camera)
        {
            return new Vector4(camera.pixelWidth, camera.pixelHeight, 1.0f / camera.pixelWidth, 1.0f / camera.pixelHeight);
        }
    }
}
