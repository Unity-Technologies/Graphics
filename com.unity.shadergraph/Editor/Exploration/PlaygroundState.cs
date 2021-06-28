using GtfPlayground.Commands;
using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEngine;
using UnityEngine.GraphToolsFoundation.CommandStateObserver;

namespace GtfPlayground
{
    public class PlaygroundState : GraphToolState
    {
        public PlaygroundState(Hash128 graphViewEditorWindowGUID, Preferences preferences)
            : base(graphViewEditorWindowGUID, preferences) { }

        public override void RegisterCommandHandlers(Dispatcher dispatcher)
        {
            base.RegisterCommandHandlers(dispatcher);
            
            if (dispatcher is not CommandDispatcher commandDispatcher)
                return;
            
            commandDispatcher.RegisterCommandHandler<ScatterNodesCommand>(ScatterNodesCommand.DefaultCommandHandler);
            commandDispatcher.RegisterCommandHandler<GenerateGraphCommand>(GenerateGraphCommand.DefaultCommandHandler);
            commandDispatcher.RegisterCommandHandler<AddPortCommand>(AddPortCommand.DefaultCommandHandler);
            commandDispatcher.RegisterCommandHandler<RemovePortCommand>(RemovePortCommand.DefaultCommandHandler);
        }
    }
}