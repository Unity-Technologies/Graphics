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
            public FogType fogType;
            public SkyType skyType;

            public VolumeData(bool isGlobal, VolumeProfile profile)
            {
                this.isGlobal = isGlobal;
                this.profile = profile;
                VisualEnvironment visualEnvironment = null;
                this.hasVisualEnvironment = profile != null ? profile.TryGet<VisualEnvironment>(typeof(VisualEnvironment), out visualEnvironment) : false;
                if (this.hasVisualEnvironment)
                {
                    this.skyType = (SkyType)visualEnvironment.skyType.value;
                    this.fogType = visualEnvironment.fogType.value;
                }
                else
                {
                    this.skyType = (SkyType)1;
                    this.fogType = (FogType)0;
                }
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
            public static readonly GUIContent ContactShadows = EditorGUIUtility.TrTextContent("Contact Shadows");
            public static readonly GUIContent ShadowResolution = EditorGUIUtility.TrTextContent("Shadows Resolution");
            public static readonly GUIContent ShapeWidth = EditorGUIUtility.TrTextContent("Shape Width");
            public static readonly GUIContent VolumeProfile = EditorGUIUtility.TrTextContent("Volume Profile");
            public static readonly GUIContent ColorTemperatureMode = EditorGUIUtility.TrTextContent("Use Color Temperature");
            public static readonly GUIContent AffectDiffuse = EditorGUIUtility.TrTextContent("Affect Diffuse");
            public static readonly GUIContent AffectSpecular = EditorGUIUtility.TrTextContent("Affect Specular");
            public static readonly GUIContent FadeDistance = EditorGUIUtility.TrTextContent("Fade Distance");
            public static readonly GUIContent ShadowFadeDistance = EditorGUIUtility.TrTextContent("Shadow Fade Distance");
            public static readonly GUIContent LightLayer = EditorGUIUtility.TrTextContent("Light Layer");
            public static readonly GUIContent IsPrefab = EditorGUIUtility.TrTextContent("Prefab");

            public static readonly GUIContent GlobalVolume = EditorGUIUtility.TrTextContent("Global");
            public static readonly GUIContent Priority = EditorGUIUtility.TrTextContent("Priority");
            public static readonly GUIContent HasVisualEnvironment = EditorGUIUtility.TrTextContent("Has Visual Environment");
            public static readonly GUIContent FogType = EditorGUIUtility.TrTextContent("Fog Type");
            public static readonly GUIContent SkyType = EditorGUIUtility.TrTextContent("Sky Type");

            public static readonly GUIContent ShadowDistance = EditorGUIUtility.TrTextContent("Shadow Distance");
            public static readonly GUIContent NearClip = EditorGUIUtility.TrTextContent("Near Clip");
            public static readonly GUIContent FarClip = EditorGUIUtility.TrTextContent("Far Clip");
            public static readonly GUIContent ParallaxCorrection = EditorGUIUtility.TrTextContent("Influence Volume as Proxy Volume");
            public static readonly GUIContent Weight = EditorGUIUtility.TrTextContent("Weight");

            public static readonly GUIContent[] LightmapBakeTypeTitles = { EditorGUIUtility.TrTextContent("Realtime"), EditorGUIUtility.TrTextContent("Mixed"), EditorGUIUtility.TrTextContent("Baked") };
            public static readonly int[] LightmapBakeTypeValues = { (int)LightmapBakeType.Realtime, (int)LightmapBakeType.Mixed, (int)LightmapBakeType.Baked };
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

        protected virtual LightingExplorerTableColumn[] GetHDLightColumns()
        {
            return new[]
            {
                new LightingExplorerTableColumn(LightingExplorerTableColumn.DataType.Checkbox, HDStyles.Enabled, "m_Enabled", 60),                                  // 0: Enabled
                new LightingExplorerTableColumn(LightingExplorerTableColumn.DataType.Name, HDStyles.Name, null, 200),                                               // 1: Name
                new LightingExplorerTableColumn(LightingExplorerTableColumn.DataType.Enum, HDStyles.Type, "m_Type", 100),                                           // 2: Type
                new LightingExplorerTableColumn(LightingExplorerTableColumn.DataType.Enum, HDStyles.Mode, "m_Lightmapping", 90),                                    // 3: Mixed mode
                new LightingExplorerTableColumn(LightingExplorerTableColumn.DataType.Float, HDStyles.Range, "m_Range", 60),                                         // 4: Range
                new LightingExplorerTableColumn(LightingExplorerTableColumn.DataType.Color, HDStyles.Color, "m_Color", 60),                                         // 5: Color
                new LightingExplorerTableColumn(LightingExplorerTableColumn.DataType.Checkbox, HDStyles.ColorTemperatureMode, "m_UseColorTemperature", 150),        // 6: Color Temperature Mode
                new LightingExplorerTableColumn(LightingExplorerTableColumn.DataType.Float, HDStyles.ColorTemperature, "m_ColorTemperature", 120, (r, prop, dep) => // 7: Color Temperature
                {
                    if (prop.serializedObject.FindProperty("m_UseColorTemperature").boolValue)
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
                    if(!TryGetAdditionalLightData(prop, out var lightData))
                    {
                        EditorGUI.LabelField(r, "--");
                        return;
                    }

                    float intensity = lightData.intensity;

                    EditorGUI.BeginChangeCheck();
                    intensity = EditorGUI.FloatField(r, intensity);
                    if (EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObject(lightData, "Changed light intensity");
                        lightData.intensity = intensity;
                    }
                }),
                new LightingExplorerTableColumn(LightingExplorerTableColumn.DataType.Enum, HDStyles.Unit, "m_Intensity", 70, (r, prop, dep) =>                      // 9: Unit
                {
                    if(!TryGetAdditionalLightData(prop, out var lightData))
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
                new LightingExplorerTableColumn(LightingExplorerTableColumn.DataType.Enum, HDStyles.ContactShadows, "m_Shadows.m_Type", 115, (r, prop, dep) =>      // 12: Contact Shadows level
                {
                    if(!TryGetAdditionalLightData(prop, out var lightData))
                    {
                        EditorGUI.LabelField(r, "--");
                        return;
                    }

                    var useContactShadow = lightData.useContactShadow;
                    EditorGUI.BeginChangeCheck();
                    var (level, useOverride) = SerializedScalableSettingValueUI.LevelFieldGUI(r, GUIContent.none, ScalableSettingSchema.GetSchemaOrNull(ScalableSettingSchemaId.With3Levels), useContactShadow.level, useContactShadow.useOverride);
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
                }),
                new LightingExplorerTableColumn(LightingExplorerTableColumn.DataType.Checkbox, HDStyles.ContactShadows, "m_Shadows.m_Type", 115, (r, prop, dep) =>  // 13: Contact Shadows override
                {
                    if(!TryGetAdditionalLightData(prop, out var lightData))
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
                }),
                new LightingExplorerTableColumn(LightingExplorerTableColumn.DataType.Enum, HDStyles.ShadowResolution, "m_Intensity", 130, (r, prop, dep) =>         // 14: Shadow Resolution level
                {
                    if(!TryGetAdditionalLightData(prop, out var lightData))
                    {
                        EditorGUI.LabelField(r, "--");
                        return;
                    }

                    var shadowResolution = lightData.shadowResolution;

                    EditorGUI.BeginChangeCheck();
                    var (level, useOverride) = SerializedScalableSettingValueUI.LevelFieldGUI(r, GUIContent.none, ScalableSettingSchema.GetSchemaOrNull(ScalableSettingSchemaId.With3Levels), shadowResolution.level, shadowResolution.useOverride);
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
                }),
                new LightingExplorerTableColumn(LightingExplorerTableColumn.DataType.Int, HDStyles.ShadowResolution, "m_Intensity", 130, (r, prop, dep) =>          // 15: Shadow resolution override
                {
                    var hdrp = HDRenderPipeline.currentAsset;

                    if(!TryGetAdditionalLightData(prop, out var lightData, out var light) || hdrp == null)
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
                        var lightShape = lightDataPairing[light].hdAdditionalLightData.type;
                        var defaultValue = HDLightUI.ScalableSettings.ShadowResolution(lightShape, hdrp);

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
                }),
                new LightingExplorerTableColumn(LightingExplorerTableColumn.DataType.Checkbox, HDStyles.AffectDiffuse, "m_Intensity", 95, (r, prop, dep) =>         // 16: Affect Diffuse
                {
                    if(!TryGetAdditionalLightData(prop, out var lightData))
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
                }),
                new LightingExplorerTableColumn(LightingExplorerTableColumn.DataType.Checkbox, HDStyles.AffectSpecular, "m_Intensity", 100, (r, prop, dep) =>       // 17: Affect Specular
                {
                    if(!TryGetAdditionalLightData(prop, out var lightData))
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
                }),
                new LightingExplorerTableColumn(LightingExplorerTableColumn.DataType.Float, HDStyles.FadeDistance, "m_Intensity", 95, (r, prop, dep) =>             // 18: Fade Distance
                {
                    if(!TryGetAdditionalLightData(prop, out var lightData))
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
                }),
                new LightingExplorerTableColumn(LightingExplorerTableColumn.DataType.Float, HDStyles.ShadowFadeDistance, "m_Intensity", 145, (r, prop, dep) =>      // 19: Shadow Fade Distance
                {
                    if(!TryGetAdditionalLightData(prop, out var lightData))
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
                }),
                new LightingExplorerTableColumn(LightingExplorerTableColumn.DataType.Custom, HDStyles.LightLayer, "m_RenderingLayerMask", 145, (r, prop, dep) =>    // 20: Light Layer
                {
                    using (new EditorGUI.DisabledScope(!HDRenderPipeline.currentAsset.currentPlatformRenderPipelineSettings.supportLightLayers))
                    {
                        if(!TryGetAdditionalLightData(prop, out var lightData))
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
                }),
                new LightingExplorerTableColumn(LightingExplorerTableColumn.DataType.Custom, HDStyles.IsPrefab, "m_Intensity", 120, (r, prop, dep) =>               // 21: Prefab
                {
                    if (!TryGetLightPrefabData(prop, out var isPrefab, out var prefabRoot))
                    {
                        return;
                    }

                    if (isPrefab)
                    {
                        EditorGUI.ObjectField(r, prefabRoot, typeof(GameObject), false);
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
                new LightingExplorerTableColumn(LightingExplorerTableColumn.DataType.Checkbox, HDStyles.GlobalVolume, "isGlobal", 50),                                  // 2: Is Global
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
                        EditorGUI.EnumPopup(r, volumeData.skyType);
                    }
                }, (lprop, rprop) =>
                {
                    bool lHasVolume = TryGetAdditionalVolumeData(lprop, out var lVolumeData);
                    bool rHasVolume = TryGetAdditionalVolumeData(rprop, out var rVolumeData);

                    return (lHasVolume ? (int)lVolumeData.skyType : -1).CompareTo((rHasVolume ? (int)rVolumeData.skyType : -1));
                }),
                new LightingExplorerTableColumn(LightingExplorerTableColumn.DataType.Enum, HDStyles.FogType, "sharedProfile", 95, (r, prop, dep) =>                     // 7: Fog type
                {
                    if (!TryGetAdditionalVolumeData(prop, out var volumeData))
                    {
                        EditorGUI.LabelField(r, "--");
                        return;
                    }

                    using (new EditorGUI.DisabledScope(true))
                    {
                        EditorGUI.EnumPopup(r, volumeData.fogType);
                    }
                }, (lprop, rprop) =>
                {
                    bool lHasVolume = TryGetAdditionalVolumeData(lprop, out var lVolumeData);
                    bool rHasVolume = TryGetAdditionalVolumeData(rprop, out var rVolumeData);

                    return (lHasVolume ? (int)lVolumeData.fogType : -1).CompareTo((rHasVolume ? (int)rVolumeData.fogType : -1));
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
                }),
                new LightingExplorerTableColumn(LightingExplorerTableColumn.DataType.Float, HDStyles.NearClip, "m_NearClip", 65, (r, prop, dep) =>                      // 4: Near clip
                {
                    if (!TryGetAdditionalReflectionData(prop, out var reflectionData))
                    {
                        EditorGUI.LabelField(r, "--");
                        return;
                    }

                    reflectionData.Update();
                    EditorGUI.PropertyField(r, reflectionData.FindProperty("m_ProbeSettings.camera.frustum.nearClipPlane"), GUIContent.none);
                    reflectionData.ApplyModifiedProperties();
                }, (lprop, rprop) =>
                {
                    TryGetAdditionalReflectionData(lprop, out var lReflectionData);
                    TryGetAdditionalReflectionData(rprop, out var rReflectionData);

                    if (IsNullComparison(lReflectionData, rReflectionData, out var order))
                        return order; 

                    return lReflectionData.FindProperty("m_ProbeSettings.camera.frustum.nearClipPlane").floatValue.CompareTo(rReflectionData.FindProperty("m_ProbeSettings.camera.frustum.nearClipPlane").floatValue);
                }),
                new LightingExplorerTableColumn(LightingExplorerTableColumn.DataType.Float, HDStyles.FarClip, "m_FarClip", 60, (r, prop, dep) =>                        // 5: Far clip
                {
                    if (!TryGetAdditionalReflectionData(prop, out var reflectionData))
                    {
                        EditorGUI.LabelField(r, "--");
                        return;
                    }

                    reflectionData.Update();
                    EditorGUI.PropertyField(r, reflectionData.FindProperty("m_ProbeSettings.camera.frustum.farClipPlane"), GUIContent.none);
                    reflectionData.ApplyModifiedProperties();
                }, (lprop, rprop) =>
                {
                    TryGetAdditionalReflectionData(lprop, out var lReflectionData);
                    TryGetAdditionalReflectionData(rprop, out var rReflectionData);

                    if (IsNullComparison(lReflectionData, rReflectionData, out var order))
                        return order; 

                    return lReflectionData.FindProperty("m_ProbeSettings.camera.frustum.farClipPlane").floatValue.CompareTo(rReflectionData.FindProperty("m_ProbeSettings.camera.frustum.farClipPlane").floatValue);
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
            
            if(light == null || !lightDataPairing.ContainsKey(light))
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
            
            if(volume == null || !volumeDataPairing.ContainsKey(volume))
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
