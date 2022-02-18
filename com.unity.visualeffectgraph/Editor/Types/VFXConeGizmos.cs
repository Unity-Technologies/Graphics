using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.VFX;

namespace UnityEditor.VFX
{
    [VFXGizmo(typeof(TCone))]
    class VFXConeGizmo : VFXSpaceableGizmo<TCone>
    {
        IProperty<Vector3> m_CenterProperty;
        IProperty<Vector3> m_AnglesProperty;
        IProperty<Vector3> m_ScaleProperty;
        IProperty<float> m_BaseRadiusProperty;
        IProperty<float> m_TopRadiusProperty;
        IProperty<float> m_HeightProperty;

        public override void RegisterEditableMembers(IContext context)
        {
            m_CenterProperty = context.RegisterProperty<Vector3>("transform.position");
            m_AnglesProperty = context.RegisterProperty<Vector3>("transform.angles");
            m_ScaleProperty = context.RegisterProperty<Vector3>("transform.scale");
            m_BaseRadiusProperty = context.RegisterProperty<float>("baseRadius");
            m_TopRadiusProperty = context.RegisterProperty<float>("topRadius");
            m_HeightProperty = context.RegisterProperty<float>("height");
        }

        float topRadiusScreen;
        float baseRadiusScreen;
        bool m_Dragging;

        public struct Extremities
        {
            public void Build(float baseRadius, float topRadius, float height)
            {
                topCap = height * Vector3.up;
                bottomCap = Vector3.zero;

                if (extremities == null)
                    extremities = new List<Vector3>(8);
                extremities.Clear();

                extremities.Add(topCap + Vector3.forward * topRadius);
                extremities.Add(topCap - Vector3.forward * topRadius);

                extremities.Add(topCap + Vector3.left * topRadius);
                extremities.Add(topCap - Vector3.left * topRadius);

                extremities.Add(bottomCap + Vector3.forward * baseRadius);
                extremities.Add(bottomCap - Vector3.forward * baseRadius);

                extremities.Add(bottomCap + Vector3.left * baseRadius);
                extremities.Add(bottomCap - Vector3.left * baseRadius);

                visibleCount = 4;
            }

            public void Build(float baseRadius, float topRadius, float height, float degArc)
            {
                topCap = height * Vector3.up;
                bottomCap = Vector3.zero;
                int count = 4;

                visibleCount = Mathf.CeilToInt(degArc / 90);
                if (visibleCount <= 0)
                {
                    visibleCount = 1;
                }

                if (extremities == null)
                    extremities = new List<Vector3>(8);
                extremities.Clear();

                extremities.Add(topCap + Vector3.forward * topRadius);
                if (count > 1)
                {
                    extremities.Add(topCap - Vector3.left * topRadius);
                    if (count > 2)
                    {
                        extremities.Add(topCap - Vector3.forward * topRadius);
                        if (count > 3)
                        {
                            extremities.Add(topCap + Vector3.left * topRadius);
                        }
                    }
                }
                extremities.Add(bottomCap + Vector3.forward * baseRadius);
                if (count > 1)
                {
                    extremities.Add(bottomCap - Vector3.left * baseRadius);
                    if (count > 2)
                    {
                        extremities.Add(bottomCap - Vector3.forward * baseRadius);
                        if (count > 3)
                        {
                            extremities.Add(bottomCap + Vector3.left * baseRadius);
                        }
                    }
                }
            }

            public Vector3 topCap;
            public Vector3 bottomCap;
            public List<Vector3> extremities;
            public int visibleCount;
        }

        private static readonly int[] s_ExtremitiesNames =
        {
            "VFX_DrawCone_Cap_0".GetHashCode(),
            "VFX_DrawCone_Cap_1".GetHashCode(),
            "VFX_DrawCone_Cap_2".GetHashCode(),
            "VFX_DrawCone_Cap_3".GetHashCode(),
            "VFX_DrawCone_Cap_4".GetHashCode(),
            "VFX_DrawCone_Cap_5".GetHashCode(),
            "VFX_DrawCone_Cap_6".GetHashCode(),
            "VFX_DrawCone_Cap_7".GetHashCode(),
        };

        private static readonly int s_HeightCapName = "VFX_DrawCone_HeightCap".GetHashCode();

