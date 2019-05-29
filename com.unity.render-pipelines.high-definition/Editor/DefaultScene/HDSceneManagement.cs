using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering.HDPipeline;
using System;
using System.Reflection;
using System.Linq.Expressions;


namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    [InitializeOnLoad]
    public class HDSceneManagement : UnityEditor.AssetPostprocessor
    {
        static Func<string, bool> s_CreateEmptySceneAsset;

        static HDSceneManagement()
        {
            EditorSceneManager.newSceneCreated += NewSceneCreated;

            var scenePathProperty = Expression.Parameter(typeof(string), "scenePath");
            var createSceneAssetInfo = typeof(EditorSceneManager)
                .GetMethod(
                    "CreateSceneAsset",
                    BindingFlags.NonPublic | BindingFlags.Static,
                    null,
                    CallingConventions.Any,
                    new[] { typeof(string), typeof(bool) },
                    null);
            var createSceneAssetCall = Expression.Call(
                createSceneAssetInfo,
                scenePathProperty,
                Expression.Constant(false)
                );
            var lambda = Expression.Lambda<Func<string, bool>>(createSceneAssetCall, scenePathProperty);
            s_CreateEmptySceneAsset = lambda.Compile();
        }

        static void NewSceneCreated(Scene scene, NewSceneSetup setup, NewSceneMode mode)
        {
            if (!InHDRP())
                return; // do not interfere outside of hdrp

            if (HDProjectSettings.defaultScenePrefab == null)
            {
                Debug.LogWarning("Default Scene not set! Please run Wizard...");
                return;
            }

            if (setup == NewSceneSetup.DefaultGameObjects)
            {
                ClearScene(scene);
                FillScene(scene);
            }
        }

        // Note: Currently we do not add Empty scene in the HDRP package.
        // But if you need it for personal use, you can uncomment the following (1/2):

        //[MenuItem("File/New Empty Scene", true, 148)]
        //[MenuItem("File/New Empty Scene Additive", true, 149)]
        //[MenuItem("Assets/Create/Empty Scene", true, 199)]
        [MenuItem("Assets/Create/HD Template Scene", true, 200)]
        static bool InHDRP()
        {
            return GraphicsSettings.renderPipelineAsset is HDRenderPipelineAsset;
        }

        // Note: Currently we do not add Empty scene in the HDRP package.
        // But if you need it for personal use, you can uncomment the following (2/2):

        //[MenuItem("File/New Empty Scene", false, 148)]
        //static void CreateEmptyScene()
        //{
        //    if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        //        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene);
        //}

        //[MenuItem("File/New Empty Scene Additive", false, 149)]
        //static void CreateEmptySceneAdditive()
        //{
        //    EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        //}

        //[MenuItem("Assets/Create/Empty Scene", false, 199)]
        //static void CreateEmptySceneAsset()
        //{
        //    //cannot use ProjectWindowUtil.CreateScene() as it will fill the scene with Default
        //    var icon = EditorGUIUtility.FindTexture("SceneAsset Icon");
        //    ProjectWindowUtil.StartNameEditingIfProjectWindowExists(0, ScriptableObject.CreateInstance<DoCreateEmptyScene>(), "New Scene.unity", icon, null);
        //}

        [MenuItem("Assets/Create/HD Template Scene", false, 200)]
        static void CreateHDSceneAsset()
        {
            //cannot use ProjectWindowUtil.CreateScene() as it will fill the scene with Default
            var icon = EditorGUIUtility.FindTexture("SceneAsset Icon");
            ProjectWindowUtil.StartNameEditingIfProjectWindowExists(0, ScriptableObject.CreateInstance<DoCreateTemplateScene>(), "New Scene.unity", icon, null);
        }

        class DoCreateEmptyScene : UnityEditor.ProjectWindowCallback.EndNameEditAction
        {
            public override void Action(int instanceId, string pathName, string resourceFile)
            {
                if (s_CreateEmptySceneAsset(pathName))
                {
                    UnityEngine.Object sceneAsset = AssetDatabase.LoadAssetAtPath(pathName, typeof(SceneAsset));
                    ProjectWindowUtil.ShowCreatedAsset(sceneAsset);
                }
            }
        }

        class DoCreateTemplateScene : UnityEditor.ProjectWindowCallback.EndNameEditAction
        {
            public override void Action(int instanceId, string pathName, string resourceFile)
            {
                if (HDProjectSettings.defaultScenePrefab == null)
                {
                    Debug.LogWarning("Default Scene not set! Please run Wizard...");
                    return;
                }

                if (s_CreateEmptySceneAsset(pathName))
                {
                    UnityEngine.Object sceneAsset = AssetDatabase.LoadAssetAtPath(pathName, typeof(SceneAsset));
                    ProjectWindowUtil.ShowCreatedAsset(sceneAsset);

                    Scene scene = EditorSceneManager.OpenScene(pathName, OpenSceneMode.Additive);
                    FillScene(scene);
                    EditorSceneManager.SaveScene(scene);
                    EditorSceneManager.CloseScene(scene, true);
                }
            }
        }

        static void ClearScene(Scene scene)
        {
            GameObject[] gameObjects = scene.GetRootGameObjects();
            for (int index = gameObjects.Length - 1; index >= 0; --index)
            {
                GameObject.DestroyImmediate(gameObjects[index]);
            }
        }

        static void FillScene(Scene scene)
        {
            HDRenderPipelineAsset hdrpAsset = (GraphicsSettings.renderPipelineAsset as HDRenderPipelineAsset);
            if (hdrpAsset == null || hdrpAsset.Equals(null))
                return;

            if (hdrpAsset.renderPipelineEditorResources == null)
            {
                Debug.LogError("Missing HDRenderPipelineEditorResources in HDRenderPipelineAsset");
                return;
            }

            GameObject root = GameObject.Instantiate(HDProjectSettings.defaultScenePrefab);
            SceneManager.MoveGameObjectToScene(root, scene);
            root.transform.DetachChildren();
            GameObject.DestroyImmediate(root);
        }
    }
}
