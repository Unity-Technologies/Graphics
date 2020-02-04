using System.Collections.Generic;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;
using System;
using UnityEngine.Serialization;

namespace UnityEngine.Rendering.HighDefinition
{
    /// <summary>
    /// Class that holds data and logic for the pass to be executed
    /// </summary>
    [System.Serializable]
    public abstract class CustomPass : IVersionable<DrawRenderersCustomPass.Version>
    {
        /// <summary>
        /// Name of the custom pass
        /// </summary>
        public string name
        {
            get => m_Name;
            set
            {
                m_Name = value;
                m_ProfilingSampler = new ProfilingSampler(m_Name);
            }
        }
        [SerializeField, FormerlySerializedAsAttribute("name")]
        string m_Name = "Custom Pass";

        internal ProfilingSampler   profilingSampler
        {
            get
            {
                if (m_ProfilingSampler == null)
                    m_ProfilingSampler = new ProfilingSampler(m_Name ?? "Custom Pass");
                return m_ProfilingSampler;
            }
        }
        ProfilingSampler            m_ProfilingSampler;

        /// <summary>
        /// Is the custom pass enabled or not
        /// </summary>
        public bool             enabled = true;

        /// <summary>
        /// Target color buffer (Camera or Custom)
        /// </summary>
        public TargetBuffer     targetColorBuffer;

        /// <summary>
        /// Target depth buffer (camera or custom)
        /// </summary>
        public TargetBuffer     targetDepthBuffer;

        /// <summary>
        /// What clear to apply when the color and depth buffer are bound
        /// </summary>
        public ClearFlag        clearFlags;

        [SerializeField]
        bool                passFoldout;
        [System.NonSerialized]
        bool                isSetup = false;
        bool                isExecuting = false;
        RenderTargets       currentRenderTarget;
        CustomPassVolume    owner;
        SharedRTManager     currentRTManager;
        HDCamera            currentHDCamera;

        /// <summary>
        /// Mirror of the value in the CustomPassVolume where this custom pass is listed
        /// </summary>
        /// <value>The blend value that should be applied to the custom pass effect</value>
        protected float fadeValue => owner.fadeValue;

        /// <summary>
        /// Get the injection point in HDRP where this pass will be executed
        /// </summary>
        /// <value></value>
        protected CustomPassInjectionPoint injectionPoint => owner.injectionPoint;

        /// <summary>
        /// True if you want your custom pass to be executed in the scene view. False for game cameras only.
        /// </summary>
        protected virtual bool executeInSceneView => true;

        /// <summary>
        /// Used to select the target buffer when executing the custom pass
        /// </summary>
        public enum TargetBuffer
        {
            /// <summary>The buffers for the currently rendering Camera.</summary>
            Camera,
            /// <summary>The custom rendering buffers that HDRP allocates.</summary>
            Custom,
            /// <summary>No target buffer.</summary>
            None,
        }

        /// <summary>
        /// Render Queue filters for the DrawRenderers custom pass
        /// </summary>
        public enum RenderQueueType
        {
            /// <summary>Opaque GameObjects without alpha test only.</summary>
            OpaqueNoAlphaTest,
            /// <summary>Opaque GameObjects with alpha test only.</summary>
            OpaqueAlphaTest,
            /// <summary>All opaque GameObjects.</summary>
            AllOpaque,
            /// <summary>Opaque GameObjects that use the after post process render pass.</summary>
            AfterPostProcessOpaque,
            /// <summary>Transparent GameObjects that use the the pre refraction render pass.</summary>
            PreRefraction,
            /// <summary>Transparent GameObjects that use the default render pass.</summary>
            Transparent,
            /// <summary>Transparent GameObjects that use the low resolution render pass.</summary>
            LowTransparent,
            /// <summary>All Transparent GameObjects.</summary>
            AllTransparent,
            /// <summary>Transparent GameObjects that use the Pre-refraction, Default, or Low resolution render pass.</summary>
            AllTransparentWithLowRes,
            /// <summary>Transparent GameObjects that use after post process render pass.</summary>
            AfterPostProcessTransparent,
            /// <summary>All GameObjects</summary>
            All,
        }

        internal struct RenderTargets
        {
            public RTHandle cameraColorMSAABuffer;
            public RTHandle cameraColorBuffer;
            public Lazy<RTHandle> customColorBuffer;
            public Lazy<RTHandle> customDepthBuffer;
        }

        enum Version
        {
            Initial,
        }

        [SerializeField]
        Version     m_Version = MigrationDescription.LastVersion<Version>();
        Version IVersionable<Version>.version
        {
            get => m_Version;
            set => m_Version = value;
        }

        internal bool WillBeExecuted(HDCamera hdCamera)
        {
            if (!enabled)
                return false;

            if (hdCamera.camera.cameraType == CameraType.SceneView && !executeInSceneView)
                return false;

            return true;
        }

