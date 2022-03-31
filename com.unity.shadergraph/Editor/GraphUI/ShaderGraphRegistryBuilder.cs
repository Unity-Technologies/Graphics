using System;
using System.Collections.Generic;
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
        const string GET_FD_METHOD_NAME = "get_FunctionDescriptor";
        const string GET_MUTLI_FD_METHOD_NAME = "get_FunctionDescriptors";
        const string SET_FD_NAME_TO_REG_KEY_METHOD_NAME = "set_FunctionDescriptorNameToRegistryKey";

        public static Registry CreateDefaultRegistry(
            Action<Dictionary<string, RegistryKey>, Type> afterNodeRegistered = null
        )
        {
            // create the registry
            var reg = new Registry();

            // register required base elements
            reg.Register<GraphType>();
            reg.Register<GraphTypeAssignment>();
            reg.Register<GradientType>();
            reg.Register<GradientTypeAssignment>();
            reg.Register<GradientNode>();
            reg.Register<SampleGradientNode>();
            reg.Register<ShaderGraphContext>();

            // register nodes from FunctionDescriptors in IStandardNode implementers
            var interfaceType = typeof(IStandardNode);
            var types = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(s => s.GetTypes())
                .Where(p => interfaceType.IsAssignableFrom(p));
            foreach (var t in types)
            {
                var fdMethod = t.GetMethod(GET_FD_METHOD_NAME);
                var multiFDMethod = t.GetMethod(GET_MUTLI_FD_METHOD_NAME);

                if (t != interfaceType)
                {
                    Dictionary<string, RegistryKey> fdNameToKey = new();

                    if (multiFDMethod != null)
                    {
                        var fds = (FunctionDescriptor[])multiFDMethod.Invoke(null, null);
                        foreach (FunctionDescriptor fd in fds)
                        {
                            RegistryKey key = reg.Register(fd);
                            fdNameToKey[fd.Name] = key;
                        }
                    }

                    if (fdMethod != null)
                    {
                        var fd = (FunctionDescriptor)fdMethod.Invoke(null, null);
                        RegistryKey key = reg.Register(fd);
                    }

                    if (fdNameToKey.Count > 0)
                    {
                        var setFDToKeyMethod = t.GetMethod(SET_FD_NAME_TO_REG_KEY_METHOD_NAME);
                        setFDToKeyMethod.Invoke(null, new object[] { fdNameToKey });
                    }

                    afterNodeRegistered?.Invoke(fdNameToKey, t);
                }
            }

            return reg;
        }
    }
}
