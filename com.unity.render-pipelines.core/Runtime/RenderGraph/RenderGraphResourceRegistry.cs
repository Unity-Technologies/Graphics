using System;
using System.Collections.Generic;
using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering.RenderGraphModule
{
    /// <summary>
    /// The mode that determines the size of a Texture.
    /// </summary>
    #region Resource Descriptors
    public enum TextureSizeMode
    {
        ///<summary>Explicit size.</summary>
        Explicit,
        ///<summary>Size automatically scaled by a Vector.</summary>
        Scale,
        ///<summary>Size automatically scaled by a Functor.</summary>
        Functor
    }

    /// <summary>
    /// Descriptor used to create texture resources
    /// </summary>
    public struct TextureDesc
    {
        ///<summary>Texture sizing mode.</summary>
        public TextureSizeMode sizeMode;
        ///<summary>Texture width.</summary>
        public int width;
        ///<summary>Texture height.</summary>
        public int height;
        ///<summary>Number of texture slices..</summary>
        public int slices;
        ///<summary>Texture scale.</summary>
        public Vector2 scale;
        ///<summary>Texture scale function.</summary>
        public ScaleFunc func;
        ///<summary>Depth buffer bit depth.</summary>
        public DepthBits depthBufferBits;
        ///<summary>Color format.</summary>
        public GraphicsFormat colorFormat;
        ///<summary>Filtering mode.</summary>
        public FilterMode filterMode;
        ///<summary>Addressing mode.</summary>
        public TextureWrapMode wrapMode;
        ///<summary>Texture dimension.</summary>
        public TextureDimension dimension;
        ///<summary>Enable random UAV read/write on the texture.</summary>
        public bool enableRandomWrite;
        ///<summary>Texture needs mip maps.</summary>
        public bool useMipMap;
        ///<summary>Automatically generate mip maps.</summary>
        public bool autoGenerateMips;
        ///<summary>Texture is a shadow map.</summary>
        public bool isShadowMap;
        ///<summary>Anisotropic filtering level.</summary>
        public int anisoLevel;
        ///<summary>Mip map bias.</summary>
        public float mipMapBias;
        ///<summary>Textre is multisampled. Only supported for Scale and Functor size mode.</summary>
        public bool enableMSAA;
        ///<summary>Number of MSAA samples. Only supported for Explicit size mode.</summary>
        public MSAASamples msaaSamples;
        ///<summary>Bind texture multi sampled.</summary>
        public bool bindTextureMS;
        ///<summary>Texture uses dynamic scaling.</summary>
        public bool useDynamicScale;
        ///<summary>Memory less flag.</summary>
        public RenderTextureMemoryless memoryless;
        ///<summary>Texture name.</summary>
        public string name;

        // Initial state. Those should not be used in the hash
        ///<summary>Texture needs to be cleared on first use.</summary>
        public bool clearBuffer;
        ///<summary>Clear color.</summary>
        public Color clearColor;

        void InitDefaultValues(bool dynamicResolution, bool xrReady)
        {
            useDynamicScale = dynamicResolution;
            // XR Ready
            if (xrReady)
            {
                slices = TextureXR.slices;
                dimension = TextureXR.dimension;
            }
            else
            {
                slices = 1;
                dimension = TextureDimension.Tex2D;
            }
        }

        /// <summary>
        /// TextureDesc constructor for a texture using explicit size
        /// </summary>
        /// <param name="width">Texture width</param>
        /// <param name="height">Texture height</param>
        /// <param name="dynamicResolution">Use dynamic resolution</param>
        /// <param name="xrReady">Set this to true if the Texture is a render texture in an XR setting.</param>
        public TextureDesc(int width, int height, bool dynamicResolution = false, bool xrReady = false)
            : this()
        {
            // Size related init
            sizeMode = TextureSizeMode.Explicit;
            this.width = width;
            this.height = height;
            // Important default values not handled by zero construction in this()
            msaaSamples = MSAASamples.None;
            InitDefaultValues(dynamicResolution, xrReady);
        }

        /// <summary>
        /// TextureDesc constructor for a texture using a fixed scaling
        /// </summary>
        /// <param name="scale">RTHandle scale used for this texture</param>
        /// <param name="dynamicResolution">Use dynamic resolution</param>
        /// <param name="xrReady">Set this to true if the Texture is a render texture in an XR setting.</param>
        public TextureDesc(Vector2 scale, bool dynamicResolution = false, bool xrReady = false)
            : this()
        {
            // Size related init
            sizeMode = TextureSizeMode.Scale;
            this.scale = scale;
            // Important default values not handled by zero construction in this()
            msaaSamples = MSAASamples.None;
            dimension = TextureDimension.Tex2D;
            InitDefaultValues(dynamicResolution, xrReady);
        }

        /// <summary>
        /// TextureDesc constructor for a texture using a functor for scaling
        /// </summary>
        /// <param name="func">Function used to determnine the texture size</param>
        /// <param name="dynamicResolution">Use dynamic resolution</param>
        /// <param name="xrReady">Set this to true if the Texture is a render texture in an XR setting.</param>
        public TextureDesc(ScaleFunc func, bool dynamicResolution = false, bool xrReady = false)
            : this()
        {
            // Size related init
            sizeMode = TextureSizeMode.Functor;
            this.func = func;
            // Important default values not handled by zero construction in this()
            msaaSamples = MSAASamples.None;
            dimension = TextureDimension.Tex2D;
            InitDefaultValues(dynamicResolution, xrReady);
        }

        /// <summary>
        /// Copy constructor
        /// </summary>
        /// <param name="input"></param>
        public TextureDesc(TextureDesc input)
        {
            this = input;
        }

        /// <summary>
        /// Hash function
        /// </summary>
        /// <returns>The texture descriptor hash.</returns>
        public override int GetHashCode()
        {
            int hashCode = 17;

            unchecked
            {
                switch (sizeMode)
                {
                    case TextureSizeMode.Explicit:
                        hashCode = hashCode * 23 + width;
                        hashCode = hashCode * 23 + height;
                        hashCode = hashCode * 23 + (int)msaaSamples;
                        break;
                    case TextureSizeMode.Functor:
                        if (func != null)
                            hashCode = hashCode * 23 + func.GetHashCode();
                        hashCode = hashCode * 23 + (enableMSAA ? 1 : 0);
                        break;
                    case TextureSizeMode.Scale:
                        hashCode = hashCode * 23 + scale.x.GetHashCode();
                        hashCode = hashCode * 23 + scale.y.GetHashCode();
                        hashCode = hashCode * 23 + (enableMSAA ? 1 : 0);
                        break;
                }

                hashCode = hashCode * 23 + mipMapBias.GetHashCode();
                hashCode = hashCode * 23 + slices;
                hashCode = hashCode * 23 + (int)depthBufferBits;
                hashCode = hashCode * 23 + (int)colorFormat;
                hashCode = hashCode * 23 + (int)filterMode;
                hashCode = hashCode * 23 + (int)wrapMode;
                hashCode = hashCode * 23 + (int)dimension;
                hashCode = hashCode * 23 + (int)memoryless;
                hashCode = hashCode * 23 + anisoLevel;
                hashCode = hashCode * 23 + (enableRandomWrite ? 1 : 0);
                hashCode = hashCode * 23 + (useMipMap ? 1 : 0);
                hashCode = hashCode * 23 + (autoGenerateMips ? 1 : 0);
                hashCode = hashCode * 23 + (isShadowMap ? 1 : 0);
                hashCode = hashCode * 23 + (bindTextureMS ? 1 : 0);
                hashCode = hashCode * 23 + (useDynamicScale ? 1 : 0);
            }

            return hashCode;
        }
    }
    #endregion

    /// <summary>
    /// The RenderGraphResourceRegistry holds all resource allocated during Render Graph execution.
    /// </summary>
    public class RenderGraphResourceRegistry
    {
        static readonly ShaderTagId s_EmptyName = new ShaderTagId("");

        #region Resources
        internal struct TextureResource
        {
            public TextureDesc  desc;
            public bool         imported;
            public RTHandle     rt;
            public int          cachedHash;
            public int          firstWritePassIndex;
            public int          lastReadPassIndex;
            public int          shaderProperty;
            public bool         wasReleased;

            internal TextureResource(RTHandle rt, int shaderProperty)
                : this()
            {
                Reset();

                this.rt = rt;
                imported = true;
                this.shaderProperty = shaderProperty;
            }

            internal TextureResource(in TextureDesc desc, int shaderProperty)
                : this()
            {
                Reset();

                this.desc = desc;
                this.shaderProperty = shaderProperty;
            }

            void Reset()
            {
                imported = false;
                rt = null;
                cachedHash = -1;
                firstWritePassIndex = int.MaxValue;
                lastReadPassIndex = -1;
                wasReleased = false;
            }
        }

        internal struct RendererListResource
        {
            public RendererListDesc desc;
            public RendererList     rendererList;

            internal RendererListResource(in RendererListDesc desc)
            {
                this.desc = desc;
                this.rendererList = new RendererList(); // Invalid by default
            }
        }
        #endregion
        DynamicArray<TextureResource>       m_TextureResources = new DynamicArray<TextureResource>();
        Dictionary<int, Stack<RTHandle>>    m_TexturePool = new Dictionary<int, Stack<RTHandle>>();
        DynamicArray<RendererListResource>  m_RendererListResources = new DynamicArray<RendererListResource>();
        RTHandleSystem                      m_RTHandleSystem = new RTHandleSystem();
        RenderGraphDebugParams              m_RenderGraphDebug;
        RenderGraphLogger                   m_Logger;

        // Diagnostic only
        List<(int, RTHandle)>               m_AllocatedTextures = new List<(int, RTHandle)>();

        #region Public Interface
        /// <summary>
        /// Returns the RTHandle associated with the provided resource handle.
        /// </summary>
        /// <param name="handle">Handle to a texture resource.</param>
        /// <returns>The RTHandle associated with the provided resource handle.</returns>
        public RTHandle GetTexture(in RenderGraphResource handle)
        {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            if (handle.type != RenderGraphResourceType.Texture)
                throw new InvalidOperationException("Trying to access a RenderGraphResource that is not a texture.");

            var res = m_TextureResources[handle.handle];

            if (res.rt == null && !res.wasReleased)
                throw new InvalidOperationException(string.Format("Trying to access texture \"{0}\" that was never created. Check that it was written at least once before trying to get it.", res.desc.name));

            if (res.rt == null && res.wasReleased)
                throw new InvalidOperationException(string.Format("Trying to access texture \"{0}\" that was already released. Check that the last pass where it's read is after this one.", res.desc.name));
#endif
            return m_TextureResources[handle.handle].rt;
        }

        /// <summary>
        /// Returns the RendererList associated with the provided resource handle.
        /// </summary>
        /// <param name="handle">Handle to a Renderer List resource.</param>
        /// <returns>The Renderer List associated with the provided resource handle.</returns>
        public RendererList GetRendererList(in RenderGraphResource handle)
        {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            if (handle.type != RenderGraphResourceType.RendererList)
                throw new InvalidOperationException("Trying to access a RenderGraphResource that is not a RendererList.");
#endif
            return m_RendererListResources[handle.handle].rendererList;
        }
        #endregion

        #region Internal Interface
        private RenderGraphResourceRegistry()
        {

        }

        internal RenderGraphResourceRegistry(bool supportMSAA, MSAASamples initialSampleCount, RenderGraphDebugParams renderGraphDebug, RenderGraphLogger logger)
        {
            // We initialize to screen width/height to avoid multiple realloc that can lead to inflated memory usage (as releasing of memory is delayed).
            m_RTHandleSystem.Initialize(Screen.width, Screen.height, supportMSAA, initialSampleCount);
            m_RenderGraphDebug = renderGraphDebug;
            m_Logger = logger;
        }

        internal void SetRTHandleReferenceSize(int width, int height, MSAASamples msaaSamples)
        {
            m_RTHandleSystem.SetReferenceSize(width, height, msaaSamples);
        }

        internal RTHandleProperties GetRTHandleProperties() { return m_RTHandleSystem.rtHandleProperties; }

        // Texture Creation/Import APIs are internal because creation should only go through RenderGraph
        internal RenderGraphMutableResource ImportTexture(RTHandle rt, int shaderProperty = 0)
        {
            int newHandle = m_TextureResources.Add(new TextureResource(rt, shaderProperty));
            return new RenderGraphMutableResource(newHandle, RenderGraphResourceType.Texture);
        }

        internal RenderGraphMutableResource CreateTexture(in TextureDesc desc, int shaderProperty = 0)
        {
            ValidateTextureDesc(desc);

            int newHandle = m_TextureResources.Add(new TextureResource(desc, shaderProperty));
            return new RenderGraphMutableResource(newHandle, RenderGraphResourceType.Texture);
        }

        internal void UpdateTextureFirstWrite(RenderGraphResource tex, int passIndex)
        {
            ref var res = ref GetTextureResource(tex);
            res.firstWritePassIndex = Math.Min(passIndex, res.firstWritePassIndex);

            //// We increment lastRead index here so that a resource used only for a single pass can be released at the end of said pass.
            //// This will also keep the resource alive as long as it is written to.
            //// Typical example is a depth buffer that may never be explicitly read from but is necessary all along
            ///
            // PROBLEM: Increasing last read on write operation will keep the target alive even if it's not used at all so it's not good.
            // If we don't do it though, it means that client code cannot write "by default" into a target as it will try to write to an already released target.
            // Example:
            // DepthPrepass: Writes to Depth and Normal buffers (pass will create normal buffer)
            // ObjectMotion: Writes to MotionVectors and Normal => Exception because NormalBuffer is already released as it not used.
            // => Solution includes : Shader Combination (without MRT for example) / Dummy Targets
            //res.lastReadPassIndex = Math.Max(passIndex, res.lastReadPassIndex);
        }

        internal void UpdateTextureLastRead(RenderGraphResource tex, int passIndex)
        {
            ref var res = ref GetTextureResource(tex);
            res.lastReadPassIndex = Math.Max(passIndex, res.lastReadPassIndex);
        }

        ref TextureResource GetTextureResource(RenderGraphResource res)
        {
            return ref m_TextureResources[res.handle];
        }

        internal TextureDesc GetTextureResourceDesc(RenderGraphResource res)
        {
            return m_TextureResources[res.handle].desc;
        }

        internal RenderGraphResource CreateRendererList(in RendererListDesc desc)
        {
            ValidateRendererListDesc(desc);

            int newHandle = m_RendererListResources.Add(new RendererListResource(desc));
            return new RenderGraphResource(newHandle, RenderGraphResourceType.RendererList);
        }

        internal void CreateAndClearTexturesForPass(RenderGraphContext rgContext, int passIndex, List<RenderGraphMutableResource> textures)
        {
            foreach (var rgResource in textures)
            {
                ref var resource = ref GetTextureResource(rgResource);
                if (!resource.imported && resource.firstWritePassIndex == passIndex)
                {
                    CreateTextureForPass(ref resource);

                    if (resource.desc.clearBuffer || m_RenderGraphDebug.clearRenderTargetsAtCreation)
                    {
                        bool debugClear = m_RenderGraphDebug.clearRenderTargetsAtCreation && !resource.desc.clearBuffer;
                        var name = debugClear ? "RenderGraph: Clear Buffer (Debug)" : "RenderGraph: Clear Buffer";
                        using (new ProfilingScope(rgContext.cmd, ProfilingSampler.Get(RenderGraphProfileId.RenderGraphClear)))
                        {
                            var clearFlag = resource.desc.depthBufferBits != DepthBits.None ? ClearFlag.Depth : ClearFlag.Color;
                            var clearColor = debugClear ? Color.magenta : resource.desc.clearColor;
                            CoreUtils.SetRenderTarget(rgContext.cmd, resource.rt, clearFlag, clearColor);
                        }
                    }

                    LogTextureCreation(resource.rt, resource.desc.clearBuffer || m_RenderGraphDebug.clearRenderTargetsAtCreation);
                }
            }
        }

        void CreateTextureForPass(ref TextureResource resource)
        {
            var desc = resource.desc;
            int hashCode = desc.GetHashCode();

            if(resource.rt != null)
                throw new InvalidOperationException(string.Format("Trying to create an already created texture ({0}). Texture was probably declared for writing more than once.", resource.desc.name));

            resource.rt = null;
            if (!TryGetRenderTarget(hashCode, out resource.rt))
            {
                // Note: Name used here will be the one visible in the memory profiler so it means that whatever is the first pass that actually allocate the texture will set the name.
                // TODO: Find a way to display name by pass.
                switch (desc.sizeMode)
                {
                    case TextureSizeMode.Explicit:
                        resource.rt = m_RTHandleSystem.Alloc(desc.width, desc.height, desc.slices, desc.depthBufferBits, desc.colorFormat, desc.filterMode, desc.wrapMode, desc.dimension, desc.enableRandomWrite,
                        desc.useMipMap, desc.autoGenerateMips, desc.isShadowMap, desc.anisoLevel, desc.mipMapBias, desc.msaaSamples, desc.bindTextureMS, desc.useDynamicScale, desc.memoryless, desc.name);
                        break;
                    case TextureSizeMode.Scale:
                        resource.rt = m_RTHandleSystem.Alloc(desc.scale, desc.slices, desc.depthBufferBits, desc.colorFormat, desc.filterMode, desc.wrapMode, desc.dimension, desc.enableRandomWrite,
                        desc.useMipMap, desc.autoGenerateMips, desc.isShadowMap, desc.anisoLevel, desc.mipMapBias, desc.enableMSAA, desc.bindTextureMS, desc.useDynamicScale, desc.memoryless, desc.name);
                        break;
                    case TextureSizeMode.Functor:
                        resource.rt = m_RTHandleSystem.Alloc(desc.func, desc.slices, desc.depthBufferBits, desc.colorFormat, desc.filterMode, desc.wrapMode, desc.dimension, desc.enableRandomWrite,
                        desc.useMipMap, desc.autoGenerateMips, desc.isShadowMap, desc.anisoLevel, desc.mipMapBias, desc.enableMSAA, desc.bindTextureMS, desc.useDynamicScale, desc.memoryless, desc.name);
                        break;
                }
            }

            //// Try to update name when re-using a texture.
            //// TODO: Check if that actually works.
            //resource.rt.name = desc.name;

#if DEVELOPMENT_BUILD || UNITY_EDITOR
            if (hashCode != -1)
            {
                m_AllocatedTextures.Add((hashCode, resource.rt));
            }
#endif

            resource.cachedHash = hashCode;
        }

        void SetGlobalTextures(RenderGraphContext rgContext, List<RenderGraphResource> textures, bool bindDummyTexture)
        {
            foreach (var resource in textures)
            {
                var resourceDesc = GetTextureResource(resource);
                if (resourceDesc.shaderProperty != 0)
                {
                    if (resourceDesc.rt == null)
                    {
                        throw new InvalidOperationException(string.Format("Trying to set Global Texture parameter for \"{0}\" which was never created.\nCheck that at least one write operation happens before reading it.", resourceDesc.desc.name));
                    }
                    rgContext.cmd.SetGlobalTexture(resourceDesc.shaderProperty, bindDummyTexture ? TextureXR.GetMagentaTexture() : resourceDesc.rt);
                }
            }
        }


        internal void PreRenderPassSetGlobalTextures(RenderGraphContext rgContext, List<RenderGraphResource> textures)
        {
            SetGlobalTextures(rgContext, textures, false);
        }

        internal void PostRenderPassUnbindGlobalTextures(RenderGraphContext rgContext, List<RenderGraphResource> textures)
        {
            SetGlobalTextures(rgContext, textures, true);
        }

        internal void ReleaseTexturesForPass(RenderGraphContext rgContext, int passIndex, List<RenderGraphResource> readTextures, List<RenderGraphMutableResource> writtenTextures)
        {
            foreach (var resource in readTextures)
            {
                ref var resourceDesc = ref GetTextureResource(resource);
                if (!resourceDesc.imported && resourceDesc.lastReadPassIndex == passIndex)
                {
                    if (m_RenderGraphDebug.clearRenderTargetsAtRelease)
                    {
                        using (new ProfilingScope(rgContext.cmd, ProfilingSampler.Get(RenderGraphProfileId.RenderGraphClearDebug)))
                        {
                            var clearFlag = resourceDesc.desc.depthBufferBits != DepthBits.None ? ClearFlag.Depth : ClearFlag.Color;
                            CoreUtils.SetRenderTarget(rgContext.cmd, GetTexture(resource), clearFlag, Color.magenta);
                        }
                    }

                    ReleaseTextureForPass(resource);
                }
            }

            // If a resource was created for only a single pass, we don't want users to have to declare explicitly the read operation.
            // So to do that, we also update lastReadIndex on resource writes.
            // This means that we need to check written resources for destruction too
            foreach (var resource in writtenTextures)
            {
                ref var resourceDesc = ref GetTextureResource(resource);
                // <= because a texture that is only declared as written in a single pass (and read implicitly in the same pass) will have the default lastReadPassIndex at -1
                if (!resourceDesc.imported && resourceDesc.lastReadPassIndex <= passIndex)
                {
                    ReleaseTextureForPass(resource);
                }
            }
        }

        void ReleaseTextureForPass(RenderGraphResource res)
        {
            Debug.Assert(res.type == RenderGraphResourceType.Texture);

            ref var resource = ref m_TextureResources[res.handle];

            // This can happen because we release texture in two passes (see ReleaseTexturesForPass) and texture can be present in both passes
            if (resource.rt != null)
            {
                LogTextureRelease(resource.rt);
                ReleaseTextureResource(resource.cachedHash, resource.rt);
                resource.cachedHash = -1;
                resource.rt = null;
                resource.wasReleased = true;
            }
        }

        void ReleaseTextureResource(int hash, RTHandle rt)
        {
            if (!m_TexturePool.TryGetValue(hash, out var stack))
            {
                stack = new Stack<RTHandle>();
                m_TexturePool.Add(hash, stack);
            }

            stack.Push(rt);

#if DEVELOPMENT_BUILD || UNITY_EDITOR
            m_AllocatedTextures.Remove((hash, rt));
#endif
        }

        void ValidateTextureDesc(in TextureDesc desc)
        {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            if (desc.colorFormat == GraphicsFormat.None && desc.depthBufferBits == DepthBits.None)
            {
                throw new ArgumentException("Texture was created with an invalid color format.");
            }

            if (desc.dimension == TextureDimension.None)
            {
                throw new ArgumentException("Texture was created with an invalid texture dimension.");
            }

            if (desc.slices == 0)
            {
                throw new ArgumentException("Texture was created with a slices parameter value of zero.");
            }

            if (desc.sizeMode == TextureSizeMode.Explicit)
            {
                if (desc.width == 0 || desc.height == 0)
                    throw new ArgumentException("Texture using Explicit size mode was create with either width or height at zero.");
                if (desc.enableMSAA)
                    throw new ArgumentException("enableMSAA TextureDesc parameter is not supported for textures using Explicit size mode.");
            }

            if (desc.sizeMode == TextureSizeMode.Scale || desc.sizeMode == TextureSizeMode.Functor)
            {
                if (desc.msaaSamples != MSAASamples.None)
                    throw new ArgumentException("msaaSamples TextureDesc parameter is not supported for textures using Scale or Functor size mode.");
            }
#endif
        }

        void ValidateRendererListDesc(in RendererListDesc desc)
        {
#if DEVELOPMENT_BUILD || UNITY_EDITOR

            if (desc.passName != ShaderTagId.none && desc.passNames != null
                || desc.passName == ShaderTagId.none && desc.passNames == null)
            {
                throw new ArgumentException("Renderer List creation descriptor must contain either a single passName or an array of passNames.");
            }

            if (desc.renderQueueRange.lowerBound == 0 && desc.renderQueueRange.upperBound == 0)
            {
                throw new ArgumentException("Renderer List creation descriptor must have a valid RenderQueueRange.");
            }

            if (desc.camera == null)
            {
                throw new ArgumentException("Renderer List creation descriptor must have a valid Camera.");
            }
#endif
        }

        bool TryGetRenderTarget(int hashCode, out RTHandle rt)
        {
            if (m_TexturePool.TryGetValue(hashCode, out var stack) && stack.Count > 0)
            {
                rt = stack.Pop();
                return true;
            }

            rt = null;
            return false;
        }

        internal void CreateRendererLists(List<RenderGraphResource> rendererLists)
        {
            // For now we just create a simple structure
            // but when the proper API is available in trunk we'll kick off renderer lists creation jobs here.
            foreach (var rendererList in rendererLists)
            {
                Debug.Assert(rendererList.type == RenderGraphResourceType.RendererList);

                ref var rendererListResource = ref m_RendererListResources[rendererList.handle];
                ref var desc = ref rendererListResource.desc;
                RendererList newRendererList = RendererList.Create(desc);
                rendererListResource.rendererList = newRendererList;
            }
        }

        internal void Clear()
        {
            LogResources();

            m_TextureResources.Clear();
            m_RendererListResources.Clear();

#if DEVELOPMENT_BUILD || UNITY_EDITOR
            if (m_AllocatedTextures.Count != 0)
            {
                Debug.LogWarning("RenderGraph: Not all textures were released.");
                List<(int, RTHandle)> tempList = new List<(int, RTHandle)>(m_AllocatedTextures);
                foreach (var value in tempList)
                {
                    ReleaseTextureResource(value.Item1, value.Item2);
                }
            }
#endif
        }

        internal void Cleanup()
        {
            foreach (var value in m_TexturePool)
            {
                foreach (var rt in value.Value)
                {
                    m_RTHandleSystem.Release(rt);
                }
            }
        }

        void LogTextureCreation(RTHandle rt, bool cleared)
        {
            if (m_RenderGraphDebug.logFrameInformation)
            {
                m_Logger.LogLine("Created Texture: {0} (Cleared: {1})", rt.rt.name, cleared);
            }
        }

        void LogTextureRelease(RTHandle rt)
        {
            if (m_RenderGraphDebug.logFrameInformation)
            {
                m_Logger.LogLine("Released Texture: {0}", rt.rt.name);
            }
        }

        void LogResources()
        {
            if (m_RenderGraphDebug.logResources)
            {
                m_Logger.LogLine("==== Allocated Resources ====\n");

                List<string> allocationList = new List<string>();
                foreach (var stack in m_TexturePool)
                {
                    foreach (var rt in stack.Value)
                    {
                        allocationList.Add(rt.rt.name);
                    }
                }

                allocationList.Sort();
                int index = 0;
                foreach (var element in allocationList)
                    m_Logger.LogLine("[{0}] {1}", index++, element);
            }
        }

        #endregion
    }
}
