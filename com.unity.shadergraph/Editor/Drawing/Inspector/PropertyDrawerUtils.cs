using System.Linq;
using UnityEngine.UIElements;

namespace Drawing.Inspector
{
    public static class PropertyDrawerUtils
    {
        public static Label CreateLabel(string text, int indentLevel = 0)
        {
            string label = new string(' ', indentLevel * 4);
            var labelVisualElement = new Label(label + text);
            return labelVisualElement;
        }
    }
}
