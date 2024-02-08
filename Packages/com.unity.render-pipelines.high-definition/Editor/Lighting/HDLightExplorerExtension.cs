using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using Object = UnityEngine.Object;
using RenderingLayerMask = UnityEngine.Rendering.HighDefinition.RenderingLayerMask;

namespace UnityEditor.Rendering.HighDefinition
{
    [SupportedOnRenderPipeline(typeof(HDRenderPipelineAsset))]
    class HDLightExplorerExtension : DefaultLightingExplorerExtension
    {
        struct LightData
        {
            public HDAdditionalLightData hdAdditionalLightData;
            public bool isPrefab;
            public Object prefabRoot;

            public LightData(HDAdditionalLightData hdAdditionalLightData, bool isPrefab, Object prefabRoot)
            {
                this.hdAdditionalLightData = hdAdditionalLightData;
                this.isPrefab = isPrefab;
                this.prefabRoot = prefabRoot;
            }
        }

        struct VolumeData
        {
            public bool isGlobal;
            public bool hasVisualEnvironment;
            public VolumeProfile profile;
            public bool fogEnabled;
            public bool volumetricEnabled;
            public int skyType;

            public VolumeData(bool isGlobal, VolumeProfile profile)
            {
                this.isGlobal = isGlobal;
                this.profile = profile;
                VisualEnvironment visualEnvironment = null;
                Fog fog = null;
                this.hasVisualEnvironment = profile != null ? profile.TryGet(out visualEnvironment) : false;
                bool hasFog = profile != null ? profile.TryGet(out fog) : false;
                this.skyType = this.hasVisualEnvironment ? visualEnvironment.skyType.value : 0;
                this.fogEnabled = hasFog ? fog.enabled.value : false;
                this.volumetricEnabled = hasFog ? fog.enableVolumetricFog.value : false;
            }
        }

        static Dictionary<Light, LightData> lightDataPairing = new Dictionary<Light, LightData>();

        static Dictionary<Volume, VolumeData> volumeDataPairing = new Dictionary<Volume, VolumeData>();

        static Dictionary<ReflectionProbe, SerializedObject> serializedReflectionProbeDataPairing = new Dictionary<ReflectionProbe, SerializedObject>();

        protected static class HDStyles
        {
            public static readonly GUIContent Name = EditorGUIUtility.TrTextContent("Name");
            public static readonly GUIContent Enabled = EditorGUIUtility.TrTextContent("Enabled");
            public static readonly GUIContent Type = EditorGUIUtility.TrTextContent("Type");
            public static readonly GUIContent LightShape = EditorGUIUtility.TrTextContent("Shape");
            public static readonly GUIContent Shape = EditorGUIUtility.TrTextContent("Shape");
            public static readonly GUIContent Mode = EditorGUIUtility.TrTextContent("Mode");
            public static readonly GUIContent Range = EditorGUIUtility.TrTextContent("Range");
            public static readonly GUIContent Color = EditorGUIUtility.TrTextContent("Color");
            public static readonly GUIContent ColorFilter = EditorGUIUtility.TrTextContent("Color Filter");
            public static readonly GUIContent Intensity = EditorGUIUtility.TrTextContent("Intensity");
            public static readonly GUIContent IndirectMultiplier = EditorGUIUtility.TrTextContent("Indirect Multiplier");
            public static readonly GUIContent Unit = EditorGUIUtility.TrTextContent("Unit");
            public static readonly GUIContent ColorTemperature = EditorGUIUtility.TrTextContent("Color Temperature");
            public static readonly GUIContent Shadows = EditorGUIUtility.TrTextContent("Shadows");
            public static readonly GUIContent ContactShadowsLevel = EditorGUIUtility.TrTextContent("Contact Shadows Level");
            public static readonly GUIContent ContactShadowsValue = EditorGUIUtility.TrTextContent("Contact Shadows Value");
            public static readonly GUIContent ShadowResolutionLevel = EditorGUIUtility.TrTextContent("Shadows Resolution Level");
            public static readonly GUIContent ShadowUpdateMode = EditorGUIUtility.TrTextContent("Shadows Update Mode");
            public static readonly GUIContent ShadowFitAtlas = EditorGUIUtility.TrTextContent("Shadows Fit Atlas");
            public static readonly GUIContent ShadowResolutionValue = EditorGUIUtility.TrTextContent("Shadows Resolution Value");
            public static readonly GUIContent ShapeWidth = EditorGUIUtility.TrTextContent("Shape Width");
            public static readonly GUIContent VolumeProfile = EditorGUIUtility.TrTextContent("Volume Profile");
            public static readonly GUIContent ColorTemperatureMode = EditorGUIUtility.TrTextContent("Use Color Temperature");
            public static readonly GUIContent AffectDiffuse = EditorGUIUtility.TrTextContent("Affect Diffuse");
            public static readonly GUIContent AffectSpecular = EditorGUIUtility.TrTextContent("Affect Specular");
            public static readonly GUIContent FadeDistance = EditorGUIUtility.TrTextContent("Fade Distance");
            public static readonly GUIContent ShadowFadeDistance = EditorGUIUtility.TrTextContent("Shadow Fade Distance");
            public static readonly GUIContent LightLayer = EditorGUIUtility.TrTextContent("Light Layer");
            public static readonly GUIContent IsPrefab = EditorGUIUtility.TrTextContent("Prefab");

            public static readonly GUIContent VolumeMode = EditorGUIUtility.TrTextContent("Mode");
            public static readonly GUIContent Priority = EditorGUIUtility.TrTextContent("Priority");
            public static readonly GUIContent HasVisualEnvironment = EditorGUIUtility.TrTextContent("Has Visual Environment");
            public static readonly GUIContent Fog = EditorGUIUtility.TrTextContent("Fog");
            public static readonly GUIContent Volumetric = EditorGUIUtility.TrTextContent("Volumetric");
            public static readonly GUIContent SkyType = EditorGUIUtility.TrTextContent("Sky Type");

            public static readonly GUIContent ShadowDistance = EditorGUIUtility.TrTextContent("Shadow Distance");
            public static readonly GUIContent NearClip = EditorGUIUtility.TrTextContent("Near Clip");
            public static readonly GUIContent FarClip = EditorGUIUtility.TrTextContent("Far Clip");
            public static readonly GUIContent Resolution = EditorGUIUtility.TrTextContent("Resolution Level");
            public static readonly GUIContent CustomResolution = EditorGUIUtility.TrTextContent("Resolution Value");
            public static readonly GUIContent ParallaxCorrection = EditorGUIUtility.TrTextContent("Influence Volume as Proxy Volume");
            public static readonly GUIContent Weight = EditorGUIUtility.TrTextContent("Weight");

            public static readonly GUIContent[] LightTypeTitles = { EditorGUIUtility.TrTextContent("Spot"), EditorGUIUtility.TrTextContent("Directional"), EditorGUIUtility.TrTextContent("Point"), EditorGUIUtility.TrTextContent("Area") };
            public static readonly int[] LightTypeValues = { (int)LightType.Spot, (int)LightType.Directional, (int)LightType.Point, (int)LightType.Rectangle };
            internal static readonly GUIContent DrawProbes = EditorGUIUtility.TrTextContent("Draw");
            internal static readonly GUIContent DebugColor = EditorGUIUtility.TrTextContent("Debug Color");
            internal static readonly GUIContent ResolutionX = EditorGUIUtility.TrTextContent("Resolution X");
            internal static readonly GUIContent ResolutionY = EditorGUIUtility.TrTextContent("Resolution Y");
            internal static readonly GUIContent ResolutionZ = EditorGUIUtility.TrTextContent("Resolution Z");
            internal static readonly GUIContent FadeStart = EditorGUIUtility.TrTextContent("Fade Start");
            internal static readonly GUIContent FadeEnd = EditorGUIUtility.TrTextContent("Fade End");

            internal static readonly GUIContent OverrideProbeSpacing = EditorGUIUtility.TrTextContent("Override Spacing");
            internal static readonly GUIContent OverrideRendererFilters = EditorGUIUtility.TrTextContent("Override Filter");
            internal static readonly GUIContent FillEmptySpaces = EditorGUIUtility.TrTextContent("Fill Empty Spaces");

            public static readonly GUIContent[] globalModes = { new GUIContent("Global"), new GUIContent("Local") };
        }

        RenderPipelineSettings.LightProbeSystem currentLightProbeMode;
        public override LightingExplorerTab[] GetContentTabs()
        {
            currentLightProbeMode = HDRenderPipeline.currentAsset.currentPlatformRenderPipelineSettings.lightProbeSystem;
            return new[]
            {
                new LightingExplorerTab("Lights", GetHDLights, GetHDLightColumns, true),
                new LightingExplorerTab("Volumes", GetVolumes, GetVolumeColumns, true),
                new LightingExplorerTab("Reflection Probes", GetHDReflectionProbes, GetHDReflectionProbeColumns, true),
                new LightingExplorerTab("Planar Reflection Probes", GetPlanarReflections, GetPlanarReflectionColumns, true),
                new LightingExplorerTab("Light Probes", GetHDLightProbes, GetHDLightProbeColumns, true),
                new LightingExplorerTab("Emissive Materials", GetEmissives, GetEmissivesColumns, false)
            };
        }

