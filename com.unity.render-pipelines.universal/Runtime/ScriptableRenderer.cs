using System;
using System.Diagnostics;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine.Scripting.APIUpdating;

namespace UnityEngine.Rendering.Universal
{
    /// <summary>
    ///  Class <c>ScriptableRenderer</c> implements a rendering strategy. It describes how culling and lighting works and
    /// the effects supported.
    ///
    ///  A renderer can be used for all cameras or be overridden on a per-camera basis. It will implement light culling and setup
    /// and describe a list of <c>ScriptableRenderPass</c> to execute in a frame. The renderer can be extended to support more effect with additional
    ///  <c>ScriptableRendererFeature</c>. Resources for the renderer are serialized in <c>ScriptableRendererData</c>.
    ///
    /// he renderer resources are serialized in <c>ScriptableRendererData</c>.
    /// <seealso cref="ScriptableRendererData"/>
    /// <seealso cref="ScriptableRendererFeature"/>
    /// <seealso cref="ScriptableRenderPass"/>
    /// </summary>
    [MovedFrom("UnityEngine.Rendering.LWRP")] public abstract class ScriptableRenderer : IDisposable
    {
        /// <summary>
        /// Configures the supported features for this renderer. When creating custom renderers
        /// for Universal Render Pipeline you can choose to opt-in or out for specific features.
        /// </summary>
        public class RenderingFeatures
        {
            /// <summary>
            /// This setting controls if the camera editor should display the camera stack category.
            /// Renderers that don't support camera stacking will only render camera of type CameraRenderType.Base
            /// <see cref="CameraRenderType"/>
            /// <seealso cref="UniversalAdditionalCameraData.cameraStack"/>
            /// </summary>
            public bool cameraStacking { get; set; } = false;
        }

        void SetShaderTimeValues(float time, float deltaTime, float smoothDeltaTime, CommandBuffer cmd = null)
        {
            // We make these parameters to mirror those described in `https://docs.unity3d.com/Manual/SL-UnityShaderVariables.html
            float timeEights = time / 8f;
            float timeFourth = time / 4f;
            float timeHalf = time / 2f;

            // Time values
            Vector4 timeVector = time * new Vector4(1f / 20f, 1f, 2f, 3f);
            Vector4 sinTimeVector = new Vector4(Mathf.Sin(timeEights), Mathf.Sin(timeFourth), Mathf.Sin(timeHalf), Mathf.Sin(time));
            Vector4 cosTimeVector = new Vector4(Mathf.Cos(timeEights), Mathf.Cos(timeFourth), Mathf.Cos(timeHalf), Mathf.Cos(time));
            Vector4 deltaTimeVector = new Vector4(deltaTime, 1f / deltaTime, smoothDeltaTime, 1f / smoothDeltaTime);
            Vector4 timeParametersVector = new Vector4(time, Mathf.Sin(time), Mathf.Cos(time), 0.0f);

            if (cmd == null)
            {
                Shader.SetGlobalVector(UniversalRenderPipeline.PerFrameBuffer._Time, timeVector);
                Shader.SetGlobalVector(UniversalRenderPipeline.PerFrameBuffer._SinTime, sinTimeVector);
                Shader.SetGlobalVector(UniversalRenderPipeline.PerFrameBuffer._CosTime, cosTimeVector);
                Shader.SetGlobalVector(UniversalRenderPipeline.PerFrameBuffer.unity_DeltaTime, deltaTimeVector);
                Shader.SetGlobalVector(UniversalRenderPipeline.PerFrameBuffer._TimeParameters, timeParametersVector);
            }
            else
            {
                cmd.SetGlobalVector(UniversalRenderPipeline.PerFrameBuffer._Time, timeVector);
                cmd.SetGlobalVector(UniversalRenderPipeline.PerFrameBuffer._SinTime, sinTimeVector);
                cmd.SetGlobalVector(UniversalRenderPipeline.PerFrameBuffer._CosTime, cosTimeVector);
                cmd.SetGlobalVector(UniversalRenderPipeline.PerFrameBuffer.unity_DeltaTime, deltaTimeVector);
                cmd.SetGlobalVector(UniversalRenderPipeline.PerFrameBuffer._TimeParameters, timeParametersVector);
            }
        }

        public RenderTargetIdentifier cameraColorTarget
        {
            get => m_CameraColorTarget;
        }

        public RenderTargetIdentifier cameraDepth
        {
            get => m_CameraDepthTarget;
        }

        protected List<ScriptableRendererFeature> rendererFeatures
        {
            get => m_RendererFeatures;
        }

        protected List<ScriptableRenderPass> activeRenderPassQueue
        {
            get => m_ActiveRenderPassQueue;
        }

        /// <summary>
        /// Supported rendering features by this renderer.
        /// <see cref="SupportedRenderingFeatures"/>
        /// </summary>
        public RenderingFeatures supportedRenderingFeatures { get; set; } = new RenderingFeatures();

        static class RenderPassBlock
        {
            // Executes render passes that are inputs to the main rendering
            // but don't depend on camera state. They all render in monoscopic mode. f.ex, shadow maps.
            public static readonly int BeforeRendering = 0;

            // Main bulk of render pass execution. They required camera state to be properly set
            // and when enabled they will render in stereo.
            public static readonly int MainRenderingOpaque = 1;
            public static readonly int MainRenderingTransparent = 2;

            // Execute after Post-processing.
            public static readonly int AfterRendering = 3;
        }

        const int k_RenderPassBlockCount = 4;

        List<ScriptableRenderPass> m_ActiveRenderPassQueue = new List<ScriptableRenderPass>(32);
        List<ScriptableRendererFeature> m_RendererFeatures = new List<ScriptableRendererFeature>(10);
        RenderTargetIdentifier m_CameraColorTarget;
        RenderTargetIdentifier m_CameraDepthTarget;
        bool m_FirstTimeCameraColorTargetIsBound = true; // flag used to track when m_CameraColorTarget should be cleared (if necessary), as well as other special actions only performed the first time m_CameraColorTarget is bound as a render target
        bool m_FirstTimeCameraDepthTargetIsBound = true; // flag used to track when m_CameraDepthTarget should be cleared (if necessary), the first time m_CameraDepthTarget is bound as a render target
        bool m_XRRenderTargetNeedsClear = false;

        const string k_SetCameraRenderStateTag = "Clear Render State";
        const string k_SetRenderTarget = "Set RenderTarget";
        const string k_ReleaseResourcesTag = "Release Resources";

        static RenderTargetIdentifier[] m_ActiveColorAttachments = new RenderTargetIdentifier[]{0, 0, 0, 0, 0, 0, 0, 0 };
        static RenderTargetIdentifier m_ActiveDepthAttachment;
        static bool m_InsideStereoRenderBlock;

        // CommandBuffer.SetRenderTarget(RenderTargetIdentifier[] colors, RenderTargetIdentifier depth, int mipLevel, CubemapFace cubemapFace, int depthSlice);
        // called from CoreUtils.SetRenderTarget will issue a warning assert from native c++ side if "colors" array contains some invalid RTIDs.
        // To avoid that warning assert we trim the RenderTargetIdentifier[] arrays we pass to CoreUtils.SetRenderTarget.
        // To avoid re-allocating a new array every time we do that, we re-use one of these arrays:
        static RenderTargetIdentifier[][] m_TrimmedColorAttachmentCopies = new RenderTargetIdentifier[][]
        {
            new RenderTargetIdentifier[0],                          // m_TrimmedColorAttachmentCopies[0] is an array of 0 RenderTargetIdentifier - only used to make indexing code easier to read
            new RenderTargetIdentifier[]{0},                        // m_TrimmedColorAttachmentCopies[1] is an array of 1 RenderTargetIdentifier
            new RenderTargetIdentifier[]{0, 0},                     // m_TrimmedColorAttachmentCopies[2] is an array of 2 RenderTargetIdentifiers
            new RenderTargetIdentifier[]{0, 0, 0},                  // m_TrimmedColorAttachmentCopies[3] is an array of 3 RenderTargetIdentifiers
            new RenderTargetIdentifier[]{0, 0, 0, 0},               // m_TrimmedColorAttachmentCopies[4] is an array of 4 RenderTargetIdentifiers
            new RenderTargetIdentifier[]{0, 0, 0, 0, 0},            // m_TrimmedColorAttachmentCopies[5] is an array of 5 RenderTargetIdentifiers
            new RenderTargetIdentifier[]{0, 0, 0, 0, 0, 0},         // m_TrimmedColorAttachmentCopies[6] is an array of 6 RenderTargetIdentifiers
            new RenderTargetIdentifier[]{0, 0, 0, 0, 0, 0, 0},      // m_TrimmedColorAttachmentCopies[7] is an array of 7 RenderTargetIdentifiers
            new RenderTargetIdentifier[]{0, 0, 0, 0, 0, 0, 0, 0 },  // m_TrimmedColorAttachmentCopies[8] is an array of 8 RenderTargetIdentifiers
        };

        internal static void ConfigureActiveTarget(RenderTargetIdentifier colorAttachment,
            RenderTargetIdentifier depthAttachment)
        {
            m_ActiveColorAttachments[0] = colorAttachment;
            for (int i = 1; i < m_ActiveColorAttachments.Length; ++i)
                m_ActiveColorAttachments[i] = 0;

            m_ActiveDepthAttachment = depthAttachment;
        }

        public ScriptableRenderer(ScriptableRendererData data)
        {
            foreach (var feature in data.rendererFeatures)
            {
                if (feature == null)
                    continue;

                feature.Create();
                m_RendererFeatures.Add(feature);
            }
            Clear(CameraRenderType.Base);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
        }

        /// <summary>
        /// Configures the camera target.
        /// </summary>
        /// <param name="colorTarget">Camera color target. Pass BuiltinRenderTextureType.CameraTarget if rendering to backbuffer.</param>
        /// <param name="depthTarget">Camera depth target. Pass BuiltinRenderTextureType.CameraTarget if color has depth or rendering to backbuffer.</param>
        public void ConfigureCameraTarget(RenderTargetIdentifier colorTarget, RenderTargetIdentifier depthTarget)
        {
            m_CameraColorTarget = colorTarget;
            m_CameraDepthTarget = depthTarget;
        }

        /// <summary>
        /// Configures the render passes that will execute for this renderer.
        /// This method is called per-camera every frame.
        /// </summary>
        /// <param name="context">Use this render context to issue any draw commands during execution.</param>
        /// <param name="renderingData">Current render state information.</param>
        /// <seealso cref="ScriptableRenderPass"/>
        /// <seealso cref="ScriptableRendererFeature"/>
        public abstract void Setup(ScriptableRenderContext context, ref RenderingData renderingData);

        /// <summary>
        /// Override this method to implement the lighting setup for the renderer. You can use this to
        /// compute and upload light CBUFFER for example.
        /// </summary>
        /// <param name="context">Use this render context to issue any draw commands during execution.</param>
        /// <param name="renderingData">Current render state information.</param>
        public virtual void SetupLights(ScriptableRenderContext context, ref RenderingData renderingData)
        {
        }

        /// <summary>
        /// Override this method to configure the culling parameters for the renderer. You can use this to configure if
        /// lights should be culled per-object or the maximum shadow distance for example.
        /// </summary>
        /// <param name="cullingParameters">Use this to change culling parameters used by the render pipeline.</param>
        /// <param name="cameraData">Current render state information.</param>
        public virtual void SetupCullingParameters(ref ScriptableCullingParameters cullingParameters,
            ref CameraData cameraData)
        {
        }

        /// <summary>
        /// Called upon finishing rendering the camera stack. You can release any resources created by the renderer here.
        /// </summary>
        /// <param name="cmd"></param>
        public virtual void FinishRendering(CommandBuffer cmd)
        {
        }

        /// <summary>
        /// Execute the enqueued render passes. This automatically handles editor and stereo rendering.
        /// </summary>
        /// <param name="context">Use this render context to issue any draw commands during execution.</param>
        /// <param name="renderingData">Current render state information.</param>
        public void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            ref CameraData cameraData = ref renderingData.cameraData;
            Camera camera = cameraData.camera;
            CommandBuffer cmd = CommandBufferPool.Get(k_SetCameraRenderStateTag);

            // Initialize Camera Render State
            SetCameraRenderState(cmd, ref cameraData);
            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();
            
            // Sort the render pass queue
            SortStable(m_ActiveRenderPassQueue);

            // Cache the time for after the call to `SetupCameraProperties` and set the time variables in shader
            // For now we set the time variables per camera, as we plan to remove `SetupCamearProperties`.
            // Setting the time per frame would take API changes to pass the variable to each camera render.
            // Once `SetupCameraProperties` is gone, the variable should be set higher in the call-stack.
#if UNITY_EDITOR
            float time = Application.isPlaying ? Time.time : Time.realtimeSinceStartup;
#else
            float time = Time.time;
#endif
            float deltaTime = Time.deltaTime;
            float smoothDeltaTime = Time.smoothDeltaTime;
            SetShaderTimeValues(time, deltaTime, smoothDeltaTime);

            // Upper limits for each block. Each block will contains render passes with events below the limit.
            NativeArray<RenderPassEvent> blockEventLimits = new NativeArray<RenderPassEvent>(k_RenderPassBlockCount, Allocator.Temp);
            blockEventLimits[RenderPassBlock.BeforeRendering] = RenderPassEvent.BeforeRenderingPrepasses;
            blockEventLimits[RenderPassBlock.MainRenderingOpaque] = RenderPassEvent.AfterRenderingOpaques;
            blockEventLimits[RenderPassBlock.MainRenderingTransparent] = RenderPassEvent.AfterRenderingPostProcessing;
            blockEventLimits[RenderPassBlock.AfterRendering] = (RenderPassEvent)Int32.MaxValue;

            NativeArray<int> blockRanges = new NativeArray<int>(blockEventLimits.Length + 1, Allocator.Temp);
            // blockRanges[0] is always 0
            // blockRanges[i] is the index of the first RenderPass found in m_ActiveRenderPassQueue that has a ScriptableRenderPass.renderPassEvent higher than blockEventLimits[i] (i.e, should be executed after blockEventLimits[i])
            // blockRanges[blockEventLimits.Length] is m_ActiveRenderPassQueue.Count
            FillBlockRanges(blockEventLimits, blockRanges);
            blockEventLimits.Dispose();

            SetupLights(context, ref renderingData);

            // Before Render Block. This render blocks always execute in mono rendering.
            // Camera is not setup. Lights are not setup.
            // Used to render input textures like shadowmaps.
            ExecuteBlock(RenderPassBlock.BeforeRendering, blockRanges, context, ref renderingData);

           for (int eyeIndex = 0; eyeIndex < renderingData.cameraData.numberOfXRPasses; ++eyeIndex)
           {
            /// Configure shader variables and other unity properties that are required for rendering.
            /// * Setup Camera RenderTarget and Viewport
            /// * VR Camera Setup and SINGLE_PASS_STEREO props
            /// * Setup camera view, projection and their inverse matrices.
            /// * Setup properties: _WorldSpaceCameraPos, _ProjectionParams, _ScreenParams, _ZBufferParams, unity_OrthoParams
            /// * Setup camera world clip planes properties
            /// * Setup HDR keyword
            /// * Setup global time properties (_Time, _SinTime, _CosTime)
            bool stereoEnabled = renderingData.cameraData.isStereoEnabled;
            context.SetupCameraProperties(camera, stereoEnabled, eyeIndex);

            // If overlay camera, we have to reset projection related matrices due to inheriting viewport from base
            // camera. This changes the aspect ratio, which requires to recompute projection.
            // TODO: We need to expose all work done in SetupCameraProperties above to c# land. This not only
            // avoids resetting values but also guarantee values are correct for all systems.
            // Known Issue: billboard will not work with camera stacking when using viewport with aspect ratio different from default aspect.
            if (cameraData.renderType == CameraRenderType.Overlay)
            {
                cmd.SetViewProjectionMatrices(cameraData.viewMatrix, cameraData.projectionMatrix);
            }

            // Override time values from when `SetupCameraProperties` were called.
            // They might be a frame behind.
            // We can remove this after removing `SetupCameraProperties` as the values should be per frame, and not per camera.
            SetShaderTimeValues(time, deltaTime, smoothDeltaTime, cmd);
            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();

            if (stereoEnabled)
                BeginXRRendering(context, camera, eyeIndex);

#if VISUAL_EFFECT_GRAPH_0_0_1_OR_NEWER
            var localCmd = CommandBufferPool.Get(string.Empty);
            //Triggers dispatch per camera, all global parameters should have been setup at this stage.
            VFX.VFXManager.ProcessCameraCommand(camera, localCmd);
            context.ExecuteCommandBuffer(localCmd);
            CommandBufferPool.Release(localCmd);
#endif

            // In the opaque and transparent blocks the main rendering executes.

            // Opaque blocks...
            ExecuteBlock(RenderPassBlock.MainRenderingOpaque, blockRanges, context, ref renderingData, eyeIndex);

            // Transparent blocks...
            ExecuteBlock(RenderPassBlock.MainRenderingTransparent, blockRanges, context, ref renderingData, eyeIndex);

            // Draw Gizmos...
            DrawGizmos(context, camera, GizmoSubset.PreImageEffects);

            // In this block after rendering drawing happens, e.g, post processing, video player capture.
            ExecuteBlock(RenderPassBlock.AfterRendering, blockRanges, context, ref renderingData, eyeIndex);

            if (stereoEnabled)
                EndXRRendering(context, renderingData, eyeIndex);
            }

            DrawGizmos(context, camera, GizmoSubset.PostImageEffects);

            InternalFinishRendering(context, renderingData.resolveFinalTarget);
            blockRanges.Dispose();
            CommandBufferPool.Release(cmd);
        }

