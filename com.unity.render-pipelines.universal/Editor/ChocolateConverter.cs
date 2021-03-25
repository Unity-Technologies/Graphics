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

    // public override bool enabled()
    // {
    //     return false;
    // }

    //List<string> m_AssetsToConvert = new List<string>();

    public override void OnInitialize(InitializeConverterContext ctx)
    {
        //ctx.RunAfter<MuppetsConverter>();
        for (int i = 0; i < 2; i++)
        {
            ConverterItemDescriptor info = new ConverterItemDescriptor()
            {
                name = "Chocolate : " + i,
                path = "Chocolate/On/My/Disk..." + i * 100,
                initialInfo = "MilkChocolate",
                helpLink = "?? here is a link",
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
            ctx.Processing(item.index);
            var path = item.descriptor.path;

            // if failed.
            //ctx.MarkFailed(item.index);
        }
    }
}
