using System;
using System.Linq.Expressions;
using System.Reflection;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Rendering;

namespace UnityEditor.Rendering.HighDefinition
{
    using CED = CoreEditorDrawer<SerializedHDLight>;

    static partial class HDLightUI
    {
        public static class ScalableSettings
        {
            public static IntScalableSetting ShadowResolution(HDLightType lightType, HDRenderPipelineAsset hdrp)
            {
                switch (lightType)
                {
                    case HDLightType.Directional: return HDAdditionalLightData.ScalableSettings.ShadowResolutionDirectional(hdrp);
                    case HDLightType.Point: return HDAdditionalLightData.ScalableSettings.ShadowResolutionPunctual(hdrp);
                    case HDLightType.Spot: return HDAdditionalLightData.ScalableSettings.ShadowResolutionPunctual(hdrp);
                    case HDLightType.Area: return HDAdditionalLightData.ScalableSettings.ShadowResolutionArea(hdrp);
                    default: throw new ArgumentOutOfRangeException(nameof(lightType));
                }
            }
        }

        enum ShadowmaskMode
        {
            Shadowmask,
            DistanceShadowmask
        }

        [HDRPHelpURL("Light-Component")]
        enum Expandable
        {
            General = 1 << 0,
            Shape = 1 << 1,
            Emission = 1 << 2,
            Volumetric = 1 << 3,
            Shadows = 1 << 4,
            ShadowMap = 1 << 5,
            ContactShadow = 1 << 6,
            BakedShadow = 1 << 7,
            ShadowQuality = 1 << 8,
            CelestialBody = 1 << 9,
        }

        enum AdditionalProperties
        {
            General = 1 << 0,
            Shape = 1 << 1,
            Emission = 1 << 2,
            Shadow = 1 << 3,
        }

        readonly static ExpandedState<Expandable, Light> k_ExpandedState = new ExpandedState<Expandable, Light>(0, "HDRP");
        readonly static AdditionalPropertiesState<AdditionalProperties, Light> k_AdditionalPropertiesState = new AdditionalPropertiesState<AdditionalProperties, Light>(0, "HDRP");

        readonly static HDLightUnitSliderUIDrawer k_LightUnitSliderUIDrawer = new HDLightUnitSliderUIDrawer();

        public static readonly CED.IDrawer Inspector;

        internal static void RegisterEditor(HDLightEditor editor)
        {
            k_AdditionalPropertiesState.RegisterEditor(editor);
        }

        internal static void UnregisterEditor(HDLightEditor editor)
        {
            k_AdditionalPropertiesState.UnregisterEditor(editor);
        }

        [SetAdditionalPropertiesVisibility]
        internal static void SetAdditionalPropertiesVisibility(bool value)
        {
            if (value)
                k_AdditionalPropertiesState.ShowAll();
            else
                k_AdditionalPropertiesState.HideAll();
        }

        static Func<LightingSettings> GetLightingSettingsOrDefaultsFallback;

        static HDLightUI()
        {
            Inspector = CED.Group(
                CED.AdditionalPropertiesFoldoutGroup(LightUI.Styles.generalHeader, Expandable.General, k_ExpandedState, AdditionalProperties.General, k_AdditionalPropertiesState,
                CED.Group((serialized, owner) => DrawGeneralContent(serialized, owner)), DrawGeneralAdditionalContent),
                CED.FoldoutGroup(LightUI.Styles.shapeHeader, Expandable.Shape, k_ExpandedState, DrawShapeContent),
                CED.Conditional((serialized, owner) => serialized.type == HDLightType.Directional && !serialized.settings.isCompletelyBaked,
                    CED.FoldoutGroup(s_Styles.celestialBodyHeader, Expandable.CelestialBody, k_ExpandedState, DrawCelestialBodyContent)),
                CED.AdditionalPropertiesFoldoutGroup(LightUI.Styles.emissionHeader, Expandable.Emission, k_ExpandedState, AdditionalProperties.Emission, k_AdditionalPropertiesState,
                    CED.Group(
                        LightUI.DrawColor,
                        DrawLightIntensityGUILayout,
                        DrawEmissionContent),
                    DrawEmissionAdditionalContent),
                CED.Conditional((serialized, owner) => serialized.type != HDLightType.Area && !serialized.settings.isCompletelyBaked,
                    CED.FoldoutGroup(s_Styles.volumetricHeader, Expandable.Volumetric, k_ExpandedState, DrawVolumetric)),
                CED.Conditional((serialized, owner) =>
                {
                    HDLightType type = serialized.type;
                    return type != HDLightType.Area || type == HDLightType.Area && serialized.areaLightShape != AreaLightShape.Tube;
                },
                    CED.TernaryConditional((serialized, owner) => !serialized.settings.isCompletelyBaked,
                        CED.AdditionalPropertiesFoldoutGroup(LightUI.Styles.shadowHeader, Expandable.Shadows, k_ExpandedState, AdditionalProperties.Shadow, k_AdditionalPropertiesState,
                            CED.Group(
                                CED.Group(
                                    CED.AdditionalPropertiesFoldoutGroup(s_Styles.shadowMapSubHeader, Expandable.ShadowMap, k_ExpandedState, AdditionalProperties.Shadow, k_AdditionalPropertiesState,
                                        DrawShadowMapContent, DrawShadowMapAdditionalContent, FoldoutOption.SubFoldout | FoldoutOption.Indent | FoldoutOption.NoSpaceAtEnd)),
                                CED.space,
                                CED.Conditional((serialized, owner) => k_AdditionalPropertiesState[AdditionalProperties.Shadow] && HasShadowQualitySettingsUI(HDShadowFilteringQuality.VeryHigh, serialized, owner),
                                    CED.FoldoutGroup(s_Styles.veryHighShadowQualitySubHeader, Expandable.ShadowQuality, k_ExpandedState, FoldoutOption.SubFoldout | FoldoutOption.Indent, DrawVeryHighShadowSettingsContent)),
                                CED.Conditional((serialized, owner) => k_AdditionalPropertiesState[AdditionalProperties.Shadow] && HasShadowQualitySettingsUI(HDShadowFilteringQuality.High, serialized, owner),
                                    CED.FoldoutGroup(s_Styles.highShadowQualitySubHeader, Expandable.ShadowQuality, k_ExpandedState, FoldoutOption.SubFoldout | FoldoutOption.Indent, DrawHighShadowSettingsContent)),
                                CED.Conditional((serialized, owner) => HasShadowQualitySettingsUI(HDShadowFilteringQuality.Medium, serialized, owner),
                                    CED.FoldoutGroup(s_Styles.mediumShadowQualitySubHeader, Expandable.ShadowQuality, k_ExpandedState, FoldoutOption.SubFoldout | FoldoutOption.Indent, DrawMediumShadowSettingsContent)),
                                CED.Conditional((serialized, owner) => HasShadowQualitySettingsUI(HDShadowFilteringQuality.Low, serialized, owner),
                                    CED.FoldoutGroup(s_Styles.lowShadowQualitySubHeader, Expandable.ShadowQuality, k_ExpandedState, FoldoutOption.SubFoldout | FoldoutOption.Indent, DrawLowShadowSettingsContent)),
                                CED.Conditional((serialized, owner) => serialized.type != HDLightType.Area,
                                    CED.FoldoutGroup(s_Styles.contactShadowsSubHeader, Expandable.ContactShadow, k_ExpandedState, FoldoutOption.SubFoldout | FoldoutOption.Indent | FoldoutOption.NoSpaceAtEnd, DrawContactShadowsContent)
                                )
                                ),
                            CED.noop //will only add parameter in first sub header
                            ),
                        CED.FoldoutGroup(LightUI.Styles.shadowHeader, Expandable.Shadows, k_ExpandedState,
                            CED.FoldoutGroup(s_Styles.bakedShadowsSubHeader, Expandable.BakedShadow, k_ExpandedState, FoldoutOption.SubFoldout | FoldoutOption.Indent | FoldoutOption.NoSpaceAtEnd, DrawBakedShadowsContent))
                    )
                )
            );

            PresetInspector = CED.Group(
                CED.Group((serialized, owner) =>
                    EditorGUILayout.HelpBox(LightUI.Styles.unsupportedPresetPropertiesMessage, MessageType.Info)),
                CED.Group((serialized, owner) => EditorGUILayout.Space()),
                CED.FoldoutGroup(LightUI.Styles.generalHeader, Expandable.General, k_ExpandedStatePreset, CED.Group((serialized, owner) => DrawGeneralContent(serialized, owner, true))),
                CED.FoldoutGroup(LightUI.Styles.emissionHeader, Expandable.Emission, k_ExpandedStatePreset, CED.Group(
                    LightUI.DrawColor,
                    DrawEmissionContent)),
                CED.FoldoutGroup(LightUI.Styles.shadowHeader, Expandable.Shadows, k_ExpandedStatePreset, DrawEnableShadowMapInternal)
            );

            Type lightMappingType = typeof(Lightmapping);
            var getLightingSettingsOrDefaultsFallbackInfo = lightMappingType.GetMethod("GetLightingSettingsOrDefaultsFallback", BindingFlags.Static | BindingFlags.NonPublic);
            var getLightingSettingsOrDefaultsFallbackLambda = Expression.Lambda<Func<LightingSettings>>(Expression.Call(null, getLightingSettingsOrDefaultsFallbackInfo));
            GetLightingSettingsOrDefaultsFallback = getLightingSettingsOrDefaultsFallbackLambda.Compile();
        }

