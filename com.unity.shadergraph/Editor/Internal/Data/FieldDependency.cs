namespace UnityEditor.ShaderGraph.Internal
{
    public struct FieldDependency
    {
        public IField field;
        public IField dependsOn;

        public FieldDependency(IField field, IField dependsOn)
        {
            this.field = field;
            this.dependsOn = dependsOn;
        }
    }
}
