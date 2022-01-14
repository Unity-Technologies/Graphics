using System;
using System.Linq;

using UnityEngine;
using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEditor.GraphToolsFoundation.Overdrive.BasicModel;

namespace UnityEditor.VFX
{
    interface IVFXNode
    {
        void SetModel(IVFXSlotContainer model);
    }

    [Serializable]
    public class VFXNodeBaseModel : NodeModel, IVFXNode
    {
        [SerializeField, HideInInspector]
        private VFXOperator m_NodeModel;

        void IVFXNode.SetModel(IVFXSlotContainer model)
        {
            if (model is VFXOperator vfxOperator)
            {
                m_NodeModel = vfxOperator;
                //Title = m_NodeModel.libraryName;
                DefineNode();
            }
            else
            {
                throw new ArgumentException("model must be a VFXBlock");
            }
        }

        protected override void OnDefineNode()
        {
            if (m_NodeModel == null)
            {
                return;
            }

            m_NodeModel.inputSlots.ToList().ForEach(x => this.AddPort(PortDirection.Input, x.name, x.property.type));
            m_NodeModel.outputSlots.ToList().ForEach(x => this.AddPort(PortDirection.Output, x.name, x.property.type));
        }
    }
}
