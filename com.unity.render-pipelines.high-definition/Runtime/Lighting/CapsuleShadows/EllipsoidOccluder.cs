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
        public Vector3 sphereFromWorldTangent;
        public Vector3 sphereFromWorldBitangent;
        public Vector3 sphereFromWorldNormal;
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
            Vector3 dir = directionWS * forward * scalingOS / Mathf.Max(right, up);

            Vector3 lossyScale = tr.lossyScale;
            float influenceRadius = Mathf.Max(Mathf.Max(lossyScale.x, lossyScale.y), lossyScale.z) * influenceRadiusScale * radiusOS;
            influenceRadius *= Mathf.Max(1.0f, scalingOS);

            Quaternion worldFromSphereRotation = tr.rotation * Quaternion.Euler(anglesOS);
            Vector3 worldFromSphereTangent = worldFromSphereRotation * Vector3.right;
            Vector3 worldFromSphereBitangent = worldFromSphereRotation * Vector3.up;
            Vector3 worldFromSphereNormal = worldFromSphereRotation * Vector3.forward;

            Vector3 worldFromSphereScale = new Vector3(radiusOS * tr.lossyScale.x, radiusOS * tr.lossyScale.y, radiusOS * scalingOS * tr.lossyScale.z);
            worldFromSphereTangent *= worldFromSphereScale.x;
            worldFromSphereBitangent *= worldFromSphereScale.y;
            worldFromSphereNormal *= worldFromSphereScale.z;

            Vector3 sphereFromWorldTangent;
            Vector3 sphereFromWorldBitangent;
            Vector3 sphereFromWorldNormal;
            Vector3x3Invert(
                out sphereFromWorldTangent,
                out sphereFromWorldBitangent,
                out sphereFromWorldNormal,
                worldFromSphereTangent,
                worldFromSphereBitangent,
                worldFromSphereNormal
            );

            return new EllipsoidOccluderData
            {
                positionRWS_radius = new Vector4(centerRWS.x, centerRWS.y, centerRWS.z, radius),
                directionWS_influence = new Vector4(dir.x, dir.y, dir.z, influenceRadius),

                // Just using these for debugging - should strip these out in the final version.
                sphereFromWorldTangent = sphereFromWorldTangent,
                sphereFromWorldBitangent = sphereFromWorldBitangent,
                sphereFromWorldNormal = sphereFromWorldNormal,
            };
        }

        private static void Vector3x3Invert(out Vector3 tangentOut, out Vector3 bitangentOut, out Vector3 normalOut, Vector3 tangentIn, Vector3 bitangentIn, Vector3 normalIn)
        {
            tangentOut = Vector3.Cross(bitangentIn, normalIn);
            bitangentOut = Vector3.Cross(normalIn, tangentIn);
            normalOut = Vector3.Cross(tangentIn, bitangentIn);

            float det = Vector3.Dot(Vector3.Cross(tangentIn, bitangentIn), normalIn);
            float detInverse = Mathf.Abs(det) > 1e-5f ? (1.0f / det) : 0.0f;

            tangentOut *= detInverse;
            bitangentOut *= detInverse;
            normalOut *= detInverse;
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
