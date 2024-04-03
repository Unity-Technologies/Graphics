using System;
using System.Collections.Generic;
using UnityEditor.ShaderGraph.Internal;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    class FunctionSource
    {
        public string code;
        public HashSet<AbstractMaterialNode> nodes;
        public bool isGeneric;
        public int graphPrecisionFlags;     // Flags<GraphPrecision>
        public int concretePrecisionFlags;  // Flags<ConcretePrecision>
    }

    class FunctionRegistry
    {
        Dictionary<string, FunctionSource> m_Sources = new Dictionary<string, FunctionSource>();
        bool m_Validate = false;
        ShaderStringBuilder m_Builder;
        IncludeCollection m_Includes;

        public FunctionRegistry(ShaderStringBuilder builder, IncludeCollection includes, bool validate = false)
        {
            m_Builder = builder;
            m_Includes = includes;
            m_Validate = validate;
        }

        internal ShaderStringBuilder builder => m_Builder;

        public Dictionary<string, FunctionSource> sources => m_Sources;

        public void RequiresIncludes(IncludeCollection includes)
        {
            m_Includes.Add(includes);
        }

        public void RequiresIncludePath(string includePath, bool shouldIncludeWithPragmas = false)
        {
            m_Includes.Add(includePath, IncludeLocation.Graph, shouldIncludeWithPragmas);
        }

        // this list is somewhat redundant, but it preserves function declaration ordering
        // (i.e. when nodes add multiple functions, they require being defined in a certain order)
        public List<string> names { get; } = new List<string>();

        public void ProvideFunction(string name, GraphPrecision graphPrecision, ConcretePrecision concretePrecision, Action<ShaderStringBuilder> generator)
        {
            // appends code, construct the standalone code string
            var originalIndex = builder.length;
            builder.AppendNewLine();

            var startIndex = builder.length;
            generator(builder);
            var length = builder.length - startIndex;
            var code = builder.ToString(startIndex, length);

            // validate some assumptions around generics
            bool isGenericName = name.Contains("$");
            bool isGenericFunc = code.Contains("$");
            bool isGeneric = isGenericName || isGenericFunc;
            bool containsFunctionName = code.Contains(name);

            var curNode = builder.currentNode;
            if (isGenericName != isGenericFunc)
                curNode.owner.AddValidationError(curNode.objectId, $"Function {name} provided by node {curNode.name} contains $precision tokens in the name or the code, but not both. This is very likely an error.");

            if (!containsFunctionName)
                curNode.owner.AddValidationError(curNode.objectId, $"Function {name} provided by node {curNode.name} does not contain the name of the function.  This is very likely an error.");

            int graphPrecisionFlag = (1 << (int)graphPrecision);
            int concretePrecisionFlag = (1 << (int)concretePrecision);

            FunctionSource existingSource;
            if (m_Sources.TryGetValue(name, out existingSource))
            {
                // function already provided
                existingSource.nodes.Add(builder.currentNode);

                // let's check if the requested precision variant has already been provided (or if it's not generic there are no variants)
                bool concretePrecisionExists = ((existingSource.concretePrecisionFlags & concretePrecisionFlag) != 0) || !isGeneric;

                // if this precision was already added -- remove the duplicate code from the builder
                if (concretePrecisionExists)
                    builder.length -= (builder.length - originalIndex);

                // save the flags
                existingSource.graphPrecisionFlags = existingSource.graphPrecisionFlags | graphPrecisionFlag;
                existingSource.concretePrecisionFlags = existingSource.concretePrecisionFlags | concretePrecisionFlag;

                // if validate, we double check that the two function declarations are the same
                if (m_Validate)
                {
                    if (code != existingSource.code)
                    {
                        var errorMessage = string.Format("Function `{0}` has conflicting implementations:{1}{1}{2}{1}{1}{3}", name, Environment.NewLine, code, existingSource.code);
                        foreach (var n in existingSource.nodes)
                            n.owner.AddValidationError(n.objectId, errorMessage);
                    }
                }
            }
            else
            {
                var newSource = new FunctionSource
                {
                    code = code,
                    isGeneric = isGeneric,
                    graphPrecisionFlags = graphPrecisionFlag,
                    concretePrecisionFlags = concretePrecisionFlag,
                    nodes = new HashSet<AbstractMaterialNode> { builder.currentNode }
                };

                m_Sources.Add(name, newSource);
                names.Add(name);
            }

            // fully concretize any generic code by replacing any precision tokens by the node's concrete precision
            if (isGeneric && (builder.length > originalIndex))
            {
                int start = originalIndex;
                int count = builder.length - start;
                builder.Replace(PrecisionUtil.Token, concretePrecision.ToShaderString(), start, count);
            }
        }

        public void ProvideFunction(string name, Action<ShaderStringBuilder> generator)
        {
            ProvideFunction(name, builder.currentNode.graphPrecision, builder.currentNode.concretePrecision, generator);
        }
    }
}
