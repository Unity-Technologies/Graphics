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

        public override string Title
        {
            get => m_NodeModel != null ? ((VFXModel)m_NodeModel).libraryName : base.Title;
            set { }
        }

        void IVFXNode.SetModel(IVFXSlotContainer model)
        {
            if (model is VFXBlock vfxBlock)
            {
                m_NodeModel = vfxBlock;
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
