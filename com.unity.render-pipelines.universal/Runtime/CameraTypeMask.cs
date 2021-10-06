using System;

namespace UnityEngine.Experimental.Rendering.Universal
{
    /// <summary>
    /// Specifies a set of camera types.
    /// </summary>
    [Flags]
    [Serializable]
    public enum CameraTypeMask
    {
        /// <summary>
        /// Used to indicate a regular in-game camera.
        /// </summary>
        Game = 1 << 0,
        /// <summary>
        /// Used to indicate that a camera is used for rendering the Scene View in the Editor.
        /// </summary>
        SceneView = 1 << 1,
        /// <summary>
        /// Used to indicate a camera that is used for rendering previews in the Editor.
        /// </summary>
        Preview = 1 << 2,
        /// <summary>
        /// Used to indicate that a camera is used for rendering VR (in edit mode) in the Editor.
        /// </summary>
        VR = 1 << 3,
        /// <summary>
        /// Used to indicate a camera that is used for rendering reflection probes.
        /// </summary>
        Reflection = 1 << 4,
    }

    static class CameraTypeMaskUtility
    {
        public static CameraTypeMask ToMask(this CameraType cameraType)
        {
            return cameraType switch
            {
                CameraType.Game => CameraTypeMask.Game,
                CameraType.SceneView => CameraTypeMask.SceneView,
                CameraType.Preview => CameraTypeMask.Preview,
                CameraType.VR => CameraTypeMask.VR,
                CameraType.Reflection => CameraTypeMask.Reflection,
                _ => throw new ArgumentOutOfRangeException(nameof(cameraType), cameraType, "Unknown camera type")
            };
        }

        public static bool Contains(this CameraTypeMask cameraTypeMask, CameraType cameraType)
        {
            return (cameraType.ToMask() & cameraTypeMask) != 0;
        }

        public static CameraTypeMask allTypes => CameraTypeMask.Game | CameraTypeMask.SceneView | CameraTypeMask.Preview | CameraTypeMask.VR | CameraTypeMask.Reflection;
    }
}
