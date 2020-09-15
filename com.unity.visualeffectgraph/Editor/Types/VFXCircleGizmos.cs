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
        IProperty<Vector3> m_AnglesProperty;
        IProperty<float> m_RadiusProperty;
        public override void RegisterEditableMembers(IContext context)
        {
            m_CenterProperty = context.RegisterProperty<Vector3>("center");
            m_AnglesProperty = context.RegisterProperty<Vector3>("angles");
            m_RadiusProperty = context.RegisterProperty<float>("radius");
        }

        public static readonly Vector3[] radiusDirections = new Vector3[] { Vector3.up, Vector3.right, Vector3.down, Vector3.left };

        public static void DrawCircle(Circle circle, VFXGizmo gizmo, IProperty<Vector3> centerProperty, IProperty<Vector3> anglesProperty, IProperty<float> radiusProperty, IEnumerable<Vector3> radiusDirections, int countVisible = int.MaxValue)
        {
            gizmo.PositionGizmo(circle.center, centerProperty, false);
            gizmo.RotationGizmo(circle.center, circle.angles, anglesProperty, false);

            // Radius controls
            if (radiusProperty.isEditable)
            {
                using (new Handles.DrawingScope(Handles.matrix * Matrix4x4.TRS(circle.center, Quaternion.Euler(circle.angles), Vector3.one)))
                {
                    int cpt = 0;
                    foreach (var dist in radiusDirections)
                    {
                        EditorGUI.BeginChangeCheck();
                        Vector3 sliderPos = dist * circle.radius;
                        Vector3 result = Handles.Slider(sliderPos, dist, cpt < countVisible ? (handleSize * HandleUtility.GetHandleSize(sliderPos)) : 0, Handles.CubeHandleCap, 0);

                        ++cpt;
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
        }

        public override void OnDrawSpacedGizmo(Circle circle)
        {
            using (new Handles.DrawingScope(Handles.matrix * Matrix4x4.TRS(circle.center, Quaternion.Euler(circle.angles), Vector3.one)))
            {
                // Draw circle around the arc
                Handles.DrawWireDisc(Vector3.zero, -Vector3.forward, circle.radius);
            }

            DrawCircle(circle, this, m_CenterProperty, m_AnglesProperty, m_RadiusProperty, radiusDirections);
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
        IProperty<Vector3> m_AnglesProperty;
        IProperty<float> m_RadiusProperty;
        IProperty<float> m_ArcProperty;

        public override void RegisterEditableMembers(IContext context)
        {
            m_CenterProperty = context.RegisterProperty<Vector3>("circle.center");
            m_AnglesProperty = context.RegisterProperty<Vector3>("circle.angles");
            m_RadiusProperty = context.RegisterProperty<float>("circle.radius");
            m_ArcProperty = context.RegisterProperty<float>("arc");
        }

        public override void OnDrawSpacedGizmo(ArcCircle arcCircle)
        {
            Vector3 center = arcCircle.circle.center;
            float radius = arcCircle.circle.radius;
            float arc = arcCircle.arc * Mathf.Rad2Deg;

            using (new Handles.DrawingScope(Handles.matrix * Matrix4x4.TRS(arcCircle.circle.center, Quaternion.Euler(arcCircle.circle.angles), Vector3.one)))
            {
                Handles.DrawWireArc(Vector3.zero, -Vector3.forward, Vector3.up, arc, radius);
                ArcGizmo(Vector3.zero, radius, arc, m_ArcProperty, Quaternion.Euler(-90.0f, 0.0f, 0.0f), true);
            }

            VFXCircleGizmo.DrawCircle(arcCircle.circle, this, m_CenterProperty, m_AnglesProperty, m_RadiusProperty, VFXCircleGizmo.radiusDirections, Mathf.CeilToInt(arc / 90));
        }

        public override Bounds OnGetSpacedGizmoBounds(ArcCircle value)
        {
            return new Bounds(value.circle.center, new Vector3(value.circle.radius, value.circle.radius, value.circle.radius / 100.0f)); //TODO take orientation in account
        }
    }
}
