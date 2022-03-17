using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.GraphToolsFoundation.Overdrive;

namespace UnityEditor.GraphToolsFoundation.Overdrive.BasicModel
{
    /// <summary>
    /// The model for context nodes.
    /// </summary>
    [Serializable]
    public class ContextNodeModel : NodeModel, IContextNodeModel
    {
        [SerializeReference]
        List<IBlockNodeModel> m_Blocks = new List<IBlockNodeModel>();

        /// <summary>
        /// Initializes a new instance of the <see cref="ContextNodeModel"/> class.
        /// </summary>
        public ContextNodeModel()
        {
            this.SetCapability(Overdrive.Capabilities.Collapsible, false);
        }

        /// <inheritdoc />
        public void InsertBlock(IBlockNodeModel blockModel, int index = -1, SpawnFlags spawnFlags = SpawnFlags.Default)
        {
            if (blockModel.ContextNodeModel != null)
                blockModel.ContextNodeModel.RemoveElements(new[] { blockModel });

            if (index > m_Blocks.Count)
                throw new ArgumentException(nameof(index));
            if (!blockModel.IsCompatibleWith(this) && GetType() != typeof(ContextNodeModel)) // Blocks have to be compatible with the base ContextNodeModel because of the searcher's "Dummy Context".
                throw new ArgumentException(nameof(blockModel));

            if ((spawnFlags & SpawnFlags.Orphan) == 0)
                GraphModel.RegisterElement(blockModel);

            if (index < 0 || index == m_Blocks.Count)
                m_Blocks.Add(blockModel);
            else
                m_Blocks.Insert(index, blockModel);

            blockModel.AssetModel = AssetModel;
            blockModel.ContextNodeModel = this;
        }

        /// <inheritdoc />
        public IBlockNodeModel CreateAndInsertBlock(Type blockType, int index = -1, SerializableGUID guid = default, Action<INodeModel> initializationCallback = null, SpawnFlags spawnFlags = SpawnFlags.Default)
        {
            //use SpawnFlags.Orphan to prevent adding the node to the GraphModel
            IBlockNodeModel block = (IBlockNodeModel)GraphModel.CreateNode(blockType, blockType.Name, Vector2.zero, guid, initializationCallback, spawnFlags | SpawnFlags.Orphan);

            InsertBlock(block, index, spawnFlags);

            return block;
        }

        /// <inheritdoc />
        public T CreateAndInsertBlock<T>(int index = -1, SerializableGUID guid = default,
            Action<INodeModel> initializationCallback = null, SpawnFlags spawnFlags = SpawnFlags.Default) where T : IBlockNodeModel, new()
        {
            return (T)CreateAndInsertBlock(typeof(T), index, guid, initializationCallback, spawnFlags);
        }

        /// <inheritdoc />
        public IEnumerable<IGraphElementModel> GraphElementModels => m_Blocks;

        void IGraphElementContainer.RemoveElements(IReadOnlyCollection<IGraphElementModel> elementModels)
        {
            foreach (var blockNodeModel in elementModels.OfType<IBlockNodeModel>())
            {
                GraphModel.UnregisterElement(blockNodeModel);
                if (!m_Blocks.Remove(blockNodeModel))
                    throw new ArgumentException(nameof(blockNodeModel));
                blockNodeModel.ContextNodeModel = null;
            }
        }

        /// <inheritdoc/>
        protected override void OnDefineNode()
        {
            base.OnDefineNode();

            foreach (var block in GraphElementModels)
            {
                (block as BlockNodeModel)?.DefineNode();
            }
        }

        public void Repair()
        {
            m_Blocks.RemoveAll(t => t == null);
        }
    }
}
