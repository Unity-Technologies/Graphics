using System;
using UnityEditor.ShaderGraph.GraphDelta;
using static UnityEditor.ShaderGraph.GraphDelta.ContextEntryEnumTags;

namespace UnityEditor.ShaderGraph.GraphUI
{
    /// <summary>
    /// VariableSettings are used to expose configuration of variable declaration models in the UI.
    /// </summary>
    abstract class VariableSetting
    {
        public string Label { get; }
        public abstract Type SettingType { get; }

        public abstract object GetAsObject(GraphDataVariableDeclarationModel model);
        public abstract void SetAsObject(GraphDataVariableDeclarationModel model, object value);

        protected VariableSetting(string label) { Label = label; }
    }

    /// <summary>
    /// A typed VariableSetting that should be used wherever possible to avoid issues with casting to/from Object.
    /// The untyped base class is primarily for use in collections and commands.
    /// </summary>
    /// <typeparam name="T">Type that the model uses to represent this setting.</typeparam>
    class VariableSetting<T> : VariableSetting
    {
        public override Type SettingType => typeof(T);

        Func<GraphDataVariableDeclarationModel, T> m_Getter;
        Action<GraphDataVariableDeclarationModel, T> m_Setter;

        public override object GetAsObject(GraphDataVariableDeclarationModel model) => GetTyped(model);
        public override void SetAsObject(GraphDataVariableDeclarationModel model, object value) => SetTyped(model, (T)value);

        public T GetTyped(GraphDataVariableDeclarationModel model) => m_Getter(model);
        public void SetTyped(GraphDataVariableDeclarationModel model, T value) => m_Setter(model, value);

        public VariableSetting(string label, Func<GraphDataVariableDeclarationModel, T> getter, Action<GraphDataVariableDeclarationModel, T> setter)
            : base(label)
        {
            m_Getter = getter;
            m_Setter = setter;
        }
    }

    /// <summary>
    /// VariableSettings contains a number of well-known settings for variables of various types. If you know a setting
    /// is valid for a variable, you can use its handle here to retrieve its value.
    /// </summary>
    static class VariableSettings
    {
        #region Float

        public static readonly VariableSetting<FloatDisplayType> floatMode =
            CreateFromContextEntryTag<FloatDisplayType>(kFloatDisplayType, "Mode");

        public static readonly VariableSetting<float> rangeMin =
            CreateFromContextEntryTag(kFloatRangeMin, "Min", defaultValue: 0.0f);

        public static readonly VariableSetting<float> rangeMax =
            CreateFromContextEntryTag(kFloatRangeMax, "Max", defaultValue: 1.0f);

        #endregion

        #region Color

        public enum ColorMode { Default, HDR }

        public static readonly VariableSetting<ColorMode> colorMode = new(
            label: "Mode",
            getter: model =>
            {
                var isHdr = model.ContextEntry.GetField<bool>(kIsHdr)?.GetData() ?? false;
                return isHdr ? ColorMode.HDR : ColorMode.Default;
            },
            setter: (model, value) =>
            {
                var field = model.ContextEntry.GetField<bool>(kIsHdr) ??
                    model.ContextEntry.AddField(kIsHdr, false);

                field.SetData(value is ColorMode.HDR);
            });

        #endregion

        #region Sampler State

        public static readonly VariableSetting<SamplerStateType.Filter> samplerStateFilter =
            CreateFromSubField<SamplerStateType.Filter>(model => model.ContextEntry.GetTypeField(), SamplerStateType.kFilter, "Filter");

        public static readonly VariableSetting<SamplerStateType.Wrap> samplerStateWrap =
            CreateFromSubField<SamplerStateType.Wrap>(model => model.ContextEntry.GetTypeField(), SamplerStateType.kWrap, "Wrap");

        #endregion

        #region Shared

        public static readonly VariableSetting<DataSource> shaderDeclaration =
            CreateFromContextEntryTag<DataSource>(kDataSource, "Shader Declaration");

        #endregion

        static VariableSetting<T> CreateFromContextEntryTag<T>(string key, string label, T defaultValue = default) =>
            new(
                label: label,
                getter: model =>
                {
                    var field = model.ContextEntry.GetField<T>(key);
                    return field == null ? defaultValue : field.GetData();
                },
                setter: (model, value) =>
                {
                    (model.ContextEntry.GetField<T>(key) ?? model.ContextEntry.AddField<T>(key)).SetData(value);
                });

        static VariableSetting<T> CreateFromSubField<T>(
            Func<GraphDataVariableDeclarationModel, FieldHandler> fieldProvider,
            string key,
            string label,
            T defaultValue = default) =>
            new(
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
