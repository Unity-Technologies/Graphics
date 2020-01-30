using System.Collections.Generic;

namespace UnityEngine.Experimental.Rendering.HDPipelineTest.TestGenerator
{
    // Prototype
    /// <summary>
    ///     Implement this interface in a <see cref="MonoBehaviour" /> to update generated game objects.
    /// </summary>
    public interface IUpdateGameObjects
    {
        /// <summary>
        ///     Defines when this updater is executed.
        /// </summary>
        ExecuteMode executeMode { get; }

        /// <summary>
        ///     Update the game object instances in play mode.
        ///     Play mode is either in a standalone, or when the editor is in play mode.
        /// </summary>
        /// <param name="parent">The parent of all game object instances.</param>
        /// <param name="instances">The game object instances to update.</param>
        void UpdateInPlayMode(Transform parent, List<GameObject> instances);

        /// <summary>
        ///     Update the game object instances in edit mode.
        ///     When implementing this method, you can use API from the Editor to manipulate assets.
        /// </summary>
        /// <param name="parent">The parent of all game object instances.</param>
        /// <param name="instances">The game object instances to update.</param>
        void UpdateInEditMode(Transform parent, List<GameObject> instances);
    }
}
