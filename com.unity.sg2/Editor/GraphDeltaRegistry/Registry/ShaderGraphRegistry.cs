using System;
using System.Linq;
using UnityEditor.ShaderGraph.Defs;
using UnityEngine;

namespace UnityEditor.ShaderGraph.GraphDelta
{
    internal class ShaderGraphRegistry
    {
        private static readonly string GET_FD_METHOD_NAME = "get_FunctionDescriptor";
        private static readonly string GET_ND_METHOD_NAME = "get_NodeDescriptor";
        private static readonly string GET_UD_METHOD_NAME = "get_NodeUIDescriptor";
        private static readonly string GET_VERSION_METHOD_NAME = "get_Version";

        // TODO (Brett) I'd prefer if this were called `DefaultInstance` or
        // TODO (Brett) something else that implies what is loaded into it.
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

        internal ShaderGraphRegistry()
        {
            Registry = new();
            NodeUIInfo = new();
            DefaultTopologies = new(Registry);
        }

        // TODO: remove direct access of these in GraphUI and from API.
        internal Registry Registry;
        internal NodeUIInfo NodeUIInfo;
        internal GraphHandler DefaultTopologies;

        internal void Register(RegistryKey key, INodeUIDescriptorBuilder descriptor) =>
            NodeUIInfo.Register(key, descriptor);

        internal void Register(NodeDescriptor node, NodeUIDescriptor descriptor)
        {
            var key = Registry.Register(node);
            NodeUIInfo.Register(key, new StaticNodeUIDescriptorBuilder(descriptor));
            DefaultTopologies.AddNode(key, key.ToString());
        }

        internal void Register(FunctionDescriptor function, NodeUIDescriptor descriptor, int version)
        {
            var key = Registry.Register(function, version);
            NodeUIInfo.Register(key, new StaticNodeUIDescriptorBuilder(descriptor));
            DefaultTopologies.AddNode(key, key.ToString());
        }

        internal void Register(FunctionDescriptor func, int version)
        {
            var key = Registry.Register(func, version);
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

        internal void Register<T>() where T : IRegistryEntry =>
            Registry.Register<T>();

        internal NodeUIDescriptor GetNodeUIDescriptor(RegistryKey key, NodeHandler node = null) =>
            NodeUIInfo.GetNodeUIDescriptor(key, node ?? GetDefaultTopology(key));

        internal NodeHandler GetDefaultTopology(RegistryKey key) =>
            DefaultTopologies.GetNode(key.ToString());

        internal INodeDefinitionBuilder GetNodeBuilder(RegistryKey key) =>
            Registry.GetNodeBuilder(key);

        internal ITypeDefinitionBuilder GetTypeBuilder(RegistryKey key) =>
            Registry.GetTypeBuilder(key);

        internal ICastDefinitionBuilder GetCastBuilder(RegistryKey key) =>
            Registry.GetCastBuilder(key);

        internal IContextDescriptor GetContextDescriptor(RegistryKey key) =>
            Registry.GetContextDescriptor(key);

        internal RegistryKey ResolveKey<T>() where T : IRegistryEntry =>
            Registry.ResolveKey<T>();

        internal void InitializeDefaults()
        {
            #region Core
            Register<GraphType>();
            Register<GraphTypeAssignment>();
            Register<GradientType>();
            Register<GradientTypeAssignment>();
            Register<BaseTextureType>();
            Register<BaseTextureTypeAssignment>();
            Register<SamplerStateType>();
            Register<SamplerStateAssignment>();

            Register(new MultiplyNode(), new MultiplyNodeUI());
            Register(new SwizzleNode(), new SwizzleNodeUI());
            Register(new SampleGradientNode(), new StaticNodeUIDescriptorBuilder(SampleGradientNode.kUIDescriptor));
            #endregion

            // TODO: remove these, but keep until equivalents are working correctly.
            //Register<GradientNode>(); // TODO: https://jira.unity3d.com/browse/GSG-1290
            //Register(new SamplerStateExampleNode());
            //Register(new SimpleTextureNode());
            //Register(new SimpleSampleTexture2DNode());
            Register<ShaderGraphContext>();


            #region IStandardNode

            // TODO (Brett) I think that higher level application logic should
            // TODO (Brett) determine which nodes are loaded into the ShaderGraphRegistry.
            // TODO (Brett) I would like this code to be moved into the tool assembly.

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
                    var uidMethod = t.GetMethod(GET_UD_METHOD_NAME);
                    var versionMethod = t.GetMethod(GET_VERSION_METHOD_NAME);

                    // has NodeDescriptor
                    if (ndMethod != null)
                    {
                        var nd = (NodeDescriptor)ndMethod.Invoke(null, null);
                        if (!nd.Equals(default(NodeDescriptor)))
                        {
                            if (uidMethod != null)
                            {
                                var ui = (NodeUIDescriptor)uidMethod.Invoke(null, null);
                                Register(nd, ui);
                            }
                            else
                                Register(nd);
                        }
                    }
                    // has FunctionDescriptor
                    else if (fdMethod != null)
                    {
                        int version = 8;
                        if (versionMethod != null)
                        {
                            version = (int)versionMethod.Invoke(null, null);
                        }
                        var fd = (FunctionDescriptor)fdMethod.Invoke(null, null);
                        if (!fd.Equals(default(NodeDescriptor)))
                        {
                            if (uidMethod != null)
                            {
                                var ui = (NodeUIDescriptor)uidMethod.Invoke(null, null);
                                Register(fd, ui, version);
                            }
                            else
                                Register(fd, version);
                        }
                    }
                    // cannot create node
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
