using System;
using UnityEngine;
using System.Reflection;

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
    ///     static HierarchicalBox box;
    ///     static HierarchicalBox containedBox;
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
    ///         box = new HierarchicalBox(new Color(1f, 1f, 1f, 0.25), handleColors);
    ///         containedBox = new HierarchicalBox(new Color(1f, 0f, 1f, 0.25), handleColors, container: box);
    ///     }
    ///
    ///     [DrawGizmo(GizmoType.Selected|GizmoType.Active)]
    ///     void DrawGizmo(MyComponent comp, GizmoType gizmoType)
    ///     {
    ///         box.center = comp.transform.position;
    ///         box.size = comp.transform.scale;
    ///         box.DrawHull(gizmoType == GizmoType.Selected);
    ///
    ///         containedBox.center = comp.innerposition;
    ///         containedBox.size = comp.innerScale;
    ///         containedBox.DrawHull(gizmoType == GizmoType.Selected);
    ///     }
    ///
    ///     void OnSceneGUI()
    ///     {
    ///         EditorGUI.BeginChangeCheck();
    ///
    ///         //container box must be also set for contained box for clamping
    ///         box.center = comp.transform.position;
    ///         box.size = comp.transform.scale;
    ///         box.DrawHandle();
    ///
    ///         containedBox.DrawHandle();
    ///         containedBox.center = comp.innerposition;
    ///         containedBox.size = comp.innerScale;
    ///
    ///         if(EditorGUI.EndChangeCheck())
    ///         {
    ///             comp.innerposition = containedBox.center;
    ///             comp.innersize = containedBox.size;
    ///         }
    ///     }
    /// }
    /// </code>
    /// </example>
    public class HierarchicalBox
    {
        const float k_HandleSizeCoef = 0.05f;
        static Material k_Material_Cache;
        static Material k_Material => (k_Material_Cache == null || k_Material_Cache.Equals(null) ? (k_Material_Cache = new Material(Shader.Find("Hidden/UnlitTransparentColored"))) : k_Material_Cache);
        static Mesh k_MeshQuad_Cache;
        static Mesh k_MeshQuad => k_MeshQuad_Cache == null || k_MeshQuad_Cache.Equals(null) ? (k_MeshQuad_Cache = Resources.GetBuiltinResource<Mesh>("Quad.fbx")) : k_MeshQuad_Cache;

        enum NamedFace { Right, Top, Front, Left, Bottom, Back, None }

        Material m_Material;
        readonly Color[] m_PolychromeHandleColor;
        readonly HierarchicalBox m_Parent;
        Color m_MonochromeFillColor;
        Color m_MonochromeHandleColor;
        Color m_WireframeColor;
        Color m_WireframeColorBehind;
        int[] m_ControlIDs = new int[6] { 0, 0, 0, 0, 0, 0 };
        bool m_MonoHandle = true;

        Material material
        {
            get
            {
                if (m_Material == null || m_Material.Equals(null))
                    m_Material = new Material(k_Material);
                //material can be lost when exiting play mode so gather the color again when reconstructing it
                m_Material.color = m_MonochromeFillColor;   
                return m_Material;
            }
        }

        /// <summary>
        /// Allow to switch between the mode where all axis are controlled together or not
        /// Note that if there is several handles, they will use the polychrome colors.
        /// </summary>
        public bool monoHandle { get => m_MonoHandle; set => m_MonoHandle = value; }

        /// <summary>The position of the center of the box in Handle.matrix space.</summary>
        public Vector3 center { get; set; }

        /// <summary>The size of the box in Handle.matrix space.</summary>
        public Vector3 size { get; set; }

        /// <summary>The baseColor used to fill hull. All other colors are deduced from it except specific handle colors.</summary>
        public Color baseColor
        {
            get { return material.color; }
            set
            {
                value.a = 8f / 255;
                m_MonochromeFillColor = value;
                material.color = m_MonochromeFillColor;
                value.a = 1f;
                m_MonochromeHandleColor = value;
                value.a = 0.7f;
                m_WireframeColor = value;
                value.a = 0.2f;
                m_WireframeColorBehind = value;
            }
        }

        //Note: Handles.Slider not allow to use a specific ControlID.
        //Thus Slider1D is used (with reflection)
        static Type k_Slider1D = Type.GetType("UnityEditorInternal.Slider1D, UnityEditor");
        static MethodInfo k_Slider1D_Do = k_Slider1D
                .GetMethod(
                    "Do",
                    BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public,
                    null,
                    CallingConventions.Any,
                    new[] { typeof(int), typeof(Vector3), typeof(Vector3), typeof(float), typeof(Handles.CapFunction), typeof(float) },
                    null);
        static void Slider1D(int controlID, ref Vector3 handlePosition, Vector3 handleOrientation, float snapScale, Color color)
        {
            using (new Handles.DrawingScope(color))
            {
                handlePosition = (Vector3)k_Slider1D_Do.Invoke(null, new object[]
                    {
                        controlID,
                        handlePosition,
                        handleOrientation,
                        HandleUtility.GetHandleSize(handlePosition) * k_HandleSizeCoef,
                        new Handles.CapFunction(Handles.DotHandleCap),
                        snapScale
                    });
            }
        }

        /// <summary>Constructor. Used to setup colors and also the container if any.</summary>
        /// <param name="baseColor">The color of each face of the box. Other colors are deduced from it.</param>
        /// <param name="polychromeHandleColors">The color of handle when they are separated. When they are grouped, they use a variation of the faceColor instead.</param>
        /// <param name="parent">The HierarchicalBox containing this box. If null, the box will not be limited in size.</param>
        public HierarchicalBox(Color baseColor, Color[] polychromeHandleColors = null, HierarchicalBox parent = null)
        {
            if (polychromeHandleColors != null && polychromeHandleColors.Length != 6)
                throw new ArgumentException("polychromeHandleColors must be null or have a size of 6.");

            m_Parent = parent;
            m_Material = new Material(k_Material);
            this.baseColor = baseColor;
            m_PolychromeHandleColor = polychromeHandleColors ?? new Color[]
            {
                Handles.xAxisColor, Handles.yAxisColor, Handles.zAxisColor,
                Handles.xAxisColor, Handles.yAxisColor, Handles.zAxisColor
            };
        }

        /// <summary>Draw the hull which means the boxes without the handles</summary>
        /// <param name="filled">If true, also fill the faces of the hull</param>
        public void DrawHull(bool filled)
        {
            Color previousColor = Handles.color;
            if (filled)
            {
                // Draw the hull
                var xSize = new Vector3(size.z, size.y, 1f);
                material.SetPass(0);
                Graphics.DrawMeshNow(k_MeshQuad, Handles.matrix * Matrix4x4.TRS(center + size.x * .5f * Vector3.left, Quaternion.FromToRotation(Vector3.forward, Vector3.left), xSize));
                Graphics.DrawMeshNow(k_MeshQuad, Handles.matrix * Matrix4x4.TRS(center + size.x * .5f * Vector3.right, Quaternion.FromToRotation(Vector3.forward, Vector3.right), xSize));

                var ySize = new Vector3(size.x, size.z, 1f);
                Graphics.DrawMeshNow(k_MeshQuad, Handles.matrix * Matrix4x4.TRS(center + size.y * .5f * Vector3.up, Quaternion.FromToRotation(Vector3.forward, Vector3.up), ySize));
                Graphics.DrawMeshNow(k_MeshQuad, Handles.matrix * Matrix4x4.TRS(center + size.y * .5f * Vector3.down, Quaternion.FromToRotation(Vector3.forward, Vector3.down), ySize));

                var zSize = new Vector3(size.x, size.y, 1f);
                Graphics.DrawMeshNow(k_MeshQuad, Handles.matrix * Matrix4x4.TRS(center + size.z * .5f * Vector3.forward, Quaternion.identity, zSize));
                Graphics.DrawMeshNow(k_MeshQuad, Handles.matrix * Matrix4x4.TRS(center + size.z * .5f * Vector3.back, Quaternion.FromToRotation(Vector3.forward, Vector3.back), zSize));

                //if as a parent, also draw handle distance to the parent
                if (m_Parent != null)
                {
                    var centerDiff = center - m_Parent.center;
                    var xRecal = centerDiff;
                    var yRecal = centerDiff;
                    var zRecal = centerDiff;
                    xRecal.x = 0;
                    yRecal.y = 0;
                    zRecal.z = 0;

                    Handles.color = GetHandleColor(NamedFace.Left);
                    Handles.DrawLine(m_Parent.center + xRecal + m_Parent.size.x * .5f * Vector3.left, center + size.x * .5f * Vector3.left);

                    Handles.color = GetHandleColor(NamedFace.Right);
                    Handles.DrawLine(m_Parent.center + xRecal + m_Parent.size.x * .5f * Vector3.right, center + size.x * .5f * Vector3.right);

                    Handles.color = GetHandleColor(NamedFace.Top);
                    Handles.DrawLine(m_Parent.center + yRecal + m_Parent.size.y * .5f * Vector3.up, center + size.y * .5f * Vector3.up);

                    Handles.color = GetHandleColor(NamedFace.Bottom);
                    Handles.DrawLine(m_Parent.center + yRecal + m_Parent.size.y * .5f * Vector3.down, center + size.y * .5f * Vector3.down);

                    Handles.color = GetHandleColor(NamedFace.Front);
                    Handles.DrawLine(m_Parent.center + zRecal + m_Parent.size.z * .5f * Vector3.forward, center + size.z * .5f * Vector3.forward);

                    Handles.color = GetHandleColor(NamedFace.Back);
                    Handles.DrawLine(m_Parent.center + zRecal + m_Parent.size.z * .5f * Vector3.back, center + size.z * .5f * Vector3.back);
                }
            }

            Handles.color = m_WireframeColor;
            Handles.zTest = UnityEngine.Rendering.CompareFunction.LessEqual;
            Handles.DrawWireCube(center, size);
            Handles.color = m_WireframeColorBehind;
            Handles.zTest = UnityEngine.Rendering.CompareFunction.Greater;
            Handles.DrawWireCube(center, size);
            Handles.zTest = UnityEngine.Rendering.CompareFunction.Always;
            Handles.color = previousColor;
        }

        /// <summary>Draw the manipulable handles</summary>
        public void DrawHandle()
        {
            Event evt = Event.current;
            bool useHomothety = evt.shift;
            bool useSymetry = evt.alt || evt.command;
            // Note: snapping is handled natively on ctrl for each Slider1D

            for (int i = 0, count = m_ControlIDs.Length; i < count; ++i)
                m_ControlIDs[i] = GUIUtility.GetControlID("HierarchicalBox".GetHashCode() + i, FocusType.Passive);

            EditorGUI.BeginChangeCheck();

            var leftPosition = center + size.x * .5f * Vector3.left;
            var rightPosition = center + size.x * .5f * Vector3.right;
            var topPosition = center + size.y * .5f * Vector3.up;
            var bottomPosition = center + size.y * .5f * Vector3.down;
            var frontPosition = center + size.z * .5f * Vector3.forward;
            var backPosition = center + size.z * .5f * Vector3.back;

            var theChangedFace = NamedFace.None;

            EditorGUI.BeginChangeCheck();
            Slider1D(m_ControlIDs[(int)NamedFace.Left], ref leftPosition, Vector3.left, EditorSnapSettings.scale, GetHandleColor(NamedFace.Left));
            if (EditorGUI.EndChangeCheck())
                theChangedFace = NamedFace.Left;

            EditorGUI.BeginChangeCheck();
            Slider1D(m_ControlIDs[(int)NamedFace.Right], ref rightPosition, Vector3.right, EditorSnapSettings.scale, GetHandleColor(NamedFace.Right));
            if (EditorGUI.EndChangeCheck())
                theChangedFace = NamedFace.Right;

            EditorGUI.BeginChangeCheck();
            Slider1D(m_ControlIDs[(int)NamedFace.Top], ref topPosition, Vector3.up, EditorSnapSettings.scale, GetHandleColor(NamedFace.Top));
            if (EditorGUI.EndChangeCheck())
                theChangedFace = NamedFace.Top;

            EditorGUI.BeginChangeCheck();
            Slider1D(m_ControlIDs[(int)NamedFace.Bottom], ref bottomPosition, Vector3.down, EditorSnapSettings.scale, GetHandleColor(NamedFace.Bottom));
            if (EditorGUI.EndChangeCheck())
                theChangedFace = NamedFace.Bottom;

            EditorGUI.BeginChangeCheck();
            Slider1D(m_ControlIDs[(int)NamedFace.Front], ref frontPosition, Vector3.forward, EditorSnapSettings.scale, GetHandleColor(NamedFace.Front));
            if (EditorGUI.EndChangeCheck())
                theChangedFace = NamedFace.Front;

            EditorGUI.BeginChangeCheck();
            Slider1D(m_ControlIDs[(int)NamedFace.Back], ref backPosition, Vector3.back, EditorSnapSettings.scale, GetHandleColor(NamedFace.Back));
            if (EditorGUI.EndChangeCheck())
                theChangedFace = NamedFace.Back;

            if (EditorGUI.EndChangeCheck())
            {
                float delta = 0f;
                switch (theChangedFace)
                {
                    case NamedFace.Left: delta = (leftPosition - center - size.x * .5f * Vector3.left).x; break;
                    case NamedFace.Right: delta = -(rightPosition - center - size.x * .5f * Vector3.right).x; break;
                    case NamedFace.Top: delta = -(topPosition - center - size.y * .5f * Vector3.up).y; break;
                    case NamedFace.Bottom: delta = (bottomPosition - center - size.y * .5f * Vector3.down).y; break;
                    case NamedFace.Front: delta = -(frontPosition - center - size.z * .5f * Vector3.forward).z; break;
                    case NamedFace.Back: delta = (backPosition - center - size.z * .5f * Vector3.back).z; break;
                }

                if (monoHandle || useHomothety && useSymetry)
                {
                    var tempSize = size - Vector3.one * delta;

                    //ensure that the box face are still facing outside
                    for (int axis = 0; axis < 3; ++axis)
                    {
                        if (tempSize[axis] < 0)
                        {
                            delta += tempSize[axis];
                            tempSize = size - Vector3.one * delta;
                        }
                    }

                    //ensure containedBox do not exit container
                    if (m_Parent != null)
                    {
                        for (int axis = 0; axis < 3; ++axis)
                        {
                            if (tempSize[axis] > m_Parent.size[axis])
                                tempSize[axis] = m_Parent.size[axis];
                        }
                    }

                    size = tempSize;
                }
                else
                {
                    if (useSymetry)
                    {
                        switch (theChangedFace)
                        {
                            case NamedFace.Left: rightPosition.x -= delta; break;
                            case NamedFace.Right: leftPosition.x += delta; break;
                            case NamedFace.Top: bottomPosition.y += delta; break;
                            case NamedFace.Bottom: topPosition.y -= delta; break;
                            case NamedFace.Front: backPosition.z += delta; break;
                            case NamedFace.Back: frontPosition.z -= delta; break;
                        }
                    }

                    if (useHomothety)
                    {
                        float halfDelta = delta * 0.5f;
                        switch (theChangedFace)
                        {
                            case NamedFace.Left:
                            case NamedFace.Right:
                                bottomPosition.y += halfDelta;
                                topPosition.y -= halfDelta;
                                backPosition.z += halfDelta;
                                frontPosition.z -= halfDelta;
                                break;
                            case NamedFace.Top:
                            case NamedFace.Bottom:
                                rightPosition.x -= halfDelta;
                                leftPosition.x += halfDelta;
                                backPosition.z += halfDelta;
                                frontPosition.z -= halfDelta;
                                break;
                            case NamedFace.Front:
                            case NamedFace.Back:
                                rightPosition.x -= halfDelta;
                                leftPosition.x += halfDelta;
                                bottomPosition.y += halfDelta;
                                topPosition.y -= halfDelta;
                                break;
                        }
                    }

                    var max = new Vector3(rightPosition.x, topPosition.y, frontPosition.z);
                    var min = new Vector3(leftPosition.x, bottomPosition.y, backPosition.z);

                    //ensure that the box face are still facing outside
                    for (int axis = 0; axis < 3; ++axis)
                    {
                        if (min[axis] > max[axis])
                        {
                            // Control IDs in m_ControlIDs[0-3[ are for positive axes
                            if (GUIUtility.hotControl == m_ControlIDs[axis])
                                max[axis] = min[axis];
                            else
                                min[axis] = max[axis];
                        }
                    }

                    //ensure containedBox do not exit container
                    if (m_Parent != null)
                    {
                        for (int axis = 0; axis < 3; ++axis)
                        {
                            if (min[axis] < m_Parent.center[axis] - m_Parent.size[axis] * 0.5f)
                                min[axis] = m_Parent.center[axis] - m_Parent.size[axis] * 0.5f;
                            if (max[axis] > m_Parent.center[axis] + m_Parent.size[axis] * 0.5f)
                                max[axis] = m_Parent.center[axis] + m_Parent.size[axis] * 0.5f;
                        }
                    }

                    center = (max + min) * .5f;
                    size = max - min;
                }
            }
        }

        Color GetHandleColor(NamedFace name) => monoHandle ? m_MonochromeHandleColor : m_PolychromeHandleColor[(int)name];
    }
}
