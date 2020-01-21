using System.Collections.Generic;

namespace UnityEngine.Experimental.Rendering.HDPipelineTest.TestGenerator
{
    // Prototype
    public interface IUpdateGameObjects
    {
        ExecuteMode executeMode { get; }

        void UpdateInPlayMode(Transform parent, List<GameObject> instances);
        void UpdateInEditMode(Transform parent, List<GameObject> instances);
    }
}
