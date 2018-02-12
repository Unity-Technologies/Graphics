using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.VFX;
using System.Linq;
using System.Reflection;
using Type = System.Type;
using Delegate = System.Delegate;

namespace UnityEditor.VFX.UI
{
    interface IValueController
    {
        object value { get; set; }

        System.Type portType { get; }
    }


    public class VFXValueGizmo
    {
        static Dictionary<System.Type, System.Action<IValueController, VisualEffect>> s_DrawFunctions;


        const float handleSize = 0.1f;
        const float arcHandleSizeMultiplier = 1.25f;

        static VFXValueGizmo()
        {
            s_DrawFunctions = new Dictionary<System.Type, System.Action<IValueController, VisualEffect>>();

            s_DrawFunctions[typeof(ArcCircle)] = OnDrawArcCircleDataAnchorGizmo;
            s_DrawFunctions[typeof(Sphere)] = OnDrawSphereDataAnchorGizmo;
            s_DrawFunctions[typeof(ArcSphere)] = OnDrawArcSphereDataAnchorGizmo;
            s_DrawFunctions[typeof(Position)] = OnDrawPositionDataAnchorGizmo;
            s_DrawFunctions[typeof(AABox)] = OnDrawAABoxDataAnchorGizmo;
            s_DrawFunctions[typeof(OrientedBox)] = OnDrawOrientedBoxDataAnchorGizmo;
            s_DrawFunctions[typeof(Plane)] = OnDrawPlaneDataAnchorGizmo;
            s_DrawFunctions[typeof(Cylinder)] = OnDrawCylinderDataAnchorGizmo;
            s_DrawFunctions[typeof(ArcTorus)] = OnDrawArcTorusDataAnchorGizmo;
            s_DrawFunctions[typeof(ArcCone)] = OnDrawArcConeDataAnchorGizmo;

            foreach (Type type in typeof(VFXValueGizmo).Assembly.GetTypes())
            {
                Type gizmoedType = GetGizmoType(type);

                if (gizmoedType != null)
                {
                    MethodInfo info = type.GetMethod("OnDrawGizmo", BindingFlags.Static | BindingFlags.Public);

                    if (info != null)
                    {
                        s_DrawFunctions[gizmoedType] = (System.Action<IValueController, VisualEffect>)Delegate.CreateDelegate(typeof(System.Action<IValueController, VisualEffect>), info);
                    }
                }
            }
        }

        static Type GetGizmoType(Type type)
        {
            Type baseType = type.BaseType;
            while (baseType != null)
            {
                if (baseType.IsGenericType && !baseType.IsGenericTypeDefinition && baseType.GetGenericTypeDefinition() == typeof(VFXGizmo<>))
                {
                    return baseType.GetGenericArguments()[0];
                }
                baseType = baseType.BaseType;
            }
            return null;
        }

        static internal void Draw(IValueController anchor, VisualEffect component)
        {
            System.Action<IValueController, VisualEffect> func;
            if (s_DrawFunctions.TryGetValue(anchor.portType, out func))
            {
                func(anchor, component);
            }
        }

        static bool PositionGizmo(VisualEffect component, CoordinateSpace space, ref Vector3 position)
        {
            EditorGUI.BeginChangeCheck();

            var saveMatrix = Handles.matrix;

            if (space == CoordinateSpace.Local)
            {
                Handles.matrix = component.transform.localToWorldMatrix;
            }

            Vector3 modifiedPosition = Handles.PositionHandle(position, Quaternion.identity);

            bool changed = GUI.changed;
            EditorGUI.EndChangeCheck();

            Handles.matrix = saveMatrix;
            if (changed)
            {
                position = modifiedPosition;
                return true;
            }

            return false;
        }

        static bool RotationGizmo(VisualEffect component, CoordinateSpace space, Vector3 position, ref Vector3 rotation)
        {
            EditorGUI.BeginChangeCheck();
            if (space == CoordinateSpace.Local)
            {
                position = component.transform.worldToLocalMatrix.MultiplyPoint(position);
            }

            Quaternion modifiedRotation = Handles.RotationHandle(Quaternion.Euler(rotation), position);

            bool changed = GUI.changed;
            EditorGUI.EndChangeCheck();
            if (changed)
            {
                rotation = modifiedRotation.eulerAngles;
                return true;
            }
            return false;
        }

