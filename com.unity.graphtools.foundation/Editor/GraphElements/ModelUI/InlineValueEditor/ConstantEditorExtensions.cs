using System;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.GraphToolsFoundation.Overdrive;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// Extension methods to build value editors for constants.
    /// </summary>
    [GraphElementsExtensionMethodsCache(typeof(GraphView), GraphElementsExtensionMethodsCacheAttribute.lowestPriority)]
    public static class ConstantEditorExtensions
    {
        public static readonly string ussClassName = "ge-inline-value-editor";

        public static VisualElement BuildInlineValueEditor<T>(object oldValue, BaseField<T> field, Action<IChangeEvent> valueChangedCallback)
        {
            var root = new VisualElement();

            root.AddStylesheet("InlineValueEditor.uss");
            // TODO VladN: fix for light skin, remove when GTF supports light skin
            if (!EditorGUIUtility.isProSkin)
                root.AddStylesheet("InlineValueEditor_lightFix.uss");

            root.AddToClassList(ussClassName);
            //Mimic UIElement property fields style
            root.AddToClassList(PropertyField.ussClassName);

            field.value = (T)oldValue;
            root.Add(field);
            field.RegisterValueChangedCallback(evt => valueChangedCallback(evt));
            return root;
        }

        static VisualElement BuildSearcherEnumEditor(EnumValueReference enumConstant, Type enumType, Action<Enum> onNewEnumValue)
        {
            var value = enumConstant.ValueAsEnum();
            var enumEditor = new Button { text = value.ToString() };
            enumEditor.clickable.clickedWithEventInfo += e =>
            {
                SearcherService.ShowEnumValues("Pick a value", enumType, e.originalMousePosition, (v, i) =>
                {
                    enumEditor.text = v.ToString();
                    onNewEnumValue(v);
                });
            };
            return enumEditor;
        }

        static VisualElement BuildEnumFieldEditor(EnumValueReference enumConstant, Action<Enum> onNewEnumValue)
        {
            var enumEditor = new EnumField(enumConstant.ValueAsEnum());
            enumEditor.RegisterValueChangedCallback(evt =>
            {
                onNewEnumValue(evt.newValue);
            });
            return enumEditor;
        }

        static VisualElement BuildEnumEditor(IConstantEditorBuilder builder, EnumValueReference enumConstant)
        {
            void TriggerOnValueChange(Enum newEnumValue)
            {
                var oldValue = enumConstant;
                var newValue = new EnumValueReference(newEnumValue);
                using (var evt = ChangeEvent<EnumValueReference>.GetPooled(oldValue, newValue))
                    builder.OnValueChanged(evt);
            }

            Type enumType = enumConstant.EnumType.Resolve();
            VisualElement editor = enumType == typeof(KeyCode)
                ? BuildSearcherEnumEditor(enumConstant, enumType, TriggerOnValueChange)
                : BuildEnumFieldEditor(enumConstant, TriggerOnValueChange);

            editor?.SetEnabled(!builder.ConstantIsLocked);
            return editor;
        }

        public static VisualElement BuildDefaultConstantEditor(this IConstantEditorBuilder builder, IConstant constant)
        {
            if (constant.Type == typeof(float))
                return BuildInlineValueEditor(constant.ObjectValue, new FloatField(), builder.OnValueChanged);

            if (constant.Type == typeof(double))
                return BuildInlineValueEditor(constant.ObjectValue, new DoubleField(), builder.OnValueChanged);

            if (constant.Type == typeof(int))
                return BuildInlineValueEditor(constant.ObjectValue, new IntegerField(), builder.OnValueChanged);

            if (constant.Type == typeof(long))
                return BuildInlineValueEditor(constant.ObjectValue, new LongField(), builder.OnValueChanged);

            if (constant.Type == typeof(bool))
                return BuildInlineValueEditor(constant.ObjectValue, new Toggle(), builder.OnValueChanged);

            if (constant.Type == typeof(string))
                return BuildInlineValueEditor(constant.ObjectValue, new TextField(), builder.OnValueChanged);

            if (constant.Type == typeof(Color))
                return BuildInlineValueEditor(constant.ObjectValue, new ColorField(), builder.OnValueChanged);

            if (constant.Type == typeof(Vector2))
                return BuildInlineValueEditor(constant.ObjectValue, new Vector2Field(), builder.OnValueChanged);

            if (constant.Type == typeof(Vector3))
                return BuildInlineValueEditor(constant.ObjectValue, new Vector3Field(), builder.OnValueChanged);

            if (constant.Type == typeof(Vector4))
                return BuildInlineValueEditor(constant.ObjectValue, new Vector4Field(), builder.OnValueChanged);

            if (constant.Type == typeof(GameObject))
                return BuildInlineValueEditor(constant.ObjectValue,
                    new ObjectField { allowSceneObjects = true, objectType = constant.Type },
                    builder.OnValueChanged);

            if (typeof(Object).IsAssignableFrom(constant.Type))
                return BuildInlineValueEditor(constant.ObjectValue,
                    new ObjectField { allowSceneObjects = false, objectType = constant.Type },
                    builder.OnValueChanged);

            if (constant.Type == typeof(EnumValueReference))
                return BuildEnumEditor(builder, (EnumValueReference)constant.ObjectValue);

            return null;
        }
    }
}
