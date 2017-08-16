using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.UIElements;

namespace UnityEditor.Graphing.Drawing
{
    public class Commandable : Manipulator, IEnumerable<KeyValuePair<string, CommandHandler>>
    {
        private readonly Dictionary<string, CommandHandler> m_Dictionary;

        public Commandable()
        {
            m_Dictionary = new Dictionary<string, CommandHandler>();
        }

        public void HandleEvent(EventBase evt)
        {
            var isValidation = evt.imguiEvent.type == EventType.ValidateCommand;
            var isExecution = evt.imguiEvent.type == EventType.ExecuteCommand;
            if (isValidation || isExecution)
                Debug.Log(evt.imguiEvent.commandName);
            CommandHandler handler;
            if ((!isValidation && !isExecution) || !m_Dictionary.TryGetValue(evt.imguiEvent.commandName, out handler))
            {
                return;
            }
            if (isValidation && handler.Validate())
            {
                evt.StopPropagation();
                return;
            }
            if (!isExecution)
            {
                return;
            }
            handler.Execute();
            evt.StopPropagation();
        }

        public void Add(string commandName, CommandValidator validator, CommandExecutor executor)
        {
            m_Dictionary[commandName] = new CommandHandler(validator, executor);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public IEnumerator<KeyValuePair<string, CommandHandler>> GetEnumerator()
        {
            return m_Dictionary.GetEnumerator();
        }

        protected override void RegisterCallbacksOnTarget()
        {
            target.RegisterCallback<IMGUIEvent>(HandleEvent);
        }

        protected override void UnregisterCallbacksFromTarget()
        {
            target.UnregisterCallback<IMGUIEvent>(HandleEvent);
        }
    }

    public delegate bool CommandValidator();

    public delegate void CommandExecutor();

    public class CommandHandler
    {
        private readonly CommandValidator m_Validator;
        private readonly CommandExecutor m_Executor;

        public CommandHandler(CommandValidator validator, CommandExecutor executor)
        {
            m_Validator = validator;
            m_Executor = executor;
        }

        public bool Validate()
        {
            if (m_Validator != null)
                return m_Validator();
            return false;
        }

        public void Execute()
        {
            if (m_Executor != null)
                m_Executor();
        }
    }
}