        static bool RotationGizmo(VisualEffect component, CoordinateSpace space, Vector3 position, ref Quaternion rotation)
        {
            var saveMatrix = Handles.matrix;

            EditorGUI.BeginChangeCheck();
            if (space == CoordinateSpace.Local)
            {
                Handles.matrix = component.transform.localToWorldMatrix;
            }

            Quaternion modifiedRotation = Handles.RotationHandle(rotation, position);

            bool changed = GUI.changed;
            EditorGUI.EndChangeCheck();

            Handles.matrix = saveMatrix;

            if (changed)
            {
                rotation = modifiedRotation;
                return true;
            }
            return false;
        }

        static void OnDrawPositionDataAnchorGizmo(IValueController anchor, VisualEffect component)
        {
            Position pos = (Position)anchor.value;

            if (PositionGizmo(component, pos.space, ref pos.position))
            {
                anchor.value = pos;
            }
        }

        static void OnDrawArcCircleDataAnchorGizmo(IValueController anchor, VisualEffect component)
        {
            Matrix4x4 oldMatrix = Handles.matrix;

            ArcCircle arcCircle = (ArcCircle)anchor.value;
            Circle circle = arcCircle.circle;

            Vector3 center = circle.center;
            float radius = circle.radius;
            float arc = arcCircle.arc * Mathf.Rad2Deg;
            if (circle.space == CoordinateSpace.Local)
            {
                Handles.matrix = component.transform.localToWorldMatrix;
            }

            // Draw circle around the arc
            Handles.DrawWireArc(center, -Vector3.forward, Vector3.up, arc, radius);

            if (PositionGizmo(component, circle.space, ref circle.center))
            {
                arcCircle.circle = circle;
                anchor.value = arcCircle;
            }

            // Radius controls
            foreach (var dist in new Vector3[] { Vector3.left, Vector3.up, Vector3.right, Vector3.down })
            {
                EditorGUI.BeginChangeCheck();
                Vector3 sliderPos = center + dist * radius;
                Vector3 result = Handles.Slider(sliderPos, dist, handleSize * HandleUtility.GetHandleSize(sliderPos), Handles.CubeHandleCap, 0);

                if (GUI.changed)
                {
                    circle.radius = (result - center).magnitude;

                    if (float.IsNaN(circle.radius))
                    {
                        circle.radius = 0;
                    }

                    arcCircle.circle = circle;
                    anchor.value = arcCircle;
                }
                EditorGUI.EndChangeCheck();
            }

            Handles.DrawLine(Vector3.zero, Vector3.up * radius);

            // Arc handle control
            using (new Handles.DrawingScope(Handles.matrix * Matrix4x4.Rotate(Quaternion.Euler(-90.0f, 0.0f, 0.0f))))
            {
                Vector3 arcHandlePosition = Quaternion.AngleAxis(arc, Vector3.up) * Vector3.forward * radius;
                EditorGUI.BeginChangeCheck();
                {
                    arcHandlePosition = Handles.Slider2D(
                            arcHandlePosition,
                            Vector3.up,
                            Vector3.forward,
                            Vector3.right,
                            handleSize * arcHandleSizeMultiplier * HandleUtility.GetHandleSize(center + arcHandlePosition),
                            DefaultAngleHandleDrawFunction,
                            0
                            );
                }
                if (EditorGUI.EndChangeCheck())
                {
                    float newArc = Vector3.Angle(Vector3.forward, arcHandlePosition) * Mathf.Sign(Vector3.Dot(Vector3.right, arcHandlePosition));
                    arc += Mathf.DeltaAngle(arc, newArc);
                    arc = Mathf.Repeat(arc, 360.0f);
                    arcCircle.arc = arc * Mathf.Deg2Rad;

                    arcCircle.circle = circle;
                    anchor.value = arcCircle;
                }
            }

            Handles.matrix = oldMatrix;
        }

        static void OnDrawSphereDataAnchorGizmo(IValueController anchor, VisualEffect component)
        {
            Sphere sphere = (Sphere)anchor.value;

            Vector3 center = sphere.center;
            float radius = sphere.radius;
            if (sphere.space == CoordinateSpace.Local)
            {
                center = component.transform.localToWorldMatrix.MultiplyPoint(center);
            }

            Handles.DrawWireArc(center, Vector3.forward, Vector3.up, 360f, radius);
            Handles.DrawWireArc(center, Vector3.up, Vector3.right, 360f, radius);
            Handles.DrawWireArc(center, Vector3.right, Vector3.forward, 360f, radius);

            if (PositionGizmo(component, sphere.space, ref sphere.center))
            {
                anchor.value = sphere;
            }

            foreach (var dist in new Vector3[] { Vector3.left, Vector3.up, Vector3.forward })
            {
                EditorGUI.BeginChangeCheck();
                Vector3 sliderPos = center + dist * radius;
                Vector3 result = Handles.Slider(sliderPos, dist, handleSize * HandleUtility.GetHandleSize(sliderPos), Handles.CubeHandleCap, 0);

                if (GUI.changed)
                {
                    sphere.radius = (result - center).magnitude;

                    if (float.IsNaN(sphere.radius))
                    {
                        sphere.radius = 0;
                    }

                    anchor.value = sphere;
                }
                EditorGUI.EndChangeCheck();
            }
        }

