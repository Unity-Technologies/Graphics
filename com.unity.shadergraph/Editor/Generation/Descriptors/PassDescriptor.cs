namespace UnityEditor.ShaderGraph
{
    [GenerationAPI]
    internal struct PassDescriptor
    {
        // Definition
        public string displayName;
        public string referenceName;
        public string lightMode;
        public bool useInPreview;
        public bool virtualTextureFeedback;

        // Templates
        public string passTemplatePath;
        public string[] sharedTemplateDirectories;

        // Port mask
        public BlockFieldDescriptor[] validVertexBlocks;
        public BlockFieldDescriptor[] validPixelBlocks;

        // Collections
        public StructCollection structs;
        public FieldCollection requiredFields;
        public DependencyCollection fieldDependencies;
        public RenderStateCollection renderStates;
        public PragmaCollection pragmas;
        public DefineCollection defines;
        public KeywordCollection keywords;
        public IncludeCollection includes;

        // Methods
        public bool Equals(PassDescriptor other)
        {
            return referenceName == other.referenceName;
        }
    }
}
