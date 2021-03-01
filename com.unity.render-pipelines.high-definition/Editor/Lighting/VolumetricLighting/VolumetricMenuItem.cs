using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Rendering;

namespace UnityEditor.Rendering.HighDefinition
{
    class VolumetricMenuItems
    {
        [MenuItem("GameObject/Volume/Density Volume", priority = CoreUtils.Sections.section2 + CoreUtils.Priorities.gameObjectMenuPriority + 2)]
        static void CreateDensityVolumeGameObject(MenuCommand menuCommand)
        {
            var parent = menuCommand.context as GameObject;
            var densityVolume = CoreEditorUtils.CreateGameObject("Density Volume", parent);

            densityVolume.AddComponent<DensityVolume>();
        }

        [MenuItem("GameObject/Light/Experimental/Probe Volume", priority = CoreUtils.Sections.section8)]
        static void CreateProbeVolumeGameObject(MenuCommand menuCommand)
        {
            var parent = menuCommand.context as GameObject;
            var probeVolume = CoreEditorUtils.CreateGameObject(parent, "Probe Volume");
            GameObjectUtility.SetParentAndAlign(probeVolume, menuCommand.context as GameObject);
            Undo.RegisterCreatedObjectUndo(probeVolume, "Create " + probeVolume.name);
            Selection.activeObject = probeVolume;

            probeVolume.AddComponent<ProbeVolume>();
        }
    }
}
