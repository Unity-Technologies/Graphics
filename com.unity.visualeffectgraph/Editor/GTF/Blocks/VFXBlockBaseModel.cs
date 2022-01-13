using System;
using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEditor.GraphToolsFoundation.Overdrive.BasicModel;
using UnityEngine.GraphToolsFoundation.Overdrive;

namespace UnityEditor.VFX
{
    [Serializable]
    public class VFXBlockBaseModel : BlockNodeModel
    {
        public VFXBlockBaseModel()
        {
            m_Capabilities.Remove(GraphToolsFoundation.Overdrive.Capabilities.NeedsContainer);
        }
    }

    public class VFXBlockNode : VFXBlockBaseModel
    {
        private VFXBlock m_NodeModel;

        internal void SetBlock(VFXBlock model)
        {
            m_NodeModel = model;
            DefineNode();
        }

        protected override void OnDefineNode()
        {
            if (this.m_NodeModel == null)
            {
                return;
            }

            foreach (var inputSlot in m_NodeModel.inputSlots)
            {
                this.AddDataInputPort(inputSlot.name, inputSlot.property.type.GenerateTypeHandle());
            }

            foreach (var outputSlot in m_NodeModel.outputSlots)
            {
                this.AddDataOutputPort(outputSlot.name, outputSlot.property.type.GenerateTypeHandle(), options: PortModelOptions.NoEmbeddedConstant);
            }
        }
    }
}
