using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class ChocolateConverter : CoreConverter
{
    public override List<ConverterItemInfo> ItemInfos { get; set; }
    public override string Name { get; }
    public override string Info { get; }

    List<string> AssetsToConvert = new List<string>();

    public ChocolateConverter()
    {
        Name = "Mars Bar";
        Info = "Is better than Snickers";
        ItemInfos = new List<ConverterItemInfo>();
    }

    public override void Convert(List<bool> Active = null)
    {
        Debug.Log("Just a basic Convert Step no List");
    }

    public override void Initialize()
    {
        for (int i = 0; i < 2; i++)
        {
            ConverterItemInfo info = new ConverterItemInfo()
            {
                Active = true,
                Name = "Chocolate : " + i,
                Path = "Chocolate/On/My/Disk..." + i * 100,
                InitialInfo = "MilkChocolate",
                HelpLink = "?? here is a link"
            };
            AssetsToConvert.Add(info.Path);
            ItemInfos.Add(info);
        }
    }
}
