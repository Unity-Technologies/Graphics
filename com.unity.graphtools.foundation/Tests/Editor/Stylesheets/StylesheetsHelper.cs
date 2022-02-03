using System;
using UnityEngine.Assertions;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests
{
    static class StylesheetsHelper
    {
        internal static void AddTestStylesheet(this VisualElement ve, string stylesheetName)
        {
            if (ve == null)
                throw new ArgumentNullException(nameof(ve));

            const string stylesheetPath = "Packages/com.unity.graphtools.foundation/Tests/Editor/Stylesheets/";

            var stylesheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(stylesheetPath + stylesheetName);
            Assert.IsNotNull(stylesheet);
            ve.styleSheets.Add(stylesheet);
        }
    }
}
