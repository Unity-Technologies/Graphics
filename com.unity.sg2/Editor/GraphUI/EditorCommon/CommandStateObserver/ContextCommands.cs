using System.Linq;
using Unity.GraphToolsFoundation.Editor;
using UnityEngine;
using Unity.CommandStateObserver;
using Unity.GraphToolsFoundation;

namespace UnityEditor.ShaderGraph.GraphUI
{
    class AddContextEntryCommand : UndoableCommand
    {
        readonly SGContextNodeModel m_Model;
        readonly string m_Name;
        readonly TypeHandle m_Type;

        public AddContextEntryCommand(SGContextNodeModel model, string name, TypeHandle type)
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
                undoStateUpdater.SaveState(graphModelState);
            }

            using var graphUpdater = graphModelState.UpdateScope;
            command.m_Model.CreateEntry(command.m_Name, command.m_Type);
            graphUpdater.MarkChanged(command.m_Model);
        }
    }

    class RemoveContextEntryCommand : UndoableCommand
    {
        readonly SGContextNodeModel m_Model;
        readonly string m_Name;

        public RemoveContextEntryCommand(SGContextNodeModel model, string name)
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
                undoStateUpdater.SaveState(graphModelState);
            }

            using var graphUpdater = graphModelState.UpdateScope;

            var model = command.m_Model;
            var oldPort = model.GetInputPortForEntry(command.m_Name);
            var oldEdges = oldPort.GetConnectedWires().ToList();
            model.GraphModel.DeleteWires(oldEdges);
            graphUpdater.MarkDeleted(oldEdges);

            command.m_Model.RemoveEntry(command.m_Name);
            graphUpdater.MarkChanged(command.m_Model);
        }
    }

    class RenameContextEntryCommand : UndoableCommand
    {
        readonly SGContextNodeModel m_Model;
        readonly string m_OldName;
        readonly string m_NewName;

        public RenameContextEntryCommand(SGContextNodeModel model, string oldName, string newName)
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
                undoStateUpdater.SaveState(graphModelState);
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
            foreach (var edge in oldPort.GetConnectedWires().ToList())
            {
                edge.ToPort = newPort;
                graphUpdater.MarkChanged(edge);
            }

            model.RemoveEntry(command.m_OldName);
            graphUpdater.MarkChanged(command.m_Model);
        }
    }

    class ChangeContextEntryTypeCommand : UndoableCommand
    {
        readonly SGContextNodeModel m_Model;
        readonly string m_Name;
        readonly TypeHandle m_NewType;

        public ChangeContextEntryTypeCommand(SGContextNodeModel model, string name, TypeHandle newType)
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
                undoStateUpdater.SaveState(graphModelState);
            }

            using var graphUpdater = graphModelState.UpdateScope;

            // TODO (Joe): SG1 preserves edges, even if the type is invalid. Figure out how we can represent that.
            var model = command.m_Model;
            var oldPort = model.GetInputPortForEntry(command.m_Name);
            var oldEdges = oldPort.GetConnectedWires().ToList();
            model.GraphModel.DeleteWires(oldEdges);
            graphUpdater.MarkDeleted(oldEdges);

            model.RemoveEntry(command.m_Name);
            model.CreateEntry(command.m_Name, command.m_NewType);

            graphUpdater.MarkChanged(command.m_Model);
        }
    }
}
