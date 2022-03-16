using System;
using UnityEngine;
using UnityEngine.GraphToolsFoundation.Overdrive;
using UnityEngine.Scripting.APIUpdating;

namespace UnityEditor.GraphToolsFoundation.Overdrive.BasicModel
{
    /// <summary>
    /// Base implementation for portals.
    /// </summary>
    [Serializable]
    [MovedFrom(false, sourceAssembly: "Unity.GraphTools.Foundation.Overdrive.Editor")]
    public abstract class EdgePortalModel : NodeModel, IEdgePortalModel, IRenamable, ICloneable
    {
        [SerializeField]
        int m_EvaluationOrder;

        [SerializeReference]
        IDeclarationModel m_DeclarationModel;

        [SerializeField]
        TypeHandle m_TypeHandle;

        /// <inheritdoc />
        public IDeclarationModel DeclarationModel
        {
            get => m_DeclarationModel;
            set => m_DeclarationModel = value;
        }

        /// <inheritdoc />
        public TypeHandle PortDataTypeHandle
        {
            get
            {
                // Type's identification of portals' ports are empty strings in the compatibility tests.
                if (m_TypeHandle.Identification == null)
                    m_TypeHandle = new TypeHandle("");
                return m_TypeHandle;
            }
            set => m_TypeHandle = value;
        }

        /// <inheritdoc />
        public override string Title => m_DeclarationModel == null ? "" : m_DeclarationModel.Title;

        /// <inheritdoc />
        public int EvaluationOrder
        {
            get => m_EvaluationOrder;
            protected set => m_EvaluationOrder = value;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="EdgePortalModel"/> class.
        /// </summary>
        protected EdgePortalModel()
        {
            m_Capabilities.Add(Overdrive.Capabilities.Renamable);
        }

        /// <inheritdoc />
        public void Rename(string newName)
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
        public virtual bool CanCreateOppositePortal()
        {
            return true;
        }
    }
}
