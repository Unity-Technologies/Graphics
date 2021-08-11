using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEditor.ShaderGraph.GraphUI.GraphElements.CommandDispatch;
using UnityEngine;
using UnityEngine.GraphToolsFoundation.CommandStateObserver;

namespace UnityEditor.ShaderGraph.GraphUI.EditorCommon.CommandStateObserver
{
    public class ShaderGraphState : GraphToolState
    {
        public ShaderGraphState(Hash128 graphViewEditorWindowGUID, Preferences preferences)
            : base(graphViewEditorWindowGUID, preferences) { }

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

            // Overrides for default GTF commands
            commandDispatcher.RegisterCommandHandler<CreateEdgeCommand>(ShaderGraphCommandOverrides.HandleCreateEdge);
            commandDispatcher.RegisterCommandHandler<DeleteElementsCommand>(ShaderGraphCommandOverrides.HandleDeleteElements);
        }
    }
}
