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
        SerializedProperty m_LightShape;
        SerializedProperty m_SpotLightShape;
        SerializedProperty m_ShapeLength;
        SerializedProperty m_ShapeWidth;
        SerializedProperty m_ShapeRadius;        
        SerializedProperty m_MaxSmoothness;
        SerializedProperty m_ApplyRangeAttenuation;

        // Extra data for GUI only
        SerializedProperty m_UseOldInspector;
        SerializedProperty m_ShowAdditionalSettings;        
        SerializedProperty m_CastShadows;

        SerializedProperty m_ShadowDimmer;
        SerializedProperty m_ShadowFadeDistance;
        SerializedProperty m_ShadowCascadeCount;
        SerializedProperty m_ShadowCascadeRatios;
        SerializedProperty m_ShadowResolution;

        private Light light { get { return target as Light; } }

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
            m_LightShape = additionalDataSerializedObject.FindProperty("m_LightShape");
            m_SpotLightShape = additionalDataSerializedObject.FindProperty("spotLightShape");
            m_ShapeLength = additionalDataSerializedObject.FindProperty("shapeLength");
            m_ShapeWidth = additionalDataSerializedObject.FindProperty("shapeWidth");
            m_ShapeRadius = additionalDataSerializedObject.FindProperty("shapeRadius");
            m_MaxSmoothness = additionalDataSerializedObject.FindProperty("maxSmoothness");
            m_ApplyRangeAttenuation = additionalDataSerializedObject.FindProperty("applyRangeAttenuation");

            // Editor only
            m_UseOldInspector = additionalDataSerializedObject.FindProperty("useOldInspector");
            m_ShowAdditionalSettings = additionalDataSerializedObject.FindProperty("showAdditionalSettings");
            m_CastShadows = additionalDataSerializedObject.FindProperty("castShadows");

            // Shadow data
            m_ShadowDimmer = shadowDataSerializedObject.FindProperty("shadowDimmer");
            m_ShadowFadeDistance = shadowDataSerializedObject.FindProperty("shadowFadeDistance");
            m_ShadowCascadeCount = shadowDataSerializedObject.FindProperty("shadowCascadeCount");
            m_ShadowCascadeRatios = shadowDataSerializedObject.FindProperty("shadowCascadeRatios");
            m_ShadowResolution = shadowDataSerializedObject.FindProperty("shadowResolution");                        
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            // Sanity check
            if (additionalDataSerializedObject == null || shadowDataSerializedObject == null)
                return;

            additionalDataSerializedObject.Update();
            shadowDataSerializedObject.Update();

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
                shadowDataSerializedObject.ApplyModifiedProperties();
                return;
            }

            ApplyAdditionalComponentsVisibility(true);

            // Light features
            EditorGUI.indentLevel++;
            EditorGUILayout.LabelField(new GUIContent("Light features"), EditorStyles.boldLabel);
            EditorLightUtilities.DrawSplitter();
           // m_affectDiffuse.isExpanded = EditorLightUtilities.DrawHeaderFoldout("Light features", m_AffectDiffuse.isExpanded);
            EditorGUI.indentLevel = 1;

            //Resolve shadows
            m_CastShadows.boolValue = m_ShadowsType.enumValueIndex == 0 ? false : true;

            /*
            if (m_AffectDiffuse.isExpanded)
            {
                EditorGUILayout.PropertyField(m_AffectDiffuse, new GUIContent("Diffuse"), GUILayout.MaxWidth(EditorGUIUtility.labelWidth + 30));
                var AffectDiffuseRect = GUILayoutUtility.GetLastRect();
                var AffectSpecularRect = new Rect(EditorGUIUtility.labelWidth + 30, AffectDiffuseRect.y, EditorGUIUtility.labelWidth + 30, AffectDiffuseRect.height);
                EditorGUI.PropertyField(AffectSpecularRect, m_AffectSpecular, new GUIContent("Specular"));
                EditorGUILayout.PropertyField(m_CastShadows, new GUIContent("Shadows"));
            }
            */


            // LightShape
            EditorGUI.indentLevel--;
            EditorLightUtilities.DrawSplitter();
            m_Type.isExpanded = EditorLightUtilities.DrawHeaderFoldout("Shape", m_Type.isExpanded);
            EditorGUI.indentLevel++;

            if (m_Type.isExpanded)
            {
                EditorGUILayout.PropertyField(m_LightShape);

                // LightShape is HD specific, it need to drive Lighttype from the original lighttype when it make sense, so the GI is still in sync with the light shape
                switch ((LightShape)m_LightShape.enumValueIndex)
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
                        m_Type.enumValueIndex = (int)LightType.Area;
                        EditorGUILayout.PropertyField(m_ShapeLength);
                        EditorGUILayout.PropertyField(m_ShapeWidth);
                        m_AreaSizeX.floatValue = m_ShapeLength.floatValue;
                        m_AreaSizeY.floatValue = m_ShapeWidth.floatValue;
                        m_ShadowsType.enumValueIndex = 0; // No shadow
                        break;

                    case LightShape.Line:
                        m_Type.enumValueIndex = (int)LightType.Area;
                        EditorGUILayout.PropertyField(m_ShapeLength);
                        m_ShapeWidth.floatValue = 0;
                        // Fake line with a small rectangle in vanilla unity for GI
                        m_AreaSizeX.floatValue = m_ShapeLength.floatValue;
                        m_AreaSizeY.floatValue = 0.01f;
                        break;

                    default:
                        Debug.Assert(false, "Not implemented light type");
                        break;
                }
            }

            // Light
            EditorGUI.indentLevel--;
            EditorLightUtilities.DrawSplitter();
            m_Intensity.isExpanded = EditorLightUtilities.DrawHeaderFoldout("Light", m_Intensity.isExpanded);
            EditorGUI.indentLevel++;

            if (m_Intensity.isExpanded)
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
    
            // Shadows
            if (m_CastShadows.boolValue)
                m_ShadowsType.enumValueIndex = Mathf.Clamp(m_ShadowsType.enumValueIndex, 1, 2);
            else 
                m_ShadowsType.enumValueIndex = 0; // No shadow

            if (m_CastShadows.boolValue)
            {
                EditorGUI.indentLevel--;
                EditorLightUtilities.DrawSplitter();
                m_ShadowsType.isExpanded = EditorLightUtilities.DrawHeaderFoldout("Shadows", m_ShadowsType.isExpanded);
                EditorGUI.indentLevel++;

                if (m_ShadowsType.isExpanded)
                {
                    if (m_Lightmapping.enumValueIndex != 1)
                    {
                        EditorGUILayout.PropertyField(m_ShadowsType);
                        EditorGUILayout.PropertyField(m_ShadowResolution);
                        EditorGUILayout.Slider(m_ShadowsBias, 0.001f, 1);
                        EditorGUILayout.Slider(m_ShadowsNormalBias, 0.001f, 1);
                        EditorGUILayout.Slider(m_ShadowsNearPlane, 0.01f, 10);
                    }
                    if (m_Lightmapping.enumValueIndex != 2)
                    {
                        switch (m_Type.enumValueIndex)
                        {
                            case 1:
                                EditorGUILayout.PropertyField(m_BakedShadowAngle, new GUIContent("Bake shadow angle"));
                                break;
                            case 0:
                                EditorGUILayout.PropertyField(m_BakedShadowRadius, new GUIContent("Bake shadow radius"));
                                break;
                            case 2:
                                EditorGUILayout.PropertyField(m_BakedShadowRadius, new GUIContent("Bake shadow radius"));
                                break;
                        }
                    }
                }

            }

            // shadow cascade
            if (m_CastShadows.boolValue && m_Type.enumValueIndex == 1)
            {
                EditorGUI.indentLevel--;
                EditorLightUtilities.DrawSplitter();
                m_ShadowCascadeCount.isExpanded = EditorLightUtilities.DrawHeaderFoldout("ShadowCascades", m_ShadowCascadeCount.isExpanded);
                EditorGUI.indentLevel++;
                if (m_ShadowCascadeCount.isExpanded)
                {
                    EditorGUILayout.IntSlider(m_ShadowCascadeCount, 1, 4);
                }
            }

            // AdditionalSettings
            EditorGUI.indentLevel--;
            EditorLightUtilities.DrawSplitter();
            m_ShowAdditionalSettings.boolValue = EditorLightUtilities.DrawHeaderFoldout("Additional Settings", m_ShowAdditionalSettings.boolValue);
            EditorGUI.indentLevel++;
            if (m_ShowAdditionalSettings.boolValue)
            {
                EditorGUILayout.LabelField(new GUIContent("General"), EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(m_CullingMask);
                EditorGUILayout.PropertyField(m_AffectDiffuse);
                EditorGUILayout.PropertyField(m_AffectSpecular);
                EditorGUILayout.PropertyField(m_FadeDistance);

                EditorGUILayout.LabelField(new GUIContent("Shadows"), EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(m_ShadowFadeDistance);
            }

            EditorGUI.indentLevel--;

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
