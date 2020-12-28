using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor.Graphing;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    sealed partial class GraphData : ISerializationCallbackReceiver
    {
        public static class GraphValidation
        {
            public static void ValidateNode(AbstractMaterialNode node)
            {
                Type t = node.GetType();
                node.ValidateNode();
                if (!(node is BlockNode))
                {
                    bool disallowedByAnyTargets = false;
                    bool disallowedByAllTargets = true;
                    IEnumerable<Target> targets = node.owner.activeTargets;
                    if(node.owner.isSubGraph)
                    {
                        targets = node.owner.allPotentialTargets;
                    }
                    foreach (var target in targets)
                    {
                        //if at least one target doesn't allow a node, it is considered invalid
                        if (!target.IsNodeAllowedByTarget(t))
                        {
                            disallowedByAnyTargets = true;
                            node.isValid = false;
                            node.owner.AddValidationError(node.objectId, $"{node.name} Node is not allowed by {target.displayName} implementation", Rendering.ShaderCompilerMessageSeverity.Warning);
                            node.owner.m_UnsupportedTargets.Add(target);
                        }
                        //at least one target does allow node, not going to be explicitly set inactive
                        else
                        {
                            disallowedByAllTargets = false;
                        }
                    }
                    if (!disallowedByAnyTargets)
                    {
                        node.isValid = true;
                    }

                    //Set ActiveState based on if all targets disallow this node
                    if (disallowedByAllTargets)
                    {
                        node.SetOverrideActiveState(AbstractMaterialNode.ActiveState.ExplicitInactive);
                        node.owner.AddValidationError(node.objectId, $"{node.name} Node is not allowed by any active targets, and will not be used in generation", Rendering.ShaderCompilerMessageSeverity.Warning);
                    }
                    else
                    {
                        node.SetOverrideActiveState(AbstractMaterialNode.ActiveState.Implicit);
                    }
                }
            }

            public static void ValidateGraph(GraphData graph)
            {
                graph.m_UnsupportedTargets.Clear();
                GraphDataUtils.ApplyActionLeafFirst(graph, ValidateNode);
            }
        }
    }
}
