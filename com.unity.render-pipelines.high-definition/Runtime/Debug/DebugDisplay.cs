using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine.Rendering.HighDefinition.Attributes;

namespace UnityEngine.Rendering.HighDefinition
{
    [GenerateHLSL(needAccessors = false, generateCBuffer = true)]
    unsafe struct ShaderVariablesDebugDisplay
    {
        [HLSLArray(32, typeof(Vector4))]
        public fixed float _DebugRenderingLayersColors[32 * 4];
        [HLSLArray(11, typeof(ShaderGenUInt4))]
        public fixed uint _DebugViewMaterialArray[11 * 4]; // Contain the id (define in various materialXXX.cs.hlsl) of the property to display

        public int _DebugLightingMode; // Match enum DebugLightingMode
        public int _DebugLightLayersMask;
        public int _DebugShadowMapMode;
        public int _DebugMipMapMode; // Match enum DebugMipMapMode

        public int _DebugFullScreenMode;
        public float _DebugTransparencyOverdrawWeight;
        public int _DebugMipMapModeTerrainTexture; // Match enum DebugMipMapModeTerrainTexture
        public int _ColorPickerMode; // Match enum ColorPickerDebugMode

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

        public int _DebugProbeVolumeMode;
        public Vector3 _DebugDisplayPad0;
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
        /// <summary>Maximum Full Screen Lighting debug mode value (used internally).</summary>
        MaxLightingFullScreenDebug,

        // Rendering
        /// <summary>Minimum Full Screen Rendering debug mode value (used internally).</summary>
        MinRenderingFullScreenDebug,
        /// <summary>Display Motion Vectors.</summary>
        MotionVectors,
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
        /// <summary>Display Vertex Density.</summary>
        VertexDensity,
        /// <summary>Display Requested Virtual Texturing tiles, colored by the mip</summary>
        RequestedVirtualTextureTiles,
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
        // TODO: Move before count for 11.0
        /// <summary>Display Screen Space Reflections buffer of the previous frame accumulated.</summary>
        ScreenSpaceReflectionsPrev,
        /// <summary>Display Screen Space Reflections buffer of the current frame hit.</summary>
        ScreenSpaceReflectionsAccum
    }

    /// <summary>
    /// Class managing debug display in HDRP.
    /// </summary>
    public class DebugDisplaySettings : IDebugData
    {
        static string k_PanelDisplayStats = "Display Stats";
        static string k_PanelMaterials = "Material";
        static string k_PanelLighting = "Lighting";
        static string k_PanelVolume = "Volume";
        static string k_PanelRendering = "Rendering";
        static string k_PanelDecals = "Decals";

        DebugUI.Widget[] m_DebugDisplayStatsItems;
        DebugUI.Widget[] m_DebugMaterialItems;
        DebugUI.Widget[] m_DebugLightingItems;
        DebugUI.Widget[] m_DebugVolumeItems;
        DebugUI.Widget[] m_DebugRenderingItems;
        DebugUI.Widget[] m_DebugDecalsAffectingTransparentItems;

        static GUIContent[] s_LightingFullScreenDebugStrings = null;
        static int[] s_LightingFullScreenDebugValues = null;
        static GUIContent[] s_RenderingFullScreenDebugStrings = null;
        static int[] s_RenderingFullScreenDebugValues = null;
        static GUIContent[] s_MaterialFullScreenDebugStrings = null;
        static int[] s_MaterialFullScreenDebugValues = null;
        static GUIContent[] s_MsaaSamplesDebugStrings = null;
        static int[] s_MsaaSamplesDebugValues = null;
        static GUIContent[] s_TileAndClusterDebugStrings = null;
        static int[] s_TileAndClusterDebugValues = null;

        static List<GUIContent> s_CameraNames = new List<GUIContent>();
        static GUIContent[] s_CameraNamesStrings = null;
        static int[] s_CameraNamesValues = null;

        static bool needsRefreshingCameraFreezeList = true;

        List<ProfilingSampler> m_RecordedSamplers = new List<ProfilingSampler>();

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
        Dictionary<string, AccumulatedTiming> m_AccumulatedGPUTiming = new Dictionary<string, AccumulatedTiming>();
        Dictionary<string, AccumulatedTiming> m_AccumulatedCPUTiming = new Dictionary<string, AccumulatedTiming>();
        Dictionary<string, AccumulatedTiming> m_AccumulatedInlineCPUTiming = new Dictionary<string, AccumulatedTiming>();
        float m_TimeSinceLastAvgValue = 0.0f;
        int m_AccumulatedFrames = 0;
        const float k_AccumulationTimeInSeconds = 1.0f;

        List<ProfilingSampler> m_RecordedSamplersRT = new List<ProfilingSampler>();
        enum DebugProfilingType
        {
            CPU,
            GPU,
            InlineCPU
        }

        /// <summary>
        /// Debug data.
        /// </summary>
        public class DebugData
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
            /// <summary>Current false color debug settings.</summary>
            public FalseColorDebugSettings falseColorDebugSettings = new FalseColorDebugSettings();
            /// <summary>Current decals debug settings.</summary>
            public DecalsDebugSettings decalsDebugSettings = new DecalsDebugSettings();
            /// <summary>Current transparency debug settings.</summary>
            public TransparencyDebugSettings transparencyDebugSettings = new TransparencyDebugSettings();
            /// <summary>Current volume debug settings.</summary>
            public VolumeDebugSettings volumeDebugSettings = new VolumeDebugSettings();
            /// <summary>Current number of samples for MSAA textures.</summary>
            public MSAASamples msaaSamples = MSAASamples.None;
            /// <summary>Index of screen space shadow to display.</summary>
            public uint screenSpaceShadowIndex = 0;
            /// <summary>Max quad cost for quad overdraw display.</summary>
            public uint maxQuadCost = 5;
            /// <summary>Max vertex density for vertex density display.</summary>
            public uint maxVertexDensity = 10;
            /// <summary>Display ray tracing ray count per frame.</summary>
            public bool countRays = false;

            /// <summary>Index of the camera to freeze for visibility.</summary>
            public int debugCameraToFreeze = 0;

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
            internal int msaaSampleDebugModeEnumIndex;
            internal int debugCameraToFreezeEnumIndex;
            internal int volumeComponentEnumIndex;
            internal int volumeCameraEnumIndex;
            internal int probeVolumeDebugModeEnumIndex;
            internal int probeVolumeAtlasSliceModeEnumIndex;

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

