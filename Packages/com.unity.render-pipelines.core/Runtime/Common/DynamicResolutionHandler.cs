using System;
using System.Collections.Generic;

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
    /// The source slots for dynamic resolution scaler. Defines registers were the scalers assigned are stored. By default the User one is always used
    /// </summary>
    public enum DynamicResScalerSlot
    {
        /// <summary> Scaler slot set by the function SetDynamicResScaler</summary>
        User,
        /// <summary> Scaler slot set by the function SetSystemDynamicResScaler</summary>
        System,
        /// <summary> total number of scaler slots </summary>
        Count
    }

    /// <summary>
    /// The class responsible to handle dynamic resolution.
    /// </summary>
    public class DynamicResolutionHandler
    {
        private bool m_Enabled;
        private bool m_UseMipBias;
        private float m_MinScreenFraction;
        private float m_MaxScreenFraction;
        private float m_CurrentFraction;
        private bool m_ForcingRes;
        private bool m_CurrentCameraRequest;
        private float m_PrevFraction;
        private bool m_ForceSoftwareFallback;
        private bool m_RunUpscalerFilterOnFullResolution;

        private float m_PrevHWScaleWidth;
        private float m_PrevHWScaleHeight;
        private Vector2Int m_LastScaledSize;

        private void Reset()
        {
            m_Enabled = false;
            m_UseMipBias = false;
            m_MinScreenFraction = 1.0f;
            m_MaxScreenFraction = 1.0f;
            m_CurrentFraction = 1.0f;
            m_ForcingRes = false;
            m_CurrentCameraRequest = true;
            m_PrevFraction = -1.0f;
            m_ForceSoftwareFallback = false;
            m_RunUpscalerFilterOnFullResolution = false;

            m_PrevHWScaleWidth = 1.0f;
            m_PrevHWScaleHeight = 1.0f;
            m_LastScaledSize = new Vector2Int(0, 0);
            filter = DynamicResUpscaleFilter.CatmullRom;
        }

        private struct ScalerContainer
        {
            public DynamicResScalePolicyType type;
            public PerformDynamicRes method;
        }

        private static DynamicResScalerSlot s_ActiveScalerSlot = DynamicResScalerSlot.User;
        private static ScalerContainer[] s_ScalerContainers = new ScalerContainer[(int)DynamicResScalerSlot.Count]
        {
            new ScalerContainer() { type = DynamicResScalePolicyType.ReturnsMinMaxLerpFactor, method = DefaultDynamicResMethod },
            new ScalerContainer() { type = DynamicResScalePolicyType.ReturnsMinMaxLerpFactor, method = DefaultDynamicResMethod }
        };

        // Debug
        private Vector2Int cachedOriginalSize;

        /// <summary>
        /// The filter that is used to upscale the rendering result to the native resolution.
        /// </summary>
        public DynamicResUpscaleFilter filter { get; private set; }

        // Used to detect the filters set via user API
        static Dictionary<int, DynamicResUpscaleFilter> s_CameraUpscaleFilters = new Dictionary<int, DynamicResUpscaleFilter>();

        /// <summary>
        /// The viewport of the final buffer. This is likely the resolution the dynamic resolution starts from before any scaling. Note this is NOT the target resolution the rendering will happen in
        /// but the resolution the scaled rendered result will be upscaled to.
        /// </summary>
        public Vector2Int finalViewport { get; set; }

        /// <summary>
        /// By default, dynamic resolution scaling is turned off automatically when the source matches the final viewport (100% scale).
        /// That is, DynamicResolutionEnabled and SoftwareDynamicResIsEnabled will return false if the scale is 100%.
        /// For certain upscalers, we dont want this behavior since they could possibly include anti aliasing and other quality improving post processes.
        /// Setting this to true will eliminate this behavior.
        /// Note: when the EdgeAdaptiveScalingUpres (FSR 1.0) filter is set, this will cause this parameter to always be true.
        /// </summary>
        public bool runUpscalerFilterOnFullResolution
        {
            set { m_RunUpscalerFilterOnFullResolution = value; }
            get { return m_RunUpscalerFilterOnFullResolution || filter == DynamicResUpscaleFilter.EdgeAdaptiveScalingUpres; }
        }

        private DynamicResolutionType type;

        private GlobalDynamicResolutionSettings m_CachedSettings = GlobalDynamicResolutionSettings.NewDefault();

        private const int CameraDictionaryMaxcCapacity = 32;
        private WeakReference m_OwnerCameraWeakRef = null;
        private static Dictionary<int, DynamicResolutionHandler> s_CameraInstances = new Dictionary<int, DynamicResolutionHandler>(CameraDictionaryMaxcCapacity);
        private static DynamicResolutionHandler s_DefaultInstance = new DynamicResolutionHandler();

        private static int s_ActiveCameraId = 0;
        private static DynamicResolutionHandler s_ActiveInstance = s_DefaultInstance;

        //private global state of ScalableBufferManager
        private static bool s_ActiveInstanceDirty = true;
        private static float s_GlobalHwFraction = 1.0f;
        private static bool s_GlobalHwUpresActive = false;

        private bool FlushScalableBufferManagerState()
        {
            if (s_GlobalHwUpresActive == HardwareDynamicResIsEnabled() && s_GlobalHwFraction == m_CurrentFraction)
                return false;

            s_GlobalHwUpresActive = HardwareDynamicResIsEnabled();
            s_GlobalHwFraction = m_CurrentFraction;
            float currentFraction = s_GlobalHwUpresActive ? s_GlobalHwFraction : 1.0f;
            ScalableBufferManager.ResizeBuffers(currentFraction, currentFraction);
            return true;
        }

        private static DynamicResolutionHandler GetOrCreateDrsInstanceHandler(Camera camera)
        {
            if (camera == null)
                return null;

            DynamicResolutionHandler instance = null;
            var key = camera.GetInstanceID();
            if (!s_CameraInstances.TryGetValue(key, out instance))
            {
                //if this camera is not available in the map of cameras lets try creating one.

                //first and foremost, if we exceed the dictionary capacity, lets try and recycle an object that is dead.
                if (s_CameraInstances.Count >= CameraDictionaryMaxcCapacity)
                {
                    int recycledInstanceKey = 0;
                    DynamicResolutionHandler recycledInstance = null;
                    foreach (var kv in s_CameraInstances)
                    {
                        //is this object dead? that is, belongs to a camera that was destroyed?
                        if (kv.Value.m_OwnerCameraWeakRef == null || !kv.Value.m_OwnerCameraWeakRef.IsAlive)
                        {
                            recycledInstance = kv.Value;
                            recycledInstanceKey = kv.Key;
                            break;
                        }
                    }

                    if (recycledInstance != null)
                    {
                        instance = recycledInstance;
                        s_CameraInstances.Remove(recycledInstanceKey);
                        s_CameraUpscaleFilters.Remove(recycledInstanceKey);
                    }
                }

                //if we didnt find a dead object, we create one from scratch.
                if (instance == null)
                {
                    instance = new DynamicResolutionHandler();
                    instance.m_OwnerCameraWeakRef = new WeakReference(camera);
                }
                else
                {
                    //otherwise, we found a dead object, lets reset it, and have a weak ref to this camera,
                    //so we can possibly recycle it in the future by checking the camera's weak pointer state.
                    instance.Reset();
                    instance.m_OwnerCameraWeakRef.Target = camera;
                }

                s_CameraInstances.Add(key, instance);
            }
            return instance;
        }

        /// <summary>
        /// The scheduling mechanism to apply upscaling.
        /// </summary>
        public enum UpsamplerScheduleType
        {
            /// <summary>
            /// Indicates that upscaling must happen before post processing.
            /// This means that everything runs at the source resolution during rasterization, and post processes will
            /// run at full resolution. Ideal for temporal upscalers.
            /// </summary>
            BeforePost,

            /// <summary>
            /// Indicates that upscaling must happen after post processing.
            /// This means that everything in the frame runs at the source resolution, and upscaling happens after
            /// the final pass. This is ideal for spatial upscalers.
            /// </summary>
            AfterPost
        }

        private UpsamplerScheduleType m_UpsamplerSchedule = UpsamplerScheduleType.AfterPost;

        /// <summary>
        /// Property that sets / gets the state of the upscaling schedule.
        /// This must be set at the beginning of the frame, once per camera.
        /// </summary>
        public UpsamplerScheduleType upsamplerSchedule { set { m_UpsamplerSchedule = value; } get { return m_UpsamplerSchedule; } }


        /// <summary>
        /// Get the instance of the global dynamic resolution handler.
        /// </summary>
        public static DynamicResolutionHandler instance { get { return s_ActiveInstance; } }


        private DynamicResolutionHandler()
        {
            Reset();
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
                m_UseMipBias = settings.useMipBias;
                float minScreenFrac = Mathf.Clamp(settings.minPercentage / 100.0f, 0.1f, 1.0f);
                m_MinScreenFraction = minScreenFrac;
                float maxScreenFrac = Mathf.Clamp(settings.maxPercentage / 100.0f, m_MinScreenFraction, 3.0f);
                m_MaxScreenFraction = maxScreenFrac;

                // Check if a filter has been set via user API, if so we use that, otherwise we use the default from the GlobalDynamicResolutionSettings
                bool hasUserRequestedFilter = s_CameraUpscaleFilters.TryGetValue(s_ActiveCameraId, out DynamicResUpscaleFilter requestedFilter);

                filter = hasUserRequestedFilter ? requestedFilter : settings.upsampleFilter;
                m_ForcingRes = settings.forceResolution;

                if (m_ForcingRes)
                {
                    float fraction = Mathf.Clamp(settings.forcedPercentage / 100.0f, 0.1f, 1.5f);
                    m_CurrentFraction = fraction;
                }
            }
            m_CachedSettings = settings;
        }

        /// <summary>
        /// Gets the scale
        /// </summary>
        /// <returns>The resolved scale</returns>
        public Vector2 GetResolvedScale()
        {
            if (!m_Enabled || !m_CurrentCameraRequest)
            {
                return new Vector2(1.0f, 1.0f);
            }

            float scaleFractionX = m_CurrentFraction;
            float scaleFractionY = m_CurrentFraction;
            if (!m_ForceSoftwareFallback && type == DynamicResolutionType.Hardware)
            {
                scaleFractionX = ScalableBufferManager.widthScaleFactor;
                scaleFractionY = ScalableBufferManager.heightScaleFactor;
            }

            return new Vector2(scaleFractionX, scaleFractionY);
        }

        /// <summary>
        /// Returns the mip bias to apply in the rendering pipeline. This mip bias helps bring detail since sampling of textures occurs at the target rate.
        /// </summary>
        /// <param name="inputResolution">The input width x height resolution in pixels.</param>
        /// <param name="outputResolution">The output width x height resolution in pixels.</param>
        /// <param name="forceApply">False by default. If true, we ignore the useMipBias setting and return a mip bias regardless.</param>
        /// <returns>The calculated value</returns>
        public float CalculateMipBias(Vector2Int inputResolution, Vector2Int outputResolution, bool forceApply = false)
        {
            if (!m_UseMipBias && !forceApply)
                return 0.0f;

            return (float)Math.Log((double)inputResolution.x / (double)outputResolution.x, 2.0);
        }

        /// <summary>
        /// Set the scaler method used to drive dynamic resolution by the user.
        /// </summary>
        /// <param name="scaler">The delegate used to determine the resolution percentage used by the dynamic resolution system.</param>
        /// <param name="scalerType">The type of scaler that is used, this is used to indicate the return type of the scaler to the dynamic resolution system.</param>
        static public void SetDynamicResScaler(PerformDynamicRes scaler, DynamicResScalePolicyType scalerType = DynamicResScalePolicyType.ReturnsMinMaxLerpFactor)
        {
            s_ScalerContainers[(int)DynamicResScalerSlot.User] = new ScalerContainer() { type = scalerType, method = scaler };
        }

        /// <summary>
        /// Set the scaler method used to drive dynamic resolution internally from the Scriptable Rendering Pipeline. This function should only be called by Scriptable Rendering Pipeline.
        /// </summary>
        /// <param name="scaler">The delegate used to determine the resolution percentage used by the dynamic resolution system.</param>
        /// <param name="scalerType">The type of scaler that is used, this is used to indicate the return type of the scaler to the dynamic resolution system.</param>
        static public void SetSystemDynamicResScaler(PerformDynamicRes scaler, DynamicResScalePolicyType scalerType = DynamicResScalePolicyType.ReturnsMinMaxLerpFactor)
        {
            s_ScalerContainers[(int)DynamicResScalerSlot.System] = new ScalerContainer() { type = scalerType, method = scaler };
        }

        /// <summary>
        /// Sets the active dynamic scaler slot to be used by the runtime when calculating frame resolutions.
        /// See DynamicResScalerSlot for more information.
        /// </summary>
        /// <param name="slot">The scaler to be selected and used by the runtime.</param>
        static public void SetActiveDynamicScalerSlot(DynamicResScalerSlot slot)
        {
            s_ActiveScalerSlot = slot;
        }

        /// <summary>
        /// Will clear the currently used camera. Use this function to restore the default instance when UpdateAndUseCamera is called.
        /// </summary>
        public static void ClearSelectedCamera()
        {
            s_ActiveInstance = s_DefaultInstance;
            s_ActiveCameraId = 0;
            s_ActiveInstanceDirty = true;
        }

        /// <summary>
        /// Set the Upscale filter used by the camera when dynamic resolution is run.
        /// </summary>
        /// <param name="camera">The camera for which the upscale filter is set.</param>
        /// <param name="filter">The filter to be used by the camera to upscale to final resolution.</param>
        static public void SetUpscaleFilter(Camera camera, DynamicResUpscaleFilter filter)
        {
            var cameraID = camera.GetInstanceID();
            if (s_CameraUpscaleFilters.ContainsKey(cameraID))
            {
                s_CameraUpscaleFilters[cameraID] = filter;
            }
            else
            {
                s_CameraUpscaleFilters.Add(cameraID, filter);
            }
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
        /// Update the state of the dynamic resolution system for a specific camera.
        /// Call this function also to switch context between cameras (will set the current camera as active).
        /// Passing a null camera has the same effect as calling Update without the camera parameter.
        /// </summary>
        /// <param name="camera">Camera used to select a specific instance tied to this DynamicResolutionHandler instance.
        /// </param>
        /// <param name="settings">(optional) The settings that are to be used by the dynamic resolution system. passing null for the settings will result in the last update's settings used.</param>
        /// <param name="OnResolutionChange">An action that will be called every time the dynamic resolution system triggers a change in resolution.</param>
        public static void UpdateAndUseCamera(Camera camera, GlobalDynamicResolutionSettings? settings = null, Action OnResolutionChange = null)
        {
            int newCameraId;
            if (camera == null)
            {
                s_ActiveInstance = s_DefaultInstance;
                newCameraId = 0;
            }
            else
            {
                s_ActiveInstance = GetOrCreateDrsInstanceHandler(camera);
                newCameraId = camera.GetInstanceID();
            }

            s_ActiveInstanceDirty = newCameraId != s_ActiveCameraId;
            s_ActiveCameraId = newCameraId;
            s_ActiveInstance.Update(settings.HasValue ? settings.Value : s_ActiveInstance.m_CachedSettings, OnResolutionChange);
        }

        /// <summary>
        /// Update the state of the dynamic resolution system.
        /// </summary>
        /// <param name="settings">The settings that are to be used by the dynamic resolution system.</param>
        /// <param name="OnResolutionChange">An action that will be called every time the dynamic resolution system triggers a change in resolution.</param>
        public void Update(GlobalDynamicResolutionSettings settings, Action OnResolutionChange = null)
        {
            ProcessSettings(settings);

            if (!m_Enabled || !s_ActiveInstanceDirty)
            {
                FlushScalableBufferManagerState();
                s_ActiveInstanceDirty = false;
                return;
            }

            if (!m_ForcingRes)
            {
                ref ScalerContainer scaler = ref s_ScalerContainers[(int)s_ActiveScalerSlot];
                if (scaler.type == DynamicResScalePolicyType.ReturnsMinMaxLerpFactor)
                {
                    float currLerp = scaler.method();
                    float lerpFactor = Mathf.Clamp(currLerp, 0.0f, 1.0f);
                    m_CurrentFraction = Mathf.Lerp(m_MinScreenFraction, m_MaxScreenFraction, lerpFactor);
                }
                else if (scaler.type == DynamicResScalePolicyType.ReturnsPercentage)
                {
                    float percentageRequested = Mathf.Max(scaler.method(), 5.0f);
                    m_CurrentFraction = Mathf.Clamp(percentageRequested / 100.0f, m_MinScreenFraction, m_MaxScreenFraction);
                }
            }

            bool hardwareResolutionChanged = false;
            bool softwareResolutionChanged = m_CurrentFraction != m_PrevFraction;

            m_PrevFraction = m_CurrentFraction;

            if (!m_ForceSoftwareFallback && type == DynamicResolutionType.Hardware)
            {
                hardwareResolutionChanged = FlushScalableBufferManagerState();
                if (ScalableBufferManager.widthScaleFactor != m_PrevHWScaleWidth ||
                    ScalableBufferManager.heightScaleFactor != m_PrevHWScaleHeight)
                {
                    hardwareResolutionChanged = true;
                }
            }


            if ((softwareResolutionChanged || hardwareResolutionChanged) && OnResolutionChange != null)
                OnResolutionChange();

            s_ActiveInstanceDirty = false;
            m_PrevHWScaleWidth = ScalableBufferManager.widthScaleFactor;
            m_PrevHWScaleHeight = ScalableBufferManager.heightScaleFactor;
        }

        /// <summary>
        /// Determines whether software dynamic resolution is enabled or not.
        /// </summary>
        /// <returns>True: Software dynamic resolution is enabled</returns>
        public bool SoftwareDynamicResIsEnabled()
        {
            return m_CurrentCameraRequest && m_Enabled && (m_CurrentFraction != 1.0f || runUpscalerFilterOnFullResolution) && (m_ForceSoftwareFallback || type == DynamicResolutionType.Software);
        }

        /// <summary>
        /// Determines whether hardware dynamic resolution is enabled or not.
        /// </summary>
        /// <returns>True: Hardware dynamic resolution is enabled</returns>
        public bool HardwareDynamicResIsEnabled()
        {
            return !m_ForceSoftwareFallback && m_CurrentCameraRequest && m_Enabled && type == DynamicResolutionType.Hardware;
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
            //we assume that the DRS schedule takes care of anti aliasing. Thus we dont care if the fraction requested is 1.0
            return m_CurrentCameraRequest && m_Enabled && (m_CurrentFraction != 1.0f || runUpscalerFilterOnFullResolution);
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
        /// Note: this function has the side effect of caching the last scale size, and the output is always smaller or equal then the input.
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

            Vector2Int scaledSize = ApplyScalesOnSize(size);

            m_LastScaledSize = scaledSize;
            return scaledSize;
        }

        /// <summary>
        /// Applies to the passed size the scale imposed by the dynamic resolution system.
        /// This function uses the internal resolved scale from the dynamic resolution system.
        /// Note: this function is pure (has no side effects), this function does not cache the pre-scale size
        /// </summary>
        /// <param name="size">The size to apply the scaling</param>
        /// <returns>The parameter size scaled by the dynamic resolution system.</returns>
        public Vector2Int ApplyScalesOnSize(Vector2Int size)
        {
            return ApplyScalesOnSize(size, GetResolvedScale());
        }

        internal Vector2Int ApplyScalesOnSize(Vector2Int size, Vector2 scales)
        {
            Vector2Int scaledSize = new Vector2Int(Mathf.CeilToInt(size.x * scales.x), Mathf.CeilToInt(size.y * scales.y));
            if (m_ForceSoftwareFallback || type != DynamicResolutionType.Hardware)
            {
                scaledSize.x += (1 & scaledSize.x);
                scaledSize.y += (1 & scaledSize.y);
            }

            scaledSize.x = Math.Min(scaledSize.x, size.x);
            scaledSize.y = Math.Min(scaledSize.y, size.y);

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

        /// <summary>
        /// Returns the resolved low res multiplier based on the low res transparency threshold settings.
        /// Note: The pipeline can use this to drive the scale for low res transparency if available.
        /// </summary>
        /// <param name="targetLowRes"> the target low resolution.
        ///     If by any chance thresholding is disabled or clamped, the exact same resolution is returned.
        ///     This allows the caller to directly compare the float result safely with the floating point target resolution.
        /// </param>
        /// <returns>Returns the resolved low res multiplier based on the low transparency threshold settings.</returns>
        public float GetLowResMultiplier(float targetLowRes)
        {
            if (!m_Enabled)
                return targetLowRes;

            float thresholdPercentage = Math.Min(m_CachedSettings.lowResTransparencyMinimumThreshold / 100.0f, targetLowRes);
            float targetPercentage = targetLowRes * m_CurrentFraction;
            if (targetPercentage >= thresholdPercentage)
                return targetLowRes;

            return Mathf.Clamp(thresholdPercentage / m_CurrentFraction, 0.0f, 1.0f);
        }
    }
}
