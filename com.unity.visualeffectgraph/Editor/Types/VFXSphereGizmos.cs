#if _WIP_TODOPAUL

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.VFX;

namespace UnityEditor.VFX
{
    [VFXGizmo(typeof(Sphere))]
    class VFXSphereGizmo : VFXSpaceableGizmo<Sphere>
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

        private static readonly Vector3[] s_RadiusDirections = new Vector3[] { Vector3.right, Vector3.up, Vector3.forward };
        private static readonly int[] s_RadiusName = { "VFX_Radius_Right".GetHashCode(), "VFX_Radius_Up".GetHashCode(), "VFX_Radius_Forward".GetHashCode() };

        public static void DrawSphere(Sphere sphere, VFXGizmo gizmo, IProperty<Vector3> centerProperty, IProperty<Vector3> anglesProperty, IProperty<float> radiusProperty)
        {
            gizmo.PositionGizmo(sphere.center, sphere.angles, centerProperty, false);
            gizmo.RotationGizmo(sphere.center, sphere.angles, anglesProperty, false);

            // Radius controls
            if (radiusProperty.isEditable)
            {
                using (new Handles.DrawingScope(Handles.matrix * Matrix4x4.TRS(sphere.center, Quaternion.Euler(sphere.angles), Vector3.one)))
                {
                    for (int i = 0; i < s_RadiusDirections.Length; ++i)
                    {
                        EditorGUI.BeginChangeCheck();
                        var dir = s_RadiusDirections[i];
                        var sliderPos = dir * sphere.radius;
                        var result = Handles.Slider(s_RadiusName[i], sliderPos, dir, handleSize * HandleUtility.GetHandleSize(sliderPos), Handles.CubeHandleCap, 0);

                        if (EditorGUI.EndChangeCheck())
                        {
                            float newRadius = result.magnitude;
                            if (float.IsNaN(newRadius))
                                newRadius = 0;
                            radiusProperty.SetValue(newRadius);
                        }
                    }
                }
            }
        }

        public override void OnDrawSpacedGizmo(Sphere sphere)
        {
            using (new Handles.DrawingScope(Handles.matrix * Matrix4x4.TRS(sphere.center, Quaternion.Euler(sphere.angles), Vector3.one)))
            {
                Handles.DrawWireDisc(Vector3.zero, Vector3.forward, sphere.radius);
                Handles.DrawWireDisc(Vector3.zero, Vector3.up, sphere.radius);
                Handles.DrawWireDisc(Vector3.zero, Vector3.right, sphere.radius);
            }
            DrawSphere(sphere, this, m_CenterProperty, m_AnglesProperty, m_RadiusProperty);
        }

        public override Bounds OnGetSpacedGizmoBounds(Sphere value)
        {
            return new Bounds(value.center, Vector3.one * value.radius);
        }
    }
    [VFXGizmo(typeof(ArcSphere))]
    class VFXArcSphereGizmo : VFXSpaceableGizmo<ArcSphere>
    {
        IProperty<Vector3> m_CenterProperty;
        IProperty<Vector3> m_AnglesProperty;
        IProperty<float> m_RadiusProperty;
        IProperty<float> m_ArcProperty;

        public override void RegisterEditableMembers(IContext context)
        {
            m_CenterProperty = context.RegisterProperty<Vector3>("sphere.center");
            m_RadiusProperty = context.RegisterProperty<float>("sphere.radius");
            m_AnglesProperty = context.RegisterProperty<Vector3>("sphere.angles");
            m_ArcProperty = context.RegisterProperty<float>("arc");
        }

        public override void OnDrawSpacedGizmo(ArcSphere arcSphere)
        {
            Vector3 center = arcSphere.sphere.center;
            float radius = arcSphere.sphere.radius;
            float arc = arcSphere.arc * Mathf.Rad2Deg;

            using (new Handles.DrawingScope(Handles.matrix * Matrix4x4.TRS(arcSphere.sphere.center, Quaternion.Euler(arcSphere.sphere.angles), Vector3.one)))
            {
                // Draw semi-circles at 90 degree angles
                for (int i = 0; i < 4; i++)
                {
                    float currentArc = (float)(i * 90);
                    if (currentArc <= arc)
                        Handles.DrawWireArc(Vector3.zero, Matrix4x4.Rotate(Quaternion.Euler(0.0f, 180.0f, currentArc)) * Vector3.right, Vector3.forward, 180.0f, radius);
                }

                // Draw an extra semi-circle at the arc angle
                if (arcSphere.arc < Mathf.PI * 2.0f)
                    Handles.DrawWireArc(Vector3.zero, Matrix4x4.Rotate(Quaternion.Euler(0.0f, 180.0f, arc)) * Vector3.right, Vector3.forward, 180.0f, radius);

                // Draw 3rd circle around the arc
                Handles.DrawWireArc(Vector3.zero, -Vector3.forward, Vector3.up, arc, radius);

                ArcGizmo(Vector3.zero, radius, arc, m_ArcProperty, Quaternion.Euler(-90.0f, 0.0f, 0.0f));
            }

            VFXSphereGizmo.DrawSphere(arcSphere.sphere, this, m_CenterProperty, m_AnglesProperty, m_RadiusProperty);
        }

        public override Bounds OnGetSpacedGizmoBounds(ArcSphere value)
        {
            return new Bounds(value.sphere.center, Vector3.one * value.sphere.radius);
        }
    }
}
#endif
