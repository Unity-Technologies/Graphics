using UnityEditor.AnimatedValues;
using UnityEngine.Events;

namespace UnityEditor.Experimental.Rendering
{
    using _ = CoreEditorUtils;
    using CED = CoreEditorDrawer<FrameSettingsUI, SerializedFrameSettings>;

    class FrameSettingsUI : BaseUI<SerializedFrameSettings>
    {
        static FrameSettingsUI()
        {
            Inspector = CED.Group(
                    SectionRenderingPasses,
                    SectionRenderingSettings,
                    SectionXRSettings,
                    SectionLightingSettings,
                    CED.Select(
                        (s, d, o) => s.lightLoopSettings,
                        (s, d, o) => d.lightLoopSettings,
                        LightLoopSettingsUI.SectionLightLoopSettings
                        )
                    );
        }

        public static CED.IDrawer Inspector;

        public static CED.IDrawer SectionRenderingPasses = CED.FoldoutGroup(
                "Rendering Passes",
                (s, p, o) => s.isSectionExpandedRenderingPasses,
                FoldoutOption.Indent,
                CED.LabelWidth(200, CED.Action(Drawer_SectionRenderingPasses))
                );

        public static CED.IDrawer SectionRenderingSettings = CED.FoldoutGroup(
                "Rendering Settings",
                (s, p, o) => s.isSectionExpandedRenderingSettings,
                FoldoutOption.Indent,
                CED.LabelWidth(300,
                    CED.Action(Drawer_FieldForwardRenderingOnly),
                    CED.FadeGroup(
                        (s, d, o, i) => s.isSectionExpandedUseForwardOnly,
                        FadeOption.None,
                        CED.Action(Drawer_FieldUseDepthPrepassWithDefferedRendering)
                        ),
                    CED.Action(Drawer_SectionOtherRenderingSettings)
                    )
                );

        public static CED.IDrawer SectionXRSettings = CED.FadeGroup(
                (s, d, o, i) => s.isSectionExpandedXRSupported,
                FadeOption.None,
                CED.FoldoutGroup(
                    "XR Settings",
                    (s, p, o) => s.isSectionExpandedXRSettings,
                    FoldoutOption.Indent,
                    CED.LabelWidth(200, CED.Action(Drawer_FieldStereoEnabled))));

        public static CED.IDrawer SectionLightingSettings = CED.FoldoutGroup(
                "Lighting Settings",
                (s, p, o) => s.isSectionExpandedLightingSettings,
                FoldoutOption.Indent,
                CED.LabelWidth(250, CED.Action(Drawer_SectionLightingSettings)));

        public AnimBool isSectionExpandedRenderingPasses { get { return m_AnimBools[0]; } }
        public AnimBool isSectionExpandedLightingSettings { get { return m_AnimBools[1]; } }
        public AnimBool isSectionExpandedRenderingSettings { get { return m_AnimBools[2]; } }
        public AnimBool isSectionExpandedXRSettings { get { return m_AnimBools[3]; } }
        public AnimBool isSectionExpandedXRSupported { get { return m_AnimBools[4]; } }
        public AnimBool isSectionExpandedUseForwardOnly { get { return m_AnimBools[5]; } }

        public LightLoopSettingsUI lightLoopSettings = new LightLoopSettingsUI();

        public FrameSettingsUI()
            : base(7)
        {
        }

        public override void Reset(SerializedFrameSettings data, UnityAction repaint)
        {
            lightLoopSettings.Reset(data.lightLoopSettings, repaint);
            base.Reset(data, repaint);
        }

        public override void Update()
        {
            isSectionExpandedXRSupported.target = PlayerSettings.virtualRealitySupported;
            isSectionExpandedUseForwardOnly.target = !data.enableForwardRenderingOnly.boolValue;
            lightLoopSettings.Update();
        }

        static void Drawer_SectionRenderingPasses(FrameSettingsUI s, SerializedFrameSettings p, Editor owner)
        {
            EditorGUILayout.PropertyField(p.enableTransparentPrepass, _.GetContent("Enable Transparent Prepass"));
            EditorGUILayout.PropertyField(p.enableTransparentPostpass, _.GetContent("Enable Transparent Postpass"));
            EditorGUILayout.PropertyField(p.enableMotionVectors, _.GetContent("Enable Motion Vectors"));
            EditorGUILayout.PropertyField(p.enableObjectMotionVectors, _.GetContent("Enable Object Motion Vectors"));
            EditorGUILayout.PropertyField(p.enableDBuffer, _.GetContent("Enable DBuffer"));
            EditorGUILayout.PropertyField(p.enableRoughRefraction, _.GetContent("Enable Rough Refraction"));
            EditorGUILayout.PropertyField(p.enableDistortion, _.GetContent("Enable Distortion"));
            EditorGUILayout.PropertyField(p.enablePostprocess, _.GetContent("Enable Postprocess"));
        }

        static void Drawer_FieldForwardRenderingOnly(FrameSettingsUI s, SerializedFrameSettings p, Editor owner)
        {
            EditorGUILayout.PropertyField(p.enableForwardRenderingOnly, _.GetContent("Enable Forward Rendering Only"));
        }

        static void Drawer_FieldUseDepthPrepassWithDefferedRendering(FrameSettingsUI s, SerializedFrameSettings p, Editor owner)
        {
            EditorGUILayout.PropertyField(p.enableDepthPrepassWithDeferredRendering, _.GetContent("Enable Depth Prepass With Deferred Rendering"));
        }

        static void Drawer_SectionOtherRenderingSettings(FrameSettingsUI s, SerializedFrameSettings p, Editor owner)
        {
            EditorGUILayout.PropertyField(p.enableAsyncCompute, _.GetContent("Enable Async Compute"));

            EditorGUILayout.PropertyField(p.enableOpaqueObjects, _.GetContent("Enable Opaque Objects"));
            EditorGUILayout.PropertyField(p.enableTransparentObjects, _.GetContent("Enable Transparent Objects"));

            EditorGUILayout.PropertyField(p.enableMSAA, _.GetContent("Enable MSAA"));
        }

        static void Drawer_FieldStereoEnabled(FrameSettingsUI s, SerializedFrameSettings p, Editor owner)
        {
            EditorGUILayout.PropertyField(p.enableStereo, _.GetContent("Enable Stereo"));
        }

        static void Drawer_SectionLightingSettings(FrameSettingsUI s, SerializedFrameSettings p, Editor owner)
        {
            EditorGUILayout.PropertyField(p.enableSSR, _.GetContent("Enable SSR"));
            EditorGUILayout.PropertyField(p.enableSSAO, _.GetContent("Enable SSAO"));
            EditorGUILayout.PropertyField(p.enableSubsurfaceScattering, _.GetContent("Enable Subsurface Scattering"));
            EditorGUILayout.PropertyField(p.enableTransmission, _.GetContent("Enable Transmission"));
            EditorGUILayout.PropertyField(p.enableAtmosphericScattering, _.GetContent("Enable Atmospheric Scattering"));
            EditorGUILayout.PropertyField(p.enableVolumetric, _.GetContent("    Enable Volumetric"));
            EditorGUILayout.PropertyField(p.enableShadow, _.GetContent("Enable Shadow"));
            EditorGUILayout.PropertyField(p.enableContactShadow, _.GetContent("Enable Contact Shadows"));
            EditorGUILayout.PropertyField(p.enableShadowMask, _.GetContent("Enable Shadow Masks"));
        }
    }
}
