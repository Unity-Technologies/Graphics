using System;
using System.Runtime.Serialization;
using System.Collections.Generic;
using UnityEditor.ShaderGraph.GraphDelta;
using System.Linq;
using UnityEditor.ShaderGraph.Registry.Defs;
using UnityEditor.ShaderGraph.Registry.Types;
using com.unity.shadergraph.defs;

namespace UnityEditor.ShaderGraph.Registry.Default
{
    public class DefaultContext : IContextDescriptor
    {
        public IReadOnlyCollection<IContextDescriptor.ContextEntry> GetEntries()
        {
            return new List<IContextDescriptor.ContextEntry>()
            {
                new ()
                {
                    fieldName = "BaseColor",
                    primitive = GraphType.Primitive.Float,
                    precision = GraphType.Precision.Fixed,
                    height = GraphType.Height.One,
                    length = GraphType.Length.One,
                }
            };
        }

        public RegistryFlags GetRegistryFlags()
        {
            return RegistryFlags.Base;
        }

        public RegistryKey GetRegistryKey()
        {
            // Defines the name of the context node on the graph
            return new RegistryKey() { Name = "DefaultContextDescriptor", Version = 1 };
        }
    }

    public static class DefaultRegistry
    {
        public static Registry CreateDefaultRegistry(Action<RegistryKey, Type> afterNodeRegistered = null)
        {
            var reg = new Registry();
            reg.Register<Types.GraphType>();
            reg.Register<Types.GraphTypeAssignment>();
            reg.Register<DefaultContext>();

            reg.Register<Texture2DType>();
            reg.Register<Texture2DAssignment>();
            reg.Register<Texture2DNode>();
            reg.Register<Types.SampleTexture2DNode>();

            //RegistryInstance.Register<Registry.Types.AddNode>();

            // Register nodes from FunctionDescriptors in IStandardNode classes.
            var interfaceType = typeof(IStandardNode);
            var types = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(s => s.GetTypes())
                .Where(p => interfaceType.IsAssignableFrom(p));
            string m = "get_FunctionDescriptor";
            foreach (var t in types)
            {
                var fdMethod = t.GetMethod(m);
                if (t != interfaceType && fdMethod != null)
                {
                    var fd = (FunctionDescriptor)fdMethod.Invoke(null, null);
                    var key = reg.Register(fd);
                    afterNodeRegistered?.Invoke(key, t);
                }
            }
            return reg;
        }
    }
}
