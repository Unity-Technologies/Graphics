using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
#if XR_MANAGEMENT_4_0_1_OR_NEWER
using UnityEditor.XR.Management;
#endif

namespace UnityEditor.Rendering.Universal
{
    using CED = CoreEditorDrawer<UniversalRenderPipelineSerializedLight>;

    internal partial class UniversalRenderPipelineLightUI
    {
        [URPHelpURL("light-component")]
        enum Expandable
        {
            General = 1 << 0,
            Shape = 1 << 1,
            Emission = 1 << 2,
            Rendering = 1 << 3,
            Shadows = 1 << 4,
            LightCookie = 1 << 5
        }

        static readonly ExpandedState<Expandable, Light> k_ExpandedState = new(~-1, "URP");

        public static readonly CED.IDrawer Inspector = CED.Group(
            CED.Conditional(
                (_, __) =>
                {
                    if (SceneView.lastActiveSceneView == null)
                        return false;

#if UNITY_2019_1_OR_NEWER
                    var sceneLighting = SceneView.lastActiveSceneView.sceneLighting;
#else
                    var sceneLighting = SceneView.lastActiveSceneView.m_SceneLighting;
#endif
                    return !sceneLighting;
                },
                (_, __) => EditorGUILayout.HelpBox(Styles.DisabledLightWarning.text, MessageType.Warning)),
            CED.FoldoutGroup(LightUI.Styles.generalHeader,
                Expandable.General,
                k_ExpandedState,
                DrawGeneralContent),
            CED.Conditional(
                (serializedLight, editor) => !serializedLight.settings.lightType.hasMultipleDifferentValues && serializedLight.settings.light.type == LightType.Spot,
                CED.FoldoutGroup(LightUI.Styles.shapeHeader, Expandable.Shape, k_ExpandedState, DrawSpotShapeContent)),
            CED.Conditional(
                (serializedLight, editor) =>
                {
                    if (serializedLight.settings.lightType.hasMultipleDifferentValues)
                        return false;
                    var lightType = serializedLight.settings.light.type;
                    return lightType == LightType.Rectangle || lightType == LightType.Disc;
                },
                CED.FoldoutGroup(LightUI.Styles.shapeHeader, Expandable.Shape, k_ExpandedState, DrawAreaShapeContent)),
            CED.FoldoutGroup(LightUI.Styles.emissionHeader,
                Expandable.Emission,
                k_ExpandedState,
                CED.Group(
                    LightUI.DrawColor,
                    DrawEmissionContent)),
            CED.FoldoutGroup(LightUI.Styles.renderingHeader,
                Expandable.Rendering,
                k_ExpandedState,
                DrawRenderingContent),
            CED.FoldoutGroup(LightUI.Styles.shadowHeader,
                Expandable.Shadows,
                k_ExpandedState,
                DrawShadowsContent)
        );

        static Func<int> s_SetGizmosDirty = SetGizmosDirty();
        static Func<int> SetGizmosDirty()
        {
            var type = Type.GetType("UnityEditor.AnnotationUtility,UnityEditor");
            var method = type.GetMethod("SetGizmosDirty", BindingFlags.Static | BindingFlags.NonPublic);
            var lambda = Expression.Lambda<Func<int>>(Expression.Call(method));
            return lambda.Compile();
        }

        static Action<GUIContent, SerializedProperty, LightEditor.Settings> k_SliderWithTexture = GetSliderWithTexture();
        static Action<GUIContent, SerializedProperty, LightEditor.Settings> GetSliderWithTexture()
        {
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
            var lambda = Expression.Lambda<System.Action<GUIContent, SerializedProperty, LightEditor.Settings>>(sliderWithTextureCall, paramLabel, paramProperty, paramSettings);
            return lambda.Compile();
        }

        static void DrawGeneralContent(UniversalRenderPipelineSerializedLight serializedLight, Editor owner)
        {
            DrawGeneralContentInternal(serializedLight, owner, isInPreset: false);
        }

        static void DrawGeneralContentPreset(UniversalRenderPipelineSerializedLight serializedLight, Editor owner)
        {
            DrawGeneralContentInternal(serializedLight, owner, isInPreset: true);
        }

