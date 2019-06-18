using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Experimental.Rendering.HDPipeline;
using UnityEngine.Rendering;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    public class VolumetricMenuItems
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
        //seongdae;fspm
        [MenuItem("GameObject/Rendering/FluidSim Volume", priority = CoreUtils.gameObjectMenuPriority)]
        static void CreateFluidSimVolumeGameObject(MenuCommand menuCommand)
        {
            var parent = menuCommand.context as GameObject;
            var fluidSimVolume = CoreEditorUtils.CreateGameObject(parent, "FluidSim Volume");
            GameObjectUtility.SetParentAndAlign(fluidSimVolume, menuCommand.context as GameObject);
            Undo.RegisterCreatedObjectUndo(fluidSimVolume, "Create " + fluidSimVolume.name);
            Selection.activeObject = fluidSimVolume;

            fluidSimVolume.AddComponent<FluidSimVolume>();
        }
        //seongdae;fspm
    }
}
