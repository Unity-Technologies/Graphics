using System;
using System.Diagnostics;
using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering.RenderGraphModule
{
    /// <summary>
    /// Texture resource handle.
    /// </summary>
    [DebuggerDisplay("Texture ({handle.index})")]
    public struct TextureHandle
    {
        private static TextureHandle s_NullHandle = new TextureHandle();

        /// <summary>
        /// Returns a null texture handle
        /// </summary>
        /// <returns>A null texture handle.</returns>
        public static TextureHandle nullHandle { get { return s_NullHandle; } }

        internal ResourceHandle handle;

        private bool builtin;

        internal TextureHandle(int handle, bool shared = false, bool builtin = false)
        {
            this.handle = new ResourceHandle(handle, RenderGraphResourceType.Texture, shared);
            this.builtin = builtin;
        }

        /// <summary>
        /// Cast to RenderTargetIdentifier
        /// </summary>
        /// <param name="texture">Input TextureHandle.</param>
        /// <returns>Resource as a RenderTargetIdentifier.</returns>
        public static implicit operator RenderTargetIdentifier(TextureHandle texture) => texture.IsValid() ? RenderGraphResourceRegistry.current.GetTexture(texture) : default(RenderTargetIdentifier);

        /// <summary>
        /// Cast to Texture
        /// </summary>
        /// <param name="texture">Input TextureHandle.</param>
        /// <returns>Resource as a Texture.</returns>
        public static implicit operator Texture(TextureHandle texture) => texture.IsValid() ? RenderGraphResourceRegistry.current.GetTexture(texture) : null;

        /// <summary>
        /// Cast to RenderTexture
        /// </summary>
        /// <param name="texture">Input TextureHandle.</param>
        /// <returns>Resource as a RenderTexture.</returns>
        public static implicit operator RenderTexture(TextureHandle texture) => texture.IsValid() ? RenderGraphResourceRegistry.current.GetTexture(texture) : null;

        /// <summary>
        /// Cast to RTHandle
        /// </summary>
        /// <param name="texture">Input TextureHandle.</param>
        /// <returns>Resource as a RTHandle.</returns>
        public static implicit operator RTHandle(TextureHandle texture) => texture.IsValid() ? RenderGraphResourceRegistry.current.GetTexture(texture) : null;

        /// <summary>
        /// Return true if the handle is valid.
        /// </summary>
        /// <returns>True if the handle is valid.</returns>
        public bool IsValid() => handle.IsValid();

        /// <summary>
        /// Return true if the handle is a builtin handle managed by RenderGraph internally.
        /// </summary>
        /// <returns>True if the handle is a builtin handle.</returns>
        internal bool IsBuiltin() => this.builtin;
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

#if UNITY_2020_2_OR_NEWER
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
#endif

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
        ///<summary>Number of MSAA samples.</summary>
        public MSAASamples msaaSamples;
        ///<summary>Bind texture multi sampled.</summary>
        public bool bindTextureMS;
        ///<summary>Texture uses dynamic scaling.</summary>
        public bool useDynamicScale;
        ///<summary>Memory less flag.</summary>
        public RenderTextureMemoryless memoryless;
        ///<summary>Special treatment of the VR eye texture used in stereoscopic rendering.</summary>
        public VRTextureUsage vrUsage;
        ///<summary>Texture name.</summary>
        public string name;
#if UNITY_2020_2_OR_NEWER
        ///<summary>Descriptor to determine how the texture will be in fast memory on platform that supports it.</summary>
        public FastMemoryDesc fastMemoryDesc;
#endif
        ///<summary>Determines whether the texture will fallback to a black texture if it is read without ever writing to it.</summary>
        public bool fallBackToBlackTexture;
        ///<summary>
        ///If all passes writing to a texture are culled by Dynamic Render Pass Culling, it will automatically fallback to a similar preallocated texture.\n
        ///Set this to false to force the allocation.
        ///</summary>
        public bool disableFallBackToImportedTexture;

        // Initial state. Those should not be used in the hash
        ///<summary>Texture needs to be cleared on first use.</summary>
        public bool clearBuffer;
        ///<summary>Clear color.</summary>
        public Color clearColor;

        void InitDefaultValues(bool dynamicResolution, bool xrReady)
        {
            useDynamicScale = dynamicResolution;
            vrUsage = VRTextureUsage.None;
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
        /// <param name="func">Function used to determine the texture size</param>
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
                        break;
                    case TextureSizeMode.Functor:
                        if (func != null)
                            hashCode = hashCode * 23 + func.GetHashCode();
                        break;
                    case TextureSizeMode.Scale:
                        hashCode = hashCode * 23 + scale.x.GetHashCode();
                        hashCode = hashCode * 23 + scale.y.GetHashCode();
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
                hashCode = hashCode * 23 + (int)vrUsage;
                hashCode = hashCode * 23 + anisoLevel;
                hashCode = hashCode * 23 + (enableRandomWrite ? 1 : 0);
                hashCode = hashCode * 23 + (useMipMap ? 1 : 0);
                hashCode = hashCode * 23 + (autoGenerateMips ? 1 : 0);
                hashCode = hashCode * 23 + (isShadowMap ? 1 : 0);
                hashCode = hashCode * 23 + (bindTextureMS ? 1 : 0);
                hashCode = hashCode * 23 + (useDynamicScale ? 1 : 0);
                hashCode = hashCode * 23 + (int)msaaSamples;
#if UNITY_2020_2_OR_NEWER
                hashCode = hashCode * 23 + (fastMemoryDesc.inFastMemory ? 1 : 0);
#endif
            }

            return hashCode;
        }
    }

    [DebuggerDisplay("TextureResource ({desc.name})")]
    class TextureResource : RenderGraphResource<TextureDesc, RTHandle>
    {
        static int m_TextureCreationIndex;

        public override string GetName()
        {
            if (imported && !shared)
                return graphicsResource != null ? graphicsResource.name : "null resource";
            else
                return desc.name;
        }

        // NOTE:
        // Next two functions should have been implemented in RenderGraphResource<DescType, ResType> but for some reason,
        // when doing so, it's impossible to break in the Texture version of the virtual function (with VS2017 at least), making this completely un-debuggable.
        // To work around this, we just copy/pasted the implementation in each final class...

        public override void CreatePooledGraphicsResource()
        {
            Debug.Assert(m_Pool != null, "TextureResource: CreatePooledGraphicsResource should only be called for regular pooled resources");

            int hashCode = desc.GetHashCode();

            if (graphicsResource != null)
                throw new InvalidOperationException(string.Format("TextureResource: Trying to create an already created resource ({0}). Resource was probably declared for writing more than once in the same pass.", GetName()));

            var pool = m_Pool as TexturePool;
            if (!pool.TryGetResource(hashCode, out graphicsResource))
            {
                CreateGraphicsResource(desc.name);
            }

            cachedHash = hashCode;
            pool.RegisterFrameAllocation(cachedHash, graphicsResource);
            graphicsResource.m_Name = desc.name;
        }

        public override void ReleasePooledGraphicsResource(int frameIndex)
        {
            if (graphicsResource == null)
                throw new InvalidOperationException($"TextureResource: Tried to release a resource ({GetName()}) that was never created. Check that there is at least one pass writing to it first.");

            // Shared resources don't use the pool
            var pool = m_Pool as TexturePool;
            if (pool != null)
            {
                pool.ReleaseResource(cachedHash, graphicsResource, frameIndex);
                pool.UnregisterFrameAllocation(cachedHash, graphicsResource);
            }

            Reset(null);
        }

        public override void CreateGraphicsResource(string name = "")
        {
            // Textures are going to be reused under different aliases along the frame so we can't provide a specific name upon creation.
            // The name in the desc is going to be used for debugging purpose and render graph visualization.
            if (name == "")
                name = $"RenderGraphTexture_{m_TextureCreationIndex++}";

            switch (desc.sizeMode)
            {
                case TextureSizeMode.Explicit:
                    graphicsResource = RTHandles.Alloc(desc.width, desc.height, desc.slices, desc.depthBufferBits, desc.colorFormat, desc.filterMode, desc.wrapMode, desc.dimension, desc.enableRandomWrite,
                        desc.useMipMap, desc.autoGenerateMips, desc.isShadowMap, desc.anisoLevel, desc.mipMapBias, desc.msaaSamples, desc.bindTextureMS, desc.useDynamicScale, desc.memoryless, desc.vrUsage, name);
                    break;
                case TextureSizeMode.Scale:
                    graphicsResource = RTHandles.Alloc(desc.scale, desc.slices, desc.depthBufferBits, desc.colorFormat, desc.filterMode, desc.wrapMode, desc.dimension, desc.enableRandomWrite,
                        desc.useMipMap, desc.autoGenerateMips, desc.isShadowMap, desc.anisoLevel, desc.mipMapBias, desc.msaaSamples, desc.bindTextureMS, desc.useDynamicScale, desc.memoryless, desc.vrUsage, name);
                    break;
                case TextureSizeMode.Functor:
                    graphicsResource = RTHandles.Alloc(desc.func, desc.slices, desc.depthBufferBits, desc.colorFormat, desc.filterMode, desc.wrapMode, desc.dimension, desc.enableRandomWrite,
                        desc.useMipMap, desc.autoGenerateMips, desc.isShadowMap, desc.anisoLevel, desc.mipMapBias, desc.msaaSamples, desc.bindTextureMS, desc.useDynamicScale, desc.memoryless, desc.vrUsage, name);
                    break;
            }
        }

        public override void ReleaseGraphicsResource()
        {
            if (graphicsResource != null)
                graphicsResource.Release();
            base.ReleaseGraphicsResource();
        }

        public override void LogCreation(RenderGraphLogger logger)
        {
            logger.LogLine($"Created Texture: {desc.name} (Cleared: {desc.clearBuffer})");
        }

        public override void LogRelease(RenderGraphLogger logger)
        {
            logger.LogLine($"Released Texture: {desc.name}");
        }
    }

    class TexturePool : RenderGraphResourcePool<RTHandle>
    {
        protected override void ReleaseInternalResource(RTHandle res)
        {
            res.Release();
        }

        protected override string GetResourceName(RTHandle res)
        {
            return res.rt.name;
        }

        protected override long GetResourceSize(RTHandle res)
        {
            return Profiling.Profiler.GetRuntimeMemorySizeLong(res.rt);
        }

        override protected string GetResourceTypeName()
        {
            return "Texture";
        }

        override protected int GetSortIndex(RTHandle res)
        {
            return res.GetInstanceID();
        }

        // Another C# nicety.
        // We need to re-implement the whole thing every time because:
        // - obj.resource.Release is Type specific so it cannot be called on a generic (and there's no shared interface for resources like RTHandle, GraphicsBuffers etc)
        // - We can't use a virtual release function because it will capture 'this' in the lambda for RemoveAll generating GCAlloc in the process.
        override public void PurgeUnusedResources(int currentFrameIndex)
        {
            // Update the frame index for the lambda. Static because we don't want to capture.
            s_CurrentFrameIndex = currentFrameIndex;
            m_RemoveList.Clear();

            foreach (var kvp in m_ResourcePool)
            {
                // WARNING: No foreach here. Sorted list GetEnumerator generates garbage...
                var list = kvp.Value;
                var keys = list.Keys;
                var values = list.Values;
                for (int i = 0; i < list.Count; ++i)
                {
                    var value = values[i];
                    if (ShouldReleaseResource(value.frameIndex, s_CurrentFrameIndex))
                    {
                        value.resource.Release();
                        m_RemoveList.Add(keys[i]);
                    }
                }

                foreach (var key in m_RemoveList)
                    list.Remove(key);
            }
        }
    }
}
