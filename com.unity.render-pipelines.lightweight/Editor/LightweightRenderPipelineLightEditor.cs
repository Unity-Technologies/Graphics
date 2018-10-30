using System;
using UnityEditor.AnimatedValues;
using UnityEngine;
using UnityEngine.Experimental.Rendering.LightweightPipeline;
using UnityEngine.Rendering;

namespace UnityEditor.Experimental.Rendering.LightweightPipeline
{
    [CanEditMultipleObjects]
    [CustomEditorForRenderPipeline(typeof(Light), typeof(LightweightRenderPipelineAsset))]
    class LightweightRenderPipelineLightEditor : LightEditor
    {
        AnimBool m_AnimSpotOptions = new AnimBool();
        AnimBool m_AnimPointOptions = new AnimBool();
        AnimBool m_AnimDirOptions = new AnimBool();
        AnimBool m_AnimAreaOptions = new AnimBool();
        AnimBool m_AnimRuntimeOptions = new AnimBool();
        AnimBool m_AnimShadowOptions = new AnimBool();
        AnimBool m_AnimShadowAngleOptions = new AnimBool();
        AnimBool m_AnimShadowRadiusOptions = new AnimBool();
        AnimBool m_AnimLightBounceIntensity = new AnimBool();

        class Styles
        {
            public readonly GUIContent SpotAngle = EditorGUIUtility.TrTextContent("Spot Angle", "Controls the angle in degrees at the base of a Spot light's cone.");

            public readonly GUIContent BakingWarning = EditorGUIUtility.TrTextContent("Light mode is currently overridden to Realtime mode. Enable Baked Global Illumination to use Mixed or Baked light modes.");
            public readonly GUIContent DisabledLightWarning = EditorGUIUtility.TrTextContent("Lighting has been disabled in at least one Scene view. Any changes applied to lights in the Scene will not be updated in these views until Lighting has been enabled again.");

            public readonly GUIContent ShadowsNotSupportedWarning = EditorGUIUtility.TrTextContent("Realtime shadows for point lights are not supported. Either disable shadows or set the light mode to Baked.");
            public static readonly GUIContent ShadowRealtimeSettings = EditorGUIUtility.TrTextContent("Realtime Shadows", "Settings for realtime direct shadows.");
            public static readonly GUIContent ShadowStrength = EditorGUIUtility.TrTextContent("Strength", "Controls how dark the shadows cast by the light will be.");
            public static readonly GUIContent ShadowNearPlane = EditorGUIUtility.TrTextContent("Near Plane", "Controls the value for the near clip plane when rendering shadows. Currently clamped to 0.1 units or 1% of the lights range property, whichever is lower.");

            public static GUIContent shadowBias = EditorGUIUtility.TrTextContent("Bias", "Select if the Bias should use the settings from the Pipeline Asset or Custom settings.");
            public static int[] optionDefaultValues = { 0, 1 };

            public static GUIContent[] displayedDefaultOptions =
            {
                new GUIContent("Custom"),
                new GUIContent("Use Pipeline Settings")
            };
        }

        static Styles s_Styles;

        public bool typeIsSame { get { return !settings.lightType.hasMultipleDifferentValues; } }
        public bool shadowTypeIsSame { get { return !settings.shadowsType.hasMultipleDifferentValues; } }
        public bool lightmappingTypeIsSame { get { return !settings.lightmapping.hasMultipleDifferentValues; } }
        public Light lightProperty { get { return target as Light; } }

        public bool spotOptionsValue { get { return typeIsSame && lightProperty.type == LightType.Spot; } }
        public bool pointOptionsValue { get { return typeIsSame && lightProperty.type == LightType.Point; } }
        public bool dirOptionsValue { get { return typeIsSame && lightProperty.type == LightType.Directional; } }
        public bool areaOptionsValue { get { return typeIsSame && lightProperty.type == LightType.Rectangle; } }

        // Point light realtime shadows not supported
        public bool runtimeOptionsValue { get { return typeIsSame && (lightProperty.type != LightType.Rectangle && lightProperty.type != LightType.Point && !settings.isCompletelyBaked); } }
        public bool bakedShadowRadius { get { return typeIsSame && (lightProperty.type == LightType.Point || lightProperty.type == LightType.Spot) && settings.isBakedOrMixed; } }
        public bool bakedShadowAngle { get { return typeIsSame && lightProperty.type == LightType.Directional && settings.isBakedOrMixed; } }
        public bool shadowOptionsValue { get { return shadowTypeIsSame && lightProperty.shadows != LightShadows.None; } }