        // This scope is here mainly to keep pointLightHDType isolated
        public struct LightTypeEditionScope : IDisposable
        {
            EditorGUI.PropertyScope lightTypeScope;
            EditorGUI.PropertyScope pointLightScope;

            public LightTypeEditionScope(Rect rect, GUIContent label, SerializedHDLight serialized, bool isPreset)
            {
                // When editing a Light Preset, the HDAdditionalData, is not editable as is not shown on the inspector, therefore, all the properties
                // That come from the HDAdditionalData are not editable, if we use the PropertyScope for those, as they are not editable this will block
                // the edition of any property that came afterwards. So make sure that we do not use the PropertyScope if the editor is for a preset
                pointLightScope = isPreset ? null : new EditorGUI.PropertyScope(rect, label, serialized.pointLightHDType);
                lightTypeScope = new EditorGUI.PropertyScope(rect, label, serialized.settings.lightType);
            }

            void IDisposable.Dispose()
            {
                lightTypeScope.Dispose();
                pointLightScope?.Dispose();
            }
        }

        static void DrawGeneralContent(SerializedHDLight serialized, Editor owner, bool isPreset = false)
        {
            EditorGUI.BeginChangeCheck();
            Rect lineRect = EditorGUILayout.GetControlRect();
            HDLightType lightType = serialized.type;
            HDLightType updatedLightType;

            //Partial support for prefab. There is no way to fully support it at the moment.
            //Missing support on the Apply and Revert contextual menu on Label for Prefab overrides. They need to be done two times.
            //(This will continue unless we remove AdditionalDatas)
            using (new LightTypeEditionScope(lineRect, s_Styles.shape, serialized, isPreset))
            {
                EditorGUI.showMixedValue = lightType == (HDLightType)(-1);
                updatedLightType = (HDLightType)EditorGUI.EnumPopup(
                    lineRect,
                    s_Styles.shape,
                    lightType,
                    e => !isPreset || (HDLightType)e != HDLightType.Area,
                    false);
            }

            if (EditorGUI.EndChangeCheck())
            {
                serialized.type = updatedLightType; //also register undo

                if (updatedLightType == HDLightType.Area)
                {
                    switch (serialized.areaLightShape)
                    {
                        case AreaLightShape.Rectangle:
                            serialized.shapeWidth.floatValue = Mathf.Max(serialized.shapeWidth.floatValue, HDAdditionalLightData.k_MinLightSize);
                            serialized.shapeHeight.floatValue = Mathf.Max(serialized.shapeHeight.floatValue, HDAdditionalLightData.k_MinLightSize);
                            break;
                        case AreaLightShape.Tube:
                            serialized.settings.shadowsType.SetEnumValue(LightShadows.None);
                            serialized.shapeWidth.floatValue = Mathf.Max(serialized.shapeWidth.floatValue, HDAdditionalLightData.k_MinLightSize);
                            break;
                        case AreaLightShape.Disc:
                            //nothing to do
                            break;
                        case (AreaLightShape)(-1):
                            // don't do anything, this is just to handle multi selection
                            break;
                    }
                }

                UpdateLightIntensityUnit(serialized, owner);

                // For GI we need to detect any change on additional data and call SetLightDirty + For intensity we need to detect light shape change
                serialized.needUpdateAreaLightEmissiveMeshComponents = true;
                serialized.FetchAreaLightEmissiveMeshComponents();
                SetLightsDirty(owner); // Should be apply only to parameter that's affect GI, but make the code cleaner
            }
            EditorGUI.showMixedValue = false;

            // Draw the mode, for Tube and Disc lights, there is only one choice, so we can disable the enum.
            using (new EditorGUI.DisabledScope(updatedLightType == HDLightType.Area && (serialized.areaLightShape == AreaLightShape.Tube || serialized.areaLightShape == AreaLightShape.Disc)))
                serialized.settings.DrawLightmapping();

            if (updatedLightType == HDLightType.Area)
            {
                switch (serialized.areaLightShape)
                {
                    case AreaLightShape.Tube:
                        if (serialized.settings.isBakedOrMixed)
                            EditorGUILayout.HelpBox("Tube Area Lights are realtime only.", MessageType.Error);
                        break;
                    case AreaLightShape.Disc:
                        if (!serialized.settings.isCompletelyBaked)
                            EditorGUILayout.HelpBox("Disc Area Lights are baked only.", MessageType.Error);
                        // Disc lights are not supported in Enlighten
                        if (!Lightmapping.bakedGI && Lightmapping.realtimeGI)
                            EditorGUILayout.HelpBox("Disc Area Lights are not supported with realtime GI.", MessageType.Error);
                        break;
                }
            }
        }

        static void DrawGeneralAdditionalContent(SerializedHDLight serialized, Editor owner)
        {
            if (HDUtils.hdrpSettings.supportLightLayers)
            {
                using (var change = new EditorGUI.ChangeCheckScope())
                {
                    EditorGUILayout.PropertyField(serialized.lightlayersMask, LightUI.Styles.lightLayer);
                    if (change.changed && serialized.linkShadowLayers.boolValue)
                        SyncLightAndShadowLayers(serialized, owner);
                }
            }
        }

