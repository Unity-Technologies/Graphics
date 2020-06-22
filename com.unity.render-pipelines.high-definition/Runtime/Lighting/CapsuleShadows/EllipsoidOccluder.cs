using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Serialization;

namespace UnityEngine.Rendering.HighDefinition
{
    [GenerateHLSL]
    internal struct EllipsoidOccluderData
    {
        public Vector3 position;
        public float radius;
        public Vector3 direction;
        public float scaling;
    }

    /// <summary>
    /// </summary>
    public class EllipsoidOccluder : MonoBehaviour
    {
        /// <summary></summary>
        public Vector3 center = Vector3.zero;

        /// <summary></summary>
        public float radius = 1.0f;

        /// <summary></summary>
        public Vector3 direction = Vector3.zero;

        /// <summary></summary>
        public float scaling = 1.0f;

        /*
        [SerializeField]
        float m_Intensity;
        /// <summary>
        /// Get/Set the intensity of the light using the current light unit.
        /// </summary>
        public float intensity
        {
            get => m_Intensity;
            set
            {
                if (m_Intensity == value)
                    return;

                m_Intensity = Mathf.Clamp(value, 0, float.MaxValue);
                UpdateLightIntensity();
            }
        }

        /// <summary>
        /// Set the light culling mask.
        /// </summary>
        /// <param name="cullingMask"></param>
        public void SetCullingMask(int cullingMask) => legacyLight.cullingMask = cullingMask;
        */
    }
}
