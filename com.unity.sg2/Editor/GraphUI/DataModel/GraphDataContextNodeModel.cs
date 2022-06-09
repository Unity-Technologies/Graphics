using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEditor.ShaderGraph.GraphDelta;
using UnityEngine;
using UnityEngine.GraphToolsFoundation.Overdrive;

namespace UnityEditor.ShaderGraph.GraphUI
{
    // TODO/NOTE: Don't rely on this inheriting from GraphDataNodeModel, it will eventually become a context w/ blocks.
    public class GraphDataContextNodeModel : GraphDataNodeModel
    {
        public GraphDataContextNodeModel()
        {
            m_Capabilities.Remove(GraphToolsFoundation.Overdrive.Capabilities.Deletable);
            m_Capabilities.Remove(GraphToolsFoundation.Overdrive.Capabilities.Copiable);
        }

        public override bool HasPreview => false;

        public IPortModel GetInputPortForEntry(string name) => this.GetInputPorts().FirstOrDefault(p => p.UniqueName == name);

        public void CreateEntry(string entryName, TypeHandle typeHandle)
        {
            if (!TryGetNodeReader(out var nodeHandler)) return;

            var entry = new IContextDescriptor.ContextEntry
            {
                fieldName = entryName,
                height = ShaderGraphExampleTypes.GetGraphTypeHeight(typeHandle),
                length = ShaderGraphExampleTypes.GetGraphTypeLength(typeHandle),
                primitive = ShaderGraphExampleTypes.GetGraphTypePrimitive(typeHandle),
                precision = GraphType.Precision.Any,
                initialValue = Matrix4x4.zero,
            };

            ContextBuilder.AddContextEntry(nodeHandler, entry, nodeHandler.Registry);
            graphHandler.ReconcretizeNode(nodeHandler.ID.FullPath);
            DefineNode();
        }

        public void RemoveEntry(string name)
        {
            if (!TryGetNodeReader(out var nodeHandler)) return;

            nodeHandler.RemovePort(name);
            nodeHandler.RemovePort("out_" + name);

            graphHandler.ReconcretizeNode(nodeHandler.ID.FullPath);
            DefineNode();
        }
    }
}
