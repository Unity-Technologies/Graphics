namespace UnityEngine.Rendering.Universal
{
    /// <summary>
    /// Utility class for component checks.
    /// </summary>
    public static class ComponentUtility
    {
        /// <summary> Check if the provided camera is compatible with Universal Render Pipeline </summary>
        /// <param name="camera">The Camera to check</param>
        /// <returns>True if it is compatible, false otherwise</returns>
        public static bool IsUniversalCamera(Camera camera)
            => camera.GetComponent<UniversalAdditionalCameraData>() != null;

        /// <summary> Check if the provided light is compatible with Universal Render Pipeline </summary>
        /// <param name="light">The Light to check</param>
        /// <returns>True if it is compatible, false otherwise</returns>
        public static bool IsUniversalLight(Light light)
            => light.GetComponent<UniversalAdditionalLightData>() != null;
    }
}
