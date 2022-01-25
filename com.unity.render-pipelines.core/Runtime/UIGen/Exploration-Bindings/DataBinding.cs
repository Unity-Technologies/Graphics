//#define USE_EDITOR_SERIALIZATION

#if UNITY_EDITOR && USE_EDITOR_SERIALIZATION
using UnityEditor;
using UnityEditor.UIElements;
#else
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
#endif

using UnityEngine;
using UnityEngine.UIElements;

public class DataBinding : MonoBehaviour
{
    VisualElement treeRoot;
    public Datas tracked;

    private void OnEnable()
    {
        treeRoot = GetComponent<UIDocument>()?.rootVisualElement;
#if UNITY_EDITOR && USE_EDITOR_SERIALIZATION
        treeRoot?.Bind(new SerializedObject(tracked));
#else
        treeRoot.Query<BindableElement>().ForEach(bindable => InitAndRegisterBinding(bindable));
#endif
    }

    private void OnDisable()
    {
#if UNITY_EDITOR && USE_EDITOR_SERIALIZATION
        treeRoot?.Unbind();
#else
        treeRoot.Query<BindableElement>().ForEach(bindable => Unbind(bindable));
#endif
    }


#if !UNITY_EDITOR || !USE_EDITOR_SERIALIZATION
    interface IBinding { }
    struct FieldBinding<T> : IBinding
    {
        public EventCallback<ChangeEvent<T>> valueChangedCallback { get; private set; }
        public FieldBinding(EventCallback<ChangeEvent<T>> valueChangedCallback)
        {
            this.valueChangedCallback = valueChangedCallback;
        }
    }
    struct ButtonBinding : IBinding
    {
        public Action clicked { get; private set; }
        public ButtonBinding(Action clicked)
        {
            this.clicked = clicked;
        }
    }
    Dictionary<string, IBinding> registeredBindable = new Dictionary<string, IBinding>();

    void InitAndRegisterBinding(BindableElement bindable)
    {
        if (String.IsNullOrEmpty(bindable.bindingPath))
            return;

        if (bindable is Button button)
        {
            MethodInfo mi = tracked.GetType().GetMethod(button.bindingPath, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (mi == null)
                return;

            var instance = Expression.Constant(tracked);
            var call = Expression.Call(instance, mi);
            var lambda = Expression.Lambda<Action>(call);
            var compiled = lambda.Compile();
            button.clicked += compiled;

            registeredBindable[bindable.bindingPath] = new ButtonBinding(compiled);
        }
        else if (bindable is INotifyValueChanged<int> intField)
        {
            FieldInfo fi = tracked.GetType().GetField(bindable.bindingPath, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            MemberExpression fieldExp = Expression.Field(Expression.Constant(tracked), fi);
            var getterLambda = Expression.Lambda<Func<int>>(fieldExp);
            var getter = getterLambda.Compile();

            ParameterExpression targetExpression = Expression.Parameter(typeof(int), "target");
            BinaryExpression assignExp = Expression.Assign(fieldExp, targetExpression);
            var setterLambda = Expression.Lambda<Action<int>>(Expression.Assign(fieldExp, targetExpression), targetExpression);
            var setter = setterLambda.Compile();

            EventCallback<ChangeEvent<int>> updater = UpdateValue(intField, setter, /*onValueChanged*/ () => Debug.Log($"Updated to {getter()}"));
            intField.RegisterValueChangedCallback(updater);
            registeredBindable[bindable.bindingPath] = new FieldBinding<int>(updater);
        }
    }

    EventCallback<ChangeEvent<T>> UpdateValue<T>(INotifyValueChanged<T> notifyingField, Action<T> setter, Action additionalCallback)
    {
        return (ChangeEvent<T> evt) =>
        {
            setter?.Invoke(evt.newValue);
            additionalCallback?.Invoke();
        };
    }

    void Unbind(BindableElement bindable)
    {
        if (String.IsNullOrEmpty(bindable.bindingPath) || !registeredBindable.ContainsKey(bindable.bindingPath))
            return;

        if (bindable is Button button)
        {
            ButtonBinding binding = (ButtonBinding)registeredBindable[bindable.bindingPath];
            button.clicked -= binding.clicked;
        }
        else if (bindable is INotifyValueChanged<int> intField)
        {
            FieldBinding<int> binding = (FieldBinding<int>)registeredBindable[bindable.bindingPath];
            intField.UnregisterValueChangedCallback(binding.valueChangedCallback);
        }
    }
#endif
}
