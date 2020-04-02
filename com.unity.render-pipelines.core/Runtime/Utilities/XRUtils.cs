using System;

namespace UnityEngine.Rendering
{
    /// <summary>
    /// XR Utility class.
    /// </summary>
    [Obsolete]
    public static class XRUtils
    {
        /// <summary>
        /// Draw the XR occlusion mesh.
        /// </summary>
        /// <param name="cmd">Command Buffer used to draw the occlusion mesh.</param>
        /// <param name="camera">Camera for which the occlusion mesh is rendered.</param>
        /// <param name="stereoEnabled">True if stereo rendering is enabled.</param>
        [Obsolete]
        public static void DrawOcclusionMesh(CommandBuffer cmd, Camera camera, bool stereoEnabled = true) // Optional stereoEnabled is for SRP-specific stereo logic
        {
            if ((!XRGraphics.enabled) || (!camera.stereoEnabled) || (!stereoEnabled))
                return;
            UnityEngine.RectInt normalizedCamViewport = new UnityEngine.RectInt(0, 0, camera.pixelWidth, camera.pixelHeight);
            cmd.DrawOcclusionMesh(normalizedCamViewport);
        }

    }
}
