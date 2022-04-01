using System;
using System.Collections.Generic;
using UnityEditor.ShaderFoundry;
using UnityEditor.ShaderGraph.GraphDelta;
using UnityEngine;

namespace UnityEditor.ShaderGraph.Defs
{
    /// <summary>
    /// NodeDescriptorNodeBuilder is a way to to make INodeDefinitionBuilder
    /// instances from NodeDescriptors.
    ///
    /// This is used to load the standard node defintions into the Registry.
    /// (See: StandardNodeDefinitions)
    /// </summary>
    internal class NodeDescriptorNodeBuilder : INodeDefinitionBuilder
    {
        private static readonly string SELECTED_FUNCTION_FIELD_NAME = "selected-function-name";

        private readonly NodeDescriptor m_nodeDescriptor;
        private readonly FunctionDescriptor? m_defaultFunction;
        private readonly Dictionary<string, FunctionDescriptor> m_nameToFunction;

        internal NodeDescriptorNodeBuilder(NodeDescriptor nodeDescriptor)
        {
            m_nodeDescriptor = nodeDescriptor; // copy

            // If there are no functions in nodeDescriptor:
            // - leave m_defaultFunction == null
            // - leave m_nameToFunction == null
            if (m_nodeDescriptor.Functions.Count < 1)
            {
                var msg = $"BuildNode called for NodeDescriptor with no defined functions: {m_nodeDescriptor.Name}";
                Debug.LogWarning(msg);
                return;
            }

            Dictionary<string, FunctionDescriptor> nameToFunction = new();
            foreach (FunctionDescriptor fd in nodeDescriptor.Functions)
            {
                // set the default function as the first FD
                if (m_defaultFunction == null)
                {
                    m_defaultFunction = fd;
                }
                nameToFunction[fd.Name] = fd;
            }
            m_nameToFunction = nameToFunction;
        }

        /// <summary>
        /// BuildNode is called when ever the node may be effected by a change in
        /// the graph (Eg. reconcretizing).
        /// A call to BuildNode is a request to set up ports, fields, and the
        /// body function to be appropriate, given the current configuration.
        /// </summary>
        /// <param name="node">
        /// The current node configuration (an empty node on the first call).
        /// </param>
        /// <param name="registry">
        /// The current registry state.
        /// </param>
        public void BuildNode(NodeHandler node, Registry registry)
        {
            /**
             * The NodeDescriptor may have multiple functions defined.
             * The currently selected FunctionDescriptor name is stored as
             * field in the node data.
             **/

            // if there is no default function return without changing the node.
            if (m_defaultFunction == null) return;

            // In building we add the selected-function field to the node.
            // Initially, set the default function.
            FunctionDescriptor defaultFunction = (FunctionDescriptor)m_defaultFunction;
            node.AddField(SELECTED_FUNCTION_FIELD_NAME, defaultFunction.Name);

            // Read from the user data layer to see if a different function is selected
            var selectedFunction = node.GetField<string>(SELECTED_FUNCTION_FIELD_NAME).GetData();

            // Build the current node topology based on the selected function.

            // TODO (Brett) THIS IS WRONG!
            // TODO (Brett) The fallback type should be determined with the currently selected FD.
            TypeDescriptor fallbackType = NodeBuilderUtils.FallbackTypeResolver(node);

            foreach (var param in m_nameToFunction[selectedFunction].Parameters)
            {
                //userData.TryGetPort(param.Name, out IPortReader portReader);
                NodeBuilderUtils.ParameterDescriptorToField(
                    param,
                    fallbackType,
                    node,
                    registry);
            }
        }

        ShaderFunction INodeDefinitionBuilder.GetShaderFunction(
            NodeHandler node,
            ShaderContainer container,
            Registry registry)
        {
            var selectedName = node.GetField<string>(SELECTED_FUNCTION_FIELD_NAME).GetData();
            FunctionDescriptor selectedFunction = m_nameToFunction[selectedName];

            // Get a builder from ShaderFoundry
            var shaderFunctionBuilder = new ShaderFunction.Builder(container, selectedFunction.Name);

            // Set up the vars in the shader function.
            foreach (var param in selectedFunction.Parameters)
            {
                var port = node.GetPort(param.Name);
                var field = port.GetTypeField();
                var shaderType = registry.GetShaderType(field, container);

                if (param.Usage == GraphType.Usage.In || param.Usage == GraphType.Usage.Static || param.Usage == GraphType.Usage.Local)
                {
                    shaderFunctionBuilder.AddInput(shaderType, param.Name);
                }
                else if (param.Usage == GraphType.Usage.Out)
                {
                    shaderFunctionBuilder.AddOutput(shaderType, param.Name);
                }
                else
                {
                    throw new Exception($"No ShaderFunction parameter type for {param.Usage}");
                }
            }

            // Add the shader function body.
            shaderFunctionBuilder.AddLine(selectedFunction.Body);

            // Return the results of ShaderFoundry's build.
            return shaderFunctionBuilder.Build();
        }

        public RegistryKey GetRegistryKey()
        {
            return new RegistryKey
            {
                Name = m_nodeDescriptor.Name,
                Version = m_nodeDescriptor.Version
            };
        }

        public RegistryFlags GetRegistryFlags()
        {
            return RegistryFlags.Func;
        }
    }
}
