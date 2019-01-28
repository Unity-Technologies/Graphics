using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Experimental.Rendering.HDPipeline;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    using CED = CoreEditorDrawer<SerializedDynamicResolutionSettings>;

    static class GlobalDynamicResolutionSettingsUI
    {
        enum Expandable
        {
            DynamicResolutionSettings = 1 << 0
        }

        static readonly ExpandedState<Expandable, GlobalDynamicResolutionSettings> k_ExpandedState = new ExpandedState<Expandable, GlobalDynamicResolutionSettings>(Expandable.DynamicResolutionSettings, "HDRP");

        static readonly GUIContent k_HeaderContent          = EditorGUIUtility.TrTextContent("Dynamic resolution");
        static readonly GUIContent k_Enabled                = EditorGUIUtility.TrTextContent("Enabled");
        static readonly GUIContent k_MaxPercentage          = EditorGUIUtility.TrTextContent("Max Screen Percentage");
        static readonly GUIContent k_MinPercentage          = EditorGUIUtility.TrTextContent("Min Screen Percentage");
        static readonly GUIContent k_DynResType             = EditorGUIUtility.TrTextContent("Dynamic Resolution Type");
        static readonly GUIContent k_UpsampleFilter         = EditorGUIUtility.TrTextContent("Upscale filter");
        static readonly GUIContent k_ForceScreenPercentage  = EditorGUIUtility.TrTextContent("Force Screen Percentage");
        static readonly GUIContent k_ForcedScreenPercentage = EditorGUIUtility.TrTextContent("Forced Screen Percentage");


        static GlobalDynamicResolutionSettingsUI()
        {
            Inspector = CED.FoldoutGroup(
                k_HeaderContent,
                Expandable.DynamicResolutionSettings,
                k_ExpandedState,
                Drawer_SectionDynamicResolutionSettings
                );
        }

        public static readonly CED.IDrawer Inspector;

        static void Drawer_SectionDynamicResolutionSettings(SerializedDynamicResolutionSettings serialized, Editor owner)
        {
            EditorGUILayout.PropertyField(serialized.enabled, k_Enabled);

            if(serialized.enabled.boolValue)
            {
                EditorGUILayout.PropertyField(serialized.dynamicResType, k_DynResType);
                if ((DynamicResolutionType)serialized.dynamicResType.intValue == DynamicResolutionType.Software)
                {
                    EditorGUILayout.PropertyField(serialized.softwareUpsamplingFilter, k_UpsampleFilter);
                }
                if (!serialized.forcePercentage.boolValue)
                {
                    EditorGUILayout.DelayedFloatField(serialized.minPercentage, k_MinPercentage);
                    EditorGUILayout.DelayedFloatField(serialized.maxPercentage, k_MaxPercentage);
                }

                EditorGUILayout.PropertyField(serialized.forcePercentage, k_ForceScreenPercentage);
                if (serialized.forcePercentage.boolValue)
                {
                    EditorGUILayout.DelayedFloatField(serialized.forcedPercentage, k_ForcedScreenPercentage);
                }

            }


        }
    }
}
