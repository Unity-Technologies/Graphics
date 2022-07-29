using System;
using System.Collections.Generic;
using UnityEditor.GraphToolsFoundation.Overdrive.BasicModel;
using UnityEditor.ShaderGraph.GraphDelta;
using UnityEngine;
using UnityEngine.GraphToolsFoundation.Overdrive;

namespace UnityEditor.ShaderGraph.GraphUI
{
    // Represents a configurable parameter that affects shader generation.
    // (Anything that isn't dependent on the variable type should be a regular property.)
    internal class GraphDataVariableSetting
    {
        public string Label;
        public Type SettingType;
        public Action<object> Setter;
        public Func<object> Getter;

        // Convenience methods for common data sources. Not required by any means, but do at least consider using
        // WithType to avoid ugly casting bugs with Object.

        #region Factory Methods

        public static GraphDataVariableSetting FromSubField<T>(Func<FieldHandler> fieldProvider, string label, string key, T defaultValue = default) =>
            new()
            {
                Label = label,
                SettingType = typeof(T),
                Getter = () =>
                {
                    var field = fieldProvider.Invoke();
                    var subField = field.GetSubField(key);
                    return subField != null ? subField.GetData<T>() : defaultValue;
                },
                Setter = value =>
                {
                    var field = fieldProvider.Invoke();
                    var subField = field.GetSubField(key);
                    if (subField != null)
                    {
                        subField.SetData(value);
                    }
                    else
                    {
                        field.AddSubField(key, value);
                    }
                },
            };

        // Consider including the <T> even when it can be inferred for readability.
        public static GraphDataVariableSetting WithType<T>(string label, Func<T> typedGetter, Action<T> typedSetter) =>
            new() {Label = label, SettingType = typeof(T), Getter = () => typedGetter(), Setter = value => typedSetter((T)value)};

        #endregion
    }

    public class GraphDataVariableDeclarationModel : VariableDeclarationModel
    {
        [SerializeField]
        [HideInInspector]
        string m_ContextNodeName;

        /// <summary>
        /// Name of the context node that owns the entry for this Variable
        /// </summary>
        public string contextNodeName
        {
            get => m_ContextNodeName;
            set => m_ContextNodeName = value;
        }

        [SerializeField]
        [HideInInspector]
        string m_GraphDataName;

        /// <summary>
        /// Name of the port on the Context Node that owns the entry for this Variable
        /// </summary>
        public string graphDataName
        {
            get => m_GraphDataName;
            set => m_GraphDataName = value;
        }

        ShaderGraphModel shaderGraphModel => GraphModel as ShaderGraphModel;

        public PortHandler ContextEntry => shaderGraphModel.GraphHandler
            .GetNode(contextNodeName)
            .GetPort(graphDataName);

        public ContextEntryEnumTags.DataSource ShaderDeclaration
        {
            get =>
                ContextEntry
                    .GetField<ContextEntryEnumTags.DataSource>(ContextEntryEnumTags.kDataSource)
                    .GetData();
            set =>
                ContextEntry
                    .GetField<ContextEntryEnumTags.DataSource>(ContextEntryEnumTags.kDataSource)
                    .SetData(value);
        }

        /// <summary>
        /// Returns true if this variable declaration's data type is exposable according to the stencil,
        /// false otherwise.
        /// </summary>
        public bool IsExposable => ((ShaderGraphStencil)shaderGraphModel?.Stencil)?.IsExposable(DataType) ?? false;

        public override bool IsExposed
        {
            get
            {
                if (!IsExposable)
                {
                    return false;
                }

                return ContextEntry
                    .GetField<ContextEntryEnumTags.PropertyBlockUsage>(ContextEntryEnumTags.kPropertyBlockUsage)
                    .GetData() == ContextEntryEnumTags.PropertyBlockUsage.Included;
            }
            set
            {
                if (!IsExposable)
                {
                    value = false;
                }

                ContextEntry
                    .GetField<ContextEntryEnumTags.PropertyBlockUsage>(ContextEntryEnumTags.kPropertyBlockUsage)
                    .SetData(value ? ContextEntryEnumTags.PropertyBlockUsage.Included : ContextEntryEnumTags.PropertyBlockUsage.Excluded);
            }
        }

