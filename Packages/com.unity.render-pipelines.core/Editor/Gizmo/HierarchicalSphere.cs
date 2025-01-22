using UnityEngine;

namespace UnityEditor.Rendering
{
    /// <summary>
    /// Provide a gizmo/handle representing a box where all face can be moved independently.
    /// Also add a contained sub gizmo/handle box if contained is used at creation.
    /// </summary>
    /// <example>
    /// <code>
    /// class MyComponentEditor : Editor
    /// {
    ///     static HierarchicalSphere sphere;
    ///     static HierarchicalSphere containedSphere;
    ///
    ///     static MyComponentEditor()
    ///     {
    ///         Color[] handleColors = new Color[]
    ///         {
    ///             Color.red,
    ///             Color.green,
    ///             Color.Blue,
    ///             new Color(0.5f, 0f, 0f, 1f),
    ///             new Color(0f, 0.5f, 0f, 1f),
    ///             new Color(0f, 0f, 0.5f, 1f)
    ///         };
    ///         sphere = new HierarchicalSphere(new Color(1f, 1f, 1f, 0.25));
    ///         containedSphere = new HierarchicalSphere(new Color(1f, 0f, 1f, 0.25), container: sphere);
    ///     }
    ///
    ///     [DrawGizmo(GizmoType.Selected|GizmoType.Active)]
    ///     void DrawGizmo(MyComponent comp, GizmoType gizmoType)
    ///     {
    ///         sphere.center = comp.transform.position;
    ///         sphere.size = comp.transform.scale;
    ///         sphere.DrawHull(gizmoType == GizmoType.Selected);
    ///
    ///         containedSphere.center = comp.innerposition;
    ///         containedSphere.size = comp.innerScale;
    ///         containedSphere.DrawHull(gizmoType == GizmoType.Selected);
    ///     }
    ///
    ///     void OnSceneGUI()
    ///     {
    ///         EditorGUI.BeginChangeCheck();
    ///
    ///         //container sphere must be also set for contained sphere for clamping
    ///         sphere.center = comp.transform.position;
    ///         sphere.size = comp.transform.scale;
    ///         sphere.DrawHandle();
    ///
    ///         containedSphere.center = comp.innerposition;
    ///         containedSphere.size = comp.innerScale;
    ///         containedSphere.DrawHandle();
    ///
    ///         if(EditorGUI.EndChangeCheck())
    ///         {
    ///             comp.innerposition = containedSphere.center;
    ///             comp.innersize = containedSphere.size;
    ///         }
    ///     }
    /// }
    /// </code>
    /// </example>
    public class HierarchicalSphere
    {
        const float k_HandleSizeCoef = 0.05f;

        static Material k_Material_Cache;
        static Material k_Material => (k_Material_Cache == null || k_Material_Cache.Equals(null) ? (k_Material_Cache = new Material(Shader.Find("Hidden/UnlitTransparentColored"))) : k_Material_Cache);
        static Mesh k_MeshSphere_Cache;
        static Mesh k_MeshSphere => k_MeshSphere_Cache == null || k_MeshSphere_Cache.Equals(null) ? (k_MeshSphere_Cache = Resources.GetBuiltinResource<Mesh>("New-Sphere.fbx")) : k_MeshSphere_Cache;

        Material m_Material;
        readonly HierarchicalSphere m_Parent;
        Color m_HandleColor;
        Color m_WireframeColor;
        Color m_WireframeColorBehind;

        Material material => m_Material == null || m_Material.Equals(null)
        ? (m_Material = new Material(k_Material))
        : m_Material;

        /// <summary>The position of the center of the box in Handle.matrix space.</summary>
        public Vector3 center { get; set; }

        /// <summary>The size of the box in Handle.matrix space.</summary>
        public float radius { get; set; }

        /// <summary>The baseColor used to fill hull. All other colors are deduced from it.</summary>
        public Color baseColor
        {
            get { return material.color; }
            set
            {
                value.a = 8f / 255;
                material.color = value;
                value.a = 1f;
                m_HandleColor = value;
                value.a = 0.7f;
                m_WireframeColor = value;
                value.a = 0.2f;
                m_WireframeColorBehind = value;
            }
        }

        /// <summary>Constructor. Used to setup colors and also the container if any.</summary>
        /// <param name="baseColor">The color of filling. All other colors are deduced from it.</param>
        /// <param name="parent">The HierarchicalSphere containing this sphere. If null, the sphere will not be limited in size.</param>
        public HierarchicalSphere(Color baseColor, HierarchicalSphere parent = null)
        {
            m_Parent = parent;
            m_Material = new Material(k_Material);
            this.baseColor = baseColor;
        }

        /// <summary>Draw the hull which means the boxes without the handles</summary>
        /// <param name="filled">If true, also draw the surface of the hull's sphere</param>
        public void DrawHull(bool filled)
        {
            if (Event.current.type != EventType.Repaint)
                return;

            Color wireframeColor = m_HandleColor;
            wireframeColor.a = 0.8f;
            using (new Handles.DrawingScope(m_WireframeColor, Matrix4x4.TRS((Vector3)Handles.matrix.GetColumn(3) + center, Quaternion.identity, Vector3.one)))
            {
                if (filled)
                {
                    material.SetPass(0);
                    Matrix4x4 drawMatrix = Matrix4x4.TRS((Vector3)Handles.matrix.GetColumn(3), Quaternion.identity, Vector3.one * radius * 2f);
                    Graphics.DrawMeshNow(k_MeshSphere, drawMatrix);
                }

                var drawCenter = Vector3.zero;
                var viewPlaneNormal = Vector3.zero;
                var drawnRadius = radius;
                if (Camera.current.orthographic)
                    viewPlaneNormal = Camera.current.transform.forward;
                else
                {
                    viewPlaneNormal = (Vector3)Handles.matrix.GetColumn(3) - Camera.current.transform.position;
                    var sqrDist = viewPlaneNormal.sqrMagnitude; // squared distance from camera to center
                    var sqrRadius = radius * radius; // squared radius
                    var sqrOffset = sqrRadius * sqrRadius / sqrDist; // squared distance from actual center to drawn disc center
                    var insideAmount = sqrOffset / sqrRadius;

                    // If we are not inside the sphere, calculate where to draw the periphery
                    if (insideAmount < 1)
                    {
                        drawnRadius = Mathf.Sqrt(sqrRadius - sqrOffset); // the radius of the drawn disc
                        drawCenter -= (sqrRadius / sqrDist) * viewPlaneNormal;
                    }
                }

                Handles.zTest = UnityEngine.Rendering.CompareFunction.LessEqual;
                Handles.DrawWireDisc(Vector3.zero, Vector3.up, radius);
                Handles.DrawWireDisc(drawCenter, viewPlaneNormal, drawnRadius);

                Handles.color = m_WireframeColorBehind;
                Handles.zTest = UnityEngine.Rendering.CompareFunction.Greater;
                Handles.DrawWireDisc(Vector3.zero, Vector3.up, radius);
                Handles.DrawWireDisc(drawCenter, viewPlaneNormal, drawnRadius);
                Handles.zTest = UnityEngine.Rendering.CompareFunction.Always;
            }
        }

        /// <summary>Draw the manipulable handles</summary>
        public void DrawHandle()
        {
            using (new Handles.DrawingScope(m_HandleColor))
            {
                radius = Handles.RadiusHandle(Quaternion.identity, center, radius, handlesOnly: true);
                if (m_Parent != null)
                    radius = Mathf.Min(radius, m_Parent.radius - Vector3.Distance(center, m_Parent.center));
            }
        }
    }
}
