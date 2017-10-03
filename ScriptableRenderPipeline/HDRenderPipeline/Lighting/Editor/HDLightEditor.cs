using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Experimental.Rendering;
using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    // TODO: Simplify this editor once we can target 2018.1
    [CanEditMultipleObjects]
    [CustomEditorForRenderPipeline(typeof(Light), typeof(HDRenderPipelineAsset))]
    sealed partial class HDLightEditor : LightEditor
    {
        sealed class SerializedBaseData
        {
            public SerializedProperty type;
            public SerializedProperty range;
            public SerializedProperty spotAngle;
            public SerializedProperty cookie;
            public SerializedProperty cookieSize;
            public SerializedProperty color;
            public SerializedProperty intensity;
            public SerializedProperty bounceIntensity;
            public SerializedProperty colorTemperature;
            public SerializedProperty useColorTemperature;
            public SerializedProperty shadowsType;
            public SerializedProperty shadowsBias;
            public SerializedProperty shadowsNormalBias;
            public SerializedProperty shadowsNearPlane;
            public SerializedProperty lightmapping;
            public SerializedProperty areaSizeX;
            public SerializedProperty areaSizeY;
            public SerializedProperty bakedShadowRadius;
            public SerializedProperty bakedShadowAngle;
        }

        sealed class SerializedLightData
        {
            public SerializedProperty spotInnerPercent;
            public SerializedProperty lightDimmer;
            public SerializedProperty fadeDistance;
            public SerializedProperty affectDiffuse;
            public SerializedProperty affectSpecular;
            public SerializedProperty lightTypeExtent;
            public SerializedProperty spotLightShape;
            public SerializedProperty shapeLength;
            public SerializedProperty shapeWidth;
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
        }

        SerializedObject m_SerializedAdditionalLightData;
        SerializedObject m_SerializedAdditionalShadowData;

        SerializedBaseData m_BaseData;
        SerializedLightData m_AdditionalLightData;
        SerializedShadowData m_AdditionalShadowData;

        // Copied over from teh original LightEditor class. Will go away once we can target 2018.1
        bool m_TypeIsSame { get { return !m_BaseData.type.hasMultipleDifferentValues; } }
        bool m_LightmappingTypeIsSame { get { return !m_BaseData.lightmapping.hasMultipleDifferentValues; } }
        bool m_IsCompletelyBaked { get { return m_BaseData.lightmapping.intValue == 2; } }
        bool m_IsRealtime { get { return m_BaseData.lightmapping.intValue == 4; } }
        Light light { get { return serializedObject.targetObject as Light; } }
        Texture m_Cookie { get { return m_BaseData.cookie.objectReferenceValue as Texture; } }
        bool m_BakingWarningValue { get { return !Lightmapping.bakedGI && m_LightmappingTypeIsSame && !m_IsRealtime; } }
        bool m_BounceWarningValue
        {
            get
            {
                return m_TypeIsSame && (light.type == LightType.Point || light.type == LightType.Spot) &&
                    m_LightmappingTypeIsSame && m_IsRealtime && !m_BaseData.bounceIntensity.hasMultipleDifferentValues
                    && m_BaseData.bounceIntensity.floatValue > 0.0f;
            }
        }
        public bool cookieWarningValue
        {
            get
            {
                return m_TypeIsSame && light.type == LightType.Spot &&
                    !m_BaseData.cookie.hasMultipleDifferentValues && m_Cookie && m_Cookie.wrapMode != TextureWrapMode.Clamp;
            }
        }

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

        void OnEnable()
        {
            // Get & automatically add additional HD data if not present
            var lightData = GetAdditionalData<HDAdditionalLightData>();
            var shadowData = GetAdditionalData<AdditionalShadowData>();
            m_SerializedAdditionalLightData = new SerializedObject(lightData);
            m_SerializedAdditionalShadowData = new SerializedObject(shadowData);

            // Grab all the serialized data we need
            m_BaseData = new SerializedBaseData
            {
                type = serializedObject.FindProperty("m_Type"),
                range = serializedObject.FindProperty("m_Range"),
                spotAngle = serializedObject.FindProperty("m_SpotAngle"),
                cookie = serializedObject.FindProperty("m_Cookie"),
                cookieSize = serializedObject.FindProperty("m_CookieSize"),
                color = serializedObject.FindProperty("m_Color"),
                intensity = serializedObject.FindProperty("m_Intensity"),
                bounceIntensity = serializedObject.FindProperty("m_BounceIntensity"),
                colorTemperature = serializedObject.FindProperty("m_ColorTemperature"),
                useColorTemperature = serializedObject.FindProperty("m_UseColorTemperature"),
                shadowsType = serializedObject.FindProperty("m_Shadows.m_Type"),
                shadowsBias = serializedObject.FindProperty("m_Shadows.m_Bias"),
                shadowsNormalBias = serializedObject.FindProperty("m_Shadows.m_NormalBias"),
                shadowsNearPlane = serializedObject.FindProperty("m_Shadows.m_NearPlane"),
                lightmapping = serializedObject.FindProperty("m_Lightmapping"),
                areaSizeX = serializedObject.FindProperty("m_AreaSize.x"),
                areaSizeY = serializedObject.FindProperty("m_AreaSize.y"),
                bakedShadowRadius = serializedObject.FindProperty("m_ShadowRadius"),
                bakedShadowAngle = serializedObject.FindProperty("m_ShadowAngle")
            };

            using (var o = new PropertyFetcher<HDAdditionalLightData>(m_SerializedAdditionalLightData))
            m_AdditionalLightData = new SerializedLightData
            {
                spotInnerPercent = o.FindProperty(x => x.m_InnerSpotPercent),
                lightDimmer = o.FindProperty(x => x.lightDimmer),
                fadeDistance = o.FindProperty(x => x.fadeDistance),
                affectDiffuse = o.FindProperty(x => x.affectDiffuse),
                affectSpecular = o.FindProperty(x => x.affectSpecular),
                lightTypeExtent = o.FindProperty(x => x.lightTypeExtent),
                spotLightShape = o.FindProperty(x => x.spotLightShape),
                shapeLength = o.FindProperty(x => x.shapeLength),
                shapeWidth = o.FindProperty(x => x.shapeWidth),
                shapeRadius = o.FindProperty(x => x.shapeRadius),
                maxSmoothness = o.FindProperty(x => x.maxSmoothness),
                applyRangeAttenuation = o.FindProperty(x => x.applyRangeAttenuation),

                // Editor stuff
                useOldInspector = o.FindProperty(x => x.useOldInspector),
                showFeatures = o.FindProperty(x => x.featuresFoldout),
                showAdditionalSettings = o.FindProperty(x => x.showAdditionalSettings)
            };

            // TODO: Review this once AdditionalShadowData is refactored
            using (var o = new PropertyFetcher<AdditionalShadowData>(m_SerializedAdditionalShadowData))
            m_AdditionalShadowData = new SerializedShadowData
            {
                dimmer = o.FindProperty(x => x.shadowDimmer),
                fadeDistance = o.FindProperty(x => x.shadowFadeDistance),
                cascadeCount = o.FindProperty("shadowCascadeCount"),
                cascadeRatios = o.FindProperty("shadowCascadeRatios"),
                cascadeBorders = o.FindProperty("shadowCascadeBorders"),
                resolution = o.FindProperty(x => x.shadowResolution)
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

            serializedObject.Update();

            ResolveLightShape();

            DrawFoldout(m_AdditionalLightData.showFeatures, "Features", DrawFeatures);
            DrawFoldout(m_BaseData.type, "Shape", DrawShape);
            DrawFoldout(m_BaseData.intensity, "Light", DrawLightSettings);

            if (m_BaseData.shadowsType.enumValueIndex != (int)LightShadows.None)
                DrawFoldout(m_BaseData.shadowsType, "Shadows", DrawShadows);

            EditorLightUtilities.DrawSplitter();
            EditorGUILayout.Space();

            m_SerializedAdditionalShadowData.ApplyModifiedProperties();
            m_SerializedAdditionalLightData.ApplyModifiedProperties();
            serializedObject.ApplyModifiedProperties();
        }

        void DrawFoldout(SerializedProperty foldoutProperty, string title, Action func)
        {
            EditorLightUtilities.DrawSplitter();

            bool state = foldoutProperty.isExpanded;
            state = EditorLightUtilities.DrawHeaderFoldout(title, state);

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
            EditorGUILayout.PropertyField(m_AdditionalLightData.showAdditionalSettings);

            bool disabledScope = m_IsCompletelyBaked
                || m_LightShape == LightShape.Line
                || m_LightShape == LightShape.Rectangle;

            using (new EditorGUI.DisabledScope(disabledScope))
            {
                bool shadowsEnabled = EditorGUILayout.Toggle(new GUIContent("Enable Shadows"), m_BaseData.shadowsType.enumValueIndex != 0);
                m_BaseData.shadowsType.enumValueIndex = shadowsEnabled ? (int)LightShadows.Hard : (int)LightShadows.None;
            }
        }

        void DrawShape()
        {
            m_LightShape = (LightShape)EditorGUILayout.Popup(s_Styles.shape, (int)m_LightShape, s_Styles.shapeNames);

            // LightShape is HD specific, it need to drive LightType from the original LightType
            // when it make sense, so the GI is still in sync with the light shape
            switch (m_LightShape)
            {
                case LightShape.Directional:
                    m_BaseData.type.enumValueIndex = (int)LightType.Directional;
                    m_AdditionalLightData.lightTypeExtent.enumValueIndex = (int)LightTypeExtent.Punctual;
                    break;

                case LightShape.Point:
                    m_BaseData.type.enumValueIndex = (int)LightType.Point;
                    m_AdditionalLightData.lightTypeExtent.enumValueIndex = (int)LightTypeExtent.Punctual;
                    EditorGUILayout.PropertyField(m_AdditionalLightData.maxSmoothness, s_Styles.maxSmoothness);
                    break;

                case LightShape.Spot:
                    m_BaseData.type.enumValueIndex = (int)LightType.Spot;
                    m_AdditionalLightData.lightTypeExtent.enumValueIndex = (int)LightTypeExtent.Punctual;
                    EditorGUILayout.PropertyField(m_AdditionalLightData.spotLightShape, s_Styles.spotLightShape);
                    var spotLightShape = (SpotLightShape)m_AdditionalLightData.spotLightShape.enumValueIndex;
                    // Cone Spot
                    if (spotLightShape == SpotLightShape.Cone)
                    {
                        EditorGUILayout.Slider(m_BaseData.spotAngle, 0f, 179.9f, s_Styles.spotAngle);
                        EditorGUILayout.Slider(m_AdditionalLightData.spotInnerPercent, 0f, 100f, s_Styles.spotInnerPercent);
                    }
                    // TODO : replace with angle and ratio
                    else if (spotLightShape == SpotLightShape.Pyramid)
                    {
                        EditorGUILayout.Slider(m_AdditionalLightData.shapeLength, 0.01f, 10f, s_Styles.shapeLengthPyramid);
                        EditorGUILayout.Slider(m_AdditionalLightData.shapeWidth, 0.01f, 10f, s_Styles.shapeWidthPyramid);
                    }
                    else if (spotLightShape == SpotLightShape.Box)
                    {
                        EditorGUILayout.PropertyField(m_AdditionalLightData.shapeLength, s_Styles.shapeLengthBox);
                        EditorGUILayout.PropertyField(m_AdditionalLightData.shapeWidth, s_Styles.shapeWidthBox);
                    }
                    EditorGUILayout.PropertyField(m_AdditionalLightData.maxSmoothness, s_Styles.maxSmoothness);
                    break;

                case LightShape.Rectangle:
                    // TODO: Currently if we use Area type as it is offline light in legacy, the light will not exist at runtime
                    //m_BaseData.type.enumValueIndex = (int)LightType.Area;
                    m_BaseData.type.enumValueIndex = (int)LightType.Point;
                    m_AdditionalLightData.lightTypeExtent.enumValueIndex = (int)LightTypeExtent.Rectangle;
                    EditorGUILayout.PropertyField(m_AdditionalLightData.shapeLength, s_Styles.shapeLengthRect);
                    EditorGUILayout.PropertyField(m_AdditionalLightData.shapeWidth, s_Styles.shapeWidthRect);
                    m_BaseData.areaSizeX.floatValue = m_AdditionalLightData.shapeLength.floatValue;
                    m_BaseData.areaSizeY.floatValue = m_AdditionalLightData.shapeWidth.floatValue;
                    m_BaseData.shadowsType.enumValueIndex = (int)LightShadows.None;
                    break;

                case LightShape.Line:
                    // TODO: Currently if we use Area type as it is offline light in legacy, the light will not exist at runtime
                    //m_BaseData.type.enumValueIndex = (int)LightType.Area;
                    m_BaseData.type.enumValueIndex = (int)LightType.Point;
                    m_AdditionalLightData.lightTypeExtent.enumValueIndex = (int)LightTypeExtent.Line;
                    EditorGUILayout.PropertyField(m_AdditionalLightData.shapeLength, s_Styles.shapeLengthLine);
                    // Fake line with a small rectangle in vanilla unity for GI
                    m_BaseData.areaSizeX.floatValue = m_AdditionalLightData.shapeLength.floatValue;
                    m_BaseData.areaSizeY.floatValue = 0.01f;
                    m_BaseData.shadowsType.enumValueIndex = (int)LightShadows.None;
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
            if (GraphicsSettings.lightsUseLinearIntensity && GraphicsSettings.lightsUseColorTemperature)
            {
                EditorGUILayout.PropertyField(m_BaseData.useColorTemperature, s_Styles.useColorTemperature);
                if (m_BaseData.useColorTemperature.boolValue)
                {
                    const float kMinKelvin = 1000f;
                    const float kMaxKelvin = 20000f;

                    EditorGUILayout.LabelField(s_Styles.color);
                    EditorGUI.indentLevel += 1;
                    EditorGUILayout.PropertyField(m_BaseData.color, s_Styles.colorFilter);
                    EditorGUILayout.Slider(m_BaseData.colorTemperature, kMinKelvin, kMaxKelvin, s_Styles.colorTemperature);
                    EditorGUI.indentLevel -= 1;
                }
                else EditorGUILayout.PropertyField(m_BaseData.color, s_Styles.color);
            }
            else EditorGUILayout.PropertyField(m_BaseData.color, s_Styles.color);

            EditorGUILayout.PropertyField(m_BaseData.intensity, s_Styles.intensity);
            EditorGUILayout.PropertyField(m_BaseData.bounceIntensity, s_Styles.lightBounceIntensity);

            // Indirect shadows warning (Should be removed when we support realtime indirect shadows)
            if (m_BounceWarningValue)
                EditorGUILayout.HelpBox(s_Styles.indirectBounceShadowWarning.text, MessageType.Info);

            EditorGUILayout.PropertyField(m_BaseData.range, s_Styles.range);
            EditorGUILayout.PropertyField(m_BaseData.lightmapping, s_Styles.lightmappingMode);

            // Warning if GI Baking disabled and m_Lightmapping isn't realtime
            if (m_BakingWarningValue)
                EditorGUILayout.HelpBox(s_Styles.bakingWarning.text, MessageType.Info);

            // No cookie with area light (maybe in future textured area light ?)
            if (m_LightShape != LightShape.Rectangle && m_LightShape != LightShape.Line)
            {
                EditorGUILayout.PropertyField(m_BaseData.cookie, s_Styles.cookie);

                // Warn on spotlights if the cookie is set to repeat
                if (cookieWarningValue)
                    EditorGUILayout.HelpBox(s_Styles.cookieWarning.text, MessageType.Warning);

                // When directional light use a cookie, it can control the size
                if (m_Cookie != null && m_LightShape == LightShape.Directional)
                {
                    EditorGUILayout.Slider(m_AdditionalLightData.shapeLength, 0.01f, 10f, s_Styles.cookieSizeX);
                    EditorGUILayout.Slider(m_AdditionalLightData.shapeWidth, 0.01f, 10f, s_Styles.cookieSizeY);
                }
            }

            if (m_AdditionalLightData.showAdditionalSettings.boolValue)
            {
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

        void DrawShadows()
        {
            if (m_IsCompletelyBaked)
            {
                switch ((LightType)m_BaseData.type.enumValueIndex)
                {
                    case LightType.Directional:
                        EditorGUILayout.Slider(m_BaseData.bakedShadowAngle, 0f, 90f, s_Styles.bakedShadowAngle);
                        break;
                    case LightType.Spot:
                    case LightType.Point:
                        EditorGUILayout.PropertyField(m_BaseData.bakedShadowRadius, s_Styles.bakedShadowRadius);
                        break;
                }

                return;
            }

            EditorGUILayout.PropertyField(m_AdditionalShadowData.resolution, s_Styles.shadowResolution);
            EditorGUILayout.Slider(m_BaseData.shadowsBias, 0.001f, 1f, s_Styles.shadowBias);
            EditorGUILayout.Slider(m_BaseData.shadowsNormalBias, 0.001f, 1f, s_Styles.shadowNormalBias);
            EditorGUILayout.Slider(m_BaseData.shadowsNearPlane, 0.01f, 10f, s_Styles.shadowNearPlane);

            if (m_BaseData.type.enumValueIndex != (int)LightType.Directional)
                return;

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

            using (var scope = new EditorGUI.ChangeCheckScope())
            {
                // Draw each field first...
                int arraySize = m_AdditionalShadowData.cascadeRatios.arraySize;
                for (int i = 0; i < arraySize; i++)
                    EditorGUILayout.Slider(m_AdditionalShadowData.cascadeRatios.GetArrayElementAtIndex(i), 0f, 1f, s_Styles.shadowCascadeRatios[i]);

                if (scope.changed)
                {
                    // ...then clamp values to avoid out of bounds cascade ratios
                    for (int i = 0; i < arraySize; i++)
                    {
                        var ratios = m_AdditionalShadowData.cascadeRatios;
                        var ratioProp = ratios.GetArrayElementAtIndex(i);
                        float val = ratioProp.floatValue;

                        if (i > 0)
                        {
                            var prevRatioProp = ratios.GetArrayElementAtIndex(i - 1);
                            float prevVal = prevRatioProp.floatValue;
                            val = Mathf.Max(val, prevVal);
                        }

                        if (i < arraySize - 1)
                        {
                            var nextRatioProp = ratios.GetArrayElementAtIndex(i + 1);
                            float nextVal = nextRatioProp.floatValue;
                            val = Mathf.Min(val, nextVal);
                        }

                        ratioProp.floatValue = val;
                    }
                }
            }

            EditorGUI.indentLevel--;

            if (m_AdditionalLightData.showAdditionalSettings.boolValue)
            {
                EditorGUILayout.LabelField("Additional Settings", EditorStyles.boldLabel);
                EditorGUI.indentLevel++;
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
            var type = m_BaseData.type;

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

        // TODO: Move this to a generic EditorUtilities class
        T[] GetAdditionalData<T>()
            where T : Component
        {
            // Handles multi-selection
            var data = targets.Cast<Component>()
                .Select(t => t.GetComponent<T>())
                .ToArray();

            for (int i = 0; i < data.Length; i++)
            {
                if (data[i] == null)
                    data[i] = Undo.AddComponent<T>(((Component)targets[i]).gameObject);
            }

            return data;
        }
    }
}
