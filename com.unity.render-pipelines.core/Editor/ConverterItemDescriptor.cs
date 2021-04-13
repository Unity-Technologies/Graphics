namespace UnityEditor.Rendering
{
    /// <summary>
    /// A structure holding the information for each Item that needs to be Converted.
    /// Name = The Name of the asset that is being converted.
    /// Info = Information that can be used to store some data. This will also be shown in the UI.
    /// WarningMessage = If there are some issues with the converter that we already know about.
    ///     Example: If we know it is a custom shader, we can not convert it so we add the information here.
    /// HelpLink = Link to the documentation of how to convert this asset. Useful if the conversion failed or if we know we can not convert this asset automatically.
    /// </summary>
    public struct ConverterItemDescriptor
    {
        public string name;
        public string info;
        public string warningMessage;
        public string helpLink;
    }
}
