#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.Rendering;

namespace UnityEngine
{
    /// <summary>
    /// Represents camera-space light controls around a virtual pivot point.
    /// </summary>
    [AddComponentMenu("Rendering/Light Anchor")]
    [ExecuteInEditMode]
    [DisallowMultipleComponent]
    [CoreRPHelpURLAttribute("View-Lighting-Tool")]
    public class LightAnchor : MonoBehaviour
    {
        const float k_ArcRadius = 5;
        const float k_AxisLength = 10;
        internal const float k_MaxDistance = 10000f;

        [SerializeField, Min(0)]
        float m_Distance = 0f;
        [SerializeField]
        UpDirection m_FrameSpace = UpDirection.World;
        [SerializeField]
        Transform m_AnchorPositionOverride;
        [SerializeField]
        Vector3 m_AnchorPositionOffset;

        [SerializeField]
        float m_Yaw;
        [SerializeField]
        float m_Pitch;
        [SerializeField]
        float m_Roll;

        /// <summary>
        /// The camera-relative yaw.
        /// </summary>
        /// <remarks>
        /// The range is -180 through 180 inclusive. Values between 0 and 180 are to the right of the camera, and values between 0 and -180 to the left.
        /// </remarks>
        public float yaw
        {
            get { return m_Yaw; }
            set { m_Yaw = NormalizeAngleDegree(value); }
        }

        /// <summary>
        /// The pitch relative to the horizon or camera depending on value of m_Space.
        /// </summary>
        /// <remarks>
        /// The range is -180 through 180 inclusive. Values between 0 and 180 are below the camera, and values between 0 and -180 are above the camera.
        /// </remarks>
        public float pitch
        {
            get { return m_Pitch; }
            set { m_Pitch = NormalizeAngleDegree(value); }
        }

        /// <summary>
        /// The camera-relative roll.
        /// </summary>
        /// <remarks>
        /// The range is -180 through 180 inclusive. Values between 0 and 180 are to the right of the camera, and values between 0 and -180 are to the left of the camera.
        /// </remarks>
        public float roll
        {
            get { return m_Roll; }
            set { m_Roll = NormalizeAngleDegree(value); }
        }

        /// <summary>
        /// The distance from the light's anchor point.
        /// </summary>
        public float distance
        {
            get => m_Distance;
            set => m_Distance = Mathf.Clamp(value, 0f, k_MaxDistance);
        }

        /// <summary>
        /// Enum to describes to up vector for the Light Anchor
        /// </summary>
        public enum UpDirection
        {
            /// <summary>
            /// Up vector is world space Vector3.up
            /// </summary>
            World = Space.World,
            /// <summary>
            /// Up vector is the up of the main camera
            /// </summary>
            Local = Space.Self
        }

        /// <summary>
        /// Indicates whether the up vector should be in world or camera space.
        /// </summary>
        public UpDirection frameSpace
        {
            get { return m_FrameSpace; }
            set { m_FrameSpace = value; }
        }

        /// <summary>
        /// The position of the light's anchor point.
        /// </summary>
        public Vector3 anchorPosition
        {
            get
            {
                if (anchorPositionOverride != null)
                    return anchorPositionOverride.position + anchorPositionOverride.TransformDirection(anchorPositionOffset);
                else
                    return transform.position + transform.forward * distance;
            }
        }

        struct Axes
        {
            public Vector3 up;
            public Vector3 right;
            public Vector3 forward;
        }

        /// <summary>
        /// Overrides the pivot of used to compute the light position. This is useful to track an existing object in the scene.
        /// The transform of the light will be automatically updated by the Update() method of the LightAnchor.
        /// </summary>
        public Transform anchorPositionOverride
        {
            get => m_AnchorPositionOverride;
            set => m_AnchorPositionOverride = value;
        }

        /// <summary>
        /// Offset relative to the position of the anchor position override transform in object space.
        /// </summary>
        public Vector3 anchorPositionOffset
        {
            get => m_AnchorPositionOffset;
            set => m_AnchorPositionOffset = value;
        }

        /// <summary>
        /// Normalizes the input angle to be in the range of -180 and 180.
        /// </summary>
        /// <param name="angle">Raw input angle or rotation.</param>
        /// <returns>Returns the angle of rotation between -180 and 180.</returns>
        public static float NormalizeAngleDegree(float angle)
        {
            const float range = 360f;
            const float startValue = -180f;
            var offset = angle - startValue;

            return offset - (Mathf.Floor(offset / range) * range) + startValue;
        }

        /// <summary>
        /// Updates Yaw, Pitch, Roll, and Distance based on the Transform.
        /// </summary>
        /// <param name="camera">The Camera to which light values are relative.</param>
        public void SynchronizeOnTransform(Camera camera)
        {
            Axes axes = GetWorldSpaceAxes(camera);

            Vector3 worldAnchorToLight = transform.position - anchorPosition;

            // In case the distance is 0 or the anchor override is at the same position than the light anchor
            if (worldAnchorToLight.magnitude == 0)
                worldAnchorToLight = -transform.forward;

            Vector3 projectOnGround = Vector3.ProjectOnPlane(worldAnchorToLight, axes.up);
            projectOnGround.Normalize();

            float extractedYaw = Vector3.SignedAngle(axes.forward, projectOnGround, axes.up);

            Vector3 yawedRight = Quaternion.AngleAxis(extractedYaw, axes.up) * axes.right;
            float extractedPitch = Vector3.SignedAngle(projectOnGround, worldAnchorToLight, yawedRight);

            yaw = extractedYaw;
            pitch = extractedPitch;
            roll = transform.rotation.eulerAngles.z;
        }

