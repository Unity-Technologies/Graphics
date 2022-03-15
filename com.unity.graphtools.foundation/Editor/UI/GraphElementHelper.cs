using System;
using System.Collections.Generic;
using System.IO;
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
                var tpl = LoadUxml(name + ".uxml");
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

        static string StylesheetPath = AssetHelper.AssetPath + "UI/Stylesheets/";
        static string TemplatePath = AssetHelper.AssetPath + "UI/Templates/";

        internal static void AddStylesheet(this VisualElement ve, string stylesheetName)
        {
            if (ve == null || stylesheetName == null)
                return;

            StyleSheet stylesheet = null;
            if (stylesheetName.StartsWith("Packages/"))
            {
                stylesheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(stylesheetName);
            }
            else
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

        /// <summary>
        /// Loads a stylesheet and the appropriate variant for the current skin.
        /// </summary>
        /// <remarks>If the stylesheet name is Common.uss and <see cref="EditorGUIUtility.isProSkin"/> is true,
        /// this method will load "Common.uss" and "Common_dark.uss". If <see cref="EditorGUIUtility.isProSkin"/> is false,
        /// this method will load "Common.uss" and "Common_light.uss".
        /// </remarks>
        /// <param name="ve">The visual element onto which to attach the stylesheets.</param>
        /// <param name="stylesheetName">The name of the common stylesheet.</param>
        internal static void AddStylesheetWithSkinVariants(this VisualElement ve, string stylesheetName)
        {
            var extension = Path.GetExtension(stylesheetName);
            var baseName = Path.ChangeExtension(stylesheetName, null);
            if (EditorGUIUtility.isProSkin)
            {
                AddStylesheet(ve, baseName + "_dark" + extension);
            }
            else
            {
                AddStylesheet(ve, baseName + "_light" + extension);
            }
            AddStylesheet(ve, stylesheetName);
        }


        internal static VisualTreeAsset LoadUxml(string uxmlName)
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