        internal DebugDisplaySettings()
        {
            FillFullScreenDebugEnum(ref s_LightingFullScreenDebugStrings, ref s_LightingFullScreenDebugValues, FullScreenDebugMode.MinLightingFullScreenDebug, FullScreenDebugMode.MaxLightingFullScreenDebug);
            FillFullScreenDebugEnum(ref s_RenderingFullScreenDebugStrings, ref s_RenderingFullScreenDebugValues, FullScreenDebugMode.MinRenderingFullScreenDebug, FullScreenDebugMode.MaxRenderingFullScreenDebug);
            FillFullScreenDebugEnum(ref s_MaterialFullScreenDebugStrings, ref s_MaterialFullScreenDebugValues, FullScreenDebugMode.MinMaterialFullScreenDebug, FullScreenDebugMode.MaxMaterialFullScreenDebug);

            var device = SystemInfo.graphicsDeviceType;
            if (device == GraphicsDeviceType.Metal)
            {
                s_RenderingFullScreenDebugStrings = s_RenderingFullScreenDebugStrings.Where((val, idx) => (idx + FullScreenDebugMode.MinRenderingFullScreenDebug) != FullScreenDebugMode.VertexDensity).ToArray();
                s_RenderingFullScreenDebugValues = s_RenderingFullScreenDebugValues.Where((val, idx) => (idx + FullScreenDebugMode.MinRenderingFullScreenDebug) != FullScreenDebugMode.VertexDensity).ToArray();
            }
            if (device == GraphicsDeviceType.Metal || device == GraphicsDeviceType.PlayStation4 || device == GraphicsDeviceType.PlayStation5)
            {
                s_RenderingFullScreenDebugStrings = s_RenderingFullScreenDebugStrings.Where((val, idx) => (idx + FullScreenDebugMode.MinRenderingFullScreenDebug) != FullScreenDebugMode.QuadOverdraw).ToArray();
                s_RenderingFullScreenDebugValues = s_RenderingFullScreenDebugValues.Where((val, idx) => (idx + FullScreenDebugMode.MinRenderingFullScreenDebug) != FullScreenDebugMode.QuadOverdraw).ToArray();
            }

            FillTileClusterDebugEnum();

            s_MaterialFullScreenDebugStrings[(int)FullScreenDebugMode.ValidateDiffuseColor - ((int)FullScreenDebugMode.MinMaterialFullScreenDebug)] = new GUIContent("Diffuse Color");
            s_MaterialFullScreenDebugStrings[(int)FullScreenDebugMode.ValidateSpecularColor - ((int)FullScreenDebugMode.MinMaterialFullScreenDebug)] = new GUIContent("Metal or SpecularColor");

            s_MsaaSamplesDebugStrings = Enum.GetNames(typeof(MSAASamples))
                .Select(t => new GUIContent(t))
                .ToArray();
            s_MsaaSamplesDebugValues = (int[])Enum.GetValues(typeof(MSAASamples));

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
        /// Returns the current Probe Volume Debug Mode.
        /// </summary>
        /// <returns>Current Probe Volume Debug Mode.</returns>
        internal ProbeVolumeDebugMode GetProbeVolumeDebugMode()
        {
            return data.lightingDebugSettings.probeVolumeDebugMode;
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
        /// Set the current Probe Volume Debug Mode.
        /// </summary>
        /// <param name="value">Desired Probe Volume Debug Mode.</param>
        internal void SetProbeVolumeDebugMode(ProbeVolumeDebugMode value)
        {
            data.lightingDebugSettings.probeVolumeDebugMode = value;
        }

        /// <summary>
        /// Set the current Probe Volume Atlas Mode.
        /// </summary>
        /// <param name="value">Desired Probe Volume Atlas Mode.</param>
        internal void SetProbeVolumeAtlasSliceMode(ProbeVolumeAtlasSliceMode value)
        {
            data.lightingDebugSettings.probeVolumeAtlasSliceMode = value;
        }

        /// <summary>
        /// Set the current Exposure Debug Mode.
        /// </summary>
        /// <param name="value">Desired Probe Volume Debug Mode.</param>
        internal void SetExposureDebugMode(ExposureDebugMode value)
        {
            data.lightingDebugSettings.exposureDebugMode = value;
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

            m_RecordedSamplers.Add(ProfilingSampler.Get(HDProfileId.HDRenderPipelineAllRenderRequest));
            m_RecordedSamplers.Add(ProfilingSampler.Get(HDProfileId.VolumeUpdate));
            m_RecordedSamplers.Add(ProfilingSampler.Get(HDProfileId.RenderShadowMaps));
            m_RecordedSamplers.Add(ProfilingSampler.Get(HDProfileId.GBuffer));
            m_RecordedSamplers.Add(ProfilingSampler.Get(HDProfileId.PrepareLightsForGPU));
            m_RecordedSamplers.Add(ProfilingSampler.Get(HDProfileId.VolumeVoxelization));
            m_RecordedSamplers.Add(ProfilingSampler.Get(HDProfileId.VolumetricLighting));
            m_RecordedSamplers.Add(ProfilingSampler.Get(HDProfileId.RenderDeferredLightingCompute));
            m_RecordedSamplers.Add(ProfilingSampler.Get(HDProfileId.ForwardOpaque));
            m_RecordedSamplers.Add(ProfilingSampler.Get(HDProfileId.ForwardTransparent));
            m_RecordedSamplers.Add(ProfilingSampler.Get(HDProfileId.ForwardPreRefraction));
            m_RecordedSamplers.Add(ProfilingSampler.Get(HDProfileId.ColorPyramid));
            m_RecordedSamplers.Add(ProfilingSampler.Get(HDProfileId.DepthPyramid));
            m_RecordedSamplers.Add(ProfilingSampler.Get(HDProfileId.PostProcessing));
        }

        void DisableProfilingRecorders()
        {
            foreach (var sampler in m_RecordedSamplers)
            {
                sampler.enableRecording = false;
            }

            m_RecordedSamplers.Clear();
        }

        void EnableProfilingRecordersRT()
        {
            Debug.Assert(m_RecordedSamplersRT.Count == 0);

            m_RecordedSamplersRT.Add(ProfilingSampler.Get(HDProfileId.RaytracingBuildCluster));
            m_RecordedSamplersRT.Add(ProfilingSampler.Get(HDProfileId.RaytracingCullLights));

            // Ray Traced Reflections
            m_RecordedSamplersRT.Add(ProfilingSampler.Get(HDProfileId.RaytracingReflectionDirectionGeneration));
            m_RecordedSamplersRT.Add(ProfilingSampler.Get(HDProfileId.RaytracingReflectionEvaluation));
            m_RecordedSamplersRT.Add(ProfilingSampler.Get(HDProfileId.RaytracingReflectionAdjustWeight));
            m_RecordedSamplersRT.Add(ProfilingSampler.Get(HDProfileId.RaytracingReflectionUpscale));
            m_RecordedSamplersRT.Add(ProfilingSampler.Get(HDProfileId.RaytracingReflectionFilter));

            // Ray Traced Ambient Occlusion
            m_RecordedSamplersRT.Add(ProfilingSampler.Get(HDProfileId.RaytracingAmbientOcclusion));
            m_RecordedSamplersRT.Add(ProfilingSampler.Get(HDProfileId.RaytracingFilterAmbientOcclusion));

            // Ray Traced Shadows
            m_RecordedSamplersRT.Add(ProfilingSampler.Get(HDProfileId.RaytracingDirectionalLightShadow));
            m_RecordedSamplersRT.Add(ProfilingSampler.Get(HDProfileId.RaytracingLightShadow));

            // Ray Traced Indirect Diffuse
            m_RecordedSamplersRT.Add(ProfilingSampler.Get(HDProfileId.RaytracingIndirectDiffuseDirectionGeneration));
            m_RecordedSamplersRT.Add(ProfilingSampler.Get(HDProfileId.RaytracingIndirectDiffuseEvaluation));
            m_RecordedSamplersRT.Add(ProfilingSampler.Get(HDProfileId.RaytracingIndirectDiffuseUpscale));
            m_RecordedSamplersRT.Add(ProfilingSampler.Get(HDProfileId.RaytracingFilterIndirectDiffuse));

            m_RecordedSamplersRT.Add(ProfilingSampler.Get(HDProfileId.RaytracingDebugOverlay));
            m_RecordedSamplersRT.Add(ProfilingSampler.Get(HDProfileId.ForwardPreRefraction));
            m_RecordedSamplersRT.Add(ProfilingSampler.Get(HDProfileId.RayTracingRecursiveRendering));
            m_RecordedSamplersRT.Add(ProfilingSampler.Get(HDProfileId.RayTracingPrepass));
            m_RecordedSamplersRT.Add(ProfilingSampler.Get(HDProfileId.RaytracingDeferredLighting));
        }

        void DisableProfilingRecordersRT()
        {
            foreach (var sampler in m_RecordedSamplersRT)
            {
                sampler.enableRecording = false;
            }

            m_RecordedSamplersRT.Clear();
        }

        float GetSamplerTiming(DebugProfilingType type, ProfilingSampler sampler)
        {
            if (data.averageProfilerTimingsOverASecond)
            {
                // Find the right accumulated dictionary
                var accumulatedDictionary = type == DebugProfilingType.CPU ? m_AccumulatedCPUTiming :
                    type == DebugProfilingType.InlineCPU ? m_AccumulatedInlineCPUTiming :
                    m_AccumulatedGPUTiming;

                AccumulatedTiming accTiming = null;
                if (accumulatedDictionary.TryGetValue(sampler.name, out accTiming))
                    return accTiming.lastAverage;
            }
            else
            {
                return (type == DebugProfilingType.CPU) ? sampler.cpuElapsedTime : ((type == DebugProfilingType.GPU) ? sampler.gpuElapsedTime : sampler.inlineCpuElapsedTime);
            }

            return 0.0f;
        }

        ObservableList<DebugUI.Widget> BuildProfilingSamplerList(DebugProfilingType type)
        {
            var result = new ObservableList<DebugUI.Widget>();

            // Find the right accumulated dictionary and add it there if not existing yet.
            var accumulatedDictionary = type == DebugProfilingType.CPU ? m_AccumulatedCPUTiming :
                type == DebugProfilingType.InlineCPU ? m_AccumulatedInlineCPUTiming :
                m_AccumulatedGPUTiming;


            foreach (var sampler in m_RecordedSamplers)
            {
                sampler.enableRecording = true;
                if (!accumulatedDictionary.ContainsKey(sampler.name))
                {
                    accumulatedDictionary.Add(sampler.name, new AccumulatedTiming());
                }

                result.Add(new DebugUI.Value
                {
                    displayName = sampler.name,
                    getter = () => string.Format("{0:F2}", GetSamplerTiming(type, sampler)),
                    refreshRate = 1.0f / 5.0f
                });
            }

            return result;
        }

        ObservableList<DebugUI.Widget> BuildProfilingSamplerListRT(DebugProfilingType type)
        {
            var result = new ObservableList<DebugUI.Widget>();

            // Find the right accumulated dictionary and add it there if not existing yet.
            var accumulatedDictionary = type == DebugProfilingType.CPU ? m_AccumulatedCPUTiming :
                type == DebugProfilingType.InlineCPU ? m_AccumulatedInlineCPUTiming :
                m_AccumulatedGPUTiming;


            foreach (var sampler in m_RecordedSamplersRT)
            {
                sampler.enableRecording = true;
                if (!accumulatedDictionary.ContainsKey(sampler.name))
                {
                    accumulatedDictionary.Add(sampler.name, new AccumulatedTiming());
                }

                result.Add(new DebugUI.Value
                {
                    displayName = sampler.name,
                    getter = () => string.Format("{0:F2}", GetSamplerTiming(type, sampler)),
                    refreshRate = 1.0f / 5.0f
                });
            }

            return result;
        }

        void UpdateListOfAveragedProfilerTimings(List<ProfilingSampler> samplers, bool needUpdatingAverages)
        {
            foreach (var sampler in samplers)
            {
                // Accumulate.
                AccumulatedTiming accCPUTiming = null;
                if (m_AccumulatedCPUTiming.TryGetValue(sampler.name, out accCPUTiming))
                    accCPUTiming.accumulatedValue += sampler.cpuElapsedTime;

                AccumulatedTiming accInlineCPUTiming = null;
                if (m_AccumulatedInlineCPUTiming.TryGetValue(sampler.name, out accInlineCPUTiming))
                    accInlineCPUTiming.accumulatedValue += sampler.inlineCpuElapsedTime;

                AccumulatedTiming accGPUTiming = null;
                if (m_AccumulatedGPUTiming.TryGetValue(sampler.name, out accGPUTiming))
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
            list.Add(new DebugUI.Value { displayName = "Frame Rate (fps)", getter = () => 1f / Time.smoothDeltaTime, refreshRate = 1f / 5f });
            list.Add(new DebugUI.Value { displayName = "Frame Time (ms)", getter = () => Time.smoothDeltaTime * 1000f, refreshRate = 1f / 5f });


            EnableProfilingRecorders();
            list.Add(new DebugUI.BoolField { displayName = "Update every second with average", getter = () => data.averageProfilerTimingsOverASecond, setter = value => data.averageProfilerTimingsOverASecond = value });
            list.Add(new DebugUI.Foldout("CPU timings (Command Buffers)", BuildProfilingSamplerList(DebugProfilingType.CPU)));
            list.Add(new DebugUI.Foldout("GPU timings", BuildProfilingSamplerList(DebugProfilingType.GPU)));
            if (HDRenderPipeline.currentAsset?.currentPlatformRenderPipelineSettings.supportRayTracing ?? true)
            {
                EnableProfilingRecordersRT();
                list.Add(new DebugUI.Foldout("CPU timings RT (Command Buffers)", BuildProfilingSamplerListRT(DebugProfilingType.CPU)));
                list.Add(new DebugUI.Foldout("GPU timings RT", BuildProfilingSamplerListRT(DebugProfilingType.GPU)));
            }
            list.Add(new DebugUI.Foldout("Inline CPU timings", BuildProfilingSamplerList(DebugProfilingType.InlineCPU)));
            list.Add(new DebugUI.BoolField { displayName = "Count Rays (MRays/Frame)", getter = () => data.countRays, setter = value => data.countRays = value, onValueChanged = RefreshDisplayStatsDebug });
            if (data.countRays)
            {
                list.Add(new DebugUI.Container
                {
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

#if DEVELOPMENT_BUILD || UNITY_EDITOR
            list.Add(new DebugUI.BoolField { displayName = "Debug XR Layout", getter = () => XRSystem.dumpDebugInfo, setter = value => XRSystem.dumpDebugInfo = value, onValueChanged = RefreshDisplayStatsDebug });
            if (XRSystem.dumpDebugInfo)
            {
                Func<object> Bind<T>(Func<T, object> func, T arg) => () => func(arg);

                for (int i = 0; i < XRSystem.passDebugInfos.Count; i++)
                    list.Add(new DebugUI.Value { displayName = "", getter = Bind(XRSystem.ReadPassDebugInfo, i) });
            }
#endif

            m_DebugDisplayStatsItems = list.ToArray();
            var panel = DebugManager.instance.GetPanel(k_PanelDisplayStats, true);
            panel.flags = DebugUI.Flags.RuntimeOnly;
            panel.children.Add(m_DebugDisplayStatsItems);
        }

        void RegisterMaterialDebug()
        {
            var list = new List<DebugUI.Widget>();

            list.Add(new DebugUI.EnumField { displayName = "Common Material Properties", getter = () => (int)data.materialDebugSettings.debugViewMaterialCommonValue, setter = value => SetDebugViewCommonMaterialProperty((MaterialSharedProperty)value), autoEnum = typeof(MaterialSharedProperty), getIndex = () => (int)data.materialDebugSettings.debugViewMaterialCommonValue, setIndex = value => { data.ResetExclusiveEnumIndices(); data.materialDebugSettings.debugViewMaterialCommonValue = (MaterialSharedProperty)value; } });
            list.Add(new DebugUI.EnumField { displayName = "Material", getter = () => (data.materialDebugSettings.debugViewMaterial[0]) == 0 ? 0 : data.materialDebugSettings.debugViewMaterial[1], setter = value => SetDebugViewMaterial(value), enumNames = MaterialDebugSettings.debugViewMaterialStrings, enumValues = MaterialDebugSettings.debugViewMaterialValues, getIndex = () => data.materialDebugSettings.materialEnumIndex, setIndex = value => { data.ResetExclusiveEnumIndices(); data.materialDebugSettings.materialEnumIndex = value; } });
            list.Add(new DebugUI.EnumField { displayName = "Engine", getter = () => data.materialDebugSettings.debugViewEngine, setter = value => SetDebugViewEngine(value), enumNames = MaterialDebugSettings.debugViewEngineStrings, enumValues = MaterialDebugSettings.debugViewEngineValues, getIndex = () => data.engineEnumIndex, setIndex = value => { data.ResetExclusiveEnumIndices(); data.engineEnumIndex = value; } });
            list.Add(new DebugUI.EnumField { displayName = "Attributes", getter = () => (int)data.materialDebugSettings.debugViewVarying, setter = value => SetDebugViewVarying((DebugViewVarying)value), autoEnum = typeof(DebugViewVarying), getIndex = () => data.attributesEnumIndex, setIndex = value => { data.ResetExclusiveEnumIndices(); data.attributesEnumIndex = value; } });
            list.Add(new DebugUI.EnumField { displayName = "Properties", getter = () => (int)data.materialDebugSettings.debugViewProperties, setter = value => SetDebugViewProperties((DebugViewProperties)value), autoEnum = typeof(DebugViewProperties), getIndex = () => data.propertiesEnumIndex, setIndex = value => { data.ResetExclusiveEnumIndices(); data.propertiesEnumIndex = value; } });
            list.Add(new DebugUI.EnumField { displayName = "GBuffer", getter = () => data.materialDebugSettings.debugViewGBuffer, setter = value => SetDebugViewGBuffer(value), enumNames = MaterialDebugSettings.debugViewMaterialGBufferStrings, enumValues = MaterialDebugSettings.debugViewMaterialGBufferValues, getIndex = () => data.gBufferEnumIndex, setIndex = value => { data.ResetExclusiveEnumIndices(); data.gBufferEnumIndex = value; } });
            list.Add(new DebugUI.EnumField { displayName = "Material Validator", getter = () => (int)data.fullScreenDebugMode, setter = value => SetFullScreenDebugMode((FullScreenDebugMode)value), enumNames = s_MaterialFullScreenDebugStrings, enumValues = s_MaterialFullScreenDebugValues, onValueChanged = RefreshMaterialDebug, getIndex = () => data.materialValidatorDebugModeEnumIndex, setIndex = value => { data.ResetExclusiveEnumIndices(); data.materialValidatorDebugModeEnumIndex = value; } });

            if (data.fullScreenDebugMode == FullScreenDebugMode.ValidateDiffuseColor || data.fullScreenDebugMode == FullScreenDebugMode.ValidateSpecularColor)
            {
                list.Add(new DebugUI.Container
                {
                    children =
                    {
                        new DebugUI.ColorField { displayName = "Too High Color", getter = () => data.materialDebugSettings.materialValidateHighColor, setter = value => data.materialDebugSettings.materialValidateHighColor = value, showAlpha = false, hdr = true },
                        new DebugUI.ColorField { displayName = "Too Low Color", getter = () => data.materialDebugSettings.materialValidateLowColor, setter = value => data.materialDebugSettings.materialValidateLowColor = value, showAlpha = false, hdr = true },
                        new DebugUI.ColorField { displayName = "Not A Pure Metal Color", getter = () => data.materialDebugSettings.materialValidateTrueMetalColor, setter = value => data.materialDebugSettings.materialValidateTrueMetalColor = value, showAlpha = false, hdr = true },
                        new DebugUI.BoolField  { displayName = "Pure Metals", getter = () => data.materialDebugSettings.materialValidateTrueMetal, setter = (v) => data.materialDebugSettings.materialValidateTrueMetal = v },
                    }
                });
            }

            m_DebugMaterialItems = list.ToArray();
            var panel = DebugManager.instance.GetPanel(k_PanelMaterials, true);
            panel.children.Add(m_DebugMaterialItems);
        }

        void RefreshDisplayStatsDebug<T>(DebugUI.Field<T> field, T value)
        {
            UnregisterDebugItems(k_PanelDisplayStats, m_DebugDisplayStatsItems);
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
            UnregisterDebugItems(k_PanelDecals, m_DebugDecalsAffectingTransparentItems);
            RegisterDecalsDebug();
        }

        void RefreshRenderingDebug<T>(DebugUI.Field<T> field, T value)
        {
            // Explicitly invoke the render debug unregister to handle render graph items.
            UnregisterDebugItems(k_PanelRendering, m_DebugRenderingItems);
            RegisterRenderingDebug();
        }

        void RefreshMaterialDebug<T>(DebugUI.Field<T> field, T value)
        {
            UnregisterDebugItems(k_PanelMaterials, m_DebugMaterialItems);
            RegisterMaterialDebug();
        }

        void RefreshVolumeDebug<T>(DebugUI.Field<T> field, T value)
        {
            UnregisterDebugItems(k_PanelVolume, m_DebugVolumeItems);
            RegisterVolumeDebug();
        }

        void RegisterLightingDebug()
        {
            var list = new List<DebugUI.Widget>();

            {
                var shadows = new DebugUI.Container() { displayName = "Shadows" };

                shadows.children.Add(new DebugUI.EnumField { displayName = "Debug Mode", getter = () => (int)data.lightingDebugSettings.shadowDebugMode, setter = value => SetShadowDebugMode((ShadowMapDebugMode)value), autoEnum = typeof(ShadowMapDebugMode), onValueChanged = RefreshLightingDebug, getIndex = () => data.shadowDebugModeEnumIndex, setIndex = value => data.shadowDebugModeEnumIndex = value });

                if (data.lightingDebugSettings.shadowDebugMode == ShadowMapDebugMode.VisualizeShadowMap || data.lightingDebugSettings.shadowDebugMode == ShadowMapDebugMode.SingleShadow)
                {
                    var container = new DebugUI.Container();
                    container.children.Add(new DebugUI.BoolField { displayName = "Use Selection", getter = () => data.lightingDebugSettings.shadowDebugUseSelection, setter = value => data.lightingDebugSettings.shadowDebugUseSelection = value, flags = DebugUI.Flags.EditorOnly, onValueChanged = RefreshLightingDebug });

                    if (!data.lightingDebugSettings.shadowDebugUseSelection)
                        container.children.Add(new DebugUI.UIntField { displayName = "Shadow Map Index", getter = () => data.lightingDebugSettings.shadowMapIndex, setter = value => data.lightingDebugSettings.shadowMapIndex = value, min = () => 0u, max = () => (uint)(Math.Max(0, (RenderPipelineManager.currentPipeline as HDRenderPipeline).GetCurrentShadowCount() - 1u)) });

                    shadows.children.Add(container);
                }

                shadows.children.Add(new DebugUI.FloatField
                {
                    displayName = "Global Scale Factor",
                    getter = () => data.lightingDebugSettings.shadowResolutionScaleFactor,
                    setter = (v) => data.lightingDebugSettings.shadowResolutionScaleFactor = v,
                    min = () => 0.01f,
                    max = () => 4.0f,
                });

                shadows.children.Add(new DebugUI.BoolField
                {
                    displayName = "Clear Shadow Atlas",
                    getter = () => data.lightingDebugSettings.clearShadowAtlas,
                    setter = (v) => data.lightingDebugSettings.clearShadowAtlas = v
                });

                shadows.children.Add(new DebugUI.FloatField { displayName = "Range Minimum Value", getter = () => data.lightingDebugSettings.shadowMinValue, setter = value => data.lightingDebugSettings.shadowMinValue = value });
                shadows.children.Add(new DebugUI.FloatField { displayName = "Range Maximum Value", getter = () => data.lightingDebugSettings.shadowMaxValue, setter = value => data.lightingDebugSettings.shadowMaxValue = value });
#if UNITY_EDITOR
                shadows.children.Add(new DebugUI.Button { displayName = "Log Cached Shadow Atlas Status", action = () => HDCachedShadowManager.instance.PrintLightStatusInCachedAtlas() });
#endif
                list.Add(shadows);
            }

            {
                var lighting = new DebugUI.Container() { displayName = "Lighting" };

                lighting.children.Add(new DebugUI.Foldout
                {
                    displayName = "Show Lights By Type",
                    children =
                    {
                        new DebugUI.BoolField { displayName = "Directional Lights", getter = () => data.lightingDebugSettings.showDirectionalLight, setter = value => data.lightingDebugSettings.showDirectionalLight = value },
                        new DebugUI.BoolField { displayName = "Punctual Lights", getter = () => data.lightingDebugSettings.showPunctualLight, setter = value => data.lightingDebugSettings.showPunctualLight = value },
                        new DebugUI.BoolField { displayName = "Area Lights", getter = () => data.lightingDebugSettings.showAreaLight, setter = value => data.lightingDebugSettings.showAreaLight = value },
                        new DebugUI.BoolField { displayName = "Reflection Probes", getter = () => data.lightingDebugSettings.showReflectionProbe, setter = value => data.lightingDebugSettings.showReflectionProbe = value },
                    }
                });

                lighting.children.Add(new DebugUI.Foldout
                {
                    displayName = "Probe Volumes",
                    children =
                    {
                        new DebugUI.EnumField { displayName = "Probe Volume Debug Mode", getter = () => (int)data.lightingDebugSettings.probeVolumeDebugMode, setter = value => SetProbeVolumeDebugMode((ProbeVolumeDebugMode)value), autoEnum = typeof(ProbeVolumeDebugMode), onValueChanged = RefreshLightingDebug, getIndex = () => data.probeVolumeDebugModeEnumIndex, setIndex = value => data.probeVolumeDebugModeEnumIndex = value },
                        new DebugUI.EnumField { displayName = "Probe Volume Atlas Slice Mode", getter = () => (int)data.lightingDebugSettings.probeVolumeAtlasSliceMode, setter = value => SetProbeVolumeAtlasSliceMode((ProbeVolumeAtlasSliceMode)value), autoEnum = typeof(ProbeVolumeAtlasSliceMode), onValueChanged = RefreshLightingDebug, getIndex = () => data.probeVolumeAtlasSliceModeEnumIndex, setIndex = value => data.probeVolumeAtlasSliceModeEnumIndex = value },
                        new DebugUI.FloatField { displayName = "Probe Volume Range Min Value", getter = () => data.lightingDebugSettings.probeVolumeMinValue, setter = value => data.lightingDebugSettings.probeVolumeMinValue = value },
                        new DebugUI.FloatField { displayName = "Probe Volume Range Max Value", getter = () => data.lightingDebugSettings.probeVolumeMaxValue, setter = value => data.lightingDebugSettings.probeVolumeMaxValue = value },
                    }
                });

                var exposureFoldout = new DebugUI.Foldout
                {
                    displayName = "Exposure ",
                    children =
                    {
                        new DebugUI.EnumField
                        {
                            displayName = "Debug Mode",
                            getter = () => (int)data.lightingDebugSettings.exposureDebugMode,
                            setter = value => SetExposureDebugMode((ExposureDebugMode)value),
                            autoEnum = typeof(ExposureDebugMode), onValueChanged = RefreshLightingDebug,
                            getIndex = () => data.exposureDebugModeEnumIndex,
                            setIndex = value => data.exposureDebugModeEnumIndex = value
                        }
                    }
                };

                if (data.lightingDebugSettings.exposureDebugMode == ExposureDebugMode.MeteringWeighted)
                {
                    exposureFoldout.children.Add(
                        new DebugUI.BoolField()
                        {
                            displayName = "Display Mask Only",
                            getter = () => data.lightingDebugSettings.displayMaskOnly,
                            setter = value => data.lightingDebugSettings.displayMaskOnly = value
                        });
                }
                if (data.lightingDebugSettings.exposureDebugMode == ExposureDebugMode.HistogramView)
                {
                    exposureFoldout.children.Add(
                        new DebugUI.BoolField()
                        {
                            displayName = "Show Tonemap curve",
                            getter = () => data.lightingDebugSettings.showTonemapCurveAlongHistogramView,
                            setter = value => data.lightingDebugSettings.showTonemapCurveAlongHistogramView = value
                        });
                    exposureFoldout.children.Add(
                        new DebugUI.BoolField()
                        {
                            displayName = "Center Around Exposure",
                            getter = () => data.lightingDebugSettings.centerHistogramAroundMiddleGrey,
                            setter = value => data.lightingDebugSettings.centerHistogramAroundMiddleGrey = value
                        });
                }
                if (data.lightingDebugSettings.exposureDebugMode == ExposureDebugMode.FinalImageHistogramView)
                {
                    exposureFoldout.children.Add(
                        new DebugUI.BoolField()
                        {
                            displayName = "Display RGB Histogram",
                            getter = () => data.lightingDebugSettings.displayFinalImageHistogramAsRGB,
                            setter = value => data.lightingDebugSettings.displayFinalImageHistogramAsRGB = value
                        });
                }

                exposureFoldout.children.Add(
                    new DebugUI.FloatField
                    {
                        displayName = "Debug Exposure Compensation",
                        getter = () => data.lightingDebugSettings.debugExposure,
                        setter = value => data.lightingDebugSettings.debugExposure = value
                    });

                lighting.children.Add(exposureFoldout);

                lighting.children.Add(new DebugUI.EnumField { displayName = "Debug Mode", getter = () => (int)data.lightingDebugSettings.debugLightingMode, setter = value => SetDebugLightingMode((DebugLightingMode)value), autoEnum = typeof(DebugLightingMode), onValueChanged = RefreshLightingDebug, getIndex = () => data.lightingDebugModeEnumIndex, setIndex = value => { data.ResetExclusiveEnumIndices(); data.lightingDebugModeEnumIndex = value; } });
                lighting.children.Add(new DebugUI.BitField { displayName = "Hierarchy Debug Mode", getter = () => data.lightingDebugSettings.debugLightFilterMode, setter = value => SetDebugLightFilterMode((DebugLightFilterMode)value), enumType = typeof(DebugLightFilterMode), onValueChanged = RefreshLightingDebug, });

                lighting.children.Add(new DebugUI.BoolField { displayName = "Light Layers Visualization", getter = () => data.lightingDebugSettings.debugLightLayers, setter = value => SetDebugLightLayersMode(value), onValueChanged = RefreshLightingDebug });

                if (data.lightingDebugSettings.debugLightLayers)
                {
                    var container = new DebugUI.Container();
                    container.children.Add(new DebugUI.BoolField
                    {
                        displayName = "Use Selected Light",
                        getter = () => data.lightingDebugSettings.debugSelectionLightLayers,
                        setter = value => data.lightingDebugSettings.debugSelectionLightLayers = value,
                        flags = DebugUI.Flags.EditorOnly,
                        onValueChanged = RefreshLightingDebug
                    });

                    if (data.lightingDebugSettings.debugSelectionLightLayers)
                    {
                        container.children.Add(new DebugUI.BoolField
                        {
                            displayName = "Switch to  Light's Shadow Layers",
                            getter = () => data.lightingDebugSettings.debugSelectionShadowLayers,
                            setter = value => data.lightingDebugSettings.debugSelectionShadowLayers = value,
                            flags = DebugUI.Flags.EditorOnly,
                            onValueChanged = RefreshLightingDebug
                        });
                    }
                    else
                    {
                        var field = new DebugUI.BitField
                        {
                            displayName = "Filter Layers",
                            getter = () => data.lightingDebugSettings.debugLightLayersFilterMask,
                            setter = value => data.lightingDebugSettings.debugLightLayersFilterMask = (DebugLightLayersMask)value,
                            enumType = typeof(DebugLightLayersMask)
                        };

                        var asset = (RenderPipelineManager.currentPipeline as HDRenderPipeline).asset;
                        for (int i = 0; i < 8; i++)
                            field.enumNames[i + 1].text = asset.renderingLayerMaskNames[i];
                        container.children.Add(field);
                    }

                    var layersColor = new DebugUI.Foldout() { displayName = "Layers Color", flags = DebugUI.Flags.EditorOnly };
                    for (int i = 0; i < 8; i++)
                    {
                        int index = i;
                        var asset = (RenderPipelineManager.currentPipeline as HDRenderPipeline).asset;
                        layersColor.children.Add(new DebugUI.ColorField
                        {
                            displayName = asset.renderingLayerMaskNames[i],
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
                var material = new DebugUI.Container() { displayName = "Material Overrides" };

                material.children.Add(new DebugUI.BoolField { displayName = "Override Smoothness", getter = () => data.lightingDebugSettings.overrideSmoothness, setter = value => data.lightingDebugSettings.overrideSmoothness = value, onValueChanged = RefreshLightingDebug });
                if (data.lightingDebugSettings.overrideSmoothness)
                {
                    material.children.Add(new DebugUI.Container
                    {
                        children =
                        {
                            new DebugUI.FloatField { displayName = "Smoothness", getter = () => data.lightingDebugSettings.overrideSmoothnessValue, setter = value => data.lightingDebugSettings.overrideSmoothnessValue = value, min = () => 0f, max = () => 1f, incStep = 0.025f }
                        }
                    });
                }

                material.children.Add(new DebugUI.BoolField { displayName = "Override Albedo", getter = () => data.lightingDebugSettings.overrideAlbedo, setter = value => data.lightingDebugSettings.overrideAlbedo = value, onValueChanged = RefreshLightingDebug });
                if (data.lightingDebugSettings.overrideAlbedo)
                {
                    material.children.Add(new DebugUI.Container
                    {
                        children =
                        {
                            new DebugUI.ColorField { displayName = "Albedo", getter = () => data.lightingDebugSettings.overrideAlbedoValue, setter = value => data.lightingDebugSettings.overrideAlbedoValue = value, showAlpha = false, hdr = false }
                        }
                    });
                }

                material.children.Add(new DebugUI.BoolField { displayName = "Override Normal", getter = () => data.lightingDebugSettings.overrideNormal, setter = value => data.lightingDebugSettings.overrideNormal = value });

                material.children.Add(new DebugUI.BoolField { displayName = "Override Specular Color", getter = () => data.lightingDebugSettings.overrideSpecularColor, setter = value => data.lightingDebugSettings.overrideSpecularColor = value, onValueChanged = RefreshLightingDebug });
                if (data.lightingDebugSettings.overrideSpecularColor)
                {
                    material.children.Add(new DebugUI.Container
                    {
                        children =
                        {
                            new DebugUI.ColorField { displayName = "Specular Color", getter = () => data.lightingDebugSettings.overrideSpecularColorValue, setter = value => data.lightingDebugSettings.overrideSpecularColorValue = value, showAlpha = false, hdr = false }
                        }
                    });
                }

                material.children.Add(new DebugUI.BoolField { displayName = "Override Ambient Occlusion", getter = () => data.lightingDebugSettings.overrideAmbientOcclusion, setter = value => data.lightingDebugSettings.overrideAmbientOcclusion = value, onValueChanged = RefreshLightingDebug });
                if (data.lightingDebugSettings.overrideAmbientOcclusion)
                {
                    material.children.Add(new DebugUI.Container
                    {
                        children =
                        {
                            new DebugUI.FloatField { displayName = "AmbientOcclusion", getter = () => data.lightingDebugSettings.overrideAmbientOcclusionValue, setter = value => data.lightingDebugSettings.overrideAmbientOcclusionValue = value, min = () => 0f, max = () => 1f, incStep = 0.025f }
                        }
                    });
                }

                material.children.Add(new DebugUI.BoolField { displayName = "Override Emissive Color", getter = () => data.lightingDebugSettings.overrideEmissiveColor, setter = value => data.lightingDebugSettings.overrideEmissiveColor = value, onValueChanged = RefreshLightingDebug });
                if (data.lightingDebugSettings.overrideEmissiveColor)
                {
                    material.children.Add(new DebugUI.Container
                    {
                        children =
                        {
                            new DebugUI.ColorField { displayName = "Emissive Color", getter = () => data.lightingDebugSettings.overrideEmissiveColorValue, setter = value => data.lightingDebugSettings.overrideEmissiveColorValue = value, showAlpha = false, hdr = true }
                        }
                    });
                }

                list.Add(material);
            }

            list.Add(new DebugUI.EnumField { displayName = "Fullscreen Debug Mode", getter = () => (int)data.fullScreenDebugMode, setter = value => SetFullScreenDebugMode((FullScreenDebugMode)value), enumNames = s_LightingFullScreenDebugStrings, enumValues = s_LightingFullScreenDebugValues, onValueChanged = RefreshLightingDebug, getIndex = () => data.lightingFulscreenDebugModeEnumIndex, setIndex = value => { data.ResetExclusiveEnumIndices(); data.lightingFulscreenDebugModeEnumIndex = value; } });

            if (data.fullScreenDebugMode == FullScreenDebugMode.ScreenSpaceShadows)
            {
                list.Add(new DebugUI.UIntField { displayName = "Screen Space Shadow Index", getter = () => data.screenSpaceShadowIndex, setter = value => data.screenSpaceShadowIndex = value, min = () => 0u, max = () => (uint)(RenderPipelineManager.currentPipeline as HDRenderPipeline).GetMaxScreenSpaceShadows() });
            }

            switch (data.fullScreenDebugMode)
            {
                case FullScreenDebugMode.PreRefractionColorPyramid:
                case FullScreenDebugMode.FinalColorPyramid:
                case FullScreenDebugMode.DepthPyramid:
                {
                    list.Add(new DebugUI.Container
                    {
                        children =
                        {
                            new DebugUI.FloatField { displayName = "Debug Mip", getter = () => data.fullscreenDebugMip, setter = value => data.fullscreenDebugMip = value, min = () => 0f, max = () => 1f, incStep = 0.05f },
                            new DebugUI.BoolField { displayName = "Enable Depth Remap", getter = () => data.enableDebugDepthRemap, setter = value => data.enableDebugDepthRemap = value },
                            new DebugUI.FloatField { displayName = "Depth range min value", getter = () => data.fullScreenDebugDepthRemap.x, setter = value => data.fullScreenDebugDepthRemap.x = value, min = () => 0f, max = () => 1f, incStep = 0.05f },
                            new DebugUI.FloatField { displayName = "Depth range max value", getter = () => data.fullScreenDebugDepthRemap.y, setter = value => data.fullScreenDebugDepthRemap.y = value, min = () => 0f, max = () => 1f, incStep = 0.05f }
                        }
                    });
                    break;
                }
                case FullScreenDebugMode.ContactShadows:
                    list.Add(new DebugUI.Container
                    {
                        children =
                        {
                            new DebugUI.IntField
                            {
                                displayName = "Light Index",
                                getter = () =>
                                {
                                    return data.fullScreenContactShadowLightIndex;
                                },
                                setter = value =>
                                {
                                    data.fullScreenContactShadowLightIndex = value;
                                },
                                min = () => - 1, // -1 will display all contact shadow
                                max = () => LightDefinitions.s_LightListMaxPrunedEntries - 1
                            },
                        }
                    });
                    break;
                default:
                    data.fullscreenDebugMip = 0;
                    break;
            }

            list.Add(new DebugUI.EnumField { displayName = "Tile/Cluster Debug", getter = () => (int)data.lightingDebugSettings.tileClusterDebug, setter = value => data.lightingDebugSettings.tileClusterDebug = (TileClusterDebug)value, autoEnum = typeof(TileClusterDebug), onValueChanged = RefreshLightingDebug, getIndex = () => data.tileClusterDebugEnumIndex, setIndex = value => data.tileClusterDebugEnumIndex = value });
            if (data.lightingDebugSettings.tileClusterDebug != TileClusterDebug.None && data.lightingDebugSettings.tileClusterDebug != TileClusterDebug.MaterialFeatureVariants)
            {
                var clusterDebugContainer = new DebugUI.Container();

                clusterDebugContainer.children.Add(new DebugUI.EnumField { displayName = "Tile/Cluster Debug By Category", getter = () => (int)data.lightingDebugSettings.tileClusterDebugByCategory, setter = value => data.lightingDebugSettings.tileClusterDebugByCategory = (TileClusterCategoryDebug)value, enumNames = s_TileAndClusterDebugStrings, enumValues = s_TileAndClusterDebugValues, getIndex = () => data.tileClusterDebugByCategoryEnumIndex, setIndex = value => data.tileClusterDebugByCategoryEnumIndex = value });
                if (data.lightingDebugSettings.tileClusterDebug == TileClusterDebug.Cluster)
                {
                    clusterDebugContainer.children.Add(new DebugUI.EnumField { displayName = "Cluster Debug Mode", getter = () => (int)data.lightingDebugSettings.clusterDebugMode, setter = value => data.lightingDebugSettings.clusterDebugMode = (ClusterDebugMode)value, autoEnum = typeof(ClusterDebugMode), onValueChanged = RefreshLightingDebug, getIndex = () => data.clusterDebugModeEnumIndex, setIndex = value => data.clusterDebugModeEnumIndex = value });

                    if (data.lightingDebugSettings.clusterDebugMode == ClusterDebugMode.VisualizeSlice)
                        clusterDebugContainer.children.Add(new DebugUI.FloatField { displayName = "Cluster Distance", getter = () => data.lightingDebugSettings.clusterDebugDistance, setter = value => data.lightingDebugSettings.clusterDebugDistance = value, min = () => 0f, max = () => 100.0f, incStep = 0.05f });
                }

                list.Add(clusterDebugContainer);
            }

            list.Add(new DebugUI.BoolField { displayName = "Display Sky Reflection", getter = () => data.lightingDebugSettings.displaySkyReflection, setter = value => data.lightingDebugSettings.displaySkyReflection = value, onValueChanged = RefreshLightingDebug });
            if (data.lightingDebugSettings.displaySkyReflection)
            {
                list.Add(new DebugUI.Container
                {
                    children =
                    {
                        new DebugUI.FloatField { displayName = "Sky Reflection Mipmap", getter = () => data.lightingDebugSettings.skyReflectionMipmap, setter = value => data.lightingDebugSettings.skyReflectionMipmap = value, min = () => 0f, max = () => 1f, incStep = 0.05f }
                    }
                });
            }

            list.Add(new DebugUI.BoolField { displayName = "Display Light Volumes", getter = () => data.lightingDebugSettings.displayLightVolumes, setter = value => data.lightingDebugSettings.displayLightVolumes = value, onValueChanged = RefreshLightingDebug });
            if (data.lightingDebugSettings.displayLightVolumes)
            {
                var container = new DebugUI.Container
                {
                    children =
                    {
                        new DebugUI.EnumField { displayName = "Light Volume Debug Type", getter = () => (int)data.lightingDebugSettings.lightVolumeDebugByCategory, setter = value => data.lightingDebugSettings.lightVolumeDebugByCategory = (LightVolumeDebug)value, autoEnum = typeof(LightVolumeDebug), getIndex = () => data.lightVolumeDebugTypeEnumIndex, setIndex = value => data.lightVolumeDebugTypeEnumIndex = value, onValueChanged = RefreshLightingDebug }
                    }
                };
                if (data.lightingDebugSettings.lightVolumeDebugByCategory == LightVolumeDebug.Gradient)
                {
                    container.children.Add(new DebugUI.UIntField { displayName = "Max Debug Light Count", getter = () => (uint)data.lightingDebugSettings.maxDebugLightCount, setter = value => data.lightingDebugSettings.maxDebugLightCount = value, min = () => 0, max = () => 24, incStep = 1 });
                }

                list.Add(container);
            }

            list.Add(new DebugUI.BoolField { displayName = "Display Cookie Atlas", getter = () => data.lightingDebugSettings.displayCookieAtlas, setter = value => data.lightingDebugSettings.displayCookieAtlas = value, onValueChanged = RefreshLightingDebug});
            if (data.lightingDebugSettings.displayCookieAtlas)
            {
                list.Add(new DebugUI.Container
                {
                    children =
                    {
                        new DebugUI.UIntField { displayName = "Mip Level", getter = () => data.lightingDebugSettings.cookieAtlasMipLevel, setter = value => data.lightingDebugSettings.cookieAtlasMipLevel = value, min = () => 0, max = () => (uint)(RenderPipelineManager.currentPipeline as HDRenderPipeline).GetCookieAtlasMipCount()},
                        new DebugUI.BoolField { displayName = "Clear Cookie Atlas", getter = () => data.lightingDebugSettings.clearCookieAtlas, setter = value => data.lightingDebugSettings.clearCookieAtlas = value}
                    }
                });
            }

            list.Add(new DebugUI.BoolField { displayName = "Display Planar Reflection Atlas", getter = () => data.lightingDebugSettings.displayPlanarReflectionProbeAtlas, setter = value => data.lightingDebugSettings.displayPlanarReflectionProbeAtlas = value, onValueChanged = RefreshLightingDebug});
            if (data.lightingDebugSettings.displayPlanarReflectionProbeAtlas)
            {
                list.Add(new DebugUI.Container
                {
                    children =
                    {
                        new DebugUI.UIntField { displayName = "Mip Level", getter = () => data.lightingDebugSettings.planarReflectionProbeMipLevel, setter = value => data.lightingDebugSettings.planarReflectionProbeMipLevel = value, min = () => 0, max = () => (uint)(RenderPipelineManager.currentPipeline as HDRenderPipeline).GetPlanarReflectionProbeMipCount()},
                        new DebugUI.BoolField { displayName = "Clear Planar Atlas", getter = () => data.lightingDebugSettings.clearPlanarReflectionProbeAtlas, setter = value => data.lightingDebugSettings.clearPlanarReflectionProbeAtlas = value},
                    }
                });
            }

            list.Add(new DebugUI.FloatField { displayName = "Debug Overlay Screen Ratio", getter = () => data.debugOverlayRatio, setter = v => data.debugOverlayRatio = v, min = () => 0.1f, max = () => 1f});

            m_DebugLightingItems = list.ToArray();
            var panel = DebugManager.instance.GetPanel(k_PanelLighting, true);
            panel.children.Add(m_DebugLightingItems);
        }

        void RegisterVolumeDebug()
        {
            var list = new List<DebugUI.Widget>();

            int componentIndex = 0;
            var componentNames = new List<GUIContent>() { new GUIContent("None") };
            var componentValues = new List<int>() { componentIndex++ };

            foreach (var type in VolumeDebugSettings.componentTypes)
            {
                componentNames.Add(new GUIContent() { text = VolumeDebugSettings.ComponentDisplayName(type) });
                componentValues.Add(componentIndex++);
            }

            list.Add(new DebugUI.EnumField
            {
                displayName = "Component",
                getter = () => data.volumeDebugSettings.selectedComponent,
                setter = value => data.volumeDebugSettings.selectedComponent = value,
                enumNames = componentNames.ToArray(),
                enumValues = componentValues.ToArray(),
                getIndex = () => data.volumeComponentEnumIndex,
                setIndex = value => { data.volumeComponentEnumIndex = value; },
                onValueChanged = RefreshVolumeDebug,
            });

            if (data.volumeDebugSettings.selectedComponent != 0)
            {
                componentIndex = 0;
                componentNames = new List<GUIContent>() { new GUIContent("None") };
                componentValues = new List<int>() { componentIndex++ };

#if UNITY_EDITOR
                componentNames.Add(new GUIContent() { text = "Editor Camera" });
                componentValues.Add(componentIndex++);
#endif

                foreach (var camera in VolumeDebugSettings.cameras)
                {
                    componentNames.Add(new GUIContent() { text = camera.name });
                    componentValues.Add(componentIndex++);
                }

                list.Add(new DebugUI.EnumField
                {
                    displayName = "Camera",
                    getter = () => data.volumeDebugSettings.selectedCameraIndex,
                    setter = value => data.volumeDebugSettings.selectedCameraIndex = value,
                    enumNames = componentNames.ToArray(),
                    enumValues = componentValues.ToArray(),
                    getIndex = () => data.volumeCameraEnumIndex,
                    setIndex = value => { data.volumeCameraEnumIndex = value; },
                    onValueChanged = RefreshVolumeDebug,
                });

                if (data.volumeDebugSettings.selectedCameraIndex != 0)
                {
                    DebugUI.Widget makeWidget(string name, VolumeParameter param)
                    {
                        if (param == null)
                            return new DebugUI.Value() { displayName = name, getter = () => "-" };

                        // Special overrides
                        if (param.GetType() == typeof(ColorParameter))
                        {
                            var p = (ColorParameter)param;
                            return new DebugUI.ColorField()
                            {
                                displayName = name,
                                hdr = p.hdr,
                                showAlpha = p.showAlpha,
                                getter = () => p.value,
                                setter = _ => {}
                            };
                        }

                        if (param.GetType() == typeof(BoolParameter))
                        {
                            var p = (BoolParameter)param;
                            return new DebugUI.BoolField()
                            {
                                displayName = name,
                                getter = () => p.value,
                                setter = _ => {}
                            };
                        }

                        // For parameters that do not override `ToString`
                        var property = param.GetType().GetProperty("value");
                        var toString = property.PropertyType.GetMethod("ToString", Type.EmptyTypes);
                        if ((toString == null) || (toString.DeclaringType == typeof(object)) || (toString.DeclaringType == typeof(UnityEngine.Object)))
                        {
                            // Check if the parameter has a name
                            var nameProp = property.PropertyType.GetProperty("name");
                            if (nameProp == null)
                                return new DebugUI.Value() { displayName = name, getter = () => "Debug view not supported" };

                            // Return the parameter name
                            return new DebugUI.Value()
                            {
                                displayName = name,
                                getter = () => {
                                    var value = property.GetValue(param);
                                    if (value == null || value.Equals(null))
                                        return "None";
                                    var valueString = nameProp.GetValue(value);
                                    return valueString == null ? "None" : valueString;
                                }
                            };
                        }

                        // Call the ToString method
                        return new DebugUI.Value()
                        {
                            displayName = name,
                            getter = () => {
                                var value = property.GetValue(param);
                                return value == null ? "None" : value.ToString();
                            }
                        };
                    }

                    Type selectedType = data.volumeDebugSettings.selectedComponentType;
                    var stackComponent = data.volumeDebugSettings.selectedCameraVolumeStack.GetComponent(selectedType);

                    var volumes = data.volumeDebugSettings.GetVolumes();
                    var table = new DebugUI.Table() { displayName = "Parameter", isReadOnly = true };

                    var inst = (VolumeComponent)ScriptableObject.CreateInstance(selectedType);

                    // First row for volume info
                    float timer = 0.0f, refreshRate = 0.2f;
                    var row = new DebugUI.Table.Row()
                    {
                        displayName = "Volume Info",
                        children = { new DebugUI.Value() {
                                         displayName = "Interpolated Value",
                                         getter = () => {
                                             // This getter is called first at each render
                                             // It is used to update the volumes
                                             if (Time.time - timer < refreshRate)
                                                 return "";
                                             timer = Time.deltaTime;
                                             if (data.volumeDebugSettings.selectedCameraIndex != 0)
                                             {
                                                 var newVolumes = data.volumeDebugSettings.GetVolumes();
                                                 if (!data.volumeDebugSettings.RefreshVolumes(newVolumes))
                                                 {
                                                     for (int i = 0; i < newVolumes.Length; i++)
                                                     {
                                                         var visible = data.volumeDebugSettings.VolumeHasInfluence(newVolumes[i]);
                                                         table.SetColumnVisibility(i + 1, visible);
                                                     }
                                                     return "";
                                                 }
                                             }
                                             RefreshVolumeDebug(null, false);
                                             return "";
                                         }
                                     } }
                    };
                    row.opened = true;

                    foreach (var volume in volumes)
                    {
                        var profile = volume.HasInstantiatedProfile() ? volume.profile : volume.sharedProfile;
                        row.children.Add(new DebugUI.Value()
                        {
                            displayName = volume.name + " (" + profile.name + ")",
                            getter = () => {
                                var scope = volume.isGlobal ? "Global" : "Local";
                                var weight = data.volumeDebugSettings.GetVolumeWeight(volume);
                                return scope + " (" + (weight * 100f) + "%)";
                            }
                        });
                    }

                    row.children.Add(new DebugUI.Value() { displayName = "Default Value", getter = () => "" });
                    table.children.Add(row);

                    // Build rows - recursively handles nested parameters
                    var rows = new List<DebugUI.Table.Row>();
                    void AddParameterRows(Type type, string baseName = null)
                    {
                        void AddRow(FieldInfo f, string prefix)
                        {
                            var fieldName = prefix + f.Name;
                            var attr = (DisplayInfoAttribute[])f.GetCustomAttributes(typeof(DisplayInfoAttribute), true);
                            if (attr.Length != 0)
                                fieldName = prefix + attr[0].name;
#if UNITY_EDITOR
                            // Would be nice to have the equivalent for the runtime debug.
                            else
                                fieldName = UnityEditor.ObjectNames.NicifyVariableName(fieldName);
#endif

                            int currentParam = rows.Count;
                            row = new DebugUI.Table.Row()
                            {
                                displayName = fieldName,
                                children = { makeWidget("Interpolated Value", stackComponent.parameters[currentParam]) }
                            };

                            foreach (var volume in volumes)
                            {
                                VolumeParameter param = null;
                                var profile = volume.HasInstantiatedProfile() ? volume.profile : volume.sharedProfile;
                                if (profile.TryGet(selectedType, out VolumeComponent component) && component.parameters[currentParam].overrideState)
                                    param = component.parameters[currentParam];
                                row.children.Add(makeWidget(volume.name + " (" + profile.name + ")", param));
                            }

                            row.children.Add(makeWidget("Default Value", inst.parameters[currentParam]));
                            rows.Add(row);
                        }

                        var fields = type
                            .GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                            .OrderBy(t => t.MetadataToken);
                        foreach (var field in fields)
                        {
                            var fieldType = field.FieldType;
                            if (fieldType.IsSubclassOf(typeof(VolumeParameter)))
                                AddRow(field, baseName ?? "");
                            else if (!fieldType.IsArray && fieldType.IsClass)
                                AddParameterRows(fieldType, baseName ?? (field.Name + " "));
                        }
                    }

                    AddParameterRows(selectedType);
                    foreach (var r in rows.OrderBy(t => t.displayName))
                        table.children.Add(r);

                    data.volumeDebugSettings.RefreshVolumes(volumes);
                    for (int i = 0; i < volumes.Length; i++)
                        table.SetColumnVisibility(i + 1, data.volumeDebugSettings.VolumeHasInfluence(volumes[i]));
                    list.Add(table);
                }
            }

            m_DebugVolumeItems = list.ToArray();
            var panel = DebugManager.instance.GetPanel(k_PanelVolume, true);
            panel.children.Add(m_DebugVolumeItems);
        }

        void RegisterRenderingDebug()
        {
            var widgetList = new List<DebugUI.Widget>();

            widgetList.Add(
                new DebugUI.EnumField { displayName = "Fullscreen Debug Mode", getter = () => (int)data.fullScreenDebugMode, setter = value => SetFullScreenDebugMode((FullScreenDebugMode)value), onValueChanged = RefreshRenderingDebug, enumNames = s_RenderingFullScreenDebugStrings, enumValues = s_RenderingFullScreenDebugValues, getIndex = () => data.renderingFulscreenDebugModeEnumIndex, setIndex = value => { data.ResetExclusiveEnumIndices(); data.renderingFulscreenDebugModeEnumIndex = value; } }
            );

            if (data.fullScreenDebugMode == FullScreenDebugMode.TransparencyOverdraw)
            {
                widgetList.Add(new DebugUI.Container
                {
                    children =
                    {
                        new DebugUI.FloatField {displayName = "Max Pixel Cost", getter = () => data.transparencyDebugSettings.maxPixelCost, setter = value => data.transparencyDebugSettings.maxPixelCost = value, min = () => 0.25f, max = () => 2048.0f}
                    }
                });
            }
            else if (data.fullScreenDebugMode == FullScreenDebugMode.QuadOverdraw)
            {
                widgetList.Add(new DebugUI.Container
                {
                    children =
                    {
                        new DebugUI.UIntField {displayName = "Max Quad Cost", getter = () => data.maxQuadCost, setter = value => data.maxQuadCost = value, min = () => 1, max = () => 10}
                    }
                });
            }
            else if (data.fullScreenDebugMode == FullScreenDebugMode.VertexDensity)
            {
                widgetList.Add(new DebugUI.Container
                {
                    children =
                    {
                        new DebugUI.UIntField {displayName = "Max Vertex Density", getter = () => data.maxVertexDensity, setter = value => data.maxVertexDensity = value, min = () => 1, max = () => 100}
                    }
                });
            }

            widgetList.AddRange(new DebugUI.Widget[]
            {
                new DebugUI.EnumField { displayName = "MipMaps", getter = () => (int)data.mipMapDebugSettings.debugMipMapMode, setter = value => SetMipMapMode((DebugMipMapMode)value), autoEnum = typeof(DebugMipMapMode), onValueChanged = RefreshRenderingDebug, getIndex = () => data.mipMapsEnumIndex, setIndex = value => { data.ResetExclusiveEnumIndices(); data.mipMapsEnumIndex = value; } },
            });

            if (data.mipMapDebugSettings.debugMipMapMode != DebugMipMapMode.None)
            {
                widgetList.Add(new DebugUI.Container
                {
                    children =
                    {
                        new DebugUI.EnumField { displayName = "Terrain Texture", getter = () => (int)data.mipMapDebugSettings.terrainTexture, setter = value => data.mipMapDebugSettings.terrainTexture = (DebugMipMapModeTerrainTexture)value, autoEnum = typeof(DebugMipMapModeTerrainTexture), getIndex = () => data.terrainTextureEnumIndex, setIndex = value => data.terrainTextureEnumIndex = value }
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
                        new DebugUI.EnumField  { displayName = "Debug Mode", getter = () => (int)data.colorPickerDebugSettings.colorPickerMode, setter = value => data.colorPickerDebugSettings.colorPickerMode = (ColorPickerDebugMode)value, autoEnum = typeof(ColorPickerDebugMode), getIndex = () => data.colorPickerDebugModeEnumIndex, setIndex = value => data.colorPickerDebugModeEnumIndex = value },
                        new DebugUI.ColorField { displayName = "Font Color", flags = DebugUI.Flags.EditorOnly, getter = () => data.colorPickerDebugSettings.fontColor, setter = value => data.colorPickerDebugSettings.fontColor = value }
                    }
                }
            });

            widgetList.Add(new DebugUI.BoolField  { displayName = "False Color Mode", getter = () => data.falseColorDebugSettings.falseColor, setter = value => data.falseColorDebugSettings.falseColor = value, onValueChanged = RefreshRenderingDebug });
            if (data.falseColorDebugSettings.falseColor)
            {
                widgetList.Add(new DebugUI.Container
                {
                    flags = DebugUI.Flags.EditorOnly,
                    children =
                    {
                        new DebugUI.FloatField { displayName = "Range Threshold 0", getter = () => data.falseColorDebugSettings.colorThreshold0, setter = value => data.falseColorDebugSettings.colorThreshold0 = Mathf.Min(value, data.falseColorDebugSettings.colorThreshold1) },
                        new DebugUI.FloatField { displayName = "Range Threshold 1", getter = () => data.falseColorDebugSettings.colorThreshold1, setter = value => data.falseColorDebugSettings.colorThreshold1 = Mathf.Clamp(value, data.falseColorDebugSettings.colorThreshold0, data.falseColorDebugSettings.colorThreshold2) },
                        new DebugUI.FloatField { displayName = "Range Threshold 2", getter = () => data.falseColorDebugSettings.colorThreshold2, setter = value => data.falseColorDebugSettings.colorThreshold2 = Mathf.Clamp(value, data.falseColorDebugSettings.colorThreshold1, data.falseColorDebugSettings.colorThreshold3) },
                        new DebugUI.FloatField { displayName = "Range Threshold 3", getter = () => data.falseColorDebugSettings.colorThreshold3, setter = value => data.falseColorDebugSettings.colorThreshold3 = Mathf.Max(value, data.falseColorDebugSettings.colorThreshold2) },
                    }
                });
            }

            if (HDRenderPipeline.currentAsset?.currentPlatformRenderPipelineSettings.supportMSAA ?? true)
            {
                widgetList.AddRange(new DebugUI.Widget[]
                {
                    new DebugUI.EnumField { displayName = "MSAA Samples", getter = () => (int)data.msaaSamples, setter = value => data.msaaSamples = (MSAASamples)value, enumNames = s_MsaaSamplesDebugStrings, enumValues = s_MsaaSamplesDebugValues, getIndex = () => data.msaaSampleDebugModeEnumIndex, setIndex = value => data.msaaSampleDebugModeEnumIndex = value },
                });
            }

            widgetList.AddRange(new DebugUI.Widget[]
            {
                new DebugUI.EnumField { displayName = "Freeze Camera for culling", getter = () => data.debugCameraToFreeze, setter = value => data.debugCameraToFreeze = value, enumNames = s_CameraNamesStrings, enumValues = s_CameraNamesValues, getIndex = () => data.debugCameraToFreezeEnumIndex, setIndex = value => data.debugCameraToFreezeEnumIndex = value },
            });

            if (XRGraphicsAutomatedTests.enabled)
            {
                widgetList.Add(new DebugUI.BoolField { displayName = "XR single-pass test mode", getter = () => data.xrSinglePassTestMode, setter = value => data.xrSinglePassTestMode = value });
            }

            m_DebugRenderingItems = widgetList.ToArray();
            var panel = DebugManager.instance.GetPanel(k_PanelRendering, true);
            panel.children.Add(m_DebugRenderingItems);
        }

        void RegisterDecalsDebug()
        {
            var decalAffectingTransparent = new DebugUI.Container()
            {
                displayName = "Decals Affecting Transparent Objects",
                children =
                {
                    new DebugUI.BoolField { displayName = "Display Atlas", getter = () => data.decalsDebugSettings.displayAtlas, setter = value => data.decalsDebugSettings.displayAtlas = value},
                    new DebugUI.UIntField { displayName = "Mip Level", getter = () => data.decalsDebugSettings.mipLevel, setter = value => data.decalsDebugSettings.mipLevel = value, min = () => 0u, max = () => (uint)(RenderPipelineManager.currentPipeline as HDRenderPipeline)?.GetDecalAtlasMipCount() }
                }
            };

            m_DebugDecalsAffectingTransparentItems = new DebugUI.Widget[] { decalAffectingTransparent };
            var panel = DebugManager.instance.GetPanel(k_PanelDecals, true);
            panel.children.Add(m_DebugDecalsAffectingTransparentItems);
        }

        internal void RegisterDebug()
        {
            RegisterDecalsDebug();
            RegisterDisplayStatsDebug();
            RegisterMaterialDebug();
            RegisterLightingDebug();
            RegisterVolumeDebug();
            RegisterRenderingDebug();
            DebugManager.instance.RegisterData(this);
        }

        internal void UnregisterDebug()
        {
            UnregisterDebugItems(k_PanelDecals, m_DebugDecalsAffectingTransparentItems);

            DisableProfilingRecorders();
            if (HDRenderPipeline.currentAsset?.currentPlatformRenderPipelineSettings.supportRayTracing ?? true)
                DisableProfilingRecordersRT();
            UnregisterDebugItems(k_PanelDisplayStats, m_DebugDisplayStatsItems);

            UnregisterDebugItems(k_PanelMaterials, m_DebugMaterialItems);
            UnregisterDebugItems(k_PanelLighting, m_DebugLightingItems);
            UnregisterDebugItems(k_PanelVolume, m_DebugVolumeItems);
            UnregisterDebugItems(k_PanelRendering, m_DebugRenderingItems);
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

        void FillTileClusterDebugEnum()
        {
            string[] names = Enum.GetNames(typeof(TileClusterCategoryDebug));
            for (int i = 0; i < names.Length; ++i)
            {
                var n = names[i];
                names[i] = n.Replace("Environment", "ReflectionProbes");
            }

            s_TileAndClusterDebugStrings = names
                .Select(t => new GUIContent(t))
                .ToArray();
            s_TileAndClusterDebugValues = (int[])Enum.GetValues(typeof(TileClusterCategoryDebug));
        }

        static string FormatVector(Vector3 v)
        {
            return string.Format("({0:F6}, {1:F6}, {2:F6})", v.x, v.y, v.z);
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

                UnregisterDebugItems(k_PanelRendering, m_DebugRenderingItems);
                RegisterRenderingDebug();
                needsRefreshingCameraFreezeList = false;
            }
        }

        internal bool DebugHideSky(HDCamera hdCamera)
        {
            return (IsMatcapViewEnabled(hdCamera) ||
                GetDebugLightingMode() ==  DebugLightingMode.DiffuseLighting ||
                GetDebugLightingMode() == DebugLightingMode.SpecularLighting ||
                GetDebugLightingMode() == DebugLightingMode.DirectDiffuseLighting ||
                GetDebugLightingMode() == DebugLightingMode.DirectSpecularLighting ||
                GetDebugLightingMode() == DebugLightingMode.IndirectDiffuseLighting ||
                GetDebugLightingMode() == DebugLightingMode.ReflectionLighting ||
                GetDebugLightingMode() == DebugLightingMode.RefractionLighting
            );
        }

        internal bool DebugNeedsExposure()
        {
            DebugLightingMode debugLighting = data.lightingDebugSettings.debugLightingMode;
            DebugViewGbuffer debugGBuffer = (DebugViewGbuffer)data.materialDebugSettings.debugViewGBuffer;
            return (debugLighting == DebugLightingMode.DirectDiffuseLighting || debugLighting == DebugLightingMode.DirectSpecularLighting || debugLighting == DebugLightingMode.IndirectDiffuseLighting || debugLighting == DebugLightingMode.ReflectionLighting || debugLighting == DebugLightingMode.RefractionLighting || debugLighting == DebugLightingMode.EmissiveLighting ||
                debugLighting == DebugLightingMode.DiffuseLighting || debugLighting == DebugLightingMode.SpecularLighting || debugLighting == DebugLightingMode.VisualizeCascade) ||
                (data.lightingDebugSettings.overrideAlbedo || data.lightingDebugSettings.overrideNormal || data.lightingDebugSettings.overrideSmoothness || data.lightingDebugSettings.overrideSpecularColor || data.lightingDebugSettings.overrideEmissiveColor || data.lightingDebugSettings.overrideAmbientOcclusion) ||
                (debugGBuffer == DebugViewGbuffer.BakeDiffuseLightingWithAlbedoPlusEmissive) || (data.lightingDebugSettings.debugLightFilterMode != DebugLightFilterMode.None) ||
                (data.fullScreenDebugMode == FullScreenDebugMode.PreRefractionColorPyramid || data.fullScreenDebugMode == FullScreenDebugMode.FinalColorPyramid || data.fullScreenDebugMode == FullScreenDebugMode.TransparentScreenSpaceReflections || data.fullScreenDebugMode == FullScreenDebugMode.ScreenSpaceReflections || data.fullScreenDebugMode == FullScreenDebugMode.ScreenSpaceReflectionsPrev || data.fullScreenDebugMode == FullScreenDebugMode.ScreenSpaceReflectionsAccum || data.fullScreenDebugMode == FullScreenDebugMode.LightCluster || data.fullScreenDebugMode == FullScreenDebugMode.ScreenSpaceShadows || data.fullScreenDebugMode == FullScreenDebugMode.NanTracker || data.fullScreenDebugMode == FullScreenDebugMode.ColorLog) || data.fullScreenDebugMode == FullScreenDebugMode.ScreenSpaceGlobalIllumination;
        }
    }
}
