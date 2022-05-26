using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEditor.ShaderGraph.GraphDelta;
using UnityEngine;
using UnityEngine.GraphToolsFoundation.CommandStateObserver;
using UnityEngine.GraphToolsFoundation.Overdrive;

namespace UnityEditor.ShaderGraph.GraphUI
{
    public class AddContextEntryCommand : UndoableCommand
    {
        readonly GraphDataContextNodeModel m_Model;
        readonly string m_Name;
        readonly TypeHandle m_Type;

        public AddContextEntryCommand(GraphDataContextNodeModel model, string name, TypeHandle type)
        {
            m_Model = model;
            m_Name = name;
            m_Type = type;
        }

        public static void DefaultCommandHandler(
            UndoStateComponent undoState,
            GraphModelStateComponent graphModelState,
            AddContextEntryCommand command)
        {
            using var graphUpdater = graphModelState.UpdateScope;

            var graphModel = (ShaderGraphModel)command.m_Model.GraphModel;
            var registry = graphModel.RegistryInstance;

            if (!command.m_Model.TryGetNodeReader(out var nodeHandler)) return;

            var entry = new IContextDescriptor.ContextEntry
            {
                fieldName = command.m_Name,
                height = ShaderGraphExampleTypes.GetGraphTypeHeight(command.m_Type),
                length = ShaderGraphExampleTypes.GetGraphTypeLength(command.m_Type),
                primitive = ShaderGraphExampleTypes.GetGraphTypePrimitive(command.m_Type),
                precision = GraphType.Precision.Any,
                initialValue = Matrix4x4.zero,
            };

            ContextBuilder.AddContextEntry(nodeHandler, entry, registry);
            graphModel.GraphHandler.ReconcretizeNode(nodeHandler.ID.FullPath);
            command.m_Model.DefineNode();

            graphUpdater.MarkChanged(command.m_Model);
        }
    }
}
