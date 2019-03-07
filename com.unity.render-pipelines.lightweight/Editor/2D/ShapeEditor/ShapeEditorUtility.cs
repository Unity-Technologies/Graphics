using UnityEngine;
using UnityEditor;

namespace Unity.Path2D
{
    public class ShapeEditorUtility
    {
        public static int Mod(int x, int m)
        {
            int r = x % m;
            return r < 0 ? r + m : r;
        }

        public static float DistanceToSegment(Vector3 p1, Vector3 p2)
        {
            p1 = HandleUtility.WorldToGUIPoint(p1);
            p2 = HandleUtility.WorldToGUIPoint(p2);

            return HandleUtility.DistancePointToLineSegment(Event.current.mousePosition, p1, p2);
        }

        public static float DistanceToCircle(Vector3 center, float radius)
        {
            return HandleUtility.DistanceToCircle(center, radius);
        }

        public static Vector3 GUIToWorld(Vector2 guiPosition)
        {
            if (Camera.current)
                GUIToWorld(guiPosition, Camera.current.transform.forward, Vector3.zero);

            return GUIToWorld(guiPosition, Vector3.forward, Vector3.zero);
        }

        public static Vector3 GUIToWorld(Vector2 guiPosition, Vector3 planeNormal, Vector3 planePos)
        {
            Vector3 worldPos = Handles.inverseMatrix.MultiplyPoint(guiPosition);

            if (Camera.current)
            {
                Ray ray = HandleUtility.GUIPointToWorldRay(guiPosition);

                planeNormal = Handles.matrix.MultiplyVector(planeNormal);

                planePos = Handles.matrix.MultiplyPoint(planePos);

                Plane plane = new Plane(planeNormal, planePos);

                float distance = 0f;

                if (plane.Raycast(ray, out distance))
                {
                    worldPos = Handles.inverseMatrix.MultiplyPoint(ray.GetPoint(distance));
                }
            }

            return worldPos;
        }

        public static float GetHandleSize(Vector3 position)
        {
            var scale = Camera.current != null ? 0.01f : 0.05f;
            return HandleUtility.GetHandleSize(position) * scale;
        }

        public static void DrawGUIStyleCap(int controlID, Vector3 position, Quaternion rotation, float size, GUIStyle guiStyle)
        {
            if (Camera.current && Vector3.Dot(position - Camera.current.transform.position, Camera.current.transform.forward) < 0f)
                return;

            Handles.BeginGUI();
            guiStyle.Draw(GetGUIStyleRect(guiStyle, position), GUIContent.none, controlID);
            Handles.EndGUI();
        }

        private static Rect GetGUIStyleRect(GUIStyle style, Vector3 position)
        {
            Vector2 vector = HandleUtility.WorldToGUIPoint(position);

            float fixedWidth = style.fixedWidth;
            float fixedHeight = style.fixedHeight;

            return new Rect(vector.x - fixedWidth / 2f, vector.y - fixedHeight / 2f, fixedWidth, fixedHeight);
        }
    }
}
