using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using Unity.Collections;
using UnityEngine.Scripting.APIUpdating;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering.RenderGraphModule;

namespace UnityEngine.Rendering.Universal
{
    /// <summary>
    /// Input requirements for <c>ScriptableRenderPass</c>.
    /// 
    /// URP adds render passes to generate the inputs, or reuses inputs that are already available from earlier in the frame.
    /// 
    /// URP binds the inputs as global shader texture properties.
    /// </summary>
    /// <seealso cref="ConfigureInput"/>
    [Flags]
    public enum ScriptableRenderPassInput
    {
        /// <summary>
        /// Used when a <c>ScriptableRenderPass</c> does not require any texture.
        /// </summary>
        None = 0,

        /// <summary>
        /// Used when a <c>ScriptableRenderPass</c> requires a depth texture.
        /// 
        /// To sample the depth texture in a shader, include `Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl`, then use the `SampleSceneDepth` method.
        /// </summary>
        Depth = 1 << 0,

        /// <summary>
        /// Used when a <c>ScriptableRenderPass</c> requires a normal texture.
        /// 
        /// To sample the normals texture in a shader, include `Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareNormalsTexture.hlsl`, then use the `SampleSceneNormals` method.
        /// </summary>
        Normal = 1 << 1,

        /// <summary>
        /// Used when a <c>ScriptableRenderPass</c> requires a color texture.
        /// 
        /// To sample the color texture in a shader, include `Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareOpaqueTexture.hlsl`, then use the `SampleSceneColor` method. 
        /// 
        /// **Note:** The opaque texture might be a downscaled copy of the framebuffer from before rendering transparent objects.
        /// </summary>
        Color = 1 << 2,

        /// <summary>
        /// Used when a <c>ScriptableRenderPass</c> requires a motion vectors texture.
        /// 
        /// To sample the motion vectors texture in a shader, use `TEXTURE2D_X(_MotionVectorTexture)`, then `LOAD_TEXTURE2D_X_LOD(_MotionVectorTexture, pixelCoords, 0).xy`.
        /// </summary>
        Motion = 1 << 3,
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
    /// Framebuffer fetch events in Universal RP
    /// </summary>
    internal enum FramebufferFetchEvent
    {
        None = 0,
        FetchGbufferInDeferred = 1
    }

    internal static class RenderPassEventsEnumValues
    {
        // we cache the values in this array at construction time to avoid runtime allocations, which we would cause if we accessed valuesInternal directly
        public static int[] values;

        static RenderPassEventsEnumValues()
        {
            System.Array valuesInternal = Enum.GetValues(typeof(RenderPassEvent));

            values = new int[valuesInternal.Length];

            int index = 0;
            foreach (int value in valuesInternal)
            {
                values[index] = value;
                index++;
            }
        }
    }

    /// <summary>
    /// <c>ScriptableRenderPass</c> implements a logical rendering pass that can be used to extend Universal RP renderer.
    /// </summary>
    /// <remarks>
    /// To implement your own rendering pass you need to take the following steps:
    /// 1. Create a new Subclass from ScriptableRenderPass that implements the rendering logic.
    /// 2. Create an instance of your subclass and set up the relevant parameters such as <c>ScriptableRenderPass.renderPassEvent</c> in the constructor or initialization code.
    /// 3. Ensure your pass instance gets picked up by URP, this can be done through a <c>ScriptableRendererFeature</c> or by calling <c>ScriptableRenderer.EnqueuePass</c> from an event callback like <c>RenderPipelineManager.beginCameraRendering</c>
    ///
    /// See [link] for more info on working with a <c>ScriptableRendererFeature</c> or [link] for more info on working with <c>ScriptableRenderer.EnqueuePass</c>.
    /// </remarks>
    public abstract partial class ScriptableRenderPass: IRenderGraphRecorder
    {
        /// <summary>
        /// RTHandle alias for BuiltinRenderTextureType.CameraTarget which is the backbuffer.
        /// </summary>
        [Obsolete(DeprecationMessage.CompatibilityScriptingAPIObsolete, false)]
        public static RTHandle k_CameraTarget = RTHandles.Alloc(BuiltinRenderTextureType.CameraTarget);