        public override void Rename(string newName)
        {
            base.Rename(newName); // Result is assigned to Title, can be different from newName (i.e. numbers at end)
            ContextEntry.GetField<string>(ContextEntryEnumTags.kDisplayName).SetData(Title);
        }

        public override void CreateInitializationValue()
        {
            if (string.IsNullOrEmpty(contextNodeName) || string.IsNullOrEmpty(graphDataName))
            {
                return;
            }

            if (GraphModel?.Stencil?.GetConstantType(DataType) != null)
            {
                InitializationModel = GraphModel.Stencil.CreateConstantValue(DataType);
                if (InitializationModel is BaseShaderGraphConstant cldsConstant)
                {
                    cldsConstant.Initialize(shaderGraphModel, contextNodeName, graphDataName);
                }

                if (DataType == ShaderGraphExampleTypes.Matrix2 ||
                    DataType == ShaderGraphExampleTypes.Matrix3 ||
                    DataType == ShaderGraphExampleTypes.Matrix4)
                {
                    InitializationModel.ObjectValue = Matrix4x4.identity;
                }
            }
        }

        public enum ColorMode { Default, HDR }

        // We can change how exactly these get exposed. In hindsight, the only thing that has any business calling the
        // setter is probably a command handler -- and even they can go through the model.
        internal IEnumerable<GraphDataVariableSetting> GetSettings()
        {
            var props = new Func<FieldHandler>(() => ContextEntry.GetPropertyDescription());

            if (DataType == TypeHandle.Float)
            {
                var mode = GraphDataVariableSetting.FromSubField<MaterialPropertyTags.FloatMode>(props, "Mode", MaterialPropertyTags.kFloatMode);
                yield return mode;

                if (mode.Getter() is MaterialPropertyTags.FloatMode.Slider)
                {
                    yield return GraphDataVariableSetting.FromSubField<MaterialPropertyTags.FloatMode>(props, "Min", MaterialPropertyTags.kFloatSliderMin);
                    yield return GraphDataVariableSetting.FromSubField<MaterialPropertyTags.FloatMode>(props, "Max", MaterialPropertyTags.kFloatSliderMax);
                }
            }

            if (DataType == ShaderGraphExampleTypes.Color)
            {
                yield return GraphDataVariableSetting.WithType<ColorMode>(
                    label: "Mode",
                    typedGetter: () =>
                        ContextEntry.GetPropertyDescription().GetField<bool>(MaterialPropertyTags.kIsHdr) ? ColorMode.HDR : ColorMode.Default,
                    typedSetter: value =>
                        ContextEntry.GetPropertyDescription().GetSubField<bool>(MaterialPropertyTags.kIsHdr).SetData(value is ColorMode.HDR)
                );
            }

            if (DataType == ShaderGraphExampleTypes.SamplerStateTypeHandle)
            {
                yield return GraphDataVariableSetting.FromSubField<SamplerStateType.Filter>(() => ContextEntry.GetTypeField(), "Filter", SamplerStateType.kFilter);
                yield return GraphDataVariableSetting.FromSubField<SamplerStateType.Wrap>(() => ContextEntry.GetTypeField(), "Wrap", SamplerStateType.kWrap);
            }

            if (IsExposable)
            {
                yield return GraphDataVariableSetting.WithType<ContextEntryEnumTags.DataSource>(
                    label: "Shader Declaration",
                    typedGetter: () => ContextEntry
                        .GetField<ContextEntryEnumTags.DataSource>(ContextEntryEnumTags.kDataSource)
                        .GetData(),
                    typedSetter: value => ContextEntry
                        .GetField<ContextEntryEnumTags.DataSource>(ContextEntryEnumTags.kDataSource)
                        .SetData(value)
                );
            }
        }
    }
}
