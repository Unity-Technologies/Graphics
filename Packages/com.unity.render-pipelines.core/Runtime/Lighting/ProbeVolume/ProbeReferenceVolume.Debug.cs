using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Experimental.Rendering.RenderGraphModule;
using UnityEditor;

namespace UnityEngine.Rendering
{
    /// <summary>
    /// Modes for Debugging Probes
    /// </summary>
    [GenerateHLSL]
    public enum DebugProbeShadingMode
    {
        /// <summary>
        /// Based on Spherical Harmonics
        /// </summary>
        SH,
        /// <summary>
        /// Based on Spherical Harmonics first band only (ambient)
        /// </summary>
        SHL0,
        /// <summary>
        /// Based on Spherical Harmonics band zero and one only
        /// </summary>
        SHL0L1,
        /// <summary>
        /// Based on validity
        /// </summary>
        Validity,
        /// <summary>
        /// Based on validity over a dilation threshold
        /// </summary>
        ValidityOverDilationThreshold,
        /// <summary>
        /// Show in red probes that have been made invalid by adjustment volumes. Important to note that this debug view will only show result for volumes still present in the scene.
        /// </summary>
        InvalidatedByTouchupVolumes,
        /// <summary>
        /// Based on size
        /// </summary>
        Size
    }

    enum ProbeSamplingDebugUpdate
    {
        Never,
        Once,
        Always
    }

    class ProbeSamplingDebugData
    {
        public ProbeSamplingDebugUpdate update = ProbeSamplingDebugUpdate.Never; // When compute buffer should be updated
        public Vector2 coordinates = new Vector2(0.5f, 0.5f);
        public bool forceScreenCenterCoordinates = false; // use screen center instead of mouse position
        public Camera camera = null; // useful in editor when multiple scene tabs are opened
        public bool shortcutPressed = false;
        public GraphicsBuffer positionNormalBuffer; // buffer storing position and normal
    }


    class ProbeVolumeDebug : IDebugData
    {
        public bool drawProbes;
        public bool drawBricks;
        public bool drawCells;
        public bool realtimeSubdivision;
        public int subdivisionCellUpdatePerFrame = 4;
        public float subdivisionDelayInSeconds = 1;
        public DebugProbeShadingMode probeShading;
        public float probeSize = 0.3f;
        public float subdivisionViewCullingDistance = 500.0f;
        public float probeCullingDistance = 200.0f;
        public int maxSubdivToVisualize = ProbeBrickIndex.kMaxSubdivisionLevels;
        public int minSubdivToVisualize = 0;
        public float exposureCompensation;
        public bool drawProbeSamplingDebug = false;
        public float probeSamplingDebugSize = 0.3f;
        public bool debugWithSamplingNoise = false;
        public bool drawVirtualOffsetPush;
        public float offsetSize = 0.025f;
        public bool freezeStreaming;
        public bool displayCellStreamingScore;
        public bool displayIndexFragmentation;
        public int otherStateIndex = 0;
        public bool verboseStreamingLog;


        public ProbeVolumeDebug()
        {
            Init();
        }

        void Init()
        {
            drawProbes = false;
            drawBricks = false;
            drawCells = false;
            realtimeSubdivision = false;
            subdivisionCellUpdatePerFrame = 4;
            subdivisionDelayInSeconds = 1;
            probeShading = DebugProbeShadingMode.SH;
            probeSize = 0.3f;
            subdivisionViewCullingDistance = 500.0f;
            probeCullingDistance = 200.0f;
            maxSubdivToVisualize = ProbeBrickIndex.kMaxSubdivisionLevels;
            minSubdivToVisualize = 0;
            exposureCompensation = 0.0f;
            drawProbeSamplingDebug = false;
            probeSamplingDebugSize = 0.3f;
            drawVirtualOffsetPush = false;
            offsetSize = 0.025f;
            freezeStreaming = false;
            displayCellStreamingScore = false;
            displayIndexFragmentation = false;
            otherStateIndex = 0;
        }

        public Action GetReset() => () => Init();
    }


#if UNITY_EDITOR
    [UnityEditor.InitializeOnLoad]
#endif
    internal class ProbeVolumeDebugColorPreferences
    {
        internal static Func<Color> GetDetailSubdivisionColor;
        internal static Func<Color> GetMediumSubdivisionColor;
        internal static Func<Color> GetLowSubdivisionColor;
        internal static Func<Color> GetVeryLowSubdivisionColor;
        internal static Func<Color> GetSparseSubdivisionColor;
        internal static Func<Color> GetSparsestSubdivisionColor;

        internal static Color s_DetailSubdivision   = new Color32(135, 35,  255, 255);
        internal static Color s_MediumSubdivision   = new Color32(54,  208, 228, 255);
        internal static Color s_LowSubdivision      = new Color32(255, 100, 45,  255);
        internal static Color s_VeryLowSubdivision  = new Color32(52,  87,  255, 255);
        internal static Color s_SparseSubdivision   = new Color32(255, 71,  97,  255);
        internal static Color s_SparsestSubdivision = new Color32(200, 227, 39,  255);

        static ProbeVolumeDebugColorPreferences()
        {
#if UNITY_EDITOR
            GetDetailSubdivisionColor = CoreRenderPipelinePreferences.RegisterPreferenceColor("Adaptive Probe Volumes/Level 0 Subdivision",    s_DetailSubdivision);
            GetMediumSubdivisionColor = CoreRenderPipelinePreferences.RegisterPreferenceColor("Adaptive Probe Volumes/Level 1 Subdivision", s_MediumSubdivision);
            GetLowSubdivisionColor = CoreRenderPipelinePreferences.RegisterPreferenceColor("Adaptive Probe Volumes/Level 2 Subdivision", s_LowSubdivision);
            GetVeryLowSubdivisionColor = CoreRenderPipelinePreferences.RegisterPreferenceColor("Adaptive Probe Volumes/Level 3 Subdivision", s_VeryLowSubdivision);
            GetSparseSubdivisionColor = CoreRenderPipelinePreferences.RegisterPreferenceColor("Adaptive Probe Volumes/Level 4 Subdivision", s_SparseSubdivision);
            GetSparsestSubdivisionColor = CoreRenderPipelinePreferences.RegisterPreferenceColor("Adaptive Probe Volumes/Level 5 Subdivision", s_SparsestSubdivision);
#endif
        }

    }
    public partial class ProbeReferenceVolume
    {
        internal class CellInstancedDebugProbes
        {
            public List<Matrix4x4[]> probeBuffers;
            public List<Matrix4x4[]> offsetBuffers;
            public List<MaterialPropertyBlock> props;
        }

