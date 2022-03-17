using System;
using UnityEditor.GraphToolsFoundation.Overdrive.BasicModel;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Samples.Blackboard
{
    public class BBGraphModel : GraphModel
    {
        protected override bool IsCompatiblePort(IPortModel startPortModel, IPortModel compatiblePortModel)
        {
            return startPortModel.DataTypeHandle == compatiblePortModel.DataTypeHandle;
        }
    }
}
