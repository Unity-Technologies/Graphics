using System;
using System.Collections.Generic;
using UnityEngine.Assertions;
using UnityEngine.SceneManagement;

namespace UnityEngine.Rendering.HighDefinition
{
    class SceneObjectIDMap
    {
        public static bool TryGetSceneObjectID<TCategory>(GameObject gameObject, out int index, out TCategory category)
            where TCategory : struct, IConvertible
        {
            if (!typeof(TCategory).IsEnum)
                throw new ArgumentException("'TCategory' must be an Enum type.");

            if (gameObject == null)
                throw new ArgumentNullException("gameObject");

            index = default;
            category = default;

            return TryGetOrCreateSceneIDMapFor(gameObject.scene, out SceneObjectIDMapSceneAsset map)
                && map.TryGetSceneIDFor(gameObject, out index, out category);
        }

        public static int GetOrCreateSceneObjectID<TCategory>(GameObject gameObject, TCategory category)
            where TCategory : struct, IConvertible
        {
            if (!typeof(TCategory).IsEnum)
                throw new ArgumentException("'TCategory' must be an Enum type.");

            if (gameObject == null)
                throw new ArgumentNullException("gameObject");

            if (!TryGetOrCreateSceneIDMapFor(gameObject.scene, out SceneObjectIDMapSceneAsset map))
                throw new ArgumentException($"Provided GameObject {gameObject} does not belong to a loaded scene.");

            if (!map.TryGetSceneIDFor(gameObject, out int index, out TCategory registeredCategory))
            {
                var insertion = map.TryInsert(gameObject, category, out index);
                Assert.IsTrue(insertion);
            }

            return index;
        }

        public static void GetAllIDsForAllScenes<TCategory>(
            TCategory category,
            List<GameObject> outGameObjects, List<int> outIndices, List<Scene> outScenes
        )
            where TCategory : struct, IConvertible
        {
            if (outGameObjects == null)
                throw new ArgumentNullException("outGameObjects");
            if (outIndices == null)
                throw new ArgumentNullException("outIndices");
            if (outIndices == null)
                throw new ArgumentNullException("outScenes");

            var lastCount = outGameObjects.Count;
            for (int i = 0; i < SceneManager.sceneCount; ++i)
            {
                var scene = SceneManager.GetSceneAt(i);
                GetAllIDsFor(category, scene, outGameObjects, outIndices);
                for (int j = 0, c = outGameObjects.Count - lastCount; j < c; ++j)
                    outScenes.Add(scene);
            }
        }

        public static void GetAllIDsFor<TCategory>(
            TCategory category, Scene scene,
            List<GameObject> outGameObjects, List<int> outIndices
        )
            where TCategory : struct, IConvertible
        {
            if (outGameObjects == null)
                throw new ArgumentNullException("outGameObjects");
            if (outIndices == null)
                throw new ArgumentNullException("outIndices");

            if (TryGetSceneIDMapFor(scene, out SceneObjectIDMapSceneAsset map))
                map.GetALLIDsFor(category, outGameObjects, outIndices);
        }

        static bool TryGetSceneIDMapFor(Scene scene, out SceneObjectIDMapSceneAsset map)
        {
            if (!scene.isLoaded)
            {
                map = default;
                return false;
            }

            var roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; ++i)
            {
                if (roots[i].name == SceneObjectIDMapSceneAsset.k_GameObjectName
                    && (map = roots[i].GetComponent<SceneObjectIDMapSceneAsset>()) != null
                    && !map.Equals(null))
                    return true;
            }
            map = null;
            return false;
        }

        static SceneObjectIDMapSceneAsset CreateSceneIDMapFor(Scene scene)
        {
            var gameObject = new GameObject(SceneObjectIDMapSceneAsset.k_GameObjectName)
            {
                hideFlags = HideFlags.DontSaveInBuild
                | HideFlags.HideInHierarchy
                | HideFlags.HideInInspector
            };
            var result = gameObject.AddComponent<SceneObjectIDMapSceneAsset>();
            SceneManager.MoveGameObjectToScene(gameObject, scene);
            return result;
        }

        static bool TryGetOrCreateSceneIDMapFor(Scene scene, out SceneObjectIDMapSceneAsset map)
        {
            if (!scene.isLoaded)
            {
                map = default;
                return false;
            }

            if (!TryGetSceneIDMapFor(scene, out map))
                map = CreateSceneIDMapFor(scene);

            return true;
        }
    }
}
