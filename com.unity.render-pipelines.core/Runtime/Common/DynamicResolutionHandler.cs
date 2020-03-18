using System;

namespace UnityEngine.Rendering
{
    // This must return a float in the range [0.0f...1.0f]. It is a lerp factor between min screen fraction and max screen fraction.
    public delegate float PerformDynamicRes();

    public enum DynamicResScalePolicyType
    {
        // If this is the chosen option, then the HDDynamicResolutionHandler expects the m_DynamicResMethod to return a screen percentage to.
        // The value set will be clamped between the min and max percentage set on the HDRP Asset.
        ReturnsPercentage,
        // If this is the chosen option, then the HDDynamicResolutionHandler expects the m_DynamicResMethod to return a lerp factor t such as
        // current_screen_percentage = lerp(min percentage, max percentage, t).
        ReturnsMinMaxLerpFactor
    }


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
        public Vector2Int cachedOriginalSize { get; private set; }
        public bool hasSwitchedResolution { get; private set; }

        public DynamicResUpscaleFilter filter { get; set; }


        private DynamicResolutionType type;

        private PerformDynamicRes m_DynamicResMethod = null;
        private static DynamicResolutionHandler s_Instance = new DynamicResolutionHandler();
        public static DynamicResolutionHandler instance { get { return s_Instance; } }


        private DynamicResolutionHandler()
        {
            m_DynamicResMethod = DefaultDynamicResMethod;
            filter = DynamicResUpscaleFilter.Bilinear;

        }

        // TODO: Eventually we will need to provide a good default implementation for this.
        static public float DefaultDynamicResMethod()
        {
            return 1.0f;
        }

        private void ProcessSettings(GlobalDynamicResolutionSettings settings)
        {
            m_Enabled = settings.enabled;
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

        static public void SetDynamicResScaler(PerformDynamicRes scaler, DynamicResScalePolicyType scalerType = DynamicResScalePolicyType.ReturnsMinMaxLerpFactor)
        {
            s_Instance.m_ScalerType = scalerType;
            s_Instance.m_DynamicResMethod = scaler;
        }

        public void SetCurrentCameraRequest(bool cameraRequest)
        {
            m_CurrentCameraRequest = cameraRequest;
        }

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
                hasSwitchedResolution = true;

                if (!m_ForceSoftwareFallback && type == DynamicResolutionType.Hardware)
                {
                    ScalableBufferManager.ResizeBuffers(m_CurrentFraction, m_CurrentFraction);
                }

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
                        OnResolutionChange();
                    }
                }
                hasSwitchedResolution = false;
            }

            m_PrevHWScaleWidth = ScalableBufferManager.widthScaleFactor;
            m_PrevHWScaleHeight = ScalableBufferManager.heightScaleFactor;
        }

        public bool SoftwareDynamicResIsEnabled()
        {
            return m_CurrentCameraRequest && m_Enabled && m_CurrentFraction != 1.0f && (m_ForceSoftwareFallback || type == DynamicResolutionType.Software);
        }
        public bool HardwareDynamicResIsEnabled()
        {
            return !m_ForceSoftwareFallback && m_CurrentCameraRequest && m_Enabled &&  type == DynamicResolutionType.Hardware;
        }

        public bool RequestsHardwareDynamicResolution()
        {
            if (m_ForceSoftwareFallback)
                return false;

            return type == DynamicResolutionType.Hardware;
        }

        public bool DynamicResolutionEnabled()
        {
            return m_CurrentCameraRequest && m_Enabled && m_CurrentFraction != 1.0f;
        }

        public void ForceSoftwareFallback()
        {
            m_ForceSoftwareFallback = true;
        }

        public Vector2Int GetRTHandleScale(Vector2Int size)
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

        public float GetCurrentScale()
        {
            return (m_Enabled && m_CurrentCameraRequest) ? m_CurrentFraction : 1.0f;
        }

        public Vector2Int GetLastScaledSize()
        {
            return m_LastScaledSize;
        }
    }
}