        static void OnDrawArcSphereDataAnchorGizmo(IValueController anchor, VisualEffect component)
        {
            Matrix4x4 oldMatrix = Handles.matrix;

            ArcSphere arcSphere = (ArcSphere)anchor.value;
            Sphere sphere = arcSphere.sphere;

            Vector3 center = sphere.center;
            float radius = sphere.radius;
            float arc = arcSphere.arc * Mathf.Rad2Deg;
            if (sphere.space == CoordinateSpace.Local)
            {
                Handles.matrix = component.transform.localToWorldMatrix;
            }

            // Draw semi-circles at 90 degree angles
            for (int i = 0; i < 4; i++)
            {
                float currentArc = (float)(i * 90);
                if (currentArc <= arc)
                    Handles.DrawWireArc(center, Matrix4x4.Rotate(Quaternion.Euler(0.0f, 180.0f, currentArc)) * Vector3.right, Vector3.forward, 180.0f, radius);
            }

            // Draw an extra semi-circle at the arc angle
            if (arcSphere.arc < Mathf.PI * 2.0f)
                Handles.DrawWireArc(center, Matrix4x4.Rotate(Quaternion.Euler(0.0f, 180.0f, arc)) * Vector3.right, Vector3.forward, 180.0f, radius);

            // Draw 3rd circle around the arc
            Handles.DrawWireArc(center, -Vector3.forward, Vector3.up, arc, radius);

            if (PositionGizmo(component, sphere.space, ref sphere.center))
            {
                arcSphere.sphere = sphere;
                anchor.value = arcSphere;
            }

            // Radius controls
            foreach (var dist in new Vector3[] { Vector3.left, Vector3.up, Vector3.forward })
            {
                EditorGUI.BeginChangeCheck();
                Vector3 sliderPos = center + dist * radius;
                Vector3 result = Handles.Slider(sliderPos, dist, handleSize * HandleUtility.GetHandleSize(sliderPos), Handles.CubeHandleCap, 0);

                if (GUI.changed)
                {
                    sphere.radius = (result - center).magnitude;

                    if (float.IsNaN(sphere.radius))
                    {
                        sphere.radius = 0;
                    }

                    arcSphere.sphere = sphere;
                    anchor.value = arcSphere;
                }
                EditorGUI.EndChangeCheck();
            }

            // Arc handle control
            using (new Handles.DrawingScope(Handles.matrix * Matrix4x4.Rotate(Quaternion.Euler(-90.0f, 0.0f, 0.0f))))
            {
                Vector3 arcHandlePosition = Quaternion.AngleAxis(arc, Vector3.up) * Vector3.forward * radius;
                EditorGUI.BeginChangeCheck();
                {
                    arcHandlePosition = Handles.Slider2D(
                            arcHandlePosition,
                            Vector3.up,
                            Vector3.forward,
                            Vector3.right,
                            handleSize * arcHandleSizeMultiplier * HandleUtility.GetHandleSize(center + arcHandlePosition),
                            DefaultAngleHandleDrawFunction,
                            0
                            );
                }
                if (EditorGUI.EndChangeCheck())
                {
                    float newArc = Vector3.Angle(Vector3.forward, arcHandlePosition) * Mathf.Sign(Vector3.Dot(Vector3.right, arcHandlePosition));
                    arc += Mathf.DeltaAngle(arc, newArc);
                    arc = Mathf.Repeat(arc, 360.0f);

                    arcSphere.arc = arc * Mathf.Deg2Rad;
                    anchor.value = arcSphere;
                }
            }

            Handles.matrix = oldMatrix;
        }

