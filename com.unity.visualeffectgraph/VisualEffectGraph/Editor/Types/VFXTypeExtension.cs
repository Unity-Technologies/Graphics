using System;
using System.Collections.Generic;
using UIElements.GraphView;
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
    }
}
