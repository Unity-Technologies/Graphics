using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using static UnityEditor.EditorGUI;

namespace UnityEditor.Rendering.HighDefinition
{
    [CustomEditor(typeof(WaterSurface))]
    sealed class WaterSurfaceEditor : Editor
    {
        // Geometry parameters
        SerializedProperty m_Infinite;
        SerializedProperty m_GeometryType;
        SerializedProperty m_Geometry;
        SerializedProperty m_EarthRadius;

        // Simulation parameters
        SerializedProperty m_HighBandCount;
        SerializedProperty m_WaterMaxPatchSize;
        SerializedProperty m_WaveAmplitude;
        SerializedProperty m_Choppiness;
        SerializedProperty m_TimeMultiplier;

        // Rendering parameters
        SerializedProperty m_Material;
        SerializedProperty m_WaterSmoothness;

        // Refraction parameters
        SerializedProperty m_MaxRefractionDistance;
        SerializedProperty m_MaxAbsorptionDistance;
        SerializedProperty m_TransparentColor;

        // Scattering parameters
        SerializedProperty m_ScatteringColor;
        SerializedProperty m_ScatteringFactor;
        SerializedProperty m_HeightScattering;
        SerializedProperty m_DisplacementScattering;
        SerializedProperty m_DirectLightTipScattering;
        SerializedProperty m_DirectLightBodyScattering;

        // Caustic parameters
        SerializedProperty m_CausticsIntensity;
        SerializedProperty m_CausticsTiling;
        SerializedProperty m_CausticsSpeed;
        SerializedProperty m_CausticsPlaneOffset;

        // Water masking
        SerializedProperty m_WaterMask;
        SerializedProperty m_WaterMaskExtent;
        SerializedProperty m_WaterMaskOffset;

        // Foam
        SerializedProperty m_SurfaceFoamSmoothness;
        SerializedProperty m_SurfaceFoamIntensity;
        SerializedProperty m_SurfaceFoamAmount;
        SerializedProperty m_SurfaceFoamTiling;
        SerializedProperty m_DeepFoam;
        SerializedProperty m_DeepFoamColor;
        SerializedProperty m_FoamMask;
        SerializedProperty m_FoamMaskExtent;
        SerializedProperty m_FoamMaskOffset;

        // Wind
        SerializedProperty m_WindOrientation;
        SerializedProperty m_WindSpeed;
        SerializedProperty m_WindAffectCurrent;
        SerializedProperty m_WindFoamCurve;

        void OnEnable()
        {
            var o = new PropertyFetcher<WaterSurface>(serializedObject);

            // Geometry parameters
            m_Infinite = o.Find(x => x.infinite);
            m_GeometryType = o.Find(x => x.geometryType);
            m_Geometry = o.Find(x => x.geometry);
            m_EarthRadius = o.Find(x => x.earthRadius);

            // Band definition parameters
            m_HighBandCount = o.Find(x => x.highBandCount);
            m_WaterMaxPatchSize = o.Find(x => x.waterMaxPatchSize);
            m_WaveAmplitude = o.Find(x => x.waveAmplitude);
            m_Choppiness = o.Find(x => x.choppiness);
            m_TimeMultiplier = o.Find(x => x.timeMultiplier);

            // Rendering parameters
            m_Material = o.Find(x => x.material);
            m_WaterSmoothness = o.Find(x => x.waterSmoothness);

            // Refraction parameters
            m_MaxAbsorptionDistance = o.Find(x => x.maxAbsorptionDistance);
            m_MaxRefractionDistance = o.Find(x => x.maxRefractionDistance);
            m_TransparentColor = o.Find(x => x.transparentColor);

            // Scattering parameters
            m_ScatteringColor = o.Find(x => x.scatteringColor);
            m_ScatteringFactor = o.Find(x => x.scatteringFactor);
            m_HeightScattering = o.Find(x => x.heightScattering);
            m_DisplacementScattering = o.Find(x => x.displacementScattering);
            m_DirectLightTipScattering = o.Find(x => x.directLightTipScattering);
            m_DirectLightBodyScattering = o.Find(x => x.directLightBodyScattering);

            // Caustic parameters
            m_CausticsIntensity = o.Find(x => x.causticsIntensity);
            m_CausticsTiling = o.Find(x => x.causticsTiling);
            m_CausticsSpeed = o.Find(x => x.causticsSpeed);
            m_CausticsPlaneOffset = o.Find(x => x.causticsPlaneOffset);

            // Foam
            m_SurfaceFoamSmoothness = o.Find(x => x.surfaceFoamSmoothness);
            m_SurfaceFoamIntensity = o.Find(x => x.surfaceFoamIntensity);
            m_SurfaceFoamAmount = o.Find(x => x.surfaceFoamAmount);
            m_SurfaceFoamTiling = o.Find(x => x.surfaceFoamTiling);
            m_DeepFoam = o.Find(x => x.deepFoam);
            m_DeepFoamColor = o.Find(x => x.deepFoamColor);
            m_FoamMask = o.Find(x => x.foamMask);
            m_FoamMaskExtent = o.Find(x => x.foamMaskExtent);
            m_FoamMaskOffset = o.Find(x => x.foamMaskOffset);

            // Water masking
            m_WaterMask = o.Find(x => x.waterMask);
            m_WaterMaskExtent = o.Find(x => x.waterMaskExtent);
            m_WaterMaskOffset = o.Find(x => x.waterMaskOffset);

            // Wind parameters
            m_WindOrientation = o.Find(x => x.windOrientation);
            m_WindSpeed = o.Find(x => x.windSpeed);
            m_WindFoamCurve = o.Find(x => x.windFoamCurve);
            m_WindAffectCurrent = o.Find(x => x.windAffectCurrent);
        }

