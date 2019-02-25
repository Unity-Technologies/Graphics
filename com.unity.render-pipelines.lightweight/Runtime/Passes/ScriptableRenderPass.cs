using System;

namespace UnityEngine.Rendering.LWRP
{
    // Note: Spaced built-in events so we can add events in between them
    // We need to leave room as we sort render passes based on event.
    // Users can also inject render pass events in a specific point by doing RenderPassEvent + offset
    /// <summary>
    /// Controls when the render pass should execute.
    /// </summary>
    public enum RenderPassEvent
    {
        BeforeRendering = 0,
        BeforeRenderingShadows = 50,
        AfterRenderingShadows = 100,
        BeforeRenderingPrepasses = 150,
        AfterRenderingPrePasses = 200,
        BeforeRenderingOpaques = 250,
        AfterRenderingOpaques = 300,
        BeforeRenderingSkybox = 350,
        AfterRenderingSkybox = 400,
        BeforeRenderingTransparents = 450,
        AfterRenderingTransparents = 500,
        AfterRendering = 1000,
    }

    /// <summary>
    /// Inherit from this class to perform custom rendering in the Lightweight Render Pipeline.
    /// </summary>
    public abstract class ScriptableRenderPass : IComparable<ScriptableRenderPass>
    {
        public RenderPassEvent renderPassEvent { get; set; }

        public RenderTargetIdentifier colorAttachment
        {
            get => m_ColorAttachment;
        }

        public RenderTargetIdentifier depthAttachment
        {
            get => m_DepthAttachment;
        }

        public ClearFlag clearFlag
        {
            get => m_ClearFlag;
        }

        public Color clearColor
        {
            get => m_ClearColor;
        }

        internal bool overrideCameraTarget { get; set; }
        internal bool isBlitRenderPass { get; set; }

        RenderTargetIdentifier m_ColorAttachment = BuiltinRenderTextureType.CameraTarget;
        RenderTargetIdentifier m_DepthAttachment = BuiltinRenderTextureType.CameraTarget;
        ClearFlag m_ClearFlag = ClearFlag.None;
        Color m_ClearColor = Color.black;

        public ScriptableRenderPass()
        {
            renderPassEvent = RenderPassEvent.AfterRenderingOpaques;
            m_ColorAttachment = BuiltinRenderTextureType.CameraTarget;
            m_DepthAttachment = BuiltinRenderTextureType.CameraTarget;
            m_ClearFlag = ClearFlag.None;
            m_ClearColor = Color.black;
            overrideCameraTarget = false;
            isBlitRenderPass = false;
        }

        public void ConfigureTarget(RenderTargetIdentifier colorAttachment, RenderTargetIdentifier depthAttachment)
        {
            overrideCameraTarget = true;
            m_ColorAttachment = colorAttachment;
            m_DepthAttachment = depthAttachment;
        }

        public void ConfigureTarget(RenderTargetIdentifier colorAttachment)
        {
            overrideCameraTarget = true;
            m_ColorAttachment = colorAttachment;
            m_DepthAttachment = BuiltinRenderTextureType.CameraTarget;
        }

        public void ConfigureTargetForBlit(RenderTargetIdentifier destination)
        {
            isBlitRenderPass = true;
            m_ColorAttachment = destination;
        }

        public void ConfigureClear(ClearFlag clearFlag, Color clearColor)
        {
            m_ClearFlag = clearFlag;
            m_ClearColor = clearColor;
        }

        public virtual void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
        {}

        /// <summary>
        /// Cleanup any allocated data that was created during the execution of the pass.
        /// </summary>
        /// <param name="cmd">Use this CommandBuffer to cleanup any generated data</param>
        public virtual void FrameCleanup(CommandBuffer cmd)
        {}

        /// <summary>
        /// Execute the pass. This is where custom rendering occurs. Specific details are left to the implementation
        /// </summary>
        /// <param name="renderer">The currently executing renderer. Contains configuration for the current execute call.</param>
        /// <param name="context">Use this render context to issue any draw commands during execution</param>
        /// <param name="renderingData">Current rendering state information</param>
        public abstract void Execute(ScriptableRenderContext context, ref RenderingData renderingData);

        public int CompareTo(ScriptableRenderPass other)
        {
            return (int)renderPassEvent - (int)other.renderPassEvent;
        }

        // TODO: Remove this. Currently only used by FinalBlit pass.
        internal void SetRenderTarget(
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
    }
}