        void RefreshTabsForLightProbeModeIfNeeded()
        {
            if (currentLightProbeMode == HDRenderPipeline.currentAsset.currentPlatformRenderPipelineSettings.lightProbeSystem)
                return;

            var type = System.Type.GetType("UnityEditor.LightingExplorerWindow,UnityEditor");
            var explorer = EditorWindow.GetWindow(type);
            var tabs = type.GetField("m_TableTabs", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);

            tabs.SetValue(explorer, GetContentTabs());
        }

        protected virtual UnityEngine.Object[] GetHDLights()
        {
#if UNITY_2020_1_OR_NEWER
            var lights = Resources.FindObjectsOfTypeAll<Light>();
#else
            var lights = UnityEngine.Object.FindObjectsOfType<Light>();
#endif

            foreach (Light light in lights)
            {
                if (PrefabUtility.GetCorrespondingObjectFromSource(light) != null) // We have a prefab
                {
                    lightDataPairing[light] = new LightData(light.GetComponent<HDAdditionalLightData>(), true, PrefabUtility.GetCorrespondingObjectFromSource(PrefabUtility.GetOutermostPrefabInstanceRoot(light.gameObject)));
                }
                else
                {
                    lightDataPairing[light] = new LightData(light.GetComponent<HDAdditionalLightData>(), false, null);
                }
            }
            return lights;
        }

        protected virtual UnityEngine.Object[] GetHDReflectionProbes()
        {
#if UNITY_2020_1_OR_NEWER
            var reflectionProbes = Resources.FindObjectsOfTypeAll<ReflectionProbe>();
#else
            var reflectionProbes = UnityEngine.Object.FindObjectsOfType<ReflectionProbe>();
#endif

            foreach (ReflectionProbe probe in reflectionProbes)
            {
                HDAdditionalReflectionData hdAdditionalReflectionData = probe.GetComponent<HDAdditionalReflectionData>();
                serializedReflectionProbeDataPairing[probe] = hdAdditionalReflectionData != null ? new SerializedObject(hdAdditionalReflectionData) : null;
            }
            return reflectionProbes;
        }

        protected virtual UnityEngine.Object[] GetPlanarReflections()
        {
#if UNITY_2020_1_OR_NEWER
            return Resources.FindObjectsOfTypeAll<PlanarReflectionProbe>();
#else
            return UnityEngine.Object.FindObjectsOfType<PlanarReflectionProbe>();
#endif
        }

        protected virtual UnityEngine.Object[] GetVolumes()
        {
#if UNITY_2020_1_OR_NEWER
            var volumes = Resources.FindObjectsOfTypeAll<Volume>();
#else
            var volumes = UnityEngine.Object.FindObjectsOfType<Volume>();
#endif

            foreach (var volume in volumes)
            {
                volumeDataPairing[volume] = !volume.HasInstantiatedProfile() && volume.sharedProfile == null
                    ? new VolumeData(volume.isGlobal, null)
                    : new VolumeData(volume.isGlobal, volume.HasInstantiatedProfile() ? volume.profile : volume.sharedProfile);
            }
            return volumes;
        }

        protected virtual UnityEngine.Object[] GetHDLightProbes()
        {
            RefreshTabsForLightProbeModeIfNeeded();
            if (!HDRenderPipeline.currentAsset.currentPlatformRenderPipelineSettings.supportProbeVolume)
                return base.GetLightProbes();
            return GetObjectsForLightingExplorer<ProbeVolume>().ToArray();
        }

