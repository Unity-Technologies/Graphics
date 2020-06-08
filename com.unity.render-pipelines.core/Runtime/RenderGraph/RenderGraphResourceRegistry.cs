using System;
using System.Diagnostics;
using System.Collections.Generic;
using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering.RenderGraphModule
{
    // RendererList is a different case so not represented here.
    internal enum RenderGraphResourceType
    {
        Texture = 0,
        ComputeBuffer,
        Count
    }

    #region Resource Descriptors
    // BEHOLD C# COPY PASTA
    // Struct can't be inherited and can't have default member values
    // Hence the copy paste and the ugly IsValid implementation.

    /// <summary>
    /// Texture resource handle.
    /// </summary>
    [DebuggerDisplay("Texture ({handle})")]
    public struct TextureHandle
    {
        bool m_IsValid;
        internal int handle { get; private set; }
        internal int transientPassIndex { get; private set; }
        internal TextureHandle(int handle, int transientPassIndex = -1) { this.handle = handle; m_IsValid = true; this.transientPassIndex = transientPassIndex; }
        /// <summary>
        /// Conversion to int.
        /// </summary>
        /// <param name="handle">Texture handle to convert.</param>
        /// <returns>The integer representation of the handle.</returns>
        public static implicit operator int(TextureHandle handle) { return handle.handle; }
        /// <summary>
        /// Return true if the handle is valid.
        /// </summary>
        /// <returns>True if the handle is valid.</returns>
        public bool IsValid() => m_IsValid;

        /// <summary>
        /// Equals Override.
        /// </summary>
        /// <param name="obj">Other handle to test against.</param>
        /// <returns>True if both handle are equals.</returns>
        public override bool Equals(System.Object obj)
        {
            //Check for null and compare run-time types.
            if ((obj == null) || !this.GetType().Equals(obj.GetType()))
            {
                return false;
            }
            else
            {
                TextureHandle texture = (TextureHandle)obj;
                return texture.handle == handle && texture.m_IsValid == m_IsValid;
            }
        }

        /// <summary>
        /// GetHashCode override.
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            return (handle << 2) ^ (m_IsValid ? 333 : 444);
        }
    }

    /// <summary>
    /// Renderer List resource handle.
    /// </summary>
    [DebuggerDisplay("RendererList ({handle})")]
    public struct RendererListHandle
    {
        bool m_IsValid;
        internal int handle { get; private set; }
        internal RendererListHandle(int handle) { this.handle = handle; m_IsValid = true; }
        /// <summary>
        /// Conversion to int.
        /// </summary>
        /// <param name="handle">Renderer List handle to convert.</param>
        /// <returns>The integer representation of the handle.</returns>
        public static implicit operator int(RendererListHandle handle) { return handle.handle; }
        /// <summary>
        /// Return true if the handle is valid.
        /// </summary>
        /// <returns>True if the handle is valid.</returns>
        public bool IsValid() => m_IsValid;
    }

    /// <summary>
    /// Compute Buffer resource handle.
    /// </summary>
    [DebuggerDisplay("ComputeBuffer ({handle})")]
    public struct ComputeBufferHandle
    {
        bool m_IsValid;
        internal int handle { get; private set; }
        internal ComputeBufferHandle(int handle) { this.handle = handle; m_IsValid = true; }
        /// <summary>
        /// Conversion to int.
        /// </summary>
        /// <param name="handle">Compute Buffer handle to convert.</param>
        /// <returns>The integer representation of the handle.</returns>
        public static implicit operator int(ComputeBufferHandle handle) { return handle.handle; }
        /// <summary>
        /// Return true if the handle is valid.
        /// </summary>
        /// <returns>True if the handle is valid.</returns>
        public bool IsValid() => m_IsValid;
    }

    /// <summary>
    /// The mode that determines the size of a Texture.
    /// </summary>
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
    /// Subset of the texture desc containing information for fast memory allocation (when platform supports it)
    /// </summary>
    public struct FastMemoryDesc
    {
        ///<summary>Whether the texture will be in fast memory.</summary>
        public bool inFastMemory;
        ///<summary>Flag to determine what parts of the render target is spilled if not fully resident in fast memory.</summary>
        public FastMemoryFlags flags;
        ///<summary>How much of the render target is to be switched into fast memory (between 0 and 1).</summary>
        public float residencyFraction;
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
        ///<summary>Descriptor to determine how the texture will be in fast memory on platform that supports it.</summary>
        public FastMemoryDesc fastMemoryDesc;

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

    /// <summary>
    /// Descriptor used to create compute buffer resources
    /// </summary>
    public struct ComputeBufferDesc
    {
        ///<summary>Number of elements in the buffer..</summary>
        public int                  count;
        ///<summary>Size of one element in the buffer. Has to match size of buffer type in the shader.</summary>
        public int                  stride;
        ///<summary>Type of the buffer, default is ComputeBufferType.Default (structured buffer).</summary>
        public ComputeBufferType    type;
        /// <summary>Compute Buffer name.</summary>
        public string               name;

        /// <summary>
        /// ComputeBufferDesc constructor.
        /// </summary>
        /// <param name="count">Number of elements in the buffer.</param>
        /// <param name="stride">Size of one element in the buffer.</param>
        public ComputeBufferDesc(int count, int stride)
            : this()
        {
            this.count = count;
            this.stride = stride;
            type = ComputeBufferType.Default;
        }

        /// <summary>
        /// ComputeBufferDesc constructor.
        /// </summary>
        /// <param name="count">Number of elements in the buffer.</param>
        /// <param name="stride">Size of one element in the buffer.</param>
        /// <param name="type">Type of the buffer.</param>
        public ComputeBufferDesc(int count, int stride, ComputeBufferType type)
            : this()
        {
            this.count = count;
            this.stride = stride;
            this.type = type;
        }

        /// <summary>
        /// Hash function
        /// </summary>
        /// <returns>The texture descriptor hash.</returns>
        public override int GetHashCode()
        {
            int hashCode = 17;

            hashCode = hashCode * 23 + count;
            hashCode = hashCode * 23 + stride;
            hashCode = hashCode * 23 + (int)type;

            return hashCode;
        }
    }
    #endregion

    class RenderGraphTexturePool
    {
        // Dictionary tracks resources by hash and stores resources with same hash in a List (list instead of a stack because we need to be able to remove stale allocations).
        Dictionary<int, List<(RTHandle resource, int frameIndex)>> m_ResourcePool = new Dictionary<int, List<(RTHandle resource, int frameIndex)>>();
        static int s_CurrentFrameIndex;

        public void ReleaseResource(int hash, RTHandle rt, int currentFrameIndex)
        {
            if (!m_ResourcePool.TryGetValue(hash, out var list))
            {
                list = new List<(RTHandle rt, int frameIndex)>();
                m_ResourcePool.Add(hash, list);
            }

            list.Add((rt, currentFrameIndex));
        }

        public bool TryGetResource(int hashCode, out RTHandle rt)
        {
            if (m_ResourcePool.TryGetValue(hashCode, out var list) && list.Count > 0)
            {
                rt = list[list.Count - 1].resource;
                list.RemoveAt(list.Count - 1); // O(1) since it's the last element.
                return true;
            }

            rt = null;
            return false;
        }

        public void PurgeUnusedResources(int currentFrameIndex)
        {
            // Update the frame index for the lambda. Static because we don't want to capture.
            s_CurrentFrameIndex = currentFrameIndex;

            foreach(var kvp in m_ResourcePool)
            {
                var list = kvp.Value;
                list.RemoveAll(obj =>
                {
                    if (obj.frameIndex < s_CurrentFrameIndex)
                    {
                        obj.resource.Release();
                        return true;
                    }
                    return false;
                });
            }
        }

        public void Cleanup()
        {
            foreach (var kvp in m_ResourcePool)
            {
                foreach (var res in kvp.Value)
                {
                    res.resource.Release();
                }
            }
        }

        public void LogResources(RenderGraphLogger logger)
        {
            List<string> allocationList = new List<string>();
            foreach (var kvp in m_ResourcePool)
            {
                foreach (var res in kvp.Value)
                {
                    allocationList.Add(res.resource.rt.name);
                }
            }

            allocationList.Sort();
            int index = 0;
            foreach (var element in allocationList)
                logger.LogLine("[{0}] {1}", index++, element);
        }
    }

    /// <summary>
    /// The RenderGraphResourceRegistry holds all resource allocated during Render Graph execution.
    /// </summary>
    public class RenderGraphResourceRegistry
    {
        static readonly ShaderTagId s_EmptyName = new ShaderTagId("");

        class IRenderGraphResource
        {
            public bool imported;
            public int  cachedHash;
            public int  shaderProperty;
            public bool wasReleased;

            public virtual void Reset()
            {
                imported = false;
                cachedHash = -1;
                shaderProperty = 0;
                wasReleased = false;
            }
        }

        #region Resources
        class RenderGraphResource<DescType, ResType>
            : IRenderGraphResource
            where DescType : struct
            where ResType : class
        {
            public DescType desc;
            public ResType  resource;

            protected RenderGraphResource()
            {

            }

            //internal RenderGraphResource(ResType resource, int shaderProperty)
            //{
            //    Reset();

            //    this.resource = resource;
            //    imported = true;
            //    this.shaderProperty = shaderProperty;
            //}

            //internal RenderGraphResource(in DescType desc, int shaderProperty)
            //{
            //    Reset();

            //    this.desc = desc;
            //    this.shaderProperty = shaderProperty;
            //}

            public override void Reset()
            {
                base.Reset();
                resource = null;
            }

            public virtual string GetName()
            {
                return "";
            }
        }

        class TextureResource : RenderGraphResource<TextureDesc, RTHandle>
        {
            public override string GetName()
            {
                return desc.name;
            }
        }

        class ComputeBufferResource : RenderGraphResource<ComputeBufferDesc, ComputeBuffer>
        {
            public override string GetName()
            {
                return desc.name;
            }
        }

        //internal struct TextureResource
        //{
        //    public TextureDesc  desc;
        //    public bool         imported;
        //    public RTHandle     rt;
        //    public int          cachedHash;
        //    public int          shaderProperty;
        //    public bool         wasReleased;

        //    internal TextureResource(RTHandle rt, int shaderProperty)
        //        : this()
        //    {
        //        Reset();

        //        this.rt = rt;
        //        imported = true;
        //        this.shaderProperty = shaderProperty;
        //    }

        //    internal TextureResource(in TextureDesc desc, int shaderProperty)
        //        : this()
        //    {
        //        Reset();

        //        this.desc = desc;
        //        this.shaderProperty = shaderProperty;
        //    }

        //    void Reset()
        //    {
        //        imported = false;
        //        rt = null;
        //        cachedHash = -1;
        //        wasReleased = false;
        //    }
        //}

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

        //internal struct ComputeBufferResource
        //{
        //    public ComputeBufferDesc    desc;
        //    public ComputeBuffer        computeBuffer;
        //    public bool                 imported;
        //    public bool                 wasReleased;

        //    internal ComputeBufferResource(ComputeBuffer computeBuffer)
        //        : this()
        //    {
        //        this.computeBuffer = computeBuffer;
        //        this.imported = true;
        //        wasReleased = false;
        //    }

        //    internal ComputeBufferResource(in ComputeBufferDesc desc)
        //        : this()
        //    {
        //        this.desc = desc;
        //        computeBuffer = null;
        //        wasReleased = false;
        //    }
        //}
        #endregion

        //DynamicArray<TextureResource>       m_TextureResources = new DynamicArray<TextureResource>();
        //DynamicArray<ComputeBufferResource> m_ComputeBufferResources = new DynamicArray<ComputeBufferResource>();

        //DynamicArray<RenderGraphResource<TextureDesc, RTHandle>>            m_TextureResources = new DynamicArray<RenderGraphResource<TextureDesc, RTHandle>>();
        //DynamicArray<RenderGraphResource<ComputeBufferDesc, ComputeBuffer>> m_ComputeBufferResources = new DynamicArray<RenderGraphResource<ComputeBufferDesc, ComputeBuffer>>();

        DynamicArray<IRenderGraphResource>[] m_Resources = new DynamicArray<IRenderGraphResource>[(int)RenderGraphResourceType.Count];

        RenderGraphTexturePool              m_TexturePool = new RenderGraphTexturePool();
        DynamicArray<RendererListResource>  m_RendererListResources = new DynamicArray<RendererListResource>();
        RTHandleSystem                      m_RTHandleSystem = new RTHandleSystem();
        RenderGraphDebugParams              m_RenderGraphDebug;
        RenderGraphLogger                   m_Logger;
        int                                 m_CurrentFrameIndex;

        RTHandle                            m_CurrentBackbuffer;

        // Diagnostic only
        // This list allows us to determine if all textures were correctly released in the frame.
        List<(int, RTHandle)>               m_FrameAllocatedTextures = new List<(int, RTHandle)>();


        #region Public Interface
        /// <summary>
        /// Returns the RTHandle associated with the provided resource handle.
        /// </summary>
        /// <param name="handle">Handle to a texture resource.</param>
        /// <returns>The RTHandle associated with the provided resource handle or null if the handle is invalid.</returns>
        public RTHandle GetTexture(in TextureHandle handle)
        {
            if (!handle.IsValid())
                return null;

            return GetTextureResource(handle).resource;
        }

        /// <summary>
        /// Returns the RendererList associated with the provided resource handle.
        /// </summary>
        /// <param name="handle">Handle to a Renderer List resource.</param>
        /// <returns>The Renderer List associated with the provided resource handle or an invalid renderer list if the handle is invalid.</returns>
        public RendererList GetRendererList(in RendererListHandle handle)
        {
            if (!handle.IsValid() || handle >= m_RendererListResources.size)
                return RendererList.nullRendererList;

            return m_RendererListResources[handle].rendererList;
        }

        /// <summary>
        /// Returns the Compute Buffer associated with the provided resource handle.
        /// </summary>
        /// <param name="handle">Handle to a Compute Buffer resource.</param>
        /// <returns>The Compute Buffer associated with the provided resource handle or a null reference if the handle is invalid.</returns>
        public ComputeBuffer GetComputeBuffer(in ComputeBufferHandle handle)
        {
            if (!handle.IsValid())
                return null;

            return GetComputeBufferResource(handle).resource;
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

            for (int i = 0; i < (int)RenderGraphResourceType.Count; ++i)
                m_Resources[i] = new DynamicArray<IRenderGraphResource>();
        }

        ResType GetResource<DescType, ResType>(DynamicArray<IRenderGraphResource> resourceArray, int index)
            where DescType : struct
            where ResType : class
        {
            var res = resourceArray[index] as RenderGraphResource<DescType, ResType>;

#if DEVELOPMENT_BUILD || UNITY_EDITOR
            if (res.resource == null && !res.wasReleased)
                throw new InvalidOperationException(string.Format("Trying to access resource \"{0}\" that was never created. Check that it was written at least once before trying to get it.", res.GetName()));

            if (res.resource == null && res.wasReleased)
                throw new InvalidOperationException(string.Format("Trying to access resource \"{0}\" that was already released. Check that the last pass where it's read is after this one.", res.GetName()));
#endif
            return res.resource;
        }

        internal void BeginRender(int width, int height, MSAASamples msaaSamples, int currentFrameIndex)
        {
            m_CurrentFrameIndex = currentFrameIndex;
            // Update RTHandleSystem with size for this rendering pass.
            m_RTHandleSystem.SetReferenceSize(width, height, msaaSamples);
        }

        internal RTHandleProperties GetRTHandleProperties() { return m_RTHandleSystem.rtHandleProperties; }

        // Texture Creation/Import APIs are internal because creation should only go through RenderGraph
        internal TextureHandle ImportTexture(RTHandle rt, int shaderProperty = 0)
        {
            int newHandle = AddNewResource(m_Resources[(int)RenderGraphResourceType.Texture], out TextureResource texResource);
            texResource.resource = rt;
            texResource.imported = true;
            texResource.shaderProperty = shaderProperty;

            return new TextureHandle(newHandle);
        }

        internal bool IsTextureImported(TextureHandle handle)
        {
            return handle.IsValid() ? GetTextureResource(handle).imported : false;
        }

        internal TextureHandle ImportBackbuffer(RenderTargetIdentifier rt)
        {
            if (m_CurrentBackbuffer != null)
                m_CurrentBackbuffer.SetTexture(rt);
            else
                m_CurrentBackbuffer = m_RTHandleSystem.Alloc(rt);

            int newHandle = AddNewResource(m_Resources[(int)RenderGraphResourceType.Texture], out TextureResource texResource);
            texResource.resource = m_CurrentBackbuffer;
            texResource.imported = true;

            return new TextureHandle(newHandle);
        }

        int AddNewResource<ResType>(DynamicArray<IRenderGraphResource> resourceArray, out ResType outRes) where ResType : IRenderGraphResource, new()
        {
            // In order to not create garbage, instead of using Add, we keep the content of the array while resizing and we just reset the existing ref (or create it if it's null).
            int result = resourceArray.size;
            resourceArray.Resize(resourceArray.size + 1, true);
            if (resourceArray[result] == null)
                resourceArray[result] = new ResType();

            outRes = resourceArray[result] as ResType;
            outRes.Reset();
            return result;
        }

        internal TextureHandle CreateTexture(in TextureDesc desc, int shaderProperty = 0, int transientPassIndex = -1)
        {
            ValidateTextureDesc(desc);

            int newHandle = AddNewResource(m_Resources[(int)RenderGraphResourceType.Texture], out TextureResource texResource);
            texResource.desc = desc;
            texResource.shaderProperty = shaderProperty;
            return new TextureHandle(newHandle, transientPassIndex);
        }

        internal int GetTextureResourceCount()
        {
            return m_Resources[(int)RenderGraphResourceType.Texture].size;
        }

        TextureResource GetTextureResource(int index)
        {
            return m_Resources[(int)RenderGraphResourceType.Texture][index] as TextureResource;
        }

        internal TextureDesc GetTextureResourceDesc(int index)
        {
            return (m_Resources[(int)RenderGraphResourceType.Texture][index] as TextureResource).desc;
        }

        internal RendererListHandle CreateRendererList(in RendererListDesc desc)
        {
            ValidateRendererListDesc(desc);

            int newHandle = m_RendererListResources.Add(new RendererListResource(desc));
            return new RendererListHandle(newHandle);
        }

        internal ComputeBufferHandle ImportComputeBuffer(ComputeBuffer computeBuffer)
        {
            int newHandle = AddNewResource(m_Resources[(int)RenderGraphResourceType.ComputeBuffer], out ComputeBufferResource bufferResource);
            bufferResource.resource = computeBuffer;
            bufferResource.imported = true;

            return new ComputeBufferHandle(newHandle);
        }

        internal ComputeBufferHandle CreateComputeBuffer(in ComputeBufferDesc desc)
        {
            ValidateComputeBufferDesc(desc);

            int newHandle = AddNewResource(m_Resources[(int)RenderGraphResourceType.ComputeBuffer], out ComputeBufferResource bufferResource);
            bufferResource.desc = desc;

            return new ComputeBufferHandle(newHandle);
        }

        internal ComputeBufferDesc GetComputeBufferResourceDesc(ComputeBufferHandle index)
        {
            return (m_Resources[(int)RenderGraphResourceType.ComputeBuffer][index] as ComputeBufferResource).desc;
        }

        internal bool IsComputeBufferImported(ComputeBufferHandle handle)
        {
            return handle.IsValid() ? GetComputeBufferResource(handle).imported : false;
        }

        internal int GetComputeBufferResourceCount()
        {
            return m_Resources[(int)RenderGraphResourceType.ComputeBuffer].size;
        }

        ComputeBufferResource GetComputeBufferResource(ComputeBufferHandle index)
        {
            return m_Resources[(int)RenderGraphResourceType.ComputeBuffer][index] as ComputeBufferResource;
        }

        internal void CreateAndClearTexture(RenderGraphContext rgContext, int index)
        {
            var resource = GetTextureResource(index);
            if (!resource.imported)
            {
                CreateTextureForPass(resource);

                var fastMemDesc = resource.desc.fastMemoryDesc;
                if(fastMemDesc.inFastMemory)
                {
                    resource.rt.SwitchToFastMemory(rgContext.cmd, fastMemDesc.residencyFraction, fastMemDesc.flags);
                }

                if (resource.desc.clearBuffer || m_RenderGraphDebug.clearRenderTargetsAtCreation)
                {
                    bool debugClear = m_RenderGraphDebug.clearRenderTargetsAtCreation && !resource.desc.clearBuffer;
                    var name = debugClear ? "RenderGraph: Clear Buffer (Debug)" : "RenderGraph: Clear Buffer";
                    using (new ProfilingScope(rgContext.cmd, ProfilingSampler.Get(RenderGraphProfileId.RenderGraphClear)))
                    {
                        var clearFlag = resource.desc.depthBufferBits != DepthBits.None ? ClearFlag.Depth : ClearFlag.Color;
                        var clearColor = debugClear ? Color.magenta : resource.desc.clearColor;
                        CoreUtils.SetRenderTarget(rgContext.cmd, resource.resource, clearFlag, clearColor);
                    }
                }

                LogTextureCreation(resource.resource, resource.desc.clearBuffer || m_RenderGraphDebug.clearRenderTargetsAtCreation);
            }
        }

        void CreateTextureForPass(TextureResource resource)
        {
            var desc = resource.desc;
            int hashCode = desc.GetHashCode();

            if(resource.resource != null)
                throw new InvalidOperationException(string.Format("Trying to create an already created texture ({0}). Texture was probably declared for writing more than once in the same pass.", resource.desc.name));

            resource.resource = null;
            if (!TryGetRenderTarget(hashCode, out resource.resource))
            {
                string name = desc.name;
                if (m_RenderGraphDebug.tagResourceNamesWithRG)
                    name = $"RenderGraph_{name}";

                // Note: Name used here will be the one visible in the memory profiler so it means that whatever is the first pass that actually allocate the texture will set the name.
                // TODO: Find a way to display name by pass.
                switch (desc.sizeMode)
                {
                    case TextureSizeMode.Explicit:
                        resource.resource = m_RTHandleSystem.Alloc(desc.width, desc.height, desc.slices, desc.depthBufferBits, desc.colorFormat, desc.filterMode, desc.wrapMode, desc.dimension, desc.enableRandomWrite,
                        desc.useMipMap, desc.autoGenerateMips, desc.isShadowMap, desc.anisoLevel, desc.mipMapBias, desc.msaaSamples, desc.bindTextureMS, desc.useDynamicScale, desc.memoryless, desc.name);
                        break;
                    case TextureSizeMode.Scale:
                        resource.resource = m_RTHandleSystem.Alloc(desc.scale, desc.slices, desc.depthBufferBits, desc.colorFormat, desc.filterMode, desc.wrapMode, desc.dimension, desc.enableRandomWrite,
                        desc.useMipMap, desc.autoGenerateMips, desc.isShadowMap, desc.anisoLevel, desc.mipMapBias, desc.enableMSAA, desc.bindTextureMS, desc.useDynamicScale, desc.memoryless, desc.name);
                        break;
                    case TextureSizeMode.Functor:
                        resource.resource = m_RTHandleSystem.Alloc(desc.func, desc.slices, desc.depthBufferBits, desc.colorFormat, desc.filterMode, desc.wrapMode, desc.dimension, desc.enableRandomWrite,
                        desc.useMipMap, desc.autoGenerateMips, desc.isShadowMap, desc.anisoLevel, desc.mipMapBias, desc.enableMSAA, desc.bindTextureMS, desc.useDynamicScale, desc.memoryless, desc.name);
                        break;
                }
            }

            //// Try to update name when re-using a texture.
            //// TODO RENDERGRAPH: Check if that actually works.
            //resource.rt.name = desc.name;

#if DEVELOPMENT_BUILD || UNITY_EDITOR
            if (hashCode != -1)
            {
                m_FrameAllocatedTextures.Add((hashCode, resource.resource));
            }
#endif

            resource.cachedHash = hashCode;
        }

        void SetGlobalTextures(RenderGraphContext rgContext, List<TextureHandle> textures, bool bindDummyTexture)
        {
            foreach (var resource in textures)
            {
                var resourceDesc = GetTextureResource(resource);
                if (resourceDesc.shaderProperty != 0)
                {
                    if (resourceDesc.resource != null)
                    {
                        rgContext.cmd.SetGlobalTexture(resourceDesc.shaderProperty, bindDummyTexture ? TextureXR.GetMagentaTexture() : resourceDesc.resource);
                    }
                }
            }
        }


        internal void PreRenderPassSetGlobalTextures(RenderGraphContext rgContext, List<TextureHandle> textures)
        {
            SetGlobalTextures(rgContext, textures, false);
        }

        internal void PostRenderPassUnbindGlobalTextures(RenderGraphContext rgContext, List<TextureHandle> textures)
        {
            SetGlobalTextures(rgContext, textures, true);
        }

        internal void ReleaseTexture(RenderGraphContext rgContext, int handle)
        {
            var resource = GetTextureResource(handle);
            if (!resource.imported)
            {
                if (m_RenderGraphDebug.clearRenderTargetsAtRelease)
                {
                    using (new ProfilingScope(rgContext.cmd, ProfilingSampler.Get(RenderGraphProfileId.RenderGraphClearDebug)))
                    {
                        var clearFlag = resource.desc.depthBufferBits != DepthBits.None ? ClearFlag.Depth : ClearFlag.Color;
                        // Not ideal to do new TextureHandle here but GetTexture is a public API and we rather have it take an explicit TextureHandle parameters.
                        // Everywhere else internally int is better because it allows us to share more code.
                        CoreUtils.SetRenderTarget(rgContext.cmd, GetTexture(new TextureHandle(handle)), clearFlag, Color.magenta);
                    }
                }

                LogTextureRelease(resource.resource);
                ReleaseTextureResource(resource.cachedHash, resource.resource);
                resource.cachedHash = -1;
                resource.resource = null;
                resource.wasReleased = true;
            }
        }

        void ReleaseTextureResource(int hash, RTHandle rt)
        {
            m_TexturePool.ReleaseResource(hash, rt, m_CurrentFrameIndex);

#if DEVELOPMENT_BUILD || UNITY_EDITOR
            m_FrameAllocatedTextures.Remove((hash, rt));
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

        void ValidateComputeBufferDesc(in ComputeBufferDesc desc)
        {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            // TODO RENDERGRAPH: Check actual condition on stride.
            //if (desc.stride % 4 != 0)
            //{
            //    throw new ArgumentException("Compute Buffer stride must be at least 4.");
            //}
#endif
        }

        bool TryGetRenderTarget(int hashCode, out RTHandle rt)
        {
            return m_TexturePool.TryGetResource(hashCode, out rt);
        }

        internal void CreateRendererLists(List<RendererListHandle> rendererLists)
        {
            // For now we just create a simple structure
            // but when the proper API is available in trunk we'll kick off renderer lists creation jobs here.
            foreach (var rendererList in rendererLists)
            {
                ref var rendererListResource = ref m_RendererListResources[rendererList];
                ref var desc = ref rendererListResource.desc;
                RendererList newRendererList = RendererList.Create(desc);
                rendererListResource.rendererList = newRendererList;
            }
        }

        internal void Clear(bool onException)
        {
            LogResources();

            for (int i = 0; i < (int)RenderGraphResourceType.Count; ++i)
                m_Resources[i].Clear();
            m_RendererListResources.Clear();

#if DEVELOPMENT_BUILD || UNITY_EDITOR
            if (m_FrameAllocatedTextures.Count != 0 && !onException)
            {
                string logMessage = "RenderGraph: Not all textures were released. This can be caused by a textures being allocated but never read by any pass.";

                List<(int, RTHandle)> tempList = new List<(int, RTHandle)>(m_FrameAllocatedTextures);
                foreach (var value in tempList)
                {
                    logMessage = $"{logMessage}\n\t{value.Item2.name}";
                    ReleaseTextureResource(value.Item1, value.Item2);
                }

                Debug.LogWarning(logMessage);
            }

            // If an error occurred during execution, it's expected that textures are not all released so we clear the tracking list.
            if (onException)
                m_FrameAllocatedTextures.Clear();
#endif

            // TODO RENDERGRAPH: Might not be ideal to purge stale resources every frame.
            // In case users enable/disable features along a level it might provoke performance spikes when things are reallocated...
            // Will be much better when we have actual resource aliasing and we can manage memory more efficiently.
            m_TexturePool.PurgeUnusedResources(m_CurrentFrameIndex);
        }

        internal void ResetRTHandleReferenceSize(int width, int height)
        {
            m_RTHandleSystem.ResetReferenceSize(width, height);
        }

        internal void Cleanup()
        {
            m_TexturePool.Cleanup();
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

                m_TexturePool.LogResources(m_Logger);
            }
        }

        #endregion
    }
}
