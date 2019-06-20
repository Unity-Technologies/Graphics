using UnityEditor.AnimatedValues;
using UnityEngine;

namespace UnityEditor.Rendering.Experimental.LookDev
{
    public interface ICameraUpdater
    {
        void UpdateCamera(Camera camera);
    }

    [System.Serializable]
    public class CameraState : ICameraUpdater
    {
        private static readonly Quaternion k_DefaultRotation = Quaternion.LookRotation(new Vector3(0.0f, 0.0f, 1.0f));
        private const float k_DefaultViewSize = 10f;
        private static readonly Vector3 k_DefaultPivot = Vector3.zero;
        private const float k_DefaultFoV = 90f;
        private const float k_NearFactor = 0.000005f;
        private const float k_MaxFar = 1000;

        [field: SerializeField]
        public Vector3 pivot { get; set; } = k_DefaultPivot;
        [field: SerializeField]
        public Quaternion rotation { get; set; } = k_DefaultRotation;
        [field: SerializeField]
        public float viewSize { get; set; } = k_DefaultViewSize;

        public float distanceFromPivot
            // distance coeficient from vertical FOV should be
            // 1f / Mathf.Tan(kDefaultFoV * 0.5f * Mathf.Deg2Rad)
            // but with fixed FoV of 90, this coef is always equal to 1f
            => viewSize;
        public Vector3 position
            => pivot + rotation * new Vector3(0, 0, -distanceFromPivot);
        public float fieldOfView => k_DefaultFoV;
        public float farClip => Mathf.Max(k_MaxFar, 2 * k_MaxFar * viewSize);
        public float nearClip => farClip * k_NearFactor;
        public Vector3 forward => rotation * Vector3.forward;
        public Vector3 up => rotation * Vector3.up;
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
                - up * verticalDistance * normalizedScreenPoint.y
                - right * verticalDistance * aspect * normalizedScreenPoint.x;
        }

        //Pivot is always on center axis by construction
        internal Vector3 QuickProjectPivotInScreen(Rect screen)
            => new Vector3(screen.width* .5f, screen.height* .5f, distanceFromPivot);

        public void UpdateCamera(Camera camera)
        {
            camera.transform.rotation = rotation;
            camera.transform.position = position;
            camera.nearClipPlane = nearClip;
            camera.farClipPlane = farClip;
            camera.fieldOfView = fieldOfView;
        }

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
