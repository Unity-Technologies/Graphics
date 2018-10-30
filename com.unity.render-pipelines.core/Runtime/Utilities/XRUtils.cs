using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering
{
    public static class XRUtils
    {
        public static void DrawOcclusionMesh(CommandBuffer cmd, Camera camera, bool stereoEnabled = true) // Optional stereoEnabled is for SRP-specific stereo logic
        {   
#if UNITY_2019_1_OR_NEWER
            if ((!XRGraphics.enabled) || (!camera.stereoEnabled) || (!stereoEnabled))
                return;
            UnityEngine.RectInt normalizedCamViewport = new UnityEngine.RectInt(0, 0, camera.pixelWidth, camera.pixelHeight);
            cmd.DrawOcclusionMesh(normalizedCamViewport);
#endif
        }

    }
}
