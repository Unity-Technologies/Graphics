using UnityEditor.Rendering;
using UnityEngine.Experimental.Rendering.HDPipeline;
using UnityEngine.Rendering;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    [CanEditMultipleObjects]
    [VolumeComponentEditor(typeof(ProbeVolumeController))]
    public class ProbeVolumeControllerEditor : VolumeComponentEditor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            if (!(GraphicsSettings.renderPipelineAsset as HDRenderPipelineAsset)
                ?.currentPlatformRenderPipelineSettings.supportProbeVolume ?? false)
            {
                EditorGUILayout.Space();
                EditorGUILayout.HelpBox("The current HDRP Asset does not support Probe Volume Global Illumination.", MessageType.Error, wide: true);
            }
        }
    }
}
