using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;

namespace UnityEngine.Rendering.LWRP
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

        protected List<RenderPassFeature> m_RenderPassFeatures = new List<RenderPassFeature>(10);

        const string k_ClearRenderStateTag = "Clear Render State";
        const string k_RenderOcclusionMesh = "Render Occlusion Mesh";
        const string k_ReleaseResourcesTag = "Release Resources";

        public RendererSetup(RendererData data)
        {
            m_RenderPassFeatures.AddRange(data.renderPassFeatures.Where(x => x != null));
        }
        public void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            Camera camera = renderingData.cameraData.camera;
            ClearRenderState(context);

            // Before Render Block
            // In this block inputs passes should execute. e.g, shadowmaps
            ExecuteBlock(RenderPassBlock.BeforeMainRender, context, ref renderingData, true);

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
            ExecuteBlock(RenderPassBlock.MainRender, context, ref renderingData);

            DrawGizmos(context, camera, GizmoSubset.PreImageEffects);

            // In this block after rendering drawing happens, e.g, post processing, video player capture.
            ExecuteBlock(RenderPassBlock.AfterMainRender, context, ref renderingData);

            if (stereoEnabled)
                EndXRRendering(context, camera);

            DrawGizmos(context, camera, GizmoSubset.PostImageEffects);

            DisposePasses(context);
        }

        void ExecuteBlock(RenderPassBlock renderPassBlock,
            ScriptableRenderContext context, ref RenderingData renderingData, bool submit = false)
        {
            int blockIndex = (int)renderPassBlock;
            for (int i = 0; i < m_ActiveRenderPassQueue[blockIndex].Count; ++i)
            {
                var renderPass = m_ActiveRenderPassQueue[blockIndex][i];
                renderPass.Execute(context, ref renderingData);
            }
            
            if (submit)
                context.Submit();
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

        protected bool EnqueuePasses(RenderPassEvent renderPassEvent, List<ScriptableRenderPass> customRenderPasses, ref int startIndex, ref RenderingData renderingData)
        {
            if (startIndex >= customRenderPasses.Count)
                return false;

            int prevIndex = startIndex;
            while (startIndex < customRenderPasses.Count && customRenderPasses[startIndex].renderPassEvent == renderPassEvent)
            {
                var renderPass = customRenderPasses[startIndex];

                if (renderPass.renderPassEvent == renderPassEvent)
                {
                    if (renderPass.ShouldExecute(ref renderingData))
                    {
                        if (renderPass.renderPassEvent < RenderPassEvent.BeforeRenderingOpaques)
                            EnqueuePass(renderPass, RenderPassBlock.BeforeMainRender);
                        else if (renderPass.renderPassEvent >= RenderPassEvent.AfterRendering)
                            EnqueuePass(renderPass, RenderPassBlock.AfterMainRender);
                        else
                            EnqueuePass(renderPass);
                    }

                    startIndex++;
                }
            }

            return prevIndex != startIndex;
        }
    }
}
