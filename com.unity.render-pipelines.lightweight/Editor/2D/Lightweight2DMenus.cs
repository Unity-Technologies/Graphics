using UnityEngine;
using UnityEngine.Experimental.Rendering.LWRP;
using UnityEngine.Rendering.LWRP;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace UnityEditor.Experimental.Rendering.LWRP
{
    static class Lightweight2DMenus
    {
        internal static void PlaceGameObjectInFrontOfSceneView(GameObject go)
        {
            var sceneViews = SceneView.sceneViews;
            if (sceneViews.Count >= 1)
            {
                SceneView view = SceneView.lastActiveSceneView;
                if (!view)
                    view = sceneViews[0] as SceneView;
                if (view)
                    view.MoveToView(go.transform);
            }
        }


        // This is from GOCreationCommands
        internal static void Place(GameObject go, GameObject parent)
        {
            if (parent != null)
            {
                var transform = go.transform;
                Undo.SetTransformParent(transform, parent.transform, "Reparenting");
                transform.localPosition = Vector3.zero;
                transform.localRotation = Quaternion.identity;
                transform.localScale = Vector3.one;
                go.layer = parent.layer;

                if (parent.GetComponent<RectTransform>())
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

            //EditorWindow.FocusWindowIfItsOpen<SceneHierarchyWindow>();
            Selection.activeGameObject = go;
        }

        static void CreateLight(MenuCommand menuCommand, string name, Light2D.LightType type)
        {
            GameObject go = ObjectFactory.CreateGameObject(name, typeof(Light2D));
            Light2D light2D = go.GetComponent<Light2D>();
            light2D.lightType = type;

            var parent = menuCommand.context as GameObject;
            Place(go, parent);
        }

        static bool CreateLightValidation()
        {
            LightweightRenderPipeline pipeline = UnityEngine.Rendering.RenderPipelineManager.currentPipeline as LightweightRenderPipeline;
            if (pipeline != null)
            {
                LightweightRenderPipelineAsset asset = LightweightRenderPipeline.asset;
                _2DRendererData assetData = asset.scriptableRendererData as _2DRendererData;
                if (assetData != null)
                    return true;
            }

            return false;
        }

        //[MenuItem("GameObject/Light/2D/Freeform Light 2D", false, -100, true)]
        [MenuItem("GameObject/Light/2D/Freeform Light 2D", false, -100)]
        static void CreateFreeformLight2D(MenuCommand menuCommand)
        {
            CreateLight(menuCommand, "Freeform Light 2D", Light2D.LightType.Freeform);
        }

        [MenuItem("GameObject/Light/2D/Freeform Light 2D", true, -100)]
        static bool CreateFreeformLight2DValidation()
        {
            return CreateLightValidation();
        }

        [MenuItem("GameObject/Light/2D/Sprite Light 2D", false, -100)]
        static void CreateSpriteLight2D(MenuCommand menuCommand)
        {
            CreateLight(menuCommand, "Sprite Light 2D", Light2D.LightType.Sprite);
        }
        [MenuItem("GameObject/Light/2D/Sprite Light 2D", true, -100)]
        static bool CreateSpriteLight2DValidation()
        {
            return CreateLightValidation();
        }

        [MenuItem("GameObject/Light/2D/Parametric Light2D", false, -100)]
        static void CreateParametricLight2D(MenuCommand menuCommand)
        {
            CreateLight(menuCommand, "Parametric Light 2D", Light2D.LightType.Parametric);
        }
        [MenuItem("GameObject/Light/2D/Parametric Light2D", true, -100)]
        static bool CreateParametricLight2DValidation()
        {
            return CreateLightValidation();
        }

        [MenuItem("GameObject/Light/2D/Point Light 2D", false, -100)]
        static void CreatePointLight2D(MenuCommand menuCommand)
        {
            CreateLight(menuCommand, "Point Light 2D", Light2D.LightType.Point);
        }

        [MenuItem("GameObject/Light/2D/Point Light 2D", true, -100)]
        static bool CreatePointLight2DValidation()
        {
            return CreateLightValidation();
        }

        [MenuItem("GameObject/Light/2D/Global Light2D", false, -100)]
        static void CreateGlobalLight2D(MenuCommand menuCommand)
        {
            CreateLight(menuCommand, "Global Light 2D", Light2D.LightType.Global);
        }
        [MenuItem("GameObject/Light/2D/Global Light2D", true, -100)]
        static bool CreateGlobalLight2DValidation()
        {
            return CreateLightValidation();
        }
    }
}
