using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.GraphToolsFoundation.Overdrive.BasicModel;
using UnityEngine.GraphToolsFoundation.Overdrive;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.TestModels
{
    class VariableDeclarationModel : DeclarationModel, IVariableDeclarationModel
    {
        IGraphModel m_GraphModel;

        public TypeHandle DataType { get; set; }
        public ModifierFlags Modifiers { get; set; }
        public string Tooltip { get; set; }
        public IConstant InitializationModel { get; set; }
        public bool IsExposed { get; set; }

        public override IGraphModel GraphModel => m_GraphModel;

        /// <inheritdoc />
        public IEnumerable<IGraphElementModel> ContainedModels
        {
            get => Enumerable.Repeat(this, 1);
        }

        /// <inheritdoc />
        public IGroupModel ParentGroup { get; set; }

        public string GetVariableName() => Title.CodifyStringInternal();
        public void CreateInitializationValue()
        {
        }

        public bool IsUsed()
        {
            return true;
        }

        // Can't be on the property as we inherit a getter only GraphModel property.
        internal void SetGraphModel(IGraphModel graphModel)
        {
            m_GraphModel = graphModel;
        }
    }

    class VariableNodeModel : NodeModel, IVariableNodeModel, ICloneable
    {
        const string k_MainPortName = "MainPortName";

        VariableDeclarationModel m_DeclarationModel;
        protected IPortModel m_MainPortModel;

        public IPortModel InputPort => m_MainPortModel?.Direction == PortDirection.Input ? m_MainPortModel : null;
        public IPortModel OutputPort => m_MainPortModel?.Direction == PortDirection.Output ? m_MainPortModel : null;
        public IDeclarationModel DeclarationModel
        {
            get => m_DeclarationModel;
            set
            {
                m_DeclarationModel = (VariableDeclarationModel)value;
                DefineNode();
            }
        }

        public IVariableDeclarationModel VariableDeclarationModel
        {
            get => DeclarationModel as IVariableDeclarationModel;
            set => DeclarationModel = value;
        }

        public void UpdateTypeFromDeclaration()
        {
            if (DeclarationModel != null && m_MainPortModel != null)
                m_MainPortModel.DataTypeHandle = VariableDeclarationModel.DataType;

            // update connected nodes' ports colors/types
            if (m_MainPortModel != null)
                foreach (var connectedPortModel in m_MainPortModel.GetConnectedPorts())
                    connectedPortModel.NodeModel.OnConnection(connectedPortModel, m_MainPortModel);
        }

        public IGraphElementModel Clone()
        {
            var decl = m_DeclarationModel;
            try
            {
                m_DeclarationModel = null;
                var clone = CloneHelpers.CloneUsingScriptableObjectInstantiate(this);
                clone.m_DeclarationModel = decl;
                return clone;
            }
            finally
            {
                m_DeclarationModel = decl;
            }
        }

        public IPortModel MainOutputPort => m_MainPortModel;

        protected override void OnDefineNode()
        {
            base.OnDefineNode();

            // used by macro outputs
            if (m_DeclarationModel != null /* this node */ && m_DeclarationModel.Modifiers.HasFlag(ModifierFlags.Write))
            {
                if (this.GetDataType() == TypeHandle.ExecutionFlow)
                    m_MainPortModel = this.AddExecutionInputPort(null);
                else
                    m_MainPortModel = this.AddDataInputPort(null, this.GetDataType(), k_MainPortName);
            }
            else
            {
                if (this.GetDataType() == TypeHandle.ExecutionFlow)
                    m_MainPortModel = this.AddExecutionOutputPort(null);
                else
                    m_MainPortModel = this.AddDataOutputPort(null, this.GetDataType(), k_MainPortName);
            }
        }
    }
}
