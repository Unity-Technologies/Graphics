using System.Collections;
using System.Collections.Generic;
using System.IO;

using UnityEngine;

using DataPair = System.Tuple<string, long>;

public class ProfilerScript : MonoBehaviour
{
    public string reportPath = "perfReport.txt";

    float FindRightUnit(long value, out string unitName)
    {
        var typeNormalization = 1.0f;
        unitName = "B";

        if (value > 1e8f)
        {
            typeNormalization = 1e9f;
            unitName = " GB";
        }
        else if (value > 1e5f)
        {
            typeNormalization = 1e6f;
            unitName = " MB";
        }
        else if (value > 1e2f)
        {
            typeNormalization = 1e3f;
            unitName = " KB";
        }

        return typeNormalization;
    }

    void FillData(System.Type type, System.IO.StreamWriter writer)
    {
        List<DataPair> outList = new List<DataPair>();
        long totalMemory = 0;
        var data = Resources.FindObjectsOfTypeAll(type);

        foreach (var item in data)
        {
            long currSize = UnityEngine.Profiling.Profiler.GetRuntimeMemorySizeLong(item);
            outList.Add(new DataPair(item.name, currSize));
            totalMemory += currSize;
        }

        outList.Sort((a, b) => b.Item2.CompareTo(a.Item2));

        writer.WriteLine("*** " + type.Name + " ***");
        string totalUnitName;
        float totalUnitNormalization = FindRightUnit(totalMemory, out totalUnitName);
        writer.WriteLine("TOTAL:\t\t" + (totalMemory / totalUnitNormalization) + " " + totalUnitName);

        foreach (var item in outList)
        {
            string unitName;
            float unitNormalization = FindRightUnit(item.Item2, out unitName);
            var line = "\t" + item.Item1 + "\t\t" + item.Item2 / unitNormalization + " " + unitName;
            writer.WriteLine(line);
        }
        writer.WriteLine();
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            if (File.Exists(reportPath))
                File.Delete(reportPath);

            System.IO.StreamWriter w = File.AppendText(reportPath);

            // Note: we probably care only about the one marked with * in the comments
            FillData(typeof(RenderTexture), w); // * 
            FillData(typeof(Texture2D), w);     // * 
            FillData(typeof(Texture3D), w);     
            FillData(typeof(CubemapArray), w);  // *
            FillData(typeof(Material), w);
            FillData(typeof(Mesh), w);
            FillData(typeof(Shader), w);        // *
            FillData(typeof(ComputeShader), w); // *

            w.Close();
        }
    }
}
