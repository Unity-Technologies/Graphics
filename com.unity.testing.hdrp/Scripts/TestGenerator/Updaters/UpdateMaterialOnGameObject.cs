using System.Collections.Generic;
using UnityEditor;

namespace UnityEngine.Experimental.Rendering.HDPipelineTest.TestGenerator
{
    // Prototype
    [AddComponentMenu("TestGenerator/Updaters/Update Material On GameObjects")]
    public class UpdateMaterialOnGameObject : MonoBehaviour, IUpdateGameObjects
    {
        public ExecuteMode executeMode => m_ExecuteMode;

        public void UpdateInPlayMode(Transform parent, List<GameObject> instances)
        {
            UpdateInstances(instances);
        }

        public void UpdateInEditMode(Transform parent, List<GameObject> instances)
        {
            UpdateInstances(instances);
        }

        void UpdateInstances(List<GameObject> instances)
        {
#if UNITY_EDITOR
            AssetDatabase.StartAssetEditing();
#endif
            var c = Mathf.Min(instances.Count, m_Modifications.Length);
            for (var i = 0; i < c; ++i)
            {
                var instance = instances[i];
                if (instance == null || instance.Equals(null)) continue;

                var renderer = instance.GetComponent<Renderer>();
                if (renderer == null || renderer.Equals(null)) continue;

                var material = m_UseSharedMaterial ? renderer.sharedMaterial : renderer.material;
                if (material == null || material.Equals(null)) continue;

                var modifications = m_Modifications[i];
                foreach (var modification in modifications.modifications)
                    modification.ApplyTo(material);
#if UNITY_EDITOR
                EditorUtility.SetDirty(material);
#endif
            }
#if UNITY_EDITOR
            AssetDatabase.StopAssetEditing();
#endif
        }
#pragma warning disable 649
        [Tooltip("When to execute this updater.")] [SerializeField]
        ExecuteMode m_ExecuteMode = ExecuteMode.All;

        [Tooltip("Tick this to use a shared material.")] [SerializeField]
        bool m_UseSharedMaterial = true;

        [Tooltip("The modifications to apply to the material.")] [SerializeField]
        MaterialModificationList[] m_Modifications;
#pragma warning restore 649
    }
}