        const int kProbesPerBatch = 511;

        /// <summary>Name of debug panel for Probe Volume</summary>
        public static readonly string k_DebugPanelName = "Probe Volume";

        internal ProbeVolumeDebug probeVolumeDebug { get; } = new ProbeVolumeDebug();

        /// <summary>Colors that can be used for debug visualization of the brick structure subdivision.</summary>
        public Color[] subdivisionDebugColors { get; } = new Color[ProbeBrickIndex.kMaxSubdivisionLevels];

        Mesh m_DebugMesh;
        Mesh debugMesh {
            get
            {
                if (m_DebugMesh == null)
                    m_DebugMesh = DebugShapes.instance.BuildCustomSphereMesh(0.5f, 9, 8); // (longSubdiv + 1) * latSubdiv + 2 = 82
                return m_DebugMesh;
            }
        }

        DebugUI.Widget[] m_DebugItems;
        Material m_DebugMaterial;

        // Sample position debug
        Mesh m_DebugProbeSamplingMesh; // mesh with 8 quads, 1 arrow and 2 locators
        Material m_ProbeSamplingDebugMaterial; // Used to draw probe sampling information (quad with weight, arrow, locator)
        Material m_ProbeSamplingDebugMaterial02; // Used to draw probe sampling information (shaded probes)

        Texture m_displayNumbersTexture;

        internal static ProbeSamplingDebugData probeSamplingDebugData = new ProbeSamplingDebugData();

        Mesh m_DebugOffsetMesh;
        Material m_DebugOffsetMaterial;
        Material m_DebugFragmentationMaterial;
        Plane[] m_DebugFrustumPlanes = new Plane[6];

        // Scenario blending debug data
        GUIContent[] m_DebugScenarioNames = new GUIContent[0];
        int[] m_DebugScenarioValues = new int[0];
        string m_DebugActiveSceneGUID, m_DebugActiveScenario;
        DebugUI.EnumField m_DebugScenarioField;

        // Field used for the realtime subdivision preview
        internal Dictionary<Bounds, ProbeBrickIndex.Brick[]> realtimeSubdivisionInfo = new ();

        bool m_MaxSubdivVisualizedIsMaxAvailable = false;

        /// <summary>
        ///  Render Probe Volume related debug
        /// </summary>
        /// <param name="camera">The <see cref="Camera"/></param>
        public void RenderDebug(Camera camera)
        {
            if (camera.cameraType != CameraType.Reflection && camera.cameraType != CameraType.Preview)
            {
                DrawProbeDebug(camera);
            }
        }

        /// <summary>
        /// Checks if APV sampling debug is enabled
        /// </summary>
        /// <returns>True if APV sampling debug is enabled</returns>
        public bool IsProbeSamplingDebugEnabled()
        {
            return probeSamplingDebugData.update != ProbeSamplingDebugUpdate.Never;
        }

        /// <summary>
        /// Returns the resources used for APV probe sampling debug mode
        /// </summary>
        /// <param name="camera">The camera for which to evaluate the debug mode</param>
        /// <param name="resultBuffer">The buffer that should be filled with position and normal</param>
        /// <param name="coords">The screen space coords to sample the position and normal</param>
        /// <returns>True if the pipeline should write position and normal at coords in resultBuffer</returns>
        public bool GetProbeSamplingDebugResources(Camera camera, out GraphicsBuffer resultBuffer, out Vector2 coords)
        {
            resultBuffer = probeSamplingDebugData.positionNormalBuffer;
            coords = probeSamplingDebugData.coordinates;

#if UNITY_EDITOR
            if (probeSamplingDebugData.camera != camera)
                return false;
#endif

            if (probeSamplingDebugData.update == ProbeSamplingDebugUpdate.Never)
                return false;

            if (probeSamplingDebugData.update == ProbeSamplingDebugUpdate.Once)
            {
                probeSamplingDebugData.update = ProbeSamplingDebugUpdate.Never;
                probeSamplingDebugData.forceScreenCenterCoordinates = false;
            }

            return true;
        }

#if UNITY_EDITOR
        static void SceneGUI(SceneView sceneView)
        {
            // APV debug needs to detect user keyboard and mouse position to update ProbeSamplingPositionDebug
            Event e = Event.current;

            if (e.control && !ProbeReferenceVolume.probeSamplingDebugData.shortcutPressed)
                ProbeReferenceVolume.probeSamplingDebugData.update = ProbeSamplingDebugUpdate.Always;

            if (!e.control && ProbeReferenceVolume.probeSamplingDebugData.shortcutPressed)
                ProbeReferenceVolume.probeSamplingDebugData.update = ProbeSamplingDebugUpdate.Never;

            ProbeReferenceVolume.probeSamplingDebugData.shortcutPressed = e.control;

            if (e.clickCount > 0 && e.button == 0)
            {
                if (ProbeReferenceVolume.probeSamplingDebugData.shortcutPressed)
                    ProbeReferenceVolume.probeSamplingDebugData.update = ProbeSamplingDebugUpdate.Once;
                else
                    ProbeReferenceVolume.probeSamplingDebugData.update = ProbeSamplingDebugUpdate.Never;
            }

            if (ProbeReferenceVolume.probeSamplingDebugData.update == ProbeSamplingDebugUpdate.Never)
                return;

            Vector2 screenCoordinates;

            if (ProbeReferenceVolume.probeSamplingDebugData.forceScreenCenterCoordinates)
                screenCoordinates = new Vector2(sceneView.camera.scaledPixelWidth / 2.0f, sceneView.camera.scaledPixelHeight / 2.0f);
            else
                screenCoordinates = HandleUtility.GUIPointToScreenPixelCoordinate(e.mousePosition);

            if (screenCoordinates.x < 0 || screenCoordinates.x > sceneView.camera.scaledPixelWidth || screenCoordinates.y < 0 || screenCoordinates.y > sceneView.camera.scaledPixelHeight)
                return;

            ProbeReferenceVolume.probeSamplingDebugData.camera = sceneView.camera;
            ProbeReferenceVolume.probeSamplingDebugData.coordinates = screenCoordinates;

            SceneView.currentDrawingSceneView.Repaint(); // useful when 'Always Refresh' is not toggled
        }
#endif

