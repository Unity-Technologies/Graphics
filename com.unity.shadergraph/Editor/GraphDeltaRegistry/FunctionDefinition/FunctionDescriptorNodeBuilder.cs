using System;
using UnityEditor.ShaderFoundry;
using UnityEditor.ShaderGraph.GraphDelta;

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
        static TypeDescriptor FallbackTypeResolver(INodeReader userData)
        {
            // TODO (Brett) You really need to test this more!
            // 1 < 4 < 3 < 2 for Height and Length
            // Bigger wins for Primitive and Precision

            GraphType.Height resolvedHeight = GraphType.Height.Any;
            GraphType.Length resolvedLength = GraphType.Length.Any;
            GraphType.Precision resolvedPrecision = GraphType.Precision.Any;
            GraphType.Primitive resolvedPrimitive = GraphType.Primitive.Any;

            // Find the highest priority value for all type parameters set
            // in the user data.
            foreach (var port in userData.GetPorts())
            {
                var field = (IFieldReader)port;
                if (field.TryGetSubField(GraphType.kLength, out IFieldReader fieldReader))
                {
                    fieldReader.TryGetValue(out GraphType.Length readLength);
                    if (GraphType.LengthToPriority[resolvedLength] < GraphType.LengthToPriority[readLength])
                    {
                        resolvedLength = readLength;
                    }
                }
                if (field.TryGetSubField(GraphType.kHeight, out fieldReader))
                {
                    fieldReader.TryGetValue(out GraphType.Height readHeight);
                    if (GraphType.HeightToPriority[resolvedHeight] < GraphType.HeightToPriority[readHeight])
                    {
                        resolvedHeight = readHeight;
                    }
                }
                if (field.TryGetSubField(GraphType.kPrecision, out fieldReader))
                {
                    fieldReader.TryGetValue(out GraphType.Precision readPrecision);
                    if (GraphType.PrecisionToPriority[resolvedPrecision] < GraphType.PrecisionToPriority[readPrecision])
                    {
                        resolvedPrecision = readPrecision;
                    }
                }
                if (field.TryGetSubField(GraphType.kPrimitive, out fieldReader))
                {
                    fieldReader.TryGetValue(out GraphType.Primitive readPrimitive);
                    if (GraphType.PrimitiveToPriority[resolvedPrimitive] < GraphType.PrimitiveToPriority[readPrimitive])
                    {
                        resolvedPrimitive = readPrimitive;
                    }
                }
            }

            // If we didn't find a value for a type parameter in user data,
            // set it to a legacy default.
            if (resolvedLength == GraphType.Length.Any)
            {
                resolvedLength = GraphType.Length.Four;
            }
            if (resolvedHeight == GraphType.Height.Any)
            {
                // this matches the legacy resolving behavior
                resolvedHeight = GraphType.Height.One;
            }
            if (resolvedPrecision == GraphType.Precision.Any)
            {
                resolvedPrecision = GraphType.Precision.Single;
            }
            if (resolvedPrimitive == GraphType.Primitive.Any)
            {
                resolvedPrimitive = GraphType.Primitive.Float;
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
        static IPortWriter ParameterDescriptorToField(
            ParameterDescriptor param,
            TypeDescriptor fallbackType,
            INodeReader nodeReader,
            INodeWriter nodeWriter,
            Registry registry)
        {
            // Create a port.
            IPortWriter port = nodeWriter.AddPort<GraphType>(
                nodeReader,
                param.Name,
                param.Usage is GraphType.Usage.In or GraphType.Usage.Static or GraphType.Usage.Local,
                registry
            );
            TypeDescriptor paramType = param.TypeDescriptor;

            // A new type descriptor with all Any values replaced.
            TypeDescriptor resolvedType = new(
                paramType.Precision == GraphType.Precision.Any ? fallbackType.Precision : paramType.Precision,
                paramType.Primitive == GraphType.Primitive.Any ? fallbackType.Primitive : paramType.Primitive,
                paramType.Length == GraphType.Length.Any ? fallbackType.Length : paramType.Length,
                paramType.Height == GraphType.Height.Any ? fallbackType.Height : paramType.Height
            );

            // Set the port's parameters from the resolved type.
            port.SetField(GraphType.kLength, resolvedType.Length);
            port.SetField(GraphType.kHeight, resolvedType.Height);
            port.SetField(GraphType.kPrecision, resolvedType.Precision);
            port.SetField(GraphType.kPrimitive, resolvedType.Primitive);

            if (param.Usage is GraphType.Usage.Static) port.SetField("IsStatic", true);
            if (param.Usage is GraphType.Usage.Local) port.SetField("IsLocal", true);

            int i = 0;
            foreach(var val in param.DefaultValue)
            {
                port.SetField($"c{i++}", val);
            }

            return port;
        }

        internal FunctionDescriptorNodeBuilder(FunctionDescriptor fd)
        {
            m_functionDescriptor = fd; // copy
        }

        public void BuildNode(
            INodeReader userData,
            INodeWriter generatedData,
            Registry registry)
        {
            TypeDescriptor fallbackType = FallbackTypeResolver(userData);
            foreach (var param in m_functionDescriptor.Parameters)
            {
                //userData.TryGetPort(param.Name, out IPortReader portReader);
                ParameterDescriptorToField(
                    param,
                    fallbackType,
                    userData,
                    generatedData,
                    registry);
            }
        }

        ShaderFunction INodeDefinitionBuilder.GetShaderFunction(
            INodeReader data,
            ShaderContainer container,
            Registry registry)
        {
            // Get a builder from ShaderFoundry
            var shaderFunctionBuilder = new ShaderFunction.Builder(container, m_functionDescriptor.Name);

            // Set up the vars in the shader function.
            foreach (var param in m_functionDescriptor.Parameters)
            {
                data.TryGetPort(param.Name, out var port);
                var shaderType = registry.GetShaderType((IFieldReader)port, container);

                if (param.Usage == GraphType.Usage.In ||
                    param.Usage == GraphType.Usage.Static ||
                    param.Usage == GraphType.Usage.Local)
                {
                    shaderFunctionBuilder.AddInput(shaderType, param.Name);
                }
                else if (param.Usage == GraphType.Usage.Out)
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
