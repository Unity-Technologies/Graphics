using System;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.HDPipeline;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    [CanEditMultipleObjects]
    [CustomEditorForRenderPipeline(typeof(Light), typeof(HDRenderPipelineAsset))]
    sealed partial class HDLightEditor : LightEditor
    {
        sealed class SerializedLightData
        {
            public SerializedProperty spotInnerPercent;
            public SerializedProperty lightDimmer;
            public SerializedProperty fadeDistance;
            public SerializedProperty affectDiffuse;
            public SerializedProperty affectSpecular;
            public SerializedProperty lightTypeExtent;
            public SerializedProperty spotLightShape;
            public SerializedProperty shapeWidth;
            public SerializedProperty shapeHeight;
            public SerializedProperty aspectRatio;
            public SerializedProperty shapeRadius;
            public SerializedProperty maxSmoothness;
            public SerializedProperty applyRangeAttenuation;

            // Editor stuff
            public SerializedProperty useOldInspector;
            public SerializedProperty showFeatures;
            public SerializedProperty showAdditionalSettings;
        }

        sealed class SerializedShadowData
        {
            public SerializedProperty dimmer;
            public SerializedProperty fadeDistance;
            public SerializedProperty cascadeCount;
            public SerializedProperty cascadeRatios;
            public SerializedProperty cascadeBorders;
            public SerializedProperty resolution;

            public SerializedProperty enableContactShadows;
            public SerializedProperty contactShadowLength;
            public SerializedProperty contactShadowDistanceScaleFactor;
            public SerializedProperty contactShadowMaxDistance;
            public SerializedProperty contactShadowFadeDistance;
            public SerializedProperty contactShadowSampleCount;
        }

        SerializedObject m_SerializedAdditionalLightData;
        SerializedObject m_SerializedAdditionalShadowData;

        SerializedLightData m_AdditionalLightData;
        SerializedShadowData m_AdditionalShadowData;

        // LightType + LightTypeExtent combined
        enum LightShape
        {
            Spot,
            Directional,
            Point,
            //Area, <= offline base type not displayed in our case but used for GI of our area light
            Rectangle,
            Line,
            //Sphere,
            //Disc,
        }

        // Used for UI only; the processing code must use LightTypeExtent and LightType
        LightShape m_LightShape;

        protected override void OnEnable()
        {
            base.OnEnable();

            // Get & automatically add additional HD data if not present
            var lightData = CoreEditorUtils.GetAdditionalData<HDAdditionalLightData>(targets);
            var shadowData = CoreEditorUtils.GetAdditionalData<AdditionalShadowData>(targets);
            m_SerializedAdditionalLightData = new SerializedObject(lightData);
            m_SerializedAdditionalShadowData = new SerializedObject(shadowData);

            using (var o = new PropertyFetcher<HDAdditionalLightData>(m_SerializedAdditionalLightData))
            m_AdditionalLightData = new SerializedLightData
            {
                spotInnerPercent = o.Find(x => x.m_InnerSpotPercent),
                lightDimmer = o.Find(x => x.lightDimmer),
                fadeDistance = o.Find(x => x.fadeDistance),
                affectDiffuse = o.Find(x => x.affectDiffuse),
                affectSpecular = o.Find(x => x.affectSpecular),
                lightTypeExtent = o.Find(x => x.lightTypeExtent),
                spotLightShape = o.Find(x => x.spotLightShape),
                shapeWidth = o.Find(x => x.shapeWidth),
                shapeHeight = o.Find(x => x.shapeHeight),
                aspectRatio = o.Find(x => x.aspectRatio),
                shapeRadius = o.Find(x => x.shapeRadius),
                maxSmoothness = o.Find(x => x.maxSmoothness),
                applyRangeAttenuation = o.Find(x => x.applyRangeAttenuation),

                // Editor stuff
                useOldInspector = o.Find(x => x.useOldInspector),
                showFeatures = o.Find(x => x.featuresFoldout),
                showAdditionalSettings = o.Find(x => x.showAdditionalSettings)
            };

            // TODO: Review this once AdditionalShadowData is refactored
            using (var o = new PropertyFetcher<AdditionalShadowData>(m_SerializedAdditionalShadowData))
            m_AdditionalShadowData = new SerializedShadowData
            {
                dimmer = o.Find(x => x.shadowDimmer),
                fadeDistance = o.Find(x => x.shadowFadeDistance),
                cascadeCount = o.Find("shadowCascadeCount"),
                cascadeRatios = o.Find("shadowCascadeRatios"),
                cascadeBorders = o.Find("shadowCascadeBorders"),
                resolution = o.Find(x => x.shadowResolution),
                enableContactShadows = o.Find(x => x.enableContactShadows),
                contactShadowLength = o.Find(x => x.contactShadowLength),
                contactShadowDistanceScaleFactor = o.Find(x => x.contactShadowDistanceScaleFactor),
                contactShadowMaxDistance = o.Find(x => x.contactShadowMaxDistance),
                contactShadowFadeDistance = o.Find(x => x.contactShadowFadeDistance),
                contactShadowSampleCount = o.Find(x => x.contactShadowSampleCount),
            };
        }

        public override void OnInspectorGUI()
        {
            m_SerializedAdditionalLightData.Update();
            m_SerializedAdditionalShadowData.Update();

            // Temporary toggle to go back to the old editor & separated additional datas
            bool useOldInspector = m_AdditionalLightData.useOldInspector.boolValue;

            if (GUILayout.Button("Toggle default light editor"))
                useOldInspector = !useOldInspector;

            m_AdditionalLightData.useOldInspector.boolValue = useOldInspector;

            if (useOldInspector)
            {
                DrawDefaultInspector();
                ApplyAdditionalComponentsVisibility(false);
                m_SerializedAdditionalShadowData.ApplyModifiedProperties();
                m_SerializedAdditionalLightData.ApplyModifiedProperties();
                return;
            }

            // New editor
            ApplyAdditionalComponentsVisibility(true);
            CheckStyles();

            settings.Update();

            ResolveLightShape();

            DrawFoldout(m_AdditionalLightData.showFeatures, "Features", DrawFeatures);
            DrawFoldout(settings.lightType, "Shape", DrawShape);
            DrawFoldout(settings.intensity, "Light", DrawLightSettings);

            if (settings.shadowsType.enumValueIndex != (int)LightShadows.None)
                DrawFoldout(settings.shadowsType, "Shadows", DrawShadows);

            CoreEditorUtils.DrawSplitter();
            EditorGUILayout.Space();

            m_SerializedAdditionalShadowData.ApplyModifiedProperties();
            m_SerializedAdditionalLightData.ApplyModifiedProperties();
            settings.ApplyModifiedProperties();
        }

        void DrawFoldout(SerializedProperty foldoutProperty, string title, Action func)
        {
            CoreEditorUtils.DrawSplitter();

            bool state = foldoutProperty.isExpanded;
            state = CoreEditorUtils.DrawHeaderFoldout(title, state);

            if (state)
            {
                EditorGUI.indentLevel++;
                func();
                EditorGUI.indentLevel--;
                GUILayout.Space(2f);
            }

            foldoutProperty.isExpanded = state;
        }

        void DrawFeatures()
        {
            bool disabledScope = settings.isCompletelyBaked
                || m_LightShape == LightShape.Line
                || m_LightShape == LightShape.Rectangle;

            using (new EditorGUI.DisabledScope(disabledScope))
            {
                bool shadowsEnabled = EditorGUILayout.Toggle(CoreEditorUtils.GetContent("Enable Shadows"), settings.shadowsType.enumValueIndex != 0);
                settings.shadowsType.enumValueIndex = shadowsEnabled ? (int)LightShadows.Hard : (int)LightShadows.None;
                if (settings.lightType.enumValueIndex == (int)LightType.Directional)
                {
                    EditorGUILayout.PropertyField(m_AdditionalShadowData.enableContactShadows, CoreEditorUtils.GetContent("Enable Contact Shadows"));
                }
            }

            EditorGUILayout.PropertyField(m_AdditionalLightData.showAdditionalSettings);
        }

        void DrawShape()
        {
            m_LightShape = (LightShape)EditorGUILayout.Popup(s_Styles.shape, (int)m_LightShape, s_Styles.shapeNames);

            if (m_LightShape != LightShape.Directional)
                settings.DrawRange(false);

            // LightShape is HD specific, it need to drive LightType from the original LightType
            // when it make sense, so the GI is still in sync with the light shape
            switch (m_LightShape)
            {
                case LightShape.Directional:
                    settings.lightType.enumValueIndex = (int)LightType.Directional;
                    m_AdditionalLightData.lightTypeExtent.enumValueIndex = (int)LightTypeExtent.Punctual;
                    break;

                case LightShape.Point:
                    settings.lightType.enumValueIndex = (int)LightType.Point;
                    m_AdditionalLightData.lightTypeExtent.enumValueIndex = (int)LightTypeExtent.Punctual;
                    EditorGUILayout.PropertyField(m_AdditionalLightData.maxSmoothness, s_Styles.maxSmoothness);
                    break;

                case LightShape.Spot:
                    settings.lightType.enumValueIndex = (int)LightType.Spot;
                    m_AdditionalLightData.lightTypeExtent.enumValueIndex = (int)LightTypeExtent.Punctual;
                    EditorGUILayout.PropertyField(m_AdditionalLightData.spotLightShape, s_Styles.spotLightShape);
                    var spotLightShape = (SpotLightShape)m_AdditionalLightData.spotLightShape.enumValueIndex;
                    // Cone Spot
                    if (spotLightShape == SpotLightShape.Cone)
                    {
                        settings.DrawSpotAngle();
                        EditorGUILayout.Slider(m_AdditionalLightData.spotInnerPercent, 0f, 100f, s_Styles.spotInnerPercent);
                    }
                    // TODO : replace with angle and ratio
                    else if (spotLightShape == SpotLightShape.Pyramid)
                    {
                        settings.DrawSpotAngle();
                        EditorGUILayout.Slider(m_AdditionalLightData.aspectRatio, 0.05f, 20.0f, s_Styles.aspectRatioPyramid);
                    }
                    else if (spotLightShape == SpotLightShape.Box)
                    {
                        EditorGUILayout.PropertyField(m_AdditionalLightData.shapeWidth, s_Styles.shapeWidthBox);
                        EditorGUILayout.PropertyField(m_AdditionalLightData.shapeHeight, s_Styles.shapeHeightBox);
                    }
                    EditorGUILayout.PropertyField(m_AdditionalLightData.maxSmoothness, s_Styles.maxSmoothness);
                    break;

                case LightShape.Rectangle:
                    // TODO: Currently if we use Area type as it is offline light in legacy, the light will not exist at runtime
                    //m_BaseData.type.enumValueIndex = (int)LightType.Area;
                    settings.lightType.enumValueIndex = (int)LightType.Point;
                    m_AdditionalLightData.lightTypeExtent.enumValueIndex = (int)LightTypeExtent.Rectangle;
                    EditorGUILayout.PropertyField(m_AdditionalLightData.shapeWidth, s_Styles.shapeWidthRect);
                    EditorGUILayout.PropertyField(m_AdditionalLightData.shapeHeight, s_Styles.shapeHeightRect);
                    settings.areaSizeX.floatValue = m_AdditionalLightData.shapeWidth.floatValue;
                    settings.areaSizeY.floatValue = m_AdditionalLightData.shapeHeight.floatValue;
                    settings.shadowsType.enumValueIndex = (int)LightShadows.None;
                    break;

                case LightShape.Line:
                    // TODO: Currently if we use Area type as it is offline light in legacy, the light will not exist at runtime
                    //m_BaseData.type.enumValueIndex = (int)LightType.Area;
                    settings.lightType.enumValueIndex = (int)LightType.Point;
                    m_AdditionalLightData.lightTypeExtent.enumValueIndex = (int)LightTypeExtent.Line;
                    EditorGUILayout.PropertyField(m_AdditionalLightData.shapeWidth, s_Styles.shapeWidthLine);
                    // Fake line with a small rectangle in vanilla unity for GI
                    settings.areaSizeX.floatValue = m_AdditionalLightData.shapeWidth.floatValue;
                    settings.areaSizeY.floatValue = 0.01f;
                    settings.shadowsType.enumValueIndex = (int)LightShadows.None;
                    break;

                case (LightShape)(-1):
                    // don't do anything, this is just to handle multi selection
                    break;

                default:
                    Debug.Assert(false, "Not implemented light type");
                    break;
            }
        }

        void DrawLightSettings()
        {
            settings.DrawColor();
            settings.DrawIntensity();
            settings.DrawBounceIntensity();
            settings.DrawLightmapping();

            // No cookie with area light (maybe in future textured area light ?)
            if (m_LightShape != LightShape.Rectangle && m_LightShape != LightShape.Line)
            {
                settings.DrawCookie();

                // When directional light use a cookie, it can control the size
                if (settings.cookie != null && m_LightShape == LightShape.Directional)
                {
                    EditorGUI.indentLevel++;
                    EditorGUILayout.PropertyField(m_AdditionalLightData.shapeWidth, s_Styles.cookieSizeX);
                    EditorGUILayout.PropertyField(m_AdditionalLightData.shapeHeight, s_Styles.cookieSizeY);
                    EditorGUI.indentLevel--;
                }
            }

            if (m_AdditionalLightData.showAdditionalSettings.boolValue)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Additional Settings", EditorStyles.boldLabel);
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(m_AdditionalLightData.affectDiffuse, s_Styles.affectDiffuse);
                EditorGUILayout.PropertyField(m_AdditionalLightData.affectSpecular, s_Styles.affectSpecular);
                EditorGUILayout.PropertyField(m_AdditionalLightData.fadeDistance, s_Styles.fadeDistance);
                EditorGUILayout.PropertyField(m_AdditionalLightData.lightDimmer, s_Styles.lightDimmer);
                EditorGUILayout.PropertyField(m_AdditionalLightData.applyRangeAttenuation, s_Styles.applyRangeAttenuation);
                EditorGUI.indentLevel--;
            }
        }

        void DrawBakedShadowParameters()
        {
            switch ((LightType)settings.lightType.enumValueIndex)
            {
                case LightType.Directional:
                    EditorGUILayout.Slider(settings.bakedShadowAngleProp, 0f, 90f, s_Styles.bakedShadowAngle);
                    break;
                case LightType.Spot:
                case LightType.Point:
                    EditorGUILayout.PropertyField(settings.bakedShadowRadiusProp, s_Styles.bakedShadowRadius);
                    break;
            }
        }

        void DrawShadows()
        {
            if (settings.isCompletelyBaked)
            {
                DrawBakedShadowParameters();
                return;
            }

            EditorGUILayout.PropertyField(m_AdditionalShadowData.resolution, s_Styles.shadowResolution);
            EditorGUILayout.Slider(settings.shadowsBias, 0.001f, 1f, s_Styles.shadowBias);
            EditorGUILayout.Slider(settings.shadowsNormalBias, 0.001f, 1f, s_Styles.shadowNormalBias);
            EditorGUILayout.Slider(settings.shadowsNearPlane, 0.01f, 10f, s_Styles.shadowNearPlane);

            if (settings.lightType.enumValueIndex == (int)LightType.Directional)
            {
                using (var scope = new EditorGUI.ChangeCheckScope())
                {
                    EditorGUILayout.IntSlider(m_AdditionalShadowData.cascadeCount, 1, 4, s_Styles.shadowCascadeCount);

                    if (scope.changed)
                    {
                        int len = m_AdditionalShadowData.cascadeCount.intValue;
                        m_AdditionalShadowData.cascadeRatios.arraySize = len - 1;
                        m_AdditionalShadowData.cascadeBorders.arraySize = len;
                    }
                }

                EditorGUI.indentLevel++;
                int arraySize = m_AdditionalShadowData.cascadeRatios.arraySize;
                for (int i = 0; i < arraySize; i++)
                    EditorGUILayout.Slider(m_AdditionalShadowData.cascadeRatios.GetArrayElementAtIndex(i), 0f, 1f, s_Styles.shadowCascadeRatios[i]);
                EditorGUI.indentLevel--;

                if(!m_AdditionalShadowData.enableContactShadows.hasMultipleDifferentValues && m_AdditionalShadowData.enableContactShadows.boolValue)
                {
                    EditorGUILayout.Space();
                    EditorGUILayout.LabelField(s_Styles.contactShadow, EditorStyles.boldLabel);

                    EditorGUI.indentLevel++;
                    EditorGUILayout.PropertyField(m_AdditionalShadowData.contactShadowLength, s_Styles.contactShadowLength);
                    EditorGUILayout.PropertyField(m_AdditionalShadowData.contactShadowDistanceScaleFactor, s_Styles.contactShadowDistanceScaleFactor);
                    EditorGUILayout.PropertyField(m_AdditionalShadowData.contactShadowMaxDistance, s_Styles.contactShadowMaxDistance);
                    EditorGUILayout.PropertyField(m_AdditionalShadowData.contactShadowFadeDistance, s_Styles.contactShadowFadeDistance);
                    EditorGUILayout.PropertyField(m_AdditionalShadowData.contactShadowSampleCount, s_Styles.contactShadowSampleCount);
                    EditorGUI.indentLevel--;
                }
            }

            if (settings.isBakedOrMixed)
                DrawBakedShadowParameters();

            // There is currently no additional settings for shadow on directional light
            if (m_AdditionalLightData.showAdditionalSettings.boolValue && settings.lightType.enumValueIndex != (int)LightType.Directional)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Additional Settings", EditorStyles.boldLabel);
                EditorGUI.indentLevel++;

                if (settings.lightType.enumValueIndex == (int)LightType.Point || settings.lightType.enumValueIndex == (int)LightType.Spot)
                    EditorGUILayout.PropertyField(m_AdditionalShadowData.fadeDistance, s_Styles.shadowFadeDistance);

                EditorGUILayout.PropertyField(m_AdditionalShadowData.dimmer, s_Styles.shadowDimmer);
                EditorGUI.indentLevel--;
            }
        }

        // Internal utilities
        void ApplyAdditionalComponentsVisibility(bool hide)
        {
            var flags = hide ? HideFlags.HideInInspector : HideFlags.None;

            foreach (var t in m_SerializedAdditionalLightData.targetObjects)
                ((HDAdditionalLightData)t).hideFlags = flags;

            foreach (var t in m_SerializedAdditionalShadowData.targetObjects)
                ((AdditionalShadowData)t).hideFlags = flags;
        }

        void ResolveLightShape()
        {
            var type = settings.lightType;

            // Special case for multi-selection: don't resolve light shape or it'll corrupt lights
            if (type.hasMultipleDifferentValues)
            {
                m_LightShape = (LightShape)(-1);
                return;
            }

            var lightTypeExtent = (LightTypeExtent)m_AdditionalLightData.lightTypeExtent.enumValueIndex;

            if (lightTypeExtent == LightTypeExtent.Punctual)
            {
                switch ((LightType)type.enumValueIndex)
                {
                    case LightType.Directional:
                        m_LightShape = LightShape.Directional;
                        break;
                    case LightType.Point:
                        m_LightShape = LightShape.Point;
                        break;
                    case LightType.Spot:
                        m_LightShape = LightShape.Spot;
                        break;
                }
            }
            else
            {
                switch (lightTypeExtent)
                {
                    case LightTypeExtent.Rectangle:
                        m_LightShape = LightShape.Rectangle;
                        break;
                    case LightTypeExtent.Line:
                        m_LightShape = LightShape.Line;
                        break;
                }
            }
        }
    }
}
