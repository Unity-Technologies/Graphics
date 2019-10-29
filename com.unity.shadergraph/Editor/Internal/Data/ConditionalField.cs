namespace UnityEditor.ShaderGraph.Internal
{
    public struct ConditionalField
    {
        public FieldDescriptor field { get; }
        public bool condition { get; }

        public ConditionalField(FieldDescriptor field, bool condition)
        {
            this.field = field;
            this.condition = condition;
        }
    }
}
