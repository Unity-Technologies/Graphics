using System;
using System.Reflection;
using System.Text.RegularExpressions;
using Drawing.Inspector;
using UnityEditor.Graphing.Util;
using UnityEditor.Rendering;
using UnityEditor.ShaderGraph;
using UnityEditor.ShaderGraph.Drawing;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.Graphing
{
    interface ICanChangeShaderGUI
    {
        string ShaderGUIOverride
        {
            get;
            set;
        }

        bool OverrideEnabled
        {
            get;
            set;
        }
    }
}
