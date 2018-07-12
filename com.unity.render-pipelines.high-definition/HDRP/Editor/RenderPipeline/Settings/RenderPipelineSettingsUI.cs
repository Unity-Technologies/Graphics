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
            EditorGUILayout.PropertyField(d.supportShadowMask, _.GetContent("Support Shadow Mask|Enable memory (Extra Gbuffer in deferred) and shader variant for shadow mask."));
            EditorGUILayout.PropertyField(d.supportSSR, _.GetContent("Support SSR|Enable memory use by SSR effect."));
            EditorGUILayout.PropertyField(d.supportSSAO, _.GetContent("Support SSAO|Enable memory use by SSAO effect."));
            EditorGUILayout.PropertyField(d.supportDBuffer, _.GetContent("Support Decal Buffer|Enable memory and variant of decal buffer."));
            // TODO: Implement MSAA - Hide for now as it doesn't work
            //EditorGUILayout.PropertyField(d.supportMSAA, _.GetContent("Support Multi Sampling Anti-Aliasing|This feature doesn't work currently."));
            //EditorGUILayout.PropertyField(d.MSAASampleCount, _.GetContent("MSAA Sample Count|Allow to select the level of MSAA."));
            EditorGUILayout.PropertyField(d.supportSubsurfaceScattering, _.GetContent("Support Subsurface Scattering"));
            EditorGUILayout.PropertyField(d.supportOnlyForward, _.GetContent("Support Only Forward|Remove all the memory and shader variant of GBuffer. The renderer can be switch to deferred anymore."));
            EditorGUILayout.PropertyField(d.supportMotionVectors, _.GetContent("Support Motion Vectors|Motion vector are use for Motion Blur, TAA, temporal re-projection of various effect like SSR."));
            EditorGUILayout.PropertyField(d.supportStereo, _.GetContent("Support Stereo Rendering"));
            EditorGUILayout.PropertyField(d.increaseSssSampleCount, _.GetContent("Increase SSS Sample Count|This allows for better SSS quality. Warning: high performance cost, do not enable on consoles."));
            EditorGUILayout.PropertyField(d.supportVolumetrics, _.GetContent("Support volumetrics|Enable memory and shader variant for volumetric."));
            EditorGUILayout.PropertyField(d.increaseResolutionOfVolumetrics, _.GetContent("Increase resolution of volumetrics|Increase the resolution of volumetric lighting buffers. Warning: high performance cost, do not enable on consoles."));
            EditorGUILayout.PropertyField(d.supportRuntimeDebugDisplay, _.GetContent("Support runtime debug display|Remove all debug display shader variant only in the player. Allow faster build."));
            EditorGUILayout.PropertyField(d.supportDitheringCrossFade, _.GetContent("Support dithering cross fade|Remove all dithering cross fade shader variant only in the player. Allow faster build."));

            --EditorGUI.indentLevel;
        }
    }
}
