using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Rendering;
using UnityEngine.Rendering.LWRP;

namespace UnityEngine.Experimental.Rendering.LWRP
{
    public abstract class RendererSetup
    {
        public enum RenderPassBlock
        {
            BeforeMainRender,
            MainRender,
            AfterMainRender,
            Count,
        }
        
        public abstract void Setup(ref RenderingData renderingData);

        public virtual void SetupLights(ScriptableRenderContext context, ref RenderingData renderingData)
        {
        }

        public virtual void SetupCullingParameters(ref ScriptableCullingParameters cullingParameters,
            ref CameraData cameraData)
        {    
        }

        protected List<ScriptableRenderPass>[] m_ActiveRenderPassQueue = 
        {
            new List<ScriptableRenderPass>(),
            new List<ScriptableRenderPass>(),
            new List<ScriptableRenderPass>(),
        };

        protected List<DrawGroup> m_DrawGroups = new List<DrawGroup>(3);
        protected List<RenderPassFeature> m_RenderPassFeatures = new List<RenderPassFeature>(10);

        const string k_ClearRenderStateTag = "Clear Render State";
        const string k_RenderOcclusionMesh = "Render Occlusion Mesh";
        const string k_ReleaseResourcesTag = "Release Resources";

        public RendererSetup(RendererData data)
        {
            m_RenderPassFeatures.AddRange(data.renderPassFeatures.Where(x => x != null));
            m_DrawGroups.AddRange(data.drawGroups.Where(x => x != null));
        }
        public void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            Camera camera = renderingData.cameraData.camera;
            ClearRenderState(context);

            // Before Render Block
            // In this block inputs passes should execute. e.g, shadowmaps
            var secondaryGroupMasks = new List<DrawGroup>(1);
            secondaryGroupMasks.Add(m_DrawGroups[0]);
            
            ExecuteBlock(secondaryGroupMasks, RenderPassBlock.BeforeMainRender, context, ref renderingData);

            /// Configure shader variables and other unity properties that are required for rendering.
            /// * Setup Camera RenderTarget and Viewport
            /// * VR Camera Setup and SINGLE_PASS_STEREO props
            /// * Setup camera view, projection and their inverse matrices.
            /// * Setup properties: _WorldSpaceCameraPos, _ProjectionParams, _ScreenParams, _ZBufferParams, unity_OrthoParams
            /// * Setup camera world clip planes properties
            /// * Setup HDR keyword
            /// * Setup global time properties (_Time, _SinTime, _CosTime)
            bool stereoEnabled = renderingData.cameraData.isStereoEnabled;
            context.SetupCameraProperties(camera, stereoEnabled);
            SetupLights(context, ref renderingData);

            if (stereoEnabled)
                BeginXRRendering(context, camera);
            
            // In this block the bulk of render passes execute.
            ExecuteBlock(m_DrawGroups, RenderPassBlock.MainRender, context, ref renderingData);

            DrawGizmos(context, camera, GizmoSubset.PreImageEffects);

            // In this block after rendering drawing happens, e.g, post processing, video player capture.
            ExecuteBlock(secondaryGroupMasks, RenderPassBlock.AfterMainRender, context, ref renderingData, false);

            if (stereoEnabled)
                EndXRRendering(context, camera);

            DrawGizmos(context, camera, GizmoSubset.PostImageEffects);
            
            DisposePasses(context);
        }

        void ExecuteBlock(List<DrawGroup> drawGroups, RenderPassBlock renderPassBlock,
            ScriptableRenderContext context, ref RenderingData renderingData, bool submit = true)
        {
            int blockIndex = (int)renderPassBlock;
            for (int i = 0; i < m_ActiveRenderPassQueue[blockIndex].Count; ++i)
            {
                for (int currGroupIndex = 0; currGroupIndex < drawGroups.Count; ++currGroupIndex)
                {
                    var renderPass = m_ActiveRenderPassQueue[blockIndex][i];
                    ExecuteRenderPass(drawGroups[currGroupIndex], renderPass, context, ref renderingData);
                }
            }
            
            if (submit)
                context.Submit();
        }

