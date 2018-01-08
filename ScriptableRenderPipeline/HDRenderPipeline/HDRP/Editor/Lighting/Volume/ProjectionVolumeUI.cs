using System;
using UnityEditor.AnimatedValues;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using UnityEngine.Experimental.Rendering.HDPipeline;
using Object = UnityEngine.Object;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    using _ = CoreEditorUtils;
    using CED = CoreEditorDrawer<ProjectionVolumeUI, SerializedProjectionVolume>;

    class ProjectionVolumeUI : BaseUI<SerializedProjectionVolume>
    {
        internal static Color k_GizmoThemeColorProjection = new Color(0x00 / 255f, 0xE5 / 255f, 0xFF / 255f, 0x20 / 255f);
        internal static Color k_GizmoThemeColorProjectionFace = new Color(0x00 / 255f, 0xE5 / 255f, 0xFF / 255f, 0x20 / 255f);
        internal static Color k_GizmoThemeColorDisabled = new Color(0x99 / 255f, 0x89 / 255f, 0x59 / 255f, 0x10 / 255f);
        internal static Color k_GizmoThemeColorDisabledFace = new Color(0x99 / 255f, 0x89 / 255f, 0x59 / 255f, 0x10 / 255f);

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

        public BoxBoundsHandle boxProjectionHandle = new BoxBoundsHandle();
        public SphereBoundsHandle sphereProjectionHandle = new SphereBoundsHandle();

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

        public static void DrawHandles(Transform transform, ProjectionVolume projectionVolume, ProjectionVolumeUI ui, Object sourceAsset)
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

        static void Handles_Sphere(Transform transform, ProjectionVolume projectionVolume, ProjectionVolumeUI s, Object sourceAsset)
        {
            s.sphereProjectionHandle.center = projectionVolume.sphereOffset;
            s.sphereProjectionHandle.radius = projectionVolume.sphereRadius;

            var mat = Handles.matrix;
            Handles.matrix = transform.localToWorldMatrix;
            Handles.color = k_GizmoThemeColorProjection;
            EditorGUI.BeginChangeCheck();
            s.sphereProjectionHandle.DrawHandle();
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(sourceAsset, "Modified Projection Volume");

                projectionVolume.sphereOffset = s.sphereProjectionHandle.center;
                projectionVolume.sphereRadius = s.sphereProjectionHandle.radius;

                EditorUtility.SetDirty(sourceAsset);
            }
            Handles.matrix = mat;
        }

        static void Handles_Box(Transform transform, ProjectionVolume projectionVolume, ProjectionVolumeUI s, Object sourceAsset)
        {
            s.boxProjectionHandle.center = projectionVolume.boxOffset;
            s.boxProjectionHandle.size = projectionVolume.boxSize;

            var mat = Handles.matrix;
            Handles.matrix = transform.localToWorldMatrix;

            Handles.color = k_GizmoThemeColorProjection;
            EditorGUI.BeginChangeCheck();
            s.boxProjectionHandle.DrawHandle();
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(sourceAsset, "Modified Projection Volume AABB");

                projectionVolume.boxOffset = s.boxProjectionHandle.center;
                projectionVolume.boxSize = s.boxProjectionHandle.size;

                EditorUtility.SetDirty(sourceAsset);
            }

            Handles.matrix = mat;
        }
    }
}
