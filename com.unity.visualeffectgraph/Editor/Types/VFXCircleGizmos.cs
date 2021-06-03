using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.VFX;

namespace UnityEditor.VFX
{
    [VFXGizmo(typeof(Circle))]
    class VFXCircleGizmo : VFXSpaceableGizmo<Circle>
    {
        IProperty<Vector3> m_CenterProperty;
        IProperty<float> m_RadiusProperty;
        public override void RegisterEditableMembers(IContext context)
        {
            m_CenterProperty = context.RegisterProperty<Vector3>("center");
            m_RadiusProperty = context.RegisterProperty<float>("radius");
        }

        private static readonly Vector3[] s_RadiusDirections = new Vector3[] { Vector3.up, Vector3.right, Vector3.down, Vector3.left };
        private static readonly int[] s_RadiusDirectionsName = new int[]
        {
            "VFX_Circle_Up".GetHashCode(),
            "VFX_Circle_Right".GetHashCode(),
            "VFX_Circle_Down".GetHashCode(),
            "VFX_Circle_Left".GetHashCode()
        };

        public static void DrawCircle(VFXGizmo gizmo, IProperty<Vector3> centerProperty, IProperty<Vector3> anglesProperty, IProperty<float> radiusProperty, int countVisible = int.MaxValue)
        {
            var center = centerProperty != null ? centerProperty.GetValue() : Vector3.zero;
            var angles = anglesProperty != null ? anglesProperty.GetValue() : Vector3.zero;
            var radius = radiusProperty != null ? radiusProperty.GetValue() : 0.0f;

            gizmo.PositionGizmo(center, angles, centerProperty, false);
            gizmo.RotationGizmo(center, angles, anglesProperty, false);

            if (radiusProperty.isEditable)
            {
                using (new Handles.DrawingScope(Handles.matrix * Matrix4x4.TRS(center, Quaternion.Euler(angles), Vector3.one)))
                {
                    for (int i = 0; i < countVisible; ++i)
                    {
                        EditorGUI.BeginChangeCheck();
                        var dir = s_RadiusDirections[i];
                        var sliderPos = dir * radius;
                        var result = Handles.Slider(s_RadiusDirectionsName[i], sliderPos, dir, handleSize * HandleUtility.GetHandleSize(sliderPos), Handles.CubeHandleCap, 0.0f);
                        if (EditorGUI.EndChangeCheck())
                        {
                            radius = result.magnitude;
                            if (float.IsNaN(radius))
                                radius = 0;

                            radiusProperty.SetValue(radius);
                        }
                    }
                }
            }
        }

        public override void OnDrawSpacedGizmo(Circle circle)
        {
            using (new Handles.DrawingScope(Handles.matrix * Matrix4x4.TRS(circle.center, Quaternion.identity, Vector3.one)))
            {
                // Draw circle around the arc
                Handles.DrawWireDisc(Vector3.zero, -Vector3.forward, circle.radius);
            }

            DrawCircle(this, m_CenterProperty, null, m_RadiusProperty);
        }

        public override Bounds OnGetSpacedGizmoBounds(Circle value)
        {
            return new Bounds(value.center, new Vector3(value.radius, value.radius, value.radius / 100.0f)); //TODO take orientation in account
        }
    }
    [VFXGizmo(typeof(ArcCircle))]
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

        public override void OnDrawSpacedGizmo(ArcCircle arcCircle)
        {
            Vector3 center = arcCircle.circle.center;
            float radius = arcCircle.circle.radius;
            float arc = arcCircle.arc * Mathf.Rad2Deg;

            using (new Handles.DrawingScope(Handles.matrix * Matrix4x4.TRS(arcCircle.circle.center, Quaternion.identity, Vector3.one)))
            {
                Handles.DrawWireArc(Vector3.zero, -Vector3.forward, Vector3.up, arc, radius);
                ArcGizmo(Vector3.zero, radius, arc, m_ArcProperty, Quaternion.Euler(-90.0f, 0.0f, 0.0f));
            }

            VFXCircleGizmo.DrawCircle(this, m_CenterProperty, null, m_RadiusProperty, Mathf.CeilToInt(arc / 90));
        }

        public override Bounds OnGetSpacedGizmoBounds(ArcCircle value)
        {
            return new Bounds(value.circle.center, new Vector3(value.circle.radius, value.circle.radius, value.circle.radius / 100.0f)); //TODO take orientation in account
        }
    }
}
