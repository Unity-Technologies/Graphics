using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Scripting.APIUpdating;

namespace UnityEngine.Rendering.RenderGraphModule
{
    internal struct TextureAccess
    {
        public TextureHandle textureHandle;
        public int mipLevel;
        public int depthSlice;
        public AccessFlags flags;

        public TextureAccess(TextureHandle handle, AccessFlags flags, int mipLevel, int depthSlice)
        {
            this.textureHandle = handle;
            this.flags = flags;
            this.mipLevel = mipLevel;
            this.depthSlice = depthSlice;
        }
    }


    /// <summary>
    /// An abstract handle representing a texture resource as known by one particular record + execute of the render graph.
    /// TextureHandles should not be used outside of the context of a render graph execution.
    ///
    /// A render graph needs to do additional state tracking on texture resources (lifetime, how is it used,...) to enable
    /// this all textures relevant to the render graph need to be make known to it. A texture handle specifies such a texture as
    /// known to the render graph.
    ///
    /// It is important to understand that a render graph texture handle does not necessarily represent an actual texture. For example
    /// textures could be created the render graph that are only referenced by passes that are later culled when executing the graph.
    /// Such textures would never be allocated as actual RenderTextures.
    ///
    /// Texture handles are only relevant to one particular record+execute phase of the render graph. After execution all texture
    /// handles are invalidated. The system will catch texture handles from a different execution of the render graph but still
    /// users should be careful to avoid keeping texture handles around from other render graph executions.
    ///
    /// Texture handles do not need to be disposed/freed (they are auto-invalidated at the end of graph execution). The RenderTextures they represent
    /// are either freed by the render graph internally (when the handle was acquired through RenderGraph.CreateTexture) or explicitly managed by
    /// some external system (when acquired through RenderGraph.ImportTexture).
    ///
    /// </summary>
    [DebuggerDisplay("Texture ({handle.index})")]
    [MovedFrom(true, "UnityEngine.Experimental.Rendering.RenderGraphModule", "UnityEngine.Rendering.RenderGraphModule")]
    public struct TextureHandle
    {
        private static TextureHandle s_NullHandle = new TextureHandle();

        /// <summary>
        /// Returns a null texture handle
        /// </summary>
        /// <value>A null texture handle.</value>
        public static TextureHandle nullHandle { get { return s_NullHandle; } }

        internal ResourceHandle handle;

        private bool builtin;

        internal TextureHandle(in ResourceHandle h)
        {
            handle = h;
            builtin = false;
        }

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
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsValid() => handle.IsValid();

        /// <summary>
        /// Return true if the handle is a builtin handle managed by RenderGraph internally.
        /// </summary>
        /// <returns>True if the handle is a builtin handle.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal bool IsBuiltin() => this.builtin;

        /// <summary>
        /// Get the Descriptor of the texture. This simply calls RenderGraph.GetTextureDesc but is more easily discoverable through auto complete.
        /// </summary>
        /// <param name="renderGraph">The rendergraph instance that was used to create the texture on. Texture handles are a lightweight object, all information is stored on the RenderGraph itself.</param>
        /// <returns>The texture descriptor for the given texture handle.</returns>
        public TextureDesc GetDescriptor(RenderGraph renderGraph) { return renderGraph.GetTextureDesc(this); }
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
        ///<summary>Color or depth stencil format.</summary>
        public GraphicsFormat format;
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
        ///<summary>[See Dynamic Resolution documentation](https://docs.unity3d.com/Manual/DynamicResolution.html)</summary>
        public bool useDynamicScale;
        ///<summary>[See Dynamic Resolution documentation](https://docs.unity3d.com/Manual/DynamicResolution.html)</summary>
        public bool useDynamicScaleExplicit;
        ///<summary>Memory less flag.</summary>
        public RenderTextureMemoryless memoryless;
        ///<summary>Special treatment of the VR eye texture used in stereoscopic rendering.</summary>
        public VRTextureUsage vrUsage;

        /// <summary>
        /// Set to true if the texture is to be used as a shading rate image.
        /// </summary>
        /// <remarks>
        /// Width and height are usually in pixels but if enableShadingRate is set to true, width and height are in tiles.
        /// See also <a href="https://docs.unity3d.com/Manual/variable-rate-shading">Variable Rate Shading</a>.
        /// </remarks>
        public bool enableShadingRate;

