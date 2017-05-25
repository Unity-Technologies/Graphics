using UnityEngine.Graphing;

namespace UnityEngine.MaterialGraph
{
    [Title("UV/ScaleOffset")]
    public class ScaleOffsetNode : AnyNode<ScaleOffsetNode.Definition>
    {
        public class Definition : IAnyNodeDefinition
        {
            public string name { get { return "ScaleOffset"; } }

            public AnyNodeProperty[] properties
            {
                get
                {
                    return new AnyNodeProperty[]
                    {
                           // slotId is the 'immutable' value we used to connect things
                            new AnyNodeProperty { slotId= 0,    name = "inUVs",       description = "Input UV coords",   propertyType = PropertyType.Vector2, value = Vector4.zero,                       state = AnyNodePropertyState.Slot },
                            new AnyNodeProperty { slotId= 1,    name = "scale",       description = "UV scale",          propertyType = PropertyType.Vector2, value= Vector4.one,                         state = AnyNodePropertyState.Constant },
                            new AnyNodeProperty { slotId= 2,    name = "scaleCenter", description = "UV scale center",   propertyType = PropertyType.Vector2, value= new Vector4(0.5f, 0.5f, 0.5f, 0.5f), state = AnyNodePropertyState.Constant },
                            new AnyNodeProperty { slotId= 3,    name = "offset",      description = "UV offset",         propertyType = PropertyType.Vector2, value= Vector4.zero,                        state = AnyNodePropertyState.Constant },
                    };
                }
            }

            public AnyNodeSlot[] outputs
            {
                get
                {
                    return new AnyNodeSlot[]
                    {
                            new AnyNodeSlot { slotId= 4,    name = "outUVs", description = "Output UV texture coordinates", slotValueType = SlotValueType.Vector2, value = Vector4.zero  }
                    };
                }
            }

            public ShaderGlobal[] globals { get { return new ShaderGlobal[] { }; } }

            public string hlsl
            {
                get
                {
                    return
                        "float4 xform= float4(scale, offset + scaleCenter - scaleCenter * scale);\n" +
                        "outUVs = inUVs * xform.xy + xform.zw;";
                }
            }
        }
    }
}