        void InitializeDebug(in ProbeVolumeSystemParameters parameters)
        {
#if UNITY_EDITOR
            SceneView.duringSceneGui += SceneGUI; // Used to get click and keyboard event on scene view for Probe Sampling Debug
#endif
            if (parameters.supportsRuntimeDebug)
            {
                m_DebugMaterial = CoreUtils.CreateEngineMaterial(parameters.probeDebugShader);
                m_DebugMaterial.enableInstancing = true;

                // Probe Sampling Debug Mesh : usefull to show additional information concerning probe sampling for a specific fragment
                // - Arrow : Debug fragment position and normal
                // - Locator : Debug sampling position
                // - 8 Quads : Debug probes weights
                m_DebugProbeSamplingMesh = parameters.probeSamplingDebugMesh;
                m_DebugProbeSamplingMesh.bounds = new Bounds(Vector3.zero, Vector3.one * 9999999.9f); // dirty way of disabling culling (objects spawned at (0.0, 0.0, 0.0) but vertices moved in vertex shader)
                m_ProbeSamplingDebugMaterial = CoreUtils.CreateEngineMaterial(parameters.probeSamplingDebugShader);
                m_ProbeSamplingDebugMaterial02 = CoreUtils.CreateEngineMaterial(parameters.probeDebugShader);
                m_ProbeSamplingDebugMaterial02.enableInstancing = true;

                probeSamplingDebugData.positionNormalBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, 2, System.Runtime.InteropServices.Marshal.SizeOf(typeof(Vector4)));

                m_displayNumbersTexture = parameters.probeSamplingDebugTexture;

                m_DebugOffsetMesh = Resources.GetBuiltinResource<Mesh>("pyramid.fbx");
                m_DebugOffsetMaterial = CoreUtils.CreateEngineMaterial(parameters.offsetDebugShader);
                m_DebugOffsetMaterial.enableInstancing = true;
                m_DebugFragmentationMaterial = CoreUtils.CreateEngineMaterial(parameters.fragmentationDebugShader);

                // Hard-coded colors for now.
                Debug.Assert(ProbeBrickIndex.kMaxSubdivisionLevels == 7); // Update list if this changes.

                subdivisionDebugColors[0] = ProbeVolumeDebugColorPreferences.s_DetailSubdivision;
                subdivisionDebugColors[1] = ProbeVolumeDebugColorPreferences.s_MediumSubdivision;
                subdivisionDebugColors[2] = ProbeVolumeDebugColorPreferences.s_LowSubdivision;
                subdivisionDebugColors[3] = ProbeVolumeDebugColorPreferences.s_VeryLowSubdivision;
                subdivisionDebugColors[4] = ProbeVolumeDebugColorPreferences.s_SparseSubdivision;
                subdivisionDebugColors[5] = ProbeVolumeDebugColorPreferences.s_SparsestSubdivision;
                subdivisionDebugColors[6] = ProbeVolumeDebugColorPreferences.s_DetailSubdivision;
            }

            RegisterDebug(parameters);

#if UNITY_EDITOR
            UnityEditor.Lightmapping.lightingDataCleared += OnClearLightingdata;
#endif
        }

        void CleanupDebug()
        {
            UnregisterDebug(true);
            CoreUtils.Destroy(m_DebugMaterial);
            CoreUtils.Destroy(m_ProbeSamplingDebugMaterial);
            CoreUtils.Destroy(m_ProbeSamplingDebugMaterial02);
            CoreUtils.Destroy(m_DebugOffsetMaterial);
            CoreUtils.Destroy(m_DebugFragmentationMaterial);
            CoreUtils.SafeRelease(probeSamplingDebugData?.positionNormalBuffer);

#if UNITY_EDITOR
            UnityEditor.Lightmapping.lightingDataCleared -= OnClearLightingdata;
            SceneView.duringSceneGui -= SceneGUI;
#endif
        }

        void DebugCellIndexChanged<T>(DebugUI.Field<T> field, T value)
        {
            ClearDebugData();
        }

