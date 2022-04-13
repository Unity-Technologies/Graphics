using System;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Experimental.Rendering.Universal;

namespace UnityEditor.Rendering.Universal
{
    static class GameObjectCreation
    {
        const int k_PixelPerfectCameraGameObjectMenuPriority = 5;

        [MenuItem("GameObject/2D Object/Pixel Perfect Camera (URP)", priority = k_PixelPerfectCameraGameObjectMenuPriority)]
        static void GameObjectCreatePixelPerfectCamera(MenuCommand menuCommand)
        {
            var go = CreateGameObject("Pixel Perfect Camera", menuCommand, new[] { typeof(PixelPerfectCamera) });
            go.GetComponent<PixelPerfectCamera>().gridSnapping = PixelPerfectCamera.GridSnapping.PixelSnapping;
        }

        static public GameObject CreateGameObject(string name, MenuCommand menuCommand, params Type[] components)
        {
            var parent = menuCommand.context as GameObject;
            var newGO = ObjectFactory.CreateGameObject(name, components);
            newGO.name = name;
            Selection.activeObject = newGO;
            Place(newGO, parent);
            if (EditorSettings.defaultBehaviorMode == EditorBehaviorMode.Mode2D)
            {
                var position = newGO.transform.position;
                position.z = 0;
                newGO.transform.position = position;
            }
            Undo.RegisterCreatedObjectUndo(newGO, string.Format("Create {0}", name));
            return newGO;
        }

        internal static void Place(GameObject go, GameObject parentTransform)
        {
            if (parentTransform != null)
            {
                var transform = go.transform;
                Undo.SetTransformParent(transform, parentTransform.transform, "Reparenting");
                transform.localPosition = Vector3.zero;
                transform.localRotation = Quaternion.identity;
                transform.localScale = Vector3.one;
                go.layer = parentTransform.gameObject.layer;

                if (parentTransform.GetComponent<RectTransform>())
                    ObjectFactory.AddComponent<RectTransform>(go);
            }
            else
            {
                PlaceGameObjectInFrontOfSceneView(go);

                StageUtility.PlaceGameObjectInCurrentStage(go); // may change parent
            }

            // Only at this point do we know the actual parent of the object and can modify its name accordingly.
            GameObjectUtility.EnsureUniqueNameForSibling(go);
            Undo.SetCurrentGroupName("Create " + go.name);
            Selection.activeGameObject = go;
        }

        internal static void PlaceGameObjectInFrontOfSceneView(GameObject go)
        {
            var view = SceneView.lastActiveSceneView;
            if (view != null)
            {
                view.MoveToView(go.transform);
            }
        }
    }
}
