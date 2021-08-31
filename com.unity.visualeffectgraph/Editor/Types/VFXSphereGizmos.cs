using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.VFX;

namespace UnityEditor.VFX
{
    [VFXGizmo(typeof(TSphere))]
    class VFXTSphereGizmo : VFXSpaceableGizmo<TSphere>
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

        private static readonly Vector3[] s_RadiusDirections = new Vector3[] { Vector3.right, Vector3.up, Vector3.forward };
        private static readonly int[] s_RadiusName = { "VFX_Radius_Right".GetHashCode(), "VFX_Radius_Up".GetHashCode(), "VFX_Radius_Forward".GetHashCode() };

        public static void DrawSphere(VFXGizmo gizmo, TSphere sphere, IProperty<Vector3> centerProperty, IProperty<Vector3> anglesProperty, IProperty<Vector3> scaleProperty, IProperty<float> radiusProperty)
        {
            gizmo.PositionGizmo(sphere.transform.position, sphere.transform.angles, centerProperty, false);
            gizmo.RotationGizmo(sphere.transform.position, sphere.transform.angles, anglesProperty, false);
            gizmo.ScaleGizmo(sphere.transform.position, sphere.transform.angles, sphere.transform.scale, scaleProperty, false);

            // Radius controls
            if (radiusProperty.isEditable)
            {
                using (new Handles.DrawingScope(Handles.matrix * sphere.transform))
                {
                    for (int i = 0; i < s_RadiusDirections.Length; ++i)
                    {
                        EditorGUI.BeginChangeCheck();
                        var dir = s_RadiusDirections[i];
                        var sliderPos = dir * sphere.radius;
                        var result = Handles.Slider(s_RadiusName[i], sliderPos, dir, handleSize * HandleUtility.GetHandleSize(sliderPos), CustomCubeHandleCap, 0);

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

        static public void DrawSpaceSphere(VFXGizmo gizmo, TSphere sphere, IProperty<Vector3> centerProperty, IProperty<Vector3> anglesProperty, IProperty<Vector3> scaleProperty, IProperty<float> radiusProperty)
        {
            using (new Handles.DrawingScope(Handles.matrix * sphere.transform))
            {
                Handles.DrawWireDisc(Vector3.zero, Vector3.forward, sphere.radius);
                Handles.DrawWireDisc(Vector3.zero, Vector3.up, sphere.radius);
                Handles.DrawWireDisc(Vector3.zero, Vector3.right, sphere.radius);
            }
            DrawSphere(gizmo, sphere, centerProperty, anglesProperty, scaleProperty, radiusProperty);
        }

        public override void OnDrawSpacedGizmo(TSphere sphere)
        {
            DrawSpaceSphere(this, sphere, m_CenterProperty, m_AnglesProperty, m_ScaleProperty, m_RadiusProperty);
        }

        static public Bounds GetBoundsFromSphere(TSphere sphere)
        {
            return new Bounds(sphere.transform.position, sphere.transform.scale * sphere.radius);
        }

        public override Bounds OnGetSpacedGizmoBounds(TSphere value)
        {
            return GetBoundsFromSphere(value);
        }
    }
    [VFXGizmo(typeof(TArcSphere))]
    class VFXArcSphereGizmo : VFXSpaceableGizmo<TArcSphere>
    {
        IProperty<Vector3> m_CenterProperty;
        IProperty<Vector3> m_AnglesProperty;
        IProperty<Vector3> m_ScaleProperty;
        IProperty<float> m_RadiusProperty;
        IProperty<float> m_ArcProperty;

        public override void RegisterEditableMembers(IContext context)
        {
            m_CenterProperty = context.RegisterProperty<Vector3>("sphere.transform.position");
            m_AnglesProperty = context.RegisterProperty<Vector3>("sphere.transform.angles");
            m_ScaleProperty = context.RegisterProperty<Vector3>("sphere.transform.scale");
            m_RadiusProperty = context.RegisterProperty<float>("sphere.radius");
            m_ArcProperty = context.RegisterProperty<float>("arc");
        }

        public override void OnDrawSpacedGizmo(TArcSphere arcSphere)
        {
            float radius = arcSphere.sphere.radius;
            float arc = arcSphere.arc * Mathf.Rad2Deg;

            using (new Handles.DrawingScope(Handles.matrix * arcSphere.sphere.transform))
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

            VFXTSphereGizmo.DrawSphere(this, arcSphere.sphere, m_CenterProperty, m_AnglesProperty, m_ScaleProperty, m_RadiusProperty);
        }

        public override Bounds OnGetSpacedGizmoBounds(TArcSphere value)
        {
            return VFXTSphereGizmo.GetBoundsFromSphere(value.sphere);
        }
    }

    [VFXGizmo(typeof(Sphere))]
    class VFXSphereGizmo : VFXSpaceableGizmo<Sphere>
    {
        IProperty<Vector3> m_CenterProperty;
        IProperty<float> m_RadiusProperty;

        public override void RegisterEditableMembers(IContext context)
        {
            m_CenterProperty = context.RegisterProperty<Vector3>("center");
            m_RadiusProperty = context.RegisterProperty<float>("radius");
        }

        public override void OnDrawSpacedGizmo(Sphere sphere)
        {
            var tSphere = (TSphere)sphere;
            VFXTSphereGizmo.DrawSpaceSphere(this, tSphere, m_CenterProperty, null, null, m_RadiusProperty);
        }

        public override Bounds OnGetSpacedGizmoBounds(Sphere value)
        {
            var tSphere = (TSphere)value;
            return VFXTSphereGizmo.GetBoundsFromSphere(tSphere);
        }
    }
}
