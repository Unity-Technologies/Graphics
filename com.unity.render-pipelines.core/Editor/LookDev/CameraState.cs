using UnityEditor.AnimatedValues;
using UnityEngine;

namespace UnityEditor.Rendering.LookDev
{
    [System.Serializable]
    public class CameraState
    {
        private static readonly Quaternion kDefaultRotation = Quaternion.LookRotation(new Vector3(0.0f, 0.0f, 1.0f));
        private const float kDefaultViewSize = 10f;
        private static readonly Vector3 kDefaultPivot = Vector3.zero;
        private const float kDefaultFoV = 90f;
        private static readonly float distanceCoef = 1f / Mathf.Tan(kDefaultFoV * 0.5f * Mathf.Deg2Rad);

        //Note: we need animation to do the same focus as in SceneView
        public AnimVector3 pivot = new AnimVector3(kDefaultPivot);
        public AnimQuaternion rotation = new AnimQuaternion(kDefaultRotation);
        public AnimFloat viewSize = new AnimFloat(kDefaultViewSize);

        public float distanceFromPivot => viewSize.value * distanceCoef;
        public Vector3 position
            => pivot.value + rotation.value * new Vector3(0, 0, -distanceFromPivot);
        public float fieldOfView => kDefaultFoV;
        public float farClip => Mathf.Max(1000f, 2000f * viewSize.value);

        public void UpdateCamera(Camera camera)
        {
            camera.transform.rotation = rotation.value;
            camera.transform.position = pivot.value + camera.transform.rotation * new Vector3(0, 0, -distanceFromPivot);

            float farClip = this.farClip;
            camera.nearClipPlane = farClip * 0.000005f;
            camera.farClipPlane = farClip;

            camera.fieldOfView = fieldOfView;
        }
    }
}
