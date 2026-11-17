using System.Collections.Generic;
using Unity.GraphAuthoring.Editor.ProviderSystem;

namespace UnityEditor.ShaderGraph.ProviderSystem
{
    static class ProviderMessageUtils
    {
        internal static void Collect(IReadOnlyList<Message> messages, IFieldCollection definition, List<NodeMessage> collected)
        {
            if (messages == null)
                return;

            List<string> subjects = new() { null };
            if (definition != null)
                foreach (var field in definition.Fields)
                    if (!subjects.Contains(field.Name))
                        subjects.Add(field.Name);
            foreach (var message in messages)
                if (!subjects.Contains(message.Subject))
                    subjects.Add(message.Subject);

            foreach (var subject in subjects)
                foreach (var message in messages)
                    if (message.Subject == subject)
                        collected.Add(new NodeMessage(subject, message.Text, (MessageType)message.Severity));
        }
    }
}
