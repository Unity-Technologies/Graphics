using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.RMGUI;

namespace UnityEditor.Graphing.Drawing
{
    public class Commandable : Manipulator, IEnumerable<KeyValuePair<string, CommandHandler>>
    {
        private readonly Dictionary<string, CommandHandler> m_Dictionary;

        public Commandable()
        {
            m_Dictionary = new Dictionary<string, CommandHandler>();
        }

        public override EventPropagation HandleEvent(Event evt, VisualElement finalTarget)
        {
            var isValidation = evt.type == EventType.ValidateCommand;
            var isExecution = evt.type == EventType.ExecuteCommand;
            if (isValidation || isExecution)
                Debug.Log(evt.commandName);
            CommandHandler handler;
            if ((!isValidation && !isExecution) || !m_Dictionary.TryGetValue(evt.commandName, out handler))
                return EventPropagation.Continue;
            if (isValidation && handler.Validate())
                return EventPropagation.Stop;
            if (!isExecution)
                return EventPropagation.Continue;
            handler.Execute();
            return EventPropagation.Stop;
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
