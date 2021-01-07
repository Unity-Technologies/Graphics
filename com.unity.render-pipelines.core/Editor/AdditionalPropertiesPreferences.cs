using System;
using System.Linq;
using UnityEngine;
using UnityEditorInternal;

namespace UnityEditor.Rendering
{
    class AdditionalPropertiesPreferences
    {
        class Styles
        {
            public static readonly GUIContent additionalPropertiesLabel = new GUIContent("Additional Properties", "Toggle all additional properties to either visible or hidden.");
            public static readonly GUIContent[] additionalPropertiesNames = { new GUIContent("All Visible"), new GUIContent("All Hidden") };
            public static readonly int[] additionalPropertiesValues = { 1, 0 };
        }

        static Type[]   s_VolumeComponentEditorTypes;
        static bool     s_ShowAllAdditionalProperties = false;

        static AdditionalPropertiesPreferences()
        {
            s_ShowAllAdditionalProperties = EditorPrefs.GetBool(Keys.showAllAdditionalProperties);
        }

        static void Load()
        {
            LoadVolumeComponentEditorTypes();
        }

        static void LoadVolumeComponentEditorTypes()
        {
            if (s_VolumeComponentEditorTypes == null)
            {
                s_VolumeComponentEditorTypes = TypeCache.GetTypesDerivedFrom<VolumeComponentEditor>()
                    .Where(
                        t => !t.IsAbstract
                    ).ToArray();
            }
        }

        static bool showAllAdditionalProperties
        {
            get => s_ShowAllAdditionalProperties;
            set
            {
                s_ShowAllAdditionalProperties = value;
                EditorPrefs.SetBool(Keys.showAllAdditionalProperties, s_ShowAllAdditionalProperties);

                ShowAllAdditionalProperties(showAllAdditionalProperties);
            }
        }

        static class Keys
        {
            internal const string showAllAdditionalProperties = "General.ShowAllAdditionalProperties";
        }


        [SettingsProvider]
        static SettingsProvider AdditionalPropertiesGUI()
        {
            return new SettingsProvider("Preferences/_General/Additional Properties", SettingsScope.User)
            {
                guiHandler = searchContext =>
                {
                    EditorGUI.BeginChangeCheck();
                    int newValue = EditorGUILayout.IntPopup(Styles.additionalPropertiesLabel, showAllAdditionalProperties ? 1 : 0, Styles.additionalPropertiesNames, Styles.additionalPropertiesValues);
                    if (EditorGUI.EndChangeCheck())
                    {
                        showAllAdditionalProperties = newValue == 1 ? true : false;
                    }
                }
            };
        }

        static void ShowAllAdditionalProperties(bool value)
        {
            LoadVolumeComponentEditorTypes();

            // Volume components
            foreach (var editorType in s_VolumeComponentEditorTypes)
            {
                var editor = Activator.CreateInstance(editorType) as VolumeComponentEditor;
                editor.InitAdditionalPropertiesPreference();
                editor.SetAdditionalPropertiesPreference(value);
            }

            // Regular components

            InternalEditorUtility.RepaintAllViews();
        }
    }
}
