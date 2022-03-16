using System;
// ReSharper disable once RedundantUsingDirective : needed by 2020.3
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.GraphToolsFoundation.CommandStateObserver;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Samples.Recipes
{
    /// <summary>
    /// Example implementation of a custom property field.
    /// </summary>
    public class TemperatureCustomPropertyField : ICustomPropertyFieldBuilder<Temperature>
    {
        VisualElement m_Container;
        IntegerField m_ValueField;
        EnumField m_UnitField;

        /// <inheritdoc />
        public bool UpdateDisplayedValue(Temperature value)
        {
            if (m_ValueField != null)
            {
                m_ValueField.SetValueWithoutNotify(value.Value);
                m_UnitField.SetValueWithoutNotify(value.Unit);
                return true;
            }

            return false;
        }

        void OnValueChanged(ChangeEvent<int> e)
        {
            var oldT = new Temperature();
            oldT.Value = e.previousValue;
            oldT.Unit = (TemperatureUnit)m_UnitField.value;

            var newT = new Temperature();
            newT.Value = e.newValue;
            newT.Unit = (TemperatureUnit)m_UnitField.value;

            using (var ee = ChangeEvent<Temperature>.GetPooled(oldT, newT))
            {
                ee.target = m_Container;
                m_Container.SendEvent(ee);
            }

            e.StopPropagation();
        }

        void OnUnitChanged(ChangeEvent<Enum> e)
        {
            var oldT = new Temperature();
            oldT.Value = m_ValueField.value;
            oldT.Unit = (TemperatureUnit)e.previousValue;

            var newT = new Temperature();
            newT.Value = m_ValueField.value;
            newT.Unit = (TemperatureUnit)e.newValue;

            using (var ee = ChangeEvent<Temperature>.GetPooled(oldT, newT))
            {
                ee.target = m_Container;
                m_Container.SendEvent(ee);
            }

            e.StopPropagation();
        }

        /// <inheritdoc />
        public VisualElement Build(ICommandTarget commandTargetView, string label, string fieldTooltip, object obj, string propertyName)
        {
            if (obj.GetType() == typeof(BakeNodeModel) && propertyName == BakeNodeModel.TemperatureFieldName)
            {
                m_Container = new VisualElement { tooltip = fieldTooltip };
                m_ValueField = new IntegerField { isDelayed = true };
                m_ValueField.RegisterCallback<ChangeEvent<int>>(OnValueChanged);
                m_ValueField.label = "Temperature";

                m_UnitField = new EnumField(TemperatureUnit.Celsius);
                m_UnitField.RegisterCallback<ChangeEvent<Enum>>(OnUnitChanged);
                m_UnitField.label = " ";

                m_Container.Add(m_ValueField);
                m_Container.Add(m_UnitField);
                return m_Container;
            }

            return null;
        }
    }
}
