using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEditor.ShaderGraph.GraphDelta;
using UnityEngine;
using UnityEngine.GraphToolsFoundation.Overdrive;
using static UnityEditor.ShaderGraph.GraphDelta.MaterialPropertyTags;

namespace UnityEditor.ShaderGraph.GraphUI
{
    enum ColorMode
    {
        Default,
        HDR
    }

    public class GraphDataVariableSettingsInspector : SGFieldsInspector
    {
        GraphDataVariableDeclarationModel graphDataModel => (GraphDataVariableDeclarationModel)m_Model;

        public GraphDataVariableSettingsInspector(string name, IModel model, IModelView ownerElement, string parentClassName)
            : base(name, model, ownerElement, parentClassName) { }

        BaseModelPropertyField MakePropertyDescriptionField<T>(string label, string key, T defaultValue = default)
        {
            return new SGModelPropertyField<T>(
                m_OwnerElement.RootView,
                m_Model,
                null,
                label,
                null,
                (value, field) =>
                {
                    var setCommand = SetVariableSettingsCommand.SetPropertyDescriptionSubField(graphDataModel, key, value);
                    field.CommandTarget.Dispatch(setCommand);
                },
                _ =>
                {
                    var subField = graphDataModel.ContextEntry
                        .GetPropertyDescription()
                        .GetSubField<T>(key);

                    return subField != null ? subField.GetData() : defaultValue;
                }
            );
        }

        BaseModelPropertyField MakeTypeField<T>(string label, string key, T defaultValue = default)
        {
            return new SGModelPropertyField<T>(
                m_OwnerElement.RootView,
                m_Model,
                null,
                label,
                null,
                (value, field) =>
                {
                    var setCommand = SetVariableSettingsCommand.SetTypeSubField(graphDataModel, key, value);
                    field.CommandTarget.Dispatch(setCommand);
                },
                _ =>
                {
                    var subField = graphDataModel.ContextEntry
                        .GetPropertyDescription()
                        .GetSubField<T>(key);

                    return subField != null ? subField.GetData() : defaultValue;
                }
            );
        }

        protected override IEnumerable<BaseModelPropertyField> GetFields()
        {
            if (graphDataModel.DataType == TypeHandle.Float)
            {
                foreach (var f in BuildFloatFields()) { yield return f; }
            }

            if (graphDataModel.DataType == ShaderGraphExampleTypes.Color)
            {
                foreach (var f in BuildColorFields()) { yield return f; }
            }

            if (graphDataModel.DataType == ShaderGraphExampleTypes.SamplerStateTypeHandle)
            {
                foreach (var f in BuildSamplerStateFields()) { yield return f; }
            }

            foreach (var f in BuildCommonFields(graphDataModel)) { yield return f; }
        }

        public override bool IsEmpty() => !GetFields().Any();

        IEnumerable<BaseModelPropertyField> BuildCommonFields(GraphDataVariableDeclarationModel variableDeclarationModel)
        {
            if (!variableDeclarationModel.IsExposable) yield break;

            yield return new SGModelPropertyField<ContextEntryEnumTags.DataSource>(
                m_OwnerElement.RootView,
                m_Model,
                null,
                "Shader Declaration",
                null,
                (value, field) =>
                {
                    var setCommand = new SetVariableSettingsCommand(graphDataModel, (m) => m.ShaderDeclaration = value);
                    field.CommandTarget.Dispatch(setCommand);
                },
                _ => graphDataModel.ShaderDeclaration
            );
        }

        IEnumerable<BaseModelPropertyField> BuildFloatFields()
        {
            yield return MakePropertyDescriptionField("Mode", kFloatMode, FloatMode.Default);

            var floatMode = graphDataModel.GetPropSubFieldOrDefault(kFloatMode, FloatMode.Default);
            if (floatMode is FloatMode.Slider)
            {
                yield return MakePropertyDescriptionField("Min", kFloatSliderMin, 0.0f);
                yield return MakePropertyDescriptionField("Max", kFloatSliderMax, 1.0f);
            }
        }

        IEnumerable<BaseModelPropertyField> BuildColorFields()
        {
            yield return new SGModelPropertyField<ColorMode>(
                m_OwnerElement.RootView,
                m_Model,
                null,
                "Mode",
                null,
                (value, field) =>
                {
                    var setCommand = SetVariableSettingsCommand.SetPropertyDescriptionSubField(graphDataModel, kIsHdr, value == ColorMode.HDR);
                    field.CommandTarget.Dispatch(setCommand);
                },
                _ =>
                    graphDataModel.GetPropSubFieldOrDefault<bool>(kIsHdr) ? ColorMode.HDR : ColorMode.Default
            );
        }

        IEnumerable<BaseModelPropertyField> BuildSamplerStateFields()
        {
            yield return MakeTypeField<SamplerStateType.Filter>("Filter", SamplerStateType.kFilter);
            yield return MakeTypeField<SamplerStateType.Wrap>("Wrap", SamplerStateType.kWrap);
        }
    }
}
