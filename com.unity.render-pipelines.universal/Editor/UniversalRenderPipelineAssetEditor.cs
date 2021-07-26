using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering;
using UnityEngine.Scripting.APIUpdating;
using UnityEditorInternal;
using Styles = UnityEditor.Rendering.Universal.UniversalRenderPipelineAssetUI.Styles;

namespace UnityEditor.Rendering.Universal
{
    [CustomEditor(typeof(UniversalRenderPipelineAsset))]
    public class UniversalRenderPipelineAssetEditor : Editor
    {
        SavedBool m_GeneralSettingsFoldout;
        SavedBool m_QualitySettingsFoldout;
        SavedBool m_LightingSettingsFoldout;
        SavedBool m_ShadowSettingsFoldout;
        SavedBool m_PostProcessingSettingsFoldout;
        SavedBool m_AdvancedSettingsFoldout;
        SavedBool m_AdaptivePerformanceFoldout;

        SerializedProperty m_RendererDataProp;
        SerializedProperty m_DefaultRendererProp;

        ReorderableList m_RendererDataList;

        private SerializedUniversalRenderPipelineAsset m_SerializedURPAsset;

        public override void OnInspectorGUI()
        {
            m_SerializedURPAsset.Update();

            DrawRenderingSettings();
            DrawQualitySettings();
            DrawLightingSettings();
            DrawShadowSettings();
            DrawPostProcessingSettings();
            DrawAdvancedSettings();
#if ADAPTIVE_PERFORMANCE_2_0_0_OR_NEWER
            DrawAdaptivePerformance();
#endif

            m_SerializedURPAsset.Apply();
        }

        void OnEnable()
        {
            m_SerializedURPAsset = new SerializedUniversalRenderPipelineAsset(serializedObject);

            m_GeneralSettingsFoldout = new SavedBool($"{target.GetType()}.GeneralSettingsFoldout", false);
            m_QualitySettingsFoldout = new SavedBool($"{target.GetType()}.QualitySettingsFoldout", false);
            m_LightingSettingsFoldout = new SavedBool($"{target.GetType()}.LightingSettingsFoldout", false);
            m_ShadowSettingsFoldout = new SavedBool($"{target.GetType()}.ShadowSettingsFoldout", false);
            m_PostProcessingSettingsFoldout = new SavedBool($"{target.GetType()}.PostProcessingSettingsFoldout", false);
            m_AdvancedSettingsFoldout = new SavedBool($"{target.GetType()}.AdvancedSettingsFoldout", false);
            m_AdaptivePerformanceFoldout = new SavedBool($"{target.GetType()}.AdaptivePerformanceFoldout", false);

            m_RendererDataProp = serializedObject.FindProperty("m_RendererDataList");
            m_DefaultRendererProp = serializedObject.FindProperty("m_DefaultRendererIndex");
            m_RendererDataList = new ReorderableList(serializedObject, m_RendererDataProp, true, true, true, true);

            DrawRendererListLayout(m_RendererDataList, m_RendererDataProp);
        }

