using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine.Rendering.HighDefinition.Attributes;
using UnityEngine.Experimental.Rendering.RenderGraphModule;
using UnityEngine.Experimental.Rendering;
using NameAndTooltip = UnityEngine.Rendering.DebugUI.Widget.NameAndTooltip;

namespace UnityEngine.Rendering.HighDefinition
{
    [GenerateHLSL(needAccessors = false, generateCBuffer = true)]
    unsafe struct ShaderVariablesDebugDisplay
    {
        [HLSLArray(32, typeof(Vector4))]
        public fixed float _DebugRenderingLayersColors[32 * 4];
        [HLSLArray(11, typeof(ShaderGenUInt4))]
        public fixed uint _DebugViewMaterialArray[11 * 4]; // Contain the id (define in various materialXXX.cs.hlsl) of the property to display
        [HLSLArray(7, typeof(Vector4))] // Must match ProbeBrickIndex.kMaxSubdivisionLevels
        public fixed float _DebugAPVSubdivColors[7 * 4];

        public int _DebugLightingMode; // Match enum DebugLightingMode
        public int _DebugLightLayersMask;
        public int _DebugShadowMapMode;
        public int _DebugMipMapMode; // Match enum DebugMipMapMode

        public int _DebugFullScreenMode;
        public float _DebugTransparencyOverdrawWeight;
        public int _DebugMipMapModeTerrainTexture; // Match enum DebugMipMapModeTerrainTexture
        public int _ColorPickerMode; // Match enum ColorPickerDebugMode

        public Vector4 _DebugViewportSize; //Frame viewport size used during rendering.
        public Vector4 _DebugLightingAlbedo; // x == bool override, yzw = albedo for diffuse
        public Vector4 _DebugLightingSmoothness; // x == bool override, y == override value
        public Vector4 _DebugLightingNormal; // x == bool override
        public Vector4 _DebugLightingAmbientOcclusion; // x == bool override, y == override value
        public Vector4 _DebugLightingSpecularColor; // x == bool override, yzw = specular color
        public Vector4 _DebugLightingEmissiveColor; // x == bool override, yzw = emissive color
        public Vector4 _DebugLightingMaterialValidateHighColor; // user can specific the colors for the validator error conditions
        public Vector4 _DebugLightingMaterialValidateLowColor;
        public Vector4 _DebugLightingMaterialValidatePureMetalColor;
        public Vector4 _MousePixelCoord;  // xy unorm, zw norm
        public Vector4 _MouseClickPixelCoord;  // xy unorm, zw norm

        public int _MatcapMixAlbedo;
        public float _MatcapViewScale;
        public int _DebugSingleShadowIndex;
        public int _DebugIsLitShaderModeDeferred;

        public int _DebugAOVOutput;
        public float _ShaderVariablesDebugDisplayPad0;
        public float _ShaderVariablesDebugDisplayPad1;
        public float _ShaderVariablesDebugDisplayPad2;
    }

    /// <summary>
    /// Full Screen Debug Mode.
    /// </summary>
    [GenerateHLSL]
    public enum FullScreenDebugMode
    {
        /// <summary>No Full Screen debug mode.</summary>
        None,

        // Lighting
        /// <summary>Minimum Full Screen Lighting debug mode value (used internally).</summary>
        MinLightingFullScreenDebug,
        /// <summary>Display Screen Space Ambient Occlusion buffer.</summary>
        ScreenSpaceAmbientOcclusion,
        /// <summary>Display Screen Space Reflections buffer used for lighting.</summary>
        ScreenSpaceReflections,
        /// <summary>Display the Transparent Screen Space Reflections buffer.</summary>
        TransparentScreenSpaceReflections,
        /// <summary>Display Screen Space Reflections buffer of the previous frame accumulated.</summary>
        ScreenSpaceReflectionsPrev,
        /// <summary>Display Screen Space Reflections buffer of the current frame hit.</summary>
        ScreenSpaceReflectionsAccum,
        /// <summary>Display Screen Space Reflections rejection used for PBR Accumulation algorithm.</summary>
        ScreenSpaceReflectionSpeedRejection,
        /// <summary>Display Contact Shadows buffer.</summary>
        ContactShadows,
        /// <summary>Display Contact Shadows fade.</summary>
        ContactShadowsFade,
        /// <summary>Display Screen Space Shadows.</summary>
        ScreenSpaceShadows,
        /// <summary>Displays the color pyramid before the refraction pass.</summary>
        PreRefractionColorPyramid,
        /// <summary>Display the Depth Pyramid.</summary>
        DepthPyramid,
        /// <summary>Display the final color pyramid for the frame.</summary>
        FinalColorPyramid,

        // Raytracing Only
        /// <summary>Display ray tracing light cluster.</summary>
        LightCluster,
        /// <summary>Display screen space global illumination.</summary>
        ScreenSpaceGlobalIllumination,
        /// <summary>Display recursive ray tracing.</summary>
        RecursiveRayTracing,
        /// <summary>Display ray-traced sub-surface scattering.</summary>
        RayTracedSubSurface,

        // Volumetric Clouds
        /// <summary>Display the volumetric clouds in-scattering x transmittance.</summary>
        VolumetricClouds,
        /// <summary>Display the volumetric clouds shadow at ground level.</summary>
        VolumetricCloudsShadow,

        /// <summary>Display the ray tracing acceleration structure</summary>
        RayTracingAccelerationStructure,

        /// <summary>Maximum Full Screen Lighting debug mode value (used internally).</summary>
        MaxLightingFullScreenDebug,

        // Rendering
        /// <summary>Minimum Full Screen Rendering debug mode value (used internally).</summary>
        MinRenderingFullScreenDebug,
        /// <summary>Display Motion Vectors.</summary>
        MotionVectors,
        /// <summary>Display the world space positions.</summary>
        WorldSpacePosition,
        /// <summary>Display NaNs.</summary>
        NanTracker,
        /// <summary>Display Log of the color buffer.</summary>
        ColorLog,
        /// <summary>Display Depth of Field circle of confusion.</summary>
        DepthOfFieldCoc,
        /// <summary>Display Transparency Overdraw.</summary>
        TransparencyOverdraw,
        /// <summary>Display Quad Overdraw.</summary>
        QuadOverdraw,
        /// <summary>Display Local Volumetric Fog Overdraw.</summary>
        LocalVolumetricFogOverdraw,
        /// <summary>Display Vertex Density.</summary>
        VertexDensity,
        /// <summary>Display Requested Virtual Texturing tiles, colored by the mip</summary>
        RequestedVirtualTextureTiles,
        /// <summary>Black background to visualize the Lens Flare</summary>
        LensFlareDataDriven,
        /// <summary>Maximum Full Screen Rendering debug mode value (used internally).</summary>
        MaxRenderingFullScreenDebug,

        //Material
        /// <summary>Minimum Full Screen Material debug mode value (used internally).</summary>
        MinMaterialFullScreenDebug,
        /// <summary>Display Diffuse Color validation mode.</summary>
        ValidateDiffuseColor,
        /// <summary>Display specular Color validation mode.</summary>
        ValidateSpecularColor,
        /// <summary>Maximum Full Screen Material debug mode value (used internally).</summary>
        MaxMaterialFullScreenDebug,
    }

    /// <summary>
    /// List of RTAS Full Screen Debug views.
    /// </summary>
    public enum RTASDebugView
    {
        /// <summary>
        /// Debug view of the RTAS for shadows.
        /// </summary>
        Shadows,
        /// <summary>
        /// Debug view of the RTAS for ambient occlusion.
        /// </summary>
        AmbientOcclusion,
        /// <summary>
        /// Debug view of the RTAS for global illumination.
        /// </summary>
        GlobalIllumination,
        /// <summary>
        /// Debug view of the RTAS for reflections.
        /// </summary>
        Reflections,
        /// <summary>
        /// Debug view of the RTAS for recursive ray tracing.
        /// </summary>
        RecursiveRayTracing,
        /// <summary>
        /// Debug view of the RTAS for path tracing.
        /// </summary>
        PathTracing
    }

    /// <summary>
    /// List of RTAS Full Screen Debug modes.
    /// </summary>
    public enum RTASDebugMode
    {
        /// <summary>
        /// Displacing the instanceID as the RTAS Debug view.
        /// </summary>
        InstanceID,
        /// <summary>
        /// Displacing the primitiveID as the RTAS Debug view.
        /// </summary>
        PrimitiveID,
    }

    /// <summary>
    /// Class managing debug display in HDRP.
    /// </summary>
    public partial class DebugDisplaySettings : IDebugData
    {
        static string k_PanelDisplayStats = "Display Stats";
        static string k_PanelMaterials = "Material";
        static string k_PanelLighting = "Lighting";
        static string k_PanelRendering = "Rendering";
        static string k_PanelDecals = "Decals";

        DebugUI.Widget[] m_DebugDisplayStatsItems;
        DebugUI.Widget[] m_DebugMaterialItems;
        DebugUI.Widget[] m_DebugLightingItems;
        DebugUI.Widget[] m_DebugRenderingItems;
        DebugUI.Widget[] m_DebugDecalsItems;

        static GUIContent[] s_LightingFullScreenDebugStrings = null;
        static int[] s_LightingFullScreenDebugValues = null;
        static GUIContent[] s_RenderingFullScreenDebugStrings = null;
        static int[] s_RenderingFullScreenDebugValues = null;
        static GUIContent[] s_MaterialFullScreenDebugStrings = null;
        static int[] s_MaterialFullScreenDebugValues = null;

        static List<GUIContent> s_CameraNames = new List<GUIContent>();
        static GUIContent[] s_CameraNamesStrings = { new ("No Visible Camera") };
        static int[] s_CameraNamesValues = { 0 };

        static bool needsRefreshingCameraFreezeList = true;

        List<HDProfileId> m_RecordedSamplers = new List<HDProfileId>();

        // Accumulate values to avg over one second.
        class AccumulatedTiming
        {
            public float accumulatedValue = 0;
            public float lastAverage = 0;

            internal void UpdateLastAverage(int frameCount)
            {
                lastAverage = accumulatedValue / frameCount;
                accumulatedValue = 0.0f;
            }
        }
        Dictionary<int, AccumulatedTiming> m_AccumulatedGPUTiming = new Dictionary<int, AccumulatedTiming>();
        Dictionary<int, AccumulatedTiming> m_AccumulatedCPUTiming = new Dictionary<int, AccumulatedTiming>();
        Dictionary<int, AccumulatedTiming> m_AccumulatedInlineCPUTiming = new Dictionary<int, AccumulatedTiming>();
        float m_TimeSinceLastAvgValue = 0.0f;
        int m_AccumulatedFrames = 0;
        const float k_AccumulationTimeInSeconds = 1.0f;

        List<HDProfileId> m_RecordedSamplersRT = new List<HDProfileId>();
        enum DebugProfilingType
        {
            CPU,
            GPU,
            InlineCPU
        }

        internal DebugFrameTiming debugFrameTiming = new DebugFrameTiming();

#if ENABLE_NVIDIA && ENABLE_NVIDIA_MODULE
        internal UnityEngine.NVIDIA.DebugView nvidiaDebugView { get; } = new UnityEngine.NVIDIA.DebugView();
#endif

        /// <summary>
        /// Debug data.
        /// </summary>
        public partial class DebugData
        {
            /// <summary>Ratio of the screen size in which overlays are rendered.</summary>
            public float debugOverlayRatio = 0.33f;
            /// <summary>Current full screen debug mode.</summary>
            public FullScreenDebugMode fullScreenDebugMode = FullScreenDebugMode.None;
            /// <summary>Enable range remapping.</summary>
            public bool enableDebugDepthRemap = false; // False per default to be compliant with AOV depth output (AOV depth must export unmodified linear depth)
            /// <summary>Depth Range remapping values for some of the fullscreen mode. Only x and y are used.</summary>
            public Vector4 fullScreenDebugDepthRemap = new Vector4(0.0f, 1.0f, 0.0f, 0.0f);
            /// <summary>Current full screen debug mode mip level (when applicable).</summary>
            public float fullscreenDebugMip = 0.0f;
            /// <summary>Index of the light used for contact shadows display.</summary>
            public int fullScreenContactShadowLightIndex = 0;
            /// <summary>XR single pass test mode.</summary>
            [Obsolete]
            public bool xrSinglePassTestMode = false;
            /// <summary>Whether to display the average timings every second.</summary>
            public bool averageProfilerTimingsOverASecond = false;

            /// <summary>Current material debug settings.</summary>
            public MaterialDebugSettings materialDebugSettings = new MaterialDebugSettings();
            /// <summary>Current lighting debug settings.</summary>
            public LightingDebugSettings lightingDebugSettings = new LightingDebugSettings();
            /// <summary>Current mip map debug settings.</summary>
            public MipMapDebugSettings mipMapDebugSettings = new MipMapDebugSettings();
            /// <summary>Current color picker debug settings.</summary>
            public ColorPickerDebugSettings colorPickerDebugSettings = new ColorPickerDebugSettings();
            /// <summary>Current monitors debug settings.</summary>
            public MonitorsDebugSettings monitorsDebugSettings = new MonitorsDebugSettings();
            /// <summary>Current false color debug settings.</summary>
            public FalseColorDebugSettings falseColorDebugSettings = new FalseColorDebugSettings();
            /// <summary>Current decals debug settings.</summary>
            public DecalsDebugSettings decalsDebugSettings = new DecalsDebugSettings();
            /// <summary>Current transparency debug settings.</summary>
            public TransparencyDebugSettings transparencyDebugSettings = new TransparencyDebugSettings();
            /// <summary>Index of screen space shadow to display.</summary>
            public uint screenSpaceShadowIndex = 0;
            /// <summary>Max quad cost for quad overdraw display.</summary>
            public uint maxQuadCost = 5;
            /// <summary>Max vertex density for vertex density display.</summary>
            public uint maxVertexDensity = 10;
            /// <summary>Display ray tracing ray count per frame.</summary>
            public bool countRays = false;
            /// <summary>Display Show Lens Flare Data Driven Only.</summary>
            public bool showLensFlareDataDrivenOnly = false;
            /// <summary>Index of the camera to freeze for visibility.</summary>
            public int debugCameraToFreeze = 0;
            internal RTASDebugView rtasDebugView = RTASDebugView.Shadows;
            internal RTASDebugMode rtasDebugMode = RTASDebugMode.InstanceID;

            /// <summary>Minimum length a motion vector needs to be to be displayed in the debug display. Unit is pixels.</summary>
            public float minMotionVectorLength = 0.0f;


            // TODO: The only reason this exist is because of Material/Engine debug enums
            // They have repeating values, which caused issues when iterating through the enum, thus the need for explicit indices
            // Once we refactor material/engine debug to avoid repeating values, we should be able to remove that.
            //saved enum fields for when repainting
            internal int lightingDebugModeEnumIndex;
            internal int lightingFulscreenDebugModeEnumIndex;
            internal int materialValidatorDebugModeEnumIndex;
            internal int tileClusterDebugEnumIndex;
            internal int mipMapsEnumIndex;
            internal int engineEnumIndex;
            internal int attributesEnumIndex;
            internal int propertiesEnumIndex;
            internal int gBufferEnumIndex;
            internal int shadowDebugModeEnumIndex;
            internal int tileClusterDebugByCategoryEnumIndex;
            internal int clusterDebugModeEnumIndex;
            internal int lightVolumeDebugTypeEnumIndex;
            internal int renderingFulscreenDebugModeEnumIndex;
            internal int terrainTextureEnumIndex;
            internal int colorPickerDebugModeEnumIndex;
            internal int exposureDebugModeEnumIndex;
            internal int hdrDebugModeEnumIndex;
            internal int msaaSampleDebugModeEnumIndex;
            internal int debugCameraToFreezeEnumIndex;
            internal int rtasDebugViewEnumIndex;
            internal int rtasDebugModeEnumIndex;

