namespace UnityEditor.Rendering
{
    /// <summary>
    /// A structure holding the information for each Item that needs to be Converted.
    /// Descriptor = The ConverterItemDescriptor this item contain.
    /// Index = The index for this item in the list of converter items.
    /// </summary>
    public struct ConverterItemInfo
    {
        public ConverterItemDescriptor descriptor { get; internal set; }
        public int index { get; internal set; }
    }
}
