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
        public Vector4 directionWS_influence;
    }

    /// <summary>
    /// </summary>
    [ExecuteAlways]
    public class EllipsoidOccluder : MonoBehaviour
    {
        /// <summary></summary>
        public Vector3 centerOS = Vector3.zero;

        /// <summary></summary>
        public float radiusOS = 0.5f;

        public Vector3 anglesOS = Vector3.zero;
        /// <summary>The direction in object space that the ellipsoid's major axis is facing.</summary>
        public Vector3 directionWS => (transform.rotation * Quaternion.Euler(anglesOS) * Vector3.forward).normalized;

        /// <summary></summary>
        public float scalingOS = 1.0f;

        public float influenceRadiusScale = 1.0f;


        internal EllipsoidOccluderData ConvertToEngineData(Vector3 camOffset)
        {
            Transform tr = transform;
            Quaternion rot = Quaternion.Euler(anglesOS);

            float forward = tr.TransformVector(rot * Vector3.forward).magnitude;
            float right = tr.TransformVector(rot * Vector3.right).magnitude;
            float up = tr.TransformVector(rot * Vector3.up).magnitude;
            float radius = radiusOS * Mathf.Max(right, up);

            Vector3 centerRWS = tr.TransformPoint(centerOS) - camOffset;
            Vector3 dir = directionWS * forward * scalingOS;

            Vector3 lossyScale = tr.lossyScale;
            float influenceRadius = Mathf.Max(Mathf.Max(lossyScale.x, lossyScale.y), lossyScale.z) * influenceRadiusScale * radiusOS;
            influenceRadius *= Mathf.Max(1.0f, scalingOS);

            return new EllipsoidOccluderData {
                positionRWS_radius = new Vector4(centerRWS.x, centerRWS.y, centerRWS.z, radius),
                directionWS_influence = new Vector4(dir.x, dir.y, dir.z, influenceRadius)
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
                Quaternion rot = Quaternion.Euler(anglesOS);

                float forward = tr.TransformVector(rot * Vector3.forward).magnitude;
                float right = tr.TransformVector(rot * Vector3.right).magnitude;
                float up = tr.TransformVector(rot * Vector3.up).magnitude;

                Vector3 scale = Vector3.one * radiusOS;
                scale.x *= Mathf.Max(right, up);
                scale.y *= Mathf.Max(right, up);
                scale.z *= forward * scalingOS;

                return Matrix4x4.TRS(tr.TransformPoint(centerOS), (tr.rotation * rot).normalized, scale);
            }
        }
    }
}
