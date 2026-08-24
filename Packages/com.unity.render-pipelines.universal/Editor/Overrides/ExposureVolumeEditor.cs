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
        SerializedDataParameter m_FixedExposure;

        public override void OnEnable()
        {
            var o = new PropertyFetcher<ExposureVolume>(serializedObject);

            m_Mode = Unpack(o.Find(x => x.mode));
            m_FixedExposure = Unpack(o.Find(x => x.fixedExposure));
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
                PropertyField(m_FixedExposure);
            }
        }
    }
}
