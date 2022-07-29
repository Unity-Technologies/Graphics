using System;
using UnityEditor.ShaderGraph.GraphDelta;

namespace UnityEditor.ShaderGraph.GraphUI
{
    /// <summary>
    /// VariableSettings are used to expose configuration of variable declaration models in the UI.
    /// </summary>
    abstract class VariableSetting
    {
        public string Label { get; }
        public abstract Type SettingType { get; }

        public abstract object Get(GraphDataVariableDeclarationModel model);
        public abstract void Set(GraphDataVariableDeclarationModel model, object value);

        protected VariableSetting(string label) { Label = label; }
    }

    class VariableSetting<T> : VariableSetting
    {
        public override Type SettingType => typeof(T);

        Func<GraphDataVariableDeclarationModel, T> m_Getter;
        Action<GraphDataVariableDeclarationModel, T> m_Setter;

        public override object Get(GraphDataVariableDeclarationModel model) => GetTyped(model);
        public override void Set(GraphDataVariableDeclarationModel model, object value) => SetTyped(model, (T)value);

        public T GetTyped(GraphDataVariableDeclarationModel model) => m_Getter(model);
        public void SetTyped(GraphDataVariableDeclarationModel model, T value) => m_Setter(model, value);

        public VariableSetting(string label, Func<GraphDataVariableDeclarationModel, T> getter, Action<GraphDataVariableDeclarationModel, T> setter)
            : base(label)
        {
            m_Getter = getter;
            m_Setter = setter;
        }
    }

    static class VariableSettings
    {
        public static readonly VariableSetting<MaterialPropertyTags.FloatMode> floatMode =
            CreateFromSubField<MaterialPropertyTags.FloatMode>(
                model => model.ContextEntry.GetPropertyDescription(),
                MaterialPropertyTags.kFloatMode,
                "Mode");
        public static readonly VariableSetting<float> rangeMin =
            CreateFromSubField(
                model => model.ContextEntry.GetPropertyDescription(),
                MaterialPropertyTags.kRangeMin,
                "Min",
                0.0f);
        public static readonly VariableSetting<float> rangeMax =
            CreateFromSubField(
                model => model.ContextEntry.GetPropertyDescription(),
                MaterialPropertyTags.kRangeMax,
                "Max",
                1.0f);

        public enum ColorMode { Default, HDR }

        public static readonly VariableSetting<ColorMode> colorMode = new(
            label: "Mode",
            getter: model =>
            {
                var isHdr = model.ContextEntry.GetPropertyDescription()?.GetSubField<bool>(MaterialPropertyTags.kIsHdr)?.GetData() ?? false;
                return isHdr ? ColorMode.HDR : ColorMode.Default;
            },
            setter: (model, value) =>
            {
                var propDescription = model.ContextEntry.GetPropertyDescription();
                var field = propDescription.GetSubField<bool>(MaterialPropertyTags.kIsHdr) ??
                    propDescription.AddSubField(MaterialPropertyTags.kIsHdr, false);
                field.SetData(value is ColorMode.HDR);
            });

        public static readonly VariableSetting samplerStateFilter =
            CreateFromSubField<SamplerStateType.Filter>(
                model => model.ContextEntry.GetTypeField(),
                SamplerStateType.kFilter,
                "Filter");
        public static readonly VariableSetting samplerStateWrap =
            CreateFromSubField<SamplerStateType.Wrap>(
                model => model.ContextEntry.GetTypeField(),
                SamplerStateType.kWrap,
                "Wrap");

        public static readonly VariableSetting<ContextEntryEnumTags.DataSource> shaderDeclaration = new(
            label: "Shader Declaration",
            getter: model => model.ContextEntry
                .GetField<ContextEntryEnumTags.DataSource>(ContextEntryEnumTags.kDataSource)
                .GetData(),
            setter: (model, value) => model.ContextEntry
                .GetField<ContextEntryEnumTags.DataSource>(ContextEntryEnumTags.kDataSource)
                .SetData(value));

        static VariableSetting<T> CreateFromSubField<T>(
            Func<GraphDataVariableDeclarationModel, FieldHandler> fieldProvider,
            string key,
            string label,
            T defaultValue = default)
        {
            return new VariableSetting<T>(
                label: label,
                getter: model =>
                {
                    var field = fieldProvider.Invoke(model);
                    var subField = field.GetSubField<T>(key);
                    return subField != null ? subField.GetData() : defaultValue;
                },
                setter: (model, value) =>
                {
                    var field = fieldProvider.Invoke(model);
                    var subField = field.GetSubField<T>(key);
                    if (subField != null)
                    {
                        subField.SetData(value);
                    }
                    else
                    {
                        field.AddSubField(key, value);
                    }
                });
        }
    }
}