        public bool bakingWarningValue { get { return !UnityEditor.Lightmapping.bakedGI && lightmappingTypeIsSame && settings.isBakedOrMixed; } }
        public bool showLightBounceIntensity { get { return true; } }

        public bool isShadowEnabled { get { return settings.shadowsType.intValue != 0; } }

        public bool realtimeShadowsWarningValue
        {
            get
            {
                return typeIsSame && lightProperty.type == LightType.Point &&
                    shadowTypeIsSame && isShadowEnabled &&
                    lightmappingTypeIsSame && !settings.isCompletelyBaked;
            }
        }

        LWRPAdditionalLightData m_AdditionalLightData;
        SerializedObject m_AdditionalLightDataSO;

        SerializedProperty m_UseAdditionalDataProp;


        protected override void OnEnable()
        {
            m_AdditionalLightData = lightProperty.gameObject.GetComponent<LWRPAdditionalLightData>();
            settings.OnEnable();
            init(m_AdditionalLightData);
            UpdateShowOptions(true);
        }

        void init(LWRPAdditionalLightData additionalLightData)
        {
            if(additionalLightData == null)
                return;
            m_AdditionalLightDataSO = new SerializedObject(additionalLightData);
            m_UseAdditionalDataProp = m_AdditionalLightDataSO.FindProperty("m_UsePipelineSettings");

            LightweightRenderPipelineAsset asset = GraphicsSettings.renderPipelineAsset as LightweightRenderPipelineAsset;
            settings.shadowsBias.floatValue = asset.shadowDepthBias;
            settings.shadowsNormalBias.floatValue = asset.shadowNormalBias;
            settings.ApplyModifiedProperties();
        }

        public override void OnInspectorGUI()
        {
            if (s_Styles == null)
                s_Styles = new Styles();

            settings.Update();

            // Update AnimBool options. For properties changed they will be smoothly interpolated.
            UpdateShowOptions(false);

            settings.DrawLightType();

            EditorGUILayout.Space();

            // When we are switching between two light types that don't show the range (directional and area lights)
            // we want the fade group to stay hidden.
            using (var group = new EditorGUILayout.FadeGroupScope(1.0f - m_AnimDirOptions.faded))
                if (group.visible)
                    settings.DrawRange(m_AnimAreaOptions.target);

            // Spot angle
            using (var group = new EditorGUILayout.FadeGroupScope(m_AnimSpotOptions.faded))
                if (group.visible)
                    DrawSpotAngle();

            // Area width & height
            using (var group = new EditorGUILayout.FadeGroupScope(m_AnimAreaOptions.faded))
                if (group.visible)
                    settings.DrawArea();

            settings.DrawColor();

            EditorGUILayout.Space();

            using (var group = new EditorGUILayout.FadeGroupScope(1.0f - m_AnimAreaOptions.faded))
                if (group.visible)
                    settings.DrawLightmapping();

            settings.DrawIntensity();

            using (var group = new EditorGUILayout.FadeGroupScope(m_AnimLightBounceIntensity.faded))
                if (group.visible)
                    settings.DrawBounceIntensity();

            ShadowsGUI();

            settings.DrawRenderMode();
            settings.DrawCullingMask();

            EditorGUILayout.Space();

#if UNITY_2019_1_OR_NEWER
            var sceneLighting = SceneView.lastActiveSceneView.sceneLighting;
#else
            var sceneLighting = SceneView.lastActiveSceneView.m_SceneLighting;
#endif

            if (SceneView.lastActiveSceneView != null && !sceneLighting)
                EditorGUILayout.HelpBox(s_Styles.DisabledLightWarning.text, MessageType.Warning);

            serializedObject.ApplyModifiedProperties();
        }

        void SetOptions(AnimBool animBool, bool initialize, bool targetValue)
        {
            if (initialize)
            {
                animBool.value = targetValue;
                animBool.valueChanged.AddListener(Repaint);
            }
            else
            {
                animBool.target = targetValue;
            }
        }

        void UpdateShowOptions(bool initialize)
        {
            SetOptions(m_AnimSpotOptions, initialize, spotOptionsValue);
            SetOptions(m_AnimPointOptions, initialize, pointOptionsValue);
            SetOptions(m_AnimDirOptions, initialize, dirOptionsValue);
            SetOptions(m_AnimAreaOptions, initialize, areaOptionsValue);
            SetOptions(m_AnimShadowOptions, initialize, shadowOptionsValue);
            SetOptions(m_AnimRuntimeOptions, initialize, runtimeOptionsValue);
            SetOptions(m_AnimShadowAngleOptions, initialize, bakedShadowAngle);
            SetOptions(m_AnimShadowRadiusOptions, initialize, bakedShadowRadius);
            SetOptions(m_AnimLightBounceIntensity, initialize, showLightBounceIntensity);
        }

