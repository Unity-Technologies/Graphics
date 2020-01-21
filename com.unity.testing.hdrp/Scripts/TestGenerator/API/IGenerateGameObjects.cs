using System.Collections.Generic;

namespace UnityEngine.Experimental.Rendering.HDPipelineTest.TestGenerator
{
    // Prototype
    // Generate or updates game object instances
    public interface IGenerateGameObjects
    {
        void GenerateInPlayMode(Transform parent, List<GameObject> instances);
        void GenerateInEditMode(Transform parent, List<GameObject> instances);
    }
}
