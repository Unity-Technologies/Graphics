using Unity.Properties;
using UnityEditor.Inspector.GraphicsSettingsInspectors;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace UnityEditor.Rendering.Universal
{
    [CustomEditor(typeof(ExposureVolume))]
    sealed class ExposureVolumeEditor : VolumeComponentEditor
    {
        SerializedDataParameter m_Mode;
        SerializedDataParameter m_FixedExposureValue;
        private LightUnitSliderUIDrawer m_LightUnitSlider;


        public override void OnEnable()
        {
            var o = new PropertyFetcher<ExposureVolume>(serializedObject);

            m_Mode = Unpack(o.Find(x => x.mode));
            m_FixedExposureValue = Unpack(o.Find(x => x.fixedExposure));

            m_LightUnitSlider = new LightUnitSliderUIDrawer();
        }

        public override void OnInspectorGUI()
        {
            if (!GraphicsSettings.TryGetRenderPipelineSettings<URPExposureSettings>(out var exposureSetting) || !exposureSetting.UseExposure)
            {
                var useExposureName = ObjectNames.NicifyVariableName(nameof(exposureSetting.UseExposure));
                var message = $"Exposure is currently disabled in the Project Settings, enable \"{useExposureName}\" to use this feature.";
                CoreEditorUtils.DrawFixMeBox(message, MessageType.Warning, "Open", () =>
                {
                    // Open the Graphics page in Project Settings and scroll to (and highlight) the UseExposure checkbox.
                    GraphicsSettingsInspectorUtility.OpenAndScrollTo<URPExposureSettings>(nameof(URPExposureSettings.UseExposure));
                });
            }

            if (m_Mode.value.intValue == (int)ExposureVolume.Mode.Fixed)
            {
                DoExposurePropertyField(m_FixedExposureValue);
            }
        }

        void DoExposurePropertyField(SerializedDataParameter exposureProperty)
        {
            using (var scope = new OverridablePropertyScope(exposureProperty, exposureProperty.displayName, this))
            {
                if (!scope.displayed)
                    return;

                using (new EditorGUILayout.VerticalScope())
                {
                    EditorGUILayout.LabelField(scope.label);

                    var xOffset = EditorGUIUtility.labelWidth + 2;

                    var lineRect = EditorGUILayout.GetControlRect();
                    lineRect.x += xOffset;
                    lineRect.width -= xOffset;

                    var sliderRect = lineRect;
                    sliderRect.y -= EditorGUIUtility.singleLineHeight;
                    m_LightUnitSlider.SetSerializedObject(serializedObject);
                    m_LightUnitSlider.DrawExposureSlider(exposureProperty.value, sliderRect);

                    // Draw the numeric field below the slider
                    lineRect.x -= EditorGUIUtility.labelWidth + 2;
                    lineRect.y += EditorGUIUtility.standardVerticalSpacing;
                    lineRect.width += EditorGUIUtility.labelWidth + 2;
                    EditorGUI.PropertyField(lineRect, exposureProperty.value, EditorGUIUtility.TrTextContent(" ", (Texture)null, null));
                }
            }
        }
    }
}
