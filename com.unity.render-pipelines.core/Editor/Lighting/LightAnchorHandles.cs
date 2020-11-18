using UnityEngine;
using UnityEditor;

namespace UnityEditor
{
    public class LightAnchorHandles
    {
        GUIState m_GUIState = new GUIState();
        GUISystem m_GUISystem;

        public Vector3 lightPosition { get; set; }
        public Vector3 anchorPosition { get; set; }

        public LightAnchorHandles()
        {
            m_GUISystem = new GUISystem(m_GUIState);

            var lightControl = new GenericControl("Light")
            {
                position = (index) => lightPosition,
                distance = (guiState, index) =>
                {
                    return guiState.DistanceToCircle(lightPosition, guiState.GetHandleSize(lightPosition) * 5f);
                },
                forward = (index) => GetForward(),
                up = (index) => GetUp(),
                right = (index) => GetRight(),
                onRepaint = (guiState, control, index) =>
                {
                    Handles.color = Color.yellow;
                    Handles.DrawDottedLine(lightPosition, anchorPosition, 2f);
                }
            };

            var startLightPosition = Vector3.zero;
            var startAnchorPosittion = Vector3.zero;
            var distanceSlider = new SliderAction(lightControl)
            {
                onClick = (guiState, control) =>
                {
                    startLightPosition = lightPosition;
                    startAnchorPosittion = anchorPosition;
                },
                onSliderChanged = (guiState, control, position) =>
                {
                    var ray = HandleUtility.GUIPointToWorldRay(guiState.mousePosition);

                    Vector3 worldPoint; float newDistance; float t;
                    if (DistanceRayLine(ray, startAnchorPosittion, startLightPosition, out worldPoint, out newDistance, out t))
                    {
                        t = Mathf.Max(0f, t);
                        var magnitude = ((startLightPosition - startAnchorPosittion) * t).magnitude;
                        magnitude = Mathf.Max(1f, magnitude);
                        lightPosition = (startLightPosition - startAnchorPosittion).normalized * magnitude + startAnchorPosittion;
                    }
                }
            };

            var anchorManipulator = new HandlesManipulator()
            {
                onGui = (guiState) =>
                {
                    anchorPosition = Handles.PositionHandle(anchorPosition, Quaternion.identity);
                }
            };

            m_GUISystem.AddControl(lightControl);
            m_GUISystem.AddAction(distanceSlider);
            m_GUISystem.AddManipulator(anchorManipulator);
        }

        public void OnGUI()
        {
            m_GUISystem.OnGUI();
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
