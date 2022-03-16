using System;
using System.Collections.Generic;
using System.Linq;

namespace UnityEditor.ShaderGraph.GraphDelta
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
            reg.Register<GraphType>();
            reg.Register<GraphTypeAssignment>();
            reg.Register<GradientType>();
            reg.Register<GradientTypeAssignment>();
            reg.Register<GradientNode>();
            reg.Register<SampleGradientNode>();
            reg.Register<DefaultContext>();
            //RegistryInstance.Register<Registry.AddNode>();

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
