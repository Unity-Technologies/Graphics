using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Rendering.HighDefinition.Attributes;

namespace UnityEngine.Rendering.HighDefinition
{
    [GenerateHLSL]
    public enum FullScreenDebugMode
    {
        None,

        // Lighting
        MinLightingFullScreenDebug,
        SSAO,
        ScreenSpaceReflections,
        ContactShadows,
        ContactShadowsFade,
        ScreenSpaceShadows,
        PreRefractionColorPyramid,
        DepthPyramid,
        FinalColorPyramid,

        // Raytracing Only
        LightCluster,
        RayTracedGlobalIllumination,
        RecursiveRayTracing,
        MaxLightingFullScreenDebug,

        // Rendering
        MinRenderingFullScreenDebug,
        MotionVectors,
        NanTracker,
        ColorLog,
        DepthOfFieldCoc,
        TransparencyOverdraw,
        MaxRenderingFullScreenDebug,

        //Material
        MinMaterialFullScreenDebug,
        ValidateDiffuseColor,
        ValidateSpecularColor,
        MaxMaterialFullScreenDebug
    }

    public class DebugDisplaySettings : IDebugData
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
        static GUIContent[] s_MsaaSamplesDebugStrings = null;
        static int[] s_MsaaSamplesDebugValues = null;

        static List<GUIContent> s_CameraNames = new List<GUIContent>();
        static GUIContent[] s_CameraNamesStrings = null;
        static int[] s_CameraNamesValues = null;

        static bool needsRefreshingCameraFreezeList = true;

        public class DebugData
        {
            public float debugOverlayRatio = 0.33f;
            public FullScreenDebugMode fullScreenDebugMode = FullScreenDebugMode.None;
            public float fullscreenDebugMip = 0.0f;
            public int fullScreenContactShadowLightIndex = 0;
            public bool showSSSampledColor = false;
            public bool showContactShadowFade = false;

            public MaterialDebugSettings materialDebugSettings = new MaterialDebugSettings();
            public LightingDebugSettings lightingDebugSettings = new LightingDebugSettings();
            public MipMapDebugSettings mipMapDebugSettings = new MipMapDebugSettings();
            public ColorPickerDebugSettings colorPickerDebugSettings = new ColorPickerDebugSettings();
            public FalseColorDebugSettings falseColorDebugSettings = new FalseColorDebugSettings();
            public DecalsDebugSettings decalsDebugSettings = new DecalsDebugSettings();
            public TransparencyDebugSettings transparencyDebugSettings = new TransparencyDebugSettings();
            public MSAASamples msaaSamples = MSAASamples.None;

            public uint screenSpaceShadowIndex = 0;
            // Raytracing
            public bool countRays = false;
            public bool showRaysPerFrame = false;
            public Color raysPerFrameFontColor = Color.white;

            public int debugCameraToFreeze = 0;

            // TODO: The only reason this exist is because of Material/Engine debug enums
            // They have repeating values, which caused issues when iterating through the enum, thus the need for explicit indices
            // Once we refactor material/engine debug to avoid repeating values, we should be able to remove that.
            //saved enum fields for when repainting
            public int lightingDebugModeEnumIndex;
            public int lightingFulscreenDebugModeEnumIndex;
            public int materialValidatorDebugModeEnumIndex;
            public int tileClusterDebugEnumIndex;
            public int mipMapsEnumIndex;
            public int engineEnumIndex;
            public int attributesEnumIndex;
            public int propertiesEnumIndex;
            public int gBufferEnumIndex;
            public int shadowDebugModeEnumIndex;
            public int tileClusterDebugByCategoryEnumIndex;
            public int lightVolumeDebugTypeEnumIndex;
            public int renderingFulscreenDebugModeEnumIndex;
            public int terrainTextureEnumIndex;
            public int colorPickerDebugModeEnumIndex;
            public int msaaSampleDebugModeEnumIndex;
            public int debugCameraToFreezeEnumIndex;

            // When settings mutually exclusives enum values, we need to reset the other ones.
            public void ResetExclusiveEnumIndices()
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

        public DebugData data { get => m_Data; }

        public static GUIContent[] renderingFullScreenDebugStrings => s_RenderingFullScreenDebugStrings;
        public static int[] renderingFullScreenDebugValues => s_RenderingFullScreenDebugValues;

