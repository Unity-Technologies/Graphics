using System;
using System.Linq.Expressions;
using System.Reflection;
using UnityEditor;

namespace UnityEngine.Rendering.Universal
{
    // Copy of UnityEditor.Rendering.HighDefinition.DisplacableRectHandles
    class ProjectedTransform
    {
        struct PositionHandleIds
        {
            static int s_xAxisMoveHandleHash = "xAxisDecalPivot".GetHashCode();
            static int s_yAxisMoveHandleHash = "yAxisDecalPivot".GetHashCode();
            static int s_zAxisMoveHandleHash = "zAxisDecalPivot".GetHashCode();
            static int s_xyAxisMoveHandleHash = "xyAxisDecalPivot".GetHashCode();

            public static PositionHandleIds @default
            {
                get
                {
                    return new PositionHandleIds(
                        GUIUtility.GetControlID(s_xAxisMoveHandleHash, FocusType.Passive),
                        GUIUtility.GetControlID(s_yAxisMoveHandleHash, FocusType.Passive),
                        GUIUtility.GetControlID(s_zAxisMoveHandleHash, FocusType.Passive),
                        GUIUtility.GetControlID(s_xyAxisMoveHandleHash, FocusType.Passive)
                    );
                }
            }

            public readonly int x, y, z, xy;

            public int this[int index]
            {
                get
                {
                    switch (index)
                    {
                        case 0: return x;
                        case 1: return y;
                        case 2: return z;
                        case 3: return xy;
                    }
                    return -1;
                }
            }

            public bool Has(int id)
            {
                return x == id
                    || y == id
                    || z == id
                    || xy == id;
            }

            public PositionHandleIds(int x, int y, int z, int xy)
            {
                this.x = x;
                this.y = y;
                this.z = z;
                this.xy = xy;
            }

            public override int GetHashCode()
            {
                return x ^ y ^ z ^ xy;
            }

            public override bool Equals(object obj)
            {
                if (!(obj is PositionHandleIds o))
                    return false;

                return o.x == x && o.y == y && o.z == z && o.xy == xy;
            }
        }

        struct PositionHandleParam
        {
            public static PositionHandleParam defaultHandleXY = new PositionHandleParam(
                Handle.X | Handle.Y | Handle.XY,
                Vector3.zero, Vector3.one, Vector3.zero, Vector3.one * .25f,
                Orientation.Signed, Orientation.Camera);

            public static PositionHandleParam defaultHandleZ = new PositionHandleParam(
                Handle.Z,
                Vector3.zero, Vector3.one, Vector3.zero, Vector3.one * .25f,
                Orientation.Signed, Orientation.Camera);

            [Flags]
            public enum Handle
            {
                None = 0,
                X = 1 << 0,
                Y = 1 << 1,
                Z = 1 << 2,
                XY = 1 << 3,
                All = ~None
            }

            public enum Orientation
            {
                Signed,
                Camera
            }

            public readonly Vector3 axisOffset;
            public readonly Vector3 axisSize;
            public readonly Vector3 planeOffset;
            public readonly Vector3 planeSize;
            public readonly Handle handles;
            public readonly Orientation axesOrientation;
            public readonly Orientation planeOrientation;

            public bool ShouldShow(int axis)
            {
                return (handles & (Handle)(1 << axis)) != 0;
            }

            public bool ShouldShow(Handle handle)
            {
                return (handles & handle) != 0;
            }

            public PositionHandleParam(
                Handle handles,
                Vector3 axisOffset,
                Vector3 axisSize,
                Vector3 planeOffset,
                Vector3 planeSize,
                Orientation axesOrientation,
                Orientation planeOrientation)
            {
                this.axisOffset = axisOffset;
                this.axisSize = axisSize;
                this.planeOffset = planeOffset;
                this.planeSize = planeSize;
                this.handles = handles;
                this.axesOrientation = axesOrientation;
                this.planeOrientation = planeOrientation;
            }
        }

        static PositionHandleParam paramXY = PositionHandleParam.defaultHandleXY;
        static PositionHandleParam paramZ = PositionHandleParam.defaultHandleZ;
        static PositionHandleIds ids = PositionHandleIds.@default;

        static int[] s_DoPositionHandle_Internal_NextIndex = { 1, 2, 0 };
        static int[] s_DoPositionHandle_Internal_PrevIndex = { 2, 0, 1 };
        static Vector3[] verts = { Vector3.zero, Vector3.zero, Vector3.zero, Vector3.zero };

        static Func<bool> s_IsGridSnappingActive;


        static ProjectedTransform()
        {
            //We need to know if grid snaping is active or not in Editor. Sadly this is internal so we must grab it by reflection.
            Type gridSnappingType = typeof(Handles).Assembly.GetType("UnityEditor.GridSnapping");
            PropertyInfo activePropertyInfo = gridSnappingType.GetProperty("active", BindingFlags.Public | BindingFlags.Static);
            MethodCallExpression activePropertyGetCall = Expression.Call(null, activePropertyInfo.GetGetMethod());
            var activeGetLambda = Expression.Lambda<Func<bool>>(activePropertyGetCall);
            s_IsGridSnappingActive = activeGetLambda.Compile();
        }