        static void OnDrawArcTorusDataAnchorGizmo(IValueController anchor, VisualEffect component)
        {
            Matrix4x4 oldMatrix = Handles.matrix;

            ArcTorus torus = (ArcTorus)anchor.value;

            Vector3 center = torus.center;
            float majorRadius = torus.majorRadius;
            float minorRadius = torus.minorRadius;
            float arc = torus.arc * Mathf.Rad2Deg;
            if (torus.space == CoordinateSpace.Local)
            {
                Handles.matrix = component.transform.localToWorldMatrix;
            }

            if (PositionGizmo(component, torus.space, ref torus.center))
            {
                anchor.value = torus;
            }

            Handles.DrawLine(Vector3.zero, Vector3.up * majorRadius);

            // Arc handle control
            using (new Handles.DrawingScope(Handles.matrix * Matrix4x4.Rotate(Quaternion.Euler(-90.0f, 0.0f, 0.0f))))
            {
                Vector3 arcHandlePosition = Quaternion.AngleAxis(arc, Vector3.up) * Vector3.forward * majorRadius;
                EditorGUI.BeginChangeCheck();
                {
                    arcHandlePosition = Handles.Slider2D(
                            arcHandlePosition,
                            Vector3.up,
                            Vector3.forward,
                            Vector3.right,
                            handleSize * arcHandleSizeMultiplier * HandleUtility.GetHandleSize(center + arcHandlePosition),
                            DefaultAngleHandleDrawFunction,
                            0
                            );
                }
                if (EditorGUI.EndChangeCheck())
                {
                    float newArc = Vector3.Angle(Vector3.forward, arcHandlePosition) * Mathf.Sign(Vector3.Dot(Vector3.right, arcHandlePosition));
                    arc += Mathf.DeltaAngle(arc, newArc);
                    arc = Mathf.Repeat(arc, 360.0f);
                    torus.arc = arc * Mathf.Deg2Rad;

                    anchor.value = torus;
                }
            }

            // Donut extents
            float excessAngle = arc % 360f;
            float angle = Mathf.Abs(arc) >= 360f ? 360f : excessAngle;

            using (new Handles.DrawingScope(Handles.matrix * Matrix4x4.Rotate(Quaternion.Euler(-90.0f, 0.0f, 0.0f))))
            {
                Handles.DrawWireArc(new Vector3(0.0f, minorRadius, 0.0f), Vector3.up, Vector3.forward, angle, majorRadius);
                Handles.DrawWireArc(new Vector3(0.0f, -minorRadius, 0.0f), Vector3.up, Vector3.forward, angle, majorRadius);
                Handles.DrawWireArc(Vector3.zero, Vector3.up, Vector3.forward, angle, majorRadius + minorRadius);
                Handles.DrawWireArc(Vector3.zero, Vector3.up, Vector3.forward, angle, majorRadius - minorRadius);

                foreach (var arcAngle in new float[] { 0.0f, 90.0f, 180.0f, 270.0f, arc }.Where(a => a <= arc))
                {
                    Quaternion arcRotation = Quaternion.AngleAxis(arcAngle, Vector3.up);
                    Vector3 capCenter = arcRotation * Vector3.forward * majorRadius;
                    Handles.DrawWireDisc(capCenter, arcRotation * Vector3.right, minorRadius);

                    if (arcAngle != arc)
                    {
                        // Minor radius
                        foreach (var dist in new Vector3[] { Vector3.left, Vector3.up, Vector3.right, Vector3.down })
                        {
                            Vector3 distRotated = Matrix4x4.Rotate(Quaternion.Euler(0.0f, arcAngle + 90.0f, 0.0f)) * dist;

                            EditorGUI.BeginChangeCheck();
                            Vector3 sliderPos = capCenter + distRotated * minorRadius;
                            Vector3 result = Handles.Slider(sliderPos, distRotated, handleSize * HandleUtility.GetHandleSize(sliderPos), Handles.CubeHandleCap, 0);

                            if (GUI.changed)
                            {
                                torus.minorRadius = (result - capCenter).magnitude;

                                if (float.IsNaN(torus.minorRadius))
                                {
                                    torus.minorRadius = 0;
                                }

                                anchor.value = torus;
                            }
                            EditorGUI.EndChangeCheck();
                        }

                        // Major radius
                        {
                            Vector3 distRotated = Matrix4x4.Rotate(Quaternion.Euler(0.0f, arcAngle + 90.0f, 0.0f)) * Vector3.left;

                            EditorGUI.BeginChangeCheck();
                            Vector3 sliderPos = center + distRotated * majorRadius;
                            Vector3 result = Handles.Slider(sliderPos, distRotated, handleSize * HandleUtility.GetHandleSize(sliderPos), Handles.CubeHandleCap, 0);

                            if (GUI.changed)
                            {
                                torus.majorRadius = (result - center).magnitude;

                                if (float.IsNaN(torus.majorRadius))
                                {
                                    torus.majorRadius = 0;
                                }

                                anchor.value = torus;
                            }
                            EditorGUI.EndChangeCheck();
                        }
                    }
                }
            }

            Handles.matrix = oldMatrix;
        }

