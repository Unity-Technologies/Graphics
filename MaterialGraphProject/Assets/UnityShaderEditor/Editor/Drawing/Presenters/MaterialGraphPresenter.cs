using UnityEditor.Graphing.Drawing;
using UnityEngine.MaterialGraph;

namespace UnityEditor.MaterialGraph.Drawing
{
    public class MaterialGraphPresenter : AbstractGraphPresenter
    {
        protected MaterialGraphPresenter()
        {
            dataMapper[typeof(AbstractMaterialNode)] = typeof(MaterialNodePresenter);
            dataMapper[typeof(ColorNode)] = typeof(ColorNodePresenter);
            dataMapper[typeof(TextureNode)] = typeof(TextureNodePresenter);
            dataMapper[typeof(UVNode)] = typeof(UVNodePresenter);
            dataMapper[typeof(Vector1Node)] = typeof(Vector1NodePresenter);
            dataMapper[typeof(Vector2Node)] = typeof(Vector2NodePresenter);
            dataMapper[typeof(Vector3Node)] = typeof(Vector3NodePresenter);
            dataMapper[typeof(Vector4Node)] = typeof(Vector4NodePresenter);
            dataMapper[typeof(SubGraphNode)] = typeof(SubgraphNodePresenter);
            dataMapper[typeof(RemapMasterNode)] = typeof(RemapMasterNodePresenter);
            dataMapper[typeof(MasterRemapInputNode)] = typeof(RemapInputNodePresenter);
            dataMapper[typeof(AbstractSubGraphIONode)] = typeof(SubgraphIONodePresenter);
            dataMapper[typeof(AbstractSurfaceMasterNode)] = typeof(SurfaceMasterPresenter);
        }
    }
}
