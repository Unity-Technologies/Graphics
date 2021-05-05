using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;

namespace UnityEditor.Rendering.HighDefinition
{
    class ProbeVolumeMenuItems
    {
        [MenuItem("GameObject/Light/Probe Volume (Experimental)", priority = CoreUtils.Sections.section8)]
        static void CreateProbeVolumeGameObject(MenuCommand menuCommand)
        {
            var parent = menuCommand.context as GameObject;
            var probeVolume = CoreEditorUtils.CreateGameObject("Probe Volume", parent);
            probeVolume.AddComponent<ProbeVolume>();
        }
    }
}