        void RegisterDebug(ProbeVolumeSystemParameters parameters)
        {
            void RefreshDebug<T>(DebugUI.Field<T> field, T value)
            {
                UnregisterDebug(false);
                RegisterDebug(parameters);
            }

            const float kProbeSizeMin = 0.05f, kProbeSizeMax = 10.0f;
            const float kOffsetSizeMin = 0.001f, kOffsetSizeMax = 0.1f;

            var widgetList = new List<DebugUI.Widget>();

            var subdivContainer = new DebugUI.Container()
            {
                displayName = "Subdivision Visualization",
                isHiddenCallback = () =>
                {
#if UNITY_EDITOR
                    return false;
#else
                    return false; // Cells / Bricks visualization is not implemented in a runtime compatible way atm.
#endif
                }
            };
            subdivContainer.children.Add(new DebugUI.BoolField 
            { 
                displayName = "Display Cells",
                tooltip = "Draw Cells used for loading and streaming.",
                getter = () => probeVolumeDebug.drawCells,
                setter = value => probeVolumeDebug.drawCells = value,
                onValueChanged = RefreshDebug
            });
            subdivContainer.children.Add(new DebugUI.BoolField
            {
                displayName = "Display Bricks",
                tooltip = "Display Subdivision bricks.",
                getter = () => probeVolumeDebug.drawBricks,
                setter = value => probeVolumeDebug.drawBricks = value,
                onValueChanged = RefreshDebug
            });

#if UNITY_EDITOR
            if (probeVolumeDebug.drawCells || probeVolumeDebug.drawBricks)
            {
                subdivContainer.children.Add(new DebugUI.BoolField
                {
                    displayName = "Live Subdivision Preview",
                    tooltip = "Enable a preview of Probe Volume data in the Scene without baking. Can impact Editor performance.",
                    getter = () => probeVolumeDebug.realtimeSubdivision,
                    setter = value => probeVolumeDebug.realtimeSubdivision = value,
                });

                var realtimeSubdivisonChildContainer = new DebugUI.Container()
                {
                    isHiddenCallback = () => !probeVolumeDebug.realtimeSubdivision
                };
                realtimeSubdivisonChildContainer.children.Add(new DebugUI.IntField { displayName = "Cell Updates Per Frame", tooltip = "The number of Cells, bricks, and probe positions updated per frame. Higher numbers can impact Editor performance.", getter = () => probeVolumeDebug.subdivisionCellUpdatePerFrame, setter = value => probeVolumeDebug.subdivisionCellUpdatePerFrame = value, min = () => 1, max = () => 100 });
                realtimeSubdivisonChildContainer.children.Add(new DebugUI.FloatField { displayName = "Update Frequency", tooltip = "Delay in seconds between updates to Cell, Brick, and Probe positions if Live Subdivision Preview is enabled.", getter = () => probeVolumeDebug.subdivisionDelayInSeconds, setter = value => probeVolumeDebug.subdivisionDelayInSeconds = value, min = () => 0.1f, max = () => 10 });
                subdivContainer.children.Add(realtimeSubdivisonChildContainer);
            }
#endif

            subdivContainer.children.Add(new DebugUI.FloatField { displayName = "Debug Draw Distance", tooltip = "How far from the Scene Camera to draw debug visualization for Cells and Bricks. Large distances can impact Editor performance.", getter = () => probeVolumeDebug.subdivisionViewCullingDistance, setter = value => probeVolumeDebug.subdivisionViewCullingDistance = value, min = () => 0.0f });

            var probeContainer = new DebugUI.Container() { displayName = "Probe Visualization" };
            probeContainer.children.Add(new DebugUI.BoolField { displayName = "Display Probes", tooltip = "Render the debug view showing probe positions. Use the shading mode to determine which type of lighting data to visualize.", getter = () => probeVolumeDebug.drawProbes, setter = value => probeVolumeDebug.drawProbes = value, onValueChanged = RefreshDebug });
            if (probeVolumeDebug.drawProbes)
            {
                var probeContainerChildren = new DebugUI.Container();
                probeContainerChildren.children.Add(new DebugUI.EnumField
                {
                    displayName = "Probe Shading Mode",
                    tooltip = "Choose which lighting data to show in the probe debug visualization.",
                    getter = () => (int)probeVolumeDebug.probeShading,
                    setter = value => probeVolumeDebug.probeShading = (DebugProbeShadingMode)value,
                    autoEnum = typeof(DebugProbeShadingMode),
                    getIndex = () => (int)probeVolumeDebug.probeShading,
                    setIndex = value => probeVolumeDebug.probeShading = (DebugProbeShadingMode)value,
                    onValueChanged = RefreshDebug
                });
                probeContainerChildren.children.Add(new DebugUI.FloatField { displayName = "Debug Size", tooltip = "The size of probes shown in the debug view.", getter = () => probeVolumeDebug.probeSize, setter = value => probeVolumeDebug.probeSize = value, min = () => kProbeSizeMin, max = () => kProbeSizeMax });
                if (probeVolumeDebug.probeShading == DebugProbeShadingMode.SH || probeVolumeDebug.probeShading == DebugProbeShadingMode.SHL0 || probeVolumeDebug.probeShading == DebugProbeShadingMode.SHL0L1)
                    probeContainerChildren.children.Add(new DebugUI.FloatField { displayName = "Exposure Compensation", tooltip = "Modify the brightness of probe visualizations. Decrease this number to make very bright probes more visible.", getter = () => probeVolumeDebug.exposureCompensation, setter = value => probeVolumeDebug.exposureCompensation = value });

                probeContainerChildren.children.Add(new DebugUI.IntField
                {
                    displayName = "Max Subdivisions Displayed",
                    tooltip = "The highest (most dense) probe subdivision level displayed in the debug view.",
                    getter = () => probeVolumeDebug.maxSubdivToVisualize,
                    setter = (v) => probeVolumeDebug.maxSubdivToVisualize = Mathf.Min(v, ProbeReferenceVolume.instance.GetMaxSubdivision() - 1),
                    min = () => 0,
                    max = () => ProbeReferenceVolume.instance.GetMaxSubdivision() - 1,
                });

                probeContainerChildren.children.Add(new DebugUI.IntField
                {
                    displayName = "Min Subdivisions Displayed",
                    tooltip = "The lowest (least dense) probe subdivision level displayed in the debug view.",
                    getter = () => probeVolumeDebug.minSubdivToVisualize,
                    setter = (v) => probeVolumeDebug.minSubdivToVisualize = Mathf.Max(v, 0),
                    min = () => 0,
                    max = () => ProbeReferenceVolume.instance.GetMaxSubdivision() - 1,
                });


                probeContainer.children.Add(probeContainerChildren);
            }

            probeContainer.children.Add(new DebugUI.BoolField
            {
                displayName = "Debug Probe Sampling",
                tooltip = "Render the debug view displaying how probes are sampled for a selected pixel. Use the viewport overlay 'SelectPixel' button or Ctrl+Click on the viewport to select the debugged pixel",
                getter = () => probeVolumeDebug.drawProbeSamplingDebug,
                setter = value =>
                {
                    probeVolumeDebug.drawProbeSamplingDebug = value;
                    probeSamplingDebugData.update = ProbeSamplingDebugUpdate.Once;
                    probeSamplingDebugData.forceScreenCenterCoordinates = true;
                },
                onValueChanged = RefreshDebug
            });

            if (probeVolumeDebug.drawProbeSamplingDebug)
            {
                var probeContainerChildren = new DebugUI.Container();
                probeContainerChildren.children.Add(new DebugUI.FloatField { displayName = "Debug Size", tooltip = "The size of gizmos shown in the debug view.", getter = () => probeVolumeDebug.probeSamplingDebugSize, setter = value => probeVolumeDebug.probeSamplingDebugSize = value, min = () => kProbeSizeMin, max = () => kProbeSizeMax });
                probeContainerChildren.children.Add(new DebugUI.BoolField { displayName = "Debug With Sampling Noise", tooltip = "Enable Sampling Noise for this debug view. It should be enabled for accuracy but it can make results more difficult to read", getter = () => probeVolumeDebug.debugWithSamplingNoise, setter = value =>{probeVolumeDebug.debugWithSamplingNoise = value;}, onValueChanged = RefreshDebug });
                probeContainer.children.Add(probeContainerChildren);
            }

            probeContainer.children.Add(new DebugUI.BoolField
            {
                displayName = "Virtual Offset Debug",
                tooltip = "Enable Virtual Offset debug visualization. Indicates the offsets applied to probe positions. These are used to capture lighting when probes are considered invalid.",
                getter = () => probeVolumeDebug.drawVirtualOffsetPush,
                setter = value =>
                {
                    probeVolumeDebug.drawVirtualOffsetPush = value;

                    if (probeVolumeDebug.drawVirtualOffsetPush && probeVolumeDebug.drawProbes)
                    {
                        // If probes are being drawn when enabling offset, automatically scale them down to a reasonable size so the arrows aren't obscured by the probes.
                        var searchDistance = CellSize(0) * MinBrickSize() / ProbeBrickPool.kBrickCellCount * m_CurrentBakingSet.settings.virtualOffsetSettings.searchMultiplier + m_CurrentBakingSet.settings.virtualOffsetSettings.outOfGeoOffset;
                        probeVolumeDebug.probeSize = Mathf.Min(probeVolumeDebug.probeSize, Mathf.Clamp(searchDistance, kProbeSizeMin, kProbeSizeMax));
                    }
                },
                onValueChanged = RefreshDebug
            });
            if (probeVolumeDebug.drawVirtualOffsetPush)
            {
                var voOffset = new DebugUI.FloatField { displayName = "Debug Size", tooltip = "Modify the size of the arrows used in the virtual offset debug visualization.", getter = () => probeVolumeDebug.offsetSize, setter = value => probeVolumeDebug.offsetSize = value, min = () => kOffsetSizeMin, max = () => kOffsetSizeMax };
                probeContainer.children.Add(new DebugUI.Container { children = { voOffset } });
            }

            probeContainer.children.Add(new DebugUI.FloatField { displayName = "Debug Draw Distance", tooltip = "How far from the Scene Camera to draw probe debug visualizations. Large distances can impact Editor performance.", getter = () => probeVolumeDebug.probeCullingDistance, setter = value => probeVolumeDebug.probeCullingDistance = value, min = () => 0.0f });

            if (parameters.supportsRuntimeDebug)
            {
                // Cells / Bricks visualization is not implemented in a runtime compatible way atm.
                if (Application.isEditor)
                    widgetList.Add(subdivContainer);

                widgetList.Add(probeContainer);
            }

            var streamingContainer = new DebugUI.Container() { displayName = "Streaming" };
            streamingContainer.children.Add(new DebugUI.BoolField { displayName = "Freeze Streaming", tooltip = "Stop Unity from streaming probe data in or out of GPU memory.", getter = () => probeVolumeDebug.freezeStreaming, setter = value => probeVolumeDebug.freezeStreaming = value });
            streamingContainer.children.Add(new DebugUI.BoolField { displayName = "Display Streaming Score", getter = () => probeVolumeDebug.displayCellStreamingScore, setter = value => probeVolumeDebug.displayCellStreamingScore = value });
            streamingContainer.children.Add(new DebugUI.BoolField { displayName = "Maximum cell streaming", tooltip = "Enable streaming as many cells as possible every frame.", getter = () => instance.loadMaxCellsPerFrame, setter = value => instance.loadMaxCellsPerFrame = value, onValueChanged = RefreshDebug });
            if (!instance.loadMaxCellsPerFrame)
            {
                var maxCellStreamingContainerChildren = new DebugUI.Container();
                maxCellStreamingContainerChildren.children.Add(new DebugUI.IntField { displayName = "Loaded Cells Per Frame", tooltip = "Determines the maximum number of Cells Unity streams per frame. Loading more Cells per frame can impact performance.", getter = () => instance.numberOfCellsLoadedPerFrame, setter = value => instance.SetNumberOfCellsLoadedPerFrame(value), min = () => 1, max = () => kMaxCellLoadedPerFrame });
                streamingContainer.children.Add(maxCellStreamingContainerChildren);
            }
            // Those are mostly for internal dev purpose.
            if (Debug.isDebugBuild)
            {
                streamingContainer.children.Add(new DebugUI.BoolField { displayName = "Display Index Fragmentation", getter = () => probeVolumeDebug.displayIndexFragmentation, setter = value => probeVolumeDebug.displayIndexFragmentation = value });
                var indexDefragContainerChildren = new DebugUI.Container();
                indexDefragContainerChildren.children.Add(new DebugUI.Value { displayName = "Index Fragmentation Rate", getter = () => instance.indexFragmentationRate });
                streamingContainer.children.Add(indexDefragContainerChildren);
                streamingContainer.children.Add(new DebugUI.BoolField { displayName = "Verbose Log", getter = () => probeVolumeDebug.verboseStreamingLog, setter = value => probeVolumeDebug.verboseStreamingLog = value });
            }

            if (parameters.supportGPUStreaming || parameters.supportDiskStreaming)
            {
                widgetList.Add(streamingContainer);
            }

            if (parameters.supportScenarios && parameters.scenarioBlendingShader != null && parameters.blendingMemoryBudget != 0 && m_CurrentBakingSet != null)
            {
                var blendingContainer = new DebugUI.Container() { displayName = "Scenario Blending" };
                blendingContainer.children.Add(new DebugUI.IntField { displayName = "Number Of Cells Blended Per Frame", getter = () => instance.numberOfCellsBlendedPerFrame, setter = value => instance.numberOfCellsBlendedPerFrame = value, min = () => 0 });
                blendingContainer.children.Add(new DebugUI.FloatField { displayName = "Turnover Rate", getter = () => instance.turnoverRate, setter = value => instance.turnoverRate = value, min = () => 0, max = () => 1 });

                void RefreshScenarioNames(string guid)
                {
                    HashSet<string> allScenarios = new();
                    foreach (var set in Resources.FindObjectsOfTypeAll<ProbeVolumeBakingSet>())
                    {
                        if (!set.sceneGUIDs.Contains(guid))
                            continue;
                        foreach (var scenario in set.lightingScenarios)
                            allScenarios.Add(scenario);
                    }

                    allScenarios.Remove(m_CurrentBakingSet.lightingScenario);
                    if (m_DebugActiveSceneGUID == guid && allScenarios.Count + 1 == m_DebugScenarioNames.Length && m_DebugActiveScenario == m_CurrentBakingSet.lightingScenario)
                        return;

                    int i = 0;
                    ArrayExtensions.ResizeArray(ref m_DebugScenarioNames, allScenarios.Count + 1);
                    ArrayExtensions.ResizeArray(ref m_DebugScenarioValues, allScenarios.Count + 1);
                    m_DebugScenarioNames[0] = new GUIContent("None");
                    m_DebugScenarioValues[0] = 0;
                    foreach (var scenario in allScenarios)
                    {
                        i++;
                        m_DebugScenarioNames[i] = new GUIContent(scenario);
                        m_DebugScenarioValues[i] = i;
                    }

                    m_DebugActiveSceneGUID = guid;
                    m_DebugActiveScenario = m_CurrentBakingSet.lightingScenario;
                    m_DebugScenarioField.enumNames = m_DebugScenarioNames;
                    m_DebugScenarioField.enumValues = m_DebugScenarioValues;
                    if (probeVolumeDebug.otherStateIndex >= m_DebugScenarioNames.Length)
                        probeVolumeDebug.otherStateIndex = 0;
                }

                m_DebugScenarioField = new DebugUI.EnumField
                {
                    displayName = "Scenario Blend Target",
                    tooltip = "Select another lighting scenario to blend with the active lighting scenario.",
                    enumNames = m_DebugScenarioNames,
                    enumValues = m_DebugScenarioValues,
                    getIndex = () =>
                    {
                        RefreshScenarioNames(ProbeVolumeSceneData.GetSceneGUID(SceneManagement.SceneManager.GetActiveScene()));

                        probeVolumeDebug.otherStateIndex = 0;
                        if (!string.IsNullOrEmpty(m_CurrentBakingSet.otherScenario))
                        {
                            for (int i = 1; i < m_DebugScenarioNames.Length; i++)
                            {
                                if (m_DebugScenarioNames[i].text == m_CurrentBakingSet.otherScenario)
                                {
                                    probeVolumeDebug.otherStateIndex = i;
                                    break;
                                }
                            }
                        }
                        return probeVolumeDebug.otherStateIndex;
                    },
                    setIndex = value =>
                    {
                        string other = value == 0 ? null : m_DebugScenarioNames[value].text;
                        m_CurrentBakingSet.BlendLightingScenario(other, m_CurrentBakingSet.scenarioBlendingFactor);
                        probeVolumeDebug.otherStateIndex = value;
                    },
                    getter = () => probeVolumeDebug.otherStateIndex,
                    setter = (value) => probeVolumeDebug.otherStateIndex = value,
                };

                blendingContainer.children.Add(m_DebugScenarioField);
                blendingContainer.children.Add(new DebugUI.FloatField { displayName = "Scenario Blending Factor", tooltip = "Blend between lighting scenarios by adjusting this slider.", getter = () => instance.scenarioBlendingFactor, setter = value => instance.scenarioBlendingFactor = value, min = () => 0.0f, max = () => 1.0f });

                widgetList.Add(blendingContainer);
            }

            if (widgetList.Count > 0)
            {
                m_DebugItems = widgetList.ToArray();
                var panel = DebugManager.instance.GetPanel(k_DebugPanelName, true);
                panel.children.Add(m_DebugItems);
            }

            DebugManager debugManager = DebugManager.instance;
            debugManager.RegisterData(probeVolumeDebug);
        }

