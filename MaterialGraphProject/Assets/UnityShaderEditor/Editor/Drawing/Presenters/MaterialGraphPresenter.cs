using System;
using UnityEditor.Graphing.Drawing;
using UnityEngine.MaterialGraph;
using RMGUI.GraphView;

namespace UnityEditor.MaterialGraph.Drawing
{
    public class MaterialGraphPresenter : AbstractGraphPresenter
    {
        protected MaterialGraphPresenter()
        {}

        protected override void AddTypeMappings(Action<Type, Type> map)
        {
            map(typeof(AbstractMaterialNode), typeof(MaterialNodePresenter));
            map(typeof(ColorNode), typeof(ColorNodePresenter));
            map(typeof(TextureNode), typeof(TextureNodePresenter));
            map(typeof(UVNode), typeof(UVNodePresenter));
            map(typeof(Vector1Node), typeof(Vector1NodePresenter));
            map(typeof(Vector2Node), typeof(Vector2NodePresenter));
            map(typeof(Vector3Node), typeof(Vector3NodePresenter));
            map(typeof(Vector4Node), typeof(Vector4NodePresenter));
            map(typeof(SubGraphNode), typeof(SubgraphNodePresenter));
            map(typeof(RemapMasterNode), typeof(RemapMasterNodePresenter));
			map(typeof(MasterRemapInputNode), typeof(RemapInputNodePresenter));
			map(typeof(AbstractSubGraphIONode), typeof(SubgraphIONodePresenter));
            map(typeof(AbstractSurfaceMasterNode), typeof(SurfaceMasterPresenter));
            map(typeof(GraphEdgePresenter), typeof(Edge));
        }
    }
}
