using UnityEngine;
using UnityEngine.Rendering;

namespace UnityEditor.Rendering
{
    static class VolumeMenuItems
    {
        [MenuItem("GameObject/Volume/Global Volume", priority = CoreUtils.Priorities.gameObjectMenuPriority)]
        static void CreateGlobalVolume(MenuCommand menuCommand)
        {
            var go = CoreEditorUtils.CreateGameObject("Global Volume", menuCommand.context);
            var volume = go.AddComponent<Volume>();
            volume.isGlobal = true;
        }

        [MenuItem("GameObject/Volume/Box Volume", priority = CoreUtils.Sections.section1 + CoreUtils.Priorities.gameObjectMenuPriority)]
        static void CreateBoxVolume(MenuCommand menuCommand)
        {
            var go = CoreEditorUtils.CreateGameObject("Box Volume", menuCommand.context);
            var collider = go.AddComponent<BoxCollider>();
            collider.isTrigger = true;
            var volume = go.AddComponent<Volume>();
            volume.isGlobal = false;
            volume.blendDistance = 1f;
        }

        [MenuItem("GameObject/Volume/Sphere Volume", priority = CoreUtils.Sections.section1 + CoreUtils.Priorities.gameObjectMenuPriority + 1)]
        static void CreateSphereVolume(MenuCommand menuCommand)
        {
            var go = CoreEditorUtils.CreateGameObject("Sphere Volume", menuCommand.context);
            var collider = go.AddComponent<SphereCollider>();
            collider.isTrigger = true;
            var volume = go.AddComponent<Volume>();
            volume.isGlobal = false;
            volume.blendDistance = 1f;
        }

        [MenuItem("GameObject/Volume/Convex Mesh Volume", priority = CoreUtils.Sections.section1 + CoreUtils.Priorities.gameObjectMenuPriority + 2)]
        static void CreateConvexMeshVolume(MenuCommand menuCommand)
        {
            var go = CoreEditorUtils.CreateGameObject("Convex Mesh Volume", menuCommand.context);
            var collider = go.AddComponent<MeshCollider>();
            collider.convex = true;
            collider.isTrigger = true;
            var volume = go.AddComponent<Volume>();
            volume.isGlobal = false;
            volume.blendDistance = 1f;
        }
    }
}
