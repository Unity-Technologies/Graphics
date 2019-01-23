using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Experimental.Rendering.HDPipeline;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    using CED = CoreEditorDrawer<SerializedGlobalPostProcessSettings>;

    static class GlobalPostProcessSettingsUI
    {
        enum Expandable
        {
            PostProcessSettings = 1 << 0
        }

        static readonly ExpandedState<Expandable, GlobalPostProcessSettings> k_ExpandedState = new ExpandedState<Expandable, GlobalPostProcessSettings>(Expandable.PostProcessSettings, "HDRP");

        static readonly GUIContent k_HeaderContent = EditorGUIUtility.TrTextContent("Post-processing");
        static readonly GUIContent k_LutSize = EditorGUIUtility.TrTextContent("Grading LUT Size");
        static readonly GUIContent k_LutFormat = EditorGUIUtility.TrTextContent("Grading LUT Format");

        static GlobalPostProcessSettingsUI()
        {
            Inspector = CED.FoldoutGroup(
                k_HeaderContent,
                Expandable.PostProcessSettings,
                k_ExpandedState,
                Drawer_SectionPostProcessSettings
            );
        }

        public static readonly CED.IDrawer Inspector;

        static void Drawer_SectionPostProcessSettings(SerializedGlobalPostProcessSettings serialized, Editor owner)
        {
            EditorGUILayout.DelayedIntField(serialized.lutSize, k_LutSize);
            serialized.lutSize.intValue = Mathf.Clamp(serialized.lutSize.intValue, GlobalPostProcessSettings.k_MinLutSize, GlobalPostProcessSettings.k_MaxLutSize);

            EditorGUILayout.PropertyField(serialized.lutFormat, k_LutFormat);
        }
    }
}