        void SanitizeVector4(SerializedProperty property, float minValue, float maxValue)
        {
            Vector4 vec4 = property.vector4Value;
            vec4.x = Mathf.Clamp(vec4.x, minValue, maxValue);
            vec4.y = Mathf.Clamp(vec4.y, minValue, maxValue);
            vec4.z = Mathf.Clamp(vec4.z, minValue, maxValue);
            vec4.w = Mathf.Clamp(vec4.w, minValue, maxValue);
            property.vector4Value = vec4;
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            HDRenderPipelineAsset currentAsset = HDRenderPipeline.currentAsset;
            if (!currentAsset?.currentPlatformRenderPipelineSettings.supportWater ?? false)
            {
                EditorGUILayout.Space();
                EditorGUILayout.HelpBox("The current HDRP Asset does not support Water Surfaces.", MessageType.Error, wide: true);
                return;
            }

            EditorGUILayout.LabelField("Geometry", EditorStyles.boldLabel);
            using (new IndentLevelScope())
            {
                EditorGUILayout.PropertyField(m_Infinite);
                using (new IndentLevelScope())
                {
                    if (!m_Infinite.boolValue)
                    {
                        EditorGUILayout.PropertyField(m_GeometryType);
                        if ((WaterSurface.WaterGeometryType)m_GeometryType.enumValueIndex == WaterSurface.WaterGeometryType.Custom)
                            EditorGUILayout.PropertyField(m_Geometry);
                    }
                    else
                    {
                        EditorGUILayout.PropertyField(m_EarthRadius);
                        m_EarthRadius.floatValue = Mathf.Max(m_EarthRadius.floatValue, 500.0f);
                    }
                }
            }
            EditorGUILayout.LabelField("Simulation", EditorStyles.boldLabel);
            using (new IndentLevelScope())
            {
                EditorGUILayout.PropertyField(m_WaterMaxPatchSize);
                m_WaterMaxPatchSize.floatValue = Mathf.Clamp(m_WaterMaxPatchSize.floatValue, 5.0f, 10000.0f);

                EditorGUILayout.PropertyField(m_HighBandCount);
                using (new IndentLevelScope())
                {
                    if (m_HighBandCount.boolValue)
                    {
                        EditorGUI.BeginChangeCheck();
                        m_WaveAmplitude.vector4Value = EditorGUILayout.Vector4Field("Amplitude", m_WaveAmplitude.vector4Value);
                        if (EditorGUI.EndChangeCheck())
                            SanitizeVector4(m_WaveAmplitude, 0.0f, 1.0f);
                    }
                    else
                    {
                        EditorGUI.BeginChangeCheck();
                        Vector2 amplitude2D = new Vector2(m_WaveAmplitude.vector4Value.x, m_WaveAmplitude.vector4Value.y);
                        amplitude2D = EditorGUILayout.Vector2Field("Amplitude", amplitude2D);
                        m_WaveAmplitude.vector4Value = new Vector4(amplitude2D.x, amplitude2D.y, m_WaveAmplitude.vector4Value.z, m_WaveAmplitude.vector4Value.w);
                        if (EditorGUI.EndChangeCheck())
                            SanitizeVector4(m_WaveAmplitude, 0.0f, 1.0f);
                    }
                }

                m_Choppiness.floatValue = EditorGUILayout.Slider("Choppiness", m_Choppiness.floatValue, 1.0f, 3.0f);
                m_TimeMultiplier.floatValue = EditorGUILayout.Slider("Time Multiplier", m_TimeMultiplier.floatValue, 0.0f, 10.0f);
            }

            EditorGUILayout.LabelField("Material", EditorStyles.boldLabel);
            using (new IndentLevelScope())
            {
                EditorGUILayout.PropertyField(m_Material);
                // Water Smoothness from 0.0f to 0.99ff
                m_WaterSmoothness.floatValue = EditorGUILayout.Slider("Water Smoothness", m_WaterSmoothness.floatValue, 0.0f, 0.99f);
            }

            EditorGUILayout.LabelField("Refraction", EditorStyles.boldLabel);
            using (new IndentLevelScope())
            {
                EditorGUILayout.PropertyField(m_TransparentColor);
                m_MaxRefractionDistance.floatValue = EditorGUILayout.Slider("Max Refraction Distance", m_MaxRefractionDistance.floatValue, 0.0f, 3.5f);
                m_MaxAbsorptionDistance.floatValue = EditorGUILayout.Slider("Max Absorption Distance", m_MaxAbsorptionDistance.floatValue, 0.0f, 100.0f);
            }

            EditorGUILayout.LabelField("Scattering", EditorStyles.boldLabel);
            using (new IndentLevelScope())
            {
                EditorGUILayout.PropertyField(m_ScatteringColor);
                m_ScatteringFactor.floatValue = EditorGUILayout.Slider("Scattering Factor", m_ScatteringFactor.floatValue, 0.0f, 1.0f);
                m_HeightScattering.floatValue = EditorGUILayout.Slider("Height Scattering", m_HeightScattering.floatValue, 0.0f, 1.0f);
                m_DisplacementScattering.floatValue = EditorGUILayout.Slider("Displacement Scattering", m_DisplacementScattering.floatValue, 0.0f, 1.0f);
                m_DirectLightTipScattering.floatValue = EditorGUILayout.Slider("Direct Light Tip Scattering", m_DirectLightTipScattering.floatValue, 0.0f, 1.0f);
                m_DirectLightBodyScattering.floatValue = EditorGUILayout.Slider("Direct Light Body Scattering", m_DirectLightBodyScattering.floatValue, 0.0f, 1.0f);
            }

            EditorGUILayout.LabelField("Caustics", EditorStyles.boldLabel);
            using (new IndentLevelScope())
            {
                EditorGUILayout.PropertyField(m_CausticsIntensity);
                m_CausticsIntensity.floatValue = Mathf.Max(m_CausticsIntensity.floatValue, 0.0f);
                EditorGUILayout.PropertyField(m_CausticsTiling);
                m_CausticsTiling.floatValue = Mathf.Max(m_CausticsTiling.floatValue, 0.001f);
                EditorGUILayout.PropertyField(m_CausticsSpeed);
                m_CausticsSpeed.floatValue = Mathf.Clamp(m_CausticsSpeed.floatValue, 0.0f, 100.0f);
                EditorGUILayout.PropertyField(m_CausticsPlaneOffset);
                m_CausticsPlaneOffset.floatValue = Mathf.Max(m_CausticsPlaneOffset.floatValue, 0.0f);
            }

            EditorGUILayout.LabelField("Masking", EditorStyles.boldLabel);
            using (new IndentLevelScope())
            {
                EditorGUILayout.PropertyField(m_WaterMask);
                if (m_WaterMask.objectReferenceValue != null)
                {
                    EditorGUILayout.PropertyField(m_WaterMaskExtent);
                    EditorGUILayout.PropertyField(m_WaterMaskOffset);
                }
            }

            EditorGUILayout.LabelField("Foam", EditorStyles.boldLabel);
            using (new IndentLevelScope())
            {
                // Surface foam
                m_SurfaceFoamSmoothness.floatValue = EditorGUILayout.Slider("Surface Foam Smoothness", m_SurfaceFoamSmoothness.floatValue, 0.0f, 1.0f);
                m_SurfaceFoamIntensity.floatValue = EditorGUILayout.Slider("Surface Foam Intensity", m_SurfaceFoamIntensity.floatValue, 0.0f, 1.0f);
                m_SurfaceFoamAmount.floatValue = EditorGUILayout.Slider("Surface Foam Amount", m_SurfaceFoamAmount.floatValue, 0.0f, 1.0f);
                EditorGUILayout.PropertyField(m_SurfaceFoamTiling);
                m_SurfaceFoamTiling.floatValue = Mathf.Max(m_SurfaceFoamTiling.floatValue, 0.01f);

                // Deep foam
                EditorGUILayout.PropertyField(m_DeepFoam);
                EditorGUILayout.PropertyField(m_DeepFoamColor);
                m_DeepFoam.floatValue = Mathf.Max(m_DeepFoam.floatValue, 0.0f);

                // Foam masking
                EditorGUILayout.PropertyField(m_FoamMask);
                if (m_FoamMask.objectReferenceValue != null)
                {
                    using (new IndentLevelScope())
                    {
                        EditorGUILayout.PropertyField(m_FoamMaskExtent);
                        EditorGUILayout.PropertyField(m_FoamMaskOffset);
                    }
                }
            }

            EditorGUILayout.LabelField("Wind", EditorStyles.boldLabel);
            using (new IndentLevelScope())
            {
                EditorGUILayout.PropertyField(m_WindOrientation);
                m_WindSpeed.floatValue = EditorGUILayout.Slider("Wind Speed", m_WindSpeed.floatValue, 0.0f, 100.0f);
                m_WindAffectCurrent.floatValue = EditorGUILayout.Slider("Wind Affects Current", m_WindAffectCurrent.floatValue, 0.0f, 1.0f);
                EditorGUILayout.PropertyField(m_WindFoamCurve);
            }
            serializedObject.ApplyModifiedProperties();
        }

        // Anis 11/09/21: Currently, there is a bug that makes the icon disappear after the first selection
        // if we do not have this. Given that the geometry is procedural, we need this to be able to
        // select the water surfaces.
        [DrawGizmo(GizmoType.Selected | GizmoType.Active)]
        static void DrawGizmosSelected(WaterSurface waterSurface, GizmoType gizmoType)
        {
        }
    }
}
