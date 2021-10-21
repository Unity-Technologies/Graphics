using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.VFX;

namespace UnityEditor.VFX
{
    [VFXGizmo(typeof(TTorus))]
    class VFXTorusGizmo : VFXSpaceableGizmo<TTorus>
    {
        IProperty<Vector3> m_CenterProperty;
        IProperty<Vector3> m_AnglesProperty;
        IProperty<Vector3> m_ScaleProperty;
        IProperty<float> m_ThicknessProperty;
        IProperty<float> m_RadiusProperty;

        public override void RegisterEditableMembers(IContext context)
        {
            m_CenterProperty = context.RegisterProperty<Vector3>("transform.position");
            m_AnglesProperty = context.RegisterProperty<Vector3>("transform.angles");
            m_ScaleProperty = context.RegisterProperty<Vector3>("transform.scale");
            m_ThicknessProperty = context.RegisterProperty<float>("minorRadius");
            m_RadiusProperty = context.RegisterProperty<float>("majorRadius");
        }

        private static readonly Vector3[] s_RadiusDirections = new Vector3[] { Vector3.left, Vector3.up, Vector3.right, Vector3.down };
        public static readonly float[] s_Angles = new float[] { 0.0f, 90.0f, 180.0f, 270.0f };

        private static readonly int[] s_RadiusDirectionsNames = new int[]
        {
            "VFX_Torus_Left".GetHashCode(),
            "VFX_Torus_Up".GetHashCode(),
            "VFX_Torus_Right".GetHashCode(),
            "VFX_Torus_Down".GetHashCode(),
        };

        private static readonly int[] s_AngleNames = new int[]
        {
            "VFX_Torus_Angle_0".GetHashCode(),
            "VFX_Torus_Angle_90".GetHashCode(),
            "VFX_Torus_Angle_180".GetHashCode(),
            "VFX_Torus_Angle_270".GetHashCode(),
            "VFX_Torus_Angle_Extra".GetHashCode()
        };

        private static readonly int s_MajorRadius = "VFX_Torus_MajorRadius".GetHashCode();

        public static void DrawTorus(VFXGizmo gizmo, TTorus torus, IProperty<Vector3> centerProperty, IProperty<Vector3> anglesProperty, IProperty<Vector3> scaleProperty, IProperty<float> thicknessProperty, IProperty<float> radiusProperty, IEnumerable<float> angles, float maxAngle = Mathf.PI * 2)
        {
            gizmo.PositionGizmo(torus.transform.position, torus.transform.angles, centerProperty, false);
            gizmo.RotationGizmo(torus.transform.position, torus.transform.angles, anglesProperty, false);
            gizmo.ScaleGizmo(torus.transform.position, torus.transform.angles, torus.transform.scale, scaleProperty, false);

            // Radius controls
            using (new Handles.DrawingScope(Handles.matrix * torus.transform * Matrix4x4.Rotate(Quaternion.Euler(-90.0f, 0.0f, 0.0f))))
            {
                for (int i = 0; i < angles.Count(); ++i)
                {
                    var arcAngle = angles.ElementAt(i);
                    if (arcAngle > maxAngle)
                        continue;

                    var angleName = s_AngleNames[i];
                    var arcRotation = Quaternion.AngleAxis(arcAngle, Vector3.up);
                    var capCenter = arcRotation * Vector3.forward * torus.majorRadius;
                    Handles.DrawWireDisc(capCenter, arcRotation * Vector3.right, torus.minorRadius);

                    if (thicknessProperty.isEditable)
                    {
                        // Minor radius
                        for (int j = 0; j < s_RadiusDirections.Length; ++j)
                        {
                            var composedName = (angleName * 397) ^ s_RadiusDirectionsNames[j];
                            Vector3 distRotated = Matrix4x4.Rotate(Quaternion.Euler(0.0f, arcAngle + 90.0f, 0.0f)) * s_RadiusDirections[j];
                            Vector3 sliderPos = capCenter + distRotated * torus.minorRadius;

                            EditorGUI.BeginChangeCheck();
                            var result = Handles.Slider(composedName, sliderPos, distRotated, arcAngle <= maxAngle ? handleSize * HandleUtility.GetHandleSize(sliderPos) : 0, CustomCubeHandleCap, 0);
                            if (EditorGUI.EndChangeCheck())
                            {
                                var newRadius = (result - capCenter).magnitude;
                                if (float.IsNaN(newRadius))
                                    newRadius = 0;

                                thicknessProperty.SetValue(newRadius);
                            }
                        }
                    }

                    if (radiusProperty.isEditable)
                    {
                        // Major radius
                        var composedName = (angleName * 397) ^ s_MajorRadius;

                        Vector3 distRotated = Matrix4x4.Rotate(Quaternion.Euler(0.0f, arcAngle + 90.0f, 0.0f)) * Vector3.left;
                        Vector3 sliderPos = distRotated * torus.majorRadius;

                        EditorGUI.BeginChangeCheck();
                        Vector3 result = Handles.Slider(composedName, sliderPos, distRotated, handleSize * HandleUtility.GetHandleSize(sliderPos), CustomCubeHandleCap, 0);
                        if (EditorGUI.EndChangeCheck())
                        {
                            float newRadius = (result).magnitude;
                            if (float.IsNaN(newRadius))
                                newRadius = 0;
                            radiusProperty.SetValue(newRadius);
                        }
                    }
                }
            }
        }

