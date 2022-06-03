using System;
using System.Collections.Generic;
using UnityEditor.ShaderGraph.GraphDelta;

namespace UnityEditor.ShaderGraph.Defs
{
    internal class ComputeDeformationNode : IStandardNode
    {
        static string Name = "ComputeDeformation";
        static int Version = 1;
        public static FunctionDescriptor FunctionDescriptor => new(
            Version,
            Name,
            //TODO: Add all of the code that needs to exist outside this main body function in order for this to work.
@"#if defined(UNITY_DOTS_INSTANCING_ENABLED)
	//TODO: this line only gets added if GenerationMode is set to ForReals
    functionNameHere(VertexID, DeformedPosition, DeformedNormal, DeformedTangent);
#else
    DeformedPosition = PositionOS;
	DeformedNormal = NormalOS;
	DeformedTangent = TangentOS;
#endif",
            new ParameterDescriptor("PositionOS", TYPE.Vec3, GraphType.Usage.Local, REF.ObjectSpace_Position),
            new ParameterDescriptor("NormalOS", TYPE.Vec3, GraphType.Usage.Local, REF.ObjectSpace_Normal),
            new ParameterDescriptor("TangentOS", TYPE.Vec3, GraphType.Usage.Local, REF.ObjectSpace_Tangent),
            new ParameterDescriptor("VertexID", TYPE.Vec3, GraphType.Usage.Local, REF.VertexID),
            new ParameterDescriptor("DeformedPosition", TYPE.Vec3, GraphType.Usage.Out),
            new ParameterDescriptor("DeformedNormal", TYPE.Vec3, GraphType.Usage.Out),
            new ParameterDescriptor("DeformedTangent", TYPE.Vec3, GraphType.Usage.Out)
/*
struct DeformedVertexData
{
	float3 Position;
	float3 Normal;
	float3 Tangent;
};

uniform StructuredBuffer<DeformedVertexData> _DeformedMeshData : register(t1);

void Unity_ComputeDeformedVertex(uint vertexID, out float3 positionOut, out float3 normalOut, out float3 tangentOut)
{
	const DeformedVertexData vertexData = _DeformedMeshData[asuint(UNITY_ACCESS_HYBRID_INSTANCED_PROP(_ComputeMeshIndex, float)) + vertexID];
	positionOut = vertexData.Position;
	normalOut = vertexData.Normal;
	tangentOut = vertexData.Tangent;
}
*/
            );

        public static NodeUIDescriptor NodeUIDescriptor => new(
            Version,
            Name,
            displayName: "Compute Deformation",
            tooltip: "Passes computed deformation data to the vertex shader.",
            categories: new string[2] { "Input", "Mesh Deformation" },
            hasPreview: false,
            synonyms: new string[0] { },
            parameters: new ParameterUIDescriptor[3] {
                new ParameterUIDescriptor(
                    name: "DeformedPosition",
                    displayName:"Deformed Position",
                    tooltip: "Outputs the deformed vertex position."
                ),
                new ParameterUIDescriptor(
                    name: "DeformedNormal",
                    displayName:"Deformed Normal",
                    tooltip: "Outputs the deformed vertex normal."
                ),
                new ParameterUIDescriptor(
                    name: "DeformedTangent",
                    displayName:"Deformed Tangent",
                    tooltip: "Outputs the deformed vertex tangent."
                )
            }
        );
    }
}