        void UnregisterDebug(bool destroyPanel)
        {
            if (destroyPanel)
                DebugManager.instance.RemovePanel(k_DebugPanelName);
            else
                DebugManager.instance.GetPanel(k_DebugPanelName, false).children.Remove(m_DebugItems);
        }

        class RenderFragmentationOverlayPassData
        {
            public Material debugFragmentationMaterial;
            public Rendering.DebugOverlay debugOverlay;
            public int chunkCount;
            public ComputeBuffer debugFragmentationData;
            public TextureHandle colorBuffer;
            public TextureHandle depthBuffer;
        }

        /// <summary>
        /// Render a debug view showing fragmentation of the GPU memory.
        /// </summary>
        /// <param name="renderGraph"></param>
        /// <param name="colorBuffer"></param>
        /// <param name="depthBuffer"></param>
        /// <param name="debugOverlay"></param>
        public void RenderFragmentationOverlay(RenderGraph renderGraph, TextureHandle colorBuffer, TextureHandle depthBuffer, DebugOverlay debugOverlay)
        {
            if (!m_ProbeReferenceVolumeInit || !probeVolumeDebug.displayIndexFragmentation)
                return;

            using (var builder = renderGraph.AddRenderPass<RenderFragmentationOverlayPassData>("APVFragmentationOverlay", out var passData))
            {
                passData.debugOverlay = debugOverlay;
                passData.debugFragmentationMaterial = m_DebugFragmentationMaterial;
                passData.colorBuffer = builder.UseColorBuffer(colorBuffer, 0);
                passData.depthBuffer = builder.UseDepthBuffer(depthBuffer, DepthAccess.ReadWrite);
                passData.debugFragmentationData = m_Index.GetDebugFragmentationBuffer();
                passData.chunkCount = passData.debugFragmentationData.count;

                builder.SetRenderFunc(
                    (RenderFragmentationOverlayPassData data, RenderGraphContext ctx) =>
                    {
                        var mpb = ctx.renderGraphPool.GetTempMaterialPropertyBlock();

                        data.debugOverlay.SetViewport(ctx.cmd);
                        mpb.SetInt("_ChunkCount", data.chunkCount);
                        mpb.SetBuffer("_DebugFragmentation", data.debugFragmentationData);
                        ctx.cmd.DrawProcedural(Matrix4x4.identity, data.debugFragmentationMaterial, 0, MeshTopology.Triangles, 3, 1, mpb);
                        data.debugOverlay.Next();
                    });
            }
        }

