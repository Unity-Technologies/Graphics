using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.Graphing.Util
{
    public class SG_ShaderGUIInfo : ScriptableObject
    {
        public string[] tooltips;
        public string[] headers;

        public void PrintMe()
        {
            foreach (string s in tooltips)
            {
                if (s != null)
                    Debug.Log("tooltip: " + s);
                else
                    Debug.Log("null tooltip!");
            }
            foreach (string s in headers)
            {
                if (s != null)
                    Debug.Log("header: " + s);
                else
                    Debug.Log("null header!");
            }
        }
    }
}
