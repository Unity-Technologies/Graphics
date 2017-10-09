using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.HDPipeline;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    [CustomEditor(typeof(SubsurfaceScatteringProfile))]
    public partial class SubsurfaceScatteringProfileEditor : Editor
    {
        RenderTexture      m_ProfileImage;
        RenderTexture      m_TransmittanceImage;
        Material           m_ProfileMaterial;
        Material           m_TransmittanceMaterial;
        SerializedProperty m_ScatteringDistance;
        SerializedProperty m_MaxRadius;
        SerializedProperty m_ShapeParam;
        SerializedProperty m_TransmissionTint;
        SerializedProperty m_TexturingMode;
        SerializedProperty m_TransmissionMode;
        SerializedProperty m_ThicknessRemap;
        SerializedProperty m_WorldScale;

        // Old SSS Model >>>
        SerializedProperty m_ScatterDistance1;
        SerializedProperty m_ScatterDistance2;
        SerializedProperty m_LerpWeight;
        // <<< Old SSS Model

        void OnEnable()
        {
            using (var o = new PropertyFetcher<SubsurfaceScatteringProfile>(serializedObject))
            {
                m_ScatteringDistance    = o.FindProperty(x => x.scatteringDistance);
                m_MaxRadius             = o.FindProperty("m_MaxRadius");
                m_ShapeParam            = o.FindProperty("m_ShapeParam");
                m_TransmissionTint      = o.FindProperty(x => x.transmissionTint);
                m_TexturingMode         = o.FindProperty(x => x.texturingMode);
                m_TransmissionMode      = o.FindProperty(x => x.transmissionMode);
                m_ThicknessRemap        = o.FindProperty(x => x.thicknessRemap);
                m_WorldScale            = o.FindProperty(x => x.worldScale);
                // Old SSS Model >>>
                m_ScatterDistance1      = o.FindProperty(x => x.scatterDistance1);
                m_ScatterDistance2      = o.FindProperty(x => x.scatterDistance2);
                m_LerpWeight            = o.FindProperty(x => x.lerpWeight);
                // <<< Old SSS Model
            }

            // These shaders don't need to be reference by RenderPipelineResource as they are not use at runtime
            m_ProfileMaterial       = CoreUtils.CreateEngineMaterial("Hidden/HDRenderPipeline/DrawSssProfile");
            m_TransmittanceMaterial = CoreUtils.CreateEngineMaterial("Hidden/HDRenderPipeline/DrawTransmittanceGraph");

            m_ProfileImage          = new RenderTexture(256, 256, 0, RenderTextureFormat.DefaultHDR);
            m_TransmittanceImage    = new RenderTexture( 16, 256, 0, RenderTextureFormat.DefaultHDR);
        }

        void OnDisable()
        {
            CoreUtils.Destroy(m_ProfileMaterial);
            CoreUtils.Destroy(m_TransmittanceMaterial);
            CoreUtils.Destroy(m_ProfileImage);
            CoreUtils.Destroy(m_TransmittanceImage);
        }

        public override void OnInspectorGUI()
        {
            var hdPipeline = RenderPipelineManager.currentPipeline as HDRenderPipeline;

            if (hdPipeline == null)
                return;

            serializedObject.Update();
            CheckStyles();

            // Old SSS Model >>>
            bool useDisneySSS = hdPipeline.sssSettings.useDisneySSS;
            // <<< Old SSS Model

            using (var scope = new EditorGUI.ChangeCheckScope())
            {
                if (useDisneySSS)
                {
                    EditorGUILayout.PropertyField(m_ScatteringDistance, s_Styles.profileScatteringDistance);

                    using (new EditorGUI.DisabledScope(true))
                        EditorGUILayout.PropertyField(m_MaxRadius, s_Styles.profileMaxRadius);
                }
                else
                {
                    EditorGUILayout.PropertyField(m_ScatterDistance1, s_Styles.profileScatterDistance1);
                    EditorGUILayout.PropertyField(m_ScatterDistance2, s_Styles.profileScatterDistance2);
                    EditorGUILayout.PropertyField(m_LerpWeight, s_Styles.profileLerpWeight);
                }

                m_TexturingMode.intValue = EditorGUILayout.Popup(s_Styles.texturingMode, m_TexturingMode.intValue, s_Styles.texturingModeOptions);
                m_TransmissionMode.intValue = EditorGUILayout.Popup(s_Styles.profileTransmissionMode, m_TransmissionMode.intValue, s_Styles.transmissionModeOptions);

                EditorGUILayout.PropertyField(m_TransmissionTint, s_Styles.profileTransmissionTint);
                EditorGUILayout.PropertyField(m_ThicknessRemap, s_Styles.profileMinMaxThickness);
                Vector2 thicknessRemap = m_ThicknessRemap.vector2Value;
                EditorGUILayout.MinMaxSlider(s_Styles.profileThicknessRemap, ref thicknessRemap.x, ref thicknessRemap.y, 0.0f, 50.0f);
                m_ThicknessRemap.vector2Value = thicknessRemap;
                EditorGUILayout.PropertyField(m_WorldScale, s_Styles.profileWorldScale);

                EditorGUILayout.Space();
                EditorGUILayout.LabelField(s_Styles.profilePreview0, s_Styles.centeredMiniBoldLabel);
                EditorGUILayout.LabelField(s_Styles.profilePreview1, EditorStyles.centeredGreyMiniLabel);
                EditorGUILayout.LabelField(s_Styles.profilePreview2, EditorStyles.centeredGreyMiniLabel);
                EditorGUILayout.LabelField(s_Styles.profilePreview3, EditorStyles.centeredGreyMiniLabel);
                EditorGUILayout.Space();

                serializedObject.ApplyModifiedProperties();

                if (scope.changed)
                {
                    // Validate each individual asset and update caches.
                    ((SubsurfaceScatteringProfile)target).Validate();
                    hdPipeline.sssSettings.UpdateCache();
                }
            }

            float r = m_MaxRadius.floatValue;
            Vector3 S = m_ShapeParam.vector3Value;
            Vector4 T = m_TransmissionTint.colorValue;
            Vector2 R = m_ThicknessRemap.vector2Value;
            bool transmissionEnabled = m_TransmissionMode.intValue != (int)SubsurfaceScatteringProfile.TransmissionMode.None;

            m_ProfileMaterial.SetFloat(HDShaderIDs._MaxRadius, r);
            m_ProfileMaterial.SetVector(HDShaderIDs._ShapeParam, S);

            // Old SSS Model >>>
            CoreUtils.SelectKeyword(m_ProfileMaterial, "SSS_MODEL_DISNEY", "SSS_MODEL_BASIC", useDisneySSS);

            // Apply the three-sigma rule, and rescale.
            float s = (1.0f / 3.0f) * SssConstants.SSS_BASIC_DISTANCE_SCALE;
            float rMax = Mathf.Max(m_ScatterDistance1.colorValue.r, m_ScatterDistance1.colorValue.g, m_ScatterDistance1.colorValue.b,
                m_ScatterDistance2.colorValue.r, m_ScatterDistance2.colorValue.g, m_ScatterDistance2.colorValue.b);
            Vector4 stdDev1 = s * m_ScatterDistance1.colorValue;
            Vector4 stdDev2 = s * m_ScatterDistance2.colorValue;
            m_ProfileMaterial.SetVector(HDShaderIDs._StdDev1, stdDev1);
            m_ProfileMaterial.SetVector(HDShaderIDs._StdDev2, stdDev2);
            m_ProfileMaterial.SetFloat(HDShaderIDs._LerpWeight, m_LerpWeight.floatValue);
            m_ProfileMaterial.SetFloat(HDShaderIDs._MaxRadius, rMax);
            // <<< Old SSS Model

            // Draw the profile.
            EditorGUI.DrawPreviewTexture(GUILayoutUtility.GetRect(256, 256), m_ProfileImage, m_ProfileMaterial, ScaleMode.ScaleToFit, 1.0f);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField(s_Styles.transmittancePreview0, s_Styles.centeredMiniBoldLabel);
            EditorGUILayout.LabelField(s_Styles.transmittancePreview1, EditorStyles.centeredGreyMiniLabel);
            EditorGUILayout.LabelField(s_Styles.transmittancePreview2, EditorStyles.centeredGreyMiniLabel);
            EditorGUILayout.Space();

            // Old SSS Model >>>
            // Multiply by 0.1 to convert from millimeters to centimeters. Apply the distance scale.
            float a = 0.1f * SssConstants.SSS_BASIC_DISTANCE_SCALE;
            Vector4 halfRcpVarianceAndWeight1 = new Vector4(a * a * 0.5f / (stdDev1.x * stdDev1.x), a * a * 0.5f / (stdDev1.y * stdDev1.y), a * a * 0.5f / (stdDev1.z * stdDev1.z), 4 * (1.0f - m_LerpWeight.floatValue));
            Vector4 halfRcpVarianceAndWeight2 = new Vector4(a * a * 0.5f / (stdDev2.x * stdDev2.x), a * a * 0.5f / (stdDev2.y * stdDev2.y), a * a * 0.5f / (stdDev2.z * stdDev2.z), 4 * m_LerpWeight.floatValue);
            m_TransmittanceMaterial.SetVector(HDShaderIDs._HalfRcpVarianceAndWeight1, halfRcpVarianceAndWeight1);
            m_TransmittanceMaterial.SetVector(HDShaderIDs._HalfRcpVarianceAndWeight2, halfRcpVarianceAndWeight2);
            // <<< Old SSS Model

            m_TransmittanceMaterial.SetVector(HDShaderIDs._ShapeParam, S);
            m_TransmittanceMaterial.SetVector(HDShaderIDs._TransmissionTint, transmissionEnabled ? T : Vector4.zero);
            m_TransmittanceMaterial.SetVector(HDShaderIDs._ThicknessRemap, R);

            // Draw the transmittance graph.
            EditorGUI.DrawPreviewTexture(GUILayoutUtility.GetRect(16, 16), m_TransmittanceImage, m_TransmittanceMaterial, ScaleMode.ScaleToFit, 16.0f);
        }
    }
}
