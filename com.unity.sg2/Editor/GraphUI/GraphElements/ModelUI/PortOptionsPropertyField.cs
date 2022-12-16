using System;
using System.Collections.Generic;
using System.Linq;
using Unity.CommandStateObserver;
using Unity.GraphToolsFoundation.Editor;
using UnityEditor.ShaderGraph.Defs;
using UnityEngine.UIElements;

namespace UnityEditor.ShaderGraph.GraphUI
{
    class PortOptionsPropertyField : BaseModelPropertyField
    {
        readonly DropdownField m_DropdownField;
        readonly string m_PortName;
        readonly SGNodeModel m_NodeModel;

        public PortOptionsPropertyField(ICommandTarget commandTarget, SGNodeModel nodeModel, string portName, IReadOnlyList<(string, object)> options)
            : base(commandTarget)
        {
            m_NodeModel = nodeModel;
            m_PortName = portName;

            m_DropdownField = new DropdownField();
            m_DropdownField.choices = options.Select(t => t.Item1).ToList();
            m_DropdownField.index = 0;
            m_DropdownField.RegisterValueChangedCallback(e =>
            {
                CommandTarget.Dispatch(new SetPortOptionCommand(m_NodeModel, m_PortName, m_DropdownField.index));
            });

            Add(m_DropdownField);
        }

        public override void UpdateDisplayedValue()
        {
            var desc = m_NodeModel.GetCurrentPortOption(m_PortName);
            if (desc < 0) return;

            m_DropdownField.SetValueWithoutNotify(m_DropdownField.choices[desc]);
        }
    }
}
