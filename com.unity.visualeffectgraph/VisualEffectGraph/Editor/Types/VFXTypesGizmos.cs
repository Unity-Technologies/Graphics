using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.Experimental.VFX;

namespace UnityEditor.VFX
{
    class VFXArcCircleGizmo : VFXSpaceableGizmo<ArcCircle>
    {
        IProperty m_CircleCenter;
        IProperty m_CircleRadius;
        IProperty m_Arc;

        public override void RegisterEditableMembers(IContext context)
        {
            m_CircleCenter = context.RegisterProperty("circle.center");
            m_CircleRadius = context.RegisterProperty("circle.radius");
            m_Arc = context.RegisterProperty("arc");
        }
        public static readonly Vector3[] radiusDirections = new Vector3[] { Vector3.left, Vector3.up, Vector3.right, Vector3.down };
        public override void OnDrawSpacedGizmo(ArcCircle arcCircle, VisualEffect component)
        {
            Vector3 center = arcCircle.circle.center;
            float radius = arcCircle.circle.radius;
            float arc = arcCircle.arc * Mathf.Rad2Deg;

            // Draw circle around the arc
            Handles.DrawWireArc(center, -Vector3.forward, Vector3.up, arc, radius);

            if (m_CircleCenter.isEditable && PositionGizmo(component, ref center))
            {
                m_CircleCenter.SetValue(center);
            }

            // Radius controls
            if (m_CircleRadius.isEditable)
            {
                foreach (var dist in radiusDirections)
                {
                    EditorGUI.BeginChangeCheck();
                    Vector3 sliderPos = center + dist * radius;
                    Vector3 result = Handles.Slider(sliderPos, dist, handleSize * HandleUtility.GetHandleSize(sliderPos), Handles.CubeHandleCap, 0);

                    if (EditorGUI.EndChangeCheck())
                    {
                        radius = (result - center).magnitude;

                        if (float.IsNaN(radius))
                        {
                            radius = 0;
                        }
                        m_CircleRadius.SetValue(radius);
                    }
                }
            }

            //Arc first line
            Handles.DrawLine(center, center + Vector3.up * radius);

            // Arc handle control
            if (m_Arc.isEditable)
            {
                using (new Handles.DrawingScope(Handles.matrix * Matrix4x4.Translate(center) * Matrix4x4.Rotate(Quaternion.Euler(-90.0f, 0.0f, 0.0f))))
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
                        m_Arc.SetValue(arc * Mathf.Deg2Rad);
                    }
                }
            }
            else
            {
                Handles.DrawLine(center, center +  Quaternion.AngleAxis(arc, -Vector3.forward) * Vector3.up * radius);
            }
        }
    }
    class VFXPositionGizmo : VFXSpaceableGizmo<Position>
    {
        IProperty m_Property;
        public override void RegisterEditableMembers(IContext context)
        {
            m_Property = context.RegisterProperty("");
        }
        public override void OnDrawSpacedGizmo(Position position, VisualEffect component)
        {
            if (m_Property.isEditable && PositionGizmo(component,  ref position.position))
            {
                m_Property.SetValue(position);
            }
        }
    }
}
