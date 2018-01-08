using System;
using UnityEditor.AnimatedValues;
using UnityEngine;
using UnityEngine.Experimental.Rendering.HDPipeline;
using Object = UnityEngine.Object;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    using _ = CoreEditorUtils;
    using CED = CoreEditorDrawer<InfluenceVolumeUI, SerializedInfluenceVolume>;

    class InfluenceVolumeUI : BaseUI<SerializedInfluenceVolume>
    {
        static readonly int k_ShapeCount = Enum.GetValues(typeof(ShapeType)).Length;

        public static readonly CED.IDrawer SectionShape;

        public static readonly CED.IDrawer SectionShapeBox = CED.Action(Drawer_SectionShapeBox);
        public static readonly CED.IDrawer SectionShapeSphere = CED.Action(Drawer_SectionShapeSphere);

        static InfluenceVolumeUI()
        {
            SectionShape = CED.Group(
                CED.Action(Drawer_FieldShapeType),
                CED.FadeGroup(
                    (s, d, o, i) => s.IsSectionExpanded_Shape((ShapeType)i),
                    true,
                    SectionShapeBox,
                    SectionShapeSphere
                )
            );
        }

        public InfluenceVolumeUI()
            : base(k_ShapeCount)
        {

        }

        public override void Update()
        {
            base.Update();
            SetIsSectionExpanded_Shape((ShapeType)data.shapeType.intValue);
        }

        void SetIsSectionExpanded_Shape(ShapeType shape)
        {
            for (var i = 0; i < k_ShapeCount; i++)
                m_AnimBools[i].target = (int)shape == i;
        }

        public AnimBool IsSectionExpanded_Shape(ShapeType shapeType)
        {
            return m_AnimBools[(int)shapeType];
        }

        static void Drawer_FieldShapeType(InfluenceVolumeUI s, SerializedInfluenceVolume d, Editor o)
        {
            EditorGUILayout.PropertyField(d.shapeType, _.GetContent("Shape Type"));
        }

        static void Drawer_SectionShapeBox(InfluenceVolumeUI s, SerializedInfluenceVolume d, Editor o)
        {
            EditorGUILayout.PropertyField(d.boxBaseSize, _.GetContent("Box Size"));
            EditorGUILayout.PropertyField(d.boxBaseOffset, _.GetContent("Box Offset"));
            EditorGUILayout.PropertyField(d.boxInfluencePositiveFade, _.GetContent("Influence Fade (Positive)"));
            EditorGUILayout.PropertyField(d.boxInfluenceNegativeFade, _.GetContent("Influence Fade (Negative)"));
            EditorGUILayout.PropertyField(d.boxInfluenceNormalPositiveFade, _.GetContent("Influence Normal Fade (Positive)"));
            EditorGUILayout.PropertyField(d.boxInfluenceNormalNegativeFade, _.GetContent("Influence Normal Fade (Negative)"));
        }

        static void Drawer_SectionShapeSphere(InfluenceVolumeUI s, SerializedInfluenceVolume d, Editor o)
        {
            EditorGUILayout.PropertyField(d.sphereBaseRadius, _.GetContent("Radius"));
            EditorGUILayout.PropertyField(d.sphereBaseOffset, _.GetContent("Offset"));
            EditorGUILayout.PropertyField(d.sphereInfluenceRadius, _.GetContent("Influence Radius"));
            EditorGUILayout.PropertyField(d.sphereInfluenceNormalRadius, _.GetContent("Influence Normal Radius"));
        }

        public static void DrawHandles(Transform transform, ProjectionVolume projectionVolume, InfluenceVolumeUI ui, Object sourceAsset)
        {
            switch (projectionVolume.shapeType)
            {
                case ShapeType.Box:
                    Handles_Box(transform, projectionVolume, ui, sourceAsset);
                    break;
                case ShapeType.Sphere:
                    Handles_Sphere(transform, projectionVolume, ui, sourceAsset);
                    break;
            }
        }

        static void Handles_Sphere(Transform transform, ProjectionVolume projectionVolume, InfluenceVolumeUI s, Object sourceAsset)
        {
            //s.sphereProjectionHandle.center = projectionVolume.sphereOffset;
            //s.sphereProjectionHandle.radius = projectionVolume.sphereRadius;

            //var mat = Handles.matrix;
            //Handles.matrix = transform.localToWorldMatrix;
            //Handles.color = k_GizmoThemeColorProjection;
            //EditorGUI.BeginChangeCheck();
            //s.sphereProjectionHandle.DrawHandle();
            //if (EditorGUI.EndChangeCheck())
            //{
            //    Undo.RecordObject(sourceAsset, "Modified Projection Volume");

            //    projectionVolume.sphereOffset = s.sphereProjectionHandle.center;
            //    projectionVolume.sphereRadius = s.sphereProjectionHandle.radius;

            //    EditorUtility.SetDirty(sourceAsset);
            //}
            //Handles.matrix = mat;
        }

        static void Handles_Box(Transform transform, ProjectionVolume projectionVolume, InfluenceVolumeUI s, Object sourceAsset)
        {
            //s.boxProjectionHandle.center = projectionVolume.boxOffset;
            //s.boxProjectionHandle.size = projectionVolume.boxSize;

            //var mat = Handles.matrix;
            //Handles.matrix = transform.localToWorldMatrix;

            //Handles.color = k_GizmoThemeColorProjection;
            //EditorGUI.BeginChangeCheck();
            //s.boxProjectionHandle.DrawHandle();
            //if (EditorGUI.EndChangeCheck())
            //{
            //    Undo.RecordObject(sourceAsset, "Modified Projection Volume AABB");

            //    projectionVolume.boxOffset = s.boxProjectionHandle.center;
            //    projectionVolume.boxSize = s.boxProjectionHandle.size;

            //    EditorUtility.SetDirty(sourceAsset);
            //}

            //Handles.matrix = mat;
        }
    }
}
