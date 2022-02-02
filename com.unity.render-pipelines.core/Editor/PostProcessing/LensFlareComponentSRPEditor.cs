using UnityEngine;
using UnityEngine.Rendering;

namespace UnityEditor.Rendering
{
    /// <summary>
    /// Editor for LensFlareComponentSRP: Lens Flare Data-Driven which can be added on any GameObject
    /// </summary>
    [CanEditMultipleObjects]
    [CustomEditorForRenderPipeline(typeof(LensFlareComponentSRP), typeof(UnityEngine.Rendering.RenderPipelineAsset))]
    class LensFlareComponentSRPEditor : Editor
    {
        SerializedProperty m_LensFlareData;
        SerializedProperty m_Intensity;
        SerializedProperty m_Scale;
        SerializedProperty m_MaxAttenuationDistance;
        SerializedProperty m_MaxAttenuationScale;
        SerializedProperty m_DistanceAttenuationCurve;
        SerializedProperty m_ScaleByDistanceCurve;
        SerializedProperty m_AttenuationByLightShape;
        SerializedProperty m_RadialScreenAttenuationCurve;
        SerializedProperty m_UseOcclusion;
        SerializedProperty m_OcclusionRadius;
        SerializedProperty m_SamplesCount;
        SerializedProperty m_OcclusionOffset;
        SerializedProperty m_AllowOffScreen;

        void OnEnable()
        {
            PropertyFetcher<LensFlareComponentSRP> entryPoint = new PropertyFetcher<LensFlareComponentSRP>(serializedObject);
            m_LensFlareData = entryPoint.Find("m_LensFlareData");
            m_Intensity = entryPoint.Find(x => x.intensity);
            m_Scale = entryPoint.Find(x => x.scale);
            m_MaxAttenuationDistance = entryPoint.Find(x => x.maxAttenuationDistance);
            m_DistanceAttenuationCurve = entryPoint.Find(x => x.distanceAttenuationCurve);
            m_MaxAttenuationScale = entryPoint.Find(x => x.maxAttenuationScale);
            m_ScaleByDistanceCurve = entryPoint.Find(x => x.scaleByDistanceCurve);
            m_AttenuationByLightShape = entryPoint.Find(x => x.attenuationByLightShape);
            m_RadialScreenAttenuationCurve = entryPoint.Find(x => x.radialScreenAttenuationCurve);
            m_UseOcclusion = entryPoint.Find(x => x.useOcclusion);
            m_OcclusionRadius = entryPoint.Find(x => x.occlusionRadius);
            m_SamplesCount = entryPoint.Find(x => x.sampleCount);
            m_OcclusionOffset = entryPoint.Find(x => x.occlusionOffset);
            m_AllowOffScreen = entryPoint.Find(x => x.allowOffScreen);
        }

