using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;

namespace UnityEditor.Rendering.HighDefinition
{
    [CustomEditor(typeof(DiffusionProfileSettings))]
    partial class DiffusionProfileSettingsEditor : HDBaseEditor<DiffusionProfileSettings>
    {
        sealed class Profile
        {
            internal SerializedProperty self;
            internal DiffusionProfile objReference;

            internal SerializedProperty scatteringDistance;
            internal SerializedProperty scatteringDistanceMultiplier;
            internal SerializedProperty transmissionTint;
            internal SerializedProperty texturingMode;
            internal SerializedProperty transmissionMode;
            internal SerializedProperty thicknessRemap;
            internal SerializedProperty worldScale;
            internal SerializedProperty ior;

            // Render preview
            internal RenderTexture profileRT;
            internal RenderTexture transmittanceRT;

            internal Profile()
            {
                profileRT = new RenderTexture(256, 256, 0, GraphicsFormat.R16G16B16A16_SFloat);
                transmittanceRT = new RenderTexture(16, 256, 0, GraphicsFormat.R16G16B16A16_SFloat);
            }

            internal void Release()
            {
                CoreUtils.Destroy(profileRT);
                CoreUtils.Destroy(transmittanceRT);
            }
        }

        Profile m_Profile;

        Material m_ProfileMaterial;
        Material m_TransmittanceMaterial;

        protected override void OnEnable()
        {
            base.OnEnable();

            // These shaders don't need to be reference by RenderPipelineResource as they are not use at runtime
            m_ProfileMaterial = CoreUtils.CreateEngineMaterial("Hidden/HDRP/DrawDiffusionProfile");
            m_TransmittanceMaterial = CoreUtils.CreateEngineMaterial("Hidden/HDRP/DrawTransmittanceGraph");

            var serializedProfile = properties.Find(x => x.profile);

            var rp = new RelativePropertyFetcher<DiffusionProfile>(serializedProfile);

            m_Profile = new Profile
            {
                self = serializedProfile,
                objReference = m_Target.profile,

                scatteringDistance = rp.Find(x => x.scatteringDistance),
                scatteringDistanceMultiplier = rp.Find(x => x.scatteringDistanceMultiplier),
                transmissionTint = rp.Find(x => x.transmissionTint),
                texturingMode = rp.Find(x => x.texturingMode),
                transmissionMode = rp.Find(x => x.transmissionMode),
                thicknessRemap = rp.Find(x => x.thicknessRemap),
                worldScale = rp.Find(x => x.worldScale),
                ior = rp.Find(x => x.ior)
            };

            Undo.undoRedoPerformed += UpdateProfile;
        }

        void OnDisable()
        {
            CoreUtils.Destroy(m_ProfileMaterial);
            CoreUtils.Destroy(m_TransmittanceMaterial);

            m_Profile.Release();

            m_Profile = null;

            Undo.undoRedoPerformed -= UpdateProfile;
        }

