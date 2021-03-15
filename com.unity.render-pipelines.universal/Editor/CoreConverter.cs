using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

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
    public string name;
    public string path;
    public string initialInfo;
    public string helpLink;
    public int id;
}

public struct InitializeConverterContext
{
    internal List<ConverterItemInfo> m_Items;
    public void AddAssetToConvert(ConverterItemInfo item) { m_Items.Add(item); }
}

public struct RunConverterContext
{
    internal List<ConverterItemInfo> m_Items;
    public IEnumerable<ConverterItemInfo> items => m_Items;
}

// Might need to change this name before making it public
public abstract class CoreConverter
{
    public abstract string name { get; }
    public abstract string info { get; }

    public abstract void OnInitialize(InitializeConverterContext ctx);

    /// <summary>
    /// The method that will be run when converting the assets.
    /// Takes a RunConverterContext.
    /// </summary>
    public abstract void OnRun(RunConverterContext ctx);
}