        public override void OnDrawSpacedGizmo(TTorus torus)
        {
            using (new Handles.DrawingScope(Handles.matrix * torus.transform))
            {
                Handles.DrawWireDisc(Vector3.forward * torus.minorRadius, Vector3.forward, torus.majorRadius);
                Handles.DrawWireDisc(Vector3.back * torus.minorRadius, Vector3.forward, torus.majorRadius);
                Handles.DrawWireDisc(Vector3.zero, Vector3.forward, torus.majorRadius + torus.minorRadius);
                Handles.DrawWireDisc(Vector3.zero, Vector3.forward, torus.majorRadius - torus.minorRadius);
            }

            DrawTorus(this, torus, m_CenterProperty, m_AnglesProperty, m_ScaleProperty, m_ThicknessProperty, m_RadiusProperty, s_Angles);
        }

        static public Bounds GetBoundsFromTorus(TTorus torus)
        {
            var scale = new Vector3(2 * (torus.majorRadius + torus.minorRadius), 2 * (torus.majorRadius + torus.minorRadius), 2 * torus.minorRadius);
            scale.x *= torus.transform.scale.x;
            scale.y *= torus.transform.scale.y;
            scale.z *= torus.transform.scale.z;
            return new Bounds(torus.transform.position, scale);
        }

        public override Bounds OnGetSpacedGizmoBounds(TTorus value)
        {
            return GetBoundsFromTorus(value);
        }
    }
    [VFXGizmo(typeof(TArcTorus))]
    class VFXArcTorusGizmo : VFXSpaceableGizmo<TArcTorus>
    {
        IProperty<Vector3> m_CenterProperty;
        IProperty<Vector3> m_AnglesProperty;
        IProperty<Vector3> m_ScaleProperty;
        IProperty<float> m_ThicknessProperty;
        IProperty<float> m_RadiusProperty;
        IProperty<float> m_ArcProperty;

        public override void RegisterEditableMembers(IContext context)
        {
            m_CenterProperty = context.RegisterProperty<Vector3>("torus.transform.position");
            m_AnglesProperty = context.RegisterProperty<Vector3>("torus.transform.angles");
            m_ScaleProperty = context.RegisterProperty<Vector3>("torus.transform.scale");
            m_ThicknessProperty = context.RegisterProperty<float>("torus.minorRadius");
            m_RadiusProperty = context.RegisterProperty<float>("torus.majorRadius");
            m_ArcProperty = context.RegisterProperty<float>("arc");
        }

        public static readonly Vector3[] radiusDirections = new Vector3[] { Vector3.left, Vector3.up, Vector3.right, Vector3.down };
        public override void OnDrawSpacedGizmo(TArcTorus arcTorus)
        {
            var arc = arcTorus.arc * Mathf.Rad2Deg;
            using (new Handles.DrawingScope(Handles.matrix * arcTorus.torus.transform))
            {
                var excessAngle = arc % 360f;
                var angle = Mathf.Abs(arc) >= 360f ? 360f : excessAngle;

                Handles.DrawWireArc(Vector3.forward * arcTorus.torus.minorRadius, Vector3.back, Vector3.up, angle, arcTorus.torus.majorRadius);
                Handles.DrawWireArc(Vector3.back * arcTorus.torus.minorRadius, Vector3.back, Vector3.up, angle, arcTorus.torus.majorRadius);
                Handles.DrawWireArc(Vector3.zero, Vector3.back, Vector3.up, angle, arcTorus.torus.majorRadius + arcTorus.torus.minorRadius);
                Handles.DrawWireArc(Vector3.zero, Vector3.back, Vector3.up, angle, arcTorus.torus.majorRadius - arcTorus.torus.minorRadius);

                ArcGizmo(Vector3.zero, arcTorus.torus.majorRadius, arc, m_ArcProperty, Quaternion.Euler(-90.0f, 0.0f, 0.0f));
            }

            VFXTorusGizmo.DrawTorus(this, arcTorus.torus, m_CenterProperty, m_AnglesProperty, m_ScaleProperty, m_ThicknessProperty, m_RadiusProperty, VFXTorusGizmo.s_Angles.Concat(new float[] { arc }), arc); ;
        }

        public override Bounds OnGetSpacedGizmoBounds(TArcTorus value)
        {
            return VFXTorusGizmo.GetBoundsFromTorus(value.torus);
        }
    }
}