        static void DrawGeneralContentInternal(UniversalRenderPipelineSerializedLight serializedLight, Editor owner, bool isInPreset)
        {
            // To the user, we will only display it as a area light, but under the hood, we have Rectangle and Disc. This is not to confuse people
            // who still use our legacy light inspector.

            int selectedLightType = serializedLight.settings.lightType.intValue;

            // Handle all lights that are not in the default set
            if (!Styles.LightTypeValues.Contains(serializedLight.settings.lightType.intValue))
            {
                if (serializedLight.settings.lightType.intValue == (int)LightType.Disc)
                {
                    selectedLightType = (int)LightType.Rectangle;
                }
            }

            var rect = EditorGUILayout.GetControlRect();
            EditorGUI.BeginProperty(rect, Styles.Type, serializedLight.settings.lightType);
            EditorGUI.BeginChangeCheck();
            int type;
            if (Styles.LightTypeValues.Contains(selectedLightType))
            {
                // ^ The currently selected light type is supported in the
                // current pipeline.
                type = EditorGUI.IntPopup(rect, Styles.Type, selectedLightType, Styles.LightTypeTitles, Styles.LightTypeValues);
            }
            else
            {
                // ^ The currently selected light type is not supported in
                // the current pipeline. Add it to the dropdown, since it
                // would show up as a blank entry.
                string currentTitle = ((LightType)selectedLightType).ToString();
                GUIContent[] titles = Styles.LightTypeTitles.Append(EditorGUIUtility.TrTextContent(currentTitle)).ToArray();
                int[] values = Styles.LightTypeValues.Append(selectedLightType).ToArray();
                type = EditorGUI.IntPopup(rect, Styles.Type, selectedLightType, titles, values);
            }

            if (EditorGUI.EndChangeCheck())
            {
                s_SetGizmosDirty();
                serializedLight.settings.lightType.intValue = type;
            }
            EditorGUI.EndProperty();

            if (!Styles.LightTypeValues.Contains(type))
            {
                EditorGUILayout.HelpBox(
                    "This light type is not supported in the current active render pipeline. Change the light type or the active Render Pipeline to use this light.",
                    MessageType.Info
                );
            }

            Light light = serializedLight.settings.light;
            var lightType = light.type;
            if (LightType.Directional != lightType && light == RenderSettings.sun)
            {
                EditorGUILayout.HelpBox(Styles.SunSourceWarning.text, MessageType.Warning);
            }

            if (!serializedLight.settings.lightType.hasMultipleDifferentValues)
            {
                using (new EditorGUI.DisabledScope(serializedLight.settings.isAreaLightType))
                    serializedLight.settings.DrawLightmapping();

                if (serializedLight.settings.isAreaLightType && serializedLight.settings.lightmapping.intValue != (int)LightmapBakeType.Baked)
                {
                    serializedLight.settings.lightmapping.intValue = (int)LightmapBakeType.Baked;
                    serializedLight.Apply();
                }
            }
        }

        internal static void SyncLightAndShadowLayers(UniversalRenderPipelineSerializedLight serializedLight, SerializedProperty serialized)
        {
            // If we're not in decoupled mode for light layers, we sync light with shadow layers.
            // In mixed state, it makes sense to do it only on Light that links the mode.
            foreach (var lightTarget in serializedLight.serializedObject.targetObjects)
            {
                var additionData = (lightTarget as Component).gameObject.GetComponent<UniversalAdditionalLightData>();
                if (additionData.customShadowLayers)
                    continue;

                Light target = lightTarget as Light;
                if (target.renderingLayerMask != serialized.intValue)
                    target.renderingLayerMask = serialized.intValue;
            }
        }

        static void DrawSpotShapeContent(UniversalRenderPipelineSerializedLight serializedLight, Editor owner)
        {
            serializedLight.settings.DrawInnerAndOuterSpotAngle();
        }

