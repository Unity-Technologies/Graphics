using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Globalization;
using UnityEngine;

public partial class PCache
{
    public void SetVector2Data(string component, List<Vector2> data)
    {
        var dataX = new List<float>();
        var dataY = new List<float>();

        foreach (var v in data)
        {
            dataX.Add(v.x);
            dataY.Add(v.y);
        }

        SetFloatData(component + ".x", dataX);
        SetFloatData(component + ".y", dataY);
    }

    public void SetVector3Data(string component, List<Vector3> data)
    {
        var dataX = new List<float>();
        var dataY = new List<float>();
        var dataZ = new List<float>();

        foreach (var v in data)
        {
            dataX.Add(v.x);
            dataY.Add(v.y);
            dataZ.Add(v.z);
        }

        SetFloatData(component + ".x", dataX);
        SetFloatData(component + ".y", dataY);
        SetFloatData(component + ".z", dataZ);
    }

    public void SetColorData(string component, List<Vector4> data)
    {
        var dataX = new List<float>();
        var dataY = new List<float>();
        var dataZ = new List<float>();
        var dataW = new List<float>();

        foreach (var v in data)
        {
            dataX.Add(v.x);
            dataY.Add(v.y);
            dataZ.Add(v.z);
            dataW.Add(v.w);
        }

        SetFloatData(component + ".r", dataX);
        SetFloatData(component + ".g", dataY);
        SetFloatData(component + ".b", dataZ);
        SetFloatData(component + ".a", dataW);
    }

    public void SetVector4Data(string component, List<Vector4> data)
    {
        var dataX = new List<float>();
        var dataY = new List<float>();
        var dataZ = new List<float>();
        var dataW = new List<float>();

        foreach (var v in data)
        {
            dataX.Add(v.x);
            dataY.Add(v.y);
            dataZ.Add(v.z);
            dataW.Add(v.w);
        }

        SetFloatData(component + ".x", dataX);
        SetFloatData(component + ".y", dataY);
        SetFloatData(component + ".z", dataZ);
        SetFloatData(component + ".w", dataW);
    }

}