        public override void OnInspectorGUI()
        {
            CheckStyles();

            serializedObject.Update();

            EditorGUILayout.Space();

            var profile = m_Profile;

            EditorGUI.indentLevel++;

            using (var scope = new EditorGUI.ChangeCheckScope())
            {
                EditorGUI.BeginChangeCheck();
                // For some reason the HDR picker is in gamma space, so convert to maintain same visual
                var color = EditorGUILayout.ColorField(s_Styles.profileScatteringColor, profile.scatteringDistance.colorValue.gamma, true, false, false);
                if (EditorGUI.EndChangeCheck())
                    profile.scatteringDistance.colorValue = color.linear;

                using (new EditorGUI.IndentLevelScope())
                    EditorGUILayout.PropertyField(profile.scatteringDistanceMultiplier, s_Styles.profileScatteringDistanceMultiplier);

                using (new EditorGUI.DisabledScope(true))
                    EditorGUILayout.FloatField(s_Styles.profileMaxRadius, profile.objReference.filterRadius);

                EditorGUILayout.Space();

                EditorGUILayout.Slider(profile.ior, 1.0f, 2.0f, s_Styles.profileIor);
                EditorGUILayout.PropertyField(profile.worldScale, s_Styles.profileWorldScale);

                EditorGUILayout.Space();
                EditorGUILayout.LabelField(s_Styles.SubsurfaceScatteringLabel, EditorStyles.boldLabel);

                profile.texturingMode.intValue = EditorGUILayout.Popup(s_Styles.texturingMode, profile.texturingMode.intValue, s_Styles.texturingModeOptions);

                EditorGUILayout.Space();
                EditorGUILayout.LabelField(s_Styles.TransmissionLabel, EditorStyles.boldLabel);

                profile.transmissionMode.intValue = EditorGUILayout.Popup(s_Styles.profileTransmissionMode, profile.transmissionMode.intValue, s_Styles.transmissionModeOptions);

                EditorGUILayout.PropertyField(profile.transmissionTint, s_Styles.profileTransmissionTint);
                EditorGUILayout.PropertyField(profile.thicknessRemap, s_Styles.profileMinMaxThickness);
                var thicknessRemap = profile.thicknessRemap.vector2Value;
                EditorGUILayout.MinMaxSlider(s_Styles.profileThicknessRemap, ref thicknessRemap.x, ref thicknessRemap.y, 0f, 50f);
                profile.thicknessRemap.vector2Value = thicknessRemap;

                EditorGUILayout.Space();
                EditorGUILayout.LabelField(s_Styles.profilePreview0, s_Styles.centeredMiniBoldLabel);
                EditorGUILayout.LabelField(s_Styles.profilePreview1, EditorStyles.centeredGreyMiniLabel);
                EditorGUILayout.LabelField(s_Styles.profilePreview2, EditorStyles.centeredGreyMiniLabel);
                EditorGUILayout.LabelField(s_Styles.profilePreview3, EditorStyles.centeredGreyMiniLabel);
                EditorGUILayout.Space();

                serializedObject.ApplyModifiedProperties();

                // NOTE: We cannot change only upon scope changed since there is no callback when Reset is triggered for Editor and the scope is not changed when Reset is called.
                // The following operations are not super cheap, but are not overly expensive, so we instead trigger the change every time inspector is drawn.
                //    if (scope.changed)
                {
                    // Validate and update the cache for this profile only
                    profile.objReference.Validate();
                    m_Target.UpdateCache();
                }
            }

            RenderPreview(profile);

            EditorGUILayout.Space();
            EditorGUI.indentLevel--;

            serializedObject.ApplyModifiedProperties();
        }

        void RenderPreview(Profile profile)
        {
            var obj = profile.objReference;
            float r = obj.filterRadius;
            var S = obj.shapeParam;

            m_ProfileMaterial.SetFloat(HDShaderIDs._MaxRadius, r);
            m_ProfileMaterial.SetVector(HDShaderIDs._ShapeParam, S);

            // Draw the profile.
            EditorGUI.DrawPreviewTexture(GUILayoutUtility.GetRect(256f, 256f), profile.profileRT, m_ProfileMaterial, ScaleMode.ScaleToFit, 1f);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField(s_Styles.transmittancePreview0, s_Styles.centeredMiniBoldLabel);
            EditorGUILayout.LabelField(s_Styles.transmittancePreview1, EditorStyles.centeredGreyMiniLabel);
            EditorGUILayout.LabelField(s_Styles.transmittancePreview2, EditorStyles.centeredGreyMiniLabel);
            EditorGUILayout.Space();

            m_TransmittanceMaterial.SetVector(HDShaderIDs._ShapeParam, S);
            m_TransmittanceMaterial.SetVector(HDShaderIDs._TransmissionTint, obj.transmissionTint);
            m_TransmittanceMaterial.SetVector(HDShaderIDs._ThicknessRemap, obj.thicknessRemap);

            // Draw the transmittance graph.
            EditorGUI.DrawPreviewTexture(GUILayoutUtility.GetRect(16f, 16f), profile.transmittanceRT, m_TransmittanceMaterial, ScaleMode.ScaleToFit, 16f);
        }

        void UpdateProfile()
        {
            m_Target.profile.Validate();
            m_Target.UpdateCache();
        }
    }
}
