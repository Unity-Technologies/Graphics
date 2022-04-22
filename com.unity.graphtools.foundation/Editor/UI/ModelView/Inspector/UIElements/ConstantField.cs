using System;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.GraphToolsFoundation.CommandStateObserver;
using UnityEngine.GraphToolsFoundation.Overdrive;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// A field to edit the value of an <see cref="IConstant"/>.
    /// </summary>
    public class ConstantField : CustomizableModelPropertyField
    {
        public static readonly string connectedModifierUssClassName = ussClassName.WithUssModifier("connected");

        ICustomPropertyFieldBuilder m_CustomFieldBuilder;

        /// <summary>
        /// The root element of the UI.
        /// </summary>
        protected VisualElement m_Field;

        /// <summary>
        /// The constant edited by the field.
        /// </summary>
        public IConstant ConstantModel { get; }

        /// <summary>
        /// The owner of the <see cref="ConstantModel"/>.
        /// </summary>
        /// <remarks>The owner will be passed to the command that is dispatched when the constant value is modified,
        /// giving it the opportunity to update itself.</remarks>
        public IGraphElementModel Owner { get; }

        internal ConstantField(IConstant constantModel, IGraphElementModel owner,
            ICommandTarget commandTarget, Action<IChangeEvent> onChanged = null, string label = null)
            : base(commandTarget, label)
        {
            ConstantModel = constantModel;
            Owner = owner;
            m_Field = CreateField();

            SetFieldChangedCallback(onChanged);

            this.AddStylesheet("ConstantField.uss");
            // TODO VladN: fix for light skin, remove when GTF supports light skin
            if (!EditorGUIUtility.isProSkin)
                this.AddStylesheet("ConstantField_lightFix.uss");

            if (m_Field != null)
            {
                hierarchy.Add(m_Field);
            }
        }

        void SetFieldChangedCallback(Action<IChangeEvent> onChanged)
        {
            if (m_Field == null)
                return;

            var registerCallbackMethod = GetRegisterCallback();
            if (registerCallbackMethod != null)
            {
                void EventCallback(IChangeEvent e, ConstantField f)
                {
                    if (onChanged == null)
                    {
                        if (e != null) // Enum editor sends null
                        {
                            var newValue = GetNewValue(e);
                            var command = new UpdateConstantValueCommand(ConstantModel, newValue, Owner);
                            f.CommandTarget.Dispatch(command);
                        }
                    }
                    else
                    {
                        onChanged(e);
                    }
                }

                registerCallbackMethod.Invoke(m_Field, new object[] { (EventCallback<IChangeEvent, ConstantField>)EventCallback, this, TrickleDown.NoTrickleDown });
            }
        }

        /// <summary>
        /// Extracts the new value of a <see cref="IChangeEvent"/>.
        /// </summary>
        /// <param name="e">The event.</param>
        /// <returns>The value of the `newValue` property of the event.</returns>
        public static object GetNewValue(IChangeEvent e)
        {
            // PF TODO when this is a module, submit modifications to UIToolkit to avoid having to do reflection.
            var p = e.GetType().GetProperty(nameof(ChangeEvent<object>.newValue));
            return p?.GetValue(e);
        }

        internal MethodInfo GetRegisterCallback()
        {
            // PF TODO when this is a module, submit modifications to UIToolkit to avoid having to do reflection.
            var type = typeof(CallbackEventHandler);
            var methods = type.GetMethods(BindingFlags.Public | BindingFlags.DeclaredOnly | BindingFlags.Instance);
            foreach (var method in methods)
            {
                if (method.Name == nameof(RegisterCallback) && method.GetGenericArguments().Length == 2)
                {
                    var t = ConstantModel.Type == typeof(EnumValueReference) ? typeof(Enum) : ConstantModel.Type;
                    var changeEventType = typeof(ChangeEvent<>).MakeGenericType(t);
                    return method.MakeGenericMethod(changeEventType, typeof(ConstantField));
                }
            }

            return null;
        }

        /// <inheritdoc />
        public override bool UpdateDisplayedValue()
        {
            if (m_Field == null)
                return false;

            var portModel = Owner as IPortModel;
            var isConnected = portModel != null && portModel.IsConnected() && portModel.GetConnectedPorts().Any(p => p.NodeModel.State == ModelState.Enabled);
            m_Field.EnableInClassList(connectedModifierUssClassName, isConnected);
            m_Field.SetEnabled(!isConnected);

            // PF TODO when this is a module, submit modifications to UIToolkit to avoid having to do reflection.
            var field = m_Field.SafeQ(null, BaseField<int>.ussClassName);
            var fieldType = field.GetType();
            try
            {
#if UNITY_2021_2_OR_NEWER
                var displayedValue = isConnected ? null : ConstantModel;
                if (displayedValue == null)
                {
                    var t = fieldType;
                    bool isBaseCompositeField = false;
                    while (t != null && t != typeof(object))
                    {
                        if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(BaseCompositeField<,,>))
                        {
                            isBaseCompositeField = true;
                            break;
                        }

                        t = t.BaseType;
                    }

                    if (isBaseCompositeField)
                    {
                        // PF TODO module: UIToolkit should properly support mixed values for composite fields.
                        foreach (var subField in field.Query().ToList().OfType<IMixedValueSupport>())
                        {
                            subField.showMixedValue = true;
                        }
                    }

                    if (field is IMixedValueSupport mixedValueSupport)
                    {
                        mixedValueSupport.showMixedValue = true;
                    }
                }
                else
                {
                    var setValueMethod = fieldType.GetMethod("SetValueWithoutNotify");
                    var value = displayedValue.Type == typeof(EnumValueReference) ?
                        ((EnumValueReference)displayedValue.ObjectValue).ValueAsEnum() : displayedValue.ObjectValue;
                    setValueMethod?.Invoke(field, new[] { value });
                }
#else
                var setValueMethod = fieldType.GetMethod("SetValueWithoutNotify");
                var value = ConstantModel.Type == typeof(EnumValueReference) ?
                    ((EnumValueReference)ConstantModel.ObjectValue).ValueAsEnum() : ConstantModel.ObjectValue;
                setValueMethod?.Invoke(field, new[] { value });
#endif
            }
            catch (Exception)
            {
                return false;
            }

            return true;
        }

        VisualElement CreateField()
        {
            var fieldType = ConstantModel.GetTypeHandle().Resolve();
            var fieldTooltip = Owner is IPortModel portModel ? portModel.ToolTip : "";

            if (m_CustomFieldBuilder == null)
            {
                TryCreateCustomPropertyFieldBuilder(fieldType, out m_CustomFieldBuilder);
            }

            return m_CustomFieldBuilder?.Build(CommandTarget, Label, fieldTooltip, ConstantModel, nameof(IConstant.ObjectValue)) ?? CreateDefaultFieldForType(fieldType, fieldTooltip);
        }
    }
}