            private float m_DebugGlobalMipBiasOverride = 0.0f;

            /// <summary>
            /// Returns the current mip bias override specified in the debug panel.
            /// </summary>
            /// <returns>Mip bias override</returns>
            public float GetDebugGlobalMipBiasOverride()
            {
                return m_DebugGlobalMipBiasOverride;
            }

            /// <summary>
            /// Sets the mip bias override to be imposed in the rendering pipeline.
            /// </summary>
            /// <param name="value">mip bias override value.</param>
            public void SetDebugGlobalMipBiasOverride(float value)
            {
                m_DebugGlobalMipBiasOverride = value;
            }

            private bool m_UseDebugGlobalMipBiasOverride = false;

            internal bool UseDebugGlobalMipBiasOverride()
            {
                return m_UseDebugGlobalMipBiasOverride;
            }

            internal void SetUseDebugGlobalMipBiasOverride(bool value)
            {
                m_UseDebugGlobalMipBiasOverride = value;
            }

            // When settings mutually exclusives enum values, we need to reset the other ones.
            internal void ResetExclusiveEnumIndices()
            {
                materialDebugSettings.materialEnumIndex = 0;
                lightingDebugModeEnumIndex = 0;
                mipMapsEnumIndex = 0;
                engineEnumIndex = 0;
                attributesEnumIndex = 0;
                propertiesEnumIndex = 0;
                gBufferEnumIndex = 0;
                lightingFulscreenDebugModeEnumIndex = 0;
                renderingFulscreenDebugModeEnumIndex = 0;
            }
        }
        DebugData m_Data;

        /// <summary>
        /// Debug data.
        /// </summary>
        public DebugData data { get => m_Data; }

        // Had to keep those public because HDRP tests using it (as a workaround to access proper enum values for this debug)
        /// <summary>List of Full Screen Rendering Debug mode names.</summary>
        public static GUIContent[] renderingFullScreenDebugStrings => s_RenderingFullScreenDebugStrings;
        /// <summary>List of Full Screen Rendering Debug mode values.</summary>
        public static int[] renderingFullScreenDebugValues => s_RenderingFullScreenDebugValues;

        /// <summary>List of Full Screen Lighting Debug mode names.</summary>
        public static GUIContent[] lightingFullScreenDebugStrings => s_LightingFullScreenDebugStrings;
        /// <summary>List of Full Screen Lighting Debug mode values.</summary>
        public static int[] lightingFullScreenDebugValues => s_LightingFullScreenDebugValues;

        internal DebugDisplaySettings()
        {
            FillFullScreenDebugEnum(ref s_LightingFullScreenDebugStrings, ref s_LightingFullScreenDebugValues, FullScreenDebugMode.MinLightingFullScreenDebug, FullScreenDebugMode.MaxLightingFullScreenDebug);
            FillFullScreenDebugEnum(ref s_RenderingFullScreenDebugStrings, ref s_RenderingFullScreenDebugValues, FullScreenDebugMode.MinRenderingFullScreenDebug, FullScreenDebugMode.MaxRenderingFullScreenDebug);
            FillFullScreenDebugEnum(ref s_MaterialFullScreenDebugStrings, ref s_MaterialFullScreenDebugValues, FullScreenDebugMode.MinMaterialFullScreenDebug, FullScreenDebugMode.MaxMaterialFullScreenDebug);

            var device = SystemInfo.graphicsDeviceType;
            if (device == GraphicsDeviceType.Metal || device == GraphicsDeviceType.PlayStation4 || device == GraphicsDeviceType.PlayStation5 || device == GraphicsDeviceType.PlayStation5NGGC)
            {
                s_RenderingFullScreenDebugStrings = s_RenderingFullScreenDebugStrings.Where((val, idx) => (idx + FullScreenDebugMode.MinRenderingFullScreenDebug) != FullScreenDebugMode.VertexDensity).ToArray();
                s_RenderingFullScreenDebugValues = s_RenderingFullScreenDebugValues.Where((val, idx) => (idx + FullScreenDebugMode.MinRenderingFullScreenDebug) != FullScreenDebugMode.VertexDensity).ToArray();
                s_RenderingFullScreenDebugStrings = s_RenderingFullScreenDebugStrings.Where((val, idx) => (idx + FullScreenDebugMode.MinRenderingFullScreenDebug) != FullScreenDebugMode.QuadOverdraw).ToArray();
                s_RenderingFullScreenDebugValues = s_RenderingFullScreenDebugValues.Where((val, idx) => (idx + FullScreenDebugMode.MinRenderingFullScreenDebug) != FullScreenDebugMode.QuadOverdraw).ToArray();
            }

            s_MaterialFullScreenDebugStrings[(int)FullScreenDebugMode.ValidateDiffuseColor - ((int)FullScreenDebugMode.MinMaterialFullScreenDebug)] = new GUIContent("Diffuse Color");
            s_MaterialFullScreenDebugStrings[(int)FullScreenDebugMode.ValidateSpecularColor - ((int)FullScreenDebugMode.MinMaterialFullScreenDebug)] = new GUIContent("Metal or SpecularColor");

            m_Data = new DebugData();
        }

        /// <summary>
        /// Get Reset action.
        /// </summary>
        /// <returns></returns>
        Action IDebugData.GetReset() => () => m_Data = new DebugData();

        internal float[] GetDebugMaterialIndexes()
        {
            return data.materialDebugSettings.GetDebugMaterialIndexes();
        }

        /// <summary>
        /// Returns the current Light filtering mode.
        /// </summary>
        /// <returns>Current Light filtering mode.</returns>
        public DebugLightFilterMode GetDebugLightFilterMode()
        {
            return data.lightingDebugSettings.debugLightFilterMode;
        }

        /// <summary>
        /// Returns the current Lighting Debug Mode.
        /// </summary>
        /// <returns>Current Lighting Debug Mode.</returns>
        public DebugLightingMode GetDebugLightingMode()
        {
            return data.lightingDebugSettings.debugLightingMode;
        }

        /// <summary>
        /// Returns the current Light Layers Debug Mask.
        /// </summary>
        /// <returns>Current Light Layers Debug Mask.</returns>
        public DebugLightLayersMask GetDebugLightLayersMask()
        {
            var settings = data.lightingDebugSettings;
            if (!settings.debugLightLayers)
                return 0;

#if UNITY_EDITOR
            if (settings.debugSelectionLightLayers)
            {
                if (UnityEditor.Selection.activeGameObject == null)
                    return 0;
                var light = UnityEditor.Selection.activeGameObject.GetComponent<HDAdditionalLightData>();
                if (light == null)
                    return 0;

                if (settings.debugSelectionShadowLayers)
                    return (DebugLightLayersMask)light.GetShadowLayers();
                return (DebugLightLayersMask)light.GetLightLayers();
            }
#endif

            return settings.debugLightLayersFilterMask;
        }

        /// <summary>
        /// Returns the current Shadow Map Debug Mode.
        /// </summary>
        /// <returns>Current Shadow Map Debug Mode.</returns>
        public ShadowMapDebugMode GetDebugShadowMapMode()
        {
            return data.lightingDebugSettings.shadowDebugMode;
        }

        /// <summary>
        /// Returns the current Mip Map Debug Mode.
        /// </summary>
        /// <returns>Current Mip Map Debug Mode.</returns>
        public DebugMipMapMode GetDebugMipMapMode()
        {
            return data.mipMapDebugSettings.debugMipMapMode;
        }

        /// <summary>
        /// Returns the current Terrain Texture Mip Map Debug Mode.
        /// </summary>
        /// <returns>Current Terrain Texture Mip Map Debug Mode.</returns>
        public DebugMipMapModeTerrainTexture GetDebugMipMapModeTerrainTexture()
        {
            return data.mipMapDebugSettings.terrainTexture;
        }

        /// <summary>
        /// Returns the current Color Picker Mode.
        /// </summary>
        /// <returns>Current Color Picker Mode.</returns>
        public ColorPickerDebugMode GetDebugColorPickerMode()
        {
            return data.colorPickerDebugSettings.colorPickerMode;
        }

        /// <summary>
        /// Returns true if camera visibility is frozen.
        /// </summary>
        /// <returns>True if camera visibility is frozen</returns>
        public bool IsCameraFreezeEnabled()
        {
            return data.debugCameraToFreeze != 0;
        }

        /// <summary>
        /// Returns true if a specific camera is frozen for visibility.
        /// </summary>
        /// <param name="camera">Camera to be tested.</param>
        /// <returns>True if a specific camera is frozen for visibility.</returns>
        public bool IsCameraFrozen(Camera camera)
        {
            return IsCameraFreezeEnabled() && camera.name.Equals(s_CameraNamesStrings[data.debugCameraToFreeze].text);
        }

        /// <summary>
        /// Returns true if any debug display is enabled.
        /// </summary>
        /// <returns>True if any debug display is enabled.</returns>
        public bool IsDebugDisplayEnabled()
        {
            return data.materialDebugSettings.IsDebugDisplayEnabled() || data.lightingDebugSettings.IsDebugDisplayEnabled() || data.mipMapDebugSettings.IsDebugDisplayEnabled() || IsDebugFullScreenEnabled();
        }

        /// <summary>
        /// Returns true if any material debug display is enabled.
        /// </summary>
        /// <returns>True if any material debug display is enabled.</returns>
        public bool IsDebugMaterialDisplayEnabled()
        {
            return data.materialDebugSettings.IsDebugDisplayEnabled();
        }

        /// <summary>
        /// Returns true if any full screen debug display is enabled.
        /// </summary>
        /// <returns>True if any full screen debug display is enabled.</returns>
        public bool IsDebugFullScreenEnabled()
        {
            return data.fullScreenDebugMode != FullScreenDebugMode.None;
        }

        /// <summary>
        /// Returns true if a full screen debug display supporting the FullScreenDebug pass is enabled.
        /// </summary>
        /// <returns>True if a full screen debug display supporting the FullScreenDebug pass is enabled.</returns>
        internal bool IsFullScreenDebugPassEnabled()
        {
            return data.fullScreenDebugMode == FullScreenDebugMode.QuadOverdraw ||
                data.fullScreenDebugMode == FullScreenDebugMode.VertexDensity;
        }

        /// <summary>
        /// Returns true if any full screen exposure debug display is enabled.
        /// </summary>
        /// <returns>True if any full screen exposure debug display is enabled.</returns>
        public bool IsDebugExposureModeEnabled()
        {
            return data.lightingDebugSettings.exposureDebugMode != ExposureDebugMode.None;
        }

        /// <summary>
        /// Returns true if any full screen HDR debug display is enabled.
        /// </summary>
        /// <returns>True if any full screen exposure debug display is enabled.</returns>
        public bool IsHDRDebugModeEnabled()
        {
            return data.lightingDebugSettings.hdrDebugMode != HDRDebugMode.None;
        }

        /// <summary>
        /// Returns true if material validation is enabled.
        /// </summary>
        /// <returns>True if any material validation is enabled.</returns>
        public bool IsMaterialValidationEnabled()
        {
            return (data.fullScreenDebugMode == FullScreenDebugMode.ValidateDiffuseColor) || (data.fullScreenDebugMode == FullScreenDebugMode.ValidateSpecularColor);
        }

        /// <summary>
        /// Returns true if mip map debug display is enabled.
        /// </summary>
        /// <returns>True if any mip mapdebug display is enabled.</returns>
        public bool IsDebugMipMapDisplayEnabled()
        {
            return data.mipMapDebugSettings.IsDebugDisplayEnabled();
        }

        /// <summary>
        /// Returns true if matcap view is enabled for a particular camera.
        /// </summary>
        /// <param name="camera">Input camera.</param>
        /// <returns>True if matcap view is enabled for a particular camera.</returns>
        public bool IsMatcapViewEnabled(HDCamera camera)
        {
            bool sceneViewLightingDisabled = CoreUtils.IsSceneLightingDisabled(camera.camera);
            return sceneViewLightingDisabled || GetDebugLightingMode() == DebugLightingMode.MatcapView;
        }

        private void DisableNonMaterialDebugSettings()
        {
            data.fullScreenDebugMode = FullScreenDebugMode.None;
            data.lightingDebugSettings.debugLightingMode = DebugLightingMode.None;
            data.mipMapDebugSettings.debugMipMapMode = DebugMipMapMode.None;
            data.lightingDebugSettings.debugLightLayers = false;
        }

        /// <summary>
        /// Set the current shared material properties debug view.
        /// </summary>
        /// <param name="value">Desired shared material property to display.</param>
        public void SetDebugViewCommonMaterialProperty(MaterialSharedProperty value)
        {
            if (value != MaterialSharedProperty.None)
                DisableNonMaterialDebugSettings();
            data.materialDebugSettings.SetDebugViewCommonMaterialProperty(value);
        }

        /// <summary>
        /// Set the current material debug view.
        /// </summary>
        /// <param name="value">Desired material debug view.</param>
        public void SetDebugViewMaterial(int value)
        {
            if (value != 0)
                DisableNonMaterialDebugSettings();
            data.materialDebugSettings.SetDebugViewMaterial(value);
        }

        /// <summary>
        /// Set the current engine debug view.
        /// </summary>
        /// <param name="value">Desired engine debug view.</param>
        public void SetDebugViewEngine(int value)
        {
            if (value != 0)
                DisableNonMaterialDebugSettings();
            data.materialDebugSettings.SetDebugViewEngine(value);
        }

        /// <summary>
        /// Set current varying debug view.
        /// </summary>
        /// <param name="value">Desired varying debug view.</param>
        public void SetDebugViewVarying(DebugViewVarying value)
        {
            if (value != 0)
                DisableNonMaterialDebugSettings();
            data.materialDebugSettings.SetDebugViewVarying(value);
        }

        /// <summary>
        /// Set the current Material Property debug view.
        /// </summary>
        /// <param name="value">Desired property debug view.</param>
        public void SetDebugViewProperties(DebugViewProperties value)
        {
            if (value != 0)
                DisableNonMaterialDebugSettings();
            data.materialDebugSettings.SetDebugViewProperties(value);
        }

        /// <summary>
        /// Set the current GBuffer debug view.
        /// </summary>
        /// <param name="value">Desired GBuffer debug view.</param>
        public void SetDebugViewGBuffer(int value)
        {
            if (value != 0)
                DisableNonMaterialDebugSettings();
            data.materialDebugSettings.SetDebugViewGBuffer(value);
        }

        /// <summary>
        /// Set the current Full Screen Debug Mode.
        /// </summary>
        /// <param name="value">Desired Full Screen Debug mode.</param>
        public void SetFullScreenDebugMode(FullScreenDebugMode value)
        {
            if (data.lightingDebugSettings.shadowDebugMode == ShadowMapDebugMode.SingleShadow)
                value = 0;

            if (value != FullScreenDebugMode.None)
            {
                data.lightingDebugSettings.debugLightingMode = DebugLightingMode.None;
                data.lightingDebugSettings.debugLightLayers = false;
                data.materialDebugSettings.DisableMaterialDebug();
                data.mipMapDebugSettings.debugMipMapMode = DebugMipMapMode.None;
            }

            data.fullScreenDebugMode = value;
        }

