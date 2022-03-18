using System;
using UnityEngine.GraphToolsFoundation.Overdrive;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// Interface for a model that represents a context (a node that contains blocks) in a graph.
    /// </summary>
    public interface IContextNodeModel : IInputOutputPortsNodeModel, IGraphElementContainer
    {
        /// <summary>
        /// Inserts a block in the context.
        /// </summary>
        /// <param name="blockModel">The block model to insert.</param>
        /// <param name="index">The index at which insert the block. -1 means at the end of the list.</param>
        /// <param name="spawnFlags">The flags specifying how the node is to be spawned.</param>
        void InsertBlock(IBlockNodeModel blockModel, int index = -1, SpawnFlags spawnFlags = SpawnFlags.Default);

        /// <summary>
        /// Creates a new block and inserts it in the context.
        /// </summary>
        /// <param name="blockType">The type of block to instantiate.</param>
        /// <param name="index">The index at which insert the block. -1 means at the end of the list.</param>
        /// <param name="guid">The GUID of the new block.</param>
        /// <param name="initializationCallback">A callback called once the block is ready.</param>
        /// <param name="spawnFlags">The flags specifying how the node is to be spawned.</param>
        /// <returns>The newly created block.</returns>
        IBlockNodeModel CreateAndInsertBlock(Type blockType, int index = -1, SerializableGUID guid = default, Action<INodeModel> initializationCallback = null, SpawnFlags spawnFlags = SpawnFlags.Default);

        /// <summary>
        /// Creates a new block and inserts it in the context.
        /// </summary>
        /// <typeparam name="T">The type of block to instantiate.</typeparam>
        /// <param name="index">The index at which insert the block. -1 means at the end of the list</param>
        /// <param name="guid">The GUID of the new block</param>
        /// <param name="initializationCallback">A callback called once the block is ready</param>
        /// <param name="spawnFlags">The flags specifying how the node is to be spawned.</param>
        /// <returns>The newly created block.</returns>
        T CreateAndInsertBlock<T>(int index = -1, SerializableGUID guid = default, Action<INodeModel> initializationCallback = null, SpawnFlags spawnFlags = SpawnFlags.Default) where T : IBlockNodeModel, new();
    }
}
