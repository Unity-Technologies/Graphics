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
            static Dictionary<Type, FieldInfo> s_ActiveSubTarget = new();
            static readonly PropertyInfo s_JsonData = typeof(Serialization.JsonData<SubTarget>).GetProperty("value");

            static SubTarget GetActiveSubTarget(Target target)
            {
                // All targets have an active subtarget but it's not accessible from ShaderGraph
                // so we use reflection to access it

                var type = target.GetType();
                if (!s_ActiveSubTarget.TryGetValue(type, out var activeSubTarget))
                {
                    activeSubTarget = type.GetField("m_ActiveSubTarget", BindingFlags.Instance | BindingFlags.NonPublic);
                    s_ActiveSubTarget.Add(type, activeSubTarget);
                }
                if (activeSubTarget != null)
                {
                    var jsonData = activeSubTarget.GetValue(target);
                    if (jsonData != null)
                    {
                        return s_JsonData.GetValue(jsonData) as SubTarget;
                    }
                }
                return null;
            }

            public static void ValidateNode(AbstractMaterialNode node)
            {
                Type t = node.GetType();
                node.ValidateNode();
                if (!(node is BlockNode))
                {
                    bool disallowedByAnyTargets = false;
                    bool disallowedByAllTargets = true;
                    bool disallowedByAnySubTarget = false;
                    IEnumerable<Target> targets = node.owner.activeTargets;
                    if (node.owner.isSubGraph)
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

                        var subtarget = GetActiveSubTarget(target);
                        if (subtarget != null && !subtarget.IsNodeAllowedBySubTarget(t))
                        {
                            disallowedByAnySubTarget = true;
                            node.isValid = false;
                            node.owner.AddValidationError(node.objectId, $"{node.name} Node is not allowed by {subtarget.displayName} implementation", Rendering.ShaderCompilerMessageSeverity.Error);
                        }
                    }
                    if (!disallowedByAnyTargets && !disallowedByAnySubTarget)
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
