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
            VisualElement container = new VisualElement {name = "container"};
            m_ContentContainer = new VisualElement { name = "content"  };
            m_LabelContainer = new VisualElement {name = "label" };

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

        public static VisualElement TryGetDeprecatedHelpBoxRow(string deprecatedTypeName, Action upgradeAction)
        {
            string depString = $"The {deprecatedTypeName} has new updates. This version maintains the old behavior. " +
                               $"If you update a {deprecatedTypeName}, you can use Undo to change it back. See the {deprecatedTypeName} " +
                               $"documentation for more information.";
            Button upgradeButton = new Button(upgradeAction) { text = "Update" , tooltip = depString};
            if (!ShaderGraphPreferences.allowDeprecatedBehaviors)
            {
                HelpBoxRow help = new HelpBoxRow(MessageType.Warning);
                var label = new Label("DEPRECATED: Hover for info")
                {
                    tooltip = depString
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
