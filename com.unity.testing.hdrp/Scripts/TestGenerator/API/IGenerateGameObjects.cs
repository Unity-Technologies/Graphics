using System.Collections.Generic;

namespace UnityEngine.Experimental.Rendering.HDPipelineTest.TestGenerator
{
    // Prototype
    // Generate or updates game object instances
    /// <summary>
    ///     Implement this interface to generate game objects
    /// </summary>
    public interface IGenerateGameObjects
    {
        /// <summary>
        ///     This implementation is only called during play mode.
        ///     Play mode is either in a standalone, or when the editor is in play mode.
        /// </summary>
        /// <param name="parent">An created game objects must have this transform as parent.</param>
        /// <param name="instances">
        ///     The existing and resulting game object instances.
        ///     It will contains the existing instances and the instance created by the previous generators.
        ///     If you create game object instances, you must also add them to this list.
        /// </param>
        void GenerateInPlayMode(Transform parent, List<GameObject> instances);

        /// <summary>
        ///     This implementation is only called during edit mode.
        ///     When implementing this method, you can use API from the Editor to manipulate assets.
        /// </summary>
        /// <param name="parent">An created game objects must have this transform as parent.</param>
        /// <param name="instances">
        ///     The existing and resulting game object instances.
        ///     It will contains the existing instances and the instance created by the previous generators.
        ///     If you create game object instances, you must also add them to this list.
        /// </param>
        void GenerateInEditMode(Transform parent, List<GameObject> instances);
    }
}