        public static void DrawCone(VFXGizmo gizmo, TCone cone, ref Extremities extremities, IProperty<Vector3> centerProperty, IProperty<Vector3> anglesProperty, IProperty<Vector3> scaleProperty, IProperty<float> baseRadiusProperty, IProperty<float> topRadiusProperty, IProperty<float> heightProperty, float baseRadiusScreen, float topRadiusScreen)
        {
            var center = cone.transform.position;
            var scale = cone.transform.scale;
            var angles = cone.transform.angles;
            var baseRadius = cone.baseRadius;
            var topRadius = cone.topRadius;
            var height = cone.height;

            gizmo.PositionGizmo(center, angles, centerProperty, false);
            gizmo.RotationGizmo(center, angles, anglesProperty, false);
            gizmo.ScaleGizmo(center, angles, scale, scaleProperty, false);

            using (new Handles.DrawingScope(Handles.matrix * cone.transform))
            {
                if (baseRadiusScreen > 2 && baseRadiusProperty.isEditable)
                {
                    for (int i = extremities.extremities.Count / 2; i < extremities.extremities.Count && (i - extremities.extremities.Count / 2) < extremities.visibleCount; ++i)
                    {
                        EditorGUI.BeginChangeCheck();
                        var pos = extremities.extremities[i];
                        var result = Handles.Slider(s_ExtremitiesNames[i], pos, pos - extremities.bottomCap, handleSize * HandleUtility.GetHandleSize(pos), CustomCubeHandleCap, 0);
                        if (EditorGUI.EndChangeCheck())
                        {
                            baseRadiusProperty.SetValue(result.magnitude);
                        }
                    }
                }

                if (topRadiusScreen > 2 && topRadiusProperty.isEditable)
                {
                    for (int i = 0; i < extremities.extremities.Count / 2 && i < extremities.visibleCount; ++i)
                    {
                        EditorGUI.BeginChangeCheck();

                        var pos = extremities.extremities[i];
                        var dir = pos - extremities.topCap;
                        var result = Handles.Slider(s_ExtremitiesNames[i], pos, dir, handleSize * HandleUtility.GetHandleSize(pos), CustomCubeHandleCap, 0);

                        if (EditorGUI.EndChangeCheck())
                            topRadiusProperty.SetValue((result - extremities.topCap).magnitude);
                    }
                }

                if (heightProperty.isEditable)
                {
                    EditorGUI.BeginChangeCheck();
                    var result = Handles.Slider(s_HeightCapName, extremities.topCap, Vector3.up, handleSize * HandleUtility.GetHandleSize(extremities.topCap), CustomCubeHandleCap, 0);
                    if (EditorGUI.EndChangeCheck())
                        heightProperty.SetValue(result.magnitude);
                }
            }
        }

        Extremities extremities;
        public override void OnDrawSpacedGizmo(TCone cone)
        {
            extremities.Build(cone.baseRadius, cone.topRadius, cone.height);

            if (Event.current != null && Event.current.type == EventType.MouseDown)
            {
                m_Dragging = true;
            }
            if (Event.current != null && Event.current.type == EventType.MouseUp)
            {
                m_Dragging = false;
            }

            if (!m_Dragging)
            {
                topRadiusScreen = (HandleUtility.WorldToGUIPoint(extremities.topCap) - HandleUtility.WorldToGUIPoint(extremities.topCap + Vector3.forward * cone.topRadius)).magnitude;
                baseRadiusScreen = (HandleUtility.WorldToGUIPoint(extremities.bottomCap) - HandleUtility.WorldToGUIPoint(extremities.bottomCap + Vector3.forward * cone.baseRadius)).magnitude;
            }

            using (new Handles.DrawingScope(Handles.matrix * cone.transform))
            {
                Handles.DrawWireDisc(extremities.topCap, Vector3.up, cone.topRadius);
                Handles.DrawWireDisc(extremities.bottomCap, Vector3.up, cone.baseRadius);

                for (int i = 0; i < extremities.extremities.Count / 2; ++i)
                {
                    Handles.DrawLine(extremities.extremities[i], extremities.extremities[i + extremities.extremities.Count / 2]);
                }
            }

            DrawCone(this, cone, ref extremities, m_CenterProperty, m_AnglesProperty, m_ScaleProperty, m_BaseRadiusProperty, m_TopRadiusProperty, m_HeightProperty, baseRadiusScreen, topRadiusScreen);
        }

        static public Bounds GetBoundsFromCone(TCone cone)
        {
            var baseScale = new Vector3(Mathf.Max(cone.baseRadius, cone.topRadius), Mathf.Max(cone.baseRadius, cone.topRadius), cone.height);
            baseScale.x *= cone.transform.scale.x;
            baseScale.y *= cone.transform.scale.y;
            baseScale.z *= cone.transform.scale.z;
            return new Bounds(cone.transform.position, baseScale);
        }

        public override Bounds OnGetSpacedGizmoBounds(TCone value)
        {
            return GetBoundsFromCone(value);
        }
    }