        internal void ExecuteInternal(ScriptableRenderContext renderContext, CommandBuffer cmd, HDCamera hdCamera, CullingResults cullingResult, SharedRTManager rtManager, RenderTargets targets, CustomPassVolume owner)
        {
            this.owner = owner;
            this.currentRTManager = rtManager;
            this.currentRenderTarget = targets;
            this.currentHDCamera = hdCamera;

            using (new ProfilingScope(cmd, profilingSampler))
            {
                if (!isSetup)
                {
                    Setup(renderContext, cmd);
                    isSetup = true;
                }

                SetCustomPassTarget(cmd);

                isExecuting = true;
                Execute(renderContext, cmd, hdCamera, cullingResult);
                isExecuting = false;

                // Set back the camera color buffer if we were using a custom buffer as target
                if (targetDepthBuffer != TargetBuffer.Camera)
                    CoreUtils.SetRenderTarget(cmd, targets.cameraColorBuffer);
            }
        }

        internal void InternalAggregateCullingParameters(ref ScriptableCullingParameters cullingParameters, HDCamera hdCamera) => AggregateCullingParameters(ref cullingParameters, hdCamera);

        /// <summary>
        /// Cleans up the custom pass when Unity destroys it unexpectedly. Currently, this happens every time you edit
        /// the UI because of a bug with the SerializeReference attribute.
        /// </summary>
        ~CustomPass() { CleanupPassInternal(); }

        internal void CleanupPassInternal()
        {
            if (isSetup)
            {
                Cleanup();
                isSetup = false;
            }
        }

        bool IsMSAAEnabled(HDCamera hdCamera)
        {
            // if MSAA is enabled and the current injection point is before transparent.
            bool msaa = hdCamera.frameSettings.IsEnabled(FrameSettingsField.MSAA);
            msaa &= injectionPoint == CustomPassInjectionPoint.BeforeTransparent || injectionPoint == CustomPassInjectionPoint.AfterOpaqueDepthAndNormal;

            return msaa;
        }

        // This function must be only called from the ExecuteInternal method (requires current render target and current RT manager)
        void SetCustomPassTarget(CommandBuffer cmd)
        {
            // In case all the buffer are set to none, we can't bind anything
            if (targetColorBuffer == TargetBuffer.None && targetDepthBuffer == TargetBuffer.None)
                return;

            bool msaa = IsMSAAEnabled(currentHDCamera);
            var cameraColorBuffer = msaa ? currentRenderTarget.cameraColorMSAABuffer : currentRenderTarget.cameraColorBuffer;
            var cameraDepthBuffer = currentRTManager.GetDepthStencilBuffer(msaa);

            RTHandle colorBuffer = (targetColorBuffer == TargetBuffer.Custom) ? currentRenderTarget.customColorBuffer.Value : cameraColorBuffer;
            RTHandle depthBuffer = (targetDepthBuffer == TargetBuffer.Custom) ? currentRenderTarget.customDepthBuffer.Value : cameraDepthBuffer;

            if (targetColorBuffer == TargetBuffer.None && targetDepthBuffer != TargetBuffer.None)
                CoreUtils.SetRenderTarget(cmd, depthBuffer, clearFlags);
            else if (targetColorBuffer != TargetBuffer.None && targetDepthBuffer == TargetBuffer.None)
                CoreUtils.SetRenderTarget(cmd, colorBuffer, clearFlags);
            else
                CoreUtils.SetRenderTarget(cmd, colorBuffer, depthBuffer, clearFlags);
        }

        /// <summary>
        /// Use this method if you want to draw objects that are not visible in the camera.
        /// For example if you disable a layer in the camera and add it in the culling parameters, then the culling result will contains your layer.
        /// </summary>
        /// <param name="cullingParameters">Aggregate the parameters in this property (use |= for masks fields, etc.)</param>
        /// <param name="hdCamera">The camera where the culling is being done</param>
        protected virtual void AggregateCullingParameters(ref ScriptableCullingParameters cullingParameters, HDCamera hdCamera) {}

        /// <summary>
        /// Called when your pass needs to be executed by a camera
        /// </summary>
        /// <param name="renderContext"></param>
        /// <param name="cmd"></param>
        /// <param name="hdCamera"></param>
        /// <param name="cullingResult"></param>
        protected abstract void Execute(ScriptableRenderContext renderContext, CommandBuffer cmd, HDCamera hdCamera, CullingResults cullingResult);

        /// <summary>
        /// Called before the first execution of the pass occurs.
        /// Allow you to allocate custom buffers.
        /// </summary>
        /// <param name="renderContext"></param>
        /// <param name="cmd"></param>
        protected virtual void Setup(ScriptableRenderContext renderContext, CommandBuffer cmd) {}

