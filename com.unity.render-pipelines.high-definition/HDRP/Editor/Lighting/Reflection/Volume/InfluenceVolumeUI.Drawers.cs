using UnityEngine;
using UnityEngine.Experimental.Rendering.HDPipeline;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    using _ = CoreEditorUtils;
    using CED = CoreEditorDrawer<InfluenceVolumeUI, SerializedInfluenceVolume>;

    partial class InfluenceVolumeUI
    {
        public static readonly CED.IDrawer SectionFoldoutShape;
        public static readonly CED.IDrawer FieldShape = CED.Action(Drawer_FieldShapeType);
        public static readonly CED.IDrawer SectionShapeBox = CED.Action(Drawer_SectionShapeBox);
        public static readonly CED.IDrawer SectionShapeSphere = CED.Action(Drawer_SectionShapeSphere);

        static InfluenceVolumeUI()
        {
            SectionFoldoutShape = CED.Group(
                    CED.FoldoutGroup(
                        "Influence Volume",
                        (s, d, o) => s.isSectionExpandedShape,
                        FoldoutOption.Indent,
                        CED.Action(Drawer_InfluenceAdvancedSwitch),
                        CED.space,
                        CED.Action(Drawer_FieldShapeType),
                        CED.FadeGroup(
                            (s, d, o, i) => s.IsSectionExpanded_Shape((ShapeType)i),
                            FadeOption.None,
                            SectionShapeBox,
                            SectionShapeSphere
                            )
                        )
                    );
        }

        static void Drawer_FieldShapeType(InfluenceVolumeUI s, SerializedInfluenceVolume d, Editor o)
        {
            //EditorGUI.showMixedValue = d.;
            EditorGUILayout.PropertyField(d.shapeType, _.GetContent("Shape Type"));
            //EditorGUI.showMixedValue = false;

            //if(s.shapeMissmatch)
            //{
            //    EditorGUILayout.HelpBox(
            //            "Proxy volume and influence volume have different shape types, this is not supported.",
            //            MessageType.Error,
            //            true
            //            );
            //}
        }

        static void Drawer_InfluenceAdvancedSwitch(InfluenceVolumeUI s, SerializedInfluenceVolume d, Editor owner)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();

                bool advanced = s.isSectionAdvancedInfluenceSettings.value;
                advanced = !GUILayout.Toggle(!advanced, CoreEditorUtils.GetContent("Normal|Normal parameters mode (only change for box shape)."), EditorStyles.miniButtonLeft, GUILayout.Width(60f), GUILayout.ExpandWidth(false));
                advanced = GUILayout.Toggle(advanced, CoreEditorUtils.GetContent("Advanced|Advanced parameters mode (only change for box shape)."), EditorStyles.miniButtonRight, GUILayout.Width(60f), GUILayout.ExpandWidth(false));
                if (s.isSectionAdvancedInfluenceSettings.value ^ advanced)
                {
                    s.isSectionAdvancedInfluenceSettings.value = advanced;
                    if (!advanced)
                    {
                        d.boxInfluenceNegativeFade.vector3Value = d.boxInfluencePositiveFade.vector3Value = Vector3.one * d.boxInfluencePositiveFade.vector3Value.x;
                        d.boxInfluenceNormalNegativeFade.vector3Value = d.boxInfluenceNormalPositiveFade.vector3Value = Vector3.one * d.boxInfluenceNormalPositiveFade.vector3Value.x;
                    }
                }
            }
        }

        static void Drawer_SectionShapeBox(InfluenceVolumeUI s, SerializedInfluenceVolume d, Editor o)
        {
            var maxFadeDistance = d.boxBaseSize.vector3Value * 0.5f;
            var minFadeDistance = Vector3.zero;

            EditorGUILayout.PropertyField(d.boxBaseSize, _.GetContent("Box Size"));
            EditorGUILayout.PropertyField(d.boxBaseOffset, _.GetContent("Box Offset"));

            EditorGUILayout.Space();

            EditorGUILayout.BeginHorizontal();
            HDReflectionProbeUI.Drawer_AdvancedBlendDistance(
                d.boxInfluencePositiveFade,
                d.boxInfluenceNegativeFade,
                maxFadeDistance,
                CoreEditorUtils.GetContent("Blend Distance|Area around the probe where it is blended with other probes. Only used in deferred probes."),
                s.isSectionAdvancedInfluenceSettings
                );
            //if (GUILayout.Button(toolbar_Contents[1], GUILayout.ExpandHeight(true), GUILayout.Width(28f), GUILayout.MinHeight(22f), GUILayout.MaxHeight((s.isSectionAdvancedInfluenceSettings.value ? 3 : 1) * (EditorGUIUtility.singleLineHeight + 3))))
            //{
            //    EditMode.ChangeEditMode(k_Toolbar_SceneViewEditModes[1], GetBoundsGetter(p)(), owner);
            //}
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();

            EditorGUILayout.BeginHorizontal();
            HDReflectionProbeUI.Drawer_AdvancedBlendDistance(
                d.boxInfluenceNormalPositiveFade,
                d.boxInfluenceNormalNegativeFade,
                maxFadeDistance,
                CoreEditorUtils.GetContent("Blend Normal Distance|Area around the probe where the normals influence the probe. Only used in deferred probes."),
                s.isSectionAdvancedInfluenceSettings
                );
            //if (GUILayout.Button(toolbar_Contents[2], GUILayout.ExpandHeight(true), GUILayout.Width(28f), GUILayout.MinHeight(22f), GUILayout.MaxHeight((s.isSectionAdvancedInfluenceSettings.value ? 3 : 1) * (EditorGUIUtility.singleLineHeight + 3))))
            //{
            //    EditMode.ChangeEditMode(k_Toolbar_SceneViewEditModes[2], GetBoundsGetter(p)(), owner);
            //}
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();

            if (s.isSectionAdvancedInfluenceSettings.value)
            {
                CoreEditorUtils.DrawVector6(
                    CoreEditorUtils.GetContent("Face fade|Fade faces of the cubemap."),
                    d.boxPositiveFaceFade, d.boxNegativeFaceFade, Vector3.zero, Vector3.one, HDReflectionProbeEditor.k_handlesColor);
            }
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
