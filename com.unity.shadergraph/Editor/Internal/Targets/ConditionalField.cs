namespace UnityEditor.ShaderGraph.Internal
{
    public struct ConditionalField
    {
        public Field field { get; }
        public bool condition { get; }

        public ConditionalField(Field field, bool condition)
        {
            this.field = field;
            this.condition = condition;
        }
    }
}
