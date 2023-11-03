using System;
using System.Collections.Generic;
using UnityEngine;
using Node = UnityEditor.Experimental.GraphView.Node;

namespace UnityEditor.ShaderGraph.Drawing
{
    internal interface IShaderGraphToolbarExtension
    {
        abstract void OnGUI(MaterialGraphView graphView);
    }
}
