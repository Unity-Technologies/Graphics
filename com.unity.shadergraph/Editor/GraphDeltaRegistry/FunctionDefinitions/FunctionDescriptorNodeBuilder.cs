using System;
using UnityEditor.ShaderFoundry;
using UnityEditor.ShaderGraph.GraphDelta;
using UnityEditor.ShaderGraph.Registry;
using UnityEditor.ShaderGraph.Registry.Defs;
using UnityEditor.ShaderGraph.Registry.Types;
using UnityEngine;
using static UnityEditor.ShaderGraph.Registry.Types.GraphType;

namespace com.unity.shadergraph.defs
{
    /// <summary>
    /// FunctionDescriptorNodeBuilder is a way to to make INodeDefinitionBuilder
    /// instances from FunctionDescriptors.
    ///
    /// This is used to load the standard node defintions into the Registry.
    /// (See: StandardNodeDefinitions)
    /// </summary>
    internal class FunctionDescriptorNodeBuilder : INodeDefinitionBuilder
    {
        private readonly FunctionDescriptor m_functionDescriptor;

        /// <summary>
        /// Calculates the fallback type for the fields of a node, given the
        /// current node data from the user layer.
        /// </summary>
        /// <param name="userData">A reader for a node in the user layer.</param>
        /// <returns>The type that Any should resolve to for ports in the node.</returns>
        static TypeDescriptor FallbackTypeResolver(NodeHandler userData)
        {
            // TODO (Brett) You really need to test this more!
            // 1 < 4 < 3 < 2 for Height and Length
            // Bigger wins for Primitive and Precision

            Height resolvedHeight = Height.Any;
            Length resolvedLength = Length.Any;
            Precision resolvedPrecision = Precision.Any;
            Primitive resolvedPrimitive = Primitive.Any;

            // Find the highest priority value for all type parameters set
            // in the user data.
            foreach (var port in userData.GetPorts())
            {
                var field = port.GetTypeField();

                var lengthField = field.GetSubField<Length>(kLength);
                var heightField = field.GetSubField<Height>(kLength);
                var precisionField = field.GetSubField<Precision>(kLength);
                var primitiveField = field.GetSubField<Primitive>(kLength);

                if (lengthField != null && LengthToPriority[resolvedLength] < LengthToPriority[lengthField.GetData()])
                    resolvedLength = lengthField.GetData();

                if (heightField != null && HeightToPriority[resolvedHeight] < HeightToPriority[heightField.GetData()])
                    resolvedHeight = heightField.GetData();

                if (precisionField != null && PrecisionToPriority[resolvedPrecision] < PrecisionToPriority[precisionField.GetData()])
                    resolvedPrecision = precisionField.GetData();

                if (primitiveField != null && PrimitiveToPriority[resolvedPrimitive] < PrimitiveToPriority[primitiveField.GetData()])
                    resolvedPrimitive = primitiveField.GetData();


            }

            // If we didn't find a value for a type parameter in user data,
            // set it to a legacy default.
            if (resolvedLength == Length.Any)
            {
                resolvedLength = Length.Four;
            }
            if (resolvedHeight == Height.Any)
            {
                // this matches the legacy resolving behavior
                resolvedHeight = Height.One;
            }
            if (resolvedPrecision == Precision.Any)
            {
                resolvedPrecision = Precision.Single;
            }
            if (resolvedPrimitive == Primitive.Any)
            {
                resolvedPrimitive = Primitive.Float;
            }

            return new TypeDescriptor(
                resolvedPrecision,
                resolvedPrimitive,
                resolvedLength,
                resolvedHeight
            );
        }

