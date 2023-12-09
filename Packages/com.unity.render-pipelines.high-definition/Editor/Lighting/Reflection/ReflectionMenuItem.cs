using UnityEditor.Rendering;
using UnityEditor.SceneManagement;
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
            GameObject parent = menuCommand.context as GameObject;
            if (parent == null)
            {
                plane.transform.position = Vector3.zero;
                StageUtility.PlaceGameObjectInCurrentStage(plane);
            }
            else
            {
                GameObjectUtility.SetParentAndAlign(plane, parent);
            }
            Undo.RegisterCreatedObjectUndo(plane, "Create " + plane.name);
            Selection.activeObject = plane;

            var planarProbe = plane.AddComponent<PlanarReflectionProbe>();
            planarProbe.influenceVolume.boxSize = new Vector3(10, 0.01f, 10);

            // Disable the influence volume as proxy by default for planar reflection. We want it enabled by default for
            // normal HD probes, but for planar reflections it can cause some undesirable default results.
            planarProbe.useInfluenceVolumeAsProxyVolume = false;

            if (GraphicsSettings.TryGetRenderPipelineSettings<HDRenderPipelineEditorMaterials>(out var defaultMaterials))
            {
                plane.GetComponent<MeshRenderer>().sharedMaterial = defaultMaterials.defaultMirrorMaterial;
            }
            else
            {
                Debug.LogWarning($"{plane.name} is missing the {nameof(MeshRenderer.sharedMaterial)} due to not being able to find {nameof(HDRenderPipelineEditorMaterials.defaultMirrorMaterial)}.");
            }
        }

        [MenuItem("GameObject/Light/Planar Reflection Probe", priority = 22)]
        static void CreatePlanarReflectionGameObject(MenuCommand menuCommand)
        {
            var parent = menuCommand.context as GameObject;
            var go = CoreEditorUtils.CreateGameObject("Planar Reflection", parent);
            var planarProbe = go.AddComponent<PlanarReflectionProbe>();
            planarProbe.influenceVolume.boxSize = new Vector3(1, 0.01f, 1);

            // Disable the influence volume as proxy by default for planar reflection. We want it enabled by default for
            // normal HD probes, but for planar reflections it can cause some undesirable default results.
            planarProbe.useInfluenceVolumeAsProxyVolume = false;
        }

        [MenuItem("GameObject/Volume/Reflection Proxy Volume", priority =  12)]
        static void CreateReflectionProxyVolumeGameObject(MenuCommand menuCommand)
        {
            var parent = menuCommand.context as GameObject;
            var go = CoreEditorUtils.CreateGameObject("Reflection Proxy Volume", parent);
            var proxyVolume = go.AddComponent<ReflectionProxyVolumeComponent>();
        }
    }
}
