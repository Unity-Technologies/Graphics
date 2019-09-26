namespace UnityEditor.ShaderGraph.Internal
{
    public class FieldCondition
    {
        public IField field { get; }
        public bool condition { get; }

        public FieldCondition(IField field, bool condition)
        {
            this.field = field;
            this.condition = condition;
        }
    }
}