        protected virtual LightingExplorerTableColumn[] GetHDLightColumns()
        {
            return new[]
            {
                new LightingExplorerTableColumn(LightingExplorerTableColumn.DataType.Checkbox, HDStyles.Enabled, "m_Enabled", 60),                                  // 0: Enabled
                new LightingExplorerTableColumn(LightingExplorerTableColumn.DataType.Name, HDStyles.Name, null, 200),                                               // 1: Name
                new LightingExplorerTableColumn(LightingExplorerTableColumn.DataType.Enum, HDStyles.Type, "m_Type", 100, (r, prop, dep) =>                          // 2: Type
                {
                    if (!TryGetAdditionalLightData(prop, out var lightData))
                    {
                        EditorGUI.LabelField(r, "--");
                        return;
                    }

                    HDLightUI.LightArchetype archetype = HDLightUI.GetArchetype(lightData.legacyLight.type);

                    EditorGUI.BeginProperty(r, GUIContent.none, prop);
                    EditorGUI.BeginChangeCheck();
                    archetype = (HDLightUI.LightArchetype)EditorGUI.EnumPopup(r, archetype);

                    if (EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObjects(new Object[] { prop.serializedObject.targetObject, lightData }, "Changed light type");
                        switch (archetype)
                        {
                            case HDLightUI.LightArchetype.Spot:
                                lightData.legacyLight.type = LightType.Spot;
                                break;
                            case HDLightUI.LightArchetype.Directional:
                                lightData.legacyLight.type = LightType.Directional;
                                break;
                            case HDLightUI.LightArchetype.Point:
                                lightData.legacyLight.type = LightType.Point;
                                break;
                            case HDLightUI.LightArchetype.Area:
                                lightData.legacyLight.type = LightType.Rectangle;
                                break;
                            default:
                                throw new ArgumentOutOfRangeException();
                        }
                    }
                    EditorGUI.EndProperty();
                }, (lprop, rprop) =>
                    {
                        TryGetAdditionalLightData(lprop, out var lLightData);
                        TryGetAdditionalLightData(rprop, out var rLightData);

                        if (IsNullComparison(lLightData, rLightData, out var order))
                            return order;

                        return ((int)lLightData.legacyLight.type).CompareTo((int)rLightData.legacyLight.type);
                    }, (target, source) =>
                    {
                        if (!TryGetAdditionalLightData(target, out var tLightData) || !TryGetAdditionalLightData(source, out var sLightData))
                            return;

                        Undo.RecordObjects(new Object[] { target.serializedObject.targetObject, tLightData }, "Changed light type");
                        tLightData.legacyLight.type = sLightData.legacyLight.type;
                    }),
                new LightingExplorerTableColumn(LightingExplorerTableColumn.DataType.Enum, HDStyles.LightShape, "m_Type", 100, (r, prop, dep) =>                          // 3: LightShape
                {
                    if (!TryGetAdditionalLightData(prop, out var lightData))
                    {
                        EditorGUI.LabelField(r, "--");
                        return;
                    }


                    if (lightData.legacyLight.type.IsSpot())
                    {
                        HDLightUI.SpotSubtype spotSubType = HDLightUI.SpotSubtype.Cone;
                        if (lightData.legacyLight.type == LightType.Spot)
                        {
                            spotSubType = HDLightUI.SpotSubtype.Cone;
                        }
                        else if (lightData.legacyLight.type == LightType.Pyramid)
                        {
                            spotSubType = HDLightUI.SpotSubtype.Pyramid;
                        }
                        else if (lightData.legacyLight.type == LightType.Box)
                        {
                            spotSubType = HDLightUI.SpotSubtype.Box;
                        }

                        EditorGUI.BeginProperty(r, GUIContent.none, prop);
                        EditorGUI.BeginChangeCheck();
                        spotSubType = (HDLightUI.SpotSubtype)EditorGUI.EnumPopup(r, spotSubType);

                        if (EditorGUI.EndChangeCheck())
                        {
                            Undo.RecordObjects(new Object[] { prop.serializedObject.targetObject, lightData }, "Changed light shape");
                            switch (spotSubType)
                            {
                                case HDLightUI.SpotSubtype.Cone:
                                    lightData.legacyLight.type = LightType.Spot;
                                    break;
                                case HDLightUI.SpotSubtype.Pyramid:
                                    lightData.legacyLight.type = LightType.Pyramid;
                                    break;
                                case HDLightUI.SpotSubtype.Box:
                                    lightData.legacyLight.type = LightType.Box;
                                    break;
                                default:
                                    throw new ArgumentOutOfRangeException();
                            }
                        }
                        EditorGUI.EndProperty();
                    }
                    else if (lightData.legacyLight.type.IsArea())
                    {
                        HDLightUI.AreaSubtype areaSubType = HDLightUI.AreaSubtype.Rectangle;
                        if (lightData.legacyLight.type == LightType.Rectangle)
                        {
                            areaSubType = HDLightUI.AreaSubtype.Rectangle;
                        }
                        else if (lightData.legacyLight.type == LightType.Tube)
                        {
                            areaSubType = HDLightUI.AreaSubtype.Tube;
                        }
                        else if (lightData.legacyLight.type == LightType.Disc)
                        {
                            areaSubType = HDLightUI.AreaSubtype.Disc;
                        }

                        EditorGUI.BeginProperty(r, GUIContent.none, prop);
                        EditorGUI.BeginChangeCheck();
                        areaSubType = (HDLightUI.AreaSubtype)EditorGUI.EnumPopup(r, areaSubType);

                        if (EditorGUI.EndChangeCheck())
                        {
                            Undo.RecordObjects(new Object[] { prop.serializedObject.targetObject, lightData }, "Changed light shape");
                            switch (areaSubType)
                            {
                                case HDLightUI.AreaSubtype.Rectangle:
                                    lightData.legacyLight.type = LightType.Rectangle;
                                    break;
                                case HDLightUI.AreaSubtype.Tube:
                                    lightData.legacyLight.type = LightType.Tube;
                                    break;
                                case HDLightUI.AreaSubtype.Disc:
                                    lightData.legacyLight.type = LightType.Disc;
                                    break;
                                default:
                                    throw new ArgumentOutOfRangeException();
                            }
                        }
                        EditorGUI.EndProperty();
                    }
                    else
                    {
                        EditorGUI.LabelField(r, "--");
                        return;
                    }
                }, (lprop, rprop) =>
                    {
                        TryGetAdditionalLightData(lprop, out var lLightData);
                        TryGetAdditionalLightData(rprop, out var rLightData);

                        if (IsNullComparison(lLightData, rLightData, out var order))
                            return order;

                        return ((int)lLightData.legacyLight.type).CompareTo((int)rLightData.legacyLight.type);
                    }, (target, source) =>
                    {
                        if (!TryGetAdditionalLightData(target, out var tLightData) || !TryGetAdditionalLightData(source, out var sLightData))
                            return;

                        Undo.RecordObjects(new Object[] { target.serializedObject.targetObject, tLightData }, "Changed light shape");
                        tLightData.legacyLight.type = sLightData.legacyLight.type;
                    }),

                new LightingExplorerTableColumn(LightingExplorerTableColumn.DataType.Enum, HDStyles.Mode, "m_Lightmapping", 90),                                    // 4: Mixed mode
                new LightingExplorerTableColumn(LightingExplorerTableColumn.DataType.Float, HDStyles.Range, "m_Range", 60),                                         // 5: Range
                new LightingExplorerTableColumn(LightingExplorerTableColumn.DataType.Color, HDStyles.Color, "m_Color", 60),                                         // 6: Color
                new LightingExplorerTableColumn(LightingExplorerTableColumn.DataType.Checkbox, HDStyles.ColorTemperatureMode, "m_UseColorTemperature", 150),        // 7: Color Temperature Mode
                new LightingExplorerTableColumn(LightingExplorerTableColumn.DataType.Float, HDStyles.ColorTemperature, "m_ColorTemperature", 120, (r, prop, dep) => // 8: Color Temperature
                {
                    // Sometimes during scene transition, the target object can be null, causing exceptions.
                    using (new EditorGUI.DisabledScope(prop.serializedObject.targetObject == null || !prop.serializedObject.FindProperty("m_UseColorTemperature").boolValue))
                    {
                        EditorGUI.BeginChangeCheck();
                        EditorGUI.PropertyField(r, prop, GUIContent.none);
                        if (EditorGUI.EndChangeCheck())
                            TemperatureSliderUIDrawer.ClampValue(prop);
                    }
                }, (lprop, rprop) =>
                    {
                        float lTemp = lprop.serializedObject.FindProperty("m_UseColorTemperature").boolValue ? lprop.floatValue : 0.0f;
                        float rTemp = rprop.serializedObject.FindProperty("m_UseColorTemperature").boolValue ? rprop.floatValue : 0.0f;

                        return lTemp.CompareTo(rTemp);
                    }),
                new LightingExplorerTableColumn(LightingExplorerTableColumn.DataType.Float, HDStyles.Intensity, "m_Intensity", 60, (r, prop, dep) =>                // 9: Intensity
                {
                    if (!TryGetAdditionalLightData(prop, out var lightData))
                    {
                        EditorGUI.LabelField(r, "--");
                        return;
                    }

                    LightType lightType = lightData.legacyLight.type;
                    LightUnit nativeUnit = LightUnitUtils.GetNativeLightUnit(lightType);
                    LightUnit lightUnit = lightData.legacyLight.lightUnit;
                    float nativeIntensity = lightData.legacyLight.intensity;

                    // Verify that ui light unit is in fact supported or revert to native.
                    lightUnit = LightUnitUtils.IsLightUnitSupported(lightType, lightUnit) ? lightUnit : nativeUnit;

                    float curIntensity = LightUnitUtils.ConvertIntensity(lightData.legacyLight, nativeIntensity, nativeUnit, lightUnit);

                    EditorGUI.BeginProperty(r, GUIContent.none, prop);
                    EditorGUI.BeginChangeCheck();
                    float newIntensity = EditorGUI.FloatField(r, curIntensity);
                    if (EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObjects(new Object[] { prop.serializedObject.targetObject, lightData }, "Changed light intensity");
                        lightData.legacyLight.intensity = LightUnitUtils.ConvertIntensity(lightData.legacyLight, newIntensity, lightUnit, nativeUnit);
                    }
                    EditorGUI.EndProperty();
                }, (lprop, rprop) =>
                    {
                        TryGetAdditionalLightData(lprop, out var lLightData);
                        TryGetAdditionalLightData(rprop, out var rLightData);

                        if (IsNullComparison(lLightData, rLightData, out var order))
                            return order;

                        return ((float)lLightData.legacyLight.intensity).CompareTo((float)rLightData.legacyLight.intensity);
                    }, (target, source) =>
                    {
                        if (!TryGetAdditionalLightData(target, out var tLightData) || !TryGetAdditionalLightData(source, out var sLightData))
                            return;

                        Undo.RecordObjects(new Object[] { target.serializedObject.targetObject, tLightData }, "Changed light intensity");
                        tLightData.legacyLight.intensity = sLightData.legacyLight.intensity;
                    }),
                new LightingExplorerTableColumn(LightingExplorerTableColumn.DataType.Enum, HDStyles.Unit, "m_Intensity", 70, (r, prop, dep) =>                      // 10: Unit
                {
                    if (!TryGetAdditionalLightData(prop, out var lightData))
                    {
                        EditorGUI.LabelField(r, "--");
                        return;
                    }

                    EditorGUI.BeginChangeCheck();

                    LightUnit unit = lightData.legacyLight.lightUnit;
                    unit = LightUI.DrawLightIntensityUnitPopup(r, unit, lightData.legacyLight.type);

                    if (EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObject(lightData, "Changed light unit");
                        lightData.legacyLight.lightUnit = unit;
                    }
                }, (lprop, rprop) =>
                    {
                        TryGetAdditionalLightData(lprop, out var lLightData);
                        TryGetAdditionalLightData(rprop, out var rLightData);

                        if (IsNullComparison(lLightData, rLightData, out var order))
                            return order;

                        return ((int)lLightData.legacyLight.lightUnit).CompareTo((int)rLightData.legacyLight.lightUnit);
                    }, (target, source) =>
                    {
                        if (!TryGetAdditionalLightData(target, out var tLightData) || !TryGetAdditionalLightData(source, out var sLightData))
                            return;

                        if (!LightUnitUtils.IsLightUnitSupported(tLightData.legacyLight.type, sLightData.legacyLight.lightUnit))
                            return;

                        Undo.RecordObject(tLightData, "Changed light unit");
                        tLightData.legacyLight.lightUnit = sLightData.legacyLight.lightUnit;
                    }),
                new LightingExplorerTableColumn(LightingExplorerTableColumn.DataType.Float, HDStyles.IndirectMultiplier, "m_BounceIntensity", 115),                 // 11: Indirect multiplier
                new LightingExplorerTableColumn(LightingExplorerTableColumn.DataType.Checkbox, HDStyles.Shadows, "m_Shadows.m_Type", 60, (r, prop, dep) =>          // 12: Shadows
                {
                    EditorGUI.BeginChangeCheck();
                    bool shadows = EditorGUI.Toggle(r, prop.intValue != (int)LightShadows.None);
                    if (EditorGUI.EndChangeCheck())
                    {
                        prop.intValue = shadows ? (int)LightShadows.Soft : (int)LightShadows.None;
                    }
                }),
                new LightingExplorerTableColumn(LightingExplorerTableColumn.DataType.Enum, HDStyles.ShadowResolutionLevel, "m_Intensity", 130, (r, prop, dep) =>         // 13: Shadow Resolution level
                {
                    if (!TryGetAdditionalLightData(prop, out var lightData))
                    {
                        EditorGUI.LabelField(r, "--");
                        return;
                    }

                    var shadowResolution = lightData.shadowResolution;

                    EditorGUI.BeginChangeCheck();
                    var(level, useOverride) = SerializedScalableSettingValueUI.LevelFieldGUI(r, GUIContent.none, ScalableSettingSchema.GetSchemaOrNull(ScalableSettingSchemaId.With4Levels), shadowResolution.level, shadowResolution.useOverride);
                    if (EditorGUI.EndChangeCheck())
                    {
                        lightData.RefreshCachedShadow();
                        Undo.RecordObject(lightData, "Changed shadow resolution");
                        shadowResolution.level = level;
                        shadowResolution.useOverride = useOverride;
                    }
                }, (lprop, rprop) =>
                    {
                        TryGetAdditionalLightData(lprop, out var lLightData);
                        TryGetAdditionalLightData(rprop, out var rLightData);

                        if (IsNullComparison(lLightData, rLightData, out var order))
                            return order;

                        return ((int)lLightData.shadowResolution.level).CompareTo((int)rLightData.shadowResolution.level);
                    }, (target, source) =>
                    {
                        if (!TryGetAdditionalLightData(target, out var tLightData) || !TryGetAdditionalLightData(source, out var sLightData))
                            return;

                        tLightData.RefreshCachedShadow();
                        Undo.RecordObject(tLightData, "Changed shadow resolution");
                        tLightData.shadowResolution.level = sLightData.shadowResolution.level;
                        tLightData.shadowResolution.useOverride = sLightData.shadowResolution.useOverride;
                    }),
                new LightingExplorerTableColumn(LightingExplorerTableColumn.DataType.Enum, HDStyles.ShadowUpdateMode, "m_Intensity", 130, (r, prop, dep) =>         // 14: Shadow Update mode level
                {
                    if (!TryGetAdditionalLightData(prop, out var lightData))
                    {
                        EditorGUI.LabelField(r, "--");
                        return;
                    }

                    var shadowUpdateMode = lightData.shadowUpdateMode;

                    EditorGUI.BeginChangeCheck();
                    shadowUpdateMode = (ShadowUpdateMode)EditorGUI.EnumPopup(r, shadowUpdateMode);
                    if (EditorGUI.EndChangeCheck())
                    {
                        lightData.RefreshCachedShadow();
                        Undo.RecordObject(lightData, "Changed shadow update mode");
                        lightData.shadowUpdateMode = shadowUpdateMode;
                    }
                }, (lprop, rprop) =>
                    {
                        TryGetAdditionalLightData(lprop, out var lLightData);
                        TryGetAdditionalLightData(rprop, out var rLightData);

                        if (IsNullComparison(lLightData, rLightData, out var order))
                            return order;

                        return ((int)lLightData.shadowUpdateMode).CompareTo((int)rLightData.shadowUpdateMode);
                    }, (target, source) =>
                    {
                        if (!TryGetAdditionalLightData(target, out var tLightData) || !TryGetAdditionalLightData(source, out var sLightData))
                            return;
                        tLightData.RefreshCachedShadow();

                        Undo.RecordObject(tLightData, "Changed shadow update mode");
                        tLightData.shadowUpdateMode = sLightData.shadowUpdateMode;
                    }),
                new LightingExplorerTableColumn(LightingExplorerTableColumn.DataType.Int, HDStyles.ShadowFitAtlas, "m_Intensity", 130, (r, prop, dep) =>         // 15: Shadow Fit atlas
                {
                    if (!TryGetAdditionalLightData(prop, out var lightData))
                    {
                        EditorGUI.LabelField(r, "--");
                        return;
                    }
                    var hdrp = HDRenderPipeline.currentAsset;

                    var shadowResolution = lightData.shadowResolution;
                    int shadowRes = 0;
                    var lightType = lightData.legacyLight.type;

                    if (shadowResolution.useOverride)
                    {
                        shadowRes = shadowResolution.@override;
                    }
                    else
                    {
                        var defaultValue = HDLightUI.ScalableSettings.ShadowResolution(lightType, hdrp);
                        shadowRes = defaultValue[shadowResolution.level];
                    }

                    if (lightData.ShadowIsUpdatedEveryFrame() || HDCachedShadowManager.instance.LightHasBeenPlacedInAtlas(lightData))
                    {
                        EditorGUI.LabelField(r, "Yes");
                    }
                    else
                    {
                        EditorGUI.LabelField(r, "No");
                    }
                    return;
                }, (lprop, rprop) =>
                    {
                        TryGetAdditionalLightData(lprop, out var lLightData);
                        TryGetAdditionalLightData(rprop, out var rLightData);

                        if (IsNullComparison(lLightData, rLightData, out var order))
                            return order;

                        var hdrp = HDRenderPipeline.currentAsset;

                        var lightType = lLightData.legacyLight.type;

                        bool lFit = lLightData.ShadowIsUpdatedEveryFrame() || HDCachedShadowManager.instance.LightHasBeenPlacedInAtlas(lLightData);

                        lightType = rLightData.legacyLight.type;

                        bool rFit = rLightData.ShadowIsUpdatedEveryFrame() || HDCachedShadowManager.instance.LightHasBeenPlacedInAtlas(rLightData);

                        return rFit.CompareTo(lFit);
                    }),
                new LightingExplorerTableColumn(LightingExplorerTableColumn.DataType.Int, HDStyles.ShadowResolutionValue, "m_Intensity", 130, (r, prop, dep) =>          // 16: Shadow resolution override
                {
                    var hdrp = HDRenderPipeline.currentAsset;

                    if (!TryGetAdditionalLightData(prop, out var lightData, out var light) || hdrp == null)
                    {
                        EditorGUI.LabelField(r, "--");
                        return;
                    }

                    var shadowResolution = lightData.shadowResolution;
                    if (shadowResolution.useOverride)
                    {
                        var overrideShadowResolution = shadowResolution.@override;

                        EditorGUI.BeginChangeCheck();
                        overrideShadowResolution = EditorGUI.IntField(r, overrideShadowResolution);
                        if (EditorGUI.EndChangeCheck())
                        {
                            lightData.RefreshCachedShadow();
                            Undo.RecordObject(lightData, "Changed shadow resolution override");
                            shadowResolution.@override = overrideShadowResolution;
                        }
                    }
                    else
                    {
                        var lightType = lightData.legacyLight.type;
                        var defaultValue = HDLightUI.ScalableSettings.ShadowResolution(lightType, hdrp);

                        using (new EditorGUI.DisabledScope(true))
                        {
                            EditorGUI.IntField(r, defaultValue[shadowResolution.level]);
                        }
                    }
                }, (lprop, rprop) =>
                    {
                        TryGetAdditionalLightData(lprop, out var lLightData, out var lLight);
                        TryGetAdditionalLightData(rprop, out var rLightData, out var rLight);

                        if (IsNullComparison(lLightData, rLightData, out var order))
                            return order;

                        var hdrp = GraphicsSettings.currentRenderPipeline as HDRenderPipelineAsset;
                        var lShadowResolution = lLightData.shadowResolution;
                        var rShadowResolution = rLightData.shadowResolution;
                        var lLightShape = lLightData.legacyLight.type;
                        var rLightShape = rLightData.legacyLight.type;

                        int lResolution = lShadowResolution.useOverride ? lShadowResolution.@override : (hdrp == null ? -1 : HDLightUI.ScalableSettings.ShadowResolution(lLightShape, hdrp)[lShadowResolution.level]);
                        int rResolution = rShadowResolution.useOverride ? rShadowResolution.@override : (hdrp == null ? -1 : HDLightUI.ScalableSettings.ShadowResolution(rLightShape, hdrp)[rShadowResolution.level]);

                        return lResolution.CompareTo(rResolution);
                    }, (target, source) =>
                    {
                        if (!TryGetAdditionalLightData(target, out var tLightData) || !TryGetAdditionalLightData(source, out var sLightData))
                            return;

                        var tShadowResolution = tLightData.shadowResolution;
                        var sShadowResolution = sLightData.shadowResolution;

                        if (tShadowResolution.useOverride)
                        {
                            tLightData.RefreshCachedShadow();
                            Undo.RecordObject(tLightData, "Changed shadow resolution override");
                            tShadowResolution.@override = sShadowResolution.@override;
                        }
                    }),
                new LightingExplorerTableColumn(LightingExplorerTableColumn.DataType.Enum, HDStyles.ContactShadowsLevel, "m_Shadows.m_Type", 115, (r, prop, dep) =>      // 17: Contact Shadows level
                {
                    if (!TryGetAdditionalLightData(prop, out var lightData))
                    {
                        EditorGUI.LabelField(r, "--");
                        return;
                    }

                    var useContactShadow = lightData.useContactShadow;
                    EditorGUI.BeginChangeCheck();
                    var(level, useOverride) = SerializedScalableSettingValueUI.LevelFieldGUI(r, GUIContent.none, ScalableSettingSchema.GetSchemaOrNull(ScalableSettingSchemaId.With3Levels), useContactShadow.level, useContactShadow.useOverride);
                    if (EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObject(lightData, "Changed contact shadows");
                        useContactShadow.level = level;
                        useContactShadow.useOverride = useOverride;
                    }
                }, (lprop, rprop) =>
                    {
                        TryGetAdditionalLightData(lprop, out var lLightData);
                        TryGetAdditionalLightData(rprop, out var rLightData);

                        if (IsNullComparison(lLightData, rLightData, out var order))
                            return order;

                        return (lLightData.useContactShadow.useOverride ? -1 : (int)lLightData.useContactShadow.level).CompareTo(rLightData.useContactShadow.useOverride ? -1 : (int)rLightData.useContactShadow.level);
                    }, (target, source) =>
                    {
                        if (!TryGetAdditionalLightData(target, out var tLightData) || !TryGetAdditionalLightData(source, out var sLightData))
                            return;

                        Undo.RecordObject(tLightData, "Changed contact shadows");
                        tLightData.useContactShadow.level = sLightData.useContactShadow.level;
                        tLightData.useContactShadow.useOverride = sLightData.useContactShadow.useOverride;
                    }),
                new LightingExplorerTableColumn(LightingExplorerTableColumn.DataType.Checkbox, HDStyles.ContactShadowsValue, "m_Shadows.m_Type", 115, (r, prop, dep) =>  // 18: Contact Shadows override
                {
                    if (!TryGetAdditionalLightData(prop, out var lightData))
                    {
                        EditorGUI.LabelField(r, "--");
                        return;
                    }

                    var useContactShadow = lightData.useContactShadow;

                    if (useContactShadow.useOverride)
                    {
                        var overrideUseContactShadows = useContactShadow.@override;

                        EditorGUI.BeginChangeCheck();
                        overrideUseContactShadows = EditorGUI.Toggle(r, overrideUseContactShadows);
                        if (EditorGUI.EndChangeCheck())
                        {
                            Undo.RecordObject(lightData, "Changed contact shadow override");
                            useContactShadow.@override = overrideUseContactShadows;

                            //SceneView don't update when interacting with Light Explorer when playing and pausing (1354129)
                            if (EditorApplication.isPlaying && EditorApplication.isPaused)
                                SceneView.RepaintAll();
                        }
                    }
                    else
                    {
                        var hdrp = HDRenderPipeline.currentAsset;
                        var defaultValue = HDAdditionalLightData.ScalableSettings.UseContactShadow(hdrp);

                        using (new EditorGUI.DisabledScope(true))
                        {
                            EditorGUI.Toggle(r, defaultValue[useContactShadow.level]);
                        }
                    }
                }, (lprop, rprop) =>
                    {
                        TryGetAdditionalLightData(lprop, out var lLightData);
                        TryGetAdditionalLightData(rprop, out var rLightData);

                        if (IsNullComparison(lLightData, rLightData, out var order))
                            return order;

                        var hdrp = GraphicsSettings.currentRenderPipeline as HDRenderPipelineAsset;
                        var lUseContactShadow = lLightData.useContactShadow;
                        var rUseContactShadow = rLightData.useContactShadow;

                        bool lEnabled = lUseContactShadow.useOverride ? lUseContactShadow.@override : HDAdditionalLightData.ScalableSettings.UseContactShadow(hdrp)[lUseContactShadow.level];
                        bool rEnabled = rUseContactShadow.useOverride ? rUseContactShadow.@override : HDAdditionalLightData.ScalableSettings.UseContactShadow(hdrp)[rUseContactShadow.level];

                        return lEnabled.CompareTo(rEnabled);
                    }, (target, source) =>
                    {
                        if (!TryGetAdditionalLightData(target, out var tLightData) || !TryGetAdditionalLightData(source, out var sLightData))
                            return;

                        var hdrp = GraphicsSettings.currentRenderPipeline as HDRenderPipelineAsset;
                        var tUseContactShadow = tLightData.useContactShadow;
                        var sUseContactShadow = sLightData.useContactShadow;

                        if (tUseContactShadow.useOverride)
                        {
                            Undo.RecordObject(tLightData, "Changed contact shadow override");
                            tUseContactShadow.@override = sUseContactShadow.@override;
                        }
                    }),
                new LightingExplorerTableColumn(LightingExplorerTableColumn.DataType.Checkbox, HDStyles.AffectDiffuse, "m_Intensity", 95, (r, prop, dep) =>         // 19: Affect Diffuse
                {
                    if (!TryGetAdditionalLightData(prop, out var lightData))
                    {
                        EditorGUI.LabelField(r, "--");
                        return;
                    }

                    bool affectDiffuse = lightData.affectDiffuse;

                    EditorGUI.BeginChangeCheck();
                    affectDiffuse = EditorGUI.Toggle(r, affectDiffuse);
                    if (EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObject(lightData, "Changed affects diffuse");
                        lightData.affectDiffuse = affectDiffuse;

                        //SceneView don't update when interacting with Light Explorer when playing and pausing (1354129)
                        if (EditorApplication.isPlaying && EditorApplication.isPaused)
                            SceneView.RepaintAll();
                    }
                }, (lprop, rprop) =>
                    {
                        TryGetAdditionalLightData(lprop, out var lLightData);
                        TryGetAdditionalLightData(rprop, out var rLightData);

                        if (IsNullComparison(lLightData, rLightData, out var order))
                            return order;

                        return lLightData.affectDiffuse.CompareTo(rLightData.affectDiffuse);
                    }, (target, source) =>
                    {
                        if (!TryGetAdditionalLightData(target, out var tLightData) || !TryGetAdditionalLightData(source, out var sLightData))
                            return;

                        Undo.RecordObject(tLightData, "Changed affects diffuse");
                        tLightData.affectDiffuse = sLightData.affectDiffuse;
                    }),
                new LightingExplorerTableColumn(LightingExplorerTableColumn.DataType.Checkbox, HDStyles.AffectSpecular, "m_Intensity", 100, (r, prop, dep) =>       // 20: Affect Specular
                {
                    if (!TryGetAdditionalLightData(prop, out var lightData))
                    {
                        EditorGUI.LabelField(r, "--");
                        return;
                    }

                    bool affectSpecular = lightData.affectSpecular;

                    EditorGUI.BeginChangeCheck();
                    affectSpecular = EditorGUI.Toggle(r, affectSpecular);
                    if (EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObject(lightData, "Changed affects specular");
                        lightData.affectSpecular = affectSpecular;

                        //SceneView don't update when interacting with Light Explorer when playing and pausing (1354129)
                        if (EditorApplication.isPlaying && EditorApplication.isPaused)
                            SceneView.RepaintAll();
                    }
                }, (lprop, rprop) =>
                    {
                        TryGetAdditionalLightData(lprop, out var lLightData);
                        TryGetAdditionalLightData(rprop, out var rLightData);

                        if (IsNullComparison(lLightData, rLightData, out var order))
                            return order;

                        return lLightData.affectSpecular.CompareTo(rLightData.affectSpecular);
                    }, (target, source) =>
                    {
                        if (!TryGetAdditionalLightData(target, out var tLightData) || !TryGetAdditionalLightData(source, out var sLightData))
                            return;

                        Undo.RecordObject(tLightData, "Changed affects specular");
                        tLightData.affectSpecular = sLightData.affectSpecular;
                    }),
                new LightingExplorerTableColumn(LightingExplorerTableColumn.DataType.Float, HDStyles.FadeDistance, "m_Intensity", 95, (r, prop, dep) =>             // 21: Fade Distance
                {
                    if (!TryGetAdditionalLightData(prop, out var lightData))
                    {
                        EditorGUI.LabelField(r, "--");
                        return;
                    }

                    float fadeDistance = lightData.fadeDistance;

                    EditorGUI.BeginChangeCheck();
                    fadeDistance = EditorGUI.FloatField(r, fadeDistance);
                    if (EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObject(lightData, "Changed light fade distance");
                        lightData.fadeDistance = fadeDistance;
                    }
                }, (lprop, rprop) =>
                    {
                        TryGetAdditionalLightData(lprop, out var lLightData);
                        TryGetAdditionalLightData(rprop, out var rLightData);

                        if (IsNullComparison(lLightData, rLightData, out var order))
                            return order;

                        return lLightData.fadeDistance.CompareTo(rLightData.fadeDistance);
                    }, (target, source) =>
                    {
                        if (!TryGetAdditionalLightData(target, out var tLightData) || !TryGetAdditionalLightData(source, out var sLightData))
                            return;

                        Undo.RecordObject(tLightData, "Changed light fade distance");
                        tLightData.fadeDistance = sLightData.fadeDistance;
                    }),
                new LightingExplorerTableColumn(LightingExplorerTableColumn.DataType.Float, HDStyles.ShadowFadeDistance, "m_Intensity", 145, (r, prop, dep) =>      // 22: Shadow Fade Distance
                {
                    if (!TryGetAdditionalLightData(prop, out var lightData))
                    {
                        EditorGUI.LabelField(r, "--");
                        return;
                    }

                    float shadowFadeDistance = lightData.shadowFadeDistance;

                    EditorGUI.BeginChangeCheck();
                    shadowFadeDistance = EditorGUI.FloatField(r, shadowFadeDistance);
                    if (EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObject(lightData, "Changed light shadow fade distance");
                        lightData.shadowFadeDistance = shadowFadeDistance;
                    }
                }, (lprop, rprop) =>
                    {
                        TryGetAdditionalLightData(lprop, out var lLightData);
                        TryGetAdditionalLightData(rprop, out var rLightData);

                        if (IsNullComparison(lLightData, rLightData, out var order))
                            return order;

                        return lLightData.shadowFadeDistance.CompareTo(rLightData.shadowFadeDistance);
                    }, (target, source) =>
                    {
                        if (!TryGetAdditionalLightData(target, out var tLightData) || !TryGetAdditionalLightData(source, out var sLightData))
                            return;

                        Undo.RecordObject(tLightData, "Changed light shadow fade distance");
                        tLightData.shadowFadeDistance = sLightData.shadowFadeDistance;
                    }),
                new LightingExplorerTableColumn(LightingExplorerTableColumn.DataType.Custom, HDStyles.LightLayer, "m_RenderingLayerMask", 145, (r, prop, dep) =>    // 23: Light Layer
                {
                    using (new EditorGUI.DisabledScope(!HDRenderPipeline.currentAsset.currentPlatformRenderPipelineSettings.supportLightLayers))
                    {
                        if (!TryGetAdditionalLightData(prop, out var lightData))
                        {
                            EditorGUI.LabelField(r, "--");
                            return;
                        }

                        var lightLayerMask = (uint)lightData.lightlayersMask;
                        EditorGUI.BeginChangeCheck();
                        lightLayerMask = HDEditorUtils.DrawRenderingLayerMask(r, lightLayerMask, null, false);
                        if (EditorGUI.EndChangeCheck())
                        {
                            Undo.RecordObject(lightData, "Changed light layer");
                            lightData.lightlayersMask = (RenderingLayerMask)lightLayerMask;
                        }
                    }
                }, (lprop, rprop) =>
                    {
                        TryGetAdditionalLightData(lprop, out var lLightData);
                        TryGetAdditionalLightData(rprop, out var rLightData);

                        if (IsNullComparison(lLightData, rLightData, out var order))
                            return order;

                        return ((int)lLightData.lightlayersMask).CompareTo((int)rLightData.lightlayersMask);
                    }, (target, source) =>
                    {
                        if (!TryGetAdditionalLightData(target, out var tLightData) || !TryGetAdditionalLightData(source, out var sLightData))
                            return;

                        Undo.RecordObject(tLightData, "Changed light layer");
                        tLightData.lightlayersMask = sLightData.lightlayersMask;
                    }),
                new LightingExplorerTableColumn(LightingExplorerTableColumn.DataType.Custom, HDStyles.IsPrefab, "m_Intensity", 120, (r, prop, dep) =>               // 24: Prefab
                {
                    if (!TryGetLightPrefabData(prop, out var isPrefab, out var prefabRoot))
                        return;

                    if (isPrefab)
                    {
                        using (new EditorGUI.DisabledScope(true))
                        {
                            EditorGUI.ObjectField(r, prefabRoot, typeof(GameObject), false);
                        }
                    }
                }, (lprop, rprop) =>
                    {
                        TryGetLightPrefabData(lprop, out var lIsPrefab, out var lPrefabRoot);
                        TryGetLightPrefabData(rprop, out var rIsPrefab, out var rPrefabRoot);

                        if (IsNullComparison(lPrefabRoot, rPrefabRoot, out var order))
                            return order;

                        return EditorUtility.NaturalCompare(lPrefabRoot.name, rPrefabRoot.name);
                    }),
            };
        }

