using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.Experimental.VFX;

namespace UnityEditor.VFX
{
    class VFXCircleGizmo : VFXSpaceableGizmo<Circle>
    {
        IProperty<Vector3> m_CenterProperty;
        IProperty<float> m_RadiusProperty;
        public override void RegisterEditableMembers(IContext context)
        {
            m_CenterProperty = context.RegisterProperty<Vector3>("center");
            m_RadiusProperty = context.RegisterProperty<float>("radius");
        }
        public static readonly Vector3[] radiusDirections = new Vector3[] { Vector3.left, Vector3.up, Vector3.right, Vector3.down };

        public static void DrawCircle(Circle circle,VisualEffect component, VFXGizmo gizmo, IProperty<Vector3> centerProperty, IProperty<float> radiusProperty)
        {
            if (centerProperty.isEditable && gizmo.PositionGizmo(component, ref circle.center))
            {
                centerProperty.SetValue(circle.center);
            }

            // Radius controls
            if (radiusProperty.isEditable)
            {
                foreach (var dist in radiusDirections)
                {
                    EditorGUI.BeginChangeCheck();
                    Vector3 sliderPos = circle.center + dist * circle.radius;
                    Vector3 result = Handles.Slider(sliderPos, dist, handleSize * HandleUtility.GetHandleSize(sliderPos), Handles.CubeHandleCap, 0);

                    if (EditorGUI.EndChangeCheck())
                    {
                        circle.radius = (result - circle.center).magnitude;

                        if (float.IsNaN(circle.radius))
                        {
                            circle.radius = 0;
                        }
                        radiusProperty.SetValue(circle.radius);
                    }
                }
            }
        }

        public override void OnDrawSpacedGizmo(Circle circle, VisualEffect component)
        {
            // Draw circle around the arc
            Handles.DrawWireDisc(circle.center, -Vector3.forward, circle.radius);

            DrawCircle(circle,component,this,m_CenterProperty,m_RadiusProperty);
        }
    }
    class VFXArcCircleGizmo : VFXSpaceableGizmo<ArcCircle>
    {
        IProperty<Vector3> m_CenterProperty;
        IProperty<float> m_RadiusProperty;
        IProperty<float> m_ArcProperty;

        public override void RegisterEditableMembers(IContext context)
        {
            m_CenterProperty = context.RegisterProperty<Vector3>("circle.center");
            m_RadiusProperty = context.RegisterProperty<float>("circle.radius");
            m_ArcProperty = context.RegisterProperty<float>("arc");
        }
        public static readonly Vector3[] radiusDirections = new Vector3[] { Vector3.left, Vector3.up, Vector3.right, Vector3.down };
        public override void OnDrawSpacedGizmo(ArcCircle arcCircle, VisualEffect component)
        {
            Vector3 center = arcCircle.circle.center;
            float radius = arcCircle.circle.radius;
            float arc = arcCircle.arc * Mathf.Rad2Deg;

            // Draw circle around the arc
            Handles.DrawWireArc(center, -Vector3.forward, Vector3.up, arc, radius);

            VFXCircleGizmo.DrawCircle(arcCircle.circle,component,this,m_CenterProperty,m_RadiusProperty);

            //Arc first line
            Handles.DrawLine(center, center + Vector3.up * radius);

            // Arc handle control
            if (m_ArcProperty.isEditable)
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
                        m_ArcProperty.SetValue(arc * Mathf.Deg2Rad);
                    }
                }
            }
            else
            {
                Handles.DrawLine(center, center +  Quaternion.AngleAxis(arc, -Vector3.forward) * Vector3.up * radius);
            }
        }
    }
}
