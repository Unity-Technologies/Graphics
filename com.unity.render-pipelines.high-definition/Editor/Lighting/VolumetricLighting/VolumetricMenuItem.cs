using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Rendering;

namespace UnityEditor.Rendering.HighDefinition
{
    class VolumetricMenuItems
    {
        [MenuItem("GameObject/Rendering/Density Volume", priority = CoreUtils.gameObjectMenuPriority)]
        static void CreateDensityVolumeGameObject(MenuCommand menuCommand)
        {
            var parent = menuCommand.context as GameObject;
            var densityVolume = CoreEditorUtils.CreateGameObject("Density Volume", parent);

            densityVolume.AddComponent<DensityVolume>();
        }

        //[MenuItem("GameObject/Light/Experimental/Probe Volume", priority = CoreUtils.gameObjectMenuPriority)]
        static void CreateProbeVolumeGameObject(MenuCommand menuCommand)
        {
            var parent = menuCommand.context as GameObject;
            var probeVolume = CoreEditorUtils.CreateGameObject("Probe Volume", parent);

            probeVolume.AddComponent<ProbeVolume>();
        }
    }
}
