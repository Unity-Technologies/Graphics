using System;
using System.Collections.Generic;
using Unity.GraphToolsFoundation.Editor;
using Unity.ItemLibrary.Editor;
using UnityEditor.ShaderGraph.GraphDelta;

namespace UnityEditor.ShaderGraph.GraphUI
{
    /// <summary>
    /// A RegistryNodeSearcherItem is a GraphNodeModelSearcherItem associated with a registry key. The key is exposed
    /// to make filtering easier.
    /// </summary>
    class RegistryNodeSearcherItem : GraphNodeModelLibraryItem
    {
        public readonly RegistryKey registryKey;

        public override string Name => registryKey.Name;


        public RegistryNodeSearcherItem(
            GraphModel graphModel,
            RegistryKey registryKey,
            string name,
            IItemLibraryData data = null,
            List<ItemLibraryItem> children = null,
            Func<string> getName = null,
            string help = null
        ) : base(
            name,
            data,
            creationData => graphModel.CreateGraphDataNode(registryKey, name, creationData.Position, creationData.Guid, creationData.SpawnFlags))
        {
            this.registryKey = registryKey;
        }
    }
}
