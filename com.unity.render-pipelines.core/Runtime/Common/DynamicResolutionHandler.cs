using System;

namespace UnityEngine.Rendering
{
    /// <summary>
    /// The format of the delegate used to perofrm dynamic resolution. 
    /// </summary>
    public delegate float PerformDynamicRes();

    /// <summary>
    /// The type of dynamic resolution scaler. It essentially defines what the output of the scaler is expected to be. 
    /// </summary>
    public enum DynamicResScalePolicyType
    {
        /// <summary>
        /// If is the option, DynamicResolutionHandler expects the scaler to return a screen percentage.
        /// The value set will be clamped between the minimum and maximum percentage set in the GlobalDynamicResolutionSettings.
        /// </summary>
        ReturnsPercentage,
        /// <summary>
        /// If is the option, DynamicResolutionHandler expects the scaler to return a factor t in the [0..1] such that the final resolution percentage
        /// is determined by lerp(minimumPercentage, maximumPercentage, t), where the minimum and maximum percentages are the one set in the GlobalDynamicResolutionSettings.  
        /// </summary>
        ReturnsMinMaxLerpFactor
    }


    /// <summary>
    /// The class responsible to handle dynamic resolution. 
    /// </summary>
    public class DynamicResolutionHandler
    {
        private bool  m_Enabled = false;
        private float m_MinScreenFraction = 1.0f;
        private float m_MaxScreenFraction = 1.0f;
        private float m_CurrentFraction = 1.0f;
        private float m_PrevFraction = -1.0f;
        private bool  m_ForcingRes = false;
        private bool m_CurrentCameraRequest = true;
        private bool m_ForceSoftwareFallback = false;

        private float m_PrevHWScaleWidth = 1.0f;
        private float m_PrevHWScaleHeight = 1.0f;
        private Vector2Int m_LastScaledSize = new Vector2Int(0, 0);

        private DynamicResScalePolicyType m_ScalerType = DynamicResScalePolicyType.ReturnsMinMaxLerpFactor;

        // Debug
        private Vector2Int cachedOriginalSize;

        /// <summary>
        /// The filter that is used to upscale the rendering result to the native resolution. 
        /// </summary>
        public DynamicResUpscaleFilter filter { get; set; }

        /// <summary>
        /// The viewport of the final buffer. This is likely the resolution the dynamic resolution starts from before any scaling. Note this is NOT the target resolution the rendering will happen in
        /// but the resolution the scaled rendered result will be upscaled to. 
        /// </summary>
        public Vector2Int finalViewport { get; set; }


        private DynamicResolutionType type;

        private PerformDynamicRes m_DynamicResMethod = null;
        private static DynamicResolutionHandler s_Instance = new DynamicResolutionHandler();

        /// <summary>
        /// Get the instance of the global dynamic resolution handler. 
        /// </summary>
        public static DynamicResolutionHandler instance { get { return s_Instance; } }


        private DynamicResolutionHandler()
        {
            m_DynamicResMethod = DefaultDynamicResMethod;
            filter = DynamicResUpscaleFilter.Bilinear;

        }

        // TODO: Eventually we will need to provide a good default implementation for this.
        static private float DefaultDynamicResMethod()
        {
            return 1.0f;
        }

        private void ProcessSettings(GlobalDynamicResolutionSettings settings)
        {
            m_Enabled = settings.enabled && (Application.isPlaying || settings.forceResolution);
            if (!m_Enabled)
            {
                m_CurrentFraction = 1.0f;
            }
            else
            {
                type = settings.dynResType;
                float minScreenFrac = Mathf.Clamp(settings.minPercentage / 100.0f, 0.1f, 1.0f);
                m_MinScreenFraction = minScreenFrac;
                float maxScreenFrac = Mathf.Clamp(settings.maxPercentage / 100.0f, m_MinScreenFraction, 3.0f);
                m_MaxScreenFraction = maxScreenFrac;

                filter = settings.upsampleFilter;
                m_ForcingRes = settings.forceResolution;

                if (m_ForcingRes)
                {
                    float fraction = Mathf.Clamp(settings.forcedPercentage / 100.0f, 0.1f, 1.5f);
                    m_CurrentFraction = fraction;
                }
            }
        }

        /// <summary>
        /// Set the scaler method used to drive dynamic resolution.
        /// </summary>
        /// <param name="scaler">The delegate used to determine the resolution percentage used by the dynamic resolution system.</param>
        /// <param name="scalerType">The type of scaler that is used, this is used to indicate the return type of the scaler to the dynamic resolution system.</param>
        static public void SetDynamicResScaler(PerformDynamicRes scaler, DynamicResScalePolicyType scalerType = DynamicResScalePolicyType.ReturnsMinMaxLerpFactor)
        {
            s_Instance.m_ScalerType = scalerType;
            s_Instance.m_DynamicResMethod = scaler;
        }

        /// <summary>
        /// Set whether the camera that is currently processed by the pipeline has requested dynamic resolution or not.
        /// </summary>
        /// <param name="cameraRequest">Determines whether the camera has requested dynamic resolution or not.</param>
        public void SetCurrentCameraRequest(bool cameraRequest)
        {
            m_CurrentCameraRequest = cameraRequest;
        }

