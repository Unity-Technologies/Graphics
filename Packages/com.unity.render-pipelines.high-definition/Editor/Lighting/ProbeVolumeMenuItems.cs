using UnityEngine;
using UnityEngine.Rendering;

namespace UnityEditor.Rendering.HighDefinition
{
    class ProbeVolumeMenuItems
    {
        [MenuItem("GameObject/Light/Probe Volume (Experimental)/Probe Volume", priority = CoreUtils.Sections.section8)]
        static void CreateProbeVolumeGameObject(MenuCommand menuCommand)
        {
            var parent = menuCommand.context as GameObject;
            var probeVolume = CoreEditorUtils.CreateGameObject("Probe Volume", parent);
            probeVolume.AddComponent<ProbeVolume>();
        }

        [MenuItem("GameObject/Light/Probe Volume (Experimental)/Probe Adjustment Volume", priority = CoreUtils.Sections.section8)]
        static void CreateProbeTouchupVolumeGameObject(MenuCommand menuCommand)
        {
            var parent = menuCommand.context as GameObject;
            var probeVolume = CoreEditorUtils.CreateGameObject("Probe Adjustment Volume", parent);
            probeVolume.AddComponent<ProbeTouchupVolume>();
        }
    }
}
