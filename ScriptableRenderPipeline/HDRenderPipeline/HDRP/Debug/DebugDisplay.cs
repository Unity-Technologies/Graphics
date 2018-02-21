using System;
using System.Collections.Generic;
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
        MaxLightingFullScreenDebug,

        // Rendering
        MinRenderingFullScreenDebug,
        MotionVectors,
        NanTracker,
        MaxRenderingFullScreenDebug
    }

    public class DebugDisplaySettings
    {
        public static string k_PanelDisplayStats = "Display Stats";
        public static string k_PanelMaterials = "Material";
        public static string k_PanelLighting = "Lighting";
        public static string k_PanelRendering = "Rendering";

        DebugUI.Widget[] m_DebugDisplayStatsItems;
        DebugUI.Widget[] m_DebugMaterialItems;
        DebugUI.Widget[] m_DebugLightingItems;
        DebugUI.Widget[] m_DebugRenderingItems;

        public float debugOverlayRatio = 0.33f;
        public FullScreenDebugMode  fullScreenDebugMode = FullScreenDebugMode.None;
        public float fullscreenDebugMip = 0.0f;

        public MaterialDebugSettings materialDebugSettings = new MaterialDebugSettings();
        public LightingDebugSettings lightingDebugSettings = new LightingDebugSettings();
        public MipMapDebugSettings mipMapDebugSettings = new MipMapDebugSettings();
        public ColorPickerDebugSettings colorPickerDebugSettings = new ColorPickerDebugSettings();

        public static GUIContent[] lightingFullScreenDebugStrings = null;
        public static int[] lightingFullScreenDebugValues = null;
        public static GUIContent[] renderingFullScreenDebugStrings = null;
        public static int[] renderingFullScreenDebugValues = null;

        public DebugDisplaySettings()
        {
            FillFullScreenDebugEnum(ref lightingFullScreenDebugStrings, ref lightingFullScreenDebugValues, FullScreenDebugMode.MinLightingFullScreenDebug, FullScreenDebugMode.MaxLightingFullScreenDebug);
            FillFullScreenDebugEnum(ref renderingFullScreenDebugStrings, ref renderingFullScreenDebugValues, FullScreenDebugMode.MinRenderingFullScreenDebug, FullScreenDebugMode.MaxRenderingFullScreenDebug);
        }

        public int GetDebugMaterialIndex()
        {
            return materialDebugSettings.GetDebugMaterialIndex();
        }

        public DebugLightingMode GetDebugLightingMode()
        {
            return lightingDebugSettings.debugLightingMode;
        }

        public DebugMipMapMode GetDebugMipMapMode()
        {
            return mipMapDebugSettings.debugMipMapMode;
        }

        public bool IsDebugDisplayEnabled()
        {
            return materialDebugSettings.IsDebugDisplayEnabled() || lightingDebugSettings.IsDebugDisplayEnabled() || mipMapDebugSettings.IsDebugDisplayEnabled();
        }

        public bool IsDebugMaterialDisplayEnabled()
        {
            return materialDebugSettings.IsDebugDisplayEnabled();
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

        public void UpdateMaterials()
        {
            //if (mipMapDebugSettings.debugMipMapMode != 0)
            //    Texture.SetStreamingTextureMaterialDebugProperties();
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
                        new DebugUI.UIntField { displayName = "Shadow Atlas Index", getter = () => lightingDebugSettings.shadowMapIndex, setter = value => lightingDebugSettings.shadowAtlasIndex = value, min = () => 0u, max = () => (uint)(RenderPipelineManager.currentPipeline as HDRenderPipeline).GetShadowAtlasCount() - 1u }
                    }
                });
            }

            list.Add(new DebugUI.FloatField { displayName = "Shadow Range Min Value", getter = () => lightingDebugSettings.shadowMinValue, setter = value => lightingDebugSettings.shadowMinValue = value });
            list.Add(new DebugUI.FloatField { displayName = "Shadow Range Max Value", getter = () => lightingDebugSettings.shadowMaxValue, setter = value => lightingDebugSettings.shadowMaxValue = value });

            list.Add(new DebugUI.EnumField { displayName = "Lighting Debug Mode", getter = () => (int)lightingDebugSettings.debugLightingMode, setter = value => SetDebugLightingMode((DebugLightingMode)value), autoEnum = typeof(DebugLightingMode), onValueChanged = RefreshLightingDebug });
            if (lightingDebugSettings.debugLightingMode == DebugLightingMode.EnvironmentProxyVolume)
            {
                list.Add(new DebugUI.Container
                {
                    children =
                    {
                        new DebugUI.FloatField { displayName = "Debug Environment Proxy Depth Scale", getter = () => lightingDebugSettings.environmentProxyDepthScale, setter = value => lightingDebugSettings.environmentProxyDepthScale = value, min = () => 0.1f, max = () => 50f }
                    }
                });
            }

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
                                            id = HDShaderIDs._GaussianPyramidColorMipSize;
                                            break;
                                        default:
                                            id = HDShaderIDs._DepthPyramidMipSize;
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
                                            id = HDShaderIDs._GaussianPyramidColorMipSize;
                                            break;
                                        default:
                                            id = HDShaderIDs._DepthPyramidMipSize;
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
                                            id = HDShaderIDs._GaussianPyramidColorMipSize;
                                            break;
                                        default:
                                            id = HDShaderIDs._DepthPyramidMipSize;
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

        public void RegisterDebug()
        {
            RegisterDisplayStatsDebug();
            RegisterMaterialDebug();
            RegisterLightingDebug();
            RegisterRenderingDebug();
        }

        public void UnregisterDebug()
        {
            UnregisterDebugItems(k_PanelDisplayStats, m_DebugDisplayStatsItems);
            UnregisterDebugItems(k_PanelMaterials, m_DebugMaterialItems);
            UnregisterDebugItems(k_PanelLighting, m_DebugLightingItems);
            UnregisterDebugItems(k_PanelRendering, m_DebugLightingItems);
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
