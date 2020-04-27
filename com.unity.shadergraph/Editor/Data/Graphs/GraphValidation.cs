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
                foreach(var targetImpl in node.owner.m_ValidImplementations)
                {
                    if(!targetImpl.allowedNodes.Contains(t))
                    {
                        node.isValid = false;
                        node.owner.AddValidationError(node.guid, $"{node.name} Node is not allowed by {targetImpl.displayName + targetImpl.targetType.Name} implementation", Rendering.ShaderCompilerMessageSeverity.Warning);
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
