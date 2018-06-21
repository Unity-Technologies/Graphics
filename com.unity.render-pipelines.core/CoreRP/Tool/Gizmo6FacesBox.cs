using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Reflection;

namespace UnityEngine.Experimental.Gizmo
{
    public class Gizmo6FacesBox
    {
        protected enum NamedFace { Right, Top, Front, Left, Bottom, Back }
        protected enum Element { Face, SelectedFace, Handle }

        Mesh m_face = null;

        Mesh face
        {
            get
            {
                if (m_face == null)
                {
                    m_face = new Mesh();
                    m_face.vertices = new Vector3[] {
                        new Vector3(-.5f,-.5f,0f),
                        new Vector3(+.5f,-.5f,0f),
                        new Vector3(+.5f,+.5f,0f),
                        new Vector3(-.5f,+.5f,0f)
                    };
                    m_face.triangles = new int[] {
                        0, 1, 2,
                        2, 3, 0
                    };
                    m_face.RecalculateNormals();
                }
                return m_face;
            }
        }

        Color[] m_faceColorsSelected;

        public Color[] faceColorsSelected
        {
            get
            {
                return m_faceColorsSelected ?? (m_faceColorsSelected = new Color[]
                {
                    new Color(1f, 0f, 0f, .15f),
                    new Color(0f, 1f, 0f, .15f),
                    new Color(0f, 0f, 1f, .15f),
                    new Color(1f, 0f, 0f, .15f),
                    new Color(0f, 1f, 0f, .15f),
                    new Color(0f, 0f, 1f, .15f)
                });
            }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException("FaceColor cannot be set to null.");
                }
                if (value.Length != 6)
                {
                    throw new ArgumentException("FaceColor must have 6 entries: X Y Z -X -Y -Z");
                }
                m_faceColorsSelected = value;
            }
        }


        Color[] m_faceColors;

        public Color[] faceColors
        {
            get
            {
                return m_faceColors ?? (m_faceColors = new Color[]
                {
                    new Color(1f, .9f, .58f, .17f),
                    new Color(1f, .9f, .58f, .17f),
                    new Color(1f, .9f, .58f, .17f),
                    new Color(1f, .9f, .58f, .17f),
                    new Color(1f, .9f, .58f, .17f),
                    new Color(1f, .9f, .58f, .17f)
                });
            }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException("FaceColor cannot be set to null.");
                }
                if (value.Length != 6)
                {
                    throw new ArgumentException("FaceColor must have 6 entries: X Y Z -X -Y -Z");
                }
                m_faceColors = value;
            }
        }


        Color[] m_handleColors;

        public Color[] handleColors
        {
            get
            {
                return m_handleColors ?? (m_handleColors = new Color[]
                {
                    new Color(1f, 0f, 0f, 1f),
                    new Color(0f, 1f, 0f, 1f),
                    new Color(0f, 0f, 1f, 1f),
                    new Color(1f, 0f, 0f, 1f),
                    new Color(0f, 1f, 0f, 1f),
                    new Color(0f, 0f, 1f, 1f)
                });
            }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException("HandleColor cannot be set to null.");
                }
                if (value.Length != 6)
                {
                    throw new ArgumentException("HandleColor must have 6 entries: X Y Z -X -Y -Z");
                }
                m_handleColors = value;
            }
        }

        public int[] m_ControlIDs = new int[6] { 0, 0, 0, 0, 0, 0 };

        public Vector3 center { get; set; }

        public Vector3 size { get; set; }

        public Gizmo6FacesBox()
        { }

        protected Color GetColor(NamedFace name, Element element)
        {
            return (element == Element.Face ? faceColors : element == Element.Handle ? handleColors : faceColorsSelected)[(int)name];
        }

        public virtual void DrawHull(bool selected)
        {
            Color colorGizmo = Gizmos.color;

            Element element = selected ? Element.SelectedFace : Element.Face;

            Vector3 xSize = new Vector3(size.z, size.y, 1f);
            Gizmos.color = GetColor(NamedFace.Left, element);
            Gizmos.DrawMesh(face, center + size.x * .5f * Vector3.left, Quaternion.FromToRotation(Vector3.forward, Vector3.left), xSize);
            Gizmos.color = GetColor(NamedFace.Right, element);
            Gizmos.DrawMesh(face, center + size.x * .5f * Vector3.right, Quaternion.FromToRotation(Vector3.forward, Vector3.right), xSize);

            Vector3 ySize = new Vector3(size.x, size.z, 1f);
            Gizmos.color = GetColor(NamedFace.Top, element);
            Gizmos.DrawMesh(face, center + size.y * .5f * Vector3.up, Quaternion.FromToRotation(Vector3.forward, Vector3.up), ySize);
            Gizmos.color = GetColor(NamedFace.Bottom, element);
            Gizmos.DrawMesh(face, center + size.y * .5f * Vector3.down, Quaternion.FromToRotation(Vector3.forward, Vector3.down), ySize);

            Vector3 zSize = new Vector3(size.x, size.y, 1f);
            Gizmos.color = GetColor(NamedFace.Front, element);
            Gizmos.DrawMesh(face, center + size.z * .5f * Vector3.forward, Quaternion.identity, zSize);
            Gizmos.color = GetColor(NamedFace.Back, element);
            Gizmos.DrawMesh(face, center + size.z * .5f * Vector3.back, Quaternion.FromToRotation(Vector3.forward, Vector3.back), zSize);

            Gizmos.color = colorGizmo;
        }

        static Type k_SnapSettings = Type.GetType("UnityEditor.SnapSettings, UnityEditor");
        //static Type k_Slider1D = Type.GetType("UnityEditorInternal.Slider1D");
        //static MethodInfo k_Slider1D_Do = k_Slider1D
        //    .GetMethod(
        //        "Do",
        //        BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public,
        //        null,
        //        CallingConventions.Any,
        //        new[] { typeof(int), typeof(Vector3), typeof(Vector3), typeof(float), typeof(Handles.CapFunction), typeof(float) },
        //        null);

        public void DrawHandle()
        {
            //Type k_Slider1D = Type.GetType("UnityEditorInternal.Slider1D, UnityEditor");
            //MethodInfo k_Slider1D_Do = k_Slider1D
            //    .GetMethod(
            //        "Do",
            //        BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public,
            //        null,
            //        CallingConventions.Any,
            //        new[] { typeof(int), typeof(Vector3), typeof(Vector3), typeof(float), typeof(Handles.CapFunction), typeof(float) },
            //        null);

            float handleSize = HandleUtility.GetHandleSize(center) * 0.05f;

            for (int i = 0, count = m_ControlIDs.Length; i < count; ++i)
                m_ControlIDs[i] = GUIUtility.GetControlID(GetHashCode(), FocusType.Passive);

            //Draw Handles
            EditorGUI.BeginChangeCheck();

            Vector3 leftPosition = center + size.x * .5f * Vector3.left;
            Vector3 rightPosition = center + size.x * .5f * Vector3.right;
            Vector3 topPosition = center + size.y * .5f * Vector3.up;
            Vector3 bottomPosition = center + size.y * .5f * Vector3.down;
            Vector3 frontPosition = center + size.z * .5f * Vector3.forward;
            Vector3 backPosition = center + size.z * .5f * Vector3.back;


            float snapScale = (float)k_SnapSettings.GetProperty("scale").GetValue(null, null);
            using (new Handles.DrawingScope(GetColor(NamedFace.Left, Element.Handle)))
                //k_Slider1D_Do.Invoke(null, new[]
                //{
                //    m_ControlIDs[(int)NamedFace.Left],
                //    leftPosition,
                //    Vector3.left,
                //    1f,
                //    (Delegate)Handles.DotHandleCap,
                //    k_SnapSettings.GetProperty("scale").GetValue(null, null)
                //});
                leftPosition = UnityEditorInternal.Slider1D.Do(
                    m_ControlIDs[(int)NamedFace.Left],
                    leftPosition,
                    Vector3.left,
                    handleSize,
                    Handles.DotHandleCap,
                    snapScale
                    );
            using (new Handles.DrawingScope(GetColor(NamedFace.Right, Element.Handle)))
                rightPosition = UnityEditorInternal.Slider1D.Do(
                    m_ControlIDs[(int)NamedFace.Right],
                    rightPosition,
                    Vector3.right,
                    handleSize,
                    Handles.DotHandleCap,
                    snapScale
                    );

            using (new Handles.DrawingScope(GetColor(NamedFace.Top, Element.Handle)))
                topPosition = UnityEditorInternal.Slider1D.Do(
                    m_ControlIDs[(int)NamedFace.Top],
                    topPosition,
                    Vector3.up,
                    handleSize,
                    Handles.DotHandleCap,
                    snapScale
                    );
            using (new Handles.DrawingScope(GetColor(NamedFace.Bottom, Element.Handle)))
                bottomPosition = UnityEditorInternal.Slider1D.Do(
                    m_ControlIDs[(int)NamedFace.Bottom],
                    bottomPosition,
                    Vector3.down,
                    handleSize,
                    Handles.DotHandleCap,
                    snapScale
                    );

            using (new Handles.DrawingScope(GetColor(NamedFace.Front, Element.Handle)))
                frontPosition = UnityEditorInternal.Slider1D.Do(
                    m_ControlIDs[(int)NamedFace.Front],
                    frontPosition,
                    Vector3.forward,
                    handleSize,
                    Handles.DotHandleCap,
                    snapScale
                    );
            using (new Handles.DrawingScope(GetColor(NamedFace.Back, Element.Handle)))
                backPosition = UnityEditorInternal.Slider1D.Do(
                    m_ControlIDs[(int)NamedFace.Back],
                    backPosition,
                    Vector3.back,
                    handleSize,
                    Handles.DotHandleCap,
                    snapScale
                    );
            

            if (EditorGUI.EndChangeCheck())
            {
                Vector3 max = new Vector3(rightPosition.x, topPosition.y, frontPosition.z);
                Vector3 min = new Vector3(leftPosition.x, bottomPosition.y, backPosition.z);

                //ensure that the box face are still facing outside
                for (int axis = 0; axis < 3; ++axis)
                {
                    if (min[axis] > max[axis])
                    {
                        if (GUIUtility.hotControl == m_ControlIDs[axis])
                        {
                            max[axis] = min[axis];
                        }
                        else
                        {
                            min[axis] = max[axis];
                        }
                    }
                }

                center = (max + min) * .5f;
                size = max - min;
            }
        }
    }

    public class Gizmo6FacesBoxContained : Gizmo6FacesBox
    {
        private Gizmo6FacesBox m_container;

        public Gizmo6FacesBox container
        {
            get
            {
                return m_container;
            }
            set
            {
                if (value == null)
                    throw new System.ArgumentNullException("Container cannot be null. Use Gizmo6FacesBox instead.");
                m_container = value;
            }
        }

        public Gizmo6FacesBoxContained(Gizmo6FacesBox container)
        {
            m_container = container;
        }

        public override void DrawHull(bool selected)
        {
            Color colorGizmo = Gizmos.color;
            base.DrawHull(selected);

            //if selected, also draw handle displacement here
            if (selected)
            {
                Vector3 centerDiff = center - m_container.center;
                Vector3 xRecal = centerDiff;
                Vector3 yRecal = centerDiff;
                Vector3 zRecal = centerDiff;
                xRecal.x = 0;
                yRecal.y = 0;
                zRecal.z = 0;

                Gizmos.color = GetColor(NamedFace.Left, Element.Handle);
                Gizmos.DrawLine(m_container.center + xRecal + m_container.size.x * .5f * Vector3.left, center + size.x * .5f * Vector3.left);

                Gizmos.color = GetColor(NamedFace.Right, Element.Handle);
                Gizmos.DrawLine(m_container.center + xRecal + m_container.size.x * .5f * Vector3.right, center + size.x * .5f * Vector3.right);

                Gizmos.color = GetColor(NamedFace.Top, Element.Handle);
                Gizmos.DrawLine(m_container.center + yRecal + m_container.size.y * .5f * Vector3.up, center + size.y * .5f * Vector3.up);

                Gizmos.color = GetColor(NamedFace.Bottom, Element.Handle);
                Gizmos.DrawLine(m_container.center + yRecal + m_container.size.y * .5f * Vector3.down, center + size.y * .5f * Vector3.down);

                Gizmos.color = GetColor(NamedFace.Front, Element.Handle);
                Gizmos.DrawLine(m_container.center + zRecal + m_container.size.z * .5f * Vector3.forward, center + size.z * .5f * Vector3.forward);

                Gizmos.color = GetColor(NamedFace.Back, Element.Handle);
                Gizmos.DrawLine(m_container.center + zRecal + m_container.size.z * .5f * Vector3.back, center + size.z * .5f * Vector3.back);

            }

            Debug.Log(m_container.size);

            Gizmos.color = colorGizmo;
        }
    }
}
