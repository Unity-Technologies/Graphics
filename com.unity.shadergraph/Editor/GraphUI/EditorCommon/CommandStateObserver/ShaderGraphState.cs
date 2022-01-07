using System.Collections.Generic;
using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEditor.ShaderGraph.GraphUI.GraphElements.CommandDispatch;
using UnityEngine;
using UnityEngine.GraphToolsFoundation.CommandStateObserver;

namespace UnityEditor.ShaderGraph.GraphUI.EditorCommon.CommandStateObserver
{
    public class ShaderGraphState : GraphToolState
    {
        public ShaderGraphState(Hash128 graphViewEditorWindowGUID, Preferences preferences)
            : base(graphViewEditorWindowGUID, preferences)
        {
        }

        /// <summary>
        /// The graph preview state component. Holds data related to node and graph previews.
        /// </summary>
        public GraphPreviewStateComponent GraphPreviewState =>
            m_GraphPreviewStateComponent ??= PersistedState.GetOrCreateViewStateComponent<GraphPreviewStateComponent>(m_GraphViewEditorWindowGUID, nameof(GraphPreviewState));

        GraphPreviewStateComponent m_GraphPreviewStateComponent;

        public override IEnumerable<IStateComponent> AllStateComponents
        {
            get
            {
                yield return BlackboardViewState;
                yield return WindowState;
                yield return GraphViewState;
                yield return SelectionState;
                yield return TracingStatusState;
                yield return TracingControlState;
                yield return TracingDataState;
                yield return GraphProcessingState;
                yield return ModelInspectorState;
                yield return GraphPreviewState;
            }
        }

        public override void RegisterCommandHandlers(Dispatcher dispatcher)
        {
            base.RegisterCommandHandlers(dispatcher);

            if (dispatcher is not CommandDispatcher commandDispatcher)
                return;

            // TODO: Instead of having this be a monolithic list of commands all gathered here, can we can break them up into being registered by individual controllers?
            // Demo commands (TODO: Remove)
            commandDispatcher.RegisterCommandHandler<AddPortCommand>(AddPortCommand.DefaultCommandHandler);
            commandDispatcher.RegisterCommandHandler<RemovePortCommand>(RemovePortCommand.DefaultCommandHandler);

            // Shader Graph commands
            commandDispatcher.RegisterCommandHandler<AddRedirectNodeCommand>(AddRedirectNodeCommand.DefaultHandler);
          	commandDispatcher.RegisterCommandHandler<ChangePreviewExpandedCommand>(ChangePreviewExpandedCommand.DefaultCommandHandler);
            commandDispatcher.RegisterCommandHandler<ChangePreviewModeCommand>(ChangePreviewModeCommand.DefaultCommandHandler);
            commandDispatcher.RegisterCommandHandler<GraphWindowTickCommand>(GraphWindowTickCommand.DefaultCommandHandler);

            // Overrides for default GTF commands
            commandDispatcher.RegisterCommandHandler<CreateEdgeCommand>(ShaderGraphCommandOverrides.HandleCreateEdge);
            commandDispatcher.RegisterCommandHandler<DeleteElementsCommand>(ShaderGraphCommandOverrides.HandleDeleteElements);
            commandDispatcher.RegisterCommandHandler<BypassNodesCommand>(ShaderGraphCommandOverrides.HandleBypassNodes);
            commandDispatcher.RegisterCommandHandler<RenameElementCommand>(ShaderGraphCommandOverrides.HandleGraphElementRenamed);
            commandDispatcher.RegisterCommandHandler<ChangeNodeStateCommand>(ShaderGraphCommandOverrides.HandleNodeStateChanged);
            commandDispatcher.RegisterCommandHandler<UpdateConstantValueCommand>(ShaderGraphCommandOverrides.HandleUpdateConstantValue);
        }
    }
}
