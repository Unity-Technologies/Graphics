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
        public Vector4 scaleddirectionWS_influenceRadius;
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

        public float influenceRadiusScale = 1.0f;


        internal EllipsoidOccluderData ConvertToEngineData(Vector3 camOffset)
        {
            Transform tr = transform;
            Vector3 centerRWS = tr.position + tr.rotation * centerOS - camOffset;
            Vector3 directionWS = (tr.rotation * Quaternion.Euler(directionOS) * Vector3.forward).normalized;
            float radiusWS = radiusOS; // TODO: Handle scale transform.
            float scalingWS = scalingOS; // TODO: Handle scale transform.
            Vector3 scaledDirectionWS = directionWS * scalingWS;
            return new EllipsoidOccluderData {
                positionRWS_radius = new Vector4(centerRWS.x, centerRWS.y, centerRWS.z, radiusWS),
                scaleddirectionWS_influenceRadius = new Vector4(scaledDirectionWS.x, scaledDirectionWS.y, scaledDirectionWS.z, radiusWS * influenceRadiusScale * scalingWS)
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


        /// <summary>
        /// Get the TRS matrix
        /// </summary>
        public Matrix4x4 TRS
        {
            get
            {
                Transform tr = transform;
                Quaternion rot = Quaternion.Euler(directionOS);

                Vector3 scale = Vector3.one * radiusOS;
                scale.z *= scalingOS;

                return Matrix4x4.TRS(tr.position + tr.rotation * centerOS, (tr.rotation * rot).normalized, scale);
            }
        }
    }
}
