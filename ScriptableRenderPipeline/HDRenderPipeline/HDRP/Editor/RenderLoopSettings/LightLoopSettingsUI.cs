using UnityEditor.AnimatedValues;
using UnityEngine;

namespace UnityEditor.Experimental.Rendering
{
    using _ = CoreEditorUtils;
    using CED = CoreEditorDrawer<LightLoopSettingsUI, SerializedLightLoopSettings>;

    class LightLoopSettingsUI : BaseUI<SerializedLightLoopSettings>
    {
        public static CED.IDrawer SectionLightLoopSettings = CED.FoldoutGroup(
            "Light Loop Settings",
            (s, p, o) => s.isSectionExpandedLightLoopSettings,
            true,
            CED.LabelWidth(250, CED.Action(Drawer_SectionLightLoopSettings)));

        public AnimBool isSectionExpandedLightLoopSettings { get { return m_AnimBools[0]; } }
        public AnimBool isSectionExpandedEnableTileAndCluster { get { return m_AnimBools[1]; } }
        public AnimBool isSectionExpandedComputeLightEvaluation { get { return m_AnimBools[2]; } }

        public LightLoopSettingsUI()
            : base(3)
        {
        }

        public override void Update()
        {
            isSectionExpandedEnableTileAndCluster.target = data.enableTileAndCluster.boolValue;
            isSectionExpandedComputeLightEvaluation.target = data.enableComputeLightEvaluation.boolValue;
            base.Update();
        }

        static void Drawer_SectionLightLoopSettings(LightLoopSettingsUI s, SerializedLightLoopSettings p, Editor owner)
        {
            EditorGUILayout.PropertyField(p.enableTileAndCluster, _.GetContent("Enable Tile And Cluster"));
            GUILayout.BeginVertical();
            if (EditorGUILayout.BeginFadeGroup(s.isSectionExpandedEnableTileAndCluster.faded))
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(p.isFptlEnabled, _.GetContent("Enable FPTL"));
                EditorGUILayout.PropertyField(p.enableFptlForForwardOpaque, _.GetContent("Enable FPTL For Forward Opaque"));
                EditorGUILayout.PropertyField(p.enableBigTilePrepass, _.GetContent("Enable Big Tile Prepass"));
                EditorGUILayout.PropertyField(p.enableComputeLightEvaluation, _.GetContent("Enable Compute Light Evaluation"));
                GUILayout.BeginVertical();
                if (EditorGUILayout.BeginFadeGroup(s.isSectionExpandedComputeLightEvaluation.faded))
                {
                    EditorGUI.indentLevel++;
                    EditorGUILayout.PropertyField(p.enableComputeLightVariants, _.GetContent("Enable Compute Light Variants"));
                    EditorGUILayout.PropertyField(p.enableComputeMaterialVariants, _.GetContent("Enable Compute Material Variants"));
                    EditorGUI.indentLevel--;
                }
                EditorGUILayout.EndFadeGroup();
                GUILayout.EndVertical();
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndFadeGroup();
            GUILayout.EndVertical();
        }
    }
}
