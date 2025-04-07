using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.ShaderGraph.Drawing
{
    class HelpBoxRow : VisualElement
    {
        // This element was originally reimplementing the entire HelpBox, but now that a UI Toolkit version is
        // available, we are wrapping that for better consistency.
        HelpBox m_HelpBox;

        // Space beneath the help box for actions, i.e. Upgrade/Dismiss buttons on out-of-date nodes.
        VisualElement m_ActionContainer;

        public override VisualElement contentContainer => m_ActionContainer;

        HelpBoxRow(string text, HelpBoxMessageType messageType)
        {
            m_HelpBox = new HelpBox(text, messageType);
            m_ActionContainer = new VisualElement();

            hierarchy.Add(m_HelpBox);
            hierarchy.Add(m_ActionContainer);
        }

        static HelpBoxMessageType ToHelpBoxMessageType(MessageType messageType) =>
            messageType switch
            {
                MessageType.Info => HelpBoxMessageType.Info,
                MessageType.Warning => HelpBoxMessageType.Warning,
                MessageType.Error => HelpBoxMessageType.Error,
                MessageType.None => HelpBoxMessageType.None,
                _ => HelpBoxMessageType.None
            };

        public HelpBoxRow(string text, MessageType messageType) : this(text, ToHelpBoxMessageType(messageType)) { }

        public static VisualElement CreateVariantLimitHelpBox(int currentVariantCount, int maxVariantCount) =>
            new HelpBoxRow("Variant limit exceeded. Hover for more info.", HelpBoxMessageType.Error)
            {
                tooltip = ShaderKeyword.kVariantLimitWarning
            };

        // Creates a standard prompt for upgrading a Shader Graph element.
        // If dismissAction is provided, a help box is created with an upgrade button and a dismiss button.
        // Otherwise, only an upgrade button is created.
        public static VisualElement CreateUpgradePrompt(
            string deprecatedTypeName,
            Action upgradeAction,
            Action dismissAction,
            string tooltip = null,
            string buttonText = null,
            string labelText = null,
            MessageType messageType = MessageType.Warning
        )
        {
            tooltip ??= GetDefaultDeprecationMessage(deprecatedTypeName);
            buttonText ??= "Update";
            labelText ??= GetDefaultDeprecationMessage(deprecatedTypeName);

            // If we are given a dismiss action, the user has not yet dismissed the warning and should be given the
            // full message. Otherwise, assume the warning has already been dismissed and show just the upgrade button.
            var displayFullWarning = dismissAction != null;
            var upgradeButton = new Button(upgradeAction) { text = buttonText, tooltip = tooltip };

            VisualElement container;

            if (displayFullWarning)
            {
                var dismissButton = new Button(dismissAction) { text = "Dismiss" };
                container = new HelpBoxRow(labelText, messageType);
                container.Add(upgradeButton);
                container.Add(dismissButton);
            }
            else
            {
                container = new VisualElement();
                container.Add(upgradeButton);
            }

            return container;

            static string GetDefaultDeprecationMessage(string typeName) =>
                $"The {typeName} has new updates. This version maintains the old behavior. " +
                $"If you update a {typeName}, you can use Undo to change it back. See the {typeName} " +
                "documentation for more information.";
        }
    }
}
