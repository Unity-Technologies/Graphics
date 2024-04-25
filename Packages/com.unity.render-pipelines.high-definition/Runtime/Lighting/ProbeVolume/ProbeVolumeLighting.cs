using UnityEngine.Rendering;

namespace UnityEngine.Rendering.HighDefinition
{
    public partial class HDRenderPipeline
    {
        // Store locally the value on the instance due as the Render Pipeline Asset data might change before the disposal of the asset, making some APV Resources leak.
        internal bool apvIsEnabled = false;

        internal void RetrieveExtraDataFromProbeVolumeBake(ProbeReferenceVolume.ExtraDataActionInput input)
        {
            var hdProbes = GameObject.FindObjectsByType<HDProbe>(FindObjectsSortMode.None);
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

        private void UpdateShaderVariablesProbeVolumes(ref ShaderVariablesGlobal cb, HDCamera hdCamera, CommandBuffer cmd)
        {
            bool enableProbeVolumes = false;
            if (hdCamera.frameSettings.IsEnabled(FrameSettingsField.AdaptiveProbeVolume))
                enableProbeVolumes = ProbeReferenceVolume.instance.UpdateShaderVariablesProbeVolumes(cmd,
                    hdCamera.volumeStack.GetComponent<ProbeVolumesOptions>(),
                    hdCamera.taaFrameIndex,
                    hdCamera.frameSettings.IsEnabled(FrameSettingsField.LightLayers));
            cb._EnableProbeVolumes = enableProbeVolumes ? 1u : 0u;
        }
    }
}
