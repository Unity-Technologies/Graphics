using System.Collections.Generic;

namespace UnityEditor.ShaderFoundry.UnitTests
{
    internal class ColorPropertyBlockBuilder : Vector4PropertyBlockBuilder
    {
        public ColorPropertyBlockBuilder()
        {
            BlockName = "ColorProperty";
            FieldName = "FieldColor";
            PropertyAttribute = new PropertyAttributeData() { DefaultValue = "(1, 1, 1, 1)" };
        }

        public Block Build(ShaderContainer container)
        {
            var colorAttribute = new ShaderAttribute.Builder(container, "Color").Build();
            return BuildWithAttributeOverrides(container, container._float4, new List<ShaderAttribute> { colorAttribute });
        }
    }
}