        /// <summary>
        /// Enqueues a render pass for execution.
        /// </summary>
        /// <param name="pass">Render pass to be enqueued.</param>
        public void EnqueuePass(ScriptableRenderPass pass)
        {
            m_ActiveRenderPassQueue.Add(pass);
        }

        /// <summary>
        /// Returns a clear flag based on CameraClearFlags.
        /// </summary>
        /// <param name="cameraClearFlags">Camera clear flags.</param>
        /// <returns>A clear flag that tells if color and/or depth should be cleared.</returns>
        protected static ClearFlag GetCameraClearFlag(ref CameraData cameraData)
        {
            var cameraClearFlags = cameraData.camera.clearFlags;

#if UNITY_EDITOR
            // We need public API to tell if FrameDebugger is active and enabled. In that case
            // we want to force a clear to see properly the drawcall stepping.
            // For now, to fix FrameDebugger in Editor, we force a clear.
            cameraClearFlags = CameraClearFlags.SolidColor;
#endif

            // Universal RP doesn't support CameraClearFlags.DepthOnly and CameraClearFlags.Nothing.
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

            // Overlay cameras composite on top of previous ones. They don't clear color.
            // For overlay cameras we check if depth should be cleared on not.
            if (cameraData.renderType == CameraRenderType.Overlay)
                return (cameraData.clearDepth) ? ClearFlag.Depth : ClearFlag.None;

            // Always clear on first render pass in mobile as it's same perf of DontCare and avoid tile clearing issues.
            if (Application.isMobilePlatform)
                return ClearFlag.All;

            if ((cameraClearFlags == CameraClearFlags.Skybox && RenderSettings.skybox != null) ||
                cameraClearFlags == CameraClearFlags.Nothing)
                return ClearFlag.Depth;

            return ClearFlag.All;
        }

