using System;
using UnityEditor.GraphToolsFoundation.Overdrive.BasicModel;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Samples.Blackboard
{
    [Serializable]
    public class BBGraphModel : GraphModel
    {
        protected override bool IsCompatiblePort(IPortModel startPortModel, IPortModel compatiblePortModel)
        {
            return startPortModel.DataTypeHandle == compatiblePortModel.DataTypeHandle;
        }
    }
}
