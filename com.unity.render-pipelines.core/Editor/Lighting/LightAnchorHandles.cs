using UnityEngine;

namespace UnityEditor
{
    /// <summary>
    /// LightAnchorHandles describes the Handles for the LightAnchorEditorTool
    /// </summary>
    public class LightAnchorHandles
    {
        /// <summary>
        /// The light position
        /// </summary>
        public Vector3 lightPosition { get; set; }
        /// <summary>
        /// The anchor position
        /// </summary>
        public Vector3 anchorPosition { get; set; }

        /// <summary>
        /// Initializes and returns an instance of LightAnchorHandles
        /// </summary>
        public LightAnchorHandles()
        {
        }

        /// <summary>
        /// On GUI
        /// </summary>
        public void OnGUI()
        {
            Handles.color = Color.yellow;
            Handles.DrawDottedLine(lightPosition, anchorPosition, 2f);

            anchorPosition = Handles.PositionHandle(anchorPosition, Quaternion.identity);
        }

        private Vector3 GetForward()
        {
            if (Camera.current != null)
                return -Camera.current.transform.forward;

            return Vector3.forward;
        }

        private Vector3 GetUp()
        {
            if (Camera.current != null)
                return Camera.current.transform.up;

            return Vector3.up;
        }

        private Vector3 GetRight()
        {
            if (Camera.current != null)
                return Camera.current.transform.right;

            return Vector3.right;
        }

        bool DistanceRayLine(Ray ray, Vector3 p1, Vector3 p2, out Vector3 point, out float distance, out float t)
        {
            t = 0f;
            point = Vector3.zero;
            distance = float.MaxValue;

            var lineDirection = (p2 - p1);
            var normal = Vector3.Cross(Vector3.Cross(ray.direction, lineDirection), lineDirection);
            var plane = new Plane(normal, p1);

            float rayDistance;
            if (plane.Raycast(ray, out rayDistance))
            {
                point = ray.GetPoint(rayDistance);
                distance = DistancePointToLineSegment(point, p1, p2, out t);

                return true;
            }

            return false;
        }

        float DistancePointToLineSegment(Vector3 p, Vector3 a, Vector3 b, out float t)
        {
            t = 0f;
            var l2 = (b - a).sqrMagnitude;    // i.e. |b-a|^2 -  avoid a sqrt
            if (l2 == 0.0)
                return (p - a).magnitude;       // a == b case
            t = Vector3.Dot(p - a, b - a) / l2;
            if (t < 0.0)
                return (p - a).magnitude;       // Beyond the 'a' end of the segment
            if (t > 1.0)
                return (p - b).magnitude;         // Beyond the 'b' end of the segment
            var projection = a + t * (b - a); // Projection falls on the segment
            return (p - projection).magnitude;
        }
    }
}
