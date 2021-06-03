#if _WIP_TODOPAUL
using System;

using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.VFX;

namespace UnityEditor.VFX
{
    [VFXGizmo(typeof(Torus))]
    class VFXTorusGizmo : VFXSpaceableGizmo<Torus>
    {
        IProperty<Vector3> m_CenterProperty;
        IProperty<Vector3> m_AnglesProperty;
        IProperty<float> m_ThicknessProperty;
        IProperty<float> m_RadiusProperty;

        public override void RegisterEditableMembers(IContext context)
        {
            m_CenterProperty = context.RegisterProperty<Vector3>("center");
            m_AnglesProperty = context.RegisterProperty<Vector3>("angles");
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

        public static void DrawTorus(Torus torus, VFXGizmo gizmo, IProperty<Vector3> centerProperty, IProperty<Vector3> anglesProperty, IProperty<float> thicknessProperty, IProperty<float> radiusProperty, IEnumerable<float> angles, float maxAngle = Mathf.PI * 2)
        {
            gizmo.PositionGizmo(torus.center, torus.angles, centerProperty, false);
            gizmo.RotationGizmo(torus.center, torus.angles, anglesProperty, false);

            // Radius controls
            using (new Handles.DrawingScope(Handles.matrix * Matrix4x4.TRS(torus.center, Quaternion.Euler(torus.angles), Vector3.one) * Matrix4x4.Rotate(Quaternion.Euler(-90.0f, 0.0f, 0.0f))))
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
                            Vector3 result = Handles.Slider(composedName, sliderPos, distRotated, arcAngle <= maxAngle ? handleSize * HandleUtility.GetHandleSize(sliderPos) : 0, Handles.CubeHandleCap, 0);
                            if (EditorGUI.EndChangeCheck())
                            {
                                float newRadius = (result - capCenter).magnitude;
                                if (float.IsNaN(newRadius))
                                {
                                    newRadius = 0;
                                }
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
                        Vector3 result = Handles.Slider(composedName, sliderPos, distRotated, handleSize * HandleUtility.GetHandleSize(sliderPos), Handles.CubeHandleCap, 0);
                        if (EditorGUI.EndChangeCheck())
                        {
                            float newRadius = (result).magnitude;
                            if (float.IsNaN(newRadius))
                            {
                                newRadius = 0;
                            }
                            radiusProperty.SetValue(newRadius);
                        }
                    }
                }
            }
        }

        public override void OnDrawSpacedGizmo(Torus torus)
        {
            using (new Handles.DrawingScope(Handles.matrix * Matrix4x4.TRS(torus.center, Quaternion.Euler(torus.angles), Vector3.one)))
            {
                Handles.DrawWireDisc(Vector3.forward * torus.minorRadius, Vector3.forward, torus.majorRadius);
                Handles.DrawWireDisc(Vector3.back * torus.minorRadius, Vector3.forward, torus.majorRadius);
                Handles.DrawWireDisc(Vector3.zero, Vector3.forward, torus.majorRadius + torus.minorRadius);
                Handles.DrawWireDisc(Vector3.zero, Vector3.forward, torus.majorRadius - torus.minorRadius);
            }

            DrawTorus(torus, this, m_CenterProperty, m_AnglesProperty, m_ThicknessProperty, m_RadiusProperty, s_Angles);
        }

        public override Bounds OnGetSpacedGizmoBounds(Torus value)
        {
            return new Bounds(value.center, new Vector3(2 * (value.majorRadius + value.minorRadius), 2 * (value.majorRadius + value.minorRadius), 2 * value.minorRadius));
        }
    }
    [VFXGizmo(typeof(ArcTorus))]
    class VFXArcTorusGizmo : VFXSpaceableGizmo<ArcTorus>
    {
        IProperty<Vector3> m_CenterProperty;
        IProperty<Vector3> m_AnglesProperty;
        IProperty<float> m_thicknessProperty;
        IProperty<float> m_radiusProperty;
        IProperty<float> m_ArcProperty;

        public override void RegisterEditableMembers(IContext context)
        {
            m_CenterProperty = context.RegisterProperty<Vector3>("center");
            m_AnglesProperty = context.RegisterProperty<Vector3>("angles");
            m_thicknessProperty = context.RegisterProperty<float>("minorRadius");
            m_radiusProperty = context.RegisterProperty<float>("majorRadius");
            m_ArcProperty = context.RegisterProperty<float>("arc");
        }

        public static readonly Vector3[] radiusDirections = new Vector3[] { Vector3.left, Vector3.up, Vector3.right, Vector3.down };
        public override void OnDrawSpacedGizmo(ArcTorus arcTorus)
        {
            float arc = arcTorus.arc * Mathf.Rad2Deg;
            using (new Handles.DrawingScope(Handles.matrix * Matrix4x4.TRS(arcTorus.center, Quaternion.Euler(arcTorus.angles), Vector3.one)))
            {
                float excessAngle = arc % 360f;
                float angle = Mathf.Abs(arc) >= 360f ? 360f : excessAngle;

                Handles.DrawWireArc(Vector3.forward * arcTorus.minorRadius, Vector3.back, Vector3.up, angle, arcTorus.majorRadius);
                Handles.DrawWireArc(Vector3.back * arcTorus.minorRadius, Vector3.back, Vector3.up, angle, arcTorus.majorRadius);
                Handles.DrawWireArc(Vector3.zero, Vector3.back, Vector3.up, angle, arcTorus.majorRadius + arcTorus.minorRadius);
                Handles.DrawWireArc(Vector3.zero, Vector3.back, Vector3.up, angle, arcTorus.majorRadius - arcTorus.minorRadius);

                ArcGizmo(Vector3.zero, arcTorus.majorRadius, arc, m_ArcProperty, Quaternion.Euler(-90.0f, 0.0f, 0.0f));
            }

            var torus = new Torus() { center = arcTorus.center, angles = arcTorus.angles, minorRadius = arcTorus.minorRadius, majorRadius = arcTorus.majorRadius };
            VFXTorusGizmo.DrawTorus(torus, this, m_CenterProperty, m_AnglesProperty, m_thicknessProperty, m_radiusProperty, VFXTorusGizmo.s_Angles.Concat(new float[] { arc }), arc);
        }

        public override Bounds OnGetSpacedGizmoBounds(ArcTorus value)
        {
            return new Bounds(value.center, new Vector3(2 * (value.majorRadius + value.minorRadius), 2 * (value.majorRadius + value.minorRadius), 2 * value.minorRadius));
        }
    }
}
#endif
