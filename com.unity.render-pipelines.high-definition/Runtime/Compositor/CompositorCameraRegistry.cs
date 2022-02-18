using System.Collections.Generic;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace UnityEngine.Rendering.HighDefinition.Compositor
{
    // Internal class to keep track of compositor allocated cameras.
    // Required to properly manage cameras that are deleted or "ressurected" by undo/redo operations.
    class CompositorCameraRegistry
    {
        static List<Camera> s_CompositorManagedCameras = new List<Camera>();
        static private CompositorCameraRegistry s_CompositorCameraRegistry;
        static public CompositorCameraRegistry GetInstance() =>
            s_CompositorCameraRegistry ?? (s_CompositorCameraRegistry = new CompositorCameraRegistry());

        // Keeps track of compositor allocated cameras
        internal void RegisterInternalCamera(Camera camera)
        {
            s_CompositorManagedCameras.Add(camera);
        }

        internal void UnregisterInternalCamera(Camera camera)
        {
            s_CompositorManagedCameras.Remove(camera);
        }

        // Checks for any compositor allocated cameras that are now unused and frees their resources.
        internal void CleanUpCameraOrphans(List<CompositorLayer> layers = null)
        {
            s_CompositorManagedCameras.RemoveAll(x => x == null);

            for (int i = s_CompositorManagedCameras.Count - 1; i >= 0; i--)
            {
                bool found = false;
                if (layers != null)
                {
                    foreach (var layer in layers)
                    {
                        if (s_CompositorManagedCameras[i].Equals(layer.camera))
                        {
                            found = true;
                            break;
                        }
                    }
                }

                // If the camera is not used by any layer anymore, then destroy it
                if (found == false && s_CompositorManagedCameras[i] != null)
                {
                    var cameraData = s_CompositorManagedCameras[i].GetComponent<HDAdditionalCameraData>();
                    if (cameraData)
                    {
                        CoreUtils.Destroy(cameraData);
                    }
                    s_CompositorManagedCameras[i].targetTexture = null;
                    CoreUtils.Destroy(s_CompositorManagedCameras[i]);
                    s_CompositorManagedCameras.RemoveAt(i);
                }
            }

            if (layers != null)
            {
                foreach (var layer in layers)
                {
                    if (layer != null && !s_CompositorManagedCameras.Contains(layer.camera))
                    {
                        s_CompositorManagedCameras.Add(layer.camera);
                    }
                }
            }
        }

        internal void PrinCameraIDs()
        {
            for (int i = s_CompositorManagedCameras.Count - 1; i >= 0; i--)
            {
                var id = s_CompositorManagedCameras[i] ? s_CompositorManagedCameras[i].GetInstanceID() : 0;
            }
        }
    }
}