        private static void DefaultAngleHandleDrawFunction(int controlID, Vector3 position, Quaternion rotation, float size, EventType eventType)
        {
            Handles.DrawLine(Vector3.zero, position);

            // draw a cylindrical "hammer head" to indicate the direction the handle will move
            Vector3 worldPosition = Handles.matrix.MultiplyPoint3x4(position);
            Vector3 normal = worldPosition - Handles.matrix.MultiplyPoint3x4(Vector3.zero);
            Vector3 tangent = Handles.matrix.MultiplyVector(Quaternion.AngleAxis(90f, Vector3.up) * position);
            rotation = Quaternion.LookRotation(tangent, normal);
            Matrix4x4 matrix = Matrix4x4.TRS(worldPosition, rotation, (Vector3.one + Vector3.forward * arcHandleSizeMultiplier));
            using (new Handles.DrawingScope(matrix))
                Handles.CylinderHandleCap(controlID, Vector3.zero, Quaternion.identity, size, eventType);
        }

        static void OnDrawAABoxDataAnchorGizmo(IValueController anchor, VisualEffect component)
        {
            AABox box = (AABox)anchor.value;

            if (OnDrawBoxDataAnchorGizmo(anchor, component, box.space, ref box.center, ref box.size, Vector3.zero))
            {
                anchor.value = box;
            }
        }

        static void OnDrawOrientedBoxDataAnchorGizmo(IValueController anchor, VisualEffect component)
        {
            OrientedBox box = (OrientedBox)anchor.value;

            if (OnDrawBoxDataAnchorGizmo(anchor, component, box.space, ref box.center, ref box.size, box.angles))
            {
                anchor.value = box;
            }
            if (RotationGizmo(component, box.space, box.center, ref box.angles))
            {
                anchor.value = box;
            }
        }

        static void OnDrawPlaneDataAnchorGizmo(IValueController anchor, VisualEffect component)
        {
            Plane plane = (Plane)anchor.value;

            Quaternion normalQuat = Quaternion.FromToRotation(Vector3.forward, plane.normal);
            Handles.RectangleHandleCap(0, plane.position, normalQuat, 10, Event.current.type);

            Handles.ArrowHandleCap(0, plane.position, normalQuat, 5, Event.current.type);

            if (PositionGizmo(component, plane.space, ref plane.position))
            {
                anchor.value = plane;
            }

            Vector3 normal = plane.normal.normalized;

            Quaternion rotation = Quaternion.FromToRotation(Vector3.forward, normal);

            EditorGUI.BeginChangeCheck();
            Quaternion result = Handles.RotationHandle(rotation, plane.position);


            //Quaternion result = UnityEditorInternal.Disc.Do(0, rotation, plane.position, Vector3.left, 3, true, 0, false, true, Color.yellow);

            if (GUI.changed)
            {
                normal = result * Vector3.forward;
                plane.normal = normal;
                anchor.value = plane;
            }
            EditorGUI.EndChangeCheck();
        }

