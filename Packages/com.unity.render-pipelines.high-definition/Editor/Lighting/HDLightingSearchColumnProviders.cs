using System;
using UnityEditor.Search;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UIElements;
using UnityEngine.Rendering.HighDefinition;

namespace UnityEditor.Rendering.HighDefinition
{
    static class HDLightingSearchColumnProviders
    {
        internal const string k_SceneProvider = "scene";
        internal const string k_LightPath = "Light/";
        internal const string k_LightShapePath = k_LightPath + "ShapeHDRP";
        internal const string k_LightIntensityPath = k_LightPath + "Intensity";
        internal const string k_LightIntensityUnitPath = k_LightPath + "IntensityUnit";
        internal const string k_LightModePath = k_LightPath + "ModeHDRP";
        internal const string k_ContactShadowsPath = k_LightPath + "ContactShadows";
        internal const string k_ShadowResolutionPath = k_LightPath + "ShadowResolution";
        internal const string k_ReflectionProbePath = "ReflectionProbe/";
        internal const string k_ReflectionProbeResolutionPath = k_ReflectionProbePath + "Resolution";
        internal const string k_MeshRendererPath = "MeshRenderer/";
        internal const string k_RayTracingModeFilter = "RayTracingMode";
        internal const string k_RayTracingModePath = k_MeshRendererPath + "RayTracingMode";

        const string k_StyleSheetPath = "StyleSheets/HDLightingSearchColumnProviders.uss";
        const float k_FlexGrowDefault = 1f;
        const float k_ImguiContainerHeight = 20f;
        const float k_RectLeftWidthRatio = 0.3f;
        const float k_RectRightWidthRatio = 0.7f;
        const float k_ToggleWidth = 18f;
        static StyleSheet s_StyleSheet;

        static StyleSheet LoadStyleSheet()
        {
            if (s_StyleSheet == null)
            {
                s_StyleSheet = EditorGUIUtility.Load(k_StyleSheetPath) as StyleSheet;
            }
            return s_StyleSheet;
        }

        internal enum DirectionalLightUnit
        {
            Lux = LightUnit.Lux,
        }

        internal enum AreaLightUnit
        {
            Lumen = LightUnit.Lumen,
            Nits = LightUnit.Nits,
            Ev100 = LightUnit.Ev100,
        }

        internal enum PunctualLightUnit
        {
            Lumen = LightUnit.Lumen,
            Candela = LightUnit.Candela,
            Lux = LightUnit.Lux,
            Ev100 = LightUnit.Ev100
        }

        internal enum ShadowResolutionOption
        {
            Low,
            Medium,
            High,
            Custom
        }

        [SearchColumnProvider(k_LightIntensityPath)]
        public static void LightIntensitySearchColumnProvider(SearchColumn column)
        {
            column.getter = args =>
            {
                var go = args.item.data as GameObject ?? args.item.ToObject<GameObject>();
                if (go == null || !go.TryGetComponent<Light>(out var light))
                    return null;

                return HDLightingSearchDataAccessors.GetLightIntensity(go);
            };
            column.setter = args =>
            {
                if (args.value is not float intensity)
                    return;

                var go = args.item.data as GameObject ?? args.item.ToObject<GameObject>();
                if (go == null)
                    return;

                HDLightingSearchDataAccessors.SetLightIntensity(go, intensity);
            };
            column.cellCreator = _ =>
            {
                var field = new FloatField { label = "\u2022" };
                field.style.flexGrow = k_FlexGrowDefault;
                field.labelElement.style.minWidth = StyleKeyword.Auto;
                field.labelElement.style.marginRight = 5;
                field.labelElement.style.paddingRight = 0;
                field.labelElement.style.marginTop = -1;

                var dragger = new FieldMouseDragger<float>(field);
                dragger.SetDragZone(field.labelElement);

                var textInput = field.Q("unity-text-input");
                if (textInput != null)
                    textInput.style.unityTextAlign = TextAnchor.MiddleRight;

                return field;
            };
            column.binder = (args, ve) =>
            {
                var field = (FloatField)ve;
                var go = args.item.data as GameObject ?? args.item.ToObject<GameObject>();

                if (go == null || !go.TryGetComponent<Light>(out _))
                {
                    field.visible = false;
                    return;
                }

                field.visible = true;

                if (args.value is float intensity)
                {
                    field.SetValueWithoutNotify(intensity);
                }
            };
        }

