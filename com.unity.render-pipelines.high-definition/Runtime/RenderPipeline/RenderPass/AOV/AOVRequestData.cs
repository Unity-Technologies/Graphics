using System;
using System.Collections.Generic;
using UnityEngine.Assertions;
using UnityEngine.Experimental.Rendering.RenderGraphModule;

namespace UnityEngine.Rendering.HighDefinition
{
    /// <summary>Called when the rendering has completed.</summary>
    /// <param name="cmd">A command buffer that can be used.</param>
    /// <param name="buffers">The buffers that has been requested.</param>
    /// <param name="outputProperties">Several properties that were computed for this frame.</param>
    public delegate void FramePassCallback(CommandBuffer cmd, List<RTHandle> buffers, RenderOutputProperties outputProperties);
    /// <summary>
    /// Called to allocate a RTHandle for a specific AOVBuffer.
    /// </summary>
    /// <param name="aovBufferId">The AOVBuffer to allocatE.</param>
    public delegate RTHandle AOVRequestBufferAllocator(AOVBuffers aovBufferId);

    /// <summary>
    /// Called to allocate a RTHandle for a specific custom pass AOVBuffer.
    /// </summary>
    /// <param name="aovBufferId">The AOVBuffer to allocatE.</param>
    public delegate RTHandle AOVRequestCustomPassBufferAllocator(CustomPassAOVBuffers aovBufferId);

    /// <summary>Describes a frame pass.</summary>
    public struct AOVRequestData
    {
        /// <summary>Default frame pass settings.</summary>
        [Obsolete("Since 2019.3, use AOVRequestData.NewDefault() instead.")]
        public static readonly AOVRequestData @default = default;
        /// <summary>
        /// Instantiate a new AOV request data with default values.
        ///
        /// Note: Allocates memory by the garbage collector.
        /// If you intend only to read the default values, you should use <see cref="defaultAOVRequestDataNonAlloc"/>.
        /// </summary>
        /// <returns>A new AOV request data with default values.</returns>
        public static AOVRequestData NewDefault() => new AOVRequestData
        {
            m_Settings = AOVRequest.NewDefault(),
            m_RequestedAOVBuffers = new AOVBuffers[] {},
            m_Callback = null
        };

        /// <summary>Default frame pass settings.</summary>
        public static readonly AOVRequestData defaultAOVRequestDataNonAlloc = NewDefault();

        private AOVRequest m_Settings;
        private AOVBuffers[] m_RequestedAOVBuffers;
        private CustomPassAOVBuffers[] m_CustomPassAOVBuffers;
        private FramePassCallback m_Callback;
        private readonly AOVRequestBufferAllocator m_BufferAllocator;
        private readonly AOVRequestCustomPassBufferAllocator m_CustomPassBufferAllocator;
        private List<GameObject> m_LightFilter;

        /// <summary>Whether this frame pass is valid.</summary>
        public bool isValid => (m_RequestedAOVBuffers != null || m_CustomPassAOVBuffers != null) && m_Callback != null;

        /// <summary>Create a new frame pass.</summary>
        /// <param name="settings">Settings to use.</param>
        /// <param name="bufferAllocator">Buffer allocators to use.</param>
        /// <param name="lightFilter">If null, all light will be rendered, if not, only those light will be rendered.</param>
        /// <param name="requestedAOVBuffers">The requested buffers for the callback.</param>
        /// <param name="callback">The callback to execute.</param>
        public AOVRequestData(
            AOVRequest settings,
            AOVRequestBufferAllocator bufferAllocator,
            List<GameObject> lightFilter,
            AOVBuffers[] requestedAOVBuffers,
            FramePassCallback callback
        )
        {
            m_Settings = settings;
            m_BufferAllocator = bufferAllocator;
            m_RequestedAOVBuffers = requestedAOVBuffers;
            m_LightFilter = lightFilter;
            m_Callback = callback;
            m_CustomPassAOVBuffers = null;
            m_CustomPassBufferAllocator = null;
        }

        /// <summary>Create a new frame pass.</summary>
        /// <param name="settings">Settings to use.</param>
        /// <param name="bufferAllocator">Buffer allocators to use.</param>
        /// <param name="lightFilter">If null, all light will be rendered, if not, only those light will be rendered.</param>
        /// <param name="requestedAOVBuffers">The requested buffers for the callback.</param>
        /// <param name="customPassAOVBuffers">The custom pass buffers that will be captured.</param>
        /// <param name="callback">The callback to execute.</param>
        public AOVRequestData(
            AOVRequest settings,
            AOVRequestBufferAllocator bufferAllocator,
            AOVRequestCustomPassBufferAllocator customPassBufferAllocator,
            List<GameObject> lightFilter,
            AOVBuffers[] requestedAOVBuffers,
            CustomPassAOVBuffers[] customPassAOVBuffers,
            FramePassCallback callback
        )
        {
            m_Settings = settings;
            m_BufferAllocator = bufferAllocator;
            m_RequestedAOVBuffers = requestedAOVBuffers;
            m_CustomPassAOVBuffers = customPassAOVBuffers;
            m_CustomPassBufferAllocator = customPassBufferAllocator;
            m_LightFilter = lightFilter;
            m_Callback = callback;
        }