        void DrawSpotAngle()
        {
            EditorGUILayout.Slider(settings.spotAngle, 1f, 179f, s_Styles.SpotAngle);
        }

        void DrawAdditionalShadowData()
        {
            bool hasChanged = false;
            int selectedUseAdditionalData;

            if (m_AdditionalLightDataSO == null)
            {
                selectedUseAdditionalData = 1;
            }
            else
            {
                m_AdditionalLightDataSO.Update();
                selectedUseAdditionalData = !m_AdditionalLightData.usePipelineSettings ? 0 : 1;
            }

            Rect controlRectAdditionalData = EditorGUILayout.GetControlRect(true);
            if(m_AdditionalLightDataSO != null)
                EditorGUI.BeginProperty(controlRectAdditionalData, Styles.shadowBias, m_UseAdditionalDataProp);
            EditorGUI.BeginChangeCheck();

            selectedUseAdditionalData = EditorGUI.IntPopup(controlRectAdditionalData, Styles.shadowBias, selectedUseAdditionalData, Styles.displayedDefaultOptions, Styles.optionDefaultValues);
            if (EditorGUI.EndChangeCheck())
            {
                hasChanged = true;
            }
            if(m_AdditionalLightDataSO != null)
                EditorGUI.EndProperty();

            if (selectedUseAdditionalData != 1 && m_AdditionalLightDataSO != null)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.Slider(settings.shadowsBias, 0f, 10f, "Depth");
                EditorGUILayout.Slider(settings.shadowsNormalBias, 0f, 10f, "Normal");
                EditorGUI.indentLevel--;

                m_AdditionalLightDataSO.ApplyModifiedProperties();
            }

            if (hasChanged)
            {
                if (m_AdditionalLightDataSO == null)
                {
                    lightProperty.gameObject.AddComponent<LWRPAdditionalLightData>();
                    m_AdditionalLightData = lightProperty.gameObject.GetComponent<LWRPAdditionalLightData>();
                    init(m_AdditionalLightData);
                }

                m_UseAdditionalDataProp.intValue = selectedUseAdditionalData;
                m_AdditionalLightDataSO.ApplyModifiedProperties();
            }
        }

        void ShadowsGUI()
        {
            // Shadows drop-down. Area lights can only be baked and always have shadows.
            float show = 1.0f - m_AnimAreaOptions.faded;

			settings.DrawShadowsType();

            EditorGUI.indentLevel += 1;
            show *= m_AnimShadowOptions.faded;
            // Baked Shadow radius
            using (var group = new EditorGUILayout.FadeGroupScope(show * m_AnimShadowRadiusOptions.faded))
                if (group.visible)
                    settings.DrawBakedShadowRadius();

            // Baked Shadow angle
            using (var group = new EditorGUILayout.FadeGroupScope(show * m_AnimShadowAngleOptions.faded))
                if (group.visible)
                    settings.DrawBakedShadowAngle();

            // Runtime shadows - shadow strength, resolution and near plane offset
            // Bias is handled differently in LWRP
            using (var group = new EditorGUILayout.FadeGroupScope(show * m_AnimRuntimeOptions.faded))
            {
                if (group.visible)
                {
                    EditorGUILayout.LabelField(Styles.ShadowRealtimeSettings);
                    EditorGUI.indentLevel += 1;
                    EditorGUILayout.Slider(settings.shadowsStrength, 0f, 1f, Styles.ShadowStrength);

                    DrawAdditionalShadowData();

                    // this min bound should match the calculation in SharedLightData::GetNearPlaneMinBound()
                    float nearPlaneMinBound = Mathf.Min(0.01f * settings.range.floatValue, 0.1f);
                    EditorGUILayout.Slider(settings.shadowsNearPlane, nearPlaneMinBound, 10.0f, Styles.ShadowNearPlane);
                    EditorGUI.indentLevel -= 1;
                }
            }

            EditorGUI.indentLevel -= 1;

            if (bakingWarningValue)
                EditorGUILayout.HelpBox(s_Styles.BakingWarning.text, MessageType.Warning);

            if (realtimeShadowsWarningValue)
                EditorGUILayout.HelpBox(s_Styles.ShadowsNotSupportedWarning.text, MessageType.Warning);

            EditorGUILayout.Space();
        }
    }
}
