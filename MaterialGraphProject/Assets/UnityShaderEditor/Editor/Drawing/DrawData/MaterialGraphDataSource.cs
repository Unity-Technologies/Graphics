using UnityEditor.Graphing.Drawing;
using UnityEngine.MaterialGraph;

namespace UnityEditor.MaterialGraph.Drawing
{
    public class MaterialGraphDataSource : AbstractGraphDataSource
    {
        protected MaterialGraphDataSource()
        {}

        protected override void AddTypeMappings()
        {
            AddTypeMapping(typeof(AbstractMaterialNode), typeof(MaterialNodeDrawData));
            AddTypeMapping(typeof(ColorNode), typeof(ColorNodeDrawData));
            AddTypeMapping(typeof(TextureNode), typeof(TextureNodeDrawData));
            AddTypeMapping(typeof(Vector1Node), typeof(Vector1NodeDrawData));
            AddTypeMapping(typeof(Vector2Node), typeof(Vector2NodeDrawData));
            AddTypeMapping(typeof(Vector3Node), typeof(Vector3NodeDrawData));
            AddTypeMapping(typeof(Vector4Node), typeof(Vector4NodeDrawData));
        }
    }
}
