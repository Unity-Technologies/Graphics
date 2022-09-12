using System;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using UnityEngine.Rendering;

namespace UnityEditor.Rendering
{
    /// <summary>Utility class for drawing light Editor gizmos</summary>
    public static class CoreLightEditorUtilities
    {
        [Flags]
        enum HandleDirections
        {
            Left = 1 << 0,
            Up = 1 << 1,
            Right = 1 << 2,
            Down = 1 << 3,
            All = Left | Up | Right | Down
        }

        static readonly Vector3[] directionalLightHandlesRayPositions =
        {
            new Vector3(1, 0, 0),
            new Vector3(-1, 0, 0),
            new Vector3(0, 1, 0),
            new Vector3(0, -1, 0),
            new Vector3(1, 1, 0).normalized,
            new Vector3(1, -1, 0).normalized,
            new Vector3(-1, 1, 0).normalized,
            new Vector3(-1, -1, 0).normalized
        };

        /// <summary>
        /// Draw a gizmo for a directional light.
        /// </summary>
        /// <param name="light">The light that is used for this gizmo.</param>
        public static void DrawDirectionalLightGizmo(Light light)
        {
            // Sets the color of the Gizmo.
            Color outerColor = GetLightAboveObjectWireframeColor(light.color);

            Vector3 lightPos = light.transform.position;
            float lightSize;
            using (new Handles.DrawingScope(Matrix4x4.identity))    //be sure no matrix affect the size computation
            {
                lightSize = HandleUtility.GetHandleSize(lightPos);
            }
            float radius = lightSize * 0.2f;

            using (new Handles.DrawingScope(outerColor))
            {
                Handles.DrawWireDisc(Vector3.zero, Vector3.forward, radius);
                foreach (Vector3 normalizedPos in directionalLightHandlesRayPositions)
                {
                    Vector3 pos = normalizedPos * radius;
                    Handles.DrawLine(pos, pos + new Vector3(0, 0, lightSize));
                }
            }
        }

        /// <summary>
        /// Draw a gizmo for a point light.
        /// </summary>
        /// <param name="light">The light that is used for this gizmo.</param>
        public static void DrawPointLightGizmo(Light light)
        {
            // Sets the color of the Gizmo.
            Color outerColor = GetLightAboveObjectWireframeColor(light.color);

            // Drawing the point light
            DrawPointLight(light, outerColor);

            // Draw the handles and labels
            DrawPointHandlesAndLabels(light);
        }

        static void DrawPointLight(Light light, Color outerColor)
        {
            float range = light.range;

            using (new Handles.DrawingScope(outerColor))
            {
                EditorGUI.BeginChangeCheck();
                //range = Handles.RadiusHandle(Quaternion.identity, light.transform.position, range, false, true);
                range = DoPointHandles(range);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(light, "Adjust Point Light");
                    light.range = range;
                }
            }
        }

        static void DrawPointHandlesAndLabels(Light light)
        {
            // Getting the first control on point handle
            var firstControl = GUIUtility.GetControlID(s_PointLightHandle.GetHashCode(), FocusType.Passive) - 6; // BoxBoundsHandle allocates 6 control IDs
            if (Event.current.type != EventType.Repaint)
                return;
            //            var firstControl = GUIUtility.GetControlID(k_RadiusHandleHash, FocusType.Passive) - 6;
            //            if (Event.current.type != EventType.Repaint)
            //                return;

            // Adding label /////////////////////////////////////
            Vector3 labelPosition = Vector3.zero;

            if (GUIUtility.hotControl != 0)
            {
                switch (GUIUtility.hotControl - firstControl)
                {
                    case 0:
                        labelPosition = Vector3.right * light.range;
                        break;
                    case 1:
                        labelPosition = Vector3.left * light.range;
                        break;
                    case 2:
                        labelPosition = Vector3.up * light.range;
                        break;
                    case 3:
                        labelPosition = Vector3.down * light.range;
                        break;
                    case 4:
                        labelPosition = Vector3.forward * light.range;
                        break;
                    case 5:
                        labelPosition = Vector3.back * light.range;
                        break;
                    default:
                        return;
                }

                string labelText = FormattableString.Invariant($"Range: {light.range:0.00}");
                DrawHandleLabel(labelPosition, labelText);
            }
        }

