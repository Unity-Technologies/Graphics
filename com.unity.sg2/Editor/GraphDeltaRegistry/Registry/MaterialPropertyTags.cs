namespace UnityEditor.ShaderGraph.GraphDelta
{
    public static class MaterialPropertyTags
    {
        public const string kPropertyDescription = "PropertyDescription";

        public enum FloatMode
        {
            Default,  // Do not change the generated type
            Slider,   // Use the Range property type

            // TODO (Joe): Handle the remaining modes from SG1
            // Integer,
            // Enum,
        }

        public const string kFloatMode = "FloatMode";
        public const string kRangeMin = "RangeMin";
        public const string kRangeMax = "RangeMax";

        public const string kIsColor = "IsColor";
        public const string kIsHdr = "IsHdr";
    }

    public static class MaterialPropertyHelpers
    {
        public static FieldHandler GetPropertyDescription(this PortHandler contextEntryPort)
        {
            return contextEntryPort.GetField(MaterialPropertyTags.kPropertyDescription) ??
                contextEntryPort.AddField(MaterialPropertyTags.kPropertyDescription);
        }
    }
}
