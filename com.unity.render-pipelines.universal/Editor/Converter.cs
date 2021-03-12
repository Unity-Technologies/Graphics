using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

// Might need to change this name before making it public
public abstract class CoreConverter
{
    /// <summary>
    /// A structure holding the information for each Item that needs to be Converted.
    /// Name = The Name of the asset that is being converted.
    /// Path = The Path to the asset being converted.
    /// InitialInfo = If there are some issues with the converter that we already know about.
    ///     Example: If we know it is a custom shader, we can not convert it so we add the information here.
    /// HelpLink = Link to the documentation of how to convert this asset. Useful if the conversion failed or if we know we can not convert this asset automatically.
    /// </summary>
    public struct ConverterItemInfo
    {
        public bool Active;
        public string Name;
        public string Path;
        public string InitialInfo;
        public string HelpLink;
    }

    public abstract List<ConverterItemInfo> ItemInfos { get; set; }

    // Name Action
    // String2 Action (path? not always)

    public abstract string Name { get; }
    public abstract string Info { get; }

    /// <summary>
    /// The method that will be run when converting the assets.
    /// The list that is provided is a list that comes from the UI in where each entry that is ticked will be used.
    /// </summary>
    public abstract void Convert(List<bool> Active = null);

    /// <summary>
    /// The method will run when the Converter UI starts to populate some info regarding assets that needs converted.
    /// </summary>
    public abstract void Initialize();

    //
    public virtual void PrintMe(int index) {}
}