        /// <summary>
        /// The event when the render pass executes.
        /// </summary>
        public RenderPassEvent renderPassEvent { get; set; }

        /// <summary>
        /// The render target identifiers for color attachments.
        /// This is obsolete, use colorAttachmentHandles instead.
        /// </summary>
        [Obsolete("Use colorAttachmentHandles", true)]
        public RenderTargetIdentifier[] colorAttachments => throw new NotSupportedException("colorAttachments has been deprecated. Use colorAttachmentHandles instead.");

        /// <summary>
        /// The render target identifier for color attachment.
        /// This is obsolete, use colorAttachmentHandle instead.
        /// </summary>
        [Obsolete("Use colorAttachmentHandle", true)]
        public RenderTargetIdentifier[] colorAttachment => throw new NotSupportedException("colorAttachment has been deprecated. Use colorAttachmentHandle instead.");

        /// <summary>
        /// The render target identifier for depth attachment.
        /// This is obsolete, use depthAttachmentHandle instead.
        /// </summary>
        [Obsolete("Use depthAttachmentHandle", true)]
        public RenderTargetIdentifier depthAttachment => throw new NotSupportedException("depthAttachment has been deprecated. Use depthAttachmentHandle instead.");

        /// <summary>
        /// List for the g-buffer attachment handles.
        /// </summary>
        public RTHandle[] colorAttachmentHandles => m_ColorAttachments;

        /// <summary>
        /// The main color attachment handle.
        /// </summary>
        public RTHandle colorAttachmentHandle => m_ColorAttachments[0];

        /// <summary>
        /// The depth attachment handle.
        /// </summary>
        public RTHandle depthAttachmentHandle => m_DepthAttachment;

        /// <summary>
        /// The store actions for Color.
        /// </summary>
        public RenderBufferStoreAction[] colorStoreActions => m_ColorStoreActions;

        /// <summary>
        /// The store actions for Depth.
        /// </summary>
        public RenderBufferStoreAction depthStoreAction => m_DepthStoreAction;

        internal bool[] overriddenColorStoreActions => m_OverriddenColorStoreActions;

        internal bool overriddenDepthStoreAction => m_OverriddenDepthStoreAction;

        /// <summary>
        /// The input requirements for the <c>ScriptableRenderPass</c>, which has been set using <c>ConfigureInput</c>
        /// </summary>
        /// <seealso cref="ConfigureInput"/>
        public ScriptableRenderPassInput input => m_Input;

        /// <summary>
        /// The flag to use when clearing.
        /// </summary>
        /// <seealso cref="ClearFlag"/>
        public ClearFlag clearFlag => m_ClearFlag;

        /// <summary>
        /// The color value to use when clearing.
        /// </summary>
        public Color clearColor => m_ClearColor;

        RenderBufferStoreAction[] m_ColorStoreActions = new RenderBufferStoreAction[] { RenderBufferStoreAction.Store };
        RenderBufferStoreAction m_DepthStoreAction = RenderBufferStoreAction.Store;

        /// <summary>
        /// Setting this property to true forces rendering of all passes in the URP frame via an intermediate texture. Use this option for passes that do not support rendering directly to the backbuffer or that require sampling the active color target. Using this option might have a significant performance impact on untethered VR platforms.
        /// </summary>
        public bool requiresIntermediateTexture { get; set; }

        // by default all store actions are Store. The overridden flags are used to keep track of explicitly requested store actions, to
        // help figuring out the correct final store action for merged render passes when using the RenderPass API.
        private bool[] m_OverriddenColorStoreActions = new bool[] { false };
        private bool m_OverriddenDepthStoreAction = false;

        private ProfilingSampler m_ProfingSampler;
        private string m_PassName;
        private RenderGraphSettings m_RenderGraphSettings;

