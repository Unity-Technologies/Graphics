using System;
using System.Linq;
using UnityEditor.GraphToolsFoundation.Overdrive.BasicModel;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Samples.MathBook
{
    public static class PortModelExtension
    {
        public static Value GetValue(this IPortModel self)
        {
            if (self == null)
                return 0;
            var node = self.GetConnectedEdges().FirstOrDefault()?.FromPort.NodeModel;

            switch (node)
            {
                case MathSubgraphNode subgraphNode:
                    return subgraphNode.Evaluate(self.GetConnectedEdges().FirstOrDefault()?.FromPort);
                case MathNode mathNode:
                    if (!mathNode.CheckInputs(out var errorMessage))
                        Debug.LogError(errorMessage);
                    return mathNode.Evaluate();
                case IVariableNodeModel varNode:
                    return varNode.VariableDeclarationModel.InitializationModel.GetValue();
                case ConstantNodeModel constNode:
                    return constNode.Value.GetValue();
                case IEdgePortalExitModel portalModel:
                    var oppositePortal = portalModel.GraphModel.FindReferencesInGraph<IEdgePortalEntryModel>(portalModel.DeclarationModel).FirstOrDefault();
                    if (oppositePortal != null)
                    {
                        return oppositePortal.InputPort.GetValue();
                    }
                    return 0;
                default:
                    return self.EmbeddedValue.GetValue();
            }
        }

        static Value GetValue(this IConstant constant)
        {
            switch (constant)
            {
                case BooleanConstant booleanConstant:
                    return booleanConstant.Value;
                case FloatConstant floatConstant:
                    return floatConstant.Value;
                case IntConstant intConstant:
                    return intConstant.Value;
                case Vector2Constant vector2Constant:
                    return vector2Constant.Value;
                case Vector3Constant vector3Constant:
                    return vector3Constant.Value;
                default:
                    throw new ArgumentOutOfRangeException(nameof(constant));
            }
        }
    }
}
