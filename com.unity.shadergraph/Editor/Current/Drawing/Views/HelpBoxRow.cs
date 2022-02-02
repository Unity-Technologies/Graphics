using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.ShaderGraph.Drawing
{
    // Similar in function to the old  EditorGUILayout.HelpBox
    class HelpBoxRow : VisualElement
    {
        VisualElement m_ContentContainer;
        VisualElement m_LabelContainer;

        public override VisualElement contentContainer
        {
            get { return m_ContentContainer; }
        }

        public HelpBoxRow(MessageType type)
        {
            styleSheets.Add(Resources.Load<StyleSheet>("Styles/HelpBoxRow"));
            VisualElement container = new VisualElement { name = "container" };
            m_ContentContainer = new VisualElement { name = "content" };
            m_LabelContainer = new VisualElement { name = "label" };

            switch (type)
            {
                case MessageType.None:
                    container.AddToClassList("help-box-row-style-info");
                    break;
                case MessageType.Info:
                    container.AddToClassList("help-box-row-style-info");
                    break;
                case MessageType.Warning:
                    container.AddToClassList("help-box-row-style-warning");
                    break;
                case MessageType.Error:
                    container.AddToClassList("help-box-row-style-error");
                    break;
                default:
                    break;
            }

            container.Add(m_LabelContainer);
            container.Add(m_ContentContainer);

            hierarchy.Add(container);
        }

        public static VisualElement CreateVariantLimitHelpBox(int currentVariantCount, int maxVariantCount)
        {
            var messageType = MessageType.Error;
            HelpBoxRow help = new HelpBoxRow(messageType);
            var label = new Label("Variant limit exceeded: Hover for more info")
            {
                tooltip = ShaderKeyword.kVariantLimitWarning,
                name = "message-" + (messageType == MessageType.Warning ? "warn" : "info")
            };
            help.Add(label);
            return help;
        }

        public static VisualElement TryGetDeprecatedHelpBoxRow(string deprecatedTypeName, Action upgradeAction, Action dismissAction, string deprecationText = null, string buttonText = null, string labelText = null, MessageType messageType = MessageType.Warning)
        {
            if (deprecationText == null)
            {
                deprecationText = $"The {deprecatedTypeName} has new updates. This version maintains the old behavior. " +
                    $"If you update a {deprecatedTypeName}, you can use Undo to change it back. See the {deprecatedTypeName} " +
                    $"documentation for more information.";
            }
            if (buttonText == null)
            {
                buttonText = "Update";
            }
            if (labelText == null)
            {
                labelText = $"The {deprecatedTypeName} has new updates. This version maintains the old behavior. " +
                    $"If you update a {deprecatedTypeName}, you can use Undo to change it back. See the {deprecatedTypeName} " +
                    $"documentation for more information.";
            }

            Button upgradeButton = new Button(upgradeAction) { text = buttonText, tooltip = deprecationText };
            Button dismissButton = null;
            if (dismissAction != null)
                dismissButton = new Button(dismissAction) { text = "Dismiss" };

            if (dismissAction != null)
            {
                HelpBoxRow help = new HelpBoxRow(messageType);
                var label = new Label(labelText)
                {
                    tooltip = labelText,
                    name = "message-" + (messageType == MessageType.Warning ? "warn" : "info")
                };
                help.Add(label);
                help.contentContainer.Add(upgradeButton);
                if (dismissButton != null)
                    help.contentContainer.Add(dismissButton);
                return help;
            }
            else
            {
                var box = new VisualElement();
                box.Add(upgradeButton);
                if (dismissButton != null)
                    box.Add(dismissButton);
                return box;
            }
        }
    }
}
