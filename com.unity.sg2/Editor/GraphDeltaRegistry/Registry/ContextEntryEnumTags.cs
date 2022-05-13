namespace UnityEditor.ShaderGraph.GraphDelta
{
    public static class ContextEntryEnumTags
    {
        public const string kPropertyBlockUsage = "_PropertyBlockUsage";
        public const string kDisplayName = "_DisplayName";
        public const string kDefaultValue = "_DefaultValue";
        public const string kDataSource = "_DataSource";
        public enum PropertyBlockUsage
        {
            Included,
            //Hidden,
            Excluded
        }

        public enum DataSource
        {
            Global,
            PerMaterial,
            PerInstance,
            Constant
        }
    }
}