        protected virtual LightingExplorerTableColumn[] GetVolumeColumns()
        {
            return new[]
            {
                new LightingExplorerTableColumn(LightingExplorerTableColumn.DataType.Checkbox, HDStyles.Enabled, "m_Enabled", 60),                                      // 0: Enabled
                new LightingExplorerTableColumn(LightingExplorerTableColumn.DataType.Name, HDStyles.Name, null, 200),                                                   // 1: Name
                new LightingExplorerTableColumn(LightingExplorerTableColumn.DataType.Enum, HDStyles.VolumeMode, "m_IsGlobal", 75, (r, prop, dep) =>                       // 2: Is Global
                {
                    if (!TryGetAdditionalVolumeData(prop, out var volumeData))
                    {
                        EditorGUI.LabelField(r, "--");
                        return;
                    }

                    int isGlobal = volumeData.isGlobal ? 0 : 1;
                    EditorGUI.BeginChangeCheck();
                    isGlobal = EditorGUI.Popup(r, isGlobal, HDStyles.globalModes);
                    if (EditorGUI.EndChangeCheck())
                        prop.boolValue = isGlobal == 0;
                }, (lprop, rprop) =>
                    {
                        bool lHasVolume = TryGetAdditionalVolumeData(lprop, out var lVolumeData);
                        bool rHasVolume = TryGetAdditionalVolumeData(rprop, out var rVolumeData);

                        return (lHasVolume ? lVolumeData.isGlobal : false).CompareTo((rHasVolume ? rVolumeData.isGlobal : false));
                    }),
                new LightingExplorerTableColumn(LightingExplorerTableColumn.DataType.Float, HDStyles.Priority, "priority", 60),                                         // 3: Priority
                new LightingExplorerTableColumn(LightingExplorerTableColumn.DataType.Custom, HDStyles.VolumeProfile, "sharedProfile", 200, (r, prop, dep) =>            // 4: Profile
                {
                    EditorGUI.PropertyField(r, prop, GUIContent.none);
                }, (lprop, rprop) =>
                    {
                        return EditorUtility.NaturalCompare(((lprop == null || lprop.objectReferenceValue == null) ? "--" : lprop.objectReferenceValue.name), ((rprop == null || rprop.objectReferenceValue == null) ? "--" : rprop.objectReferenceValue.name));
                    }),
                new LightingExplorerTableColumn(LightingExplorerTableColumn.DataType.Checkbox, HDStyles.HasVisualEnvironment, "sharedProfile", 150, (r, prop, dep) =>   // 5: Has Visual environment
                {
                    if (!TryGetAdditionalVolumeData(prop, out var volumeData))
                    {
                        EditorGUI.LabelField(r, "--");
                        return;
                    }

                    using (new EditorGUI.DisabledScope(true))
                    {
                        EditorGUI.Toggle(r, volumeData.hasVisualEnvironment);
                    }
                }, (lprop, rprop) =>
                    {
                        bool lHasVolume = TryGetAdditionalVolumeData(lprop, out var lVolumeData);
                        bool rHasVolume = TryGetAdditionalVolumeData(rprop, out var rVolumeData);

                        return (lHasVolume ? System.Convert.ToInt32(lVolumeData.hasVisualEnvironment) : -1).CompareTo((rHasVolume ? System.Convert.ToInt32(rVolumeData.hasVisualEnvironment) : -1));
                    }),
                new LightingExplorerTableColumn(LightingExplorerTableColumn.DataType.Enum, HDStyles.SkyType, "sharedProfile", 75, (r, prop, dep) =>                     // 6: Sky type
                {
                    if (!TryGetAdditionalVolumeData(prop, out var volumeData))
                    {
                        EditorGUI.LabelField(r, "--");
                        return;
                    }

                    using (new EditorGUI.DisabledScope(true))
                    {
                        EditorGUI.IntPopup(r, volumeData.skyType, VisualEnvironmentEditor.skyClassNames.ToArray(), VisualEnvironmentEditor.skyUniqueIDs.ToArray());
                    }
                }, (lprop, rprop) =>
                    {
                        bool lHasVolume = TryGetAdditionalVolumeData(lprop, out var lVolumeData);
                        bool rHasVolume = TryGetAdditionalVolumeData(rprop, out var rVolumeData);

                        return (lHasVolume ? (int)lVolumeData.skyType : -1).CompareTo((rHasVolume ? (int)rVolumeData.skyType : -1));
                    }),
                new LightingExplorerTableColumn(LightingExplorerTableColumn.DataType.Checkbox, HDStyles.Fog, "sharedProfile", 50, (r, prop, dep) =>                     // 8: Fog enabled
                {
                    if (!TryGetAdditionalVolumeData(prop, out var volumeData))
                    {
                        EditorGUI.LabelField(r, "--");
                        return;
                    }

                    using (new EditorGUI.DisabledScope(true))
                    {
                        EditorGUI.Toggle(r, volumeData.fogEnabled);
                    }
                }, (lprop, rprop) =>
                    {
                        bool lHasVolume = TryGetAdditionalVolumeData(lprop, out var lVolumeData);
                        bool rHasVolume = TryGetAdditionalVolumeData(rprop, out var rVolumeData);

                        return (lHasVolume ? lVolumeData.fogEnabled : false).CompareTo((rHasVolume ? rVolumeData.fogEnabled : false));
                    }),
                new LightingExplorerTableColumn(LightingExplorerTableColumn.DataType.Checkbox, HDStyles.Volumetric, "sharedProfile", 95, (r, prop, dep) =>                  // 9: Volumetric enabled
                {
                    if (!TryGetAdditionalVolumeData(prop, out var volumeData))
                    {
                        EditorGUI.LabelField(r, "--");
                        return;
                    }

                    using (new EditorGUI.DisabledScope(true))
                    {
                        EditorGUI.Toggle(r, volumeData.volumetricEnabled);
                    }
                }, (lprop, rprop) =>
                    {
                        bool lHasVolume = TryGetAdditionalVolumeData(lprop, out var lVolumeData);
                        bool rHasVolume = TryGetAdditionalVolumeData(rprop, out var rVolumeData);

                        return (lHasVolume ? lVolumeData.volumetricEnabled : false).CompareTo((rHasVolume ? rVolumeData.volumetricEnabled : false));
                    })
            };
        }

