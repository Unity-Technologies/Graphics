using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Experimental.Rendering.HDPipeline;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    using CED = CoreEditorDrawer<SerializedGlobalDecalSettings>;
    
    static class GlobalDecalSettingsUI
    {
        enum Expandable
        {
            DecalSettings = 1 << 0
        }

        readonly static ExpandedState<Expandable, GlobalDecalSettings> k_ExpandedState = new ExpandedState<Expandable, GlobalDecalSettings>(Expandable.DecalSettings, "HDRP");

        static readonly GUIContent k_HeaderContent = CoreEditorUtils.GetContent("Decals");

        static readonly GUIContent k_DrawDistanceContent = CoreEditorUtils.GetContent("Draw Distance");
        static readonly GUIContent k_AtlasWidthContent = CoreEditorUtils.GetContent("Atlas Width");
        static readonly GUIContent k_AtlasHeightContent = CoreEditorUtils.GetContent("Atlas Height");
        static readonly GUIContent k_MetalAndAOContent = CoreEditorUtils.GetContent("Metal and AO properties");

        static GlobalDecalSettingsUI()
        {
            Inspector = CED.FoldoutGroup(
                k_HeaderContent,
                Expandable.DecalSettings,
                k_ExpandedState,
                Drawer_SectionDecalSettings
                );
        }
        
        public static readonly CED.IDrawer Inspector;
        
        static void Drawer_SectionDecalSettings(SerializedGlobalDecalSettings serialized, Editor owner)
        {
            EditorGUILayout.PropertyField(serialized.drawDistance, k_DrawDistanceContent);
            EditorGUILayout.DelayedIntField(serialized.atlasWidth, k_AtlasWidthContent);
            EditorGUILayout.DelayedIntField(serialized.atlasHeight, k_AtlasHeightContent);
            EditorGUILayout.PropertyField(serialized.perChannelMask, k_MetalAndAOContent);

            // Clamp input values
            serialized.drawDistance.intValue = Mathf.Max(serialized.drawDistance.intValue, 0);
            serialized.atlasWidth.intValue = Mathf.Max(serialized.atlasWidth.intValue, 0);
            serialized.atlasHeight.intValue = Mathf.Max(serialized.atlasHeight.intValue, 0);
        }
    }
}
