using System;
using System.Collections.Generic;
using UnityEditor.ShaderGraph.Registry.Experimental;

namespace UnityEditor.ShaderGraph.Registry
{
    public class RegistryPlaceholder
    {
        public int data;

        public RegistryPlaceholder(int d) { data = d; }
    }


    // TODO: The following will all be replaced or reworked to leverage GraphDelta's storage model for nodes/topologies
    namespace Mock
    {
        [Flags] public enum PortFlags { Input = 0, Output = 1, Vertical = 0, Horizontal = 2 }
        public interface INodeReader
        {
            // For mocking, we need to get some static information about the node so we can properly draw the topology.
            // Ports will ultimately have a type and some flags- but they'll also have static information that may describe the type (eg. HLSL template primitives).

            // even in it's final implementation, this interface will not be very powerful. It will be important to create UI associations with Registration Keys
            // that can allow for specialized dressings for nodes and types.
            bool GetPort(string portKey, out RegistryKey key, out PortFlags flags);
            bool GetNumericLiteral(string path, out float value);
            bool GetStringLiteral(string path, out string value);
        }

        public interface INodeWriter
        {
            void AddPortType<T>(string portKey, PortFlags flags) where T : INodeDefinitionBuilder;
            void AddNumericLiteral(string path, float value);
            void AddStringLiteral(string path, string value);
        }

        class MockNode : INodeReader, INodeWriter
        {
            // Null Concretized nodes and topologies will all just be stored in GraphDelta
            INodeDefinitionBuilder builderRef;
            internal MockNode(INodeDefinitionBuilder builder) { builderRef = builder; }

            struct PortData { public RegistryKey key; public PortFlags flags; }

            Dictionary<string, PortData> ports = new Dictionary<string, PortData>();
            Dictionary<string, float> numericLiterals = new Dictionary<string, float>();
            Dictionary<string, string> stringLiterals = new Dictionary<string, string>();


            public void AddPortType<T>(string portKey, PortFlags flags) where T : INodeDefinitionBuilder
                => ports[portKey] = new PortData { key = Experimental.Registry.ResolveKey<T>(), flags = flags };

            public void AddNumericLiteral(string path, float value) => numericLiterals[path] = value;
            public void AddStringLiteral(string path, string value) => stringLiterals[path] = value;


            public bool GetPort(string portKey, out RegistryKey key, out PortFlags flags)
            {
                if (ports.TryGetValue(portKey, out PortData data))
                {
                    key = data.key;
                    flags = data.flags;
                    return true;
                }
                key = default;
                flags = default;
                return false;
            }

            public bool GetNumericLiteral(string path, out float value) => numericLiterals.TryGetValue(path, out value);
            public bool GetStringLiteral(string path, out string value) => stringLiterals.TryGetValue(path, out value);

        }

    }

    namespace Example
    {
        public class NumericLiteralNode : INodeDefinitionBuilder
        {
            private static RegistryKey RegKey = new RegistryKey { Name = "NumericLiteral", Version = 1 };
            public RegistryKey GetRegistryKey() => RegKey;
            public void BuildNode(Mock.INodeReader userData, Mock.INodeWriter concreteData)
            {
                userData.GetNumericLiteral("value", out float value);
                concreteData.AddPortType<NumericLiteralNode>("out", Mock.PortFlags.Horizontal | Mock.PortFlags.Output);
                concreteData.AddNumericLiteral("out.value", value);
            }
        }

        public class StringLiteralNode : INodeDefinitionBuilder
        {
            private static RegistryKey RegKey = new RegistryKey { Name = "StringLiteral", Version = 1 };
            public RegistryKey GetRegistryKey() => RegKey;
            public void BuildNode(Mock.INodeReader userData, Mock.INodeWriter concreteData)
            {
                userData.GetStringLiteral("value", out string value);
                concreteData.AddPortType<NumericLiteralNode>("out", Mock.PortFlags.Horizontal | Mock.PortFlags.Output);
                concreteData.AddStringLiteral("out.value", value);
            }
        }

        // TODO: Explore an accelerator to convert Enums directly to definitions

        public class GraphType : INodeDefinitionBuilder
        {
            // TODO: This is just an example exploration of a Dynamic Vector
            public enum Precision { Fixed, Half, Full }
            public enum Primitive { Bool, Int, Float }

            public static RegistryKey RegKey => new RegistryKey { Name = "GraphType", Version = 1 };

            public RegistryKey GetRegistryKey() => RegKey;


            public static Precision GetPrecision(Mock.INodeReader node)
            {
                if (node.GetStringLiteral("precision.value", out string outPrecision))
                {
                    if (Enum.TryParse<Precision>(outPrecision, out Precision result))
                        return result;
                }
                return Precision.Full;
            }

            public static Primitive GetPrimitive(Mock.INodeReader node)
            {
                if (node.GetStringLiteral("primitive.value", out string outPrimitive))
                {
                    if (Enum.TryParse<Primitive>(outPrimitive, out Primitive result))
                        return result;
                }
                return Primitive.Float;
            }

            public static int GetCount(Mock.INodeReader node)
            {
                if (node.GetNumericLiteral("count.value", out float outCount))
                    return (int)outCount;
                return 4;
            }


            public void BuildNode(Mock.INodeReader userData, Mock.INodeWriter concreteData)
            {
                // Expected default values- example of static helpers to specifically reach into expected static information.
                string precision = GetPrecision(userData).ToString();
                string primitive = GetPrimitive(userData).ToString();
                float count = GetCount(userData);

                // Create our input ports for literal data to build the type.
                concreteData.AddPortType<NumericLiteralNode>("count", Mock.PortFlags.Horizontal | Mock.PortFlags.Input);
                concreteData.AddPortType<StringLiteralNode>("precision", Mock.PortFlags.Horizontal | Mock.PortFlags.Input);
                concreteData.AddPortType<StringLiteralNode>("primitive", Mock.PortFlags.Horizontal | Mock.PortFlags.Input);

                // Create our output port
                concreteData.AddPortType<GraphType>("out", Mock.PortFlags.Horizontal | Mock.PortFlags.Output);

                // Copy out our statically known data to our output port (it won't work like this with GraphDelta)
                concreteData.AddNumericLiteral("out.count", count);
                concreteData.AddStringLiteral("out.precision", precision);
                concreteData.AddStringLiteral("out.primitive", primitive);

                if (count > 0)
                    concreteData.AddPortType<NumericLiteralNode>("x", Mock.PortFlags.Horizontal | Mock.PortFlags.Input);
                if (count > 1)
                    concreteData.AddPortType<NumericLiteralNode>("y", Mock.PortFlags.Horizontal | Mock.PortFlags.Input);
                if (count > 2)
                    concreteData.AddPortType<NumericLiteralNode>("z", Mock.PortFlags.Horizontal | Mock.PortFlags.Input);
                if (count > 3)
                    concreteData.AddPortType<NumericLiteralNode>("w", Mock.PortFlags.Horizontal | Mock.PortFlags.Input);
            }
        }
    }
}
