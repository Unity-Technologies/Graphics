using System.Collections.Generic;
using UnityEngine;

namespace Unity.GraphCommon.LowLevel.Editor
{
    static class HlslCodeHelper
    {
        static Dictionary<System.Type, string> s_TypeNames;
        static HlslCodeHelper()
        {
            s_TypeNames = new()
            {
                { typeof(bool), "bool" },
                { typeof(float), "float" },
                { typeof(Vector2), "float2" },
                { typeof(Vector3), "float3" },
                { typeof(Vector4), "float4" },
                { typeof(int), "int" },
                { typeof(Vector2Int), "int2" },
                { typeof(Vector3Int), "int3" },
                //{ typeof(Vector4Int), "int4" },
                { typeof(uint), "uint" },
                //{ typeof(uint), "uint2" },
                //{ typeof(uint), "uint3" },
                //{ typeof(uint), "uint4" },
                { typeof(Color), "float4" },
                { typeof(Matrix4x4), "float4x4" },
                { typeof(Quaternion), "float4" },
                { typeof(Texture2D), "texture2D" },
            };
        }

        public static string GetTypeName(System.Type type) => s_TypeNames.TryGetValue(type, out var typeName) ? typeName : type.Name;

        public static string GetValueString(object o)
        {
            return o switch
            {
                bool b => b ? "true" : "false",
                float f => GetValueString(f),
                Vector2 f2 => $"float2({GetValueString(f2.x)}, {GetValueString(f2.y)})",
                Vector3 f3 => $"float3({GetValueString(f3.x)}, {GetValueString(f3.y)}, {GetValueString(f3.z)})",
                Vector4 f4 => $"float4({GetValueString(f4.x)}, {GetValueString(f4.y)}, {GetValueString(f4.z)}, {GetValueString(f4.w)})",
                Color c => $"float4({GetValueString(c.r)}, {GetValueString(c.g)}, {GetValueString(c.b)}, {GetValueString(c.a)})",
                uint u => $"{u}u",
                _ => o != null ? o.ToString() : "null"
            };
        }

        static string GetValueString(float f) => $"{f:0.0#####}f";
    }
}
