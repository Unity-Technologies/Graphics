using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEditor.GraphToolsFoundation.Overdrive.BasicModel;
using UnityEngine;
using UnityEngine.GraphToolsFoundation.Overdrive;

[Serializable]
public class VFXGraphModel : GraphModel
{
    // Data types belonging to a list are compatible
    // We can have the same data type in multiple lists
    private static List<List<TypeHandle>> s_HandlesCompatibilityMap = new List<List<TypeHandle>>()
    {
        new()
        {
            TypeHandle.Bool,
            TypeHandle.Char,
            TypeHandle.Double,
            TypeHandle.Float,
            TypeHandle.Long,
            TypeHandle.Vector2,
            TypeHandle.Vector3,
            TypeHandle.Vector4,
            TypeHandle.UInt
        }
    };

    // This will be displayed in the basic section of the graph inspector, because of the ModelSetting attribute.
    [SerializeField]
    [ModelSetting]
    // ReSharper disable once NotAccessedField.Local
    int m_Rating;

    // This will be displayed in the advanced section of the graph inspector, because of the lack of ModelSetting attribute.
    [SerializeField]
    // ReSharper disable once NotAccessedField.Local
    string m_Author;

    // This will not be displayed in the graph inspector, because of the lack of the HideInInspector attribute.
    [SerializeField]
    [HideInInspector]
    // ReSharper disable once NotAccessedField.Local
    string m_SecretSauce;

    protected override bool IsCompatiblePort(IPortModel startPortModel, IPortModel compatiblePortModel)
    {
        return
            startPortModel.DataTypeHandle.Equals(compatiblePortModel.DataTypeHandle)
            || s_HandlesCompatibilityMap.Any(x => x.Contains(startPortModel.DataTypeHandle) && x.Contains(compatiblePortModel.DataTypeHandle));
    }
}
