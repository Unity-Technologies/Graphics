using System;
using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEngine.GraphToolsFoundation.CommandStateObserver;

namespace UnityEditor.ShaderGraph.GraphUI
{
    public class ShaderGraphCommandDispatcher : CommandDispatcher
    {
        Action<LoadGraphCommand> m_OnGraphLoadDelegate;
        Action<UndoableCommand> m_OnUndoRedoDelegate;
        public ShaderGraphCommandDispatcher(
            Action<LoadGraphCommand> onGraphLoadDelegate,
            Action<UndoableCommand> onUndoRedoDelegate)
        {
            m_OnGraphLoadDelegate = onGraphLoadDelegate;
            m_OnUndoRedoDelegate = onUndoRedoDelegate;
        }

        // Command being undone/redone
        UndoableCommand m_LastCommand;

        public override void Dispatch(ICommand command, Diagnostics diagnosticFlags = Diagnostics.None)
        {
            base.Dispatch(command, diagnosticFlags);

            PostCommandHandling(command);

            if(command is UndoableCommand undoableCommand)
                m_LastCommand = undoableCommand;
        }

        void PostCommandHandling(ICommand command)
        {
            switch (command)
            {
                case LoadGraphCommand loadGraphCommand:
                    m_OnGraphLoadDelegate.Invoke(loadGraphCommand);
                    break;
                case UndoRedoCommand undoRedoCommand:
                    m_OnUndoRedoDelegate(m_LastCommand);
                    break;
            }
        }
    }
}
