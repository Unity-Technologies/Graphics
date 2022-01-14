using System;
using System.Linq;

using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEditor.GraphToolsFoundation.Overdrive.BasicModel;
using UnityEngine;

namespace UnityEditor.VFX
{
    [Serializable]
    public class VFXContextBaseModel : ContextNodeModel, IVFXNode
    {
        [SerializeField, HideInInspector]
        private VFXContext m_NodeModel;

        public override string Title
        {
            get => m_NodeModel != null ? ((VFXModel)m_NodeModel).libraryName : base.Title;
            set { }
        }

        void IVFXNode.SetModel(IVFXSlotContainer model)
        {
            if (model is VFXContext vfxContext)
            {
                m_NodeModel = vfxContext;
                DefineNode();
            }
            else
            {
                throw new ArgumentException("model must be a VFXContext");
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

            this.AddExecutionInputPort(null, GUID.Generate().ToString(), PortOrientation.Vertical);
            this.AddExecutionOutputPort(null, GUID.Generate().ToString(), PortOrientation.Vertical);
        }
    }
}