        /// <summary>
        /// Implement this function to make a custom inspector
        /// </summary>
        public override void OnInspectorGUI()
        {
            LensFlareComponentSRP lensFlareData = m_Intensity.serializedObject.targetObject as LensFlareComponentSRP;
            bool attachedToLight = false;
            bool lightIsDirLight = false;
            Light light = null;
            if (lensFlareData != null &&
                (light = lensFlareData.GetComponent<Light>()) != null)
            {
                attachedToLight = true;
                if (light.type == LightType.Directional)
                    lightIsDirLight = true;
            }

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.LabelField(Styles.generalData.text, EditorStyles.boldLabel);
            {
                EditorGUILayout.PropertyField(m_LensFlareData, Styles.lensFlareData);
                EditorGUILayout.PropertyField(m_Intensity, Styles.intensity);
                EditorGUILayout.PropertyField(m_Scale, Styles.scale);
                if (!lightIsDirLight)
                {
                    if (attachedToLight)
                        EditorGUILayout.PropertyField(m_AttenuationByLightShape, Styles.attenuationByLightShape);
                    EditorGUILayout.PropertyField(m_MaxAttenuationDistance, Styles.maxAttenuationDistance);
                    ++EditorGUI.indentLevel;
                    EditorGUILayout.PropertyField(m_DistanceAttenuationCurve, Styles.distanceAttenuationCurve);
                    --EditorGUI.indentLevel;
                    EditorGUILayout.PropertyField(m_MaxAttenuationScale, Styles.maxAttenuationScale);
                    ++EditorGUI.indentLevel;
                    EditorGUILayout.PropertyField(m_ScaleByDistanceCurve, Styles.scaleByDistanceCurve);
                    --EditorGUI.indentLevel;
                }
                EditorGUILayout.PropertyField(m_RadialScreenAttenuationCurve, Styles.radialScreenAttenuationCurve);
            }
            EditorGUILayout.LabelField(Styles.occlusionData.text, EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(m_UseOcclusion, Styles.enableOcclusion);
            if (m_UseOcclusion.boolValue)
            {
                EditorGUILayout.PropertyField(m_OcclusionRadius, Styles.occlusionRadius);
                ++EditorGUI.indentLevel;
                EditorGUILayout.PropertyField(m_SamplesCount, Styles.sampleCount);
                --EditorGUI.indentLevel;
                EditorGUILayout.PropertyField(m_OcclusionOffset, Styles.occlusionOffset);
            }
            EditorGUILayout.PropertyField(m_AllowOffScreen, Styles.allowOffScreen);

            if (EditorGUI.EndChangeCheck())
            {
                m_LensFlareData.serializedObject.ApplyModifiedProperties();
            }
        }

        static class Styles
        {
            static public readonly GUIContent generalData = EditorGUIUtility.TrTextContent("General");
            static public readonly GUIContent occlusionData = EditorGUIUtility.TrTextContent("Occlusion");

            static public readonly GUIContent lensFlareData = EditorGUIUtility.TrTextContent("Lens Flare Data", "Specifies the SRP Lens Flare Data asset this component uses.");
            static public readonly GUIContent intensity = EditorGUIUtility.TrTextContent("Intensity", "Sets the intensity of the lens flare.");
            static public readonly GUIContent scale = EditorGUIUtility.TrTextContent("Scale", "Sets the scale of the lens flare.");
            static public readonly GUIContent maxAttenuationDistance = EditorGUIUtility.TrTextContent("Attenuation Distance", "Sets the distance, in meters, between the start and the end of the Distance Attenuation Curve.");
            static public readonly GUIContent distanceAttenuationCurve = EditorGUIUtility.TrTextContent("Attenuation Distance Curve", "Specifies the curve that reduces the effect of the lens flare  based on the distance between the GameObject this asset is attached to and the Camera.");
            static public readonly GUIContent maxAttenuationScale = EditorGUIUtility.TrTextContent("Scale Distance", "Sets the distance, in meters, between the start and the end of the Scale Attenuation Curve.");
            static public readonly GUIContent scaleByDistanceCurve = EditorGUIUtility.TrTextContent("Scale Distance Curve", "Specifies the curve used to calculate the size of the lens flare based on the distance between the GameObject this asset is attached to, and the Camera.");
            static public readonly GUIContent attenuationByLightShape = EditorGUIUtility.TrTextContent("Attenuation By Light Shape", "When enabled, if the component is attached to a light, automatically reduces the effect of the lens flare based on the type and shape of the light.");
            static public readonly GUIContent radialScreenAttenuationCurve = EditorGUIUtility.TrTextContent("Screen Attenuation Curve", "Specifies the curve that modifies the intensity of the lens flare based on its distance from the edge of the screen.");
            static public readonly GUIContent enableOcclusion = EditorGUIUtility.TrTextContent("Enable", "When enabled, the renderer uses the depth buffer to occlude (partially or completely) the lens flare. Partial occlusion also occurs when the lens flare is partially offscreen.");
            static public readonly GUIContent occlusionRadius = EditorGUIUtility.TrTextContent("Occlusion Radius", "Sets the radius, in meters, around the light used to compute the occlusion of the lens flare. If this area is half occluded by geometry (or half off-screen), the intensity of the lens flare is cut by half.");
            static public readonly GUIContent sampleCount = EditorGUIUtility.TrTextContent("Sample Count", "Sets the number of random samples used inside the Occlusion Radius area. A higher sample count gives a smoother attenuation when occluded.");
            static public readonly GUIContent occlusionOffset = EditorGUIUtility.TrTextContent("Occlusion Offset", "Sets the offset of the occlusion area in meters between the GameObject this asset is attached to, and the Camera. A positive value moves the occlusion area closer to the Camera.");
            static public readonly GUIContent allowOffScreen = EditorGUIUtility.TrTextContent("Allow Off Screen", "When enabled, allows the lens flare to affect the scene even when it is outside the Camera's field of view.");
        }
    }
}
