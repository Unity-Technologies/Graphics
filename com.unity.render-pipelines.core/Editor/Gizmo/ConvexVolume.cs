using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditorInternal;

namespace UnityEditor.Rendering
{
    /// <summary>
    /// Provide a gizmo/handle representing a convex volumes where all face can be moved independently.
    /// </summary>
    public class ConvexVolume
    {
        static EditMode.SceneViewEditMode[] k_EditModes = new EditMode.SceneViewEditMode[] {
            (EditMode.SceneViewEditMode)106, (EditMode.SceneViewEditMode)107
        };
        static KeyCode[] k_ToolbarShortcuts = new KeyCode[] {
            KeyCode.Alpha1,  KeyCode.Alpha2
        };
        static GUIContent[] k_ModesContent = new GUIContent[] {
            EditorGUIUtility.TrIconContent("MoveTool", "Translate the selected plane along its normal.\nPress alt while dragging a plane to snap it to the collider under the cursor."),
            EditorGUIUtility.TrIconContent("RotateTool", "Rotate the selected plane.")
        };

        const float k_HandleSizeCoef = 0.05f;

        Color m_MonochromeFillColor;
        Color m_MonochromeHandleColor;
        Color m_WireframeColor;
        Color m_WireframeColorBehind;

        List<Vector4> m_Planes;
        List<Face> m_Faces;
        int m_Selected;

        /// <summary>The baseColor used to draw the hull. All other colors are deduced from it.</summary>
        public Color baseColor
        {
            set
            {
                value.a = 8f / 255;
                m_MonochromeFillColor = value;
                value.a = 1f;
                m_MonochromeHandleColor = value;
                value.a = 0.7f;
                m_WireframeColor = value;
                value.a = 0.2f;
                m_WireframeColorBehind = value;
            }
        }

        /// <summary>The index of the selected plane or -1 if there is no selection.</summary>
        public int selected
        {
            get { return m_Selected; }
            set
            {
                if (value < m_Faces.Count)
                    m_Selected = value;
            }
        }

        /// <summary>The position of the center of the box in Handle.matrix space.</summary>
        public Vector3 center { get; set; }

        /// <summary>Constructor. Used to setup colors.</summary>
        /// <param name="baseColor">The color of the hull. Other colors are deduced from it.</param>
        public ConvexVolume(Color baseColor)
        {
            this.baseColor = baseColor;

            m_Planes = new List<Vector4>();
            m_Faces = new List<Face>();
            m_Selected = -1;
        }

        bool IntersectPlanes(Face p1, Face p2, Face p3, out Vector3 p)
        {
            p = Vector3.zero;
            Vector3 u = Vector3.Cross(p2.n, p3.n);
            float denom = Vector3.Dot(p1.n, u);
            if (Mathf.Abs(denom) < Mathf.Epsilon)
                return false; // Planes do not intersect in a point
            Vector3 temp = Vector3.Cross(p1.n, p3.d * p2.n - p2.d * p3.n);
            p = (p1.d*u + temp) / denom;
            return true;
        }

        bool IntersectPlanes(Face p1, Face p2, out Vector3 p, out Vector3 d)
        {
            p = Vector3.zero;
            // Compute direction of intersection line
            d = Vector3.Cross(p1.n, p2.n);
            // If d is (near) zero, the planes are parallel (and separated)
            // or coincident, so they’re not considered intersecting
            float denom = Vector3.Dot(d, d);
            if (denom < Mathf.Epsilon) return false;
            float d12 = Vector3.Dot(p1.n, p2.n);
            float k1 = p1.d - p2.d * d12;
            float k2 = p2.d - p1.d * d12;
            p = (k1 * p1.n + k2 * p2.n) / denom;
            return true;
        }

        bool IntersectSegmentPlane(Vector3 pos, Vector3 dir, Face p, out float t)
        {
            float denom = Vector3.Dot(dir, p.n);
            t = (p.d - Vector3.Dot(pos, p.n)) / denom;
            return Mathf.Abs(denom) > 0.1f;
        }