        static void OnDrawCylinderDataAnchorGizmo(IValueController anchor, VisualEffect component)
        {
            Cylinder cylinder = (Cylinder)anchor.value;

            Vector3 center = cylinder.center;
            Vector3 normal = Vector3.up;

            Vector3 worldNormal = normal;

            Vector3 topCap = cylinder.height * 0.5f * Vector3.up;
            Vector3 bottomCap = -cylinder.height * 0.5f * Vector3.up;

            Vector3[] extremities = new Vector3[8];

            extremities[0] = topCap + Vector3.forward * cylinder.radius;
            extremities[1] = topCap - Vector3.forward * cylinder.radius;

            extremities[2] = topCap + Vector3.left * cylinder.radius;
            extremities[3] = topCap - Vector3.left * cylinder.radius;

            extremities[4] = bottomCap + Vector3.forward * cylinder.radius;
            extremities[5] = bottomCap - Vector3.forward * cylinder.radius;

            extremities[6] = bottomCap + Vector3.left * cylinder.radius;
            extremities[7] = bottomCap - Vector3.left * cylinder.radius;


            Quaternion normalRotation = Quaternion.FromToRotation(Vector3.up, normal);

            for (int i = 0; i < extremities.Length; ++i)
            {
                extremities[i] = normalRotation * extremities[i];
            }

            topCap = normalRotation * topCap;
            bottomCap = normalRotation * bottomCap;

            for (int i = 0; i < extremities.Length; ++i)
            {
                extremities[i] = center + extremities[i];
            }

            topCap += center;
            bottomCap += center;


            if (cylinder.space == CoordinateSpace.Local)
            {
                Matrix4x4 mat = component.transform.localToWorldMatrix;

                center = mat.MultiplyPoint(center);
                topCap = mat.MultiplyPoint(topCap);
                bottomCap = mat.MultiplyPoint(bottomCap);

                worldNormal = mat.MultiplyVector(normal).normalized;

                for (int i = 0; i < extremities.Length; ++i)
                {
                    extremities[i] = mat.MultiplyPoint(extremities[i]);
                }
            }

            Handles.DrawWireDisc(topCap, worldNormal, cylinder.radius);
            Handles.DrawWireDisc(bottomCap, worldNormal, cylinder.radius);

            for (int i = 0; i < extremities.Length / 2; ++i)
            {
                Handles.DrawLine(extremities[i], extremities[i + extremities.Length / 2]);
            }

            if (PositionGizmo(component, cylinder.space, ref cylinder.center))
            {
                anchor.value = cylinder;
            }

            Vector3 result;
            for (int i = 0; i < extremities.Length / 2; ++i)
            {
                EditorGUI.BeginChangeCheck();

                Vector3 pos = (extremities[i] + extremities[i + +extremities.Length / 2]) * 0.5f;
                result = Handles.Slider(pos, pos - center, handleSize * HandleUtility.GetHandleSize(pos), Handles.CubeHandleCap, 0);

                if (GUI.changed)
                {
                    cylinder.radius = (result - center).magnitude;
                    anchor.value = cylinder;
                }

                EditorGUI.EndChangeCheck();
            }

            EditorGUI.BeginChangeCheck();

            result = Handles.Slider(topCap, topCap - center, handleSize * HandleUtility.GetHandleSize(topCap), Handles.CubeHandleCap, 0);

            if (GUI.changed)
            {
                cylinder.height = (result - center).magnitude * 2;
                anchor.value = cylinder;
            }

            EditorGUI.EndChangeCheck();

            EditorGUI.BeginChangeCheck();

            result = Handles.Slider(bottomCap, bottomCap - center, handleSize * HandleUtility.GetHandleSize(bottomCap), Handles.CubeHandleCap, 0);

            if (GUI.changed)
            {
                cylinder.height = (result - center).magnitude * 2;
                anchor.value = cylinder;
            }

            EditorGUI.EndChangeCheck();
        }

