using UnityEngine;
using UnityEngine.UIElements;

namespace Drawing.Inspector
{
    public static class PropertyDrawerUtils
    {
        public static Label CreateLabel(string text, int indentLevel = 0)
        {
            string label = "";
            for (var i = 0; i < indentLevel; i++)
            {
                label += "    ";
            }

            var labelVisualElement = new Label(label + text);
            //labelVisualElement.styleSheets.Add(Resources.Load<StyleSheet>("Styles/InspectorLabel"));
            return labelVisualElement;
        }
    }

    // #TODO: Add property raw default instantiation and styling code here that all property drawers should use,
    // to reduce cost of refactor in future if its ever needed
}
