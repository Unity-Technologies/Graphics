using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Serialization;
using UnityEngine.UIElements;

namespace UnityEngine.Rendering.HighDefinition
{
    [GenerateHLSL(needAccessors = false)]
    internal struct EllipsoidOccluderData
    {
        public Vector4 positionRWS_radius;
        public Vector4 directionWS_scaling;
    }

    /// <summary>
    /// </summary>
    [ExecuteAlways]
    public class EllipsoidOccluder : MonoBehaviour
    {
        /// <summary></summary>
        public Vector3 centerOS = Vector3.zero;

        /// <summary></summary>
        public float radiusOS = 1.0f;

        /// <summary>The direction in object space that the ellipsoid's major axis is facing, parameterized as euler degrees.</summary>
        public Vector3 directionOS = Vector3.zero;

        /// <summary></summary>
        public float scalingOS = 1.0f;


        internal EllipsoidOccluderData ConvertToEngineData(Vector3 camOffset)
        {
            Vector3 centerRWS = transform.TransformPoint(centerOS) - camOffset;
            Vector3 directionWS = transform.TransformVector(Quaternion.Euler(directionOS) * Vector3.forward).normalized;
            float radiusWS = radiusOS; // TODO: Handle scale transform.
            float scalingWS = scalingOS; // TODO: Handle scale transform.
            return new EllipsoidOccluderData {
                positionRWS_radius = new Vector4(centerRWS.x, centerRWS.y, centerRWS.z, radiusWS),
                directionWS_scaling = new Vector4(directionWS.x, directionWS.y, directionWS.z, scalingWS)
            };
        }

        private void OnEnable()
        {
            EllipsoidOccluderManager.manager.RegisterCapsule(this);
        }

        private void OnDisable()
        {
            EllipsoidOccluderManager.manager.DeRegisterCapsule(this);
        }

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
