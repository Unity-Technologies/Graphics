using System;
using UnityEditor.Rendering;
using UnityEditor.Rendering.Universal;
using UnityEngine;

public sealed class ChocolateConverter : RenderPipelineConverter
{
    public override string name => "Mars";
    public override string info => "Is better than Snickers";
    public override string category { get; }
    public override Type conversion => typeof(BuiltInToURPConversion);

    //List<string> m_AssetsToConvert = new List<string>();

    public override void OnInitialize(InitializeConverterContext ctx)
    {
        //ctx.RunAfter<MuppetsConverter>();
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
            // Each converter needs to add this info using this API.
            ctx.AddAssetToConvert(info);
            //m_AssetsToConvert.Add(info.path);
        }
    }

    public override void OnRun(RunConverterContext ctx)
    {
        foreach (var item in ctx.items)
        {
            var path = item.path;
        }
    }
}
