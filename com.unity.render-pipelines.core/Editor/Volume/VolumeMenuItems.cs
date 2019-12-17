using UnityEngine;
using UnityEngine.Rendering;

namespace UnityEditor.Rendering
{
    static class VolumeMenuItems
    {
        const string k_VolumeRootMenu = "GameObject/Volume/";

        [MenuItem(k_VolumeRootMenu + "Global Volume", priority = CoreUtils.gameObjectMenuPriority)]
        static void CreateGlobalVolume(MenuCommand menuCommand)
        {
            var go = CoreEditorUtils.CreateGameObject("Global Volume", menuCommand.context);
            var volume = go.AddComponent<Volume>();
            volume.isGlobal = true;
        }

        [MenuItem(k_VolumeRootMenu + "Box Volume", priority = CoreUtils.gameObjectMenuPriority)]
        static void CreateBoxVolume(MenuCommand menuCommand)
        {
            var go = CoreEditorUtils.CreateGameObject("Box Volume", menuCommand.context);
            var collider = go.AddComponent<BoxCollider>();
            collider.isTrigger = true;
            var volume = go.AddComponent<Volume>();
            volume.isGlobal = false;
            volume.blendDistance = 1f;
        }

        [MenuItem(k_VolumeRootMenu + "Sphere Volume", priority = CoreUtils.gameObjectMenuPriority)]
        static void CreateSphereVolume(MenuCommand menuCommand)
        {
            var go = CoreEditorUtils.CreateGameObject("Sphere Volume", menuCommand.context);
            var collider = go.AddComponent<SphereCollider>();
            collider.isTrigger = true;
            var volume = go.AddComponent<Volume>();
            volume.isGlobal = false;
            volume.blendDistance = 1f;
        }

        [MenuItem(k_VolumeRootMenu + "Convex Mesh Volume", priority = CoreUtils.gameObjectMenuPriority)]
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
