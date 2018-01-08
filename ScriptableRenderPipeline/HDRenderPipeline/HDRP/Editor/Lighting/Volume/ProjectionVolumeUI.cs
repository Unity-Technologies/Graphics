using System;
using UnityEditor.AnimatedValues;
using UnityEngine.Experimental.Rendering.HDPipeline;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    using _ = CoreEditorUtils;
    using CED = CoreEditorDrawer<ProjectionVolumeUI, SerializedProjectionVolume>;

    class ProjectionVolumeUI : BaseUI<SerializedProjectionVolume>
    {
        static readonly int k_ShapeCount = Enum.GetValues(typeof(ShapeType)).Length;

        public static readonly CED.IDrawer SectionShape;

        public static readonly CED.IDrawer SectionShapeBox = CED.Action(Drawer_SectionShapeBox);
        public static readonly CED.IDrawer SectionShapeSphere = CED.Action(Drawer_SectionShapeSphere);

        static ProjectionVolumeUI()
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


        public ProjectionVolumeUI()
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

        static void Drawer_FieldShapeType(ProjectionVolumeUI s, SerializedProjectionVolume d, Editor o)
        {
            EditorGUILayout.PropertyField(d.shapeType, _.GetContent("Shape Type"));
        }

        static void Drawer_SectionShapeBox(ProjectionVolumeUI s, SerializedProjectionVolume d, Editor o)
        {
            EditorGUILayout.PropertyField(d.boxSize, _.GetContent("Box Size"));
            EditorGUILayout.PropertyField(d.boxOffset, _.GetContent("Box Offset"));
            EditorGUILayout.PropertyField(d.boxInfiniteProjection, _.GetContent("Infinite Projection"));
        }

        static void Drawer_SectionShapeSphere(ProjectionVolumeUI s, SerializedProjectionVolume d, Editor o)
        {
            EditorGUILayout.PropertyField(d.sphereRadius, _.GetContent("Sphere Radius"));
            EditorGUILayout.PropertyField(d.sphereOffset, _.GetContent("Sphere Offset"));
            EditorGUILayout.PropertyField(d.sphereInfiniteProjection, _.GetContent("Infinite Projection"));
        }
    }
}
