using System;
using System.Collections.Generic;
using UnityEngine.Assertions;
using UnityEngine.Experimental.Rendering;

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
        bool                m_HardwareDynamicResRequested = false;
        bool                m_ScaledRTSupportsMSAA = false;
        MSAASamples         m_ScaledRTCurrentMSAASamples = MSAASamples.None;
        HashSet<RTHandle>   m_AutoSizedRTs;
        RTHandle[]          m_AutoSizedRTsArray; // For fast iteration
        HashSet<RTHandle>   m_ResizeOnDemandRTs;
        RTHandleProperties  m_RTHandleProperties;

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
        /// <param name="scaledRTsupportsMSAA">Set to true if automatically scaled RTHandles should support MSAA</param>
        /// <param name="scaledRTMSAASamples">Number of MSAA samples for automatically scaled RTHandles.</param>
        public void Initialize(int width, int height, bool scaledRTsupportsMSAA, MSAASamples scaledRTMSAASamples)
        {
            if (m_AutoSizedRTs.Count != 0)
            {
                string leakingResources = "Unreleased RTHandles:";
                foreach (var rt in m_AutoSizedRTs)
                {
                    leakingResources = string.Format("{0}\n    {1}", leakingResources, rt.name);
                }
                Debug.LogError(string.Format("RTHandle.Initialize should only be called once before allocating any Render Texture. This may be caused by an unreleased RTHandle resource.\n{0}\n", leakingResources));
            }

            m_MaxWidths = width;
            m_MaxHeights = height;

            m_ScaledRTSupportsMSAA = scaledRTsupportsMSAA;
            m_ScaledRTCurrentMSAASamples = scaledRTMSAASamples;

            m_HardwareDynamicResRequested = DynamicResolutionHandler.instance.RequestsHardwareDynamicResolution();
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
            SetReferenceSize(width, height, m_ScaledRTCurrentMSAASamples, reset: true);
        }

        /// <summary>
        /// Sets the reference rendering size for subsequent rendering for the RTHandle System
        /// </summary>
        /// <param name="width">Reference rendering width for subsequent rendering.</param>
        /// <param name="height">Reference rendering height for subsequent rendering.</param>
        /// <param name="msaaSamples">Number of MSAA samples for multisampled textures for subsequent rendering.</param>
        public void SetReferenceSize(int width, int height, MSAASamples msaaSamples)
        {
            SetReferenceSize(width, height, msaaSamples, false);
        }

        /// <summary>
        /// Sets the reference rendering size for subsequent rendering for the RTHandle System
        /// </summary>
        /// <param name="width">Reference rendering width for subsequent rendering.</param>
        /// <param name="height">Reference rendering height for subsequent rendering.</param>
        /// <param name="msaaSamples">Number of MSAA samples for multisampled textures for subsequent rendering.</param>
        /// <param name="reset">If set to true, the new width and height will override the old values even if they are not bigger.</param>
        public void SetReferenceSize(int width, int height, MSAASamples msaaSamples, bool reset)
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

            // If some cameras is requesting the same res as the max res, we don't want to reset
            if (m_MaxWidths == width && m_MaxHeights == height)
                m_FramesSinceLastReset = 0;
