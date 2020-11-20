using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace UnityEditor.Rendering.HighDefinition
{
    [LightingExplorerExtensionAttribute(typeof(HDRenderPipelineAsset))]
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
            public static readonly GUIContent ParallaxCorrection = EditorGUIUtility.TrTextContent("Influence Volume as Proxy Volume");
            public static readonly GUIContent Weight = EditorGUIUtility.TrTextContent("Weight");

            public static readonly GUIContent[] LightTypeTitles = { EditorGUIUtility.TrTextContent("Spot"), EditorGUIUtility.TrTextContent("Directional"), EditorGUIUtility.TrTextContent("Point"), EditorGUIUtility.TrTextContent("Area") };
            public static readonly int[] LightTypeValues = { (int)HDLightType.Spot, (int)HDLightType.Directional, (int)HDLightType.Point, (int)HDLightType.Area };
            internal static readonly GUIContent DrawProbes = EditorGUIUtility.TrTextContent("Draw");
            internal static readonly GUIContent DebugColor = EditorGUIUtility.TrTextContent("Debug Color");
            internal static readonly GUIContent ResolutionX = EditorGUIUtility.TrTextContent("Resolution X");
            internal static readonly GUIContent ResolutionY = EditorGUIUtility.TrTextContent("Resolution Y");
            internal static readonly GUIContent ResolutionZ = EditorGUIUtility.TrTextContent("Resolution Z");
            internal static readonly GUIContent FadeStart = EditorGUIUtility.TrTextContent("Fade Start");
            internal static readonly GUIContent FadeEnd = EditorGUIUtility.TrTextContent("Fade End");

            public static readonly GUIContent[] globalModes = { new GUIContent("Global"), new GUIContent("Local") };
        }

        public override LightingExplorerTab[] GetContentTabs()
        {
            return new[]
            {
                new LightingExplorerTab("Lights", GetHDLights, GetHDLightColumns),
                new LightingExplorerTab("Volumes", GetVolumes, GetVolumeColumns),
                new LightingExplorerTab("Reflection Probes", GetHDReflectionProbes, GetHDReflectionProbeColumns),
                new LightingExplorerTab("Planar Reflection Probes", GetPlanarReflections, GetPlanarReflectionColumns),
                new LightingExplorerTab("Light Probes", GetLightProbes, GetLightProbeColumns),
                new LightingExplorerTab("Probe Volumes", GetProbeVolumes, GetProbeVolumeColumns),
                new LightingExplorerTab("Emissive Materials", GetEmissives, GetEmissivesColumns)
            };
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

        protected internal virtual UnityEngine.Object[] GetProbeVolumes()
        {
            return Resources.FindObjectsOfTypeAll<ProbeVolume>();
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

                    HDLightType lightType = lightData.type;

                    EditorGUI.BeginProperty(r, GUIContent.none, prop);
                    EditorGUI.BeginChangeCheck();
                    lightType = (HDLightType)EditorGUI.IntPopup(r, (int)lightType, HDStyles.LightTypeTitles, HDStyles.LightTypeValues);

                    if (EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObjects(new Object[] { prop.serializedObject.targetObject, lightData }, "Changed light type");
                        lightData.type = lightType;
                    }
                    EditorGUI.EndProperty();
                }, (lprop, rprop) =>
                    {
                        TryGetAdditionalLightData(lprop, out var lLightData);
                        TryGetAdditionalLightData(rprop, out var rLightData);

                        if (IsNullComparison(lLightData, rLightData, out var order))
                            return order;

                        return ((int)lLightData.type).CompareTo((int)rLightData.type);
                    }, (target, source) =>
                    {
                        if (!TryGetAdditionalLightData(target, out var tLightData) || !TryGetAdditionalLightData(source, out var sLightData))
                            return;

                        Undo.RecordObjects(new Object[] { target.serializedObject.targetObject, tLightData }, "Changed light type");
                        tLightData.type = sLightData.type;
                    }),
                new LightingExplorerTableColumn(LightingExplorerTableColumn.DataType.Enum, HDStyles.Mode, "m_Lightmapping", 90),                                    // 3: Mixed mode
                new LightingExplorerTableColumn(LightingExplorerTableColumn.DataType.Float, HDStyles.Range, "m_Range", 60),                                         // 4: Range
                new LightingExplorerTableColumn(LightingExplorerTableColumn.DataType.Color, HDStyles.Color, "m_Color", 60),                                         // 5: Color
                new LightingExplorerTableColumn(LightingExplorerTableColumn.DataType.Checkbox, HDStyles.ColorTemperatureMode, "m_UseColorTemperature", 150),        // 6: Color Temperature Mode
                new LightingExplorerTableColumn(LightingExplorerTableColumn.DataType.Float, HDStyles.ColorTemperature, "m_ColorTemperature", 120, (r, prop, dep) => // 7: Color Temperature
                {
                    using (new EditorGUI.DisabledScope(!prop.serializedObject.FindProperty("m_UseColorTemperature").boolValue))
                    {
                        EditorGUI.PropertyField(r, prop, GUIContent.none);
                    }
                }, (lprop, rprop) =>
                    {
                        float lTemp = lprop.serializedObject.FindProperty("m_UseColorTemperature").boolValue ? lprop.floatValue : 0.0f;
                        float rTemp = rprop.serializedObject.FindProperty("m_UseColorTemperature").boolValue ? rprop.floatValue : 0.0f;

                        return lTemp.CompareTo(rTemp);
                    }),
                new LightingExplorerTableColumn(LightingExplorerTableColumn.DataType.Float, HDStyles.Intensity, "m_Intensity", 60, (r, prop, dep) =>                // 8: Intensity
                {
                    if (!TryGetAdditionalLightData(prop, out var lightData))
                    {
                        EditorGUI.LabelField(r, "--");
                        return;
                    }

                    float intensity = lightData.intensity;

                    EditorGUI.BeginProperty(r, GUIContent.none, prop);
                    EditorGUI.BeginChangeCheck();
                    intensity = EditorGUI.FloatField(r, intensity);
                    if (EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObjects(new Object[] { prop.serializedObject.targetObject, lightData }, "Changed light intensity");
                        lightData.intensity = intensity;
                    }
                    EditorGUI.EndProperty();
                }, (lprop, rprop) =>
                    {
                        TryGetAdditionalLightData(lprop, out var lLightData);
                        TryGetAdditionalLightData(rprop, out var rLightData);

                        if (IsNullComparison(lLightData, rLightData, out var order))
                            return order;

                        return ((float)lLightData.intensity).CompareTo((float)rLightData.intensity);
                    }, (target, source) =>
                    {
                        if (!TryGetAdditionalLightData(target, out var tLightData) || !TryGetAdditionalLightData(source, out var sLightData))
                            return;

                        Undo.RecordObjects(new Object[] { target.serializedObject.targetObject, tLightData }, "Changed light intensity");
                        tLightData.intensity = sLightData.intensity;
                    }),
                new LightingExplorerTableColumn(LightingExplorerTableColumn.DataType.Enum, HDStyles.Unit, "m_Intensity", 70, (r, prop, dep) =>                      // 9: Unit
                {
                    if (!TryGetAdditionalLightData(prop, out var lightData))
                    {
                        EditorGUI.LabelField(r, "--");
                        return;
                    }

                    LightUnit unit = lightData.lightUnit;

                    EditorGUI.BeginChangeCheck();
                    unit = (LightUnit)EditorGUI.EnumPopup(r, unit);
                    if (EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObject(lightData, "Changed light unit");
                        lightData.lightUnit = unit;
                    }
                }, (lprop, rprop) =>
                    {
                        TryGetAdditionalLightData(lprop, out var lLightData);
                        TryGetAdditionalLightData(rprop, out var rLightData);

                        if (IsNullComparison(lLightData, rLightData, out var order))
                            return order;

                        return ((int)lLightData.lightUnit).CompareTo((int)rLightData.lightUnit);
                    }, (target, source) =>
                    {
                        if (!TryGetAdditionalLightData(target, out var tLightData) || !TryGetAdditionalLightData(source, out var sLightData))
                            return;

                        Undo.RecordObject(tLightData, "Changed light unit");
                        tLightData.lightUnit = sLightData.lightUnit;
                    }),
                new LightingExplorerTableColumn(LightingExplorerTableColumn.DataType.Float, HDStyles.IndirectMultiplier, "m_BounceIntensity", 115),                 // 10: Indirect multiplier
                new LightingExplorerTableColumn(LightingExplorerTableColumn.DataType.Checkbox, HDStyles.Shadows, "m_Shadows.m_Type", 60, (r, prop, dep) =>          // 11: Shadows
                {
                    EditorGUI.BeginChangeCheck();
                    bool shadows = EditorGUI.Toggle(r, prop.intValue != (int)LightShadows.None);
                    if (EditorGUI.EndChangeCheck())
                    {
                        prop.intValue = shadows ? (int)LightShadows.Soft : (int)LightShadows.None;
                    }
                }),
                new LightingExplorerTableColumn(LightingExplorerTableColumn.DataType.Enum, HDStyles.ContactShadowsLevel, "m_Shadows.m_Type", 115, (r, prop, dep) =>      // 12: Contact Shadows level
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
                new LightingExplorerTableColumn(LightingExplorerTableColumn.DataType.Checkbox, HDStyles.ContactShadowsValue, "m_Shadows.m_Type", 115, (r, prop, dep) =>  // 13: Contact Shadows override
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
                new LightingExplorerTableColumn(LightingExplorerTableColumn.DataType.Enum, HDStyles.ShadowResolutionLevel, "m_Intensity", 130, (r, prop, dep) =>         // 14: Shadow Resolution level
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
                        Undo.RecordObject(lightData, "Changed contact shadow resolution");
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

                        Undo.RecordObject(tLightData, "Changed contact shadow resolution");
                        tLightData.shadowResolution.level = sLightData.shadowResolution.level;
                        tLightData.shadowResolution.useOverride = sLightData.shadowResolution.useOverride;
                    }),
                new LightingExplorerTableColumn(LightingExplorerTableColumn.DataType.Int, HDStyles.ShadowResolutionValue, "m_Intensity", 130, (r, prop, dep) =>          // 15: Shadow resolution override
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
                            Undo.RecordObject(lightData, "Changed shadow resolution override");
                            shadowResolution.@override = overrideShadowResolution;
                        }
                    }
                    else
                    {
                        var lightType = lightData.type;
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
                        var lLightShape = lLightData.type;
                        var rLightShape = rLightData.type;

                        int lResolution = lShadowResolution.useOverride ? lShadowResolution.@override : (hdrp == null ? -1 : HDLightUI.ScalableSettings.ShadowResolution(lLightShape, hdrp)[lShadowResolution.level]);
                        int rResolution = rShadowResolution.useOverride ? rShadowResolution.@override : (hdrp == null ? -1 : HDLightUI.ScalableSettings.ShadowResolution(rLightShape, hdrp)[rShadowResolution.level]);

                        return lResolution.CompareTo(rResolution);
                    }, (target, source) =>
                    {
                        if (!TryGetAdditionalLightData(target, out var tLightData) || !TryGetAdditionalLightData(source, out var sLightData))
                            return;

                        var hdrp = GraphicsSettings.currentRenderPipeline as HDRenderPipelineAsset;
                        var tShadowResolution = tLightData.shadowResolution;
                        var sShadowResolution = sLightData.shadowResolution;

                        if (tShadowResolution.useOverride)
                        {
                            Undo.RecordObject(tLightData, "Changed shadow resolution override");
                            tShadowResolution.@override = sShadowResolution.@override;
                        }
                    }),
                new LightingExplorerTableColumn(LightingExplorerTableColumn.DataType.Checkbox, HDStyles.AffectDiffuse, "m_Intensity", 95, (r, prop, dep) =>         // 16: Affect Diffuse
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
                new LightingExplorerTableColumn(LightingExplorerTableColumn.DataType.Checkbox, HDStyles.AffectSpecular, "m_Intensity", 100, (r, prop, dep) =>       // 17: Affect Specular
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
                new LightingExplorerTableColumn(LightingExplorerTableColumn.DataType.Float, HDStyles.FadeDistance, "m_Intensity", 95, (r, prop, dep) =>             // 18: Fade Distance
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
                new LightingExplorerTableColumn(LightingExplorerTableColumn.DataType.Float, HDStyles.ShadowFadeDistance, "m_Intensity", 145, (r, prop, dep) =>      // 19: Shadow Fade Distance
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
                new LightingExplorerTableColumn(LightingExplorerTableColumn.DataType.Custom, HDStyles.LightLayer, "m_RenderingLayerMask", 145, (r, prop, dep) =>    // 20: Light Layer
                {
                    using (new EditorGUI.DisabledScope(!HDRenderPipeline.currentAsset.currentPlatformRenderPipelineSettings.supportLightLayers))
                    {
                        if (!TryGetAdditionalLightData(prop, out var lightData))
                        {
                            EditorGUI.LabelField(r, "--");
                            return;
                        }

                        int lightlayersMask = (int)lightData.lightlayersMask;

                        EditorGUI.BeginChangeCheck();
                        lightlayersMask = HDEditorUtils.DrawLightLayerMask(r, lightlayersMask);
                        if (EditorGUI.EndChangeCheck())
                        {
                            Undo.RecordObject(lightData, "Changed light layer");
                            lightData.lightlayersMask = (LightLayerEnum)lightlayersMask;
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
                new LightingExplorerTableColumn(LightingExplorerTableColumn.DataType.Custom, HDStyles.IsPrefab, "m_Intensity", 120, (r, prop, dep) =>               // 21: Prefab
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
                new LightingExplorerTableColumn(LightingExplorerTableColumn.DataType.Enum, HDStyles.VolumeMode, "isGlobal", 75, (r, prop, dep) =>                     // 2: Is Global
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
                new LightingExplorerTableColumn(LightingExplorerTableColumn.DataType.Checkbox, HDStyles.ParallaxCorrection, "m_BoxProjection", 215, (r, prop, dep) =>   // 6. Use Influence volume as proxy
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
                new LightingExplorerTableColumn(LightingExplorerTableColumn.DataType.Float, HDStyles.Weight, "m_Mode", 60, (r, prop, dep) =>                            // 7: Weight
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

        protected internal virtual LightingExplorerTableColumn[] GetProbeVolumeColumns()
        {
            return new[]
            {
                new LightingExplorerTableColumn(LightingExplorerTableColumn.DataType.Name, HDStyles.Name, null, 200),                                       // 0: Name
                new LightingExplorerTableColumn(LightingExplorerTableColumn.DataType.Checkbox, HDStyles.DrawProbes, "parameters", 35, (r, prop, dep) =>       // 1: Draw Probes
                {
                    SerializedProperty drawProbes = prop.FindPropertyRelative("drawProbes");
                    EditorGUI.PropertyField(r, drawProbes, GUIContent.none);
                }, (lhs, rhs) =>
                    {
                        return lhs.FindPropertyRelative("drawProbes").boolValue.CompareTo(rhs.FindPropertyRelative("drawProbes").boolValue);
                    }, (target, source) =>
                    {
                        target.FindPropertyRelative("drawProbes").boolValue = source.FindPropertyRelative("drawProbes").boolValue;
                    }),
                new LightingExplorerTableColumn(LightingExplorerTableColumn.DataType.Color, HDStyles.DebugColor, "parameters", 75, (r, prop, dep) =>       // 2: Debug Color
                {
                    SerializedProperty debugColor = prop.FindPropertyRelative("debugColor");
                    EditorGUI.PropertyField(r, debugColor, GUIContent.none);
                }, (lhs, rhs) =>
                    {
                        float lh, ls, lv, rh, rs, rv;
                        Color.RGBToHSV(lhs.FindPropertyRelative("debugColor").colorValue, out lh, out ls, out lv);
                        Color.RGBToHSV(rhs.FindPropertyRelative("debugColor").colorValue, out rh, out rs, out rv);
                        return lh.CompareTo(rh);
                    }, (target, source) =>
                    {
                        target.FindPropertyRelative("debugColor").colorValue = source.FindPropertyRelative("debugColor").colorValue;
                    }),
                new LightingExplorerTableColumn(LightingExplorerTableColumn.DataType.Int, HDStyles.ResolutionX, "parameters", 75, (r, prop, dep) =>      // 3: Resolution X
                {
                    SerializedProperty resolutionX = prop.FindPropertyRelative("resolutionX");

                    EditorGUI.BeginChangeCheck();
                    EditorGUI.PropertyField(r, resolutionX, GUIContent.none);

                    if (EditorGUI.EndChangeCheck())
                    {
                        resolutionX.intValue = Mathf.Max(1, resolutionX.intValue);
                    }
                }, (lhs, rhs) =>
                    {
                        return lhs.FindPropertyRelative("resolutionX").intValue.CompareTo(rhs.FindPropertyRelative("resolutionX").intValue);
                    }, (target, source) =>
                    {
                        target.FindPropertyRelative("resolutionX").intValue = source.FindPropertyRelative("resolutionX").intValue;
                    }),
                new LightingExplorerTableColumn(LightingExplorerTableColumn.DataType.Int, HDStyles.ResolutionY, "parameters", 75, (r, prop, dep) =>      // 4: Resolution Y
                {
                    SerializedProperty resolutionY = prop.FindPropertyRelative("resolutionY");

                    EditorGUI.BeginChangeCheck();
                    EditorGUI.PropertyField(r, resolutionY, GUIContent.none);

                    if (EditorGUI.EndChangeCheck())
                    {
                        SerializedProperty resolutionX = prop.FindPropertyRelative("resolutionX");
                        resolutionY.intValue = Mathf.Max(1, resolutionY.intValue);
                    }
                }, (lhs, rhs) =>
                    {
                        return lhs.FindPropertyRelative("resolutionY").intValue.CompareTo(rhs.FindPropertyRelative("resolutionY").intValue);
                    }, (target, source) =>
                    {
                        target.FindPropertyRelative("resolutionY").intValue = source.FindPropertyRelative("resolutionY").intValue;
                    }),
                new LightingExplorerTableColumn(LightingExplorerTableColumn.DataType.Int, HDStyles.ResolutionZ, "parameters", 75, (r, prop, dep) =>      // 5: Resolution Z
                {
                    SerializedProperty resolutionZ = prop.FindPropertyRelative("resolutionZ");

                    EditorGUI.BeginChangeCheck();
                    EditorGUI.PropertyField(r, resolutionZ, GUIContent.none);

                    if (EditorGUI.EndChangeCheck())
                    {
                        SerializedProperty resolutionX = prop.FindPropertyRelative("resolutionX");
                        resolutionZ.intValue = Mathf.Max(1, resolutionZ.intValue);
                    }
                }, (lhs, rhs) =>
                    {
                        return lhs.FindPropertyRelative("resolutionZ").intValue.CompareTo(rhs.FindPropertyRelative("resolutionZ").intValue);
                    }, (target, source) =>
                    {
                        target.FindPropertyRelative("resolutionZ").intValue = source.FindPropertyRelative("resolutionZ").intValue;
                    }),
                new LightingExplorerTableColumn(LightingExplorerTableColumn.DataType.Float, HDStyles.FadeStart, "parameters", 65, (r, prop, dep) =>        // 6: Distance Fade Start
                {
                    SerializedProperty distanceFadeStart = prop.FindPropertyRelative("distanceFadeStart");
                    EditorGUI.PropertyField(r, distanceFadeStart, GUIContent.none);
                }, (lhs, rhs) =>
                    {
                        return lhs.FindPropertyRelative("distanceFadeStart").floatValue.CompareTo(rhs.FindPropertyRelative("distanceFadeStart").floatValue);
                    }, (target, source) =>
                    {
                        target.FindPropertyRelative("distanceFadeStart").floatValue = source.FindPropertyRelative("distanceFadeStart").floatValue;
                    }),
                new LightingExplorerTableColumn(LightingExplorerTableColumn.DataType.Float, HDStyles.FadeEnd, "parameters", 65, (r, prop, dep) =>          // 7: Distance Fade End
                {
                    SerializedProperty distanceFadeEnd = prop.FindPropertyRelative("distanceFadeEnd");
                    EditorGUI.PropertyField(r, distanceFadeEnd, GUIContent.none);
                }, (lhs, rhs) =>
                    {
                        return lhs.FindPropertyRelative("distanceFadeEnd").floatValue.CompareTo(rhs.FindPropertyRelative("distanceFadeEnd").floatValue);
                    }, (target, source) =>
                    {
                        target.FindPropertyRelative("distanceFadeEnd").floatValue = source.FindPropertyRelative("distanceFadeEnd").floatValue;
                    })
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