        /// <summary>
        /// Called when HDRP is destroyed.
        /// Allow you to free custom buffers.
        /// </summary>
        protected virtual void Cleanup() {}

        /// <summary>
        /// Bind the camera color buffer as the current render target
        /// </summary>
        /// <param name="cmd"></param>
        /// <param name="bindDepth">if true we bind the camera depth buffer in addition to the color</param>
        /// <param name="clearFlags"></param>
        protected void SetCameraRenderTarget(CommandBuffer cmd, bool bindDepth = true, ClearFlag clearFlags = ClearFlag.None)
        {
            if (!isExecuting)
                throw new Exception("SetCameraRenderTarget can only be called inside the CustomPass.Execute function");

            if (bindDepth)
                CoreUtils.SetRenderTarget(cmd, currentRenderTarget.cameraColorBuffer, currentRTManager.GetDepthStencilBuffer(IsMSAAEnabled(currentHDCamera)), clearFlags);
            else
                CoreUtils.SetRenderTarget(cmd, currentRenderTarget.cameraColorBuffer, clearFlags);
        }

        /// <summary>
        /// Bind the custom color buffer as the current render target
        /// </summary>
        /// <param name="cmd"></param>
        /// <param name="bindDepth">if true we bind the custom depth buffer in addition to the color</param>
        /// <param name="clearFlags"></param>
        protected void SetCustomRenderTarget(CommandBuffer cmd, bool bindDepth = true, ClearFlag clearFlags = ClearFlag.None)
        {
            if (!isExecuting)
                throw new Exception("SetCameraRenderTarget can only be called inside the CustomPass.Execute function");

            if (bindDepth)
                CoreUtils.SetRenderTarget(cmd, currentRenderTarget.customColorBuffer.Value, currentRenderTarget.customDepthBuffer.Value, clearFlags);
            else
                CoreUtils.SetRenderTarget(cmd, currentRenderTarget.customColorBuffer.Value, clearFlags);
        }

        /// <summary>
        /// Bind the render targets according to the parameters of the UI (targetColorBuffer, targetDepthBuffer and clearFlags)
        /// </summary>
        /// <param name="cmd"></param>
        protected void SetRenderTargetAuto(CommandBuffer cmd) => SetCustomPassTarget(cmd);

        /// <summary>
        /// Resolve the camera color buffer only if the MSAA is enabled and the pass is executed in before transparent.
        /// </summary>
        /// <param name="cmd"></param>
        /// <param name="hdCamera"></param>
        protected void ResolveMSAAColorBuffer(CommandBuffer cmd, HDCamera hdCamera)
        {
            if (!isExecuting)
                throw new Exception("ResolveMSAAColorBuffer can only be called inside the CustomPass.Execute function");

            if (IsMSAAEnabled(hdCamera))
            {
                currentRTManager.ResolveMSAAColor(cmd, hdCamera, currentRenderTarget.cameraColorMSAABuffer, currentRenderTarget.cameraColorBuffer);
            }
        }

        /// <summary>
        /// Get the current camera buffers (can be MSAA)
        /// </summary>
        /// <param name="colorBuffer">outputs the camera color buffer</param>
        /// <param name="depthBuffer">outputs the camera depth buffer</param>
        protected void GetCameraBuffers(out RTHandle colorBuffer, out RTHandle depthBuffer)
        {
            if (!isExecuting)
                throw new Exception("GetCameraBuffers can only be called inside the CustomPass.Execute function");

            bool msaa = IsMSAAEnabled(currentHDCamera);
            colorBuffer = msaa ? currentRenderTarget.cameraColorMSAABuffer : currentRenderTarget.cameraColorBuffer;
            depthBuffer = currentRTManager.GetDepthStencilBuffer(msaa);
        }

        /// <summary>
        /// Get the current custom buffers
        /// </summary>
        /// <param name="colorBuffer">outputs the custom color buffer</param>
        /// <param name="depthBuffer">outputs the custom depth buffer</param>
        protected void GetCustomBuffers(out RTHandle colorBuffer, out RTHandle depthBuffer)
        {
            if (!isExecuting)
                throw new Exception("GetCustomBuffers can only be called inside the CustomPass.Execute function");

            colorBuffer = currentRenderTarget.customColorBuffer.Value;
            depthBuffer = currentRenderTarget.customDepthBuffer.Value;
        }

        /// <summary>
        /// Get the current normal buffer (can be MSAA)
        /// </summary>
        /// <returns></returns>
        protected RTHandle GetNormalBuffer()
        {
            if (!isExecuting)
                throw new Exception("GetNormalBuffer can only be called inside the CustomPass.Execute function");

            return currentRTManager.GetNormalBuffer(IsMSAAEnabled(currentHDCamera));
        }

