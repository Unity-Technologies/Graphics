using System;
using System.Reflection;
using System.Collections.Generic;
using UnityEditor.Experimental.UIElements.GraphView;
using UnityEngine;

namespace UnityEditor.VFX
{
    static class VFXTypeExtension
    {
        static Dictionary<Type, string> s_FriendlyName = new Dictionary<Type, string>();

        static VFXTypeExtension()
        {
            s_FriendlyName[typeof(float)] = "float";
            s_FriendlyName[typeof(int)] = "int";
            s_FriendlyName[typeof(bool)] = "bool";
            s_FriendlyName[typeof(uint)] = "uint";
            s_FriendlyName[typeof(char)] = "char";
            s_FriendlyName[typeof(bool)] = "bool";
            s_FriendlyName[typeof(double)] = "double";
            s_FriendlyName[typeof(short)] = "short";
            s_FriendlyName[typeof(ushort)] = "ushort";
            s_FriendlyName[typeof(long)] = "long";
            s_FriendlyName[typeof(ulong)] = "ulong";
            s_FriendlyName[typeof(byte)] = "byte";
            s_FriendlyName[typeof(sbyte)] = "sbyte";
            s_FriendlyName[typeof(decimal)] = "decimal";
            s_FriendlyName[typeof(char)] = "char";
            s_FriendlyName[typeof(string)] = "string";
        }

        public static string UserFriendlyName(this Type type)
        {
            string result;
            if (s_FriendlyName.TryGetValue(type, out result))
            {
                return result;
            }
            return type.Name;
        }

        // needed only for .NET 4.6 which make the GetNestedType non recursive
        public static Type GetRecursiveNestedType(this Type type, string typeName)
        {
            do
            {
                Type nestedType = type.GetNestedType(typeName);
                if (nestedType != null)
                    return nestedType;
                type = type.BaseType;
            }
            while (type != null);

            return null;
        }

        public static object GetDefaultField(Type type)
        {
            if (type == typeof(Matrix4x4))
            {
                return Matrix4x4.identity;
            }

            var defaultField = type.GetField("defaultValue", BindingFlags.Public | BindingFlags.Static);
            if (defaultField != null)
            {
                return defaultField.GetValue(null);
            }
            return null;
        }
    }
}
