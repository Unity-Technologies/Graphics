namespace UnityEngine.Rendering.HighDefinition
{
    /// <summary>
    /// Utility class for HDRP specific components.
    /// </summary>
    public static class ComponentUtility
    {
        /// <summary> Check if the provided camera is compatible with High-Definition Render Pipeline </summary>
        /// <param name="camera">The Camera to check</param>
        /// <returns>True if it is compatible, false otherwise</returns>
        public static bool IsHDCamera(Camera camera)
            => camera.GetComponent<HDAdditionalCameraData>() != null;

        /// <summary> Check if the provided light is compatible with High-Definition Render Pipeline </summary>
        /// <param name="light">The Light to check</param>
        /// <returns>True if it is compatible, false otherwise</returns>
        public static bool IsHDLight(Light light)
            => light.GetComponent<HDAdditionalLightData>() != null;

        /// <summary> Check if the provided reflection probe is compatible with High-Definition Render Pipeline </summary>
        /// <param name="reflectionProbe">The ReflectionProbe to check</param>
        /// <returns>True if it is compatible, false otherwise</returns>
        public static bool IsHDReflectionProbe(ReflectionProbe reflectionProbe)
            => reflectionProbe.GetComponent<HDAdditionalReflectionData>() != null;
    }
}