        [SearchColumnProvider(k_LightIntensityUnitPath)]
        public static void LightIntensityUnitSearchColumnProvider(SearchColumn column)
        {
            column.getter = args =>
            {
                var go = args.item.data as GameObject ?? args.item.ToObject<GameObject>();
                if (go == null)
                    return null;

                return HDLightingSearchDataAccessors.GetLightIntensityUnit(go);
            };
            column.setter = args =>
            {
                if (args.value == null || !args.value.GetType().IsEnum)
                    return;

                var go = args.item.data as GameObject ?? args.item.ToObject<GameObject>();
                if (go == null)
                    return;

                HDLightingSearchDataAccessors.SetLightIntensityUnit(go, (LightUnit)args.value);
            };
            column.cellCreator = _ => new EnumField() { style = { flexGrow = k_FlexGrowDefault } };
            column.binder = (args, ve) =>
            {
                var field = (EnumField)ve;
                var go = args.item.data as GameObject ?? args.item.ToObject<GameObject>();

                if (go == null || !go.TryGetComponent<Light>(out var light))
                {
                    field.visible = false;
                    return;
                }

                field.visible = true;

                if (args.value is LightUnit lightUnit)
                {
                    LightType lightType = light.type;
                    LightUnit validUnit = LightUnitUtils.IsLightUnitSupported(lightType, lightUnit)
                        ? lightUnit
                        : LightUnitUtils.GetNativeLightUnit(lightType);

                    if (lightType == LightType.Directional)
                    {
                        field.Init(DirectionalLightUnit.Lux);
                        field.SetValueWithoutNotify((DirectionalLightUnit)validUnit);
                    }
                    else if (lightType == LightType.Rectangle || lightType == LightType.Disc || lightType == LightType.Tube)
                    {
                        field.Init(AreaLightUnit.Lumen);
                        field.SetValueWithoutNotify((AreaLightUnit)validUnit);
                    }
                    else
                    {
                        field.Init(PunctualLightUnit.Lumen);
                        field.SetValueWithoutNotify((PunctualLightUnit)validUnit);
                    }
                }
            };
        }

        [SearchColumnProvider(k_RayTracingModePath)]
        public static void RayTracingModeSearchColumnProvider(SearchColumn column)
        {
            column.getter = args =>
            {
                var go = args.item.data as GameObject ?? args.item.ToObject<GameObject>();
                if (go == null)
                    return null;

                if (!go.TryGetComponent<MeshRenderer>(out var meshRenderer))
                    return null;

                return HDLightingSearchDataAccessors.GetRayTracingMode(go);
            };
            column.setter = args =>
            {
                if (args.value == null || !args.value.GetType().IsEnum)
                    return;

                var go = args.item.data as GameObject ?? args.item.ToObject<GameObject>();
                if (go == null)
                    return;

                HDLightingSearchDataAccessors.SetRayTracingMode(go, (UnityEngine.Experimental.Rendering.RayTracingMode)args.value);
            };
            column.cellCreator = _ => new EnumField(null, UnityEngine.Experimental.Rendering.RayTracingMode.Off) { style = { flexGrow = k_FlexGrowDefault } };
            column.binder = (args, ve) =>
            {
                var field = (EnumField)ve;
                if (args.value != null)
                {
                    field.visible = true;
                    field.SetValueWithoutNotify((UnityEngine.Experimental.Rendering.RayTracingMode)args.value);
                }
                else
                {
                    field.visible = false;
                }
            };
        }

