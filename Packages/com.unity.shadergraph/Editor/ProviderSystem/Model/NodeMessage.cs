namespace UnityEditor.ShaderGraph.ProviderSystem
{
    // A null subject is about the whole node.
    readonly struct NodeMessage
    {
        internal string subject { get; }
        internal string text { get; }
        internal MessageType severity { get; }

        internal NodeMessage(string subject, string text, MessageType severity)
        {
            this.subject = subject;
            this.text = text;
            this.severity = severity;
        }

        public override string ToString() => subject != null ? $"'{subject}': {text}" : text;
    }
}
