using UnityEngine;
using UnityEngine.Rendering;

namespace UnityEditor.Rendering
{
    public static class VolumeMenuItems
    {
        const string k_VolumeRootMenu = "GameObject/Volume/";

        static GameObject CreateGameObject(string name, UnityEngine.Object context)
        {
            var parent = context as GameObject;
            var go = CoreEditorUtils.CreateGameObject(parent, name);
            GameObjectUtility.SetParentAndAlign(go, context as GameObject);
            Undo.RegisterCreatedObjectUndo(go, "Create " + go.name);
            Selection.activeObject = go;
            EditorApplication.ExecuteMenuItem("GameObject/Move To View");
            return go;
        }

        [MenuItem(k_VolumeRootMenu + "Global Volume", priority = CoreUtils.gameObjectMenuPriority)]
        static void CreateGlobalVolume(MenuCommand menuCommand)
        {
            var go = CreateGameObject("Global Volume", menuCommand.context);
            var volume = go.AddComponent<Volume>();
            volume.isGlobal = true;
        }

        [MenuItem(k_VolumeRootMenu + "Box Volume", priority = CoreUtils.gameObjectMenuPriority)]
        static void CreateBoxVolume(MenuCommand menuCommand)
        {
            var go = CreateGameObject("Box Volume", menuCommand.context);
            var collider = go.AddComponent<BoxCollider>();
            collider.isTrigger = true;
            var volume = go.AddComponent<Volume>();
            volume.isGlobal = false;
            volume.blendDistance = 1f;
        }

        [MenuItem(k_VolumeRootMenu + "Sphere Volume", priority = CoreUtils.gameObjectMenuPriority)]
        static void CreateSphereVolume(MenuCommand menuCommand)
        {
            var go = CreateGameObject("Sphere Volume", menuCommand.context);
            var collider = go.AddComponent<SphereCollider>();
            collider.isTrigger = true;
            var volume = go.AddComponent<Volume>();
            volume.isGlobal = false;
            volume.blendDistance = 1f;
        }

        [MenuItem(k_VolumeRootMenu + "Convex Mesh Volume", priority = CoreUtils.gameObjectMenuPriority)]
        static void CreateConvexMeshVolume(MenuCommand menuCommand)
        {
            var go = CreateGameObject("Convex Mesh Volume", menuCommand.context);
            var collider = go.AddComponent<MeshCollider>();
            collider.convex = true;
            collider.isTrigger = true;
            var volume = go.AddComponent<Volume>();
            volume.isGlobal = false;
            volume.blendDistance = 1f;
        }
    }
}
