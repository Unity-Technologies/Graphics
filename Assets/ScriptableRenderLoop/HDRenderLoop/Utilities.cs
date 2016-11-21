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

        public static void SetRenderTarget(RenderLoop renderLoop, RenderTargetIdentifier buffer, string name = "", int miplevel = 0, CubemapFace cubemapFace = CubemapFace.Unknown)
        {
            var cmd = new CommandBuffer();
            cmd.name = name;
            cmd.SetRenderTarget(buffer, miplevel, cubemapFace);
            renderLoop.ExecuteCommandBuffer(cmd);
            cmd.Dispose();
        }

        public static void SetRenderTarget(RenderLoop renderLoop, RenderTargetIdentifier colorBuffer, RenderTargetIdentifier depthBuffer, string name = "", int miplevel = 0, CubemapFace cubemapFace = CubemapFace.Unknown)
        {
            SetRenderTarget(renderLoop, colorBuffer, depthBuffer, ClearFlag.ClearNone, new Color(0.0f, 0.0f, 0.0f, 0.0f), name, miplevel, cubemapFace);
        }

        public static void SetRenderTarget(RenderLoop renderLoop, RenderTargetIdentifier colorBuffer, RenderTargetIdentifier depthBuffer, ClearFlag clearFlag, string name = "", int miplevel = 0, CubemapFace cubemapFace = CubemapFace.Unknown)
        {
            SetRenderTarget(renderLoop, colorBuffer, depthBuffer, clearFlag, new Color(0.0f, 0.0f, 0.0f, 0.0f), name, miplevel, cubemapFace);
        }

        public static void SetRenderTarget(RenderLoop renderLoop, RenderTargetIdentifier colorBuffer, RenderTargetIdentifier depthBuffer, ClearFlag clearFlag, Color clearColor, string name = "", int miplevel = 0, CubemapFace cubemapFace = CubemapFace.Unknown)
        {
            var cmd = new CommandBuffer();
            cmd.name = name;
            cmd.SetRenderTarget(colorBuffer, depthBuffer, miplevel, cubemapFace);
            if (clearFlag != ClearFlag.ClearNone)
                cmd.ClearRenderTarget((clearFlag & ClearFlag.ClearDepth) != 0, (clearFlag & ClearFlag.ClearColor) != 0, clearColor);
            renderLoop.ExecuteCommandBuffer(cmd);
            cmd.Dispose();
        }

        public static void SetRenderTarget(RenderLoop renderLoop, RenderTargetIdentifier[] colorBuffers, RenderTargetIdentifier depthBuffer, string name = "")
        {
            SetRenderTarget(renderLoop, colorBuffers, depthBuffer, ClearFlag.ClearNone, Color.black, name);
        }

        public static void SetRenderTarget(RenderLoop renderLoop, RenderTargetIdentifier[] colorBuffers, RenderTargetIdentifier depthBuffer, ClearFlag clearFlag = ClearFlag.ClearNone, string name = "")
        {
            SetRenderTarget(renderLoop, colorBuffers, depthBuffer, clearFlag, Color.black, name);
        }

        public static void SetRenderTarget(RenderLoop renderLoop, RenderTargetIdentifier[] colorBuffers, RenderTargetIdentifier depthBuffer, ClearFlag clearFlag, Color clearColor, string name = "")
        {
            var cmd = new CommandBuffer();
            cmd.name = name;
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
    }
}
