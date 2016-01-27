using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace UnityEditor.Experimental.Graph
{
    public class NodeAdapter
    {
        private static List<MethodInfo> s_TypeAdapters;
        private static Dictionary<int, MethodInfo> s_NodeAdapterDictionary;

        public bool CanAdapt(object a, object b)
        {
            if (a == b)
                return false; // self connections are not permitted

            if (a == null || b == null)
                return false;

            MethodInfo mi = GetAdapter(a, b);
            if (mi == null)
            {
                Debug.Log("adapter node not found for: " + a.GetType() + " -> " + b.GetType());
            }
            return mi != null;
        }

        public bool Connect(object a, object b)
        {
            MethodInfo mi = GetAdapter(a, b);
            if (mi == null)
            {
                Debug.LogError("Attempt to connect 2 unadaptable types: " + a.GetType() + " -> " + b.GetType());
                return false;
            }
            object retVal = mi.Invoke(this, new[] {this, a, b});
            return (bool)retVal;
        }

        IEnumerable<MethodInfo> GetExtensionMethods(Assembly assembly, Type extendedType)
        {
            var query = from type in assembly.GetTypes()
                where type.IsSealed && !type.IsGenericType && !type.IsNested
                from method in type.GetMethods(BindingFlags.Static
                    | BindingFlags.Public | BindingFlags.NonPublic)
                where method.IsDefined(typeof(ExtensionAttribute), false)
                where method.GetParameters()[0].ParameterType == extendedType
                select method;
            return query;
        }

        public MethodInfo GetAdapter(object a, object b)
        {
            if (a == null || b == null)
                return null;

            if (s_NodeAdapterDictionary == null)
            {
                s_NodeAdapterDictionary = new Dictionary<int, MethodInfo>();

                // add extension methods
                AppDomain currentDomain = AppDomain.CurrentDomain;
                foreach (Assembly assembly in currentDomain.GetAssemblies())
                {
                    foreach (MethodInfo method in GetExtensionMethods(assembly, typeof(NodeAdapter)))
                    {
                        var methodParams = method.GetParameters();
                        if (methodParams.Count() == 3)
                        {
                            string pa = methodParams[1].ParameterType + methodParams[2].ParameterType.ToString();
                            s_NodeAdapterDictionary.Add(pa.GetHashCode(), method);
                        }
                    }
                }
            }

            string s = a.GetType().ToString() + b.GetType();

            try
            {
                return s_NodeAdapterDictionary[s.GetHashCode()];
            }
            catch (Exception)
            {}

            return null;
        }

        public MethodInfo GetTypeAdapter(Type from, Type to)
        {
            if (s_TypeAdapters == null)
            {
                s_TypeAdapters = new List<MethodInfo>();
                AppDomain currentDomain = AppDomain.CurrentDomain;
                foreach (Assembly assembly in currentDomain.GetAssemblies())
                {
                    try
                    {
                        foreach (Type temptype in assembly.GetTypes())
                        {
                            MethodInfo[] methodInfos = temptype.GetMethods(BindingFlags.Public | BindingFlags.Static);
                            foreach (MethodInfo i in methodInfos)
                            {
                                object[] allAttrs = i.GetCustomAttributes(typeof(TypeAdapter), false);
                                if (allAttrs.Count() > 0)
                                {
                                    s_TypeAdapters.Add(i);
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.Log(ex);
                    }
                }
            }

            foreach (MethodInfo i in s_TypeAdapters)
            {
                if (i.ReturnType == to)
                {
                    ParameterInfo[] allParams = i.GetParameters();
                    if (allParams.Count() == 1)
                    {
                        if (allParams[0].ParameterType == from)
                            return i;
                    }
                }
            }
            return null;
        }
    };
}