        protected virtual LightingExplorerTableColumn[] GetHDReflectionProbeColumns()
        {
            return new[]
            {
                new LightingExplorerTableColumn(LightingExplorerTableColumn.DataType.Checkbox, HDStyles.Enabled, "m_Enabled", 60),                                      // 0: Enabled
                new LightingExplorerTableColumn(LightingExplorerTableColumn.DataType.Name, HDStyles.Name, null, 200),                                                   // 1: Name
                new LightingExplorerTableColumn(LightingExplorerTableColumn.DataType.Enum, HDStyles.Mode, "m_Mode", 80, (r, prop, dep) =>                               // 2: Mode
                {
                    if (!TryGetAdditionalReflectionData(prop, out var reflectionData))
                    {
                        EditorGUI.LabelField(r, "--");
                        return;
                    }

                    reflectionData.Update();
                    EditorGUI.PropertyField(r, reflectionData.FindProperty("m_ProbeSettings.mode"), GUIContent.none);
                    reflectionData.ApplyModifiedProperties();
                }, (lprop, rprop) =>
                    {
                        TryGetAdditionalReflectionData(lprop, out var lReflectionData);
                        TryGetAdditionalReflectionData(rprop, out var rReflectionData);

                        if (IsNullComparison(lReflectionData, rReflectionData, out var order))
                            return order;

                        return lReflectionData.FindProperty("m_ProbeSettings.mode").intValue.CompareTo(rReflectionData.FindProperty("m_ProbeSettings.mode").intValue);
                    }, (target, source) =>
                    {
                        if (!TryGetAdditionalReflectionData(target, out var tReflectionData) || !TryGetAdditionalReflectionData(source, out var sReflectionData))
                            return;

                        tReflectionData.Update();
                        tReflectionData.FindProperty("m_ProbeSettings.mode").intValue = sReflectionData.FindProperty("m_ProbeSettings.mode").intValue;
                        tReflectionData.ApplyModifiedProperties();
                    }),
                new LightingExplorerTableColumn(LightingExplorerTableColumn.DataType.Enum, HDStyles.Shape, "m_Mode", 70, (r, prop, dep) =>                              // 3: Shape
                {
                    if (!TryGetAdditionalReflectionData(prop, out var reflectionData))
                    {
                        EditorGUI.LabelField(r, "--");
                        return;
                    }

                    reflectionData.Update();
                    EditorGUI.PropertyField(r, reflectionData.FindProperty("m_ProbeSettings.influence.m_Shape"), GUIContent.none);
                    reflectionData.ApplyModifiedProperties();
                }, (lprop, rprop) =>
                    {
                        TryGetAdditionalReflectionData(lprop, out var lReflectionData);
                        TryGetAdditionalReflectionData(rprop, out var rReflectionData);

                        if (IsNullComparison(lReflectionData, rReflectionData, out var order))
                            return order;

                        return lReflectionData.FindProperty("m_ProbeSettings.influence.m_Shape").intValue.CompareTo(rReflectionData.FindProperty("m_ProbeSettings.influence.m_Shape").intValue);
                    }, (target, source) =>
                    {
                        if (!TryGetAdditionalReflectionData(target, out var tReflectionData) || !TryGetAdditionalReflectionData(source, out var sReflectionData))
                            return;

                        tReflectionData.Update();
                        tReflectionData.FindProperty("m_ProbeSettings.influence.m_Shape").intValue = sReflectionData.FindProperty("m_ProbeSettings.influence.m_Shape").intValue;
                        tReflectionData.ApplyModifiedProperties();
                    }),
                new LightingExplorerTableColumn(LightingExplorerTableColumn.DataType.Float, HDStyles.NearClip, "m_NearClip", 65, (r, prop, dep) =>                      // 4: Near clip
                {
                    if (!TryGetAdditionalReflectionData(prop, out var reflectionData))
                    {
                        EditorGUI.LabelField(r, "--");
                        return;
                    }

                    reflectionData.Update();
                    EditorGUI.PropertyField(r, reflectionData.FindProperty("m_ProbeSettings.cameraSettings.frustum.nearClipPlaneRaw"), GUIContent.none);
                    reflectionData.ApplyModifiedProperties();
                }, (lprop, rprop) =>
                    {
                        TryGetAdditionalReflectionData(lprop, out var lReflectionData);
                        TryGetAdditionalReflectionData(rprop, out var rReflectionData);

                        if (IsNullComparison(lReflectionData, rReflectionData, out var order))
                            return order;

                        return lReflectionData.FindProperty("m_ProbeSettings.cameraSettings.frustum.nearClipPlaneRaw").floatValue.CompareTo(rReflectionData.FindProperty("m_ProbeSettings.cameraSettings.frustum.nearClipPlaneRaw").floatValue);
                    }, (target, source) =>
                    {
                        if (!TryGetAdditionalReflectionData(target, out var tReflectionData) || !TryGetAdditionalReflectionData(source, out var sReflectionData))
                            return;

                        tReflectionData.Update();
                        tReflectionData.FindProperty("m_ProbeSettings.cameraSettings.frustum.nearClipPlaneRaw").floatValue = sReflectionData.FindProperty("m_ProbeSettings.cameraSettings.frustum.nearClipPlaneRaw").floatValue;
                        tReflectionData.ApplyModifiedProperties();
                    }),
                new LightingExplorerTableColumn(LightingExplorerTableColumn.DataType.Float, HDStyles.FarClip, "m_FarClip", 60, (r, prop, dep) =>                        // 5: Far clip
                {
                    if (!TryGetAdditionalReflectionData(prop, out var reflectionData))
                    {
                        EditorGUI.LabelField(r, "--");
                        return;
                    }

                    reflectionData.Update();
                    EditorGUI.PropertyField(r, reflectionData.FindProperty("m_ProbeSettings.cameraSettings.frustum.farClipPlaneRaw"), GUIContent.none);
                    reflectionData.ApplyModifiedProperties();
                }, (lprop, rprop) =>
                    {
                        TryGetAdditionalReflectionData(lprop, out var lReflectionData);
                        TryGetAdditionalReflectionData(rprop, out var rReflectionData);

                        if (IsNullComparison(lReflectionData, rReflectionData, out var order))
                            return order;

                        return lReflectionData.FindProperty("m_ProbeSettings.cameraSettings.frustum.farClipPlaneRaw").floatValue.CompareTo(rReflectionData.FindProperty("m_ProbeSettings.cameraSettings.frustum.farClipPlaneRaw").floatValue);
                    }, (target, source) =>
                    {
                        if (!TryGetAdditionalReflectionData(target, out var tReflectionData) || !TryGetAdditionalReflectionData(source, out var sReflectionData))
                            return;

                        tReflectionData.Update();
                        tReflectionData.FindProperty("m_ProbeSettings.cameraSettings.frustum.farClipPlaneRaw").floatValue = sReflectionData.FindProperty("m_ProbeSettings.cameraSettings.frustum.farClipPlaneRaw").floatValue;
                        tReflectionData.ApplyModifiedProperties();
                    }),
                new LightingExplorerTableColumn(LightingExplorerTableColumn.DataType.Enum, HDStyles.Resolution, "m_Resolution", 130, (r, prop, dep) =>                        // 6: Resolution
                {
                    if (!TryGetAdditionalReflectionData(prop, out var reflectionData))
                    {
                        EditorGUI.LabelField(r, "--");
                        return;
                    }

                    reflectionData.Update();

                    EditorGUI.BeginChangeCheck();
                    var(level, useOverride) = SerializedScalableSettingValueUI.LevelFieldGUI(r, GUIContent.none, ScalableSettingSchema.GetSchemaOrNull(ScalableSettingSchemaId.With3Levels), reflectionData.FindProperty("m_ProbeSettings.cubeResolution.m_Level").intValue, reflectionData.FindProperty("m_ProbeSettings.cubeResolution.m_UseOverride").boolValue);
                    if(EditorGUI.EndChangeCheck())
                    {
                        reflectionData.FindProperty("m_ProbeSettings.cubeResolution.m_Level").intValue = level;
                        reflectionData.FindProperty("m_ProbeSettings.cubeResolution.m_UseOverride").boolValue = useOverride;
                    }
                    reflectionData.ApplyModifiedProperties();
                }, (lprop, rprop) =>
                    {
                        TryGetAdditionalReflectionData(lprop, out var lReflectionData);
                        TryGetAdditionalReflectionData(rprop, out var rReflectionData);

                        if (IsNullComparison(lReflectionData, rReflectionData, out var order))
                            return order;

                        return lReflectionData.FindProperty("m_ProbeSettings.cubeResolution.m_Level").intValue.CompareTo(rReflectionData.FindProperty("m_ProbeSettings.cubeResolution.m_Level").intValue);
                    }, (target, source) =>
                    {
                        if (!TryGetAdditionalReflectionData(target, out var tReflectionData) || !TryGetAdditionalReflectionData(source, out var sReflectionData))
                            return;

                        tReflectionData.Update();
                        tReflectionData.FindProperty("m_ProbeSettings.cubeResolution.m_Level").intValue = sReflectionData.FindProperty("m_ProbeSettings.cubeResolution.m_Level").intValue;
                        tReflectionData.FindProperty("m_ProbeSettings.cubeResolution.m_UseOverride").boolValue = sReflectionData.FindProperty("m_ProbeSettings.cubeResolution.m_UseOverride").boolValue;
                        tReflectionData.ApplyModifiedProperties();
                    }),
                new LightingExplorerTableColumn(LightingExplorerTableColumn.DataType.Enum, HDStyles.CustomResolution, "m_Resolution", 130, (r, prop, dep) =>                        // 7:  Resolution Override
                {
                    if (!TryGetAdditionalReflectionData(prop, out var reflectionData))
                    {
                        EditorGUI.LabelField(r, "--");
                        return;
                    }

                    if(!TryGetCubemapResolution(reflectionData, out int resolution))
                    {
                        EditorGUI.LabelField(r, "--");
                        return;
                    }

                    if(reflectionData.FindProperty("m_ProbeSettings.cubeResolution.m_UseOverride").boolValue)
                    {
                        reflectionData.Update();

                        EditorGUI.BeginChangeCheck();
                        var overrideResolution = (CubeReflectionResolution)EditorGUI.EnumPopup(r, (CubeReflectionResolution)resolution);
                        if(EditorGUI.EndChangeCheck())
                        {
                            reflectionData.FindProperty("m_ProbeSettings.cubeResolution.m_Override").intValue = (int)overrideResolution;
                        }

                        reflectionData.ApplyModifiedProperties();
                    }
                    else
                    {
                        using (new EditorGUI.DisabledScope(true))
                        {
                            EditorGUI.EnumFlagsField(r, (CubeReflectionResolution)resolution);
                        }
                    }
                }, (lprop, rprop) =>
                    {
                        TryGetAdditionalReflectionData(lprop, out var lReflectionData);
                        TryGetAdditionalReflectionData(rprop, out var rReflectionData);

                        if (IsNullComparison(lReflectionData, rReflectionData, out var order))
                            return order;

                        if(!TryGetCubemapResolution(lReflectionData, out int lResolution) || !TryGetCubemapResolution(rReflectionData, out int rResolution))
                            return order;

                        return lResolution.CompareTo(rResolution);
                    }, (target, source) =>
                    {
                        if (!TryGetAdditionalReflectionData(target, out var tReflectionData) || !TryGetAdditionalReflectionData(source, out var sReflectionData))
                            return;

                        if(tReflectionData.FindProperty("m_ProbeSettings.cubeResolution.m_UseOverride").boolValue)
                        {
                            tReflectionData.Update();
                            tReflectionData.FindProperty("m_ProbeSettings.cubeResolution.m_Override").intValue = sReflectionData.FindProperty("m_ProbeSettings.cubeResolution.m_Override").intValue;
                            tReflectionData.ApplyModifiedProperties();
                        }
                    }),
                new LightingExplorerTableColumn(LightingExplorerTableColumn.DataType.Checkbox, HDStyles.ParallaxCorrection, "m_BoxProjection", 215, (r, prop, dep) =>   // 8. Use Influence volume as proxy
                {
                    if (!TryGetAdditionalReflectionData(prop, out var reflectionData))
                    {
                        EditorGUI.LabelField(r, "--");
                        return;
                    }

                    reflectionData.Update();
                    EditorGUI.PropertyField(r, reflectionData.FindProperty("m_ProbeSettings.proxySettings.useInfluenceVolumeAsProxyVolume"), GUIContent.none);
                    reflectionData.ApplyModifiedProperties();
                }, (lprop, rprop) =>
                    {
                        TryGetAdditionalReflectionData(lprop, out var lReflectionData);
                        TryGetAdditionalReflectionData(rprop, out var rReflectionData);

                        if (IsNullComparison(lReflectionData, rReflectionData, out var order))
                            return order;

                        return lReflectionData.FindProperty("m_ProbeSettings.proxySettings.useInfluenceVolumeAsProxyVolume").boolValue.CompareTo(rReflectionData.FindProperty("m_ProbeSettings.proxySettings.useInfluenceVolumeAsProxyVolume").boolValue);
                    }, (target, source) =>
                    {
                        if (!TryGetAdditionalReflectionData(target, out var tReflectionData) || !TryGetAdditionalReflectionData(source, out var sReflectionData))
                            return;

                        tReflectionData.Update();
                        tReflectionData.FindProperty("m_ProbeSettings.proxySettings.useInfluenceVolumeAsProxyVolume").boolValue = sReflectionData.FindProperty("m_ProbeSettings.proxySettings.useInfluenceVolumeAsProxyVolume").boolValue;
                        tReflectionData.ApplyModifiedProperties();
                    }),
                new LightingExplorerTableColumn(LightingExplorerTableColumn.DataType.Float, HDStyles.Weight, "m_Mode", 60, (r, prop, dep) =>                            // 9: Weight
                {
                    if (!TryGetAdditionalReflectionData(prop, out var reflectionData))
                    {
                        EditorGUI.LabelField(r, "--");
                        return;
                    }

                    reflectionData.Update();
                    EditorGUI.PropertyField(r, reflectionData.FindProperty("m_ProbeSettings.lighting.weight"), GUIContent.none);
                    reflectionData.ApplyModifiedProperties();
                }, (lprop, rprop) =>
                    {
                        TryGetAdditionalReflectionData(lprop, out var lReflectionData);
                        TryGetAdditionalReflectionData(rprop, out var rReflectionData);

                        if (IsNullComparison(lReflectionData, rReflectionData, out var order))
                            return order;

                        return lReflectionData.FindProperty("m_ProbeSettings.lighting.weight").floatValue.CompareTo(rReflectionData.FindProperty("m_ProbeSettings.lighting.weight").floatValue);
                    }, (target, source) =>
                    {
                        if (!TryGetAdditionalReflectionData(target, out var tReflectionData) || !TryGetAdditionalReflectionData(source, out var sReflectionData))
                            return;

                        tReflectionData.Update();
                        tReflectionData.FindProperty("m_ProbeSettings.lighting.weight").floatValue = sReflectionData.FindProperty("m_ProbeSettings.lighting.weight").floatValue;
                        tReflectionData.ApplyModifiedProperties();
                    }),
            };
        }

