using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using Unity.Collections;
using UnityEngine.Scripting.APIUpdating;
using UnityEngine.Experimental.Rendering;

namespace UnityEngine.Rendering.Universal
{
    /// <summary>
    /// Input requirements for <c>ScriptableRenderPass</c>.
    /// </summary>
    /// <seealso cref="ConfigureInput"/>
    [Flags]
    public enum ScriptableRenderPassInput
    {
        None = 0,
        Depth = 1 << 0,
        Normal = 1 << 1,
        Color = 1 << 2,
        Motion = 1 << 3
    }

    // Note: Spaced built-in events so we can add events in between them
    // We need to leave room as we sort render passes based on event.
    // Users can also inject render pass events in a specific point by doing RenderPassEvent + offset
    /// <summary>
    /// Controls when the render pass executes.
    /// </summary>
    public enum RenderPassEvent
    {
        /// <summary>
        /// Executes a <c>ScriptableRenderPass</c> before rendering any other passes in the pipeline.
        /// Camera matrices and stereo rendering are not setup this point.
        /// You can use this to draw to custom input textures used later in the pipeline, f.ex LUT textures.
        /// </summary>
        BeforeRendering = 0,

        /// <summary>
        /// Executes a <c>ScriptableRenderPass</c> before rendering shadowmaps.
        /// Camera matrices and stereo rendering are not setup this point.
        /// </summary>
        BeforeRenderingShadows = 50,

        /// <summary>
        /// Executes a <c>ScriptableRenderPass</c> after rendering shadowmaps.
        /// Camera matrices and stereo rendering are not setup this point.
        /// </summary>
        AfterRenderingShadows = 100,

        /// <summary>
        /// Executes a <c>ScriptableRenderPass</c> before rendering prepasses, f.ex, depth prepass.
        /// Camera matrices and stereo rendering are already setup at this point.
        /// </summary>
        BeforeRenderingPrePasses = 150,

        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("Obsolete, to match the capital from 'Prepass' to 'PrePass' (UnityUpgradable) -> BeforeRenderingPrePasses")]
        BeforeRenderingPrepasses = 151,

        /// <summary>
        /// Executes a <c>ScriptableRenderPass</c> after rendering prepasses, f.ex, depth prepass.
        /// Camera matrices and stereo rendering are already setup at this point.
        /// </summary>
        AfterRenderingPrePasses = 200,

        /// <summary>
        /// Executes a <c>ScriptableRenderPass</c> before rendering gbuffer pass.
        /// </summary>
        BeforeRenderingGbuffer = 210,

        /// <summary>
        /// Executes a <c>ScriptableRenderPass</c> after rendering gbuffer pass.
        /// </summary>
        AfterRenderingGbuffer = 220,

        /// <summary>
        /// Executes a <c>ScriptableRenderPass</c> before rendering deferred shading pass.
        /// </summary>
        BeforeRenderingDeferredLights = 230,

        /// <summary>
        /// Executes a <c>ScriptableRenderPass</c> after rendering deferred shading pass.
        /// </summary>
        AfterRenderingDeferredLights = 240,

        /// <summary>
        /// Executes a <c>ScriptableRenderPass</c> before rendering opaque objects.
        /// </summary>
        BeforeRenderingOpaques = 250,

        /// <summary>
        /// Executes a <c>ScriptableRenderPass</c> after rendering opaque objects.
        /// </summary>
        AfterRenderingOpaques = 300,

        /// <summary>
        /// Executes a <c>ScriptableRenderPass</c> before rendering the sky.
        /// </summary>
        BeforeRenderingSkybox = 350,

        /// <summary>
        /// Executes a <c>ScriptableRenderPass</c> after rendering the sky.
        /// </summary>
        AfterRenderingSkybox = 400,

        /// <summary>
        /// Executes a <c>ScriptableRenderPass</c> before rendering transparent objects.
        /// </summary>
        BeforeRenderingTransparents = 450,

        /// <summary>
        /// Executes a <c>ScriptableRenderPass</c> after rendering transparent objects.
        /// </summary>
        AfterRenderingTransparents = 500,

        /// <summary>
        /// Executes a <c>ScriptableRenderPass</c> before rendering post-processing effects.
        /// </summary>
        BeforeRenderingPostProcessing = 550,

