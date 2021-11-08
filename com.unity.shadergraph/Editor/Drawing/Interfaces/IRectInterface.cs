using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Drawing.Colors;
using UnityEditor.ShaderGraph.Internal;
using UnityEditor.ShaderGraph.Drawing;
using UnityEditor.ShaderGraph.Serialization;
using UnityEngine.Assertions;
using UnityEngine.Pool;

namespace UnityEditor.ShaderGraph
{
    interface IRectInterface
    {
        Rect rect
        {
            get;
            internal set;
        }
    }
}
