using System.Collections.Generic;
using System.Text;
using UnityEditor.ShaderGraph.Drawing;
using UnityEngine.UIElements;

namespace UnityEditor.ShaderGraph.ProviderSystem
{
    static class ProviderInspectorUtils
    {
        const string kClass = "sg-provider-inspector";

        internal static VisualElement AddSection(VisualElement parentElement, string title)
        {
            var section = new VisualElement();
            ProviderStyleSheets.Add(section, ProviderStyleSheets.kProviderInspector);
            var header = new Label(title);
            header.AddToClassList($"{kClass}__header");
            section.Add(header);

            var rows = new VisualElement();
            rows.AddToClassList($"{kClass}__rows");
            section.Add(rows);
            parentElement.Add(section);
            return rows;
        }

        internal static void AddValue(VisualElement section, string label, string value)
        {
            var text = new Label(value);
            text.selection.isSelectable = true;
            text.AddToClassList($"{kClass}__value");
            var row = new PropertyRow(new Label(label));
            row.Add(text);
            section.Add(row);
        }

        internal static void AddBlock(VisualElement section, string label, string value)
        {
            var caption = new Label(label);
            caption.AddToClassList($"{kClass}__caption");
            section.Add(caption);
            section.Add(new HelpBoxRow(value, MessageType.None));
        }

        internal static void AddSetting(VisualElement section, string label, string tooltip, VisualElement field)
        {
            var row = new PropertyRow(new Label(label) { tooltip = tooltip });
            row.Add(field);
            section.Add(row);
        }

        internal static void AddMessages(VisualElement parentElement, IReadOnlyList<NodeMessage> messages)
        {
            if (messages.Count == 0)
                return;

            var section = AddSection(parentElement, "Messages");
            StringBuilder sb = new();
            for (int i = 0; i < messages.Count;)
            {
                var subject = messages[i].subject;
                var worstSeverity = messages[i].severity;
                sb.Clear();
                for (; i < messages.Count && messages[i].subject == subject; i++)
                {
                    sb.AppendLine(messages[i].ToString());
                    if (messages[i].severity > worstSeverity)
                        worstSeverity = messages[i].severity;
                }
                section.Add(new HelpBoxRow(sb.ToString().TrimEnd(), worstSeverity));
            }
        }
    }
}
