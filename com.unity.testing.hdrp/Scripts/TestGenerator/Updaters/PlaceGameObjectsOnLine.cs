using System;
using System.Collections.Generic;

namespace UnityEngine.Experimental.Rendering.HDPipelineTest.TestGenerator
{
    // Prototype
    [AddComponentMenu("TestGenerator/Updaters/Place GameObjects On Line")]
    public class PlaceGameObjectsOnLine : MonoBehaviour, IUpdateGameObjects
    {
        #pragma warning disable 649
        [SerializeField] ExecuteMode m_ExecuteMode = ExecuteMode.All;
        [SerializeField] Vector3 m_Start;
        [SerializeField] Vector3 m_Offset = Vector3.right;
        #pragma warning restore 649

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
            for (var i = 0; i < instances.Count; ++i)
            {
                var tr = instances[i].transform;
                tr.localPosition = m_Start + m_Offset * i;
            }
        }
    }
}
