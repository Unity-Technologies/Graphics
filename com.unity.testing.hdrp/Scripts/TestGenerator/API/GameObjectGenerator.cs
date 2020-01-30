using UnityEditor;
using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering.HDPipelineTest.TestGenerator
{
    // Prototype
    // Extensible game object generator
    /// <summary>
    ///     Generate a fixed amount of game objects.
    ///     How elements are generated are defined by associated components that inherits from:
    ///     - <see cref="IGenerateGameObjects" />
    ///     - <see cref="IUpdateGameObjects" />
    ///     The generators and updaters will be executed in the order of the components attached to the game object with
    ///     the <see cref="GameObjectGenerator" /> component.
    /// </summary>
    [AddComponentMenu("TestGenerator/Game Object Generator")]
    public class GameObjectGenerator : MonoBehaviour
    {
#pragma warning disable 649
        [SerializeField] Transform m_InstanceContainer;
#pragma warning restore 649

        void OnEnable()
        {
            Synchronize();
        }

        /// <summary>
        ///     Call this method to synchronize existing game objects with the requirements of the generators.
        ///     It will create, destroy, and update game objects.
        /// </summary>
        [ContextMenu("Synchronize")]
        void Synchronize()
        {
            using (ListPool<GameObject>.Get(out var instances))
            using (ListPool<IUpdateGameObjects>.Get(out var updaters))
            {
                var parent = m_InstanceContainer;
                if (parent == null || parent.Equals(null))
                    parent = transform;

                var generator = GetComponent<IGenerateGameObjects>();

                if (generator != null && !generator.Equals(null))
                {
                    foreach (Transform child in parent)
                        instances.Add(child.gameObject);
#if UNITY_EDITOR
                    if (!EditorApplication.isPlayingOrWillChangePlaymode)
                        generator.GenerateInEditMode(parent, instances);
                    else
#endif
                        generator.GenerateInPlayMode(parent, instances);
                }

                GetComponents(updaters);
#if UNITY_EDITOR
                if (!EditorApplication.isPlayingOrWillChangePlaymode)
                {
                    foreach (var updater in updaters)
                        if ((updater.executeMode & ExecuteMode.EditMode) != 0)
                            updater.UpdateInEditMode(parent, instances);
                }
                else
#endif
                {
                    foreach (var updater in updaters)
                        if ((updater.executeMode & ExecuteMode.PlayMode) != 0)
                            updater.UpdateInPlayMode(parent, instances);
                }
            }
        }
    }
}
