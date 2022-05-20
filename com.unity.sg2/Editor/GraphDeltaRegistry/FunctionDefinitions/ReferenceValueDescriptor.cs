namespace UnityEditor.ShaderGraph.Defs
{
    public class ReferenceValueDescriptor : IValueDescriptor
    {
        public string Tag { get; private set; }

        public ReferenceValueDescriptor(string tag)
        {
            Tag = tag;
        }
    }
}