        ///<summary>Texture name.</summary>
        public string name;
#if UNITY_2020_2_OR_NEWER
        ///<summary>Descriptor to determine how the texture will be in fast memory on platform that supports it.</summary>
        public FastMemoryDesc fastMemoryDesc;
#endif
        ///<summary>Determines whether the texture will fallback to a black texture if it is read without ever writing to it.</summary>
        public bool fallBackToBlackTexture;
        ///<summary>
        ///If all passes writing to a texture are culled by Dynamic Render Pass Culling, it will automatically fallback to a similar preallocated texture.
        ///Set this to true to force the allocation.
        ///</summary>
        public bool disableFallBackToImportedTexture;

        // Initial state. Those should not be used in the hash
        ///<summary>Texture needs to be cleared on first use.</summary>
        public bool clearBuffer;
        ///<summary>Clear color.</summary>
        public Color clearColor;

        ///<summary>Texture needs to be discarded on last use.</summary>
        public bool discardBuffer;


        ///<summary>Depth buffer bit depth of the format. The setter convert the bits to valid depth stencil format and sets the format. The getter gets the depth bits of the format.</summary>
        public DepthBits depthBufferBits
        {
            get { return (DepthBits)GraphicsFormatUtility.GetDepthBits(format); }
            set
            {
                if (value == DepthBits.None)
                {
                    if( !GraphicsFormatUtility.IsDepthStencilFormat(format) )
                        return;
                    else
                        format = GraphicsFormat.None;
                }
                else
                {
                    format = GraphicsFormatUtility.GetDepthStencilFormat((int)value);
                }
            }
        }

        ///<summary>Color format. Sets the format. The getter checks if format is a color format. Returns the format if a color format, otherwise returns GraphicsFormat.None.</summary>
        public GraphicsFormat colorFormat
        {
            get { return GraphicsFormatUtility.IsDepthStencilFormat(format) ? GraphicsFormat.None : format; }
            set { format = value; }
        }

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

            discardBuffer = false;
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
        /// <param name="input">The TextureDesc instance to copy from.</param>
        public TextureDesc(TextureDesc input)
        {
            this = input;
        }

        /// <summary>
        /// Do a best effort conversion from a RenderTextureDescriptor to a TextureDesc. This tries to initialize a descriptor to be as close as possible to the given render texture descriptor but there might be subtle differences when creating
        /// render graph textures using this TextureDesc due to the underlying RTHandle system.
        /// Some parameters of the TextureDesc (like name and filtering modes) are not present in the RenderTextureDescriptor for these the returned TextureDesc will contain plausible default values.
        /// </summary>
        /// <param name="input">The texture descriptor to create a TextureDesc from</param>
        public TextureDesc(RenderTextureDescriptor input)
        {
            sizeMode = TextureSizeMode.Explicit;
            width = input.width;
            height = input.height;
            slices = input.volumeDepth;
            scale = Vector2.one;
            func = null;
            format = (input.depthStencilFormat != GraphicsFormat.None) ? input.depthStencilFormat : input.graphicsFormat;
            filterMode = FilterMode.Bilinear;
            wrapMode = TextureWrapMode.Clamp;
            dimension = input.dimension;
            enableRandomWrite = input.enableRandomWrite;
            useMipMap = input.useMipMap;
            autoGenerateMips = input.autoGenerateMips;
            isShadowMap = (input.shadowSamplingMode != ShadowSamplingMode.None);
            anisoLevel = 1;
            mipMapBias = 0;
            msaaSamples = (MSAASamples)input.msaaSamples;
            bindTextureMS = input.bindMS;
            useDynamicScale = input.useDynamicScale;
            useDynamicScaleExplicit = false;
            memoryless = input.memoryless;
            vrUsage = input.vrUsage;
            name = "UnNamedFromRenderTextureDescriptor";
            fastMemoryDesc = new FastMemoryDesc();
            fastMemoryDesc.inFastMemory = false;
            fallBackToBlackTexture = false;
            disableFallBackToImportedTexture = true;
            clearBuffer = true;
            clearColor = Color.black;
            discardBuffer = false;
            enableShadingRate = input.enableShadingRate;
        }

        /// <summary>
        /// Do a best effort conversion from a RenderTexture to a TextureDesc. This tries to initialize a descriptor to be as close as possible to the given render texture but there might be subtle differences when creating
        /// render graph textures using this TextureDesc due to the underlying RTHandle system.
        /// </summary>
        /// <param name="input">The texture to create a TextureDesc from</param>
        public TextureDesc(RenderTexture input) : this(input.descriptor)
        {
            filterMode = input.filterMode;
            wrapMode = input.wrapMode;
            anisoLevel = input.anisoLevel;
            mipMapBias = input.mipMapBias;
            name = "UnNamedFromRenderTextureDescriptor";
        }

