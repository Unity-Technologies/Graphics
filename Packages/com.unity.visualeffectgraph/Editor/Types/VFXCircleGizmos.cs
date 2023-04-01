using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.VFX;

namespace UnityEditor.VFX
{
    [VFXGizmo(typeof(TCircle))]
    class VFXCircleGizmo : VFXSpaceableGizmo<TCircle>
    {
        IProperty<Vector3> m_CenterProperty;
        IProperty<Vector3> m_AnglesProperty;
        IProperty<Vector3> m_ScaleProperty;
        IProperty<float> m_RadiusProperty;
        public override void RegisterEditableMembers(IContext context)
        {
            m_CenterProperty = context.RegisterProperty<Vector3>("transform.position");
            m_AnglesProperty = context.RegisterProperty<Vector3>("transform.angles");
            m_ScaleProperty = context.RegisterProperty<Vector3>("transform.scale");
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

        public static void DrawCircle(VFXGizmo gizmo, TCircle circle, IProperty<Vector3> centerProperty, IProperty<Vector3> anglesProperty, IProperty<Vector3> scaleProperty, IProperty<float> radiusProperty, int countVisible = int.MaxValue)
        {
            var radius = circle.radius;

            gizmo.TransformGizmo(
                circle.transform.position,
                circle.transform.angles,
                circle.transform.scale,
                centerProperty,
                anglesProperty,
                scaleProperty);

            if (radiusProperty.isEditable)
            {
                using (new Handles.DrawingScope(Handles.matrix * circle.transform))
                {
                    for (int i = 0; i < countVisible && i < s_RadiusDirections.Length; ++i)
                    {
                        EditorGUI.BeginChangeCheck();
                        var dir = s_RadiusDirections[i];
                        var sliderPos = dir * radius;
                        var result = CustomSlider(gizmo.GetCombinedHashCode(s_RadiusDirectionsName[i]), sliderPos, dir, handleSize * HandleUtility.GetHandleSize(sliderPos));
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

        public override void OnDrawSpacedGizmo(TCircle circle)
        {
            using (new Handles.DrawingScope(Handles.matrix * circle.transform))
            {
                // Draw circle around the arc
                Handles.DrawWireDisc(Vector3.zero, -Vector3.forward, circle.radius);
            }

            DrawCircle(this, circle, m_CenterProperty, m_AnglesProperty, m_ScaleProperty, m_RadiusProperty);
        }

        public static Bounds GetBoundsFromCircle(TCircle value)
        {
            return new Bounds(value.transform.position, value.transform.scale * value.radius);
        }

        public override Bounds OnGetSpacedGizmoBounds(TCircle value)
        {
            return GetBoundsFromCircle(value);
        }
    }
    [VFXGizmo(typeof(TArcCircle))]
    class VFXArcCircleGizmo : VFXSpaceableGizmo<TArcCircle>
    {
        IProperty<Vector3> m_CenterProperty;
        IProperty<Vector3> m_AnglesProperty;
        IProperty<Vector3> m_ScaleProperty;
        IProperty<float> m_RadiusProperty;
        IProperty<float> m_ArcProperty;

        public override void RegisterEditableMembers(IContext context)
        {
            m_CenterProperty = context.RegisterProperty<Vector3>("circle.transform.position");
            m_AnglesProperty = context.RegisterProperty<Vector3>("circle.transform.angles");
            m_ScaleProperty = context.RegisterProperty<Vector3>("circle.transform.scale");
            m_RadiusProperty = context.RegisterProperty<float>("circle.radius");
            m_ArcProperty = context.RegisterProperty<float>("arc");
        }

        public override void OnDrawSpacedGizmo(TArcCircle arcCircle)
        {
            float radius = arcCircle.circle.radius;
            float arc = arcCircle.arc * Mathf.Rad2Deg;

            using (new Handles.DrawingScope(Handles.matrix * arcCircle.circle.transform))
            {
                Handles.DrawWireArc(Vector3.zero, -Vector3.forward, Vector3.up, arc, radius);
                ArcGizmo(Vector3.zero, radius, arc, m_ArcProperty, Quaternion.Euler(-90.0f, 0.0f, 0.0f));
            }
            VFXCircleGizmo.DrawCircle(this, arcCircle.circle, m_CenterProperty, m_AnglesProperty, m_ScaleProperty, m_RadiusProperty, Mathf.CeilToInt(arc / 90));
        }

        public override Bounds OnGetSpacedGizmoBounds(TArcCircle value)
        {
            return VFXCircleGizmo.GetBoundsFromCircle(value.circle);
        }
    }
}
