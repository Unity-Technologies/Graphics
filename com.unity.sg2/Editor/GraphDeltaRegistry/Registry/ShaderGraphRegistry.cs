using System;
using System.Runtime.Serialization;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.ShaderGraph.Defs;
using UnityEngine;

namespace UnityEditor.ShaderGraph.GraphDelta
{
    class ShaderGraphRegistry
    {
        private static readonly string GET_FD_METHOD_NAME = "get_FunctionDescriptor";
        private static readonly string GET_ND_METHOD_NAME = "get_NodeDescriptor";
        private static readonly string GET_UD_METHOD_NAME = "get_NodeUIDescriptor";

        internal static ShaderGraphRegistry Instance
        {
            get
            {
                if (s_instance == null)
                {
                    s_instance = new();
                    s_instance.InitializeDefaults();
                }
                return s_instance;
            }
        }
        private static ShaderGraphRegistry s_instance = null;

        private ShaderGraphRegistry()
        {
            Registry = new();
            NodeUIInfo = new();
            DefaultTopologies = new(Registry);
        }

        // TODO: remove direct access of these in GraphUI and from API.
        internal Registry Registry;
        internal NodeUIInfo NodeUIInfo;
        internal GraphHandler DefaultTopologies;

        internal void Register(RegistryKey key, INodeUIDescriptorBuilder descriptor) => NodeUIInfo.Register(key, descriptor);
        internal void Register(NodeDescriptor node, NodeUIDescriptor descriptor)
        {
            var key = Registry.Register(node);
            NodeUIInfo.Register(key, new StaticNodeUIDescriptorBuilder(descriptor));
            DefaultTopologies.AddNode(key, key.ToString());
        }
        internal void Register(FunctionDescriptor function, NodeUIDescriptor descriptor)
        {
            var key = Registry.Register(function);
            NodeUIInfo.Register(key, new StaticNodeUIDescriptorBuilder(descriptor));
            DefaultTopologies.AddNode(key, key.ToString());
        }
        internal void Register(FunctionDescriptor func)
        {
            var key = Registry.Register(func);
            DefaultTopologies.AddNode(key, key.ToString());
        }
        internal void Register(NodeDescriptor node)
        {
            var key = Registry.Register(node);
            DefaultTopologies.AddNode(key, key.ToString());
        }
        internal void Register(INodeDefinitionBuilder builder, INodeUIDescriptorBuilder descriptor = null)
        {
            var key = builder.GetRegistryKey();
            Registry.Register(builder);
            if (descriptor != null)
                NodeUIInfo.Register(key, descriptor);
            DefaultTopologies.AddNode(key, key.ToString());
        }
        internal void Register<T>() where T : IRegistryEntry
        {
            var registryEntry = Activator.CreateInstance<T>();
            if (registryEntry is INodeDefinitionBuilder nodeDef)
            {
                Register(nodeDef);
            }
            else
            {
                Registry.Register<T>();
            }
        }


        internal NodeUIDescriptor GetNodeUIDescriptor(RegistryKey key, NodeHandler node) => NodeUIInfo.GetNodeUIDescriptor(key, node);
        internal NodeHandler GetDefaultTopology(RegistryKey key) => DefaultTopologies.GetNode(key.ToString());
        internal INodeDefinitionBuilder GetNodeBuilder(RegistryKey key) => Registry.GetNodeBuilder(key);
        internal ITypeDefinitionBuilder GetTypeBuilder(RegistryKey key) => Registry.GetTypeBuilder(key);
        internal ICastDefinitionBuilder GetCastBuilder(RegistryKey key) => Registry.GetCastBuilder(key);
        internal IContextDescriptor GetContextDescriptor(RegistryKey key) => Registry.GetContextDescriptor(key);

        internal RegistryKey ResolveKey<T>() where T : IRegistryEntry => Registry.ResolveKey<T>();

        internal void InitializeDefaults()
        {
            #region Core
            Register<GraphType>();
            Register<GraphTypeAssignment>();
            Register<GradientType>();
            Register<GradientTypeAssignment>();
            Register<GradientNode>(); // TODO: Needs descriptor or IStandardNode implementation.

            Register<BaseTextureType>();
            Register<BaseTextureTypeAssignment>();
            Register<SamplerStateType>();
            Register<SamplerStateAssignment>();
            #endregion

            // TODO: remove these, but keep until equivalents are working correctly.
            Register<SampleGradientNode>();
            Register<SamplerStateExampleNode>();
            Register<SimpleTextureNode>();
            Register<SimpleSampleTexture2DNode>();
            Register<ShaderGraphContext>();


            #region IStandardNode
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
                    var udMethod = t.GetMethod(GET_UD_METHOD_NAME);

                    if (ndMethod != null)
                    {
                        var nd = (NodeDescriptor)ndMethod.Invoke(null, null);
                        if (!nd.Equals(default(NodeDescriptor)))
                        { // use the NodeDescriptor
                            if (udMethod != null)
                            {
                                var ui = (NodeUIDescriptor)udMethod.Invoke(null, null);
                                Register(nd, ui);
                            }
                            else Register(nd);
                        }
                    }
                    else if (fdMethod != null)
                    {
                        var fd = (FunctionDescriptor)fdMethod.Invoke(null, null);
                        if (!fd.Equals(default(NodeDescriptor)))
                        {  // use the FunctionDescriptor
                            if (udMethod != null)
                            {
                                var ui = (NodeUIDescriptor)udMethod.Invoke(null, null);
                                Register(fd, ui);
                            }
                            else Register(fd);
                        }
                    }
                    else
                    {
                        var msg = $"IStandard node {t} has no node or function descriptor. It was not registered.";
                        Debug.LogWarning(msg);
                    }

                }
            }
            #endregion
        }

        // TODO: Initialize SubGraphs from files
        // TODO: Register SubGraph
        // TODO: Refresh this registry w/subgraphs are modified
        // TODO: Refresh dependent graphs for subgraphs ^^
    }
}
