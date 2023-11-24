using UnityEngine;

namespace UnityEngine.Rendering.HighDefinition
{
    /// <summary>
    /// Contains utility methods for HDRP to query DRS Capabilities.
    /// </summary>
    public static class HDDynamicResolutionPlatformCapabilities
    {
        /// <summary>
        /// True if the render pipeline detected DLSS capable platform. False otherwise.
        /// </summary>
        public static bool DLSSDetected { get { return m_DLSSDetected; } }

        /// <summary>
        /// True if the render pipeline detected FSR2 capable platform. False otherwise.
        /// </summary>
        public static bool FSR2Detected { get { return m_FSR2Detected; } }

        private static bool m_DLSSDetected = false;
        private static bool m_FSR2Detected = false;

        internal static void SetupFeatures()
        {
            m_DLSSDetected = DLSSPass.SetupFeature();
            m_FSR2Detected = FSR2Pass.SetupFeature();
        }
    }
}