        /// <summary>
        /// Adds a port/field to the passed in node with configuration from param.
        /// </summary>
        /// <param name="param">Configuration info</param>
        /// <param name="resolveType">The type to resolve ANY fields to.</param>
        /// <param name="nodeReader">The way to read from the port/field.</param>
        /// <param name="nodeWriter">The way to write to the port/field.</param>
        /// <param name="registry">The registry holding the node.</param>
        /// <returns></returns>
        static PortHandler ParameterDescriptorToField(
            ParameterDescriptor param,
            TypeDescriptor fallbackType,
            NodeHandler node,
            Registry registry)
        {
            // Create a port.
            var port = node.AddPort<GraphType>(param.Name, param.Usage is Usage.In or Usage.Static or Usage.Local, registry);

            TypeDescriptor paramType = param.TypeDescriptor;

            // A new type descriptor with all Any values replaced.
            TypeDescriptor resolvedType = new(
                paramType.Precision == Precision.Any ? fallbackType.Precision : paramType.Precision,
                paramType.Primitive == Primitive.Any ? fallbackType.Primitive : paramType.Primitive,
                paramType.Length == Length.Any ? fallbackType.Length : paramType.Length,
                paramType.Height == Height.Any ? fallbackType.Height : paramType.Height
            );

            // Set the port's parameters from the resolved type.
            var typeField = port.GetTypeField();
            typeField.GetSubField<Length>(kLength).SetData(resolvedType.Length);
            typeField.GetSubField<Height>(kHeight).SetData(resolvedType.Height);
            typeField.GetSubField<Precision>(kPrecision).SetData(resolvedType.Precision);
            typeField.GetSubField<Primitive>(kPrimitive).SetData(resolvedType.Primitive);

            if (param.Usage is Usage.Static) typeField.AddSubField("IsStatic", true); // TODO(Liz) : should be metadata
            if (param.Usage is Usage.Local)  typeField.AddSubField("IsLocal", true);

            int i = 0;
            foreach(var val in param.DefaultValue)
            {
                typeField.SetField<float>($"c{i++}", val);
            }

            return port;
        }

        internal FunctionDescriptorNodeBuilder(FunctionDescriptor fd)
        {
            m_functionDescriptor = fd; // copy
        }

        public void BuildNode(
            NodeHandler node,
            Registry registry)
        {
            TypeDescriptor fallbackType = FallbackTypeResolver(node);
            foreach (var param in m_functionDescriptor.Parameters)
            {
                //userData.TryGetPort(param.Name, out IPortReader portReader);
                ParameterDescriptorToField(
                    param,
                    fallbackType,
                    node,
                    registry);
            }
        }

        ShaderFunction INodeDefinitionBuilder.GetShaderFunction(
            NodeHandler data,
            ShaderContainer container,
            Registry registry)
        {
            // Get a builder from ShaderFoundry
            var shaderFunctionBuilder = new ShaderFunction.Builder(container, m_functionDescriptor.Name);

            // Set up the vars in the shader function.
            foreach (var param in m_functionDescriptor.Parameters)
            {
                var port = data.GetPort(param.Name);
                var field = port.GetTypeField();
                var shaderType = registry.GetShaderType(field, container);

                if (param.Usage == Usage.In || param.Usage == Usage.Static || param.Usage == Usage.Local)
                {
                    shaderFunctionBuilder.AddInput(shaderType, param.Name);
                }
                else if (param.Usage == Usage.Out)
                {
                    shaderFunctionBuilder.AddOutput(shaderType, param.Name);
                }
                else
                {
                    throw new Exception($"No ShaderFunction parameter type for {param.Usage}");
                }
            }

            // Add the shader function body.
            shaderFunctionBuilder.AddLine(m_functionDescriptor.Body);

            // Return the results of ShaderFoundry's build.
            return shaderFunctionBuilder.Build();
        }

        RegistryKey IRegistryEntry.GetRegistryKey()
        {
            return new RegistryKey
            {
                Name = m_functionDescriptor.Name,
                Version = m_functionDescriptor.Version
            };
        }

        RegistryFlags IRegistryEntry.GetRegistryFlags()
        {
            return RegistryFlags.Func;
        }
    }
}