        public DebugDisplaySettings()
        {
            FillFullScreenDebugEnum(ref s_LightingFullScreenDebugStrings, ref s_LightingFullScreenDebugValues, FullScreenDebugMode.MinLightingFullScreenDebug, FullScreenDebugMode.MaxLightingFullScreenDebug);
            FillFullScreenDebugEnum(ref s_RenderingFullScreenDebugStrings, ref s_RenderingFullScreenDebugValues, FullScreenDebugMode.MinRenderingFullScreenDebug, FullScreenDebugMode.MaxRenderingFullScreenDebug);
            FillFullScreenDebugEnum(ref s_MaterialFullScreenDebugStrings, ref s_MaterialFullScreenDebugValues, FullScreenDebugMode.MinMaterialFullScreenDebug, FullScreenDebugMode.MaxMaterialFullScreenDebug);

            s_MaterialFullScreenDebugStrings[(int)FullScreenDebugMode.ValidateDiffuseColor - ((int)FullScreenDebugMode.MinMaterialFullScreenDebug)] = new GUIContent("Diffuse Color");
            s_MaterialFullScreenDebugStrings[(int)FullScreenDebugMode.ValidateSpecularColor - ((int)FullScreenDebugMode.MinMaterialFullScreenDebug)] = new GUIContent("Metal or SpecularColor");

            s_MsaaSamplesDebugStrings = Enum.GetNames(typeof(MSAASamples))
                .Select(t => new GUIContent(t))
                .ToArray();
            s_MsaaSamplesDebugValues = (int[])Enum.GetValues(typeof(MSAASamples));

            m_Data = new DebugData();
        }

        Action IDebugData.GetReset() => () => m_Data = new DebugData();

        public float[] GetDebugMaterialIndexes()
        {
            return data.materialDebugSettings.GetDebugMaterialIndexes();
        }

        public DebugLightFilterMode GetDebugLightFilterMode()
        {
            return data.lightingDebugSettings.debugLightFilterMode;
        }

        public DebugLightingMode GetDebugLightingMode()
        {
            return data.lightingDebugSettings.debugLightingMode;
        }

        public ShadowMapDebugMode GetDebugShadowMapMode()
        {
            return data.lightingDebugSettings.shadowDebugMode;
        }

        public DebugMipMapMode GetDebugMipMapMode()
        {
            return data.mipMapDebugSettings.debugMipMapMode;
        }

        public DebugMipMapModeTerrainTexture GetDebugMipMapModeTerrainTexture()
        {
            return data.mipMapDebugSettings.terrainTexture;
        }

        public ColorPickerDebugMode GetDebugColorPickerMode()
        {
            return data.colorPickerDebugSettings.colorPickerMode;
        }

        public bool IsCameraFreezeEnabled()
        {
            return data.debugCameraToFreeze != 0;
        }
        public string GetFrozenCameraName()
        {
            return s_CameraNamesStrings[data.debugCameraToFreeze].text;
        }

        public bool IsDebugDisplayEnabled()
        {
            return data.materialDebugSettings.IsDebugDisplayEnabled() || data.lightingDebugSettings.IsDebugDisplayEnabled() || data.mipMapDebugSettings.IsDebugDisplayEnabled() || IsDebugFullScreenEnabled();
        }

        public bool IsDebugDisplayRemovePostprocess()
        {
            // We want to keep post process when only the override more are enabled and none of the other
            return data.materialDebugSettings.IsDebugDisplayEnabled() || data.lightingDebugSettings.IsDebugDisplayRemovePostprocess() || data.mipMapDebugSettings.IsDebugDisplayEnabled();
        }

        public bool IsDebugMaterialDisplayEnabled()
        {
            return data.materialDebugSettings.IsDebugDisplayEnabled();
        }

        public bool IsDebugFullScreenEnabled()
        {
            return data.fullScreenDebugMode != FullScreenDebugMode.None;
        }

        public bool IsMaterialValidationEnabled()
        {
            return (data.fullScreenDebugMode == FullScreenDebugMode.ValidateDiffuseColor) || (data.fullScreenDebugMode == FullScreenDebugMode.ValidateSpecularColor);
        }

        public bool IsDebugMipMapDisplayEnabled()
        {
            return data.mipMapDebugSettings.IsDebugDisplayEnabled();
        }