        /// <summary>
        /// List all the materials that need to be displayed at the bottom of the component.
        /// All the materials gathered by this method will be used to create a Material Editor and then can be edited directly on the custom pass.
        /// </summary>
        /// <returns>An enumerable of materials to show in the inspector. These materials can be null, the list is cleaned afterwards</returns>
        public virtual IEnumerable<Material> RegisterMaterialForInspector() { yield break; }

        /// <summary>
        /// Returns the render queue range associated with the custom render queue type
        /// </summary>
        /// <param name="type">The custom pass render queue type.</param>
        /// <returns>Returns a render queue range compatible with a ScriptableRenderContext.DrawRenderers.</returns>
        protected RenderQueueRange GetRenderQueueRange(CustomPass.RenderQueueType type)
        {
            switch (type)
            {
                case CustomPass.RenderQueueType.OpaqueNoAlphaTest: return HDRenderQueue.k_RenderQueue_OpaqueNoAlphaTest;
                case CustomPass.RenderQueueType.OpaqueAlphaTest: return HDRenderQueue.k_RenderQueue_OpaqueAlphaTest;
                case CustomPass.RenderQueueType.AllOpaque: return HDRenderQueue.k_RenderQueue_AllOpaque;
                case CustomPass.RenderQueueType.AfterPostProcessOpaque: return HDRenderQueue.k_RenderQueue_AfterPostProcessOpaque;
                case CustomPass.RenderQueueType.PreRefraction: return HDRenderQueue.k_RenderQueue_PreRefraction;
                case CustomPass.RenderQueueType.Transparent: return HDRenderQueue.k_RenderQueue_Transparent;
                case CustomPass.RenderQueueType.LowTransparent: return HDRenderQueue.k_RenderQueue_LowTransparent;
                case CustomPass.RenderQueueType.AllTransparent: return HDRenderQueue.k_RenderQueue_AllTransparent;
                case CustomPass.RenderQueueType.AllTransparentWithLowRes: return HDRenderQueue.k_RenderQueue_AllTransparentWithLowRes;
                case CustomPass.RenderQueueType.AfterPostProcessTransparent: return HDRenderQueue.k_RenderQueue_AfterPostProcessTransparent;
                case CustomPass.RenderQueueType.All:
                default:
                    return HDRenderQueue.k_RenderQueue_All;
            }
        }

        /// <summary>
        /// Create a custom pass to execute a fullscreen pass
        /// </summary>
        /// <param name="fullScreenMaterial">The material to use for your fullscreen pass. It must have a shader based on the Custom Pass Fullscreen shader or equivalent</param>
        /// <param name="targetColorBuffer"></param>
        /// <param name="targetDepthBuffer"></param>
        /// <returns></returns>
        public static CustomPass CreateFullScreenPass(Material fullScreenMaterial, TargetBuffer targetColorBuffer = TargetBuffer.Camera,
            TargetBuffer targetDepthBuffer = TargetBuffer.Camera)
        {
            return new FullScreenCustomPass()
            {
                name = "FullScreen Pass",
                targetColorBuffer = targetColorBuffer,
                targetDepthBuffer = targetDepthBuffer,
                fullscreenPassMaterial = fullScreenMaterial,
            };
        }

        /// <summary>
        /// Create a Custom Pass to render objects
        /// </summary>
        /// <param name="queue">The render queue filter to select which object will be rendered</param>
        /// <param name="mask">The layer mask to select which layer(s) will be rendered</param>
        /// <param name="overrideMaterial">The replacement material to use when renering objects</param>
        /// <param name="overrideMaterialPassName">The pass name to use in the override material</param>
        /// <param name="sorting">Sorting options when rendering objects</param>
        /// <param name="clearFlags">Clear options when the target buffers are bound. Before executing the pass</param>
        /// <param name="targetColorBuffer">Target Color buffer</param>
        /// <param name="targetDepthBuffer">Target Depth buffer. Note: It's also the buffer which will do the Depth Test</param>
        /// <returns></returns>
        public static CustomPass CreateDrawRenderersPass(RenderQueueType queue, LayerMask mask,
            Material overrideMaterial, string overrideMaterialPassName = "Forward", SortingCriteria sorting = SortingCriteria.CommonOpaque,
            ClearFlag clearFlags = ClearFlag.None, TargetBuffer targetColorBuffer = TargetBuffer.Camera,
            TargetBuffer targetDepthBuffer = TargetBuffer.Camera)
        {
            return new DrawRenderersCustomPass()
            {
                name = "DrawRenderers Pass",
                renderQueueType = queue,
                layerMask = mask,
                overrideMaterial = overrideMaterial,
                overrideMaterialPassName = overrideMaterialPassName,
                sortingCriteria = sorting,
                clearFlags = clearFlags,
                targetColorBuffer = targetColorBuffer,
                targetDepthBuffer = targetDepthBuffer,
            };
        }
    }
}