        /// <summary>
        /// Set the current RTAS Full Screen Debug view.
        /// </summary>
        /// <param name="value">Desired Full Screen RTAS Debug view.</param>
        public void SetRTASDebugView(RTASDebugView value)
        {
            data.rtasDebugView = value;
        }

        /// <summary>
        /// Set the current RTAS Full Screen Debug mode.
        /// </summary>
        /// <param name="value">Desired Full Screen RTAS Debug mode.</param>
        public void SetRTASDebugMode(RTASDebugMode value)
        {
            data.rtasDebugMode = value;
        }

        /// <summary>
        /// Set the current Shadow Map Debug Mode.
        /// </summary>
        /// <param name="value">Desired Shadow Map debug mode.</param>
        public void SetShadowDebugMode(ShadowMapDebugMode value)
        {
            // When SingleShadow is enabled, we don't render full screen debug modes
            if (value == ShadowMapDebugMode.SingleShadow)
                data.fullScreenDebugMode = 0;
            data.lightingDebugSettings.shadowDebugMode = value;
        }

        /// <summary>
        /// Set the current Light Filtering.
        /// </summary>
        /// <param name="value">Desired Light Filtering.</param>
        public void SetDebugLightFilterMode(DebugLightFilterMode value)
        {
            if (value != 0)
            {
                data.materialDebugSettings.DisableMaterialDebug();
                data.mipMapDebugSettings.debugMipMapMode = DebugMipMapMode.None;
                data.lightingDebugSettings.debugLightLayers = false;
            }
            data.lightingDebugSettings.debugLightFilterMode = value;
        }

        /// <summary>
        /// Set the current Light layers Debug Mode
        /// </summary>
        /// <param name="value">Desired Light Layers Debug Mode.</param>
        public void SetDebugLightLayersMode(bool value)
        {
            if (value)
            {
                data.ResetExclusiveEnumIndices();
                data.lightingDebugSettings.debugLightFilterMode = DebugLightFilterMode.None;

                var builtins = typeof(Builtin.BuiltinData);
                var attr = builtins.GetCustomAttributes(true)[0] as GenerateHLSL;
                var renderingLayers = Array.IndexOf(builtins.GetFields(), builtins.GetField("renderingLayers"));

                SetDebugViewMaterial(attr.paramDefinesStart + renderingLayers);
            }
            else
            {
                SetDebugViewMaterial(0);
            }
            data.lightingDebugSettings.debugLightLayers = value;
        }

        /// <summary>
        /// Set the current Lighting Debug Mode.
        /// </summary>
        /// <param name="value">Desired Lighting Debug Mode.</param>
        public void SetDebugLightingMode(DebugLightingMode value)
        {
            if (value != 0)
            {
                data.fullScreenDebugMode = FullScreenDebugMode.None;
                data.materialDebugSettings.DisableMaterialDebug();
                data.mipMapDebugSettings.debugMipMapMode = DebugMipMapMode.None;
                data.lightingDebugSettings.debugLightLayers = false;
            }
            data.lightingDebugSettings.debugLightingMode = value;
        }

        /// <summary>
        /// Set the current Exposure Debug Mode.
        /// </summary>
        /// <param name="value">Desired Exposure Debug Mode.</param>
        internal void SetExposureDebugMode(ExposureDebugMode value)
        {
            data.lightingDebugSettings.exposureDebugMode = value;
        }

        /// <summary>
        /// Set the current HDR Debug Mode.
        /// </summary>
        /// <param name="value">Desired HDR output Debug Mode.</param>
        internal void SetHDRDebugMode(HDRDebugMode value)
        {
            data.lightingDebugSettings.hdrDebugMode = value;
        }

        /// <summary>
        /// Set the current Mip Map Debug Mode.
        /// </summary>
        /// <param name="value">Desired Mip Map debug mode.</param>
        public void SetMipMapMode(DebugMipMapMode value)
        {
            if (value != 0)
            {
                data.materialDebugSettings.DisableMaterialDebug();
                data.lightingDebugSettings.debugLightingMode = DebugLightingMode.None;
                data.lightingDebugSettings.debugLightLayers = false;
                data.fullScreenDebugMode = FullScreenDebugMode.None;
            }
            data.mipMapDebugSettings.debugMipMapMode = value;
        }

        void EnableProfilingRecorders()
        {
            Debug.Assert(m_RecordedSamplers.Count == 0);

            m_RecordedSamplers.Add(HDProfileId.HDRenderPipelineAllRenderRequest);
            m_RecordedSamplers.Add(HDProfileId.VolumeUpdate);
            m_RecordedSamplers.Add(HDProfileId.RenderShadowMaps);
            m_RecordedSamplers.Add(HDProfileId.GBuffer);
            m_RecordedSamplers.Add(HDProfileId.PrepareLightsForGPU);
            m_RecordedSamplers.Add(HDProfileId.VolumeVoxelization);
            m_RecordedSamplers.Add(HDProfileId.VolumetricLighting);
            m_RecordedSamplers.Add(HDProfileId.VolumetricClouds);
            m_RecordedSamplers.Add(HDProfileId.VolumetricCloudsTrace);
            m_RecordedSamplers.Add(HDProfileId.VolumetricCloudsReproject);
            m_RecordedSamplers.Add(HDProfileId.VolumetricCloudsUpscaleAndCombine);
            m_RecordedSamplers.Add(HDProfileId.RenderDeferredLightingCompute);
            m_RecordedSamplers.Add(HDProfileId.ForwardOpaque);
            m_RecordedSamplers.Add(HDProfileId.ForwardTransparent);
            m_RecordedSamplers.Add(HDProfileId.ForwardPreRefraction);
            m_RecordedSamplers.Add(HDProfileId.ColorPyramid);
            m_RecordedSamplers.Add(HDProfileId.DepthPyramid);
            m_RecordedSamplers.Add(HDProfileId.PostProcessing);
        }

        void DisableProfilingRecorders(List<HDProfileId> samplers)
        {
            foreach (var sampler in samplers)
            {
                ProfilingSampler.Get(sampler).enableRecording = false;
            }

            samplers.Clear();
        }

        void EnableProfilingRecordersRT()
        {
            Debug.Assert(m_RecordedSamplersRT.Count == 0);

            m_RecordedSamplersRT.Add(HDProfileId.RaytracingBuildCluster);
            m_RecordedSamplersRT.Add(HDProfileId.RaytracingCullLights);
            m_RecordedSamplersRT.Add(HDProfileId.RaytracingBuildAccelerationStructure);

            // Ray Traced Reflections
            m_RecordedSamplersRT.Add(HDProfileId.RaytracingReflectionDirectionGeneration);
            m_RecordedSamplersRT.Add(HDProfileId.RaytracingReflectionEvaluation);
            m_RecordedSamplersRT.Add(HDProfileId.RaytracingReflectionAdjustWeight);
            m_RecordedSamplersRT.Add(HDProfileId.RaytracingReflectionUpscale);
            m_RecordedSamplersRT.Add(HDProfileId.RaytracingReflectionFilter);

            // Ray Traced Ambient Occlusion
            m_RecordedSamplersRT.Add(HDProfileId.RaytracingAmbientOcclusion);
            m_RecordedSamplersRT.Add(HDProfileId.RaytracingFilterAmbientOcclusion);

            // Ray Traced Shadows
            m_RecordedSamplersRT.Add(HDProfileId.RaytracingDirectionalLightShadow);
            m_RecordedSamplersRT.Add(HDProfileId.RaytracingLightShadow);

            // Ray Traced Indirect Diffuse
            m_RecordedSamplersRT.Add(HDProfileId.RaytracingIndirectDiffuseDirectionGeneration);
            m_RecordedSamplersRT.Add(HDProfileId.RaytracingIndirectDiffuseEvaluation);
            m_RecordedSamplersRT.Add(HDProfileId.RaytracingIndirectDiffuseUpscale);
            m_RecordedSamplersRT.Add(HDProfileId.RaytracingFilterIndirectDiffuse);

            m_RecordedSamplersRT.Add(HDProfileId.RaytracingDebugOverlay);
            m_RecordedSamplersRT.Add(HDProfileId.ForwardPreRefraction);
            m_RecordedSamplersRT.Add(HDProfileId.RayTracingRecursiveRendering);
            m_RecordedSamplersRT.Add(HDProfileId.RayTracingDepthPrepass);
            m_RecordedSamplersRT.Add(HDProfileId.RayTracingFlagMask);
            m_RecordedSamplersRT.Add(HDProfileId.RaytracingDeferredLighting);
        }

        float GetSamplerTiming(HDProfileId samplerId, ProfilingSampler sampler, DebugProfilingType type)
        {
            if (data.averageProfilerTimingsOverASecond)
            {
                // Find the right accumulated dictionary
                var accumulatedDictionary = type == DebugProfilingType.CPU ? m_AccumulatedCPUTiming :
                    type == DebugProfilingType.InlineCPU ? m_AccumulatedInlineCPUTiming :
                    m_AccumulatedGPUTiming;

                AccumulatedTiming accTiming = null;
                if (accumulatedDictionary.TryGetValue((int)samplerId, out accTiming))
                    return accTiming.lastAverage;
            }
            else
            {
                return (type == DebugProfilingType.CPU) ? sampler.cpuElapsedTime : ((type == DebugProfilingType.GPU) ? sampler.gpuElapsedTime : sampler.inlineCpuElapsedTime);
            }

            return 0.0f;
        }

        ObservableList<DebugUI.Widget> BuildProfilingSamplerWidgetList(List<HDProfileId> samplerList)
        {
            var result = new ObservableList<DebugUI.Widget>();

            DebugUI.Value CreateWidgetForSampler(HDProfileId samplerId, ProfilingSampler sampler, DebugProfilingType type)
            {
                // Find the right accumulated dictionary and add it there if not existing yet.
                var accumulatedDictionary = type == DebugProfilingType.CPU ? m_AccumulatedCPUTiming :
                    type == DebugProfilingType.InlineCPU ? m_AccumulatedInlineCPUTiming :
                    m_AccumulatedGPUTiming;

                if (!accumulatedDictionary.ContainsKey((int)samplerId))
                {
                    accumulatedDictionary.Add((int)samplerId, new AccumulatedTiming());
                }
                return new()
                {
                    formatString = "{0:F2}ms",
                    refreshRate = 1.0f / 5.0f,
                    getter = () => GetSamplerTiming(samplerId, sampler, type),
                };
            }

            foreach (var samplerId in samplerList)
            {
                var sampler = ProfilingSampler.Get(samplerId);

                sampler.enableRecording = true;

                result.Add(new DebugUI.ValueTuple
                {
                    displayName = sampler.name,
                    values = new[]
                    {
                        CreateWidgetForSampler(samplerId, sampler, DebugProfilingType.CPU),
                        CreateWidgetForSampler(samplerId, sampler, DebugProfilingType.InlineCPU),
                        CreateWidgetForSampler(samplerId, sampler, DebugProfilingType.GPU),
                    }
                });
            }

            return result;
        }

        void UpdateListOfAveragedProfilerTimings(List<HDProfileId> samplers, bool needUpdatingAverages)
        {
            foreach (var samplerId in samplers)
            {
                var sampler = ProfilingSampler.Get(samplerId);

                // Accumulate.
                AccumulatedTiming accCPUTiming = null;
                if (m_AccumulatedCPUTiming.TryGetValue((int)samplerId, out accCPUTiming))
                    accCPUTiming.accumulatedValue += sampler.cpuElapsedTime;

                AccumulatedTiming accInlineCPUTiming = null;
                if (m_AccumulatedInlineCPUTiming.TryGetValue((int)samplerId, out accInlineCPUTiming))
                    accInlineCPUTiming.accumulatedValue += sampler.inlineCpuElapsedTime;

                AccumulatedTiming accGPUTiming = null;
                if (m_AccumulatedGPUTiming.TryGetValue((int)samplerId, out accGPUTiming))
                    accGPUTiming.accumulatedValue += sampler.gpuElapsedTime;

                if (needUpdatingAverages)
                {
                    if (accCPUTiming != null)
                        accCPUTiming.UpdateLastAverage(m_AccumulatedFrames);
                    if (accInlineCPUTiming != null)
                        accInlineCPUTiming.UpdateLastAverage(m_AccumulatedFrames);
                    if (accGPUTiming != null)
                        accGPUTiming.UpdateLastAverage(m_AccumulatedFrames);
                }
            }
        }

        internal void UpdateAveragedProfilerTimings()
        {
            m_TimeSinceLastAvgValue += Time.unscaledDeltaTime;
            m_AccumulatedFrames++;
            bool needUpdatingAverages = m_TimeSinceLastAvgValue >= k_AccumulationTimeInSeconds;

            UpdateListOfAveragedProfilerTimings(m_RecordedSamplers, needUpdatingAverages);
            UpdateListOfAveragedProfilerTimings(m_RecordedSamplersRT, needUpdatingAverages);

            if (needUpdatingAverages)
            {
                m_TimeSinceLastAvgValue = 0.0f;
                m_AccumulatedFrames = 0;
            }
        }

