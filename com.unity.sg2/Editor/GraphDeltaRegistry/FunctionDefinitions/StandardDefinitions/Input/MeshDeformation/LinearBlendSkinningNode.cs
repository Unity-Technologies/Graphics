using System;
using System.Collections.Generic;
using UnityEditor.ShaderGraph.GraphDelta;

namespace UnityEditor.ShaderGraph.Defs
{
    internal class LinearBlendSkinningNode : IStandardNode
    {
        static string Name = "LinearBlendSkinning";
        static int Version = 1;
        public static FunctionDescriptor FunctionDescriptor => new(
            Version,
            Name,
//TODO: How to handle Unity_LinearBlendSkinning_float being float or half
//TODO: This node only works in the vertex shader - so the outputs can only be connected to the vertex shader inputs
//TODO: Need to define the Unity_LinearBlendSkinning_float function
@"#if defined(UNITY_DOTS_INSTANCING_ENABLED)
	Unity_LinearBlendSkinning_float(BoneIndices, BoneWeights, VertexPosition, VertexNormal, VertexTangent, SkinnedPosition, SkinnedNormal, SkinnedTangent);
#else
	SkinnedPosition = VertexPosition;
	SkinnedNormal = VertexNormal;
	SkinnedTangent = VertexTangent;
#endif	
	description.Position = SkinnedPosition;
	description.Normal = SkinnedNormal;
	description.Tangent = SkinnedTangent;
	return description;",
            new ParameterDescriptor("VertexPosition", TYPE.Vec3, GraphType.Usage.In, REF.ObjectSpace_Position),
            new ParameterDescriptor("VertexNormal", TYPE.Vec3, GraphType.Usage.In, REF.ObjectSpace_Normal),
            new ParameterDescriptor("VertexTangent", TYPE.Vec3, GraphType.Usage.In, REF.ObjectSpace_Tangent),
            new ParameterDescriptor("SkinnedPosition", TYPE.Vec3, GraphType.Usage.Out),
            new ParameterDescriptor("SkinnedNormal", TYPE.Vec3, GraphType.Usage.Out),
            new ParameterDescriptor("SkinnedTangent", TYPE.Vec3, GraphType.Usage.Out),
            new ParameterDescriptor("BoneIndices", TYPE.Int4, GraphType.Usage.Local, REF.BoneIndices),
            new ParameterDescriptor("BoneWeights", TYPE.Vec4, GraphType.Usage.Local, REF.BoneWeights1)
/*
uniform StructuredBuffer<float3x4> _SkinMatrices;
        
void Unity_LinearBlendSkinning_float(uint4 indices, float4 weights, float3 positionIn, float3 normalIn, float3 tangentIn, out float3 positionOut, out float3 normalOut, out float3 tangentOut)
{
    positionOut = 0;
    normalOut = 0;
    tangentOut = 0;
    for (int i = 0; i < 4; ++i)
        {
            float3x4 skinMatrix = _SkinMatrices[indices[i] + asint(UNITY_ACCESS_HYBRID_INSTANCED_PROP(_SkinMatrixIndex,float))];
            float3 vtransformed = mul(skinMatrix, float4(positionIn, 1));
            float3 ntransformed = mul(skinMatrix, float4(normalIn, 0));
            float3 ttransformed = mul(skinMatrix, float4(tangentIn, 0));
        
            positionOut += vtransformed * weights[i];
            normalOut   += ntransformed * weights[i];
            tangentOut  += ttransformed * weights[i];
        }
}
*/
            );

        public static NodeUIDescriptor NodeUIDescriptor => new(
            Version,
            Name,
            displayName: "Linear Blend Skinning",
            tooltip: "Applies linear blend vertex skinning in the vertex shader.",
            categories: new string[2] { "Input", "Mesh Deformation" },
            hasPreview: false,
            synonyms: new string[0] { },
            parameters: new ParameterUIDescriptor[6] {
                new ParameterUIDescriptor(
                    name: "VertexPosition",
                    displayName:"Vertex Position",
                    tooltip: "Position of the vertex in object space."
                ),
                new ParameterUIDescriptor(
                    name: "VertexNormal",
                    displayName:"Vertex Normal",
                    tooltip: "Normal of the vertex in object space."
                ),
                new ParameterUIDescriptor(
                    name: "VertexTangent",
                    displayName:"Vertex Tangent",
                    tooltip: "Tangent of the vertex in object space."
                ),
                new ParameterUIDescriptor(
                    name: "SkinnedPosition",
                    displayName:"Skinned Position",
                    tooltip: "Outputs the skinned vertex position."
                ),
                new ParameterUIDescriptor(
                    name: "SkinnedNormal",
                    displayName:"Skinned Normal",
                    tooltip: "Outputs the skinned vertex normal."
                ),
                new ParameterUIDescriptor(
                    name: "SkinnedTangent",
                    displayName:"Skinned Tangent",
                    tooltip: "Outputs the skinned vertex tangent."
                )
            }
        );
    }
}
