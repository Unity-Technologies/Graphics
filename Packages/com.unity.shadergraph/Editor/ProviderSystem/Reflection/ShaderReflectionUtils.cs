
using System.Collections.Generic;
using System.Text;
using UnityEditor.ShaderApiReflection;
using UnityEngine;

namespace UnityEditor.ShaderGraph.ProviderSystem
{
    internal static class ShaderReflectionUtils
    {
        internal static bool TryResolve(string providerKey, GUID assetId, out ReflectedFunction function)
        {
            var source = AssetDatabase.LoadAssetByGUID<ShaderInclude>(assetId)?.Reflection;

            function = default;
            if (source == null)
                return false;

            foreach (var func in source.ReflectedFunctions)
            {
                var otherProviderKey = ShaderObjectUtils.EvaluateProviderKey(ToShaderFunction(func));
                if (providerKey == otherProviderKey)
                {
                    function = func;
                    return true;
                }
            }

            return false;
        }

        internal static IShaderType ToShaderType(string typeName)
        {
            return new ShaderType(typeName);
        }

        internal static IShaderField ToShaderField(ReflectedParameter refParam)
        {
            var type = ToShaderType(refParam.TypeName);
            bool isInput = refParam.DirectionFlags == ReflectedParameter.Direction.In || refParam.DirectionFlags == ReflectedParameter.Direction.InOut;
            bool isOutput = refParam.DirectionFlags == ReflectedParameter.Direction.Out || refParam.DirectionFlags == ReflectedParameter.Direction.InOut;
            return new ShaderField(refParam.Name, isInput, isOutput, type, refParam.Hints);
        }

        internal static IShaderFunction ToShaderFunction(ReflectedFunction refFunc)
        {
            var returnType = ToShaderType(refFunc.ReturnTypeName);
            List<IShaderField> fields = new();
            foreach (var param in refFunc.Parameters)
            {
                fields.Add(ToShaderField(param));
            }            
            return new ShaderFunction(refFunc.Name, refFunc.EnclosingNamespace, fields, returnType, refFunc.BodyText, refFunc.Hints);
        }
    }
}