        // Initialize Camera Render State
        // Place all per-camera rendering logic that is generic for all types of renderers here.
        void SetCameraRenderState(CommandBuffer cmd, ref CameraData cameraData)
        {
            // Reset per-camera shader keywords. They are enabled depending on which render passes are executed.
            cmd.DisableShaderKeyword(ShaderKeywordStrings.MainLightShadows);
            cmd.DisableShaderKeyword(ShaderKeywordStrings.MainLightShadowCascades);
            cmd.DisableShaderKeyword(ShaderKeywordStrings.AdditionalLightsVertex);
            cmd.DisableShaderKeyword(ShaderKeywordStrings.AdditionalLightsPixel);
            cmd.DisableShaderKeyword(ShaderKeywordStrings.AdditionalLightShadows);
            cmd.DisableShaderKeyword(ShaderKeywordStrings.SoftShadows);
            cmd.DisableShaderKeyword(ShaderKeywordStrings.MixedLightingSubtractive);
            cmd.DisableShaderKeyword(ShaderKeywordStrings.LinearToSRGBConversion);
        }

        internal void Clear(CameraRenderType cameraType)
        {
            m_ActiveColorAttachments[0] = BuiltinRenderTextureType.CameraTarget;
            for (int i = 1; i < m_ActiveColorAttachments.Length; ++i)
                m_ActiveColorAttachments[i] = 0;

            m_ActiveDepthAttachment = BuiltinRenderTextureType.CameraTarget;

            m_InsideStereoRenderBlock = false;

            m_FirstTimeCameraColorTargetIsBound = cameraType == CameraRenderType.Base;
            m_FirstTimeCameraDepthTargetIsBound = true;

            m_ActiveRenderPassQueue.Clear();

            m_CameraColorTarget = BuiltinRenderTextureType.CameraTarget;
            m_CameraDepthTarget = BuiltinRenderTextureType.CameraTarget;
        }

