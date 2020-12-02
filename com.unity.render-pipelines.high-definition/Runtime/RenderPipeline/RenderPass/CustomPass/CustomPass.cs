using System.Collections.Generic;
using System;
using UnityEngine.Serialization;
using UnityEngine.Experimental.Rendering.RenderGraphModule;

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

        MaterialPropertyBlock userMaterialPropertyBlock;

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
            public bool useRenderGraph;

            public RTHandle cameraColorMSAABuffer;
            public RTHandle cameraColorBuffer;
            public Lazy<RTHandle> customColorBuffer;
            public Lazy<RTHandle> customDepthBuffer;

            // Render graph specific
            // TODO RENDERGRAPH cleanup the other ones when we only have the render graph path.
            public TextureHandle colorBufferRG;
            public TextureHandle nonMSAAColorBufferRG;
            public TextureHandle depthBufferRG;
            public TextureHandle normalBufferRG;
            public TextureHandle motionVectorBufferRG;
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
                    userMaterialPropertyBlock = new MaterialPropertyBlock();
                }

                SetCustomPassTarget(cmd);

                // Create the custom pass context:
                bool msaa = IsMSAAEnabled(hdCamera);
                CustomPassContext ctx = new CustomPassContext(
                    renderContext, cmd, hdCamera,
                    cullingResult, msaa ? targets.cameraColorMSAABuffer : targets.cameraColorBuffer,
                    rtManager.GetDepthStencilBuffer(msaa),
                    rtManager.GetNormalBuffer(msaa),
                    targets.customColorBuffer,
                    targets.customDepthBuffer,
                    userMaterialPropertyBlock
                );

                isExecuting = true;
                Execute(ctx);
                isExecuting = false;

                // Set back the camera color buffer if we were using a custom buffer as target
                if (targetDepthBuffer != TargetBuffer.Camera)
                    CoreUtils.SetRenderTarget(cmd, targets.cameraColorBuffer);
            }
        }

        class ExecutePassData
        {
            public CustomPass customPass;
            public CullingResults cullingResult;
            public HDCamera hdCamera;
        }

        RenderTargets ReadRenderTargets(in RenderGraphBuilder builder, in RenderTargets targets)
        {
            RenderTargets output = new RenderTargets();

            output.useRenderGraph = true;

            // Copy over builtin textures.
            output.customColorBuffer = targets.customColorBuffer;
            output.customDepthBuffer = targets.customDepthBuffer;

            // TODO RENDERGRAPH
            // For now we assume that all "outside" textures are both read and written.
            // We can change that once we properly integrate render graph into custom passes.
            // Problem with that is that it will extend the lifetime of any of those textures to the last custom pass that is executed...
            // Also, we test validity of all handles because depending on where the custom pass is executed, they may not always be.
            if (targets.colorBufferRG.IsValid())
                output.colorBufferRG = builder.ReadTexture(builder.WriteTexture(targets.colorBufferRG));
            if (targets.nonMSAAColorBufferRG.IsValid())
                output.nonMSAAColorBufferRG = builder.ReadTexture(builder.WriteTexture(targets.nonMSAAColorBufferRG));
            if (targets.depthBufferRG.IsValid())
                output.depthBufferRG = builder.ReadTexture(builder.WriteTexture(targets.depthBufferRG));
            if (targets.normalBufferRG.IsValid())
                output.normalBufferRG = builder.ReadTexture(builder.WriteTexture(targets.normalBufferRG));
            if (targets.motionVectorBufferRG.IsValid())
                output.motionVectorBufferRG = builder.ReadTexture(targets.motionVectorBufferRG);

            return output;
        }

        internal void ExecuteInternal(RenderGraph renderGraph, HDCamera hdCamera, CullingResults cullingResult, in RenderTargets targets, CustomPassVolume owner)
        {
            this.owner = owner;
            this.currentRTManager = null;
            this.currentRenderTarget = targets;
            this.currentHDCamera = hdCamera;

            using (var builder = renderGraph.AddRenderPass<ExecutePassData>(name, out ExecutePassData passData, profilingSampler))
            {
                passData.customPass = this;
                passData.cullingResult = cullingResult;
                passData.hdCamera = hdCamera;

                this.currentRenderTarget = ReadRenderTargets(builder, targets);

                builder.SetRenderFunc(
                (ExecutePassData data, RenderGraphContext ctx) =>
                {
                    var customPass = data.customPass;

                    ctx.cmd.SetGlobalFloat(HDShaderIDs._CustomPassInjectionPoint, (float)customPass.injectionPoint);
                    if (customPass.currentRenderTarget.colorBufferRG.IsValid() && customPass.injectionPoint == CustomPassInjectionPoint.AfterPostProcess)
                        ctx.cmd.SetGlobalTexture(HDShaderIDs._AfterPostProcessColorBuffer, customPass.currentRenderTarget.colorBufferRG);

                    if (customPass.currentRenderTarget.motionVectorBufferRG.IsValid() && (customPass.injectionPoint == CustomPassInjectionPoint.BeforePostProcess || customPass.injectionPoint == CustomPassInjectionPoint.AfterPostProcess))
                        ctx.cmd.SetGlobalTexture(HDShaderIDs._CameraMotionVectorsTexture, customPass.currentRenderTarget.motionVectorBufferRG);

                    if (customPass.currentRenderTarget.normalBufferRG.IsValid() && customPass.injectionPoint != CustomPassInjectionPoint.AfterPostProcess)
                        ctx.cmd.SetGlobalTexture(HDShaderIDs._NormalBufferTexture, customPass.currentRenderTarget.normalBufferRG);

                    if (!customPass.isSetup)
                    {
                        customPass.Setup(ctx.renderContext, ctx.cmd);
                        customPass.isSetup = true;
                        // TODO RENDERGRAPH: We still need to allocate this otherwise it would be null when switching off render graph (because isSetup stays true).
                        // We can remove the member altogether when we remove the non render graph code path.
                        customPass.userMaterialPropertyBlock = new MaterialPropertyBlock();
                    }

                    customPass.SetCustomPassTarget(ctx.cmd);

                    var outputColorBuffer = customPass.currentRenderTarget.colorBufferRG;

                    // Create the custom pass context:
                    CustomPassContext customPassCtx = new CustomPassContext(
                        ctx.renderContext, ctx.cmd, data.hdCamera,
                        data.cullingResult,
                        outputColorBuffer,
                        customPass.currentRenderTarget.depthBufferRG,
                        customPass.currentRenderTarget.normalBufferRG,
                        customPass.currentRenderTarget.customColorBuffer,
                        customPass.currentRenderTarget.customDepthBuffer,
                        ctx.renderGraphPool.GetTempMaterialPropertyBlock()
                        );

                    customPass.isExecuting = true;
                    customPass.Execute(customPassCtx);
                    customPass.isExecuting = false;

                    // Set back the camera color buffer if we were using a custom buffer as target
                    if (customPass.targetDepthBuffer != TargetBuffer.Camera)
                        CoreUtils.SetRenderTarget(ctx.cmd, outputColorBuffer);
                });
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
            msaa &= injectionPoint == CustomPassInjectionPoint.BeforePreRefraction
                 || injectionPoint == CustomPassInjectionPoint.BeforeTransparent
                 || injectionPoint == CustomPassInjectionPoint.AfterOpaqueDepthAndNormal;

            return msaa;
        }

        // This function must be only called from the ExecuteInternal method (requires current render target and current RT manager)
        void SetCustomPassTarget(CommandBuffer cmd)
        {
            // In case all the buffer are set to none, we can't bind anything
            if (targetColorBuffer == TargetBuffer.None && targetDepthBuffer == TargetBuffer.None)
                return;

            RTHandle colorBuffer, depthBuffer;

            if (!currentRenderTarget.useRenderGraph)
            {
                bool msaa = IsMSAAEnabled(currentHDCamera);
                var cameraColorBuffer = msaa ? currentRenderTarget.cameraColorMSAABuffer : currentRenderTarget.cameraColorBuffer;
                var cameraDepthBuffer = currentRTManager.GetDepthStencilBuffer(msaa);

                colorBuffer = (targetColorBuffer == TargetBuffer.Custom) ? currentRenderTarget.customColorBuffer.Value : cameraColorBuffer;
                depthBuffer = (targetDepthBuffer == TargetBuffer.Custom) ? currentRenderTarget.customDepthBuffer.Value : cameraDepthBuffer;
            }
            else
            {
                colorBuffer = (targetColorBuffer == TargetBuffer.Custom) ? currentRenderTarget.customColorBuffer.Value : currentRenderTarget.colorBufferRG;
                depthBuffer = (targetDepthBuffer == TargetBuffer.Custom) ? currentRenderTarget.customDepthBuffer.Value : currentRenderTarget.depthBufferRG;
            }

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
        [Obsolete("This Execute signature is obsolete and will be removed in the future. Please use Execute(CustomPassContext) instead")]
        protected virtual void Execute(ScriptableRenderContext renderContext, CommandBuffer cmd, HDCamera hdCamera, CullingResults cullingResult) {}

        /// <summary>
        /// Called when your pass needs to be executed by a camera
        /// </summary>
        /// <param name="ctx">The context of the custom pass. Contains command buffer, render context, buffer, etc.</param>
        // TODO: move this function to abstract when we remove the method above
        protected virtual void Execute(CustomPassContext ctx)
        {
#pragma warning disable CS0618 // Member is obsolete
                Execute(ctx.renderContext, ctx.cmd, ctx.hdCamera, ctx.cullingResults);
#pragma warning restore CS0618
        }

        /// <summary>
        /// Called before the first execution of the pass occurs.
        /// Allow you to allocate custom buffers.
        /// </summary>
        /// <param name="renderContext">The render context</param>
        /// <param name="cmd">Current command buffer of the frame</param>
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
        [Obsolete("Use directly CoreUtils.SetRenderTarget with the render target of your choice.")]
        protected void SetCameraRenderTarget(CommandBuffer cmd, bool bindDepth = true, ClearFlag clearFlags = ClearFlag.None)
        {
            if (!isExecuting)
                throw new Exception("SetCameraRenderTarget can only be called inside the CustomPass.Execute function");

            RTHandle colorBuffer, depthBuffer;
            if (currentRenderTarget.useRenderGraph)
            {
                colorBuffer = currentRenderTarget.colorBufferRG;
                depthBuffer = currentRenderTarget.depthBufferRG;
            }
            else
            {
                colorBuffer = currentRenderTarget.cameraColorBuffer;
                depthBuffer = currentRTManager.GetDepthStencilBuffer(IsMSAAEnabled(currentHDCamera));
            }

            if (bindDepth)
                CoreUtils.SetRenderTarget(cmd, colorBuffer, depthBuffer, clearFlags);
            else
                CoreUtils.SetRenderTarget(cmd, colorBuffer, clearFlags);
        }

        /// <summary>
        /// Bind the custom color buffer as the current render target
        /// </summary>
        /// <param name="cmd"></param>
        /// <param name="bindDepth">if true we bind the custom depth buffer in addition to the color</param>
        /// <param name="clearFlags"></param>
        [Obsolete("Use directly CoreUtils.SetRenderTarget with the render target of your choice.")]
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

            // TODO RENDERGRAPH
            // See how to implement this correctly...
            // When running with render graph, the design was to have both msaa/non-msaa textures at the same time, which makes a lot of the code simpler.
            // This pattern here breaks this.
            if (IsMSAAEnabled(hdCamera))
            {
                RTHandle input, output;
                if (currentRenderTarget.useRenderGraph)
                {
                    input = currentRenderTarget.colorBufferRG;
                    output = currentRenderTarget.nonMSAAColorBufferRG;
                }
                else
                {
                    input = currentRenderTarget.cameraColorMSAABuffer;
                    output = currentRenderTarget.cameraColorBuffer;
                }

                currentRTManager.ResolveMSAAColor(cmd, hdCamera, input, output);
            }
        }

        /// <summary>
        /// Resolve the camera color buffer only if the MSAA is enabled and the pass is executed in before transparent.
        /// </summary>
        /// <param name="ctx">Custom Pass Context from the Execute() method</param>
        protected void ResolveMSAAColorBuffer(CustomPassContext ctx) => ResolveMSAAColorBuffer(ctx.cmd, ctx.hdCamera);

        /// <summary>
        /// Get the current camera buffers (can be MSAA)
        /// </summary>
        /// <param name="colorBuffer">outputs the camera color buffer</param>
        /// <param name="depthBuffer">outputs the camera depth buffer</param>
        [Obsolete("GetCameraBuffers is obsolete and will be removed in the future. All camera buffers are now avaliable directly in the CustomPassContext in parameter of the Execute function")]
        protected void GetCameraBuffers(out RTHandle colorBuffer, out RTHandle depthBuffer)
        {
            if (!isExecuting)
                throw new Exception("GetCameraBuffers can only be called inside the CustomPass.Execute function");

            bool msaa = IsMSAAEnabled(currentHDCamera);
            if (currentRenderTarget.useRenderGraph)
            {
                colorBuffer = currentRenderTarget.colorBufferRG;
                depthBuffer = currentRenderTarget.depthBufferRG;
            }
            else
            {
                colorBuffer = msaa ? currentRenderTarget.cameraColorMSAABuffer : currentRenderTarget.cameraColorBuffer;
                depthBuffer = currentRTManager.GetDepthStencilBuffer(msaa);
            }
        }

        /// <summary>
        /// Get the current custom buffers
        /// </summary>
        /// <param name="colorBuffer">outputs the custom color buffer</param>
        /// <param name="depthBuffer">outputs the custom depth buffer</param>
        [Obsolete("GetCustomBuffers is obsolete and will be removed in the future. All custom buffers are now avaliable directly in the CustomPassContext in parameter of the Execute function")]
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
        [Obsolete("GetNormalBuffer is obsolete and will be removed in the future. Normal buffer is now avaliable directly in the CustomPassContext in parameter of the Execute function")]
        protected RTHandle GetNormalBuffer()
        {
            if (!isExecuting)
                throw new Exception("GetNormalBuffer can only be called inside the CustomPass.Execute function");

            if (currentRenderTarget.useRenderGraph)
                return currentRenderTarget.normalBufferRG;
            else
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
            => CustomPassUtils.GetRenderQueueRangeFromRenderQueueType(type);

        /// <summary>
        /// Create a custom pass to execute a fullscreen pass
        /// </summary>
        /// <param name="fullScreenMaterial">The material to use for your fullscreen pass. It must have a shader based on the Custom Pass Fullscreen shader or equivalent</param>
        /// <param name="targetColorBuffer"></param>
        /// <param name="targetDepthBuffer"></param>
        /// <returns></returns>
        public static FullScreenCustomPass CreateFullScreenPass(Material fullScreenMaterial, TargetBuffer targetColorBuffer = TargetBuffer.Camera,
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
        public static DrawRenderersCustomPass CreateDrawRenderersPass(RenderQueueType queue, LayerMask mask,
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
