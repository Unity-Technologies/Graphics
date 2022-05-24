using UnityEngine.Rendering;
using System.Collections.Generic;
using System.Linq;

namespace UnityEngine.Rendering.HighDefinition
{
    /// <summary>
    /// Volume component class to inherit when you implement a custom post process
    /// </summary>
    public abstract class CustomPostProcessVolumeComponent : VolumeComponent
    {
        bool m_IsInitialized = false;

        internal string typeName;

        // Keep track of all the instances alive of the custom post process component so we can release them when needed
        internal static HashSet<CustomPostProcessVolumeComponent> instances = new HashSet<CustomPostProcessVolumeComponent>();

        /// <summary>
        /// Injection point of the custom post process in HDRP.
        /// </summary>
        public virtual CustomPostProcessInjectionPoint injectionPoint => CustomPostProcessInjectionPoint.AfterPostProcess;

        /// <summary>
        /// True if you want your custom post process to be visible in the scene view.false False otherwise.
        /// </summary>
        public virtual bool visibleInSceneView => true;

        /// <summary>
        /// Setup function, called once before render is called.
        /// </summary>
        public virtual void Setup() { }

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
        public virtual void Cleanup() { }

        /// <summary>
        /// Unity calls this method when the object goes out of scope.
        /// </summary>
        protected override void OnDisable()
        {
            base.OnDisable();
            CleanupInternal();
        }

        internal void CleanupInternal()
        {
            if (m_IsInitialized)
                Cleanup();

            m_IsInitialized = false;
            instances.Remove(this);
        }

        internal void SetupIfNeeded()
        {
            if (!m_IsInitialized)
            {
                Setup();
                m_IsInitialized = true;
                typeName = GetType().Name;
                instances.Add(this);
            }
        }

        // If the HDRP asset is destroyed or changed, we reset the post process resources
        internal static void CleanupAllCustomPostProcesses()
        {
            foreach (var instance in instances.ToList()) // Copy to remove elements safely
                instance.CleanupInternal();
        }
    }
}
