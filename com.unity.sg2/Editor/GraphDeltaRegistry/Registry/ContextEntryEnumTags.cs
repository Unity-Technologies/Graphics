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

        public enum FloatDisplayType
        {
            Default,
            Slider,
        }

        public const string kFloatDisplayType = "FloatDisplayType";
        public const string kFloatRangeMin = "FloatRangeMin";
        public const string kFloatRangeMax = "FloatRangeMax";

        public const string kIsColor = "IsColor";
        public const string kIsHdr = "IsHdr";

        public enum TextureDefaultType
        {
            White,
            Black,
            Grey,
            NormalMap,
            LinearGrey,
            Red
        }

        public const string kTextureDefaultType = "TextureDefaultType";
        public const string kTextureUseTilingOffset = "TextureUseTilingOffset";
    }
}
