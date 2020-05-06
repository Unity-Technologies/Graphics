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
                    bool isValid = true;
                    bool disalowedByAll = true;
                    foreach (var target in node.owner.activeTargets)
                    {
                        if (!target.allowedNodes.Contains(t))
                        {
                            isValid = false;
                            node.isValid = false;
                            node.owner.AddValidationError(node.objectId, $"{node.name} Node is not allowed by {target.displayName} implementation", Rendering.ShaderCompilerMessageSeverity.Warning);
                        }
                        else
                        {
                            disalowedByAll = false;
                        }
                    }

                    if (disalowedByAll)
                    {
                        node.SetOverrideActiveState(AbstractMaterialNode.ActiveState.ExplicitInactive);
                        node.owner.AddValidationError(node.objectId, $"{node.name} Node is not allowed by any active targets, and will not be used in generation", Rendering.ShaderCompilerMessageSeverity.Warning);
                    }
                    else
                    {
                        node.SetOverrideActiveState(AbstractMaterialNode.ActiveState.Implicit);
                    }

                    if (isValid)
                    {
                        node.isValid = true;
                    }
                }
            }

            public static void ValidateGraph(GraphData graph)
            {
                GraphDataUtils.ApplyActionLeafFirst(graph, ValidateNode);
            }
        }
    }
}