        /// <summary>Allocate texture if required.</summary>
        /// <param name="textures">A buffer of texture ready to use.</param>
        public void AllocateTargetTexturesIfRequired(ref List<RTHandle> textures)
        {
            if (!isValid || textures == null)
                return;

            textures.Clear();

            if (m_RequestedAOVBuffers != null)
            {
                foreach (var bufferId in m_RequestedAOVBuffers)
                    textures.Add(m_BufferAllocator(bufferId));
            }

            if (m_CustomPassAOVBuffers != null)
            {
                foreach (var aovBufferId in m_CustomPassAOVBuffers)
                    textures.Add(m_CustomPassBufferAllocator(aovBufferId));
            }
        }

        /// <summary>Copy a camera sized texture into the texture buffers.</summary>
        /// <param name="cmd">the command buffer to use for the copy.</param>
        /// <param name="aovBufferId">The id of the buffer to copy.</param>
        /// <param name="camera">The camera associated with the source texture.</param>
        /// <param name="source">The source texture to copy</param>
        /// <param name="targets">The target texture buffer.</param>
        internal void PushCameraTexture(
            CommandBuffer cmd,
            AOVBuffers aovBufferId,
            HDCamera camera,
            RTHandle source,
            List<RTHandle> targets
        )
        {
            if (!isValid || m_RequestedAOVBuffers == null)
                return;

            Assert.IsNotNull(m_RequestedAOVBuffers);
            Assert.IsNotNull(targets);

            var index = Array.IndexOf(m_RequestedAOVBuffers, aovBufferId);
            if (index == -1)
                return;

            HDUtils.BlitCameraTexture(cmd, source, targets[index]);
        }

        internal void PushCustomPassTexture(
            CommandBuffer cmd,
            CustomPassInjectionPoint injectionPoint,
            RTHandle cameraSource,
            Lazy<RTHandle> customPassSource,
            List<RTHandle> targets
        )
        {
            if (!isValid || m_CustomPassAOVBuffers == null)
                return;

            Assert.IsNotNull(targets);

            var index = Array.FindIndex(m_CustomPassAOVBuffers, x => x.injectionPoint == injectionPoint);
            if (index == -1)
                return;

            if (m_CustomPassAOVBuffers[index].outputType == CustomPassAOVBuffers.OutputType.Camera)
            {
                HDUtils.BlitCameraTexture(cmd, cameraSource, targets[index]);
            }
            else
            {
                if (customPassSource.IsValueCreated)
                {
                    HDUtils.BlitCameraTexture(cmd, customPassSource.Value, targets[index]);
                }
            }
        }

        class PushCameraTexturePassData
        {
            public int                  requestIndex;
            public TextureHandle        source;
            // Not super clean to not use TextureHandles here. In practice it's ok because those texture are never passed back to any other render pass.
            public List<RTHandle>       targets;
        }

        internal void PushCameraTexture(
            RenderGraph         renderGraph,
            AOVBuffers          aovBufferId,
            HDCamera            camera,
            TextureHandle       source,
            List<RTHandle>      targets
        )
        {
            if (!isValid || m_RequestedAOVBuffers == null)
                return;

            Assert.IsNotNull(m_RequestedAOVBuffers);
            Assert.IsNotNull(targets);

            var index = Array.IndexOf(m_RequestedAOVBuffers, aovBufferId);
            if (index == -1)
                return;

            using (var builder = renderGraph.AddRenderPass<PushCameraTexturePassData>("Push AOV Camera Texture", out var passData))
            {
                passData.requestIndex = index;
                passData.source = builder.ReadTexture(source);
                passData.targets = targets;

                builder.SetRenderFunc(
                (PushCameraTexturePassData data, RenderGraphContext ctx) =>
                {
                    HDUtils.BlitCameraTexture(ctx.cmd, ctx.resources.GetTexture(data.source), data.targets[data.requestIndex]);
                });
            }
        }

        /// <summary>Execute the frame pass callback. It assumes that the textures are properly initialized and filled.</summary>
        /// <param name="cmd">The command buffer to use.</param>
        /// <param name="framePassTextures">The textures to use.</param>
        /// <param name="outputProperties">The properties computed for this frame.</param>
        public void Execute(CommandBuffer cmd, List<RTHandle> framePassTextures, RenderOutputProperties outputProperties)
        {
            if (!isValid)
                return;

            m_Callback(cmd, framePassTextures, outputProperties);
        }

        /// <summary>Setup the display manager if necessary.</summary>
        /// <param name="debugDisplaySettings"></param>
        public void SetupDebugData(ref DebugDisplaySettings debugDisplaySettings)
        {
            if (!isValid)
                return;

            debugDisplaySettings = new DebugDisplaySettings();
            m_Settings.FillDebugData(debugDisplaySettings);
        }

        /// <summary>Whether a light should be rendered.</summary>
        /// <param name="gameObject">The game object of the light to be rendered.</param>
        /// <returns><c>true</c> when the light must be rendered, <c>false</c> when it should be ignored.</returns>
        public bool IsLightEnabled(GameObject gameObject) => m_LightFilter == null || m_LightFilter.Contains(gameObject);
    }
}
