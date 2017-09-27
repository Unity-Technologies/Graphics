using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;
using System.Linq;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    [CustomEditorForRenderPipeline(typeof(Light), typeof(HDRenderPipelineAsset))]
    [CanEditMultipleObjects]
    public class HDLightEditor : Editor
    {
        // Original light editor
        SerializedProperty m_Type;
        SerializedProperty m_Range;
        SerializedProperty m_SpotAngle;
        SerializedProperty m_CookieSize;
        SerializedProperty m_Color;
        SerializedProperty m_Intensity;
        SerializedProperty m_BounceIntensity;
        SerializedProperty m_ColorTemperature;
        SerializedProperty m_UseColorTemperature;
        SerializedProperty m_Cookie;
        SerializedProperty m_ShadowsType;
        SerializedProperty m_ShadowsStrength;
        SerializedProperty m_ShadowsResolution;
        SerializedProperty m_ShadowsBias;
        SerializedProperty m_ShadowsNormalBias;
        SerializedProperty m_ShadowsNearPlane;
        SerializedProperty m_Flare;
        SerializedProperty m_RenderMode;
        SerializedProperty m_CullingMask;
        SerializedProperty m_Lightmapping;
        SerializedProperty m_AreaSizeX;
        SerializedProperty m_AreaSizeY;
        SerializedProperty m_BakedShadowRadius;
        SerializedProperty m_BakedShadowAngle;

        // HD Light editor part
        SerializedObject additionalDataSerializedObject;
        SerializedObject shadowDataSerializedObject;

        SerializedProperty m_SpotInnerPercent;
        SerializedProperty m_LightDimmer;
        SerializedProperty m_FadeDistance;
        SerializedProperty m_AffectDiffuse;
        SerializedProperty m_AffectSpecular;
        SerializedProperty m_LightTypeExtent;
        SerializedProperty m_SpotLightShape;
        SerializedProperty m_ShapeLength;
        SerializedProperty m_ShapeWidth;
        SerializedProperty m_ShapeRadius;
        SerializedProperty m_MaxSmoothness;
        SerializedProperty m_ApplyRangeAttenuation;

        // Extra data for GUI only
        SerializedProperty m_UseOldInspector;
        SerializedProperty m_ShowAdditionalSettings;

        SerializedProperty m_ShadowDimmer;
        SerializedProperty m_ShadowFadeDistance;
        SerializedProperty m_ShadowCascadeCount;
        SerializedProperty m_ShadowCascadeRatios;
        SerializedProperty m_ShadowCascadeBorders;
        SerializedProperty m_ShadowResolution;

        // This enum below is LightType enum + LightTypeExtent enum
        public enum LightShape
        {
            Spot,
            Directional,
            Point,
            //Area, <= offline type of Unity not dispay in our case but reuse for GI of our area light
            Rectangle,
            Line,
            // Sphere,
            // Disc,
        }

        // LightShape is use for displaying UI only. The processing code must use LightTypeExtent and LightType
        LightShape m_LightShape;

        private Light light { get { return target as Light; } }

        // Note: There is no Lightmapping enum, the code C# side must use int
        // Light.h file: int m_Lightmapping; ///< enum { Dynamic=4, Stationary=1, Static=2 }
        // Lighting.h file:
        // enum LightmapBakeType (not accessible in C#)
        //{
        //    kLightRealtime  = 1 << 2,   // Light is realtime
        //    kLightBaked     = 1 << 1,   // light will always be fully baked
        //    kLightMixed     = 1 << 0,   // depends on selected LightmapMixedBakeMode
        //};
        // This mean that m_Lightmapping.enumValueIndex is enum { Mixed=0, Baked=1, Realtime=2 }
        enum LightMappingType
        {
            Mixed,
            Baked,
            Realtime
        }

        protected class Styles
        {
            public static GUIContent CookieSizeX = new GUIContent("CookieSizeX", "");
            public static GUIContent CookieSizeY = new GUIContent("CookieSizeY", "");

            public static GUIContent ShapeLengthLine = new GUIContent("Length", "Length of the line light");
            public static GUIContent ShapeLengthRect = new GUIContent("SizeX", "SizeX of the rectangle light");
            public static GUIContent ShapeWidthRect = new GUIContent("SizeY", "SizeY of the rectangle light");

            public static GUIContent ShapeLengthPyramid = new GUIContent("SizeX", "");
            public static GUIContent ShapeWidthPyramid = new GUIContent("SizeY", "");

            public static GUIContent ShapeLengthBox = new GUIContent("SizeX", "");
            public static GUIContent ShapeWidthBox = new GUIContent("SizeY", "");

            public static GUIContent MaxSmoothness = new GUIContent("MaxSmoothness", "Very low cost way of faking spherical area lighting. This will modify the roughness of the material lit. This is useful when the specular highlight is too small or too sharp.");
            public static GUIContent SpotLightShape = new GUIContent("SpotLightShape", "The shape use for the spotlight. Has an impact on the cookie transformation and light angular attenuation.");

            public static GUIContent SpotAngle = new GUIContent("Spot Angle", "Controls the angle in degrees at the base of a Spot light's cone.");
            public static GUIContent SpotInnerPercent = new GUIContent("Spot Inner Percent", "Controls size of the angular attenuation in percent of the base angle of the Spot light's cone.");

            public static GUIContent Color = new GUIContent("Color", "Controls the color being emitted by the light.");
            public static GUIContent Intensity = new GUIContent("Intensity", "Controls the brightness of the light. Light color is multiplied by this value.");

            public static GUIContent Range = new GUIContent("Range", "Controls how far the light is emitted from the center of the object.");
            public static GUIContent LightmappingMode = new GUIContent("Mode", "Specifies the light mode used to determine if and how a light will be baked. Possible modes are Baked, Mixed, and Realtime.");
            public static GUIContent BounceIntensity = new GUIContent("Indirect Multiplier", "Controls the intensity of indirect light being contributed to the scene. A value of 0 will cause Realtime lights to be removed from realtime global illumination and Baked and Mixed lights to no longer emit indirect lighting. Has no effect when both Realtime and Baked Global Illumination are disabled.");
            public static GUIContent Cookie = new GUIContent("Cookie", "Specifies the Texture projected by the light. Spotlights require 2D texture and pointlights require texture cube.");

            public static GUIContent BakedShadowRadius = new GUIContent("Baked Shadow Radius", "Controls the amount of artificial softening applied to the edges of shadows cast by the Point or Spot light.");
            public static GUIContent BakedShadowAngle = new GUIContent("Baked Shadow Angle", "Controls the amount of artificial softening applied to the edges of shadows cast by directional lights.");

            public static GUIContent ShadowResolution = new GUIContent("Resolution", "Controls the rendered resolution of the shadow maps. A higher resolution will increase the fidelity of shadows at the cost of GPU performance and memory usage.");
            public static GUIContent ShadowBias = new GUIContent("Bias", "Controls the distance at which the shadows will be pushed away from the light. Useful for avoiding false self-shadowing artifacts.");
            public static GUIContent ShadowNormalBias = new GUIContent("Normal Bias", "Controls distance at which the shadow casting surfaces will be shrunk along the surface normal. Useful for avoiding false self-shadowing artifacts.");
            public static GUIContent ShadowNearPlane = new GUIContent("Near Plane", "Controls the value for the near clip plane when rendering shadows. Currently clamped to 0.1 units or 1% of the lights range property, whichever is lower.");


            public static GUIContent ShadowCascadeCount = new GUIContent("ShadowCascadeCount", "");
            public static GUIContent[] ShadowCascadeRatios = new GUIContent[6] { new GUIContent("Cascade 1"), new GUIContent("Cascade 2"), new GUIContent("Cascade 3"), new GUIContent("Cascade 4"), new GUIContent("Cascade 5"), new GUIContent("Cascade 6") };

            public static GUIContent AffectDiffuse = new GUIContent("AffectDiffuse", "This will disable diffuse lighting for this light. Doesn't save performance, diffuse lighting is still computed.");
            public static GUIContent AffectSpecular = new GUIContent("AffectSpecular", "This will disable specular lighting for this light. Doesn't save performance, specular lighting is still computed.");
            public static GUIContent FadeDistance = new GUIContent("FadeDistance", "The distance at which the light will smoothly fade before being culled to minimize popping.");
            public static GUIContent LightDimmer = new GUIContent("LightDimmer", "Aim to be used with script, timeline or animation. It allows dimming one or multiple lights of heterogeneous intensity easily (without needing to know the intensity of each light).");
            public static GUIContent ApplyRangeAttenuation = new GUIContent("ApplyRangeAttenuation", "Allows disabling range attenuation. This is useful indoor (like a room) to avoid having to setup a large range for a light to get correct inverse square attenuation that may leak out of the indoor");
            public static GUIContent ShadowFadeDistance = new GUIContent("ShadowFadeDistance", "The shadow will fade at distance ShadowFadeDistance before being culled to minimize popping.");
            public static GUIContent ShadowDimmer = new GUIContent("ShadowDimmer", "Aim to be use with script, timeline or animation. It allows dimming one or multiple shadows. This can also be used as an optimization to fit in shadow budget manually and minimize popping.");

            public static GUIContent DisabledLightWarning = new GUIContent("Lighting has been disabled in at least one Scene view. Any changes applied to lights in the Scene will not be updated in these views until Lighting has been enabled again.");
            public static GUIContent CookieWarning = new GUIContent("Cookie textures for spot lights must be set to clamp. Repeat is not supported.");
            public static GUIContent IndirectBounceShadowWarning = new GUIContent("Realtime indirect bounce shadowing is not supported for Spot and Point lights.");
            public static GUIContent BakingWarning = new GUIContent("Light mode is currently overridden to Realtime mode. Enable Baked Global Illumination to use Mixed or Baked light modes.");

            public static string lightShapeText = "LightShape";
            public static readonly string[] lightShapeNames = Enum.GetNames(typeof(LightShape));

            public static string lightmappingModeText = "Mode";
            public static readonly string[] lightmappingModeNames = Enum.GetNames(typeof(LightMappingType));
        }

        static Styles s_Styles;

        // Should match same colors in GizmoDrawers.cpp!
        static Color kGizmoLight = new Color(254 / 255f, 253 / 255f, 136 / 255f, 128 / 255f);
        static Color kGizmoDisabledLight = new Color(135 / 255f, 116 / 255f, 50 / 255f, 128 / 255f);

        private bool typeIsSame { get { return !m_Type.hasMultipleDifferentValues; } }
        private Texture cookie { get { return m_Cookie.objectReferenceValue as Texture; } }

        private bool lightmappingTypeIsSame { get { return !m_Lightmapping.hasMultipleDifferentValues; } }

        private bool isRealtime             { get { return m_Lightmapping.enumValueIndex == (int)LightMappingType.Realtime; } }

        private bool isBakedOrMixed { get { return !isRealtime; } }

        private bool bakingWarningValue { get { return !Lightmapping.bakedGI && lightmappingTypeIsSame && isBakedOrMixed; } }

        private bool cookieWarningValue
        {
            get
            {
                return typeIsSame && light.type == LightType.Spot &&
                    !m_Cookie.hasMultipleDifferentValues && cookie && cookie.wrapMode != TextureWrapMode.Clamp;
            }
        }
        private bool bounceWarningValue
        {
            get
            {
                return typeIsSame && (light.type == LightType.Point || light.type == LightType.Spot) &&
                    lightmappingTypeIsSame && isRealtime && !m_BounceIntensity.hasMultipleDifferentValues && m_BounceIntensity.floatValue > 0.0f && m_ShadowsType.enumValueIndex != (int)LightShadows.None;
            }
        }

        private void OnEnable()
        {
            m_Type = serializedObject.FindProperty("m_Type");
            m_Range = serializedObject.FindProperty("m_Range");
            m_SpotAngle = serializedObject.FindProperty("m_SpotAngle");
            m_CookieSize = serializedObject.FindProperty("m_CookieSize");
            m_Color = serializedObject.FindProperty("m_Color");
            m_Intensity = serializedObject.FindProperty("m_Intensity");
            m_BounceIntensity = serializedObject.FindProperty("m_BounceIntensity");
            m_ColorTemperature = serializedObject.FindProperty("m_ColorTemperature");
            m_UseColorTemperature = serializedObject.FindProperty("m_UseColorTemperature");
            m_Cookie = serializedObject.FindProperty("m_Cookie");
            m_ShadowsType = serializedObject.FindProperty("m_Shadows.m_Type");
            m_ShadowsStrength = serializedObject.FindProperty("m_Shadows.m_Strength");
            m_ShadowsResolution = serializedObject.FindProperty("m_Shadows.m_Resolution");
            m_ShadowsBias = serializedObject.FindProperty("m_Shadows.m_Bias");
            m_ShadowsNormalBias = serializedObject.FindProperty("m_Shadows.m_NormalBias");
            m_ShadowsNearPlane = serializedObject.FindProperty("m_Shadows.m_NearPlane");
            m_Flare = serializedObject.FindProperty("m_Flare");
            m_RenderMode = serializedObject.FindProperty("m_RenderMode");
            m_CullingMask = serializedObject.FindProperty("m_CullingMask");
            m_Lightmapping = serializedObject.FindProperty("m_Lightmapping");
            m_AreaSizeX = serializedObject.FindProperty("m_AreaSize.x");
            m_AreaSizeY = serializedObject.FindProperty("m_AreaSize.y");
            m_BakedShadowRadius = serializedObject.FindProperty("m_ShadowRadius");
            m_BakedShadowAngle = serializedObject.FindProperty("m_ShadowAngle");

            // Automatically add HD data if not present
            // We need to handle multiSelection. To do this we need to get the array of selection and assign it to additionalDataSerializedObject
            // additionalDataSerializedObject must be see as an array of selection in all following operation (this is transparent)

            var additionalDatas = this.targets.Select(t => (t as Component).GetComponent<HDAdditionalLightData>()).ToArray();
            var shadowDatas = this.targets.Select(t => (t as Component).GetComponent<AdditionalShadowData>()).ToArray();

            for (int i = 0; i < additionalDatas.Length; ++i)
            {
                if (additionalDatas[i] == null)
                {
                    additionalDatas[i] = Undo.AddComponent<HDAdditionalLightData>((targets[i] as Component).gameObject);
                }
            }

            for (int i = 0; i < shadowDatas.Length; ++i)
            {
                if (shadowDatas[i] == null)
                {
                    shadowDatas[i] = Undo.AddComponent<AdditionalShadowData>((targets[i] as Component).gameObject);
                }
            }

            additionalDataSerializedObject = new SerializedObject(additionalDatas);
            shadowDataSerializedObject = new SerializedObject(shadowDatas);

            // Additional data
            m_SpotInnerPercent = additionalDataSerializedObject.FindProperty("m_InnerSpotPercent");
            m_LightDimmer = additionalDataSerializedObject.FindProperty("lightDimmer");
            m_FadeDistance = additionalDataSerializedObject.FindProperty("fadeDistance");
            m_AffectDiffuse = additionalDataSerializedObject.FindProperty("affectDiffuse");
            m_AffectSpecular = additionalDataSerializedObject.FindProperty("affectSpecular");
            m_LightTypeExtent = additionalDataSerializedObject.FindProperty("lightTypeExtent");
            m_SpotLightShape = additionalDataSerializedObject.FindProperty("spotLightShape");
            m_ShapeLength = additionalDataSerializedObject.FindProperty("shapeLength");
            m_ShapeWidth = additionalDataSerializedObject.FindProperty("shapeWidth");
            m_ShapeRadius = additionalDataSerializedObject.FindProperty("shapeRadius");
            m_MaxSmoothness = additionalDataSerializedObject.FindProperty("maxSmoothness");
            m_ApplyRangeAttenuation = additionalDataSerializedObject.FindProperty("applyRangeAttenuation");

            // Editor only
            m_UseOldInspector = additionalDataSerializedObject.FindProperty("useOldInspector");
            m_ShowAdditionalSettings = additionalDataSerializedObject.FindProperty("showAdditionalSettings");

            // Shadow data
            m_ShadowDimmer = shadowDataSerializedObject.FindProperty("shadowDimmer");
            m_ShadowFadeDistance = shadowDataSerializedObject.FindProperty("shadowFadeDistance");
            m_ShadowCascadeCount = shadowDataSerializedObject.FindProperty("shadowCascadeCount");
            m_ShadowCascadeRatios = shadowDataSerializedObject.FindProperty("shadowCascadeRatios");
            m_ShadowCascadeBorders = shadowDataSerializedObject.FindProperty("shadowCascadeBorders");
            m_ShadowResolution = shadowDataSerializedObject.FindProperty("shadowResolution");
        }

        void ResolveLightShape()
        {
            // When we do multiple selection we must not avoid to chose a type, else it may corrupt light
            if (m_Type.hasMultipleDifferentValues)
            {
                m_LightShape = (LightShape)(-1);

                return;
            }

            if (m_LightTypeExtent.enumValueIndex == (int)LightTypeExtent.Punctual)
            {
                switch ((LightType)m_Type.enumValueIndex)
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
                switch ((LightTypeExtent)m_LightTypeExtent.enumValueIndex)
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

        void LigthShapeGUI()
        {
            m_LightShape = (LightShape)EditorGUILayout.Popup(Styles.lightShapeText, (int)m_LightShape, Styles.lightShapeNames);

            // LightShape is HD specific, it need to drive LightType from the original LightType when it make sense, so the GI is still in sync with the light shape
            switch (m_LightShape)
            {
                case LightShape.Directional:
                    m_Type.enumValueIndex = (int)LightType.Directional;
                    m_LightTypeExtent.enumValueIndex = (int)LightTypeExtent.Punctual;
                    break;

                case LightShape.Point:
                    m_Type.enumValueIndex = (int)LightType.Point;
                    m_LightTypeExtent.enumValueIndex = (int)LightTypeExtent.Punctual;
                    EditorGUILayout.PropertyField(m_MaxSmoothness, Styles.MaxSmoothness);
                    break;

                case LightShape.Spot:
                    m_Type.enumValueIndex = (int)LightType.Spot;
                    m_LightTypeExtent.enumValueIndex = (int)LightTypeExtent.Punctual;
                    EditorGUILayout.PropertyField(m_SpotLightShape, Styles.SpotLightShape);
                    //Cone Spot
                    if (m_SpotLightShape.enumValueIndex == (int)SpotLightShape.Cone)
                    {
                        EditorGUILayout.Slider(m_SpotAngle, 1f, 179f, Styles.SpotAngle);
                        EditorGUILayout.Slider(m_SpotInnerPercent, 0f, 100f, Styles.SpotInnerPercent);
                    }
                    // TODO : replace with angle and ratio
                    if (m_SpotLightShape.enumValueIndex == (int)SpotLightShape.Pyramid)
                    {
                        EditorGUILayout.Slider(m_ShapeLength, 0.01f, 10, Styles.ShapeLengthPyramid);
                        EditorGUILayout.Slider(m_ShapeWidth, 0.01f, 10, Styles.ShapeWidthPyramid);
                    }
                    if (m_SpotLightShape.enumValueIndex == (int)SpotLightShape.Box)
                    {
                        EditorGUILayout.PropertyField(m_ShapeLength, Styles.ShapeLengthBox);
                        EditorGUILayout.PropertyField(m_ShapeWidth, Styles.ShapeWidthBox);
                    }
                    EditorGUILayout.PropertyField(m_MaxSmoothness, Styles.MaxSmoothness);
                    break;

                case LightShape.Rectangle:
                    // TODO: Currently if we use Area type as it is offline light in legacy, the light will not exist at runtime
                    //m_Type.enumValueIndex = (int)LightType.Area;
                    m_Type.enumValueIndex = (int)LightType.Point;
                    m_LightTypeExtent.enumValueIndex = (int)LightTypeExtent.Rectangle;
                    EditorGUILayout.PropertyField(m_ShapeLength, Styles.ShapeLengthRect);
                    EditorGUILayout.PropertyField(m_ShapeWidth, Styles.ShapeWidthRect);
                    m_AreaSizeX.floatValue = m_ShapeLength.floatValue;
                    m_AreaSizeY.floatValue = m_ShapeWidth.floatValue;
                    m_ShadowsType.enumValueIndex = (int)LightShadows.None;
                    break;

                case LightShape.Line:
                    // TODO: Currently if we use Area type as it is offline light in legacy, the light will not exist at runtime
                    //m_Type.enumValueIndex = (int)LightType.Area;
                    m_Type.enumValueIndex = (int)LightType.Point;
                    m_LightTypeExtent.enumValueIndex = (int)LightTypeExtent.Line;
                    EditorGUILayout.PropertyField(m_ShapeLength, Styles.ShapeLengthLine);
                    // Fake line with a small rectangle in vanilla unity for GI
                    m_AreaSizeX.floatValue = m_ShapeLength.floatValue;
                    m_AreaSizeY.floatValue = 0.01f;
                    m_ShadowsType.enumValueIndex = (int)LightShadows.None;
                    break;

                case (LightShape)(-1):
                    // don't do anything, this is just to handle multi selection
                    break;

                default:
                    Debug.Assert(false, "Not implemented light type");
                    break;
            }
        }

        void LightGUI()
        {
            EditorGUILayout.PropertyField(m_Color, Styles.Color);
            EditorGUILayout.PropertyField(m_Intensity, Styles.Intensity);
            EditorGUILayout.PropertyField(m_BounceIntensity, Styles.BounceIntensity);
            // Indirect shadows warning (Should be removed when we support realtime indirect shadows)
            if (bounceWarningValue)
            {
                EditorGUILayout.HelpBox(Styles.IndirectBounceShadowWarning.text, MessageType.Info);
            }
            EditorGUILayout.PropertyField(m_Range, Styles.Range);

            // We need to overwrite the name of the default enum that doesn't make any sense
            // EditorGUILayout.PropertyField(m_Lightmapping, Styles.LightmappingMode);
            m_Lightmapping.enumValueIndex = EditorGUILayout.Popup(Styles.lightmappingModeText, (int)m_Lightmapping.enumValueIndex, Styles.lightmappingModeNames);

            // Warning if GI Baking disabled and m_Lightmapping isn't realtime
            if (bakingWarningValue)
            {
                EditorGUILayout.HelpBox(Styles.BakingWarning.text, MessageType.Info);
            }

            // no cookie with area light (maybe in future textured area light ?)
            if (!(m_LightShape == LightShape.Rectangle) && !(m_LightShape == LightShape.Line))
            {
                EditorGUILayout.PropertyField(m_Cookie, Styles.Cookie);

                // When directional light use a cookie, it can control the size
                if (m_LightShape == LightShape.Directional)
                {
                    EditorGUILayout.Slider(m_ShapeLength, 0.01f, 10, Styles.CookieSizeX);
                    EditorGUILayout.Slider(m_ShapeWidth, 0.01f, 10, Styles.CookieSizeY);
                }

                if (cookieWarningValue)
                {
                    // warn on spotlights if the cookie is set to repeat
                    EditorGUILayout.HelpBox(Styles.CookieWarning.text, MessageType.Warning);
                }
            }
        }

        void ShadowsGUI()
        {
            if (m_ShadowsType.enumValueIndex != (int)LightShadows.None)
            {
                if (m_Lightmapping.enumValueIndex == (int)LightMappingType.Baked)
                {
                    switch ((LightType)m_Type.enumValueIndex)
                    {
                        case LightType.Directional:
                            EditorGUILayout.PropertyField(m_BakedShadowAngle, Styles.BakedShadowAngle);
                            break;
                        case LightType.Spot:
                        case LightType.Point:
                            EditorGUILayout.PropertyField(m_BakedShadowRadius, Styles.BakedShadowRadius);
                            break;
                    }
                }
                else
                {
                    EditorGUILayout.PropertyField(m_ShadowResolution, Styles.ShadowResolution);
                    EditorGUILayout.Slider(m_ShadowsBias, 0.001f, 1, Styles.ShadowBias);
                    EditorGUILayout.Slider(m_ShadowsNormalBias, 0.001f, 1, Styles.ShadowNormalBias);
                    EditorGUILayout.Slider(m_ShadowsNearPlane, 0.01f, 10, Styles.ShadowNearPlane);
                }
            }
        }

        void ShadowsCascadeGUI()
        {
            UnityEditor.EditorGUI.BeginChangeCheck();
            EditorGUILayout.IntSlider(m_ShadowCascadeCount, 1, 4, Styles.ShadowCascadeCount);
            if (UnityEditor.EditorGUI.EndChangeCheck())
            {
                m_ShadowCascadeRatios.arraySize = m_ShadowCascadeCount.intValue - 1;
                m_ShadowCascadeBorders.arraySize = m_ShadowCascadeCount.intValue;
            }

            for (int i = 0; i < m_ShadowCascadeRatios.arraySize; i++)
            {
                UnityEditor.EditorGUILayout.Slider(m_ShadowCascadeRatios.GetArrayElementAtIndex(i), 0.0f, 1.0f, Styles.ShadowCascadeRatios[i]);
            }
        }

        void AdditionalSettingsGUI()
        {
            // Currently culling mask is not working with HD
            // EditorGUILayout.LabelField(new GUIContent("General"), EditorStyles.boldLabel);
            // EditorGUILayout.PropertyField(m_CullingMask);

            EditorGUILayout.LabelField(new GUIContent("Light"), EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(m_AffectDiffuse, Styles.AffectDiffuse);
            EditorGUILayout.PropertyField(m_AffectSpecular, Styles.AffectSpecular);
            EditorGUILayout.PropertyField(m_FadeDistance, Styles.FadeDistance);
            EditorGUILayout.PropertyField(m_LightDimmer, Styles.LightDimmer);
            EditorGUILayout.PropertyField(m_ApplyRangeAttenuation, Styles.ApplyRangeAttenuation);

            if (m_ShadowsType.enumValueIndex != (int)LightShadows.None && m_Lightmapping.enumValueIndex != (int)LightMappingType.Baked)
            {
                EditorGUILayout.LabelField(new GUIContent("Shadows"), EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(m_ShadowFadeDistance, Styles.ShadowFadeDistance);
                EditorGUILayout.PropertyField(m_ShadowDimmer, Styles.ShadowDimmer);
            }
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            // Sanity check
            if (additionalDataSerializedObject == null || shadowDataSerializedObject == null)
                return;

            additionalDataSerializedObject.Update();
            shadowDataSerializedObject.Update();

            ResolveLightShape();

            if (GUILayout.Button("Toggle light editor"))
            {
                m_UseOldInspector.boolValue = !m_UseOldInspector.boolValue;
            }

            EditorGUILayout.Space();

            if (m_UseOldInspector.boolValue)
            {
                base.DrawDefaultInspector();
                ApplyAdditionalComponentsVisibility(false);
                serializedObject.ApplyModifiedProperties();
                // It is required to save m_UseOldInspector value
                additionalDataSerializedObject.ApplyModifiedProperties();
                shadowDataSerializedObject.ApplyModifiedProperties(); // Should not be needed but if we do some change, could be, so let it here.
                return;
            }

            ApplyAdditionalComponentsVisibility(true);

            // Light features
            EditorGUI.indentLevel++;
            EditorGUILayout.LabelField(new GUIContent("Light features"), EditorStyles.boldLabel);

            // Do not display option for shadow if we are fully bake
            if (m_Lightmapping.enumValueIndex != (int)LightMappingType.Baked)
            {
                if (EditorGUILayout.Toggle(new GUIContent("Enable Shadow"), m_ShadowsType.enumValueIndex != 0))
                    m_ShadowsType.enumValueIndex = (int)LightShadows.Hard;
                else
                    m_ShadowsType.enumValueIndex = (int)LightShadows.None;
            }

            EditorGUI.indentLevel--;

            // Allow to display the arrow for reduce/expand correctly
            EditorGUI.indentLevel = 1;

            // LightShape
            EditorGUI.indentLevel--;
            EditorLightUtilities.DrawSplitter();
            m_Type.isExpanded = EditorLightUtilities.DrawHeaderFoldout("Shape", m_Type.isExpanded);
            EditorGUI.indentLevel++;

            if (m_Type.isExpanded)
            {
                LigthShapeGUI();
            }

            // Light
            EditorGUI.indentLevel--;
            EditorLightUtilities.DrawSplitter();
            m_Intensity.isExpanded = EditorLightUtilities.DrawHeaderFoldout("Light", m_Intensity.isExpanded);
            EditorGUI.indentLevel++;

            if (m_Intensity.isExpanded)
            {
                LightGUI();
            }

            // Shadows
            if (m_ShadowsType.enumValueIndex != (int)LightShadows.None)
            {
                EditorGUI.indentLevel--;
                EditorLightUtilities.DrawSplitter();
                m_ShadowsType.isExpanded = EditorLightUtilities.DrawHeaderFoldout("Shadow", m_ShadowsType.isExpanded);
                EditorGUI.indentLevel++;

                if (m_ShadowsType.isExpanded)
                {
                    ShadowsGUI();
                }

                // shadow cascade
                if (m_Type.enumValueIndex == (int)LightType.Directional && m_Lightmapping.enumValueIndex != (int)LightMappingType.Baked)
                {
                    EditorGUI.indentLevel--;
                    EditorLightUtilities.DrawSplitter();
                    m_ShadowCascadeCount.isExpanded = EditorLightUtilities.DrawHeaderFoldout("ShadowCascades", m_ShadowCascadeCount.isExpanded);
                    EditorGUI.indentLevel++;

                    if (m_ShadowCascadeCount.isExpanded)
                    {
                        ShadowsCascadeGUI();
                    }
                }
            }

            // AdditionalSettings
            EditorGUI.indentLevel--;
            EditorLightUtilities.DrawSplitter();
            m_ShowAdditionalSettings.boolValue = EditorLightUtilities.DrawHeaderFoldout("Additional Settings", m_ShowAdditionalSettings.boolValue);
            EditorGUI.indentLevel++;

            if (m_ShowAdditionalSettings.boolValue)
            {
                AdditionalSettingsGUI();
            }

            EditorGUI.indentLevel = 0; // Reset the value that we have init to 1

            if (SceneView.lastActiveSceneView != null && SceneView.lastActiveSceneView.m_SceneLighting == false)
            {
                EditorGUILayout.HelpBox(Styles.DisabledLightWarning.text, MessageType.Warning);
            }

            serializedObject.ApplyModifiedProperties();
            additionalDataSerializedObject.ApplyModifiedProperties();
            shadowDataSerializedObject.ApplyModifiedProperties();
        }

        void ApplyAdditionalComponentsVisibility(bool hide)
        {
            var additionalData = light.GetComponent<HDAdditionalLightData>();
            var shadowData = light.GetComponent<AdditionalShadowData>();

            additionalData.hideFlags = hide ? HideFlags.HideInInspector : HideFlags.None;
            shadowData.hideFlags = hide ? HideFlags.HideInInspector : HideFlags.None;
        }
    }
}
