using System;
using System.Linq;
using Unity.GraphToolsFoundation.Editor;
using UnityEditor.ShaderGraph.GraphDelta;
using UnityEngine;
using Unity.GraphToolsFoundation;
using UnityEngine.Assertions;

namespace UnityEditor.ShaderGraph.GraphUI
{
    class GraphDataContextNodeModel : ContextNodeModel, IPreviewUpdateListener, IGraphDataOwner
    {
        [SerializeField]
        string m_GraphDataName;

        public string graphDataName
        {
            get => m_GraphDataName;
            set => m_GraphDataName = value;
        }

        public bool existsInGraphData => m_GraphDataName != null && TryGetNodeHandler(out _);
        GraphHandler graphHandler => ((ShaderGraphModel)GraphModel).GraphHandler;
        ShaderGraphModel shaderGraphModel => GraphModel as ShaderGraphModel;

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

        public GraphDataContextNodeModel()
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
            if (!TryGetNodeHandler(out var nodeReader)) return;

            var currentBlockNames = GraphElementModels.OfType<GraphDataBlockNodeModel>().Select(b => b.ContextEntryName).ToHashSet();

            foreach (var portHandler in nodeReader.GetPorts())
            {
                if (!portHandler.IsHorizontal) continue;
                if (portHandler.LocalID.Contains("out_")) continue;

                var staticField = portHandler.GetTypeField().GetSubField<bool>("IsStatic");
                var localField = portHandler.GetTypeField().GetSubField<bool>("IsLocal");
                if (staticField != null && staticField.GetData()) continue;
                if (localField != null && localField.GetData()) continue;

                if (!currentBlockNames.Contains(portHandler.LocalID))
                {
                    CreateAndInsertBlockForEntry(portHandler.LocalID);
                }
            }
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

        GraphDataBlockNodeModel CreateAndInsertBlockForEntry(string entryName)
        {
            return CreateAndInsertBlock<GraphDataBlockNodeModel>(initializationCallback: node =>
            {
                if (node is not GraphDataBlockNodeModel blockNode) return;

                blockNode.Title = entryName;
                blockNode.ContextEntryName = entryName;
            });
        }

        public bool IsMainContextNode()
        {
            return graphDataName == shaderGraphModel.DefaultContextName;
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
            RemoveElements(GraphElementModels.Where(model => model is GraphDataBlockNodeModel blockNode && blockNode.ContextEntryName == entryName).ToList());
        }

        #endregion
    }
}
