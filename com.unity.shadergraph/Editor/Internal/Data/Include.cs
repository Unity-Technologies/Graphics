namespace UnityEditor.ShaderGraph.Internal
{
    public class ConditionalInclude : IConditionalShaderString
    {        
        public Include include { get; }
        public FieldCondition[] fieldConditions { get; }
        public string value => include.value;

        public ConditionalInclude(Include include)
        {
            this.include = include;
            this.fieldConditions = null;
        }

        public ConditionalInclude(Include include, FieldCondition fieldCondition)
        {
            this.include = include;
            this.fieldConditions = new FieldCondition[] { fieldCondition };
        }

        public ConditionalInclude(Include include, FieldCondition[] fieldConditions)
        {
            this.include = include;
            this.fieldConditions = fieldConditions;
        }
    }

    public class Include
    {
        public string value { get; }

        Include(string value)
        {
            this.value = value;
        }

        public static Include File(string value)
        {
            return new Include($"#include \"{value}\"");
        }
    }
}
