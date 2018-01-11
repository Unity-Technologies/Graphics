using UnityEngine;
using UnityEngine.Experimental.Rendering.HDPipeline;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    using _ = CoreEditorUtils;
    using CED = CoreEditorDrawer<InfluenceVolumeUI, SerializedInfluenceVolume>;

    partial class InfluenceVolumeUI
    {
        public static readonly CED.IDrawer SectionShape;
        public static readonly CED.IDrawer SectionFoldoutShape;

        public static readonly CED.IDrawer FieldShape = CED.Action(Drawer_FieldShapeType);
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

            SectionFoldoutShape = CED.Group(
                CED.FoldoutGroup(
                    "Influence Volume",
                    (s, d, o) => s.isSectionExpandedShape,
                    true,
                    CED.Action(Drawer_FieldShapeType),
                    CED.FadeGroup(
                        (s, d, o, i) => s.IsSectionExpanded_Shape((ShapeType)i),
                        false,
                        SectionShapeBox,
                        SectionShapeSphere
                    )
                )
            );
        }

        static void Drawer_FieldShapeType(InfluenceVolumeUI s, SerializedInfluenceVolume d, Editor o)
        {
            EditorGUILayout.PropertyField(d.shapeType, _.GetContent("Shape Type"));
        }

        static void Drawer_SectionShapeBox(InfluenceVolumeUI s, SerializedInfluenceVolume d, Editor o)
        {
            var maxFadeDistance = d.boxBaseSize.vector3Value * 0.5f;
            var minFadeDistance = Vector3.zero;

            EditorGUILayout.PropertyField(d.boxBaseSize, _.GetContent("Box Size"));
            EditorGUILayout.PropertyField(d.boxBaseOffset, _.GetContent("Box Offset"));

            EditorGUILayout.Space();

            _.DrawVector6Slider(
                _.GetContent("Influence Fade"), 
                d.boxInfluencePositiveFade, d.boxInfluenceNegativeFade, 
                minFadeDistance, maxFadeDistance);

            EditorGUILayout.Space();

            _.DrawVector6Slider(
                _.GetContent("Influence Normal Fade"),
                d.boxInfluenceNormalPositiveFade, d.boxInfluenceNormalNegativeFade,
                minFadeDistance, maxFadeDistance);

            EditorGUILayout.Space();

            _.DrawVector6Slider(
                _.GetContent("Influence Face Fade"),
                d.boxPositiveFaceFade, d.boxNegativeFaceFade,
                Vector3.zero, Vector3.one);
        }

        static void Drawer_SectionShapeSphere(InfluenceVolumeUI s, SerializedInfluenceVolume d, Editor o)
        {
            var maxFaceDistance = d.sphereBaseRadius.floatValue;

            EditorGUILayout.PropertyField(d.sphereBaseRadius, _.GetContent("Radius"));
            //EditorGUILayout.PropertyField(d.sphereBaseOffset, _.GetContent("Offset"));
            d.sphereBaseOffset.vector3Value = Vector3.zero;

            EditorGUILayout.Space();

            EditorGUILayout.Slider(d.sphereInfluenceFade, 0, maxFaceDistance, _.GetContent("Influence Fade"));
            EditorGUILayout.Slider(d.sphereInfluenceNormalFade, 0, maxFaceDistance, _.GetContent("Influence Normal Fade"));
        }
    }
}
