using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjectToMaterialArray : MonoBehaviour
{
    [SerializeField] private Renderer referenceObject;

    enum ArrayType { _1D, _2D };
    [SerializeField] ArrayType arrayType = ArrayType._1D;

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
        }
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
