using UnityEngine.Rendering;

namespace UnityEngine.Rendering.HighDefinition
{
    /// <summary>
    /// Volume component class to inherit when you implement a custom post process
    /// </summary>
    public abstract class CustomPostProcessVolumeComponent : VolumeComponent
    {
        bool m_IsInitialized = false;

        /// <summary>
        /// Injection point of the custom post process in HDRP.
        /// </summary>
        public virtual CustomPostProcessInjectionPoint injectionPoint => CustomPostProcessInjectionPoint.AfterPostProcess;

        /// <summary>
        /// Setup function, called once before render is called.
        /// </summary>
        public virtual void Setup() {}

        /// <summary>
        /// Called every frame for each camera when the post process needs to be rendered.
        /// </summary>
        /// <param name="cmd">Command Buffer used to issue your commands</param>
        /// <param name="camera">Current Camera</param>
        /// <param name="source">Source Render Target, it contains the camera color buffer in it's current state</param>
        /// <param name="destination">Destination Render Target</param>
        public abstract void Render(CommandBuffer cmd, HDCamera camera, RTHandle source, RTHandle destination);

        /// <summary>
        /// Cleanup function, called when the render pipeline is disposed.
        /// </summary>
        public virtual void Cleanup() {}

        internal void CleanupInternal()
        {
            if (m_IsInitialized)
                Cleanup();

            m_IsInitialized = false;
        }

        internal void SetupIfNeeded()
        {
            if (!m_IsInitialized)
            {
                Setup();
                m_IsInitialized = true;
            }
        }
    }
}
