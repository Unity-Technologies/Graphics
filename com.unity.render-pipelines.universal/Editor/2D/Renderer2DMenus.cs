using System;
using System.IO;
using System.ComponentModel;
using System.Linq;

using UnityEngine;
using UnityEngine.Experimental.Rendering.Universal;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Scripting.APIUpdating;

using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.ProjectWindowCallback;
using UnityEditorInternal;


namespace UnityEditor.Experimental.Rendering.Universal
{
    static class Renderer2DMenus
    {
        const int k_MenuPriority = 50;

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
                instance.postProcessData = PostProcessData.GetDefaultPostProcessData();
                AssetDatabase.CreateAsset(instance, pathName);
                Selection.activeObject = instance;

                onCreated?.Invoke(instance);
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

        static void CreateLight(MenuCommand menuCommand, Light2D.LightType type, Vector3[] shapePath = null)
        {
            GameObject go = ObjectFactory.CreateGameObject("Light 2D", typeof(Light2D));
            Light2D light2D = go.GetComponent<Light2D>();
            light2D.lightType = type;

            if (shapePath != null && shapePath.Length > 0)
                light2D.shapePath = shapePath;

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

        [MenuItem("GameObject/Light/Freeform Light 2D/Square", false, k_MenuPriority)]
        static void CreateSquareFreeformLight2D(MenuCommand menuCommand)
        {
            CreateLight(menuCommand, Light2D.LightType.Freeform, FreeformPathPresets.CreateSquare());
        }

        [MenuItem("GameObject/Light/Freeform Light 2D/Circle", false, k_MenuPriority)]
        static void CreateCircleFreeformLight2D(MenuCommand menuCommand)
        {
            CreateLight(menuCommand, Light2D.LightType.Freeform, FreeformPathPresets.CreateCircle());
        }

        [MenuItem("GameObject/Light/Freeform Light 2D/Isometric Diamond", false, k_MenuPriority)]
        static void CreateIsometricDiamondFreeformLight2D(MenuCommand menuCommand)
        {
            CreateLight(menuCommand, Light2D.LightType.Freeform, FreeformPathPresets.CreateIsometricDiamond());
        }

        [MenuItem("GameObject/Light/Freeform Light 2D/Hexagon Flat Top", false, k_MenuPriority)]
        static void CreateHexagonFlatTopFreeformLight2D(MenuCommand menuCommand)
        {
            CreateLight(menuCommand, Light2D.LightType.Freeform, FreeformPathPresets.CreateHexagonFlatTop());
        }

        [MenuItem("GameObject/Light/Freeform Light 2D/Hexagon Pointed Top", false, k_MenuPriority)]
        static void CreateHexagonPointedTopFreeformLight2D(MenuCommand menuCommand)
        {
            CreateLight(menuCommand, Light2D.LightType.Freeform, FreeformPathPresets.CreateHexagonPointedTop());
        }

        [MenuItem("GameObject/Light/Sprite Light 2D", false, k_MenuPriority)]
        static void CreateSpriteLight2D(MenuCommand menuCommand)
        {
            CreateLight(menuCommand, Light2D.LightType.Sprite);
        }

        [MenuItem("GameObject/Light/Spot Light 2D", false, k_MenuPriority)]
        static void CreatePointLight2D(MenuCommand menuCommand)
        {
            CreateLight(menuCommand, Light2D.LightType.Point);
        }

        [MenuItem("GameObject/Light/Global Light 2D", false, k_MenuPriority)]
        static void CreateGlobalLight2D(MenuCommand menuCommand)
        {
            CreateLight(menuCommand, Light2D.LightType.Global);
        }

        [MenuItem("GameObject/Light/Freeform Light 2D/Isometric Diamond", true, k_MenuPriority)]
        [MenuItem("GameObject/Light/Freeform Light 2D/Square", true, k_MenuPriority)]
        [MenuItem("GameObject/Light/Freeform Light 2D/Circle", true, k_MenuPriority)]
        [MenuItem("GameObject/Light/Freeform Light 2D/Hexagon Flat Top", true, k_MenuPriority)]
        [MenuItem("GameObject/Light/Freeform Light 2D/Hexagon Pointed Top", true, k_MenuPriority)]
        [MenuItem("GameObject/Light/Sprite Light 2D", true, k_MenuPriority)]
        [MenuItem("GameObject/Light/Spot Light 2D", true, k_MenuPriority)]
        [MenuItem("GameObject/Light/Global Light 2D", true, k_MenuPriority)]
        static bool CreateLight2DValidation()
        {
            return CreateLightValidation();
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1812")]
        internal class CreateUniversalPipelineAsset : EndNameEditAction
        {
            public override void Action(int instanceId, string pathName, string resourceFile)
            {
                //Create asset
                AssetDatabase.CreateAsset(UniversalRenderPipelineAsset.Create(UniversalRenderPipelineAsset.CreateRendererAsset(pathName, RendererType._2DRenderer)), pathName);
            }
        }

        [MenuItem("Assets/Create/Rendering/Universal Render Pipeline/Pipeline Asset (2D Renderer)", priority = CoreUtils.assetCreateMenuPriority1 + 1)]
        static void CreateUniversalPipeline()
        {
            ProjectWindowUtil.StartNameEditingIfProjectWindowExists(0, UniversalRenderPipelineAsset.CreateInstance<CreateUniversalPipelineAsset>(),
                "UniversalRenderPipelineAsset.asset", null, null);
        }

        [MenuItem("Assets/Create/Rendering/Universal Render Pipeline/2D Renderer", priority = CoreUtils.assetCreateMenuPriority2 + 1)]
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