        void ExecuteBlock(int blockIndex, NativeArray<int> blockRanges,
            ScriptableRenderContext context, ref RenderingData renderingData, int eyeIndex = 0, bool submit = false)
        {
            int endIndex = blockRanges[blockIndex + 1];
            for (int currIndex = blockRanges[blockIndex]; currIndex < endIndex; ++currIndex)
            {
                var renderPass = m_ActiveRenderPassQueue[currIndex];
                ExecuteRenderPass(context, renderPass, ref renderingData, eyeIndex);
            }

            if (submit)
                context.Submit();
        }

        void ExecuteRenderPass(ScriptableRenderContext context, ScriptableRenderPass renderPass, ref RenderingData renderingData, int eyeIndex)
        {
            ref CameraData cameraData = ref renderingData.cameraData;
            Camera camera = cameraData.camera;
            bool firstTimeStereo = false;

            CommandBuffer cmd = CommandBufferPool.Get(k_SetRenderTarget);
            renderPass.Configure(cmd, cameraData.cameraTargetDescriptor);
            renderPass.eyeIndex = eyeIndex;

            ClearFlag cameraClearFlag = GetCameraClearFlag(ref cameraData);

            // We use a different code path for MRT since it calls a different version of API SetRenderTarget
            if (RenderingUtils.IsMRT(renderPass.colorAttachments))
            {
                // In the MRT path we assume that all color attachments are REAL color attachments,
                // and that the depth attachment is a REAL depth attachment too.


                // Determine what attachments need to be cleared. ----------------

                bool needCustomCameraColorClear = false;
                bool needCustomCameraDepthClear = false;

                int cameraColorTargetIndex = RenderingUtils.IndexOf(renderPass.colorAttachments, m_CameraColorTarget);
                if (cameraColorTargetIndex != -1 && (m_FirstTimeCameraColorTargetIsBound || (cameraData.isXRMultipass && m_XRRenderTargetNeedsClear) ))
                {
                    m_FirstTimeCameraColorTargetIsBound = false; // register that we did clear the camera target the first time it was bound
                    firstTimeStereo = true;

                    // Overlay cameras composite on top of previous ones. They don't clear.
                    // MTT: Commented due to not implemented yet
                    //                    if (renderingData.cameraData.renderType == CameraRenderType.Overlay)
                    //                        clearFlag = ClearFlag.None;

                    // We need to specifically clear the camera color target.
                    // But there is still a chance we don't need to issue individual clear() on each render-targets if they all have the same clear parameters.
                    needCustomCameraColorClear = (cameraClearFlag & ClearFlag.Color) != (renderPass.clearFlag & ClearFlag.Color)
                                                || CoreUtils.ConvertSRGBToActiveColorSpace(camera.backgroundColor) != renderPass.clearColor;

                    if(cameraData.isXRMultipass && m_XRRenderTargetNeedsClear)
                        // For multipass mode, if m_XRRenderTargetNeedsClear == true, then both color and depth buffer need clearing (not just color)
                        needCustomCameraDepthClear = (cameraClearFlag & ClearFlag.Depth) != (renderPass.clearFlag & ClearFlag.Depth);

                    m_XRRenderTargetNeedsClear = false; // register that the XR camera multi-pass target does not need clear any more (until next call to BeginXRRendering)
                }

                // Note: if we have to give up the assumption that no depthTarget can be included in the MRT colorAttachments, we might need something like this:
                // int cameraTargetDepthIndex = IndexOf(renderPass.colorAttachments, m_CameraDepthTarget);
                // if( !renderTargetAlreadySet && cameraTargetDepthIndex != -1 && m_FirstTimeCameraDepthTargetIsBound)
                // { ...
                // }

                if (renderPass.depthAttachment == m_CameraDepthTarget && m_FirstTimeCameraDepthTargetIsBound)
                // note: should be split m_XRRenderTargetNeedsClear into m_XRColorTargetNeedsClear and m_XRDepthTargetNeedsClear and use m_XRDepthTargetNeedsClear here?
                {
                    m_FirstTimeCameraDepthTargetIsBound = false;
                    //firstTimeStereo = true; // <- we do not call this here as the first render pass might be a shadow pass (non-stereo)
                    needCustomCameraDepthClear = (cameraClearFlag & ClearFlag.Depth) != (renderPass.clearFlag & ClearFlag.Depth);

                    //m_XRRenderTargetNeedsClear = false; // note: is it possible that XR camera multi-pass target gets clear first when bound as depth target?
                                                          //       in this case we might need need to register that it does not need clear any more (until next call to BeginXRRendering)
                }


                // Perform all clear operations needed. ----------------
                // We try to minimize calls to SetRenderTarget().

                // We get here only if cameraColorTarget needs to be handled separately from the rest of the color attachments.
                if (needCustomCameraColorClear)
                {
                    // Clear camera color render-target separately from the rest of the render-targets.

                    if ((cameraClearFlag & ClearFlag.Color) != 0)
                        SetRenderTarget(cmd, renderPass.colorAttachments[cameraColorTargetIndex], renderPass.depthAttachment, ClearFlag.Color, CoreUtils.ConvertSRGBToActiveColorSpace(camera.backgroundColor));

                    if ((renderPass.clearFlag & ClearFlag.Color) != 0)
                    {
                        uint otherTargetsCount = RenderingUtils.CountDistinct(renderPass.colorAttachments, m_CameraColorTarget);
                        var nonCameraAttachments = m_TrimmedColorAttachmentCopies[otherTargetsCount];
                        int writeIndex = 0;
                        for (int readIndex = 0; readIndex < renderPass.colorAttachments.Length; ++readIndex)
                        {
                            if (renderPass.colorAttachments[readIndex] != m_CameraColorTarget && renderPass.colorAttachments[readIndex] != 0)
                            {
                                nonCameraAttachments[writeIndex] = renderPass.colorAttachments[readIndex];
                                ++writeIndex;
                            }
                        }
                        if (writeIndex != otherTargetsCount)
                            Debug.LogError("writeIndex and otherTargetsCount values differed. writeIndex:" + writeIndex + " otherTargetsCount:" + otherTargetsCount);
                        SetRenderTarget(cmd, nonCameraAttachments, m_CameraDepthTarget, ClearFlag.Color, renderPass.clearColor);
                    }
                }

                // Bind all attachments, clear color only if there was no custom behaviour for cameraColorTarget, clear depth as needed.
                ClearFlag finalClearFlag = ClearFlag.None;
                finalClearFlag |= needCustomCameraDepthClear ? (cameraClearFlag & ClearFlag.Depth) : (renderPass.clearFlag & ClearFlag.Depth);
                finalClearFlag |= needCustomCameraColorClear ? 0 : (renderPass.clearFlag & ClearFlag.Color);

                // Only setup render target if current render pass attachments are different from the active ones.
                if (!RenderingUtils.SequenceEqual(renderPass.colorAttachments, m_ActiveColorAttachments) || renderPass.depthAttachment != m_ActiveDepthAttachment || finalClearFlag != ClearFlag.None)
                {
                    int lastValidRTindex = RenderingUtils.LastValid(renderPass.colorAttachments);
                    if (lastValidRTindex >= 0)
                    {
                        int rtCount = lastValidRTindex + 1;
                        var trimmedAttachments = m_TrimmedColorAttachmentCopies[rtCount];
                        for (int i = 0; i < rtCount; ++i)
                            trimmedAttachments[i] = renderPass.colorAttachments[i];
                        SetRenderTarget(cmd, trimmedAttachments, renderPass.depthAttachment, finalClearFlag, renderPass.clearColor);
                    }
                }
            }
            else
            {
                // Currently in non-MRT case, color attachment can actually be a depth attachment.

                RenderTargetIdentifier passColorAttachment = renderPass.colorAttachment;
                RenderTargetIdentifier passDepthAttachment = renderPass.depthAttachment;

                // When render pass doesn't call ConfigureTarget we assume it's expected to render to camera target
                // which might be backbuffer or the framebuffer render textures.
                if (!renderPass.overrideCameraTarget)
                {
                    passColorAttachment = m_CameraColorTarget;
                    passDepthAttachment = m_CameraDepthTarget;
                }

                ClearFlag finalClearFlag = ClearFlag.None;
                Color finalClearColor;

                if (passColorAttachment == m_CameraColorTarget && (m_FirstTimeCameraColorTargetIsBound || (cameraData.isXRMultipass && m_XRRenderTargetNeedsClear)))
                {
                    m_FirstTimeCameraColorTargetIsBound = false; // register that we did clear the camera target the first time it was bound

                    finalClearFlag |= (cameraClearFlag & ClearFlag.Color);
                    finalClearColor = CoreUtils.ConvertSRGBToActiveColorSpace(camera.backgroundColor);
                    firstTimeStereo = true;

                    if (m_FirstTimeCameraDepthTargetIsBound || (cameraData.isXRMultipass && m_XRRenderTargetNeedsClear))
                    {
                        // m_CameraColorTarget can be an opaque pointer to a RenderTexture with depth-surface.
                        // We cannot infer this information here, so we must assume both camera color and depth are first-time bound here (this is the legacy behaviour).
                        m_FirstTimeCameraDepthTargetIsBound = false;
                        finalClearFlag |= (cameraClearFlag & ClearFlag.Depth);
                    }

                    m_XRRenderTargetNeedsClear = false; // register that the XR camera multi-pass target does not need clear any more (until next call to BeginXRRendering)
                }
                else
                {
                    finalClearFlag |= (renderPass.clearFlag & ClearFlag.Color);
                    finalClearColor = renderPass.clearColor;
                }

                // Condition (m_CameraDepthTarget!=BuiltinRenderTextureType.CameraTarget) below prevents m_FirstTimeCameraDepthTargetIsBound flag from being reset during non-camera passes (such as Color Grading LUT). This ensures that in those cases, cameraDepth will actually be cleared during the later camera pass.
                if (   (m_CameraDepthTarget!=BuiltinRenderTextureType.CameraTarget ) && (passDepthAttachment == m_CameraDepthTarget || passColorAttachment == m_CameraDepthTarget) && m_FirstTimeCameraDepthTargetIsBound )
                // note: should be split m_XRRenderTargetNeedsClear into m_XRColorTargetNeedsClear and m_XRDepthTargetNeedsClear and use m_XRDepthTargetNeedsClear here?
                {
                    m_FirstTimeCameraDepthTargetIsBound = false;

                    finalClearFlag |= (cameraClearFlag & ClearFlag.Depth);

                    //firstTimeStereo = true; // <- we do not call this here as the first render pass might be a shadow pass (non-stereo)

                    // finalClearFlag |= (cameraClearFlag & ClearFlag.Color);  // <- m_CameraDepthTarget is never a color-surface, so no need to add this here.

                    //m_XRRenderTargetNeedsClear = false; // note: is it possible that XR camera multi-pass target gets clear first when bound as depth target?
                    //       in this case we might need need to register that it does not need clear any more (until next call to BeginXRRendering)
                }
                else
                    finalClearFlag |= (renderPass.clearFlag & ClearFlag.Depth);

                // Only setup render target if current render pass attachments are different from the active ones
                if (passColorAttachment != m_ActiveColorAttachments[0] || passDepthAttachment != m_ActiveDepthAttachment || finalClearFlag != ClearFlag.None)
                    SetRenderTarget(cmd, passColorAttachment, passDepthAttachment, finalClearFlag, finalClearColor);
            }

            // We must execute the commands recorded at this point because potential call to context.StartMultiEye(cameraData.camera) below will alter internal renderer states
            // Also, we execute the commands recorded at this point to ensure SetRenderTarget is called before RenderPass.Execute
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);

