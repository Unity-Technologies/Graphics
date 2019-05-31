namespace UnityEngine.Rendering.LookDev
{

    //IMPORTANT: LookDev is still experimental. Use it at your own risk.
    //Notably known issue: there is no isolation for volume at the moment that could cause leaks in rendering.

    public interface IDataProvider
    {
        void FirstInit(StageRuntimeInterface stage);
        void UpdateSky(Camera camera, Cubemap skybox, StageRuntimeInterface stage);
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