        [SearchColumnProvider(k_ReflectionProbeResolutionPath)]
        public static void ReflectionProbeResolutionSearchColumnProvider(SearchColumn column)
        {
            column.getter = args =>
            {
                var go = args.item.data as GameObject ?? args.item.ToObject<GameObject>();
                if (go == null || !go.TryGetComponent<HDProbe>(out _))
                    return null;

                return HDLightingSearchDataAccessors.GetReflectionProbeResolution(go);
            };

            column.setter = args =>
            {
                if (args.value is not HDLightingSearchDataAccessors.ReflectionProbeResolutionData data)
                    return;

                var go = args.item.data as GameObject ?? args.item.ToObject<GameObject>();
                if (go == null)
                    return;

                HDLightingSearchDataAccessors.SetReflectionProbeResolution(go, data);
            };

            column.drawer = args =>
            {
                var go = args.item.data as GameObject ?? args.item.ToObject<GameObject>();
                if (go == null || !go.TryGetComponent<HDProbe>(out var hdProbe))
                {
                    return args.value;
                }

                var reflectionProbeResolutionData = (HDLightingSearchDataAccessors.ReflectionProbeResolutionData)args.value;
                switch (hdProbe.type)
                {
                    case ProbeSettings.ProbeType.ReflectionProbe:
                        {
                            var rect = EditorGUILayout.GetControlRect(false, k_ImguiContainerHeight);
                            var leftRect = new Rect(rect.x, rect.y, rect.width * k_RectLeftWidthRatio, rect.height);
                            var rightRect = new Rect(rect.x + rect.width * k_RectLeftWidthRatio, rect.y, rect.width * k_RectRightWidthRatio, rect.height);

                            GUILayout.BeginHorizontal("box", GUILayout.ExpandWidth(true));

                            var (level, useOverride) = SerializedScalableSettingValueUI.LevelFieldGUI(leftRect, GUIContent.none, ScalableSettingSchema.GetSchemaOrNull(ScalableSettingSchemaId.With3Levels), reflectionProbeResolutionData.level, reflectionProbeResolutionData.useOverride);
                            Enum overrideLevel;
                            if (reflectionProbeResolutionData.useOverride)
                            {
                                overrideLevel = EditorGUI.EnumPopup(rightRect, reflectionProbeResolutionData.overrideLevel);
                            }
                            else
                            {
                                using (new EditorGUI.DisabledScope(true))
                                {
                                    overrideLevel = EditorGUI.EnumFlagsField(rightRect, reflectionProbeResolutionData.overrideLevel);
                                }
                            }
                            
                            reflectionProbeResolutionData.level = level;
                            reflectionProbeResolutionData.useOverride = useOverride;
                            reflectionProbeResolutionData.overrideLevel = (CubeReflectionResolution)overrideLevel;

                            GUILayout.EndHorizontal();
                        };
                        break;
                    case ProbeSettings.ProbeType.PlanarProbe:
                    default:
                    {
                        var rect = EditorGUILayout.GetControlRect(false, k_ImguiContainerHeight);
                        var leftRect = new Rect(rect.x, rect.y, rect.width * k_RectLeftWidthRatio, rect.height);
                        var rightRect = new Rect(rect.x + rect.width * k_RectLeftWidthRatio, rect.y, rect.width * k_RectRightWidthRatio, rect.height);

                        GUILayout.BeginHorizontal("box", GUILayout.ExpandWidth(true));

                        var (level, useOverride) = SerializedScalableSettingValueUI.LevelFieldGUI(leftRect, GUIContent.none, ScalableSettingSchema.GetSchemaOrNull(ScalableSettingSchemaId.With3Levels), reflectionProbeResolutionData.level, reflectionProbeResolutionData.useOverride);
                        Enum overrideLevel;
                        if (reflectionProbeResolutionData.useOverride)
                        {
                            overrideLevel = EditorGUI.EnumPopup(rightRect, reflectionProbeResolutionData.overrideLevel);
                        }
                        else
                        {
                            using (new EditorGUI.DisabledScope(true))
                            {
                                overrideLevel = EditorGUI.EnumFlagsField(rightRect, reflectionProbeResolutionData.overrideLevel);
                            }
                        }
                        
                        reflectionProbeResolutionData.level = level;
                        reflectionProbeResolutionData.useOverride = useOverride;
                        reflectionProbeResolutionData.overrideLevel = (CubeReflectionResolution)overrideLevel;

                        GUILayout.EndHorizontal();
                        break;
                    }
                }
                return reflectionProbeResolutionData;
            };
        }

