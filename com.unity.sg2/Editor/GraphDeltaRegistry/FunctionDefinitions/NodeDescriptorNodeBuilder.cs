using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
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
        public static readonly string SELECTED_FUNCTION_FIELD_NAME = "selected-function-name";

        private readonly NodeDescriptor m_nodeDescriptor;
        private readonly FunctionDescriptor? m_defaultFunction;
        private readonly Dictionary<string, FunctionDescriptor> m_nameToFunction;
        private readonly Dictionary<string, string> m_functionNameToShaderFunctionName;
        private readonly Dictionary<string, string> m_functionNameToModifiedBody;

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
            Dictionary<string, string> functionNameToCodeName = new();

            foreach (FunctionDescriptor fd in nodeDescriptor.Functions)
            {
                // set the first function as the default function by default
                if (m_defaultFunction == null)
                {
                    m_defaultFunction = fd;
                }
                // if the FD is the specified main, set it as the default
                if (fd.Name.Equals(m_nodeDescriptor.Main))
                {
                    m_defaultFunction = fd;
                }
                nameToFunction[fd.Name] = fd;
                functionNameToCodeName[fd.Name] = $"{m_nodeDescriptor.Name}_{fd.Name}";
            }

            // modify the body code for all functions
            Dictionary<string, string>functionNameToModifiedBody = new();
            foreach (FunctionDescriptor fd in nodeDescriptor.Functions)
            {
                string newBody = UpdateBodyCode(functionNameToCodeName, fd);
            }

            m_nameToFunction = nameToFunction;
            m_functionNameToShaderFunctionName = functionNameToCodeName;
            m_functionNameToModifiedBody = functionNameToModifiedBody;
        }

        private string UpdateBodyCode(
            Dictionary<string, string> functionNameToCodeName,
            FunctionDescriptor functionDescriptor)
        {
            var oldName = functionDescriptor.Name;
            var newBody = Regex.Replace(oldName, functionNameToCodeName[oldName], functionDescriptor.Body);
            return newBody;
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

            FunctionDescriptor selectedFunction = (FunctionDescriptor)m_defaultFunction;

            // check node metadata for a selected function name
            if (node.HasMetadata(SELECTED_FUNCTION_FIELD_NAME))
            {
                string functionName = node.GetMetadata<string>(SELECTED_FUNCTION_FIELD_NAME);
                if (m_nameToFunction.ContainsKey(functionName))
                {
                    selectedFunction = m_nameToFunction[functionName];
                }
            }

            // TODO (Brett) THIS MIGHT BE WRONG!
            // TODO (Brett) Should the fallback type should be determined with the currently selected FD?
            // determine a fallback type
            ParametricTypeDescriptor fallbackType = NodeBuilderUtils.FallbackTypeResolver(node);

            // setup the node topology
            foreach (var param in selectedFunction.Parameters)
            {
                NodeBuilderUtils.ParameterDescriptorToField(
                    param,
                    fallbackType,
                    node,
                    registry);
            }
        }

        private ShaderFunction BuildShaderFunction(
            ShaderContainer container,
            FunctionDescriptor functionDescriptor)
        {
            // Get a shader function builder
            var shaderFunctionBuilder = new ShaderFunction.Builder(container, functionDescriptor.Name);

            // Set up the vars in the shader function.
            foreach (var param in functionDescriptor.Parameters)
            {
                //var port = node.GetPort(param.Name);
                //var field = port.GetTypeField();
                //var shaderType = registry.GetShaderType(field, container);

                if (param.Usage == GraphType.Usage.In ||
                    param.Usage == GraphType.Usage.Static ||
                    param.Usage == GraphType.Usage.Local)
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
            shaderFunctionBuilder.AddLine(functionDescriptor.Body);

            // Return the results of ShaderFoundry's build.
            return shaderFunctionBuilder.Build();
        }

        private void AddIncludesFromFunctionDescriptor(
            ShaderContainer shaderContainer,
            FunctionDescriptor fd,
            List<ShaderFoundry.IncludeDescriptor> includes)
        {
            foreach (string include in fd.Includes)
            {
                var builder = new ShaderFoundry.IncludeDescriptor.Builder(shaderContainer, include);
                var includeDescriptor = builder.Build();
                includes.Add(includeDescriptor);
            }
        }

        ShaderFunction INodeDefinitionBuilder.GetShaderFunction(
            NodeHandler node,
            ShaderContainer container,
            Registry registry,
            out INodeDefinitionBuilder.Dependencies deps)
        {
            // create the dependencies object that is returned
            deps = new();

            FunctionDescriptor selectedFunction = (FunctionDescriptor)m_defaultFunction;

            // check node metadata for a selected function name
            if (node.HasMetadata(SELECTED_FUNCTION_FIELD_NAME))
            {
                string functionName = node.GetMetadata<string>(SELECTED_FUNCTION_FIELD_NAME);
                if (m_nameToFunction.ContainsKey(functionName))
                {
                    selectedFunction = m_nameToFunction[functionName];
                }
            }

            Dictionary<string, ShaderFunction> nameToShaderFunction = new();
            foreach (FunctionDescriptor fd in m_nodeDescriptor.Functions)
            {
                ShaderFunction shaderFunction = BuildShaderFunction(container, fd);
                nameToShaderFunction[fd.Name] = shaderFunction;
            }

            return nameToShaderFunction[selectedFunction.Name];
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