        public bool IsMatcapViewEnabled(HDCamera camera)
        {
            bool sceneViewLightingDisabled = CoreUtils.IsSceneLightingDisabled(camera.camera);
            return sceneViewLightingDisabled || GetDebugLightingMode() == DebugLightingMode.MatcapView;
        }

        private void DisableNonMaterialDebugSettings()
        {
            data.lightingDebugSettings.debugLightingMode = DebugLightingMode.None;
            data.mipMapDebugSettings.debugMipMapMode = DebugMipMapMode.None;
        }

        public void SetDebugViewCommonMaterialProperty(MaterialSharedProperty value)
        {
            if (value != MaterialSharedProperty.None)
                DisableNonMaterialDebugSettings();
            data.materialDebugSettings.SetDebugViewCommonMaterialProperty(value);
        }

        public void SetDebugViewMaterial(int value)
        {
            if (value != 0)
                DisableNonMaterialDebugSettings();
            data.materialDebugSettings.SetDebugViewMaterial(value);
        }

        public void SetDebugViewEngine(int value)
        {
            if (value != 0)
                DisableNonMaterialDebugSettings();
            data.materialDebugSettings.SetDebugViewEngine(value);
        }

        public void SetDebugViewVarying(DebugViewVarying value)
        {
            if (value != 0)
                DisableNonMaterialDebugSettings();
            data.materialDebugSettings.SetDebugViewVarying(value);
        }

        public void SetDebugViewProperties(DebugViewProperties value)
        {
            if (value != 0)
                DisableNonMaterialDebugSettings();
            data.materialDebugSettings.SetDebugViewProperties(value);
        }

        public void SetDebugViewGBuffer(int value)
        {
            if (value != 0)
                DisableNonMaterialDebugSettings();
            data.materialDebugSettings.SetDebugViewGBuffer(value);
        }

        public void SetFullScreenDebugMode(FullScreenDebugMode value)
        {
            if (data.lightingDebugSettings.shadowDebugMode == ShadowMapDebugMode.SingleShadow)
                value = 0;

            data.fullScreenDebugMode = value;
        }

        public void SetShadowDebugMode(ShadowMapDebugMode value)
        {
            // When SingleShadow is enabled, we don't render full screen debug modes
            if (value == ShadowMapDebugMode.SingleShadow)
                data.fullScreenDebugMode = 0;
            data.lightingDebugSettings.shadowDebugMode = value;
        }

        public void SetDebugLightFilterMode(DebugLightFilterMode value)
        {
            if (value != 0)
            {
                data.materialDebugSettings.DisableMaterialDebug();
                data.mipMapDebugSettings.debugMipMapMode = DebugMipMapMode.None;
            }
            data.lightingDebugSettings.debugLightFilterMode = value;
        }

        public void SetDebugLightingMode(DebugLightingMode value)
        {
            if (value != 0)
            {
                data.materialDebugSettings.DisableMaterialDebug();
                data.mipMapDebugSettings.debugMipMapMode = DebugMipMapMode.None;
            }
            data.lightingDebugSettings.debugLightingMode = value;
        }

        public void SetMipMapMode(DebugMipMapMode value)
        {
            if (value != 0)
            {
                data.materialDebugSettings.DisableMaterialDebug();
                data.lightingDebugSettings.debugLightingMode = DebugLightingMode.None;
            }
            data.mipMapDebugSettings.debugMipMapMode = value;
        }

        public void UpdateMaterials()
        {
            if (data.mipMapDebugSettings.debugMipMapMode != 0)
                Texture.SetStreamingTextureMaterialDebugProperties();
        }

        public void UpdateCameraFreezeOptions()
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

