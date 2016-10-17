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
        }
    }
}
