using System.Collections.Generic;
using System.Linq;
using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEditor.ShaderGraph.GraphUI.DataModel;
using UnityEngine.GraphToolsFoundation.CommandStateObserver;

namespace UnityEditor.ShaderGraph.GraphUI.GraphElements.CommandDispatch
{
    public class RemovePortCommand : ModelCommand<CustomizableNodeModel>
    {
        bool m_Output;
        string m_Name;

        public RemovePortCommand(bool output, string name, IReadOnlyList<CustomizableNodeModel> models)
            : base(
                "Remove Port", "Remove Ports", models)
        {
            m_Output = output;
            m_Name = name;
        }

        public static void DefaultCommandHandler(GraphToolState graphToolState, RemovePortCommand command)
        {
            graphToolState.PushUndo(command);

            using var graphUpdater = graphToolState.GraphViewState.UpdateScope;
            foreach (var nodeModel in command.Models)
            {
                var removedPort =
                    (command.m_Output ? nodeModel.GetOutputPorts() : nodeModel.GetInputPorts()).FirstOrDefault(p =>
                        p.UniqueName == command.m_Name);

                if (removedPort == null) continue;

                var edgesToDelete = removedPort.GetConnectedEdges().ToList();
                foreach (var connectedEdge in edgesToDelete)
                {
                    graphUpdater.MarkDeleted(connectedEdge);
                    graphToolState.GraphViewState.GraphModel.DeleteEdge(connectedEdge);
                }

                nodeModel.RemovePortByName(command.m_Name, command.m_Output);
                graphUpdater.MarkChanged(nodeModel);
            }
        }
    }
}