        /// <summary>
        /// A ProfilingSampler for the entire render pass. Used as a profiling name by <c>ScriptableRenderer</c> when executing the pass.
        /// The default is named as the class type of the sub-class.
        /// Set <c>base.profilingSampler</c> from the sub-class constructor to set a different profiling name for a custom <c>ScriptableRenderPass
        /// This returns null in release build (non-development).</c>.
        /// </summary>
        protected internal ProfilingSampler profilingSampler
        {
            get
            {
                //We only need this in release (non-dev build) but putting it here to track it in more test automation.
                if (m_RenderGraphSettings == null)
                {
                    m_RenderGraphSettings = GraphicsSettings.GetRenderPipelineSettings<RenderGraphSettings>();
                }

#if (DEVELOPMENT_BUILD || UNITY_EDITOR)
                return m_ProfingSampler;
#else 
                //We only remove the sampler in release build when not in Compatibility Mode to avoid breaking user projects in the very unlikely scenario they would get the sampler.
                return m_RenderGraphSettings.enableRenderCompatibilityMode ? m_ProfingSampler : null;
#endif
            }
            set
            {
                m_ProfingSampler = value;
                m_PassName = (value != null) ? value.name : this.GetType().Name;                
            }
        }

        /// <summary>
        /// The name of the pass that will show up in profiler and other tools. This will be indentical to the 
        /// name of <c>profilingSampler</c>. <c>profilingSampler</c> is set to null in the release build (non-development)
        /// so this <c>passName</c> property is the safe way to access the name and use it consistently. This will always return a valid string.
        /// </summary>
        protected internal string passName{ get { return m_PassName; } }

        internal bool overrideCameraTarget { get; set; }
        internal bool isBlitRenderPass { get; set; }

        internal bool useNativeRenderPass { get; set; }

        // index to track the position in the current frame
        internal int renderPassQueueIndex { get; set; }

        internal NativeArray<int> m_ColorAttachmentIndices;
        internal NativeArray<int> m_InputAttachmentIndices;

        internal GraphicsFormat[] renderTargetFormat { get; set; }

        RTHandle[] m_ColorAttachments;
        internal RTHandle[] m_InputAttachments = new RTHandle[8];
        internal bool[] m_InputAttachmentIsTransient = new bool[8];
        RTHandle m_DepthAttachment;

        ScriptableRenderPassInput m_Input = ScriptableRenderPassInput.None;
        ClearFlag m_ClearFlag = ClearFlag.None;
        Color m_ClearColor = Color.black;

        static internal DebugHandler GetActiveDebugHandler(UniversalCameraData cameraData)
        {
            var debugHandler = cameraData.renderer.DebugHandler;
            if ((debugHandler != null) && debugHandler.IsActiveForCamera(cameraData.isPreviewCamera))
                return debugHandler;
            return null;
        }

