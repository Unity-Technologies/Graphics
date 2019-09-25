namespace UnityEditor.ShaderGraph.Internal
{
    struct FieldDependency
    {
        public IField field;             // the name of the thing
        public IField dependsOn;        // the thing above depends on this -- it reads it / calls it / requires it to be defined

        public FieldDependency(IField field, IField dependsOn)
        {
            this.field = field;
            this.dependsOn = dependsOn;
        }
    }
}