        protected virtual LightingExplorerTableColumn[] GetPlanarReflectionColumns()
        {
            return new[]
            {
                new LightingExplorerTableColumn(LightingExplorerTableColumn.DataType.Checkbox, HDStyles.Enabled, "m_Enabled", 60),                      // 0: Enabled
                new LightingExplorerTableColumn(LightingExplorerTableColumn.DataType.Name, HDStyles.Name, null, 200),                                   // 1: Name
                new LightingExplorerTableColumn(LightingExplorerTableColumn.DataType.Float, HDStyles.Weight, "m_ProbeSettings.lighting.weight", 60),    // 2: Weight
            };
        }

        protected virtual LightingExplorerTableColumn[] GetHDLightProbeColumns()
        {
            if (!HDRenderPipeline.currentAsset.currentPlatformRenderPipelineSettings.supportProbeVolume)
                return base.GetLightProbeColumns();

            return new[]
            {
                new LightingExplorerTableColumn(LightingExplorerTableColumn.DataType.Checkbox, HDStyles.Enabled, "m_Enabled", 60),
                new LightingExplorerTableColumn(LightingExplorerTableColumn.DataType.Name, HDStyles.Name, null, 200),
                new LightingExplorerTableColumn(LightingExplorerTableColumn.DataType.Enum, HDStyles.VolumeMode, "mode", 75,
                    (r, prop, dep) => {
                        if (prop == null) return;
                        ProbeVolume pv = prop.serializedObject.targetObject as ProbeVolume;

                        EditorGUI.BeginChangeCheck();
                        int newMode = EditorGUI.Popup(r, (int)pv.mode, System.Enum.GetNames(typeof(ProbeVolume.Mode)));
                        if (EditorGUI.EndChangeCheck())
                            prop.intValue = newMode;
                    },
                    (lprop, rprop) => {
                        ProbeVolume pv1 = lprop.serializedObject.targetObject as ProbeVolume;
                        ProbeVolume pv2 = rprop.serializedObject.targetObject as ProbeVolume;

                        return pv1.mode.CompareTo(pv2.mode);
                    }
                ),
                new LightingExplorerTableColumn(LightingExplorerTableColumn.DataType.Checkbox, HDStyles.OverrideProbeSpacing, "overridesSubdivLevels", 140),
                new LightingExplorerTableColumn(LightingExplorerTableColumn.DataType.Checkbox, HDStyles.OverrideRendererFilters, "overrideRendererFilters", 140),
                new LightingExplorerTableColumn(LightingExplorerTableColumn.DataType.Checkbox, HDStyles.FillEmptySpaces, "fillEmptySpaces", 140),
            };
        }

