using UnityEditor.AnimatedValues;
using UnityEngine;

namespace UnityEditor.Rendering.LookDev
{
    /// <summary>
    /// Interface to comunicate with simple <see cref="Renderer"/>
    /// </summary>
    public interface ICameraUpdater
    {
        /// <summary>Method called To update the LookDev camera position</summary>
        /// <param name="camera">The camera</param>
        void UpdateCamera(Camera camera);
    }

    /// <summary>
    /// Class containing data regarding position, rotation and viewport size of a camera
    /// </summary>
    [System.Serializable]
    public class CameraState : ICameraUpdater
    {
        private static readonly Quaternion k_DefaultRotation = Quaternion.LookRotation(new Vector3(0.0f, 0.0f, 1.0f));
        private const float k_DefaultViewSize = 10f;
        private static readonly Vector3 k_DefaultPivot = Vector3.zero;
        private const float k_DefaultFoV = 90f;
        private const float k_NearFactor = 0.000005f;
        private const float k_MaxFar = 1000;

        /// <summary>The position of the camera pivot</summary>
        [field: SerializeField]
        public Vector3 pivot { get; set; } = k_DefaultPivot;

        /// <summary>The rotation of the camera arround the pivot</summary>
        [field: SerializeField]
        public Quaternion rotation { get; set; } = k_DefaultRotation;

        /// <summary>The size of the view</summary>
        [field: SerializeField]
        public float viewSize { get; set; } = k_DefaultViewSize;

        /// <summary>The distance from pivot</summary>
        public float distanceFromPivot
        // distance coeficient from vertical FOV should be
        // 1f / Mathf.Tan(kDefaultFoV * 0.5f * Mathf.Deg2Rad)
        // but with fixed FoV of 90, this coef is always equal to 1f
            => viewSize;

        /// <summary>The position of the camera</summary>
        public Vector3 position
            => pivot + rotation * new Vector3(0, 0, -distanceFromPivot);

        /// <summary>The field of view of the camera</summary>
        public float fieldOfView => k_DefaultFoV;

        /// <summary>The far clip distance from camera</summary>
        public float farClip => Mathf.Max(k_MaxFar, 2 * k_MaxFar * viewSize);

        /// <summary>The near clip distance from camera</summary>
        public float nearClip => farClip * k_NearFactor;

        /// <summary>The Forward vector in world space</summary>
        public Vector3 forward => rotation * Vector3.forward;

        /// <summary>The Up vector in world space</summary>
        public Vector3 up => rotation * Vector3.up;

        /// <summary>The Right vector in world space</summary>
        public Vector3 right => rotation * Vector3.right;

        internal Vector3 QuickReprojectionWithFixedFOVOnPivotPlane(Rect screen, Vector2 screenPoint)
        {
            if (screen.height == 0)
                return Vector3.zero;
            float aspect = screen.width / screen.height;
            //Note: verticalDistance is same than distance from pivot with fixed FoV 90°
            float verticalDistance = distanceFromPivot;
            Vector2 normalizedScreenPoint = new Vector2(
                screenPoint.x * 2f / screen.width - 1f,
                screenPoint.y * 2f / screen.height - 1f);
            return pivot
                - verticalDistance * normalizedScreenPoint.y * up
                - verticalDistance * aspect * normalizedScreenPoint.x * right;
        }

        //Pivot is always on center axis by construction
        internal Vector3 QuickProjectPivotInScreen(Rect screen)
            => new Vector3(screen.width * .5f, screen.height * .5f, distanceFromPivot);

        /// <summary>
        /// Update a Camera component and its transform with this state values
        /// </summary>
        /// <param name="camera">The camera to update</param>
        public void UpdateCamera(Camera camera)
        {
            camera.transform.SetPositionAndRotation(position, rotation);
            camera.nearClipPlane = nearClip;
            camera.farClipPlane = farClip;
            camera.fieldOfView = fieldOfView;
        }

        /// <summary>
        /// Reset the State to its default values
        /// </summary>
        public void Reset()
        {
            pivot = k_DefaultPivot;
            rotation = k_DefaultRotation;
            viewSize = k_DefaultViewSize;
        }

        internal void SynchronizeFrom(CameraState other)
        {
            pivot = other.pivot;
            rotation = other.rotation;
            viewSize = other.viewSize;
        }
    }
}
