using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.ShaderFoundry;
using UnityEditor.ShaderGraph.GraphDelta;
using static UnityEditor.ShaderGraph.GraphDelta.ContextEntryEnumTags;

namespace UnityEditor.ShaderGraph.Generation
{
    internal static class InterpreterExtensions
    {
        public static ShaderType GetShaderType(this PortHandler port, Registry registry, ShaderContainer container)
        {
            return registry.GetTypeBuilder(port.GetTypeField().GetRegistryKey()).GetShaderType(port.GetTypeField(), container, registry);
        }

        public static ITypeDefinitionBuilder GetBuilder(this PortHandler port, Registry registry)
        {
            return registry.GetTypeBuilder(port.GetTypeField().GetRegistryKey());
        }

        public static string GetDefaultValueString(this PortHandler port, Registry registry, ShaderContainer container)
        {
            var explicitDefault = port.GetField<string>(kDefaultValue);
            if(explicitDefault != null)
            {
                return explicitDefault.GetData();
            }
            ShaderType type = port.GetShaderType(registry, container);
            var builder = port.GetBuilder(registry);
            switch(builder)
            {
                case GraphType _:
                    var field = port.GetTypeField();
                    if ((int)GraphTypeHelpers.GetLength(field) > 1)
                    {
                         var val = GraphTypeHelpers.GetAsVec4(port.GetTypeField());
                        return $"({val.x}, {val.y}, {val.z}, {val.w})";
                    }
                    else
                    {
                        var val = GraphTypeHelpers.GetAsFloat(port.GetTypeField());
                        return $"{val}";
                    }
                case BaseTextureType _:
                    return "\"white\" {}";
                default:
                    break;
            }
            return "";
        }

        public static string GetDisplayNameString(this PortHandler port)
        {
            var explicitName = port.GetField<string>(kDisplayName);
            if(explicitName != null)
            {
                return explicitName.GetData();
            }
            else
            {
                return $"_{port.ID.ParentPath}_{port.ID.LocalPath}";
            }
        }
    }
}
