namespace UnityEditor.Rendering.Universal
{
    using UnityEngine;
    using CED = CoreEditorDrawer<object>;

    internal partial class UniversalRenderPipelineLightUI
    {
        enum Expandable
        {
            General = 1 << 0,
            Shape = 1 << 1,
            Emission = 1 << 2,
            Rendering = 1 << 3,
            Shadows = 1 << 4
        }

        static readonly ExpandedState<Expandable, Light> k_ExpandedState = new ExpandedState<Expandable, Light>(~-1, "URP");

        public static readonly CED.IDrawer Inspector = CED.Group(
            CED.Conditional(
                (_, __) =>
                {
                    if (SceneView.lastActiveSceneView == null)
                        return false;

#if UNITY_2019_1_OR_NEWER
                    var sceneLighting = SceneView.lastActiveSceneView.sceneLighting;
#else
                    var sceneLighting = SceneView.lastActiveSceneView.m_SceneLighting;
#endif
                    return !sceneLighting;
                },
                (_, __) => EditorGUILayout.HelpBox(Styles.DisabledLightWarning.text, MessageType.Warning)),
            CED.FoldoutGroup(Styles.generalHeader,
                Expandable.General,
                k_ExpandedState,
                DrawGeneralContent),
            CED.Conditional(
                (serialized, editor) => false,
                CED.FoldoutGroup(Styles.shapeHeader, Expandable.Shape, k_ExpandedState, DrawShapeContent)),
            CED.FoldoutGroup(Styles.emissionHeader,
                Expandable.Emission,
                k_ExpandedState,
                DrawEmissionContent),
            CED.FoldoutGroup(Styles.renderingHeader,
                Expandable.Rendering,
                k_ExpandedState,
                DrawRenderingContent),
            CED.FoldoutGroup(Styles.shadowHeader,
                Expandable.Shadows,
                k_ExpandedState,
                DrawShadowsContent)
        );


        static void DrawGeneralContent(object serialized, Editor owner)
        {
        }

        static void DrawShapeContent(object serialized, Editor owner)
        {
        }

        static void DrawEmissionContent(object serialized, Editor owner)
        {
        }

        static void DrawRenderingContent(object serialized, Editor owner)
        {
        }

        static void DrawShadowsContent(object serialized, Editor owner)
        {
        }
    }
}
