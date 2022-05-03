using System;
using System.Linq;
using UnityEditor.ShaderGraph.Defs;
using UnityEditor.ShaderGraph.GraphDelta;
using UnityEngine;

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
            reg.Register<UnityEditor.ShaderGraph.GraphDelta.GradientNode>();
            reg.Register<SampleGradientNode>();
            reg.Register<BaseTextureType>();
            reg.Register<BaseTextureTypeAssignment>();
            reg.Register<SamplerStateType>();
            reg.Register<SamplerStateAssignment>();

            reg.Register<SamplerStateExampleNode>();
            reg.Register<SimpleTextureNode>();
            reg.Register<SimpleSampleTexture2DNode>();
            reg.Register<ShaderGraphContext>();

            // Register nodes from IStandardNode implementers.
            var interfaceType = typeof(IStandardNode);
            var types = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(s => s.GetTypes())
                .Where(p => interfaceType.IsAssignableFrom(p));
            foreach (Type t in types)
            {
                if (t != interfaceType)
                {
                    var ndMethod = t.GetMethod(GET_ND_METHOD_NAME);
                    var fdMethod = t.GetMethod(GET_FD_METHOD_NAME);
                    if (ndMethod != null)
                    {
                        var nd = (NodeDescriptor)ndMethod.Invoke(null, null);
                        if (!nd.Equals(default(NodeDescriptor)))
                        { // use the NodeDescriptor
                            RegistryKey registryKey = reg.Register(nd);
                            afterNodeRegistered?.Invoke(registryKey, t);
                        }
                    }
                    else if (fdMethod != null)
                    {
                        var fd = (FunctionDescriptor)fdMethod.Invoke(null, null);
                        if (!fd.Equals(default(NodeDescriptor)))
                        {  // use the FunctionDescriptor
                            RegistryKey registryKey = reg.Register(fd);
                            afterNodeRegistered?.Invoke(registryKey, t);
                        }
                    }
                    else
                    {
                        var msg = $"IStandard node {t} has no node or function descriptor. It was not registered.";
                        Debug.LogWarning(msg);
                    }
                }
            }
            return reg;
        }
    }
}