    [VFXGizmo(typeof(TArcCone))]
    class VFXTArcConeGizmo : VFXSpaceableGizmo<TArcCone>
    {
        IProperty<Vector3> m_CenterProperty;
        IProperty<Vector3> m_AnglesProperty;
        IProperty<Vector3> m_ScaleProperty;
        IProperty<float> m_baseRadiusProperty;
        IProperty<float> m_topRadiusProperty;
        IProperty<float> m_HeightProperty;
        IProperty<float> m_ArcProperty;

        public override void RegisterEditableMembers(IContext context)
        {
            m_CenterProperty = context.RegisterProperty<Vector3>("cone.transform.position");
            m_AnglesProperty = context.RegisterProperty<Vector3>("cone.transform.angles");
            m_ScaleProperty = context.RegisterProperty<Vector3>("cone.transform.scale");
            m_baseRadiusProperty = context.RegisterProperty<float>("cone.baseRadius");
            m_topRadiusProperty = context.RegisterProperty<float>("cone.topRadius");
            m_HeightProperty = context.RegisterProperty<float>("cone.height");
            m_ArcProperty = context.RegisterProperty<float>("arc");
        }

        VFXConeGizmo.Extremities extremities;
        bool m_Dragging;
        float topRadiusScreen;
        float baseRadiusScreen;

        public override void OnDrawSpacedGizmo(TArcCone arcCone)
        {
            var arc = arcCone.arc * Mathf.Rad2Deg;
            extremities.Build(arcCone.cone.baseRadius, arcCone.cone.topRadius, arcCone.cone.height, arc);
            var arcDirection = Quaternion.AngleAxis(arc, Vector3.up) * Vector3.forward;
            if (Event.current != null && Event.current.type == EventType.MouseDown)
            {
                m_Dragging = true;
            }
            if (Event.current != null && Event.current.type == EventType.MouseUp)
            {
                m_Dragging = false;
            }

            if (!m_Dragging)
            {
                topRadiusScreen = (HandleUtility.WorldToGUIPoint(extremities.topCap) - HandleUtility.WorldToGUIPoint(extremities.topCap + Vector3.forward * arcCone.cone.topRadius)).magnitude;
                baseRadiusScreen = (HandleUtility.WorldToGUIPoint(extremities.bottomCap) - HandleUtility.WorldToGUIPoint(extremities.bottomCap + Vector3.forward * arcCone.cone.baseRadius)).magnitude;
            }

            using (new Handles.DrawingScope(Handles.matrix * arcCone.cone.transform))
            {
                if (topRadiusScreen > 2)
                    Handles.DrawWireArc(extremities.topCap, Vector3.up, Vector3.forward, arc, arcCone.cone.topRadius);

                if (baseRadiusScreen > 2)
                    Handles.DrawWireArc(extremities.bottomCap, Vector3.up, Vector3.forward, arc, arcCone.cone.baseRadius);

                for (int i = 0; i < extremities.extremities.Count / 2 && i < extremities.visibleCount; ++i)
                {
                    Handles.DrawLine(extremities.extremities[i], extremities.extremities[i + extremities.extremities.Count / 2]);
                }

                Handles.DrawLine(extremities.topCap, extremities.extremities[0]);
                Handles.DrawLine(extremities.bottomCap, extremities.extremities[extremities.extremities.Count / 2]);

                Handles.DrawLine(extremities.topCap, extremities.topCap + arcDirection * arcCone.cone.topRadius);
                Handles.DrawLine(extremities.bottomCap, arcDirection * arcCone.cone.baseRadius);

                Handles.DrawLine(arcDirection * arcCone.cone.baseRadius, extremities.topCap + arcDirection * arcCone.cone.topRadius);
                var radius = arcCone.cone.baseRadius > arcCone.cone.topRadius ? arcCone.cone.baseRadius : arcCone.cone.topRadius;
                var center = arcCone.cone.baseRadius > arcCone.cone.topRadius ? Vector3.zero : extremities.topCap;

                if (radius != 0)
                    ArcGizmo(center, radius, arc, m_ArcProperty, Quaternion.identity);
            }

            VFXConeGizmo.DrawCone(this, arcCone.cone, ref extremities, m_CenterProperty, m_AnglesProperty, m_ScaleProperty, m_baseRadiusProperty, m_topRadiusProperty, m_HeightProperty, baseRadiusScreen, topRadiusScreen);
        }

        public override Bounds OnGetSpacedGizmoBounds(TArcCone value)
        {
            return VFXConeGizmo.GetBoundsFromCone(value.cone);
        }
    }
}