        public override void OnDisable()
        {
            lightDataPairing.Clear();
            volumeDataPairing.Clear();
            serializedReflectionProbeDataPairing.Clear();
        }

        private bool TryGetAdditionalLightData(SerializedProperty prop, out HDAdditionalLightData lightData)
        {
            return TryGetAdditionalLightData(prop, out lightData, out var light);
        }

        private bool TryGetAdditionalLightData(SerializedProperty prop, out HDAdditionalLightData lightData, out Light light)
        {
            light = prop.serializedObject.targetObject as Light;

            if (light == null || !lightDataPairing.ContainsKey(light))
                lightData = null;
            else
                lightData = lightDataPairing[light].hdAdditionalLightData;

            return lightData != null;
        }

        private bool TryGetLightPrefabData(SerializedProperty prop, out bool isPrefab, out Object prefabRoot)
        {
            Light light = prop.serializedObject.targetObject as Light;

            if (light == null || !lightDataPairing.ContainsKey(light))
            {
                isPrefab = false;
                prefabRoot = null;
                return false;
            }

            isPrefab = lightDataPairing[light].isPrefab;
            prefabRoot = lightDataPairing[light].prefabRoot;

            return prefabRoot != null;
        }

        private bool TryGetAdditionalVolumeData(SerializedProperty prop, out VolumeData volumeData)
        {
            Volume volume = prop.serializedObject.targetObject as Volume;

            if (volume == null || !volumeDataPairing.ContainsKey(volume))
            {
                volumeData = new VolumeData();
                return false;
            }

            volumeData = volumeDataPairing[volume];
            return true;
        }

