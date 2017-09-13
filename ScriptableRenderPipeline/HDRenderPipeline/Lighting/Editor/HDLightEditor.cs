using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    [CustomEditor(typeof(Light))]
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

        SerializedProperty m_SpotInnerAngle;
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
        // This mean that m_Lightmapping.enumValueIndex is enum { Stationary=0, Static=1, Dynamic=2 }
        enum LightMappingType
        {
            Stationary = 0,
            Static=1, 
            Dynamic=2
        }

        class Styles
        {
            /*
            public readonly GUIContent Type = EditorGUIUtility.TextContent("Type|Specifies the current type of light. Possible types are Directional, Spot, Point, and Area lights.");
            public readonly GUIContent Range = EditorGUIUtility.TextContent("Range|Controls how far the light is emitted from the center of the object.");
            public readonly GUIContent SpotAngle = EditorGUIUtility.TextContent("Spot Angle|Controls the angle in degrees at the base of a Spot light's cone.");
            public readonly GUIContent Color = EditorGUIUtility.TextContent("Color|Controls the color being emitted by the light.");
            public readonly GUIContent UseColorTemperature = EditorGUIUtility.TextContent("Use color temperature mode|Cho0se between RGB and temperature mode for light's color.");
            public readonly GUIContent ColorFilter = EditorGUIUtility.TextContent("Filter|A colored gel can be put in front of the light source to tint the light.");
            public readonly GUIContent ColorTemperature = EditorGUIUtility.TextContent("Temperature|Also known as CCT (Correlated color temperature). The color temperature of the electromagnetic radiation emitted from an ideal black body is defined as its surface temperature in Kelvin. White is 6500K");
            public readonly GUIContent Intensity = EditorGUIUtility.TextContent("Intensity|Controls the brightness of the light. Light color is multiplied by this value.");
            public readonly GUIContent LightmappingMode = EditorGUIUtility.TextContent("Mode|Specifies the light mode used to determine if and how a light will be baked. Possible modes are Baked, Mixed, and Realtime.");
            public readonly GUIContent LightBounceIntensity = EditorGUIUtility.TextContent("Indirect Multiplier|Controls the intensity of indirect light being contributed to the scene. A value of 0 will cause Realtime lights to be removed from realtime global illumination and Baked and Mixed lights to no longer emit indirect lighting. Has no effect when both Realtime and Baked Global Illumination are disabled.");
            public readonly GUIContent ShadowType = EditorGUIUtility.TextContent("Shadow Type|Specifies whether Hard Shadows, Soft Shadows, or No Shadows will be cast by the light.");
            //realtime
            public readonly GUIContent ShadowRealtimeSettings = EditorGUIUtility.TextContent("Realtime Shadows|Settings for realtime direct shadows.");
            public readonly GUIContent ShadowStrength = EditorGUIUtility.TextContent("Strength|Controls how dark the shadows cast by the light will be.");
            public readonly GUIContent ShadowResolution = EditorGUIUtility.TextContent("Resolution|Controls the rendered resolution of the shadow maps. A higher resolution will increase the fidelity of shadows at the cost of GPU performance and memory usage.");
            public readonly GUIContent ShadowBias = EditorGUIUtility.TextContent("Bias|Controls the distance at which the shadows will be pushed away from the light. Useful for avoiding false self-shadowing artifacts.");
            public readonly GUIContent ShadowNormalBias = EditorGUIUtility.TextContent("Normal Bias|Controls distance at which the shadow casting surfaces will be shrunk along the surface normal. Useful for avoiding false self-shadowing artifacts.");
            public readonly GUIContent ShadowNearPlane = EditorGUIUtility.TextContent("Near Plane|Controls the value for the near clip plane when rendering shadows. Currently clamped to 0.1 units or 1% of the lights range property, whichever is lower.");
            //baked
            public readonly GUIContent BakedShadowRadius = EditorGUIUtility.TextContent("Baked Shadow Radius|Controls the amount of artificial softening applied to the edges of shadows cast by the Point or Spot light.");
            public readonly GUIContent BakedShadowAngle = EditorGUIUtility.TextContent("Baked Shadow Angle|Controls the amount of artificial softening applied to the edges of shadows cast by directional lights.");

            public readonly GUIContent Cookie = EditorGUIUtility.TextContent("Cookie|Specifies the Texture mask to cast shadows, create silhouettes, or patterned illumination for the light.");
            public readonly GUIContent CookieSize = EditorGUIUtility.TextContent("Cookie Size|Controls the size of the cookie mask currently assigned to the light.");
            public readonly GUIContent DrawHalo = EditorGUIUtility.TextContent("Draw Halo|When enabled, draws a spherical halo of light with a radius equal to the lights range value.");
            public readonly GUIContent Flare = EditorGUIUtility.TextContent("Flare|Specifies the flare object to be used by the light to render lens flares in the scene.");
            public readonly GUIContent RenderMode = EditorGUIUtility.TextContent("Render Mode|Specifies the importance of the light which impacts lighting fidelity and performance. Options are Auto, Important, and Not Important. This only affects Forward Rendering");
            public readonly GUIContent CullingMask = EditorGUIUtility.TextContent("Culling Mask|Specifies which layers will be affected or excluded from the light's effect on objects in the scene.");

            public readonly GUIContent iconRemove = EditorGUIUtility.IconContent("Toolbar Minus", "Remove command buffer");
            public readonly GUIStyle invisibleButton = "InvisibleButton";

            public readonly GUIContent AreaWidth = EditorGUIUtility.TextContent("Width|Controls the width in units of the area light.");
            public readonly GUIContent AreaHeight = EditorGUIUtility.TextContent("Height|Controls the height in units of the area light.");

            public readonly GUIContent BakingWarning = EditorGUIUtility.TextContent("Light mode is currently overridden to Realtime mode. Enable Baked Global Illumination to use Mixed or Baked light modes.");
            public readonly GUIContent IndirectBounceShadowWarning = EditorGUIUtility.TextContent("Realtime indirect bounce shadowing is not supported for Spot and Point lights.");
            public readonly GUIContent CookieWarning = EditorGUIUtility.TextContent("Cookie textures for spot lights should be set to clamp, not repeat, to avoid artifacts.");
            public readonly GUIContent DisabledLightWarning = EditorGUIUtility.TextContent("Lighting has been disabled in at least one Scene view. Any changes applied to lights in the Scene will not be updated in these views until Lighting has been enabled again.");
            */

            public static string lightShapeText = "LightShape";
            public static readonly string[] lightShapeNames = Enum.GetNames(typeof(LightShape));
        }

        static Styles s_Styles;

        // Should match same colors in GizmoDrawers.cpp!
        static Color kGizmoLight = new Color(254 / 255f, 253 / 255f, 136 / 255f, 128 / 255f);
        static Color kGizmoDisabledLight = new Color(135 / 255f, 116 / 255f, 50 / 255f, 128 / 255f);

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
            var additionalData = light.GetComponent<HDAdditionalLightData>();
            var shadowData = light.GetComponent<AdditionalShadowData>();

            if (additionalData == null || shadowData == null)
            {
                AddAdditionalComponents(additionalData, shadowData);
            }

            additionalDataSerializedObject = new SerializedObject(additionalData);
            shadowDataSerializedObject = new SerializedObject(shadowData);

            // Additional data
            m_SpotInnerAngle = additionalDataSerializedObject.FindProperty("m_InnerSpotPercent");
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
            m_ShadowCascadeBorders = serializedObject.FindProperty("shadowCascadeBorders");
            m_ShadowResolution = shadowDataSerializedObject.FindProperty("shadowResolution");                        
        }

        void ResolveLightShape()
        {
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
                    break;

                case LightShape.Point:
                    m_Type.enumValueIndex = (int)LightType.Point;
                    EditorGUILayout.PropertyField(m_MaxSmoothness);
                    break;

                case LightShape.Spot:
                    m_Type.enumValueIndex = (int)LightType.Spot;
                    EditorGUILayout.PropertyField(m_SpotLightShape);
                    //Cone Spot
                    if (m_SpotLightShape.enumValueIndex == (int)SpotLightShape.Cone)
                    {
                        EditorGUILayout.Slider(m_SpotAngle, 1f, 179f);
                        EditorGUILayout.Slider(m_SpotInnerAngle, 0f, 100f);
                    }
                    // TODO : replace with angle and ratio
                    if (m_SpotLightShape.enumValueIndex == (int)SpotLightShape.Pyramid)
                    {
                        EditorGUILayout.Slider(m_ShapeLength, 0.01f, 10);
                        EditorGUILayout.Slider(m_ShapeWidth, 0.01f, 10);
                    }
                    if (m_SpotLightShape.enumValueIndex == (int)SpotLightShape.Box)
                    {
                        EditorGUILayout.PropertyField(m_ShapeLength);
                        EditorGUILayout.PropertyField(m_ShapeWidth);
                    }
                    EditorGUILayout.PropertyField(m_MaxSmoothness);
                    break;

                case LightShape.Rectangle:
                    // TODO: Currently if we use Area type as it is offline light in legacy, the light will not exist at runtime
                    //m_Type.enumValueIndex = (int)LightType.Area;
                    m_Type.enumValueIndex = (int)LightType.Point;
                    m_LightTypeExtent.enumValueIndex = (int)LightTypeExtent.Rectangle;
                    EditorGUILayout.PropertyField(m_ShapeLength);
                    EditorGUILayout.PropertyField(m_ShapeWidth);
                    m_AreaSizeX.floatValue = m_ShapeLength.floatValue;
                    m_AreaSizeY.floatValue = m_ShapeWidth.floatValue;
                    m_ShadowsType.enumValueIndex = (int)LightShadows.None;
                    break;

                case LightShape.Line:
                    // TODO: Currently if we use Area type as it is offline light in legacy, the light will not exist at runtime
                    //m_Type.enumValueIndex = (int)LightType.Area;
                    m_Type.enumValueIndex = (int)LightType.Point;
                    m_LightTypeExtent.enumValueIndex = (int)LightTypeExtent.Line;
                    EditorGUILayout.PropertyField(m_ShapeLength);
                    // Fake line with a small rectangle in vanilla unity for GI
                    m_AreaSizeX.floatValue = m_ShapeLength.floatValue;
                    m_AreaSizeY.floatValue = 0.01f;
                    break;

                default:
                    Debug.Assert(false, "Not implemented light type");
                    break;
            }
        }

        void LightGUI()
        {
            EditorGUILayout.PropertyField(m_Color);
            EditorGUILayout.PropertyField(m_Intensity);
            EditorGUILayout.PropertyField(m_BounceIntensity);
            EditorGUILayout.PropertyField(m_Range);
            EditorGUILayout.PropertyField(m_Lightmapping);
            EditorGUILayout.PropertyField(m_Cookie);
            if (m_Cookie.objectReferenceValue != null && m_Type.enumValueIndex == 1)
            {
                EditorGUILayout.PropertyField(m_CookieSize);
                m_ShapeLength.floatValue = m_CookieSize.floatValue;
                m_ShapeWidth.floatValue = m_CookieSize.floatValue;
            }
        }

        void ShadowsGUI()
        {
            if (m_ShadowsType.enumValueIndex != (int)LightShadows.None)
            {
                if (m_Lightmapping.enumValueIndex == (int)LightMappingType.Static)
                {
                    switch ((LightType)m_Type.enumValueIndex)
                    {
                        case LightType.Directional:
                            EditorGUILayout.PropertyField(m_BakedShadowAngle, new GUIContent("Bake shadow angle"));
                            break;
                        case LightType.Spot:
                        case LightType.Point:
                            EditorGUILayout.PropertyField(m_BakedShadowRadius, new GUIContent("Bake shadow radius"));
                            break;
                    }
                }
                else 
                {
                    EditorGUILayout.PropertyField(m_ShadowResolution);
                    EditorGUILayout.Slider(m_ShadowsBias, 0.001f, 1);
                    EditorGUILayout.Slider(m_ShadowsNormalBias, 0.001f, 1);
                    EditorGUILayout.Slider(m_ShadowsNearPlane, 0.01f, 10);
                }
            }
        }

        void ShadowsCascadeGUI()
        {
            UnityEditor.EditorGUI.BeginChangeCheck();
            EditorGUILayout.IntSlider(m_ShadowCascadeCount, 1, 4);
            if (UnityEditor.EditorGUI.EndChangeCheck())
            {
                m_ShadowCascadeRatios.arraySize = m_ShadowCascadeCount.intValue - 1;
                m_ShadowCascadeBorders.arraySize = m_ShadowCascadeCount.intValue;
            }

            for (int i = 0; i < m_ShadowCascadeRatios.arraySize; i++)
            {
                UnityEditor.EditorGUILayout.Slider(m_ShadowCascadeRatios.GetArrayElementAtIndex(i), 0.0f, 1.0f, new GUIContent("Cascade " + i));
            }
        }

        void AdditionalSettingsGUI()
        {
            // Currently culling mask is not working with HD
            // EditorGUILayout.LabelField(new GUIContent("General"), EditorStyles.boldLabel);
            // EditorGUILayout.PropertyField(m_CullingMask);

            EditorGUILayout.LabelField(new GUIContent("Light"), EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(m_AffectDiffuse);
            EditorGUILayout.PropertyField(m_AffectSpecular);
            EditorGUILayout.PropertyField(m_FadeDistance);
            EditorGUILayout.PropertyField(m_LightDimmer);
            EditorGUILayout.PropertyField(m_ApplyRangeAttenuation);

            if (m_ShadowsType.enumValueIndex != (int)LightShadows.None && m_Lightmapping.enumValueIndex != (int)LightMappingType.Static)
            {
                EditorGUILayout.LabelField(new GUIContent("Shadows"), EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(m_ShadowFadeDistance);
                EditorGUILayout.PropertyField(m_ShadowDimmer);
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

            var additionalData = light.GetComponent<HDAdditionalLightData>();
            var shadowData = light.GetComponent<AdditionalShadowData>();

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
            if (m_Lightmapping.enumValueIndex != (int)LightMappingType.Static)
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
                if (m_Type.enumValueIndex == (int)LightType.Directional && m_Lightmapping.enumValueIndex != (int)LightMappingType.Static)
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

            serializedObject.ApplyModifiedProperties();
            additionalDataSerializedObject.ApplyModifiedProperties();
            shadowDataSerializedObject.ApplyModifiedProperties();
        }

        void AddAdditionalComponents(HDAdditionalLightData additionalData, AdditionalShadowData shadowData)
        {
            if (additionalData == null)
                additionalData = light.gameObject.AddComponent<HDAdditionalLightData>();

            if (shadowData == null)
                shadowData = light.gameObject.AddComponent<AdditionalShadowData>();
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
