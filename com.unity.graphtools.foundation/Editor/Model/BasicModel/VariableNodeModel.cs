using System;
using UnityEngine;
using UnityEngine.GraphToolsFoundation.Overdrive;
using UnityEngine.Scripting.APIUpdating;

namespace UnityEditor.GraphToolsFoundation.Overdrive.BasicModel
{
    [Serializable]
    [MovedFrom(false, sourceAssembly: "Unity.GraphTools.Foundation.Overdrive.Editor")]
    public class VariableNodeModel : NodeModel, IVariableNodeModel, IRenamable, ICloneable
    {
        const string k_MainPortName = "MainPortName";

        [SerializeReference]
        VariableDeclarationModel m_DeclarationModel;

        protected IPortModel m_MainPortModel;

        /// <summary>
        /// The human readable name of the data type of the variable declaration model.
        /// </summary>
        public virtual string DataTypeString => VariableDeclarationModel?.DataType.GetMetadata(GraphModel.Stencil).FriendlyName ?? string.Empty;

        /// <summary>
        /// The string used to describe this variable.
        /// </summary>
        public virtual string VariableString => DeclarationModel == null ? string.Empty : VariableDeclarationModel.IsExposed ? "Exposed variable" : "Variable";

        /// <inheritdoc />
        public override string Title => m_DeclarationModel == null ? "" : m_DeclarationModel.Title;

        /// <inheritdoc />
        public IDeclarationModel DeclarationModel
        {
            get => m_DeclarationModel;
            set
            {
                m_DeclarationModel = (VariableDeclarationModel)value;
                DefineNode();
            }
        }

        /// <inheritdoc />
        public IVariableDeclarationModel VariableDeclarationModel
        {
            get => DeclarationModel as IVariableDeclarationModel;
            set => DeclarationModel = value;
        }

        /// <inheritdoc />
        public IPortModel InputPort => m_MainPortModel?.Direction == PortDirection.Input ? m_MainPortModel : null;

        /// <inheritdoc />
        public IPortModel OutputPort => m_MainPortModel?.Direction == PortDirection.Output ? m_MainPortModel : null;

        /// <summary>
        /// Initializes a new instance of the <see cref="VariableNodeModel"/> class.
        /// </summary>
        public VariableNodeModel()
        {
            m_Capabilities.Add(Overdrive.Capabilities.Renamable);
        }

        /// <inheritdoc />
        public virtual void UpdateTypeFromDeclaration()
        {
            if (DeclarationModel != null && m_MainPortModel != null)
                m_MainPortModel.DataTypeHandle = VariableDeclarationModel.DataType;

            // update connected nodes' ports colors/types
            if (m_MainPortModel != null)
                foreach (var connectedPortModel in m_MainPortModel.GetConnectedPorts())
                    connectedPortModel.NodeModel.OnConnection(connectedPortModel, m_MainPortModel);
        }

        /// <inheritdoc />
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

        /// <inheritdoc />
        public virtual void Rename(string newName)
        {
            if (!this.IsRenamable())
                return;

            (DeclarationModel as IRenamable)?.Rename(newName);
        }

        /// <inheritdoc />
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

        /// <inheritdoc />
        public override string Tooltip
        {
            get
            {
                var tooltip = $"{VariableString}";
                if (!string.IsNullOrEmpty(DataTypeString))
                    tooltip += $" of type {DataTypeString}";
                if (!string.IsNullOrEmpty(VariableDeclarationModel?.Tooltip))
                    tooltip += "\n" + VariableDeclarationModel.Tooltip;

                if (string.IsNullOrEmpty(tooltip))
                    return base.Tooltip;

                return tooltip;
            }
            set => base.Tooltip = value;
        }
    }
}
