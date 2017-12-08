using System.Reflection;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    /*
    [Title("UV", "Spherize 3D")]
    public class SphericalIndentationNode : CodeFunctionNode
    {
        public SphericalIndentationNode()
        {
            name = "Spherize 3D";
        }
        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod("Unity_SphericalIndentation", BindingFlags.Static | BindingFlags.NonPublic);
        }

        static string Unity_SphericalIndentation(
            [Slot(0, Binding.MeshUV0)] Vector2 uv,
            [Slot(1, Binding.None, 0.5f, 0.5f, 0.5f, 0.5f)] Vector2 center,
            [Slot(2, Binding.None)] Vector1 height,
            [Slot(3, Binding.None, 1f, 1f, 1f, 1f)] Vector1 radius,
            [Slot(4, Binding.None)] out Vector3 resultUV,
            [Slot(5, Binding.None)] out Vector3 resultNormal,
            [Slot(6, Binding.TangentSpaceViewDirection, true)] Vector3 tangentSpaceViewDirection)
        {
            resultUV = Vector3.zero;
            resultNormal = Vector3.up;
            return
                @"
{
    float radius2= radius*radius;
    float3 cur= float3(uv.xy, 0.0f);
    float3 sphereCenter = float3(center, height);
    float3 edgeA = sphereCenter - cur;
    float a2 = dot(edgeA, edgeA);
    resultUV= float3(uv.xy, 0.0f);
    resultNormal= float3(0.0f, 0.0f, 1.0f);
    if (a2 < radius2)
    {
       float a = sqrt(a2);
       edgeA = edgeA / a;
       float cosineR = dot(edgeA, tangentSpaceViewDirection.xyz);
       float x = cosineR * a - sqrt(-a2 + radius2 + a2 * cosineR * cosineR);
       float3 intersectedEdge = cur + tangentSpaceViewDirection * x;
       resultNormal= normalize(sphereCenter - intersectedEdge);
       resultUV = intersectedEdge.xyz;
    }
}";
        }
    }
    */
}
