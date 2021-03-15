using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class ChocolateConverter : CoreConverter
{
    public override string name => "Mars";
    public override string info => "Is better than Snickers";

    List<string> m_AssetsToConvert = new List<string>();

    public override void OnInitialize(InitializeConverterContext ctx)
    {
        for (int i = 0; i < 2; i++)
        {
            ConverterItemInfo info = new ConverterItemInfo()
            {
                name = "Chocolate : " + i,
                path = "Chocolate/On/My/Disk..." + i * 100,
                initialInfo = "MilkChocolate",
                helpLink = "?? here is a link",
                id = i
            };
            ctx.AddAssetToConvert(info);
            m_AssetsToConvert.Add(info.path);
        }
    }

    public override void OnRun(RunConverterContext ctx)
    {
        foreach (var item in ctx.items)
        {
            var path = m_AssetsToConvert[item.id];
        }
    }
}
