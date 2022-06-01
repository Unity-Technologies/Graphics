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

        private IPortModel GetEntryInputPort(string name) => this.GetInputPorts().First(p => p.UniqueName == name);

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

        public void RenameEntry(string name, string newName)
        {
            if (!TryGetNodeReader(out var nodeHandler)) return;

            var currentType = this.GetInputPorts().First(p => p.UniqueName == name).DataTypeHandle;
            CreateEntry(newName, currentType);

            var oldPort = GetEntryInputPort(name);
            var newPort = GetEntryInputPort(newName);
            foreach (var edge in oldPort.GetConnectedEdges().ToList()) {
                edge.ToPort = newPort;
            }

            RemoveEntry(name);
        }

        public void ChangeEntryType(string name, TypeHandle newType)
        {
            RemoveEntry(name);
            CreateEntry(name, newType);
            DefineNode();
        }
    }
}
