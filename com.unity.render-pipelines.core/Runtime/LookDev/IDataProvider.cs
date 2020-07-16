using System.Collections.Generic;

namespace UnityEngine.Rendering.LookDev
{
    /// <summary>
    /// Interface that Scriptable Render Pipelines should implement to be able to use LookDev window
    /// </summary>
    public interface IDataProvider
    {
        /// <summary>Additional configuration required by this SRP on LookDev's scene creation</summary>
        /// <param name="stage">Access element of the LookDev's scene</param>
        void FirstInitScene(StageRuntimeInterface stage);

        /// <summary>Notify the SRP that sky have changed in LookDev</summary>
        /// <param name="camera">The camera of the LookDev's scene</param>
        /// <param name="sky">The new Sky informations</param>
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

        /// <summary>
        /// Compute the shadow mask in SRP for LookDev sun simulation
        /// </summary>
        /// <param name="output">The computed ShadowMask</param>
        /// <param name="stage">Access element of the LookDev's scene</param>
        void GetShadowMask(ref RenderTexture output, StageRuntimeInterface stage);

        /// <summary>
        /// Callback called at the beginning of LookDev rendering.
        /// </summary>
        /// <param name="stage">Access element of the LookDev's scene</param>
        void OnBeginRendering(StageRuntimeInterface stage);

        /// <summary>
        /// Callback called at the beginning of LookDev rendering.
        /// </summary>
        /// <param name="stage">Access element of the LookDev's scene</param>
        void OnEndRendering(StageRuntimeInterface stage);

        /// <summary>
        /// Callback called to do any necessary cleanup.
        /// </summary>
        /// <param name="SRI">Access element of the LookDev's scene</param>
        void Cleanup(StageRuntimeInterface SRI);
    }

    /// <summary>
    /// Runtime container representing Sky data given to the scriptable render pipeline for rendering
    /// </summary>
    public struct Sky
    {
        /// <summary>The cubemap representing this sky</summary>
        public Cubemap cubemap;
        /// <summary>The longitude offset to rotate this cubemap</summary>
        public float longitudeOffset;
        /// <summary>The sky exposure</summary>
        public float exposure;
    }

    /// <summary>Runtime link to reflect some Stage functionality for SRP editing</summary>
    public class StageRuntimeInterface
    {
        System.Func<bool, GameObject> m_AddGameObject;
        System.Func<Camera> m_GetCamera;
        System.Func<Light> m_GetSunLight;

        /// <summary>Construct a StageRuntimeInterface</summary>
        /// <param name="AddGameObject">Callback to call when adding a GameObject</param>
        /// <param name="GetCamera">Callback to call for getting the Camera</param>
        /// <param name="GetSunLight">Callback to call for getting the sun Light</param>
        public StageRuntimeInterface(
            System.Func<bool, GameObject> AddGameObject,
            System.Func<Camera> GetCamera,
            System.Func<Light> GetSunLight)
        {
            m_AddGameObject = AddGameObject;
            m_GetCamera = GetCamera;
            m_GetSunLight = GetSunLight;
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

        /// <summary>Get the sun used in the stage</summary>
        public Light sunLight => m_GetSunLight?.Invoke();

        /// <summary>Custom data pointer for convenience</summary>
        public object SRPData;
    }
}