        void RegisterDisplayStatsDebug()
        {
            var list = new List<DebugUI.Widget>();

            debugFrameTiming.RegisterDebugUI(list);

            EnableProfilingRecorders();
            list.Add(new DebugUI.BoolField { displayName = "Update every second with average", getter = () => data.averageProfilerTimingsOverASecond, setter = value => data.averageProfilerTimingsOverASecond = value });
            list.Add(new DebugUI.Foldout("Detailed Stats", BuildProfilingSamplerWidgetList(m_RecordedSamplers), new[] { "CPU", "CPUInline", "GPU" }));

            if (HDRenderPipeline.currentAsset?.currentPlatformRenderPipelineSettings.supportRayTracing ?? true)
            {
                EnableProfilingRecordersRT();
                list.Add(new DebugUI.Foldout("Ray Tracing Stats", BuildProfilingSamplerWidgetList(m_RecordedSamplersRT), new[] { "CPU", "CPUInline", "GPU" }));
            }
            list.Add(new DebugUI.BoolField { displayName = "Count Rays (MRays/Frame)", getter = () => data.countRays, setter = value => data.countRays = value });
            {
                list.Add(new DebugUI.Container
                {
                    isHiddenCallback = () => !data.countRays,
                    children =
                    {
                        new DebugUI.Value { displayName = "Ambient Occlusion", getter = () => ((float)(RenderPipelineManager.currentPipeline as HDRenderPipeline).GetRaysPerFrame(RayCountValues.AmbientOcclusion)) / 1e6f, refreshRate = 1f / 30f },
                        new DebugUI.Value { displayName = "Shadows Directional", getter = () => ((float)(RenderPipelineManager.currentPipeline as HDRenderPipeline).GetRaysPerFrame(RayCountValues.ShadowDirectional)) / 1e6f, refreshRate = 1f / 30f },
                        new DebugUI.Value { displayName = "Shadows Area", getter = () => ((float)(RenderPipelineManager.currentPipeline as HDRenderPipeline).GetRaysPerFrame(RayCountValues.ShadowAreaLight)) / 1e6f, refreshRate = 1f / 30f },
                        new DebugUI.Value { displayName = "Shadows Point/Spot", getter = () => ((float)(RenderPipelineManager.currentPipeline as HDRenderPipeline).GetRaysPerFrame(RayCountValues.ShadowPointSpot)) / 1e6f, refreshRate = 1f / 30f },
                        new DebugUI.Value { displayName = "Reflections Forward ", getter = () => ((float)(RenderPipelineManager.currentPipeline as HDRenderPipeline).GetRaysPerFrame(RayCountValues.ReflectionForward)) / 1e6f, refreshRate = 1f / 30f },
                        new DebugUI.Value { displayName = "Reflections Deferred", getter = () => ((float)(RenderPipelineManager.currentPipeline as HDRenderPipeline).GetRaysPerFrame(RayCountValues.ReflectionDeferred)) / 1e6f, refreshRate = 1f / 30f },
                        new DebugUI.Value { displayName = "Diffuse GI Forward", getter = () => ((float)(RenderPipelineManager.currentPipeline as HDRenderPipeline).GetRaysPerFrame(RayCountValues.DiffuseGI_Forward)) / 1e6f, refreshRate = 1f / 30f },
                        new DebugUI.Value { displayName = "Diffuse GI Deferred", getter = () => ((float)(RenderPipelineManager.currentPipeline as HDRenderPipeline).GetRaysPerFrame(RayCountValues.DiffuseGI_Deferred)) / 1e6f, refreshRate = 1f / 30f },
                        new DebugUI.Value { displayName = "Recursive Rendering", getter = () => ((float)(RenderPipelineManager.currentPipeline as HDRenderPipeline).GetRaysPerFrame(RayCountValues.Recursive)) / 1e6f, refreshRate = 1f / 30f },
                        new DebugUI.Value { displayName = "Total", getter = () => ((float)(RenderPipelineManager.currentPipeline as HDRenderPipeline).GetRaysPerFrame(RayCountValues.Total)) / 1e6f, refreshRate = 1f / 30f },
                    }
                });
            }

            m_DebugDisplayStatsItems = list.ToArray();
            var panel = DebugManager.instance.GetPanel(k_PanelDisplayStats, true, groupIndex: int.MinValue);
            panel.flags = DebugUI.Flags.RuntimeOnly;
            panel.children.Add(m_DebugDisplayStatsItems);
        }

        DebugUI.Widget CreateMissingDebugShadersWarning()
        {
            return new DebugUI.MessageBox
            {
                displayName = "Warning: the debug shader variants are missing. Ensure that the \"Runtime Debug Shaders\" option is enabled in HDRP Global Settings.",
                style = DebugUI.MessageBox.Style.Warning,
                isHiddenCallback = () =>
                {
#if UNITY_EDITOR
                    return true;
#else
                    if (HDRenderPipelineGlobalSettings.instance != null)
                        return HDRenderPipelineGlobalSettings.instance.supportRuntimeDebugDisplay;
                    return true;
#endif
                }
            };
        }

        void UnregisterDisplayStatsDebug()
        {
            DisableProfilingRecorders(m_RecordedSamplers);
            if (HDRenderPipeline.currentAsset?.currentPlatformRenderPipelineSettings.supportRayTracing ?? true)
                DisableProfilingRecorders(m_RecordedSamplersRT);

            UnregisterDebugItems(k_PanelDisplayStats, m_DebugDisplayStatsItems);
        }

        static class MaterialStrings
        {
            public static readonly NameAndTooltip CommonMaterialProperties = new() { name = "Common Material Properties", tooltip = "Use the drop-down to select and debug a Material property to visualize on every GameObject on screen." };
            public static readonly NameAndTooltip Material = new() { name = "Material", tooltip = "Use the drop-down to select a Material property to visualize on every GameObject on screen using a specific Shader." };
            public static readonly NameAndTooltip Engine = new() { name = "Engine", tooltip = "Use the drop-down to select a Material property to visualize on every GameObject on screen that uses a specific Shader." };
            public static readonly NameAndTooltip Attributes = new() { name = "Attributes", tooltip = "Use the drop-down to select a 3D GameObject attribute, like Texture Coordinates or Vertex Color, to visualize on screen." };
            public static readonly NameAndTooltip Properties = new() { name = "Properties", tooltip = "Use the drop-down to select a property that the debugger uses to highlight GameObjects on screen. The debugger highlights GameObjects that use a Material with the property that you select." };
            public static readonly NameAndTooltip GBuffer = new() { name = "GBuffer", tooltip = "Use the drop-down to select a property from the GBuffer to visualize for deferred Materials." };
            public static readonly NameAndTooltip MaterialValidator = new() { name = "Material Validator", tooltip = "Use the drop-down to select which properties show validation colors." };
            public static readonly NameAndTooltip ValidatorTooHighColor = new() { name = "Too High Color", tooltip = "Select the color that the debugger displays when a Material's diffuse color is above the acceptable PBR range." };
            public static readonly NameAndTooltip ValidatorTooLowColor = new() { name = "Too Low Color", tooltip = "Select the color that the debugger displays when a Material's diffuse color is below the acceptable PBR range." };
            public static readonly NameAndTooltip ValidatorNotAPureMetalColor = new() { name = "Not A Pure Metal Color", tooltip = "Select the color that the debugger displays if a pixel defined as metallic has a non-zero albedo value." };
            public static readonly NameAndTooltip ValidatorPureMetals = new() { name = "Pure Metals", tooltip = "Enable to make the debugger highlight any pixels which Unity defines as metallic, but which have a non-zero albedo value." };
            public static readonly NameAndTooltip OverrideGlobalMaterialTextureMipBias = new() { name = "Override Global Material Texture Mip Bias", tooltip = "Enable to override the mipmap level bias of texture samplers in material shaders." };
            public static readonly NameAndTooltip DebugGlobalMaterialTextureMipBiasValue = new() { name = "Debug Global Material Texture Mip Bias Value", tooltip = "Use the slider to control the amount of mip bias of texture samplers in material shaders." };
        }

        void RegisterMaterialDebug()
        {
            var list = new List<DebugUI.Widget>();
            list.Add(CreateMissingDebugShadersWarning());
            list.Add(new DebugUI.EnumField { nameAndTooltip = MaterialStrings.CommonMaterialProperties, getter = () => (int)data.materialDebugSettings.debugViewMaterialCommonValue, setter = value => SetDebugViewCommonMaterialProperty((MaterialSharedProperty)value), autoEnum = typeof(MaterialSharedProperty), getIndex = () => (int)data.materialDebugSettings.debugViewMaterialCommonValue, setIndex = value => { data.ResetExclusiveEnumIndices(); data.materialDebugSettings.debugViewMaterialCommonValue = (MaterialSharedProperty)value; } });
            list.Add(new DebugUI.EnumField { nameAndTooltip = MaterialStrings.Material, getter = () => (data.materialDebugSettings.debugViewMaterial[0]) == 0 ? 0 : data.materialDebugSettings.debugViewMaterial[1], setter = value => SetDebugViewMaterial(value), enumNames = MaterialDebugSettings.debugViewMaterialStrings, enumValues = MaterialDebugSettings.debugViewMaterialValues, getIndex = () => data.materialDebugSettings.materialEnumIndex, setIndex = value => { data.ResetExclusiveEnumIndices(); data.materialDebugSettings.materialEnumIndex = value; } });
            list.Add(new DebugUI.EnumField { nameAndTooltip = MaterialStrings.Engine, getter = () => data.materialDebugSettings.debugViewEngine, setter = value => SetDebugViewEngine(value), enumNames = MaterialDebugSettings.debugViewEngineStrings, enumValues = MaterialDebugSettings.debugViewEngineValues, getIndex = () => data.engineEnumIndex, setIndex = value => { data.ResetExclusiveEnumIndices(); data.engineEnumIndex = value; } });
            list.Add(new DebugUI.EnumField { nameAndTooltip = MaterialStrings.Attributes, getter = () => (int)data.materialDebugSettings.debugViewVarying, setter = value => SetDebugViewVarying((DebugViewVarying)value), autoEnum = typeof(DebugViewVarying), getIndex = () => data.attributesEnumIndex, setIndex = value => { data.ResetExclusiveEnumIndices(); data.attributesEnumIndex = value; } });
            list.Add(new DebugUI.EnumField { nameAndTooltip = MaterialStrings.Properties, getter = () => (int)data.materialDebugSettings.debugViewProperties, setter = value => SetDebugViewProperties((DebugViewProperties)value), autoEnum = typeof(DebugViewProperties), getIndex = () => data.propertiesEnumIndex, setIndex = value => { data.ResetExclusiveEnumIndices(); data.propertiesEnumIndex = value; } });
            list.Add(new DebugUI.EnumField { nameAndTooltip = MaterialStrings.GBuffer, getter = () => data.materialDebugSettings.debugViewGBuffer, setter = value => SetDebugViewGBuffer(value), enumNames = MaterialDebugSettings.debugViewMaterialGBufferStrings, enumValues = MaterialDebugSettings.debugViewMaterialGBufferValues, getIndex = () => data.gBufferEnumIndex, setIndex = value => { data.ResetExclusiveEnumIndices(); data.gBufferEnumIndex = value; } });
            list.Add(new DebugUI.EnumField { nameAndTooltip = MaterialStrings.MaterialValidator, getter = () => (int)data.fullScreenDebugMode, setter = value => SetFullScreenDebugMode((FullScreenDebugMode)value), enumNames = s_MaterialFullScreenDebugStrings, enumValues = s_MaterialFullScreenDebugValues, getIndex = () => data.materialValidatorDebugModeEnumIndex, setIndex = value => { data.ResetExclusiveEnumIndices(); data.materialValidatorDebugModeEnumIndex = value; } });

            list.Add(new DebugUI.Container
            {
                isHiddenCallback = () => data.fullScreenDebugMode != FullScreenDebugMode.ValidateDiffuseColor && data.fullScreenDebugMode != FullScreenDebugMode.ValidateSpecularColor,
                children =
                {
                    new DebugUI.ColorField { nameAndTooltip = MaterialStrings.ValidatorTooHighColor, getter = () => data.materialDebugSettings.materialValidateHighColor, setter = value => data.materialDebugSettings.materialValidateHighColor = value, showAlpha = false, hdr = true },
                    new DebugUI.ColorField { nameAndTooltip = MaterialStrings.ValidatorTooLowColor, getter = () => data.materialDebugSettings.materialValidateLowColor, setter = value => data.materialDebugSettings.materialValidateLowColor = value, showAlpha = false, hdr = true },
                    new DebugUI.ColorField { nameAndTooltip = MaterialStrings.ValidatorNotAPureMetalColor, getter = () => data.materialDebugSettings.materialValidateTrueMetalColor, setter = value => data.materialDebugSettings.materialValidateTrueMetalColor = value, showAlpha = false, hdr = true },
                    new DebugUI.BoolField  { nameAndTooltip = MaterialStrings.ValidatorPureMetals, getter = () => data.materialDebugSettings.materialValidateTrueMetal, setter = (v) => data.materialDebugSettings.materialValidateTrueMetal = v },
                }
            });

            list.Add(new DebugUI.Container
            {
                isHiddenCallback = () => !ShaderConfig.s_GlobalMipBias,
                children =
                {
                    new DebugUI.BoolField
                    {
                        nameAndTooltip = MaterialStrings.OverrideGlobalMaterialTextureMipBias,
                        getter = () => data.UseDebugGlobalMipBiasOverride(),
                        setter = (value) => data.SetUseDebugGlobalMipBiasOverride(value),
                    },
                    new DebugUI.FloatField
                    {
                        nameAndTooltip = MaterialStrings.DebugGlobalMaterialTextureMipBiasValue,
                        getter = () => data.GetDebugGlobalMipBiasOverride(),
                        setter = (value) => data.SetDebugGlobalMipBiasOverride(value),
                        isHiddenCallback = () => !data.UseDebugGlobalMipBiasOverride()
                    }
                }
            });

            m_DebugMaterialItems = list.ToArray();
            var panel = DebugManager.instance.GetPanel(k_PanelMaterials, true);
            panel.children.Add(m_DebugMaterialItems);
        }

        void RefreshDisplayStatsDebug<T>(DebugUI.Field<T> field, T value)
        {
            UnregisterDisplayStatsDebug();
            RegisterDisplayStatsDebug();
        }

        // For now we just rebuild the lighting panel if needed, but ultimately it could be done in a better way
        void RefreshLightingDebug<T>(DebugUI.Field<T> field, T value)
        {
            UnregisterDebugItems(k_PanelLighting, m_DebugLightingItems);
            RegisterLightingDebug();
        }

        void RefreshDecalsDebug<T>(DebugUI.Field<T> field, T value)
        {
            UnregisterDebugItems(k_PanelDecals, m_DebugDecalsItems);
            RegisterDecalsDebug();
        }

        void RefreshRenderingDebug<T>(DebugUI.Field<T> field, T value)
        {
            // Explicitly invoke the render debug unregister to handle render graph items.
            UnregisterRenderingDebug();
            RegisterRenderingDebug();
        }

        void RefreshMaterialDebug<T>(DebugUI.Field<T> field, T value)
        {
            UnregisterDebugItems(k_PanelMaterials, m_DebugMaterialItems);
            RegisterMaterialDebug();
        }

        static class LightingStrings
        {
            // Shadows
            public static readonly NameAndTooltip ShadowDebugMode = new() { name = "Shadow Debug Mode", tooltip = "Use the drop-down to select which shadow debug information to overlay on the screen." };
            public static readonly NameAndTooltip ShadowDebugUseSelection = new() { name = "Use Selection", tooltip = "Enable the checkbox to display the shadow map for the Light you have selected in the Scene." };
            public static readonly NameAndTooltip ShadowDebugShadowMapIndex = new() { name = "Shadow Map Index", tooltip = "Use the slider to view a specific index of the shadow map. To use this property, your scene must include a Light that uses a shadow map." };
            public static readonly NameAndTooltip GlobalShadowScaleFactor = new() { name = "Global Shadow Scale Factor", tooltip = "Use the slider to set the global scale that HDRP applies to the shadow rendering resolution." };
            public static readonly NameAndTooltip ClearShadowAtlas = new() { name = "Clear Shadow Atlas", tooltip = "Enable the checkbox to clear the shadow atlas every frame." };
            public static readonly NameAndTooltip ShadowRangeMinimumValue = new() { name = "Shadow Range Minimum Value", tooltip = "Set the minimum shadow value to display in the various shadow debug overlays." };
            public static readonly NameAndTooltip ShadowRangeMaximumValue = new() { name = "Shadow Range Maximum Value", tooltip = "Set the maximum shadow value to display in the various shadow debug overlays." };
            public static readonly NameAndTooltip LogCachedShadowAtlasStatus = new() { name = "Log Cached Shadow Atlas Status", tooltip = "Displays a list of the Lights currently in the cached shadow atlas in the Console." };

            // Lighting
            public static readonly NameAndTooltip ShowLightsByType = new() { name = "Show Lights By Type", tooltip = "Allows the user to enable or disable lights in the scene based on their type. This will not change the actual settings of the light." };
            public static readonly NameAndTooltip DirectionalLights = new() { name = "Directional Lights", tooltip = "Temporarily enables or disables Directional Lights in your Scene." };
            public static readonly NameAndTooltip PunctualLights = new() { name = "Punctual Lights", tooltip = "Temporarily enables or disables Punctual Lights in your Scene." };
            public static readonly NameAndTooltip AreaLights = new() { name = "Area Lights", tooltip = "Temporarily enables or disables Area Lights in your Scene." };
            public static readonly NameAndTooltip ReflectionProbes = new() { name = "Reflection Probes", tooltip = "Temporarily enables or disables Reflection Probes in your Scene." };