        static void DrawAreaShapeContent(UniversalRenderPipelineSerializedLight serializedLight, Editor owner)
        {
            int selectedShape = serializedLight.settings.isAreaLightType ? serializedLight.settings.lightType.intValue : 0;

            // Handle all lights that are not in the default set
            if (!Styles.LightTypeValues.Contains(serializedLight.settings.lightType.intValue))
            {
                if (serializedLight.settings.lightType.intValue == (int)LightType.Disc)
                {
                    selectedShape = (int)LightType.Disc;
                }
            }

            var rect = EditorGUILayout.GetControlRect();
            EditorGUI.BeginProperty(rect, Styles.AreaLightShapeContent, serializedLight.settings.lightType);
            EditorGUI.BeginChangeCheck();
            int shape = EditorGUI.IntPopup(rect, Styles.AreaLightShapeContent, selectedShape, Styles.AreaLightShapeTitles, Styles.AreaLightShapeValues);

            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(serializedLight.settings.light, "Adjust Light Shape");
                serializedLight.settings.lightType.intValue = shape;
            }
            EditorGUI.EndProperty();

            using (new EditorGUI.IndentLevelScope())
                serializedLight.settings.DrawArea();
        }

        static void DrawEmissionContent(UniversalRenderPipelineSerializedLight serializedLight, Editor owner)
        {
            serializedLight.settings.DrawIntensity();
            serializedLight.settings.DrawBounceIntensity();

            if (!serializedLight.settings.lightType.hasMultipleDifferentValues)
            {
                var lightType = serializedLight.settings.light.type;
                if (lightType != LightType.Directional)
                {
#if UNITY_2020_1_OR_NEWER
                    serializedLight.settings.DrawRange();
#else
                    serializedLight.settings.DrawRange(false);
#endif
                }
            }

            DrawLightCookieContent(serializedLight, owner);
        }

        static void DrawRenderingContent(UniversalRenderPipelineSerializedLight serializedLight, Editor owner)
        {
            serializedLight.settings.DrawRenderMode();

            if (serializedLight.settings.light.type != LightType.Rectangle &&
                !serializedLight.settings.isCompletelyBaked)
            {
                EditorGUI.BeginChangeCheck();
                GUI.enabled = UniversalRenderPipeline.asset.useRenderingLayers;
                EditorUtils.DrawRenderingLayerMask(
                    serializedLight.renderingLayers,
                    UniversalRenderPipeline.asset.useRenderingLayers ? Styles.RenderingLayers : Styles.RenderingLayersDisabled
                );
                GUI.enabled = true;
                if (EditorGUI.EndChangeCheck())
                {
                    if (!serializedLight.customShadowLayers.boolValue)
                        SyncLightAndShadowLayers(serializedLight, serializedLight.renderingLayers);
                }
            }

            EditorGUILayout.PropertyField(serializedLight.settings.cullingMask, Styles.CullingMask);
            if (serializedLight.settings.cullingMask.intValue != -1)
            {
                EditorGUILayout.HelpBox(Styles.CullingMaskWarning.text, MessageType.Info);
            }
        }

