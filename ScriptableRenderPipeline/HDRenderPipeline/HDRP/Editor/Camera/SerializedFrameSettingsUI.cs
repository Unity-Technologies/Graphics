using UnityEditor.AnimatedValues;
using UnityEngine.Events;

namespace UnityEditor.Experimental.Rendering
{
    using _ = CoreEditorUtils;
    using CED = CoreEditorDrawer<SerializedFrameSettingsUI, SerializedFrameSettings>;

    class SerializedFrameSettingsUI : SerializedUIBase
    {
        public static CED.IDrawer SectionRenderingPasses = CED.FoldoutGroup(
            "Rendering Passes",
            (s, p, o) => s.isSectionExpandedRenderingPasses,
            true,
            CED.Action(Drawer_SectionRenderingPasses));

        public static CED.IDrawer SectionRenderingSettings = CED.FoldoutGroup(
            "Rendering Settings",
            (s, p, o) => s.isSectionExpandedRenderingSettings,
            true,
            CED.Action(Drawer_SectionRenderingSettings));

        public static CED.IDrawer SectionXRSettings = CED.FadeGroup(
            (s, d, o, i) => s.isSectionExpandedXRSupported.faded,
            false,
            CED.FoldoutGroup(
                "XR Settings",
                (s, p, o) => s.isSectionExpandedXRSettings,
                true,
                CED.Action(Drawer_FieldStereoEnabled)));

        public static CED.IDrawer SectionLightingSettings = CED.FoldoutGroup(
            "Lighting Settings",
            (s, p, o) => s.isSectionExpandedLightingSettings,
            true,
            CED.Action(Drawer_SectionLightingSettings));

        public AnimBool isSectionExpandedRenderingPasses { get { return m_AnimBools[0]; } }
        public AnimBool isSectionExpandedLightingSettings { get { return m_AnimBools[1]; } }
        public AnimBool isSectionExpandedRenderingSettings { get { return m_AnimBools[2]; } }
        public AnimBool isSectionExpandedXRSettings { get { return m_AnimBools[3]; } }
        public AnimBool isSectionExpandedXRSupported { get { return m_AnimBools[4]; } }

        public SerializedLightLoopSettingsUI serializedLightLoopSettingsUI = new SerializedLightLoopSettingsUI();

        public SerializedFrameSettingsUI()
             : base(5)
        {
        }

        public override void Reset(UnityAction repaint)
        {
            base.Reset(repaint);
            serializedLightLoopSettingsUI.Reset(repaint);
        }

        public override void Update()
        {
            isSectionExpandedXRSupported.target = PlayerSettings.virtualRealitySupported;
            serializedLightLoopSettingsUI.Update();
        }

        static void Drawer_SectionRenderingPasses(SerializedFrameSettingsUI s, SerializedFrameSettings p, Editor owner)
        {
            EditorGUILayout.PropertyField(p.enableTransparentPrepass, _.GetContent("Enable Transparent Prepass"));
            EditorGUILayout.PropertyField(p.enableTransparentPostpass, _.GetContent("Enable Transparent Postpass"));
            EditorGUILayout.PropertyField(p.enableMotionVectors, _.GetContent("Enable Motion Vectors"));
            EditorGUILayout.PropertyField(p.enableObjectMotionVectors, _.GetContent("Enable Object Motion Vectors"));
            EditorGUILayout.PropertyField(p.enableDBuffer, _.GetContent("Enable DBuffer"));
            EditorGUILayout.PropertyField(p.enableAtmosphericScattering, _.GetContent("Enable Atmospheric Scattering"));
            EditorGUILayout.PropertyField(p.enableRoughRefraction, _.GetContent("Enable Rough Refraction"));
            EditorGUILayout.PropertyField(p.enableDistortion, _.GetContent("Enable Distortion"));
            EditorGUILayout.PropertyField(p.enablePostprocess, _.GetContent("Enable Postprocess"));
        }

        static void Drawer_SectionRenderingSettings(SerializedFrameSettingsUI s, SerializedFrameSettings p, Editor owner)
        {
            EditorGUILayout.PropertyField(p.enableForwardRenderingOnly, _.GetContent("Enable Forward Rendering Only"));
            EditorGUILayout.PropertyField(p.enableDepthPrepassWithDeferredRendering, _.GetContent("Enable Depth Prepass With Deferred Rendering"));
            EditorGUILayout.PropertyField(p.enableAlphaTestOnlyInDeferredPrepass, _.GetContent("Enable Alpha Test Only In Deferred Prepass"));

            EditorGUILayout.PropertyField(p.enableAsyncCompute, _.GetContent("Enable Async Compute"));

            EditorGUILayout.PropertyField(p.enableOpaqueObjects, _.GetContent("Enable Opaque Objects"));
            EditorGUILayout.PropertyField(p.enableTransparentObjects, _.GetContent("Enable Transparent Objects"));

            EditorGUILayout.PropertyField(p.enableMSAA, _.GetContent("Enable MSAA"));
        }

        static void Drawer_FieldStereoEnabled(SerializedFrameSettingsUI s, SerializedFrameSettings p, Editor owner)
        {
            EditorGUILayout.PropertyField(p.enableStereo, _.GetContent("Enable Stereo"));
        }

        static void Drawer_SectionLightingSettings(SerializedFrameSettingsUI s, SerializedFrameSettings p, Editor owner)
        {
            EditorGUILayout.PropertyField(p.enableSSR, _.GetContent("Enable SSR"));
            EditorGUILayout.PropertyField(p.enableSSAO, _.GetContent("Enable SSAO"));
            EditorGUILayout.PropertyField(p.enableSSSAndTransmission, _.GetContent("Enable SSS And Transmission"));
            EditorGUILayout.PropertyField(p.enableShadow, _.GetContent("Enable Shadow"));
            EditorGUILayout.PropertyField(p.enableShadowMask, _.GetContent("Enable Shadow Masks"));
        }
    }
}