            public static readonly NameAndTooltip Exposure = new() { name = "Exposure", tooltip = "Allows the selection of an Exposure debug mode to use." };
            public static readonly NameAndTooltip HDROutput = new() { name = "HDR", tooltip = "Allows the selection of an HDR debug mode to use." };
            public static readonly NameAndTooltip HDROutputDebugMode = new() { name = "DebugMode", tooltip = "Use the drop-down to select a debug mode for HDR Output." };
            public static readonly NameAndTooltip ExposureDebugMode = new() { name = "DebugMode", tooltip = "Use the drop-down to select a debug mode to validate the exposure." };
            public static readonly NameAndTooltip ExposureDisplayMaskOnly = new() { name = "Display Mask Only", tooltip = "Display only the metering mask in the picture-in-picture. When disabled, the mask is visible after weighting the scene color instead." };
            public static readonly NameAndTooltip ExposureShowTonemapCurve = new() { name = "Show Tonemap Curve", tooltip = "Overlay the tonemap curve to the histogram debug view." };
            public static readonly NameAndTooltip DisplayHistogramSceneOverlay = new () { name = "Show Scene Overlay", tooltip = "Display the scene overlay showing pixels excluded by the exposure computation via histogram." };
            public static readonly NameAndTooltip ExposureCenterAroundExposure = new() { name = "Center Around Exposure", tooltip = "Center the histogram around the current exposure value." };
            public static readonly NameAndTooltip ExposureDisplayRGBHistogram = new() { name = "Display RGB Histogram", tooltip = "Display the Final Image Histogram as an RGB histogram instead of just luminance." };
            public static readonly NameAndTooltip DebugExposureCompensation = new() { name = "Debug Exposure Compensation", tooltip = "Set an additional exposure on top of your current exposure for debug purposes." };

            public static readonly NameAndTooltip LightingDebugMode = new() { name = "Lighting Debug Mode", tooltip = "Use the drop-down to select a lighting mode to debug." };
            public static readonly NameAndTooltip LightHierarchyDebugMode = new() { name = "Light Hierarchy Debug Mode", tooltip = "Use the drop-down to select a light type to show the direct lighting for or a Reflection Probe type to show the indirect lighting for." };
            public static readonly NameAndTooltip LightLayersVisualization = new() { name = "Light Layers Visualization", tooltip = "Visualize the light layers of GameObjects in your Scene." };

            public static readonly NameAndTooltip LightLayersUseSelectedLight = new() { name = "Use Selected Light", tooltip = "Visualize GameObjects affected by the selected light." };
            public static readonly NameAndTooltip LightLayersSwitchToLightShadowLayers = new() { name = "Switch To Light's Shadow Layers", tooltip = "Visualize GameObjects that cast shadows for the selected light." };
            public static readonly NameAndTooltip LightLayersFilterLayers = new() { name = "Filter Layers", tooltip = "Use the drop-down to filter light layers that you want to visialize." };
            public static readonly NameAndTooltip LightLayersColor = new() { name = "Layers Color", tooltip = "Select the display color of each light layer." };

            // Material Overrides
            public static readonly NameAndTooltip OverrideSmoothness = new() { name = "Override Smoothness", tooltip = "Enable the checkbox to override the smoothness for the entire Scene." };
            public static readonly NameAndTooltip OverrideAlbedo = new() { name = "Override Albedo", tooltip = "Enable the checkbox to override the albedo for the entire Scene." };
            public static readonly NameAndTooltip OverrideNormal = new() { name = "Override Normal", tooltip = "Enable the checkbox to override the normals for the entire Scene with object normals for lighting debug." };
            public static readonly NameAndTooltip OverrideSpecularColor = new() { name = "Override Specular Color", tooltip = "Enable the checkbox to override the specular color for the entire Scene." };
            public static readonly NameAndTooltip OverrideAmbientOcclusion = new() { name = "Override Ambient Occlusion", tooltip = "Enable the checkbox to override the ambient occlusion for the entire Scene." };
            public static readonly NameAndTooltip OverrideEmissiveColor = new() { name = "Override Emissive Color", tooltip = "Enable the checkbox to override the emissive color for the entire Scene." };
            public static readonly NameAndTooltip Smoothness = new() { name = "Smoothness", tooltip = "Use the slider to set the smoothness override value that HDRP uses for the entire Scene." };
            public static readonly NameAndTooltip Albedo = new() { name = "Albedo", tooltip = "Use the color picker to set the albedo color that HDRP uses for the entire Scene." };
            public static readonly NameAndTooltip SpecularColor = new() { name = "Specular Color", tooltip = "Use the color picker to set the specular color that HDRP uses for the entire Scene." };
            public static readonly NameAndTooltip AmbientOcclusion = new() { name = "Ambient Occlusion", tooltip = "Use the slider to set the Ambient Occlusion override value that HDRP uses for the entire Scene." };
            public static readonly NameAndTooltip EmissiveColor = new() { name = "Emissive Color", tooltip = "Use the color picker to set the emissive color that HDRP uses for the entire Scene." };

            // Fullscreen debug
            public static readonly NameAndTooltip FullscreenDebugMode = new() { name = "Fullscreen Debug Mode", tooltip = "Use the drop-down to select a rendering mode to display as an overlay on the screen." };
            public static readonly NameAndTooltip ScreenSpaceShadowIndex = new() { name = "Screen Space Shadow Index", tooltip = "Select the index of the screen space shadows to view with the slider. There must be a Light in the scene that uses Screen Space Shadows." };
            public static readonly NameAndTooltip DepthPyramidDebugMip = new() { name = "Debug Mip", tooltip = "Enable to view a lower-resolution mipmap." };
            public static readonly NameAndTooltip DepthPyramidEnableRemap = new() { name = "Enable Depth Remap", tooltip = "Enable remapping of displayed depth values for better vizualization." };
            public static readonly NameAndTooltip DepthPyramidRangeMin = new() { name = "Depth Range Min Value", tooltip = "Distance at which depth values remap starts (0 is near plane, 1 is far plane)" };
            public static readonly NameAndTooltip DepthPyramidRangeMax = new() { name = "Depth Range Max Value", tooltip = "Distance at which depth values remap ends (0 is near plane, 1 is far plane)" };
            public static readonly NameAndTooltip ContactShadowsLightIndex = new() { name = "Light Index", tooltip = "Enable to display Contact shadows for each Light individually." };
            public static readonly NameAndTooltip RTASDebugView = new() { name = "Ray Tracing Acceleration Structure View", tooltip = "Use the drop-down to select a rendering view to display the ray tracing acceleration structure." };
            public static readonly NameAndTooltip RTASDebugMode = new() { name = "Ray Tracing Acceleration Structure Mode", tooltip = "Use the drop-down to select a rendering mode to display the ray tracing acceleration structure." };

            // Tile/Cluster debug
            public static readonly NameAndTooltip TileClusterDebug = new() { name = "Tile/Cluster Debug", tooltip = "Use the drop-down to select the Light type that you want to show the Tile/Cluster debug information for." };
            public static readonly NameAndTooltip TileClusterDebugByCategory = new() { name = "Tile/Cluster Debug By Category", tooltip = "Use the drop-down to select the visualization mode for the cluster." };
            public static readonly NameAndTooltip ClusterDebugMode = new() { name = "Cluster Debug Mode", tooltip = "Select the debug visualization mode for the Cluster." };
            public static readonly NameAndTooltip ClusterDistance = new() { name = "Cluster Distance", tooltip = "Set the distance from the camera that HDRP displays the Cluster slice." };

            // Sky reflections
            public static readonly NameAndTooltip DisplaySkyReflection = new() { name = "Display Sky Reflection", tooltip = "Enable the checkbox to display an overlay of the cube map that the current sky generates and HDRP uses for lighting." };
            public static readonly NameAndTooltip SkyReflectionMipmap = new() { name = "Sky Reflection Mipmap", tooltip = "Use the slider to set the mipmap level of the sky reflection cubemap. Use this to view the sky reflection cubemap's different mipmap levels." };

            // Light volumes
            public static readonly NameAndTooltip DisplayLightVolumes = new() { name = "Display Light Volumes", tooltip = "Enable the checkbox to show an overlay of all light bounding volumes." };
            public static readonly NameAndTooltip LightVolumeDebugType = new() { name = "Light Volume Debug Type", tooltip = "Use the drop-down to select the method HDRP uses to display the light volumes." };
            public static readonly NameAndTooltip MaxDebugLightCount = new() { name = "Max Debug Light Count", tooltip = "Use this property to change the maximum acceptable number of lights for your application and still see areas in red." };

            // Cookie atlas
            public static readonly NameAndTooltip DisplayCookieAtlas = new() { name = "Display Cookie Atlas", tooltip = "Enable the checkbox to display an overlay of the cookie atlas." };
            public static readonly NameAndTooltip CookieAtlasMipLevel = new() { name = "Mip Level", tooltip = "Use the slider to set the mipmap level of the cookie atlas." };
            public static readonly NameAndTooltip ClearCookieAtlas = new() { name = "Clear Cookie Atlas", tooltip = "Enable to clear the cookie atlas at each frame." };

            // Reflection probe atlas
            public static readonly NameAndTooltip DisplayReflectionProbeAtlas = new() { name = "Display Reflection Probe Atlas", tooltip = "Enable the checkbox to display an overlay of the reflection probe atlas." };
            public static readonly NameAndTooltip ReflectionProbeAtlasMipLevel = new() { name = "Mip Level", tooltip = "Use the slider to set the mipmap level of the reflection probe atlas." };
            public static readonly NameAndTooltip ReflectionProbeAtlasSlice = new() { name = "Slice", tooltip = "Use the slider to set the slice of the reflection probe atlas." };
            public static readonly NameAndTooltip ReflectionProbeApplyExposure = new() { name = "Apply Exposure", tooltip = "Apply exposure to displayed atlas." };
            public static readonly NameAndTooltip ClearReflectionProbeAtlas = new() { name = "Clear Reflection Probe Atlas", tooltip = "Enable to clear the reflection probe atlas each frame." };

            public static readonly NameAndTooltip DebugOverlayScreenRatio = new() { name = "Debug Overlay Screen Ratio", tooltip = "Set the size of the debug overlay textures with a ratio of the screen size." };
        }

