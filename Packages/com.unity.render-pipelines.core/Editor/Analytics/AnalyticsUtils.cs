using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Reflection;
using UnityEngine;
using UnityEngine.Analytics;
using UnityEngine.Pool;

namespace UnityEditor.Rendering
{
    /// <summary>
    /// Set of utilities for analytics
    /// </summary>
    public static class AnalyticsUtils
    {
        const string k_VendorKey = "unity.srp";

        internal static void SendData(IAnalytic analytic)
        {
             EditorAnalytics.SendAnalytic(analytic);
        }

        internal static IEnumerable<FieldInfo> GetSerializableFields(this Type type, bool removeObsolete = false)
        {
            var members = type.GetMembers(BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic);

            if (type.BaseType != null && type.BaseType != typeof(object))
            {
                foreach (FieldInfo field in type.BaseType.GetSerializableFields())
                {
                    yield return field;
                }
            }

            foreach (var member in members)
            {
                if (member.MemberType != MemberTypes.Field && member.MemberType != MemberTypes.Property)
                {
                    continue;
                }

                if (member.DeclaringType != type || member is not FieldInfo field)
                {
                    continue;
                }

                if (removeObsolete && member.GetCustomAttribute<ObsoleteAttribute>() != null)
                    continue;

                if (field.IsPublic)
                {
                    if (member.GetCustomAttribute<NonSerializedAttribute>() != null)
                        continue;

                    yield return field;
                }
                else
                {
                    if (member.GetCustomAttribute<SerializeField>() != null)
                        yield return field;
                }
            }
        }

        static bool AreArraysDifferent(IList a, IList b)
        {
            if ((a == null) && (b == null))
                return false;

            if ((a == null) ^ (b == null))
                return true;

            if (a.Count != b.Count)
                return true;
            for (int i = 0; i < a.Count; i++)
            {
                if (!a[i].Equals(b[i]))
                    return true;
            }
            return false;
        }

        static string DumpValues(this IList list)
        {
            using (ListPool<string>.Get(out var tempList))
            {
                for (int i = 0; i < list.Count; i++)
                {
                    tempList.Add(list[i].ToString());
                }

                var arrayValues = string.Join(",", tempList);
                return $"[{arrayValues}]";
            }
        }

