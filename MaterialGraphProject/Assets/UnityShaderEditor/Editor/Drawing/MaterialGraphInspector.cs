using System;
using System.Linq;
using UnityEditor.Graphing.Drawing;
using UnityEditor.MaterialGraph.Drawing.NodeInspectors;
using UnityEngine.MaterialGraph;

namespace UnityEditor.MaterialGraph.Drawing
{
    [CustomEditor(typeof(MaterialGraphAsset))]
    public class MaterialGraphInspector : AbstractGraphInspector
    {
        protected override void AddTypeMappings(Action<Type, Type> map)
        {
            map(typeof(AbstractSurfaceMasterNode), typeof(SurfaceMasterNodeInspector));
        }
    }
}