        bool ShouldCullCell(Vector3 cellPosition, Transform cameraTransform, Plane[] frustumPlanes)
        {
            var cellSize = MaxBrickSize();
            var originWS = GetTransform().posWS;
            Vector3 cellCenterWS = cellPosition * cellSize + originWS + Vector3.one * (cellSize / 2.0f);

            // We do coarse culling with cell, finer culling later.
            float distanceRoundedUpWithCellSize = Mathf.CeilToInt(probeVolumeDebug.probeCullingDistance / cellSize) * cellSize;

            if (Vector3.Distance(cameraTransform.position, cellCenterWS) > distanceRoundedUpWithCellSize)
                return true;

            var volumeAABB = new Bounds(cellCenterWS, cellSize * Vector3.one);
            return !GeometryUtility.TestPlanesAABB(frustumPlanes, volumeAABB);
        }

        void DrawProbeDebug(Camera camera)
        {
            if (!enabledBySRP || !isInitialized)
                return;

            if (!probeVolumeDebug.drawProbes && !probeVolumeDebug.drawVirtualOffsetPush &&!probeVolumeDebug.drawProbeSamplingDebug)
                return;

            GeometryUtility.CalculateFrustumPlanes(camera, m_DebugFrustumPlanes);

            m_DebugMaterial.shaderKeywords = null;
            if (m_SHBands == ProbeVolumeSHBands.SphericalHarmonicsL1)
                m_DebugMaterial.EnableKeyword("PROBE_VOLUMES_L1");
            else if (m_SHBands == ProbeVolumeSHBands.SphericalHarmonicsL2)
                m_DebugMaterial.EnableKeyword("PROBE_VOLUMES_L2");

            // This is to force the rendering not to draw to the depth pre pass and still behave.
            // They are going to be rendered opaque anyhow, just using the transparent render queue to make sure
            // they properly behave w.r.t fog.
            m_DebugMaterial.renderQueue = (int)RenderQueue.Transparent;
            m_DebugOffsetMaterial.renderQueue = (int)RenderQueue.Transparent;
            m_ProbeSamplingDebugMaterial.renderQueue = (int)RenderQueue.Transparent;
            m_ProbeSamplingDebugMaterial02.renderQueue = (int)RenderQueue.Transparent;

            if (probeVolumeDebug.drawProbeSamplingDebug)
            {
                m_ProbeSamplingDebugMaterial.SetInt("_ShadingMode", (int)probeVolumeDebug.probeShading);
                m_ProbeSamplingDebugMaterial.SetVector("_DebugArrowColor", new Vector4(1.0f, 1.0f, 1.0f, 1.0f));
                m_ProbeSamplingDebugMaterial.SetVector("_DebugLocator01Color", new Vector4(1.0f, 1.0f, 1.0f, 1.0f));
                m_ProbeSamplingDebugMaterial.SetVector("_DebugLocator02Color", new Vector4(0.3f, 0.3f, 0.3f, 1.0f));
                m_ProbeSamplingDebugMaterial.SetFloat("_ProbeSize", probeVolumeDebug.probeSamplingDebugSize);
                m_ProbeSamplingDebugMaterial.SetTexture("_NumbersTex", m_displayNumbersTexture);
                m_ProbeSamplingDebugMaterial.SetInt("_DebugSamplingNoise", Convert.ToInt32(probeVolumeDebug.debugWithSamplingNoise));
                m_ProbeSamplingDebugMaterial.SetInt("_ForceDebugNormalViewBias", 0); // Add a secondary locator to show intermediate position (with no Anti-Leak) when Anti-Leak is active

                m_ProbeSamplingDebugMaterial.SetBuffer("_positionNormalBuffer", probeSamplingDebugData.positionNormalBuffer);

                Graphics.DrawMesh(m_DebugProbeSamplingMesh, new Vector4(0.0f, 0.0f, 0.0f, 1.0f), Quaternion.identity, m_ProbeSamplingDebugMaterial, 0, camera);
                Graphics.ClearRandomWriteTargets();
            }

            // Sanitize the min max subdiv levels with what is available
            int minAvailableSubdiv = ProbeReferenceVolume.instance.cells.Count > 0 ? ProbeReferenceVolume.instance.GetMaxSubdivision()-1 : 0;
            foreach (var cell in ProbeReferenceVolume.instance.cells.Values)
            {
                minAvailableSubdiv = Mathf.Min(minAvailableSubdiv, cell.desc.minSubdiv);
            }

            probeVolumeDebug.maxSubdivToVisualize = Mathf.Min(probeVolumeDebug.maxSubdivToVisualize, ProbeReferenceVolume.instance.GetMaxSubdivision() - 1);
            m_MaxSubdivVisualizedIsMaxAvailable = probeVolumeDebug.maxSubdivToVisualize == ProbeReferenceVolume.instance.GetMaxSubdivision() - 1;
            probeVolumeDebug.minSubdivToVisualize = Mathf.Clamp(probeVolumeDebug.minSubdivToVisualize, minAvailableSubdiv, probeVolumeDebug.maxSubdivToVisualize);

            foreach (var cell in ProbeReferenceVolume.instance.cells.Values)
            {
                if (ShouldCullCell(cell.desc.position, camera.transform, m_DebugFrustumPlanes))
                    continue;

                var debug = CreateInstancedProbes(cell);

                if (debug == null)
                    continue;

                for (int i = 0; i < debug.probeBuffers.Count; ++i)
                {
                    var props = debug.props[i];
                    props.SetInt("_ShadingMode", (int)probeVolumeDebug.probeShading);
                    props.SetFloat("_ExposureCompensation", probeVolumeDebug.exposureCompensation);
                    props.SetFloat("_ProbeSize", probeVolumeDebug.probeSize);
                    props.SetFloat("_CullDistance", probeVolumeDebug.probeCullingDistance);
                    props.SetInt("_MaxAllowedSubdiv", probeVolumeDebug.maxSubdivToVisualize);
                    props.SetInt("_MinAllowedSubdiv", probeVolumeDebug.minSubdivToVisualize);
                    props.SetFloat("_ValidityThreshold", m_CurrentBakingSet.settings.dilationSettings.dilationValidityThreshold);
                    props.SetFloat("_OffsetSize", probeVolumeDebug.offsetSize);

                    if (probeVolumeDebug.drawProbes)
                    {
                        var probeBuffer = debug.probeBuffers[i];
                        m_DebugMaterial.SetInt("_DebugProbeVolumeSampling", 0);
                        m_DebugMaterial.SetBuffer("_positionNormalBuffer", probeSamplingDebugData.positionNormalBuffer);
                        Graphics.DrawMeshInstanced(debugMesh, 0, m_DebugMaterial, probeBuffer, probeBuffer.Length, props, ShadowCastingMode.Off, false, 0, camera, LightProbeUsage.Off, null);
                    }

                    if (probeVolumeDebug.drawProbeSamplingDebug)
                    {
                        var probeBuffer = debug.probeBuffers[i];
                        m_ProbeSamplingDebugMaterial02.SetInt("_DebugProbeVolumeSampling", 1);
                        props.SetFloat("_ProbeSize", probeVolumeDebug.probeSamplingDebugSize);
                        props.SetInt("_DebugSamplingNoise", Convert.ToInt32(probeVolumeDebug.debugWithSamplingNoise));
                        m_ProbeSamplingDebugMaterial02.SetBuffer("_positionNormalBuffer", probeSamplingDebugData.positionNormalBuffer);
                        Graphics.DrawMeshInstanced(debugMesh, 0, m_ProbeSamplingDebugMaterial02, probeBuffer, probeBuffer.Length, props, ShadowCastingMode.Off, false, 0, camera, LightProbeUsage.Off, null);
                    }

                    if (probeVolumeDebug.drawVirtualOffsetPush)
                    {
                        var offsetBuffer = debug.offsetBuffers[i];
                        Graphics.DrawMeshInstanced(m_DebugOffsetMesh, 0, m_DebugOffsetMaterial, offsetBuffer, offsetBuffer.Length, props, ShadowCastingMode.Off, false, 0, camera, LightProbeUsage.Off, null);
                    }
                }
            }
        }

