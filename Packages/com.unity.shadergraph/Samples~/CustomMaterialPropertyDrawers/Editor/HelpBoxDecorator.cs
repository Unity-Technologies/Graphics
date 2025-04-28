using System;
using UnityEngine;

namespace UnityEditor.ShaderGraph.Samples
{
    public class HelpBoxDecorator : MaterialPropertyDrawer
    {
        string label, tooltip;
        MessageType messageType;
        float height = EditorGUIUtility.singleLineHeight;

        public HelpBoxDecorator() {}

        public HelpBoxDecorator(string label)
        {
            this.label = label;
        }

        public HelpBoxDecorator(string label, float height)
        {
            this.label = label;
            this.height = height;
        }

        public HelpBoxDecorator(string label, string tooltip)
        {
            this.label = label;

            if (!Enum.TryParse(tooltip.Trim(), out messageType))
                this.tooltip = tooltip;
        }

        public HelpBoxDecorator(string label, string tooltip, float height)
        {
            this.label = label;
            this.height = height;

            if (!Enum.TryParse(tooltip.Trim(), out messageType))
                this.tooltip = tooltip;
        }

        public override float GetPropertyHeight(MaterialProperty prop, string label, MaterialEditor editor) => height;

        public override void OnGUI(Rect position, MaterialProperty prop, String label, MaterialEditor editor)
        {
            var guiContent = new GUIContent(this.label, tooltip);
            if (messageType == MessageType.None)
                EditorGUI.HelpBox(position, guiContent);
            else
                EditorGUI.HelpBox(position, guiContent.text, messageType);
        }
    }
}