        static void DrawShapeContent(SerializedHDLight serialized, Editor owner)
        {
            EditorGUI.BeginChangeCheck(); // For GI we need to detect any change on additional data and call SetLightDirty + For intensity we need to detect light shape change

            // LightShape is HD specific, it need to drive LightType from the original LightType
            // when it make sense, so the GI is still in sync with the light shape
            switch (serialized.type)
            {
                case HDLightType.Directional:
                    EditorGUI.BeginChangeCheck();
                    EditorGUILayout.PropertyField(serialized.angularDiameter, s_Styles.angularDiameter);
                    if (EditorGUI.EndChangeCheck())
                    {
                        serialized.angularDiameter.floatValue = Mathf.Clamp(serialized.angularDiameter.floatValue, 0, 90);
                        serialized.settings.bakedShadowAngleProp.floatValue = serialized.angularDiameter.floatValue;
                    }
                    break;

                case HDLightType.Point:
                    EditorGUI.BeginChangeCheck();
                    EditorGUILayout.PropertyField(serialized.shapeRadius, s_Styles.lightRadius);
                    if (EditorGUI.EndChangeCheck())
                    {
                        //Also affect baked shadows
                        serialized.settings.bakedShadowRadiusProp.floatValue = serialized.shapeRadius.floatValue;
                    }
                    break;

                case HDLightType.Spot:
                    using (var change = new EditorGUI.ChangeCheckScope())
                    {
                        EditorGUILayout.PropertyField(serialized.spotLightShape, s_Styles.spotLightShape);
                        if (change.changed)
                            UpdateLightIntensityUnit(serialized, owner);
                    }

                    using (new EditorGUI.IndentLevelScope())
                    {
                        // If realtime GI is enabled and the shape is unsupported or not implemented, show a warning.
                        if (serialized.settings.isRealtime && SupportedRenderingFeatures.active.enlighten && GetLightingSettingsOrDefaultsFallback.Invoke().realtimeGI)
                        {
                            if (serialized.spotLightShape.GetEnumValue<SpotLightShape>() == SpotLightShape.Box
                                || serialized.spotLightShape.GetEnumValue<SpotLightShape>() == SpotLightShape.Pyramid)
                                EditorGUILayout.HelpBox(s_Styles.unsupportedLightShapeWarning, MessageType.Warning);
                        }

                        switch (serialized.spotLightShape.GetEnumValue<SpotLightShape>())
                        {
                            case SpotLightShape.Box:
                                // Box directional light.
                                EditorGUILayout.PropertyField(serialized.shapeWidth, s_Styles.shapeWidthBox);
                                EditorGUILayout.PropertyField(serialized.shapeHeight, s_Styles.shapeHeightBox);
                                break;
                            case SpotLightShape.Cone:
                                // Cone spot projector
                                EditorGUI.BeginChangeCheck();
                                EditorGUILayout.Slider(serialized.settings.spotAngle, HDAdditionalLightData.k_MinSpotAngle, HDAdditionalLightData.k_MaxSpotAngle, s_Styles.outterAngle);
                                if (EditorGUI.EndChangeCheck())
                                {
                                    serialized.customSpotLightShadowCone.floatValue = Math.Min(serialized.customSpotLightShadowCone.floatValue, serialized.settings.spotAngle.floatValue);
                                }
                                EditorGUILayout.PropertyField(serialized.spotInnerPercent, s_Styles.spotInnerPercent);
                                EditorGUI.BeginChangeCheck();
                                EditorGUILayout.PropertyField(serialized.shapeRadius, s_Styles.lightRadius);
                                if (EditorGUI.EndChangeCheck())
                                {
                                    //Also affect baked shadows
                                    serialized.settings.bakedShadowRadiusProp.floatValue = serialized.shapeRadius.floatValue;
                                }
                                break;
                            case SpotLightShape.Pyramid:
                                // pyramid spot projector
                                EditorGUI.BeginChangeCheck();
                                serialized.settings.DrawSpotAngle();
                                if (EditorGUI.EndChangeCheck())
                                {
                                    serialized.customSpotLightShadowCone.floatValue = Math.Min(serialized.customSpotLightShadowCone.floatValue, serialized.settings.spotAngle.floatValue);
                                }
                                EditorGUILayout.Slider(serialized.aspectRatio, HDAdditionalLightData.k_MinAspectRatio, HDAdditionalLightData.k_MaxAspectRatio, s_Styles.aspectRatioPyramid);
                                EditorGUI.BeginChangeCheck();
                                EditorGUILayout.PropertyField(serialized.shapeRadius, s_Styles.lightRadius);
                                if (EditorGUI.EndChangeCheck())
                                {
                                    //Also affect baked shadows
                                    serialized.settings.bakedShadowRadiusProp.floatValue = serialized.shapeRadius.floatValue;
                                }
                                break;
                            case (SpotLightShape)(-1): //multiple different values
                                using (new EditorGUI.DisabledScope(true))
                                    EditorGUILayout.LabelField("Multiple different spot Shapes in selection");
                                break;
                            default:
                                Debug.Assert(false, "Not implemented spot light shape");
                                break;
                        }
                    }
                    break;

                case HDLightType.Area:
                    EditorGUI.BeginChangeCheck();
                    Rect lineRect = EditorGUILayout.GetControlRect();
                    AreaLightShape updatedAreaLightShape;

                    //Partial support for prefab. There is no way to fully support it at the moment.
                    //Missing support on the Apply and Revert contextual menu on Label for Prefab overrides. They need to be done two times.
                    //(This will continue unless we have our own handling for Disc or remove AdditionalDatas)
                    using (new SerializedHDLight.AreaLightShapeEditionScope(lineRect, s_Styles.shape, serialized))
                    {
                        AreaLightShape areaLightShape = serialized.areaLightShape;
                        EditorGUI.showMixedValue = areaLightShape == (AreaLightShape)(-1);
                        int index = Array.FindIndex((AreaLightShape[])Enum.GetValues(typeof(AreaLightShape)), x => x == areaLightShape);
                        updatedAreaLightShape = (AreaLightShape)EditorGUI.Popup(lineRect, s_Styles.areaLightShape, index, s_Styles.areaShapeNames);
                    }

                    if (EditorGUI.EndChangeCheck())
                    {
                        serialized.areaLightShape = updatedAreaLightShape; //also register undo
                        UpdateLightIntensityUnit(serialized, owner);
                    }
                    EditorGUI.showMixedValue = false;

                    using (new EditorGUI.IndentLevelScope())
                    {
                        switch (updatedAreaLightShape)
                        {
                            case AreaLightShape.Rectangle:
                                EditorGUI.BeginChangeCheck();
                                EditorGUILayout.PropertyField(serialized.shapeWidth, s_Styles.shapeWidthRect);
                                EditorGUILayout.PropertyField(serialized.shapeHeight, s_Styles.shapeHeightRect);
                                if (ShaderConfig.s_BarnDoor == 1)
                                {
                                    EditorGUILayout.PropertyField(serialized.barnDoorAngle, s_Styles.barnDoorAngle);
                                    EditorGUILayout.PropertyField(serialized.barnDoorLength, s_Styles.barnDoorLength);
                                }
                                if (EditorGUI.EndChangeCheck())
                                {
                                    serialized.settings.areaSizeX.floatValue = serialized.shapeWidth.floatValue;
                                    serialized.settings.areaSizeY.floatValue = serialized.shapeHeight.floatValue;
                                    if (ShaderConfig.s_BarnDoor == 1)
                                    {
                                        serialized.barnDoorAngle.floatValue = Mathf.Clamp(serialized.barnDoorAngle.floatValue, 0.0f, 90.0f);
                                        serialized.barnDoorLength.floatValue = Mathf.Clamp(serialized.barnDoorLength.floatValue, 0.0f, float.MaxValue);
                                    }
                                }
                                break;
                            case AreaLightShape.Tube:
                                EditorGUI.BeginChangeCheck();
                                EditorGUILayout.PropertyField(serialized.shapeWidth, s_Styles.shapeWidthTube);
                                if (EditorGUI.EndChangeCheck())
                                {
                                    // Fake line with a small rectangle in vanilla unity for GI
                                    serialized.settings.areaSizeX.floatValue = serialized.shapeWidth.floatValue;
                                    serialized.settings.areaSizeY.floatValue = HDAdditionalLightData.k_MinLightSize;
                                }
                                // If realtime GI is enabled and the shape is unsupported or not implemented, show a warning.
                                if (serialized.settings.isRealtime && SupportedRenderingFeatures.active.enlighten && GetLightingSettingsOrDefaultsFallback.Invoke().realtimeGI)
                                {
                                    EditorGUILayout.HelpBox(s_Styles.unsupportedLightShapeWarning, MessageType.Warning);
                                }
                                break;
                            case AreaLightShape.Disc:
                                //draw the built-in area light control at the moment as everything is handled by built-in
                                serialized.settings.DrawArea();
                                serialized.displayAreaLightEmissiveMesh.boolValue = false; //force deactivate emissive mesh for Disc (not supported)
                                break;
                            case (AreaLightShape)(-1): //multiple different values
                                using (new EditorGUI.DisabledScope(true))
                                    EditorGUILayout.LabelField("Multiple different area Shapes in selection");
                                break;
                            default:
                                Debug.Assert(false, "Not implemented area light shape");
                                break;
                        }
                    }
                    break;

                case (HDLightType)(-1): //multiple different values
                    using (new EditorGUI.DisabledScope(true))
                        EditorGUILayout.LabelField("Multiple different Types in selection");
                    break;

                default:
                    Debug.Assert(false, "Not implemented light type");
                    break;
            }

            if (EditorGUI.EndChangeCheck())
            {
                serialized.needUpdateAreaLightEmissiveMeshComponents = true;
                SetLightsDirty(owner); // Should be apply only to parameter that's affect GI, but make the code cleaner
            }
        }

        static void DrawCelestialBodyContent(SerializedHDLight serialized, Editor owner)
        {
            EditorGUI.BeginChangeCheck();
            {
                EditorGUILayout.PropertyField(serialized.interactsWithSky, s_Styles.interactsWithSky);

                using (new EditorGUI.DisabledScope(!serialized.interactsWithSky.boolValue))
                {
                    EditorGUI.indentLevel++;
                    EditorGUILayout.PropertyField(serialized.flareSize, s_Styles.flareSize);
                    EditorGUILayout.PropertyField(serialized.flareFalloff, s_Styles.flareFalloff);
                    EditorGUILayout.PropertyField(serialized.flareTint, s_Styles.flareTint);
                    EditorGUILayout.PropertyField(serialized.surfaceTexture, s_Styles.surfaceTexture);
                    EditorGUILayout.PropertyField(serialized.surfaceTint, s_Styles.surfaceTint);
                    EditorGUILayout.PropertyField(serialized.distance, s_Styles.distance);
                    EditorGUI.indentLevel--;
                }
            }

            if (EditorGUI.EndChangeCheck())
            {
                // Clamp the value and also affect baked shadows.
                serialized.flareSize.floatValue = Mathf.Clamp(serialized.flareSize.floatValue, 0, 90);
                serialized.flareFalloff.floatValue = Mathf.Max(serialized.flareFalloff.floatValue, 0);
                serialized.distance.floatValue = Mathf.Max(serialized.distance.floatValue, 0);
            }
        }

        static void UpdateLightIntensityUnit(SerializedHDLight serialized, Editor owner)
        {
            HDLightType lightType = serialized.type;
            // Box are local directional light
            if (lightType == HDLightType.Directional ||
                (lightType == HDLightType.Spot && (serialized.spotLightShape.GetEnumValue<SpotLightShape>() == SpotLightShape.Box)))
            {
                serialized.lightUnit.SetEnumValue((LightUnit)DirectionalLightUnit.Lux);
                // We need to reset luxAtDistance to neutral when changing to (local) directional light, otherwise first display value ins't correct
                serialized.luxAtDistance.floatValue = 1.0f;
            }
        }

        static void DrawLightIntensityUnitPopup(Rect rect, SerializedHDLight serialized, Editor owner)
        {
            LightUnit selectedLightUnit;
            LightUnit oldLigthUnit = serialized.lightUnit.GetEnumValue<LightUnit>();

            EditorGUI.showMixedValue = serialized.lightUnit.hasMultipleDifferentValues;
            EditorGUI.BeginChangeCheck();

            EditorGUI.BeginProperty(rect, GUIContent.none, serialized.lightUnit);
            switch (serialized.type)
            {
                case HDLightType.Directional:
                    selectedLightUnit = (LightUnit)EditorGUI.EnumPopup(rect, (DirectionalLightUnit)serialized.lightUnit.GetEnumValue<DirectionalLightUnit>());
                    break;
                case HDLightType.Point:
                    selectedLightUnit = (LightUnit)EditorGUI.EnumPopup(rect, (PunctualLightUnit)serialized.lightUnit.GetEnumValue<PunctualLightUnit>());
                    break;
                case HDLightType.Spot:
                    if (serialized.spotLightShape.GetEnumValue<SpotLightShape>() == SpotLightShape.Box)
                        selectedLightUnit = (LightUnit)EditorGUI.EnumPopup(rect, (DirectionalLightUnit)serialized.lightUnit.GetEnumValue<DirectionalLightUnit>());
                    else
                        selectedLightUnit = (LightUnit)EditorGUI.EnumPopup(rect, (PunctualLightUnit)serialized.lightUnit.GetEnumValue<PunctualLightUnit>());
                    break;
                default:
                    selectedLightUnit = (LightUnit)EditorGUI.EnumPopup(rect, (AreaLightUnit)serialized.lightUnit.GetEnumValue<AreaLightUnit>());
                    break;
            }
            EditorGUI.EndProperty();

            EditorGUI.showMixedValue = false;

            if (EditorGUI.EndChangeCheck())
            {
                ConvertLightIntensity(oldLigthUnit, selectedLightUnit, serialized, owner);
                serialized.lightUnit.SetEnumValue(selectedLightUnit);
            }
        }

