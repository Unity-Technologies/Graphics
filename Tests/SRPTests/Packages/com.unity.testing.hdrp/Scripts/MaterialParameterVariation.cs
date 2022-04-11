using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct MaterialParameterVariation
{
    public string parameter;

    public enum ParamType { Float, Bool, Int, Vector, Texture, Color }
    public ParamType paramType;

    public bool multi;

    public bool b_Value;
    public int i_Value;
    public float f_Value;
    public Vector4 v_Value;
    [ColorUsage(true, true)] public Color c_Value;

    public int i_Value_Max;
    public float f_Value_Max;
    public Vector4 v_Value_Max;
    [ColorUsage(true, true)] public Color c_Value_Max;

    public Texture t_Value;

    public int count;
}
