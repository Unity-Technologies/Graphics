using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace UnityEditor.Rendering.HighDefinition
{
    class DecalMenuItems
    {
        [MenuItem("GameObject/Decal Projector", priority = CoreUtils.Priorities.gameObjectMenuPriority + 1)]
        static void CreateDecal(MenuCommand menuCommand)
        {
            var parent = menuCommand.context as GameObject;
            var go = CoreEditorUtils.CreateGameObject("Decal Projector", parent);
            var decal = go.AddComponent<DecalProjector>();
            decal.transform.RotateAround(decal.transform.position, decal.transform.right, 90);
        }
    }
}
