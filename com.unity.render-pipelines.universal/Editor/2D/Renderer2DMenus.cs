using System;
using UnityEngine;
using UnityEngine.Experimental.Rendering.Universal;
using UnityEngine.Rendering.Universal;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.ProjectWindowCallback;
using UnityEngine.Rendering;

namespace UnityEditor.Experimental.Rendering.Universal
{

    static class Renderer2DMenus
    {
        static void Create2DRendererData(Action<Renderer2DData> onCreatedCallback)
        {
            var instance = ScriptableObject.CreateInstance<Create2DRendererDataAsset>();
            instance.onCreated += onCreatedCallback;
            ProjectWindowUtil.StartNameEditingIfProjectWindowExists(0, instance, "New 2D Renderer Data.asset", null, null);
        }

        class Create2DRendererDataAsset : EndNameEditAction
        {
            public event Action<Renderer2DData> onCreated;

            public override void Action(int instanceId, string pathName, string resourceFile)
            {
                var instance = CreateInstance<Renderer2DData>();
                AssetDatabase.CreateAsset(instance, pathName);
                Selection.activeObject = instance;

                onCreated(instance);
            }
        }

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
                go.transform.position = new Vector3(go.transform.position.x, go.transform.position.y, 0);
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

            Analytics.Light2DData lightData = new Analytics.Light2DData();
            lightData.was_create_event = true;
            lightData.instance_id = light2D.GetInstanceID();
            lightData.light_type = light2D.lightType;
            Analytics.Renderer2DAnalytics.instance.SendData(Analytics.AnalyticsDataTypes.k_LightDataString, lightData);
        }

        static bool CreateLightValidation()
        {
            return Light2DEditorUtility.IsUsing2DRenderer();
        }

        [MenuItem("GameObject/Light/2D/Freeform Light 2D (Experimental)", false, -100)]
        static void CreateFreeformLight2D(MenuCommand menuCommand)
        {
            CreateLight(menuCommand, "Freeform Light 2D", Light2D.LightType.Freeform);
        }

        [MenuItem("GameObject/Light/2D/Freeform Light 2D (Experimental)", true, -100)]
        static bool CreateFreeformLight2DValidation()
        {
            return CreateLightValidation();
        }

        [MenuItem("GameObject/Light/2D/Sprite Light 2D (Experimental)", false, -100)]
        static void CreateSpriteLight2D(MenuCommand menuCommand)
        {
            CreateLight(menuCommand, "Sprite Light 2D", Light2D.LightType.Sprite);
        }
        [MenuItem("GameObject/Light/2D/Sprite Light 2D (Experimental)", true, -100)]
        static bool CreateSpriteLight2DValidation()
        {
            return CreateLightValidation();
        }

        [MenuItem("GameObject/Light/2D/Parametric Light 2D (Experimental)", false, -100)]
        static void CreateParametricLight2D(MenuCommand menuCommand)
        {
            CreateLight(menuCommand, "Parametric Light 2D", Light2D.LightType.Parametric);
        }
        [MenuItem("GameObject/Light/2D/Parametric Light 2D (Experimental)", true, -100)]
        static bool CreateParametricLight2DValidation()
        {
            return CreateLightValidation();
        }

        [MenuItem("GameObject/Light/2D/Point Light 2D (Experimental)", false, -100)]
        static void CreatePointLight2D(MenuCommand menuCommand)
        {
            CreateLight(menuCommand, "Point Light 2D", Light2D.LightType.Point);
        }

        [MenuItem("GameObject/Light/2D/Point Light 2D (Experimental)", true, -100)]
        static bool CreatePointLight2DValidation()
        {
            return CreateLightValidation();
        }

        [MenuItem("GameObject/Light/2D/Global Light 2D (Experimental)", false, -100)]
        static void CreateGlobalLight2D(MenuCommand menuCommand)
        {
            CreateLight(menuCommand, "Global Light 2D", Light2D.LightType.Global);
        }
        [MenuItem("GameObject/Light/2D/Global Light 2D (Experimental)", true, -100)]
        static bool CreateGlobalLight2DValidation()
        {
            return CreateLightValidation();
        }

        [MenuItem("Assets/Create/Rendering/Universal Render Pipeline/2D Renderer (Experimental)", priority = CoreUtils.assetCreateMenuPriority2 + 1)]
        static void Create2DRendererData()
        {
            Renderer2DMenus.Create2DRendererData((instance) =>
            {
                Analytics.RendererAssetData modifiedData = new Analytics.RendererAssetData();
                modifiedData.instance_id = instance.GetInstanceID();
                modifiedData.was_create_event = true;
                modifiedData.blending_layers_count = 1;
                modifiedData.blending_modes_used = 2;
                Analytics.Renderer2DAnalytics.instance.SendData(Analytics.AnalyticsDataTypes.k_Renderer2DDataString, modifiedData);
            });
        }
    }
}