        internal void ResetDebugViewToMaxSubdiv()
        {
            if (m_MaxSubdivVisualizedIsMaxAvailable)
                probeVolumeDebug.maxSubdivToVisualize = ProbeReferenceVolume.instance.GetMaxSubdivision() - 1;
        }

        void ClearDebugData()
        {
            realtimeSubdivisionInfo.Clear();
        }

        CellInstancedDebugProbes CreateInstancedProbes(Cell cell)
        {
            if (cell.debugProbes != null)
                return cell.debugProbes;

            int maxSubdiv = ProbeReferenceVolume.instance.GetMaxSubdivision() - 1;

            if (!cell.data.bricks.IsCreated || cell.data.bricks.Length == 0 || !cell.data.probePositions.IsCreated || !cell.loaded)
                return null;

            List<Matrix4x4[]> probeBuffers = new List<Matrix4x4[]>();
            List<Matrix4x4[]> offsetBuffers = new List<Matrix4x4[]>();
            List<MaterialPropertyBlock> props = new List<MaterialPropertyBlock>();
            var chunks = cell.poolInfo.chunkList;

            Vector4[] texels = new Vector4[kProbesPerBatch];
            float[] validity = new float[kProbesPerBatch];
            float[] dilationThreshold = new float[kProbesPerBatch];
            float[] relativeSize = new float[kProbesPerBatch];
            float[] touchupUpVolumeAction = cell.data.touchupVolumeInteraction.Length > 0 ? new float[kProbesPerBatch] : null;
            Vector4[] offsets = cell.data.offsetVectors.Length > 0 ? new Vector4[kProbesPerBatch] : null;

            List<Matrix4x4> probeBuffer = new List<Matrix4x4>();
            List<Matrix4x4> offsetBuffer = new List<Matrix4x4>();

            var debugData = new CellInstancedDebugProbes();
            debugData.probeBuffers = probeBuffers;
            debugData.offsetBuffers = offsetBuffers;
            debugData.props = props;

            var chunkSizeInProbes = ProbeBrickPool.GetChunkSizeInProbeCount();

            var loc = ProbeBrickPool.ProbeCountToDataLocSize(chunkSizeInProbes);

            float baseThreshold = m_CurrentBakingSet.settings.dilationSettings.dilationValidityThreshold;
            int idxInBatch = 0;
            int globalIndex = 0;
            int brickCount = cell.desc.probeCount / ProbeBrickPool.kBrickProbeCountTotal;
            int bx = 0, by = 0, bz = 0;
            for (int brickIndex = 0; brickIndex < brickCount; ++brickIndex)
            {
                Debug.Assert(bz < loc.z);

                int brickSize = cell.data.bricks[brickIndex].subdivisionLevel;
                int chunkIndex = brickIndex / ProbeBrickPool.GetChunkSizeInBrickCount();
                var chunk = chunks[chunkIndex];
                Vector3Int brickStart = new Vector3Int(chunk.x + bx, chunk.y + by, chunk.z + bz);

                for (int z = 0; z < ProbeBrickPool.kBrickProbeCountPerDim; ++z)
                {
                    for (int y = 0; y < ProbeBrickPool.kBrickProbeCountPerDim; ++y)
                    {
                        for (int x = 0; x < ProbeBrickPool.kBrickProbeCountPerDim; ++x)
                        {
                            Vector3Int texelLoc = new Vector3Int(brickStart.x + x, brickStart.y + y, brickStart.z + z);

                            int probeFlatIndex = chunkIndex * chunkSizeInProbes + (bx + x) + loc.x * ((by + y) + loc.y * (bz + z));

                            probeBuffer.Add(Matrix4x4.TRS(cell.data.probePositions[probeFlatIndex], Quaternion.identity, Vector3.one * (0.3f * (brickSize + 1))));
                            validity[idxInBatch] = cell.data.validity[probeFlatIndex];
                            dilationThreshold[idxInBatch] =  baseThreshold;
                            texels[idxInBatch] = new Vector4(texelLoc.x, texelLoc.y, texelLoc.z, brickSize);
                            relativeSize[idxInBatch] = (float)brickSize / (float)maxSubdiv;

                            if (touchupUpVolumeAction != null)
                            {
                                touchupUpVolumeAction[idxInBatch] = cell.data.touchupVolumeInteraction[probeFlatIndex];
                                dilationThreshold[idxInBatch] = touchupUpVolumeAction[idxInBatch] > 1.0f ? touchupUpVolumeAction[idxInBatch] - 1.0f : baseThreshold;
                            }

                            if (offsets != null)
                            {
                                const float kOffsetThresholdSqr = 1e-6f;

                                var offset = cell.data.offsetVectors[probeFlatIndex];
                                offsets[idxInBatch] = offset;

                                if (offset.sqrMagnitude < kOffsetThresholdSqr)
                                {
                                    offsetBuffer.Add(Matrix4x4.identity);
                                }
                                else
                                {
                                    var position = cell.data.probePositions[probeFlatIndex] + offset;
                                    var orientation = Quaternion.LookRotation(-offset);
                                    var scale = new Vector3(0.5f, 0.5f, offset.magnitude);
                                    offsetBuffer.Add(Matrix4x4.TRS(position, orientation, scale));
                                }
                            }
                            idxInBatch++;

                            if (probeBuffer.Count >= kProbesPerBatch || globalIndex == cell.desc.probeCount - 1)
                            {
                                idxInBatch = 0;
                                MaterialPropertyBlock prop = new MaterialPropertyBlock();

                                prop.SetFloatArray("_Validity", validity);
                                prop.SetFloatArray("_DilationThreshold", dilationThreshold);
                                prop.SetFloatArray("_TouchupedByVolume", touchupUpVolumeAction);
                                prop.SetFloatArray("_RelativeSize", relativeSize);
                                prop.SetVectorArray("_IndexInAtlas", texels);

                                if (offsets != null)
                                    prop.SetVectorArray("_Offset", offsets);

                                props.Add(prop);

                                probeBuffers.Add(probeBuffer.ToArray());
                                probeBuffer = new List<Matrix4x4>();
                                probeBuffer.Clear();

                                offsetBuffers.Add(offsetBuffer.ToArray());
                                offsetBuffer.Clear();
                            }

                            globalIndex++;
                        }
                    }
                }

                bx += ProbeBrickPool.kBrickProbeCountPerDim;
                if (bx >= loc.x)
                {
                    bx = 0;
                    by += ProbeBrickPool.kBrickProbeCountPerDim;
                    if (by >= loc.y)
                    {
                        by = 0;
                        bz += ProbeBrickPool.kBrickProbeCountPerDim;
                        if (bz >= loc.z)
                        {
                            bx = 0;
                            by = 0;
                            bz = 0;
                        }
                    }
                }
            }

            cell.debugProbes = debugData;

            return debugData;
        }

        void OnClearLightingdata()
        {
            ClearDebugData();
        }
    }
}
