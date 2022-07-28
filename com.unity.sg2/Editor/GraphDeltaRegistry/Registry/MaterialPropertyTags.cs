namespace UnityEditor.ShaderGraph.GraphDelta
{
    // TODO: figure out naming
    public static class MaterialPropertyTags
    {
        public const string kPropertyDescription = "PropertyDescription";

        public enum FloatMode
        {
            Default,
            Integer,  // Use the Integer property type
            Slider,   // Use the Range property type
            Enum      // Use the Float property type and add the [Enum] attribute
        }

        public const string kFloatMode = "FloatMode";
        public const string kFloatSliderMin = "SliderMin";
        public const string kFloatSliderMax = "SliderMax";

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
