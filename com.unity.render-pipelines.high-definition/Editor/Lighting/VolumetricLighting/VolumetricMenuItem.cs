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
            var densityVolume = CoreEditorUtils.CreateGameObject(parent, "Density Volume");
            GameObjectUtility.SetParentAndAlign(densityVolume, menuCommand.context as GameObject);
            Undo.RegisterCreatedObjectUndo(densityVolume, "Create " + densityVolume.name);
            Selection.activeObject = densityVolume;

            densityVolume.AddComponent<DensityVolume>();
        }

        [MenuItem("GameObject/Light/Experimental/Probe Volume", priority = CoreUtils.gameObjectMenuPriority)]
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
