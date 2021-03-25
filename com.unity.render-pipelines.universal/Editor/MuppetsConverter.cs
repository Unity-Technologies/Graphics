using System;
using System.Collections.Generic;
using UnityEditor.Rendering;
using UnityEditor.Rendering.Universal;
using UnityEngine;

public sealed class MuppetsConverter : RenderPipelineConverter
{
    public override string name => "Muppets";
    public override string info => "Need to update all my Muppets";
    public override string category { get; }
    public override Type conversion => typeof(BuiltInToURPConversion);

    //List<string> m_AssetsToConvert = new List<string>();

    public override void OnInitialize(InitializeConverterContext ctx)
    {
        for (int i = 0; i < 10; i++)
        {
            ConverterItemDescriptor info = new ConverterItemDescriptor()
            {
                name = "Muppet : " + i,
                path = "Somewhere/On/My/Disk..." + i,
                initialInfo = "",
                helpLink = "?? yupp",
            };
            ctx.AddAssetToConvert(info);
            //m_AssetsToConvert.Add(info.path);
        }
    }

    public override void OnRun(RunConverterContext ctx)
    {
        foreach (var item in ctx.items)
        {
            ctx.Processing(item.index);

            //var path = m_AssetsToConvert[item.id];
            // if failed.
            if (item.index == 2)
                ctx.MarkFailed(item.index);
            else if (item.descriptor.name == "Muppet : 5")
                ctx.MarkFailed(item.index, "Super Serious Awesomeness");
            else
            {
                ctx.MarkSuccessful(item.index);
            }
        }
    }

    public override void OnClicked(int index)
    {
        Debug.Log($"{index} has been clicked. Do something awesome with that.");
    }
}
