using UnityEngine.Events;

namespace UnityEditor.Experimental.Rendering
{
    using _ = CoreEditorUtils;
    using CED = CoreEditorDrawer<HDRenderPipelineUI, SerializedHDRenderPipelineAsset>;

    class HDRenderPipelineUI : BaseUI<SerializedHDRenderPipelineAsset>
    {
        static HDRenderPipelineUI()
        {
            Inspector = CED.Group(
                SectionPrimarySettings,
                CED.space,
                CED.Select(
                    (s, d, o) => s.renderPipelineSettings,
                    (s, d, o) => d.renderPipelineSettings,
                    RenderPipelineSettingsUI.Inspector
                ),
                CED.space,
                CED.Action(Drawer_TitleDefaultFrameSettings),
                CED.Select(
                    (s, d, o) => s.defaultFrameSettings,
                    (s, d, o) => d.defaultFrameSettings,
                    FrameSettingsUI.Inspector
                )
            );
        }

        public static readonly CED.IDrawer Inspector;

        public static readonly CED.IDrawer SectionPrimarySettings = CED.Action(Drawer_SectionPrimarySettings);

        public FrameSettingsUI defaultFrameSettings = new FrameSettingsUI();
        public RenderPipelineSettingsUI renderPipelineSettings = new RenderPipelineSettingsUI();

        public HDRenderPipelineUI()
            : base(0)
        {
            
        }

        public override void Reset(SerializedHDRenderPipelineAsset data, UnityAction repaint)
        {
            renderPipelineSettings.Reset(data.renderPipelineSettings, repaint);
            defaultFrameSettings.Reset(data.defaultFrameSettings, repaint);
            base.Reset(data, repaint);
        }

        public override void Update()
        {
            renderPipelineSettings.Update();
            defaultFrameSettings.Update();
            base.Update();
        }

        static void Drawer_TitleDefaultFrameSettings(HDRenderPipelineUI s, SerializedHDRenderPipelineAsset d, Editor o)
        {
            EditorGUILayout.LabelField(_.GetContent("Default Frame Settings"), EditorStyles.boldLabel);
        }

        static void Drawer_SectionPrimarySettings(HDRenderPipelineUI s, SerializedHDRenderPipelineAsset d, Editor o)
        {
            EditorGUILayout.PropertyField(d.renderPipelineResources, _.GetContent("Render Pipeline Resources|Set of resources that need to be loaded when creating stand alone"));
            EditorGUILayout.PropertyField(d.diffusionProfileSettings, _.GetContent("Diffusion Profile Settings"));
        }
    }
}
