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

            commandDispatcher.RegisterCommandHandler<AddPortCommand>(AddPortCommand.DefaultCommandHandler);
            commandDispatcher.RegisterCommandHandler<RemovePortCommand>(RemovePortCommand.DefaultCommandHandler);
        }
    }
}
