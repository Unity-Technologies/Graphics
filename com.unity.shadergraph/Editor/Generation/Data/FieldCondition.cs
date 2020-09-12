namespace UnityEditor.ShaderGraph
{
    [GenerationAPI]
    internal class FieldCondition
    {
        public FieldDescriptor field { get; }
        public bool condition { get; }

        public FieldCondition(FieldDescriptor field, bool condition)
        {
            this.field = field;
            this.condition = condition;
        }
    }
}
