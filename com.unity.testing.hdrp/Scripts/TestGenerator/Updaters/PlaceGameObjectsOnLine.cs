using System.Collections.Generic;

namespace UnityEngine.Experimental.Rendering.HDPipelineTest.TestGenerator
{
    // Prototype
    [AddComponentMenu("TestGenerator/Updaters/Place GameObjects On Line")]
    public class PlaceGameObjectsOnLine : MonoBehaviour, IUpdateGameObjects
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
            for (var i = 0; i < instances.Count; ++i)
            {
                var tr = instances[i].transform;
                tr.localPosition = m_Start + m_Offset * i;
            }
        }
#pragma warning disable 649
        [Tooltip("When to execute this updater.")] [SerializeField]
        ExecuteMode m_ExecuteMode = ExecuteMode.All;

        [Tooltip("The start position of the line. (Local space)")] [SerializeField]
        Vector3 m_Start;

        [Tooltip("The offset between each items. (Local space)")] [SerializeField]
        Vector3 m_Offset = Vector3.right;
#pragma warning restore 649
    }
}
