using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Rendering;

namespace UnityEditor.Rendering.HighDefinition
{
    class DecalMenuItems
    {
        [MenuItem("GameObject/Rendering/Decal Projector", priority = CoreUtils.gameObjectMenuPriority)]
        static void CreateDecal(MenuCommand menuCommand)
        {
            var parent = menuCommand.context as GameObject;
            var go = CoreEditorUtils.CreateGameObject("Decal Projector", parent);
            var decal = go.AddComponent<DecalProjector>();
            decal.transform.RotateAround(decal.transform.position, decal.transform.right, 90);
        }
    }
}