        static bool IsHovering(int controlID, Event evt)
        {
            return controlID == HandleUtility.nearestControl && GUIUtility.hotControl == 0 && !Tools.viewToolActive;
        }

        public static Vector3 DrawHandles(Vector3 position, float zProjectionDistance, Quaternion rotation)
        {
            var isHot = ids.Has(GUIUtility.hotControl);
            var planeSize = isHot ? paramXY.planeSize + paramXY.planeOffset : paramXY.planeSize;
            var planarSize = Mathf.Max(planeSize[0], planeSize[s_DoPositionHandle_Internal_NextIndex[0]]);
            Vector3 sliderRotatedWorldPos = Quaternion.Inverse(rotation) * position;
            var size1D = HandleUtility.GetHandleSize(sliderRotatedWorldPos);
            var size2D = HandleUtility.GetHandleSize(sliderRotatedWorldPos - new Vector3(0, 0, zProjectionDistance)) * planarSize * .5f;
            Vector3 depthSlider = sliderRotatedWorldPos;

            EditorGUI.BeginChangeCheck();
            {
                // dot offset = transform position seen as a sphere
                EditorGUI.BeginChangeCheck();
                depthSlider = Handles.Slider(depthSlider, Vector3.forward, size1D * .1f, Handles.SphereHandleCap, -1);
                if (EditorGUI.EndChangeCheck())
                    sliderRotatedWorldPos.z = depthSlider.z;

                // 2D slider: square xy-axis
                Vector3 sliderFaceProjected = sliderRotatedWorldPos - new Vector3(0, 0, zProjectionDistance);
                sliderFaceProjected.x += size2D;
                sliderFaceProjected.y += size2D;
                using (new Handles.DrawingScope(Handles.zAxisColor))
                {
                    verts[0] = sliderFaceProjected + (Vector3.right + Vector3.up) * size2D;
                    verts[1] = sliderFaceProjected + (-Vector3.right + Vector3.up) * size2D;
                    verts[2] = sliderFaceProjected + (-Vector3.right - Vector3.up) * size2D;
                    verts[3] = sliderFaceProjected + (Vector3.right - Vector3.up) * size2D;
                    int id = GUIUtility.GetControlID(ids.xy, FocusType.Passive);
                    float faceOpacity = 0.8f;
                    if (GUIUtility.hotControl == id)
                        Handles.color = Handles.selectedColor;
                    else if (IsHovering(id, Event.current))
                        faceOpacity = 0.4f;
                    else
                        faceOpacity = 0.1f;
                    Color faceColor = new Color(Handles.zAxisColor.r, Handles.zAxisColor.g, Handles.zAxisColor.b, Handles.zAxisColor.a * faceOpacity);
                    Handles.DrawSolidRectangleWithOutline(verts, faceColor, Color.clear);
                    EditorGUI.BeginChangeCheck();
                    sliderFaceProjected = Handles.Slider2D(id, sliderFaceProjected, Vector3.forward, Vector3.right, Vector3.up, size2D, Handles.RectangleHandleCap, s_IsGridSnappingActive() ? Vector2.zero : new Vector2(EditorSnapSettings.move[0], EditorSnapSettings.move[1]), false);
                    if (EditorGUI.EndChangeCheck())
                    {
                        sliderRotatedWorldPos.x = sliderFaceProjected.x;
                        sliderRotatedWorldPos.y = sliderFaceProjected.y;
                    }
                }
                sliderFaceProjected.x -= size2D;
                sliderFaceProjected.y -= size2D;

                // 2D slider: x-axis
                EditorGUI.BeginChangeCheck();
                using (new Handles.DrawingScope(Handles.xAxisColor))
                    sliderFaceProjected = Handles.Slider(sliderFaceProjected, Vector3.right);
                if (EditorGUI.EndChangeCheck())
                    sliderRotatedWorldPos.x = sliderFaceProjected.x;

                // 2D slider: y-axis
                EditorGUI.BeginChangeCheck();
                using (new Handles.DrawingScope(Handles.yAxisColor))
                    sliderFaceProjected = Handles.Slider(sliderFaceProjected, Vector3.up);
                if (EditorGUI.EndChangeCheck())
                    sliderRotatedWorldPos.y = sliderFaceProjected.y;

                // depth: z-axis
                EditorGUI.BeginChangeCheck();
                using (new Handles.DrawingScope(Handles.zAxisColor))
                    depthSlider = Handles.Slider(depthSlider, Vector3.forward);
                if (EditorGUI.EndChangeCheck())
                    sliderRotatedWorldPos.z = depthSlider.z;
            }
            if (EditorGUI.EndChangeCheck())
            {
                position = rotation * sliderRotatedWorldPos;
            }

            return position;
        }
    }
}
