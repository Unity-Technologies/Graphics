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
                ),
                CED.space,
                CED.Select(
                    (s, d, o) => s.decalSettings,
                    (s, d, o) => d.decalSettings,
                    GlobalDecalSettingsUI.Inspector
                )

            );
        }

        public static readonly CED.IDrawer Inspector;

        public static readonly CED.IDrawer SectionPrimarySettings = CED.Group(
            CED.Action(Drawer_SectionPrimarySettings)
        );

        GlobalLightLoopSettingsUI lightLoopSettings = new GlobalLightLoopSettingsUI();
		GlobalDecalSettingsUI decalSettings = new GlobalDecalSettingsUI();
        ShadowInitParametersUI shadowInitParams = new ShadowInitParametersUI();

        public RenderPipelineSettingsUI()
            : base(0)
        {

        }

        public override void Reset(SerializedRenderPipelineSettings data, UnityAction repaint)
        {
            lightLoopSettings.Reset(data.lightLoopSettings, repaint);
            shadowInitParams.Reset(data.shadowInitParams, repaint);
			decalSettings.Reset(data.decalSettings, repaint);
            base.Reset(data, repaint);
        }

        public override void Update()
        {
            lightLoopSettings.Update();
            shadowInitParams.Update();
			decalSettings.Update();
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
            EditorGUILayout.PropertyField(d.supportMSAA, _.GetContent("Support Multi Sampling Anti-Aliasing"));
            EditorGUILayout.PropertyField(d.MSAASampleCount, _.GetContent("MSAA Sample Count"));
            EditorGUILayout.PropertyField(d.supportSubsurfaceScattering, _.GetContent("Support Subsurface Scattering"));
            EditorGUILayout.PropertyField(d.supportForwardOnly, _.GetContent("Support Forward Only"));
            EditorGUILayout.PropertyField(d.supportMotionVectors, _.GetContent("Support Motion Vectors"));
            EditorGUILayout.PropertyField(d.supportStereo, _.GetContent("Support Stereo Rendering"));
            EditorGUILayout.PropertyField(d.enableUltraQualitySSS, _.GetContent("Enable Ultra Quality SSS"));
            --EditorGUI.indentLevel;
        }
    }
}