        static Dictionary<string, string> DumpValues(Type type, object current)
        {
            var diff = new Dictionary<string, string>();

            foreach (var field in type.GetSerializableFields(removeObsolete: true))
            {
                var t = field.FieldType;
                try
                {
                    if (typeof(ScriptableObject).IsAssignableFrom(t))
                        continue;

                    var valueCurrent = current != null ? field.GetValue(current) : null;

                    if (t == typeof(string))
                    {
                        var stringCurrent = (string)valueCurrent;
                        diff[field.Name] = stringCurrent;
                    }
                    else if (t.IsPrimitive || t.IsEnum)
                    {
                        diff[field.Name] = ConvertPrimitiveWithInvariants(valueCurrent);
                    }
                    else if (t.IsArray && valueCurrent is IList valueCurrentList)
                    {
                        diff[field.Name] = valueCurrentList.DumpValues();
                    }
                    else if (t.IsClass || t.IsValueType)
                    {
                        if (valueCurrent is IEnumerable ea)
                            continue; // List<T> not supported

                        var subDiff = DumpValues(t, valueCurrent);
                        foreach (var d in subDiff)
                        {
                            diff[field.Name + "." + d.Key] = d.Value;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Exception found while parsing {field}, {ex}");
                }
            }

            return diff;
        }

        static Dictionary<string, string> GetDiffAsDictionary(Type type, object current, object defaults)
        {
            var diff = new Dictionary<string, string>();

            foreach (var field in type.GetSerializableFields())
            {
                var t = field.FieldType;
                try
                {
                    if (t.GetCustomAttribute<ObsoleteAttribute>() != null || typeof(ScriptableObject).IsAssignableFrom(t))
                        continue;

                    var valueCurrent = current != null ? field.GetValue(current) : null;
                    var valueDefault = defaults != null ? field.GetValue(defaults) : null;

                    if (t == typeof(string))
                    {
                        var stringCurrent = (string)valueCurrent;
                        var stringDefault = (string)valueDefault;
                        if (stringCurrent != stringDefault)
                        {
                            diff[field.Name] = stringCurrent;
                        }
                    }
                    else if (t.IsPrimitive || t.IsEnum)
                    {
                        if (!valueCurrent.Equals(valueDefault))
                            diff[field.Name] = ConvertPrimitiveWithInvariants(valueCurrent);
                    }
                    else if (t.IsArray && valueCurrent is IList valueCurrentList)
                    {
                        if (AreArraysDifferent(valueCurrentList, valueDefault as IList))
                            diff[field.Name] = valueCurrentList.DumpValues();
                    }
                    else if (t.IsClass || t.IsValueType)
                    {
                        if (valueCurrent is IEnumerable ea)
                            continue; // List<T> not supported

                        var subDiff = GetDiffAsDictionary(t, valueCurrent, valueDefault);
                        foreach (var d in subDiff)
                        {
                            diff[field.Name + "." + d.Key] = d.Value;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Exception found while parsing {field}, {ex}");
                }
            }

            return diff;
        }

        static string ConvertPrimitiveWithInvariants(object obj)
        {
            if (obj is IConvertible convertible)
                return convertible.ToString(CultureInfo.InvariantCulture);
            return obj.ToString();
        }

        static string[] ToStringArray(Dictionary<string, string> diff)
        {
            var changedSettings = new string[diff.Count];

            int i = 0;
            foreach (var d in diff)
                changedSettings[i++] = $@"{{""{d.Key}"":""{d.Value}""}}";

            return changedSettings;
        }

        /// <summary>
        /// Obtains the Serialized fields and values in form of nested columns for BigQuery
        /// https://cloud.google.com/bigquery/docs/nested-repeated
        /// </summary>
        /// <typeparam name="T">The given type</typeparam>
        /// <param name="current">The current object to obtain the fields and values.</param>
        /// <param name="compareAndSimplifyWithDefault">If a comparison against the default value must be done.</param>
        /// <returns>The nested columns in form of {key.nestedKey : value} </returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static string[] ToNestedColumn<T>([DisallowNull] this T current, bool compareAndSimplifyWithDefault = false)
            where T : new()
        {
            if (current == null)
                throw new ArgumentNullException(nameof(current));

            var type = current.GetType();

            Dictionary<string, string> diff;
            if (compareAndSimplifyWithDefault)
            {
                if (typeof(UnityEngine.Object).IsAssignableFrom(typeof(T)))
                {
                    var instance = ScriptableObject.CreateInstance(type);
                    diff = GetDiffAsDictionary(type, current, instance);
                    ScriptableObject.DestroyImmediate(instance);
                }
                else
                {
                    diff = GetDiffAsDictionary(type, current, new T());
                }
            }
            else
            {
                diff = DumpValues(type, current);
            }

            return ToStringArray(diff);
        }


        /// <summary>
        /// Obtains the Serialized fields and values in form of nested columns for BigQuery
        /// https://cloud.google.com/bigquery/docs/nested-repeated
        /// </summary>
        /// <typeparam name="T">The given type</typeparam>
        /// <param name="current">The current object to obtain the fields and values.</param>
        /// <param name="defaultObject">The default object</param>
        /// <param name="compareAndSimplifyWithDefault">If a comparison against the default value must be done.</param>
        /// <returns>The nested columns in form of {key.nestedKey : value} </returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static string[] ToNestedColumnWithDefault<T>([DisallowNull] this T current, [DisallowNull] T defaultObject, bool compareAndSimplifyWithDefault = false)
        {
            if (current == null)
                throw new ArgumentNullException(nameof(current));

            var type = current.GetType();

            Dictionary<string, string> diff = (compareAndSimplifyWithDefault) ?
                GetDiffAsDictionary(type, current, defaultObject) : DumpValues(type, current);

            return ToStringArray(diff);
        }
    }
}
