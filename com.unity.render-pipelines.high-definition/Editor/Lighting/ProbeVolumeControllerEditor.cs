using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace UnityEditor.Rendering.HighDefinition
{
    [CanEditMultipleObjects]
    [VolumeComponentEditor(typeof(ProbeVolumeController))]
    internal class ProbeVolumeControllerEditor : VolumeComponentEditor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            if (ShaderConfig.s_EnableProbeVolumes == 1)
            {
                if (!(GraphicsSettings.renderPipelineAsset as HDRenderPipelineAsset)
                    ?.currentPlatformRenderPipelineSettings.supportProbeVolume ?? false)
                {
                    EditorGUILayout.Space();
                    EditorGUILayout.HelpBox("The current HDRP Asset does not support Probe Volume Global Illumination.", MessageType.Error, wide: true);
                }
            }
        }
    }
}
