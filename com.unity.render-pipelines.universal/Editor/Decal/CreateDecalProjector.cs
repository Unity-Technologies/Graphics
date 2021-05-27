using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace UnityEditor.Rendering.Universal
{
    internal static class CreateDecalProjector
    {
        [MenuItem("GameObject/Rendering/URP Decal Projector", priority = CoreUtils.Priorities.gameObjectMenuPriority)]
        public static void CreateDecal(MenuCommand menuCommand)
        {
            var go = CoreEditorUtils.CreateGameObject("Decal Projector", menuCommand.context);
            go.AddComponent<DecalProjector>();
            go.transform.RotateAround(go.transform.position, go.transform.right, 90);
        }
    }
}