        /// <summary>The planes of the convex volume in Handle.matrix space.</summary>
        public Vector4[] planes
        {
            get { return m_Planes.ToArray(); }
            set
            {
                m_Planes.Clear();
                m_Faces.Clear();
                for (int i = 0; i < value.Length; i++)
                {
                    m_Planes.Add(value[i]);
                    m_Faces.Add(new Face(m_Planes[i]));
                }

                Vector3[] points = new Vector3[2];
                for (int i = 0; i < m_Planes.Count; i++)
                {
                    for (int j = 0; j < m_Planes.Count; j++)
                    {
                        if (j == i) continue;

                        if (IntersectPlanes(m_Faces[i], m_Faces[j], out Vector3 point, out Vector3 dir))
                        {
                            // Try to find a third and fourth plane that intersect the line
                            int[] indices = new int[2] { -1, -1 };
                            for (int k = 0; k < m_Planes.Count; k++)
                            {
                                if (k == i || k == j) continue;

                                int idx = Vector3.Dot(m_Faces[k].n, dir) > 0.0f ? 1 : 0;
                                if (indices[idx] == -1)
                                {
                                    if (IntersectSegmentPlane(point, dir, m_Faces[k], out float t))
                                    {
                                        indices[idx] = k;
                                        points[idx] = point + t * dir;
                                    }
                                }
                                else if (Vector3.Dot(m_Faces[k].n, points[idx]) > m_Faces[k].d)
                                {
                                    if (IntersectSegmentPlane(point, dir, m_Faces[k], out float t))
                                    {
                                        indices[idx] = k;
                                        points[idx] = point + t * dir;
                                    }
                                    else
                                        indices[idx] = -1;
                                }
                            }

                            // Make sure the points found are not actually outside of the shape
                            // If none or one of the two points are valid, we have an infinite intersection
                            // Else we simply have a line
                            if (indices[1] != -1) IntersectPlanes(m_Faces[i], m_Faces[j], m_Faces[indices[1]], out points[1]);
                            if (indices[0] != -1) IntersectPlanes(m_Faces[i], m_Faces[j], m_Faces[indices[0]], out points[0]);
                            else if (indices[1] == -1) points[0] = point;
                            else
                            {
                                points[0] = points[1];
                                dir = -dir;
                            }

                            bool outside = false;
                            for (int k = 0; k < m_Planes.Count; k++)
                            {
                                if (k == i || k == j) continue;
                                if ((k != indices[0] && Vector3.Dot(m_Faces[k].n, points[0]) > m_Faces[k].d) ||
                                    (k != indices[1] && indices[1] != -1 && Vector3.Dot(m_Faces[k].n, points[1]) > m_Faces[k].d))
                                {
                                    outside = true;
                                    break;
                                }
                            }
                            if (!outside)
                            {
                                if (indices[0] == -1 || indices[1] == -1)
                                    m_Faces[i].inf_lines.Add(new Line() { a = points[0], b = dir.normalized });
                                else
                                    m_Faces[i].lines.Add(new Line() { a=points[0], b=points[1] });
                            }
                        }
                    }

                    // Reorder lines to draw them with Handles.DrawAAConvexPolygon
                    if (m_Faces[i].inf_lines.Count == 0 && m_Faces[i].lines.Count != 0)
                    {
                        var lines = m_Faces[i].lines;
                        for (int k = 0; k < lines.Count - 1; k++)
                        for (int l = k + 1; l < lines.Count; l++)
                        {
                            if (lines[k].b == lines[l].a)
                            {
                                var temp = lines[k + 1];
                                lines[k + 1] = lines[l];
                                lines[l] = temp;
                                break;
                            }
                            else if (lines[k].b == lines[l].b)
                            {
                                var temp = lines[k + 1];
                                lines[k + 1] = new Line(){ a = lines[l].b, b = lines[l].a };
                                lines[l] = temp;
                                break;
                            }
                        }
                    }
                }
            }
        }