        [SearchColumnProvider(k_ContactShadowsPath)]
        public static void ContactShadowsSearchColumnProvider(SearchColumn column)
        {
            column.getter = args =>
            {
                var go = args.item.data as GameObject ?? args.item.ToObject<GameObject>();
                if (go == null || !go.TryGetComponent<HDAdditionalLightData>(out _))
                    return null;

                return HDLightingSearchDataAccessors.GetContactShadowsData(go);
            };

            column.setter = args =>
            {
                if (args.value is not HDLightingSearchDataAccessors.ContactShadowsData data)
                    return;

                var go = args.item.data as GameObject ?? args.item.ToObject<GameObject>();
                if (go == null)
                    return;

                HDLightingSearchDataAccessors.SetContactShadowsData(go, data);
            };

            column.drawer = args =>
            {
                var go = args.item.data as GameObject ?? args.item.ToObject<GameObject>();
                if (go == null || !go.TryGetComponent<HDAdditionalLightData>(out _))
                {
                    return null;
                }

                var contactShadowsData = (HDLightingSearchDataAccessors.ContactShadowsData)args.value;
                var rect = EditorGUILayout.GetControlRect(false, k_ImguiContainerHeight);
                var leftRect = new Rect(rect.x, rect.y, rect.width - k_ToggleWidth - 4f, rect.height);
                var rightRect = new Rect(rect.xMax - k_ToggleWidth, rect.y, k_ToggleWidth, rect.height);
                var (level, useOverride) = SerializedScalableSettingValueUI.LevelFieldGUI(
                    leftRect,
                    GUIContent.none,
                    ScalableSettingSchema.GetSchemaOrNull(ScalableSettingSchemaId.With3Levels),
                    contactShadowsData.level,
                    contactShadowsData.useOverride);

                contactShadowsData.level = level;
                contactShadowsData.useOverride = useOverride;

                if (contactShadowsData.useOverride)
                {
                    contactShadowsData.overrideValue = EditorGUI.Toggle(rightRect, contactShadowsData.overrideValue);
                }
                else
                {
                    var hdrp = HDRenderPipeline.currentAsset;
                    var defaultValue = HDAdditionalLightData.ScalableSettings.UseContactShadow(hdrp);
                    using (new EditorGUI.DisabledScope(true))
                    {
                        contactShadowsData.overrideValue = EditorGUI.Toggle(rightRect, defaultValue[contactShadowsData.level]);
                    }
                }
                return contactShadowsData;
            };
        }

        [SearchColumnProvider(k_ShadowResolutionPath)]
        public static void ShadowResolutionSearchColumnProvider(SearchColumn column)
        {
            column.getter = args =>
            {
                var go = args.item.data as GameObject ?? args.item.ToObject<GameObject>();
                if (go == null || !go.TryGetComponent<HDAdditionalLightData>(out _))
                    return null;

                return HDLightingSearchDataAccessors.GetShadowResolutionData(go);
            };

            column.setter = args =>
            {
                if (args.value is not ShadowResolutionOption option)
                    return;

                var go = args.item.data as GameObject ?? args.item.ToObject<GameObject>();
                if (go == null)
                    return;

                HDLightingSearchDataAccessors.SetShadowResolutionData(go, option);
            };

            column.drawer = args =>
            {
                var go = args.item.data as GameObject ?? args.item.ToObject<GameObject>();
                if (go == null || !go.TryGetComponent<HDAdditionalLightData>(out _))
                {
                    return null;
                }

                var currentOption = (ShadowResolutionOption)args.value;
                var rect = EditorGUILayout.GetControlRect(false, 20);
                var newOption = (ShadowResolutionOption)EditorGUI.EnumPopup(rect, currentOption);
                return newOption;
            };
        }

        [SearchColumnProvider(k_LightShapePath)]
        public static void LightShapeSearchColumnProvider(SearchColumn column)
        {
            column.getter = args =>
            {
                var go = args.item.data as GameObject ?? args.item.ToObject<GameObject>();
                if (go == null)
                    return null;

                if (!go.TryGetComponent<Light>(out var light))
                    return null;

                return HDLightingSearchDataAccessors.GetLightShape(go);
            };
            column.setter = args =>
            {
                if (args.value == null || !args.value.GetType().IsEnum)
                    return;

                var go = args.item.data as GameObject ?? args.item.ToObject<GameObject>();
                if (go == null)
                    return;

                HDLightingSearchDataAccessors.SetLightShape(go, (LightType)args.value);
            };
            column.cellCreator = _ => new HDLightShapeField();
            column.binder = (args, ve) =>
            {
                var field = (HDLightShapeField)ve;
                if (args.value is LightType lightType)
                {
                    if (HDLightingSearchDataAccessors.IsLightShapeApplicable(lightType))
                    {
                        field.visible = true;
                        field.SetValueWithoutNotify(lightType);
                    }
                    else
                    {
                        field.visible = false;
                    }
                }
                else
                {
                    field.visible = false;
                }
            };
        }