#endif

            bool sizeChanged = width > GetMaxWidth() || height > GetMaxHeight() || reset;
            bool msaaSamplesChanged = (msaaSamples != m_ScaledRTCurrentMSAASamples);

            if (sizeChanged || msaaSamplesChanged)
            {
                Resize(width, height, msaaSamples, sizeChanged, msaaSamplesChanged);
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

            if (DynamicResolutionHandler.instance.HardwareDynamicResIsEnabled() && m_HardwareDynamicResRequested)
            {
                Vector2Int maxSize = new Vector2Int(GetMaxWidth(), GetMaxHeight());
                // Making the final scale in 'drs' space, since the final scale must account for rounding pixel values.
                var scaledFinalViewport = DynamicResolutionHandler.instance.ApplyScalesOnSize(DynamicResolutionHandler.instance.finalViewport);
                var scaledMaxSize = DynamicResolutionHandler.instance.ApplyScalesOnSize(maxSize);
                float xScale = (float)scaledFinalViewport.x / (float)scaledMaxSize.x;
                float yScale = (float)scaledFinalViewport.y / (float)scaledMaxSize.y;
                m_RTHandleProperties.rtHandleScale = new Vector4(xScale, yScale, m_RTHandleProperties.rtHandleScale.x, m_RTHandleProperties.rtHandleScale.y);
            }
            else
            {
                Vector2 maxSize = new Vector2(GetMaxWidth(), GetMaxHeight());
                Vector2 scaleCurrent = m_RTHandleProperties.currentViewportSize / maxSize;
                Vector2 scalePrevious = m_RTHandleProperties.previousViewportSize / lastFrameMaxSize;
                m_RTHandleProperties.rtHandleScale = new Vector4(scaleCurrent.x, scaleCurrent.y, scalePrevious.x, scalePrevious.y);
            }
        }

        /// <summary>
        /// Enable or disable hardware dynamic resolution for the RTHandle System
        /// </summary>
        /// <param name="enableHWDynamicRes">State of hardware dynamic resolution.</param>
        public void SetHardwareDynamicResolutionState(bool enableHWDynamicRes)
        {
            if(enableHWDynamicRes != m_HardwareDynamicResRequested)
            {
                m_HardwareDynamicResRequested = enableHWDynamicRes;

                Array.Resize(ref m_AutoSizedRTsArray, m_AutoSizedRTs.Count);
                m_AutoSizedRTs.CopyTo(m_AutoSizedRTsArray);
                for (int i = 0, c = m_AutoSizedRTsArray.Length; i < c; ++i)
                {
                    var rth = m_AutoSizedRTsArray[i];

                    // Grab the render texture
                    var renderTexture = rth.m_RT;
                    if(renderTexture)
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
            // If this is an MSAA texture, did the sample count change?
            var msaaSampleChanged = rth.m_EnableMSAA && rt.antiAliasing != (int)m_ScaledRTCurrentMSAASamples;

            if (sizeChanged || msaaSampleChanged)
            {
                // Free this render texture
                rt.Release();

                // Update the antialiasing count
                if (rth.m_EnableMSAA)
                    rt.antiAliasing = (int)m_ScaledRTCurrentMSAASamples;

                // Update the size
                rt.width = scaledSize.x;
                rt.height = scaledSize.y;

                // Generate a new name
                rt.name = CoreUtils.GetRenderTargetAutoName(
                    rt.width,
                    rt.height,
                    rt.volumeDepth,
                    rt.graphicsFormat,
                    rt.dimension,
                    rth.m_Name,
                    mips: rt.useMipMap,
                    enableMSAA: rth.m_EnableMSAA,
                    msaaSamples: m_ScaledRTCurrentMSAASamples,
                    dynamicRes: rt.useDynamicScale
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

        void Resize(int width, int height, MSAASamples msaaSamples, bool sizeChanged, bool msaaSampleChanged)
        {
            m_MaxWidths = Math.Max(width, m_MaxWidths);
            m_MaxHeights = Math.Max(height, m_MaxHeights);
            m_ScaledRTCurrentMSAASamples = msaaSamples;

            var maxSize = new Vector2Int(m_MaxWidths, m_MaxHeights);

            Array.Resize(ref m_AutoSizedRTsArray, m_AutoSizedRTs.Count);
            m_AutoSizedRTs.CopyTo(m_AutoSizedRTsArray);

            for (int i = 0, c = m_AutoSizedRTsArray.Length; i < c; ++i)
            {
                // Grab the RT Handle
                var rth = m_AutoSizedRTsArray[i];

                // If we are only processing MSAA sample count change, make sure this RT is an MSAA one
                if (!sizeChanged && msaaSampleChanged && !rth.m_EnableMSAA)
                {
                    continue;
                }

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

                // If this is a msaa texture, make sure to update its msaa count
                if (rth.m_EnableMSAA)
                {
                    renderTexture.antiAliasing = (int)m_ScaledRTCurrentMSAASamples;
                }

                // Regenerate the name
                renderTexture.name = CoreUtils.GetRenderTargetAutoName(renderTexture.width, renderTexture.height, renderTexture.volumeDepth, renderTexture.graphicsFormat, renderTexture.dimension, rth.m_Name, mips: renderTexture.useMipMap, enableMSAA: rth.m_EnableMSAA, msaaSamples: m_ScaledRTCurrentMSAASamples, dynamicRes: renderTexture.useDynamicScale);

                // Create the render texture
                renderTexture.Create();
            }
        }

        /// <summary>
        /// Allocate a new fixed sized RTHandle.
        /// </summary>
        /// <param name="width">With of the RTHandle.</param>
        /// <param name="height">Heigh of the RTHandle.</param>
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
        /// <param name="useDynamicScale">Set to true to use hardware dynamic scaling.</param>
        /// <param name="memoryless">Use this property to set the render texture memoryless modes.</param>
        /// <param name="name">Name of the RTHandle.</param>
        /// <returns></returns>
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
            RenderTextureMemoryless memoryless = RenderTextureMemoryless.None,
            string name = ""
            )
        {
            bool enableMSAA = msaaSamples != MSAASamples.None;
            if (!enableMSAA && bindTextureMS == true)
            {
                Debug.LogWarning("RTHandle allocated without MSAA but with bindMS set to true, forcing bindMS to false.");
                bindTextureMS = false;
            }

            // We need to handle this in an explicit way since GraphicsFormat does not expose depth formats. TODO: Get rid of this branch once GraphicsFormat'll expose depth related formats
            RenderTexture rt;
            if (isShadowMap || depthBufferBits != DepthBits.None)
            {
                RenderTextureFormat format = isShadowMap ? RenderTextureFormat.Shadowmap : RenderTextureFormat.Depth;
                rt = new RenderTexture(width, height, (int)depthBufferBits, format, RenderTextureReadWrite.Linear)
                {
                    hideFlags = HideFlags.HideAndDontSave,
                    volumeDepth = slices,
                    filterMode = filterMode,
                    wrapMode = wrapMode,
                    dimension = dimension,
                    enableRandomWrite = enableRandomWrite,
                    useMipMap = useMipMap,
                    autoGenerateMips = autoGenerateMips,
                    anisoLevel = anisoLevel,
                    mipMapBias = mipMapBias,
                    antiAliasing = (int)msaaSamples,
                    bindTextureMS = bindTextureMS,
                    useDynamicScale = m_HardwareDynamicResRequested && useDynamicScale,
                    memorylessMode = memoryless,
                    name = CoreUtils.GetRenderTargetAutoName(width, height, slices, format, name, mips: useMipMap, enableMSAA: enableMSAA, msaaSamples: msaaSamples)
                };

            }
            else
            {

                rt = new RenderTexture(width, height, (int)depthBufferBits, colorFormat)
                {
                    hideFlags = HideFlags.HideAndDontSave,
                    volumeDepth = slices,
                    filterMode = filterMode,
                    wrapMode = wrapMode,
                    dimension = dimension,
                    enableRandomWrite = enableRandomWrite,
                    useMipMap = useMipMap,
                    autoGenerateMips = autoGenerateMips,
                    anisoLevel = anisoLevel,
                    mipMapBias = mipMapBias,
                    antiAliasing = (int)msaaSamples,
                    bindTextureMS = bindTextureMS,
                    useDynamicScale = m_HardwareDynamicResRequested && useDynamicScale,
                    memorylessMode = memoryless,
                    name = CoreUtils.GetRenderTargetAutoName(width, height, slices, colorFormat, dimension, name, mips: useMipMap, enableMSAA: enableMSAA, msaaSamples: msaaSamples, dynamicRes: useDynamicScale)
                };
            }

            rt.Create();

            var newRT = new RTHandle(this);
            newRT.SetRenderTexture(rt);
            newRT.useScaling = false;
            newRT.m_EnableRandomWrite = enableRandomWrite;
            newRT.m_EnableMSAA = enableMSAA;
            newRT.m_EnableHWDynamicScale = useDynamicScale;
            newRT.m_Name = name;

            newRT.referenceSize = new Vector2Int(width, height);

            return newRT;
        }

        // Next two methods are used to allocate RenderTexture that depend on the frame settings (resolution and msaa for now)
        // RenderTextures allocated this way are meant to be defined by a scale of camera resolution (full/half/quarter resolution for example).
        // The idea is that internally the system will scale up the size of all render texture so that it amortizes with time and not reallocate when a smaller size is required (which is what happens with TemporaryRTs).
        // Since MSAA cannot be changed on the fly for a given RenderTexture, a separate instance will be created if the user requires it. This instance will be the one used after the next call of SetReferenceSize if MSAA is required.

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
        /// <param name="enableMSAA">Enable MSAA for this RTHandle.</param>
        /// <param name="bindTextureMS">Set to true if the texture needs to be bound as a multisampled texture in the shader.</param>
        /// <param name="useDynamicScale">Set to true to use hardware dynamic scaling.</param>
        /// <param name="memoryless">Use this property to set the render texture memoryless modes.</param>
        /// <param name="name">Name of the RTHandle.</param>
        /// <returns></returns>
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
            bool enableMSAA = false,
            bool bindTextureMS = false,
            bool useDynamicScale = false,
            RenderTextureMemoryless memoryless = RenderTextureMemoryless.None,
            string name = ""
            )
        {
            // If an MSAA target is requested, make sure the support was on
            if (enableMSAA)
                Debug.Assert(m_ScaledRTSupportsMSAA);

            int width = Mathf.Max(Mathf.RoundToInt(scaleFactor.x * GetMaxWidth()), 1);
            int height = Mathf.Max(Mathf.RoundToInt(scaleFactor.y * GetMaxHeight()), 1);

            var rth = AllocAutoSizedRenderTexture(width,
                    height,
                    slices,
                    depthBufferBits,
                    colorFormat,
                    filterMode,
                    wrapMode,
                    dimension,
                    enableRandomWrite,
                    useMipMap,
                    autoGenerateMips,
                    isShadowMap,
                    anisoLevel,
                    mipMapBias,
                    enableMSAA,
                    bindTextureMS,
                    useDynamicScale,
                    memoryless,
                    name
                    );

            rth.referenceSize = new Vector2Int(width, height);

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
        /// <param name="enableMSAA">Enable MSAA for this RTHandle.</param>
        /// <param name="bindTextureMS">Set to true if the texture needs to be bound as a multisampled texture in the shader.</param>
        /// <param name="useDynamicScale">Set to true to use hardware dynamic scaling.</param>
        /// <param name="memoryless">Use this property to set the render texture memoryless modes.</param>
        /// <param name="name">Name of the RTHandle.</param>
        /// <returns></returns>
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
            bool enableMSAA = false,
            bool bindTextureMS = false,
            bool useDynamicScale = false,
            RenderTextureMemoryless memoryless = RenderTextureMemoryless.None,
            string name = ""
            )
        {
            var scaleFactor = scaleFunc(new Vector2Int(GetMaxWidth(), GetMaxHeight()));
            int width = Mathf.Max(scaleFactor.x, 1);
            int height = Mathf.Max(scaleFactor.y, 1);

            var rth = AllocAutoSizedRenderTexture(width,
                    height,
                    slices,
                    depthBufferBits,
                    colorFormat,
                    filterMode,
                    wrapMode,
                    dimension,
                    enableRandomWrite,
                    useMipMap,
                    autoGenerateMips,
                    isShadowMap,
                    anisoLevel,
                    mipMapBias,
                    enableMSAA,
                    bindTextureMS,
                    useDynamicScale,
                    memoryless,
                    name
                    );

            rth.referenceSize = new Vector2Int(width, height);

            rth.scaleFunc = scaleFunc;
            return rth;
        }

        // Internal function
        RTHandle AllocAutoSizedRenderTexture(
            int width,
            int height,
            int slices,
            DepthBits depthBufferBits,
            GraphicsFormat colorFormat,
            FilterMode filterMode,
            TextureWrapMode wrapMode,
            TextureDimension dimension,
            bool enableRandomWrite,
            bool useMipMap,
            bool autoGenerateMips,
            bool isShadowMap,
            int anisoLevel,
            float mipMapBias,
            bool enableMSAA,
            bool bindTextureMS,
            bool useDynamicScale,
            RenderTextureMemoryless memoryless,
            string name
            )
        {
            // Here user made a mistake in setting up msaa/bindMS, hence the warning
            if (!enableMSAA && bindTextureMS == true)
            {
                Debug.LogWarning("RTHandle allocated without MSAA but with bindMS set to true, forcing bindMS to false.");
                bindTextureMS = false;
            }

            bool allocForMSAA = m_ScaledRTSupportsMSAA ? enableMSAA : false;
            // Here we purposefully disable MSAA so we just force the bindMS param to false.
            if (!allocForMSAA)
            {
                bindTextureMS = false;
            }

            // MSAA Does not support random read/write.
            bool UAV = enableRandomWrite;
            if (allocForMSAA && (UAV == true))
            {
                Debug.LogWarning("RTHandle that is MSAA-enabled cannot allocate MSAA RT with 'enableRandomWrite = true'.");
                UAV = false;
            }

            int msaaSamples = allocForMSAA ? (int)m_ScaledRTCurrentMSAASamples : 1;

            // We need to handle this in an explicit way since GraphicsFormat does not expose depth formats. TODO: Get rid of this branch once GraphicsFormat'll expose depth related formats
            RenderTexture rt;
            if (isShadowMap || depthBufferBits != DepthBits.None)
            {
                RenderTextureFormat format = isShadowMap ? RenderTextureFormat.Shadowmap : RenderTextureFormat.Depth;
                GraphicsFormat stencilFormat = isShadowMap ? GraphicsFormat.None : GraphicsFormat.R8_UInt;
                rt = new RenderTexture(width, height, (int)depthBufferBits, format, RenderTextureReadWrite.Linear)
                {
                    hideFlags = HideFlags.HideAndDontSave,
                    volumeDepth = slices,
                    filterMode = filterMode,
                    wrapMode = wrapMode,
                    dimension = dimension,
                    enableRandomWrite = UAV,
                    useMipMap = useMipMap,
                    autoGenerateMips = autoGenerateMips,
                    anisoLevel = anisoLevel,
                    mipMapBias = mipMapBias,
                    antiAliasing = msaaSamples,
                    bindTextureMS = bindTextureMS,
                    useDynamicScale = m_HardwareDynamicResRequested && useDynamicScale,
                    memorylessMode = memoryless,
                    stencilFormat = stencilFormat,
                    name = CoreUtils.GetRenderTargetAutoName(width, height, slices, colorFormat, dimension, name, mips: useMipMap, enableMSAA: allocForMSAA, msaaSamples: m_ScaledRTCurrentMSAASamples, dynamicRes: useDynamicScale)
                };
            }
            else
            {
                rt = new RenderTexture(width, height, (int)depthBufferBits, colorFormat)
                {
                    hideFlags = HideFlags.HideAndDontSave,
                    volumeDepth = slices,
                    filterMode = filterMode,
                    wrapMode = wrapMode,
                    dimension = dimension,
                    enableRandomWrite = UAV,
                    useMipMap = useMipMap,
                    autoGenerateMips = autoGenerateMips,
                    anisoLevel = anisoLevel,
                    mipMapBias = mipMapBias,
                    antiAliasing = msaaSamples,
                    bindTextureMS = bindTextureMS,
                    useDynamicScale = m_HardwareDynamicResRequested && useDynamicScale,
                    memorylessMode = memoryless,
                    name = CoreUtils.GetRenderTargetAutoName(width, height, slices, colorFormat, dimension, name, mips: useMipMap, enableMSAA: allocForMSAA, msaaSamples: m_ScaledRTCurrentMSAASamples, dynamicRes: useDynamicScale)
                };
            }

            rt.Create();

            var rth = new RTHandle(this);
            rth.SetRenderTexture(rt);
            rth.m_EnableMSAA = enableMSAA;
            rth.m_EnableRandomWrite = enableRandomWrite;
            rth.useScaling = true;
            rth.m_EnableHWDynamicScale = useDynamicScale;
            rth.m_Name = name;
            m_AutoSizedRTs.Add(rth);
            return rth;
        }

        /// <summary>
        /// Allocate a RTHandle from a regular RenderTexture.
        /// </summary>
        /// <param name="texture">Input texture</param>
        /// <returns>A new RTHandle referencing the input texture.</returns>
        public RTHandle Alloc(RenderTexture texture)
        {
            var rth = new RTHandle(this);
            rth.SetRenderTexture(texture);
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
    }
}
