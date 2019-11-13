namespace UnityEditor.ShaderGraph.Internal
{
    public struct FieldDependency
    {
        public FieldDescriptor field;
        public FieldDescriptor dependsOn;

        public FieldDependency(FieldDescriptor field, FieldDescriptor dependsOn)
        {
            this.field = field;
            this.dependsOn = dependsOn;
        }
    }
}