        static bool OnDrawBoxDataAnchorGizmo(IValueController anchor, VisualEffect component, CoordinateSpace space, ref Vector3 center, ref Vector3 size, Vector3 additionnalRotation)
        {
            var saveMatrix = Handles.matrix;
            if (space == CoordinateSpace.Local)
            {
                Matrix4x4 addMat = Matrix4x4.Rotate(Quaternion.Euler(additionnalRotation));

                addMat *= component.transform.localToWorldMatrix;
                Handles.matrix = addMat;
            }
            Vector3[] points = new Vector3[8];


            points[0] = center + new Vector3(size.x * 0.5f, size.y * 0.5f, size.z * 0.5f);
            points[1] = center + new Vector3(size.x * 0.5f, -size.y * 0.5f, size.z * 0.5f);

            points[2] = center + new Vector3(-size.x * 0.5f, size.y * 0.5f, size.z * 0.5f);
            points[3] = center + new Vector3(-size.x * 0.5f, -size.y * 0.5f, size.z * 0.5f);

            points[4] = center + new Vector3(size.x * 0.5f, size.y * 0.5f, -size.z * 0.5f);
            points[5] = center + new Vector3(size.x * 0.5f, -size.y * 0.5f, -size.z * 0.5f);

            points[6] = center + new Vector3(-size.x * 0.5f, size.y * 0.5f, -size.z * 0.5f);
            points[7] = center + new Vector3(-size.x * 0.5f, -size.y * 0.5f, -size.z * 0.5f);


            Matrix4x4 mat = Matrix4x4.identity;

            Handles.DrawLine(points[0], points[1]);
            Handles.DrawLine(points[2], points[3]);
            Handles.DrawLine(points[4], points[5]);
            Handles.DrawLine(points[6], points[7]);

            Handles.DrawLine(points[0], points[2]);
            Handles.DrawLine(points[0], points[4]);
            Handles.DrawLine(points[1], points[3]);
            Handles.DrawLine(points[1], points[5]);

            Handles.DrawLine(points[2], points[6]);
            Handles.DrawLine(points[3], points[7]);
            Handles.DrawLine(points[4], points[6]);
            Handles.DrawLine(points[5], points[7]);

            bool changed = false;

            EditorGUI.BeginChangeCheck();

            Handles.color = Color.blue;
            {
                // axis +Z
                Vector3 middle = (points[0] + points[1] + points[2] + points[3]) * 0.25f;
                Vector3 othermiddle = (points[4] + points[5] + points[6] + points[7]) * 0.25f;
                Vector3 middleResult = Handles.Slider(middle, (middle - center), handleSize * HandleUtility.GetHandleSize(middle), Handles.CubeHandleCap, 0);

                if (GUI.changed)
                {
                    size.z = (middleResult - othermiddle).magnitude;
                    center = (middleResult + othermiddle) * 0.5f;
                    changed = true;
                }
            }
            EditorGUI.EndChangeCheck();
            EditorGUI.BeginChangeCheck();
            {
                // axis -Z
                Vector3 middle = (points[4] + points[5] + points[6] + points[7]) * 0.25f;
                Vector3 othermiddle = (points[0] + points[1] + points[2] + points[3]) * 0.25f;
                Vector3 middleResult = Handles.Slider(middle, (middle - center), handleSize * HandleUtility.GetHandleSize(middle), Handles.CubeHandleCap, 0);

                if (GUI.changed)
                {
                    size.z = (middleResult - othermiddle).magnitude;
                    center = (middleResult + othermiddle) * 0.5f;
                    changed = true;
                }
            }
            EditorGUI.EndChangeCheck();


            Handles.color = Color.red;
            EditorGUI.BeginChangeCheck();
            {
                // axis +X
                Vector3 middle = (points[0] + points[1] + points[4] + points[5]) * 0.25f;
                Vector3 othermiddle = (points[2] + points[3] + points[6] + points[7]) * 0.25f;
                Vector3 middleResult = Handles.Slider(middle, (middle - center), handleSize * HandleUtility.GetHandleSize(middle), Handles.CubeHandleCap, 0);

                if (GUI.changed)
                {
                    size.x = (middleResult - othermiddle).magnitude;
                    center = (middleResult + othermiddle) * 0.5f;
                    changed = true;
                }
            }
            EditorGUI.EndChangeCheck();
            EditorGUI.BeginChangeCheck();
            {
                // axis -X
                Vector3 middle = (points[2] + points[3] + points[6] + points[7]) * 0.25f;
                Vector3 othermiddle = (points[0] + points[1] + points[4] + points[5]) * 0.25f;
                Vector3 middleResult = Handles.Slider(middle, (middle - center), handleSize * HandleUtility.GetHandleSize(middle), Handles.CubeHandleCap, 0);

                if (GUI.changed)
                {
                    size.x = (middleResult - othermiddle).magnitude;
                    center = (middleResult + othermiddle) * 0.5f;
                    changed = true;
                }
            }

            EditorGUI.EndChangeCheck();

            Handles.color = Color.green;
            EditorGUI.BeginChangeCheck();
            {
                // axis +Y
                Vector3 middle = (points[0] + points[2] + points[4] + points[6]) * 0.25f;
                Vector3 othermiddle = (points[1] + points[3] + points[5] + points[7]) * 0.25f;
                Vector3 middleResult = Handles.Slider(middle, (middle - center), handleSize * HandleUtility.GetHandleSize(middle), Handles.CubeHandleCap, 0);

                if (GUI.changed)
                {
                    size.y = (middleResult - othermiddle).magnitude;
                    center = (middleResult + othermiddle) * 0.5f;
                    changed = true;
                }
            }
            EditorGUI.EndChangeCheck();
            EditorGUI.BeginChangeCheck();
            {
                // axis -Y
                Vector3 middle = (points[1] + points[3] + points[5] + points[7]) * 0.25f;
                Vector3 othermiddle = (points[0] + points[2] + points[4] + points[6]) * 0.25f;
                Vector3 middleResult = Handles.Slider(middle, (middle - center), handleSize * HandleUtility.GetHandleSize(middle), Handles.CubeHandleCap, 0);

                if (GUI.changed)
                {
                    size.y = (middleResult - othermiddle).magnitude;
                    center = (middleResult + othermiddle) * 0.5f;
                    changed = true;
                }
            }
            EditorGUI.EndChangeCheck();


            if (PositionGizmo(component, space, ref center))
            {
                changed = true;
            }


            Handles.matrix = saveMatrix;
            return changed;
        }

