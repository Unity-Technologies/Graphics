using UnityEditor.AnimatedValues;
using UnityEngine.Events;

namespace UnityEditor.Experimental.Rendering
{
    using _ = CoreEditorUtils;
    using CED = CoreEditorDrawer<SerializedLightLoopSettingsUI, SerializedLightLoopSettings>;

    class SerializedLightLoopSettingsUI : SerializedUIBase
    {
        public static CED.IDrawer SectionLightLoopSettings = CED.FoldoutGroup(
            "Light Loop Settings",
            (s, p, o) => s.isSectionExpandedLightLoopSettings,
            true,
            CED.Action(Drawer_SectionLightLoopSettings));

        public AnimBool isSectionExpandedLightLoopSettings { get { return m_AnimBools[0]; } }

        public SerializedLightLoopSettingsUI()
            : base(1)
        {
        }

        static void Drawer_SectionLightLoopSettings(SerializedLightLoopSettingsUI s, SerializedLightLoopSettings p, Editor owner)
        {
            EditorGUILayout.PropertyField(p.enableTileAndCluster, _.GetContent("Enable Tile And Cluster"));
            EditorGUILayout.PropertyField(p.enableComputeLightEvaluation, _.GetContent("Enable Compute Light Evaluation"));
            EditorGUILayout.PropertyField(p.enableComputeLightVariants, _.GetContent("Enable Compute Light Variants"));
            EditorGUILayout.PropertyField(p.enableComputeMaterialVariants, _.GetContent("Enable Compute Material Variants"));

            EditorGUILayout.PropertyField(p.isFptlEnabled, _.GetContent("Enable FPTL"));
            EditorGUILayout.PropertyField(p.enableFptlForForwardOpaque, _.GetContent("Enable FPTL For Forward Opaque"));

            EditorGUILayout.PropertyField(p.enableBigTilePrepass, _.GetContent("Enable Big Tile Prepass"));
        }
    }
}
