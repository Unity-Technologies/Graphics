using System.Collections.Generic;

namespace UnityEngine.Rendering.Experimental.LookDev
{

    //IMPORTANT: LookDev is still experimental. Use it at your own risk.
    //Notably known issue: there is no isolation for volume at the moment that could cause leaks in rendering.

    public interface IDataProvider
    {
        /// <summary>Additional configuration required by this SRP on LookDev's scene creation</summary>
        /// <param name="stage">Access element of the LookDev's scene</param>
        void FirstInitScene(StageRuntimeInterface stage);

        /// <summary>Notify the SRP that sky have changed in LookDev</summary>
        /// <param name="camera">The camera of the LookDev's scene</param>
        /// <param name="sky">The new Sky informations</param>
        /// <param name="shadow">The new Shadow information</param>
        /// <param name="stage">Access element of the LookDev's scene</param>
        void UpdateSky(Camera camera, Sky sky, StageRuntimeInterface stage);

        /// <summary>Notify the LookDev about what debug view mode are available in this SRP</summary>
        /// <returns>The list of the mode, None is not required.</returns>
        IEnumerable<string> supportedDebugModes { get; }

        /// <summary>Notify the SRP about a change in the DebugMode used</summary>
        /// <param name="debugIndex">
        /// -1: None
        /// Others: map the result of <see cref="supportedDebugModes()"/>
        /// </param>
        void UpdateDebugMode(int debugIndex);
    }

    public struct Sky
    {
        public Cubemap cubemap;
        public float longitudeOffset;
        public float exposure;
    }
    public struct Shadow
    {
        public Cubemap cubemap;
        public Vector2 sunPosition;
        public Color color;
    }

    /// <summary>Runtime link to reflect some Stage functionality for SRP editing</summary>
    public class StageRuntimeInterface
    {
        System.Func<bool, GameObject> m_AddGameObject;
        System.Func<Camera> m_GetCamera;

        public StageRuntimeInterface(System.Func<bool, GameObject> AddGameObject, System.Func<Camera> GetCamera)
        {
            m_AddGameObject = AddGameObject;
            m_GetCamera = GetCamera;
        }

        /// <summary>Create a gameObject in the stage</summary>
        /// <param name="persistent">
        /// [OPTIONAL] If true, the object is not recreated with the scene update.
        /// Default value: false.
        /// </param>
        /// <returns></returns>
        public GameObject AddGameObject(bool persistent = false)
            => m_AddGameObject?.Invoke(persistent);

        /// <summary>Get the camera used in the stage</summary>
        public Camera camera => m_GetCamera?.Invoke();

        /// <summary>Custom data pointer for convenience</summary>
        public object SRPData;
    }
}