        void RegisterLightingDebug()
        {
            var list = new List<DebugUI.Widget>();
            list.Add(CreateMissingDebugShadersWarning());
            {
                var shadows = new DebugUI.Container() { displayName = "Shadows" };

                shadows.children.Add(new DebugUI.EnumField { nameAndTooltip = LightingStrings.ShadowDebugMode, getter = () => (int)data.lightingDebugSettings.shadowDebugMode, setter = value => SetShadowDebugMode((ShadowMapDebugMode)value), autoEnum = typeof(ShadowMapDebugMode), getIndex = () => data.shadowDebugModeEnumIndex, setIndex = value => data.shadowDebugModeEnumIndex = value });
                {
                    shadows.children.Add(new DebugUI.Container()
                    {
                        isHiddenCallback = () => data.lightingDebugSettings.shadowDebugMode != ShadowMapDebugMode.VisualizeShadowMap && data.lightingDebugSettings.shadowDebugMode != ShadowMapDebugMode.SingleShadow,
                        children =
                        {
                            new DebugUI.BoolField { nameAndTooltip = LightingStrings.ShadowDebugUseSelection, getter = () => data.lightingDebugSettings.shadowDebugUseSelection, setter = value => data.lightingDebugSettings.shadowDebugUseSelection = value, flags = DebugUI.Flags.EditorOnly },
                            new DebugUI.UIntField { nameAndTooltip = LightingStrings.ShadowDebugShadowMapIndex, getter = () => data.lightingDebugSettings.shadowMapIndex, setter = value => data.lightingDebugSettings.shadowMapIndex = value, min = () => 0u, max = () => (uint)(Math.Max(0, (RenderPipelineManager.currentPipeline as HDRenderPipeline).GetCurrentShadowCount() - 1u)), isHiddenCallback = () => data.lightingDebugSettings.shadowDebugUseSelection }
                        }
                    });
                }

                shadows.children.Add(new DebugUI.FloatField
                {
                    nameAndTooltip = LightingStrings.GlobalShadowScaleFactor,
                    getter = () => data.lightingDebugSettings.shadowResolutionScaleFactor,
                    setter = (v) => data.lightingDebugSettings.shadowResolutionScaleFactor = v,
                    min = () => 0.01f,
                    max = () => 4.0f,
                });

                shadows.children.Add(new DebugUI.BoolField
                {
                    nameAndTooltip = LightingStrings.ClearShadowAtlas,
                    getter = () => data.lightingDebugSettings.clearShadowAtlas,
                    setter = (v) => data.lightingDebugSettings.clearShadowAtlas = v
                });

                shadows.children.Add(new DebugUI.FloatField { nameAndTooltip = LightingStrings.ShadowRangeMinimumValue, getter = () => data.lightingDebugSettings.shadowMinValue, setter = value => data.lightingDebugSettings.shadowMinValue = value });
                shadows.children.Add(new DebugUI.FloatField { nameAndTooltip = LightingStrings.ShadowRangeMaximumValue, getter = () => data.lightingDebugSettings.shadowMaxValue, setter = value => data.lightingDebugSettings.shadowMaxValue = value });
                list.Add(shadows);
            }

            {
                var lighting = new DebugUI.Container() { displayName = "Lighting" };

                lighting.children.Add(new DebugUI.Foldout
                {
                    nameAndTooltip = LightingStrings.ShowLightsByType,
                    children =
                    {
                        new DebugUI.BoolField { nameAndTooltip = LightingStrings.DirectionalLights, getter = () => data.lightingDebugSettings.showDirectionalLight, setter = value => data.lightingDebugSettings.showDirectionalLight = value },
                        new DebugUI.BoolField { nameAndTooltip = LightingStrings.PunctualLights, getter = () => data.lightingDebugSettings.showPunctualLight, setter = value => data.lightingDebugSettings.showPunctualLight = value },
                        new DebugUI.BoolField { nameAndTooltip = LightingStrings.AreaLights, getter = () => data.lightingDebugSettings.showAreaLight, setter = value => data.lightingDebugSettings.showAreaLight = value },
                        new DebugUI.BoolField { nameAndTooltip = LightingStrings.ReflectionProbes, getter = () => data.lightingDebugSettings.showReflectionProbe, setter = value => data.lightingDebugSettings.showReflectionProbe = value },
                    }
                });

                {
                    var exposureFoldout = new DebugUI.Foldout
                    {
                        nameAndTooltip = LightingStrings.Exposure,
                        children =
                        {
                            new DebugUI.EnumField
                            {
                                nameAndTooltip = LightingStrings.ExposureDebugMode,
                                getter = () => (int)data.lightingDebugSettings.exposureDebugMode,
                                setter = value => SetExposureDebugMode((ExposureDebugMode)value),
                                autoEnum = typeof(ExposureDebugMode),
                                getIndex = () => data.exposureDebugModeEnumIndex,
                                setIndex = value => data.exposureDebugModeEnumIndex = value
                            },
                            new DebugUI.BoolField()
                            {
                                nameAndTooltip = LightingStrings.ExposureDisplayMaskOnly,
                                getter = () => data.lightingDebugSettings.displayMaskOnly,
                                setter = value => data.lightingDebugSettings.displayMaskOnly = value,
                                isHiddenCallback = () => data.lightingDebugSettings.exposureDebugMode != ExposureDebugMode.MeteringWeighted
                            },
                            new DebugUI.Container()
                            {
                                isHiddenCallback = () => data.lightingDebugSettings.exposureDebugMode != ExposureDebugMode.HistogramView,
                                children =
                                {
                                    new DebugUI.BoolField()
                                    {
                                        nameAndTooltip = LightingStrings.DisplayHistogramSceneOverlay,
                                        getter = () => data.lightingDebugSettings.displayOnSceneOverlay,
                                        setter = value => data.lightingDebugSettings.displayOnSceneOverlay = value
                                    },
                                    new DebugUI.BoolField()
                                    {
                                        nameAndTooltip = LightingStrings.ExposureShowTonemapCurve,
                                        getter = () => data.lightingDebugSettings.showTonemapCurveAlongHistogramView,
                                        setter = value => data.lightingDebugSettings.showTonemapCurveAlongHistogramView = value
                                    },
                                    new DebugUI.BoolField()
                                    {
                                        nameAndTooltip = LightingStrings.ExposureCenterAroundExposure,
                                        getter = () => data.lightingDebugSettings.centerHistogramAroundMiddleGrey,
                                        setter = value => data.lightingDebugSettings.centerHistogramAroundMiddleGrey = value
                                    }
                                }
                            },
                            new DebugUI.BoolField()
                            {
                                nameAndTooltip = LightingStrings.ExposureDisplayRGBHistogram,
                                getter = () => data.lightingDebugSettings.displayFinalImageHistogramAsRGB,
                                setter = value => data.lightingDebugSettings.displayFinalImageHistogramAsRGB = value,
                                isHiddenCallback = () => data.lightingDebugSettings.exposureDebugMode != ExposureDebugMode.FinalImageHistogramView
                            },
                            new DebugUI.FloatField
                            {
                                nameAndTooltip = LightingStrings.DebugExposureCompensation,
                                getter = () => data.lightingDebugSettings.debugExposure,
                                setter = value => data.lightingDebugSettings.debugExposure = value
                            }
                        }
                    };

                    lighting.children.Add(exposureFoldout);
                }

                var hdrFoldout = new DebugUI.Foldout
                {
                    nameAndTooltip = LightingStrings.HDROutput,
                    children =
                    {
                        new DebugUI.MessageBox
                        {
                            displayName = "No HDR monitor detected.",
                            style = DebugUI.MessageBox.Style.Warning,
                            isHiddenCallback = () => HDRenderPipeline.HDROutputForMainDisplayIsActive()
                        },
                        new DebugUI.MessageBox
                        {
                            displayName = "To display the Gamut View, Gamut Clip, Paper White modes without affecting them, the overlay will be hidden.",
                            style = DebugUI.MessageBox.Style.Info,
                            isHiddenCallback = () => !HDRenderPipeline.HDROutputForMainDisplayIsActive()
                        },
                        new DebugUI.EnumField
                        {
                            nameAndTooltip = LightingStrings.HDROutputDebugMode,
                            getter = () => (int)data.lightingDebugSettings.hdrDebugMode,
                            setter = value => SetHDRDebugMode((HDRDebugMode)value),
                            autoEnum = typeof(HDRDebugMode),
                            getIndex = () => data.hdrDebugModeEnumIndex,
                            setIndex = value => data.hdrDebugModeEnumIndex = value
                        },
                    }
                };

                lighting.children.Add(hdrFoldout);

                lighting.children.Add(new DebugUI.EnumField { nameAndTooltip = LightingStrings.LightingDebugMode, getter = () => (int)data.lightingDebugSettings.debugLightingMode, setter = value => SetDebugLightingMode((DebugLightingMode)value), autoEnum = typeof(DebugLightingMode), getIndex = () => data.lightingDebugModeEnumIndex, setIndex = value => { data.ResetExclusiveEnumIndices(); data.lightingDebugModeEnumIndex = value; } });
                lighting.children.Add(new DebugUI.BitField { nameAndTooltip = LightingStrings.LightHierarchyDebugMode, getter = () => data.lightingDebugSettings.debugLightFilterMode, setter = value => SetDebugLightFilterMode((DebugLightFilterMode)value), enumType = typeof(DebugLightFilterMode)});

                lighting.children.Add(new DebugUI.BoolField { nameAndTooltip = LightingStrings.LightLayersVisualization, getter = () => data.lightingDebugSettings.debugLightLayers, setter = value => SetDebugLightLayersMode(value)});

                {
                    var container = new DebugUI.Container()
                    {
                        isHiddenCallback = () => !data.lightingDebugSettings.debugLightLayers,
                        children =
                        {
                            new DebugUI.BoolField
                            {
                                nameAndTooltip = LightingStrings.LightLayersUseSelectedLight,
                                getter = () => data.lightingDebugSettings.debugSelectionLightLayers,
                                setter = value => data.lightingDebugSettings.debugSelectionLightLayers = value,
                                flags = DebugUI.Flags.EditorOnly,
                            },
                            new DebugUI.BoolField
                            {
                                nameAndTooltip = LightingStrings.LightLayersSwitchToLightShadowLayers,
                                getter = () => data.lightingDebugSettings.debugSelectionShadowLayers,
                                setter = value => data.lightingDebugSettings.debugSelectionShadowLayers = value,
                                flags = DebugUI.Flags.EditorOnly,
                                isHiddenCallback = () => !data.lightingDebugSettings.debugSelectionLightLayers
                            }
                        }
                    };

                    {
                        var field = new DebugUI.BitField
                        {
                            nameAndTooltip = LightingStrings.LightLayersFilterLayers,
                            getter = () => data.lightingDebugSettings.debugLightLayersFilterMask,
                            setter = value => data.lightingDebugSettings.debugLightLayersFilterMask = (DebugLightLayersMask)value,
                            enumType = typeof(DebugLightLayersMask),
                            isHiddenCallback = () => data.lightingDebugSettings.debugSelectionLightLayers
                        };

                        for (int i = 0; i < 8; i++)
                            field.enumNames[i + 1].text = HDRenderPipelineGlobalSettings.instance.prefixedRenderingLayerMaskNames[i];
                        container.children.Add(field);
                    }

                    var layersColor = new DebugUI.Foldout() { nameAndTooltip = LightingStrings.LightLayersColor, flags = DebugUI.Flags.EditorOnly };
                    for (int i = 0; i < 8; i++)
                    {
                        int index = i;
                        layersColor.children.Add(new DebugUI.ColorField
                        {
                            displayName = HDRenderPipelineGlobalSettings.instance.prefixedRenderingLayerMaskNames[i],
                            flags = DebugUI.Flags.EditorOnly,
                            getter = () => data.lightingDebugSettings.debugRenderingLayersColors[index],
                            setter = value => data.lightingDebugSettings.debugRenderingLayersColors[index] = value
                        });
                    }

                    container.children.Add(layersColor);
                    lighting.children.Add(container);
                }
                list.Add(lighting);
            }

            {
                var material = new DebugUI.Container()
                {
                    displayName = "Material Overrides",
                    children =
                    {
                        new DebugUI.BoolField { nameAndTooltip = LightingStrings.OverrideSmoothness, getter = () => data.lightingDebugSettings.overrideSmoothness, setter = value => data.lightingDebugSettings.overrideSmoothness = value },
                        new DebugUI.Container
                        {
                            isHiddenCallback = () => !data.lightingDebugSettings.overrideSmoothness,
                            children =
                            {
                                new DebugUI.FloatField { nameAndTooltip = LightingStrings.Smoothness, getter = () => data.lightingDebugSettings.overrideSmoothnessValue, setter = value => data.lightingDebugSettings.overrideSmoothnessValue = value, min = () => 0f, max = () => 1f, incStep = 0.025f }
                            }
                        },
                        new DebugUI.BoolField { nameAndTooltip = LightingStrings.OverrideAlbedo, getter = () => data.lightingDebugSettings.overrideAlbedo, setter = value => data.lightingDebugSettings.overrideAlbedo = value },
                        new DebugUI.Container
                        {
                            isHiddenCallback = () => !data.lightingDebugSettings.overrideAlbedo,
                            children =
                            {
                                new DebugUI.ColorField { nameAndTooltip = LightingStrings.Albedo, getter = () => data.lightingDebugSettings.overrideAlbedoValue, setter = value => data.lightingDebugSettings.overrideAlbedoValue = value, showAlpha = false, hdr = false }
                            }
                        },
                        new DebugUI.BoolField { nameAndTooltip = LightingStrings.OverrideNormal, getter = () => data.lightingDebugSettings.overrideNormal, setter = value => data.lightingDebugSettings.overrideNormal = value },
                        new DebugUI.BoolField { nameAndTooltip = LightingStrings.OverrideSpecularColor, getter = () => data.lightingDebugSettings.overrideSpecularColor, setter = value => data.lightingDebugSettings.overrideSpecularColor = value },
                        new DebugUI.Container
                        {
                            isHiddenCallback = () => !data.lightingDebugSettings.overrideSpecularColor,
                            children =
                            {
                                new DebugUI.ColorField { nameAndTooltip = LightingStrings.SpecularColor, getter = () => data.lightingDebugSettings.overrideSpecularColorValue, setter = value => data.lightingDebugSettings.overrideSpecularColorValue = value, showAlpha = false, hdr = false }
                            }
                        },
                        new DebugUI.BoolField { nameAndTooltip = LightingStrings.OverrideAmbientOcclusion, getter = () => data.lightingDebugSettings.overrideAmbientOcclusion, setter = value => data.lightingDebugSettings.overrideAmbientOcclusion = value},
                        new DebugUI.Container
                        {
                            isHiddenCallback = () => !data.lightingDebugSettings.overrideAmbientOcclusion,
                            children =
                            {
                                new DebugUI.FloatField { nameAndTooltip = LightingStrings.AmbientOcclusion, getter = () => data.lightingDebugSettings.overrideAmbientOcclusionValue, setter = value => data.lightingDebugSettings.overrideAmbientOcclusionValue = value, min = () => 0f, max = () => 1f, incStep = 0.025f }
                            }
                        },
                        new DebugUI.BoolField { nameAndTooltip = LightingStrings.OverrideEmissiveColor, getter = () => data.lightingDebugSettings.overrideEmissiveColor, setter = value => data.lightingDebugSettings.overrideEmissiveColor = value },
                        new DebugUI.Container
                        {
                            isHiddenCallback = () => !data.lightingDebugSettings.overrideEmissiveColor,
                            children =
                            {
                                new DebugUI.ColorField { nameAndTooltip = LightingStrings.EmissiveColor, getter = () => data.lightingDebugSettings.overrideEmissiveColorValue, setter = value => data.lightingDebugSettings.overrideEmissiveColorValue = value, showAlpha = false, hdr = true }
                            }
                        }
                    }
                };

                list.Add(material);
            }

            list.Add(new DebugUI.EnumField
            {
                nameAndTooltip = LightingStrings.FullscreenDebugMode,
                getter = () => (int)data.fullScreenDebugMode,
                setter = value => SetFullScreenDebugMode((FullScreenDebugMode)value),
                enumNames = s_LightingFullScreenDebugStrings,
                enumValues = s_LightingFullScreenDebugValues,
                getIndex = () => data.lightingFulscreenDebugModeEnumIndex,
                setIndex = value =>
                {
                    data.ResetExclusiveEnumIndices();
                    data.lightingFulscreenDebugModeEnumIndex = value;
                },
                onValueChanged = (_, __) =>
                {
                    switch (data.fullScreenDebugMode)
                    {
                        case FullScreenDebugMode.PreRefractionColorPyramid:
                        case FullScreenDebugMode.FinalColorPyramid:
                        case FullScreenDebugMode.DepthPyramid:
                        case FullScreenDebugMode.ContactShadows:
                            break;
                        default:
                            data.fullscreenDebugMip = 0;
                            break;
                    }
                }
            });
            list.Add(new DebugUI.Container
            {
                children =
                {
                    new DebugUI.UIntField
                    {
                        nameAndTooltip = LightingStrings.ScreenSpaceShadowIndex,
                        getter = () => data.screenSpaceShadowIndex,
                        setter = value => data.screenSpaceShadowIndex = value,
                        min = () => 0u,
                        max = () => (uint)(RenderPipelineManager.currentPipeline as HDRenderPipeline).GetMaxScreenSpaceShadows() - 1,
                        isHiddenCallback = () => data.fullScreenDebugMode != FullScreenDebugMode.ScreenSpaceShadows
                    }
                }
            });
            list.Add(new DebugUI.Container
            {
                isHiddenCallback = () => data.fullScreenDebugMode != FullScreenDebugMode.RayTracingAccelerationStructure,
                children =
                {
                    new DebugUI.EnumField { nameAndTooltip = LightingStrings.RTASDebugView, getter = () => (int)data.rtasDebugView, setter = value => SetRTASDebugView((RTASDebugView)value), autoEnum = typeof(RTASDebugView), getIndex = () => data.rtasDebugViewEnumIndex, setIndex = value => { data.rtasDebugViewEnumIndex = value; } },
                    new DebugUI.EnumField { nameAndTooltip = LightingStrings.RTASDebugMode, getter = () => (int)data.rtasDebugMode, setter = value => SetRTASDebugMode((RTASDebugMode)value), autoEnum = typeof(RTASDebugMode), getIndex = () => data.rtasDebugModeEnumIndex, setIndex = value => { data.rtasDebugModeEnumIndex = value; } }
                }
            });
            list.Add(new DebugUI.Container()
            {
                isHiddenCallback = () => data.fullScreenDebugMode != FullScreenDebugMode.PreRefractionColorPyramid && data.fullScreenDebugMode != FullScreenDebugMode.FinalColorPyramid && data.fullScreenDebugMode != FullScreenDebugMode.DepthPyramid,
                children =
                {
                    new DebugUI.FloatField { nameAndTooltip = LightingStrings.DepthPyramidDebugMip, getter = () => data.fullscreenDebugMip, setter = value => data.fullscreenDebugMip = value, min = () => 0f, max = () => 1f, incStep = 0.05f },
                    new DebugUI.BoolField { nameAndTooltip = LightingStrings.DepthPyramidEnableRemap, getter = () => data.enableDebugDepthRemap, setter = value => data.enableDebugDepthRemap = value },
                    new DebugUI.Container()
                    {
                        isHiddenCallback = () => !data.enableDebugDepthRemap,
                        children =
                        {
                            new DebugUI.FloatField { nameAndTooltip = LightingStrings.DepthPyramidRangeMin, getter = () => data.fullScreenDebugDepthRemap.x, setter = value => data.fullScreenDebugDepthRemap.x = Mathf.Min(value, data.fullScreenDebugDepthRemap.y), min = () => 0f, max = () => 1f, incStep = 0.01f },
                            new DebugUI.FloatField { nameAndTooltip = LightingStrings.DepthPyramidRangeMax, getter = () => data.fullScreenDebugDepthRemap.y, setter = value => data.fullScreenDebugDepthRemap.y = Mathf.Max(value, data.fullScreenDebugDepthRemap.x), min = () => 0.01f, max = () => 1f, incStep = 0.01f }
                        }
                    }
                }
            });
            list.Add(new DebugUI.Container
            {
                isHiddenCallback = () => data.fullScreenDebugMode != FullScreenDebugMode.ContactShadows,
                children =
                {
                    new DebugUI.IntField
                    {
                        nameAndTooltip = LightingStrings.ContactShadowsLightIndex,
                        getter = () => data.fullScreenContactShadowLightIndex,
                        setter = value => data.fullScreenContactShadowLightIndex = value,
                        min = () => - 1, // -1 will display all contact shadow
                        max = () => ShaderConfig.FPTLMaxLightCount - 1
                    },
                }
            });

            list.Add(new DebugUI.EnumField { nameAndTooltip = LightingStrings.TileClusterDebug, getter = () => (int)data.lightingDebugSettings.tileClusterDebug, setter = value => data.lightingDebugSettings.tileClusterDebug = (TileClusterDebug)value, autoEnum = typeof(TileClusterDebug), getIndex = () => data.tileClusterDebugEnumIndex, setIndex = value => data.tileClusterDebugEnumIndex = value });
            {
                list.Add(new DebugUI.Container()
                {
                    isHiddenCallback = () => data.lightingDebugSettings.tileClusterDebug == TileClusterDebug.None || data.lightingDebugSettings.tileClusterDebug == TileClusterDebug.MaterialFeatureVariants,
                    children =
                    {
                        new DebugUI.EnumField { nameAndTooltip = LightingStrings.TileClusterDebugByCategory, getter = () => (int)data.lightingDebugSettings.tileClusterDebugByCategory, setter = value => data.lightingDebugSettings.tileClusterDebugByCategory = (TileClusterCategoryDebug)value, autoEnum = typeof(TileClusterCategoryDebug), getIndex = () => data.tileClusterDebugByCategoryEnumIndex, setIndex = value => data.tileClusterDebugByCategoryEnumIndex = value },
                        new DebugUI.Container()
                        {
                            isHiddenCallback = () => data.lightingDebugSettings.tileClusterDebug != TileClusterDebug.Cluster,
                            children =
                            {
                                new DebugUI.EnumField { nameAndTooltip = LightingStrings.ClusterDebugMode, getter = () => (int)data.lightingDebugSettings.clusterDebugMode, setter = value => data.lightingDebugSettings.clusterDebugMode = (ClusterDebugMode)value, autoEnum = typeof(ClusterDebugMode), getIndex = () => data.clusterDebugModeEnumIndex, setIndex = value => data.clusterDebugModeEnumIndex = value },
                                new DebugUI.FloatField { isHiddenCallback = () => data.lightingDebugSettings.clusterDebugMode != ClusterDebugMode.VisualizeSlice, nameAndTooltip = LightingStrings.ClusterDistance, getter = () => data.lightingDebugSettings.clusterDebugDistance, setter = value => data.lightingDebugSettings.clusterDebugDistance = value, min = () => 0f, max = () => 100.0f, incStep = 0.05f }
                            }
                        }
                    }
                });
            }

            list.Add(new DebugUI.BoolField { nameAndTooltip = LightingStrings.DisplaySkyReflection, getter = () => data.lightingDebugSettings.displaySkyReflection, setter = value => data.lightingDebugSettings.displaySkyReflection = value });
            {
                list.Add(new DebugUI.Container
                {
                    isHiddenCallback = () => !data.lightingDebugSettings.displaySkyReflection,
                    children =
                    {
                        new DebugUI.FloatField { nameAndTooltip = LightingStrings.SkyReflectionMipmap, getter = () => data.lightingDebugSettings.skyReflectionMipmap, setter = value => data.lightingDebugSettings.skyReflectionMipmap = value, min = () => 0f, max = () => 1f, incStep = 0.05f }
                    }
                });
            }

            list.Add(new DebugUI.BoolField { nameAndTooltip = LightingStrings.DisplayLightVolumes, getter = () => data.lightingDebugSettings.displayLightVolumes, setter = value => data.lightingDebugSettings.displayLightVolumes = value});
            {
                list.Add(new DebugUI.Container
                {
                    isHiddenCallback = () => !data.lightingDebugSettings.displayLightVolumes,
                    children =
                    {
                        new DebugUI.EnumField { nameAndTooltip = LightingStrings.LightVolumeDebugType, getter = () => (int)data.lightingDebugSettings.lightVolumeDebugByCategory, setter = value => data.lightingDebugSettings.lightVolumeDebugByCategory = (LightVolumeDebug)value, autoEnum = typeof(LightVolumeDebug), getIndex = () => data.lightVolumeDebugTypeEnumIndex, setIndex = value => data.lightVolumeDebugTypeEnumIndex = value },
                        new DebugUI.UIntField { isHiddenCallback = () => data.lightingDebugSettings.lightVolumeDebugByCategory != LightVolumeDebug.Gradient, nameAndTooltip = LightingStrings.MaxDebugLightCount, getter = () => (uint)data.lightingDebugSettings.maxDebugLightCount, setter = value => data.lightingDebugSettings.maxDebugLightCount = value, min = () => 0, max = () => 24, incStep = 1 }
                    }
                });
            }

            list.Add(new DebugUI.BoolField { nameAndTooltip = LightingStrings.DisplayCookieAtlas, getter = () => data.lightingDebugSettings.displayCookieAtlas, setter = value => data.lightingDebugSettings.displayCookieAtlas = value });
            {
                list.Add(new DebugUI.Container
                {
                    isHiddenCallback = () => !data.lightingDebugSettings.displayCookieAtlas,
                    children =
                    {
                        new DebugUI.UIntField { nameAndTooltip = LightingStrings.CookieAtlasMipLevel, getter = () => data.lightingDebugSettings.cookieAtlasMipLevel, setter = value => data.lightingDebugSettings.cookieAtlasMipLevel = value, min = () => 0, max = () => (uint)(RenderPipelineManager.currentPipeline as HDRenderPipeline).GetCookieAtlasMipCount()},
                        new DebugUI.BoolField { nameAndTooltip = LightingStrings.ClearCookieAtlas, getter = () => data.lightingDebugSettings.clearCookieAtlas, setter = value => data.lightingDebugSettings.clearCookieAtlas = value}
                    }
                });
            }

            list.Add(new DebugUI.BoolField { nameAndTooltip = LightingStrings.DisplayReflectionProbeAtlas, getter = () => data.lightingDebugSettings.displayReflectionProbeAtlas, setter = value => data.lightingDebugSettings.displayReflectionProbeAtlas = value, onValueChanged = RefreshLightingDebug });
            {
                list.Add(new DebugUI.Container
                {
                    isHiddenCallback = () => !data.lightingDebugSettings.displayReflectionProbeAtlas,
                    children =
                    {
                        new DebugUI.UIntField { nameAndTooltip = LightingStrings.ReflectionProbeAtlasSlice,
                            getter = () => data.lightingDebugSettings.reflectionProbeSlice,
                            setter = value => data.lightingDebugSettings.reflectionProbeSlice = value,
                            min = () => 0,
                            max = () => (uint)(RenderPipelineManager.currentPipeline as HDRenderPipeline).GetReflectionProbeArraySize() - 1,
                            isHiddenCallback = () => (RenderPipelineManager.currentPipeline as HDRenderPipeline).GetReflectionProbeArraySize() == 1},
                        new DebugUI.UIntField { nameAndTooltip = LightingStrings.ReflectionProbeAtlasMipLevel, getter = () => data.lightingDebugSettings.reflectionProbeMipLevel, setter = value => data.lightingDebugSettings.reflectionProbeMipLevel = value, min = () => 0, max = () => (uint)(RenderPipelineManager.currentPipeline as HDRenderPipeline).GetReflectionProbeMipCount()},
                        new DebugUI.BoolField { nameAndTooltip = LightingStrings.ClearReflectionProbeAtlas, getter = () => data.lightingDebugSettings.clearReflectionProbeAtlas, setter = value => data.lightingDebugSettings.clearReflectionProbeAtlas = value},
                        new DebugUI.BoolField { nameAndTooltip = LightingStrings.ReflectionProbeApplyExposure, getter = () => data.lightingDebugSettings.reflectionProbeApplyExposure, setter = value => data.lightingDebugSettings.reflectionProbeApplyExposure = value},
                    }
                });
            }

            list.Add(new DebugUI.FloatField { nameAndTooltip = LightingStrings.DebugOverlayScreenRatio, getter = () => data.debugOverlayRatio, setter = v => data.debugOverlayRatio = v, min = () => 0.1f, max = () => 1f });

            m_DebugLightingItems = list.ToArray();
            var panel = DebugManager.instance.GetPanel(k_PanelLighting, true);
            panel.children.Add(m_DebugLightingItems);
        }

