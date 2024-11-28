using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace UnityEditor.Rendering.HighDefinition
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(DiffusionProfileSettings))]
    partial class DiffusionProfileSettingsEditor : Editor
    {
        Material m_ProfileMaterial;
        Material m_TransmittanceMaterial;

        List<DiffusionProfileSettings> m_DiffusionProfileSettingsTargets;
        SerializedDiffusionProfileSettings m_SerializedDiffusionProfileSettings;
        bool m_MultipleObjectSelected;

        void OnEnable()
        {
            // These shaders don't need to be reference by RenderPipelineResource as they are not use at runtime
            m_ProfileMaterial = CoreUtils.CreateEngineMaterial("Hidden/HDRP/DrawDiffusionProfile");
            m_TransmittanceMaterial = CoreUtils.CreateEngineMaterial("Hidden/HDRP/DrawTransmittanceGraph");

            m_DiffusionProfileSettingsTargets = targets.Cast<DiffusionProfileSettings>().ToList();
            m_SerializedDiffusionProfileSettings = new SerializedDiffusionProfileSettings((DiffusionProfileSettings)target, serializedObject);
            m_MultipleObjectSelected = targets.Length > 1;

            Undo.undoRedoPerformed += UpdateProfile;
        }

        void OnDisable()
        {
            CoreUtils.Destroy(m_ProfileMaterial);
            CoreUtils.Destroy(m_TransmittanceMaterial);

            m_SerializedDiffusionProfileSettings.Dispose();

            Undo.undoRedoPerformed -= UpdateProfile;
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            DrawScattering(m_MultipleObjectSelected);
            DrawIORAndScale();
            DrawSSS();
            DrawTransmission();

            //NOTE: We manually apply changes and update all properties every time to fix a case when User click Reset on Component.
            //Unfortunately there is no way to receive callback from that Reset so only way to have correct Preview is to update target every time.
            foreach (var settings in m_DiffusionProfileSettingsTargets)
            {
                UpdateProfile(settings);
            }

            serializedObject.ApplyModifiedProperties();

            if (!m_MultipleObjectSelected)
                RenderPreview();
        }

        void DrawScattering(bool multipleObjectSelected)
        {
            EditorGUILayout.LabelField(Styles.scatteringLabel, EditorStyles.boldLabel);
            using (new EditorGUI.IndentLevelScope())
            {
                using (new EditorGUI.MixedValueScope(m_SerializedDiffusionProfileSettings.scatteringDistance.hasMultipleDifferentValues))
                using (var changeCheckScope = new EditorGUI.ChangeCheckScope())
                {
                    // For some reason the HDR picker is in gamma space, so convert to maintain same visual
                    var color = EditorGUILayout.ColorField(Styles.profileScatteringColor,
                        m_SerializedDiffusionProfileSettings.scatteringDistance.colorValue.gamma, true, false, false);
                    if (changeCheckScope.changed)
                        m_SerializedDiffusionProfileSettings.scatteringDistance.colorValue = color.linear;
                }

                using (new EditorGUI.IndentLevelScope())
                    EditorGUILayout.PropertyField(m_SerializedDiffusionProfileSettings.scatteringDistanceMultiplier,
                        Styles.profileScatteringDistanceMultiplier);

                if (!multipleObjectSelected)
                {
                    using (new EditorGUI.DisabledScope(true))
                        EditorGUILayout.FloatField(Styles.profileMaxRadius, m_SerializedDiffusionProfileSettings.objReference.filterRadius);
                }
            }

            EditorGUILayout.Space();
        }

        void DrawIORAndScale()
        {
            EditorGUILayout.Slider(m_SerializedDiffusionProfileSettings.ior, 1.0f, 2.0f, Styles.profileIor);
            EditorGUILayout.PropertyField(m_SerializedDiffusionProfileSettings.worldScale, Styles.profileWorldScale);
            EditorGUILayout.Space();
        }

        internal void DualSliderWithFields(GUIContent label, SerializedProperty values, float minLimit, float maxLimit)
        {
            const float fieldWidth = 65, padding = 4;
            var rect = EditorGUILayout.GetControlRect();
            rect = EditorGUI.PrefixLabel(rect, label);

            float slider;
            Vector2 value = values.vector2Value;
            float midLevel = (minLimit + maxLimit) * 0.5f;

            EditorGUI.showMixedValue = values.hasMultipleDifferentValues;
            EditorGUI.BeginChangeCheck();

            if (rect.width >= 3 * fieldWidth + 2 * padding)
            {
                rect.xMin -= 15 * EditorGUI.indentLevel;
                var tmpRect = new Rect(rect);
                tmpRect.width = fieldWidth;

                EditorGUI.BeginChangeCheck();
                slider = EditorGUI.FloatField(tmpRect, value.x);
                if (EditorGUI.EndChangeCheck())
                    value.x = Mathf.Clamp(slider, minLimit, midLevel);

                tmpRect.x = rect.xMax - fieldWidth;
                EditorGUI.BeginChangeCheck();
                slider = EditorGUI.FloatField(tmpRect, value.y);
                if (EditorGUI.EndChangeCheck())
                    value.y = Mathf.Clamp(slider, midLevel, maxLimit);

                tmpRect.xMin = rect.xMin + (fieldWidth + padding);
                tmpRect.xMax = rect.xMax - (EditorGUIUtility.fieldWidth + padding);
                rect = tmpRect;
            }

            rect.width = (rect.width - padding) * 0.5f;
            EditorGUI.BeginChangeCheck();
            slider = GUI.HorizontalSlider(rect, value.x, minLimit, midLevel);
            if (EditorGUI.EndChangeCheck())
                value.x = slider;

            rect.x += rect.width + padding - 1;
            EditorGUI.BeginChangeCheck();
            slider = GUI.HorizontalSlider(rect, value.y, midLevel, maxLimit);
            if (EditorGUI.EndChangeCheck())
                value.y = slider;

            if (EditorGUI.EndChangeCheck())
                values.vector2Value = value;
            EditorGUI.showMixedValue = false;
        }

        void DrawSSS()
        {
            EditorGUILayout.LabelField(Styles.subsurfaceScatteringLabel, EditorStyles.boldLabel);

            using (new EditorGUI.IndentLevelScope())
            {
                EditorGUILayout.PropertyField(m_SerializedDiffusionProfileSettings.texturingMode);
                DualSliderWithFields(Styles.smoothnessMultipliers, m_SerializedDiffusionProfileSettings.smoothnessMultipliers, 0.0f, 2.0f);
                EditorGUILayout.PropertyField(m_SerializedDiffusionProfileSettings.lobeMix);
                EditorGUILayout.PropertyField(m_SerializedDiffusionProfileSettings.diffusePower);
                if (HDRenderPipeline.currentAsset == null || HDRenderPipeline.currentAsset.currentPlatformRenderPipelineSettings.subsurfaceScatteringAttenuation)
                    EditorGUILayout.PropertyField(m_SerializedDiffusionProfileSettings.borderAttenuationColor);
            }

            EditorGUILayout.Space();
        }

        void DrawTransmission()
        {
            EditorGUILayout.LabelField(Styles.transmissionLabel, EditorStyles.boldLabel);
            using (new EditorGUI.IndentLevelScope())
            {
                var transmissionModeMixedValues = m_SerializedDiffusionProfileSettings.transmissionMode.hasMultipleDifferentValues;
                using (new EditorGUI.MixedValueScope(transmissionModeMixedValues))
                using (var changeCheckScope = new EditorGUI.ChangeCheckScope())
                {
                    var previousTransmissionMode = transmissionModeMixedValues ? int.MinValue : m_SerializedDiffusionProfileSettings.transmissionMode.intValue;
                    var newTransmissionMode = EditorGUILayout.EnumPopup(Styles.profileTransmissionMode, (DiffusionProfile.TransmissionMode)previousTransmissionMode);
                    if (changeCheckScope.changed)
                        m_SerializedDiffusionProfileSettings.transmissionMode.intValue = (int)(DiffusionProfile.TransmissionMode)newTransmissionMode;
                }

                EditorGUILayout.PropertyField(m_SerializedDiffusionProfileSettings.transmissionTint, Styles.profileTransmissionTint);
                EditorGUILayout.PropertyField(m_SerializedDiffusionProfileSettings.thicknessRemap, Styles.profileMinMaxThickness);

                if (!m_SerializedDiffusionProfileSettings.thicknessRemap.hasMultipleDifferentValues)
                {
                    using (var changeCheckScope = new EditorGUI.ChangeCheckScope())
                    {
                        var thicknessRemap = m_SerializedDiffusionProfileSettings.thicknessRemap.vector2Value;
                        EditorGUILayout.MinMaxSlider(Styles.profileThicknessRemap, ref thicknessRemap.x, ref thicknessRemap.y, 0f, 50f);
                        if (changeCheckScope.changed)
                            m_SerializedDiffusionProfileSettings.thicknessRemap.vector2Value = thicknessRemap;
                    }
                }
            }

            EditorGUILayout.Space();
        }

        void RenderPreview()
        {
            EditorGUILayout.LabelField(Styles.profilePreview0, Styles.miniBoldButton);
            EditorGUILayout.LabelField(Styles.profilePreview1, EditorStyles.centeredGreyMiniLabel);
            EditorGUILayout.Space();

            var obj = m_SerializedDiffusionProfileSettings.objReference;
            var radius = obj.filterRadius;
            var shape = obj.shapeParam;

            m_ProfileMaterial.SetFloat(HDShaderIDs._MaxRadius, radius);
            m_ProfileMaterial.SetVector(HDShaderIDs._ShapeParam, shape);

            // Draw the profile.
            EditorGUI.DrawPreviewTexture(GUILayoutUtility.GetRect(256f, 256f), m_SerializedDiffusionProfileSettings.profileRT, m_ProfileMaterial, ScaleMode.ScaleToFit, 1f);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField(Styles.transmittancePreview0, Styles.miniBoldButton);
            EditorGUILayout.LabelField(Styles.transmittancePreview1, EditorStyles.centeredGreyMiniLabel);
            EditorGUILayout.Space();

            m_TransmittanceMaterial.SetVector(HDShaderIDs._ShapeParam, shape);
            m_TransmittanceMaterial.SetVector(HDShaderIDs._TransmissionTint, obj.transmissionTint);
            m_TransmittanceMaterial.SetVector(HDShaderIDs._ThicknessRemap, obj.thicknessRemap);

            // Draw the transmittance graph.
            EditorGUI.DrawPreviewTexture(GUILayoutUtility.GetRect(16f, 16f), m_SerializedDiffusionProfileSettings.transmittanceRT, m_TransmittanceMaterial, ScaleMode.ScaleToFit, 16f);
        }

        void UpdateProfile()
        {
            UpdateProfile(m_SerializedDiffusionProfileSettings.settings);
        }

        void UpdateProfile(DiffusionProfileSettings settings)
        {
            settings.profile.Validate();
            settings.UpdateCache();
        }
    }
}
