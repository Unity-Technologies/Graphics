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

        // Regex pattern for updating function calls in body code.
        // Assumes
        //   - first character must be either a alpha or _
        //   - all other characters are alphanums and _ only
        //   - space is allowed between function name and (
        //   - closing ) is not required for match
        //   - will match in comments
        private static readonly string FUNCTION_CALL_PATTERN = @"([a-zA-Z_][a-zA-Z0-9_]*)\s*\(";

        public static readonly string SELECTED_FUNCTION_FIELD_NAME = "selected-function-name";

        private readonly NodeDescriptor m_nodeDescriptor;
        private readonly FunctionDescriptor? m_defaultFunction;
        private readonly Dictionary<string, FunctionDescriptor> m_nameToFunction;
        private readonly Dictionary<string, string> m_functionNameToShaderFunctionName;
        private readonly Dictionary<string, string> m_functionNameToModifiedBody;

        /// <summary>
        /// Sets up the uniformed version of the node from a a NodeDescriptor.
        /// 
        /// The constructed builder has no information that would be derived from
        /// the graph state (Eg. the selected function).
        /// </summary>
        /// <param name="nodeDescriptor"></param>
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
            Dictionary<string, string> functionNameToShaderFunctionName = new();
            Dictionary<string, string> functionNameToModifiedBody = new();

            foreach (FunctionDescriptor fd in nodeDescriptor.Functions)
            {
                // set the first function as the default function by default
                if (m_defaultFunction == null)
                    m_defaultFunction = fd;
                // if the FD is the specified main, set it as the default
                if (fd.Name.Equals(m_nodeDescriptor.MainFunction))
                    m_defaultFunction = fd;
                nameToFunction[fd.Name] = fd;
                functionNameToShaderFunctionName[fd.Name] = $"{m_nodeDescriptor.Name}_{fd.Name}";
            }

            // modify the body code for all functions
            foreach (FunctionDescriptor fd in nodeDescriptor.Functions)
            {
                string newBody = LocalizeFunctionCalls(functionNameToShaderFunctionName, fd);
                functionNameToModifiedBody[fd.Name] = newBody;
            }

            m_nameToFunction = nameToFunction;
            m_functionNameToShaderFunctionName = functionNameToShaderFunctionName;
            m_functionNameToModifiedBody = functionNameToModifiedBody;
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
            // If there is no default function return without changing the node.
            if (m_defaultFunction == null) return;

            // The NodeDescriptor may have multiple functions defined.
            // The currently selected FunctionDescriptor name is stored as
            // field in the node data.

            FunctionDescriptor selectedFunction = (FunctionDescriptor)m_defaultFunction;

            // on the concrete layer, the selected function is the default
            node.AddField(SELECTED_FUNCTION_FIELD_NAME, selectedFunction.Name, true);

            // Get the consolodated value for the selected function field
            // this must be defined because it was added, at least, to the
            // concrete layer, above.
            FieldHandler selectedFunctionField = node.GetField<string>(SELECTED_FUNCTION_FIELD_NAME);
            string selectedFunctionName = selectedFunctionField.GetData<string>();
            if (!m_nameToFunction.ContainsKey(selectedFunctionName))
            {
                Debug.LogWarning($"Cannot select function with name {selectedFunctionName}. No FunctionDescriptor with this name available.");
            }
            else
            {
                selectedFunction = m_nameToFunction[selectedFunctionName];
            }

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

            GraphTypeHelpers.ResolveDynamicPorts(node);
        }

        ShaderFunction INodeDefinitionBuilder.GetShaderFunction(
            NodeHandler node,
            ShaderContainer container,
            Registry registry,
            out INodeDefinitionBuilder.Dependencies deps)
        {
            // determine the dynamic fallback type
            var fallbackType = NodeBuilderUtils.FallbackTypeResolver(node);

            Dictionary<string, ShaderFunction> nameToShaderFunction = new();

            // make shader functions for each helper function
            foreach (FunctionDescriptor fd in m_nodeDescriptor.Functions)
            {
                if (fd.IsHelper)
                {
                    ShaderFunction shaderFunction = BuildShaderFunction(container, fd, fallbackType);
                    nameToShaderFunction[fd.Name] = shaderFunction;
                }
            }

            // make a shader function for the selected function
            FunctionDescriptor selectedFunction = (FunctionDescriptor)m_defaultFunction;
            FieldHandler selectedFunctionField = nodeHandler.GetField<string>(SELECTED_FUNCTION_FIELD_NAME);
            string selectedFunctionName = selectedFunctionField.GetData<string>();
            if (!m_nameToFunction.ContainsKey(selectedFunctionName))
            {
                Debug.LogWarning($"Cannot select function with name {selectedFunctionName}. No FunctionDescriptor with this name available.");
            }
            else
            {
                selectedFunction = m_nameToFunction[selectedFunctionName];
            }
            var selectedShaderFunction = BuildShaderFunction(container, selectedFunction, fallbackType);
            nameToShaderFunction[selectedFunction.Name] = selectedShaderFunction;

            // create the dependencies object that is returned
            deps = new();

            // put all non-main functions in deps
            deps.localFunctions = new List<ShaderFunction>();
            foreach (string functionName in nameToShaderFunction.Keys)
            {
                if (!functionName.Equals(selectedFunction.Name))
                    deps.localFunctions.Add(nameToShaderFunction[functionName]);
            }

            // put all includes in deps
            deps.includes = new List<ShaderFoundry.IncludeDescriptor>();
            foreach (FunctionDescriptor fd in m_nodeDescriptor.Functions)
            {
                AddIncludesFromFunctionDescriptor(container, fd, deps.includes);
            }

            return nameToShaderFunction[selectedFunction.Name];
        }

        private ShaderFunction BuildShaderFunction(
            ShaderContainer container,
            FunctionDescriptor functionDescriptor,
            ParametricTypeDescriptor fallbackType)
        {
            // Get a shader function builder
            var shaderFunctionBuilder = new ShaderFunction.Builder(
                container,
                m_functionNameToShaderFunctionName[functionDescriptor.Name]);

            // Set up the vars in the shader function.
            foreach (var param in functionDescriptor.Parameters)
            {
                var shaderType = ShaderTypeForParameter(container, param, fallbackType);

                if (param.Usage == Usage.Out)
                {
                    shaderFunctionBuilder.AddOutput(shaderType, param.Name);
                }
                else if (param.Usage == GraphType.Usage.In
                    || param.Usage == GraphType.Usage.Static
                    || param.Usage == GraphType.Usage.Local && !ParametricTypeUtils.IsParametric(param))
                {
                    shaderFunctionBuilder.AddInput(shaderType, param.Name);
                }
                else if (param.Usage == Usage.Local)
                {
                    var init = ParametricTypeUtils.ManagedToParametricToHLSL(param.DefaultValue);
                    shaderFunctionBuilder.AddVariableDeclarationStatement(shaderType, param.Name, init);
                }
                else
                {
                    throw new Exception($"No ShaderFunction parameter type for {param.Usage}");
                }
            }

            // Add the shader function body.
            shaderFunctionBuilder.AddLine(m_functionNameToModifiedBody[functionDescriptor.Name]);


            // Return the results of ShaderFoundry's build.
            return shaderFunctionBuilder.Build();
        }

        private static ShaderType GetShaderTypeForParametricTypeDescriptor(
            ShaderContainer container,
            ParametricTypeDescriptor resolvedType)
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
                        paramType.Precision == Precision.Any ? fallbackType.Precision : paramType.Precision,
                        paramType.Primitive == Primitive.Any ? fallbackType.Primitive : paramType.Primitive,
                        paramType.Length == Length.Any ? fallbackType.Length : paramType.Length,
                        paramType.Height == Height.Any ? fallbackType.Height : paramType.Height
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

        /// <summary>
        /// Returns the given FunctionDescriptor's body code with function calls
        /// that match keys in functionNameToRewrittenFunctionName replaced with
        /// the associated value in functionNameToRewrittenFunctionName.
        ///
        /// Example
        /// If functionNameToRewrittenFunctionName contains
        /// [MyFuncName, NewFuncName]
        /// functionDescriptor.Body will have all calls like MyFuncName(...)
        /// replaced with NewFuncName(...).
        /// </summary>
        private static string LocalizeFunctionCalls(
            Dictionary<string, string> functionNameToRewrittenFunctionName,
            FunctionDescriptor functionDescriptor)
        {
            // Find all places in the body code that match FUNCTION_CALL_PATTERN
            // and repalce if the value is a key in functionNameToRewrittenFunctionName.
            string matchReplaceBody = Regex.Replace(
                functionDescriptor.Body,
                FUNCTION_CALL_PATTERN,
                (Match match) =>
                {
                    // the value of the first capturing group is the function name
                    var functionName = match.Groups[1].Value;
                    // NOTE - To avoid checking keywords for match, we assume
                    // that language keywords (Eg. "if") are not in functionNameToRewrittenFunctionName.
                    return functionNameToRewrittenFunctionName.ContainsKey(functionName) ?
                        functionNameToRewrittenFunctionName[functionName] + "(" :
                        match.Value;
                }
            );
            return matchReplaceBody;
        }
    }
}