        static class RenderingStrings
        {
            public static readonly NameAndTooltip FullscreenDebugMode = new() { name = "Fullscreen Debug Mode", tooltip = "Use the drop-down to select a rendering mode to display as an overlay on the screen." };
            public static readonly NameAndTooltip MaxOverdrawCount = new() { name = "Max Overdraw Count", tooltip = "Maximum overdraw count allowed for a single pixel." };
            public static readonly NameAndTooltip MaxQuadCost = new() { name = "Max Quad Cost", tooltip = "The scale of the quad mode overdraw heat map." };
            public static readonly NameAndTooltip MaxVertexDensity = new() { name = "Max Vertex Density", tooltip = "The scale of the vertex density mode overdraw heat map." };

            // Mipmaps
            public static readonly NameAndTooltip MipMaps = new() { name = "Mip Maps", tooltip = "Use the drop-down to select a mipmap property to debug." };
            public static readonly NameAndTooltip TerrainTexture = new() { name = "Terrain Texture", tooltip = "Use the drop-down to select the terrain Texture to debug the mipmap for." };

            // Color picker
            public static readonly NameAndTooltip ColorPickerDebugMode = new() { name = "Debug Mode", tooltip = "Use the drop-down to select the format of the color picker display." };
            public static readonly NameAndTooltip ColorPickerFontColor = new() { name = "Font Color", tooltip = "Use the color picker to select a color for the font that the Color Picker uses for its display." };

            // False color
            public static readonly NameAndTooltip FalseColorMode = new() { name = "False Color Mode", tooltip = "Enable the checkbox to define intensity ranges that the debugger uses to show a color temperature gradient for the current frame." };
            public static readonly NameAndTooltip FalseColorRangeThreshold0 = new() { name = "Range Threshold 0", tooltip = "Set the split for the intensity range." };
            public static readonly NameAndTooltip FalseColorRangeThreshold1 = new() { name = "Range Threshold 1", tooltip = "Set the split for the intensity range." };
            public static readonly NameAndTooltip FalseColorRangeThreshold2 = new() { name = "Range Threshold 2", tooltip = "Set the split for the intensity range." };
            public static readonly NameAndTooltip FalseColorRangeThreshold3 = new() { name = "Range Threshold 3", tooltip = "Set the split for the intensity range." };

            public static readonly NameAndTooltip FreezeCameraForCulling = new() { name = "Freeze Camera For Culling", tooltip = "Use the drop-down to select a Camera to freeze in order to check its culling. To check if the Camera's culling works correctly, freeze the Camera and move occluders around it." };

            // Monitors
            public static readonly NameAndTooltip MonitorsSize        = new() { name = "Size"       , tooltip = "Sets the size ratio of the displayed monitors" };
            public static readonly NameAndTooltip WaveformToggle      = new() { name = "Waveform"   , tooltip = "Toggles the waveform monitor, displaying the full range of luma information in the render." };
            public static readonly NameAndTooltip WaveformExposure    = new() { name = "Exposure"   , tooltip = "Set the exposure of the waveform monitor." };
            public static readonly NameAndTooltip WaveformParade      = new() { name = "Parade mode", tooltip = "Toggles the parade mode of the waveform monitor, splitting the waveform into the red, green and blue channels separately." };
            public static readonly NameAndTooltip VectorscopeToggle   = new() { name = "Vectorscope", tooltip = "Toggles the vectorscope monitor, allowing to measure the overall range of hue and saturation within the image." };
            public static readonly NameAndTooltip VectorscopeExposure = new() { name = "Exposure"   , tooltip = "Set the exposure of the vectorscope monitor." };
        }

