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

        public static VisualElement TryGetDeprecatedHelpBoxRow(string deprecatedTypeName, Action upgradeAction, string deprecationText = null, string buttonText = null, string labelText = null, MessageType messageType = MessageType.Warning)
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
                labelText = "DEPRECATED: Hover for info";
            }

            Button upgradeButton = new Button(upgradeAction) { text = buttonText, tooltip = deprecationText };
            if (!ShaderGraphPreferences.allowDeprecatedBehaviors || messageType == MessageType.Info)
            {
                HelpBoxRow help = new HelpBoxRow(messageType);
                var label = new Label(labelText)
                {
                    tooltip = deprecationText,
                    name = "message-" + (messageType == MessageType.Warning ? "warn" : "info")
                };
                help.Add(label);
                help.contentContainer.Add(upgradeButton);
                return help;
            }
            else
            {
                return upgradeButton;
            }
        }
    }
}
