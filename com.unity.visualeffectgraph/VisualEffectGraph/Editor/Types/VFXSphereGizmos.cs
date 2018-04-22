using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.Experimental.VFX;

namespace UnityEditor.VFX
{
    class VFXSphereGizmo : VFXSpaceableGizmo<Sphere>
    {
        IProperty<Vector3> m_CenterProperty;
        IProperty<float> m_RadiusProperty;
        public override void RegisterEditableMembers(IContext context)
        {
            m_CenterProperty = context.RegisterProperty<Vector3>("center");
            m_RadiusProperty = context.RegisterProperty<float>("radius");
        }
        public static readonly Vector3[] radiusDirections = new Vector3[] { Vector3.left, Vector3.up, Vector3.forward };

        public static void DrawSphere(Sphere sphere,VisualEffect component, VFXGizmo gizmo, IProperty<Vector3> centerProperty, IProperty<float> radiusProperty)
        {
            if (centerProperty.isEditable && gizmo.PositionGizmo(component, ref sphere.center))
            {
                centerProperty.SetValue(sphere.center);
            }

            // Radius controls
            if (radiusProperty.isEditable)
            {
                foreach (var dist in radiusDirections)
                {
                    EditorGUI.BeginChangeCheck();
                    Vector3 sliderPos = sphere.center + dist * sphere.radius;
                    Vector3 result = Handles.Slider(sliderPos, dist, handleSize * HandleUtility.GetHandleSize(sliderPos), Handles.CubeHandleCap, 0);

                    if (EditorGUI.EndChangeCheck())
                    {
                        float newRadius = (result - sphere.center).magnitude;

                        if (float.IsNaN(newRadius))
                        {
                            newRadius = 0;
                        }
                        radiusProperty.SetValue(newRadius);
                    }
                }
            }
        }

        public override void OnDrawSpacedGizmo(Sphere sphere, VisualEffect component)
        {
            Handles.DrawWireDisc(sphere.center, Vector3.forward, sphere.radius);
            Handles.DrawWireDisc(sphere.center, Vector3.up, sphere.radius);
            Handles.DrawWireDisc(sphere.center, Vector3.right, sphere.radius);

            DrawSphere(sphere,component,this,m_CenterProperty,m_RadiusProperty);
        }
    }
    class VFXArcSphereGizmo : VFXSpaceableGizmo<ArcSphere>
    {
        IProperty<Vector3> m_CenterProperty;
        IProperty<float> m_RadiusProperty;
        IProperty<float> m_ArcProperty;

        public override void RegisterEditableMembers(IContext context)
        {
            m_CenterProperty = context.RegisterProperty<Vector3>("sphere.center");
            m_RadiusProperty = context.RegisterProperty<float>("sphere.radius");
            m_ArcProperty = context.RegisterProperty<float>("arc");
        }
        public static readonly Vector3[] radiusDirections = new Vector3[] { Vector3.left, Vector3.up, Vector3.right, Vector3.down };
        public override void OnDrawSpacedGizmo(ArcSphere arcSphere, VisualEffect component)
        {
            Vector3 center = arcSphere.sphere.center;
            float radius = arcSphere.sphere.radius;
            float arc = arcSphere.arc * Mathf.Rad2Deg;


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

            VFXSphereGizmo.DrawSphere(arcSphere.sphere,component,this,m_CenterProperty,m_RadiusProperty);

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
        }
    }
}
