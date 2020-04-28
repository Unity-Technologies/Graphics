using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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
                foreach(var target in node.owner.activeTargets)
                {
                    if(!target.allowedNodes.Contains(t))
                    {
                        node.isValid = false;
                        node.owner.AddValidationError(node.objectId, $"{node.name} Node is not allowed by {target.displayName} implementation", Rendering.ShaderCompilerMessageSeverity.Warning);
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
