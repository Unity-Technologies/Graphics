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
            var go = CoreEditorUtils.CreateGameObject(parent, "Decal Projector");
            go.AddComponent<DecalProjector>();
            // Ensure it gets re-parented if this was a context click (otherwise does nothing)
            GameObjectUtility.SetParentAndAlign(go, menuCommand.context as GameObject);
            // Register the creation in the undo system
            Undo.RegisterCreatedObjectUndo(go, "Create " + go.name);
            Selection.activeObject = go;
        }
    }
}
