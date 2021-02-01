using System;
using UnityEditor.ShaderGraph.Internal;

namespace UnityEditor.ShaderGraph
{
    static class ValueUtilities
    {
        public static string ToShaderString(this ShaderValueType type, string precisionToken = PrecisionUtil.Token)
        {
            switch (type)
            {
                case ShaderValueType.Boolean:
                    return precisionToken;
                case ShaderValueType.Float:
                    return precisionToken;
                case ShaderValueType.Float2:
                    return $"{precisionToken}2";
                case ShaderValueType.Float3:
                    return $"{precisionToken}3";
                case ShaderValueType.Float4:
                    return $"{precisionToken}4";
                case ShaderValueType.Half:
                    return precisionToken;
                case ShaderValueType.Half2:
                    return $"{precisionToken}2";
                case ShaderValueType.Half3:
                    return $"{precisionToken}3";
                case ShaderValueType.Half4:
                    return $"{precisionToken}4";
                case ShaderValueType.Matrix2:
                    return $"{precisionToken}2x2";
                case ShaderValueType.Matrix3:
                    return $"{precisionToken}3x3";
                case ShaderValueType.Matrix4:
                    return $"{precisionToken}4x4";
                case ShaderValueType.Integer:
                    return "int";
                case ShaderValueType.Uint:
                    return "uint";
                case ShaderValueType.Uint4:
                    return "uint4";
                default:
                    return "Error";
            }
        }

        public static string ToNativeShaderString(this ShaderValueType type)
        {
            switch (type)
            {
                case ShaderValueType.Boolean:
                    return "bool";
                case ShaderValueType.Float:
                    return "float";
                case ShaderValueType.Float2:
                    return "float2";
                case ShaderValueType.Float3:
                    return "float3";
                case ShaderValueType.Float4:
                    return "float4";
                case ShaderValueType.Half:
                    return "half";
                case ShaderValueType.Half2:
                    return "half2";
                case ShaderValueType.Half3:
                    return "half3";
                case ShaderValueType.Half4:
                    return "half4";
                case ShaderValueType.Matrix2:
                    return "float2x2";
                case ShaderValueType.Matrix3:
                    return "float3x3";
                case ShaderValueType.Matrix4:
                    return "float4x4";
                case ShaderValueType.Integer:
                    return "int";
                case ShaderValueType.Uint:
                    return "uint";
                case ShaderValueType.Uint4:
                    return "uint4";
                default:
                    return "Error";
            }
        }

        public static int GetVectorCount(this ShaderValueType type)
        {
            switch (type)
            {
                case ShaderValueType.Float2:
                    return 2;
                case ShaderValueType.Float3:
                    return 3;
                case ShaderValueType.Float4:
                    return 4;
                case ShaderValueType.Half2:
                    return 2;
                case ShaderValueType.Half3:
                    return 3;
                case ShaderValueType.Half4:
                    return 4;
                default:
                    return 0;
            }
        }
    }
}