        public bool DebugNeedsExposure()
        {
            DebugLightingMode debugLighting = data.lightingDebugSettings.debugLightingMode;
            DebugViewGbuffer debugGBuffer = (DebugViewGbuffer)data.materialDebugSettings.debugViewGBuffer;
            return (debugLighting == DebugLightingMode.DiffuseLighting || debugLighting == DebugLightingMode.SpecularLighting || debugLighting == DebugLightingMode.VisualizeCascade) ||
                (data.lightingDebugSettings.overrideAlbedo || data.lightingDebugSettings.overrideNormal || data.lightingDebugSettings.overrideSmoothness || data.lightingDebugSettings.overrideSpecularColor || data.lightingDebugSettings.overrideEmissiveColor || data.lightingDebugSettings.overrideAmbientOcclusion) ||
                (debugGBuffer == DebugViewGbuffer.BakeDiffuseLightingWithAlbedoPlusEmissive) ||
                (data.fullScreenDebugMode == FullScreenDebugMode.PreRefractionColorPyramid || data.fullScreenDebugMode == FullScreenDebugMode.FinalColorPyramid || data.fullScreenDebugMode == FullScreenDebugMode.ScreenSpaceReflections || data.fullScreenDebugMode == FullScreenDebugMode.LightCluster || data.fullScreenDebugMode == FullScreenDebugMode.ScreenSpaceShadows || data.fullScreenDebugMode == FullScreenDebugMode.NanTracker || data.fullScreenDebugMode == FullScreenDebugMode.ColorLog) || data.fullScreenDebugMode == FullScreenDebugMode.RayTracedGlobalIllumination;
        }

