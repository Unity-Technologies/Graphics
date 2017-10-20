using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjectToMaterialArray : MonoBehaviour
{
    [SerializeField] private Renderer referenceObject;

    enum ArrayType { _1D, _2D };
    [SerializeField] ArrayType arrayType = ArrayType._1D;

    [Header("List of parameters:")]
    [Header("- name:type:value")]
    [Header("- supported types : float (f), vector (v)")]
    [SerializeField] string[] globalParams;
    [Header("- use \";\" to use multiple parameters")]
    [Header("- value can be \"min_max_count\"")]
    [Header("- leave empty for a space")]
    [SerializeField] string[] matParams;

    [SerializeField] float spacing = 1.5f;

    [SerializeField] bool generateObjects = false;

    private void OnDrawGizmosSelected()
    {
        if (generateObjects)
        {
            generateObjects = false;

            // Clear the hierarchy
            foreach ( Transform child in transform)
            {
                DestroyImmediate(transform);
            }

            if (arrayType == ArrayType._1D)
            {
                for (int i=0; i<matParams.Length; i++)
                {
                    string[] paramsArr = matParams[i].Split(";"[0]);
                    for (int j = 0; j < paramsArr.Length; j++)
                    {
                        string[] paramStrings = matParams[j].Split(":"[0]);

                        System.Type type;
                        if (paramStrings[1] == "f")
                            type = typeof(float);
                        else if (paramStrings[1] == "v")
                            type = typeof(Vector4);
                        else return;

                        if (paramStrings[2].Contains("_")) // min, max, count
                        {
                            string[] valueParams = paramStrings[2].Split("_"[0]);
                            if (valueParams.Length != 3) return;
                            //var min;
                        }
                    }
                }
            }
        }
    }

    /*
    System.Array ParseParams( string _in )
    {
        string[] paramsArr = _in.Split(":"[0]);

        System.Array o;

        if (paramsArr.Length > 3)
        {
            o = new System.Array[2 + int.Parse(paramsArr[paramsArr.Length - 1])];
        }
        else
            o = new System.Array[3];

        o[0] = paramsArr[0];

    }
    */

    Vector4 ParseVector(string _inV)
    {
        Vector4 o = new Vector4();
        string[] strings = _inV.Split(","[0]);
        o.x = float.Parse(strings[0]);
        o.y = float.Parse(strings[1]);
        o.z = float.Parse(strings[2]);
        o.w = float.Parse(strings[3]);

        return o;
    }

    [System.Serializable]
    public class MatParam
    {
        [SerializeField] string name = "";

        enum ParamType { Float, Vector, Bool };
        [SerializeField] ParamType paramType = ParamType.Float;

        [Tooltip("value | list of values with \"_\" | min/max/count with \";\"")]
        [SerializeField] string paramValues = "";

        public Renderer[] MakeObjectArray ( Renderer _rndr )
        {
            Renderer[] outArray;

            string[] strArray;

            if (paramValues.Contains("_")) // contain a list of values
            {
                strArray = paramValues.Split("_"[0]);
            }
            else if (paramValues.Contains(";")) // contain min / max / count
            {
                string[] arrayBuilder = paramValues.Split(";"[0]);

                strArray = new string[int.Parse(arrayBuilder[2])];
            }
            else // single value
            {
                strArray = new string[1] { paramValues };
            }

            outArray = new Renderer[strArray.Length];

            for (int i=0; i<strArray.Length; i++)
            {
                outArray[i] = GameObject.Instantiate( _rndr.gameObject ).GetComponent<Renderer>();
                switch ( paramType )
                {
                    case ParamType.Float:
                        float f = float.Parse(strArray[i]);
                        outArray[i].material.SetFloat(name, f);
                        break;
                    case ParamType.Vector:
                        string[] vecArray = strArray[i].Split(","[0]);
                        Vector4 v = Vector4.zero;
                        v.x = float.Parse(vecArray[0]);
                        v.y = float.Parse(vecArray[1]);
                        v.z = float.Parse(vecArray[2]);
                        v.w = float.Parse(vecArray[3]);
                        outArray[i].material.SetVector(name, v);
                        break;
                    case ParamType.Bool:
                        bool b = bool.Parse(strArray[i]);
                        break;
                }
            }

            return outArray;
        }
    }
}