        void DrawRenderingSettings()
        {
            m_GeneralSettingsFoldout.value = EditorGUILayout.BeginFoldoutHeaderGroup(m_GeneralSettingsFoldout.value, Styles.renderingSettingsText);
            if (m_GeneralSettingsFoldout.value)
            {
                EditorGUI.indentLevel++;

                EditorGUILayout.Space();
                EditorGUI.indentLevel--;
                m_RendererDataList.DoLayoutList();
                EditorGUI.indentLevel++;

                UniversalRenderPipelineAsset asset = target as UniversalRenderPipelineAsset;
                string unsupportedGraphicsApisMessage;

                if (!asset.ValidateRendererData(-1))
                    EditorGUILayout.HelpBox(Styles.rendererMissingDefaultMessage.text, MessageType.Error, true);
                else if (!asset.ValidateRendererDataList(true))
                    EditorGUILayout.HelpBox(Styles.rendererMissingMessage.text, MessageType.Warning, true);
                else if (!ValidateRendererGraphicsAPIs(asset, out unsupportedGraphicsApisMessage))
                    EditorGUILayout.HelpBox(Styles.rendererUnsupportedAPIMessage.text + unsupportedGraphicsApisMessage, MessageType.Warning, true);

                EditorGUILayout.PropertyField(m_SerializedURPAsset.requireDepthTextureProp, Styles.requireDepthTextureText);
                EditorGUILayout.PropertyField(m_SerializedURPAsset.requireOpaqueTextureProp, Styles.requireOpaqueTextureText);
                EditorGUI.indentLevel++;
                EditorGUI.BeginDisabledGroup(!m_SerializedURPAsset.requireOpaqueTextureProp.boolValue);
                EditorGUILayout.PropertyField(m_SerializedURPAsset.opaqueDownsamplingProp, Styles.opaqueDownsamplingText);
                EditorGUI.EndDisabledGroup();
                EditorGUI.indentLevel--;
                EditorGUILayout.PropertyField(m_SerializedURPAsset.supportsTerrainHolesProp, Styles.supportsTerrainHolesText);
                EditorGUI.indentLevel--;
                EditorGUILayout.Space();
                EditorGUILayout.Space();
            }

            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        void DrawQualitySettings()
        {
            m_QualitySettingsFoldout.value = EditorGUILayout.BeginFoldoutHeaderGroup(m_QualitySettingsFoldout.value, Styles.qualitySettingsText);
            if (m_QualitySettingsFoldout.value)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(m_SerializedURPAsset.hdr, Styles.hdrText);
                EditorGUILayout.PropertyField(m_SerializedURPAsset.msaa, Styles.msaaText);
                m_SerializedURPAsset.renderScale.floatValue = EditorGUILayout.Slider(Styles.renderScaleText, m_SerializedURPAsset.renderScale.floatValue, UniversalRenderPipeline.minRenderScale, UniversalRenderPipeline.maxRenderScale);
                EditorGUI.indentLevel--;
                EditorGUILayout.Space();
                EditorGUILayout.Space();
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        void DrawLightingSettings()
        {
            m_LightingSettingsFoldout.value = EditorGUILayout.BeginFoldoutHeaderGroup(m_LightingSettingsFoldout.value, Styles.lightingSettingsText);
            if (m_LightingSettingsFoldout.value)
            {
                EditorGUI.indentLevel++;

                // Main Light
                bool disableGroup = false;
                EditorGUI.BeginDisabledGroup(disableGroup);
                CoreEditorUtils.DrawPopup(Styles.mainLightRenderingModeText, m_SerializedURPAsset.mainLightRenderingModeProp, Styles.mainLightOptions);
                EditorGUI.EndDisabledGroup();

                EditorGUI.indentLevel++;
                disableGroup |= !m_SerializedURPAsset.mainLightRenderingModeProp.boolValue;

                EditorGUI.BeginDisabledGroup(disableGroup);
                EditorGUILayout.PropertyField(m_SerializedURPAsset.mainLightShadowsSupportedProp, Styles.supportsMainLightShadowsText);
                EditorGUI.EndDisabledGroup();

                disableGroup |= !m_SerializedURPAsset.mainLightShadowsSupportedProp.boolValue;
                EditorGUI.BeginDisabledGroup(disableGroup);
                EditorGUILayout.PropertyField(m_SerializedURPAsset.mainLightShadowmapResolutionProp, Styles.mainLightShadowmapResolutionText);
                EditorGUI.EndDisabledGroup();

                EditorGUI.indentLevel--;
                EditorGUILayout.Space();

                // Additional light
                EditorGUILayout.PropertyField(m_SerializedURPAsset.additionalLightsRenderingModeProp, Styles.addditionalLightsRenderingModeText);
                EditorGUI.indentLevel++;

                disableGroup = m_SerializedURPAsset.additionalLightsRenderingModeProp.intValue == (int)LightRenderingMode.Disabled;
                EditorGUI.BeginDisabledGroup(disableGroup);
                m_SerializedURPAsset.additionalLightsPerObjectLimitProp.intValue = EditorGUILayout.IntSlider(Styles.perObjectLimit, m_SerializedURPAsset.additionalLightsPerObjectLimitProp.intValue, 0, UniversalRenderPipeline.maxPerObjectLights);
                EditorGUI.EndDisabledGroup();

                disableGroup |= (m_SerializedURPAsset.additionalLightsPerObjectLimitProp.intValue == 0 || m_SerializedURPAsset.additionalLightsRenderingModeProp.intValue != (int)LightRenderingMode.PerPixel);
                EditorGUI.BeginDisabledGroup(disableGroup);
                EditorGUILayout.PropertyField(m_SerializedURPAsset.additionalLightShadowsSupportedProp, Styles.supportsAdditionalShadowsText);
                EditorGUI.EndDisabledGroup();

                disableGroup |= !m_SerializedURPAsset.additionalLightShadowsSupportedProp.boolValue;
                EditorGUI.BeginDisabledGroup(disableGroup);
                EditorGUILayout.PropertyField(m_SerializedURPAsset.additionalLightShadowmapResolutionProp, Styles.additionalLightsShadowmapResolution);
                DrawShadowResolutionTierSettings();
                EditorGUI.EndDisabledGroup();

                EditorGUILayout.Space();
                disableGroup = m_SerializedURPAsset.additionalLightsRenderingModeProp.intValue == (int)LightRenderingMode.Disabled;

                EditorGUI.BeginDisabledGroup(disableGroup);
                EditorGUILayout.PropertyField(m_SerializedURPAsset.additionalLightCookieResolutionProp, Styles.additionalLightsCookieResolution);
                EditorGUI.EndDisabledGroup();

                EditorGUI.BeginDisabledGroup(disableGroup);
                EditorGUILayout.PropertyField(m_SerializedURPAsset.additionalLightCookieFormatProp, Styles.additionalLightsCookieFormat);
                EditorGUI.EndDisabledGroup();

                EditorGUI.indentLevel--;
                EditorGUILayout.Space();

                // Reflection Probes
                EditorGUILayout.LabelField(Styles.reflectionProbesSettingsText);
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(m_SerializedURPAsset.reflectionProbeBlendingProp, Styles.reflectionProbeBlendingText);
                EditorGUILayout.PropertyField(m_SerializedURPAsset.reflectionProbeBoxProjectionProp, Styles.reflectionProbeBoxProjectionText);
                EditorGUI.indentLevel--;

                EditorGUILayout.Space();
                EditorGUILayout.Space();
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        void DrawShadowResolutionTierSettings()
        {
            // UI code adapted from HDRP U.I logic implemented in com.unity.render-pipelines.high-definition/Editor/RenderPipeline/Settings/SerializedScalableSetting.cs )

            var rect = GUILayoutUtility.GetRect(0, float.Epsilon, EditorGUIUtility.singleLineHeight, EditorGUIUtility.singleLineHeight);
            var contentRect = EditorGUI.PrefixLabel(rect, Styles.additionalLightsShadowResolutionTiers);

            EditorGUI.BeginChangeCheck();

            const int k_ShadowResolutionTiersCount = 3;
            var values = new[] { m_SerializedURPAsset.additionalLightsShadowResolutionTierLowProp, m_SerializedURPAsset.additionalLightsShadowResolutionTierMediumProp, m_SerializedURPAsset.additionalLightsShadowResolutionTierHighProp };

            var num = contentRect.width / (float)k_ShadowResolutionTiersCount;  // space allocated for every field including the label

            var indentLevel = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0; // Reset the indentation

            float pixelShift = 0;  // Variable to keep track of the current pixel shift in the rectangle we were assigned for this whole section.
            for (var index = 0; index < k_ShadowResolutionTiersCount; ++index)
            {
                var labelWidth = Mathf.Clamp(EditorStyles.label.CalcSize(Styles.additionalLightsShadowResolutionTierNames[index]).x, 0, num);
                EditorGUI.LabelField(new Rect(contentRect.x + pixelShift, contentRect.y, labelWidth, contentRect.height), Styles.additionalLightsShadowResolutionTierNames[index]);
                pixelShift += labelWidth;           // We need to remove from the position the label size that we've just drawn and shift by it's length
                float spaceLeft = num - labelWidth; // The amount of space left for the field
                if (spaceLeft > 2) // If at least two pixels are left to draw this field, draw it, otherwise, skip
                {
                    var fieldSlot = new Rect(contentRect.x + pixelShift, contentRect.y, num - labelWidth, contentRect.height); // Define the rectangle for the field
                    int value = EditorGUI.DelayedIntField(fieldSlot, values[index].intValue);
                    values[index].intValue = Mathf.Max(UniversalAdditionalLightData.AdditionalLightsShadowMinimumResolution, Mathf.NextPowerOfTwo(value));
                }
                pixelShift += spaceLeft;  // Shift by the slot that was left for the field
            }

            EditorGUI.indentLevel = indentLevel;

            EditorGUI.EndChangeCheck();
        }

        void DrawShadowSettings()
        {
            m_ShadowSettingsFoldout.value = EditorGUILayout.BeginFoldoutHeaderGroup(m_ShadowSettingsFoldout.value, Styles.shadowSettingsText);
            if (m_ShadowSettingsFoldout.value)
            {
                EditorGUI.indentLevel++;
                m_SerializedURPAsset.shadowDistanceProp.floatValue = Mathf.Max(0.0f, EditorGUILayout.FloatField(Styles.shadowDistanceText, m_SerializedURPAsset.shadowDistanceProp.floatValue));
                EditorUtils.Unit unit = EditorUtils.Unit.Metric;
                if (m_SerializedURPAsset.shadowCascadeCountProp.intValue != 0)
                {
                    EditorGUI.BeginChangeCheck();
                    unit = (EditorUtils.Unit)EditorGUILayout.EnumPopup(Styles.shadowWorkingUnitText, m_SerializedURPAsset.state.value);
                    if (EditorGUI.EndChangeCheck())
                    {
                        m_SerializedURPAsset.state.value = unit;
                    }
                }

                EditorGUILayout.IntSlider(m_SerializedURPAsset.shadowCascadeCountProp, UniversalRenderPipelineAsset.k_ShadowCascadeMinCount, UniversalRenderPipelineAsset.k_ShadowCascadeMaxCount, Styles.shadowCascadesText);

                int cascadeCount = m_SerializedURPAsset.shadowCascadeCountProp.intValue;
                EditorGUI.indentLevel++;

                bool useMetric = unit == EditorUtils.Unit.Metric;
                float baseMetric = m_SerializedURPAsset.shadowDistanceProp.floatValue;
                int cascadeSplitCount = cascadeCount - 1;

                DrawCascadeSliders(cascadeSplitCount, useMetric, baseMetric);

                EditorGUI.indentLevel--;
                DrawCascades(cascadeCount, useMetric, baseMetric);
                EditorGUI.indentLevel++;

                m_SerializedURPAsset.shadowDepthBiasProp.floatValue = EditorGUILayout.Slider(Styles.shadowDepthBias, m_SerializedURPAsset.shadowDepthBiasProp.floatValue, 0.0f, UniversalRenderPipeline.maxShadowBias);
                m_SerializedURPAsset.shadowNormalBiasProp.floatValue = EditorGUILayout.Slider(Styles.shadowNormalBias, m_SerializedURPAsset.shadowNormalBiasProp.floatValue, 0.0f, UniversalRenderPipeline.maxShadowBias);
                EditorGUILayout.PropertyField(m_SerializedURPAsset.softShadowsSupportedProp, Styles.supportsSoftShadows);
                EditorGUI.indentLevel--;
                EditorGUILayout.Space();
                EditorGUILayout.Space();
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        private void DrawCascadeSliders(int splitCount, bool useMetric, float baseMetric)
        {
            Vector4 shadowCascadeSplit = Vector4.one;
            if (splitCount == 3)
                shadowCascadeSplit = new Vector4(m_SerializedURPAsset.shadowCascade4SplitProp.vector3Value.x, m_SerializedURPAsset.shadowCascade4SplitProp.vector3Value.y, m_SerializedURPAsset.shadowCascade4SplitProp.vector3Value.z, 1);
            else if (splitCount == 2)
                shadowCascadeSplit = new Vector4(m_SerializedURPAsset.shadowCascade3SplitProp.vector2Value.x, m_SerializedURPAsset.shadowCascade3SplitProp.vector2Value.y, 1, 0);
            else if (splitCount == 1)
                shadowCascadeSplit = new Vector4(m_SerializedURPAsset.shadowCascade2SplitProp.floatValue, 1, 0, 0);

            float splitBias = 0.001f;
            float invBaseMetric = baseMetric == 0 ? 0 : 1f / baseMetric;

            // Ensure correct split order
            shadowCascadeSplit[0] = Mathf.Clamp(shadowCascadeSplit[0], 0f, shadowCascadeSplit[1] - splitBias);
            shadowCascadeSplit[1] = Mathf.Clamp(shadowCascadeSplit[1], shadowCascadeSplit[0] + splitBias, shadowCascadeSplit[2] - splitBias);
            shadowCascadeSplit[2] = Mathf.Clamp(shadowCascadeSplit[2], shadowCascadeSplit[1] + splitBias, shadowCascadeSplit[3] - splitBias);


            EditorGUI.BeginChangeCheck();
            for (int i = 0; i < splitCount; ++i)
            {
                float value = shadowCascadeSplit[i];

                float minimum = i == 0 ? 0 : shadowCascadeSplit[i - 1] + splitBias;
                float maximum = i == splitCount - 1 ? 1 : shadowCascadeSplit[i + 1] - splitBias;

                if (useMetric)
                {
                    float valueMetric = value * baseMetric;
                    valueMetric = EditorGUILayout.Slider(EditorGUIUtility.TrTextContent($"Split {i + 1}", "The distance where this cascade ends and the next one starts."), valueMetric, 0f, baseMetric, null);

                    shadowCascadeSplit[i] = Mathf.Clamp(valueMetric * invBaseMetric, minimum, maximum);
                }
                else
                {
                    float valueProcentage = value * 100f;
                    valueProcentage = EditorGUILayout.Slider(EditorGUIUtility.TrTextContent($"Split {i + 1}", "The distance where this cascade ends and the next one starts."), valueProcentage, 0f, 100f, null);

                    shadowCascadeSplit[i] = Mathf.Clamp(valueProcentage * 0.01f, minimum, maximum);
                }
            }

            if (EditorGUI.EndChangeCheck())
            {
                if (splitCount == 3)
                    m_SerializedURPAsset.shadowCascade4SplitProp.vector3Value = shadowCascadeSplit;
                else if (splitCount == 2)
                    m_SerializedURPAsset.shadowCascade3SplitProp.vector2Value = shadowCascadeSplit;
                else if (splitCount == 1)
                    m_SerializedURPAsset.shadowCascade2SplitProp.floatValue = shadowCascadeSplit.x;
            }

            var borderValue = m_SerializedURPAsset.shadowCascadeBorderProp.floatValue;

            EditorGUI.BeginChangeCheck();
            if (useMetric)
            {
                var lastCascadeSplitSize = splitCount == 0 ? baseMetric : (1.0f - shadowCascadeSplit[splitCount - 1]) * baseMetric;
                var invLastCascadeSplitSize = lastCascadeSplitSize == 0 ? 0 : 1f / lastCascadeSplitSize;
                float valueMetric = borderValue * lastCascadeSplitSize;
                valueMetric = EditorGUILayout.Slider(EditorGUIUtility.TrTextContent("Last Border", "The distance of the last cascade."), valueMetric, 0f, lastCascadeSplitSize, null);

                borderValue = valueMetric * invLastCascadeSplitSize;
            }
            else
            {
                float valueProcentage = borderValue * 100f;
                valueProcentage = EditorGUILayout.Slider(EditorGUIUtility.TrTextContent("Last Border", "The distance of the last cascade."), valueProcentage, 0f, 100f, null);

                borderValue = valueProcentage * 0.01f;
            }

            if (EditorGUI.EndChangeCheck())
            {
                m_SerializedURPAsset.shadowCascadeBorderProp.floatValue = borderValue;
            }
        }

        private void DrawCascades(int cascadeCount, bool useMetric, float baseMetric)
        {
            var cascades = new ShadowCascadeGUI.Cascade[cascadeCount];

            Vector3 shadowCascadeSplit = Vector3.zero;
            if (cascadeCount == 4)
                shadowCascadeSplit = m_SerializedURPAsset.shadowCascade4SplitProp.vector3Value;
            else if (cascadeCount == 3)
                shadowCascadeSplit = m_SerializedURPAsset.shadowCascade3SplitProp.vector2Value;
            else if (cascadeCount == 2)
                shadowCascadeSplit.x = m_SerializedURPAsset.shadowCascade2SplitProp.floatValue;
            else
                shadowCascadeSplit.x = m_SerializedURPAsset.shadowCascade2SplitProp.floatValue;

            float lastCascadePartitionSplit = 0;
            for (int i = 0; i < cascadeCount - 1; ++i)
            {
                cascades[i] = new ShadowCascadeGUI.Cascade()
                {
                    size = i == 0 ? shadowCascadeSplit[i] : shadowCascadeSplit[i] - lastCascadePartitionSplit, // Calculate the size of cascade
                    borderSize = 0,
                    cascadeHandleState = ShadowCascadeGUI.HandleState.Enabled,
                    borderHandleState = ShadowCascadeGUI.HandleState.Hidden,
                };
                lastCascadePartitionSplit = shadowCascadeSplit[i];
            }

            // Last cascade is special
            var lastCascade = cascadeCount - 1;
            cascades[lastCascade] = new ShadowCascadeGUI.Cascade()
            {
                size = lastCascade == 0 ? 1.0f : 1 - shadowCascadeSplit[lastCascade - 1], // Calculate the size of cascade
                borderSize = m_SerializedURPAsset.shadowCascadeBorderProp.floatValue,
                cascadeHandleState = ShadowCascadeGUI.HandleState.Hidden,
                borderHandleState = ShadowCascadeGUI.HandleState.Enabled,
            };

            EditorGUI.BeginChangeCheck();
            ShadowCascadeGUI.DrawCascades(ref cascades, useMetric, baseMetric);
            if (EditorGUI.EndChangeCheck())
            {
                if (cascadeCount == 4)
                    m_SerializedURPAsset.shadowCascade4SplitProp.vector3Value = new Vector3(
                        cascades[0].size,
                        cascades[0].size + cascades[1].size,
                        cascades[0].size + cascades[1].size + cascades[2].size
                    );
                else if (cascadeCount == 3)
                    m_SerializedURPAsset.shadowCascade3SplitProp.vector2Value = new Vector2(
                        cascades[0].size,
                        cascades[0].size + cascades[1].size
                    );
                else if (cascadeCount == 2)
                    m_SerializedURPAsset.shadowCascade2SplitProp.floatValue = cascades[0].size;

                m_SerializedURPAsset.shadowCascadeBorderProp.floatValue = cascades[lastCascade].borderSize;
            }
        }

        void DrawPostProcessingSettings()
        {
            m_PostProcessingSettingsFoldout.value = EditorGUILayout.BeginFoldoutHeaderGroup(m_PostProcessingSettingsFoldout.value, Styles.postProcessingSettingsText);
            if (m_PostProcessingSettingsFoldout.value)
            {
                bool isHdrOn = m_SerializedURPAsset.hdr.boolValue;

                EditorGUI.indentLevel++;

                EditorGUILayout.PropertyField(m_SerializedURPAsset.colorGradingMode, Styles.colorGradingMode);
                if (!isHdrOn && m_SerializedURPAsset.colorGradingMode.intValue == (int)ColorGradingMode.HighDynamicRange)
                    EditorGUILayout.HelpBox(Styles.colorGradingModeWarning, MessageType.Warning);
                else if (isHdrOn && m_SerializedURPAsset.colorGradingMode.intValue == (int)ColorGradingMode.HighDynamicRange)
                    EditorGUILayout.HelpBox(Styles.colorGradingModeSpecInfo, MessageType.Info);

                EditorGUILayout.DelayedIntField(m_SerializedURPAsset.colorGradingLutSize, Styles.colorGradingLutSize);
                m_SerializedURPAsset.colorGradingLutSize.intValue = Mathf.Clamp(m_SerializedURPAsset.colorGradingLutSize.intValue, UniversalRenderPipelineAsset.k_MinLutSize, UniversalRenderPipelineAsset.k_MaxLutSize);
                if (isHdrOn && m_SerializedURPAsset.colorGradingMode.intValue == (int)ColorGradingMode.HighDynamicRange && m_SerializedURPAsset.colorGradingLutSize.intValue < 32)
                    EditorGUILayout.HelpBox(Styles.colorGradingLutSizeWarning, MessageType.Warning);

                EditorGUILayout.PropertyField(m_SerializedURPAsset.useFastSRGBLinearConversion, Styles.useFastSRGBLinearConversion);

                EditorGUI.indentLevel--;
                EditorGUILayout.Space();
                EditorGUILayout.Space();
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        void DrawAdvancedSettings()
        {
            m_AdvancedSettingsFoldout.value = EditorGUILayout.BeginFoldoutHeaderGroup(m_AdvancedSettingsFoldout.value, Styles.advancedSettingsText);
            if (m_AdvancedSettingsFoldout.value)
            {
                UniversalRenderPipelineAsset asset = target as UniversalRenderPipelineAsset;
                string unsupportedGraphicsApisMessage;

                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(m_SerializedURPAsset.srpBatcher, Styles.srpBatcher);
                EditorGUILayout.PropertyField(m_SerializedURPAsset.supportsDynamicBatching, Styles.dynamicBatching);
                EditorGUILayout.PropertyField(m_SerializedURPAsset.mixedLightingSupportedProp, Styles.mixedLightingSupportLabel);
                EditorGUILayout.PropertyField(m_SerializedURPAsset.supportsLightLayers, Styles.supportsLightLayers);

                if (m_SerializedURPAsset.supportsLightLayers.boolValue && !ValidateRendererGraphicsAPIsForLightLayers(asset, out unsupportedGraphicsApisMessage))
                    EditorGUILayout.HelpBox(Styles.lightlayersUnsupportedMessage.text + unsupportedGraphicsApisMessage, MessageType.Warning, true);
                EditorGUILayout.PropertyField(m_SerializedURPAsset.debugLevelProp, Styles.debugLevel);
                EditorGUILayout.PropertyField(m_SerializedURPAsset.shaderVariantLogLevel, Styles.shaderVariantLogLevel);
                EditorGUILayout.PropertyField(m_SerializedURPAsset.storeActionsOptimizationProperty, Styles.storeActionsOptimizationText);
                CoreEditorUtils.DrawPopup(Styles.volumeFrameworkUpdateMode, m_SerializedURPAsset.volumeFrameworkUpdateModeProp, Styles.volumeFrameworkUpdateOptions);
                EditorGUI.indentLevel--;
                EditorGUILayout.Space();
                EditorGUILayout.Space();
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        void DrawAdaptivePerformance()
        {
            m_AdaptivePerformanceFoldout.value = EditorGUILayout.BeginFoldoutHeaderGroup(m_AdaptivePerformanceFoldout.value, Styles.adaptivePerformanceText);
            if (m_AdaptivePerformanceFoldout.value)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(m_SerializedURPAsset.useAdaptivePerformance, Styles.useAdaptivePerformance);
                EditorGUI.indentLevel--;
                EditorGUILayout.Space();
                EditorGUILayout.Space();
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        void DrawRendererListLayout(ReorderableList list, SerializedProperty prop)
        {
            list.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
            {
                rect.y += 2;
                Rect indexRect = new Rect(rect.x, rect.y, 14, EditorGUIUtility.singleLineHeight);
                EditorGUI.LabelField(indexRect, index.ToString());
                Rect objRect = new Rect(rect.x + indexRect.width, rect.y, rect.width - 134, EditorGUIUtility.singleLineHeight);

                EditorGUI.BeginChangeCheck();
                EditorGUI.ObjectField(objRect, prop.GetArrayElementAtIndex(index), GUIContent.none);
                if (EditorGUI.EndChangeCheck())
                    EditorUtility.SetDirty(target);

                Rect defaultButton = new Rect(rect.width - 75, rect.y, 86, EditorGUIUtility.singleLineHeight);
                var defaultRenderer = m_DefaultRendererProp.intValue;
                GUI.enabled = index != defaultRenderer;
                if (GUI.Button(defaultButton, !GUI.enabled ? Styles.rendererDefaultText : Styles.rendererSetDefaultText))
                {
                    m_DefaultRendererProp.intValue = index;
                    EditorUtility.SetDirty(target);
                }
                GUI.enabled = true;

                Rect selectRect = new Rect(rect.x + rect.width - 24, rect.y, 24, EditorGUIUtility.singleLineHeight);

                UniversalRenderPipelineAsset asset = target as UniversalRenderPipelineAsset;

                if (asset.ValidateRendererData(index))
                {
                    if (GUI.Button(selectRect, Styles.rendererSettingsText))
                    {
                        Selection.SetActiveObjectWithContext(prop.GetArrayElementAtIndex(index).objectReferenceValue,
                            null);
                    }
                }
                else // Missing ScriptableRendererData
                {
                    if (GUI.Button(selectRect, index == defaultRenderer ? Styles.rendererDefaultMissingText : Styles.rendererMissingText))
                    {
                        EditorGUIUtility.ShowObjectPicker<ScriptableRendererData>(null, false, null, index);
                    }
                }

                // If object selector chose an object, assign it to the correct ScriptableRendererData slot.
                if (Event.current.commandName == "ObjectSelectorUpdated" && EditorGUIUtility.GetObjectPickerControlID() == index)
                {
                    prop.GetArrayElementAtIndex(index).objectReferenceValue = EditorGUIUtility.GetObjectPickerObject();
                }
            };

            list.drawHeaderCallback = (Rect rect) =>
            {
                EditorGUI.LabelField(rect, Styles.rendererHeaderText);
            };

            list.onCanRemoveCallback = li => { return li.count > 1; };

            list.onRemoveCallback = li =>
            {
                bool shouldUpdateIndex = false;
                // Checking so that the user is not deleting  the default renderer
                if (li.index != m_DefaultRendererProp.intValue)
                {
                    // Need to add the undo to the removal of our assets here, for it to work properly.
                    Undo.RecordObject(target, $"Deleting renderer at index {li.index}");

                    if (prop.GetArrayElementAtIndex(li.index).objectReferenceValue == null)
                    {
                        shouldUpdateIndex = true;
                    }
                    prop.DeleteArrayElementAtIndex(li.index);
                }
                else
                {
                    EditorUtility.DisplayDialog(Styles.rendererListDefaultMessage.text, Styles.rendererListDefaultMessage.tooltip,
                        "Close");
                }

                if (shouldUpdateIndex)
                {
                    UpdateDefaultRendererValue(li.index);
                }

                EditorUtility.SetDirty(target);
            };

            list.onReorderCallbackWithDetails += (reorderableList, index, newIndex) =>
            {
                // Need to update the default renderer index
                UpdateDefaultRendererValue(index, newIndex);
            };
        }

        void UpdateDefaultRendererValue(int index)
        {
            // If the index that is being removed is lower than the default renderer value,
            // the default prop value needs to be one lower.
            if (index < m_DefaultRendererProp.intValue)
            {
                m_DefaultRendererProp.intValue--;
            }
        }

        void UpdateDefaultRendererValue(int prevIndex, int newIndex)
        {
            // If we are moving the index that is the same as the default renderer we need to update that
            if (prevIndex == m_DefaultRendererProp.intValue)
            {
                m_DefaultRendererProp.intValue = newIndex;
            }
            // If newIndex is the same as default
            // then we need to know if newIndex is above or below the default index
            else if (newIndex == m_DefaultRendererProp.intValue)
            {
                m_DefaultRendererProp.intValue += prevIndex > newIndex ? 1 : -1;
            }
            // If the old index is lower than default renderer and
            // the new index is higher then we need to move the default renderer index one lower
            else if (prevIndex < m_DefaultRendererProp.intValue && newIndex > m_DefaultRendererProp.intValue)
            {
                m_DefaultRendererProp.intValue--;
            }
            else if (newIndex < m_DefaultRendererProp.intValue && prevIndex > m_DefaultRendererProp.intValue)
            {
                m_DefaultRendererProp.intValue++;
            }
        }

        internal static bool ValidateRendererGraphicsAPIs(UniversalRenderPipelineAsset pipelineAsset, out string unsupportedGraphicsApisMessage)
        {
            // Check the list of Renderers against all Graphics APIs the player is built with.
            unsupportedGraphicsApisMessage = null;

            BuildTarget platform = EditorUserBuildSettings.activeBuildTarget;
            GraphicsDeviceType[] graphicsAPIs = PlayerSettings.GetGraphicsAPIs(platform);
            int rendererCount = pipelineAsset.m_RendererDataList.Length;

            for (int i = 0; i < rendererCount; i++)
            {
                ScriptableRenderer renderer = pipelineAsset.GetRenderer(i);
                if (renderer == null)
                    continue;

                GraphicsDeviceType[] unsupportedAPIs = renderer.unsupportedGraphicsDeviceTypes;

                for (int apiIndex = 0; apiIndex < unsupportedAPIs.Length; apiIndex++)
                {
                    if (System.Array.FindIndex(graphicsAPIs, element => element == unsupportedAPIs[apiIndex]) >= 0)
                        unsupportedGraphicsApisMessage += System.String.Format("{0} at index {1} does not support {2}.\n", renderer, i, unsupportedAPIs[apiIndex]);
                }
            }

            return unsupportedGraphicsApisMessage == null;
        }

        internal static bool ValidateRendererGraphicsAPIsForLightLayers(UniversalRenderPipelineAsset pipelineAsset, out string unsupportedGraphicsApisMessage)
        {
            unsupportedGraphicsApisMessage = null;

            BuildTarget platform = EditorUserBuildSettings.activeBuildTarget;
            GraphicsDeviceType[] graphicsAPIs = PlayerSettings.GetGraphicsAPIs(platform);

            for (int apiIndex = 0; apiIndex < graphicsAPIs.Length; apiIndex++)
            {
                if (!RenderingUtils.SupportsLightLayers(graphicsAPIs[apiIndex]))
                {
                    if (unsupportedGraphicsApisMessage != null)
                        unsupportedGraphicsApisMessage += ", ";
                    unsupportedGraphicsApisMessage += System.String.Format("{0}", graphicsAPIs[apiIndex]);
                }
            }

            if (unsupportedGraphicsApisMessage != null)
                unsupportedGraphicsApisMessage += ".";

            return unsupportedGraphicsApisMessage == null;
        }
    }
}