        /// <summary>
        /// Updates the light's transform with respect to a given camera and anchor point
        /// </summary>
        /// <param name="camera">The camera to which values are relative.</param>
        /// <param name="anchor">The anchor position.</param>
        public void UpdateTransform(Camera camera, Vector3 anchor)
        {
            var axes = GetWorldSpaceAxes(camera);
            UpdateTransform(axes.up, axes.right, axes.forward, anchor);
        }

        Axes GetWorldSpaceAxes(Camera camera)
        {
            // Fallback when the light anchor object is child of the camera (bad setup)
            if (transform.IsChildOf(camera.transform))
            {
                return new Axes
                {
                    up = Vector3.up,
                    right = Vector3.right,
                    forward = Vector3.forward,
                };
            }

            Matrix4x4 viewToWorld = camera.cameraToWorldMatrix;

            // Correct view to world for perspective
            if (!camera.orthographic && camera.transform.position != anchorPosition)
            {
                var d = (anchorPosition - camera.transform.position).normalized;
                var f = Quaternion.LookRotation(d);
                viewToWorld = Matrix4x4.Scale(new Vector3(1, 1, -1)) * Matrix4x4.TRS(camera.transform.position, f, Vector3.one).inverse;
                viewToWorld = viewToWorld.inverse;
            }

            if (m_FrameSpace == UpDirection.World)
            {
                Vector3 viewUp = (Vector3)(Camera.main.worldToCameraMatrix * Vector3.up);
                Quaternion worldTilt = Quaternion.FromToRotation(Vector3.up, viewUp);
                viewToWorld = viewToWorld * Matrix4x4.Rotate(worldTilt);
            }

            Vector3 up = (viewToWorld * Vector3.up).normalized;
            Vector3 right = (viewToWorld * Vector3.right).normalized;
            Vector3 forward = (viewToWorld * Vector3.forward).normalized;

            return new Axes
            {
                up = up,
                right = right,
                forward = forward
            };
        }

        void Update()
        {
            if (anchorPositionOverride == null || Camera.main == null)
                return;

            if (anchorPositionOverride.hasChanged || Camera.main.transform.hasChanged)
                UpdateTransform(Camera.main, anchorPosition);
        }

        void OnDrawGizmosSelected()
        {
            var camera = Camera.main;

            if (camera == null)
            {
                return;
            }

            // TODO: fix light rotated when camera rotates

            Axes axes = GetWorldSpaceAxes(camera);
            Vector3 anchor = anchorPosition;
            Vector3 d = transform.position - anchor;
            Vector3 proj = Vector3.ProjectOnPlane(d, axes.up);

            float arcRadius = Mathf.Min(distance * 0.25f, k_ArcRadius);
            float axisLength = Mathf.Min(distance * 0.5f, k_AxisLength);

#if UNITY_EDITOR
            const float alpha = 0.2f;

            Handles.color = Color.grey;
            Handles.DrawDottedLine(anchorPosition, anchorPosition + proj, 2);
            Handles.DrawDottedLine(anchorPosition + proj, transform.position, 2);
            Handles.DrawDottedLine(anchorPosition, transform.position, 2);

            // forward
            Color color = Color.green;
            color.a = alpha;
            Handles.color = color;
            Handles.DrawLine(anchorPosition, anchorPosition + axes.forward * axisLength);
            Handles.DrawSolidArc(anchor, axes.up, axes.forward, yaw, arcRadius);

            // up
            color = Color.blue;
            color.a = alpha;
            Handles.color = color;
            Quaternion yawRot = Quaternion.AngleAxis(yaw, axes.up * k_AxisLength);
            Handles.DrawSolidArc(anchor, yawRot * axes.right, yawRot * axes.forward, pitch, arcRadius);
            Handles.DrawLine(anchorPosition, anchorPosition + (yawRot * axes.forward) * axisLength);
#endif
        }

        // arguments are passed in world space
        void UpdateTransform(Vector3 up, Vector3 right, Vector3 forward, Vector3 anchor)
        {
            Quaternion worldYawRot = Quaternion.AngleAxis(m_Yaw, up);
            Quaternion worldPitchRot = Quaternion.AngleAxis(m_Pitch, right);
            Vector3 worldPosition = anchor + (worldYawRot * worldPitchRot) * forward * distance;
            transform.position = worldPosition;

            Vector3 lookAt = -((worldYawRot * worldPitchRot) * forward).normalized;
            Vector3 angles = Quaternion.LookRotation(lookAt, up).eulerAngles;
            angles.z = m_Roll;
            transform.eulerAngles = angles;
        }
    }
}
