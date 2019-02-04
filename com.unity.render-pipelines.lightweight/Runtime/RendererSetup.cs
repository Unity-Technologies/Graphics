using System;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine.Rendering;
using UnityEngine.Rendering.LWRP;

namespace UnityEngine.Experimental.Rendering.LWRP
{
    public abstract class RendererSetup
    {
        public abstract void Setup(ref RenderingData renderingData);

        protected List<ScriptableRenderPass> m_ActiveRenderPassQueue = new List<ScriptableRenderPass>();
        protected List<RenderPassFeature> m_RenderPassFeatures = new List<RenderPassFeature>(10);

        const string k_ReleaseResourcesTag = "Release Resources";

        public void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            // Keywords are enabled while executing passes.
            CommandBuffer cmd = CommandBufferPool.Get("Clear Pipeline Keywords");
            cmd.DisableShaderKeyword(ShaderKeywordStrings.MainLightShadows);
            cmd.DisableShaderKeyword(ShaderKeywordStrings.MainLightShadowCascades);
            cmd.DisableShaderKeyword(ShaderKeywordStrings.AdditionalLightsVertex);
            cmd.DisableShaderKeyword(ShaderKeywordStrings.AdditionalLightsPixel);
            cmd.DisableShaderKeyword(ShaderKeywordStrings.AdditionalLightShadows);
            cmd.DisableShaderKeyword(ShaderKeywordStrings.SoftShadows);
            cmd.DisableShaderKeyword(ShaderKeywordStrings.MixedLightingSubtractive);
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);

            for (int i = 0; i < m_ActiveRenderPassQueue.Count; ++i)
                m_ActiveRenderPassQueue[i].Execute(context, ref renderingData);

            DisposePasses(ref context);
        }

        public void Clear()
        {
            m_ActiveRenderPassQueue.Clear();
        }

        public void EnqueuePass(ScriptableRenderPass pass)
        {
            if (pass != null)
                m_ActiveRenderPassQueue.Add(pass);
        }

        public void SetupPerObjectLightIndices(ref CullingResults cullResults, ref LightData lightData)
        {
            if (lightData.additionalLightsCount == 0)
                return;

            var visibleLights = lightData.visibleLights;
            var perObjectLightIndexMap = cullResults.GetLightIndexMap(Allocator.Temp);
            int directionalLightsCount = 0;
            int additionalLightsCount = 0;

            // Disable all directional lights from the perobject light indices
            // Pipeline handles them globally.
            for (int i = 0; i < visibleLights.Length; ++i)
            {
                if (additionalLightsCount >= LightweightRenderPipeline.maxVisibleAdditionalLights)
                    break;

                VisibleLight light = visibleLights[i];
                if (light.lightType == LightType.Directional)
                {
                    perObjectLightIndexMap[i] = -1;
                    ++directionalLightsCount;
                }
                else
                {
                    perObjectLightIndexMap[i] -= directionalLightsCount;
                    ++additionalLightsCount;
                }
            }

            // Disable all remaining lights we cannot fit into the global light buffer.
            for (int i = directionalLightsCount + additionalLightsCount; i < visibleLights.Length; ++i)
                perObjectLightIndexMap[i] = -1;

            cullResults.SetLightIndexMap(perObjectLightIndexMap);
            perObjectLightIndexMap.Dispose();
        }

        public static ClearFlag GetCameraClearFlag(Camera camera)
        {
            if (camera == null)
                throw new ArgumentNullException("camera");

            CameraClearFlags cameraClearFlags = camera.clearFlags;

#if UNITY_EDITOR
            // We need public API to tell if FrameDebugger is active and enabled. In that case
            // we want to force a clear to see properly the drawcall stepping.
            // For now, to fix FrameDebugger in Editor, we force a clear. 
            cameraClearFlags = CameraClearFlags.SolidColor;
#endif

            // LWRP doesn't support CameraClearFlags.DepthOnly and CameraClearFlags.Nothing.
            // CameraClearFlags.DepthOnly has the same effect of CameraClearFlags.SolidColor
            // CameraClearFlags.Nothing clears Depth on PC/Desktop and in mobile it clears both
            // depth and color.
            // CameraClearFlags.Skybox clears depth only.

            // Implementation details:
            // Camera clear flags are used to initialize the attachments on the first render pass.
            // ClearFlag is used together with Tile Load action to figure out how to clear the camera render target.
            // In Tile Based GPUs ClearFlag.Depth + RenderBufferLoadAction.DontCare becomes DontCare load action.
            // While ClearFlag.All + RenderBufferLoadAction.DontCare become Clear load action.
            // In mobile we force ClearFlag.All as DontCare doesn't have noticeable perf. difference from Clear
            // and this avoid tile clearing issue when not rendering all pixels in some GPUs.
            // In desktop/consoles there's actually performance difference between DontCare and Clear.

            // RenderBufferLoadAction.DontCare in PC/Desktop behaves as not clearing screen
            // RenderBufferLoadAction.DontCare in Vulkan/Metal behaves as DontCare load action
            // RenderBufferLoadAction.DontCare in GLES behaves as glInvalidateBuffer

            // Always clear on first render pass in mobile as it's same perf of DontCare and avoid tile clearing issues.
            if (Application.isMobilePlatform)
                return ClearFlag.All;

            if ((cameraClearFlags == CameraClearFlags.Skybox && RenderSettings.skybox != null) ||
                cameraClearFlags == CameraClearFlags.Nothing)
                return ClearFlag.Depth;

            return ClearFlag.All;
        }

        void DisposePasses(ref ScriptableRenderContext context)
        {
            CommandBuffer cmd = CommandBufferPool.Get(k_ReleaseResourcesTag);

            for (int i = 0; i < m_ActiveRenderPassQueue.Count; ++i)
                m_ActiveRenderPassQueue[i].FrameCleanup(cmd);

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        protected void EnqueuePasses(RenderPassFeature.InjectionPoint injectionCallback, RenderPassFeature.InjectionPoint injectionCallbackMask,
            RenderTextureDescriptor baseDescriptor, RenderTargetHandle colorHandle, RenderTargetHandle depthHandle)
        {
            if (CoreUtils.HasFlag(injectionCallbackMask, injectionCallback))
            {
                foreach (var renderPassFeature in m_RenderPassFeatures)
                {
                    var renderPass = renderPassFeature.GetPassToEnqueue(injectionCallback, baseDescriptor, colorHandle, depthHandle);
                    if (renderPass != null)
                        EnqueuePass(renderPass);
                }
            }
        }
    }
}
