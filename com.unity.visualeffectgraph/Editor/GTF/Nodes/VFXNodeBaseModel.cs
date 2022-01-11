using System;
using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEditor.GraphToolsFoundation.Overdrive.BasicModel;

namespace UnityEditor.VFX
{
    [Serializable]
    public class VFXNodeBaseModel : NodeModel
    {
    }

    public class VFXOperatorNode : VFXNodeBaseModel
    {
        private readonly VFXOperator m_NodeModel;

        internal VFXOperatorNode(VFXOperator nodeModel)
        {
            m_NodeModel = nodeModel;
        }

        protected override void OnDefineNode()
        {
            base.OnDefineNode();

            foreach (var inputSlot in m_NodeModel.inputSlots)
            {
                AddInputPort(inputSlot.name, PortType.Data, VFXStencil.Operator, options: PortModelOptions.NoEmbeddedConstant);
            }

            foreach (var outputSlot in m_NodeModel.outputSlots)
            {
                AddOutputPort(outputSlot.name, PortType.Data, VFXStencil.Operator, options: PortModelOptions.NoEmbeddedConstant);
            }
        }
    }
}
