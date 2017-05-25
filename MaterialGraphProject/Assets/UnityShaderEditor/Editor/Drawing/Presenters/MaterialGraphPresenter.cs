using UnityEditor.Graphing.Drawing;
using UnityEngine.MaterialGraph;

namespace UnityEditor.MaterialGraph.Drawing
{
    public class MaterialGraphPresenter : AbstractGraphPresenter
    {
        protected MaterialGraphPresenter()
        {
            typeMapper[typeof(AbstractMaterialNode)] = typeof(MaterialNodePresenter);
            typeMapper[typeof(ColorNode)] = typeof(ColorNodePresenter);
            typeMapper[typeof(GradientNode)] = typeof(GradientNodePresenter);
            typeMapper[typeof(TextureNode)] = typeof(TextureNodePresenter);
            typeMapper[typeof(TextureAssetNode)] = typeof(TextureAssetNodePresenter);
            typeMapper[typeof(TextureLODNode)] = typeof(TextureLODNodePresenter);
            typeMapper[typeof(CubemapNode)] = typeof(CubeNodePresenter);
			typeMapper[typeof(ToggleNode)] = typeof(ToggleNodePresenter);
            typeMapper[typeof(UVNode)] = typeof(UVNodePresenter);
            typeMapper[typeof(Vector1Node)] = typeof(Vector1NodePresenter);
            typeMapper[typeof(Vector2Node)] = typeof(Vector2NodePresenter);
            typeMapper[typeof(Vector3Node)] = typeof(Vector3NodePresenter);
            typeMapper[typeof(Vector4Node)] = typeof(Vector4NodePresenter);
            typeMapper[typeof(ScaleOffsetNode)] = typeof(AnyNodePresenter);         // anything derived from AnyNode should use the AnyNodePresenter
            typeMapper[typeof(RadialShearNode)] = typeof(AnyNodePresenter);         // anything derived from AnyNode should use the AnyNodePresenter
            typeMapper[typeof(SphereWarpNode)] = typeof(AnyNodePresenter);          // anything derived from AnyNode should use the AnyNodePresenter
            typeMapper[typeof(AACheckerboardNode)] = typeof(AnyNodePresenter);         // anything derived from AnyNode should use the AnyNodePresenter
            typeMapper[typeof(SubGraphNode)] = typeof(SubgraphNodePresenter);
            typeMapper[typeof(RemapMasterNode)] = typeof(RemapMasterNodePresenter);
            typeMapper[typeof(MasterRemapInputNode)] = typeof(RemapInputNodePresenter);
            typeMapper[typeof(AbstractSubGraphIONode)] = typeof(SubgraphIONodePresenter);
            typeMapper[typeof(AbstractSurfaceMasterNode)] = typeof(SurfaceMasterPresenter);
            typeMapper[typeof(LevelsNode)] = typeof(LevelsNodePresenter);
            typeMapper[typeof(ConstantsNode)] = typeof(ConstantsNodePresenter);
            typeMapper[typeof(SwizzleNode)] = typeof(SwizzleNodePresenter);
			typeMapper[typeof(BlendModeNode)] = typeof(BlendModeNodePresenter);
            typeMapper[typeof(AddManyNode)] = typeof(AddManyNodePresenter);
            typeMapper[typeof(IfNode)] = typeof(IfNodePresenter);
            typeMapper[typeof(CustomCodeNode)] = typeof(CustomCodePresenter);
            typeMapper[typeof(Matrix2Node)] = typeof(Matrix2NodePresenter);
            typeMapper[typeof(Matrix3Node)] = typeof(Matrix3NodePresenter);
            typeMapper[typeof(Matrix4Node)] = typeof(Matrix4NodePresenter);
			typeMapper[typeof(TransformNode)] = typeof(TransformNodePresenter);
        }
    }
}