        [SearchColumnProvider(k_LightModePath)]
        public static void LightModeSearchColumnProvider(SearchColumn column)
        {
            column.getter = args =>
            {
                var go = args.item.data as GameObject ?? args.item.ToObject<GameObject>();
                if (go == null)
                    return null;
                return HDLightingSearchDataAccessors.GetLightMode(go);
            };
            column.setter = args =>
            {
                if (args.value == null || !args.value.GetType().IsEnum)
                    return;
                var go = args.item.data as GameObject ?? args.item.ToObject<GameObject>();
                if (go == null)
                    return;
                HDLightingSearchDataAccessors.SetLightMode(go, (LightmapBakeType)args.value);
            };
            column.cellCreator = _ => new EnumField(LightmapBakeType.Realtime) { style = { flexGrow = k_FlexGrowDefault } };
            column.binder = (args, ve) =>
            {
                var field = (EnumField)ve;
                var go = args.item.data as GameObject ?? args.item.ToObject<GameObject>();
                if (go == null || !go.TryGetComponent<Light>(out var light))
                {
                    field.visible = false;
                    return;
                }
                if (args.value is LightmapBakeType bakeType)
                {
                    field.visible = true;
                    field.SetValueWithoutNotify(bakeType);
                    field.SetEnabled(!HDLightingSearchDataAccessors.IsLightModeLocked(light.type));
                }
                else
                {
                    field.visible = false;
                }
            };
        }

        static class HDLightingSearchDataAccessors
        {
            internal struct ReflectionProbeResolutionData
            {
                public int level;
                public bool useOverride;
                public CubeReflectionResolution overrideLevel;
            }

            internal struct ContactShadowsData
            {
                public int level;
                public bool useOverride;
                public bool overrideValue;
            }

            internal static float GetLightIntensity(GameObject go)
            {
                if (!go.TryGetComponent<Light>(out var light))
                    return 0f;

                LightType lightType = light.type;
                LightUnit nativeUnit = LightUnitUtils.GetNativeLightUnit(lightType);
                LightUnit lightUnit = light.lightUnit;
                float nativeIntensity = light.intensity;

                lightUnit = LightUnitUtils.IsLightUnitSupported(lightType, lightUnit) ? lightUnit : nativeUnit;

                return LightUnitUtils.ConvertIntensity(light, nativeIntensity, nativeUnit, lightUnit);
            }

            internal static void SetLightIntensity(GameObject go, float intensity)
            {
                if (!go.TryGetComponent<Light>(out var light))
                    return;

                LightType lightType = light.type;
                LightUnit nativeUnit = LightUnitUtils.GetNativeLightUnit(lightType);
                LightUnit lightUnit = light.lightUnit;

                if (!LightUnitUtils.IsLightUnitSupported(lightType, lightUnit))
                    return;

                Undo.RecordObject(light, "Change light intensity");
                light.intensity = LightUnitUtils.ConvertIntensity(light, intensity, lightUnit, nativeUnit);
            }

            internal static LightUnit GetLightIntensityUnit(GameObject go)
            {
                if (!go.TryGetComponent<Light>(out var light))
                    return LightUnit.Lumen;

                return light.lightUnit;
            }

            internal static void SetLightIntensityUnit(GameObject go, LightUnit unit)
            {
                if (!go.TryGetComponent<Light>(out var light))
                    return;

                if (!LightUnitUtils.IsLightUnitSupported(light.type, unit))
                    return;

                Undo.RecordObject(light, "Change light intensity unit");
                light.lightUnit = unit;

                // Mark the light component as dirty so Unity's change detection system
                // notifies all views (including Search) that this object changed.
                // This triggers the intensity column to rebind and show the converted value.
                EditorUtility.SetDirty(light);
            }

