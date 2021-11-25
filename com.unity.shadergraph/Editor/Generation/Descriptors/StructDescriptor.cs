namespace UnityEditor.ShaderGraph
{
    [GenerationAPI]
    internal struct StructDescriptor
    {
        public string name;
        public bool packFields;
        public bool populateWithCustomInterpolators;
        public FieldDescriptor[] fields;
    }
}
