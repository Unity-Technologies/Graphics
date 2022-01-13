using System;
using UnityEngine;

using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEditor.GraphToolsFoundation.Overdrive.BasicModel;
using UnityEngine.GraphToolsFoundation.Overdrive;

namespace UnityEditor.VFX
{
    [Serializable]
    public class VFXContextBaseModel : ContextNodeModel
    {
    }

    public class VFXContextNode : VFXContextBaseModel
    {
        private VFXContext m_NodeModel;

        internal void SetContext(VFXContext model)
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
                this.AddDataInputPort(inputSlot.name, inputSlot.property.type.GenerateTypeHandle(), orientation: PortOrientation.Horizontal);
            }

            foreach (var outputSlot in m_NodeModel.outputSlots)
            {
                this.AddDataOutputPort(outputSlot.name, outputSlot.property.type.GenerateTypeHandle(), orientation: PortOrientation.Horizontal);
            }

            this.AddExecutionInputPort(string.Empty, orientation: PortOrientation.Vertical);
            this.AddExecutionOutputPort(string.Empty, orientation: PortOrientation.Vertical);
        }
    }
}
