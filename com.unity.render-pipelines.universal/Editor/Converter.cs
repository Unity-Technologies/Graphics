using System;
using System.Collections.Generic;
using UnityEngine;

// Might need to change this name before making it public
public abstract class CoreConverter
{
    /// <summary>
    /// A structure holding the information for each Item that needs to be Converted.
    /// </summary>
    public struct ConverterItemInfo
    {
        public string Name;
        public string Path;
        public string InitialInfo;
        public string HelpLink;
    }

    public abstract string Name { get; }
    public abstract string Info { get; }

    /// <summary>
    /// The method that will be run when converting the assets.
    /// </summary>
    public abstract void Convert();

    /// <summary>
    /// The method will run when the Converter UI starts to populate some info regarding assets that needs converted.
    /// </summary>
    public abstract List<ConverterItemInfo> Initialize();
}
