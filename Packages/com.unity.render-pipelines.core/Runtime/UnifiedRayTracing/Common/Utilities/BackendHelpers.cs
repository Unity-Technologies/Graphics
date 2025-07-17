using System;

namespace UnityEngine.Rendering.UnifiedRayTracing
{
    /// <summary>
    /// Helper functions for querying the shader type and filename for a given backend.
    /// </summary>
    public static class BackendHelpers
    {
        /// <summary>
        /// Builds a file path with the right extension for the given <see cref="RayTracingBackend"/>.
        /// </summary>
        /// <param name="backend">Backend for which the shader will be loaded.</param>
        /// <param name="fileName">Path to the shader file, without any extension.</param>
        /// <returns>The file path.</returns>
        public static string GetFileNameOfShader(RayTracingBackend backend, string fileName)
        {
            string postFix = backend switch
            {
                RayTracingBackend.Hardware => "raytrace",
                RayTracingBackend.Compute => "compute",
                _ => throw new ArgumentOutOfRangeException(nameof(backend), backend, null)
            };
            return $"{fileName}.{postFix}";
        }

        /// <summary>
        /// Returns the <see cref="System.Type"/> of shader used by the given <see cref="RayTracingBackend"/>.
        /// </summary>
        /// <param name="backend">Backend for which the shader will be loaded.</param>
        /// <returns>The Type of the shader.</returns>
        public static Type GetTypeOfShader(RayTracingBackend backend)
        {
            Type shaderType = backend switch
            {
                RayTracingBackend.Hardware => typeof(RayTracingShader),
                RayTracingBackend.Compute => typeof(ComputeShader),
                _ => throw new ArgumentOutOfRangeException(nameof(backend), backend, null)
            };
            return shaderType;
        }
    }
}