            internal static ReflectionProbeResolutionData GetReflectionProbeResolution(GameObject go)
            {
                var reflectionProbeResolutionData = new ReflectionProbeResolutionData();

                if (!go.TryGetComponent<HDProbe>(out var hdProbe))
                    return reflectionProbeResolutionData;

                switch (hdProbe.type)
                {
                    case ProbeSettings.ProbeType.ReflectionProbe:
                        reflectionProbeResolutionData.useOverride = hdProbe.settingsRaw.cubeResolution.useOverride;
                        reflectionProbeResolutionData.level = hdProbe.settingsRaw.cubeResolution.level;
                        reflectionProbeResolutionData.overrideLevel = hdProbe.settingsRaw.cubeResolution.@override;
                        break;
                    case ProbeSettings.ProbeType.PlanarProbe:
                    default:
                        reflectionProbeResolutionData.useOverride = hdProbe.settingsRaw.resolutionScalable.useOverride;
                        reflectionProbeResolutionData.level = hdProbe.settingsRaw.resolutionScalable.level;
                        reflectionProbeResolutionData.overrideLevel = (CubeReflectionResolution)hdProbe.settingsRaw.resolutionScalable.@override;
                        break;
                }

                return reflectionProbeResolutionData;
            }

            internal static ContactShadowsData GetContactShadowsData(GameObject go)
            {
                var contactShadowsData = new ContactShadowsData();
                if (!go.TryGetComponent<HDAdditionalLightData>(out var lightData))
                    return contactShadowsData;

                contactShadowsData.level = lightData.useContactShadow.level;
                contactShadowsData.useOverride = lightData.useContactShadow.useOverride;
                contactShadowsData.overrideValue = lightData.useContactShadow.@override;
                return contactShadowsData;
            }

            internal static ShadowResolutionOption GetShadowResolutionData(GameObject go)
            {
                if (!go.TryGetComponent<HDAdditionalLightData>(out var lightData))
                    return ShadowResolutionOption.Custom;

                var currentOption = lightData.shadowResolution.useOverride
                    ? ShadowResolutionOption.Custom
                    : lightData.shadowResolution.level switch
                    {
                        <= 0 => ShadowResolutionOption.Low,
                        1 => ShadowResolutionOption.Medium,
                        _ => ShadowResolutionOption.High
                    };
                return currentOption;
            }

            internal static void SetReflectionProbeResolution(GameObject go, ReflectionProbeResolutionData reflectionProbeResolutionData)
            {
                if (!go.TryGetComponent<HDProbe>(out var hdProbe))
                    return;

                Undo.RecordObject(hdProbe, "Change reflection probe resolution");
                switch (hdProbe.type)
                {
                    case ProbeSettings.ProbeType.ReflectionProbe:
                        hdProbe.settingsRaw.cubeResolution.useOverride = reflectionProbeResolutionData.useOverride;
                        hdProbe.settingsRaw.cubeResolution.level = reflectionProbeResolutionData.level;
                        hdProbe.settingsRaw.cubeResolution.@override = reflectionProbeResolutionData.overrideLevel;
                        break;
                    case ProbeSettings.ProbeType.PlanarProbe:
                    default:
                        hdProbe.settingsRaw.resolutionScalable.useOverride = reflectionProbeResolutionData.useOverride;
                        hdProbe.settingsRaw.resolutionScalable.level = reflectionProbeResolutionData.level;
                        hdProbe.settingsRaw.resolutionScalable.@override = (PlanarReflectionAtlasResolution)reflectionProbeResolutionData.overrideLevel;
                        break;
                }
            }

            internal static void SetContactShadowsData(GameObject go, ContactShadowsData contactShadowsData)
            {
                if (!go.TryGetComponent<HDAdditionalLightData>(out var lightData))
                    return;

                Undo.RecordObject(lightData, "Change contact shadow data");
                lightData.useContactShadow.level = contactShadowsData.level;
                lightData.useContactShadow.useOverride = contactShadowsData.useOverride;
                lightData.useContactShadow.@override = contactShadowsData.overrideValue;
            }

