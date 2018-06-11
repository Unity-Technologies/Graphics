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
                FoldoutOption.Indent,
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
            // Uncomment if you re-enable LIGHTLOOP_SINGLE_PASS multi_compile in lit*.shader
            //EditorGUILayout.PropertyField(p.enableTileAndCluster, _.GetContent("Enable Tile And Cluster"));
            //EditorGUI.indentLevel++;

            GUILayout.BeginVertical();
            if (s.isSectionExpandedEnableTileAndCluster.target)
            {
                EditorGUILayout.PropertyField(p.enableFptlForForwardOpaque, _.GetContent("Enable FPTL For Forward Opaque"));
                EditorGUILayout.PropertyField(p.enableBigTilePrepass, _.GetContent("Enable Big Tile Prepass"));
                EditorGUILayout.PropertyField(p.enableComputeLightEvaluation, _.GetContent("Enable Compute Light Evaluation"));
                GUILayout.BeginVertical();
                if (s.isSectionExpandedComputeLightEvaluation.target)
                {
                    EditorGUI.indentLevel++;
                    EditorGUILayout.PropertyField(p.enableComputeLightVariants, _.GetContent("Enable Compute Light Variants"));
                    EditorGUILayout.PropertyField(p.enableComputeMaterialVariants, _.GetContent("Enable Compute Material Variants"));
                    EditorGUI.indentLevel--;
                }
                GUILayout.EndVertical();
            }
            GUILayout.EndVertical();

            //EditorGUI.indentLevel--;
        }
    }
}
