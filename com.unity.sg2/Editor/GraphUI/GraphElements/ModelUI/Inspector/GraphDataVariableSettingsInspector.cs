using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEngine;
using UnityEngine.GraphToolsFoundation.CommandStateObserver;

namespace UnityEditor.ShaderGraph.GraphUI
{
    class GraphDataVariableSettingField<T> : SGModelPropertyField<T>
    {
        public GraphDataVariableSettingField(
            ICommandTarget commandTarget,
            GraphDataVariableDeclarationModel model,
            VariableSetting s
        )
            : base(commandTarget, model, null, s.Label, null,
                (newValue, field) => field.CommandTarget.Dispatch(new SetVariableSettingCommand(model, s, newValue)),
                _ => (T)s.GetAsObject(model)) { }
    }

    public class GraphDataVariableSettingsInspector : SGFieldsInspector
    {
        GraphDataVariableDeclarationModel graphDataModel => (GraphDataVariableDeclarationModel)m_Model;

        public GraphDataVariableSettingsInspector(string name, IModel model, IModelView ownerElement, string parentClassName)
            : base(name, model, ownerElement, parentClassName) { }

        BaseModelPropertyField MakeSettingField(VariableSetting s)
        {
            var fieldTypeParam = s.SettingType;
            var fieldType = typeof(GraphDataVariableSettingField<>).MakeGenericType(fieldTypeParam);
            return (BaseModelPropertyField)Activator.CreateInstance(fieldType, m_OwnerElement.RootView, m_Model, s);
        }

        protected override IEnumerable<BaseModelPropertyField> GetFields()
        {
            // This is fine until we reach things like Keywords that need to show something complicated like
            // reorderable lists. In that case, check for the type and draw the right fields manually.
            return graphDataModel.GetSettings().Select(MakeSettingField);
        }

        public override bool IsEmpty() => false;
    }
}