        /// <summary>
        /// Draw a gizmo for an area/rectangle light.
        /// </summary>
        /// <param name="light">The light that is used for this gizmo.</param>
        public static void DrawRectangleLightGizmo(Light light)
        {
            // Color to use for gizmo drawing
            Color outerColor = GetLightAboveObjectWireframeColor(light.color);

            // Drawing the gizmo
            DrawRectangleLight(light, outerColor);

            // Draw the handles and labels
            DrawRectangleHandlesAndLabels(light);
        }

        static void DrawRectangleLight(Light light, Color outerColor)
        {
            Vector2 size = light.areaSize;
            var range = light.range;
            var innerColor = GetLightBehindObjectWireframeColor(light.color);
            DrawZTestedLine(range, outerColor, innerColor);

            using (new Handles.DrawingScope(outerColor))
            {
                EditorGUI.BeginChangeCheck();
                size = DoRectHandles(size);

                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(light, "Adjust Area Rectangle Light");
                    light.areaSize = size;
                }
            }
        }

        static void DrawRectangleHandlesAndLabels(Light light)
        {
            // Getting the first control on radius handle
            var firstControl = GUIUtility.GetControlID(s_AreaLightHandle.GetHashCode(), FocusType.Passive) - 6; // BoxBoundsHandle allocates 6 control IDs
            if (Event.current.type != EventType.Repaint)
                return;

            // Adding label /////////////////////////////////////
            Vector3 labelPosition = Vector3.zero;

            if (GUIUtility.hotControl != 0)
            {
                switch (GUIUtility.hotControl - firstControl)
                {
                    case 0: // PositiveX
                        labelPosition = Vector3.right * (light.areaSize.x / 2);
                        break;
                    case 1: // NegativeX
                        labelPosition = Vector3.left * (light.areaSize.x / 2);
                        break;
                    case 2: // PositiveY
                        labelPosition = Vector3.up * (light.areaSize.y / 2);
                        break;
                    case 3: // NegativeY
                        labelPosition = Vector3.down * (light.areaSize.y / 2);
                        break;
                    default:
                        return;
                }
                string labelText = FormattableString.Invariant($"w:{light.areaSize.x:0.00} x h:{light.areaSize.y:0.00}");
                DrawHandleLabel(labelPosition, labelText);
            }
        }

        /// <summary>
        /// Draw a gizmo for a disc light.
        /// </summary>
        /// <param name="light">The light that is used for this gizmo.</param>
        public static void DrawDiscLightGizmo(Light light)
        {
            // Color to use for gizmo drawing.
            Color outerColor = GetLightAboveObjectWireframeColor(light.color);

            // Drawing before objects
            DrawDiscLight(light, outerColor);

            // Draw handles
            DrawDiscHandlesAndLabels(light);
        }

        static void DrawDiscLight(Light light, Color outerColor)
        {
            float radius = light.areaSize.x;
            var range = light.range;
            var innerColor = GetLightBehindObjectWireframeColor(light.color);
            DrawZTestedLine(range, outerColor, innerColor);

            using (new Handles.DrawingScope(outerColor))
            {
                EditorGUI.BeginChangeCheck();
                radius = DoDiscHandles(radius);

                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(light, "Adjust Area Disc Light");
                    light.areaSize = new Vector2(radius, light.areaSize.y);
                }
            }
        }

        static void DrawDiscHandlesAndLabels(Light light)
        {
            // Getting the first control on radius handle
            var firstControl = GUIUtility.GetControlID(s_DiscLightHandle.GetHashCode(), FocusType.Passive) - 6; // BoxBoundsHandle allocates 6 control IDs
            if (Event.current.type != EventType.Repaint)
                return;

            Vector3 labelPosition = Vector3.zero;

            if (GUIUtility.hotControl != 0)
            {
                switch (GUIUtility.hotControl - firstControl)
                {
                    case 0: // PositiveX
                        labelPosition = Vector3.right * light.areaSize.x;
                        break;
                    case 1: // NegativeX
                        labelPosition = Vector3.left * light.areaSize.x;
                        break;
                    case 2: // PositiveY
                        labelPosition = Vector3.up * light.areaSize.x;
                        break;
                    case 3: // NegativeY
                        labelPosition = Vector3.down * light.areaSize.x;
                        break;
                    default:
                        return;
                }
                string labelText = FormattableString.Invariant($"Radius: {light.areaSize.x:0.00}");
                DrawHandleLabel(labelPosition, labelText);
            }
        }

        static void DrawWithZTest(PrimitiveBoundsHandle primitiveHandle, float alpha = 0.2f)
        {
            primitiveHandle.center = Vector3.zero;

            primitiveHandle.handleColor = Color.clear;
            primitiveHandle.wireframeColor = Color.white;
            Handles.zTest = CompareFunction.LessEqual;
            primitiveHandle.DrawHandle();

            primitiveHandle.wireframeColor = new Color(1f, 1f, 1f, alpha);
            Handles.zTest = CompareFunction.Greater;
            primitiveHandle.DrawHandle();

            primitiveHandle.handleColor = Color.white;
            primitiveHandle.wireframeColor = Color.clear;
            Handles.zTest = CompareFunction.Always;
            primitiveHandle.DrawHandle();
        }

        static void DrawZTestedLine(float range, Color outerColor, Color innerColor)
        {
            using (new Handles.DrawingScope(outerColor))
            {
                Handles.zTest = CompareFunction.LessEqual;
                Handles.DrawLine(Vector3.zero, Vector3.forward * range);
            }
            using (new Handles.DrawingScope(innerColor))
            {
                Handles.zTest = CompareFunction.Greater;
                Handles.DrawLine(Vector3.zero, Vector3.forward * range);
            }
            Handles.zTest = CompareFunction.Always;
        }

        static void DrawHandleLabel(Vector3 handlePosition, string labelText, float offsetFromHandle = 0.3f)
        {
            Vector3 labelPosition = Vector3.zero;

            var style = new GUIStyle { normal = { background = Texture2D.whiteTexture } };
            GUI.color = new Color(0.82f, 0.82f, 0.82f, 1);

            labelPosition = handlePosition + HandleUtility.GetHandleSize(handlePosition) * offsetFromHandle * Handles.inverseMatrix.MultiplyVector(Vector3.up);
            Handles.Label(labelPosition, labelText, style);
        }

        static readonly BoxBoundsHandle s_AreaLightHandle =
            new BoxBoundsHandle { axes = PrimitiveBoundsHandle.Axes.X | PrimitiveBoundsHandle.Axes.Y };

        static Vector2 DoRectHandles(Vector2 size)
        {
            s_AreaLightHandle.center = Vector3.zero;
            s_AreaLightHandle.size = size;
            DrawWithZTest(s_AreaLightHandle);

            return s_AreaLightHandle.size;
        }

        static readonly SphereBoundsHandle s_DiscLightHandle =
            new SphereBoundsHandle { axes = PrimitiveBoundsHandle.Axes.X | PrimitiveBoundsHandle.Axes.Y };
        static float DoDiscHandles(float radius)
        {
            s_DiscLightHandle.center = Vector3.zero;
            s_DiscLightHandle.radius = radius;
            DrawWithZTest(s_DiscLightHandle);

            return s_DiscLightHandle.radius;
        }

        static readonly SphereBoundsHandle s_PointLightHandle =
            new SphereBoundsHandle { axes = PrimitiveBoundsHandle.Axes.All };

        static float DoPointHandles(float range)
        {
            s_PointLightHandle.radius = range;
            DrawWithZTest(s_PointLightHandle);

            return s_PointLightHandle.radius;
        }

        static bool drawInnerConeAngle = true;

        /// <summary>
        /// Draw a gizmo for a spot light.
        /// </summary>
        /// <param name="light">The light that is used for this gizmo.</param>
        public static void DrawSpotLightGizmo(Light light)
        {
            // Saving the default colors
            var defColor = Handles.color;
            var defZTest = Handles.zTest;

            // Default Color for outer cone will be Yellow if nothing has been provided.
            Color outerColor = GetLightAboveObjectWireframeColor(light.color);

            // The default z-test outer color will be 20% opacity of the outer color
            Color outerColorZTest = GetLightBehindObjectWireframeColor(outerColor);

            // Default Color for inner cone will be Yellow-ish if nothing has been provided.
            Color innerColor = GetLightInnerConeColor(light.color);

            // The default z-test outer color will be 20% opacity of the inner color
            Color innerColorZTest = GetLightBehindObjectWireframeColor(innerColor);

            // Drawing before objects
            Handles.zTest = CompareFunction.LessEqual;
            DrawSpotlightWireframe(light, outerColor, innerColor);

            // Drawing behind objects
            Handles.zTest = CompareFunction.Greater;
            DrawSpotlightWireframe(light, outerColorZTest, innerColorZTest);

            // Resets the compare function to always
            Handles.zTest = CompareFunction.Always;

            // Draw handles
            if (!Event.current.alt)
            {
                DrawHandlesAndLabels(light, outerColor);
            }

            // Resets the handle colors
            Handles.color = defColor;
            Handles.zTest = defZTest;
        }

        static void DrawHandlesAndLabels(Light light, Color color)
        {
            // Zero position vector3
            Vector3 zeroPos = Vector3.zero;

            // Variable for which direction to draw the handles
            HandleDirections DrawHandleDirections;

            // Draw the handles ///////////////////////////////
            Handles.color = color;

            // Draw Center Handle
            float range = light.range;
            var id = GUIUtility.GetControlID(FocusType.Passive);
            EditorGUI.BeginChangeCheck();
            range = SliderLineHandle(id, Vector3.zero, Vector3.forward, range, "Range: ");
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObjects(new[] { light }, "Undo range change.");
            }

            // Draw outer handles
            DrawHandleDirections = HandleDirections.Down | HandleDirections.Up;
            const string outerLabel = "Outer Angle: ";

            EditorGUI.BeginChangeCheck();
            float outerAngle = DrawConeHandles(zeroPos, light.spotAngle, range, DrawHandleDirections, outerLabel);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObjects(new[] { light }, "Undo outer angle change.");
            }

            // Draw inner handles
            float innerAngle = 0;
            const string innerLabel = "Inner Angle: ";
            if (light.innerSpotAngle > 0f && drawInnerConeAngle)
            {
                DrawHandleDirections = HandleDirections.Left | HandleDirections.Right;
                EditorGUI.BeginChangeCheck();
                innerAngle = DrawConeHandles(zeroPos, light.innerSpotAngle, range, DrawHandleDirections, innerLabel);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObjects(new[] { light }, "Undo inner angle change.");
                }
            }

            // Draw Near Plane Handle
            float nearPlaneRange = light.shadowNearPlane;
            if (light.shadows != LightShadows.None && light.lightmapBakeType != LightmapBakeType.Baked)
            {
                EditorGUI.BeginChangeCheck();
                nearPlaneRange = SliderLineHandle(GUIUtility.GetControlID(FocusType.Passive), Vector3.zero, Vector3.forward, nearPlaneRange, "Near Plane: ");
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObjects(new[] { light }, "Undo shadow near plane change.");
                    nearPlaneRange = Mathf.Clamp(nearPlaneRange, 0.1f, light.range);
                }
            }

            // If changes has been made we update the corresponding property
            if (GUI.changed)
            {
                light.spotAngle = outerAngle;
                light.innerSpotAngle = innerAngle;
                light.range = Math.Max(range, 0.01f);
                light.shadowNearPlane = Mathf.Clamp(nearPlaneRange, 0.1f, light.range);
            }
        }

        static Color GetLightInnerConeColor(Color wireframeColor)
        {
            Color color = wireframeColor;
            color.a = 0.4f;
            return RemapLightColor(CoreUtils.ConvertLinearToActiveColorSpace(color.linear));
        }

        static Color GetLightAboveObjectWireframeColor(Color wireframeColor)
        {
            Color color = wireframeColor;
            color.a = 1f;
            return RemapLightColor(CoreUtils.ConvertLinearToActiveColorSpace(color.linear));
        }

        static Color GetLightBehindObjectWireframeColor(Color wireframeColor)
        {
            Color color = wireframeColor;
            color.a = 0.2f;
            return RemapLightColor(CoreUtils.ConvertLinearToActiveColorSpace(color.linear));
        }

        static Color RemapLightColor(Color src)
        {
            Color color = src;
            float max = Mathf.Max(Mathf.Max(color.r, color.g), color.b);
            if (max > 0f)
            {
                float mult = 1f / max;
                color.r *= mult;
                color.g *= mult;
                color.b *= mult;
            }
            else
            {
                color = Color.white;
            }

            return color;
        }

        static void DrawSpotlightWireframe(Light spotlight, Color outerColor, Color innerColor)
        {
            // Variable for which direction to draw the handles
            HandleDirections DrawHandleDirections;

            float outerAngle = spotlight.spotAngle;
            float innerAngle = spotlight.innerSpotAngle;
            float range = spotlight.range;

            var outerDiscRadius = range * Mathf.Sin(outerAngle * Mathf.Deg2Rad * 0.5f);
            var outerDiscDistance = Mathf.Cos(Mathf.Deg2Rad * outerAngle * 0.5f) * range;
            var vectorLineUp = Vector3.Normalize(Vector3.forward * outerDiscDistance + Vector3.up * outerDiscRadius);
            var vectorLineLeft = Vector3.Normalize(Vector3.forward * outerDiscDistance + Vector3.left * outerDiscRadius);

            // Need to check if we need to draw inner angle
            // Need to disable this for now until we get all the inner angle baking working.
            if (innerAngle > 0f && drawInnerConeAngle)
            {
                DrawHandleDirections = HandleDirections.Up | HandleDirections.Down;
                var innerDiscRadius = range * Mathf.Sin(innerAngle * Mathf.Deg2Rad * 0.5f);
                var innerDiscDistance = Mathf.Cos(Mathf.Deg2Rad * innerAngle * 0.5f) * range;

                // Drawing the inner Cone and also z-testing it to draw another color if behind
                Handles.color = innerColor;
                DrawConeWireframe(innerDiscRadius, innerDiscDistance, DrawHandleDirections);
            }

            // Draw range line
            Handles.color = innerColor;
            var rangeCenter = Vector3.forward * range;
            Handles.DrawLine(Vector3.zero, rangeCenter);

            // Drawing the outer Cone and also z-testing it to draw another color if behind
            Handles.color = outerColor;

            DrawHandleDirections = HandleDirections.Left | HandleDirections.Right;
            DrawConeWireframe(outerDiscRadius, outerDiscDistance, DrawHandleDirections);

            // Bottom arcs, making a nice rounded shape
            Handles.DrawWireArc(Vector3.zero, Vector3.right, vectorLineUp, outerAngle, range);
            Handles.DrawWireArc(Vector3.zero, Vector3.up, vectorLineLeft, outerAngle, range);

            // If we are using shadows we draw the near plane for shadows
            if (spotlight.shadows != LightShadows.None && spotlight.lightmapBakeType != LightmapBakeType.Baked)
            {
                DrawShadowNearPlane(spotlight, innerColor);
            }
        }

        static void DrawShadowNearPlane(Light spotlight, Color color)
        {
            Color previousColor = Handles.color;
            Handles.color = color;

            var shadowDiscRadius = Mathf.Tan(spotlight.spotAngle * Mathf.Deg2Rad * 0.5f) * spotlight.shadowNearPlane;
            var shadowDiscDistance = spotlight.shadowNearPlane;
            Handles.DrawWireDisc(Vector3.forward * shadowDiscDistance, Vector3.forward, shadowDiscRadius);
            Handles.DrawLine(Vector3.forward * shadowDiscDistance, (Vector3.right * shadowDiscRadius) + (Vector3.forward * shadowDiscDistance));
            Handles.DrawLine(Vector3.forward * shadowDiscDistance, (-Vector3.right * shadowDiscRadius) + (Vector3.forward * shadowDiscDistance));

            Handles.color = previousColor;
        }

        static void DrawConeWireframe(float radius, float height, HandleDirections handleDirections)
        {
            var rangeCenter = Vector3.forward * height;
            if (handleDirections.HasFlag(HandleDirections.Up))
            {
                var rangeUp = rangeCenter + Vector3.up * radius;
                Handles.DrawLine(Vector3.zero, rangeUp);
            }

            if (handleDirections.HasFlag(HandleDirections.Down))
            {
                var rangeDown = rangeCenter - Vector3.up * radius;
                Handles.DrawLine(Vector3.zero, rangeDown);
            }

            if (handleDirections.HasFlag(HandleDirections.Right))
            {
                var rangeRight = rangeCenter + Vector3.right * radius;
                Handles.DrawLine(Vector3.zero, rangeRight);
            }

            if (handleDirections.HasFlag(HandleDirections.Left))
            {
                var rangeLeft = rangeCenter - Vector3.right * radius;
                Handles.DrawLine(Vector3.zero, rangeLeft);
            }

            //Draw Circle
            Handles.DrawWireDisc(rangeCenter, Vector3.forward, radius);
        }

        static float DrawConeHandles(Vector3 position, float angle, float range, HandleDirections handleDirections, string controlName)
        {
            if (handleDirections.HasFlag(HandleDirections.Left))
            {
                angle = SizeSliderSpotAngle(position, Vector3.forward, -Vector3.right, range, angle, controlName);
            }
            if (handleDirections.HasFlag(HandleDirections.Up))
            {
                angle = SizeSliderSpotAngle(position, Vector3.forward, Vector3.up, range, angle, controlName);
            }
            if (handleDirections.HasFlag(HandleDirections.Right))
            {
                angle = SizeSliderSpotAngle(position, Vector3.forward, Vector3.right, range, angle, controlName);
            }
            if (handleDirections.HasFlag(HandleDirections.Down))
            {
                angle = SizeSliderSpotAngle(position, Vector3.forward, -Vector3.up, range, angle, controlName);
            }
            return angle;
        }

        static float SliderLineHandle(int id, Vector3 position, Vector3 direction, float value, string labelText = "")
        {
            Vector3 pos = position + direction * value;
            float sizeHandle = HandleUtility.GetHandleSize(pos);
            bool temp = GUI.changed;
            GUI.changed = false;
            pos = Handles.Slider(id, pos, direction, sizeHandle * 0.03f, Handles.DotHandleCap, 0f);
            if (GUI.changed)
            {
                value = Vector3.Dot(pos - position, direction);
            }
            GUI.changed |= temp;

            if (GUIUtility.hotControl == id && !String.IsNullOrEmpty(labelText))
            {
                labelText += FormattableString.Invariant($"{value:0.00}");
                DrawHandleLabel(pos, labelText);
            }

            return value;
        }

        static float SizeSliderSpotAngle(Vector3 position, Vector3 forward, Vector3 axis, float range, float spotAngle, string controlName)
        {
            if (Math.Abs(spotAngle) <= 0.05f)
                return spotAngle;
            var angledForward = Quaternion.AngleAxis(Mathf.Max(spotAngle, 0.05f) * 0.5f, axis) * forward;
            var centerToLeftOnSphere = (angledForward * range + position) - (position + forward * range);
            bool temp = GUI.changed;
            GUI.changed = false;
            var handlePosition = position + forward * range;
            var id = GUIUtility.GetControlID(FocusType.Passive);
            var newMagnitude = Mathf.Max(0f, SliderLineHandle(id, handlePosition, centerToLeftOnSphere.normalized, centerToLeftOnSphere.magnitude));
            if (GUI.changed)
            {
                centerToLeftOnSphere = centerToLeftOnSphere.normalized * newMagnitude;
                angledForward = (centerToLeftOnSphere + (position + forward * range) - position).normalized;
                spotAngle = Mathf.Clamp(Mathf.Acos(Vector3.Dot(forward, angledForward)) * Mathf.Rad2Deg * 2, 0f, 179f);
                if (spotAngle <= 0.05f || float.IsNaN(spotAngle))
                    spotAngle = 0f;
            }
            GUI.changed |= temp;

            if (GUIUtility.hotControl == id)
            {
                var pos = handlePosition + centerToLeftOnSphere.normalized * newMagnitude;
                string labelText = FormattableString.Invariant($"{controlName} {spotAngle:0.00}");
                DrawHandleLabel(pos, labelText);
            }

            return spotAngle;
        }
    }
}
