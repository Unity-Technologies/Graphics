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
        #region Copied from GraphDataNodeModel // TODO: Factor out

        [SerializeField]
        string m_GraphDataName;

        public string graphDataName
        {
            get => m_GraphDataName;
            set => m_GraphDataName = value;
        }

        RegistryKey m_PreviewRegistryKey;

        public bool existsInGraphData =>
            m_GraphDataName != null && TryGetNodeHandler(out _);

        public bool TryGetNodeHandler(out NodeHandler reader)
        {
            try
            {
                if (graphDataName == null)
                {
                    reader = registry.GetDefaultTopology(m_PreviewRegistryKey);
                    return true;
                }

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

        protected GraphHandler graphHandler =>
            ((ShaderGraphModel)GraphModel).GraphHandler;

        ShaderGraphRegistry registry =>
            ((ShaderGraphStencil)GraphModel.Stencil).GetRegistry();

        internal ShaderGraphModel shaderGraphModel => GraphModel as ShaderGraphModel;

        public RegistryKey registryKey
        {
            get
            {
                if (!existsInGraphData)
                    return m_PreviewRegistryKey;

                Assert.IsTrue(TryGetNodeHandler(out var reader));

                // Store the registry key to use for node duplication
                // duplicationRegistryKey = reader.GetRegistryKey();
                return reader.GetRegistryKey();
            }
        }

        #endregion

        public Texture PreviewTexture { get; private set; }
        public int CurrentVersion { get; private set; }
        public string ListenerID => m_GraphDataName;

        public GraphDataContextNodeModel()
        {
            m_Capabilities.Remove(Unity.GraphToolsFoundation.Editor.Capabilities.Deletable);
            m_Capabilities.Remove(Unity.GraphToolsFoundation.Editor.Capabilities.Copiable);
        }

        protected override void OnDefineNode()
        {
            if (!TryGetNodeHandler(out var nodeReader)) return;

            // TODO: Not the desired behavior, brute force way to prevent duplicates for now
            var currentBlocks = GraphElementModels.OfType<GraphDataBlockNodeModel>().Select(b => b.ContextEntryName).ToHashSet();

            foreach (var portHandler in nodeReader.GetPorts())
            {
                if (!portHandler.IsHorizontal) continue;
                if (portHandler.LocalID.Contains("out_")) continue;

                var staticField = portHandler.GetTypeField().GetSubField<bool>("IsStatic");
                var localField = portHandler.GetTypeField().GetSubField<bool>("IsLocal");
                if (staticField != null && staticField.GetData()) continue;
                if (localField != null && localField.GetData()) continue;

                if (!currentBlocks.Contains(portHandler.LocalID))
                {
                    CreateAndInsertBlock<GraphDataBlockNodeModel>(initializationCallback: node =>
                    {
                        if (node is not GraphDataBlockNodeModel blockNode) return;

                        blockNode.Title = portHandler.LocalID;
                        blockNode.ContextEntryName = portHandler.LocalID;
                    });
                }
            }
        }

        public void HandlePreviewTextureUpdated(Texture newPreviewTexture)
        {
            PreviewTexture = newPreviewTexture;
        }

        public void HandlePreviewShaderErrors(ShaderMessage[] shaderMessages)
        {
            throw new NotImplementedException();
        }

        #region Legacy Context methods // TODO: Update to use blocks

        public bool IsMainContextNode()
        {
            return graphDataName == shaderGraphModel.DefaultContextName;
        }

        public PortModel GetInputPortForEntry(string name) => this.GetInputPorts().FirstOrDefault(p => p.UniqueName == name);

        public void CreateEntry(string entryName, TypeHandle typeHandle)
        {
            if (!TryGetNodeHandler(out var nodeHandler)) return;

            ContextBuilder.AddContextEntry(nodeHandler, typeHandle.GetBackingDescriptor(), entryName, nodeHandler.Registry);
            graphHandler.ReconcretizeNode(nodeHandler.ID.FullPath);
            DefineNode();
        }

        public void RemoveEntry(string name)
        {
            if (!TryGetNodeHandler(out var nodeHandler)) return;

            nodeHandler.RemovePort(name);
            nodeHandler.RemovePort("out_" + name);

            graphHandler.ReconcretizeNode(nodeHandler.ID.FullPath);
            DefineNode();
        }

        #endregion
    }
}
