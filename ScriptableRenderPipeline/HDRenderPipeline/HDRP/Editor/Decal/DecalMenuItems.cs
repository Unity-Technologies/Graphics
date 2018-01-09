using System.IO;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.HDPipeline;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    public class DecalMenuItems
    {
        [MenuItem("GameObject/Render Pipeline/High Definition/DecalProjector", priority = CoreUtils.gameObjectMenuPriority)]
        static void CreateDecal(MenuCommand menuCommand)
        {
            // Create a custom game object
            GameObject go = new GameObject("DecalProjector");
            go.AddComponent<DecalProjectorComponent>();
            // Ensure it gets re-parented if this was a context click (otherwise does nothing)
            GameObjectUtility.SetParentAndAlign(go, menuCommand.context as GameObject);
            // Register the creation in the undo system
            Undo.RegisterCreatedObjectUndo(go, "Create " + go.name);
            Selection.activeObject = go;
        }
    }
}