            if (firstTimeStereo && cameraData.isStereoEnabled )
            {
                // The following call alters internal renderer states (we can think of some of the states as global states).
                // So any cmd recorded before must be executed before calling into that built-in call.
                context.StartMultiEye(camera, eyeIndex);
                XRUtils.DrawOcclusionMesh(cmd, camera);
            }

            renderPass.Execute(context, ref renderingData);
        }

        void BeginXRRendering(ScriptableRenderContext context, Camera camera, int eyeIndex)
        {
            context.StartMultiEye(camera, eyeIndex);
            m_InsideStereoRenderBlock = true;
            m_XRRenderTargetNeedsClear = true;
        }

        void EndXRRendering(ScriptableRenderContext context, in RenderingData renderingData, int eyeIndex)
        {
            Camera camera = renderingData.cameraData.camera;
            context.StopMultiEye(camera);
            bool isLastPass = renderingData.resolveFinalTarget && (eyeIndex == renderingData.cameraData.numberOfXRPasses - 1);
            context.StereoEndRender(camera, eyeIndex, isLastPass);
            m_InsideStereoRenderBlock = false;
        }

        internal static void SetRenderTarget(CommandBuffer cmd, RenderTargetIdentifier colorAttachment, RenderTargetIdentifier depthAttachment, ClearFlag clearFlag, Color clearColor)
        {
            m_ActiveColorAttachments[0] = colorAttachment;
            for (int i = 1; i < m_ActiveColorAttachments.Length; ++i)
                m_ActiveColorAttachments[i] = 0;

            m_ActiveDepthAttachment = depthAttachment;

            RenderBufferLoadAction colorLoadAction = ((uint)clearFlag & (uint)ClearFlag.Color) != 0 ?
                RenderBufferLoadAction.DontCare : RenderBufferLoadAction.Load;

            RenderBufferLoadAction depthLoadAction = ((uint)clearFlag & (uint)ClearFlag.Depth) != 0 ?
                RenderBufferLoadAction.DontCare : RenderBufferLoadAction.Load;

            TextureDimension dimension = (m_InsideStereoRenderBlock) ? XRGraphics.eyeTextureDesc.dimension : TextureDimension.Tex2D;
            SetRenderTarget(cmd, colorAttachment, colorLoadAction, RenderBufferStoreAction.Store,
                depthAttachment, depthLoadAction, RenderBufferStoreAction.Store, clearFlag, clearColor, dimension);
        }

        static void SetRenderTarget(
            CommandBuffer cmd,
            RenderTargetIdentifier colorAttachment,
            RenderBufferLoadAction colorLoadAction,
            RenderBufferStoreAction colorStoreAction,
            ClearFlag clearFlags,
            Color clearColor,
            TextureDimension dimension)
        {
            if (dimension == TextureDimension.Tex2DArray)
                CoreUtils.SetRenderTarget(cmd, colorAttachment, clearFlags, clearColor, 0, CubemapFace.Unknown, -1);
            else
                CoreUtils.SetRenderTarget(cmd, colorAttachment, colorLoadAction, colorStoreAction, clearFlags, clearColor);
        }

        static void SetRenderTarget(
            CommandBuffer cmd,
            RenderTargetIdentifier colorAttachment,
            RenderBufferLoadAction colorLoadAction,
            RenderBufferStoreAction colorStoreAction,
            RenderTargetIdentifier depthAttachment,
            RenderBufferLoadAction depthLoadAction,
            RenderBufferStoreAction depthStoreAction,
            ClearFlag clearFlags,
            Color clearColor,
            TextureDimension dimension)
        {
            if (depthAttachment == BuiltinRenderTextureType.CameraTarget)
            {
                SetRenderTarget(cmd, colorAttachment, colorLoadAction, colorStoreAction, clearFlags, clearColor,
                    dimension);
            }
            else
            {
                if (dimension == TextureDimension.Tex2DArray)
                    CoreUtils.SetRenderTarget(cmd, colorAttachment, depthAttachment,
                        clearFlags, clearColor, 0, CubemapFace.Unknown, -1);
                else
                    CoreUtils.SetRenderTarget(cmd, colorAttachment, colorLoadAction, colorStoreAction,
                        depthAttachment, depthLoadAction, depthStoreAction, clearFlags, clearColor);
            }
        }

        static void SetRenderTarget(CommandBuffer cmd, RenderTargetIdentifier[] colorAttachments, RenderTargetIdentifier depthAttachment, ClearFlag clearFlag, Color clearColor)
        {
            m_ActiveColorAttachments = colorAttachments;
            m_ActiveDepthAttachment = depthAttachment;

            CoreUtils.SetRenderTarget(cmd, colorAttachments, depthAttachment, clearFlag, clearColor);
        }

        [Conditional("UNITY_EDITOR")]
        void DrawGizmos(ScriptableRenderContext context, Camera camera, GizmoSubset gizmoSubset)
        {
#if UNITY_EDITOR
            if (UnityEditor.Handles.ShouldRenderGizmos())
                context.DrawGizmos(camera, gizmoSubset);
#endif
        }

        // Fill in render pass indices for each block. End index is startIndex + 1.
        void FillBlockRanges(NativeArray<RenderPassEvent> blockEventLimits, NativeArray<int> blockRanges)
        {
            int currRangeIndex = 0;
            int currRenderPass = 0;
            blockRanges[currRangeIndex++] = 0;

            // For each block, it finds the first render pass index that has an event
            // higher than the block limit.
            for (int i = 0; i < blockEventLimits.Length - 1; ++i)
            {
                while (currRenderPass < m_ActiveRenderPassQueue.Count &&
                    m_ActiveRenderPassQueue[currRenderPass].renderPassEvent < blockEventLimits[i])
                    currRenderPass++;

                blockRanges[currRangeIndex++] = currRenderPass;
            }

            blockRanges[currRangeIndex] = m_ActiveRenderPassQueue.Count;
        }

        void InternalFinishRendering(ScriptableRenderContext context, bool resolveFinalTarget)
        {
            CommandBuffer cmd = CommandBufferPool.Get(k_ReleaseResourcesTag);

            for (int i = 0; i < m_ActiveRenderPassQueue.Count; ++i)
                m_ActiveRenderPassQueue[i].FrameCleanup(cmd);

            // Happens when rendering the last camera in the camera stack.
            if (resolveFinalTarget)
            {
                for (int i = 0; i < m_ActiveRenderPassQueue.Count; ++i)
                    m_ActiveRenderPassQueue[i].OnFinishCameraStackRendering(cmd);

                FinishRendering(cmd);
            }

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        internal static void SortStable(List<ScriptableRenderPass> list)
        {
            int j;
            for (int i = 1; i < list.Count; ++i)
            {
                ScriptableRenderPass curr = list[i];

                j = i - 1;
                for (; j >= 0 && curr < list[j]; --j)
                    list[j + 1] = list[j];

                list[j + 1] = curr;
            }
        }
    }
}
