using System;
using UnityEditor.ProjectWindowCallback;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering;
using System.IO;
using UnityEngine.Analytics;


namespace UnityEditor.Rendering.Universal
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
                var instance = CreateRendererAsset(pathName, RendererType._2DRenderer, false) as Renderer2DData;
                Selection.activeObject = instance;

                onCreated?.Invoke(instance);
            }
        }

        static ScriptableRendererData CreateRendererAsset(string path, RendererType type, bool relativePath = true, string suffix = "Renderer")
        {
            string packagePath = "Packages/com.unity.render-pipelines.universal";

            ScriptableRendererData data = CreateRendererData(type);
            string dataPath;
            if (relativePath)
                dataPath =
                    $"{Path.Combine(Path.GetDirectoryName(path), Path.GetFileNameWithoutExtension(path))}_{suffix}{Path.GetExtension(path)}";
            else
                dataPath = path;
            AssetDatabase.CreateAsset(data, dataPath);
            ResourceReloader.ReloadAllNullIn(data, packagePath);
            return data;
        }

        static ScriptableRendererData CreateRendererData(RendererType type)
        {
            var rendererData = ScriptableObject.CreateInstance<Renderer2DData>();
            rendererData.postProcessData = PostProcessData.GetDefaultPostProcessData();
            return rendererData;
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

        static Light2D CreateLight(MenuCommand menuCommand, Light2D.LightType type, Vector3[] shapePath = null)
        {
            var lightName = type != Light2D.LightType.Point ? type.ToString() : "Spot";
            GameObject go = ObjectFactory.CreateGameObject(lightName + " Light 2D", typeof(Light2D));
            Light2D light2D = go.GetComponent<Light2D>();
            light2D.batchSlotIndex = LightBatch.batchSlotIndex;
            light2D.lightType = type;

            if (shapePath != null && shapePath.Length > 0)
                light2D.shapePath = shapePath;

            var parent = menuCommand.context as GameObject;
            Place(go, parent);

            Analytics.LightDataAnalytic lightData = new Analytics.LightDataAnalytic(light2D.GetInstanceID(), true, light2D.lightType);
            Analytics.Renderer2DAnalytics.instance.SendData(lightData);

            return light2D;
        }

        static bool CreateLightValidation()
        {
            return Light2DEditorUtility.IsUsing2DRenderer();
        }

        [MenuItem("GameObject/Light/Freeform Light 2D/Square", priority = CoreUtils.Sections.section3 + CoreUtils.Priorities.gameObjectMenuPriority + 4)]
        static void CreateSquareFreeformLight2D(MenuCommand menuCommand)
        {
            CreateLight(menuCommand, Light2D.LightType.Freeform, FreeformPathPresets.CreateSquare());
        }

        [MenuItem("GameObject/Light/Freeform Light 2D/Circle", priority = CoreUtils.Sections.section3 + CoreUtils.Priorities.gameObjectMenuPriority + 5)]
        static void CreateCircleFreeformLight2D(MenuCommand menuCommand)
        {
            CreateLight(menuCommand, Light2D.LightType.Freeform, FreeformPathPresets.CreateCircle());
        }

        [MenuItem("GameObject/Light/Freeform Light 2D/Isometric Diamond", priority = CoreUtils.Sections.section3 + CoreUtils.Priorities.gameObjectMenuPriority + 6)]
        static void CreateIsometricDiamondFreeformLight2D(MenuCommand menuCommand)
        {
            CreateLight(menuCommand, Light2D.LightType.Freeform, FreeformPathPresets.CreateIsometricDiamond());
        }

        [MenuItem("GameObject/Light/Freeform Light 2D/Hexagon Flat Top", priority = CoreUtils.Sections.section3 + CoreUtils.Priorities.gameObjectMenuPriority + 7)]
        static void CreateHexagonFlatTopFreeformLight2D(MenuCommand menuCommand)
        {
            CreateLight(menuCommand, Light2D.LightType.Freeform, FreeformPathPresets.CreateHexagonFlatTop());
        }

        [MenuItem("GameObject/Light/Freeform Light 2D/Hexagon Pointed Top", priority = CoreUtils.Sections.section3 + CoreUtils.Priorities.gameObjectMenuPriority + 8)]
        static void CreateHexagonPointedTopFreeformLight2D(MenuCommand menuCommand)
        {
            CreateLight(menuCommand, Light2D.LightType.Freeform, FreeformPathPresets.CreateHexagonPointedTop());
        }

        [MenuItem("GameObject/Light/Sprite Light 2D", priority = CoreUtils.Sections.section3 + CoreUtils.Priorities.gameObjectMenuPriority + 1)]
        static void CreateSpriteLight2D(MenuCommand menuCommand)
        {
            Light2D light = CreateLight(menuCommand, Light2D.LightType.Sprite);
            ResourceReloader.ReloadAllNullIn(light, UniversalRenderPipelineAsset.packagePath);
        }

        [MenuItem("GameObject/Light/Spot Light 2D", priority = CoreUtils.Sections.section3 + CoreUtils.Priorities.gameObjectMenuPriority + 2)]
        static void CreatePointLight2D(MenuCommand menuCommand)
        {
            CreateLight(menuCommand, Light2D.LightType.Point);
        }

        [MenuItem("GameObject/Light/Global Light 2D", priority = CoreUtils.Sections.section3 + CoreUtils.Priorities.gameObjectMenuPriority + 3)]
        static void CreateGlobalLight2D(MenuCommand menuCommand)
        {
            CreateLight(menuCommand, Light2D.LightType.Global);
        }

        [MenuItem("GameObject/Light/Freeform Light 2D/Isometric Diamond", true)]
        [MenuItem("GameObject/Light/Freeform Light 2D/Square", true)]
        [MenuItem("GameObject/Light/Freeform Light 2D/Circle", true)]
        [MenuItem("GameObject/Light/Freeform Light 2D/Hexagon Flat Top", true)]
        [MenuItem("GameObject/Light/Freeform Light 2D/Hexagon Pointed Top", true)]
        [MenuItem("GameObject/Light/Sprite Light 2D", true)]
        [MenuItem("GameObject/Light/Spot Light 2D", true)]
        [MenuItem("GameObject/Light/Global Light 2D", true)]
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
                AssetDatabase.CreateAsset(UniversalRenderPipelineAsset.Create(CreateRendererAsset(pathName, RendererType._2DRenderer)), pathName);
            }
        }

        [MenuItem("Assets/Create/Rendering/URP Asset (with 2D Renderer)", priority = CoreUtils.Sections.section2 + CoreUtils.Priorities.assetsCreateRenderingMenuPriority)]
        static void CreateUniversalPipeline()
        {
            ProjectWindowUtil.StartNameEditingIfProjectWindowExists(0, UniversalRenderPipelineAsset.CreateInstance<CreateUniversalPipelineAsset>(),
                "New Universal Render Pipeline Asset.asset", null, null);
        }

        [MenuItem("Assets/Create/Rendering/URP 2D Renderer", priority = CoreUtils.Sections.section3 + CoreUtils.Priorities.assetsCreateRenderingMenuPriority + 1)]
        static void Create2DRendererData()
        {
            Renderer2DMenus.Create2DRendererData((instance) =>
            {
                Analytics.RenderAssetAnalytic modifiedData = new Analytics.RenderAssetAnalytic(instance.GetInstanceID(), true, 1, 2);
                Analytics.Renderer2DAnalytics.instance.SendData(modifiedData);
            });
        }
    }
}
