using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Graphing;
using UnityEditor.Graphing.Util;
using UnityEditor.Rendering;
using UnityEditor.ShaderGraph.Drawing;
using UnityEditor.ShaderGraph.Internal;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UIElements;

namespace UnityEditor.ShaderGraph
{
    // TODO: rename this file to VirtualTexturingFeedbackUtils
    static class VirtualTexturingFeedbackUtils
    {
        // TODO: could get rid of this if we could run a codegen prepass (with proper keyword #ifdef)
        public static void GenerateVirtualTextureFeedback(
            List<AbstractMaterialNode> downstreamNodesIncludingRoot,
            List<int>[] keywordPermutationsPerNode,
            ShaderStringBuilder surfaceDescriptionFunction,
            KeywordCollector shaderKeywords)
        {
            // A note on how we handle vt feedback in combination with keywords:
            // We essentially generate a fully separate feedback path for each permutation of keywords
            // so per permutation we gather variables contribution to feedback and we generate
            // feedback gathering for each permutation individually.

            var feedbackVariablesPerPermutation = PooledList<PooledList<string>>.Get();
            try
            {
                if (shaderKeywords.permutations.Count >= 1)
                {
                    for (int i = 0; i < shaderKeywords.permutations.Count; i++)
                    {
                        feedbackVariablesPerPermutation.Add(PooledList<string>.Get());
                    }
                }
                else
                {
                    // Create a dummy single permutation
                    feedbackVariablesPerPermutation.Add(PooledList<string>.Get());
                }

                int index = 0; //for keywordPermutationsPerNode
                foreach (var node in downstreamNodesIncludingRoot)
                {
                    if (node is SampleVirtualTextureNode vtNode)
                    {
                        if (vtNode.noFeedback) continue;
                        if (keywordPermutationsPerNode[index] == null)
                        {
                            Debug.Assert(shaderKeywords.permutations.Count == 0, $"Shader has {shaderKeywords.permutations.Count} permutations but keywordPermutationsPerNode of some nodes are null.");
                            feedbackVariablesPerPermutation[0].Add(vtNode.GetFeedbackVariableName());
                        }
                        else
                        {
                            foreach (int perm in keywordPermutationsPerNode[index])
                            {
                                feedbackVariablesPerPermutation[perm].Add(vtNode.GetFeedbackVariableName());
                            }
                        }
                    }

                    if (node is SubGraphNode sgNode)
                    {
                        if (sgNode.asset == null) continue;
                        if (keywordPermutationsPerNode[index] == null)
                        {
                            Debug.Assert(shaderKeywords.permutations.Count == 0, $"Shader has {shaderKeywords.permutations.Count} permutations but keywordPermutationsPerNode of some nodes are null.");
                            foreach (var feedbackSlot in sgNode.asset.vtFeedbackVariables)
                            {
                                feedbackVariablesPerPermutation[0].Add(node.GetVariableNameForNode() + "_" + feedbackSlot);
                            }
                        }
                        else
                        {
                            foreach (var feedbackSlot in sgNode.asset.vtFeedbackVariables)
                            {
                                foreach (int perm in keywordPermutationsPerNode[index])
                                {
                                    feedbackVariablesPerPermutation[perm].Add(node.GetVariableNameForNode() + "_" + feedbackSlot);
                                }
                            }
                        }
                    }

                    index++;
                }

                index = 0;
                foreach (var feedbackVariables in feedbackVariablesPerPermutation)
                {
                    // If it's a dummy single always-on permutation don't put an ifdef around the code
                    if (shaderKeywords.permutations.Count >= 1)
                    {
                        surfaceDescriptionFunction.AppendLine(KeywordUtil.GetKeywordPermutationConditional(index));
                    }

                    using (surfaceDescriptionFunction.BlockScope())
                    {
                        if (feedbackVariables.Count == 0)
                        {
                            string feedBackCode = "surface.VTPackedFeedback = float4(1.0f,1.0f,1.0f,.0f);";
                            surfaceDescriptionFunction.AppendLine(feedBackCode);
                        }
                        else if (feedbackVariables.Count == 1)
                        {
                            string feedBackCode = "surface.VTPackedFeedback = GetPackedVTFeedback(" + feedbackVariables[0] + ");";
                            surfaceDescriptionFunction.AppendLine(feedBackCode);
                        }
                        else if (feedbackVariables.Count > 1)
                        {
                            surfaceDescriptionFunction.AppendLine("float4 VTFeedback_array[" + feedbackVariables.Count + "];");

                            int arrayIndex = 0;
                            foreach (var variable in feedbackVariables)
                            {
                                surfaceDescriptionFunction.AppendLine("VTFeedback_array[" + arrayIndex + "] = " + variable + ";");
                                arrayIndex++;
                            }

                            surfaceDescriptionFunction.AppendLine("uint pixelColumn = (IN.ScreenPosition.x / IN.ScreenPosition.w) * _ScreenParams.x;");
                            surfaceDescriptionFunction.AppendLine(
                                "surface.VTPackedFeedback = GetPackedVTFeedback(VTFeedback_array[(pixelColumn + _FrameCount) % (uint)" + feedbackVariables.Count + "]);");
                        }
                    }

                    if (shaderKeywords.permutations.Count >= 1)
                    {
                        surfaceDescriptionFunction.AppendLine("#endif");
                    }

                    index++;
                }
            }
            finally
            {
                foreach (var list in feedbackVariablesPerPermutation)
                {
                    list.Dispose();
                }
                feedbackVariablesPerPermutation.Dispose();
            }
        }

        // Automatically add a  streaming feedback node and correctly connect it to stack samples are connected to it and it is connected to the master node output
        public static List<string> GetFeedbackVariables(SubGraphOutputNode masterNode)
        {
            // TODO: make use a generic interface instead of hard-coding the node types that we need to look at here
            var VTNodes = GraphUtil.FindDownStreamNodesOfType<SampleVirtualTextureNode>(masterNode);
            var subGraphNodes = GraphUtil.FindDownStreamNodesOfType<SubGraphNode>(masterNode);

            List<string> result = new List<string>();

            // Early out if there are no nodes we care about in the graph
            if (subGraphNodes.Count <= 0 && VTNodes.Count <= 0)
            {
                return result;
            }

            // Add inputs to feedback node
            foreach (var node in VTNodes)
            {
                if (node.noFeedback) continue;
                result.Add(node.GetFeedbackVariableName());
            }

            foreach (var node in subGraphNodes)
            {
                if (node.asset == null) continue;
                // TODO: subgraph.GetFeedbackVariableNames(...)
                foreach (var feedbackSlot in node.asset.vtFeedbackVariables)
                {
                    result.Add(node.GetVariableNameForNode() + "_" + feedbackSlot);
                }
            }

            return result;
        }
    }
}
