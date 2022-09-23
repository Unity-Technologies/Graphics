using System;
using System.Collections.Generic;
using System.Linq;
using Unity.GraphToolsFoundation.Editor;
using UnityEngine;
using Unity.CommandStateObserver;

namespace UnityEditor.ShaderGraph.GraphUI
{
    class GraphDataVariableSettingField<T> : SGModelPropertyField<T>
    {
        // TODO GTF UPGRADE: support edition of multiple models.

        public GraphDataVariableSettingField(
            ICommandTarget commandTarget,
            IEnumerable<GraphDataVariableDeclarationModel> models,
            VariableSetting s
        )
            : base(commandTarget, models, null, s.Label, null,
                (newValue, field) => field.CommandTarget.Dispatch(new SetVariableSettingCommand(models.First(), s, newValue)),
                _ => (T)s.GetAsObject(models.First())) { }
    }

    class GraphDataVariableSettingsInspector : SGFieldsInspector
    {
        IEnumerable<GraphDataVariableDeclarationModel> graphDataModel => m_Models.OfType<GraphDataVariableDeclarationModel>();

        public GraphDataVariableSettingsInspector(string name, IEnumerable<Model> models, RootView rootView, string parentClassName)
            : base(name, models, rootView, parentClassName) { }

        BaseModelPropertyField MakeSettingField(VariableSetting s)
        {
            var fieldTypeParam = s.SettingType;
            var fieldType = typeof(GraphDataVariableSettingField<>).MakeGenericType(fieldTypeParam);
            return (BaseModelPropertyField)Activator.CreateInstance(fieldType, RootView, m_Models, s);
        }

        protected override IEnumerable<BaseModelPropertyField> GetFields()
        {
            // This is fine until we reach things like Keywords that need to show something complicated like
            // reorderable lists. In that case, check for the type and draw the right fields manually.
            return graphDataModel.First().GetSettings().Select(MakeSettingField);
        }

        public override bool IsEmpty() => !GetFields().Any();
    }
}
