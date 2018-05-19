using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine.Experimental.Rendering.HDPipeline.Attributes;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    [GenerateHLSL]
    public enum FullScreenDebugMode
    {
        None,

        // Lighting
        MinLightingFullScreenDebug,
        SSAO,
        DeferredShadows,
        PreRefractionColorPyramid,
        DepthPyramid,
        FinalColorPyramid,
        ScreenSpaceTracing,
        MaxLightingFullScreenDebug,

        // Rendering
        MinRenderingFullScreenDebug,
        MotionVectors,
        NanTracker,
        MaxRenderingFullScreenDebug
    }

    [GenerateHLSL]
    public struct ScreenSpaceTracingDebug
    {
                                                        // Used to debug SSRay model
        // 1x32 bits
        public Lit.RefractionSSRayModel tracingModel;

        // 6x32 bits
        public uint loopStartPositionSSX;                           // Proxy, HiZ
        public uint loopStartPositionSSY;                           // Proxy, HiZ
        public float loopStartLinearDepth;                          // Proxy, HiZ
        public Vector3 loopRayDirectionSS;                          // HiZ
        public uint loopMipLevelMax;                                // HiZ
        public uint loopIterationMax;                               // HiZ

        // 9x32 bits
        public Vector3 iterationPositionSS;                         // HiZ
        public uint iterationMipLevel;                              // HiZ
        public uint iteration;                                      // HiZ
        public float iterationLinearDepthBuffer;                    // HiZ
        public Lit.HiZIntersectionKind iterationIntersectionKind;   // HiZ
        public uint iterationCellSizeW;                             // HiZ
        public uint iterationCellSizeH;                             // HiZ
        public EnvShapeType proxyShapeType;                         // Proxy
        public float projectionDistance;                            // Proxy

        // 4x32 bits
        public bool endHitSuccess;                                  // Proxy, HiZ
        public float endLinearDepth;                                // Proxy, HiZ
        public uint endPositionSSX;                                 // Proxy, HiZ
        public uint endPositionSSY;                                 // Proxy, HiZ

        // 0x32 bits (padding)

        public Vector2 loopStartPositionSS { get { return new Vector2(loopStartPositionSSX, loopStartPositionSSY); } }
        public Vector2 endPositionSS { get { return new Vector2(endPositionSSX, endPositionSSY); } }
        public Vector2 iterationCellId { get { return new Vector2(((int)iterationPositionSS.x) >> (int)iterationMipLevel, ((int)iterationPositionSS.y) >> (int)iterationMipLevel); } }
        public Vector2 iterationCellSize { get { return new Vector2(iterationCellSizeW, iterationCellSizeH); } }
    }

    public class DebugDisplaySettings
    {
        public static string k_PanelDisplayStats = "Display Stats";
        public static string k_PanelMaterials = "Material";
        public static string k_PanelLighting = "Lighting";
        public static string k_PanelRendering = "Rendering";

        public static string k_PanelScreenSpaceTracing = "Screen Space Tracing";
        public static string k_PanelDecals = "Decals";

        //static readonly string[] k_HiZIntersectionKind = { "None", "Depth", "Cell" };

        DebugUI.Widget[] m_DebugDisplayStatsItems;
        DebugUI.Widget[] m_DebugMaterialItems;
        DebugUI.Widget[] m_DebugLightingItems;
        DebugUI.Widget[] m_DebugRenderingItems;
        DebugUI.Widget[] m_DebugScreenSpaceTracingItems;
        DebugUI.Widget[] m_DebugDecalsItems;


        public float debugOverlayRatio = 0.33f;
        public FullScreenDebugMode  fullScreenDebugMode = FullScreenDebugMode.None;
        public float fullscreenDebugMip = 0.0f;
        public bool showSSRayGrid = true;
        public bool showSSRayDepthPyramid = true;

        public MaterialDebugSettings materialDebugSettings = new MaterialDebugSettings();
        public LightingDebugSettings lightingDebugSettings = new LightingDebugSettings();
        public MipMapDebugSettings mipMapDebugSettings = new MipMapDebugSettings();
        public ColorPickerDebugSettings colorPickerDebugSettings = new ColorPickerDebugSettings();
        public DecalsDebugSettings decalsDebugSettings = new DecalsDebugSettings();

        public static GUIContent[] lightingFullScreenDebugStrings = null;
        public static int[] lightingFullScreenDebugValues = null;
        public static GUIContent[] renderingFullScreenDebugStrings = null;
        public static int[] renderingFullScreenDebugValues = null;
        public static GUIContent[] debugScreenSpaceTracingProxyStrings = null;
        public static int[] debugScreenSpaceTracingProxyValues = null;
        public static GUIContent[] debugScreenSpaceTracingHiZStrings = null;
        public static int[] debugScreenSpaceTracingHiZValues = null;

        Lit.RefractionSSRayModel m_LastSSRayModel = Lit.RefractionSSRayModel.None;
        ScreenSpaceTracingDebug m_ScreenSpaceTracingDebugData;
        public ScreenSpaceTracingDebug screenSpaceTracingDebugData
        {
            get { return m_ScreenSpaceTracingDebugData; }
            internal set
            {
                m_ScreenSpaceTracingDebugData = value;
                if (m_LastSSRayModel != m_ScreenSpaceTracingDebugData.tracingModel)
                {
                    m_LastSSRayModel = m_ScreenSpaceTracingDebugData.tracingModel;
                    RefreshScreenSpaceTracingDebug<Lit.RefractionSSRayModel>(null, m_LastSSRayModel);
                }

                if (m_ScreenSpaceTracingDebugData.tracingModel != Lit.RefractionSSRayModel.HiZ)
                {
                    showSSRayDepthPyramid = false;
                    showSSRayGrid = false;
                }
            }
        }

        public DebugDisplaySettings()
        {
            FillFullScreenDebugEnum(ref lightingFullScreenDebugStrings, ref lightingFullScreenDebugValues, FullScreenDebugMode.MinLightingFullScreenDebug, FullScreenDebugMode.MaxLightingFullScreenDebug);
            FillFullScreenDebugEnum(ref renderingFullScreenDebugStrings, ref renderingFullScreenDebugValues, FullScreenDebugMode.MinRenderingFullScreenDebug, FullScreenDebugMode.MaxRenderingFullScreenDebug);

            var debugScreenSpaceTracingStrings = Enum.GetNames(typeof(DebugScreenSpaceTracing))
                .Select(s => new GUIContent(s))
                .ToArray();
            var debugScreenSpaceTracingValues = (int[])Enum.GetValues(typeof(DebugScreenSpaceTracing));

            var debugScreenSpaceTracingHiZStringsList = new List<GUIContent>();
            var debugScreenSpaceTracingProxyStringsList = new List<GUIContent>();
            var debugScreenSpaceTracingHiZValueList = new List<int>();
            var debugScreenSpaceTracingProxyValueList = new List<int>();
            for (int i = 0, c = debugScreenSpaceTracingStrings.Length; i < c; ++i)
            {
                var g = debugScreenSpaceTracingStrings[i];
                var v = debugScreenSpaceTracingValues[i];
                if (!g.text.StartsWith("Proxy"))
                {
                    debugScreenSpaceTracingHiZStringsList.Add(g);
                    debugScreenSpaceTracingHiZValueList.Add(v);
                }
                if (!g.text.StartsWith("HiZ"))
                {
                    debugScreenSpaceTracingProxyStringsList.Add(g);
                    debugScreenSpaceTracingProxyValueList.Add(v);
                }
            }

            debugScreenSpaceTracingHiZStrings = debugScreenSpaceTracingHiZStringsList.ToArray();
            debugScreenSpaceTracingHiZValues = debugScreenSpaceTracingHiZValueList.ToArray();
            debugScreenSpaceTracingProxyStrings = debugScreenSpaceTracingProxyStringsList.ToArray();
            debugScreenSpaceTracingProxyValues = debugScreenSpaceTracingProxyValueList.ToArray();
        }

        public int GetDebugMaterialIndex()
        {
            return materialDebugSettings.GetDebugMaterialIndex();
        }

        public DebugLightingMode GetDebugLightingMode()
        {
            return lightingDebugSettings.debugLightingMode;
        }

        public int GetDebugLightingSubMode()
        {
            switch (lightingDebugSettings.debugLightingMode)
            {
                case DebugLightingMode.ScreenSpaceTracingRefraction:
                case DebugLightingMode.ScreenSpaceTracingReflection:
                    return (int)lightingDebugSettings.debugScreenSpaceTracingMode;
                default:
                    return 0;
            }
        }

        public DebugMipMapMode GetDebugMipMapMode()
        {
            return mipMapDebugSettings.debugMipMapMode;
        }

        public bool IsDebugDisplayEnabled()
        {
            return materialDebugSettings.IsDebugDisplayEnabled() || lightingDebugSettings.IsDebugDisplayEnabled() || mipMapDebugSettings.IsDebugDisplayEnabled() || IsDebugFullScreenEnabled();
        }

        public bool IsDebugDisplayRemovePostprocess()
        {
            // We want to keep post process when only the override more are enabled and none of the other
            return materialDebugSettings.IsDebugDisplayEnabled() || lightingDebugSettings.IsDebugDisplayRemovePostprocess() || mipMapDebugSettings.IsDebugDisplayEnabled() || IsDebugFullScreenEnabled();
        }

        public bool IsDebugMaterialDisplayEnabled()
        {
            return materialDebugSettings.IsDebugDisplayEnabled();
        }

        public bool IsDebugFullScreenEnabled()
        {
            return fullScreenDebugMode != FullScreenDebugMode.None;
        }

        public bool IsDebugMipMapDisplayEnabled()
        {
            return mipMapDebugSettings.IsDebugDisplayEnabled();
        }

        private void DisableNonMaterialDebugSettings()
        {
            lightingDebugSettings.debugLightingMode = DebugLightingMode.None;
            mipMapDebugSettings.debugMipMapMode = DebugMipMapMode.None;
        }

        public void SetDebugViewMaterial(int value)
        {
            if (value != 0)
                DisableNonMaterialDebugSettings();
            materialDebugSettings.SetDebugViewMaterial(value);
        }

        public void SetDebugViewEngine(int value)
        {
            if (value != 0)
                DisableNonMaterialDebugSettings();
            materialDebugSettings.SetDebugViewEngine(value);
        }

        public void SetDebugViewVarying(DebugViewVarying value)
        {
            if (value != 0)
                DisableNonMaterialDebugSettings();
            materialDebugSettings.SetDebugViewVarying(value);
        }

        public void SetDebugViewProperties(DebugViewProperties value)
        {
            if (value != 0)
                DisableNonMaterialDebugSettings();
            materialDebugSettings.SetDebugViewProperties(value);
        }

        public void SetDebugViewGBuffer(int value)
        {
            if (value != 0)
                DisableNonMaterialDebugSettings();
            materialDebugSettings.SetDebugViewGBuffer(value);
        }

        public void SetDebugLightingMode(DebugLightingMode value)
        {
            if (value != 0)
            {
                materialDebugSettings.DisableMaterialDebug();
                mipMapDebugSettings.debugMipMapMode = DebugMipMapMode.None;
            }
            lightingDebugSettings.debugLightingMode = value;
        }

        public void SetMipMapMode(DebugMipMapMode value)
        {
            if (value != 0)
            {
                materialDebugSettings.DisableMaterialDebug();
                lightingDebugSettings.debugLightingMode = DebugLightingMode.None;
            }
            mipMapDebugSettings.debugMipMapMode = value;
        }

        bool IsScreenSpaceTracingRefractionDebugEnabled()
        {
            return fullScreenDebugMode == FullScreenDebugMode.ScreenSpaceTracing
                && lightingDebugSettings.debugLightingMode == DebugLightingMode.ScreenSpaceTracingRefraction;
        }

        void SetScreenSpaceTracingRefractionDebugEnabled(bool value)
        {
            if (value)
            {
                lightingDebugSettings.debugLightingMode = DebugLightingMode.ScreenSpaceTracingRefraction;
                fullScreenDebugMode = FullScreenDebugMode.ScreenSpaceTracing;
            }
            else
            {
                lightingDebugSettings.debugLightingMode = DebugLightingMode.None;
                fullScreenDebugMode = FullScreenDebugMode.None;
            }
        }

        void SetScreenSpaceTracingRefractionDebugMode(int value)
        {
            var val = (DebugScreenSpaceTracing)value;
            if (val != DebugScreenSpaceTracing.None)
            {
                lightingDebugSettings.debugLightingMode = DebugLightingMode.ScreenSpaceTracingRefraction;
                lightingDebugSettings.debugScreenSpaceTracingMode = (DebugScreenSpaceTracing)value;
            }
            else
            {
                lightingDebugSettings.debugLightingMode = DebugLightingMode.None;
                lightingDebugSettings.debugScreenSpaceTracingMode = DebugScreenSpaceTracing.None;
            }
        }

        public void UpdateMaterials()
        {
#if UNITY_2018_2_OR_NEWER
            if (mipMapDebugSettings.debugMipMapMode != 0)
                Texture.SetStreamingTextureMaterialDebugProperties();
#endif
        }

        public bool DebugNeedsExposure()
        {
            DebugLightingMode debugLighting = lightingDebugSettings.debugLightingMode;
            DebugViewGbuffer debugGBuffer = (DebugViewGbuffer)materialDebugSettings.debugViewGBuffer;
            return  (debugLighting == DebugLightingMode.DiffuseLighting || debugLighting == DebugLightingMode.SpecularLighting) ||
                    (debugGBuffer == DebugViewGbuffer.BakeDiffuseLightingWithAlbedoPlusEmissive) ||
                    (fullScreenDebugMode == FullScreenDebugMode.PreRefractionColorPyramid || fullScreenDebugMode == FullScreenDebugMode.FinalColorPyramid);
        }

        void RegisterDisplayStatsDebug()
        {
            m_DebugDisplayStatsItems = new DebugUI.Widget[]
            {
                new DebugUI.Value { displayName = "Frame Rate", getter = () => 1f / Time.smoothDeltaTime, refreshRate = 1f / 30f },
                new DebugUI.Value { displayName = "Frame Rate (ms)", getter = () => Time.smoothDeltaTime * 1000f, refreshRate = 1f / 30f }
            };

            var panel = DebugManager.instance.GetPanel(k_PanelDisplayStats, true);
            panel.flags = DebugUI.Flags.RuntimeOnly;
            panel.children.Add(m_DebugDisplayStatsItems);
        }

        void RegisterScreenSpaceTracingDebug()
        {
            var list = new List<DebugUI.Container>();

            var refractionContainer = new DebugUI.Container
            {
                displayName = "Refraction",
                children =
                {
                    new DebugUI.BoolField { displayName = "Debug Enabled", getter = IsScreenSpaceTracingRefractionDebugEnabled, setter = SetScreenSpaceTracingRefractionDebugEnabled, onValueChanged = RefreshScreenSpaceTracingDebug },
                }
            };
            list.Add(refractionContainer);

            if (IsScreenSpaceTracingRefractionDebugEnabled())
            {
                var debugSettingsContainer = new DebugUI.Container
                {
                    displayName = "Debug Settings",
                    children =
                    {
                        new DebugUI.Value { displayName = string.Empty, getter = () => "Click in the scene view, or press 'End' key to select the pixel under the mouse in the scene view to debug." },
                        new DebugUI.Value { displayName = "SSRay Model", getter = () => screenSpaceTracingDebugData.tracingModel }
                    }
                };
                refractionContainer.children.Add(debugSettingsContainer);

                switch (screenSpaceTracingDebugData.tracingModel)
                {
                    case Lit.RefractionSSRayModel.Proxy:
                    {
                        debugSettingsContainer.children.Add(
                            new DebugUI.EnumField { displayName = "Debug Mode", getter = GetDebugLightingSubMode, setter = SetScreenSpaceTracingRefractionDebugMode, enumNames = debugScreenSpaceTracingProxyStrings, enumValues = debugScreenSpaceTracingProxyValues, onValueChanged = RefreshScreenSpaceTracingDebug }
                        );
                        refractionContainer.children.Add(
                            new DebugUI.Container
                            {
                                displayName = "Debug Values",
                                children =
                                {
                                    new DebugUI.Value { displayName = "Hit Success", getter = () => screenSpaceTracingDebugData.endHitSuccess },
                                    new DebugUI.Value { displayName = "Proxy Shape", getter = () => screenSpaceTracingDebugData.proxyShapeType },
                                    new DebugUI.Value { displayName = "Projection Distance", getter = () => screenSpaceTracingDebugData.projectionDistance },
                                    new DebugUI.Value { displayName = "Start Position", getter = () => screenSpaceTracingDebugData.loopStartPositionSS },
                                    new DebugUI.Value { displayName = "Start Linear Depth", getter = () => screenSpaceTracingDebugData.loopStartLinearDepth },
                                    new DebugUI.Value { displayName = "End Linear Depth", getter = () => screenSpaceTracingDebugData.endLinearDepth },
                                    new DebugUI.Value { displayName = "End Position", getter = () => screenSpaceTracingDebugData.endPositionSS },
                                }
                            }
                        );
                        break;
                    }
                    case Lit.RefractionSSRayModel.HiZ:
                    {
                        debugSettingsContainer.children.Insert(1, new DebugUI.Value { displayName = string.Empty, getter = () => "Press PageUp/PageDown to Increase/Decrease the HiZ step." });
                        debugSettingsContainer.children.Add(
                            new DebugUI.EnumField { displayName = "Debug Mode", getter = GetDebugLightingSubMode, setter = SetScreenSpaceTracingRefractionDebugMode, enumNames = debugScreenSpaceTracingHiZStrings, enumValues = debugScreenSpaceTracingHiZValues, onValueChanged = RefreshScreenSpaceTracingDebug },
                            new DebugUI.BoolField { displayName = "Display Grid", getter = () => showSSRayGrid, setter = v => showSSRayGrid = v },
                            new DebugUI.BoolField { displayName = "Display Depth", getter = () => showSSRayDepthPyramid, setter = v => showSSRayDepthPyramid = v }
                        );
                        refractionContainer.children.Add(
                            new DebugUI.Container
                            {
                                displayName = "Debug Values (loop)",
                                children =
                                {
                                    new DebugUI.Value { displayName = "Hit Success", getter = () => screenSpaceTracingDebugData.endHitSuccess },
                                    new DebugUI.Value { displayName = "Start Position", getter = () => screenSpaceTracingDebugData.loopStartPositionSS },
                                    new DebugUI.Value { displayName = "Start Linear Depth", getter = () => screenSpaceTracingDebugData.loopStartLinearDepth },
                                    new DebugUI.Value { displayName = "Ray Direction SS", getter = () => new Vector2(screenSpaceTracingDebugData.loopRayDirectionSS.x, screenSpaceTracingDebugData.loopRayDirectionSS.y) },
                                    new DebugUI.Value { displayName = "Ray Depth", getter = () => 1f / screenSpaceTracingDebugData.loopRayDirectionSS.z },
                                    new DebugUI.Value { displayName = "End Position", getter = () => screenSpaceTracingDebugData.endPositionSS },
                                    new DebugUI.Value { displayName = "End Linear Depth", getter = () => screenSpaceTracingDebugData.endLinearDepth },
                                }
                            },
                            new DebugUI.Container
                            {
                                displayName = "Debug Values (iteration)",
                                children =
                                {
                                    new DebugUI.Value { displayName = "Iteration", getter = () => string.Format("{0}/{1}", screenSpaceTracingDebugData.iteration, screenSpaceTracingDebugData.loopIterationMax) },
                                    new DebugUI.Value { displayName = "Position SS", getter = () => new Vector2(screenSpaceTracingDebugData.iterationPositionSS.x, screenSpaceTracingDebugData.iterationPositionSS.y) },
                                    new DebugUI.Value { displayName = "Depth", getter = () => 1f / screenSpaceTracingDebugData.iterationPositionSS.z },
                                    new DebugUI.Value { displayName = "Depth Buffer", getter = () => screenSpaceTracingDebugData.iterationLinearDepthBuffer },
                                    new DebugUI.Value { displayName = "Mip Level", getter = () => string.Format("{0}/{1}", screenSpaceTracingDebugData.iterationMipLevel, screenSpaceTracingDebugData.loopMipLevelMax) },
                                    new DebugUI.Value { displayName = "Intersection kind", getter = () => screenSpaceTracingDebugData.iterationIntersectionKind },
                                    new DebugUI.Value { displayName = "Cell Id", getter = () => screenSpaceTracingDebugData.iterationCellId },
                                    new DebugUI.Value { displayName = "Cell Size", getter = () => screenSpaceTracingDebugData.iterationCellSize },
                                }
                            }
                        );
                        break;
                    }
                }
            }

            m_DebugScreenSpaceTracingItems = list.ToArray();
            var panel = DebugManager.instance.GetPanel(k_PanelScreenSpaceTracing, true);
            panel.flags |= DebugUI.Flags.EditorForceUpdate;
            panel.children.Add(m_DebugScreenSpaceTracingItems);
        }

        public void RegisterMaterialDebug()
        {
            m_DebugMaterialItems = new DebugUI.Widget[]
            {
                new DebugUI.EnumField { displayName = "Material", getter = () => materialDebugSettings.debugViewMaterial, setter = value => SetDebugViewMaterial(value), enumNames = MaterialDebugSettings.debugViewMaterialStrings, enumValues = MaterialDebugSettings.debugViewMaterialValues },
                new DebugUI.EnumField { displayName = "Engine", getter = () => materialDebugSettings.debugViewEngine, setter = value => SetDebugViewEngine(value), enumNames = MaterialDebugSettings.debugViewEngineStrings, enumValues = MaterialDebugSettings.debugViewEngineValues },
                new DebugUI.EnumField { displayName = "Attributes", getter = () => (int)materialDebugSettings.debugViewVarying, setter = value => SetDebugViewVarying((DebugViewVarying)value), autoEnum = typeof(DebugViewVarying) },
                new DebugUI.EnumField { displayName = "Properties", getter = () => (int)materialDebugSettings.debugViewProperties, setter = value => SetDebugViewProperties((DebugViewProperties)value), autoEnum = typeof(DebugViewProperties) },
                new DebugUI.EnumField { displayName = "GBuffer", getter = () => materialDebugSettings.debugViewGBuffer, setter = value => SetDebugViewGBuffer(value), enumNames = MaterialDebugSettings.debugViewMaterialGBufferStrings, enumValues = MaterialDebugSettings.debugViewMaterialGBufferValues }
            };

            var panel = DebugManager.instance.GetPanel(k_PanelMaterials, true);
            panel.children.Add(m_DebugMaterialItems);
        }

        // For now we just rebuild the lighting panel if needed, but ultimately it could be done in a better way
        void RefreshLightingDebug<T>(DebugUI.Field<T> field, T value)
        {
            UnregisterDebugItems(k_PanelLighting, m_DebugLightingItems);
            RegisterLightingDebug();
        }

        void RefreshScreenSpaceTracingDebug<T>(DebugUI.Field<T> field, T value)
        {
            UnregisterDebugItems(k_PanelScreenSpaceTracing, m_DebugScreenSpaceTracingItems);
            RegisterScreenSpaceTracingDebug();
        }

        void RefreshDecalsDebug<T>(DebugUI.Field<T> field, T value)
        {
            UnregisterDebugItems(k_PanelDecals, m_DebugDecalsItems);
            RegisterDecalsDebug();
        }

        public void RegisterLightingDebug()
        {
            var list = new List<DebugUI.Widget>();

            list.Add(new DebugUI.EnumField
            {
                displayName = "Shadow Debug Mode",
                getter = () => (int)lightingDebugSettings.shadowDebugMode,
                setter = value => lightingDebugSettings.shadowDebugMode = (ShadowMapDebugMode)value,
                autoEnum = typeof(ShadowMapDebugMode),
                onValueChanged = RefreshLightingDebug
            });

            if (lightingDebugSettings.shadowDebugMode == ShadowMapDebugMode.VisualizeShadowMap)
            {
                var container = new DebugUI.Container();
                container.children.Add(new DebugUI.BoolField { displayName = "Use Selection", getter = () => lightingDebugSettings.shadowDebugUseSelection, setter = value => lightingDebugSettings.shadowDebugUseSelection = value, flags = DebugUI.Flags.EditorOnly, onValueChanged = RefreshLightingDebug });

                if (!lightingDebugSettings.shadowDebugUseSelection)
                    container.children.Add(new DebugUI.UIntField { displayName = "Shadow Map Index", getter = () => lightingDebugSettings.shadowMapIndex, setter = value => lightingDebugSettings.shadowMapIndex = value, min = () => 0u, max = () => (uint)(RenderPipelineManager.currentPipeline as HDRenderPipeline).GetCurrentShadowCount() - 1u });

                list.Add(container);
            }
            else if (lightingDebugSettings.shadowDebugMode == ShadowMapDebugMode.VisualizeAtlas)
            {
                list.Add(new DebugUI.Container
                {
                    children =
                    {
                        new DebugUI.UIntField { displayName = "Shadow Atlas Index", getter = () => lightingDebugSettings.shadowAtlasIndex, setter = value => lightingDebugSettings.shadowAtlasIndex = value, min = () => 0u, max = () => (uint)(RenderPipelineManager.currentPipeline as HDRenderPipeline).GetShadowAtlasCount() - 1u },
                        new DebugUI.UIntField { displayName = "Shadow Slice Index", getter = () => lightingDebugSettings.shadowSliceIndex, setter = value => lightingDebugSettings.shadowSliceIndex = value, min = () => 0u, max = () => (uint)(RenderPipelineManager.currentPipeline as HDRenderPipeline).GetShadowSliceCount(lightingDebugSettings.shadowAtlasIndex) - 1u }
                    }
                });
            }

            list.Add(new DebugUI.FloatField { displayName = "Shadow Range Min Value", getter = () => lightingDebugSettings.shadowMinValue, setter = value => lightingDebugSettings.shadowMinValue = value });
            list.Add(new DebugUI.FloatField { displayName = "Shadow Range Max Value", getter = () => lightingDebugSettings.shadowMaxValue, setter = value => lightingDebugSettings.shadowMaxValue = value });

            list.Add(new DebugUI.EnumField { displayName = "Lighting Debug Mode", getter = () => (int)lightingDebugSettings.debugLightingMode, setter = value => SetDebugLightingMode((DebugLightingMode)value), autoEnum = typeof(DebugLightingMode), onValueChanged = RefreshLightingDebug });
            list.Add(new DebugUI.EnumField { displayName = "Fullscreen Debug Mode", getter = () => (int)fullScreenDebugMode, setter = value => fullScreenDebugMode = (FullScreenDebugMode)value, enumNames = lightingFullScreenDebugStrings, enumValues = lightingFullScreenDebugValues, onValueChanged = RefreshLightingDebug });
            switch (fullScreenDebugMode)
            {
                case FullScreenDebugMode.PreRefractionColorPyramid:
                case FullScreenDebugMode.FinalColorPyramid:
                case FullScreenDebugMode.DepthPyramid:
                {
                    list.Add(new DebugUI.Container
                    {
                        children =
                        {
                            new DebugUI.UIntField
                            {
                                displayName = "Fullscreen Debug Mip",
                                getter = () =>
                                {
                                    int id;
                                    switch (fullScreenDebugMode)
                                    {
                                        case FullScreenDebugMode.FinalColorPyramid:
                                        case FullScreenDebugMode.PreRefractionColorPyramid:
                                            id = HDShaderIDs._ColorPyramidScale;
                                            break;
                                        default:
                                            id = HDShaderIDs._DepthPyramidScale;
                                            break;
                                    }
                                    var size = Shader.GetGlobalVector(id);
                                    float lodCount = size.z;
                                    return (uint)(fullscreenDebugMip * lodCount);
                                },
                                setter = value =>
                                {
                                    int id;
                                    switch (fullScreenDebugMode)
                                    {
                                        case FullScreenDebugMode.FinalColorPyramid:
                                        case FullScreenDebugMode.PreRefractionColorPyramid:
                                            id = HDShaderIDs._ColorPyramidScale;
                                            break;
                                        default:
                                            id = HDShaderIDs._DepthPyramidScale;
                                            break;
                                    }
                                    var size = Shader.GetGlobalVector(id);
                                    float lodCount = size.z;
                                    fullscreenDebugMip = (float)Convert.ChangeType(value, typeof(float)) / lodCount;
                                },
                                min = () => 0u,
                                max = () =>
                                {
                                    int id;
                                    switch (fullScreenDebugMode)
                                    {
                                        case FullScreenDebugMode.FinalColorPyramid:
                                        case FullScreenDebugMode.PreRefractionColorPyramid:
                                            id = HDShaderIDs._ColorPyramidScale;
                                            break;
                                        default:
                                            id = HDShaderIDs._DepthPyramidScale;
                                            break;
                                    }
                                    var size = Shader.GetGlobalVector(id);
                                    float lodCount = size.z;
                                    return (uint)lodCount;
                                }
                            }
                        }
                    });
                    break;
                }
                default:
                    fullscreenDebugMip = 0;
                    break;
            }

            list.Add(new DebugUI.BoolField { displayName = "Override Smoothness", getter = () => lightingDebugSettings.overrideSmoothness, setter = value => lightingDebugSettings.overrideSmoothness = value, onValueChanged = RefreshLightingDebug });
            if (lightingDebugSettings.overrideSmoothness)
            {
                list.Add(new DebugUI.Container
                {
                    children =
                    {
                        new DebugUI.FloatField { displayName = "Smoothness", getter = () => lightingDebugSettings.overrideSmoothnessValue, setter = value => lightingDebugSettings.overrideSmoothnessValue = value, min = () => 0f, max = () => 1f, incStep = 0.025f }
                    }
                });
            }

            list.Add(new DebugUI.BoolField { displayName = "Override Albedo", getter = () => lightingDebugSettings.overrideAlbedo, setter = value => lightingDebugSettings.overrideAlbedo = value, onValueChanged = RefreshLightingDebug });
            if (lightingDebugSettings.overrideAlbedo)
            {
                list.Add(new DebugUI.Container
                {
                    children =
                    {
                        new DebugUI.ColorField { displayName = "Albedo", getter = () => lightingDebugSettings.overrideAlbedoValue, setter = value => lightingDebugSettings.overrideAlbedoValue = value, showAlpha = false, hdr = false }
                    }
                });
            }

            list.Add(new DebugUI.BoolField { displayName = "Override Normal", getter = () => lightingDebugSettings.overrideNormal, setter = value => lightingDebugSettings.overrideNormal = value });

            list.Add(new DebugUI.BoolField { displayName = "Override Specular Color", getter = () => lightingDebugSettings.overrideSpecularColor, setter = value => lightingDebugSettings.overrideSpecularColor = value, onValueChanged = RefreshLightingDebug });
            if (lightingDebugSettings.overrideSpecularColor)
            {
                list.Add(new DebugUI.Container
                {
                    children =
                    {
                        new DebugUI.ColorField { displayName = "Specular Color", getter = () => lightingDebugSettings.overrideSpecularColorValue, setter = value => lightingDebugSettings.overrideSpecularColorValue = value, showAlpha = false, hdr = false }
                    }
                });
            }

            list.Add(new DebugUI.EnumField { displayName = "Tile/Cluster Debug", getter = () => (int)lightingDebugSettings.tileClusterDebug, setter = value => lightingDebugSettings.tileClusterDebug = (LightLoop.TileClusterDebug)value, autoEnum = typeof(LightLoop.TileClusterDebug), onValueChanged = RefreshLightingDebug });
            if (lightingDebugSettings.tileClusterDebug != LightLoop.TileClusterDebug.None && lightingDebugSettings.tileClusterDebug != LightLoop.TileClusterDebug.MaterialFeatureVariants)
            {
                list.Add(new DebugUI.Container
                {
                    children =
                    {
                        new DebugUI.EnumField { displayName = "Tile/Cluster Debug By Category", getter = () => (int)lightingDebugSettings.tileClusterDebugByCategory, setter = value => lightingDebugSettings.tileClusterDebugByCategory = (LightLoop.TileClusterCategoryDebug)value, autoEnum = typeof(LightLoop.TileClusterCategoryDebug) }
                    }
                });
            }

            list.Add(new DebugUI.BoolField { displayName = "Display Sky Reflection", getter = () => lightingDebugSettings.displaySkyReflection, setter = value => lightingDebugSettings.displaySkyReflection = value, onValueChanged = RefreshLightingDebug });
            if (lightingDebugSettings.displaySkyReflection)
            {
                list.Add(new DebugUI.Container
                {
                    children =
                    {
                        new DebugUI.FloatField { displayName = "Sky Reflection Mipmap", getter = () => lightingDebugSettings.skyReflectionMipmap, setter = value => lightingDebugSettings.skyReflectionMipmap = value, min = () => 0f, max = () => 1f, incStep = 0.05f }
                    }
                });
            }

            if (DebugNeedsExposure())
                list.Add(new DebugUI.FloatField { displayName = "Debug Exposure", getter = () => lightingDebugSettings.debugExposure, setter = value => lightingDebugSettings.debugExposure = value });

            m_DebugLightingItems = list.ToArray();
            var panel = DebugManager.instance.GetPanel(k_PanelLighting, true);
            panel.children.Add(m_DebugLightingItems);
        }

        public void RegisterRenderingDebug()
        {
            m_DebugRenderingItems = new DebugUI.Widget[]
            {
                new DebugUI.EnumField { displayName = "Fullscreen Debug Mode", getter = () => (int)fullScreenDebugMode, setter = value => fullScreenDebugMode = (FullScreenDebugMode)value, enumNames = renderingFullScreenDebugStrings, enumValues = renderingFullScreenDebugValues },
                new DebugUI.EnumField { displayName = "MipMaps", getter = () => (int)mipMapDebugSettings.debugMipMapMode, setter = value => SetMipMapMode((DebugMipMapMode)value), autoEnum = typeof(DebugMipMapMode) },

                new DebugUI.Container
                {
                    displayName = "Color Picker",
                    flags = DebugUI.Flags.EditorOnly,
                    children =
                    {
                        new DebugUI.EnumField  { displayName = "Debug Mode", getter = () => (int)colorPickerDebugSettings.colorPickerMode, setter = value => colorPickerDebugSettings.colorPickerMode = (ColorPickerDebugMode)value, autoEnum = typeof(ColorPickerDebugMode) },
                        new DebugUI.FloatField { displayName = "Range Threshold 0", getter = () => colorPickerDebugSettings.colorThreshold0, setter = value => colorPickerDebugSettings.colorThreshold0 = value },
                        new DebugUI.FloatField { displayName = "Range Threshold 1", getter = () => colorPickerDebugSettings.colorThreshold1, setter = value => colorPickerDebugSettings.colorThreshold1 = value },
                        new DebugUI.FloatField { displayName = "Range Threshold 2", getter = () => colorPickerDebugSettings.colorThreshold2, setter = value => colorPickerDebugSettings.colorThreshold2 = value },
                        new DebugUI.FloatField { displayName = "Range Threshold 3", getter = () => colorPickerDebugSettings.colorThreshold3, setter = value => colorPickerDebugSettings.colorThreshold3 = value },
                        new DebugUI.ColorField { displayName = "Font Color", flags = DebugUI.Flags.EditorOnly, getter = () => colorPickerDebugSettings.fontColor, setter = value => colorPickerDebugSettings.fontColor = value }
                    }
                }
            };

            var panel = DebugManager.instance.GetPanel(k_PanelRendering, true);
            panel.children.Add(m_DebugRenderingItems);
        }

        public void RegisterDecalsDebug()
        {
            m_DebugDecalsItems = new DebugUI.Widget[]
            {
                new DebugUI.BoolField { displayName = "Display atlas", getter = () => decalsDebugSettings.m_DisplayAtlas, setter = value => decalsDebugSettings.m_DisplayAtlas = value},
                new DebugUI.UIntField { displayName = "Mip Level", getter = () => decalsDebugSettings.m_MipLevel, setter = value => decalsDebugSettings.m_MipLevel = value, min = () => 0u, max = () => (uint)(RenderPipelineManager.currentPipeline as HDRenderPipeline).GetDecalAtlasMipCount() }
            };

            var panel = DebugManager.instance.GetPanel(k_PanelDecals, true);
            panel.children.Add(m_DebugDecalsItems);
        }

        public void RegisterDebug()
        {
            RegisterDecalsDebug();
            RegisterDisplayStatsDebug();
            RegisterMaterialDebug();
            RegisterLightingDebug();
            RegisterRenderingDebug();
            RegisterScreenSpaceTracingDebug();
        }

        public void UnregisterDebug()
        {
            UnregisterDebugItems(k_PanelDecals, m_DebugDecalsItems);
            UnregisterDebugItems(k_PanelDisplayStats, m_DebugDisplayStatsItems);
            UnregisterDebugItems(k_PanelMaterials, m_DebugMaterialItems);
            UnregisterDebugItems(k_PanelLighting, m_DebugLightingItems);
            UnregisterDebugItems(k_PanelRendering, m_DebugRenderingItems);
            UnregisterDebugItems(k_PanelScreenSpaceTracing, m_DebugScreenSpaceTracingItems);
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
    }
}
