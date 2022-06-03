using System.Linq;
using UnityEditor.GraphToolsFoundation.Overdrive;
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
            using (var undoStateUpdater = undoState.UpdateScope)
            {
                undoStateUpdater.SaveSingleState(graphModelState, command);
            }

            using var graphUpdater = graphModelState.UpdateScope;
            command.m_Model.CreateEntry(command.m_Name, command.m_Type);
            graphUpdater.MarkChanged(command.m_Model);
        }
    }

    public class RemoveContextEntryCommand : UndoableCommand
    {
        readonly GraphDataContextNodeModel m_Model;
        readonly string m_Name;

        public RemoveContextEntryCommand(GraphDataContextNodeModel model, string name)
        {
            m_Model = model;
            m_Name = name;
        }

        public static void DefaultCommandHandler(
            UndoStateComponent undoState,
            GraphModelStateComponent graphModelState,
            RemoveContextEntryCommand command)
        {
            using (var undoStateUpdater = undoState.UpdateScope)
            {
                undoStateUpdater.SaveSingleState(graphModelState, command);
            }

            using var graphUpdater = graphModelState.UpdateScope;

            var model = command.m_Model;
            var oldPort = model.GetInputPortForEntry(command.m_Name);
            var oldEdges = oldPort.GetConnectedEdges().ToList();
            model.GraphModel.DeleteEdges(oldEdges);
            graphUpdater.MarkDeleted(oldEdges);

            command.m_Model.RemoveEntry(command.m_Name);
            graphUpdater.MarkChanged(command.m_Model);
        }
    }

    public class RenameContextEntryCommand : UndoableCommand
    {
        readonly GraphDataContextNodeModel m_Model;
        readonly string m_OldName;
        readonly string m_NewName;

        public RenameContextEntryCommand(GraphDataContextNodeModel model, string oldName, string newName)
        {
            m_Model = model;
            m_OldName = oldName;
            m_NewName = newName;
        }

        public static void DefaultCommandHandler(
            UndoStateComponent undoState,
            GraphModelStateComponent graphModelState,
            RenameContextEntryCommand command)
        {
            using (var undoStateUpdater = undoState.UpdateScope)
            {
                undoStateUpdater.SaveSingleState(graphModelState, command);
            }

            using var graphUpdater = graphModelState.UpdateScope;

            var model = command.m_Model;
            var oldPort = model.GetInputPortForEntry(command.m_OldName);
            if (oldPort == null)
            {
                return;
            }

            var currentType = oldPort.DataTypeHandle;
            model.CreateEntry(command.m_NewName, currentType);

            var newPort = model.GetInputPortForEntry(command.m_NewName);
            foreach (var edge in oldPort.GetConnectedEdges().ToList())
            {
                edge.ToPort = newPort;
                graphUpdater.MarkChanged(edge);
            }

            model.RemoveEntry(command.m_OldName);
            graphUpdater.MarkChanged(command.m_Model);
        }
    }

    public class ChangeContextEntryTypeCommand : UndoableCommand
    {
        readonly GraphDataContextNodeModel m_Model;
        readonly string m_Name;
        readonly TypeHandle m_NewType;

        public ChangeContextEntryTypeCommand(GraphDataContextNodeModel model, string name, TypeHandle newType)
        {
            m_Model = model;
            m_Name = name;
            m_NewType = newType;
        }

        public static void DefaultCommandHandler(
            UndoStateComponent undoState,
            GraphModelStateComponent graphModelState,
            ChangeContextEntryTypeCommand command)
        {
            using (var undoStateUpdater = undoState.UpdateScope)
            {
                undoStateUpdater.SaveSingleState(graphModelState, command);
            }

            using var graphUpdater = graphModelState.UpdateScope;

            // TODO (Joe): SG1 preserves edges, even if the type is invalid. Figure out how we can represent that.
            var model = command.m_Model;
            var oldPort = model.GetInputPortForEntry(command.m_Name);
            var oldEdges = oldPort.GetConnectedEdges().ToList();
            model.GraphModel.DeleteEdges(oldEdges);
            graphUpdater.MarkDeleted(oldEdges);

            model.RemoveEntry(command.m_Name);
            model.CreateEntry(command.m_Name, command.m_NewType);

            graphUpdater.MarkChanged(command.m_Model);
        }
    }
}
