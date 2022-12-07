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
    class SGContextNodeModel : ContextNodeModel, IPreviewUpdateListener, IGraphDataOwner
    {
        [SerializeField]
        string m_GraphDataName;

        public string graphDataName
        {
            get => m_GraphDataName;
            set => m_GraphDataName = value;
        }

        public bool existsInGraphData => m_GraphDataName != null && TryGetNodeHandler(out _);
        GraphHandler graphHandler => ((SGGraphModel)GraphModel).GraphHandler;
        SGGraphModel sgGraphModel => GraphModel as SGGraphModel;

        public RegistryKey registryKey
        {
            get
            {
                Assert.IsTrue(TryGetNodeHandler(out var reader));
                return reader.GetRegistryKey();
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

        public bool TryGetNodeHandler(out NodeHandler reader)
        {
            try
            {
                reader = graphHandler.GetNode(graphDataName);
                return reader != null;
            }
            catch (Exception exception)
            {
                AssertHelpers.Fail("Failed to retrieve node due to exception:" + exception);
                reader = null;
                return false;
            }
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
            if (!TryGetNodeHandler(out var nodeReader))
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
            if (!TryGetNodeHandler(out var nodeHandler)) return;

            ContextBuilder.AddContextEntry(nodeHandler, typeHandle.GetBackingDescriptor(), entryName, nodeHandler.Registry);
            graphHandler.ReconcretizeNode(nodeHandler.ID.FullPath);
            CreateAndInsertBlockForEntry(entryName);
        }

        public void RemoveEntry(string entryName)
        {
            if (!TryGetNodeHandler(out var nodeHandler)) return;

            nodeHandler.RemovePort(entryName);
            nodeHandler.RemovePort("out_" + entryName);

            graphHandler.ReconcretizeNode(nodeHandler.ID.FullPath);

            if (TryGetBlockForContextEntry(entryName, out var blockNode))
            {
                RemoveElements(new[] { blockNode });
            }
        }

        #endregion
    }
}
