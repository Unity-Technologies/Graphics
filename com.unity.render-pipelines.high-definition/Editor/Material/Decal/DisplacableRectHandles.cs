using System;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace UnityEditor.Rendering.HighDefinition
{
    class DisplacableRectHandles
    {
        const float k_HandleSizeCoef = 0.05f;

        enum NamedEdge { Right, Top, Left, Bottom, None }

        int[] m_ControlIDs = new int[4] { 0, 0, 0, 0 };
        Color m_MonochromeHandleColor;
        Color m_WireframeColor;
        Color m_WireframeColorBehind;

        /// <summary>The position of the center of the box in Handle.matrix space. On plane z=0.</summary>
        public Vector2 center { get; set; }

        /// <summary>The size of the box in Handle.matrix space. On plane z=0.</summary>
        public Vector2 size { get; set; }

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
        static void Slider1D(int controlID, ref Vector3 handlePosition, Vector3 handleOrientation, float snapScale)
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

        /// <summary>The baseColor used to draw the rect.</summary>
        public Color baseColor
        {
            get { return m_MonochromeHandleColor; }
            set
            {
                m_MonochromeHandleColor = GizmoUtility.GetHandleColor(value);
                m_WireframeColor = GizmoUtility.GetWireframeColor(value);
                m_WireframeColorBehind = GizmoUtility.GetWireframeColorBehindObjects(value);
            }
        }

        public DisplacableRectHandles(Color baseColor)
        {
            this.baseColor = baseColor;
        }

        /// <summary>Draw the rect.</summary>
        public void DrawRect(bool dottedLine = false, float thickness = .0f, float screenSpaceSize = 5f)
        {
            Vector2 start = center - size * .5f;
            Vector3[] positions = new Vector3[]
            {
                start,
                start + size * Vector2.right,
                start + size,
                start + size * Vector2.up
            };
            Vector3[] edges = new Vector3[]
            {
                positions[0], positions[1],
                positions[1], positions[2],
                positions[2], positions[3],
                positions[3], positions[0],
            };

            void Draw()
            {
                if (dottedLine)
                    Handles.DrawDottedLines(edges, screenSpaceSize);
                else
                {
                    Handles.DrawLine(positions[0], positions[1], thickness);
                    Handles.DrawLine(positions[1], positions[2], thickness);
                    Handles.DrawLine(positions[2], positions[3], thickness);
                    Handles.DrawLine(positions[3], positions[0], thickness);
                }
            }

            Color previousColor = Handles.color;
            Handles.color = m_WireframeColor;
            Handles.zTest = UnityEngine.Rendering.CompareFunction.LessEqual;
            Draw();
            Handles.color = m_WireframeColorBehind;
            Handles.zTest = UnityEngine.Rendering.CompareFunction.Greater;
            Draw();
            Handles.zTest = UnityEngine.Rendering.CompareFunction.Always;
            Handles.color = previousColor;
        }

        NamedEdge DrawSliders(ref Vector3 leftPosition, ref Vector3 rightPosition, ref Vector3 topPosition, ref Vector3 bottomPosition)
        {
            NamedEdge theChangedEdge = NamedEdge.None;

            using (new Handles.DrawingScope(m_MonochromeHandleColor))
            {
                EditorGUI.BeginChangeCheck();
                Slider1D(m_ControlIDs[(int)NamedEdge.Left], ref leftPosition, Vector3.left, EditorSnapSettings.scale);
                if (EditorGUI.EndChangeCheck())
                    theChangedEdge = NamedEdge.Left;

                EditorGUI.BeginChangeCheck();
                Slider1D(m_ControlIDs[(int)NamedEdge.Right], ref rightPosition, Vector3.right, EditorSnapSettings.scale);
                if (EditorGUI.EndChangeCheck())
                    theChangedEdge = NamedEdge.Right;

                EditorGUI.BeginChangeCheck();
                Slider1D(m_ControlIDs[(int)NamedEdge.Top], ref topPosition, Vector3.up, EditorSnapSettings.scale);
                if (EditorGUI.EndChangeCheck())
                    theChangedEdge = NamedEdge.Top;

                EditorGUI.BeginChangeCheck();
                Slider1D(m_ControlIDs[(int)NamedEdge.Bottom], ref bottomPosition, Vector3.down, EditorSnapSettings.scale);
                if (EditorGUI.EndChangeCheck())
                    theChangedEdge = NamedEdge.Bottom;
            }

            return theChangedEdge;
        }

        void EnsureEdgeFacesOutsideForHomothety(NamedEdge theChangedEdge, ref Vector3 leftPosition, ref Vector3 rightPosition, ref Vector3 topPosition, ref Vector3 bottomPosition)
        {
            switch (theChangedEdge)
            {
                case NamedEdge.Left:
                    if (rightPosition.x < leftPosition.x)
                        leftPosition.x = rightPosition.x;
                    if (topPosition.y < bottomPosition.y)
                        topPosition.y = bottomPosition.y = center.y;
                    break;
                case NamedEdge.Right:
                    if (rightPosition.x < leftPosition.x)
                        rightPosition.x = leftPosition.x;
                    if (topPosition.y < bottomPosition.y)
                        topPosition.y = bottomPosition.y = center.y;
                    break;
                case NamedEdge.Top:
                    if (topPosition.y < bottomPosition.y)
                        topPosition.y = bottomPosition.y;
                    if (rightPosition.x < leftPosition.x)
                        rightPosition.x = leftPosition.x = center.x;
                    break;
                case NamedEdge.Bottom:
                    if (topPosition.y < bottomPosition.y)
                        bottomPosition.y = topPosition.y;
                    if (rightPosition.x < leftPosition.x)
                        rightPosition.x = leftPosition.x = center.x;
                    break;
            }
        }

        void EnsureEdgeFacesOutsideForSymetry(NamedEdge theChangedEdge, ref Vector3 leftPosition, ref Vector3 rightPosition, ref Vector3 topPosition, ref Vector3 bottomPosition)
        {
            switch (theChangedEdge)
            {
                case NamedEdge.Left:
                case NamedEdge.Right:
                    if (rightPosition.x < leftPosition.x)
                        rightPosition.x = leftPosition.x = center.x;
                    break;
                case NamedEdge.Top:
                case NamedEdge.Bottom:
                    if (topPosition.y < bottomPosition.y)
                        topPosition.y = bottomPosition.y = center.y;
                    break;
            }
        }

        void EnsureEdgeFacesOutsideForOtherTransformation(ref Vector2 max, ref Vector2 min)
        {
            for (int axis = 0; axis < 2; ++axis)
            {
                if (min[axis] > max[axis])
                {
                    // Control IDs in m_ControlIDs[0-1] are for positive axes
                    if (GUIUtility.hotControl == m_ControlIDs[axis])
                        max[axis] = min[axis];
                    else
                        min[axis] = max[axis];
                }
            }
        }

        /// <summary>Draw the manipulable handles</summary>
        public void DrawHandle()
        {
            Event evt = Event.current;
            bool useHomothety = evt.shift;
            bool useSymetry = evt.alt || evt.command;
            // Note: snapping is handled natively on ctrl for each Slider1D

            for (int i = 0, count = m_ControlIDs.Length; i < count; ++i)
                m_ControlIDs[i] = GUIUtility.GetControlID("DisplacableRectHandles".GetHashCode() + i, FocusType.Passive);

            Vector3 leftPosition = center + size.x * .5f * Vector2.left;
            Vector3 rightPosition = center + size.x * .5f * Vector2.right;
            Vector3 topPosition = center + size.y * .5f * Vector2.up;
            Vector3 bottomPosition = center + size.y * .5f * Vector2.down;

            var theChangedEdge = NamedEdge.None;

            EditorGUI.BeginChangeCheck();
            theChangedEdge = DrawSliders(ref leftPosition, ref rightPosition, ref topPosition, ref bottomPosition);
            if (EditorGUI.EndChangeCheck())
            {
                float delta = 0f;
                switch (theChangedEdge)
                {
                    case NamedEdge.Left: delta = ((Vector2)leftPosition - center - size.x * .5f * Vector2.left).x; break;
                    case NamedEdge.Right: delta = -((Vector2)rightPosition - center - size.x * .5f * Vector2.right).x; break;
                    case NamedEdge.Top: delta = -((Vector2)topPosition - center - size.y * .5f * Vector2.up).y; break;
                    case NamedEdge.Bottom: delta = ((Vector2)bottomPosition - center - size.y * .5f * Vector2.down).y; break;
                }

                if (useHomothety && useSymetry)
                {
                    var tempSize = size - Vector2.one * delta;

                    //ensure that the rect edges are still facing outside
                    for (int axis = 0; axis < 3; ++axis)
                    {
                        if (tempSize[axis] < 0)
                        {
                            delta += tempSize[axis];
                            tempSize = size - Vector2.one * delta;
                        }
                    }

                    size = tempSize;
                }
                else
                {
                    if (useSymetry)
                    {
                        switch (theChangedEdge)
                        {
                            case NamedEdge.Left: rightPosition.x -= delta; break;
                            case NamedEdge.Right: leftPosition.x += delta; break;
                            case NamedEdge.Top: bottomPosition.y += delta; break;
                            case NamedEdge.Bottom: topPosition.y -= delta; break;
                        }

                        EnsureEdgeFacesOutsideForSymetry(theChangedEdge, ref leftPosition, ref rightPosition, ref topPosition, ref bottomPosition);
                    }

                    if (useHomothety)
                    {
                        float halfDelta = delta * 0.5f;
                        switch (theChangedEdge)
                        {
                            case NamedEdge.Left:
                            case NamedEdge.Right:
                                bottomPosition.y += halfDelta;
                                topPosition.y -= halfDelta;
                                break;
                            case NamedEdge.Top:
                            case NamedEdge.Bottom:
                                rightPosition.x -= halfDelta;
                                leftPosition.x += halfDelta;
                                break;
                        }

                        EnsureEdgeFacesOutsideForHomothety(theChangedEdge, ref leftPosition, ref rightPosition, ref topPosition, ref bottomPosition);
                    }

                    var max = new Vector2(rightPosition.x, topPosition.y);
                    var min = new Vector2(leftPosition.x, bottomPosition.y);

                    if (!useSymetry && !useHomothety)
                        EnsureEdgeFacesOutsideForOtherTransformation(ref max, ref min);

                    center = (max + min) * .5f;
                    size = max - min;
                }
            }
        }
    }
}