        /// <summary>
        /// Creates a new <c>ScriptableRenderPass"</c> instance.
        /// </summary>
        public ScriptableRenderPass()            
        {
            renderPassEvent = RenderPassEvent.AfterRenderingOpaques;
            // Disable obsolete warning for internal usage
            #pragma warning disable CS0618
            m_ColorAttachments = new RTHandle[] { k_CameraTarget, null, null, null, null, null, null, null };
            m_DepthAttachment = k_CameraTarget;
            #pragma warning restore CS0618
            m_InputAttachments = new RTHandle[] { null, null, null, null, null, null, null, null };
            m_InputAttachmentIsTransient = new bool[] { false, false, false, false, false, false, false, false };
            m_ColorStoreActions = new RenderBufferStoreAction[] { RenderBufferStoreAction.Store, 0, 0, 0, 0, 0, 0, 0 };
            m_DepthStoreAction = RenderBufferStoreAction.Store;
            m_OverriddenColorStoreActions = new bool[] { false, false, false, false, false, false, false, false };
            m_OverriddenDepthStoreAction = false;
            m_ClearFlag = ClearFlag.None;
            m_ClearColor = Color.black;
            overrideCameraTarget = false;
            isBlitRenderPass = false;
            useNativeRenderPass = true;
            renderPassQueueIndex = -1;
            renderTargetFormat = new GraphicsFormat[]
            {
                GraphicsFormat.None, GraphicsFormat.None, GraphicsFormat.None,
                GraphicsFormat.None, GraphicsFormat.None, GraphicsFormat.None, GraphicsFormat.None, GraphicsFormat.None
            };

            profilingSampler = new ProfilingSampler(this.GetType().Name);
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
        [Obsolete(DeprecationMessage.CompatibilityScriptingAPIObsolete, false)]
        public void ConfigureColorStoreAction(RenderBufferStoreAction storeAction, uint attachmentIndex = 0)
        {
            m_ColorStoreActions[attachmentIndex] = storeAction;
            m_OverriddenColorStoreActions[attachmentIndex] = true;
        }

        /// <summary>
        /// Configures the Store Actions for all the color attachments of this render pass.
        /// </summary>
        /// <param name="storeActions">Array of RenderBufferStoreActions to use</param>
        [Obsolete(DeprecationMessage.CompatibilityScriptingAPIObsolete, false)]
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
        [Obsolete(DeprecationMessage.CompatibilityScriptingAPIObsolete, false)]
        public void ConfigureDepthStoreAction(RenderBufferStoreAction storeAction)
        {
            m_DepthStoreAction = storeAction;
            m_OverriddenDepthStoreAction = true;
        }

        [Obsolete(DeprecationMessage.CompatibilityScriptingAPIObsolete, false)]
        internal void ConfigureInputAttachments(RTHandle input, bool isTransient = false)
        {
            m_InputAttachments[0] = input;
            m_InputAttachmentIsTransient[0] = isTransient;
        }

        [Obsolete(DeprecationMessage.CompatibilityScriptingAPIObsolete, false)]
        internal void ConfigureInputAttachments(RTHandle[] inputs)
        {
            m_InputAttachments = inputs;
        }

        [Obsolete(DeprecationMessage.CompatibilityScriptingAPIObsolete, false)]
        internal void ConfigureInputAttachments(RTHandle[] inputs, bool[] isTransient)
        {
            // Disable obsolete warning for internal usage
            #pragma warning disable CS0618
            ConfigureInputAttachments(inputs);
            #pragma warning restore CS0618

            m_InputAttachmentIsTransient = isTransient;
        }

        [Obsolete(DeprecationMessage.CompatibilityScriptingAPIObsolete, false)]
        internal void SetInputAttachmentTransient(int idx, bool isTransient)
        {
            m_InputAttachmentIsTransient[idx] = isTransient;
        }

        [Obsolete(DeprecationMessage.CompatibilityScriptingAPIObsolete, false)]
        internal bool IsInputAttachmentTransient(int idx)
        {
            return m_InputAttachmentIsTransient[idx];
        }

        /// <summary>
        /// Resets render targets to default.
        /// This method effectively reset changes done by ConfigureTarget.
        /// </summary>
        /// <seealso cref="ConfigureTarget"/>
        [Obsolete(DeprecationMessage.CompatibilityScriptingAPIObsolete, false)]
        public void ResetTarget()
        {
            overrideCameraTarget = false;

            // Reset depth
            m_DepthAttachment = null;

            // Reset colors
            m_ColorAttachments[0] = null;
            for (int i = 1; i < m_ColorAttachments.Length; ++i)
            {
                m_ColorAttachments[i] = null;
            }
        }

        /// <summary>
        /// Configures render targets for this render pass. Call this instead of CommandBuffer.SetRenderTarget.
        /// This method should be called inside Configure.
        /// </summary>
        /// <param name="colorAttachment">Color attachment identifier.</param>
        /// <param name="depthAttachment">Depth attachment identifier.</param>
        /// <seealso cref="Configure"/>
        [Obsolete("Use RTHandles for colorAttachment and depthAttachment", true)]
        public void ConfigureTarget(RenderTargetIdentifier colorAttachment, RenderTargetIdentifier depthAttachment)
        {
            throw new NotSupportedException("ConfigureTarget with RenderTargetIdentifier has been deprecated. Use RTHandles instead");
        }

        /// <summary>
        /// Configures render targets for this render pass. Call this instead of CommandBuffer.SetRenderTarget.
        /// This method should be called inside Configure.
        /// </summary>
        /// <param name="colorAttachment">Color attachment handle.</param>
        /// <param name="depthAttachment">Depth attachment handle.</param>
        /// <seealso cref="Configure"/>
        [Obsolete(DeprecationMessage.CompatibilityScriptingAPIObsolete, false)]
        public void ConfigureTarget(RTHandle colorAttachment, RTHandle depthAttachment)
        {
            overrideCameraTarget = true;

            m_DepthAttachment = depthAttachment;
            m_ColorAttachments[0] = colorAttachment;
            for (int i = 1; i < m_ColorAttachments.Length; ++i)
            {
                m_ColorAttachments[i] = null;
            }
        }

        /// <summary>
        /// Configures render targets for this render pass. Call this instead of CommandBuffer.SetRenderTarget.
        /// This method should be called inside Configure.
        /// </summary>
        /// <param name="colorAttachments">Color attachment identifier.</param>
        /// <param name="depthAttachment">Depth attachment identifier.</param>
        /// <seealso cref="Configure"/>
        [Obsolete("Use RTHandles for colorAttachments and depthAttachment", true)]
        public void ConfigureTarget(RenderTargetIdentifier[] colorAttachments, RenderTargetIdentifier depthAttachment)
        {
            throw new NotSupportedException("ConfigureTarget with RenderTargetIdentifier has been deprecated. Use it with RTHandles instead");
        }

        /// <summary>
        /// Configures render targets for this render pass. Call this instead of CommandBuffer.SetRenderTarget.
        /// This method should be called inside Configure.
        /// </summary>
        /// <param name="colorAttachments">Color attachment handle.</param>
        /// <param name="depthAttachment">Depth attachment handle.</param>
        /// <seealso cref="Configure"/>
        [Obsolete(DeprecationMessage.CompatibilityScriptingAPIObsolete, false)]
        public void ConfigureTarget(RTHandle[] colorAttachments, RTHandle depthAttachment)
        {
            overrideCameraTarget = true;

            uint nonNullColorBuffers = RenderingUtils.GetValidColorBufferCount(colorAttachments);
            if (nonNullColorBuffers > SystemInfo.supportedRenderTargetCount)
                Debug.LogError("Trying to set " + nonNullColorBuffers + " renderTargets, which is more than the maximum supported:" + SystemInfo.supportedRenderTargetCount);

            if (colorAttachments.Length > m_ColorAttachments.Length)
                Debug.LogError("Trying to set " + colorAttachments.Length + " color attachments, which is more than the maximum supported:" + m_ColorAttachments.Length);

            for (int i = 0; i < colorAttachments.Length; ++i)
            {
                m_ColorAttachments[i] = colorAttachments[i];
            }

            for (int i = colorAttachments.Length; i < m_ColorAttachments.Length; ++i)
            {
                m_ColorAttachments[i] = null;
            }

            m_DepthAttachment = depthAttachment;
        }

        [Obsolete(DeprecationMessage.CompatibilityScriptingAPIObsolete, false)]
        internal void ConfigureTarget(RTHandle[] colorAttachments, RTHandle depthAttachment, GraphicsFormat[] formats)
        {
            // Disable obsolete warning for internal usage
            #pragma warning disable CS0618
            ConfigureTarget(colorAttachments, depthAttachment);
            #pragma warning restore CS0618

            for (int i = 0; i < formats.Length; ++i)
                renderTargetFormat[i] = formats[i];
        }

        /// <summary>
        /// Configures render targets for this render pass. Call this instead of CommandBuffer.SetRenderTarget.
        /// This method should be called inside Configure.
        /// </summary>
        /// <param name="colorAttachment">Color attachment identifier.</param>
        /// <seealso cref="Configure"/>
        [Obsolete("Use RTHandle for colorAttachment", true)]
        public void ConfigureTarget(RenderTargetIdentifier colorAttachment)
        {
            throw new NotSupportedException("ConfigureTarget with RenderTargetIdentifier has been deprecated. Use it with RTHandles instead");
        }

        /// <summary>
        /// Configures render targets for this render pass. Call this instead of CommandBuffer.SetRenderTarget.
        /// This method should be called inside Configure.
        /// </summary>
        /// <param name="colorAttachment">Color attachment handle.</param>
        /// <seealso cref="Configure"/>
        [Obsolete(DeprecationMessage.CompatibilityScriptingAPIObsolete, false)]
        public void ConfigureTarget(RTHandle colorAttachment)
        {
            // Disable obsolete warning for internal usage
            #pragma warning disable CS0618
            ConfigureTarget(colorAttachment, k_CameraTarget);
            #pragma warning restore CS0618
        }

        /// <summary>
        /// Configures render targets for this render pass. Call this instead of CommandBuffer.SetRenderTarget.
        /// This method should be called inside Configure.
        /// </summary>
        /// <param name="colorAttachments">Color attachment identifiers.</param>
        /// <seealso cref="Configure"/>
        [Obsolete("Use RTHandles for colorAttachments", true)]
        public void ConfigureTarget(RenderTargetIdentifier[] colorAttachments)
        {
            throw new NotSupportedException("ConfigureTarget with RenderTargetIdentifier has been deprecated. Use it with RTHandles instead");
        }

        /// <summary>
        /// Configures render targets for this render pass. Call this instead of CommandBuffer.SetRenderTarget.
        /// This method should be called inside Configure.
        /// </summary>
        /// <param name="colorAttachments">Color attachment handle.</param>
        /// <seealso cref="Configure"/>
        [Obsolete(DeprecationMessage.CompatibilityScriptingAPIObsolete, false)]
        public void ConfigureTarget(RTHandle[] colorAttachments)
        {
            // Disable obsolete warning for internal usage
            #pragma warning disable CS0618
            ConfigureTarget(colorAttachments, k_CameraTarget);
            #pragma warning restore CS0618
        }

        /// <summary>
        /// Configures clearing for the render targets for this render pass. Call this inside Configure.
        /// </summary>
        /// <param name="clearFlag">ClearFlag containing information about what targets to clear.</param>
        /// <param name="clearColor">Clear color.</param>
        /// <seealso cref="Configure"/>
        [Obsolete(DeprecationMessage.CompatibilityScriptingAPIObsolete, false)]
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
        [Obsolete(DeprecationMessage.CompatibilityScriptingAPIObsolete, false)]
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
        [Obsolete(DeprecationMessage.CompatibilityScriptingAPIObsolete, false)]
        public virtual void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
        { }


        /// <summary>
        /// Called upon finish rendering a camera. You can use this callback to release any resources created
        /// by this render
        /// pass that need to be cleanup once camera has finished rendering.
        /// This method should be called for all cameras in a camera stack.
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
        [Obsolete(DeprecationMessage.CompatibilityScriptingAPIObsolete, false)]
        public virtual void OnFinishCameraStackRendering(CommandBuffer cmd)
        { }

        /// <summary>
        /// Execute the pass. This is where custom rendering occurs. Specific details are left to the implementation
        /// </summary>
        /// <param name="context">Use this render context to issue any draw commands during execution</param>
        /// <param name="renderingData">Current rendering state information</param>
        [Obsolete(DeprecationMessage.CompatibilityScriptingAPIObsolete, false)]
        public virtual void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            Debug.LogWarning("Execute is not implemented, the pass " + this.ToString() + " won't be executed in the current render loop.");
        }

