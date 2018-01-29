using System;
using UnityEditor.AnimatedValues;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using UnityEngine.Experimental.Rendering.HDPipeline;
using Object = UnityEngine.Object;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    using _ = CoreEditorUtils;
    using CED = CoreEditorDrawer<ProxyVolumeUI, SerializedProxyVolume>;

    class ProxyVolumeUI : BaseUI<SerializedProxyVolume>
    {
        internal static Color k_GizmoThemeColorProjection = new Color(0x00 / 255f, 0xE5 / 255f, 0xFF / 255f, 0x20 / 255f);
        internal static Color k_GizmoThemeColorProjectionFace = new Color(0x00 / 255f, 0xE5 / 255f, 0xFF / 255f, 0x20 / 255f);
        internal static Color k_GizmoThemeColorDisabled = new Color(0x99 / 255f, 0x89 / 255f, 0x59 / 255f, 0x10 / 255f);
        internal static Color k_GizmoThemeColorDisabledFace = new Color(0x99 / 255f, 0x89 / 255f, 0x59 / 255f, 0x10 / 255f);

        static readonly int k_ShapeCount = Enum.GetValues(typeof(ShapeType)).Length;

        public static readonly CED.IDrawer SectionShape;

        public static readonly CED.IDrawer SectionShapeBox = CED.Action(Drawer_SectionShapeBox);
        public static readonly CED.IDrawer SectionShapeSphere = CED.Action(Drawer_SectionShapeSphere);

        static ProxyVolumeUI()
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

        public ProxyVolumeUI()
            : base(k_ShapeCount)
        {
            
        }

        public override void Update()
        {
            base.Update();
            if (data != null)
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

        static void Drawer_FieldShapeType(ProxyVolumeUI s, SerializedProxyVolume d, Editor o)
        {
            EditorGUILayout.PropertyField(d.shapeType, _.GetContent("Shape Type"));
        }

        static void Drawer_SectionShapeBox(ProxyVolumeUI s, SerializedProxyVolume d, Editor o)
        {
            EditorGUILayout.PropertyField(d.boxSize, _.GetContent("Box Size"));
            EditorGUILayout.PropertyField(d.boxOffset, _.GetContent("Box Offset"));
            EditorGUILayout.PropertyField(d.boxInfiniteProjection, _.GetContent("Infinite Projection"));
        }

        static void Drawer_SectionShapeSphere(ProxyVolumeUI s, SerializedProxyVolume d, Editor o)
        {
            EditorGUILayout.PropertyField(d.sphereRadius, _.GetContent("Sphere Radius"));
            EditorGUILayout.PropertyField(d.sphereOffset, _.GetContent("Sphere Offset"));
            EditorGUILayout.PropertyField(d.sphereInfiniteProjection, _.GetContent("Infinite Projection"));
        }

        public static void DrawHandles_EditBase(Transform transform, ProxyVolume proxyVolume, ProxyVolumeUI ui, Object sourceAsset)
        {
            switch (proxyVolume.shapeType)
            {
                case ShapeType.Box:
                    Handles_EditBase_Box(transform, proxyVolume, ui, sourceAsset);
                    break;
                case ShapeType.Sphere:
                    Handles_EditBase_Sphere(transform, proxyVolume, ui, sourceAsset);
                    break;
            }
        }

        public static void DrawHandles_EditNone(Transform transform, ProxyVolume proxyVolume, ProxyVolumeUI ui, Object sourceAsset)
        {

        }

        static void Handles_EditBase_Sphere(Transform transform, ProxyVolume proxyVolume, ProxyVolumeUI s, Object sourceAsset)
        {
            s.sphereProjectionHandle.center = proxyVolume.sphereOffset;
            s.sphereProjectionHandle.radius = proxyVolume.sphereRadius;

            var mat = Handles.matrix;
            Handles.matrix = transform.localToWorldMatrix;
            Handles.color = k_GizmoThemeColorProjection;
            EditorGUI.BeginChangeCheck();
            s.sphereProjectionHandle.DrawHandle();
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(sourceAsset, "Modified Projection Volume");

                proxyVolume.sphereOffset = s.sphereProjectionHandle.center;
                proxyVolume.sphereRadius = s.sphereProjectionHandle.radius;

                EditorUtility.SetDirty(sourceAsset);
            }
            Handles.matrix = mat;
        }

        static void Handles_EditBase_Box(Transform transform, ProxyVolume proxyVolume, ProxyVolumeUI s, Object sourceAsset)
        {
            s.boxProjectionHandle.center = proxyVolume.boxOffset;
            s.boxProjectionHandle.size = proxyVolume.boxSize;

            var mat = Handles.matrix;
            Handles.matrix = transform.localToWorldMatrix;

            Handles.color = k_GizmoThemeColorProjection;
            EditorGUI.BeginChangeCheck();
            s.boxProjectionHandle.DrawHandle();
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(sourceAsset, "Modified Projection Volume AABB");

                proxyVolume.boxOffset = s.boxProjectionHandle.center;
                proxyVolume.boxSize = s.boxProjectionHandle.size;

                EditorUtility.SetDirty(sourceAsset);
            }

            Handles.matrix = mat;
        }

        public static void DrawGizmos_EditNone(Transform transform, ProxyVolume proxyVolume, ProxyVolumeUI ui, Object sourceAsset)
        {
            switch (proxyVolume.shapeType)
            {
                case ShapeType.Box:
                    Gizmos_EditNone_Box(transform, proxyVolume, ui, sourceAsset);
                    break;
                case ShapeType.Sphere:
                    Gizmos_EditNone_Sphere(transform, proxyVolume, ui, sourceAsset);
                    break;
            }
        }

        static void Gizmos_EditNone_Sphere(Transform t, ProxyVolume d, ProxyVolumeUI s, Object o)
        {
            var mat = Gizmos.matrix;
            Gizmos.matrix = t.localToWorldMatrix;

            Gizmos.color = k_GizmoThemeColorProjection;
            Gizmos.DrawWireSphere(d.sphereOffset, d.sphereRadius);

            Gizmos.matrix = mat;
        }

        static void Gizmos_EditNone_Box(Transform t, ProxyVolume d, ProxyVolumeUI s, Object o)
        {
            var mat = Gizmos.matrix;
            Gizmos.matrix = t.localToWorldMatrix;

            Gizmos.color = k_GizmoThemeColorProjection;
            Gizmos.DrawWireCube(d.boxOffset, d.boxSize);

            Gizmos.matrix = mat;
        }
    }
}
