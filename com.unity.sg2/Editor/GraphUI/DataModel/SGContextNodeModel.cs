using System;
using System.Linq;
using Unity.GraphToolsFoundation.Editor;
using UnityEditor.ShaderGraph.GraphDelta;
using UnityEngine;
using Unity.GraphToolsFoundation;

namespace UnityEditor.ShaderGraph.GraphUI
{
    // TODO/NOTE: Don't rely on this inheriting from GraphDataNodeModel, it will eventually become a context w/ blocks.
    class GraphDataContextNodeModel : GraphDataNodeModel
    {
        public GraphDataContextNodeModel()
        {
            m_Capabilities.Remove(Unity.GraphToolsFoundation.Editor.Capabilities.Deletable);
            m_Capabilities.Remove(Unity.GraphToolsFoundation.Editor.Capabilities.Copiable);
        }

        public override bool HasPreview => false;

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
    }
}
