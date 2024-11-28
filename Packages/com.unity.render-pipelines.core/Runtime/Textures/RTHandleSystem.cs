using System;
using System.Collections.Generic;
using UnityEngine.Assertions;
using UnityEngine.Experimental.Rendering;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace UnityEngine.Rendering
{
    /// <summary>
    /// Scaled function used to compute the size of a RTHandle for the current frame.
    /// </summary>
    /// <param name="size">Reference size of the RTHandle system for the frame.</param>
    /// <returns>The size of the RTHandled computed from the reference size.</returns>
    public delegate Vector2Int ScaleFunc(Vector2Int size);

    /// <summary>
    /// List of properties of the RTHandle System for the current frame.
    /// </summary>
    public struct RTHandleProperties
    {
        /// <summary>
        /// Size set as reference at the previous frame
        /// </summary>
        public Vector2Int previousViewportSize;
        /// <summary>
        /// Size of the render targets at the previous frame
        /// </summary>
        public Vector2Int previousRenderTargetSize;
        /// <summary>
        /// Size set as reference at the current frame
        /// </summary>
        public Vector2Int currentViewportSize;
        /// <summary>
        /// Size of the render targets at the current frame
        /// </summary>
        public Vector2Int currentRenderTargetSize;
        /// <summary>
        /// Scale factor from RTHandleSystem max size to requested reference size (referenceSize/maxSize)
        /// (x,y) current frame (z,w) last frame (this is only used for buffered RTHandle Systems)
        /// </summary>
        public Vector4 rtHandleScale;
    }

    /// <summary>
    /// Information about the allocation of a RTHandle
    /// </summary>
    public struct RTHandleAllocInfo
    {
        /// <summary> Number of slices of the RTHandle.</summary>
        public int slices { get; set; }

        /// <summary> GraphicsFormat of a color buffer.</summary>
        public GraphicsFormat format { get; set; }

        /// <summary> Filtering mode of the RTHandle.</summary>
        public FilterMode filterMode { get; set; }

        /// <summary> Addressing mode of the RTHandle.</summary>
        public TextureWrapMode wrapModeU { get; set; }

        /// <summary> Addressing mode of the RTHandle.</summary>
        public TextureWrapMode wrapModeV { get; set; }

        /// <summary> Addressing mode of the RTHandle.</summary>
        public TextureWrapMode wrapModeW { get; set; }

        /// <summary> Texture dimension of the RTHandle.</summary>
        public TextureDimension dimension { get; set; }

        /// <summary> Set to true to enable UAV random read writes on the texture.</summary>
        public bool enableRandomWrite { get; set; }

        /// <summary> Set to true if the texture should have mipmaps.</summary>
        public bool useMipMap { get; set; }

        /// <summary> Set to true to automatically generate mipmaps.</summary>
        public bool autoGenerateMips { get; set; }

        /// <summary> Set to true if the texture is sampled as a shadow map.</summary>
        public bool isShadowMap { get; set; }

        /// <summary> Anisotropic filtering level.</summary>
        public int anisoLevel { get; set; }

        /// <summary> Bias applied to mipmaps during filtering.</summary>
        public float mipMapBias { get; set; }

        /// <summary> Number of MSAA samples for the RTHandle.</summary>
        public MSAASamples msaaSamples { get; set; }

        /// <summary> Set to true if the texture needs to be bound as a multisampled texture in the shader.</summary>
        public bool bindTextureMS { get; set; }

        /// <summary> See Dynamic Resolution documentation](https://docs.unity3d.com/Manual/DynamicResolution.html)</summary>
        public bool useDynamicScale { get; set; }

        ///<summary> See Dynamic Resolution documentation](https://docs.unity3d.com/Manual/DynamicResolution-control-when-occurs.html)</summary>
        public bool useDynamicScaleExplicit { get; set; }

        /// <summary> Use this property to set the render texture memoryless modes.</summary>
        public RenderTextureMemoryless memoryless { get; set; }

        /// <summary> Special treatment of the VR eye texture used in stereoscopic rendering.</summary>
        public VRTextureUsage vrUsage { get; set; }

        /// <summary>
        /// Set to true if the texture is to be used as a shading rate image.
        /// </summary>
        /// <remarks>
        /// Width and height are usually in pixels but if enableShadingRate is set to true, width and height are in tiles.
        /// See also <a href="https://docs.unity3d.com/Manual/variable-rate-shading">Variable Rate Shading</a>.
        /// </remarks>
        public bool enableShadingRate { get; set; }

        /// <summary> Name of the RTHandle.</summary>
        public string name { get; set; }

        /// <summary>
        /// RTHandleAllocInfo constructor.
        /// </summary>
        /// <param name="name">Name of the RTHandle.</param>
        public RTHandleAllocInfo(string name = "")
        {
            this.slices = 1;
            this.format = GraphicsFormat.R8G8B8A8_SRGB;
            this.filterMode = FilterMode.Point;
            this.wrapModeU = TextureWrapMode.Repeat;
            this.wrapModeV = TextureWrapMode.Repeat;
            this.wrapModeW = TextureWrapMode.Repeat;
            this.dimension = TextureDimension.Tex2D;
            this.enableRandomWrite = false;
            this.useMipMap = false;
            this.autoGenerateMips = true;
            this.isShadowMap = false;
            this.anisoLevel = 1;
            this.mipMapBias = 0f;
            this.msaaSamples = MSAASamples.None;
            this.bindTextureMS = false;
            this.useDynamicScale = false;
            this.useDynamicScaleExplicit = false;
            this.memoryless = RenderTextureMemoryless.None;
            this.vrUsage = VRTextureUsage.None;
            this.enableShadingRate = false;
            this.name = name;
        }
    }

    /// <summary>
    /// System managing a set of RTHandle textures
    /// </summary>
    public partial class RTHandleSystem : IDisposable
    {
        internal enum ResizeMode
        {
            Auto,
            OnDemand
        }

        // Parameters for auto-scaled Render Textures
        bool m_HardwareDynamicResRequested = false;
        HashSet<RTHandle> m_AutoSizedRTs;
        RTHandle[] m_AutoSizedRTsArray; // For fast iteration
        HashSet<RTHandle> m_ResizeOnDemandRTs;
        RTHandleProperties m_RTHandleProperties;

        /// <summary>
        /// Current properties of the RTHandle System.
        /// </summary>
        public RTHandleProperties rtHandleProperties { get { return m_RTHandleProperties; } }

        int m_MaxWidths = 0;
        int m_MaxHeights = 0;
#if UNITY_EDITOR
        // In editor every now and then we must reset the size of the rthandle system if it was set very high and then switched back to a much smaller scale.
        int m_FramesSinceLastReset = 0;
#endif

        /// <summary>
        /// RTHandleSystem constructor.
        /// </summary>
        public RTHandleSystem()
        {
            m_AutoSizedRTs = new HashSet<RTHandle>();
            m_ResizeOnDemandRTs = new HashSet<RTHandle>();
            m_MaxWidths = 1;
            m_MaxHeights = 1;
        }

        /// <summary>
        /// Disposable pattern implementation
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
        }

        /// <summary>
        /// Initialize the RTHandle system.
        /// </summary>
        /// <param name="width">Initial reference rendering width.</param>
        /// <param name="height">Initial reference rendering height.</param>
        public void Initialize(int width, int height)
        {
            if (m_AutoSizedRTs.Count != 0)
            {
                string leakingResources = "Unreleased RTHandles:";
                foreach (var rt in m_AutoSizedRTs)
                {
                    leakingResources = string.Format("{0}\n    {1}", leakingResources, rt.name);
                }
                Debug.LogError(string.Format("RTHandleSystem.Initialize should only be called once before allocating any Render Texture. This may be caused by an unreleased RTHandle resource.\n{0}\n", leakingResources));
            }

            m_MaxWidths = width;
            m_MaxHeights = height;

            m_HardwareDynamicResRequested = DynamicResolutionHandler.instance.RequestsHardwareDynamicResolution();
        }

        /// <summary>
        /// Initialize the RTHandle system.
        /// </summary>
        /// <param name="width">Initial reference rendering width.</param>
        /// <param name="height">Initial reference rendering height.</param>
        /// <param name="useLegacyDynamicResControl">Use legacy hardware DynamicResolution control in RTHandle system.</param>
        [Obsolete("useLegacyDynamicResControl is deprecated. Please use SetHardwareDynamicResolutionState() instead.")]
        public void Initialize(int width, int height, bool useLegacyDynamicResControl = false)
        {
            Initialize(width, height);

            if (useLegacyDynamicResControl)
                m_HardwareDynamicResRequested = true;
        }

        /// <summary>
        /// Release memory of a RTHandle from the RTHandle System
        /// </summary>
        /// <param name="rth">RTHandle that should be released.</param>
        public void Release(RTHandle rth)
        {
            if (rth != null)
            {
                Assert.AreEqual(this, rth.m_Owner);
                rth.Release();
            }
        }

        internal void Remove(RTHandle rth)
        {
            m_AutoSizedRTs.Remove(rth);
        }

        /// <summary>
        /// Reset the reference size of the system and reallocate all textures.
        /// </summary>
        /// <param name="width">New width.</param>
        /// <param name="height">New height.</param>
        public void ResetReferenceSize(int width, int height)
        {
            m_MaxWidths = width;
            m_MaxHeights = height;
            SetReferenceSize(width, height, reset: true);
        }

        /// <summary>
        /// Sets the reference rendering size for subsequent rendering for the RTHandle System
        /// </summary>
        /// <param name="width">Reference rendering width for subsequent rendering.</param>
        /// <param name="height">Reference rendering height for subsequent rendering.</param>
        public void SetReferenceSize(int width, int height)
        {
            SetReferenceSize(width, height, false);
        }

        /// <summary>
        /// Sets the reference rendering size for subsequent rendering for the RTHandle System
        /// </summary>
        /// <param name="width">Reference rendering width for subsequent rendering.</param>
        /// <param name="height">Reference rendering height for subsequent rendering.</param>
        /// <param name="reset">If set to true, the new width and height will override the old values even if they are not bigger.</param>
        public void SetReferenceSize(int width, int height, bool reset)
        {
            m_RTHandleProperties.previousViewportSize = m_RTHandleProperties.currentViewportSize;
            m_RTHandleProperties.previousRenderTargetSize = m_RTHandleProperties.currentRenderTargetSize;
            Vector2 lastFrameMaxSize = new Vector2(GetMaxWidth(), GetMaxHeight());

            width = Mathf.Max(width, 1);
            height = Mathf.Max(height, 1);

#if UNITY_EDITOR
            // If the reference size is significantly higher than the current actualWidth/Height and it is larger than 1440p dimensions, we reset the reference size every several frames
            // in editor to avoid issues if a large resolution was temporarily set.
            const int resetInterval = 100;
            if (((m_MaxWidths / (float)width) > 2.0f && m_MaxWidths > 2560) ||
                ((m_MaxHeights / (float)height) > 2.0f && m_MaxHeights > 1440))
            {
                if (m_FramesSinceLastReset > resetInterval)
                {
                    m_FramesSinceLastReset = 0;
                    ResetReferenceSize(width, height);
                }
                m_FramesSinceLastReset++;
            }
            else
            {
                // If some cameras is a reasonable resolution size, we dont reset.
                m_FramesSinceLastReset = 0;
            }
#endif

            bool sizeChanged = width > GetMaxWidth() || height > GetMaxHeight() || reset;
            if (sizeChanged)
            {
                Resize(width, height, sizeChanged);
            }

            m_RTHandleProperties.currentViewportSize = new Vector2Int(width, height);
            m_RTHandleProperties.currentRenderTargetSize = new Vector2Int(GetMaxWidth(), GetMaxHeight());

            // If the currentViewportSize is 0, it mean we are the first frame of rendering (can happen when doing domain reload for example or for reflection probe)
            // in this case the scalePrevious below could be invalided. But some effect rely on having a correct value like TAA with the history buffer for the first frame.
            // to work around this, when we detect that size is 0, we setup previous size to current size.
            if (m_RTHandleProperties.previousViewportSize.x == 0)
            {
                m_RTHandleProperties.previousViewportSize = m_RTHandleProperties.currentViewportSize;
                m_RTHandleProperties.previousRenderTargetSize = m_RTHandleProperties.currentRenderTargetSize;
                lastFrameMaxSize = new Vector2(GetMaxWidth(), GetMaxHeight());
            }

            var scales = CalculateRatioAgainstMaxSize(m_RTHandleProperties.currentViewportSize);
            if (DynamicResolutionHandler.instance.HardwareDynamicResIsEnabled() && m_HardwareDynamicResRequested)
            {
                // Making the final scale in 'drs' space, since the final scale must account for rounding pixel values.
                m_RTHandleProperties.rtHandleScale = new Vector4(scales.x, scales.y, m_RTHandleProperties.rtHandleScale.x, m_RTHandleProperties.rtHandleScale.y);
            }
            else
            {
                Vector2 scalePrevious = m_RTHandleProperties.previousViewportSize / lastFrameMaxSize;
                m_RTHandleProperties.rtHandleScale = new Vector4(scales.x, scales.y, scalePrevious.x, scalePrevious.y);
            }
        }

        internal Vector2 CalculateRatioAgainstMaxSize(in Vector2Int viewportSize)
        {
            Vector2 maxSize = new Vector2(GetMaxWidth(), GetMaxHeight());

            if (DynamicResolutionHandler.instance.HardwareDynamicResIsEnabled() && m_HardwareDynamicResRequested && viewportSize != DynamicResolutionHandler.instance.finalViewport)
            {
                //for hardware resolution, the final goal is to figure out a scale from finalViewport into maxViewport.
                //This is however wrong! because the actualViewport might not fit the finalViewport perfectly, due to rounding.
                //A correct way is to instead downscale the maxViewport, and keep the final scale in terms of downsampled buffers.
                Vector2 currentScale = (Vector2)viewportSize / (Vector2)DynamicResolutionHandler.instance.finalViewport;
                maxSize = DynamicResolutionHandler.instance.ApplyScalesOnSize(new Vector2Int(GetMaxWidth(), GetMaxHeight()), currentScale);
            }

            return new Vector2((float)viewportSize.x / maxSize.x, (float)viewportSize.y / maxSize.y);
        }

        /// <summary>
        /// Enable or disable hardware dynamic resolution for the RTHandle System
        /// </summary>
        /// <param name="enableHWDynamicRes">State of hardware dynamic resolution.</param>
        public void SetHardwareDynamicResolutionState(bool enableHWDynamicRes)
        {
            if (enableHWDynamicRes != m_HardwareDynamicResRequested)
            {
                m_HardwareDynamicResRequested = enableHWDynamicRes;

                Array.Resize(ref m_AutoSizedRTsArray, m_AutoSizedRTs.Count);
                m_AutoSizedRTs.CopyTo(m_AutoSizedRTsArray);
                for (int i = 0, c = m_AutoSizedRTsArray.Length; i < c; ++i)
                {
                    var rth = m_AutoSizedRTsArray[i];

                    // Grab the render texture
                    var renderTexture = rth.m_RT;
                    if (renderTexture)
                    {
                        // Free the previous version
                        renderTexture.Release();

                        renderTexture.useDynamicScale = m_HardwareDynamicResRequested && rth.m_EnableHWDynamicScale;

                        // Create the render texture
                        renderTexture.Create();
                    }
                }
            }
        }

        internal void SwitchResizeMode(RTHandle rth, ResizeMode mode)
        {
            // Don't do anything is scaling isn't enabled on this RT
            // TODO: useScaling should probably be moved to ResizeMode.Fixed or something
            if (!rth.useScaling)
                return;

            switch (mode)
            {
                case ResizeMode.OnDemand:
                    m_AutoSizedRTs.Remove(rth);
                    m_ResizeOnDemandRTs.Add(rth);
                    break;
                case ResizeMode.Auto:
                    // Resize now so it is consistent with other auto resize RTHs
                    if (m_ResizeOnDemandRTs.Contains(rth))
                        DemandResize(rth);
                    m_ResizeOnDemandRTs.Remove(rth);
                    m_AutoSizedRTs.Add(rth);
                    break;
            }
        }

        void DemandResize(RTHandle rth)
        {
            Assert.IsTrue(m_ResizeOnDemandRTs.Contains(rth), "The RTHandle is not an resize on demand handle in this RTHandleSystem. Please call SwitchToResizeOnDemand(rth, true) before resizing on demand.");

            // Grab the render texture
            var rt = rth.m_RT;
            rth.referenceSize = new Vector2Int(m_MaxWidths, m_MaxHeights);
            var scaledSize = rth.GetScaledSize(rth.referenceSize);
            scaledSize = Vector2Int.Max(Vector2Int.one, scaledSize);

            // Did the size change?
            var sizeChanged = rt.width != scaledSize.x || rt.height != scaledSize.y;

            if (sizeChanged)
            {
                // Free this render texture
                rt.Release();

                // Update the size
                rt.width = scaledSize.x;
                rt.height = scaledSize.y;

                // Generate a new name
                rt.name = CoreUtils.GetRenderTargetAutoName(
                    rt.width,
                    rt.height,
                    rt.volumeDepth,
                    (rt.depthStencilFormat!=GraphicsFormat.None)? rt.depthStencilFormat : rt.graphicsFormat,
                    rt.dimension,
                    rth.m_Name,
                    mips: rt.useMipMap,
                    enableMSAA: rth.m_EnableMSAA,
                    msaaSamples: (MSAASamples)rt.antiAliasing,
                    dynamicRes: rt.useDynamicScale,
                    dynamicResExplicit: rt.useDynamicScaleExplicit
                );

                // Create the new texture
                rt.Create();
            }
        }

        /// <summary>
        /// Returns the maximum allocated width of the RTHandle System.
        /// </summary>
        /// <returns>Maximum allocated width of the RTHandle System.</returns>
        public int GetMaxWidth() { return m_MaxWidths; }
        /// <summary>
        /// Returns the maximum allocated height of the RTHandle System.
        /// </summary>
        /// <returns>Maximum allocated height of the RTHandle System.</returns>
        public int GetMaxHeight() { return m_MaxHeights; }

        void Dispose(bool disposing)
        {
            if (disposing)
            {
                Array.Resize(ref m_AutoSizedRTsArray, m_AutoSizedRTs.Count);
                m_AutoSizedRTs.CopyTo(m_AutoSizedRTsArray);
                for (int i = 0, c = m_AutoSizedRTsArray.Length; i < c; ++i)
                {
                    var rt = m_AutoSizedRTsArray[i];
                    Release(rt);
                }
                m_AutoSizedRTs.Clear();

                Array.Resize(ref m_AutoSizedRTsArray, m_ResizeOnDemandRTs.Count);
                m_ResizeOnDemandRTs.CopyTo(m_AutoSizedRTsArray);
                for (int i = 0, c = m_AutoSizedRTsArray.Length; i < c; ++i)
                {
                    var rt = m_AutoSizedRTsArray[i];
                    Release(rt);
                }
                m_ResizeOnDemandRTs.Clear();
                m_AutoSizedRTsArray = null;
            }
        }

        void Resize(int width, int height, bool sizeChanged)
        {
            m_MaxWidths = Math.Max(width, m_MaxWidths);
            m_MaxHeights = Math.Max(height, m_MaxHeights);

            var maxSize = new Vector2Int(m_MaxWidths, m_MaxHeights);

            Array.Resize(ref m_AutoSizedRTsArray, m_AutoSizedRTs.Count);
            m_AutoSizedRTs.CopyTo(m_AutoSizedRTsArray);

            for (int i = 0, c = m_AutoSizedRTsArray.Length; i < c; ++i)
            {
                // Grab the RT Handle
                var rth = m_AutoSizedRTsArray[i];

                // Force its new reference size
                rth.referenceSize = maxSize;

                // Grab the render texture
                var renderTexture = rth.m_RT;

                // Free the previous version
                renderTexture.Release();

                // Get the scaled size
                var scaledSize = rth.GetScaledSize(maxSize);

                renderTexture.width = Mathf.Max(scaledSize.x, 1);
                renderTexture.height = Mathf.Max(scaledSize.y, 1);

                // Regenerate the name
                renderTexture.name = CoreUtils.GetRenderTargetAutoName(renderTexture.width, renderTexture.height, renderTexture.volumeDepth
                    , (renderTexture.depthStencilFormat != GraphicsFormat.None) ? renderTexture.depthStencilFormat : renderTexture.graphicsFormat
                    , renderTexture.dimension, rth.m_Name, mips: renderTexture.useMipMap, enableMSAA: rth.m_EnableMSAA
                    , msaaSamples: (MSAASamples)renderTexture.antiAliasing, dynamicRes: renderTexture.useDynamicScale, dynamicResExplicit: renderTexture.useDynamicScaleExplicit);

                // Create the render texture
                renderTexture.Create();
            }
        }

        /// <summary>
        /// Allocate a new fixed sized RTHandle.
        /// </summary>
        /// <param name="width">With of the RTHandle.</param>
        /// <param name="height">height of the RTHandle.</param>
        /// <param name="slices">Number of slices of the RTHandle.</param>
        /// <param name="depthBufferBits">Bit depths of a depth buffer.</param>
        /// <param name="colorFormat">GraphicsFormat of a color buffer.</param>
        /// <param name="filterMode">Filtering mode of the RTHandle.</param>
        /// <param name="wrapMode">Addressing mode of the RTHandle.</param>
        /// <param name="dimension">Texture dimension of the RTHandle.</param>
        /// <param name="enableRandomWrite">Set to true to enable UAV random read writes on the texture.</param>
        /// <param name="useMipMap">Set to true if the texture should have mipmaps.</param>
        /// <param name="autoGenerateMips">Set to true to automatically generate mipmaps.</param>
        /// <param name="isShadowMap">Set to true if the depth buffer should be used as a shadow map.</param>
        /// <param name="anisoLevel">Anisotropic filtering level.</param>
        /// <param name="mipMapBias">Bias applied to mipmaps during filtering.</param>
        /// <param name="msaaSamples">Number of MSAA samples for the RTHandle.</param>
        /// <param name="bindTextureMS">Set to true if the texture needs to be bound as a multisampled texture in the shader.</param>
        /// <param name="useDynamicScale">[See Dynamic Resolution documentation](https://docs.unity3d.com/Manual/DynamicResolution.html)</param>
        /// <param name="useDynamicScaleExplicit">[See Dynamic Resolution documentation](https://docs.unity3d.com/Manual/DynamicResolution.html)</param>
        /// <param name="memoryless">Use this property to set the render texture memoryless modes.</param>
        /// <param name="vrUsage">Special treatment of the VR eye texture used in stereoscopic rendering.</param>
        /// <param name="name">Name of the RTHandle.</param>
        /// <returns>A new RTHandle.</returns>
        public RTHandle Alloc(
            int width,
            int height,
            int slices = 1,
            DepthBits depthBufferBits = DepthBits.None,
            GraphicsFormat colorFormat = GraphicsFormat.R8G8B8A8_SRGB,
            FilterMode filterMode = FilterMode.Point,
            TextureWrapMode wrapMode = TextureWrapMode.Repeat,
            TextureDimension dimension = TextureDimension.Tex2D,
            bool enableRandomWrite = false,
            bool useMipMap = false,
            bool autoGenerateMips = true,
            bool isShadowMap = false,
            int anisoLevel = 1,
            float mipMapBias = 0f,
            MSAASamples msaaSamples = MSAASamples.None,
            bool bindTextureMS = false,
            bool useDynamicScale = false,
            bool useDynamicScaleExplicit = false,
            RenderTextureMemoryless memoryless = RenderTextureMemoryless.None,
            VRTextureUsage vrUsage = VRTextureUsage.None,
            string name = ""
        )
        {
            var format = (depthBufferBits != DepthBits.None) ? GraphicsFormatUtility.GetDepthStencilFormat((int)depthBufferBits) : colorFormat;

            return Alloc(width, height, format, wrapMode, wrapMode, wrapMode, slices, filterMode, dimension, enableRandomWrite, useMipMap,
                autoGenerateMips, isShadowMap, anisoLevel, mipMapBias, msaaSamples, bindTextureMS, useDynamicScale, useDynamicScaleExplicit, memoryless, vrUsage, name);
        }

        /// <summary>
        /// Allocate a new fixed sized RTHandle.
        /// </summary>
        /// <param name="width">With of the RTHandle.</param>
        /// <param name="height">height of the RTHandle.</param>
        /// <param name="format">GraphicsFormat of a color or depth stencil buffer.</param>
        /// <param name="slices">Number of slices of the RTHandle.</param>
        /// <param name="filterMode">Filtering mode of the RTHandle.</param>
        /// <param name="wrapMode">Addressing mode of the RTHandle.</param>
        /// <param name="dimension">Texture dimension of the RTHandle.</param>
        /// <param name="enableRandomWrite">Set to true to enable UAV random read writes on the texture.</param>
        /// <param name="useMipMap">Set to true if the texture should have mipmaps.</param>
        /// <param name="autoGenerateMips">Set to true to automatically generate mipmaps.</param>
        /// <param name="isShadowMap">Set to true if the depth buffer should be used as a shadow map.</param>
        /// <param name="anisoLevel">Anisotropic filtering level.</param>
        /// <param name="mipMapBias">Bias applied to mipmaps during filtering.</param>
        /// <param name="msaaSamples">Number of MSAA samples for the RTHandle.</param>
        /// <param name="bindTextureMS">Set to true if the texture needs to be bound as a multisampled texture in the shader.</param>
        /// <param name="useDynamicScale">[See Dynamic Resolution documentation](https://docs.unity3d.com/Manual/DynamicResolution.html)</param>
        /// <param name="useDynamicScaleExplicit">[See Dynamic Resolution documentation](https://docs.unity3d.com/Manual/DynamicResolution.html)</param>
        /// <param name="memoryless">Use this property to set the render texture memoryless modes.</param>
        /// <param name="vrUsage">Special treatment of the VR eye texture used in stereoscopic rendering.</param>
        /// <param name="name">Name of the RTHandle.</param>
        /// <returns>A new RTHandle.</returns>
        public RTHandle Alloc(
            int width,
            int height,
            GraphicsFormat format,
            int slices = 1,
            FilterMode filterMode = FilterMode.Point,
            TextureWrapMode wrapMode = TextureWrapMode.Repeat,
            TextureDimension dimension = TextureDimension.Tex2D,
            bool enableRandomWrite = false,
            bool useMipMap = false,
            bool autoGenerateMips = true,
            bool isShadowMap = false,
            int anisoLevel = 1,
            float mipMapBias = 0f,
            MSAASamples msaaSamples = MSAASamples.None,
            bool bindTextureMS = false,
            bool useDynamicScale = false,
            bool useDynamicScaleExplicit = false,
            RenderTextureMemoryless memoryless = RenderTextureMemoryless.None,
            VRTextureUsage vrUsage = VRTextureUsage.None,
            string name = ""
        )
        {
            return Alloc(width, height, format, wrapMode, wrapMode, wrapMode, slices, filterMode, dimension, enableRandomWrite, useMipMap,
                autoGenerateMips, isShadowMap, anisoLevel, mipMapBias, msaaSamples, bindTextureMS, useDynamicScale, useDynamicScaleExplicit, memoryless, vrUsage, name);
        }

        /// <summary>
        /// Allocate a new fixed sized RTHandle.
        /// </summary>
        /// <param name="width">With of the RTHandle.</param>
        /// <param name="height">height of the RTHandle.</param>
        /// <param name="wrapModeU">U coordinate wrapping mode of the RTHandle.</param>
        /// <param name="wrapModeV">V coordinate wrapping mode of the RTHandle.</param>
        /// <param name="wrapModeW">W coordinate wrapping mode of the RTHandle.</param>
        /// <param name="slices">Number of slices of the RTHandle.</param>
        /// <param name="depthBufferBits">Bit depths of a depth buffer.</param>
        /// <param name="colorFormat">GraphicsFormat of a color buffer.</param>
        /// <param name="filterMode">Filtering mode of the RTHandle.</param>
        /// <param name="dimension">Texture dimension of the RTHandle.</param>
        /// <param name="enableRandomWrite">Set to true to enable UAV random read writes on the texture.</param>
        /// <param name="useMipMap">Set to true if the texture should have mipmaps.</param>
        /// <param name="autoGenerateMips">Set to true to automatically generate mipmaps.</param>
        /// <param name="isShadowMap">Set to true if the depth buffer should be used as a shadow map.</param>
        /// <param name="anisoLevel">Anisotropic filtering level.</param>
        /// <param name="mipMapBias">Bias applied to mipmaps during filtering.</param>
        /// <param name="msaaSamples">Number of MSAA samples for the RTHandle.</param>
        /// <param name="bindTextureMS">Set to true if the texture needs to be bound as a multisampled texture in the shader.</param>
        /// <param name="useDynamicScale">[See Dynamic Resolution documentation](https://docs.unity3d.com/Manual/DynamicResolution.html)</param>
        /// <param name="useDynamicScaleExplicit">[See Dynamic Resolution documentation](https://docs.unity3d.com/Manual/DynamicResolution.html)</param>
        /// <param name="memoryless">Use this property to set the render texture memoryless modes.</param>
        /// <param name="vrUsage">Special treatment of the VR eye texture used in stereoscopic rendering.</param>
        /// <param name="name">Name of the RTHandle.</param>
        /// <returns>A new RTHandle.</returns>
        public RTHandle Alloc(
            int width,
            int height,
            TextureWrapMode wrapModeU,
            TextureWrapMode wrapModeV,
            TextureWrapMode wrapModeW = TextureWrapMode.Repeat,
            int slices = 1,
            DepthBits depthBufferBits = DepthBits.None,
            GraphicsFormat colorFormat = GraphicsFormat.R8G8B8A8_SRGB,
            FilterMode filterMode = FilterMode.Point,
            TextureDimension dimension = TextureDimension.Tex2D,
            bool enableRandomWrite = false,
            bool useMipMap = false,
            bool autoGenerateMips = true,
            bool isShadowMap = false,
            int anisoLevel = 1,
            float mipMapBias = 0f,
            MSAASamples msaaSamples = MSAASamples.None,
            bool bindTextureMS = false,
            bool useDynamicScale = false,
            bool useDynamicScaleExplicit = false,
            RenderTextureMemoryless memoryless = RenderTextureMemoryless.None,
            VRTextureUsage vrUsage = VRTextureUsage.None,
            string name = ""
        )
        {
            var format = (depthBufferBits != DepthBits.None) ? GraphicsFormatUtility.GetDepthStencilFormat((int)depthBufferBits) : colorFormat;

            return Alloc(
                 width,
                 height,
                 format,
                 wrapModeU,
                 wrapModeV,
                 wrapModeW,
                 slices,
                 filterMode,
                 dimension,
                 enableRandomWrite,
                 useMipMap,
                 autoGenerateMips ,
                 isShadowMap,
                 anisoLevel,
                 mipMapBias,
                 msaaSamples,
                 bindTextureMS,
                 useDynamicScale,
                 useDynamicScaleExplicit,
                 memoryless,
                 vrUsage,
                 name
             );
        }

        /// <summary>
        /// Allocate a new fixed sized RTHandle.
        /// </summary>
        /// <param name="width">With of the RTHandle.</param>
        /// <param name="height">height of the RTHandle.</param>
        /// <param name="format">GraphicsFormat of the color or a depth stencil buffer.</param>
        /// <param name="wrapModeU">U coordinate wrapping mode of the RTHandle.</param>
        /// <param name="wrapModeV">V coordinate wrapping mode of the RTHandle.</param>
        /// <param name="wrapModeW">W coordinate wrapping mode of the RTHandle.</param>
        /// <param name="slices">Number of slices of the RTHandle.</param>
        /// <param name="filterMode">Filtering mode of the RTHandle.</param>
        /// <param name="dimension">Texture dimension of the RTHandle.</param>
        /// <param name="enableRandomWrite">Set to true to enable UAV random read writes on the texture.</param>
        /// <param name="useMipMap">Set to true if the texture should have mipmaps.</param>
        /// <param name="autoGenerateMips">Set to true to automatically generate mipmaps.</param>
        /// <param name="isShadowMap">Set to true if the depth buffer should be used as a shadow map.</param>
        /// <param name="anisoLevel">Anisotropic filtering level.</param>
        /// <param name="mipMapBias">Bias applied to mipmaps during filtering.</param>
        /// <param name="msaaSamples">Number of MSAA samples for the RTHandle.</param>
        /// <param name="bindTextureMS">Set to true if the texture needs to be bound as a multisampled texture in the shader.</param>
        /// <param name="useDynamicScale">[See Dynamic Resolution documentation](https://docs.unity3d.com/Manual/DynamicResolution.html)</param>
        /// <param name="useDynamicScaleExplicit">[See Dynamic Resolution documentation](https://docs.unity3d.com/Manual/DynamicResolution.html)</param>
        /// <param name="memoryless">Use this property to set the render texture memoryless modes.</param>
        /// <param name="vrUsage">Special treatment of the VR eye texture used in stereoscopic rendering.</param>
        /// <param name="name">Name of the RTHandle.</param>
        /// <returns>A new RTHandle.</returns>
        public RTHandle Alloc(
            int width,
            int height,
            GraphicsFormat format,
            TextureWrapMode wrapModeU,
            TextureWrapMode wrapModeV,
            TextureWrapMode wrapModeW = TextureWrapMode.Repeat,
            int slices = 1,
            FilterMode filterMode = FilterMode.Point,
            TextureDimension dimension = TextureDimension.Tex2D,
            bool enableRandomWrite = false,
            bool useMipMap = false,
            bool autoGenerateMips = true,
            bool isShadowMap = false,
            int anisoLevel = 1,
            float mipMapBias = 0f,
            MSAASamples msaaSamples = MSAASamples.None,
            bool bindTextureMS = false,
            bool useDynamicScale = false,
            bool useDynamicScaleExplicit = false,
            RenderTextureMemoryless memoryless = RenderTextureMemoryless.None,
            VRTextureUsage vrUsage = VRTextureUsage.None,
            string name = ""
        )
        {
            var rt = CreateRenderTexture(
                width, height, format, slices, filterMode, wrapModeU, wrapModeV, wrapModeW, dimension, enableRandomWrite, useMipMap
                , autoGenerateMips, isShadowMap, anisoLevel, mipMapBias, msaaSamples, bindTextureMS
                , useDynamicScale, useDynamicScaleExplicit, memoryless, vrUsage, false, name);

            var newRT = new RTHandle(this);
            newRT.SetRenderTexture(rt);
            newRT.useScaling = false;
            newRT.m_EnableRandomWrite = enableRandomWrite;
            newRT.m_EnableMSAA = msaaSamples != MSAASamples.None;
            newRT.m_EnableHWDynamicScale = useDynamicScale;
            newRT.m_Name = name;

            newRT.referenceSize = new Vector2Int(width, height);

            return newRT;
        }

        private RenderTexture CreateRenderTexture(
            int width,
            int height,
            GraphicsFormat format,
            int slices,
            FilterMode filterMode,
            TextureWrapMode wrapModeU,
            TextureWrapMode wrapModeV,
            TextureWrapMode wrapModeW,
            TextureDimension dimension,
            bool enableRandomWrite,
            bool useMipMap,
            bool autoGenerateMips,
            bool isShadowMap,
            int anisoLevel,
            float mipMapBias,
            MSAASamples msaaSamples,
            bool bindTextureMS,
            bool useDynamicScale,
            bool useDynamicScaleExplicit,
            RenderTextureMemoryless memoryless,
            VRTextureUsage vrUsage,
            bool enableShadingRate,
            string name)
        {
            bool enableMSAA = msaaSamples != MSAASamples.None;
            // Here user made a mistake in setting up msaa/bindMS, hence the warning
            if (!enableMSAA && bindTextureMS == true)
            {
                Debug.LogWarning("RTHandle allocated without MSAA but with bindMS set to true, forcing bindMS to false.");
                bindTextureMS = false;
            }

            // MSAA Does not support random read/write.
            if (enableMSAA && (enableRandomWrite == true))
            {
                Debug.LogWarning("RTHandle that is MSAA-enabled cannot allocate MSAA RT with 'enableRandomWrite = true'.");
                enableRandomWrite = false;
            }

            bool isDepthStencilFormat = GraphicsFormatUtility.IsDepthStencilFormat(format);

            if (enableShadingRate && (isShadowMap || isDepthStencilFormat))
            {
                Debug.LogWarning("RTHandle allocated with incompatible enableShadingRate, forcing enableShadingRate to false.");
                enableShadingRate = false;
            }

            string fullName;
            GraphicsFormat colorFormat, depthStencilFormat, stencilFormat;
            ShadowSamplingMode shadowSamplingMode = ShadowSamplingMode.None;

            if (isShadowMap)
            {
                //This is the same "magic" behavior like setting the desc.colorFormat = RenderTextureFormat.Shadowmap
                //We elevate the magic to here to only use the explict properties (graphicsFormat, depthStencilFormat, ShadowSamplingMode) from now.
                int depthBits = GraphicsFormatUtility.GetDepthBits(format);
                if (depthBits < 16) depthBits = 16;

                depthStencilFormat = GraphicsFormatUtility.GetDepthStencilFormat(depthBits, 0);
                colorFormat = GraphicsFormat.None;
                stencilFormat = GraphicsFormat.None;
                shadowSamplingMode = ShadowSamplingMode.CompareDepths;

                fullName = CoreUtils.GetRenderTargetAutoName(width, height, slices, RenderTextureFormat.Shadowmap, name, mips: useMipMap, enableMSAA: enableMSAA, msaaSamples: msaaSamples);
            }
            else if (isDepthStencilFormat)
            {
                //depth stencil texture
                colorFormat = GraphicsFormat.None;
                depthStencilFormat = format;
                stencilFormat = GetStencilFormat(format);

                fullName = CoreUtils.GetRenderTargetAutoName(width, height, slices, format, dimension, name, mips: useMipMap, enableMSAA: enableMSAA, msaaSamples: msaaSamples, dynamicRes: useDynamicScale, dynamicResExplicit: useDynamicScaleExplicit);
            }
            else
            {
                // color texture
                colorFormat = format;
                depthStencilFormat = GraphicsFormat.None;
                stencilFormat = GraphicsFormat.None;

                fullName = CoreUtils.GetRenderTargetAutoName(width, height, slices, format, dimension, name, mips: useMipMap, enableMSAA: enableMSAA, msaaSamples: msaaSamples, dynamicRes: useDynamicScale, dynamicResExplicit: useDynamicScaleExplicit);
            }

            var desc = new RenderTextureDescriptor(width, height, colorFormat, depthStencilFormat)
            {
                msaaSamples = (int)msaaSamples,
                volumeDepth = slices,
                stencilFormat = stencilFormat,
                dimension = dimension,
                shadowSamplingMode = shadowSamplingMode,
                vrUsage = vrUsage,
                memoryless = memoryless,
                useMipMap = useMipMap,
                autoGenerateMips = autoGenerateMips,
                enableRandomWrite = enableRandomWrite,
                bindMS = bindTextureMS,
                useDynamicScale = m_HardwareDynamicResRequested && useDynamicScale,
                useDynamicScaleExplicit = m_HardwareDynamicResRequested && useDynamicScaleExplicit,
                enableShadingRate = enableShadingRate,
            };

            var rt = new RenderTexture(desc);

            rt.name = fullName;

            rt.anisoLevel = anisoLevel;
            rt.mipMapBias = mipMapBias;
            rt.hideFlags = HideFlags.HideAndDontSave;
            rt.filterMode = filterMode;

            rt.wrapModeU = wrapModeU;
            rt.wrapModeV = wrapModeV;
            rt.wrapModeW = wrapModeW;

            rt.Create();
            return rt;
        }

        /// <summary>
        /// Allocate a new fixed sized RTHandle.
        /// </summary>
        /// <param name="width">With of the RTHandle.</param>
        /// <param name="height">Height of the RTHandle.</param>
        /// <param name="info">Struct containing details of allocation</param>
        /// <returns>A new RTHandle.</returns>
        /// <remarks>
        /// Width and height are usually in pixels but if enableShadingRate is set to true, width and height are in tiles.
        /// See also <a href="https://docs.unity3d.com/Manual/variable-rate-shading">Variable Rate Shading</a>.
        /// </remarks>
        public RTHandle Alloc(int width, int height, RTHandleAllocInfo info)
        {
            var rt = CreateRenderTexture(
                 width, height, info.format, info.slices, info.filterMode, info.wrapModeU, info.wrapModeV, info.wrapModeW, info.dimension, info.enableRandomWrite, info.useMipMap
                 , info.autoGenerateMips, info.isShadowMap, info.anisoLevel, info.mipMapBias, info.msaaSamples, info.bindTextureMS
                 , info.useDynamicScale, info.useDynamicScaleExplicit, info.memoryless, info.vrUsage, info.enableShadingRate, info.name);

            var newRT = new RTHandle(this);
            newRT.SetRenderTexture(rt);
            newRT.useScaling = false;
            newRT.m_EnableRandomWrite = info.enableRandomWrite;
            newRT.m_EnableMSAA = info.msaaSamples != MSAASamples.None;
            newRT.m_EnableHWDynamicScale = info.useDynamicScale;
            newRT.m_Name = info.name;

            newRT.referenceSize = new Vector2Int(width, height);

            if (info.enableShadingRate)
            {
                // even though allocation ask for an explicit size, it's possible the
                // resize mode is changed afterward hence assigning the scaling function
                // because shading rate image resolution is in tiles
                newRT.scaleFunc = (refSize) => ShadingRateImage.GetAllocTileSize(refSize);
            }

            return newRT;
        }

        // Next two methods are used to allocate RenderTexture that depend on the frame settings (resolution and msaa for now)
        // RenderTextures allocated this way are meant to be defined by a scale of camera resolution (full/half/quarter resolution for example).
        // The idea is that internally the system will scale up the size of all render texture so that it amortizes with time and not reallocate when a smaller size is required (which is what happens with TemporaryRTs).
        // Since MSAA cannot be changed on the fly for a given RenderTexture, a separate instance will be created if the user requires it. This instance will be the one used after the next call of SetReferenceSize if MSAA is required.

        /// <summary>
        /// Calculate the dimensions (in pixels) of the RTHandles given the scale factor.
        /// </summary>
        /// <param name="scaleFactor">The scale factor to use when calculating the dimensions. The base unscaled size used, is the sizes passed to the last ResetReferenceSize call.</param>
        /// <returns>The calculated dimensions.</returns>
        public Vector2Int CalculateDimensions(Vector2 scaleFactor)
        {
            return CalculateDimensions(scaleFactor, new Vector2Int(GetMaxWidth(), GetMaxHeight()));
        }

        static Vector2Int CalculateDimensions(Vector2 scaleFactor, Vector2Int size)
        {
            return new Vector2Int(
                Mathf.Max(Mathf.RoundToInt(scaleFactor.x * size.x), 1),
                Mathf.Max(Mathf.RoundToInt(scaleFactor.y * size.y), 1)
            );
        }

        /// <summary>
        /// Allocate a new automatically sized RTHandle.
        /// </summary>
        /// <param name="scaleFactor">Constant scale for the RTHandle size computation.</param>
        /// <param name="format">GraphicsFormat of a color or depth stencil buffer.</param>
        /// <param name="slices">Number of slices of the RTHandle.</param>
        /// <param name="filterMode">Filtering mode of the RTHandle.</param>
        /// <param name="wrapMode">Addressing mode of the RTHandle.</param>
        /// <param name="dimension">Texture dimension of the RTHandle.</param>
        /// <param name="enableRandomWrite">Set to true to enable UAV random read writes on the texture.</param>
        /// <param name="useMipMap">Set to true if the texture should have mipmaps.</param>
        /// <param name="autoGenerateMips">Set to true to automatically generate mipmaps.</param>
        /// <param name="isShadowMap">Set to true if the depth buffer should be used as a shadow map.</param>
        /// <param name="anisoLevel">Anisotropic filtering level.</param>
        /// <param name="mipMapBias">Bias applied to mipmaps during filtering.</param>
        /// <param name="msaaSamples">Number of MSAA samples.</param>
        /// <param name="bindTextureMS">Set to true if the texture needs to be bound as a multisampled texture in the shader.</param>
        /// <param name="useDynamicScale">[See Dynamic Resolution documentation](https://docs.unity3d.com/Manual/DynamicResolution.html)</param>
        /// <param name="useDynamicScaleExplicit">[See Dynamic Resolution documentation](https://docs.unity3d.com/Manual/DynamicResolution.html)</param>
        /// <param name="memoryless">Use this property to set the render texture memoryless modes.</param>
        /// <param name="vrUsage">Special treatment of the VR eye texture used in stereoscopic rendering.</param>
        /// <param name="name">Name of the RTHandle.</param>
        /// <returns>A new RTHandle.</returns>
        public RTHandle Alloc(
            Vector2 scaleFactor,
            GraphicsFormat format,
            int slices = 1,
            FilterMode filterMode = FilterMode.Point,
            TextureWrapMode wrapMode = TextureWrapMode.Repeat,
            TextureDimension dimension = TextureDimension.Tex2D,
            bool enableRandomWrite = false,
            bool useMipMap = false,
            bool autoGenerateMips = true,
            bool isShadowMap = false,
            int anisoLevel = 1,
            float mipMapBias = 0f,
            MSAASamples msaaSamples = MSAASamples.None,
            bool bindTextureMS = false,
            bool useDynamicScale = false,
            bool useDynamicScaleExplicit = false,
            RenderTextureMemoryless memoryless = RenderTextureMemoryless.None,
            VRTextureUsage vrUsage = VRTextureUsage.None,
            string name = ""
        )
        {
            var actualDimensions = CalculateDimensions(scaleFactor);

            bool enableShadingRate = false; // Not supported, use RTHandleAllocInfo API instead.
            var rth = AllocAutoSizedRenderTexture(actualDimensions.x,
                actualDimensions.y,
                slices,
                format,
                filterMode,
                wrapMode,
                dimension,
                enableRandomWrite,
                useMipMap,
                autoGenerateMips,
                isShadowMap,
                anisoLevel,
                mipMapBias,
                msaaSamples,
                bindTextureMS,
                useDynamicScale,
                useDynamicScaleExplicit,
                memoryless,
                vrUsage,
                enableShadingRate,
                name
            );

            rth.referenceSize = actualDimensions;
            rth.scaleFactor = scaleFactor;

            return rth;
        }

        /// <summary>
        /// Allocate a new automatically sized RTHandle.
        /// </summary>
        /// <param name="scaleFactor">Constant scale for the RTHandle size computation.</param>
        /// <param name="slices">Number of slices of the RTHandle.</param>
        /// <param name="depthBufferBits">Bit depths of a depth buffer.</param>
        /// <param name="colorFormat">GraphicsFormat of a color buffer.</param>
        /// <param name="filterMode">Filtering mode of the RTHandle.</param>
        /// <param name="wrapMode">Addressing mode of the RTHandle.</param>
        /// <param name="dimension">Texture dimension of the RTHandle.</param>
        /// <param name="enableRandomWrite">Set to true to enable UAV random read writes on the texture.</param>
        /// <param name="useMipMap">Set to true if the texture should have mipmaps.</param>
        /// <param name="autoGenerateMips">Set to true to automatically generate mipmaps.</param>
        /// <param name="isShadowMap">Set to true if the depth buffer should be used as a shadow map.</param>
        /// <param name="anisoLevel">Anisotropic filtering level.</param>
        /// <param name="mipMapBias">Bias applied to mipmaps during filtering.</param>
        /// <param name="msaaSamples">Number of MSAA samples.</param>
        /// <param name="bindTextureMS">Set to true if the texture needs to be bound as a multisampled texture in the shader.</param>
        /// <param name="useDynamicScale">[See Dynamic Resolution documentation](https://docs.unity3d.com/Manual/DynamicResolution.html)</param>
        /// <param name="useDynamicScaleExplicit">[See Dynamic Resolution documentation](https://docs.unity3d.com/Manual/DynamicResolution.html)</param>
        /// <param name="memoryless">Use this property to set the render texture memoryless modes.</param>
        /// <param name="vrUsage">Special treatment of the VR eye texture used in stereoscopic rendering.</param>
        /// <param name="name">Name of the RTHandle.</param>
        /// <returns>A new RTHandle.</returns>
        public RTHandle Alloc(
            Vector2 scaleFactor,
            int slices = 1,
            DepthBits depthBufferBits = DepthBits.None,
            GraphicsFormat colorFormat = GraphicsFormat.R8G8B8A8_SRGB,
            FilterMode filterMode = FilterMode.Point,
            TextureWrapMode wrapMode = TextureWrapMode.Repeat,
            TextureDimension dimension = TextureDimension.Tex2D,
            bool enableRandomWrite = false,
            bool useMipMap = false,
            bool autoGenerateMips = true,
            bool isShadowMap = false,
            int anisoLevel = 1,
            float mipMapBias = 0f,
            MSAASamples msaaSamples = MSAASamples.None,
            bool bindTextureMS = false,
            bool useDynamicScale = false,
            bool useDynamicScaleExplicit = false,
            RenderTextureMemoryless memoryless = RenderTextureMemoryless.None,
            VRTextureUsage vrUsage = VRTextureUsage.None,
            string name = ""
        )
        {
            var format = (depthBufferBits != DepthBits.None) ? GraphicsFormatUtility.GetDepthStencilFormat((int)depthBufferBits) : colorFormat;

            return Alloc(scaleFactor,
                format,
                slices,
                filterMode,
                wrapMode,
                dimension,
                enableRandomWrite,
                useMipMap,
                autoGenerateMips,
                isShadowMap,
                anisoLevel,
                mipMapBias,
                msaaSamples,
                bindTextureMS,
                useDynamicScale,
                useDynamicScaleExplicit,
                memoryless,
                vrUsage,
                name
            );
        }

        /// <summary>
        /// Allocate a new automatically sized RTHandle.
        /// </summary>
        /// <param name="scaleFactor">Constant scale for the RTHandle size computation.</param>
        /// <param name="info">Struct containing details of allocation</param>
        /// <returns>A new RTHandle.</returns>
        /// <remarks>
        /// scaleFactor is expected to be based on the reference size in pixels. If enableShadingRate is set to true,
        /// conversion in tiles is implicitly done prior to allocation.
        /// See also <a href="https://docs.unity3d.com/Manual/variable-rate-shading">Variable Rate Shading</a>.
        /// </remarks>
        public RTHandle Alloc(Vector2 scaleFactor, RTHandleAllocInfo info)
        {
            int width = Mathf.Max(Mathf.RoundToInt(scaleFactor.x * GetMaxWidth()), 1);
            int height = Mathf.Max(Mathf.RoundToInt(scaleFactor.y * GetMaxHeight()), 1);

            var rth = AllocAutoSizedRenderTexture(width, height, info);
            rth.referenceSize = new Vector2Int(width, height);

            if (info.enableShadingRate)
            {
                // shading rate image resolution is in tiles; adjust refSize
                rth.scaleFunc = (refSize) =>
                {
                    var dimensions = CalculateDimensions(scaleFactor, refSize);
                    return ShadingRateImage.GetAllocTileSize(dimensions);
                };
            }
            else
                rth.scaleFactor = scaleFactor;

            return rth;
        }

        //
        // You can provide your own scaling function for advanced scaling schemes (e.g. scaling to
        // the next POT). The function takes a Vec2 as parameter that holds max width & height
        // values for the current manager context and returns a Vec2 of the final size in pixels.
        //
        // var rth = Alloc(
        //     size => new Vector2Int(size.x / 2, size.y),
        //     [...]
        // );
        //

        /// <summary>
        /// Calculate the dimensions (in pixels) of the RTHandles given the scale function. The base unscaled size used, is the sizes passed to the last ResetReferenceSize call.
        /// </summary>
        /// <param name="scaleFunc">The scale function to use when calculating the dimensions.</param>
        /// <returns>The calculated dimensions.</returns>
        public Vector2Int CalculateDimensions(ScaleFunc scaleFunc)
        {
            var scaleFactor = scaleFunc(new Vector2Int(GetMaxWidth(), GetMaxHeight()));
            return new Vector2Int(
                Mathf.Max(scaleFactor.x, 1),
                Mathf.Max(scaleFactor.y, 1)
            );
        }

        /// <summary>
        /// Allocate a new automatically sized RTHandle.
        /// </summary>
        /// <param name="scaleFunc">Function used for the RTHandle size computation.</param>
        /// <param name="slices">Number of slices of the RTHandle.</param>
        /// <param name="depthBufferBits">Bit depths of a depth buffer.</param>
        /// <param name="colorFormat">GraphicsFormat of a color buffer.</param>
        /// <param name="filterMode">Filtering mode of the RTHandle.</param>
        /// <param name="wrapMode">Addressing mode of the RTHandle.</param>
        /// <param name="dimension">Texture dimension of the RTHandle.</param>
        /// <param name="enableRandomWrite">Set to true to enable UAV random read writes on the texture.</param>
        /// <param name="useMipMap">Set to true if the texture should have mipmaps.</param>
        /// <param name="autoGenerateMips">Set to true to automatically generate mipmaps.</param>
        /// <param name="isShadowMap">Set to true if the depth buffer should be used as a shadow map.</param>
        /// <param name="anisoLevel">Anisotropic filtering level.</param>
        /// <param name="mipMapBias">Bias applied to mipmaps during filtering.</param>
        /// <param name="msaaSamples">Number of MSAA samples.</param>
        /// <param name="bindTextureMS">Set to true if the texture needs to be bound as a multisampled texture in the shader.</param>
        /// <param name="useDynamicScale">[See Dynamic Resolution documentation](https://docs.unity3d.com/Manual/DynamicResolution.html)</param>
        /// <param name="useDynamicScaleExplicit">[See Dynamic Resolution documentation](https://docs.unity3d.com/Manual/DynamicResolution.html)</param>
        /// <param name="memoryless">Use this property to set the render texture memoryless modes.</param>
        /// <param name="vrUsage">Special treatment of the VR eye texture used in stereoscopic rendering.</param>
        /// <param name="name">Name of the RTHandle.</param>
        /// <returns>A new RTHandle.</returns>
        public RTHandle Alloc(
            ScaleFunc scaleFunc,
            int slices = 1,
            DepthBits depthBufferBits = DepthBits.None,
            GraphicsFormat colorFormat = GraphicsFormat.R8G8B8A8_SRGB,
            FilterMode filterMode = FilterMode.Point,
            TextureWrapMode wrapMode = TextureWrapMode.Repeat,
            TextureDimension dimension = TextureDimension.Tex2D,
            bool enableRandomWrite = false,
            bool useMipMap = false,
            bool autoGenerateMips = true,
            bool isShadowMap = false,
            int anisoLevel = 1,
            float mipMapBias = 0f,
            MSAASamples msaaSamples = MSAASamples.None,
            bool bindTextureMS = false,
            bool useDynamicScale = false,
            bool useDynamicScaleExplicit = false,
            RenderTextureMemoryless memoryless = RenderTextureMemoryless.None,
            VRTextureUsage vrUsage = VRTextureUsage.None,
            string name = ""
        )
        {
            var format = (depthBufferBits != DepthBits.None) ? GraphicsFormatUtility.GetDepthStencilFormat((int)depthBufferBits) : colorFormat;

            return Alloc(scaleFunc,
                format,
                slices,
                filterMode,
                wrapMode,
                dimension,
                enableRandomWrite,
                useMipMap,
                autoGenerateMips,
                isShadowMap,
                anisoLevel,
                mipMapBias,
                msaaSamples,
                bindTextureMS,
                useDynamicScale,
                useDynamicScaleExplicit,
                memoryless,
                vrUsage,
                name
            );
        }

        /// <summary>
        /// Allocate a new automatically sized RTHandle.
        /// </summary>
        /// <param name="scaleFunc">Function used for the RTHandle size computation.</param>
        /// <param name="format">GraphicsFormat of a color or depth stencil buffer.</param>
        /// <param name="slices">Number of slices of the RTHandle.</param>
        /// <param name="filterMode">Filtering mode of the RTHandle.</param>
        /// <param name="wrapMode">Addressing mode of the RTHandle.</param>
        /// <param name="dimension">Texture dimension of the RTHandle.</param>
        /// <param name="enableRandomWrite">Set to true to enable UAV random read writes on the texture.</param>
        /// <param name="useMipMap">Set to true if the texture should have mipmaps.</param>
        /// <param name="autoGenerateMips">Set to true to automatically generate mipmaps.</param>
        /// <param name="isShadowMap">Set to true if the depth buffer should be used as a shadow map.</param>
        /// <param name="anisoLevel">Anisotropic filtering level.</param>
        /// <param name="mipMapBias">Bias applied to mipmaps during filtering.</param>
        /// <param name="msaaSamples">Number of MSAA samples.</param>
        /// <param name="bindTextureMS">Set to true if the texture needs to be bound as a multisampled texture in the shader.</param>
        /// <param name="useDynamicScale">[See Dynamic Resolution documentation](https://docs.unity3d.com/Manual/DynamicResolution.html)</param>
        /// <param name="useDynamicScaleExplicit">[See Dynamic Resolution documentation](https://docs.unity3d.com/Manual/DynamicResolution.html)</param>
        /// <param name="memoryless">Use this property to set the render texture memoryless modes.</param>
        /// <param name="vrUsage">Special treatment of the VR eye texture used in stereoscopic rendering.</param>
        /// <param name="name">Name of the RTHandle.</param>
        /// <returns>A new RTHandle.</returns>
        public RTHandle Alloc(
            ScaleFunc scaleFunc,
            GraphicsFormat format,
            int slices = 1,
            FilterMode filterMode = FilterMode.Point,
            TextureWrapMode wrapMode = TextureWrapMode.Repeat,
            TextureDimension dimension = TextureDimension.Tex2D,
            bool enableRandomWrite = false,
            bool useMipMap = false,
            bool autoGenerateMips = true,
            bool isShadowMap = false,
            int anisoLevel = 1,
            float mipMapBias = 0f,
            MSAASamples msaaSamples = MSAASamples.None,
            bool bindTextureMS = false,
            bool useDynamicScale = false,
            bool useDynamicScaleExplicit = false,
            RenderTextureMemoryless memoryless = RenderTextureMemoryless.None,
            VRTextureUsage vrUsage = VRTextureUsage.None,
            string name = ""
        )
        {
            var actualDimensions = CalculateDimensions(scaleFunc);

            bool enableShadingRate = false; // Not supported, use RTHandleAllocInfo API instead.
            var rth = AllocAutoSizedRenderTexture(actualDimensions.x,
                actualDimensions.y,
                slices,
                format,
                filterMode,
                wrapMode,
                dimension,
                enableRandomWrite,
                useMipMap,
                autoGenerateMips,
                isShadowMap,
                anisoLevel,
                mipMapBias,
                msaaSamples,
                bindTextureMS,
                useDynamicScale,
                useDynamicScaleExplicit,
                memoryless,
                vrUsage,
                enableShadingRate,
                name
            );

            rth.referenceSize = actualDimensions;
            rth.scaleFunc = scaleFunc;

            return rth;
        }

        /// <summary>
        /// Allocate a new automatically sized RTHandle.
        /// </summary>
        /// <param name="scaleFunc">Function used for the RTHandle size computation.</param>
        /// <param name="info">Struct containing details of allocation</param>
        /// <returns>A new RTHandle.</returns>
        /// <remarks>
        /// scaleFunc is expected to receive pixel values. If enableShadingRate is set to true,
        /// conversion in tiles is done prior to allocation so that scaleFunc does not have to handle it.
        /// See also <a href="https://docs.unity3d.com/Manual/variable-rate-shading">Variable Rate Shading</a>.
        /// </remarks>
        public RTHandle Alloc(ScaleFunc scaleFunc, RTHandleAllocInfo info)
        {
            var scaleFactor = scaleFunc(new Vector2Int(GetMaxWidth(), GetMaxHeight()));
            int width = Mathf.Max(scaleFactor.x, 1);
            int height = Mathf.Max(scaleFactor.y, 1);

            var rth = AllocAutoSizedRenderTexture(width, height, info);
            rth.referenceSize = new Vector2Int(width, height);

            if (info.enableShadingRate)
            {
                rth.scaleFunc = (refSize) =>
                {
                    var dimensions = scaleFunc(refSize);

                    // shading rate image resolution is in tiles and current values are in pixels.
                    // Alloc() with a scaling function is based on refSize which is in pixels.
                    // adjust dimensions
                    return ShadingRateImage.GetAllocTileSize(dimensions);
                };
            }
            else
                rth.scaleFunc = scaleFunc;

            return rth;
        }

        internal RTHandle AllocAutoSizedRenderTexture(
            int width,
            int height,
            int slices,
            GraphicsFormat format,
            FilterMode filterMode,
            TextureWrapMode wrapMode,
            TextureDimension dimension,
            bool enableRandomWrite,
            bool useMipMap,
            bool autoGenerateMips,
            bool isShadowMap,
            int anisoLevel,
            float mipMapBias,
            MSAASamples msaaSamples,
            bool bindTextureMS,
            bool useDynamicScale,
            bool useDynamicScaleExplicit,
            RenderTextureMemoryless memoryless,
            VRTextureUsage vrUsage,
            bool enableShadingRate,
            string name
        )
        {
            if (enableShadingRate)
            {
                // this function is called when auto scaling is needed and always expect size in pixels.
                // shading rate image resolution is in tiles and current values are in pixels.
                // then must adjust width and height
                var actualDimensions = ShadingRateImage.GetAllocTileSize(width, height);
                width = actualDimensions.x;
                height = actualDimensions.y;
            }

            var rt = CreateRenderTexture(
                width, height, format, slices, filterMode, wrapMode, wrapMode, wrapMode, dimension, enableRandomWrite, useMipMap
                , autoGenerateMips, isShadowMap, anisoLevel, mipMapBias, msaaSamples, bindTextureMS
                , useDynamicScale, useDynamicScaleExplicit, memoryless, vrUsage, enableShadingRate, name);

            var rth = new RTHandle(this);
            rth.SetRenderTexture(rt);
            rth.m_EnableMSAA = msaaSamples != MSAASamples.None;
            rth.m_EnableRandomWrite = enableRandomWrite;
            rth.useScaling = true;
            rth.m_EnableHWDynamicScale = useDynamicScale;
            rth.m_Name = name;
            m_AutoSizedRTs.Add(rth);
            return rth;
        }

        internal RTHandle AllocAutoSizedRenderTexture(int width, int height, RTHandleAllocInfo info)
        {
            if (info.enableShadingRate)
            {
                // this function is called when auto scaling is needed and always expect size in pixels.
                // shading rate image resolution is in tiles and current values are in pixels.
                // then must adjust width and height
                var actualDimensions = ShadingRateImage.GetAllocTileSize(width, height);
                width = actualDimensions.x;
                height = actualDimensions.y;
            }

            var rt = CreateRenderTexture(
                width, height, info.format, info.slices, info.filterMode, info.wrapModeU, info.wrapModeV, info.wrapModeW, info.dimension, info.enableRandomWrite, info.useMipMap
                , info.autoGenerateMips, info.isShadowMap, info.anisoLevel, info.mipMapBias, info.msaaSamples, info.bindTextureMS
                , info.useDynamicScale, info.useDynamicScaleExplicit, info.memoryless, info.vrUsage, info.enableShadingRate, info.name);

            var rth = new RTHandle(this);
            rth.SetRenderTexture(rt);
            rth.m_EnableMSAA = info.msaaSamples != MSAASamples.None;
            rth.m_EnableRandomWrite = info.enableRandomWrite;
            rth.useScaling = true;
            rth.m_EnableHWDynamicScale = info.useDynamicScale;
            rth.m_Name = info.name;
            m_AutoSizedRTs.Add(rth);
            return rth;
        }

        /// <summary>
        /// Allocate a RTHandle from a regular RenderTexture.
        /// </summary>
        /// <param name="texture">Input texture</param>
        /// <param name="transferOwnership">Says if the RTHandleSystem has the ownership of the external RenderTarget, false by default</param>
        /// <returns>A new RTHandle referencing the input texture.</returns>
        public RTHandle Alloc(RenderTexture texture, bool transferOwnership = false)
        {
            var rth = new RTHandle(this);
#if UNITY_EDITOR
            Debug.Assert(!(EditorUtility.IsPersistent(texture) == true && transferOwnership == true),
                    "RTHandle should not have ownership of RenderTarget asset, set transfer ownership as false");
#endif
            rth.SetRenderTexture(texture, transferOwnership);
            rth.m_EnableMSAA = false;
            rth.m_EnableRandomWrite = false;
            rth.useScaling = false;
            rth.m_EnableHWDynamicScale = false;
            rth.m_Name = texture.name;
            return rth;
        }

        /// <summary>
        /// Allocate a RTHandle from a regular Texture.
        /// </summary>
        /// <param name="texture">Input texture</param>
        /// <returns>A new RTHandle referencing the input texture.</returns>
        public RTHandle Alloc(Texture texture)
        {
            var rth = new RTHandle(this);
            rth.SetTexture(texture);
            rth.m_EnableMSAA = false;
            rth.m_EnableRandomWrite = false;
            rth.useScaling = false;
            rth.m_EnableHWDynamicScale = false;
            rth.m_Name = texture.name;
            return rth;
        }

        /// <summary>
        /// Allocate a RTHandle from a regular render target identifier.
        /// </summary>
        /// <param name="texture">Input render target identifier.</param>
        /// <returns>A new RTHandle referencing the input render target identifier.</returns>
        public RTHandle Alloc(RenderTargetIdentifier texture)
        {
            return Alloc(texture, "");
        }

        /// <summary>
        /// Allocate a RTHandle from a regular render target identifier.
        /// </summary>
        /// <param name="texture">Input render target identifier.</param>
        /// <param name="name">Name of the texture.</param>
        /// <returns>A new RTHandle referencing the input render target identifier.</returns>
        public RTHandle Alloc(RenderTargetIdentifier texture, string name)
        {
            var rth = new RTHandle(this);
            rth.SetTexture(texture);
            rth.m_EnableMSAA = false;
            rth.m_EnableRandomWrite = false;
            rth.useScaling = false;
            rth.m_EnableHWDynamicScale = false;
            rth.m_Name = name;
            return rth;
        }

        private static RTHandle Alloc(RTHandle tex)
        {
            Debug.LogError("Allocation a RTHandle from another one is forbidden.");
            return null;
        }

        internal string DumpRTInfo()
        {
            string result = "";
            Array.Resize(ref m_AutoSizedRTsArray, m_AutoSizedRTs.Count);
            m_AutoSizedRTs.CopyTo(m_AutoSizedRTsArray);
            for (int i = 0, c = m_AutoSizedRTsArray.Length; i < c; ++i)
            {
                var rt = m_AutoSizedRTsArray[i].rt;
                result = string.Format("{0}\nRT ({1})\t Format: {2} W: {3} H {4}\n", result, i, rt.format, rt.width, rt.height);
            }

            return result;
        }

        private GraphicsFormat GetStencilFormat(GraphicsFormat depthStencilFormat)
        {
            return (GraphicsFormatUtility.IsStencilFormat(depthStencilFormat) && SystemInfo.IsFormatSupported(GraphicsFormat.R8_UInt, GraphicsFormatUsage.StencilSampling)) ?
                GraphicsFormat.R8_UInt : GraphicsFormat.None;
        }
    }
}