        class Line
        {
            public Vector3 a, b;
        }
        class Face
        {
            public Vector3 n { get { return normal; }}
            public float d { get { return distance; }}
            public Vector3 center
            { get {
                if (lines.Count != 0)
                {
                    Vector3 local = Vector3.zero;
                    foreach (var l in lines)
                        local += l.a + l.b;
                    return local / (2.0f * lines.Count);
                }
                else
                    return distance * normal;
            }}

            public Vector3 normal;
            public float distance;
            public List<Line> lines;
            public List<Line> inf_lines;

            public Face(Vector4 p)
            {
                Vector3 norm = new Vector3(p.x, p.y, p.z);
                normal = norm == Vector3.zero ? Vector3.forward : norm.normalized;
                distance = p.w;
                lines = new List<Line>();
                inf_lines = new List<Line>();
            }
            public void DrawFace()
            {
                Vector3[] points = new Vector3[lines.Count];
                for (int i = 0; i < lines.Count; i++)
                    points[i] = lines[i].a;
                Handles.DrawAAConvexPolygon(points);
            }
            public void DrawLines(float dist)
            {
                foreach (var l in lines)
                    Handles.DrawLine(l.a, l.b);
                foreach (var l in inf_lines)
                    Handles.DrawLine(l.a, l.a + dist * l.b);
            }
        }

        /// <summary>Draw the inspector toolbar</summary>
        /// <param name="owner">The editor drawing</param>
        /// <returns>True if the volume is in edit mode</returns>
        public static bool DrawToolbar(Editor owner)
        {
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUI.changed = false;

            EditMode.DoInspectorToolbar(k_EditModes, k_ModesContent, null, owner);

            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            return ArrayUtility.IndexOf(k_EditModes, EditMode.editMode) != -1;
        }

        /// <summary>Handles shortcuts to change edit mode</summary>
        /// <param name="owner">The editor handling the event</param>
        public static void DoToolbarShortcutKey(Editor owner)
        {
            int shortcut = ArrayUtility.IndexOf(k_ToolbarShortcuts, Event.current.keyCode);
            if (shortcut != -1 && Event.current.type == EventType.KeyUp)
            {
                var bounds = new Bounds();
                bounds.Encapsulate(((Component)owner.target).transform.position);

                var mode = EditMode.editMode == k_EditModes[shortcut] ? EditMode.SceneViewEditMode.None : k_EditModes[shortcut];
                EditorApplication.delayCall += () => EditMode.ChangeEditMode(mode, bounds, owner);
                Event.current.Use();
            }
        }

        /// <summary>Draw the hull which means the intersections between the planes</summary>
        /// <param name="filled">If true, also fill the faces of the hull</param>
        public void DrawHull(bool filled)
        {
            Color previousColor = Handles.color;
            Matrix4x4 previousMatrix = Handles.matrix;

            Vector3 camera = Handles.inverseMatrix * Camera.current.transform.position;
            Handles.matrix *= Matrix4x4.Translate(center);

            float dot = Vector3.Distance(center, camera) * 0.5f;
            for (int i = 0; i < m_Faces.Count; i++)
            {
                Handles.color = m_WireframeColorBehind;
                Handles.zTest = UnityEngine.Rendering.CompareFunction.Greater;
                m_Faces[i].DrawLines(Mathf.Abs(dot));

                Handles.color = m_WireframeColor;
                Handles.zTest = UnityEngine.Rendering.CompareFunction.LessEqual;
                m_Faces[i].DrawLines(Mathf.Abs(dot));

                if (m_Faces[i].lines.Count != 0)
                {
                    Handles.color = m_MonochromeFillColor;
                    m_Faces[i].DrawFace();
                }
            }

            Handles.zTest = UnityEngine.Rendering.CompareFunction.Always;
            Handles.matrix = previousMatrix;
            Handles.color = previousColor;
        }

