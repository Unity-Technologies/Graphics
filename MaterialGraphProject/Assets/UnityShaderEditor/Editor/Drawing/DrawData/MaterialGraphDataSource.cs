using System;
using UnityEditor.Graphing.Drawing;
using UnityEngine.MaterialGraph;
using RMGUI.GraphView;

namespace UnityEditor.MaterialGraph.Drawing
{
    public class MaterialGraphDataSource : AbstractGraphDataSource
    {
        protected MaterialGraphDataSource()
        {}

        protected override void AddTypeMappings(Action<Type, Type> map)
        {
            map(typeof(AbstractMaterialNode), typeof(MaterialNodeDrawData));
            map(typeof(ColorNode), typeof(ColorNodeDrawData));
            map(typeof(TextureNode), typeof(TextureNodeDrawData));
            map(typeof(Vector1Node), typeof(Vector1NodeDrawData));
            map(typeof(Vector2Node), typeof(Vector2NodeDrawData));
            map(typeof(Vector3Node), typeof(Vector3NodeDrawData));
            map(typeof(Vector4Node), typeof(Vector4NodeDrawData));
            map(typeof(SubGraphNode), typeof(SubgraphNodeDrawData));
            map(typeof(RemapMasterNode), typeof(RemapMasterNodeDrawData));
			map(typeof(MasterRemapInputNode), typeof(RemapInputNodeDrawData));
			map(typeof(AbstractSubGraphIONode), typeof(SubgraphIONodeDrawData));
            map(typeof(AbstractSurfaceMasterNode), typeof(SurfaceMasterDrawData));
            map(typeof(EdgeDrawData), typeof(Edge));
        }
    }
}
