using System;
using System.Collections.Generic;
using UnityEngine;

public class MuppetsConverter : CoreConverter
{
    public override List<ConverterItemInfo> ItemInfos { get; set; }
    public override string Name { get; }
    public override string Info { get; }

    List<string> converterAssetsPath;

    public MuppetsConverter()
    {
        Name = "Muppets";
        Info = "Need to update all my Muppets";

        //URPConvertersEditor.OnClicked += PrintMe;
        converterAssetsPath = new List<string>();
        ItemInfos = new List<ConverterItemInfo>();
    }

    public override void Convert(List<bool> activeList)
    {
        // if (activeList.Count > 0)
        // {
        // Convert using the list entries
        Debug.Log("Converting using a list of items");
        Debug.Log(activeList.Count);
        for (int i = 0; i < activeList.Count; i++)
        {
            if (activeList[i])
            {
                Debug.Log("We should convert this one :: " + converterAssetsPath[i]);
            }
        }
        // }
    }

    public override void Initialize()
    {
        for (int i = 0; i < 20; i++)
        {
            ConverterItemInfo info = new ConverterItemInfo()
            {
                Active = true,
                Name = "Muppet : " + i,
                Path = "Somewhere/On/My/Disk..." + i,
                InitialInfo = "",
                HelpLink = "?? yupp"
            };
            converterAssetsPath.Add(info.Path);
            ItemInfos.Add(info);
        }

        //return ItemInfos;
    }

    public override void PrintMe(int index)
    {
        Debug.Log("Clicked Index " + index);
    }
}
