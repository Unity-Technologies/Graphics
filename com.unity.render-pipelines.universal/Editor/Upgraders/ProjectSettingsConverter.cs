using System;
using UnityEditor.Rendering;
using UnityEditor.Rendering.Universal;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class ProjectSettingsConverter : RenderPipelineConverter
{
    public override string name => "Quality and Graphics Settings";

    public override string info =>
        "This converter will look at creating Universal Render Pipeline assets and respective renderers and set their " +
        "settings based on equivalent settings from builtin renderer.";
    public override Type conversion => typeof(BuiltInToURPConversion);
    public override void OnInitialize(InitializeConverterContext context)
    {
        var id = 0;
        foreach (var levelName in QualitySettings.names)
        {
            var setting = QualitySettings.GetRenderPipelineAssetAt(id);
            var item = new ConverterItemInfo();
            item.id = id;
            item.name = levelName;

            var text = "";
            if (setting != null)
            {
                if (setting.GetType().ToString().Contains("Universal.UniversalRenderPipelineAsset"))
                {
                    text = "Contains URP Asset, will override existing asset.";
                }
                else
                {
                    text = "Contains SRP Asset, will override existing asset with URP asset.";
                }
            }
            else
            {
                text = "Will Generate Pipeline Asset";
            }


            item.path = text;

            context.AddAssetToConvert(item);
            id++;
        }
    }

    public override void OnRun(RunConverterContext context)
    {
        foreach (var item in context.items)
        {
            // which one am I?
            Debug.Log(item);
            UniversalRenderPipelineAsset.
        }
    }
}
