using UnityEditor.ShaderGraph.GraphDelta;

namespace UnityEditor.ShaderGraph.Defs
{
    internal static class NodeBuilderUtils
    {
        /// <summary>
        /// Calculates the fallback type for the fields of a node, given the
        /// current node data from the user layer.
        /// </summary>
        /// <param name="userData">A reader for a node in the user layer.</param>
        /// <returns>The type that Any should resolve to for ports in the node.</returns>
        internal static ITypeDescriptor FallbackTypeResolver(NodeHandler userData)
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

            return new ITypeDescriptor(
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
        internal static PortHandler ParameterDescriptorToField(
            ParameterDescriptor param,
            ITypeDescriptor fallbackType,
            NodeHandler node,
            Registry registry)
        {
            // Create a port.
            var port = node.AddPort<GraphType>(
                param.Name,
                param.Usage is GraphType.Usage.In or GraphType.Usage.Static or GraphType.Usage.Local,
                registry
            );

            ITypeDescriptor paramType = param.TypeDescriptor;

            // A new type descriptor with all Any values replaced.
            ITypeDescriptor resolvedType = new(
                paramType.Precision == GraphType.Precision.Any ? fallbackType.Precision : paramType.Precision,
                paramType.Primitive == GraphType.Primitive.Any ? fallbackType.Primitive : paramType.Primitive,
                paramType.Length == GraphType.Length.Any ? fallbackType.Length : paramType.Length,
                paramType.Height == GraphType.Height.Any ? fallbackType.Height : paramType.Height
            );

            // Set the port's parameters from the resolved type.
            var typeField = port.GetTypeField();
            typeField.GetSubField<GraphType.Length>(GraphType.kLength).SetData(resolvedType.Length);
            typeField.GetSubField<GraphType.Height>(GraphType.kHeight).SetData(resolvedType.Height);
            typeField.GetSubField<GraphType.Precision>(GraphType.kPrecision).SetData(resolvedType.Precision);
            typeField.GetSubField<GraphType.Primitive>(GraphType.kPrimitive).SetData(resolvedType.Primitive);

            if (param.Usage is GraphType.Usage.Static) typeField.AddSubField("IsStatic", true); // TODO(Liz) : should be metadata
            if (param.Usage is GraphType.Usage.Local) typeField.AddSubField("IsLocal", true);

            int i = 0;
            foreach (var val in param.DefaultValue)
            {
                typeField.SetField<float>($"c{i++}", val);
            }

            return port;
        }
    }
}