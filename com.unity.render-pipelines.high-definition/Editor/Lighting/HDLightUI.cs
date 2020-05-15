using System;
using System.Linq.Expressions;
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
            ShadowMask,
            DistanceShadowmask
        }

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

        enum AdvancedMode
        {
            General = 1 << 0,
            Shape = 1 << 1,
            Emission = 1 << 2,
            Shadow = 1 << 3,
        }

        const float k_MinLightSize = 0.01f; // Provide a small size of 1cm for line light

        readonly static ExpandedState<Expandable, Light> k_ExpandedState = new ExpandedState<Expandable, Light>(~(-1), "HDRP");

        public static readonly CED.IDrawer Inspector;

        static bool GetAdvanced(AdvancedMode mask, SerializedHDLight serialized, Editor owner)
        {
            return (serialized.showAdditionalSettings.intValue & (int)mask) != 0;
        }

        static void SetAdvanced(AdvancedMode mask, bool value, SerializedHDLight serialized, Editor owner)
        {
            if (value)
            {
                serialized.showAdditionalSettings.intValue |= (int)mask;
            }
            else
            {
                serialized.showAdditionalSettings.intValue &= ~(int)mask;
            }
        }

        static void SwitchAdvanced(AdvancedMode mask, SerializedHDLight serialized, Editor owner)
        {
            if ((serialized.showAdditionalSettings.intValue & (int)mask) != 0)
            {
                serialized.showAdditionalSettings.intValue &= ~(int)mask;
            }
            else
            {
                serialized.showAdditionalSettings.intValue |= (int)mask;
            }
        }

        static Action<GUIContent, SerializedProperty, LightEditor.Settings> SliderWithTexture;

        static HDLightUI()
        {
            Inspector = CED.Group(
                CED.AdvancedFoldoutGroup(s_Styles.generalHeader, Expandable.General, k_ExpandedState,
                    (serialized, owner) => GetAdvanced(AdvancedMode.General, serialized, owner),
                    (serialized, owner) => SwitchAdvanced(AdvancedMode.General, serialized, owner),
                    DrawGeneralContent,
                    DrawGeneralAdvancedContent
                    ),
                CED.FoldoutGroup(s_Styles.shapeHeader, Expandable.Shape, k_ExpandedState, DrawShapeContent),
                CED.Conditional((serialized, owner) => serialized.type == HDLightType.Directional && !serialized.settings.isCompletelyBaked,
                    CED.FoldoutGroup(s_Styles.celestialBodyHeader, Expandable.CelestialBody, k_ExpandedState, DrawCelestialBodyContent)),
                CED.AdvancedFoldoutGroup(s_Styles.emissionHeader, Expandable.Emission, k_ExpandedState,
                    (serialized, owner) => GetAdvanced(AdvancedMode.Emission, serialized, owner),
                    (serialized, owner) => SwitchAdvanced(AdvancedMode.Emission, serialized, owner),
                    DrawEmissionContent,
                    DrawEmissionAdvancedContent
                    ),
                CED.Conditional((serialized, owner) => serialized.type != HDLightType.Area && !serialized.settings.isCompletelyBaked,
                    CED.FoldoutGroup(s_Styles.volumetricHeader, Expandable.Volumetric, k_ExpandedState, DrawVolumetric)),
                CED.Conditional((serialized, owner) =>
                    {
                        HDLightType type = serialized.type;
                        return type != HDLightType.Area || type == HDLightType.Area && serialized.areaLightShape != AreaLightShape.Tube;
                    },
                    CED.TernaryConditional((serialized, owner) => !serialized.settings.isCompletelyBaked,
                        CED.AdvancedFoldoutGroup(s_Styles.shadowHeader, Expandable.Shadows, k_ExpandedState,
                            (serialized, owner) => GetAdvanced(AdvancedMode.Shadow, serialized, owner),
                            (serialized, owner) => SwitchAdvanced(AdvancedMode.Shadow, serialized, owner),
                            CED.Group(
                                CED.FoldoutGroup(s_Styles.shadowMapSubHeader, Expandable.ShadowMap, k_ExpandedState, FoldoutOption.SubFoldout | FoldoutOption.Indent | FoldoutOption.NoSpaceAtEnd, DrawShadowMapContent),
                                CED.Conditional((serialized, owner) => GetAdvanced(AdvancedMode.Shadow, serialized, owner) && k_ExpandedState[Expandable.ShadowMap],
                                    CED.Group(GroupOption.Indent, DrawShadowMapAdvancedContent)),
                                CED.space,
                                CED.Conditional((serialized, owner) => GetAdvanced(AdvancedMode.Shadow, serialized, owner) && HasShadowQualitySettingsUI(HDShadowFilteringQuality.High, serialized, owner),
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
                        CED.FoldoutGroup(s_Styles.shadowHeader, Expandable.Shadows, k_ExpandedState,
                            CED.FoldoutGroup(s_Styles.bakedShadowsSubHeader, Expandable.BakedShadow, k_ExpandedState, FoldoutOption.SubFoldout | FoldoutOption.Indent | FoldoutOption.NoSpaceAtEnd, DrawBakedShadowsContent))
                    )
                )
            );

            //quicker than standard reflection as it is compiled
            var paramLabel = Expression.Parameter(typeof(GUIContent), "label");
            var paramProperty = Expression.Parameter(typeof(SerializedProperty), "property");
            var paramSettings = Expression.Parameter(typeof(LightEditor.Settings), "settings");
            System.Reflection.MethodInfo sliderWithTextureInfo = typeof(EditorGUILayout)
                .GetMethod(
                    "SliderWithTexture",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static,
                    null,
                    System.Reflection.CallingConventions.Any,
                    new[] { typeof(GUIContent), typeof(SerializedProperty), typeof(float), typeof(float), typeof(float), typeof(Texture2D), typeof(GUILayoutOption[]) },
                    null);
            var sliderWithTextureCall = Expression.Call(
                sliderWithTextureInfo,
                paramLabel,
                paramProperty,
                Expression.Constant((float)typeof(LightEditor.Settings).GetField("kMinKelvin", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic).GetRawConstantValue()),
                Expression.Constant((float)typeof(LightEditor.Settings).GetField("kMaxKelvin", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic).GetRawConstantValue()),
                Expression.Constant((float)typeof(LightEditor.Settings).GetField("kSliderPower", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic).GetRawConstantValue()),
                Expression.Field(paramSettings, typeof(LightEditor.Settings).GetField("m_KelvinGradientTexture", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)),
                Expression.Constant(null, typeof(GUILayoutOption[])));
            var lambda = Expression.Lambda<Action<GUIContent, SerializedProperty, LightEditor.Settings>>(sliderWithTextureCall, paramLabel, paramProperty, paramSettings);
            SliderWithTexture = lambda.Compile();
        }

        static void DrawGeneralContent(SerializedHDLight serialized, Editor owner)
        {
            EditorGUI.BeginChangeCheck();
            Rect lineRect = EditorGUILayout.GetControlRect();
            HDLightType lightType = serialized.type;
            HDLightType updatedLightType;

            //Partial support for prefab. There is no way to fully support it at the moment.
            //Missing support on the Apply and Revert contextual menu on Label for Prefab overrides. They need to be done two times.
            //(This will continue unless we remove AdditionalDatas)
            using (new SerializedHDLight.LightTypeEditionScope(lineRect, s_Styles.shape, serialized))
            {
                EditorGUI.showMixedValue = lightType == (HDLightType)(-1);
                int index = Array.FindIndex((HDLightType[])Enum.GetValues(typeof(HDLightType)), x => x == lightType);
                updatedLightType = (HDLightType)EditorGUI.Popup(lineRect, s_Styles.shape, index, s_Styles.shapeNames);
            }

            if (EditorGUI.EndChangeCheck())
            {
                serialized.type = updatedLightType; //also register undo

                if (updatedLightType == HDLightType.Area)
                {
                    switch (serialized.areaLightShape)
                    {
                        case AreaLightShape.Rectangle:
                            serialized.shapeWidth.floatValue = Mathf.Max(serialized.shapeWidth.floatValue, k_MinLightSize);
                            serialized.shapeHeight.floatValue = Mathf.Max(serialized.shapeHeight.floatValue, k_MinLightSize);
                            break;
                        case AreaLightShape.Tube:
                            serialized.settings.shadowsType.SetEnumValue(LightShadows.None);
                            serialized.shapeWidth.floatValue = Mathf.Max(serialized.shapeWidth.floatValue, k_MinLightSize);
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

            //Draw the mode
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
                        break;
                }
            }
        }

        static void DrawGeneralAdvancedContent(SerializedHDLight serialized, Editor owner)
        {
            using (new EditorGUI.DisabledScope(!HDUtils.hdrpSettings.supportLightLayers))
            {
                using (var change = new EditorGUI.ChangeCheckScope())
                {
                    EditorGUILayout.PropertyField(serialized.lightlayersMask, s_Styles.lightLayer);

                    // If we're not in decoupled mode for light layers, we sync light with shadow layers:
                    if (serialized.linkLightLayers.boolValue && change.changed)
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
                    if(EditorGUI.EndChangeCheck())
                    {
                        //Also affect baked shadows
                        serialized.settings.bakedShadowRadiusProp.floatValue = serialized.shapeRadius.floatValue;
                    }
                    break;

                case HDLightType.Spot:
                    EditorGUI.BeginChangeCheck();
                    EditorGUILayout.PropertyField(serialized.spotLightShape, s_Styles.spotLightShape);
                    if (EditorGUI.EndChangeCheck())
                    {
                        UpdateLightIntensityUnit(serialized, owner);
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
                            if(EditorGUI.EndChangeCheck())
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
                                serialized.settings.areaSizeY.floatValue = k_MinLightSize;
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
                // Light size must be non-zero, else we get NaNs.
                serialized.shapeWidth.floatValue = Mathf.Max(serialized.shapeWidth.floatValue, k_MinLightSize);
                serialized.shapeHeight.floatValue = Mathf.Max(serialized.shapeHeight.floatValue, k_MinLightSize);
                serialized.shapeRadius.floatValue = Mathf.Max(serialized.shapeRadius.floatValue, 0.0f);
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
                    EditorGUILayout.PropertyField(serialized.flareSize,      s_Styles.flareSize);
                    EditorGUILayout.PropertyField(serialized.flareFalloff,   s_Styles.flareFalloff);
                    EditorGUILayout.PropertyField(serialized.flareTint,      s_Styles.flareTint);
                    EditorGUILayout.PropertyField(serialized.surfaceTexture, s_Styles.surfaceTexture);
                    EditorGUILayout.PropertyField(serialized.surfaceTint,    s_Styles.surfaceTint);
                    EditorGUILayout.PropertyField(serialized.distance,       s_Styles.distance);
                    EditorGUI.indentLevel--;
                }
            }

            if (EditorGUI.EndChangeCheck())
            {
                // Clamp the value and also affect baked shadows.
                serialized.flareSize.floatValue                     = Mathf.Clamp(serialized.flareSize.floatValue, 0, 90);
                serialized.flareFalloff.floatValue                  = Mathf.Max(serialized.flareFalloff.floatValue, 0);
                serialized.distance.floatValue                      = Mathf.Max(serialized.distance.floatValue, 0);
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

        static void ConvertLightIntensity(LightUnit oldLightUnit, LightUnit newLightUnit, SerializedHDLight serialized, Editor owner)
        {
            float intensity = serialized.intensity.floatValue;
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

            serialized.intensity.floatValue = intensity;
        }

        static void DrawLightIntensityGUILayout(SerializedHDLight serialized, Editor owner)
        {
            // Match const defined in EditorGUI.cs
            const int k_IndentPerLevel = 15;

            const int k_ValueUnitSeparator = 2;
            const int k_UnitWidth = 100;

            float indent = k_IndentPerLevel * EditorGUI.indentLevel;

            Rect lineRect = EditorGUILayout.GetControlRect();
            Rect valueRect = lineRect;
            Rect labelRect = lineRect;
            labelRect.width = EditorGUIUtility.labelWidth;

            // We use PropertyField to draw the value to keep the handle at left of the field
            // This will apply the indent again thus we need to remove it time for alignment
            valueRect.width += indent - k_ValueUnitSeparator - k_UnitWidth;
            Rect unitRect = valueRect;
            unitRect.x += valueRect.width - indent + k_ValueUnitSeparator;
            unitRect.width = k_UnitWidth + .5f;

            //handling of prefab overrides in a parent label
            GUIContent parentLabel = s_Styles.lightIntensity;
            parentLabel = EditorGUI.BeginProperty(labelRect, parentLabel, serialized.intensity);
            parentLabel = EditorGUI.BeginProperty(labelRect, parentLabel, serialized.lightUnit);
            {
                EditorGUI.LabelField(labelRect, parentLabel);
            }
            EditorGUI.EndProperty();
            EditorGUI.EndProperty();
            
            EditorGUI.PropertyField(valueRect, serialized.intensity, s_Styles.empty);
            DrawLightIntensityUnitPopup(unitRect, serialized, owner);

            if (EditorGUI.EndChangeCheck())
            {
                serialized.intensity.floatValue = Mathf.Max(serialized.intensity.floatValue, 0.0f);
            }
        }

        static void DrawEmissionContent(SerializedHDLight serialized, Editor owner)
        {
            using (var changes = new EditorGUI.ChangeCheckScope())
            {
                if (GraphicsSettings.lightsUseLinearIntensity && GraphicsSettings.lightsUseColorTemperature)
                {
                    EditorGUILayout.PropertyField(serialized.settings.useColorTemperature, s_Styles.useColorTemperature);
                    if (serialized.settings.useColorTemperature.boolValue)
                    {
                        EditorGUI.indentLevel += 1;
                        EditorGUILayout.PropertyField(serialized.settings.color, s_Styles.colorFilter);
                        SliderWithTexture(s_Styles.colorTemperature, serialized.settings.colorTemperature, serialized.settings);
                        EditorGUI.indentLevel -= 1;
                    }
                    else
                        EditorGUILayout.PropertyField(serialized.settings.color, s_Styles.color);
                }
                else
                    EditorGUILayout.PropertyField(serialized.settings.color, s_Styles.color);

                if (changes.changed && HDRenderPipelinePreferences.lightColorNormalization)
                    serialized.settings.color.colorValue = HDUtils.NormalizeColor(serialized.settings.color.colorValue);
            }

            EditorGUI.BeginChangeCheck();

            DrawLightIntensityGUILayout(serialized, owner);

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
                // Display reflector only in advance mode
                && (lightUnit == (int)PunctualLightUnit.Lumen && GetAdvanced(AdvancedMode.Emission, serialized, owner)))
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

                // When directional light use a cookie, it can control the size
                if (serialized.settings.cookie != null && lightType == HDLightType.Directional)
                {
                    EditorGUI.indentLevel++;
                    EditorGUILayout.PropertyField(serialized.shapeWidth, s_Styles.cookieSizeX);
                    EditorGUILayout.PropertyField(serialized.shapeHeight, s_Styles.cookieSizeY);
                    EditorGUI.indentLevel--;
                }

                ShowCookieTextureWarnings(serialized.settings.cookie);
            }
            else if (serialized.areaLightShape == AreaLightShape.Rectangle)
            {
                EditorGUILayout.ObjectField( serialized.areaLightCookie, s_Styles.areaLightCookie );
                ShowCookieTextureWarnings(serialized.areaLightCookie.objectReferenceValue as Texture);
            }

            if (EditorGUI.EndChangeCheck())
            {
                serialized.needUpdateAreaLightEmissiveMeshComponents = true;
                SetLightsDirty(owner); // Should be apply only to parameter that's affect GI, but make the code cleaner
            }
        }

        static void ShowCookieTextureWarnings(Texture cookie)
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
                        GUIStyle wordWrap = new GUIStyle(EditorStyles.miniLabel){ wordWrap = true};
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

            if (cookie.width != cookie.height)
                EditorGUILayout.HelpBox(s_Styles.cookieNonPOT, MessageType.Warning);
            if (cookie.width < LightCookieManager.k_MinCookieSize || cookie.height < LightCookieManager.k_MinCookieSize)
                EditorGUILayout.HelpBox(s_Styles.cookieTooSmall, MessageType.Warning);
        }
        
        static void DrawEmissionAdvancedContent(SerializedHDLight serialized, Editor owner)
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
                    if (serialized.spotLightShape.GetEnumValue<SpotLightShape>() != SpotLightShape.Box)
                        EditorGUILayout.PropertyField(serialized.applyRangeAttenuation, s_Styles.applyRangeAttenuation);
                    EditorGUILayout.PropertyField(serialized.fadeDistance, s_Styles.fadeDistance);
                }
                EditorGUILayout.PropertyField(serialized.lightDimmer, s_Styles.lightDimmer);
            }
            else if (lightType == HDLightType.Point
                    || lightType == HDLightType.Spot && serialized.spotLightShape.GetEnumValue<SpotLightShape>() != SpotLightShape.Box)
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
            {
                EditorGUILayout.PropertyField(serialized.volumetricDimmer, s_Styles.volumetricDimmer);
                EditorGUILayout.Slider(serialized.volumetricShadowDimmer, 0.0f, 1.0f, s_Styles.volumetricShadowDimmer);
            }
        }

        static bool DrawEnableShadowMap(SerializedHDLight serialized, Editor owne)
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

        static void DrawShadowMapContent(SerializedHDLight serialized, Editor owner)
        {
            var hdrp = HDRenderPipeline.currentAsset;
            bool newShadowsEnabled = DrawEnableShadowMap(serialized, owner);

            using (new EditorGUI.DisabledScope(!newShadowsEnabled))
            {
                EditorGUILayout.PropertyField(serialized.shadowUpdateMode, s_Styles.shadowUpdateMode);

                HDLightType lightType = serialized.type;

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

                if(lightType != HDLightType.Directional)
                    EditorGUILayout.Slider(serialized.shadowNearPlane, HDShadowUtils.k_MinShadowNearPlane, HDShadowUtils.k_MaxShadowNearPlane, s_Styles.shadowNearPlane);

                if (serialized.settings.isMixed)
                {
                    using (new EditorGUI.DisabledScope(!HDRenderPipeline.currentAsset.currentPlatformRenderPipelineSettings.supportShadowMask))
                    {
                        Rect nonLightmappedOnlyRect = EditorGUILayout.GetControlRect();
                        EditorGUI.BeginProperty(nonLightmappedOnlyRect, s_Styles.nonLightmappedOnly, serialized.nonLightmappedOnly);
                        {
                            EditorGUI.BeginChangeCheck();
                            ShadowmaskMode shadowmask = serialized.nonLightmappedOnly.boolValue ? ShadowmaskMode.ShadowMask : ShadowmaskMode.DistanceShadowmask;
                            shadowmask = (ShadowmaskMode)EditorGUI.EnumPopup(nonLightmappedOnlyRect, s_Styles.nonLightmappedOnly, shadowmask);
                            if (EditorGUI.EndChangeCheck())
                            {
                                Undo.RecordObjects(owner.targets, "Light Update Shadowmask Mode");
                                serialized.nonLightmappedOnly.boolValue = shadowmask == ShadowmaskMode.ShadowMask;
                                foreach (Light target in owner.targets)
                                    target.lightShadowCasterMode = shadowmask == ShadowmaskMode.ShadowMask ? LightShadowCasterMode.NonLightmappedOnly : LightShadowCasterMode.Everything;
                            }
                        }
                        EditorGUI.EndProperty();
                    }
                }

                if (lightType == HDLightType.Area && serialized.areaLightShape == AreaLightShape.Rectangle)
                {
                    EditorGUILayout.Slider(serialized.areaLightShadowCone, HDAdditionalLightData.k_MinAreaLightShadowCone, HDAdditionalLightData.k_MaxAreaLightShadowCone, s_Styles.areaLightShadowCone);
                }

                if (HDRenderPipeline.pipelineSupportsRayTracing)
                {
                    bool isPunctual = lightType == HDLightType.Point || (lightType == HDLightType.Spot && serialized.spotLightShape.GetEnumValue<SpotLightShape>() == SpotLightShape.Cone);
                    if (isPunctual || (lightType == HDLightType.Area && serialized.areaLightShape == AreaLightShape.Rectangle))
                    {
                        EditorGUILayout.PropertyField(serialized.useRayTracedShadows, s_Styles.useRayTracedShadows);
                        if(serialized.useRayTracedShadows.boolValue)
                        {
                            if (hdrp != null && lightType == HDLightType.Area && serialized.areaLightShape == AreaLightShape.Rectangle
                                && (hdrp.currentPlatformRenderPipelineSettings.supportedLitShaderMode != RenderPipelineSettings.SupportedLitShaderMode.DeferredOnly))
                                EditorGUILayout.HelpBox("Ray traced area light shadows are only available in deferred mode.", MessageType.Warning);

                            EditorGUI.indentLevel++;

                            // We only support semi transparent shadows for punctual lights
                            if (isPunctual)
                                EditorGUILayout.PropertyField(serialized.semiTransparentShadow, s_Styles.semiTransparentShadow);

                            EditorGUILayout.PropertyField(serialized.numRayTracingSamples, s_Styles.numRayTracingSamples);
                            EditorGUILayout.PropertyField(serialized.filterTracedShadow, s_Styles.denoiseTracedShadow);
                            EditorGUILayout.PropertyField(serialized.filterSizeTraced, s_Styles.denoiserRadius);
                            EditorGUI.indentLevel--;
                        }
                    }

                    // For the moment, we only support screen space rasterized shadows for directional lights
                    if (lightType == HDLightType.Directional)
                    {
                        EditorGUILayout.PropertyField(serialized.useScreenSpaceShadows, s_Styles.useScreenSpaceShadows);
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


        static void DrawShadowMapAdvancedContent(SerializedHDLight serialized, Editor owner)
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
                using (new EditorGUI.DisabledScope(!HDUtils.hdrpSettings.supportLightLayers))
                {
                    using (var change = new EditorGUI.ChangeCheckScope())
                    {
                        EditorGUILayout.PropertyField(serialized.linkLightLayers, s_Styles.linkLightAndShadowLayersText);

                        // Undo the changes in the light component because the SyncLightAndShadowLayers will change the value automatically when link is ticked
                        if (change.changed)
                            Undo.RecordObjects(owner.targets, "Undo Light Layers Changed");
                    }
                    if (!serialized.linkLightLayers.hasMultipleDifferentValues)
                    {
                        using (new EditorGUI.DisabledGroupScope(serialized.linkLightLayers.boolValue))
                        {
                            HDEditorUtils.DrawLightLayerMaskFromInt(s_Styles.shadowLayerMaskText, serialized.settings.renderingLayerMask);
                        }
                        if (serialized.linkLightLayers.boolValue)
                            SyncLightAndShadowLayers(serialized, owner);
                    }
                }
            }
        }

        static void SyncLightAndShadowLayers(SerializedHDLight serialized, Editor owner)
        {
            // If we're not in decoupled mode for light layers, we sync light with shadow layers:
            foreach (Light target in owner.targets)
                if (target.renderingLayerMask != serialized.lightlayersMask.intValue)
                    target.renderingLayerMask = serialized.lightlayersMask.intValue;
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
            if (HDRenderPipeline.pipelineSupportsRayTracing
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
            EditorGUILayout.PropertyField(serialized.kernelSize, s_Styles.kernelSize);
            EditorGUILayout.PropertyField(serialized.lightAngle, s_Styles.lightAngle);
            EditorGUILayout.PropertyField(serialized.maxDepthBias, s_Styles.maxDepthBias);
        }

        static void SetLightsDirty(Editor owner)
        {
            foreach (Light light in owner.targets)
                light.SetLightDirty(); // Should be apply only to parameter that's affect GI, but make the code cleaner
        }
    }
}
