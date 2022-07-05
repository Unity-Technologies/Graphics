namespace UnityEditor.ShaderGraph.Defs
{
    public class ReferenceValueDescriptor : IValueDescriptor
    {
        public string ContextName { get; private set; }

        public ReferenceValueDescriptor(string contextName)
        {
            ContextName = contextName;
        }
    }
}