        void RegisterDisplayStatsDebug()
        {
            var list = new List<DebugUI.Widget>();
            list.Add(new DebugUI.Value { displayName = "Frame Rate (fps)", getter = () => 1f / Time.smoothDeltaTime, refreshRate = 1f / 30f });
            list.Add(new DebugUI.Value { displayName = "Frame Time (ms)", getter = () => Time.smoothDeltaTime * 1000f, refreshRate = 1f / 30f });
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
                        /*
                        new DebugUI.BoolField { displayName = "Display Ray Count", getter = () => data.showRaysPerFrame, setter = value => data.showRaysPerFrame = value, onValueChanged = RefreshDisplayStatsDebug },
                        new DebugUI.ColorField { displayName = "Ray Count Font Color", getter = () => data.raysPerFrameFontColor, setter = value => data.raysPerFrameFontColor = value },
                        */
                    }
                });
            }
               
            m_DebugDisplayStatsItems = list.ToArray();
            var panel = DebugManager.instance.GetPanel(k_PanelDisplayStats, true);
            panel.flags = DebugUI.Flags.RuntimeOnly;
            panel.children.Add(m_DebugDisplayStatsItems);
        }

        public void RegisterMaterialDebug()
        {
            var list = new List<DebugUI.Widget>();

            list.Add(new DebugUI.EnumField { displayName = "Common Material Property", getter = () => (int)data.materialDebugSettings.debugViewMaterialCommonValue, setter = value => SetDebugViewCommonMaterialProperty((MaterialSharedProperty)value), autoEnum = typeof(MaterialSharedProperty), getIndex = () => (int)data.materialDebugSettings.debugViewMaterialCommonValue, setIndex = value => { data.ResetExclusiveEnumIndices(); data.materialDebugSettings.debugViewMaterialCommonValue = (MaterialSharedProperty)value; } });
            list.Add( new DebugUI.EnumField { displayName = "Material", getter = () => (data.materialDebugSettings.debugViewMaterial[0]) == 0 ? 0 : data.materialDebugSettings.debugViewMaterial[1], setter = value => SetDebugViewMaterial(value), enumNames = MaterialDebugSettings.debugViewMaterialStrings, enumValues = MaterialDebugSettings.debugViewMaterialValues, getIndex = () => data.materialDebugSettings.materialEnumIndex, setIndex = value => { data.ResetExclusiveEnumIndices(); data.materialDebugSettings.materialEnumIndex = value; } });
            list.Add( new DebugUI.EnumField { displayName = "Engine", getter = () => data.materialDebugSettings.debugViewEngine, setter = value => SetDebugViewEngine(value), enumNames = MaterialDebugSettings.debugViewEngineStrings, enumValues = MaterialDebugSettings.debugViewEngineValues, getIndex = () => data.engineEnumIndex, setIndex = value => { data.ResetExclusiveEnumIndices(); data.engineEnumIndex = value; } });
            list.Add( new DebugUI.EnumField { displayName = "Attributes", getter = () => (int)data.materialDebugSettings.debugViewVarying, setter = value => SetDebugViewVarying((DebugViewVarying)value), autoEnum = typeof(DebugViewVarying), getIndex = () => data.attributesEnumIndex, setIndex = value => { data.ResetExclusiveEnumIndices(); data.attributesEnumIndex = value; } });
            list.Add( new DebugUI.EnumField { displayName = "Properties", getter = () => (int)data.materialDebugSettings.debugViewProperties, setter = value => SetDebugViewProperties((DebugViewProperties)value), autoEnum = typeof(DebugViewProperties), getIndex = () => data.propertiesEnumIndex, setIndex = value => { data.ResetExclusiveEnumIndices(); data.propertiesEnumIndex = value; } });
            list.Add( new DebugUI.EnumField { displayName = "GBuffer", getter = () => data.materialDebugSettings.debugViewGBuffer, setter = value => SetDebugViewGBuffer(value), enumNames = MaterialDebugSettings.debugViewMaterialGBufferStrings, enumValues = MaterialDebugSettings.debugViewMaterialGBufferValues, getIndex = () => data.gBufferEnumIndex, setIndex = value => { data.ResetExclusiveEnumIndices(); data.gBufferEnumIndex = value; } });
            list.Add( new DebugUI.EnumField { displayName = "Material Validator", getter = () => (int)data.fullScreenDebugMode, setter = value => SetFullScreenDebugMode((FullScreenDebugMode)value), enumNames = s_MaterialFullScreenDebugStrings, enumValues = s_MaterialFullScreenDebugValues, onValueChanged = RefreshMaterialDebug, getIndex = () => data.materialValidatorDebugModeEnumIndex, setIndex = value => { data.ResetExclusiveEnumIndices(); data.materialValidatorDebugModeEnumIndex = value; } });

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
            UnregisterDebugItems(k_PanelDecals, m_DebugDecalsItems);
            RegisterDecalsDebug();
        }

        void RefreshRenderingDebug<T>(DebugUI.Field<T> field, T value)
        {
            UnregisterDebugItems(k_PanelRendering, m_DebugRenderingItems);
            RegisterRenderingDebug();
        }

        void RefreshMaterialDebug<T>(DebugUI.Field<T> field, T value)
        {
            UnregisterDebugItems(k_PanelMaterials, m_DebugMaterialItems);
            RegisterMaterialDebug();
        }

        public void RegisterLightingDebug()
        {
            var list = new List<DebugUI.Widget>();

            list.Add(new DebugUI.Foldout
            {
                displayName = "Show Light By Type",
                children = {
                    new DebugUI.BoolField { displayName = "Show Directional Lights", getter = () => data.lightingDebugSettings.showDirectionalLight, setter = value => data.lightingDebugSettings.showDirectionalLight = value },
                    new DebugUI.BoolField { displayName = "Show Punctual Lights", getter = () => data.lightingDebugSettings.showPunctualLight, setter = value => data.lightingDebugSettings.showPunctualLight = value },
                    new DebugUI.BoolField { displayName = "Show Area Lights", getter = () => data.lightingDebugSettings.showAreaLight, setter = value => data.lightingDebugSettings.showAreaLight = value },
                    new DebugUI.BoolField { displayName = "Show Reflection Probe", getter = () => data.lightingDebugSettings.showReflectionProbe, setter = value => data.lightingDebugSettings.showReflectionProbe = value },
                }
            });

            list.Add(new DebugUI.EnumField { displayName = "Shadow Debug Mode", getter = () => (int)data.lightingDebugSettings.shadowDebugMode, setter = value => SetShadowDebugMode((ShadowMapDebugMode)value), autoEnum = typeof(ShadowMapDebugMode), onValueChanged = RefreshLightingDebug, getIndex = () => data.shadowDebugModeEnumIndex, setIndex = value => data.shadowDebugModeEnumIndex = value });

            if (data.lightingDebugSettings.shadowDebugMode == ShadowMapDebugMode.VisualizeShadowMap || data.lightingDebugSettings.shadowDebugMode == ShadowMapDebugMode.SingleShadow)
            {
                var container = new DebugUI.Container();
                container.children.Add(new DebugUI.BoolField { displayName = "Use Selection", getter = () => data.lightingDebugSettings.shadowDebugUseSelection, setter = value => data.lightingDebugSettings.shadowDebugUseSelection = value, flags = DebugUI.Flags.EditorOnly, onValueChanged = RefreshLightingDebug });

                if (!data.lightingDebugSettings.shadowDebugUseSelection)
                    container.children.Add(new DebugUI.UIntField { displayName = "Shadow Map Index", getter = () => data.lightingDebugSettings.shadowMapIndex, setter = value => data.lightingDebugSettings.shadowMapIndex = value, min = () => 0u, max = () => (uint)(Math.Max(0, (RenderPipelineManager.currentPipeline as HDRenderPipeline).GetCurrentShadowCount() - 1u)) });

                list.Add(container);
            }

            list.Add(new DebugUI.FloatField
            {
                displayName = "Global Shadow Scale Factor",
                getter = () => data.lightingDebugSettings.shadowResolutionScaleFactor,
                setter = (v) => data.lightingDebugSettings.shadowResolutionScaleFactor = v,
                min = () => 0.01f,
                max = () => 4.0f,
            });

            list.Add(new DebugUI.BoolField{
                displayName = "Clear Shadow Atlas",
                getter = () => data.lightingDebugSettings.clearShadowAtlas,
                setter = (v) => data.lightingDebugSettings.clearShadowAtlas = v
            });

            list.Add(new DebugUI.FloatField { displayName = "Shadow Range Minimum Value", getter = () => data.lightingDebugSettings.shadowMinValue, setter = value => data.lightingDebugSettings.shadowMinValue = value });
            list.Add(new DebugUI.FloatField { displayName = "Shadow Range Maximum Value", getter = () => data.lightingDebugSettings.shadowMaxValue, setter = value => data.lightingDebugSettings.shadowMaxValue = value });

            list.Add(new DebugUI.EnumField { displayName = "Lighting Debug Mode", getter = () => (int)data.lightingDebugSettings.debugLightingMode, setter = value => SetDebugLightingMode((DebugLightingMode)value), autoEnum = typeof(DebugLightingMode), onValueChanged = RefreshLightingDebug, getIndex = () => data.lightingDebugModeEnumIndex, setIndex = value => { data.ResetExclusiveEnumIndices(); data.lightingDebugModeEnumIndex = value; } });
            list.Add(new DebugUI.BitField { displayName = "Light Hierarchy Debug Mode", getter = () => data.lightingDebugSettings.debugLightFilterMode, setter = value => SetDebugLightFilterMode((DebugLightFilterMode)value), enumType = typeof(DebugLightFilterMode), onValueChanged = RefreshLightingDebug, });
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
                            new DebugUI.UIntField
                            {
                                displayName = "Fullscreen Debug Mip",
                                getter = () =>
                                    {
                                        int id;
                                        switch (data.fullScreenDebugMode)
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
                                        return (uint)(data.fullscreenDebugMip * lodCount);
                                    },
                                setter = value =>
                                    {
                                        int id;
                                        switch (data.fullScreenDebugMode)
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
                                        data.fullscreenDebugMip = (float)Convert.ChangeType(value, typeof(float)) / lodCount;
                                    },
                                min = () => 0u,
                                max = () =>
                                    {
                                        int id;
                                        switch (data.fullScreenDebugMode)
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
                                min = () => -1, // -1 will display all contact shadow
                                max = () => LightDefinitions.s_LightListMaxPrunedEntries - 1
                            },
                        }
                    });
                    break;
                default:
                    data.fullscreenDebugMip = 0;
                    break;
            }

            list.Add(new DebugUI.BoolField { displayName = "Override Smoothness", getter = () => data.lightingDebugSettings.overrideSmoothness, setter = value => data.lightingDebugSettings.overrideSmoothness = value, onValueChanged = RefreshLightingDebug });
            if (data.lightingDebugSettings.overrideSmoothness)
            {
                list.Add(new DebugUI.Container
                {
                    children =
                    {
                        new DebugUI.FloatField { displayName = "Smoothness", getter = () => data.lightingDebugSettings.overrideSmoothnessValue, setter = value => data.lightingDebugSettings.overrideSmoothnessValue = value, min = () => 0f, max = () => 1f, incStep = 0.025f }
                    }
                });
            }

            list.Add(new DebugUI.BoolField { displayName = "Override Albedo", getter = () => data.lightingDebugSettings.overrideAlbedo, setter = value => data.lightingDebugSettings.overrideAlbedo = value, onValueChanged = RefreshLightingDebug });
            if (data.lightingDebugSettings.overrideAlbedo)
            {
                list.Add(new DebugUI.Container
                {
                    children =
                    {
                        new DebugUI.ColorField { displayName = "Albedo", getter = () => data.lightingDebugSettings.overrideAlbedoValue, setter = value => data.lightingDebugSettings.overrideAlbedoValue = value, showAlpha = false, hdr = false }
                    }
                });
            }

            list.Add(new DebugUI.BoolField { displayName = "Override Normal", getter = () => data.lightingDebugSettings.overrideNormal, setter = value => data.lightingDebugSettings.overrideNormal = value });

            list.Add(new DebugUI.BoolField { displayName = "Override Specular Color", getter = () => data.lightingDebugSettings.overrideSpecularColor, setter = value => data.lightingDebugSettings.overrideSpecularColor = value, onValueChanged = RefreshLightingDebug });
            if (data.lightingDebugSettings.overrideSpecularColor)
            {
                list.Add(new DebugUI.Container
                {
                    children =
                    {
                        new DebugUI.ColorField { displayName = "Specular Color", getter = () => data.lightingDebugSettings.overrideSpecularColorValue, setter = value => data.lightingDebugSettings.overrideSpecularColorValue = value, showAlpha = false, hdr = false }
                    }
                });
            }

            list.Add(new DebugUI.BoolField { displayName = "Override AmbientOcclusion", getter = () => data.lightingDebugSettings.overrideAmbientOcclusion, setter = value => data.lightingDebugSettings.overrideAmbientOcclusion = value, onValueChanged = RefreshLightingDebug });
            if (data.lightingDebugSettings.overrideAmbientOcclusion)
            {
                list.Add(new DebugUI.Container
                {
                    children =
                    {
                        new DebugUI.FloatField { displayName = "AmbientOcclusion", getter = () => data.lightingDebugSettings.overrideAmbientOcclusionValue, setter = value => data.lightingDebugSettings.overrideAmbientOcclusionValue = value, min = () => 0f, max = () => 1f, incStep = 0.025f }
                    }
                });
            }

            list.Add(new DebugUI.BoolField { displayName = "Override Emissive Color", getter = () => data.lightingDebugSettings.overrideEmissiveColor, setter = value => data.lightingDebugSettings.overrideEmissiveColor = value, onValueChanged = RefreshLightingDebug });
            if (data.lightingDebugSettings.overrideEmissiveColor)
            {
                list.Add(new DebugUI.Container
                {
                    children =
                    {
                        new DebugUI.ColorField { displayName = "Emissive Color", getter = () => data.lightingDebugSettings.overrideEmissiveColorValue, setter = value => data.lightingDebugSettings.overrideEmissiveColorValue = value, showAlpha = false, hdr = true }
                    }
                });
            }

            list.Add(new DebugUI.EnumField { displayName = "Tile/Cluster Debug", getter = () => (int)data.lightingDebugSettings.tileClusterDebug, setter = value => data.lightingDebugSettings.tileClusterDebug = (TileClusterDebug)value, autoEnum = typeof(TileClusterDebug), onValueChanged = RefreshLightingDebug, getIndex = () => data.tileClusterDebugEnumIndex, setIndex = value => data.tileClusterDebugEnumIndex = value });
            if (data.lightingDebugSettings.tileClusterDebug != TileClusterDebug.None && data.lightingDebugSettings.tileClusterDebug != TileClusterDebug.MaterialFeatureVariants)
            {
                list.Add(new DebugUI.Container
                {
                    children =
                    {
                        new DebugUI.EnumField { displayName = "Tile/Cluster Debug By Category", getter = () => (int)data.lightingDebugSettings.tileClusterDebugByCategory, setter = value => data.lightingDebugSettings.tileClusterDebugByCategory = (TileClusterCategoryDebug)value, autoEnum = typeof(TileClusterCategoryDebug), getIndex = () => data.tileClusterDebugByCategoryEnumIndex, setIndex = value => data.tileClusterDebugByCategoryEnumIndex = value }
                    }
                });
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
                list.Add(new DebugUI.Container
                {
                    children =
                    {
                        new DebugUI.EnumField { displayName = "Light Volume Debug Type", getter = () => (int)data.lightingDebugSettings.lightVolumeDebugByCategory, setter = value => data.lightingDebugSettings.lightVolumeDebugByCategory = (LightVolumeDebug)value, autoEnum = typeof(LightVolumeDebug), getIndex = () => data.lightVolumeDebugTypeEnumIndex, setIndex = value => data.lightVolumeDebugTypeEnumIndex = value },
                        new DebugUI.UIntField { displayName = "Max Debug Light Count", getter = () => (uint)data.lightingDebugSettings.maxDebugLightCount, setter = value => data.lightingDebugSettings.maxDebugLightCount = value, min = () => 0, max = () => 24, incStep = 1 }
                    }
                });
            }

            if (DebugNeedsExposure() || data.lightingDebugSettings.displaySkyReflection)
                list.Add(new DebugUI.FloatField { displayName = "Debug Exposure", getter = () => data.lightingDebugSettings.debugExposure, setter = value => data.lightingDebugSettings.debugExposure = value });


            m_DebugLightingItems = list.ToArray();
            var panel = DebugManager.instance.GetPanel(k_PanelLighting, true);
            panel.children.Add(m_DebugLightingItems);
        }
        public void RegisterRenderingDebug()
        {
            var widgetList = new List<DebugUI.Widget>();

            widgetList.Add(
                new DebugUI.EnumField { displayName = "Fullscreen Debug Mode", getter = () => (int)data.fullScreenDebugMode, setter = value => data.fullScreenDebugMode = (FullScreenDebugMode)value, onValueChanged = RefreshRenderingDebug, enumNames = s_RenderingFullScreenDebugStrings, enumValues = s_RenderingFullScreenDebugValues, getIndex = () => data.renderingFulscreenDebugModeEnumIndex, setIndex = value => data.renderingFulscreenDebugModeEnumIndex = value }
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
                        new DebugUI.EnumField { displayName = "Terrain Texture", getter = ()=>(int)data.mipMapDebugSettings.terrainTexture, setter = value => data.mipMapDebugSettings.terrainTexture = (DebugMipMapModeTerrainTexture)value, autoEnum = typeof(DebugMipMapModeTerrainTexture), getIndex = () => data.terrainTextureEnumIndex, setIndex = value => data.terrainTextureEnumIndex = value }
                    }
                });
            }

            widgetList.AddRange(new []
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
                widgetList.Add(new DebugUI.Container{
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

            widgetList.AddRange(new DebugUI.Widget[]
            {
                new DebugUI.EnumField { displayName = "MSAA Samples", getter = () => (int)data.msaaSamples, setter = value => data.msaaSamples = (MSAASamples)value, enumNames = s_MsaaSamplesDebugStrings, enumValues = s_MsaaSamplesDebugValues, getIndex = () => data.msaaSampleDebugModeEnumIndex, setIndex = value => data.msaaSampleDebugModeEnumIndex = value },
            });

            widgetList.AddRange(new DebugUI.Widget[]
            {
                new DebugUI.EnumField { displayName = "Freeze Camera for culling", getter = () => data.debugCameraToFreeze, setter = value => data.debugCameraToFreeze = value, enumNames = s_CameraNamesStrings, enumValues = s_CameraNamesValues, getIndex = () => data.debugCameraToFreezeEnumIndex, setIndex = value => data.debugCameraToFreezeEnumIndex = value },
            });

            m_DebugRenderingItems = widgetList.ToArray();
            var panel = DebugManager.instance.GetPanel(k_PanelRendering, true);
            panel.children.Add(m_DebugRenderingItems);
        }

        public void RegisterDecalsDebug()
        {
            m_DebugDecalsItems = new DebugUI.Widget[]
            {
                new DebugUI.BoolField { displayName = "Display Atlas", getter = () => data.decalsDebugSettings.displayAtlas, setter = value => data.decalsDebugSettings.displayAtlas = value},
                new DebugUI.UIntField { displayName = "Mip Level", getter = () => data.decalsDebugSettings.mipLevel, setter = value => data.decalsDebugSettings.mipLevel = value, min = () => 0u, max = () => (uint)(RenderPipelineManager.currentPipeline as HDRenderPipeline).GetDecalAtlasMipCount() }
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
            DebugManager.instance.RegisterData(this);
        }

        public void UnregisterDebug()
        {
            UnregisterDebugItems(k_PanelDecals, m_DebugDecalsItems);
            UnregisterDebugItems(k_PanelDisplayStats, m_DebugDisplayStatsItems);
            UnregisterDebugItems(k_PanelMaterials, m_DebugMaterialItems);
            UnregisterDebugItems(k_PanelLighting, m_DebugLightingItems);
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
    }
}