        internal static void ConvertLightIntensity(LightUnit oldLightUnit, LightUnit newLightUnit, SerializedHDLight serialized, Editor owner)
        {
            serialized.intensity.floatValue = ConvertLightIntensity(oldLightUnit, newLightUnit, serialized, owner, serialized.intensity.floatValue);
        }

        internal static float ConvertLightIntensity(LightUnit oldLightUnit, LightUnit newLightUnit, SerializedHDLight serialized, Editor owner, float intensity)
        {
            Light light = (Light)owner.target;

            // For punctual lights
            HDLightType lightType = serialized.type;
            switch (lightType)
            {
                case HDLightType.Directional:
                case HDLightType.Point:
                case HDLightType.Spot:
                    // Lumen ->
                    if (oldLightUnit == LightUnit.Lumen && newLightUnit == LightUnit.Candela)
                        intensity = LightUtils.ConvertPunctualLightLumenToCandela(lightType, intensity, light.intensity, serialized.enableSpotReflector.boolValue);
                    else if (oldLightUnit == LightUnit.Lumen && newLightUnit == LightUnit.Lux)
                        intensity = LightUtils.ConvertPunctualLightLumenToLux(lightType, intensity, light.intensity, serialized.enableSpotReflector.boolValue,
                            serialized.luxAtDistance.floatValue);
                    else if (oldLightUnit == LightUnit.Lumen && newLightUnit == LightUnit.Ev100)
                        intensity = LightUtils.ConvertPunctualLightLumenToEv(lightType, intensity, light.intensity, serialized.enableSpotReflector.boolValue);
                    // Candela ->
                    else if (oldLightUnit == LightUnit.Candela && newLightUnit == LightUnit.Lumen)
                        intensity = LightUtils.ConvertPunctualLightCandelaToLumen(lightType, serialized.spotLightShape.GetEnumValue<SpotLightShape>(), intensity, serialized.enableSpotReflector.boolValue,
                            light.spotAngle, serialized.aspectRatio.floatValue);
                    else if (oldLightUnit == LightUnit.Candela && newLightUnit == LightUnit.Lux)
                        intensity = LightUtils.ConvertCandelaToLux(intensity, serialized.luxAtDistance.floatValue);
                    else if (oldLightUnit == LightUnit.Candela && newLightUnit == LightUnit.Ev100)
                        intensity = LightUtils.ConvertCandelaToEv(intensity);
                    // Lux ->
                    else if (oldLightUnit == LightUnit.Lux && newLightUnit == LightUnit.Lumen)
                        intensity = LightUtils.ConvertPunctualLightLuxToLumen(lightType, serialized.spotLightShape.GetEnumValue<SpotLightShape>(), intensity, serialized.enableSpotReflector.boolValue,
                            light.spotAngle, serialized.aspectRatio.floatValue, serialized.luxAtDistance.floatValue);
                    else if (oldLightUnit == LightUnit.Lux && newLightUnit == LightUnit.Candela)
                        intensity = LightUtils.ConvertLuxToCandela(intensity, serialized.luxAtDistance.floatValue);
                    else if (oldLightUnit == LightUnit.Lux && newLightUnit == LightUnit.Ev100)
                        intensity = LightUtils.ConvertLuxToEv(intensity, serialized.luxAtDistance.floatValue);
                    // EV100 ->
                    else if (oldLightUnit == LightUnit.Ev100 && newLightUnit == LightUnit.Lumen)
                        intensity = LightUtils.ConvertPunctualLightEvToLumen(lightType, serialized.spotLightShape.GetEnumValue<SpotLightShape>(), intensity, serialized.enableSpotReflector.boolValue,
                            light.spotAngle, serialized.aspectRatio.floatValue);
                    else if (oldLightUnit == LightUnit.Ev100 && newLightUnit == LightUnit.Candela)
                        intensity = LightUtils.ConvertEvToCandela(intensity);
                    else if (oldLightUnit == LightUnit.Ev100 && newLightUnit == LightUnit.Lux)
                        intensity = LightUtils.ConvertEvToLux(intensity, serialized.luxAtDistance.floatValue);
                    break;

                case HDLightType.Area:
                    if (oldLightUnit == LightUnit.Lumen && newLightUnit == LightUnit.Nits)
                        intensity = LightUtils.ConvertAreaLightLumenToLuminance(serialized.areaLightShape, intensity, serialized.shapeWidth.floatValue, serialized.shapeHeight.floatValue);
                    if (oldLightUnit == LightUnit.Nits && newLightUnit == LightUnit.Lumen)
                        intensity = LightUtils.ConvertAreaLightLuminanceToLumen(serialized.areaLightShape, intensity, serialized.shapeWidth.floatValue, serialized.shapeHeight.floatValue);
                    if (oldLightUnit == LightUnit.Nits && newLightUnit == LightUnit.Ev100)
                        intensity = LightUtils.ConvertLuminanceToEv(intensity);
                    if (oldLightUnit == LightUnit.Ev100 && newLightUnit == LightUnit.Nits)
                        intensity = LightUtils.ConvertEvToLuminance(intensity);
                    if (oldLightUnit == LightUnit.Ev100 && newLightUnit == LightUnit.Lumen)
                        intensity = LightUtils.ConvertAreaLightEvToLumen(serialized.areaLightShape, intensity, serialized.shapeWidth.floatValue, serialized.shapeHeight.floatValue);
                    if (oldLightUnit == LightUnit.Lumen && newLightUnit == LightUnit.Ev100)
                        intensity = LightUtils.ConvertAreaLightLumenToEv(serialized.areaLightShape, intensity, serialized.shapeWidth.floatValue, serialized.shapeHeight.floatValue);
                    break;

                default:
                case (HDLightType)(-1): // multiple different values
                    break;  // do nothing
            }

            return intensity;
        }

        static void DrawLightIntensityGUILayout(SerializedHDLight serialized, Editor owner)
        {
            // Match const defined in EditorGUI.cs
            const int k_IndentPerLevel = 15;

            const int k_ValueUnitSeparator = 2;
            const int k_UnitWidth = 100;

            float indent = k_IndentPerLevel * EditorGUI.indentLevel;

            Rect lineRect = EditorGUILayout.GetControlRect();
            Rect labelRect = lineRect;
            labelRect.width = EditorGUIUtility.labelWidth;

            // Expand to reach both lines of the intensity field.
            var interlineOffset = EditorGUIUtility.singleLineHeight + 2f;
            labelRect.height += interlineOffset;

            //handling of prefab overrides in a parent label
            GUIContent parentLabel = s_Styles.lightIntensity;
            parentLabel = EditorGUI.BeginProperty(labelRect, parentLabel, serialized.lightUnit);
            parentLabel = EditorGUI.BeginProperty(labelRect, parentLabel, serialized.intensity);
            {
                // Restore the original rect for actually drawing the label.
                labelRect.height -= interlineOffset;

                EditorGUI.LabelField(labelRect, parentLabel);
            }
            EditorGUI.EndProperty();
            EditorGUI.EndProperty();

            // Draw the light unit slider + icon + tooltip
            Rect lightUnitSliderRect = lineRect; // TODO: Move the value and unit rects to new line
            lightUnitSliderRect.x += EditorGUIUtility.labelWidth + k_ValueUnitSeparator;
            lightUnitSliderRect.width -= EditorGUIUtility.labelWidth + k_ValueUnitSeparator;

            var lightType = serialized.type;
            var lightUnit = serialized.lightUnit.GetEnumValue<LightUnit>();
            k_LightUnitSliderUIDrawer.SetSerializedObject(serialized.serializedObject);
            k_LightUnitSliderUIDrawer.Draw(lightType, lightUnit, serialized.intensity, lightUnitSliderRect, serialized, owner);

            // We use PropertyField to draw the value to keep the handle at left of the field
            // This will apply the indent again thus we need to remove it time for alignment
            Rect valueRect = EditorGUILayout.GetControlRect();
            labelRect.width = EditorGUIUtility.labelWidth;
            valueRect.width += indent - k_ValueUnitSeparator - k_UnitWidth;
            Rect unitRect = valueRect;
            unitRect.x += valueRect.width - indent + k_ValueUnitSeparator;
            unitRect.width = k_UnitWidth + .5f;

            // Draw the unit textfield
            EditorGUI.BeginChangeCheck();
            EditorGUI.PropertyField(valueRect, serialized.intensity, CoreEditorStyles.empty);
            DrawLightIntensityUnitPopup(unitRect, serialized, owner);

            if (EditorGUI.EndChangeCheck())
            {
                serialized.intensity.floatValue = Mathf.Max(serialized.intensity.floatValue, 0.0f);
            }
        }

