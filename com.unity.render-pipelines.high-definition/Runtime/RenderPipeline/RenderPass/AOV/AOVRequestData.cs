using System;
using System.Collections.Generic;
using UnityEngine.Assertions;
using UnityEngine.Experimental.Rendering;
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

    /// <summary>Called when the rendering has completed.</summary>
    /// <param name="cmd">A command buffer that can be used.</param>
    /// <param name="buffers">The buffers that has been requested.</param>
    /// <param name="outputProperties">Several properties that were computed for this frame.</param>
    public delegate void FramePassCallbackEx(CommandBuffer cmd, List<RTHandle> buffers, List<RTHandle> customPassbuffers, RenderOutputProperties outputProperties);
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
            m_RequestedAOVBuffers = new AOVBuffers[] { },
            m_Callback = null
        };

        /// <summary>Default frame pass settings.</summary>
        public static readonly AOVRequestData defaultAOVRequestDataNonAlloc = NewDefault();

        private AOVRequest m_Settings;
        private AOVBuffers[] m_RequestedAOVBuffers;
        private CustomPassAOVBuffers[] m_CustomPassAOVBuffers;
        private FramePassCallback m_Callback;
        private FramePassCallbackEx m_CallbackEx;
        private readonly AOVRequestBufferAllocator m_BufferAllocator;
        private readonly AOVRequestCustomPassBufferAllocator m_CustomPassBufferAllocator;
        private List<GameObject> m_LightFilter;

        /// <summary>Whether this frame pass is valid.</summary>
        public bool isValid => (m_RequestedAOVBuffers != null || m_CustomPassAOVBuffers != null) && (m_Callback != null || m_CallbackEx != null);

        /// <summary>Whether internal rendering should be done at the same format as the user allocated AOV output buffer.</summary>
        public bool overrideRenderFormat => m_Settings.overrideRenderFormat;

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

            m_CallbackEx = null;
            m_CustomPassAOVBuffers = null;
            m_CustomPassBufferAllocator = null;
        }

        /// <summary>Create a new frame pass.</summary>
        /// <param name="settings">Settings to use.</param>
        /// <param name="bufferAllocator">Buffer allocators to use.</param>
        /// <param name="lightFilter">If null, all light will be rendered, if not, only those light will be rendered.</param>
        /// <param name="requestedAOVBuffers">The requested buffers for the callback.</param>
        /// <param name="customPassAOVBuffers">The custom pass buffers that will be captured.</param>
        /// <param name="customPassBufferAllocator">Buffer allocators to use for custom passes.</param>
        /// <param name="callback">The callback to execute.</param>
        public AOVRequestData(
            AOVRequest settings,
            AOVRequestBufferAllocator bufferAllocator,
            List<GameObject> lightFilter,
            AOVBuffers[] requestedAOVBuffers,
            CustomPassAOVBuffers[] customPassAOVBuffers,
            AOVRequestCustomPassBufferAllocator customPassBufferAllocator,
            FramePassCallbackEx callback
        )
        {
            m_Settings = settings;
            m_BufferAllocator = bufferAllocator;
            m_RequestedAOVBuffers = requestedAOVBuffers;
            m_CustomPassAOVBuffers = customPassAOVBuffers;
            m_CustomPassBufferAllocator = customPassBufferAllocator;
            m_LightFilter = lightFilter;
            m_Callback = null;
            m_CallbackEx = callback;
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
        }

        /// <summary>Allocate texture if required.</summary>
        /// <param name="textures">A buffer of textures ready to use.</param>
        /// <param name="customPassTextures">A buffer of textures ready to use for custom pass AOVs.</param>
        public void AllocateTargetTexturesIfRequired(ref List<RTHandle> textures, ref List<RTHandle> customPassTextures)
        {
            if (!isValid || textures == null)
                return;

            textures.Clear();
            customPassTextures.Clear();

            if (m_RequestedAOVBuffers != null)
            {
                foreach (var bufferId in m_RequestedAOVBuffers)
                {
                    var rtHandle = m_BufferAllocator(bufferId);
                    textures.Add(rtHandle);
                    if (rtHandle == null)
                    {
                        Debug.LogError("Allocation for requested AOVBuffers ID: " + bufferId.ToString() + " have fail. Please ensure the callback allocator do the correct allocation.");
                    }
                }
            }

            if (m_CustomPassAOVBuffers != null)
            {
                foreach (var aovBufferId in m_CustomPassAOVBuffers)
                {
                    var rtHandle = m_CustomPassBufferAllocator(aovBufferId);
                    customPassTextures.Add(rtHandle);

                    if (rtHandle == null)
                    {
                        Debug.LogError("Allocation for requested AOVBuffers ID: " + aovBufferId.ToString() + " have fail. Please ensure the callback for custom pass allocator do the correct allocation.");
                    }
                }
            }
        }

        internal void OverrideBufferFormatForAOVs(ref GraphicsFormat format, List<RTHandle> aovBuffers)
        {
            if (m_RequestedAOVBuffers == null || aovBuffers.Count == 0)
            {
                return;
            }

            var index = Array.IndexOf(m_RequestedAOVBuffers, AOVBuffers.Color);
            if (index < 0)
            {
                index = Array.IndexOf(m_RequestedAOVBuffers, AOVBuffers.Output);
            }
            if (index >= 0)
            {
                format = aovBuffers[index].rt.graphicsFormat;
            }
        }

        class PushCameraTexturePassData
        {
            public TextureHandle source;
            // Not super clean to not use TextureHandles here. In practice it's ok because those texture are never passed back to any other render pass.
            public RTHandle target;
        }

        internal void PushCameraTexture(
            RenderGraph renderGraph,
            AOVBuffers aovBufferId,
            HDCamera camera,
            TextureHandle source,
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

            using (var builder = renderGraph.AddRenderPass<PushCameraTexturePassData>("Push AOV Camera Texture", out var passData, ProfilingSampler.Get(HDProfileId.AOVOutput + (int)aovBufferId)))
            {
                passData.source = builder.ReadTexture(source);
                passData.target = targets[index];

                builder.SetRenderFunc(
                    (PushCameraTexturePassData data, RenderGraphContext ctx) =>
                    {
                        HDUtils.BlitCameraTexture(ctx.cmd, data.source, data.target);
                    });
            }
        }

        class PushCustomPassTexturePassData
        {
            public TextureHandle source;
            public RTHandle customPassSource;
            // Not super clean to not use TextureHandles here. In practice it's ok because those texture are never passed back to any other render pass.
            public RTHandle target;
        }

        internal void PushCustomPassTexture(
            RenderGraph renderGraph,
            CustomPassInjectionPoint injectionPoint,
            TextureHandle cameraSource,
            Lazy<RTHandle> customPassSource,
            List<RTHandle> targets
        )
        {
            if (!isValid || m_CustomPassAOVBuffers == null)
                return;

            Assert.IsNotNull(targets);

            int index = -1;
            for (int i = 0; i < m_CustomPassAOVBuffers.Length; ++i)
            {
                if (m_CustomPassAOVBuffers[i].injectionPoint == injectionPoint)
                {
                    index = i;
                    break;
                }
            }

            if (index == -1)
                return;

            using (var builder = renderGraph.AddRenderPass<PushCustomPassTexturePassData>("Push Custom Pass Texture", out var passData))
            {
                if (m_CustomPassAOVBuffers[index].outputType == CustomPassAOVBuffers.OutputType.Camera)
                {
                    passData.source = builder.ReadTexture(cameraSource);
                    passData.customPassSource = null;
                }
                else
                {
                    passData.customPassSource = customPassSource.Value;
                }
                passData.target = targets[index];

                builder.SetRenderFunc(
                    (PushCustomPassTexturePassData data, RenderGraphContext ctx) =>
                    {
                        if (data.customPassSource != null)
                            HDUtils.BlitCameraTexture(ctx.cmd, data.customPassSource, data.target);
                        else
                            HDUtils.BlitCameraTexture(ctx.cmd, data.source, data.target);
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

        /// <summary>Execute the frame pass callback. It assumes that the textures are properly initialized and filled.</summary>
        /// <param name="cmd">The command buffer to use.</param>
        /// <param name="framePassTextures">The textures to use.</param>
        /// <param name="customPassTextures">The custom pass AOV textures to use.</param>
        /// <param name="outputProperties">The properties computed for this frame.</param>
        public void Execute(CommandBuffer cmd, List<RTHandle> framePassTextures, List<RTHandle> customPassTextures, RenderOutputProperties outputProperties)
        {
            if (!isValid)
                return;

            if (m_CallbackEx != null)
            {
                m_CallbackEx(cmd, framePassTextures, customPassTextures, outputProperties);
            }
            else
            {
                m_Callback(cmd, framePassTextures, outputProperties);
            }
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

        internal bool hasLightFilter => m_LightFilter != null;

        internal int GetHash()
        {
            int hash = m_Settings.GetHashCode();

            if (m_LightFilter != null)
            {
                foreach (var obj in m_LightFilter)
                {
                    hash += obj.GetHashCode();
                }
            }

            return hash;
        }

        internal bool HasSameSettings(AOVRequestData other)
        {
            if (m_Settings != other.m_Settings)
                return false;

            if (m_LightFilter != null)
                return m_LightFilter.Equals(other.m_LightFilter);

            return true;
        }
    }

    internal class AOVRequestDataComparer : IEqualityComparer<AOVRequestData>
    {
        public bool Equals(AOVRequestData x, AOVRequestData y)
        {
            return x.HasSameSettings(y);
        }

        public int GetHashCode(AOVRequestData obj)
        {
            return obj.GetHash();
        }
    }
}
