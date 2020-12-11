using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

using UnityEditor.ShaderGraph;
using UnityEditor.ShaderGraph.Internal;

namespace UnityEditor.VFX
{
    static class VFXShaderGraphGeneration
    {
        internal enum VFXAttributeType
        {
            Current,
            Source
        }

        private static string[] kVFXAttributeStructNames =
        {
            "Attributes",
            "SourceAttributes"
        };

        // See: VFXShaderWriter.TypeToUniformCode
        // TODO: Collapse these two maps into one
        static readonly Dictionary<Type, Type> kVFXShaderPropertyMap = new Dictionary<Type, Type>
        {
            { typeof(float),     typeof(Vector1ShaderProperty) },
            { typeof(Vector2),   typeof(Vector2ShaderProperty) },
            { typeof(Vector3),   typeof(Vector3ShaderProperty) },
            { typeof(Vector4),   typeof(Vector4ShaderProperty) },
            { typeof(int),       typeof(Vector1ShaderProperty) },
            { typeof(uint),      typeof(Vector1ShaderProperty) },
            { typeof(Matrix4x4), typeof(Matrix4ShaderProperty) },
            { typeof(bool),      typeof(BooleanShaderProperty) },
        };

        static readonly Dictionary<Type, ShaderValueType> kVFXShaderValueTypeyMap = new Dictionary<Type, ShaderValueType>
        {
            { typeof(float),     ShaderValueType.Float   },
            { typeof(Vector2),   ShaderValueType.Float2  },
            { typeof(Vector3),   ShaderValueType.Float3  },
            { typeof(Vector4),   ShaderValueType.Float4  },
            { typeof(int),       ShaderValueType.Integer },
            { typeof(uint),      ShaderValueType.Uint    },
            { typeof(Matrix4x4), ShaderValueType.Matrix4 },
            { typeof(bool),      ShaderValueType.Boolean },
        };

        internal static FieldDescriptor VFXAttributeToFieldDescriptor(VFXAttribute attribute)
        {
            var type = VFXExpression.TypeToType(attribute.type);

            if (!kVFXShaderValueTypeyMap.TryGetValue(type, out var shaderValueType))
                return null;

            return new FieldDescriptor("Attributes", attribute.name, "", shaderValueType);
        }

        internal static AbstractShaderProperty VFXExpressionToShaderProperty(VFXExpression expression, string name)
        {
            var type = VFXExpression.TypeToType(expression.valueType);

            if (!kVFXShaderPropertyMap.TryGetValue(type, out var shaderPropertyType))
                return null;

            // Must flag for non public here since all shader property constructors are internal.
            var property =  (AbstractShaderProperty)Activator.CreateInstance(shaderPropertyType, true);

            property.overrideReferenceName   = name;
            property.overrideHLSLDeclaration = true;
            property.hlslDeclarationOverride = HLSLDeclaration.VFX;

            return property;
        }

        internal static void CollectVFXShaderProperties(PropertyCollector collector, VFXContextCompiledData contextData)
        {
            // See: VFXShaderWriter.WriteCBuffer
            var mapper = contextData.uniformMapper;
            var uniformValues = mapper.uniforms
                .Where(e => !e.IsAny(VFXExpression.Flags.Constant | VFXExpression.Flags.InvalidOnCPU)) // Filter out constant expressions
                .OrderByDescending(e => VFXValue.TypeToSize(e.valueType));

            var uniformBlocks = new List<List<VFXExpression>>();
            foreach (var value in uniformValues)
            {
                var block = uniformBlocks.FirstOrDefault(b => b.Sum(e => VFXValue.TypeToSize(e.valueType)) + VFXValue.TypeToSize(value.valueType) <= 4);
                if (block != null)
                    block.Add(value);
                else
                    uniformBlocks.Add(new List<VFXExpression>() { value });
            }

            foreach (var block in uniformBlocks)
            {
                foreach (var value in block)
                {
                    string name = mapper.GetName(value);

                    //Reserved unity variable name (could be filled manually see : VFXCameraUpdate)
                    if (name.StartsWith("unity_"))
                        continue;

                    var property = VFXShaderGraphGeneration.VFXExpressionToShaderProperty(value, name);
                    collector.AddShaderProperty(property);
                }
            }
        }

        internal static StructDescriptor GenerateVFXAttributesStruct(VFXContext context, VFXAttributeType attributeType)
        {
            IEnumerable<VFXAttributeInfo> attributeInfos;

            if (attributeType == VFXAttributeType.Current)
            {
                attributeInfos = context.GetData().GetAttributes().Where(a =>
                    (context.GetData().IsCurrentAttributeUsed(a.attrib, context)) ||
                    (context.contextType == VFXContextType.Init && context.GetData().IsAttributeStored(a.attrib))); // In init, needs to declare all stored attributes for intialization
            }
            else
            {
                attributeInfos = context.GetData().GetAttributes().Where(a => (context.GetData().IsSourceAttributeUsed(a.attrib, context)));
            }

            var attributes = attributeInfos.Select(a => a.attrib);

            var attributeFieldDescriptors = new List<FieldDescriptor>();
            foreach (var attribute in attributes)
            {
                var afd = VFXShaderGraphGeneration.VFXAttributeToFieldDescriptor(attribute);
                attributeFieldDescriptors.Add(afd);
            }

            return new StructDescriptor
            {
                name = kVFXAttributeStructNames[(int)attributeType],
                fields = attributeFieldDescriptors.ToArray()
            };
        }
    }
}
