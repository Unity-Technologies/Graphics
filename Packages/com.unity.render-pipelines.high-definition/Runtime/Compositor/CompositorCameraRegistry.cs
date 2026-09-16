using System.Collections.Generic;
using Unity.Scripting.LifecycleManagement;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace UnityEngine.Rendering.HighDefinition.Compositor
{
    // Internal class to keep track of compositor allocated cameras.
    // Required to properly manage cameras that are deleted or "ressurected" by undo/redo operations.
    class CompositorCameraRegistry
    {
        [NoAutoStaticsCleanup] // Owned by the surviving CompositionManager; cleaned up via its own teardown, not on every Play Mode entry.
        static CompositorCameraRegistry s_CompositorCameraRegistry;

        public static CompositorCameraRegistry GetInstance() => s_CompositorCameraRegistry ??= new CompositorCameraRegistry();

        List<Camera> m_CompositorManagedCameras = new List<Camera>();

        // Keeps track of compositor allocated cameras
        internal void RegisterInternalCamera(Camera camera)
        {
            m_CompositorManagedCameras.Add(camera);
        }

        internal void UnregisterInternalCamera(Camera camera)
        {
            m_CompositorManagedCameras.Remove(camera);
        }

        // Checks for any compositor allocated cameras that are now unused and frees their resources.
        internal void CleanUpCameraOrphans(List<CompositorLayer> layers = null)
        {
            m_CompositorManagedCameras.RemoveAll(x => x == null);

            for (int i = m_CompositorManagedCameras.Count - 1; i >= 0; i--)
            {
                bool found = false;
                if (layers != null)
                {
                    foreach (var layer in layers)
                    {
                        if (m_CompositorManagedCameras[i].Equals(layer.camera))
                        {
                            found = true;
                            break;
                        }
                    }
                }

                // If the camera is not used by any layer anymore, then destroy it
                if (found == false && m_CompositorManagedCameras[i] != null)
                {
                    var cameraData = m_CompositorManagedCameras[i].GetComponent<HDAdditionalCameraData>();
                    if (cameraData)
                    {
                        CoreUtils.Destroy(cameraData);
                    }

                    m_CompositorManagedCameras[i].targetTexture = null;
                    CoreUtils.Destroy(m_CompositorManagedCameras[i]);
                    m_CompositorManagedCameras.RemoveAt(i);
                }
            }

            if (layers != null)
            {
                foreach (var layer in layers)
                {
                    if (layer != null && !m_CompositorManagedCameras.Contains(layer.camera))
                    {
                        m_CompositorManagedCameras.Add(layer.camera);
                    }
                }
            }
        }
    }
}
