using UnityEngine.UIElements;
using UnityEngine;

namespace UnityEditor.ShaderGraph.Drawing.Inspector
{
    public static class PropertyDrawerUtils
    {
        public static Label CreateLabel(string text, int indentLevel = 0, FontStyle fontStyle = FontStyle.Normal)
        {
            string label = new string(' ', indentLevel * 4);
            var labelVisualElement = new Label(label + text);
            labelVisualElement.style.unityFontStyleAndWeight = fontStyle;
            return labelVisualElement;
        }
    }
}