        static void DrawShadowsContent(UniversalRenderPipelineSerializedLight serializedLight, Editor owner)
        {
            if (serializedLight.settings.lightType.hasMultipleDifferentValues)
            {
                EditorGUILayout.HelpBox("Cannot multi edit shadows from different light types.", MessageType.Info);
                return;
            }

            serializedLight.settings.DrawShadowsType();

            if (serializedLight.settings.shadowsType.hasMultipleDifferentValues)
            {
                EditorGUILayout.HelpBox("Cannot multi edit different shadow types", MessageType.Info);
                return;
            }

            if (serializedLight.settings.light.shadows == LightShadows.None)
                return;

            var lightType = serializedLight.settings.light.type;

            using (new EditorGUI.IndentLevelScope())
            {
                if (serializedLight.settings.isBakedOrMixed)
                {
                    switch (lightType)
                    {
                        // Baked Shadow radius
                        case LightType.Point:
                        case LightType.Spot:
                            serializedLight.settings.DrawBakedShadowRadius();
                            break;
                        case LightType.Directional:
                            serializedLight.settings.DrawBakedShadowAngle();
                            break;
                    }
                }

                if (lightType != LightType.Rectangle && !serializedLight.settings.isCompletelyBaked)
                {
                    EditorGUILayout.LabelField(Styles.ShadowRealtimeSettings, EditorStyles.boldLabel);
                    using (new EditorGUI.IndentLevelScope())
                    {
                        // Resolution
                        if (lightType == LightType.Point || lightType == LightType.Spot)
                            DrawShadowsResolutionGUI(serializedLight);

                        EditorGUILayout.Slider(serializedLight.settings.shadowsStrength, 0f, 1f, Styles.ShadowStrength);

                        // Bias
                        DrawAdditionalShadowData(serializedLight, owner);

                        // this min bound should match the calculation in SharedLightData::GetNearPlaneMinBound()
                        float nearPlaneMinBound = Mathf.Min(0.01f * serializedLight.settings.range.floatValue, 0.1f);
                        EditorGUILayout.Slider(serializedLight.settings.shadowsNearPlane, nearPlaneMinBound, 10.0f, Styles.ShadowNearPlane);
                        var isHololens = false;
                        var isQuest = false;
#if XR_MANAGEMENT_4_0_1_OR_NEWER
                        var buildTargetGroup = BuildPipeline.GetBuildTargetGroup(EditorUserBuildSettings.activeBuildTarget);
                        var buildTargetSettings = XRGeneralSettingsPerBuildTarget.XRGeneralSettingsForBuildTarget(buildTargetGroup);
                        if (buildTargetSettings != null && buildTargetSettings.AssignedSettings != null && buildTargetSettings.AssignedSettings.activeLoaders.Count > 0)
                        {
                            isHololens = buildTargetGroup == BuildTargetGroup.WSA;
                            isQuest = buildTargetGroup == BuildTargetGroup.Android;
                        }

#endif
                        // Soft Shadow Quality
                        if (serializedLight.settings.light.shadows == LightShadows.Soft)
                            EditorGUILayout.PropertyField(serializedLight.softShadowQualityProp, Styles.SoftShadowQuality);

                        if (isHololens || isQuest)
                        {
                            EditorGUILayout.HelpBox(
                                "Per-light soft shadow quality level is not supported on untethered XR platforms. Use the Soft Shadow Quality setting in the URP Asset instead",
                                MessageType.Warning
                            );
                        }

                    }

                    if (UniversalRenderPipeline.asset.useRenderingLayers)
                    {
                        EditorGUI.BeginChangeCheck();
                        EditorGUILayout.PropertyField(serializedLight.customShadowLayers, Styles.customShadowLayers);
                        // Undo the changes in the light component because the SyncLightAndShadowLayers will change the value automatically when link is ticked
                        if (EditorGUI.EndChangeCheck())
                        {
                            if (serializedLight.customShadowLayers.boolValue)
                            {
                                serializedLight.settings.light.renderingLayerMask = serializedLight.shadowRenderingLayers.intValue;
                            }
                            else
                            {
                                serializedLight.serializedAdditionalDataObject.ApplyModifiedProperties(); // we need to push above modification the modification on object as it is used to sync
                                SyncLightAndShadowLayers(serializedLight, serializedLight.renderingLayers);
                            }
                        }

                        if (serializedLight.customShadowLayers.boolValue)
                        {
                            using (new EditorGUI.IndentLevelScope())
                            {
                                EditorGUI.BeginChangeCheck();
                                EditorUtils.DrawRenderingLayerMask(serializedLight.shadowRenderingLayers, Styles.ShadowLayer);
                                if (EditorGUI.EndChangeCheck())
                                {
                                    serializedLight.settings.light.renderingLayerMask = serializedLight.shadowRenderingLayers.intValue;
                                    serializedLight.Apply();
                                }
                            }
                        }
                    }
                }
            }

            if (!UnityEditor.Lightmapping.bakedGI && !serializedLight.settings.lightmapping.hasMultipleDifferentValues && serializedLight.settings.isBakedOrMixed)
                EditorGUILayout.HelpBox(Styles.BakingWarning.text, MessageType.Warning);
        }

