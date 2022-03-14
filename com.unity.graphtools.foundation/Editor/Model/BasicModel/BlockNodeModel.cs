using System;
using UnityEngine;
using UnityEngine.GraphToolsFoundation.Overdrive;

namespace UnityEditor.GraphToolsFoundation.Overdrive.BasicModel
{
    /// <summary>
    /// A model that represents a block node.
    /// </summary>
    [Serializable]
    public class BlockNodeModel : NodeModel, IBlockNodeModel
    {
        [SerializeReference, HideInInspector]
        IContextNodeModel m_ContextNodeModel;

        /// <inheritdoc />
        public IContextNodeModel ContextNodeModel
        {
            get => m_ContextNodeModel;
            set => m_ContextNodeModel = value;
        }

        /// <inheritdoc />
        public override IGraphElementContainer Container => ContextNodeModel;

        /// <summary>
        /// Initializes a new instance of the <see cref="BlockNodeModel" /> class.
        /// </summary>
        public BlockNodeModel()
        {
            this.SetCapability(Overdrive.Capabilities.Movable, false);
            this.SetCapability(Overdrive.Capabilities.Ascendable, false);
            this.SetCapability(Overdrive.Capabilities.NeedsContainer, true);
        }

        /// <inheritdoc/>
        public virtual bool IsCompatibleWith(IContextNodeModel context)
        {
            return true;
        }

        /// <inheritdoc/>
        public int GetIndex()
        {
            int cpt = 0;
            foreach (var block in ContextNodeModel.GraphElementModels)
            {
                if (block == this)
                    return cpt;
                cpt++;
            }

            return -1;
        }
    }
}
