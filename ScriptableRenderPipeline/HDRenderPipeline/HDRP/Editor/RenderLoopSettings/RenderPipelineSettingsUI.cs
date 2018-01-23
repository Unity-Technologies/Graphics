using UnityEngine.Events;

namespace UnityEditor.Experimental.Rendering
{
    using _ = CoreEditorUtils;
    using CED = CoreEditorDrawer<RenderPipelineSettingsUI, SerializedRenderPipelineSettings>;

    class RenderPipelineSettingsUI : BaseUI<SerializedRenderPipelineSettings>
    {
        static RenderPipelineSettingsUI()
        {
            Inspector = CED.Group(
                SectionPrimarySettings,
                CED.space,
                CED.Select(
                    (s, d, o) => s.lightLoopSettings,
                    (s, d, o) => d.lightLoopSettings,
                    GlobalLightLoopSettingsUI.Inspector
                ),
                CED.space,
                CED.Select(
                    (s, d, o) => s.shadowInitParams,
                    (s, d, o) => d.shadowInitParams,
                    ShadowInitParametersUI.SectionAtlas
                )
            );
        }

        public static readonly CED.IDrawer Inspector;

        public static readonly CED.IDrawer SectionPrimarySettings = CED.Group(
            CED.Action(Drawer_SectionPrimarySettings)
        );

        GlobalLightLoopSettingsUI lightLoopSettings = new GlobalLightLoopSettingsUI();
        ShadowInitParametersUI shadowInitParams = new ShadowInitParametersUI();

        public RenderPipelineSettingsUI()
            : base(0)
        {

        }

        public override void Reset(SerializedRenderPipelineSettings data, UnityAction repaint)
        {
            lightLoopSettings.Reset(data.lightLoopSettings, repaint);
            shadowInitParams.Reset(data.shadowInitParams, repaint);
            base.Reset(data, repaint);
        }

        public override void Update()
        {
            lightLoopSettings.Update();
            shadowInitParams.Update();
            base.Update();
        }

        static void Drawer_SectionPrimarySettings(RenderPipelineSettingsUI s, SerializedRenderPipelineSettings d, Editor o)
        {
            EditorGUILayout.LabelField(_.GetContent("Render Pipeline Settings"), EditorStyles.boldLabel);
            ++EditorGUI.indentLevel;
            EditorGUILayout.PropertyField(d.supportShadowMask, _.GetContent("Support Shadow Mask"));
            EditorGUILayout.PropertyField(d.supportSSR, _.GetContent("Support SSR"));
            EditorGUILayout.PropertyField(d.supportSSAO, _.GetContent("Support SSAO"));
            EditorGUILayout.PropertyField(d.supportDBuffer, _.GetContent("Support Decal Buffer"));
            EditorGUILayout.PropertyField(d.supportMSAA, _.GetContent("Support MSAA"));
            EditorGUILayout.PropertyField(d.supportSubsurfaceScattering, _.GetContent("Support Subsurface Scattering"));
            --EditorGUI.indentLevel;
        }
    }
}
