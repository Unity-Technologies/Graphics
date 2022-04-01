using System;
using System.Linq;
using UnityEditor.ShaderGraph.Defs;
using UnityEditor.ShaderGraph.GraphDelta;

namespace UnityEditor.ShaderGraph.GraphUI
{
    /// <summary>
    /// Creates and sets up a shader graph Registry.
    /// </summary>
    public static class ShaderGraphRegistryBuilder
    {
        private static readonly string GET_FD_METHOD_NAME = "get_FunctionDescriptor";
        private static readonly string GET_ND_METHOD_NAME = "get_NodeDescriptor";

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

            // Register nodes from IStandardNode implementers.
            var interfaceType = typeof(IStandardNode);
            var types = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(s => s.GetTypes())
                .Where(p => interfaceType.IsAssignableFrom(p));
            foreach (var t in types)
            {
                if (t != interfaceType)
                {
                    var ndMethod = t.GetMethod(GET_ND_METHOD_NAME);
                    if (ndMethod != null)
                    {
                        var nd = (NodeDescriptor)ndMethod.Invoke(null, null);
                        if (!nd.Equals(default(NodeDescriptor)))
                        { // use the NodeDescriptor
                            RegistryKey registryKey = reg.Register(nd);
                            afterNodeRegistered?.Invoke(registryKey, t);
                        }
                        else
                        { // use the FunctionDescriptor
                            var fdMethod = t.GetMethod(GET_FD_METHOD_NAME);
                            if (fdMethod != null)
                            {
                                var fd = (FunctionDescriptor)fdMethod.Invoke(null, null);
                                RegistryKey registryKey = reg.Register(fd);
                                afterNodeRegistered?.Invoke(registryKey, t);
                            }
                        }
                    }
                }
            }
            return reg;
        }
    }
}
