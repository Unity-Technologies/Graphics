using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEditor.ShaderFoundry;
using UnityEditor.ShaderGraph.GraphDelta;
using UnityEngine;
using static UnityEditor.ShaderGraph.GraphDelta.GraphType;

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

            // If there are no functions in nodeDescriptor
            //   leave m_defaultFunction == null
            //   leave m_nameToFunction == null
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

        private static ShaderType GetShaderTypeForParametricTypeDescriptor(ShaderContainer container, ParametricTypeDescriptor resolvedType)
        {
            var height = resolvedType.Height;
            var length = resolvedType.Length;
            var primitive = resolvedType.Primitive;
            var precision = resolvedType.Precision;

            int l = Mathf.Clamp((int)length, 1, 4);
            int h = Mathf.Clamp((int)height, 1, 4);

            string name = "float";

            switch (primitive)
            {
                case Primitive.Bool: name = "bool"; break;
                case Primitive.Int: name = "int"; break;
                case Primitive.Float:
                    switch (precision)
                    {
                        case Precision.Fixed: name = "float"; break;
                        case Precision.Half: name = "half"; break;
                    }
                    break;
            }

            var shaderType = ShaderType.Scalar(container, name);
            if (h != 1 && l != 1)
            {
                shaderType = ShaderType.Matrix(container, shaderType, l, h);
            }
            else if (h != 1 || l != 1)
            {
                shaderType = ShaderType.Vector(container, shaderType, Mathf.Max(l, h));
            }
            return shaderType;
        }

        private static ShaderType ShaderTypeForParameter(
            ShaderContainer container,
            ParameterDescriptor parameter,
            ParametricTypeDescriptor fallbackType)
        {
            switch (parameter.TypeDescriptor)
            {
                case ParametricTypeDescriptor paramType:
                    ParametricTypeDescriptor resolvedType = new(
                        paramType.Precision == GraphType.Precision.Any ? fallbackType.Precision : paramType.Precision,
                        paramType.Primitive == GraphType.Primitive.Any ? fallbackType.Primitive : paramType.Primitive,
                        paramType.Length == GraphType.Length.Any ? fallbackType.Length : paramType.Length,
                        paramType.Height == GraphType.Height.Any ? fallbackType.Height : paramType.Height
                    );
                    return GetShaderTypeForParametricTypeDescriptor(container, resolvedType);
                case SamplerStateTypeDescriptor:
                    return container._UnitySamplerState;
                case TextureTypeDescriptor textureType:
                    return textureType.TextureType switch
                    {
                        BaseTextureType.TextureType.Texture3D => container._UnityTexture3D,
                        BaseTextureType.TextureType.Texture2DArray => container._UnityTexture2DArray,
                        BaseTextureType.TextureType.CubeMap => container._UnityTextureCube,
                        _ => container._UnityTexture2D,
                    };
                case GradientTypeDescriptor:
                    var gradientBuilder = new ShaderType.StructBuilder(container, "Gradient");
                    gradientBuilder.DeclaredExternally();
                    return gradientBuilder.Build();
                default:
                    throw new Exception($"Parameter ({parameter.Name}) does not have an equivalent shader type.");
            }
        }

        private ShaderFunction BuildShaderFunction(
            ShaderContainer container,
            FunctionDescriptor functionDescriptor,
            ParametricTypeDescriptor fallbackType)
        {
            // Get a shader function builder
            var shaderFunctionBuilder = new ShaderFunction.Builder(container, functionDescriptor.Name);

            // Set up the vars in the shader function.
            foreach (var param in functionDescriptor.Parameters)
            {
                var shaderType = ShaderTypeForParameter(container, param, fallbackType);
                if (param.Usage == Usage.In ||
                    param.Usage == Usage.Static ||
                    param.Usage == Usage.Local)
                {
                    shaderFunctionBuilder.AddInput(shaderType, param.Name);
                }
                else if (param.Usage == Usage.Out)
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

            // find the selected function
            if (node.HasMetadata(SELECTED_FUNCTION_FIELD_NAME))
            {
                string functionName = node.GetMetadata<string>(SELECTED_FUNCTION_FIELD_NAME);
                if (m_nameToFunction.ContainsKey(functionName))
                {
                    selectedFunction = m_nameToFunction[functionName];
                }
            }

            // determine the dynamic fallback type
            var fallbackType = NodeBuilderUtils.FallbackTypeResolver(node);

            // make shader functions for each internal function descriptor
            Dictionary<string, ShaderFunction> nameToShaderFunction = new();
            foreach (FunctionDescriptor fd in m_nodeDescriptor.Functions)
            {
                ShaderFunction shaderFunction = BuildShaderFunction(container, fd, fallbackType);
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
