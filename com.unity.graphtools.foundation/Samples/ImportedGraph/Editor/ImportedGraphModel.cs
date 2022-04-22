using System;
using System.Linq;
using UnityEditor.GraphToolsFoundation.Overdrive.BasicModel;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Samples.ImportedGraph
{
    [Serializable]
    public class ImportedGraphModel : GraphModel
    {
        protected override bool IsCompatiblePort(IPortModel startPortModel, IPortModel compatiblePortModel)
        {
            return base.IsCompatiblePort(startPortModel, compatiblePortModel) && startPortModel.DataTypeHandle.Equals(compatiblePortModel.DataTypeHandle);
        }

        public override bool CanBeSubgraph() => VariableDeclarations.Any(variable => variable.IsInputOrOutput());
    }
}