        static void DrawEmissionContent(SerializedHDLight serialized, Editor owner)
        {
            HDLightType lightType = serialized.type;
            SpotLightShape spotLightShape = serialized.spotLightShape.GetEnumValue<SpotLightShape>();
            LightUnit lightUnit = serialized.lightUnit.GetEnumValue<LightUnit>();

            if (lightType != HDLightType.Directional
                // Box are local directional light and shouldn't display the Lux At widget. It use only lux
                && !(lightType == HDLightType.Spot && (spotLightShape == SpotLightShape.Box))
                && lightUnit == (LightUnit)PunctualLightUnit.Lux)
            {
                EditorGUI.indentLevel++;
                EditorGUI.BeginChangeCheck();
                EditorGUILayout.PropertyField(serialized.luxAtDistance, s_Styles.luxAtDistance);
                if (EditorGUI.EndChangeCheck())
                {
                    serialized.luxAtDistance.floatValue = Mathf.Max(serialized.luxAtDistance.floatValue, 0.01f);
                }
                EditorGUI.indentLevel--;
            }

            if (lightType == HDLightType.Spot
                && (spotLightShape == SpotLightShape.Cone || spotLightShape == SpotLightShape.Pyramid)
                // Display reflector only when showing additional properties.
                && (lightUnit == (int)PunctualLightUnit.Lumen && k_AdditionalPropertiesState[AdditionalProperties.Emission]))
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(serialized.enableSpotReflector, s_Styles.enableSpotReflector);
                EditorGUI.indentLevel--;
            }

            if (lightType != HDLightType.Directional)
            {
                EditorGUI.BeginChangeCheck();
#if UNITY_2020_1_OR_NEWER
                serialized.settings.DrawRange();
#else
                serialized.settings.DrawRange(false);
#endif
                // Make sure the range is not 0.0
                serialized.settings.range.floatValue = Mathf.Max(0.001f, serialized.settings.range.floatValue);

                if (EditorGUI.EndChangeCheck())
                {
                    // For GI we need to detect any change on additional data and call SetLightDirty + For intensity we need to detect light shape change
                    serialized.needUpdateAreaLightEmissiveMeshComponents = true;
                    SetLightsDirty(owner); // Should be apply only to parameter that's affect GI, but make the code cleaner
                }
            }

            serialized.settings.DrawBounceIntensity();

            EditorGUI.BeginChangeCheck(); // For GI we need to detect any change on additional data and call SetLightDirty

            if (lightType != HDLightType.Area)
            {
                serialized.settings.DrawCookie();

                if (serialized.settings.cookie is Texture cookie && cookie != null)
                {
                    // When directional light use a cookie, it can control the size
                    if (lightType == HDLightType.Directional)
                    {
                        EditorGUI.indentLevel++;
                        EditorGUI.BeginChangeCheck();
                        var size = new Vector2(serialized.shapeWidth.floatValue, serialized.shapeHeight.floatValue);
                        size = EditorGUILayout.Vector2Field(s_Styles.cookieSize, size);
                        if (EditorGUI.EndChangeCheck())
                        {
                            serialized.shapeWidth.floatValue = size.x;
                            serialized.shapeHeight.floatValue = size.y;
                        }
                        EditorGUI.indentLevel--;
                    }
                    else if (lightType == HDLightType.Point && cookie.dimension != TextureDimension.Cube)
                    {
                        Debug.LogError($"The cookie texture '{cookie.name}' isn't compatible with the Point Light type. Only Cube textures are supported.");
                        serialized.settings.cookieProp.objectReferenceValue = null;
                    }
                    else if (lightType == HDLightType.Spot && cookie.dimension != TextureDimension.Tex2D)
                    {
                        Debug.LogError($"The cookie texture '{cookie.name}' isn't compatible with the Spot Light type. Only 2D textures are supported.");
                        serialized.settings.cookieProp.objectReferenceValue = null;
                    }
                }

                ShowCookieTextureWarnings(serialized.settings.cookie, serialized.settings.isCompletelyBaked || serialized.settings.isBakedOrMixed);
            }
            else if (serialized.areaLightShape == AreaLightShape.Rectangle || serialized.areaLightShape == AreaLightShape.Disc)
            {
                EditorGUILayout.ObjectField(serialized.areaLightCookie, s_Styles.areaLightCookie);
                ShowCookieTextureWarnings(serialized.areaLightCookie.objectReferenceValue as Texture, serialized.settings.isCompletelyBaked || serialized.settings.isBakedOrMixed);
            }
            if (serialized.type == HDLightType.Point || serialized.type == HDLightType.Spot || (serialized.type == HDLightType.Area && serialized.areaLightShape == AreaLightShape.Rectangle))
            {
                EditorGUI.BeginChangeCheck();
                UnityEngine.Object iesAsset = EditorGUILayout.ObjectField(
                    s_Styles.iesTexture,
                    serialized.type == HDLightType.Point ? serialized.iesPoint.objectReferenceValue : serialized.iesSpot.objectReferenceValue,
                    typeof(IESObject), false);
                if (EditorGUI.EndChangeCheck())
                {
                    SerializedProperty pointTex = serialized.iesPoint;
                    SerializedProperty spotTex = serialized.iesSpot;
                    if (iesAsset == null)
                    {
                        pointTex.objectReferenceValue = null;
                        spotTex.objectReferenceValue = null;
                    }
                    else
                    {
                        string guid;
                        long localID;
                        AssetDatabase.TryGetGUIDAndLocalFileIdentifier(iesAsset, out guid, out localID);
                        string path = AssetDatabase.GUIDToAssetPath(guid);
                        UnityEngine.Object[] textures = AssetDatabase.LoadAllAssetRepresentationsAtPath(path);
                        foreach (var subAsset in textures)
                        {
                            if (AssetDatabase.IsSubAsset(subAsset) && subAsset.name.EndsWith("-Cube-IES"))
                            {
                                pointTex.objectReferenceValue = subAsset;
                            }
                            else if (AssetDatabase.IsSubAsset(subAsset) && subAsset.name.EndsWith("-2D-IES"))
                            {
                                spotTex.objectReferenceValue = subAsset;
                            }
                        }
                    }
                    serialized.iesPoint.serializedObject.ApplyModifiedProperties();
                    serialized.iesSpot.serializedObject.ApplyModifiedProperties();
                }

                if (serialized.type == HDLightType.Spot &&
                    serialized.spotLightShape.enumValueIndex == (int)SpotLightShape.Cone &&
                    serialized.iesSpot.objectReferenceValue != null)
                {
                    EditorGUILayout.PropertyField(serialized.spotIESCutoffPercent, s_Styles.spotIESCutoffPercent);
                }
            }

            if (EditorGUI.EndChangeCheck())
            {
                serialized.needUpdateAreaLightEmissiveMeshComponents = true;
                SetLightsDirty(owner); // Should be apply only to parameter that's affect GI, but make the code cleaner
            }
        }

        static void ShowCookieTextureWarnings(Texture cookie, bool useBaking)
        {
            if (cookie == null)
                return;

            // The texture type is stored in the texture importer so we need to get it:
            TextureImporter texImporter = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(cookie)) as TextureImporter;

