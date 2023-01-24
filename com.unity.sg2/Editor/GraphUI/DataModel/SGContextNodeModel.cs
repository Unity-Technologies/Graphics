using System;
using System.Collections.Generic;
using System.Linq;
using Unity.GraphToolsFoundation.Editor;
using UnityEditor.ShaderGraph.GraphDelta;
using UnityEngine;
using Unity.GraphToolsFoundation;
using UnityEngine.Assertions;

namespace UnityEditor.ShaderGraph.GraphUI
{
    [Serializable]
    class SGContextNodeModel : ContextNodeModel, IPreviewUpdateListener, IGraphDataOwner<SGContextNodeModel>
    {
        [SerializeField]
        RegistryKey m_RegistryKey;

        [SerializeField]
        string m_GraphDataName;

        /// <summary>
        /// The <see cref="IGraphDataOwner{T}"/> interface for this object.
        /// </summary>
        public IGraphDataOwner<SGContextNodeModel> graphDataOwner => this;

        /// <summary>
        /// The identifier/unique name used to represent this entity and retrieve info. regarding it from CLDS.
        /// </summary>
        public string graphDataName
        {
            get => m_GraphDataName;
            set => m_GraphDataName = value;
        }

        SGGraphModel sgGraphModel => GraphModel as SGGraphModel;

        /// <summary>
        /// The <see cref="RegistryKey"/> that represents the concrete type within the Registry, of this object.
        /// </summary>
        public RegistryKey registryKey
        {
            get
            {
                if (!m_RegistryKey.Valid())
                {
                    m_RegistryKey = this.GetRegistryKeyFromNodeHandler();
                }

                return m_RegistryKey;
            }
        }

        public Texture PreviewTexture { get; private set; }
        public int CurrentVersion { get; private set; }
        public string ListenerID => m_GraphDataName;

        public SGContextNodeModel()
        {
            m_Capabilities.Remove(Unity.GraphToolsFoundation.Editor.Capabilities.Deletable);
            m_Capabilities.Remove(Unity.GraphToolsFoundation.Editor.Capabilities.Copiable);
        }

        public void AddBlocksFromGraphDelta()
        {
            var currentBlockNames = GraphElementModels.OfType<SGBlockNodeModel>().Select(b => b.ContextEntryName).ToHashSet();

            foreach (var blockName in GetContextEntryNames())
            {
                if (!currentBlockNames.Contains(blockName))
                {
                    CreateAndInsertBlockForEntry(blockName);
                }
            }
        }

        // Right now context entry names are all that are needed to represent blocks. This could eventually return
        // something like a list of SGPortViewModels when more information (i.e., display names) is available.
        public IEnumerable<string> GetContextEntryNames()
        {
            if (!graphDataOwner.TryGetNodeHandler(out var nodeReader))
            {
                yield break;
            }

            foreach (var portHandler in nodeReader.GetPorts())
            {
                if (!portHandler.IsHorizontal || !portHandler.IsInput) continue;

                var staticField = portHandler.GetTypeField().GetSubField<bool>("IsStatic");
                var localField = portHandler.GetTypeField().GetSubField<bool>("IsLocal");
                if (staticField != null && staticField.GetData()) continue;
                if (localField != null && localField.GetData()) continue;

                yield return portHandler.LocalID;
            }
        }

        public bool TryGetBlockForContextEntry(string contextEntryName, out SGBlockNodeModel block)
        {
            foreach (var subModel in GraphElementModels)
            {
                if (subModel is SGBlockNodeModel existingBlock && existingBlock.ContextEntryName == contextEntryName)
                {
                    block = existingBlock;
                    return true;
                }
            }

            block = null;
            return false;
        }

        public void HandlePreviewTextureUpdated(Texture newPreviewTexture)
        {
            PreviewTexture = newPreviewTexture;
            CurrentVersion++;
        }

        public void HandlePreviewShaderErrors(ShaderMessage[] shaderMessages)
        {
            throw new NotImplementedException();
        }

        SGBlockNodeModel CreateAndInsertBlockForEntry(string entryName)
        {
            return CreateAndInsertBlock<SGBlockNodeModel>(initializationCallback: node =>
            {
                if (node is not SGBlockNodeModel blockNode) return;

                blockNode.Title = entryName;
                blockNode.ContextEntryName = entryName;
            });
        }

        public bool IsMainContextNode()
        {
            return graphDataName == sgGraphModel.DefaultContextName;
        }

        // TODO (Joe): These were only used by the prototype subgraph output editor and can eventually be removed.
        #region Legacy Context methods

        public PortModel GetInputPortForEntry(string entryName) => this.GetInputPorts().FirstOrDefault(p => p.UniqueName == entryName);

        public void CreateEntry(string entryName, TypeHandle typeHandle)
        {
            if (!graphDataOwner.TryGetNodeHandler(out var nodeHandler)) return;

            ContextBuilder.AddContextEntry(nodeHandler, typeHandle.GetBackingDescriptor(), entryName, nodeHandler.Registry);
            graphDataOwner.graphHandler.ReconcretizeNode(nodeHandler.ID.FullPath);
            CreateAndInsertBlockForEntry(entryName);
        }

        public void RemoveEntry(string entryName)
        {
            if (!graphDataOwner.TryGetNodeHandler(out var nodeHandler)) return;

            nodeHandler.RemovePort(entryName);
            nodeHandler.RemovePort("out_" + entryName);

            graphDataOwner.graphHandler.ReconcretizeNode(nodeHandler.ID.FullPath);

            if (TryGetBlockForContextEntry(entryName, out var blockNode))
            {
                RemoveElements(new[] { blockNode });
            }
        }

        #endregion

        /// <inheritdoc />
        public override void OnBeforeSerialize()
        {
            base.OnBeforeSerialize();

            if (graphDataOwner.TryGetNodeHandler(out var reader))
            {
                m_RegistryKey = reader.GetRegistryKey();
            }
        }
    }
}