        static void OnDrawArcConeDataAnchorGizmo(IValueController anchor, VisualEffect component)
        {
            ArcCone cone = (ArcCone)anchor.value;

            Vector3 center = cone.center;
            Vector3 normal = Vector3.up;

            Vector3 worldNormal = normal;

            Vector3 topCap = cone.height * Vector3.up;
            Vector3 bottomCap = Vector3.zero;

            Vector3[] extremities = new Vector3[8];

            extremities[0] = topCap + Vector3.forward * cone.radius1;
            extremities[1] = topCap - Vector3.forward * cone.radius1;

            extremities[2] = topCap + Vector3.left * cone.radius1;
            extremities[3] = topCap - Vector3.left * cone.radius1;

            extremities[4] = bottomCap + Vector3.forward * cone.radius0;
            extremities[5] = bottomCap - Vector3.forward * cone.radius0;

            extremities[6] = bottomCap + Vector3.left * cone.radius0;
            extremities[7] = bottomCap - Vector3.left * cone.radius0;

            Quaternion normalRotation = Quaternion.FromToRotation(Vector3.up, normal);

            for (int i = 0; i < extremities.Length; ++i)
            {
                extremities[i] = normalRotation * extremities[i];
            }

            topCap = normalRotation * topCap;
            bottomCap = normalRotation * bottomCap;

            for (int i = 0; i < extremities.Length; ++i)
            {
                extremities[i] = center + extremities[i];
            }

            topCap += center;
            bottomCap += center;

            if (cone.space == CoordinateSpace.Local)
            {
                Matrix4x4 mat = component.transform.localToWorldMatrix;

                center = mat.MultiplyPoint(center);
                topCap = mat.MultiplyPoint(topCap);
                bottomCap = mat.MultiplyPoint(bottomCap);

                worldNormal = mat.MultiplyVector(normal).normalized;

                for (int i = 0; i < extremities.Length; ++i)
                {
                    extremities[i] = mat.MultiplyPoint(extremities[i]);
                }
            }

            Handles.DrawWireDisc(topCap, worldNormal, cone.radius1);
            Handles.DrawWireDisc(bottomCap, worldNormal, cone.radius0);

            for (int i = 0; i < extremities.Length / 2; ++i)
            {
                Handles.DrawLine(extremities[i], extremities[i + extremities.Length / 2]);
            }

            if (PositionGizmo(component, cone.space, ref cone.center))
            {
                anchor.value = cone;
            }

            Vector3 result;
            for (int i = 0; i < extremities.Length; ++i)
            {
                EditorGUI.BeginChangeCheck();

                Vector3 pos = extremities[i];
                result = Handles.Slider(pos, pos - center, handleSize * HandleUtility.GetHandleSize(pos), Handles.CubeHandleCap, 0);

                if (GUI.changed)
                {
                    if (i >= extremities.Length / 2)
                        cone.radius0 = (result - center).magnitude;
                    else
                        cone.radius1 = (result - topCap).magnitude;
                    anchor.value = cone;
                }

                EditorGUI.EndChangeCheck();
            }

            EditorGUI.BeginChangeCheck();

            result = Handles.Slider(topCap, topCap - center, handleSize * HandleUtility.GetHandleSize(topCap), Handles.CubeHandleCap, 0);

            if (GUI.changed)
            {
                cone.height = (result - center).magnitude;
                anchor.value = cone;
            }

            EditorGUI.EndChangeCheck();
        }
    }

    class Gizmo
    {
    }

    class VFXGizmo<T> : VFXGizmo
    {
        public static bool PositionGizmo(VisualEffect component, CoordinateSpace space, ref Vector3 position)
        {
            EditorGUI.BeginChangeCheck();

            Vector3 worldPosition = position;
            if (space == CoordinateSpace.Local)
            {
                worldPosition = component.transform.localToWorldMatrix.MultiplyPoint(position);
            }

            Vector3 modifiedPosition = Handles.PositionHandle(worldPosition, space == CoordinateSpace.Local ? component.transform.rotation : Quaternion.identity);
            if (space == CoordinateSpace.Local)
            {
                modifiedPosition = component.transform.worldToLocalMatrix.MultiplyPoint(modifiedPosition);
            }
            bool changed = GUI.changed;
            EditorGUI.EndChangeCheck();
            if (changed)
            {
                position = modifiedPosition;
                return true;
            }
            return false;
        }
    }

    class VFXPositionGizmo : VFXGizmo<Position>
    {
        public static void OnDrawGizmo(IValueController anchor, VisualEffect component)
        {
            Position pos = (Position)anchor.value;

            if (PositionGizmo(component, pos.space, ref pos.position))
            {
                anchor.value = pos;
            }
        }
    }
}
