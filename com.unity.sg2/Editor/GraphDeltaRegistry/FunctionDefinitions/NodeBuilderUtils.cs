using System.Collections;
using UnityEditor.ShaderGraph.GraphDelta;
using UnityEngine;

namespace UnityEditor.ShaderGraph.Defs
{
    internal static class NodeBuilderUtils
    {

        // TODO (Brett) FallbackTypeResolver used to return a TypeDescriptor,
        // TODO however, that changed to being a ParametricTypeDescriptor.
        // TODO For types that can't fallback, we need to decide where the logic
        // TODO for that goes.
        // TODO For now, having a Parametric in all cases is harmless.

        /// <summary>
        /// Calculates the fallback type for the fields of a node, given the
        /// current node data from the user layer.
        /// </summary>
        /// <param name="userData">A reader for a node in the user layer.</param>
        /// <returns>The type that Any should resolve to for ports in the node.</returns>
        internal static ParametricTypeDescriptor FallbackTypeResolver(NodeHandler userData)
        {
            GraphType.Height resolvedHeight = GraphType.Height.Any;
            GraphType.Length resolvedLength = GraphType.Length.Any;
            GraphType.Precision resolvedPrecision = GraphType.Precision.Any;
            GraphType.Primitive resolvedPrimitive = GraphType.Primitive.Any;

            // Find the highest priority value for all type parameters set
            // in the user data.
            foreach (var port in userData.GetPorts())
            {
                var field = port.GetTypeField();

                var lengthField = field.GetSubField<GraphType.Length>(GraphType.kLength);
                var heightField = field.GetSubField<GraphType.Height>(GraphType.kLength);
                var precisionField = field.GetSubField<GraphType.Precision>(GraphType.kLength);
                var primitiveField = field.GetSubField<GraphType.Primitive>(GraphType.kLength);

                if (lengthField != null && GraphType.LengthToPriority[resolvedLength] < GraphType.LengthToPriority[lengthField.GetData()])
                    resolvedLength = lengthField.GetData();

                if (heightField != null && GraphType.HeightToPriority[resolvedHeight] < GraphType.HeightToPriority[heightField.GetData()])
                    resolvedHeight = heightField.GetData();

                if (precisionField != null && GraphType.PrecisionToPriority[resolvedPrecision] < GraphType.PrecisionToPriority[precisionField.GetData()])
                    resolvedPrecision = precisionField.GetData();

                if (primitiveField != null && GraphType.PrimitiveToPriority[resolvedPrimitive] < GraphType.PrimitiveToPriority[primitiveField.GetData()])
                    resolvedPrimitive = primitiveField.GetData();
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

            return new ParametricTypeDescriptor(
                resolvedPrecision,
                resolvedPrimitive,
                resolvedLength,
                resolvedHeight
            );
        }

        private static PortHandler ParametricToField(
            ParameterDescriptor param,
            ParametricTypeDescriptor fallbackType,
            NodeHandler node,
            Registry registry)
        {
            // Create a port.
            var port = node.AddPort<GraphType>(
                param.Name,
                param.Usage is GraphType.Usage.In or GraphType.Usage.Static or GraphType.Usage.Local,
                registry
            );

            ParametricTypeDescriptor paramType = (ParametricTypeDescriptor)param.TypeDescriptor;

            bool isPrecisionDynamic = paramType.Precision == GraphType.Precision.Any;
            bool isPrimitiveDynamic = paramType.Primitive == GraphType.Primitive.Any;
            bool isHeightDynamic = paramType.Height == GraphType.Height.Any;
            bool isLengthDynamic = paramType.Length == GraphType.Length.Any;

            // A new type descriptor with all Any values replaced.
            ParametricTypeDescriptor resolvedType = new(
                isPrecisionDynamic ? fallbackType.Precision : paramType.Precision,
                isPrimitiveDynamic ? fallbackType.Primitive : paramType.Primitive,
                isHeightDynamic ? fallbackType.Length : paramType.Length,
                isLengthDynamic ? fallbackType.Height : paramType.Height
            );
            // Set the port's parameters from the resolved type.
            var typeField = port.GetTypeField();

            // TODO(Liz) : should be metadata
            if (param.Usage is GraphType.Usage.Static) typeField.AddSubField("IsStatic", true);
            if (param.Usage is GraphType.Usage.Local) typeField.AddSubField("IsLocal", true);

            GraphTypeHelpers.InitGraphType(
                typeField,
                length: resolvedType.Length,
                height: resolvedType.Height,
                primitive: resolvedType.Primitive,
                precision: resolvedType.Precision,
                lengthDynamic: isLengthDynamic,
                heightDynamic: isHeightDynamic,
                primitiveDynamic: isPrimitiveDynamic,
                precisionDynamic: isPrecisionDynamic);

            if (param.DefaultValue != null
                && param.DefaultValue is not ReferenceValueDescriptor // there is special handling for these
                && !GraphTypeHelpers.SetByManaged(typeField, param.DefaultValue))
            {
                var defName = node.GetRegistryKey().Name;
                var paramName = param.Name;
                var typeName = param.DefaultValue.GetType().ToString();
                throw new System.Exception($"Default Value of type {typeName} for parameter {paramName} of node definition {defName} is not valid.");
            }
            return port;
        }

        private static PortHandler TextureToField(
            ParameterDescriptor param,
            NodeHandler node,
            Registry registry)
        {
            TextureTypeDescriptor typeDescriptor = (TextureTypeDescriptor)param.TypeDescriptor;
            var port = node.AddPort<BaseTextureType>(
                param.Name,
                param.Usage is GraphType.Usage.In or GraphType.Usage.Static or GraphType.Usage.Local,
                registry);

            BaseTextureType.SetTextureType(port.GetTypeField(), typeDescriptor.TextureType);
            return port;
        }

        private static PortHandler SamplerStateToField(
            ParameterDescriptor param,
            NodeHandler node,
            Registry registry)
        {
            return node.AddPort<SamplerStateType>(
                param.Name,
                param.Usage is GraphType.Usage.In or GraphType.Usage.Static or GraphType.Usage.Local,
                registry);
        }

        private static PortHandler GradientToField(
            ParameterDescriptor param,
            NodeHandler node,
            Registry registry)
        {
            return node.AddPort<GradientType>(
                param.Name,
                param.Usage is GraphType.Usage.In or GraphType.Usage.Static or GraphType.Usage.Local,
                registry
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
        internal static PortHandler ParameterDescriptorToField(
            ParameterDescriptor param,
            ParametricTypeDescriptor fallbackType,
            NodeHandler node,
            Registry registry)
        {
            PortHandler portHandler = param.TypeDescriptor switch
            {
                ParametricTypeDescriptor => ParametricToField(param, fallbackType, node, registry),
                SamplerStateTypeDescriptor => SamplerStateToField(param, node, registry),
                TextureTypeDescriptor => TextureToField(param, node, registry),
                GradientTypeDescriptor => GradientToField(param, node, registry),
                _ => null,
            };
            if (param.DefaultValue is ReferenceValueDescriptor referenceValueDescriptor)
            {
                var graphDelta = portHandler.Owner;
                var existingConnection = graphDelta.GetDefaultConnectionToPort(portHandler.ID, registry);
                if (existingConnection == null)
                {
                    portHandler.Owner.AddDefaultConnection(
                        referenceValueDescriptor.ContextName,
                        portHandler.ID,
                        portHandler.Registry);
                }
            }

            return portHandler;
        }
    }
}
