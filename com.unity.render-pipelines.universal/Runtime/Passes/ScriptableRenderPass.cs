using System;
using System.Collections.Generic;
using UnityEngine.Scripting.APIUpdating;

namespace UnityEngine.Rendering.Universal
{
    // Note: Spaced built-in events so we can add events in between them
    // We need to leave room as we sort render passes based on event.
    // Users can also inject render pass events in a specific point by doing RenderPassEvent + offset
    /// <summary>
    /// Controls when the render pass executes.
    /// </summary>
    [MovedFrom("UnityEngine.Rendering.LWRP")] public enum RenderPassEvent
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
        BeforeRenderingPostProcessing = 550,
        AfterRenderingPostProcessing = 600,
        AfterRendering = 1000,
    }

    /// <summary>
    /// <c>ScriptableRenderPass</c> implements a logical rendering pass that can be used to extend Universal RP renderer.
    /// </summary>
    [MovedFrom("UnityEngine.Rendering.LWRP")] public abstract class ScriptableRenderPass
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

        /// <summary>
        /// Configures render targets for this render pass. Call this instead of CommandBuffer.SetRenderTarget.
        /// This method should be called inside Configure.
        /// </summary>
        /// <param name="colorAttachment">Color attachment identifier.</param>
        /// <param name="depthAttachment">Depth attachment identifier.</param>
        /// <seealso cref="Configure"/>
        public void ConfigureTarget(RenderTargetIdentifier colorAttachment, RenderTargetIdentifier depthAttachment)
        {
            overrideCameraTarget = true;
            m_ColorAttachment = colorAttachment;
            m_DepthAttachment = depthAttachment;
        }

        /// <summary>
        /// Configures render targets for this render pass. Call this instead of CommandBuffer.SetRenderTarget.
        /// This method should be called inside Configure.
        /// </summary>
        /// <param name="colorAttachment">Color attachment identifier.</param>
        /// <seealso cref="Configure"/>
        public void ConfigureTarget(RenderTargetIdentifier colorAttachment)
        {
            overrideCameraTarget = true;
            m_ColorAttachment = colorAttachment;
            m_DepthAttachment = BuiltinRenderTextureType.CameraTarget;
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
        public void Blit(CommandBuffer cmd, RenderTargetIdentifier source, RenderTargetIdentifier destination, Material material = null, int passIndex = 0)
        {
            ScriptableRenderer.SetRenderTarget(cmd, destination, BuiltinRenderTextureType.CameraTarget, clearFlag, clearColor);
            cmd.Blit(source, destination, material, passIndex);
        }

        /// <summary>
        /// Adds a Render Post-processing command for execution. This changes the active render target in the ScriptableRenderer to destination.
        /// </summary>
        /// <param name="cmd">Command buffer to record command for execution.</param>
        /// <param name="cameraData">Camera rendering data.</param>
        /// <param name="sourceDescriptor">Render texture descriptor for source.</param>
        /// <param name="source">Source texture or render target identifier.</param>
        /// <param name="destination">Destination texture or render target identifier.</param>
        /// <param name="opaqueOnly">If true, only renders opaque post-processing effects. Otherwise, renders before and after stack post-processing effects.</param>
        /// <param name="flip">If true, flips image vertically.</param>
#if POST_PROCESSING_STACK_2_0_0_OR_NEWER
        [Obsolete("The use of the Post-processing Stack V2 is deprecated in the Universal Render Pipeline. Use the builtin post-processing effects instead.")]
        public void RenderPostProcessing(CommandBuffer cmd, ref CameraData cameraData, RenderTextureDescriptor sourceDescriptor, RenderTargetIdentifier source, RenderTargetIdentifier destination, bool opaqueOnly, bool flip)
        {
        }
#endif

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
