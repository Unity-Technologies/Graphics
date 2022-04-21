using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.ShaderGraph.GraphUI
{
   static class GraphElementHelper
    {
        internal const string AssetPath = "Packages/com.unity.shadergraph/Editor/GraphUI/";

        internal static void LoadTemplate(VisualElement container, string name)
        {
            if (name != null && container != null)
            {
                var tpl = LoadUXML(name + ".uxml");
                tpl.CloneTree(container);
            }
        }

        internal static void LoadTemplateAndStylesheet(
            VisualElement container,
            string name,
            string rootClassName,
            IEnumerable<string> additionalStylesheets = null)
        {
            if (name == null)
            {
                Debug.LogWarning("Template not loaded because name is null.");
                return;
            }
            if (container == null)
            {
                Debug.LogWarning("Template not loaded because container is null.");
                return;
            }
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

        static string StylesheetPath = AssetPath + "GraphElements/Stylesheets/";
        static string NewLookStylesheetPath = AssetPath + "GraphElements/Stylesheets/NewLook/";
        static string TemplatePath = AssetPath + "GraphElements/Templates/";
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
