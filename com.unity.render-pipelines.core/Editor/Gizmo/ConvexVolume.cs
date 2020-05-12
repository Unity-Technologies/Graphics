using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditorInternal;

namespace UnityEditor.Rendering
{
    public class ConvexVolume
    {
        static EditMode.SceneViewEditMode[] k_EditModes = new EditMode.SceneViewEditMode[]{
            (EditMode.SceneViewEditMode)106, (EditMode.SceneViewEditMode)107
        };
        static GUIContent[] k_ModesContent = new GUIContent[]{
            EditorGUIUtility.TrIconContent("MoveTool", "Translate the planes."),
            EditorGUIUtility.TrIconContent("RotateTool", "Rotate the planes.")
        };

        const float k_HandleSizeCoef = 0.1f;

        Color m_MonochromeHandleColor;
        Color m_WireframeColor;
        Color m_WireframeColorBehind;
        List<Vector4> m_Planes;
        List<Face> m_Faces;
        int m_Selected;

        /// <summary>The baseColor used to fill hull. All other colors are deduced from it except specific handle colors.</summary>
        public Color baseColor
        {
            set
            {
                value.a = 1f;
                m_MonochromeHandleColor = value;
                value.a = 0.7f;
                m_WireframeColor = value;
                value.a = 0.2f;
                m_WireframeColorBehind = value;
            }
        }

        public int selected
        {
            get { return m_Selected; }
            set
            {
                if (m_Selected != value && value < m_Faces.Count)
                {
                    m_Selected = value;
                    if (m_Selected != -1)
                        rotationOrigin = m_Faces[value].center;
                }
            }
        }

        /// <summary>The position of the center of the box in Handle.matrix space.</summary>
        public Vector3 center { get; set; }

        public ConvexVolume(Color baseColor)
        {
            this.baseColor = baseColor;

            m_Planes = new List<Vector4>();
            m_Faces = new List<Face>();
            m_Selected = -1;
        }

        public static bool DrawToolbar(Func<Bounds> getBoundsOfTargets, Editor owner)
        {
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUI.changed = false;

            EditMode.DoInspectorToolbar(k_EditModes, k_ModesContent, getBoundsOfTargets, owner);

            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            return ArrayUtility.IndexOf(k_EditModes, EditMode.editMode) != -1;
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
            return Mathf.Abs(denom) > Mathf.Epsilon;
        }

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
                                    m_Faces[i].inf_lines.Add(new Line() { a = points[0], b = dir });
                                else
                                    m_Faces[i].lines.Add(new Line() { a=points[0], b=points[1] });
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

            public Face(Vector4 p) {
                Vector3 norm = new Vector3(p.x, p.y, p.z).normalized;
                normal = norm == Vector3.zero ? Vector3.forward : norm;
                distance = p.w;
                lines = new List<Line>();
                inf_lines = new List<Line>();
            }
            public void Draw(float dist)
            {
                foreach (var l in lines)
                    Handles.DrawLine(l.a, l.b);
                foreach (var l in inf_lines)
                    Handles.DrawLine(l.a, l.a + dist * l.b);
            }
        }

        /// <summary>Draw the hull which means the boxes without the handles</summary>
        public void DrawHull()
        {
            Color previousColor = Handles.color;
            Matrix4x4 previousMatrix = Handles.matrix;
            Vector3 camera = Handles.inverseMatrix * Camera.current.transform.position;

            Handles.matrix *= Matrix4x4.Translate(center);

            float dot;
            List<Vector3> points = new List<Vector3>();
            for (int i = 0; i < m_Faces.Count; i++)
            {
                if (m_Faces[i].lines.Count != 0)
                    dot = Vector3.Dot(m_Faces[i].lines[0].a - camera, m_Faces[i].normal);
                else if (m_Faces[i].inf_lines.Count != 0)
                    dot = Vector3.Dot(m_Faces[i].inf_lines[0].a - camera, m_Faces[i].normal);
                else continue;

                if (m_Selected != i && dot <= 0.0f)
                {
                    Handles.color = m_WireframeColor;
                    Handles.zTest = UnityEngine.Rendering.CompareFunction.LessEqual;
                    m_Faces[i].Draw(-dot);

                    Handles.color = m_WireframeColorBehind;
                    Handles.zTest = UnityEngine.Rendering.CompareFunction.Greater;
                    m_Faces[i].Draw(-dot);
                }
                else
                {
                    Handles.color = (m_Selected == i) ? m_MonochromeHandleColor : m_WireframeColorBehind;
                    Handles.zTest = UnityEngine.Rendering.CompareFunction.Always;
                    m_Faces[i].Draw(Mathf.Abs(dot));
                }
            }

            Handles.zTest = UnityEngine.Rendering.CompareFunction.Always;
            Handles.matrix = previousMatrix;
            Handles.color = previousColor;
        }

        Vector3 rotationOrigin;

        /// <summary>Draw the manipulable handles</summary>
        public void DrawHandle()
        {
            Color previousColor = Handles.color;
            Vector3 camera = Handles.inverseMatrix * Camera.current.transform.position;

            int editMode = ArrayUtility.IndexOf(k_EditModes, EditMode.editMode);
            Color color = m_MonochromeHandleColor;
            Vector3 face, position;
            for (int i = 0; i < m_Faces.Count; i++)
            {
                face = m_Faces[i].center;
                position = face + center; 

                color.a = 1.0f;
                if (m_Selected == i)
                {
                    if (Vector3.Dot(face - camera, m_Faces[i].normal) > 0.0f)
                        color.a = 1.0f - Vector3.Dot((face - camera).normalized, m_Faces[i].normal) + 0.1f;
                    Handles.color = color;

                    if (editMode == 0)
                    {
                        EditorGUI.BeginChangeCheck();
                        Vector3 new_center = Handles.Slider(position, m_Faces[i].normal, HandleUtility.GetHandleSize(position), Handles.ArrowHandleCap, 0.5f);
                        if (EditorGUI.EndChangeCheck())
                        {
                            Vector4 plane = m_Planes[i];
                            plane.w += Vector3.Dot(new_center - position, m_Faces[i].normal);
                            m_Planes[i] = plane;
                        }
                        rotationOrigin = face;
                    }
                    else if (editMode == 1)
                    {
                        EditorGUI.BeginChangeCheck();
                        Quaternion rot = Handles.RotationHandle(Quaternion.LookRotation(m_Faces[i].normal, Vector3.up), rotationOrigin + center);
                        if (EditorGUI.EndChangeCheck())
                        {
                            Vector4 plane = (rot * Vector3.forward).normalized;
                            plane.w = m_Planes[i].w;
                            m_Planes[i] = plane;
                        }
                    }
                }
                else if (editMode != -1)
                {
                    float handleSize = k_HandleSizeCoef * HandleUtility.GetHandleSize(center);
                    Handles.color = color;
                    if (Handles.Button(position, Quaternion.identity, handleSize, handleSize, Handles.SphereHandleCap))
                    {
                        m_Selected = i;
                        rotationOrigin = face;
                    }
                }
            }

            Handles.color = previousColor;
        }
    }
}