        static void DrawAdditionalShadowData(UniversalRenderPipelineSerializedLight serializedLight, Editor editor)
        {
            // 0: Custom bias - 1: Bias values defined in Pipeline settings
            int selectedUseAdditionalData = serializedLight.additionalLightData.usePipelineSettings ? 1 : 0;
            Rect r = EditorGUILayout.GetControlRect(true);
            EditorGUI.BeginProperty(r, Styles.shadowBias, serializedLight.useAdditionalDataProp);
            {
                using (var checkScope = new EditorGUI.ChangeCheckScope())
                {
                    selectedUseAdditionalData = EditorGUI.IntPopup(r, Styles.shadowBias, selectedUseAdditionalData, Styles.displayedDefaultOptions, Styles.optionDefaultValues);
                    if (checkScope.changed)
                    {
                        Undo.RecordObjects(serializedLight.lightsAdditionalData, "Modified light additional data");
                        foreach (var additionData in serializedLight.lightsAdditionalData)
                            additionData.usePipelineSettings = selectedUseAdditionalData != 0;

                        serializedLight.Apply();
                        (editor as UniversalRenderPipelineLightEditor)?.ReconstructReferenceToAdditionalDataSO();
                    }
                }
            }
            EditorGUI.EndProperty();

            if (!serializedLight.useAdditionalDataProp.hasMultipleDifferentValues)
            {
                if (selectedUseAdditionalData != 1) // Custom Bias
                {
                    using (new EditorGUI.IndentLevelScope())
                    {
                        using (var checkScope = new EditorGUI.ChangeCheckScope())
                        {
                            EditorGUILayout.Slider(serializedLight.settings.shadowsBias, 0f, 10f, Styles.ShadowDepthBias);
                            EditorGUILayout.Slider(serializedLight.settings.shadowsNormalBias, 0f, 10f, Styles.ShadowNormalBias);
                            if (checkScope.changed)
                                serializedLight.Apply();
                        }
                    }
                }
            }
        }

        static void DrawShadowsResolutionGUI(UniversalRenderPipelineSerializedLight serializedLight)
        {
            int shadowResolutionTier = serializedLight.additionalLightData.additionalLightsShadowResolutionTier;

            using (new EditorGUILayout.HorizontalScope())
            {
                using (var checkScope = new EditorGUI.ChangeCheckScope())
                {
                    Rect r = EditorGUILayout.GetControlRect(true);
                    r.width += 30;

                    shadowResolutionTier = EditorGUI.IntPopup(r, Styles.ShadowResolution, shadowResolutionTier, Styles.ShadowResolutionDefaultOptions, Styles.ShadowResolutionDefaultValues);
                    if (shadowResolutionTier == UniversalAdditionalLightData.AdditionalLightsShadowResolutionTierCustom)
                    {
                        // show the custom value field GUI.
                        var newResolution = EditorGUILayout.IntField(serializedLight.settings.shadowsResolution.intValue, GUILayout.ExpandWidth(false));
                        serializedLight.settings.shadowsResolution.intValue = Mathf.Max(UniversalAdditionalLightData.AdditionalLightsShadowMinimumResolution, Mathf.NextPowerOfTwo(newResolution));
                    }
                    else
                    {
                        if (GraphicsSettings.renderPipelineAsset is UniversalRenderPipelineAsset urpAsset)
                            EditorGUILayout.LabelField($"{urpAsset.GetAdditionalLightsShadowResolution(shadowResolutionTier)} ({urpAsset.name})", GUILayout.ExpandWidth(false));
                    }
                    if (checkScope.changed)
                    {
                        serializedLight.additionalLightsShadowResolutionTierProp.intValue = shadowResolutionTier;
                        serializedLight.Apply();
                    }
                }
            }

            EditorGUILayout.HelpBox(Styles.ShadowInfo.text, MessageType.Info);
        }

        static void DrawLightCookieContent(UniversalRenderPipelineSerializedLight serializedLight, Editor owner)
        {
            var settings = serializedLight.settings;
            if (settings.lightType.hasMultipleDifferentValues)
            {
                EditorGUILayout.HelpBox("Cannot multi edit light cookies from different light types.", MessageType.Info);
                return;
            }

            settings.DrawCookie();

            // Draw 2D cookie size for directional lights
            bool isDirectionalLight = settings.light.type == LightType.Directional;
            if (isDirectionalLight)
            {
                if (settings.cookie != null)
                {
                    EditorGUI.BeginChangeCheck();
                    EditorGUILayout.PropertyField(serializedLight.lightCookieSizeProp, Styles.LightCookieSize);
                    EditorGUILayout.PropertyField(serializedLight.lightCookieOffsetProp, Styles.LightCookieOffset);
                    if (EditorGUI.EndChangeCheck())
                        Experimental.Lightmapping.SetLightDirty((UnityEngine.Light)serializedLight.serializedObject.targetObject);
                }
            }
        }
    }
}
