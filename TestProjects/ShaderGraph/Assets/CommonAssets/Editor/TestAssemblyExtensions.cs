using System;
using System.Reflection;

namespace UnityEditor.ShaderGraph.UnitTests
{
    public static class TestAssemblyExtensions
    {
        private const BindingFlags privateBindingFlags = BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.FlattenHierarchy;
        public static void InvokePrivateAction(this object obj, string methodName, object[] parameters = null) => GetMethod(obj, methodName).Invoke(obj, parameters);
        private static MethodInfo GetMethod(object obj, string methodName)
        {
            MethodInfo method = GetMethodRecursive(obj.GetType(), methodName);
            if (method == null)
                throw new System.Exception($"Unnable to find private method {methodName} in class {obj.GetType().ToString()}");
            return method;
        }
        private static MethodInfo GetMethodRecursive(Type type, string methodName)
        {
            MethodInfo method = type.GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Instance);
            if (method == null && type.BaseType != null)
                return GetMethodRecursive(type.BaseType, methodName);
            return method;
        }

        public static void InvokePrivateFunc(this object obj, string methodName, object[] parameters = null)
        {
            GetMethod(obj, methodName).Invoke(obj, parameters);
        }

        public static T InvokePrivateFunc<T>(this object obj, string methodName, object[] parameters = null)
        {
            object value = GetMethod(obj, methodName).Invoke(obj, parameters);
            return TryConvertValueToType<T>(value, methodName);
        }
        private static T TryConvertValueToType<T>(object input, string name)
        {
            T output = default;
            try { output = (T)input; }
            catch { throw new System.ArgumentException($"Unnable to convert {name} to type of {output.GetType().ToString()}"); }
            return output;
        }

        public static void SetPrivateField(this object obj, string fieldName, object value)
        {
            FieldInfo fieldInfo = GetField(obj, fieldName, privateBindingFlags);
            fieldInfo.SetValue(obj, value);
        }
        public static T GetPrivateField<T>(this object obj, string fieldName)
        {
            object value = GetField(obj, fieldName, privateBindingFlags).GetValue(obj);
            return TryConvertValueToType<T>(value, fieldName);
        }
        private static FieldInfo GetField(object obj, string fieldName, BindingFlags bindingFlags)
        {
            FieldInfo field = obj.GetType().GetField(fieldName, bindingFlags);
            if (field == null)
                throw new System.Exception($"Unnable to find field {fieldName} in class {obj.GetType().ToString()} with binding flags {bindingFlags}");
            return field;
        }

        public static T GetPrivateProperty<T>(this object obj, string propertyName)
        {
            object value = GetProperty(obj, propertyName, privateBindingFlags).GetValue(obj);
            return TryConvertValueToType<T>(value, propertyName);
        }

        private static PropertyInfo GetProperty(object obj, string propertyName, BindingFlags bindingFlags)
        {
            PropertyInfo property = obj.GetType().GetProperty(propertyName, bindingFlags);
           if(property == null)
                throw new System.Exception($"Unnable to find private property {propertyName} in class {obj.GetType().ToString()} with binding flags {bindingFlags}");
            return property;
        }

    }
}
