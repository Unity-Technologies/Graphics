using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    static class GraphElementHelper
    {
        internal static void LoadTemplateAndStylesheet(VisualElement container, string name, string rootClassName, IEnumerable<string> additionalStylesheets = null)
        {
            if (name != null && container != null)
            {
                var tpl = LoadUXML(name + ".uxml");
                tpl.CloneTree(container);

                if (additionalStylesheets != null)
                {
                    foreach (var additionalStylesheet in additionalStylesheets)
                    {
                        container.AddStylesheet(additionalStylesheet + ".uss");
                    }
                }

                container.AddStylesheet(name + ".uss");
            }
        }

        static string StylesheetPath = AssetHelper.AssetPath + "GraphElements/Stylesheets/";
        static string NewLookStylesheetPath = AssetHelper.AssetPath + "GraphElements/Stylesheets/NewLook/";
        static string TemplatePath = AssetHelper.AssetPath + "GraphElements/Templates/";
        internal static bool UseNewStylesheets { get; set; }

        internal static void AddStylesheet(this VisualElement ve, string stylesheetName)
        {
            StyleSheet stylesheet = null;

            // ReSharper disable once ConditionIsAlwaysTrueOrFalse
            if (UseNewStylesheets)
                stylesheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(NewLookStylesheetPath + stylesheetName);

            // ReSharper disable once ConditionIsAlwaysTrueOrFalse
            if (stylesheet == null)
            {
                stylesheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(StylesheetPath + stylesheetName);
            }

            if (stylesheet != null)
            {
                ve.styleSheets.Add(stylesheet);
            }
            else
            {
                Debug.Log("Failed to load stylesheet " + StylesheetPath + stylesheetName);
            }
        }

        internal static VisualTreeAsset LoadUXML(string uxmlName)
        {
            var tpl = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(TemplatePath + uxmlName);
            if (tpl == null)
            {
                Debug.Log("Failed to load template " + TemplatePath + uxmlName);
            }

            return tpl;
        }
    }
}
