using System;
using System.Linq;
using com.unity.shadergraph.defs;
using UnityEditor.ShaderGraph.GraphDelta;

namespace UnityEditor.ShaderGraph.GraphUI
{
    /// <summary>
    /// Creates and sets up a shader graph Registry.
    /// </summary>
    public static class ShaderGraphRegistryBuilder
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
            reg.Register<ShaderGraphContext>();
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
