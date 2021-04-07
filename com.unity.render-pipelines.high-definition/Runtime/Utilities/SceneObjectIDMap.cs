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

    class SceneObjectIDMapSceneAsset : MonoBehaviour, ISerializationCallbackReceiver
    {
        internal const string k_GameObjectName = "SceneIDMap";

        [Serializable]
        struct Entry
        {
            public int id;
            public int category;
            public GameObject gameObject;
        }

        [SerializeField]
        List<Entry> m_Entries = new List<Entry>();

        Dictionary<GameObject, int> m_IndexByGameObject = new Dictionary<GameObject, int>();

        public void GetALLIDsFor<TCategory>(
            TCategory category,
            List<GameObject> outGameObjects, List<int> outIndices
        )
            where TCategory : struct, IConvertible
        {
            if (outGameObjects == null)
                throw new ArgumentNullException("outGameObjects");
            if (outIndices == null)
                throw new ArgumentNullException("outIndices");

            CleanDestroyedGameObjects();

            var intCategory = Convert.ToInt32(category);
            for (int i = m_Entries.Count - 1; i >= 0 ; --i)
            {
                if (m_Entries[i].category != intCategory)
                    continue;

                outIndices.Add(m_Entries[i].id);
                outGameObjects.Add(m_Entries[i].gameObject);
            }
        }

        internal bool TryGetSceneIDFor<TCategory>(GameObject gameObject, out int index, out TCategory category)
            where TCategory : struct, IConvertible
        {
            if (!typeof(TCategory).IsEnum)
                throw new ArgumentException("'TCategory' must be an Enum type.");

            if (gameObject == null)
                throw new ArgumentNullException("gameObject");

            int entryIndex;
            if (m_IndexByGameObject.TryGetValue(gameObject, out entryIndex))
            {
                if (entryIndex < m_Entries.Count)
                {
                    category = (TCategory)(object)m_Entries[entryIndex].category;
                    index = m_Entries[entryIndex].id;
                    return true;
                }
                else
                    m_IndexByGameObject.Remove(gameObject);
            }
            category = default(TCategory);
            index = -1;
            return false;
        }

        internal bool TryInsert<TCategory>(GameObject gameObject, TCategory category, out int index)
            where TCategory : struct, IConvertible
        {
            if (!typeof(TCategory).IsEnum)
                throw new ArgumentException("'TCategory' must be an Enum type.");

            if (gameObject == null)
                throw new ArgumentNullException("gameObject");

            if (gameObject.scene != this.gameObject.scene)
            {
                index = -1;
                return false;
            }
            TCategory registeredCategory;
            if (TryGetSceneIDFor(gameObject, out index, out registeredCategory))
                return false;

            index = Insert(gameObject, category);
            return true;
        }

        int Insert<TCategory>(GameObject gameObject, TCategory category)
            where TCategory : struct, IConvertible
        {
            Assert.IsFalse(m_IndexByGameObject.ContainsKey(gameObject));
            Assert.AreEqual(gameObject.scene, this.gameObject.scene);

            var entry = new Entry
            {
                gameObject = gameObject,
                category = Convert.ToInt32(category)
            };
            // Sorted insert
            // Insert where there is room between to indices
            var index = -1;
            if (m_Entries.Count > 0 && m_Entries[0].id != 0)
            {
                index = 0;
                entry.id = 0;
            }
            else
            {
                for (int i = 0; i < m_Entries.Count - 1; ++i)
                {
                    if (m_Entries[i].id + 1 == m_Entries[i + 1].id)
                        continue;

                    index = i + 1;
                    entry.id = m_Entries[i].id + 1;
                    break;
                }
            }

            if (index == -1)
            {
                index = m_Entries.Count;
                // entries are full, so the id is the number of entries
                entry.id = m_Entries.Count;
            }

            m_IndexByGameObject.Add(gameObject, index);
            m_Entries.Insert(index, entry);
            for (int i = index + 1; i < m_Entries.Count; ++i)
            {
                // Upon insertion, all index by game object entries after the insertion point need their index updated.
                m_IndexByGameObject[m_Entries[i].gameObject] = i;
            }
            return m_Entries[index].id;
        }

        void ISerializationCallbackReceiver.OnAfterDeserialize()
        {
            BuildIndex();
        }

        void ISerializationCallbackReceiver.OnBeforeSerialize()
        {
            CleanDestroyedGameObjects();
        }

        void CleanDestroyedGameObjects()
        {
            var rebuildIndex = false;
            for (int i = m_Entries.Count - 1; i >= 0; --i)
            {
                // Clean destroyed game object in
                if (m_Entries[i].gameObject == null)
                {
                    m_Entries.RemoveAt(i);
                    rebuildIndex = true;
                }
            }

            if (rebuildIndex)
                BuildIndex();
        }

        void BuildIndex()
        {
            m_IndexByGameObject.Clear();
            for (int i = 0; i < m_Entries.Count; ++i)
                m_IndexByGameObject[m_Entries[i].gameObject] = i;
        }
    }
}
