using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace UnityEditor.Rendering.HighDefinition
{
    class ReflectionMenuItems
    {
        [MenuItem("GameObject/3D Object/Mirror", priority = CoreUtils.Priorities.gameObjectMenuPriority)]
        static void CreateMirrorGameObject(MenuCommand menuCommand)
        {
            GameObject plane = GameObject.CreatePrimitive(PrimitiveType.Plane);
            GameObjectUtility.SetParentAndAlign(plane, menuCommand.context as GameObject);
            Undo.RegisterCreatedObjectUndo(plane, "Create " + plane.name);
            Selection.activeObject = plane;

            var planarProbe = plane.AddComponent<PlanarReflectionProbe>();
            planarProbe.influenceVolume.boxSize = new Vector3(10, 0.01f, 10);

            var material = HDRenderPipelineGlobalSettings.instance?.GetDefaultMirrorMaterial();
            if (material)
            {
                plane.GetComponent<MeshRenderer>().sharedMaterial = material;
            }
        }

        [MenuItem("GameObject/Light/Planar Reflection Probe", priority = 22)]
        static void CreatePlanarReflectionGameObject(MenuCommand menuCommand)
        {
            var parent = menuCommand.context as GameObject;
            var go = CoreEditorUtils.CreateGameObject("Planar Reflection", parent);
            var planarProbe = go.AddComponent<PlanarReflectionProbe>();
            planarProbe.influenceVolume.boxSize = new Vector3(1, 0.01f, 1);
        }
    }
}
