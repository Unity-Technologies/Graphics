using UnityEngine.Graphing;

namespace UnityEngine.MaterialGraph
{
    [Title("Procedural/AACheckerboard")]
    public class AACheckerboardNode : AnyNode<AACheckerboardNode.Definition>
    {
        public class Definition : IAnyNodeDefinition
        {
            public string name { get { return "AACheckerboard"; } }

            public AnyNodeProperty[] properties
            {
                get
                {
                    return new AnyNodeProperty[]
                    {
                           // slotId is the 'immutable' value we used to connect things
                            new AnyNodeProperty { slotId= 0,    name = "inUVs",       description = "Input UV coords",          propertyType = PropertyType.Vector2,    value = Vector4.zero,                       state = AnyNodePropertyState.Slot },
                            new AnyNodeProperty { slotId= 1,    name = "A",           description = "color A",                  propertyType = PropertyType.Vector4,    value= new Vector4(0.2f, 0.2f, 0.2f, 0.2f), state = AnyNodePropertyState.Constant },
                            new AnyNodeProperty { slotId= 2,    name = "B",           description = "color B",                  propertyType = PropertyType.Vector4,    value= new Vector4(0.7f, 0.7f, 0.7f, 0.7f), state = AnyNodePropertyState.Constant },
                            new AnyNodeProperty { slotId= 3,    name = "aaTweak",     description = "AA Tweak",                 propertyType = PropertyType.Vector3,    value= new Vector4(0.05f, 3.0f, 0.0f, 0.0f),state = AnyNodePropertyState.Constant },
                            new AnyNodeProperty { slotId= 4,    name = "frequency",   description = "Frequency",                propertyType = PropertyType.Vector2,    value = Vector4.one,                        state = AnyNodePropertyState.Constant },
                    };
                }
            }

            public AnyNodeSlot[] outputs
            {
                get
                {
                    return new AnyNodeSlot[]
                    {
                            new AnyNodeSlot { slotId= 5,    name = "outColor", description = "Output color", slotValueType = SlotValueType.Vector4, value = Vector4.zero  }
                    };
                }
            }

            public string hlsl
            {
                get
                {
                    return
                        "float4 derivatives = float4(ddx(inUVs), ddy(inUVs));\n" +
                        "float2 duv_length = sqrt(float2(dot(derivatives.xz, derivatives.xz), dot(derivatives.yw, derivatives.yw)));\n" +
                        "float width = 0.5f;\n" +
                        "float2 distance3 = 2.0f * abs(frac(inUVs.xy * frequency) - 0.5f) - width;\n" +
                        "float2 scale = aaTweak.x / duv_length.xy;\n" +
                        "float2 blend_out = saturate((scale - aaTweak.zz) / (aaTweak.yy - aaTweak.zz));\n" +
                        "float2 vector_alpha = clamp(distance3 * scale.xy * blend_out.xy, -1.0f, 1.0f);\n" +
                        "float alpha = saturate(0.5f + 0.5f * vector_alpha.x * vector_alpha.y);\n" +
                        "outColor= lerp(A, B, alpha.xxxx);";
                }
            }
        }
    }
}