        void RegisterRenderingDebug()
        {
            var widgetList = new List<DebugUI.Widget>();

            widgetList.Add(CreateMissingDebugShadersWarning());

            widgetList.Add(
                new DebugUI.EnumField { nameAndTooltip = RenderingStrings.FullscreenDebugMode, getter = () => (int)data.fullScreenDebugMode, setter = value => SetFullScreenDebugMode((FullScreenDebugMode)value), enumNames = s_RenderingFullScreenDebugStrings, enumValues = s_RenderingFullScreenDebugValues, getIndex = () => data.renderingFulscreenDebugModeEnumIndex, setIndex = value => { data.ResetExclusiveEnumIndices(); data.renderingFulscreenDebugModeEnumIndex = value; } }
            );

            {
                widgetList.Add(new DebugUI.Container
                {
                    isHiddenCallback = () => data.fullScreenDebugMode != FullScreenDebugMode.TransparencyOverdraw,
                    children =
                    {
                        new DebugUI.FloatField { nameAndTooltip = RenderingStrings.MaxOverdrawCount, getter = () => data.transparencyDebugSettings.maxPixelCost, setter = value => data.transparencyDebugSettings.maxPixelCost = value, min = () => 0.25f, max = () => 2048.0f}
                    }
                });
            }

            {
                widgetList.Add(new DebugUI.Container
                {
                    isHiddenCallback = () => data.fullScreenDebugMode != FullScreenDebugMode.QuadOverdraw,
                    children =
                    {
                        new DebugUI.UIntField { nameAndTooltip = RenderingStrings.MaxQuadCost, getter = () => data.maxQuadCost, setter = value => data.maxQuadCost = value, min = () => 1, max = () => 10}
                    }
                });
            }

            {
                widgetList.Add(new DebugUI.Container
                {
                    isHiddenCallback = () => data.fullScreenDebugMode != FullScreenDebugMode.VertexDensity,
                    children =
                    {
                        new DebugUI.UIntField { nameAndTooltip = RenderingStrings.MaxVertexDensity, getter = () => data.maxVertexDensity, setter = value => data.maxVertexDensity = value, min = () => 1, max = () => 100}
                    }
                });
            }

            {
                widgetList.Add(new DebugUI.Container
                {
                    isHiddenCallback = () => data.fullScreenDebugMode != FullScreenDebugMode.MotionVectors,
                    children =
                    {
                        new DebugUI.FloatField {displayName = "Min Motion Vector Length (in pixels)", getter = () => data.minMotionVectorLength, setter = value => data.minMotionVectorLength = value, min = () => 0}
                    }
                });
            }

            widgetList.AddRange(new DebugUI.Widget[]
            {
                new DebugUI.EnumField { nameAndTooltip = RenderingStrings.MipMaps, getter = () => (int)data.mipMapDebugSettings.debugMipMapMode, setter = value => SetMipMapMode((DebugMipMapMode)value), autoEnum = typeof(DebugMipMapMode), getIndex = () => data.mipMapsEnumIndex, setIndex = value => { data.ResetExclusiveEnumIndices(); data.mipMapsEnumIndex = value; } },
            });

            {
                widgetList.Add(new DebugUI.Container
                {
                    isHiddenCallback = () => data.mipMapDebugSettings.debugMipMapMode == DebugMipMapMode.None,
                    children =
                    {
                        new DebugUI.EnumField { nameAndTooltip = RenderingStrings.TerrainTexture, getter = () => (int)data.mipMapDebugSettings.terrainTexture, setter = value => data.mipMapDebugSettings.terrainTexture = (DebugMipMapModeTerrainTexture)value, autoEnum = typeof(DebugMipMapModeTerrainTexture), getIndex = () => data.terrainTextureEnumIndex, setIndex = value => data.terrainTextureEnumIndex = value }
                    }
                });
            }

            widgetList.AddRange(new[]
            {
                new DebugUI.Container
                {
                    displayName = "Color Picker",
                    flags = DebugUI.Flags.EditorOnly,
                    children =
                    {
                        new DebugUI.EnumField  { nameAndTooltip = RenderingStrings.ColorPickerDebugMode, getter = () => (int)data.colorPickerDebugSettings.colorPickerMode, setter = value => data.colorPickerDebugSettings.colorPickerMode = (ColorPickerDebugMode)value, autoEnum = typeof(ColorPickerDebugMode), getIndex = () => data.colorPickerDebugModeEnumIndex, setIndex = value => data.colorPickerDebugModeEnumIndex = value },
                        new DebugUI.ColorField { nameAndTooltip = RenderingStrings.ColorPickerFontColor, flags = DebugUI.Flags.EditorOnly, getter = () => data.colorPickerDebugSettings.fontColor, setter = value => data.colorPickerDebugSettings.fontColor = value }
                    }
                }
            });

            widgetList.Add(new DebugUI.BoolField { nameAndTooltip = RenderingStrings.FalseColorMode, getter = () => data.falseColorDebugSettings.falseColor, setter = value => data.falseColorDebugSettings.falseColor = value});
            {
                widgetList.Add(new DebugUI.Container
                {
                    isHiddenCallback = () => !data.falseColorDebugSettings.falseColor,
                    flags = DebugUI.Flags.EditorOnly,
                    children =
                    {
                        new DebugUI.FloatField { nameAndTooltip = RenderingStrings.FalseColorRangeThreshold0, getter = () => data.falseColorDebugSettings.colorThreshold0, setter = value => data.falseColorDebugSettings.colorThreshold0 = Mathf.Min(value, data.falseColorDebugSettings.colorThreshold1) },
                        new DebugUI.FloatField { nameAndTooltip = RenderingStrings.FalseColorRangeThreshold1, getter = () => data.falseColorDebugSettings.colorThreshold1, setter = value => data.falseColorDebugSettings.colorThreshold1 = Mathf.Clamp(value, data.falseColorDebugSettings.colorThreshold0, data.falseColorDebugSettings.colorThreshold2) },
                        new DebugUI.FloatField { nameAndTooltip = RenderingStrings.FalseColorRangeThreshold2, getter = () => data.falseColorDebugSettings.colorThreshold2, setter = value => data.falseColorDebugSettings.colorThreshold2 = Mathf.Clamp(value, data.falseColorDebugSettings.colorThreshold1, data.falseColorDebugSettings.colorThreshold3) },
                        new DebugUI.FloatField { nameAndTooltip = RenderingStrings.FalseColorRangeThreshold3, getter = () => data.falseColorDebugSettings.colorThreshold3, setter = value => data.falseColorDebugSettings.colorThreshold3 = Mathf.Max(value, data.falseColorDebugSettings.colorThreshold2) },
                    }
                });
            }

            widgetList.AddRange(new DebugUI.Widget[]
            {
                new DebugUI.EnumField { nameAndTooltip = RenderingStrings.FreezeCameraForCulling, getter = () => data.debugCameraToFreeze, setter = value => data.debugCameraToFreeze = value, enumNames = s_CameraNamesStrings, enumValues = s_CameraNamesValues, getIndex = () => data.debugCameraToFreezeEnumIndex, setIndex = value => data.debugCameraToFreezeEnumIndex = value },
            });

            widgetList.Add(new DebugUI.Container
            {
                displayName = "Color Monitors",
                children    =
                {
                    new DebugUI.BoolField
                    {
                        nameAndTooltip = RenderingStrings.WaveformToggle,
                        getter = ()    =>   data.monitorsDebugSettings.waveformToggle,
                        setter = value => { data.monitorsDebugSettings.waveformToggle = value; }
                    },
                    new DebugUI.Container("WaveformContainer")
                    {
                        isHiddenCallback = () => !data.monitorsDebugSettings.waveformToggle,
                        children =
                        {
                            new DebugUI.FloatField
                            {
                                nameAndTooltip = RenderingStrings.WaveformExposure,
                                getter = ()    =>   data.monitorsDebugSettings.waveformExposure,
                                setter = value => { data.monitorsDebugSettings.waveformExposure = value; },
                                min    = ()    => 0f
                            },
                            new DebugUI.BoolField
                            {
                                nameAndTooltip = RenderingStrings.WaveformParade,
                                getter = ()    =>   data.monitorsDebugSettings.waveformParade,
                                setter = value => { data.monitorsDebugSettings.waveformParade = value; }
                            }
                        }
                    },
                    new DebugUI.BoolField
                    {
                        nameAndTooltip = RenderingStrings.VectorscopeToggle,
                        getter = ()    =>   data.monitorsDebugSettings.vectorscopeToggle,
                        setter = value => { data.monitorsDebugSettings.vectorscopeToggle = value; }
                    },
                    new DebugUI.Container("VectorscopeContainer")
                    {
                        isHiddenCallback = () => !data.monitorsDebugSettings.vectorscopeToggle,
                        children =
                        {
                            new DebugUI.FloatField
                            {
                                nameAndTooltip = RenderingStrings.VectorscopeExposure,
                                getter         = () => data.monitorsDebugSettings.vectorscopeExposure,
                                setter         = value => { data.monitorsDebugSettings.vectorscopeExposure = value; },
                                min            = ()    => 0f
                            }
                        }
                    },
                    new DebugUI.FloatField
                    {
                        nameAndTooltip = RenderingStrings.MonitorsSize,
                        getter = ()    =>   data.monitorsDebugSettings.monitorsSize,
                        setter = value => { data.monitorsDebugSettings.monitorsSize = value; },
                        min    = ()    => 0.1f,
                        max    = ()    => 0.8f
                    }
                }
            });

#if ENABLE_NVIDIA && ENABLE_NVIDIA_MODULE
            widgetList.Add(nvidiaDebugView.CreateWidget());
#endif

            m_DebugRenderingItems = widgetList.ToArray();
            var panel = DebugManager.instance.GetPanel(k_PanelRendering, true);
            panel.children.Add(m_DebugRenderingItems);

            var renderGraphs = RenderGraph.GetRegisteredRenderGraphs();
            foreach (var graph in renderGraphs)
                graph.RegisterDebug(panel);
        }

        void UnregisterRenderingDebug()
        {
            UnregisterDebugItems(k_PanelRendering, m_DebugRenderingItems);
        }

        static class DecalStrings
        {
            public static readonly NameAndTooltip DisplayAtlas = new() { name = "Display Atlas", tooltip = "Enable the checkbox to debug and display the decal atlas for a Camera in the top left of that Camera's view." };
            public static readonly NameAndTooltip MipLevel = new() { name = "Mip Level", tooltip = "Use the slider to select the mip level for the decal atlas." };
        }

        void RegisterDecalsDebug()
        {
            var decalAffectingTransparent = new DebugUI.Container()
            {
                displayName = "Decals Affecting Transparent Objects",
                children =
                {
                    new DebugUI.BoolField { nameAndTooltip = DecalStrings.DisplayAtlas, getter = () => data.decalsDebugSettings.displayAtlas, setter = value => data.decalsDebugSettings.displayAtlas = value},
                    new DebugUI.UIntField { nameAndTooltip = DecalStrings.MipLevel, getter = () => data.decalsDebugSettings.mipLevel, setter = value => data.decalsDebugSettings.mipLevel = value, min = () => 0u, max = () => (uint)(RenderPipelineManager.currentPipeline as HDRenderPipeline)?.GetDecalAtlasMipCount() }
                }
            };

            m_DebugDecalsItems = new DebugUI.Widget[]
            {
                CreateMissingDebugShadersWarning(),
                decalAffectingTransparent
            };

            var panel = DebugManager.instance.GetPanel(k_PanelDecals, true);
            panel.children.Add(m_DebugDecalsItems);
        }

        internal void RegisterDebug()
        {
            RegisterDecalsDebug();
            RegisterDisplayStatsDebug();
            RegisterMaterialDebug();
            RegisterLightingDebug();
            RegisterRenderingDebug();
            DebugManager.instance.RegisterData(this);
        }

        internal void UnregisterDebug()
        {
            UnregisterDebugItems(k_PanelDecals, m_DebugDecalsItems);
            UnregisterDisplayStatsDebug();
            UnregisterDebugItems(k_PanelMaterials, m_DebugMaterialItems);
            UnregisterDebugItems(k_PanelLighting, m_DebugLightingItems);
            UnregisterRenderingDebug();
            DebugManager.instance.UnregisterData(this);
        }

        void UnregisterDebugItems(string panelName, DebugUI.Widget[] items)
        {
            var panel = DebugManager.instance.GetPanel(panelName);
            if (panel != null)
                panel.children.Remove(items);
        }

        void FillFullScreenDebugEnum(ref GUIContent[] strings, ref int[] values, FullScreenDebugMode min, FullScreenDebugMode max)
        {
            int count = max - min - 1;
            strings = new GUIContent[count + 1];
            values = new int[count + 1];
            strings[0] = new GUIContent(FullScreenDebugMode.None.ToString());
            values[0] = (int)FullScreenDebugMode.None;
            int index = 1;
            for (int i = (int)min + 1; i < (int)max; ++i)
            {
                strings[index] = new GUIContent(((FullScreenDebugMode)i).ToString());
                values[index] = i;
                index++;
            }
        }

        internal static void RegisterCamera(IFrameSettingsHistoryContainer container)
        {
            string name = container.panelName;
            if (s_CameraNames.FindIndex(x => x.text.Equals(name)) < 0)
            {
                s_CameraNames.Add(new GUIContent(name));
                needsRefreshingCameraFreezeList = true;
            }

            if (!FrameSettingsHistory.IsRegistered(container))
            {
                var history = FrameSettingsHistory.RegisterDebug(container);
                DebugManager.instance.RegisterData(history);
            }
        }

        internal static void UnRegisterCamera(IFrameSettingsHistoryContainer container)
        {
            string name = container.panelName;
            int indexOfCamera = s_CameraNames.FindIndex(x => x.text.Equals(name));
            if (indexOfCamera > 0)
            {
                s_CameraNames.RemoveAt(indexOfCamera);
                needsRefreshingCameraFreezeList = true;
            }

            if (FrameSettingsHistory.IsRegistered(container))
            {
                DebugManager.instance.UnregisterData(container);
                FrameSettingsHistory.UnRegisterDebug(container);
            }
        }

        internal bool IsDebugDisplayRemovePostprocess()
        {
            return data.materialDebugSettings.IsDebugDisplayEnabled() || data.lightingDebugSettings.IsDebugDisplayRemovePostprocess() || data.mipMapDebugSettings.IsDebugDisplayEnabled();
        }

        internal void UpdateMaterials()
        {
            if (data.mipMapDebugSettings.debugMipMapMode != 0)
                Texture.SetStreamingTextureMaterialDebugProperties();
        }

        internal void UpdateCameraFreezeOptions()
        {
            if (needsRefreshingCameraFreezeList)
            {
                s_CameraNames.Insert(0, new GUIContent("None"));

                s_CameraNamesStrings = s_CameraNames.ToArray();
                s_CameraNamesValues = Enumerable.Range(0, s_CameraNames.Count()).ToArray();

                UnregisterRenderingDebug();
                RegisterRenderingDebug();
                needsRefreshingCameraFreezeList = false;
            }
        }

        internal bool DebugHideSky(HDCamera hdCamera)
        {
            return (IsMatcapViewEnabled(hdCamera) ||
                GetDebugLightingMode() == DebugLightingMode.DiffuseLighting ||
                GetDebugLightingMode() == DebugLightingMode.SpecularLighting ||
                GetDebugLightingMode() == DebugLightingMode.DirectDiffuseLighting ||
                GetDebugLightingMode() == DebugLightingMode.DirectSpecularLighting ||
                GetDebugLightingMode() == DebugLightingMode.IndirectDiffuseLighting ||
                GetDebugLightingMode() == DebugLightingMode.ReflectionLighting ||
                GetDebugLightingMode() == DebugLightingMode.RefractionLighting ||
                GetDebugLightingMode() == DebugLightingMode.ProbeVolumeSampledSubdivision ||
                GetDebugMipMapMode() != DebugMipMapMode.None
            );
        }

        internal bool DebugNeedsExposure()
        {
            DebugLightingMode debugLighting = data.lightingDebugSettings.debugLightingMode;
            DebugViewGbuffer debugGBuffer = (DebugViewGbuffer)data.materialDebugSettings.debugViewGBuffer;
            return (debugLighting == DebugLightingMode.DirectDiffuseLighting || debugLighting == DebugLightingMode.DirectSpecularLighting || debugLighting == DebugLightingMode.IndirectDiffuseLighting || debugLighting == DebugLightingMode.ReflectionLighting || debugLighting == DebugLightingMode.RefractionLighting || debugLighting == DebugLightingMode.EmissiveLighting ||
                debugLighting == DebugLightingMode.DiffuseLighting || debugLighting == DebugLightingMode.SpecularLighting || debugLighting == DebugLightingMode.VisualizeCascade || debugLighting == DebugLightingMode.ProbeVolumeSampledSubdivision) ||
                (data.lightingDebugSettings.overrideAlbedo || data.lightingDebugSettings.overrideNormal || data.lightingDebugSettings.overrideSmoothness || data.lightingDebugSettings.overrideSpecularColor || data.lightingDebugSettings.overrideEmissiveColor || data.lightingDebugSettings.overrideAmbientOcclusion) ||
                (debugGBuffer == DebugViewGbuffer.BakeDiffuseLightingWithAlbedoPlusEmissive) || (data.lightingDebugSettings.debugLightFilterMode != DebugLightFilterMode.None) ||
                (data.fullScreenDebugMode == FullScreenDebugMode.PreRefractionColorPyramid || data.fullScreenDebugMode == FullScreenDebugMode.FinalColorPyramid || data.fullScreenDebugMode == FullScreenDebugMode.VolumetricClouds ||
                    data.fullScreenDebugMode == FullScreenDebugMode.TransparentScreenSpaceReflections || data.fullScreenDebugMode == FullScreenDebugMode.ScreenSpaceReflections || data.fullScreenDebugMode == FullScreenDebugMode.ScreenSpaceReflectionsPrev || data.fullScreenDebugMode == FullScreenDebugMode.ScreenSpaceReflectionsAccum || data.fullScreenDebugMode == FullScreenDebugMode.ScreenSpaceReflectionSpeedRejection ||
                    data.fullScreenDebugMode == FullScreenDebugMode.LightCluster || data.fullScreenDebugMode == FullScreenDebugMode.ScreenSpaceShadows || data.fullScreenDebugMode == FullScreenDebugMode.NanTracker || data.fullScreenDebugMode == FullScreenDebugMode.ColorLog) || data.fullScreenDebugMode == FullScreenDebugMode.ScreenSpaceGlobalIllumination;
        }
    }
}