        /// <summary>
        /// Update the state of the dynamic resolution system.
        /// </summary>
        /// <param name="settings">The settings that are to be used by the dynamic resolution system.</param>
        /// <param name="OnResolutionChange">An action that will be called every time the dynamic resolution system triggers a change in resolution.</param>
        public void Update(GlobalDynamicResolutionSettings settings, Action OnResolutionChange = null)
        {
            ProcessSettings(settings);

            if (!m_Enabled) return;

            if (!m_ForcingRes)
            {
                if(m_ScalerType == DynamicResScalePolicyType.ReturnsMinMaxLerpFactor)
                {
                    float currLerp = m_DynamicResMethod();
                    float lerpFactor = Mathf.Clamp(currLerp, 0.0f, 1.0f);
                    m_CurrentFraction = Mathf.Lerp(m_MinScreenFraction, m_MaxScreenFraction, lerpFactor);
                }
                else if(m_ScalerType == DynamicResScalePolicyType.ReturnsPercentage)
                {
                    float percentageRequested = Mathf.Max(m_DynamicResMethod(), 5.0f);
                    m_CurrentFraction = Mathf.Clamp(percentageRequested / 100.0f, m_MinScreenFraction, m_MaxScreenFraction);
                }
            }

            if (m_CurrentFraction != m_PrevFraction)
            {
                m_PrevFraction = m_CurrentFraction;

                if (!m_ForceSoftwareFallback && type == DynamicResolutionType.Hardware)
                {
                    ScalableBufferManager.ResizeBuffers(m_CurrentFraction, m_CurrentFraction);
                }

                if(OnResolutionChange != null)
                    OnResolutionChange();
            }
            else
            {
                // Unity can change the scale factor by itself so we need to trigger the Action if that happens as well.
                if (!m_ForceSoftwareFallback && type == DynamicResolutionType.Hardware)
                {
                    if(ScalableBufferManager.widthScaleFactor != m_PrevHWScaleWidth  ||
                        ScalableBufferManager.heightScaleFactor != m_PrevHWScaleHeight)
                    {
                        if (OnResolutionChange != null)
                            OnResolutionChange();
                    }
                }
            }

            m_PrevHWScaleWidth = ScalableBufferManager.widthScaleFactor;
            m_PrevHWScaleHeight = ScalableBufferManager.heightScaleFactor;
        }

        /// <summary>
        /// Determines whether software dynamic resolution is enabled or not.
        /// </summary>
        /// <returns>True: Software dynamic resolution is enabled</returns>
        public bool SoftwareDynamicResIsEnabled()
        {
            return m_CurrentCameraRequest && m_Enabled && m_CurrentFraction != 1.0f && (m_ForceSoftwareFallback || type == DynamicResolutionType.Software);
        }

        /// <summary>
        /// Determines whether hardware dynamic resolution is enabled or not.
        /// </summary>
        /// <returns>True: Hardware dynamic resolution is enabled</returns>
        public bool HardwareDynamicResIsEnabled()
        {
            return !m_ForceSoftwareFallback && m_CurrentCameraRequest && m_Enabled &&  type == DynamicResolutionType.Hardware;
        }

        /// <summary>
        /// Identifies whether hardware dynamic resolution has been requested and is going to be used.
        /// </summary>
        /// <returns>True: Hardware dynamic resolution is requested by user and software fallback has not been forced</returns>
        public bool RequestsHardwareDynamicResolution()
        {
            if (m_ForceSoftwareFallback)
                return false;

            return type == DynamicResolutionType.Hardware;
        }

        /// <summary>
        /// Identifies whether dynamic resolution is enabled and scaling the render targets.
        /// </summary>
        /// <returns>True: Dynamic resolution is enabled.</returns>
        public bool DynamicResolutionEnabled()
        {
            return m_CurrentCameraRequest && m_Enabled && m_CurrentFraction != 1.0f;
        }

        /// <summary>
        /// Forces software fallback for dynamic resolution. Needs to be called in case Hardware dynamic resolution is requested by the user, but not supported by the platform.
        /// </summary>
        public void ForceSoftwareFallback()
        {
            m_ForceSoftwareFallback = true;
        }

        /// <summary>
        /// Applies to the passed size the scale imposed by the dynamic resolution system.
        /// </summary>
        /// <param name="size">The starting size of the render target that will be scaled by dynamic resolution.</param>
        /// <returns>The parameter size scaled by the dynamic resolution system.</returns>
        public Vector2Int GetScaledSize(Vector2Int size)
        {
            cachedOriginalSize = size;

            if (!m_Enabled || !m_CurrentCameraRequest)
            {
                return size;
            }

            float scaleFractionX = m_CurrentFraction;
            float scaleFractionY = m_CurrentFraction;
            if (!m_ForceSoftwareFallback && type == DynamicResolutionType.Hardware)
            {
                scaleFractionX = ScalableBufferManager.widthScaleFactor;
                scaleFractionY = ScalableBufferManager.heightScaleFactor;
            }

            Vector2Int scaledSize = new Vector2Int(Mathf.CeilToInt(size.x * scaleFractionX), Mathf.CeilToInt(size.y * scaleFractionY));
            if (m_ForceSoftwareFallback || type != DynamicResolutionType.Hardware)
            {
                scaledSize.x += (1 & scaledSize.x);
                scaledSize.y += (1 & scaledSize.y);
            }
            m_LastScaledSize = scaledSize;

            return scaledSize;
        }

        /// <summary>
        /// Returns the scale that is currently applied by the dynamic resolution system.
        /// </summary>
        /// <returns>The scale that is currently applied by the dynamic resolution system.</returns>
        public float GetCurrentScale()
        {
            return (m_Enabled && m_CurrentCameraRequest) ? m_CurrentFraction : 1.0f;
        }

        /// <summary>
        /// Returns the latest scaled size that has been produced by GetScaledSize.
        /// </summary>
        /// <returns>The latest scaled size that has been produced by GetScaledSize.</returns>
        public Vector2Int GetLastScaledSize()
        {
            return m_LastScaledSize;
        }
    }
}