        // Snap to collider under cursor
        bool TrySnapPlane(out Vector4 plane)
        {
            plane = Vector4.zero;

            if (!Event.current.shift)
                return false;

            var pos = new Vector2(Event.current.mousePosition.x, Camera.current.pixelHeight - Event.current.mousePosition.y);
            if (Physics.Raycast(Camera.current.ScreenPointToRay(pos), out RaycastHit hit))
            {
                plane = -(Handles.inverseMatrix * hit.normal).normalized;
                plane.w = Vector3.Dot((Vector3)(Handles.inverseMatrix * hit.point) - center, plane) - 0.05f;
                return true;
            }
            return false;
        }

        // Keep this values between frames otherwise the rotation gizmo is unstable
        Quaternion rotation;
        Vector3 rotationOrigin;

        /// <summary>Draw the manipulable handles</summary>
        public void DrawHandle()
        {
            int editMode = ArrayUtility.IndexOf(k_EditModes, EditMode.editMode);
            if (editMode == -1)
                return;

            Color previousColor = Handles.color;
            Vector3 camera = Handles.inverseMatrix * Camera.current.transform.position;

            Color color = m_MonochromeHandleColor;
            Vector3 face, position;
            for (int i = 0; i < m_Faces.Count; i++)
            {
                face = m_Faces[i].center;
                position = face + center;

                color.a = 1.0f;
                if (editMode == 0)
                {
                    Handles.color = color;
                    float handleSize = k_HandleSizeCoef * HandleUtility.GetHandleSize(position);

                    EditorGUI.BeginChangeCheck();
                    Vector3 newPos = Handles.Slider(position, m_Faces[i].normal, handleSize, Handles.DotHandleCap, EditorSnapSettings.scale);
                    if (EditorGUI.EndChangeCheck())
                    {
                        if (!TrySnapPlane(out Vector4 plane))
                        {
                            plane = m_Planes[i];
                            plane.w += Vector3.Dot(newPos - position, m_Faces[i].normal);
                        }
                        m_Planes[i] = plane;
                    }

                    //if (GUIUtility.hotControl != 0)
                    {
                        Handles.DrawLine(position, position + m_Faces[i].normal * 10.0f * handleSize);
                    }
                }
                else if (editMode == 1 && m_Selected == i)
                {
                    if (Vector3.Dot(position - camera, m_Faces[i].normal) > 0.0f)
                        color.a = 1.0f - Vector3.Dot((position - camera).normalized, m_Faces[i].normal) + 0.1f;
                    Handles.color = color;

                    if (GUIUtility.hotControl == 0)
                    {
                        rotation = Quaternion.LookRotation(m_Faces[i].normal, Vector3.up);
                        rotationOrigin = face;
                    }
                    EditorGUI.BeginChangeCheck();
                    rotation = Handles.RotationHandle(rotation, rotationOrigin + center);
                    if (EditorGUI.EndChangeCheck())
                    {
                        if (!TrySnapPlane(out Vector4 plane))
                        {
                            plane = (rotation * Vector3.forward).normalized;
                            plane.w = Vector3.Dot(rotationOrigin, plane);
                        }
                        m_Planes[i] = plane;
                    }
                }
                else
                {
                    Handles.color = color;
                    float handleSize = 2.0f * k_HandleSizeCoef * HandleUtility.GetHandleSize(center);
                    if (Handles.Button(position, Quaternion.identity, handleSize, handleSize, Handles.SphereHandleCap))
                        m_Selected = i;
                }
            }

            Handles.color = previousColor;
        }

        /// <summary>Returns an array of plane index not contributing to the convex shape.</summary>
        /// <param name="planes">The planes making the shape.</param>
        /// <returns>An aray of indices to useless planes</returns>
        public static int[] ComputeUselessPlanes(Vector4[] planes)
        {
            ConvexVolume vol = new ConvexVolume(Color.white);
            vol.planes = planes;

            var indices = new List<int>();
            for (int i = 0; i < vol.m_Faces.Count; i++)
            {
                if (vol.m_Faces[i].lines.Count == 0 && vol.m_Faces[i].inf_lines.Count == 0)
                    indices.Add(i);
            }
            return indices.ToArray();
        }
    }
}
