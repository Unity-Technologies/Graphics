using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.Experimental.VFX;

namespace UnityEditor.VFX
{
    class VFXTorusGizmo : VFXSpaceableGizmo<Torus>
    {
        IProperty<Vector3> m_CenterProperty;
        IProperty<float> m_MinorRadiusProperty;
        IProperty<float> m_MajorRadiusProperty;

        public override void RegisterEditableMembers(IContext context)
        {
            m_CenterProperty = context.RegisterProperty<Vector3>("center");
            m_MinorRadiusProperty = context.RegisterProperty<float>("minorRadius");
            m_MajorRadiusProperty = context.RegisterProperty<float>("majorRadius");
        }
        public static readonly Vector3[] radiusDirections = new Vector3[] { Vector3.left, Vector3.up, Vector3.right, Vector3.down };

        public static readonly float[] angles = new float[] { 0.0f, 90.0f, 180.0f, 270.0f };

        public static void DrawTorus(Torus torus,VFXGizmo gizmo, IProperty<Vector3> centerProperty, IProperty<float> minorRadiusProperty, IProperty<float> majorRadiusProperty, IEnumerable<float> angles)
        {
            if (centerProperty.isEditable && gizmo.PositionGizmo(ref torus.center))
            {
                centerProperty.SetValue(torus.center);
            }

            // Radius controls


            using (new Handles.DrawingScope(Handles.matrix * Matrix4x4.Translate(torus.center) * Matrix4x4.Rotate(Quaternion.Euler(-90.0f, 0.0f, 0.0f))))
            {
                foreach (var arcAngle in angles)
                {
                    Quaternion arcRotation = Quaternion.AngleAxis(arcAngle, Vector3.up);
                    Vector3 capCenter = arcRotation * Vector3.forward * torus.majorRadius;
                    Handles.DrawWireDisc(capCenter, arcRotation * Vector3.right, torus.minorRadius);

                    if (minorRadiusProperty.isEditable)
                    {
                        // Minor radius
                        foreach (var dist in radiusDirections)
                        {
                            Vector3 distRotated = Matrix4x4.Rotate(Quaternion.Euler(0.0f, arcAngle + 90.0f, 0.0f)) * dist;

                            EditorGUI.BeginChangeCheck();
                            Vector3 sliderPos = capCenter + distRotated * torus.minorRadius;
                            Vector3 result = Handles.Slider(sliderPos, distRotated, handleSize * HandleUtility.GetHandleSize(sliderPos), Handles.CubeHandleCap, 0);

                            if (EditorGUI.EndChangeCheck())
                            {
                                float newRadius = (result - capCenter).magnitude;

                                if (float.IsNaN(newRadius))
                                {
                                    newRadius = 0;
                                }
                                minorRadiusProperty.SetValue(newRadius);
                            }
                        }
                    }

                    if (majorRadiusProperty.isEditable)
                    {
                        // Major radius
                        {
                            Vector3 distRotated = Matrix4x4.Rotate(Quaternion.Euler(0.0f, arcAngle + 90.0f, 0.0f)) * Vector3.left;

                            EditorGUI.BeginChangeCheck();
                            Vector3 sliderPos = distRotated * torus.majorRadius;
                            Vector3 result = Handles.Slider(sliderPos, distRotated, handleSize * HandleUtility.GetHandleSize(sliderPos), Handles.CubeHandleCap, 0);

                            if (EditorGUI.EndChangeCheck())
                            {
                                float newRadius = (result).magnitude;

                                if (float.IsNaN(newRadius))
                                {
                                    newRadius = 0;
                                }
                                majorRadiusProperty.SetValue(newRadius);
                            }
                        }
                    }
                }
            }
        }

        public override void OnDrawSpacedGizmo(Torus torus)
        {
            // Draw torus
            Handles.DrawWireDisc(torus.center + Vector3.forward * torus.minorRadius, Vector3.forward, torus.majorRadius);
            Handles.DrawWireDisc(torus.center + Vector3.back * torus.minorRadius, Vector3.forward, torus.majorRadius);
            Handles.DrawWireDisc(torus.center, Vector3.forward, torus.majorRadius + torus.minorRadius);
            Handles.DrawWireDisc(torus.center, Vector3.forward, torus.majorRadius - torus.minorRadius);

            DrawTorus(torus,this,m_CenterProperty, m_MinorRadiusProperty,m_MajorRadiusProperty, angles);
        }
    }
    class VFXArcTorusGizmo : VFXSpaceableGizmo<ArcTorus>
    {
        IProperty<Vector3> m_CenterProperty;
        IProperty<float> m_MinorRadiusProperty;
        IProperty<float> m_MajorRadiusProperty;
        IProperty<float> m_ArcProperty;

        public override void RegisterEditableMembers(IContext context)
        {
            m_CenterProperty = context.RegisterProperty<Vector3>("center");
            m_MinorRadiusProperty = context.RegisterProperty<float>("minorRadius");
            m_MajorRadiusProperty = context.RegisterProperty<float>("majorRadius");
            m_ArcProperty = context.RegisterProperty<float>("arc");
        }
        public static readonly Vector3[] radiusDirections = new Vector3[] { Vector3.left, Vector3.up, Vector3.right, Vector3.down };
        public override void OnDrawSpacedGizmo(ArcTorus arcTorus)
        {
            Vector3 center = arcTorus.center;
            float arc = arcTorus.arc * Mathf.Rad2Deg;

            float excessAngle = arc % 360f;
            float angle = Mathf.Abs(arc) >= 360f ? 360f : excessAngle;

            Handles.DrawWireArc(arcTorus.center + Vector3.forward * arcTorus.minorRadius, Vector3.back, Vector3.up, angle, arcTorus.majorRadius);
            Handles.DrawWireArc(arcTorus.center + Vector3.back * arcTorus.minorRadius, Vector3.back, Vector3.up, angle, arcTorus.majorRadius);
            Handles.DrawWireArc(arcTorus.center, Vector3.back, Vector3.up, angle, arcTorus.majorRadius + arcTorus.minorRadius);
            Handles.DrawWireArc(arcTorus.center, Vector3.back, Vector3.up, angle, arcTorus.majorRadius - arcTorus.minorRadius);

            Torus torus = new Torus() { center = arcTorus.center, minorRadius = arcTorus.minorRadius, majorRadius = arcTorus.majorRadius };
            VFXTorusGizmo.DrawTorus(torus,this,m_CenterProperty, m_MinorRadiusProperty, m_MajorRadiusProperty, VFXTorusGizmo.angles.Concat(new float[] { arc }).Where(t=>t <= arc));

            // Arc handle control
            if (m_ArcProperty.isEditable)
            {
                using (new Handles.DrawingScope(Handles.matrix * Matrix4x4.Translate(center) * Matrix4x4.Rotate(Quaternion.Euler(-90.0f, 0.0f, 0.0f))))
                {
                    Vector3 arcHandlePosition = Quaternion.AngleAxis(arc, Vector3.up) * Vector3.forward * arcTorus.majorRadius;
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
                        m_ArcProperty.SetValue(arc * Mathf.Deg2Rad);
                    }
                }
            }
        }
    }
}
