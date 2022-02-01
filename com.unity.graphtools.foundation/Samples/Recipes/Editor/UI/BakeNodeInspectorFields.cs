using System;
using System.Collections.Generic;
using UnityEngine;
namespace UnityEditor.GraphToolsFoundation.Overdrive.Samples.Recipes
{
    class BakeNodeInspectorFields : FieldsInspector
    {
        public static BakeNodeInspectorFields Create(string name, IGraphElementModel model, IModelUI ownerElement, string parentClassName)
        {
            if (model is BakeNodeModel)
            {
                return new BakeNodeInspectorFields(name, model, ownerElement, parentClassName);
            }

            return null;
        }

        BakeNodeInspectorFields(string name, IGraphElementModel model, IModelUI ownerElement, string parentClassName)
            : base(name, model, ownerElement, parentClassName) {}

        protected override IEnumerable<BaseModelPropertyField> GetFields()
        {
            if (m_Model is BakeNodeModel bakeNodeModel)
            {
                yield return new ModelPropertyField<int>(
                    m_OwnerElement.View,
                    bakeNodeModel,
                    nameof(BakeNodeModel.Temperature),
                    nameof(BakeNodeModel.Temperature) + " (C)",
                    typeof(SetTemperatureCommand));

                yield return new ModelPropertyField<int>(
                    m_OwnerElement.View,
                    bakeNodeModel,
                    nameof(BakeNodeModel.Duration),
                    null,
                    typeof(SetDurationCommand));
            }
        }
    }
}
