using UnityEngine.Graphing;


namespace UnityEngine.MaterialGraph
{
	[Title("UV/SphericalIndentation")]
	public class SphericalIndentationNode : AnyNode<SphericalIndentationNode.Definition>
	{
		public class Definition : IAnyNodeDefinition
		{
			public string name { get { return "SphericalIndentation"; } }

			public AnyNodeProperty[] properties
			{
				get
				{
					return new AnyNodeProperty[]
					{
                           // slotId is the 'immutable' value we used to connect things
                            new AnyNodeProperty { slotId= 0,    name = "inUVs",       description = "Input UV coords",          propertyType = PropertyType.Vector2,    value = Vector4.zero,                       state = AnyNodePropertyState.Slot },
							new AnyNodeProperty { slotId= 1,    name = "center",      description = "UV center point",          propertyType = PropertyType.Vector2,    value= new Vector4(0.5f, 0.5f, 0.5f, 0.5f), state = AnyNodePropertyState.Constant },
							new AnyNodeProperty { slotId= 2,    name = "height",      description = "Height off surface",       propertyType = PropertyType.Float,      value= Vector4.zero,                        state = AnyNodePropertyState.Constant },
							new AnyNodeProperty { slotId= 3,    name = "radius",      description = "Radius",                   propertyType = PropertyType.Float,      value= Vector4.one,                         state = AnyNodePropertyState.Constant },
					};
				}
			}

			public AnyNodeSlot[] outputs
			{
				get
				{
					return new AnyNodeSlot[]
					{
							new AnyNodeSlot { slotId= 4,    name = "outUVs",   description = "Output UV texture coordinates", slotValueType = SlotValueType.Vector2, value = Vector4.zero  },
							new AnyNodeSlot { slotId= 5,    name = "outNormal", description = "Output Normal in tangent space", slotValueType = SlotValueType.Vector3, value = Vector4.zero  }
					};
				}
			}

			public ShaderGlobal[] globals { get { return new ShaderGlobal[] { ShaderGlobal.TangentSpaceViewDirection }; } }

			public string hlsl
			{
				get
				{
					return
						"float radius2= radius*radius;\n" +
						"float3 cur= float3(inUVs.xy, 0.0f);\n" +
						"float3 sphereCenter = float3(center, height);\n" +
						"float3 edgeA = sphereCenter - cur;\n" +
						"float a2 = dot(edgeA, edgeA);\n" +
						"outUVs= inUVs;\n" +
                        "outNormal= float3(0.0f, 0.0f, 1.0f);\n" +
						"if (a2 < radius2)\n" +
						"{\n" +
						"   float a = sqrt(a2);\n" +
						"   edgeA = edgeA / a;\n" +
						"   float cosineR = dot(edgeA, tangentSpaceViewDirection.xyz);\n" +
						"   float x = cosineR * a - sqrt(-a2 + radius2 + a2 * cosineR * cosineR);\n" +
						"   float3 intersectedEdge = cur + tangentSpaceViewDirection * x;\n" +
                        "   outNormal= normalize(sphereCenter - intersectedEdge);\n" +
						"   outUVs = intersectedEdge.xy;\n" +
						"}\n";

				}
			}
		}
	}
}
