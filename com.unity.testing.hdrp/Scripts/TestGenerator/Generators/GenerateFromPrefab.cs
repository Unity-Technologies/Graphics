using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering.HDPipelineTest.TestGenerator
{
    // Prototype
    [AddComponentMenu("TestGenerator/Generators/Generate From Prefab")]
    public class GenerateFromPrefab : MonoBehaviour, IGenerateGameObjects
    {
        #pragma warning disable 649
        [SerializeField] int m_Count;
        [SerializeField] GameObject m_Prefab;
        [Tooltip("0: Index, 1: Prefab name")]
        [SerializeField] string m_FormatName = "{1}_{0}";
        #pragma warning restore 649

        public void GenerateInPlayMode(Transform parent, List<GameObject> instances)
        {
            if (m_Prefab == null || m_Prefab.Equals(null)) return;

            // Fill missing instances with prefab
            for (var i = instances.Count; i < m_Count; ++i)
            {
                var instance = Instantiate(m_Prefab, parent, false);
                SetName(instance, i);
                instances.Add(instance);
            }
        }

        public void GenerateInEditMode(Transform parent, List<GameObject> instances)
        {
            if (m_Prefab == null || m_Prefab.Equals(null)) return;

#if UNITY_EDITOR
            // Delete unused game object
            var diff = instances.Count - m_Count;
            for (var i = 0; i < diff; ++i)
            {
                var instance = instances[0];
                if (instance != null && !instance.Equals(null))
                    CoreUtils.Destroy(instances[0]);

                instances.RemoveAt(0);
            }

            // Make sure existing instances are from the expected prefab
            for (var i = 0; i < instances.Count; ++i)
            {
                var instance = instances[i];
                if (instance != null && !instance.Equals(null))
                {
                    var instanceStatus = PrefabUtility.GetPrefabInstanceStatus(instance);
                    switch (instanceStatus)
                    {
                        // We miss the prefab
                        case PrefabInstanceStatus.Disconnected:
                        case PrefabInstanceStatus.MissingAsset:
                        case PrefabInstanceStatus.NotAPrefab:
                        {
                            CoreUtils.Destroy(instance);
                            break;
                        }
                    }

                    // Check the prefab is correct
                    if (instance != null)
                    {
                        var instanceParent = PrefabUtility.GetCorrespondingObjectFromSource(instance);
                        if (instanceParent != m_Prefab)
                            CoreUtils.Destroy(instance);
                    }
                }

                if (instance == null || instance.Equals(null))
                {
                    instance = (GameObject)PrefabUtility.InstantiatePrefab(m_Prefab, parent);
                    SetName(instance, i);
                    instances[i] = instance;
                }
            }

            // Fill missing prefabs
            for (int i = 0, c = m_Count - instances.Count; i < c; ++i)
            {
                var instance = (GameObject) PrefabUtility.InstantiatePrefab(m_Prefab, parent);
                SetName(instance, i);
                instances.Add(instance);
            }
#endif
        }

        void SetName(GameObject instance, int index)
        {
            instance.name = string.Format(m_FormatName, index, m_Prefab.name);
        }
    }
}
