#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

namespace UnityEngine
{
    /// <summary>
    /// LightAnchor component represents Camera space based light controls around a virtual pivot point
    /// </summary>
    [AddComponentMenu("Rendering/Light Anchor")]
    [RequireComponent(typeof(Light))]
    [ExecuteInEditMode]
    [DisallowMultipleComponent]
    public class LightAnchor : MonoBehaviour
    {
        const float k_ArcRadius = 5;
        const float k_AxisLength = 10;

        [SerializeField]
        float m_Distance = 3;
        [SerializeField]
        bool m_UpIsWorldSpace = true;

        float m_Yaw;
        float m_Pitch;
        float m_Roll;

        /// <summary>
        /// Camera relative Yaw between 0,180 to the right of the camera, and 0,-180 to the left
        /// </summary>
        public float yaw
        {
            get { return m_Yaw; }
            set { m_Yaw = NormalizeAngleDegree(value); }
        }

        /// <summary>
        /// Pitch relative to the horizon or camera depending on value of m_UpIsWorldSpace.  0,180 is down, 0,-180 is up.
        /// </summary>
        public float pitch
        {
            get { return m_Pitch; }
            set { m_Pitch = NormalizeAngleDegree(value); }
        }

        /// <summary>
        /// Camera relative Roll between 0,180 to the right of the camera, and 0,-180 to the left
        /// </summary>
        public float roll
        {
            get { return m_Roll; }
            set { m_Roll = NormalizeAngleDegree(value); }
        }

        /// <summary>
        /// Distance from the light's anchor point
        /// </summary>
        public float distance
        {
            get { return m_Distance; }
            set { m_Distance = Mathf.Max(value, .01f); }
        }

        /// <summary>
        /// Should Up be in World or Camera Space
        /// </summary>
        public bool upIsWorldSpace
        {
            get { return m_UpIsWorldSpace; }
            set { m_UpIsWorldSpace = value; }
        }

        /// <summary>
        /// Position of the light's anchor point
        /// </summary>
        public Vector3 anchorPosition
        {
            get { return transform.position + transform.forward * m_Distance; }
        }

        struct Axes
        {
            public Vector3 up;
            public Vector3 right;
            public Vector3 forward;
        }

        /// <summary>
        /// Normalizes the input angle to be in the range of -180 and 180
        /// </summary>
        /// <param name="angle">Raw input angle or rotation</param>
        /// <returns>angle of rotation between -180 and 180</returns>
        public static float NormalizeAngleDegree(float angle)
        {
            const float range = 360f;
            const float startValue = -180f;
            var offset = angle - startValue;

            return offset - (Mathf.Floor(offset / range) * range) + startValue;
        }

        /// <summary>
        /// Update Yaw, Pitch, Roll, and Distance base don world state
        /// </summary>
        /// <param name="camera">Camera to which light values are relative</param>
        public void SynchronizeOnTransform(Camera camera)
        {
            var axes = GetWorldSpaceAxes(camera);

            var extractedYaw = 0f;
            var extractedPitch = 0f;

            var worldAnchorToLight = transform.position - anchorPosition;
            var extractedDistance = worldAnchorToLight.magnitude;

            var projectOnGround = Vector3.ProjectOnPlane(worldAnchorToLight, axes.up);
            projectOnGround.Normalize();

            extractedYaw = Vector3.SignedAngle(axes.forward, projectOnGround, axes.up);

            var yawedRight = Quaternion.AngleAxis(extractedYaw, axes.up) * axes.right;
            extractedPitch = Vector3.SignedAngle(projectOnGround, worldAnchorToLight, yawedRight);

            yaw = extractedYaw;
            pitch = extractedPitch;
            roll = transform.rotation.eulerAngles.z;
            distance = extractedDistance;
        }

        /// <summary>
        /// Update the light's transform with respect to a given camera and anchor point
        /// </summary>
        /// <param name="camera">The camera to which values are relative</param>
        /// <param name="anchor">The light's desired anchor position</param>
        public void UpdateTransform(Camera camera, Vector3 anchor)
        {
            var axes = GetWorldSpaceAxes(camera);
            UpdateTransform(axes.up, axes.right, axes.forward, anchor);
        }

        Axes GetWorldSpaceAxes(Camera camera)
        {
            var viewToWorld = camera.cameraToWorldMatrix;
            if (m_UpIsWorldSpace)
            {
                var viewUp = (Vector3)(Camera.main.worldToCameraMatrix * Vector3.up);
                var worldTilt = Quaternion.FromToRotation(Vector3.up, viewUp);
                viewToWorld = viewToWorld * Matrix4x4.Rotate(worldTilt);
            }

            var up = (viewToWorld * Vector3.up).normalized;
            var right = (viewToWorld * Vector3.right).normalized;
            var forward = (viewToWorld * Vector3.forward).normalized;

            return new Axes
            {
                up = up,
                right = right,
                forward = forward
            };
        }

        void OnDrawGizmosSelected()
        {
            var axes = GetWorldSpaceAxes(Camera.main);
            var anchor = anchorPosition;
            var d = transform.position - anchor;
            var proj = Vector3.ProjectOnPlane(d, axes.up);

            var arcRadius = Mathf.Min(distance * 0.25f, k_ArcRadius);
            var axisLength = Mathf.Min(distance * 0.5f, k_AxisLength);
            var alpha = 0.2f;

            Handles.color = Color.grey;
            Handles.DrawDottedLine(anchorPosition, anchorPosition + proj, 2);
            Handles.DrawDottedLine(anchorPosition + proj, transform.position, 2);
            Handles.DrawDottedLine(anchorPosition, transform.position, 2);

            // forward
            var color = Color.blue;
            color.a = alpha;
            Handles.color = color;
            Handles.DrawLine(anchorPosition, anchorPosition + axes.forward * axisLength);
            Handles.DrawSolidArc(anchor, axes.up, axes.forward, yaw, arcRadius);

            // up
            color = Color.green;
            color.a = alpha;
            Handles.color = color;
            var yawRot = Quaternion.AngleAxis(yaw, axes.up * k_AxisLength);
            Handles.DrawSolidArc(anchor, yawRot * axes.right, yawRot * axes.forward, pitch, arcRadius);
            Handles.DrawLine(anchorPosition, anchorPosition + (yawRot * axes.forward) * axisLength);
        }

        // arguments are passed in world space
        void UpdateTransform(Vector3 up, Vector3 right, Vector3 forward, Vector3 anchor)
        {
            var worldYawRot = Quaternion.AngleAxis(m_Yaw, up);
            var worldPitchRot = Quaternion.AngleAxis(m_Pitch, right);
            var worldPosition = anchor + (worldYawRot * worldPitchRot) * forward * m_Distance;
            transform.position = worldPosition;

            var lookAt = (anchor - worldPosition).normalized;
            var worldRotation = Quaternion.LookRotation(lookAt, up) * Quaternion.AngleAxis(m_Roll, Vector3.forward);
            transform.rotation = worldRotation;
        }
    }
}
