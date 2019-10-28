namespace UnityEditor.ShaderGraph.Internal
{
    public struct ConditionalField
    {
        public IField field { get; }
        public bool condition { get; }

        public ConditionalField(IField field, bool condition)
        {
            this.field = field;
            this.condition = condition;
        }
    }
}
