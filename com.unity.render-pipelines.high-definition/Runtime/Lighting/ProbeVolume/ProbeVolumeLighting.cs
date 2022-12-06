using UnityEngine.Rendering;

namespace UnityEngine.Rendering.HighDefinition
{
    public partial class HDRenderPipeline
    {
        internal void RetrieveExtraDataFromProbeVolumeBake(ProbeReferenceVolume.ExtraDataActionInput input)
        {
            var hdProbes = GameObject.FindObjectsByType<HDProbe>(FindObjectsSortMode.InstanceID);
            foreach (var hdProbe in hdProbes)
            {
                hdProbe.TryUpdateLuminanceSHL2ForNormalization();
#if UNITY_EDITOR
                // If we are treating probes inside a prefab, we need to explicitly record the mods
                UnityEditor.PrefabUtility.RecordPrefabInstancePropertyModifications(hdProbe);
#endif
            }
        }

        void RegisterRetrieveOfProbeVolumeExtraDataAction()
        {
            ProbeReferenceVolume.instance.retrieveExtraDataAction = null;
            ProbeReferenceVolume.instance.retrieveExtraDataAction += RetrieveExtraDataFromProbeVolumeBake;
        }

        bool IsAPVEnabled()
        {
            return m_Asset.currentPlatformRenderPipelineSettings.supportProbeVolume;
        }

        private void UpdateShaderVariablesProbeVolumes(ref ShaderVariablesGlobal cb, HDCamera hdCamera, CommandBuffer cmd)
        {
            bool enableProbeVolumes = false;
            if (hdCamera.frameSettings.IsEnabled(FrameSettingsField.ProbeVolume))
                enableProbeVolumes = ProbeVolumeLighting.instance.UpdateShaderVariablesProbeVolumes(
                    hdCamera.volumeStack.GetComponent<ProbeVolumesOptions>(),
                    hdCamera.taaFrameIndex,
                    cmd);
            cb._EnableProbeVolumes = enableProbeVolumes ? 1u : 0u;
        }
    }
}