        void ExecuteRenderPass(DrawGroup drawGroup, ScriptableRenderPass renderPass,
            ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (CoreUtils.HasFlag(renderPass.drawGroupsMask, drawGroup.mask))
            {
                if (drawGroup.overrideCamera)
                {
                    // TODO: Setup camera matrices
                    renderPass.filteringSettings.layerMask = drawGroup.layerMask;
                }
                renderPass.Execute(context, ref renderingData);
            }
        }

        public void Clear()
        {
            for (int i = 0; i < (int)RenderPassBlock.Count; ++i)
                m_ActiveRenderPassQueue[i].Clear();
        }

        public void ClearRenderState(ScriptableRenderContext context)
        {
            // Keywords are enabled while executing passes.
            CommandBuffer cmd = CommandBufferPool.Get(k_ClearRenderStateTag);
            cmd.DisableShaderKeyword(ShaderKeywordStrings.MainLightShadows);
            cmd.DisableShaderKeyword(ShaderKeywordStrings.MainLightShadowCascades);
            cmd.DisableShaderKeyword(ShaderKeywordStrings.AdditionalLightsVertex);
            cmd.DisableShaderKeyword(ShaderKeywordStrings.AdditionalLightsPixel);
            cmd.DisableShaderKeyword(ShaderKeywordStrings.AdditionalLightShadows);
            cmd.DisableShaderKeyword(ShaderKeywordStrings.SoftShadows);
            cmd.DisableShaderKeyword(ShaderKeywordStrings.MixedLightingSubtractive);
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        public void EnqueuePass(ScriptableRenderPass pass, RenderPassBlock renderPassBlock = RenderPassBlock.MainRender)
        {
            if (pass != null)
                m_ActiveRenderPassQueue[(int)renderPassBlock].Add(pass);
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

        public void BeginXRRendering(ScriptableRenderContext context, Camera camera)
        {
            context.StartMultiEye(camera);
            var cmd = CommandBufferPool.Get(k_RenderOcclusionMesh);
            XRUtils.DrawOcclusionMesh(cmd, camera, true);
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        public void EndXRRendering(ScriptableRenderContext context, Camera camera)
        {
            context.StopMultiEye(camera);
            context.StereoEndRender(camera);
        }

        [Conditional("UNITY_EDITOR")]
        public void DrawGizmos(ScriptableRenderContext context, Camera camera, GizmoSubset gizmoSubset)
        {
#if UNITY_EDITOR
            if (UnityEditor.Handles.ShouldRenderGizmos())
                context.DrawGizmos(camera, gizmoSubset);
#endif
        }

        void DisposePasses(ScriptableRenderContext context)
        {
            CommandBuffer cmd = CommandBufferPool.Get(k_ReleaseResourcesTag);

            for (int block = 0; block < (int)RenderPassBlock.Count; ++block)
                for (int i = 0; i < m_ActiveRenderPassQueue[block].Count; ++i)
                    m_ActiveRenderPassQueue[block][i].FrameCleanup(cmd);

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        protected void EnqueuePasses(RenderPassFeature.InjectionPoint injectionCallback, RenderPassFeature.InjectionPoint injectionCallbackMask,
            RenderTextureDescriptor baseDescriptor, RenderTargetHandle colorHandle, RenderTargetHandle depthHandle,
            RenderPassBlock renderPassBlock = RenderPassBlock.MainRender)
        {
            if (CoreUtils.HasFlag(injectionCallbackMask, injectionCallback))
            {
                foreach (var renderPassFeature in m_RenderPassFeatures)
                {
                    var renderPass = renderPassFeature.GetPassToEnqueue(injectionCallback, baseDescriptor, colorHandle, depthHandle);
                    if (renderPass != null)
                        EnqueuePass(renderPass, renderPassBlock);
                }
            }
        }
    }
}
