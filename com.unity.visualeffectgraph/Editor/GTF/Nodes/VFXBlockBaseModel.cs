using System;
using System.Linq;

using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEditor.GraphToolsFoundation.Overdrive.BasicModel;
using UnityEngine;

namespace UnityEditor.VFX
{
    [Serializable]
    public class VFXBlockBaseModel : BlockNodeModel, IVFXNode
    {
        [SerializeField, HideInInspector]
        private VFXBlock m_NodeModel;

        public VFXBlockBaseModel()
        {
            m_Capabilities.Remove(GraphToolsFoundation.Overdrive.Capabilities.NeedsContainer);
        }

        void IVFXNode.SetModel(IVFXSlotContainer model)
        {
            if (model is VFXBlock vfxBlock)
            {
                m_NodeModel = vfxBlock;
                //Title = m_NodeModel.libraryName;
                DefineNode();
            }
            else
            {
                throw new ArgumentException("model must be a VFXBlock");
            }
        }

        public override void OnAfterDeserialize()
        {
            base.OnAfterDeserialize();
            Debug.Log("deserialize");
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
