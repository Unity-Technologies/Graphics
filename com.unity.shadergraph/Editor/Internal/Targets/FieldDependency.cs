namespace UnityEditor.ShaderGraph.Internal
{
    struct FieldDependency
    {
        public IField name;             // the name of the thing
        public IField dependsOn;        // the thing above depends on this -- it reads it / calls it / requires it to be defined

        public FieldDependency(IField name, IField dependsOn)
        {
            this.name = name;
            this.dependsOn = dependsOn;
        }
    }
}
