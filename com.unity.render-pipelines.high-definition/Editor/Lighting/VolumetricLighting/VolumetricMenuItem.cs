using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;

namespace UnityEditor.Rendering.HighDefinition
{
    class VolumetricMenuItems
    {
        [MenuItem("GameObject/Rendering/Local Volumetric Fog", priority = CoreUtils.Priorities.gameObjectMenuPriority + 2)]
        static void CreateLocalVolumetricFogGameObject(MenuCommand menuCommand)
        {
            var parent = menuCommand.context as GameObject;
            var localVolumetricFog = CoreEditorUtils.CreateGameObject("Local Volumetric Fog", parent);
            localVolumetricFog.AddComponent<LocalVolumetricFog>();
        }
    }
}