        /// <inheritdoc cref="IRenderGraphRecorder.RecordRenderGraph"/>
        public virtual void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            Debug.LogWarning("The render pass " + this.ToString() + " does not have an implementation of the RecordRenderGraph method. Please implement this method, or consider turning on Compatibility Mode (RenderGraph disabled) in the menu Edit > Project Settings > Graphics > URP. Otherwise the render pass will have no effect. For more information, refer to https://docs.unity3d.com/Packages/com.unity.render-pipelines.universal@latest/index.html?subfolder=/manual/customizing-urp.html.");
        }

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
        [Obsolete("Use RTHandles for source and destination", true)]
        public void Blit(CommandBuffer cmd, RenderTargetIdentifier source, RenderTargetIdentifier destination, Material material = null, int passIndex = 0)
        {
            throw new NotSupportedException("Blit with RenderTargetIdentifier has been deprecated. Use RTHandles instead");
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
        [Obsolete(DeprecationMessage.CompatibilityScriptingAPIObsolete, false)]
        public void Blit(CommandBuffer cmd, RTHandle source, RTHandle destination, Material material = null, int passIndex = 0)
        {
            if (material == null)
                Blitter.BlitCameraTexture(cmd, source, destination, bilinear: source.rt.filterMode == FilterMode.Bilinear);
            else
                Blitter.BlitCameraTexture(cmd, source, destination, material, passIndex);
        }

        /// <summary>
        /// Add a blit command to the context for execution. This applies the material to the color target.
        /// </summary>
        /// <param name="cmd">Command buffer to record command for execution.</param>
        /// <param name="data">RenderingData to access the active renderer.</param>
        /// <param name="material">Material to use.</param>
        /// <param name="passIndex">Shader pass to use. Default is 0.</param>
        [Obsolete(DeprecationMessage.CompatibilityScriptingAPIObsolete, false)]
        public void Blit(CommandBuffer cmd, ref RenderingData data, Material material, int passIndex = 0)
        {
            var renderer = data.cameraData.renderer;

            Blit(cmd, renderer.cameraColorTargetHandle, renderer.GetCameraColorFrontBuffer(cmd), material, passIndex);
            renderer.SwapColorBuffer(cmd);
        }

        /// <summary>
        /// Add a blit command to the context for execution. This applies the material to the color target.
        /// </summary>
        /// <param name="cmd">Command buffer to record command for execution.</param>
        /// <param name="data">RenderingData to access the active renderer.</param>
        /// <param name="source">Source texture or target identifier to blit from.</param>
        /// <param name="material">Material to use.</param>
        /// <param name="passIndex">Shader pass to use. Default is 0.</param>
        [Obsolete(DeprecationMessage.CompatibilityScriptingAPIObsolete, false)]
        public void Blit(CommandBuffer cmd, ref RenderingData data, RTHandle source, Material material, int passIndex = 0)
        {
            var renderer = data.cameraData.renderer;
            Blit(cmd, source, renderer.cameraColorTargetHandle, material, passIndex);
        }

        /// <summary>
        /// Creates <c>DrawingSettings</c> based on current the rendering state.
        /// </summary>
        /// <param name="shaderTagId">Shader pass tag to render.</param>
        /// <param name="renderingData">Current rendering state.</param>
        /// <param name="sortingCriteria">Criteria to sort objects being rendered.</param>
        /// <returns>Returns the draw settings created.</returns>
        /// <seealso cref="DrawingSettings"/>
        public DrawingSettings CreateDrawingSettings(ShaderTagId shaderTagId, ref RenderingData renderingData, SortingCriteria sortingCriteria)
        {
            ContextContainer frameData = renderingData.frameData;
            UniversalRenderingData universalRenderingData = frameData.Get<UniversalRenderingData>();
            UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
            UniversalLightData lightData = frameData.Get<UniversalLightData>();

            return RenderingUtils.CreateDrawingSettings(shaderTagId, universalRenderingData, cameraData, lightData, sortingCriteria);
        }

        /// <summary>
        /// Creates <c>DrawingSettings</c> based on current the rendering state.
        /// </summary>
        /// <param name="shaderTagId">Shader pass tag to render.</param>
        /// <param name="renderingData">Current rendering state.</param>
        /// <param name="cameraData">Current camera state.</param>
        /// <param name="lightData">Current light state.</param>
        /// <param name="sortingCriteria">Criteria to sort objects being rendered.</param>
        /// <returns>Returns the draw settings created.</returns>
        /// <seealso cref="DrawingSettings"/>
        public DrawingSettings CreateDrawingSettings(ShaderTagId shaderTagId, UniversalRenderingData renderingData,
            UniversalCameraData cameraData, UniversalLightData lightData, SortingCriteria sortingCriteria)
        {
            return RenderingUtils.CreateDrawingSettings(shaderTagId, renderingData, cameraData, lightData, sortingCriteria);
        }

        /// <summary>
        /// Creates <c>DrawingSettings</c> based on current rendering state.
        /// </summary>
        /// <param name="shaderTagIdList">List of shader pass tag to render.</param>
        /// <param name="renderingData">Current rendering state.</param>
        /// <param name="sortingCriteria">Criteria to sort objects being rendered.</param>
        /// <returns>Returns the draw settings created.</returns>
        /// <seealso cref="DrawingSettings"/>
        public DrawingSettings CreateDrawingSettings(List<ShaderTagId> shaderTagIdList,
            ref RenderingData renderingData, SortingCriteria sortingCriteria)
        {
            ContextContainer frameData = renderingData.frameData;
            UniversalRenderingData universalRenderingData = frameData.Get<UniversalRenderingData>();
            UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
            UniversalLightData lightData = frameData.Get<UniversalLightData>();

            return RenderingUtils.CreateDrawingSettings(shaderTagIdList, universalRenderingData, cameraData, lightData, sortingCriteria);
        }

        /// <summary>
        /// Creates <c>DrawingSettings</c> based on current rendering state.
        /// </summary>
        /// <param name="shaderTagIdList">List of shader pass tag to render.</param>
        /// <param name="renderingData">Current rendering state.</param>
        /// <param name="cameraData">Current camera state.</param>
        /// <param name="lightData">Current light state.</param>
        /// <param name="sortingCriteria">Criteria to sort objects being rendered.</param>
        /// <returns>Returns the draw settings created.</returns>
        /// <seealso cref="DrawingSettings"/>
        public DrawingSettings CreateDrawingSettings(List<ShaderTagId> shaderTagIdList,
            UniversalRenderingData renderingData, UniversalCameraData cameraData,
            UniversalLightData lightData, SortingCriteria sortingCriteria)
        {
            return RenderingUtils.CreateDrawingSettings(shaderTagIdList, renderingData, cameraData, lightData, sortingCriteria);
        }

        /// <summary>
        /// Compares two instances of <c>ScriptableRenderPass</c> by their <c>RenderPassEvent</c> and returns if <paramref name="lhs"/> is executed before <paramref name="rhs"/>.
        /// </summary>
        /// <param name="lhs"></param>
        /// <param name="rhs"></param>
        /// <returns></returns>
        public static bool operator <(ScriptableRenderPass lhs, ScriptableRenderPass rhs)
        {
            return lhs.renderPassEvent < rhs.renderPassEvent;
        }

        /// <summary>
        /// Compares two instances of <c>ScriptableRenderPass</c> by their <c>RenderPassEvent</c> and returns if <paramref name="lhs"/> is executed after <paramref name="rhs"/>.
        /// </summary>
        /// <param name="lhs"></param>
        /// <param name="rhs"></param>
        /// <returns></returns>
        public static bool operator >(ScriptableRenderPass lhs, ScriptableRenderPass rhs)
        {
            return lhs.renderPassEvent > rhs.renderPassEvent;
        }

        static internal int GetRenderPassEventRange(RenderPassEvent renderPassEvent)
        {
            int numEvents = RenderPassEventsEnumValues.values.Length;
            int currentIndex = 0;

            // find the index of the renderPassEvent in the values array
            for(int i = 0; i < numEvents; ++i)
            {
                if (RenderPassEventsEnumValues.values[currentIndex] == (int)renderPassEvent)
                    break;

                currentIndex++;
            }

            if (currentIndex >= numEvents)
            {
                Debug.LogError("GetRenderPassEventRange: invalid renderPassEvent value cannot be found in the RenderPassEvent enumeration");
                return 0;
            }

            if (currentIndex + 1 >= numEvents)
                return 50; // if this was the last event in the enum, then add 50 as the range

            int nextValue = RenderPassEventsEnumValues.values[currentIndex + 1];

            return nextValue - (int) renderPassEvent;
        }
    }
}
