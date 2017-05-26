using UnityEngine.Graphing;

namespace UnityEngine.MaterialGraph
{
    [Title("Procedural/AACheckerboard3d")]
    public class AACheckerboard3dNode : AnyNode<AACheckerboard3dNode.Definition>
    {
        public class Definition : IAnyNodeDefinition
        {
            public string name { get { return "AACheckerboard3d"; } }

            public AnyNodeProperty[] properties
            {
                get
                {
                    return new AnyNodeProperty[]
                    {
                           // slotId is the 'immutable' value we used to connect things
                            new AnyNodeProperty { slotId= 0,    name = "inUVs",       description = "Input UVW coords",         propertyType = PropertyType.Vector3,    value = Vector4.zero,                       state = AnyNodePropertyState.Slot },
                            new AnyNodeProperty { slotId= 1,    name = "A",           description = "color A",                  propertyType = PropertyType.Vector4,    value= new Vector4(0.2f, 0.2f, 0.2f, 0.2f), state = AnyNodePropertyState.Constant },
                            new AnyNodeProperty { slotId= 2,    name = "B",           description = "color B",                  propertyType = PropertyType.Vector4,    value= new Vector4(0.7f, 0.7f, 0.7f, 0.7f), state = AnyNodePropertyState.Constant },
                            new AnyNodeProperty { slotId= 3,    name = "aaTweak",     description = "AA Tweak",                 propertyType = PropertyType.Vector3,    value= new Vector4(0.05f, 3.0f, 0.0f, 0.0f),state = AnyNodePropertyState.Constant },
                            new AnyNodeProperty { slotId= 4,    name = "frequency",   description = "Frequency",                propertyType = PropertyType.Vector3,    value = Vector4.one,                        state = AnyNodePropertyState.Constant },
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
            public ShaderGlobal[] globals { get { return new ShaderGlobal[] { }; } }

            public string hlsl
            {
                get
                {
                    return
                        "float3 dx = ddx(inUVs);\n" +
                        "float3 dy = ddy(inUVs);\n" +
                        "float du=  sqrt(dx.x * dx.x + dy.x * dy.x);\n" +
                        "float dv=  sqrt(dx.y * dx.y + dy.y * dy.y);\n" +
                        "float dw=  sqrt(dx.z * dx.z + dy.z * dy.z);\n" +
                        "float3 distance3 = 2.0f * abs(frac((inUVs.xyz + 0.5f) * frequency.xyz) - 0.5f) - 0.5f;\n" +
                        "float3 scale = aaTweak.xxx / float3(du, dv, dw);\n" +
                        "float3 blend_out = saturate((scale - aaTweak.zzz) / (aaTweak.yyy - aaTweak.zzz));\n" +
                        "float3 vectorAlpha = clamp(distance3 * scale.xyz * blend_out.xyz, -1.0f, 1.0f);\n" +
                        "float alpha = saturate(0.5f + 0.5f * vectorAlpha.x * vectorAlpha.y * vectorAlpha.z);\n" +
                        "outColor= lerp(A, B, alpha.xxxx);";
                }
            }
        }
    }
}