            if (texImporter != null && texImporter.textureType == TextureImporterType.Cookie)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    int indentSpace = (int)EditorGUI.IndentedRect(new Rect()).x;
                    GUILayout.Space(indentSpace);
                    using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
                    {
                        int oldIndentLevel = EditorGUI.indentLevel;
                        EditorGUI.indentLevel = 0;
                        GUIStyle wordWrap = new GUIStyle(EditorStyles.miniLabel) { wordWrap = true };
                        EditorGUILayout.LabelField(s_Styles.cookieTextureTypeError, wordWrap);
                        if (GUILayout.Button("Fix", GUILayout.ExpandHeight(true)))
                        {
                            texImporter.textureType = TextureImporterType.Default;
                            texImporter.SaveAndReimport();
                        }
                        EditorGUI.indentLevel = oldIndentLevel;
                    }
                }
            }

            if (useBaking && !UnityEditor.EditorSettings.enableCookiesInLightmapper)
                EditorGUILayout.HelpBox(s_Styles.cookieBaking, MessageType.Warning);
            if (cookie.width != cookie.height)
                EditorGUILayout.HelpBox(s_Styles.cookieNonPOT, MessageType.Warning);
            if (cookie.width < LightCookieManager.k_MinCookieSize || cookie.height < LightCookieManager.k_MinCookieSize)
                EditorGUILayout.HelpBox(s_Styles.cookieTooSmall, MessageType.Warning);
        }

        static void DrawEmissionAdditionalContent(SerializedHDLight serialized, Editor owner)
        {
            HDLightType lightType = serialized.type;
            EditorGUI.BeginChangeCheck(); // For GI we need to detect any change on additional data and call SetLightDirty

            bool bakedOnly = serialized.settings.isCompletelyBaked;
            if (!bakedOnly)
            {
                EditorGUILayout.PropertyField(serialized.affectDiffuse, s_Styles.affectDiffuse);
                EditorGUILayout.PropertyField(serialized.affectSpecular, s_Styles.affectSpecular);
                if (lightType != HDLightType.Directional)
                {
                    EditorGUILayout.PropertyField(serialized.applyRangeAttenuation, s_Styles.applyRangeAttenuation);
                    EditorGUILayout.PropertyField(serialized.fadeDistance, s_Styles.fadeDistance);
                }
                EditorGUILayout.PropertyField(serialized.lightDimmer, s_Styles.lightDimmer);
            }
            else if (lightType == HDLightType.Point || lightType == HDLightType.Spot)
                EditorGUILayout.PropertyField(serialized.applyRangeAttenuation, s_Styles.applyRangeAttenuation);

            // Emissive mesh for area light only (and not supported on Disc currently)
            if (lightType == HDLightType.Area && serialized.areaLightShape != AreaLightShape.Disc)
            {
                EditorGUI.BeginChangeCheck();
                EditorGUILayout.PropertyField(serialized.displayAreaLightEmissiveMesh, s_Styles.displayAreaLightEmissiveMesh);
                if (EditorGUI.EndChangeCheck())
                {
                    serialized.FetchAreaLightEmissiveMeshComponents();
                    serialized.needUpdateAreaLightEmissiveMeshComponents = true;
                }

                bool showSubArea = serialized.displayAreaLightEmissiveMesh.boolValue && !serialized.displayAreaLightEmissiveMesh.hasMultipleDifferentValues;
                ++EditorGUI.indentLevel;

                Rect lineRect = EditorGUILayout.GetControlRect();
                ShadowCastingMode newCastShadow;
                EditorGUI.showMixedValue = serialized.areaLightEmissiveMeshCastShadow.hasMultipleDifferentValues;
                EditorGUI.BeginChangeCheck();
                using (new SerializedHDLight.AreaLightEmissiveMeshDrawScope(lineRect, s_Styles.areaLightEmissiveMeshCastShadow, showSubArea, serialized.areaLightEmissiveMeshCastShadow, serialized.deportedAreaLightEmissiveMeshCastShadow))
                {
                    newCastShadow = (ShadowCastingMode)EditorGUI.EnumPopup(lineRect, s_Styles.areaLightEmissiveMeshCastShadow, (ShadowCastingMode)serialized.areaLightEmissiveMeshCastShadow.intValue);
                }
                if (EditorGUI.EndChangeCheck())
                {
                    serialized.UpdateAreaLightEmissiveMeshCastShadow(newCastShadow);
                }
                EditorGUI.showMixedValue = false;

                lineRect = EditorGUILayout.GetControlRect();
                SerializedHDLight.MotionVector newMotionVector;
                EditorGUI.showMixedValue = serialized.areaLightEmissiveMeshMotionVector.hasMultipleDifferentValues;
                EditorGUI.BeginChangeCheck();
                using (new SerializedHDLight.AreaLightEmissiveMeshDrawScope(lineRect, s_Styles.areaLightEmissiveMeshMotionVector, showSubArea, serialized.areaLightEmissiveMeshMotionVector, serialized.deportedAreaLightEmissiveMeshMotionVector))
                {
                    newMotionVector = (SerializedHDLight.MotionVector)EditorGUI.EnumPopup(lineRect, s_Styles.areaLightEmissiveMeshMotionVector, (SerializedHDLight.MotionVector)serialized.areaLightEmissiveMeshMotionVector.intValue);
                }
                if (EditorGUI.EndChangeCheck())
                {
                    serialized.UpdateAreaLightEmissiveMeshMotionVectorGeneration(newMotionVector);
                }
                EditorGUI.showMixedValue = false;

                EditorGUI.showMixedValue = serialized.areaLightEmissiveMeshLayer.hasMultipleDifferentValues || serialized.lightLayer.hasMultipleDifferentValues;
                EditorGUI.BeginChangeCheck();
                bool toggle;
                using (new SerializedHDLight.AreaLightEmissiveMeshDrawScope(lineRect, s_Styles.areaLightEmissiveMeshSameLayer, showSubArea, serialized.areaLightEmissiveMeshLayer, serialized.deportedAreaLightEmissiveMeshLayer))
                {
                    toggle = EditorGUILayout.Toggle(s_Styles.areaLightEmissiveMeshSameLayer, serialized.areaLightEmissiveMeshLayer.intValue == -1);
                }
                if (EditorGUI.EndChangeCheck())
                {
                    serialized.UpdateAreaLightEmissiveMeshLayer(serialized.lightLayer.intValue);
                    if (toggle)
                        serialized.areaLightEmissiveMeshLayer.intValue = -1;
                }
                EditorGUI.showMixedValue = false;

                ++EditorGUI.indentLevel;
                if (toggle || serialized.areaLightEmissiveMeshLayer.hasMultipleDifferentValues)
                {
                    using (new EditorGUI.DisabledScope(true))
                    {
                        lineRect = EditorGUILayout.GetControlRect();
                        EditorGUI.showMixedValue = serialized.areaLightEmissiveMeshLayer.hasMultipleDifferentValues || serialized.lightLayer.hasMultipleDifferentValues;
                        EditorGUI.LayerField(lineRect, s_Styles.areaLightEmissiveMeshCustomLayer, serialized.lightLayer.intValue);
                        EditorGUI.showMixedValue = false;
                    }
                }
                else
                {
                    EditorGUI.showMixedValue = serialized.areaLightEmissiveMeshLayer.hasMultipleDifferentValues;
                    lineRect = EditorGUILayout.GetControlRect();
                    int layer;
                    EditorGUI.BeginChangeCheck();
                    using (new SerializedHDLight.AreaLightEmissiveMeshDrawScope(lineRect, s_Styles.areaLightEmissiveMeshCustomLayer, showSubArea, serialized.areaLightEmissiveMeshLayer, serialized.deportedAreaLightEmissiveMeshLayer))
                    {
                        layer = EditorGUI.LayerField(lineRect, s_Styles.areaLightEmissiveMeshCustomLayer, serialized.areaLightEmissiveMeshLayer.intValue);
                    }
                    if (EditorGUI.EndChangeCheck())
                    {
                        serialized.UpdateAreaLightEmissiveMeshLayer(layer);
                    }
                    // or if the value of layer got changed using the layer change including child mechanism (strangely apply even if object not editable),
                    // discard the change: the child is not saved anyway so the value in HDAdditionalLightData is the only serialized one.
                    else if (!EditorGUI.showMixedValue
                             && serialized.deportedAreaLightEmissiveMeshLayer != null
                             && !serialized.deportedAreaLightEmissiveMeshLayer.Equals(null)
                             && serialized.areaLightEmissiveMeshLayer.intValue != serialized.deportedAreaLightEmissiveMeshLayer.intValue)
                    {
                        GUI.changed = true; //force register change to handle update and apply later
                        serialized.UpdateAreaLightEmissiveMeshLayer(layer);
                    }
                    EditorGUI.showMixedValue = false;
                }
                --EditorGUI.indentLevel;

                --EditorGUI.indentLevel;
            }

            EditorGUILayout.PropertyField(serialized.includeForRayTracing, s_Styles.includeLightForRayTracing);

            if (EditorGUI.EndChangeCheck())
            {
                serialized.needUpdateAreaLightEmissiveMeshComponents = true;
                serialized.fadeDistance.floatValue = Mathf.Max(serialized.fadeDistance.floatValue, 0.01f);
                SetLightsDirty(owner); // Should be apply only to parameter that's affect GI, but make the code cleaner
            }
        }

        static void DrawVolumetric(SerializedHDLight serialized, Editor owner)
        {
            EditorGUILayout.PropertyField(serialized.useVolumetric, s_Styles.volumetricEnable);
            using (new EditorGUI.DisabledScope(!serialized.useVolumetric.boolValue))
            using (new EditorGUI.IndentLevelScope())
            {
                EditorGUILayout.PropertyField(serialized.volumetricDimmer, s_Styles.volumetricDimmer);
                EditorGUILayout.Slider(serialized.volumetricShadowDimmer, 0.0f, 1.0f, s_Styles.volumetricShadowDimmer);
                HDLightType lightType = serialized.type;
                if (lightType != HDLightType.Directional)
                {
                    EditorGUILayout.PropertyField(serialized.volumetricFadeDistance, s_Styles.volumetricFadeDistance);
                }
            }
        }

        static bool DrawEnableShadowMap(SerializedHDLight serialized, Editor owner)
        {
            Rect lineRect = EditorGUILayout.GetControlRect();
            bool newShadowsEnabled;

            EditorGUI.BeginProperty(lineRect, s_Styles.enableShadowMap, serialized.settings.shadowsType);
            {
                bool oldShadowEnabled = serialized.settings.shadowsType.GetEnumValue<LightShadows>() != LightShadows.None;
                newShadowsEnabled = EditorGUI.Toggle(lineRect, s_Styles.enableShadowMap, oldShadowEnabled);
                if (oldShadowEnabled ^ newShadowsEnabled)
                {
                    serialized.settings.shadowsType.SetEnumValue(newShadowsEnabled ? LightShadows.Hard : LightShadows.None);
                }
            }
            EditorGUI.EndProperty();

            return newShadowsEnabled;
        }

        // Needed to work around the need for CED Group with no return value
        static void DrawEnableShadowMapInternal(SerializedHDLight serialized, Editor owner)
        {
            DrawEnableShadowMap(serialized, owner);
        }

        static void DrawShadowMapContent(SerializedHDLight serialized, Editor owner)
        {
            var hdrp = HDRenderPipeline.currentAsset;
            bool newShadowsEnabled = DrawEnableShadowMap(serialized, owner);


            HDLightType lightType = serialized.type;

            using (new EditorGUI.DisabledScope(!newShadowsEnabled))
            {
                EditorGUILayout.PropertyField(serialized.shadowUpdateMode, s_Styles.shadowUpdateMode);

                EditorGUI.indentLevel++;

                if (serialized.shadowUpdateMode.intValue > 0 && serialized.type != HDLightType.Directional)
                {
                    if (owner.targets.Length == 1)
                    {
                        HDLightEditor editor = owner as HDLightEditor;
                        var additionalLightData = editor.GetAdditionalDataForTargetIndex(0);
                        // If the light was registered, but not placed it means it doesn't fit.
                        if (additionalLightData.lightIdxForCachedShadows >= 0 && !HDCachedShadowManager.instance.LightHasBeenPlacedInAtlas(additionalLightData))
                        {
                            string warningMessage = "The shadow for this light doesn't fit the cached shadow atlas and therefore won't be rendered. Please ensure you have enough space in the cached shadow atlas. You can use the light explorer (Window->Rendering->Light Explorer) to see which lights fit and which don't.\nConsult HDRP Shadow documentation for more information about cached shadow management.";
                            // Loop backward in "tile" size to check
                            const int slotSize = HDCachedShadowManager.k_MinSlotSize;

                            bool showFitButton = false;
                            if (HDCachedShadowManager.instance.WouldFitInAtlas(slotSize, lightType))
                            {
                                warningMessage += "\nAlternatively, click the button below to find the resolution that will fit the shadow in the atlas.";
                                showFitButton = true;
                            }
                            else
                            {
                                warningMessage += "\nThe atlas is completely full so either change the resolution of other shadow maps or increase atlas size.";
                            }
                            EditorGUILayout.HelpBox(warningMessage, MessageType.Warning);

                            Rect rect = EditorGUILayout.GetControlRect();
                            rect = EditorGUI.IndentedRect(rect);

                            if (showFitButton)
                            {
                                if (GUI.Button(rect, "Set resolution to the maximum that fits"))
                                {
                                    var scalableSetting = ScalableSettings.ShadowResolution(lightType, hdrp);
                                    int res = additionalLightData.GetResolutionFromSettings(lightType, hdrp.currentPlatformRenderPipelineSettings.hdShadowInitParams);
                                    int foundResFit = -1;
                                    // Round up to multiple of slotSize
                                    res = HDUtils.DivRoundUp(res, slotSize) * slotSize;
                                    for (int testRes = res; testRes >= slotSize; testRes -= slotSize)
                                    {
                                        if (HDCachedShadowManager.instance.WouldFitInAtlas(Mathf.Max(testRes, slotSize), lightType))
                                        {
                                            foundResFit = Mathf.Max(testRes, slotSize);
                                            break;
                                        }
                                    }
                                    if (foundResFit > 0)
                                    {
                                        serialized.shadowResolution.useOverride.boolValue = true;
                                        serialized.shadowResolution.@override.intValue = foundResFit;
                                    }
                                    else
                                    {
                                        // Should never reach this point.
                                        Debug.LogWarning("The atlas is completely full.");
                                    }
                                }
                            }
                        }
                    }

#if UNITY_2021_1_OR_NEWER
                    EditorGUILayout.PropertyField(serialized.shadowAlwaysDrawDynamic, s_Styles.shadowAlwaysDrawDynamic);
#endif
                }


                if (serialized.shadowUpdateMode.intValue > 0)
                {
                    EditorGUILayout.PropertyField(serialized.shadowUpdateUponTransformChange, s_Styles.shadowUpdateOnLightTransformChange);
                }

                EditorGUI.indentLevel--;

                using (var change = new EditorGUI.ChangeCheckScope())
                {
                    var hasEditorLightShapeMultipleValues = lightType == (HDLightType)(-1);
                    if (hasEditorLightShapeMultipleValues)
                    {
                        serialized.shadowResolution.LevelAndIntGUILayout(
                            s_Styles.shadowResolution, null, null
                        );
                    }
                    else
                    {
                        var scalableSetting = ScalableSettings.ShadowResolution(lightType, hdrp);

                        serialized.shadowResolution.LevelAndIntGUILayout(
                            s_Styles.shadowResolution, scalableSetting, hdrp.name
                        );
                    }

                    if (change.changed)
                        serialized.shadowResolution.@override.intValue = Mathf.Max(HDShadowManager.k_MinShadowMapResolution, serialized.shadowResolution.@override.intValue);
                }

                if (lightType != HDLightType.Directional)
                    EditorGUILayout.Slider(serialized.shadowNearPlane, HDShadowUtils.k_MinShadowNearPlane, HDShadowUtils.k_MaxShadowNearPlane, s_Styles.shadowNearPlane);

                if (serialized.settings.isMixed)
                {
                    bool enabled = HDRenderPipeline.currentAsset.currentPlatformRenderPipelineSettings.supportShadowMask;
                    if (Lightmapping.TryGetLightingSettings(out var settings))
                        enabled &= settings.mixedBakeMode == MixedLightingMode.Shadowmask;
                    using (new EditorGUI.DisabledScope(!enabled))
                    {
                        Rect nonLightmappedOnlyRect = EditorGUILayout.GetControlRect();
                        EditorGUI.BeginProperty(nonLightmappedOnlyRect, s_Styles.nonLightmappedOnly, serialized.nonLightmappedOnly);
                        {
                            EditorGUI.BeginChangeCheck();
                            ShadowmaskMode shadowmask = serialized.nonLightmappedOnly.boolValue ? ShadowmaskMode.Shadowmask : ShadowmaskMode.DistanceShadowmask;
                            shadowmask = (ShadowmaskMode)EditorGUI.EnumPopup(nonLightmappedOnlyRect, s_Styles.nonLightmappedOnly, shadowmask);
                            if (EditorGUI.EndChangeCheck())
                            {
                                Undo.RecordObjects(owner.targets, "Light Update Shadowmask Mode");
                                serialized.nonLightmappedOnly.boolValue = shadowmask == ShadowmaskMode.Shadowmask;
                                foreach (Light target in owner.targets)
                                    target.lightShadowCasterMode = shadowmask == ShadowmaskMode.Shadowmask ? LightShadowCasterMode.NonLightmappedOnly : LightShadowCasterMode.Everything;
                            }
                        }
                        EditorGUI.EndProperty();
                    }
                }

                if (lightType == HDLightType.Area && serialized.areaLightShape == AreaLightShape.Rectangle)
                {
                    EditorGUILayout.Slider(serialized.areaLightShadowCone, HDAdditionalLightData.k_MinAreaLightShadowCone, HDAdditionalLightData.k_MaxAreaLightShadowCone, s_Styles.areaLightShadowCone);
                }

                if (HDRenderPipeline.assetSupportsRayTracing && HDRenderPipeline.pipelineSupportsScreenSpaceShadows)
                {
                    bool isPunctual = lightType == HDLightType.Point || (lightType == HDLightType.Spot && serialized.spotLightShape.GetEnumValue<SpotLightShape>() == SpotLightShape.Cone);
                    if (isPunctual || (lightType == HDLightType.Area && serialized.areaLightShape == AreaLightShape.Rectangle))
                    {
                        EditorGUILayout.PropertyField(serialized.useRayTracedShadows, s_Styles.useRayTracedShadows);
                        if (serialized.useRayTracedShadows.boolValue)
                        {
                            if (hdrp != null && lightType == HDLightType.Area && serialized.areaLightShape == AreaLightShape.Rectangle
                                && (hdrp.currentPlatformRenderPipelineSettings.supportedLitShaderMode != RenderPipelineSettings.SupportedLitShaderMode.DeferredOnly))
                                EditorGUILayout.HelpBox("Ray traced area light shadows are approximated for the Lit shader when not in deferred mode.", MessageType.Warning);

                            EditorGUI.indentLevel++;

                            // We only support semi transparent shadows for punctual lights
                            if (isPunctual)
                                EditorGUILayout.PropertyField(serialized.semiTransparentShadow, s_Styles.semiTransparentShadow);

                            EditorGUILayout.PropertyField(serialized.numRayTracingSamples, s_Styles.numRayTracingSamples);
                            EditorGUILayout.PropertyField(serialized.filterTracedShadow, s_Styles.denoiseTracedShadow);
                            EditorGUI.indentLevel++;
                            EditorGUILayout.PropertyField(serialized.filterSizeTraced, s_Styles.denoiserRadius);
                            // We only support distance based filtering if we have a punctual light source (point or spot)
                            if (isPunctual)
                                EditorGUILayout.PropertyField(serialized.distanceBasedFiltering, s_Styles.distanceBasedFiltering);
                            EditorGUI.indentLevel--;
                            EditorGUI.indentLevel--;
                        }
                    }
                }

                // For the moment, we only support screen space rasterized shadows for directional lights
                if (lightType == HDLightType.Directional && HDRenderPipeline.pipelineSupportsScreenSpaceShadows)
                {
                    EditorGUILayout.PropertyField(serialized.useScreenSpaceShadows, s_Styles.useScreenSpaceShadows);
                    if (HDRenderPipeline.assetSupportsRayTracing)
                    {
                        using (new EditorGUI.DisabledScope(!serialized.useScreenSpaceShadows.boolValue))
                        {
                            EditorGUI.indentLevel++;
                            EditorGUILayout.PropertyField(serialized.useRayTracedShadows, s_Styles.useRayTracedShadows);
                            using (new EditorGUI.DisabledScope(!serialized.useRayTracedShadows.boolValue))
                            {
                                EditorGUI.indentLevel++;
                                EditorGUILayout.PropertyField(serialized.numRayTracingSamples, s_Styles.numRayTracingSamples);
                                EditorGUILayout.PropertyField(serialized.colorShadow, s_Styles.colorShadow);
                                EditorGUILayout.PropertyField(serialized.filterTracedShadow, s_Styles.denoiseTracedShadow);
                                using (new EditorGUI.DisabledScope(!serialized.filterTracedShadow.boolValue))
                                {
                                    EditorGUI.indentLevel++;
                                    EditorGUILayout.PropertyField(serialized.filterSizeTraced, s_Styles.denoiserRadius);
                                    EditorGUI.indentLevel--;
                                }
                                EditorGUI.indentLevel--;
                            }
                            EditorGUI.indentLevel--;
                        }
                    }
                }
            }
        }

        static void DrawShadowMapAdditionalContent(SerializedHDLight serialized, Editor owner)
        {
            using (new EditorGUI.DisabledScope(serialized.settings.shadowsType.GetEnumValue<LightShadows>() == LightShadows.None))
            {
                HDLightType lightType = serialized.type;

                if (lightType == HDLightType.Area && serialized.areaLightShape == AreaLightShape.Rectangle)
                {
                    EditorGUILayout.Slider(serialized.evsmExponent, HDAdditionalLightData.k_MinEvsmExponent, HDAdditionalLightData.k_MaxEvsmExponent, s_Styles.evsmExponent);
                    EditorGUILayout.Slider(serialized.evsmLightLeakBias, HDAdditionalLightData.k_MinEvsmLightLeakBias, HDAdditionalLightData.k_MaxEvsmLightLeakBias, s_Styles.evsmLightLeakBias);
                    EditorGUILayout.Slider(serialized.evsmVarianceBias, HDAdditionalLightData.k_MinEvsmVarianceBias, HDAdditionalLightData.k_MaxEvsmVarianceBias, s_Styles.evsmVarianceBias);
                    EditorGUILayout.IntSlider(serialized.evsmBlurPasses, HDAdditionalLightData.k_MinEvsmBlurPasses, HDAdditionalLightData.k_MaxEvsmBlurPasses, s_Styles.evsmAdditionalBlurPasses);
                }
                else
                {
                    EditorGUILayout.Slider(serialized.slopeBias, 0.0f, 1.0f, s_Styles.slopeBias);
                    EditorGUILayout.Slider(serialized.normalBias, 0.0f, 5.0f, s_Styles.normalBias);

                    if (lightType == HDLightType.Spot
                        && serialized.spotLightShape.GetEnumValue<SpotLightShape>() != SpotLightShape.Box)
                    {
                        EditorGUILayout.PropertyField(serialized.useCustomSpotLightShadowCone, s_Styles.useCustomSpotLightShadowCone);
                        if (serialized.useCustomSpotLightShadowCone.boolValue)
                        {
                            EditorGUILayout.Slider(serialized.customSpotLightShadowCone, 1.0f, serialized.settings.spotAngle.floatValue, s_Styles.customSpotLightShadowCone);
                        }
                    }
                }

                // Dimmer and Tint don't have effect on baked shadow
                if (!serialized.settings.isCompletelyBaked)
                {
                    EditorGUILayout.Slider(serialized.shadowDimmer, 0.0f, 1.0f, s_Styles.shadowDimmer);
                    EditorGUILayout.PropertyField(serialized.shadowTint, s_Styles.shadowTint);
                    EditorGUILayout.PropertyField(serialized.penumbraTint, s_Styles.penumbraTint);
                }

                if (lightType != HDLightType.Directional)
                {
                    EditorGUILayout.PropertyField(serialized.shadowFadeDistance, s_Styles.shadowFadeDistance);
                }

                // Shadow Layers
                if (HDUtils.hdrpSettings.supportLightLayers)
                {
                    using (var change = new EditorGUI.ChangeCheckScope())
                    {
                        Rect lineRect = EditorGUILayout.GetControlRect();
                        EditorGUI.BeginProperty(lineRect, s_Styles.unlinkLightAndShadowLayersText, serialized.linkShadowLayers);
                        bool savedHasMultipleDifferentValue = EditorGUI.showMixedValue;
                        EditorGUI.showMixedValue = serialized.linkShadowLayers.hasMultipleDifferentValues;
                        bool newValue = !EditorGUI.Toggle(lineRect, s_Styles.unlinkLightAndShadowLayersText, !serialized.linkShadowLayers.boolValue);
                        EditorGUI.showMixedValue = savedHasMultipleDifferentValue;
                        EditorGUI.EndProperty();

                        // Undo the changes in the light component because the SyncLightAndShadowLayers will change the value automatically when link is ticked
                        if (change.changed)
                        {
                            Undo.RecordObjects(owner.targets, "Undo Light Layers Changed");
                            serialized.linkShadowLayers.boolValue = newValue;
                            if (!newValue)
                            {
                                serialized.Apply(); //we need to push above modification the modification on object as it is used to sync
                                SyncLightAndShadowLayers(serialized, owner);
                            }
                        }
                    }
                    //
                    if (serialized.linkShadowLayers.hasMultipleDifferentValues || !serialized.linkShadowLayers.boolValue)
                    {
                        using (new EditorGUI.DisabledGroupScope(serialized.linkShadowLayers.hasMultipleDifferentValues))
                        {
                            ++EditorGUI.indentLevel;
                            HDEditorUtils.DrawLightLayerMaskFromInt(s_Styles.shadowLayerMaskText, serialized.settings.renderingLayerMask);
                            --EditorGUI.indentLevel;
                        }
                    }
                }
            }
        }

        static void SyncLightAndShadowLayers(SerializedHDLight serialized, Editor owner)
        {
            // If we're not in decoupled mode for light layers, we sync light with shadow layers.
            // In mixed state, it make sens to do it only on Light that links the mode.
            HDLightEditor editor = owner as HDLightEditor;
            for (int i = 0; i < owner.targets.Length; ++i)
            {
                HDAdditionalLightData additionalData = editor.GetAdditionalDataForTargetIndex(i);
                if (!additionalData.linkShadowLayers)
                    continue;

                Light target = owner.targets[i] as Light;
                if (target.renderingLayerMask != serialized.lightlayersMask.intValue)
                    target.renderingLayerMask = serialized.lightlayersMask.intValue;
            }
        }

        static void DrawContactShadowsContent(SerializedHDLight serialized, Editor owner)
        {
            var hdrp = HDRenderPipeline.currentAsset;
            SerializedScalableSettingValueUI.LevelAndToggleGUILayout(
                serialized.contactShadows,
                s_Styles.contactShadows,
                HDAdditionalLightData.ScalableSettings.UseContactShadow(hdrp),
                hdrp.name
            );
            if (HDRenderPipeline.assetSupportsRayTracing
                && serialized.contactShadows.@override.boolValue)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(serialized.rayTracedContactShadow, s_Styles.rayTracedContactShadow);
                EditorGUI.indentLevel--;
            }
        }

        static void DrawBakedShadowsContent(SerializedHDLight serialized, Editor owner)
        {
            DrawEnableShadowMap(serialized, owner);
            if (serialized.type != HDLightType.Directional)
                EditorGUILayout.Slider(serialized.shadowNearPlane, HDShadowUtils.k_MinShadowNearPlane, HDShadowUtils.k_MaxShadowNearPlane, s_Styles.shadowNearPlane);
        }

        static bool HasShadowQualitySettingsUI(HDShadowFilteringQuality quality, SerializedHDLight serialized, Editor owner)
        {
            // Handle quality where there is nothing to draw directly here
            // No PCSS for now with directional light
            if (quality == HDShadowFilteringQuality.Medium || quality == HDShadowFilteringQuality.Low)
                return false;

            // Draw shadow settings using the current shadow algorithm

            HDShadowInitParameters hdShadowInitParameters = HDRenderPipeline.currentAsset.currentPlatformRenderPipelineSettings.hdShadowInitParams;
            return hdShadowInitParameters.shadowFilteringQuality == quality;
        }

        static void DrawLowShadowSettingsContent(SerializedHDLight serialized, Editor owner)
        {
            // Currently there is nothing to display here
            // when adding something, update IsShadowSettings
        }

        static void DrawMediumShadowSettingsContent(SerializedHDLight serialized, Editor owner)
        {
            // Currently there is nothing to display here
            // when adding something, update IsShadowSettings
        }

        static void DrawHighShadowSettingsContent(SerializedHDLight serialized, Editor owner)
        {
            EditorGUILayout.PropertyField(serialized.blockerSampleCount, s_Styles.blockerSampleCount);
            EditorGUILayout.PropertyField(serialized.filterSampleCount, s_Styles.filterSampleCount);
            EditorGUILayout.PropertyField(serialized.minFilterSize, s_Styles.minFilterSize);
            GUIContent styleForScale = s_Styles.radiusScaleForSoftness;
            if (serialized.type == HDLightType.Directional)
            {
                styleForScale = s_Styles.diameterScaleForSoftness;
            }
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(serialized.scaleForSoftness, styleForScale);
            if (EditorGUI.EndChangeCheck())
            {
                //Clamp the value and also affect baked shadows
                serialized.scaleForSoftness.floatValue = Mathf.Max(serialized.scaleForSoftness.floatValue, 0);
            }
        }

        static void DrawVeryHighShadowSettingsContent(SerializedHDLight serialized, Editor owner)
        {
            // Same as high for now (PCSS)
            DrawHighShadowSettingsContent(serialized, owner);
        }

        static void SetLightsDirty(Editor owner)
        {
            foreach (Light light in owner.targets)
                light.SetLightDirty(); // Should be apply only to parameter that's affect GI, but make the code cleaner
        }
    }
}