        /// <summary>
        /// Hash function
        /// </summary>
        /// <returns>The texture descriptor hash.</returns>
        public override int GetHashCode()
        {
            var hashCode = HashFNV1A32.Create();
            switch (sizeMode)
            {
                case TextureSizeMode.Explicit:
                    hashCode.Append(width);
                    hashCode.Append(height);
                    break;
                case TextureSizeMode.Functor:
                    if (func != null)
                        hashCode.Append(DelegateHashCodeUtils.GetFuncHashCode(func));
                    break;
                case TextureSizeMode.Scale:
                    hashCode.Append(scale);
                    break;
            }

            hashCode.Append(mipMapBias);
            hashCode.Append(slices);
            hashCode.Append((int) format);
            hashCode.Append((int) filterMode);
            hashCode.Append((int) wrapMode);
            hashCode.Append((int) dimension);
            hashCode.Append((int) memoryless);
            hashCode.Append((int) vrUsage);
            hashCode.Append(anisoLevel);
            hashCode.Append(enableRandomWrite);
            hashCode.Append(useMipMap);
            hashCode.Append(autoGenerateMips);
            hashCode.Append(isShadowMap);
            hashCode.Append(bindTextureMS);
            hashCode.Append(useDynamicScale);
            hashCode.Append((int) msaaSamples);
#if UNITY_2020_2_OR_NEWER
            hashCode.Append(fastMemoryDesc.inFastMemory);
#endif
            hashCode.Append(enableShadingRate);
            return hashCode.value;
        }

        /// <summary>
        /// Calculate the final size of the texture descriptor in pixels. This takes into account the sizeMode set for this descriptor.
        /// For the automatically scaled sizes the size will be relative to the RTHandle reference size <see cref="RTHandles.SetReferenceSize">SetReferenceSize</see>.
        /// </summary>
        /// <returns>The calculated size.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if the texture descriptor's size mode falls outside the expected range.</exception>
        public Vector2Int CalculateFinalDimensions()
        {
            return sizeMode switch
            {
                TextureSizeMode.Explicit => new Vector2Int(width, height),
                TextureSizeMode.Scale => RTHandles.CalculateDimensions(scale),
                TextureSizeMode.Functor => RTHandles.CalculateDimensions(func),
                _ => throw new ArgumentOutOfRangeException()
            };

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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override int GetDescHashCode() { return desc.GetHashCode(); }

        public override void CreateGraphicsResource()
        {
            var name = GetName();

            // Textures are going to be reused under different aliases along the frame so we can't provide a specific name upon creation.
            // The name in the desc is going to be used for debugging purpose and render graph visualization.
            if (name == "")
                name = $"RenderGraphTexture_{m_TextureCreationIndex++}";

            RTHandleAllocInfo rtAllocInfo = new RTHandleAllocInfo(name)
            {
                slices = desc.slices,
                format = desc.format,
                filterMode = desc.filterMode,
                wrapModeU = desc.wrapMode,
                wrapModeV = desc.wrapMode,
                wrapModeW = desc.wrapMode,
                dimension = desc.dimension,
                enableRandomWrite = desc.enableRandomWrite,
                useMipMap = desc.useMipMap,
                autoGenerateMips = desc.autoGenerateMips,
                anisoLevel = desc.anisoLevel,
                mipMapBias = desc.mipMapBias,
                isShadowMap = desc.isShadowMap,
                msaaSamples = (MSAASamples)desc.msaaSamples,
                bindTextureMS = desc.bindTextureMS,
                useDynamicScale = desc.useDynamicScale,
                useDynamicScaleExplicit = desc.useDynamicScaleExplicit,
                memoryless = desc.memoryless,
                vrUsage = desc.vrUsage,
                enableShadingRate = desc.enableShadingRate,
            };

            switch (desc.sizeMode)
            {
                case TextureSizeMode.Explicit:
                    graphicsResource = RTHandles.Alloc(desc.width, desc.height, rtAllocInfo);
                    break;
                case TextureSizeMode.Scale:
                    graphicsResource = RTHandles.Alloc(desc.scale, rtAllocInfo);
                    break;
                case TextureSizeMode.Functor:
                    graphicsResource = RTHandles.Alloc(desc.func, rtAllocInfo);
                    break;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override void UpdateGraphicsResource()
        {
            if (graphicsResource != null)
                graphicsResource.m_Name = GetName();
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

        protected override string GetResourceName(in RTHandle res)
        {
            return res.rt.name;
        }

        protected override long GetResourceSize(in RTHandle res)
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
    }
}