        /// <summary>
        /// Executes a <c>ScriptableRenderPass</c> after rendering post-processing effects but before final blit, post-processing AA effects and color grading.
        /// </summary>
        AfterRenderingPostProcessing = 600,

        /// <summary>
        /// Executes a <c>ScriptableRenderPass</c> after rendering all effects.
        /// </summary>
        AfterRendering = 1000,
    }

    /// <summary>
    /// <c>ScriptableRenderPass</c> implements a logical rendering pass that can be used to extend Universal RP renderer.
    /// </summary>
    public abstract partial class ScriptableRenderPass
    {
        /// <summary>
        /// RTHandle alias for BuiltinRenderTextureType.CameraTarget which is the backbuffer.
        /// </summary>
        static public RTHandle k_CameraTarget = RTHandles.Alloc(BuiltinRenderTextureType.CameraTarget);

        public RenderPassEvent renderPassEvent { get; set; }

        [Obsolete("Use colorAttachmentHandles")]
        public RenderTargetIdentifier[] colorAttachments
        {
            get => m_ColorAttachmentIds;
        }

        [Obsolete("Use colorAttachmentHandle")]
        public RenderTargetIdentifier colorAttachment
        {
            get => m_ColorAttachmentIds[0];
        }

        [Obsolete("Use depthAttachmentHandle")]
        public RenderTargetIdentifier depthAttachment
        {
            get => m_UsesRTHandles ? new RenderTargetIdentifier(m_DepthAttachment.nameID, 0, CubemapFace.Unknown, -1) : m_DepthAttachmentId;
        }

        /// <summary>
        /// List for the g-buffer attachment handles.
        /// </summary>
        public RTHandle[] colorAttachmentHandles
        {
            get => m_ColorAttachments;
        }

        /// <summary>
        /// The main color attachment handle.
        /// </summary>
        public RTHandle colorAttachmentHandle
        {
            get => m_ColorAttachments[0];
        }

        /// <summary>
        /// The depth attachment handle.
        /// </summary>
        public RTHandle depthAttachmentHandle
        {
            get => m_DepthAttachment;
        }

        public RenderBufferStoreAction[] colorStoreActions
        {
            get => m_ColorStoreActions;
        }

        public RenderBufferStoreAction depthStoreAction
        {
            get => m_DepthStoreAction;
        }

        internal bool[] overriddenColorStoreActions
        {
            get => m_OverriddenColorStoreActions;
        }

        internal bool overriddenDepthStoreAction
        {
            get => m_OverriddenDepthStoreAction;
        }

        /// <summary>
        /// The input requirements for the <c>ScriptableRenderPass</c>, which has been set using <c>ConfigureInput</c>
        /// </summary>
        /// <seealso cref="ConfigureInput"/>
        public ScriptableRenderPassInput input
        {
            get => m_Input;
        }

        public ClearFlag clearFlag
        {
            get => m_ClearFlag;
        }

        public Color clearColor
        {
            get => m_ClearColor;
        }

        RenderBufferStoreAction[] m_ColorStoreActions = new RenderBufferStoreAction[] { RenderBufferStoreAction.Store };
        RenderBufferStoreAction m_DepthStoreAction = RenderBufferStoreAction.Store;

        // by default all store actions are Store. The overridden flags are used to keep track of explicitly requested store actions, to
        // help figuring out the correct final store action for merged render passes when using the RenderPass API.
        private bool[] m_OverriddenColorStoreActions = new bool[] { false };
        private bool m_OverriddenDepthStoreAction = false;

        /// <summary>
        /// A ProfilingSampler for the entire render pass. Used as a profiling name by <c>ScriptableRenderer</c> when executing the pass.
        /// Default is <c>Unnamed_ScriptableRenderPass</c>.
        /// Set <c>base.profilingSampler</c> from the sub-class constructor to set a profiling name for a custom <c>ScriptableRenderPass</c>.
        /// </summary>
        protected internal ProfilingSampler profilingSampler { get; set; }
        internal bool overrideCameraTarget { get; set; }
        internal bool isBlitRenderPass { get; set; }

        internal bool useNativeRenderPass { get; set; }

        internal int renderTargetWidth { get; set; }
        internal int renderTargetHeight { get; set; }
        internal int renderTargetSampleCount { get; set; }

        internal bool depthOnly { get; set; }
        // this flag is updated each frame to keep track of which pass is the last for the current camera
        internal bool isLastPass { get; set; }
        // index to track the position in the current frame
        internal int renderPassQueueIndex { get; set; }

        internal NativeArray<int> m_ColorAttachmentIndices;
        internal NativeArray<int> m_InputAttachmentIndices;

        internal GraphicsFormat[] renderTargetFormat { get; set; }

        internal bool m_UsesRTHandles;
        RTHandle[] m_ColorAttachments;
        RenderTargetIdentifier[] m_ColorAttachmentIds;
        internal RTHandle[] m_InputAttachments = new RTHandle[8];
        internal bool[] m_InputAttachmentIsTransient = new bool[8];
        RTHandle m_DepthAttachment;
        RenderTargetIdentifier m_DepthAttachmentId;

        ScriptableRenderPassInput m_Input = ScriptableRenderPassInput.None;
        ClearFlag m_ClearFlag = ClearFlag.None;
        Color m_ClearColor = Color.black;

        internal DebugHandler GetActiveDebugHandler(RenderingData renderingData)
        {
            var debugHandler = renderingData.cameraData.renderer.DebugHandler;
            if ((debugHandler != null) && debugHandler.IsActiveForCamera(ref renderingData.cameraData))
                return debugHandler;
            return null;
        }

        public ScriptableRenderPass()
        {
            m_UsesRTHandles = true;
            renderPassEvent = RenderPassEvent.AfterRenderingOpaques;
            m_ColorAttachments = new RTHandle[] { k_CameraTarget, null, null, null, null, null, null, null };
            m_InputAttachments = new RTHandle[] { null, null, null, null, null, null, null, null };
            m_InputAttachmentIsTransient = new bool[] { false, false, false, false, false, false, false, false };
            m_DepthAttachment = k_CameraTarget;
            m_ColorStoreActions = new RenderBufferStoreAction[] { RenderBufferStoreAction.Store, 0, 0, 0, 0, 0, 0, 0 };
            m_DepthStoreAction = RenderBufferStoreAction.Store;
            m_OverriddenColorStoreActions = new bool[] { false, false, false, false, false, false, false, false };
            m_OverriddenDepthStoreAction = false;
            m_DepthAttachment = k_CameraTarget;
            m_DepthAttachmentId = m_DepthAttachment.nameID;
            m_ColorAttachmentIds = new RenderTargetIdentifier[] { k_CameraTarget.nameID, 0, 0, 0, 0, 0, 0, 0 };
            m_ClearFlag = ClearFlag.None;
            m_ClearColor = Color.black;
            overrideCameraTarget = false;
            isBlitRenderPass = false;
            profilingSampler = new ProfilingSampler($"Unnamed_{nameof(ScriptableRenderPass)}");
            useNativeRenderPass = true;
            renderTargetWidth = -1;
            renderTargetHeight = -1;
            renderTargetSampleCount = -1;
            renderPassQueueIndex = -1;
            renderTargetFormat = new GraphicsFormat[]
            {
                GraphicsFormat.None, GraphicsFormat.None, GraphicsFormat.None,
                GraphicsFormat.None, GraphicsFormat.None, GraphicsFormat.None, GraphicsFormat.None, GraphicsFormat.None
            };
            depthOnly = false;
        }

        /// <summary>
        /// Configures Input Requirements for this render pass.
        /// This method should be called inside <c>ScriptableRendererFeature.AddRenderPasses</c>.
        /// </summary>
        /// <param name="passInput">ScriptableRenderPassInput containing information about what requirements the pass needs.</param>
        /// <seealso cref="ScriptableRendererFeature.AddRenderPasses"/>
        public void ConfigureInput(ScriptableRenderPassInput passInput)
        {
            m_Input = passInput;
        }

        /// <summary>
        /// Configures the Store Action for a color attachment of this render pass.
        /// </summary>
        /// <param name="storeAction">RenderBufferStoreAction to use</param>
        /// <param name="attachmentIndex">Index of the color attachment</param>
        public void ConfigureColorStoreAction(RenderBufferStoreAction storeAction, uint attachmentIndex = 0)
        {
            m_ColorStoreActions[attachmentIndex] = storeAction;
            m_OverriddenColorStoreActions[attachmentIndex] = true;
        }

        /// <summary>
        /// Configures the Store Actions for all the color attachments of this render pass.
        /// </summary>
        /// <param name="storeActions">Array of RenderBufferStoreActions to use</param>
        public void ConfigureColorStoreActions(RenderBufferStoreAction[] storeActions)
        {
            int count = Math.Min(storeActions.Length, m_ColorStoreActions.Length);
            for (uint i = 0; i < count; ++i)
            {
                m_ColorStoreActions[i] = storeActions[i];
                m_OverriddenColorStoreActions[i] = true;
            }
        }

        /// <summary>
        /// Configures the Store Action for the depth attachment of this render pass.
        /// </summary>
        /// <param name="storeAction">RenderBufferStoreAction to use</param>
        public void ConfigureDepthStoreAction(RenderBufferStoreAction storeAction)
        {
            m_DepthStoreAction = storeAction;
            m_OverriddenDepthStoreAction = true;
        }

        internal void ConfigureInputAttachments(RTHandle input, bool isTransient = false)
        {
            m_InputAttachments[0] = input;
            m_InputAttachmentIsTransient[0] = isTransient;
        }

        [Obsolete("Use RTHandle for input")]
        internal void ConfigureInputAttachments(RenderTargetIdentifier input)
        {
            m_InputAttachments[0] = RTHandles.Alloc(input);
        }

        internal void ConfigureInputAttachments(RTHandle[] inputs)
        {
            m_InputAttachments = inputs;
        }

        internal void ConfigureInputAttachments(RTHandle[] inputs, bool[] isTransient)
        {
            ConfigureInputAttachments(inputs);
            m_InputAttachmentIsTransient = isTransient;
        }

        internal void SetInputAttachmentTransient(int idx, bool isTransient)
        {
            m_InputAttachmentIsTransient[idx] = isTransient;
        }

        internal bool IsInputAttachmentTransient(int idx)
        {
            return m_InputAttachmentIsTransient[idx];
        }

        [Obsolete("Use RTHandles for inputs")]
        internal void ConfigureInputAttachments(RenderTargetIdentifier[] inputs)
        {
            for (int i = 0; i < inputs.Length; ++i)
            {
                if (m_InputAttachments[i] == null || m_InputAttachments[i].nameID != inputs[i])
                {
                    m_InputAttachments[i]?.Release();
                    m_InputAttachments[i] = RTHandles.Alloc(inputs[i]);
                }
            }

            for (int i = inputs.Length; i < m_InputAttachments.Length; ++i)
            {
                m_InputAttachments[i]?.Release();
                m_InputAttachments[i] = null;
            }
        }

        /// <summary>
        /// Configures render targets for this render pass. Call this instead of CommandBuffer.SetRenderTarget.
        /// This method should be called inside Configure.
        /// </summary>
        /// <param name="colorAttachment">Color attachment identifier.</param>
        /// <param name="depthAttachment">Depth attachment identifier.</param>
        /// <seealso cref="Configure"/>
        [Obsolete("Use RTHandles for colorAttachment and depthAttachment")]
        public void ConfigureTarget(RenderTargetIdentifier colorAttachment, RenderTargetIdentifier depthAttachment)
        {
            m_DepthAttachmentId = depthAttachment;
            m_DepthAttachment = null;
            ConfigureTarget(colorAttachment);
        }

        /// <summary>
        /// Configures render targets for this render pass. Call this instead of CommandBuffer.SetRenderTarget.
        /// This method should be called inside Configure.
        /// </summary>
        /// <param name="colorAttachment">Color attachment handle.</param>
        /// <param name="depthAttachment">Depth attachment handle.</param>
        /// <seealso cref="Configure"/>
        public void ConfigureTarget(RTHandle colorAttachment, RTHandle depthAttachment)
        {
            m_DepthAttachment = depthAttachment;
            m_DepthAttachmentId = m_DepthAttachment.nameID;
            ConfigureTarget(colorAttachment);
        }

        [Obsolete("Use RTHandles for colorAttachment and depthAttachment")]
        internal void ConfigureTarget(RenderTargetIdentifier colorAttachment, RenderTargetIdentifier depthAttachment, GraphicsFormat format)
        {
            m_DepthAttachment = null;
            m_DepthAttachmentId = depthAttachment;
            ConfigureTarget(colorAttachment, format);
        }

        internal void ConfigureTarget(RTHandle colorAttachment, RTHandle depthAttachment, GraphicsFormat format)
        {
            m_DepthAttachment = depthAttachment;
            m_DepthAttachmentId = m_DepthAttachment.nameID;
            ConfigureTarget(colorAttachment, format);
        }

        /// <summary>
        /// Configures render targets for this render pass. Call this instead of CommandBuffer.SetRenderTarget.
        /// This method should be called inside Configure.
        /// </summary>
        /// <param name="colorAttachment">Color attachment identifier.</param>
        /// <param name="depthAttachment">Depth attachment identifier.</param>
        /// <seealso cref="Configure"/>
        [Obsolete("Use RTHandles for colorAttachments and depthAttachment")]
        public void ConfigureTarget(RenderTargetIdentifier[] colorAttachments, RenderTargetIdentifier depthAttachment)
        {
            m_UsesRTHandles = false;
            overrideCameraTarget = true;

            uint nonNullColorBuffers = RenderingUtils.GetValidColorBufferCount(colorAttachments);
            if (nonNullColorBuffers > SystemInfo.supportedRenderTargetCount)
                Debug.LogError("Trying to set " + nonNullColorBuffers + " renderTargets, which is more than the maximum supported:" + SystemInfo.supportedRenderTargetCount);

            m_ColorAttachmentIds = colorAttachments;
            m_DepthAttachmentId = depthAttachment;
        }

        /// <summary>
        /// Configures render targets for this render pass. Call this instead of CommandBuffer.SetRenderTarget.
        /// This method should be called inside Configure.
        /// </summary>
        /// <param name="colorAttachment">Color attachment handle.</param>
        /// <param name="depthAttachment">Depth attachment handle.</param>
        /// <seealso cref="Configure"/>
        public void ConfigureTarget(RTHandle[] colorAttachments, RTHandle depthAttachment)
        {
            m_UsesRTHandles = true;
            overrideCameraTarget = true;

            uint nonNullColorBuffers = RenderingUtils.GetValidColorBufferCount(colorAttachments);
            if (nonNullColorBuffers > SystemInfo.supportedRenderTargetCount)
                Debug.LogError("Trying to set " + nonNullColorBuffers + " renderTargets, which is more than the maximum supported:" + SystemInfo.supportedRenderTargetCount);

            m_ColorAttachments = colorAttachments;
            if (m_ColorAttachmentIds.Length != m_ColorAttachments.Length)
                m_ColorAttachmentIds = new RenderTargetIdentifier[m_ColorAttachments.Length];
            for (var i = 0; i < m_ColorAttachmentIds.Length; ++i)
                m_ColorAttachmentIds[i] = new RenderTargetIdentifier(colorAttachments[i].nameID, 0, CubemapFace.Unknown, -1);
            m_DepthAttachmentId = depthAttachment.nameID;
            m_DepthAttachment = depthAttachment;
        }

        [Obsolete("Use RTHandles for colorAttachments and depthAttachment")]
        internal void ConfigureTarget(RenderTargetIdentifier[] colorAttachments, RenderTargetIdentifier depthAttachment, GraphicsFormat[] formats)
        {
            ConfigureTarget(colorAttachments, depthAttachment);
            for (int i = 0; i < formats.Length; ++i)
                renderTargetFormat[i] = formats[i];
        }

        internal void ConfigureTarget(RTHandle[] colorAttachments, RTHandle depthAttachment, GraphicsFormat[] formats)
        {
            ConfigureTarget(colorAttachments, depthAttachment);
            for (int i = 0; i < formats.Length; ++i)
                renderTargetFormat[i] = formats[i];
        }

        /// <summary>
        /// Configures render targets for this render pass. Call this instead of CommandBuffer.SetRenderTarget.
        /// This method should be called inside Configure.
        /// </summary>
        /// <param name="colorAttachment">Color attachment identifier.</param>
        /// <seealso cref="Configure"/>
        [Obsolete("Use RTHandle for colorAttachment")]
        public void ConfigureTarget(RenderTargetIdentifier colorAttachment)
        {
            m_UsesRTHandles = false;
            overrideCameraTarget = true;

            m_ColorAttachmentIds[0] = colorAttachment;
            for (int i = 1; i < m_ColorAttachmentIds.Length; ++i)
                m_ColorAttachmentIds[i] = 0;
        }

        /// <summary>
        /// Configures render targets for this render pass. Call this instead of CommandBuffer.SetRenderTarget.
        /// This method should be called inside Configure.
        /// </summary>
        /// <param name="colorAttachment">Color attachment handle.</param>
        /// <seealso cref="Configure"/>
        public void ConfigureTarget(RTHandle colorAttachment)
        {
            m_UsesRTHandles = true;
            overrideCameraTarget = true;

            m_ColorAttachments[0] = colorAttachment;
            m_ColorAttachmentIds[0] = new RenderTargetIdentifier(colorAttachment.nameID, 0, CubemapFace.Unknown, -1);
            for (int i = 1; i < m_ColorAttachments.Length; ++i)
            {
                m_ColorAttachments[i] = null;
                m_ColorAttachmentIds[i] = 0;
            }
        }

        [Obsolete("Use RTHandle for colorAttachment")]
        internal void ConfigureTarget(RenderTargetIdentifier colorAttachment, GraphicsFormat format, int width = -1, int height = -1, int sampleCount = -1, bool depth = false)
        {
            ConfigureTarget(colorAttachment);
            for (int i = 1; i < m_ColorAttachments.Length; ++i)
                renderTargetFormat[i] = GraphicsFormat.None;

            if (depth == true && !GraphicsFormatUtility.IsDepthFormat(format))
                throw new ArgumentException("When configuring a depth only target the passed in format must be a depth format.");

            renderTargetWidth = width;
            renderTargetHeight = height;
            renderTargetSampleCount = sampleCount;
            depthOnly = depth;
            renderTargetFormat[0] = format;
        }

        internal void ConfigureTarget(RTHandle colorAttachment, GraphicsFormat format, int width = -1, int height = -1, int sampleCount = -1, bool depth = false)
        {
            ConfigureTarget(colorAttachment);
            for (int i = 1; i < m_ColorAttachments.Length; ++i)
                renderTargetFormat[i] = GraphicsFormat.None;

            if (depth == true && !GraphicsFormatUtility.IsDepthFormat(format))
                throw new ArgumentException("When configuring a depth only target the passed in format must be a depth format.");

            renderTargetWidth = width;
            renderTargetHeight = height;
            renderTargetSampleCount = sampleCount;
            depthOnly = depth;
            renderTargetFormat[0] = format;
        }
        internal void ConfigureTarget(RTHandle colorAttachment, RTHandle depthAttachment, GraphicsFormat format, int width = -1, int height = -1, int sampleCount = -1, bool depth = false)
        {
            ConfigureTarget(colorAttachment, format, width, height, sampleCount, depth);
            m_DepthAttachment = depthAttachment;

        }
        /// <summary>
        /// Configures render targets for this render pass. Call this instead of CommandBuffer.SetRenderTarget.
        /// This method should be called inside Configure.
        /// </summary>
        /// <param name="colorAttachments">Color attachment identifiers.</param>
        /// <seealso cref="Configure"/>
        [Obsolete("Use RTHandles for colorAttachments")]
        public void ConfigureTarget(RenderTargetIdentifier[] colorAttachments)
        {
            ConfigureTarget(colorAttachments, k_CameraTarget.nameID);
        }

        /// <summary>
        /// Configures render targets for this render pass. Call this instead of CommandBuffer.SetRenderTarget.
        /// This method should be called inside Configure.
        /// </summary>
        /// <param name="colorAttachment">Color attachment handle.</param>
        /// <seealso cref="Configure"/>
        public void ConfigureTarget(RTHandle[] colorAttachments)
        {
            ConfigureTarget(colorAttachments, k_CameraTarget);
        }

        /// <summary>
        /// Configures clearing for the render targets for this render pass. Call this inside Configure.
        /// </summary>
        /// <param name="clearFlag">ClearFlag containing information about what targets to clear.</param>
        /// <param name="clearColor">Clear color.</param>
        /// <seealso cref="Configure"/>
        public void ConfigureClear(ClearFlag clearFlag, Color clearColor)
        {
            m_ClearFlag = clearFlag;
            m_ClearColor = clearColor;
        }

        /// <summary>
        /// This method is called by the renderer before rendering a camera
        /// Override this method if you need to to configure render targets and their clear state, and to create temporary render target textures.
        /// If a render pass doesn't override this method, this render pass renders to the active Camera's render target.
        /// You should never call CommandBuffer.SetRenderTarget. Instead call <c>ConfigureTarget</c> and <c>ConfigureClear</c>.
        /// </summary>
        /// <param name="cmd">CommandBuffer to enqueue rendering commands. This will be executed by the pipeline.</param>
        /// <param name="renderingData">Current rendering state information</param>
        /// <seealso cref="ConfigureTarget"/>
        /// <seealso cref="ConfigureClear"/>
        public virtual void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        { }

        /// <summary>
        /// This method is called by the renderer before executing the render pass.
        /// Override this method if you need to to configure render targets and their clear state, and to create temporary render target textures.
        /// If a render pass doesn't override this method, this render pass renders to the active Camera's render target.
        /// You should never call CommandBuffer.SetRenderTarget. Instead call <c>ConfigureTarget</c> and <c>ConfigureClear</c>.
        /// </summary>
        /// <param name="cmd">CommandBuffer to enqueue rendering commands. This will be executed by the pipeline.</param>
        /// <param name="cameraTextureDescriptor">Render texture descriptor of the camera render target.</param>
        /// <seealso cref="ConfigureTarget"/>
        /// <seealso cref="ConfigureClear"/>
        public virtual void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
        { }


        /// <summary>
        /// Called upon finish rendering a camera. You can use this callback to release any resources created
        /// by this render
        /// pass that need to be cleanup once camera has finished rendering.
        /// This method be called for all cameras in a camera stack.
        /// </summary>
        /// <param name="cmd">Use this CommandBuffer to cleanup any generated data</param>
        public virtual void OnCameraCleanup(CommandBuffer cmd)
        {
        }

        /// <summary>
        /// Called upon finish rendering a camera stack. You can use this callback to release any resources created
        /// by this render pass that need to be cleanup once all cameras in the stack have finished rendering.
        /// This method will be called once after rendering the last camera in the camera stack.
        /// Cameras that don't have an explicit camera stack are also considered stacked rendering.
        /// In that case the Base camera is the first and last camera in the stack.
        /// </summary>
        /// <param name="cmd">Use this CommandBuffer to cleanup any generated data</param>
        public virtual void OnFinishCameraStackRendering(CommandBuffer cmd)
        { }

        /// <summary>
        /// Execute the pass. This is where custom rendering occurs. Specific details are left to the implementation
        /// </summary>
        /// <param name="context">Use this render context to issue any draw commands during execution</param>
        /// <param name="renderingData">Current rendering state information</param>
        public abstract void Execute(ScriptableRenderContext context, ref RenderingData renderingData);

        /// <summary>
        /// Add a blit command to the context for execution. This changes the active render target in the ScriptableRenderer to
        /// destination.
        /// </summary>
        /// <param name="cmd">Command buffer to record command for execution.</param>
        /// <param name="source">Source texture or target identifier to blit from.</param>
        /// <param name="destination">Destination texture or target identifier to blit into. This becomes the renderer active render target.</param>
        /// <param name="material">Material to use.</param>
        /// <param name="passIndex">Shader pass to use. Default is 0.</param>
        /// <seealso cref="ScriptableRenderer"/>
        [Obsolete("Use RTHandles for source and destination")]
        public void Blit(CommandBuffer cmd, RenderTargetIdentifier source, RenderTargetIdentifier destination, Material material = null, int passIndex = 0)
        {
            ScriptableRenderer.SetRenderTarget(cmd, destination, BuiltinRenderTextureType.CameraTarget, clearFlag, clearColor);
            cmd.Blit(source, destination, material, passIndex);
        }

        /// <summary>
        /// Add a blit command to the context for execution. This changes the active render target in the ScriptableRenderer to
        /// destination.
        /// </summary>
        /// <param name="cmd">Command buffer to record command for execution.</param>
        /// <param name="source">Source texture or target handle to blit from.</param>
        /// <param name="destination">Destination texture or target handle to blit into. This becomes the renderer active render target.</param>
        /// <param name="material">Material to use.</param>
        /// <param name="passIndex">Shader pass to use. Default is 0.</param>
        /// <seealso cref="ScriptableRenderer"/>
        public void Blit(CommandBuffer cmd, RTHandle source, RTHandle destination, Material material = null, int passIndex = 0)
        {
            ScriptableRenderer.SetRenderTarget(cmd, destination, k_CameraTarget, clearFlag, clearColor);
            cmd.Blit(source.nameID, destination.nameID, material, passIndex);
        }

        /// <summary>
        /// Add a blit command to the context for execution. This applies the material to the color target.
        /// </summary>
        /// <param name="cmd">Command buffer to record command for execution.</param>
        /// <param name="data">RenderingData to access the active renderer.</param>
        /// <param name="material">Material to use.</param>
        /// <param name="passIndex">Shader pass to use. Default is 0.</param>
        public void Blit(CommandBuffer cmd, ref RenderingData data, Material material, int passIndex = 0)
        {
            var renderer = data.cameraData.renderer;

            Blit(cmd, renderer.cameraColorTargetHandle, renderer.GetCameraColorFrontBuffer(cmd), material, passIndex);
            renderer.SwapColorBuffer(cmd);
        }

        /// <summary>
        /// Creates <c>DrawingSettings</c> based on current the rendering state.
        /// </summary>
        /// <param name="shaderTagId">Shader pass tag to render.</param>
        /// <param name="renderingData">Current rendering state.</param>
        /// <param name="sortingCriteria">Criteria to sort objects being rendered.</param>
        /// <returns></returns>
        /// <seealso cref="DrawingSettings"/>
        public DrawingSettings CreateDrawingSettings(ShaderTagId shaderTagId, ref RenderingData renderingData, SortingCriteria sortingCriteria)
        {
            Camera camera = renderingData.cameraData.camera;
            SortingSettings sortingSettings = new SortingSettings(camera) { criteria = sortingCriteria };
            DrawingSettings settings = new DrawingSettings(shaderTagId, sortingSettings)
            {
                perObjectData = renderingData.perObjectData,
                mainLightIndex = renderingData.lightData.mainLightIndex,
                enableDynamicBatching = renderingData.supportsDynamicBatching,

                // Disable instancing for preview cameras. This is consistent with the built-in forward renderer. Also fixes case 1127324.
                enableInstancing = camera.cameraType == CameraType.Preview ? false : true,
            };
            return settings;
        }

        /// <summary>
        /// Creates <c>DrawingSettings</c> based on current rendering state.
        /// </summary>
        /// /// <param name="shaderTagIdList">List of shader pass tag to render.</param>
        /// <param name="renderingData">Current rendering state.</param>
        /// <param name="sortingCriteria">Criteria to sort objects being rendered.</param>
        /// <returns></returns>
        /// <seealso cref="DrawingSettings"/>
        public DrawingSettings CreateDrawingSettings(List<ShaderTagId> shaderTagIdList,
            ref RenderingData renderingData, SortingCriteria sortingCriteria)
        {
            if (shaderTagIdList == null || shaderTagIdList.Count == 0)
            {
                Debug.LogWarning("ShaderTagId list is invalid. DrawingSettings is created with default pipeline ShaderTagId");
                return CreateDrawingSettings(new ShaderTagId("UniversalPipeline"), ref renderingData, sortingCriteria);
            }

            DrawingSettings settings = CreateDrawingSettings(shaderTagIdList[0], ref renderingData, sortingCriteria);
            for (int i = 1; i < shaderTagIdList.Count; ++i)
                settings.SetShaderPassName(i, shaderTagIdList[i]);
            return settings;
        }

        public static bool operator <(ScriptableRenderPass lhs, ScriptableRenderPass rhs)
        {
            return lhs.renderPassEvent < rhs.renderPassEvent;
        }

        public static bool operator >(ScriptableRenderPass lhs, ScriptableRenderPass rhs)
        {
            return lhs.renderPassEvent > rhs.renderPassEvent;
        }
    }
}