            internal static void SetShadowResolutionData(GameObject go, ShadowResolutionOption newOption)
            {
                if (!go.TryGetComponent<HDAdditionalLightData>(out var lightData))
                    return;

                Undo.RecordObject(lightData, "Change shadow resolution");
                if (newOption == ShadowResolutionOption.Custom)
                {
                    lightData.shadowResolution.useOverride = true;
                }
                else
                {
                    lightData.shadowResolution.useOverride = false;
                    lightData.shadowResolution.level = (int)newOption;
                }
                lightData.RefreshCachedShadow();
            }

            internal static UnityEngine.Experimental.Rendering.RayTracingMode GetRayTracingMode(GameObject go)
            {
                if (!go.TryGetComponent<MeshRenderer>(out var meshRenderer))
                    return UnityEngine.Experimental.Rendering.RayTracingMode.Off;

                return meshRenderer.rayTracingMode;
            }

            internal static void SetRayTracingMode(GameObject go, UnityEngine.Experimental.Rendering.RayTracingMode value)
            {
                if (!go.TryGetComponent<MeshRenderer>(out var meshRenderer))
                    return;

                Undo.RecordObject(meshRenderer, "Change ray tracing mode");
                meshRenderer.rayTracingMode = value;
            }

            internal static LightmapBakeType? GetLightMode(GameObject go)
            {
                if (!go.TryGetComponent<Light>(out var light))
                    return null;
                if (light.type == LightType.Tube)
                    return LightmapBakeType.Realtime;
                if (light.type == LightType.Disc)
                    return LightmapBakeType.Baked;
                return light.lightmapBakeType;
            }

            internal static void SetLightMode(GameObject go, LightmapBakeType value)
            {
                if (!go.TryGetComponent<Light>(out var light))
                    return;
                if (light.type == LightType.Tube || light.type == LightType.Disc)
                    return;

                Undo.RecordObject(light, "Change light mode");
                light.lightmapBakeType = value;
                EditorUtility.SetDirty(light);
            }

            internal static bool IsLightModeLocked(LightType type)
            {
                return type == LightType.Tube || type == LightType.Disc;
            }

            internal static LightType? GetLightShape(GameObject go)
            {
                if (!go.TryGetComponent<Light>(out var light))
                    return null;

                return light.type;
            }

            internal static void SetLightShape(GameObject go, LightType value)
            {
                if (!go.TryGetComponent<Light>(out var light))
                    return;

                if (IsLightShapeApplicable(value))
                {
                    Undo.RecordObject(light, "Change light shape");
                    light.type = value;

                    if (!LightUnitUtils.IsLightUnitSupported(value, light.lightUnit))
                        light.lightUnit = LightUnitUtils.GetNativeLightUnit(value);

                    EditorUtility.SetDirty(light);
                }
            }

            internal static bool IsLightShapeApplicable(LightType lightType)
            {
                return IsSpotLight(lightType) || lightType is LightType.Rectangle or LightType.Disc or LightType.Tube;
            }

            internal static bool IsSpotLight(LightType lightType)
            {
                return lightType is LightType.Spot or LightType.Pyramid or LightType.Box;
            }

            internal static bool IsAreaLight(LightType lightType)
            {
                return lightType is LightType.Rectangle or LightType.Disc or LightType.Tube;
            }
        }

        enum SpotLightShape
        {
            Cone = LightType.Spot,
            Pyramid = LightType.Pyramid,
            Box = LightType.Box
        }

        enum AreaLightShape
        {
            Rectangle = LightType.Rectangle,
            Disc = LightType.Disc,
            Tube = LightType.Tube
        }

        class HDLightShapeField : EnumField
        {
            LightType m_Value;

            public HDLightShapeField() : base(SpotLightShape.Cone)
            {
                m_Value = LightType.Spot;

                AddToClassList("hdrp-lighting-search-light-shape");

                var styleSheet = LoadStyleSheet();
                if (styleSheet != null)
                    styleSheets.Add(styleSheet);
            }

            public void SetValueWithoutNotify(LightType newValue)
            {
                if (m_Value == newValue)
                    return;

                m_Value = newValue;
                UpdateEnumField();
            }

            void UpdateEnumField()
            {
                if (HDLightingSearchDataAccessors.IsSpotLight(m_Value))
                {
                    Init((SpotLightShape)m_Value);
                }
                else if (HDLightingSearchDataAccessors.IsAreaLight(m_Value))
                {
                    Init((AreaLightShape)m_Value);
                }
            }
        }
    }
}
