using System.IO;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Searcher
{
    static class VisualElementsHelpers
    {
        static readonly string StylesheetPath = "Packages/com.unity.graphtools.foundation/Searcher/Editor/Templates/";
        static readonly string TemplatePath = StylesheetPath;

        internal static void AddStylesheet(this VisualElement ve, string stylesheetName, bool ignoreFail = false)
        {
            AddStylesheetByPath(ve, StylesheetPath + stylesheetName, ignoreFail);
        }

        internal static void AddStylesheetByPath(this VisualElement ve, string stylesheetPath, bool ignoreFail = false)
        {
            if (ve == null)
                return;

            var stylesheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(stylesheetPath);
            if (stylesheet != null)
            {
                ve.styleSheets.Add(stylesheet);
            }
            else if (!ignoreFail)
            {
                Debug.Log("Failed to load stylesheet " + stylesheetPath);
            }
        }

        /// <summary>
        /// Loads a stylesheet and the appropriate variant for the current skin.
        /// </summary>
        /// <remarks>If the stylesheet name is Common.uss and <see cref="EditorGUIUtility.isProSkin"/> is <c>true</c>,
        /// this method will load "Common.uss" and "Common_dark.uss". If <see cref="EditorGUIUtility.isProSkin"/> is <c>false</c>,
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
                AddStylesheet(ve, baseName + "_dark" + extension, true);
            }
            else
            {
                AddStylesheet(ve, baseName + "_light" + extension, true);
            }
            AddStylesheet(ve, stylesheetName);
        }

        /// <summary>
        /// Loads a stylesheet and the appropriate variant for the current skin.
        /// </summary>
        /// <remarks>If the stylesheet name is Common.uss and <see cref="EditorGUIUtility.isProSkin"/> is <c>true</c>,
        /// this method will load "Common.uss" and "Common_dark.uss". If <see cref="EditorGUIUtility.isProSkin"/> is <c>false</c>,
        /// this method will load "Common.uss" and "Common_light.uss".
        /// </remarks>
        /// <param name="ve">The visual element onto which to attach the stylesheets.</param>
        /// <param name="stylesheetPath">The name of the common stylesheet with full path.</param>
        internal static void AddStylesheetWithSkinVariantsByPath(this VisualElement ve, string stylesheetPath)
        {
            var extension = Path.GetExtension(stylesheetPath);
            var baseName = Path.ChangeExtension(stylesheetPath, null);
            if (EditorGUIUtility.isProSkin)
            {
                AddStylesheetByPath(ve, baseName + "_dark" + extension, true);
            }
            else
            {
                AddStylesheetByPath(ve, baseName + "_light" + extension, true);
            }
            AddStylesheetByPath(ve, stylesheetPath);
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