        private bool TryGetAdditionalReflectionData(SerializedProperty prop, out SerializedObject reflectionData)
        {
            ReflectionProbe probe = prop.serializedObject.targetObject as ReflectionProbe;

            if (probe == null || !serializedReflectionProbeDataPairing.ContainsKey(probe))
                reflectionData = null;
            else
                reflectionData = serializedReflectionProbeDataPairing[probe];

            return reflectionData != null;
        }

        private bool TryGetCubemapResolution(SerializedObject reflectionData, out int resolution)
        {
            if (reflectionData.FindProperty("m_ProbeSettings.cubeResolution.m_UseOverride").boolValue)
            {
                resolution = reflectionData.FindProperty("m_ProbeSettings.cubeResolution.m_Override").intValue;
                return true;
            }

            int level = reflectionData.FindProperty("m_ProbeSettings.cubeResolution.m_Level").intValue;
            if (HDRenderPipeline.currentAsset.currentPlatformRenderPipelineSettings.cubeReflectionResolution.TryGet(level, out var cubeResolution))
            {
                resolution = (int)cubeResolution;
                return true;
            }

            resolution = -1;
            return false;
        }

        private bool IsNullComparison<T>(T l, T r, out int order)
        {
            if (l == null)
            {
                order = r == null ? 0 : -1;
                return true;
            }
            else if (r == null)
            {
                order = 1;
                return true;
            }

            order = 0;
            return false;
        }
    }
}
