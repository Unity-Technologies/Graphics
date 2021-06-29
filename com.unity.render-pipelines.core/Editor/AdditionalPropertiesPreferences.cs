using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditorInternal;

namespace UnityEditor.Rendering
{
    /// <summary>
    /// Callback method that will be called when the Global Preferences for Additional Properties is changed
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    public sealed class SetAdditionalPropertiesVisibilityAttribute : Attribute
    {
    }

    class AdditionalPropertiesPreferences
    {
        class Styles
        {
            public static readonly GUIContent additionalPropertiesLabel = EditorGUIUtility.TrTextContent("Additional Properties", "Toggle all additional properties to either visible or hidden.");
            public static readonly GUIContent[] additionalPropertiesNames = { EditorGUIUtility.TrTextContent("All Visible"), EditorGUIUtility.TrTextContent("All Hidden") };
            public static readonly int[] additionalPropertiesValues = { 1, 0 };
        }

        static List<Type>                   s_VolumeComponentEditorTypes;
        static TypeCache.MethodCollection   s_AdditionalPropertiesVisibilityMethods;
        static bool                         s_ShowAllAdditionalProperties = false;
        static List<string>                 s_SearchKeywords;

        static AdditionalPropertiesPreferences()
        {
            s_ShowAllAdditionalProperties = EditorPrefs.GetBool(Keys.showAllAdditionalProperties);
        }

        static void InitializeIfNeeded()
        {
            if (s_VolumeComponentEditorTypes == null)
            {
                s_AdditionalPropertiesVisibilityMethods = TypeCache.GetMethodsWithAttribute<SetAdditionalPropertiesVisibilityAttribute>();

                s_VolumeComponentEditorTypes = TypeCache.GetTypesDerivedFrom<VolumeComponentEditor>()
                    .Where(
                        t => !t.IsAbstract
                    ).ToList();
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

        internal static void PreferenceGUI()
        {
            Rect r = EditorGUILayout.GetControlRect();
            r.xMin = 10;
            EditorGUIUtility.labelWidth = 251;

            EditorGUI.BeginChangeCheck();
            int newValue = EditorGUI.IntPopup(r, Styles.additionalPropertiesLabel, showAllAdditionalProperties ? 1 : 0, Styles.additionalPropertiesNames, Styles.additionalPropertiesValues);
            if (EditorGUI.EndChangeCheck())
            {
                showAllAdditionalProperties = newValue == 1;
            }
        }

        internal static List<string> GetPreferenceSearchKeywords()
        {
            if (s_SearchKeywords == null)
            {
                s_SearchKeywords = new List<string>();
                s_SearchKeywords.Add("Additional");
                s_SearchKeywords.Add("Properties");
            }
            return s_SearchKeywords;
        }

        static void ShowAllAdditionalProperties(bool value)
        {
            // The way we do this here is to gather all types of either VolumeComponentEditor or IAdditionalPropertiesBoolFlagsHandler (for regular components)
            // then we instantiate those classes in order to be able to call the relevant function to update the "ShowAdditionalProperties" flags.
            // The instance on which we call is not important because in the end it will only change a global editor preference.
            InitializeIfNeeded();

            // Volume components
            foreach (var editorType in s_VolumeComponentEditorTypes)
            {
                var editor = Activator.CreateInstance(editorType) as VolumeComponentEditor;
                editor.InitAdditionalPropertiesPreference();
                editor.SetAdditionalPropertiesPreference(value);
            }

            // Regular components
            foreach (var method in s_AdditionalPropertiesVisibilityMethods)
            {
                method.Invoke(null, new object[1] { value });
            }

            // Force repaint in case some editors are already open.
            InternalEditorUtility.RepaintAllViews();
        }
    }
}
