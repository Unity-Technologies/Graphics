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
            EditorGUILayout.PropertyField(d.shapeType, _.GetContent("Shape Type"));
        }

        static void Drawer_InfluenceAdvancedSwitch(InfluenceVolumeUI s, SerializedInfluenceVolume d, Editor owner)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();

                bool advanced = d.editorAdvancedModeEnabled.boolValue;
                advanced = !GUILayout.Toggle(!advanced, CoreEditorUtils.GetContent("Normal|Normal parameters mode (only change for box shape)."), EditorStyles.miniButtonLeft, GUILayout.Width(60f), GUILayout.ExpandWidth(false));
                advanced = GUILayout.Toggle(advanced, CoreEditorUtils.GetContent("Advanced|Advanced parameters mode (only change for box shape)."), EditorStyles.miniButtonRight, GUILayout.Width(60f), GUILayout.ExpandWidth(false));
                s.boxInfluenceHandle.allHandleControledByOne = s.boxInfluenceNormalHandle.allHandleControledByOne = !advanced;
                if (d.editorAdvancedModeEnabled.boolValue ^ advanced)
                {
                    d.editorAdvancedModeEnabled.boolValue = advanced;
                    if (advanced)
                    {
                        d.boxInfluencePositiveFade.vector3Value = d.editorAdvancedModeBlendDistancePositive.vector3Value;
                        d.boxInfluenceNegativeFade.vector3Value = d.editorAdvancedModeBlendDistanceNegative.vector3Value;
                        d.boxInfluenceNormalPositiveFade.vector3Value = d.editorAdvancedModeBlendNormalDistancePositive.vector3Value;
                        d.boxInfluenceNormalNegativeFade.vector3Value = d.editorAdvancedModeBlendNormalDistanceNegative.vector3Value;
                    }
                    else
                    {
                        d.boxInfluenceNegativeFade.vector3Value = d.boxInfluencePositiveFade.vector3Value = Vector3.one * d.editorSimplifiedModeBlendDistance.floatValue;
                        d.boxInfluenceNormalNegativeFade.vector3Value = d.boxInfluenceNormalPositiveFade.vector3Value = Vector3.one * d.editorSimplifiedModeBlendNormalDistance.floatValue;
                    }
                    d.Apply();
                }
            }
        }

        static void Drawer_SectionShapeBox(InfluenceVolumeUI s, SerializedInfluenceVolume d, Editor o)
        {
            bool advanced = d.editorAdvancedModeEnabled.boolValue;
            var maxFadeDistance = d.boxBaseSize.vector3Value * 0.5f;
            var minFadeDistance = Vector3.zero;

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PropertyField(d.boxBaseSize, _.GetContent("Box Size"));
            PlanarReflectionProbeUI.Drawer_ToolBarButton(0, o, GUILayout.Width(28f), GUILayout.MinHeight(22f));
            EditorGUILayout.EndHorizontal();

            //offset have no meaning for planar reflexion probe
            //EditorGUILayout.PropertyField(d.boxBaseOffset, _.GetContent("Box Offset"));

            EditorGUILayout.BeginHorizontal();
            Drawer_AdvancedBlendDistance(
                d,
                false,
                maxFadeDistance,
                CoreEditorUtils.GetContent("Blend Distance|Area around the probe where it is blended with other probes. Only used in deferred probes.")
                );
            PlanarReflectionProbeUI.Drawer_ToolBarButton(1, o, GUILayout.ExpandHeight(true), GUILayout.Width(28f), GUILayout.MinHeight(22f), GUILayout.MaxHeight((advanced ? 3 : 1) * (EditorGUIUtility.singleLineHeight + 3)));
            EditorGUILayout.EndHorizontal();

            if (advanced)
            {
                CoreEditorUtils.DrawVector6(
                    CoreEditorUtils.GetContent("Face fade|Fade faces of the cubemap."),
                    d.boxPositiveFaceFade, d.boxNegativeFaceFade, Vector3.zero, Vector3.one, HDReflectionProbeEditor.k_handlesColor);
            }
        }

        static void Drawer_AdvancedBlendDistance(SerializedInfluenceVolume d, bool isNormal, Vector3 maxBlendDistance, GUIContent content)
        {
            SerializedProperty blendDistancePositive = isNormal ? d.boxInfluenceNormalPositiveFade : d.boxInfluencePositiveFade;
            SerializedProperty blendDistanceNegative = isNormal ? d.boxInfluenceNormalNegativeFade : d.boxInfluenceNegativeFade;
            SerializedProperty editorAdvancedModeBlendDistancePositive = isNormal ? d.editorAdvancedModeBlendNormalDistancePositive : d.editorAdvancedModeBlendDistancePositive;
            SerializedProperty editorAdvancedModeBlendDistanceNegative = isNormal ? d.editorAdvancedModeBlendNormalDistanceNegative : d.editorAdvancedModeBlendDistanceNegative;
            SerializedProperty editorSimplifiedModeBlendDistance = isNormal ? d.editorSimplifiedModeBlendNormalDistance : d.editorSimplifiedModeBlendDistance;
            Vector3 bdp = blendDistancePositive.vector3Value;
            Vector3 bdn = blendDistanceNegative.vector3Value;

            EditorGUILayout.BeginVertical();

            if (d.editorAdvancedModeEnabled.boolValue)
            {
                EditorGUI.BeginChangeCheck();
                blendDistancePositive.vector3Value = editorAdvancedModeBlendDistancePositive.vector3Value;
                blendDistanceNegative.vector3Value = editorAdvancedModeBlendDistanceNegative.vector3Value;
                CoreEditorUtils.DrawVector6(
                    content,
                    blendDistancePositive, blendDistanceNegative, Vector3.zero, maxBlendDistance, HDReflectionProbeEditor.k_handlesColor);
                if(EditorGUI.EndChangeCheck())
                {
                    editorAdvancedModeBlendDistancePositive.vector3Value = blendDistancePositive.vector3Value;
                    editorAdvancedModeBlendDistanceNegative.vector3Value = blendDistanceNegative.vector3Value;
                }
            }
            else
            {
                float distance = editorSimplifiedModeBlendDistance.floatValue;
                EditorGUI.BeginChangeCheck();
                distance = EditorGUILayout.FloatField(content, distance);
                if (EditorGUI.EndChangeCheck())
                {
                    Vector3 decal = Vector3.one * distance;
                    bdp.x = Mathf.Clamp(decal.x, 0f, maxBlendDistance.x);
                    bdp.y = Mathf.Clamp(decal.y, 0f, maxBlendDistance.y);
                    bdp.z = Mathf.Clamp(decal.z, 0f, maxBlendDistance.z);
                    bdn.x = Mathf.Clamp(decal.x, 0f, maxBlendDistance.x);
                    bdn.y = Mathf.Clamp(decal.y, 0f, maxBlendDistance.y);
                    bdn.z = Mathf.Clamp(decal.z, 0f, maxBlendDistance.z);
                    blendDistancePositive.vector3Value = bdp;
                    blendDistanceNegative.vector3Value = bdn;
                    editorSimplifiedModeBlendDistance.floatValue = distance;
                }
            }

            GUILayout.EndVertical();
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
