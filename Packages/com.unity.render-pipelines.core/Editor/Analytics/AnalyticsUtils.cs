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

        /// <summary>
        /// Gets a list of the serializable fields of the given type
        /// </summary>
        /// <param name="type">The type to get fields that are serialized.</param>
        /// <param name="removeObsolete">If obsolete fields are taken into account</param>
        /// <returns>The collection of <see cref="FieldInfo"/> that are serialized for this type</returns>
        public static IEnumerable<FieldInfo> GetSerializableFields(this Type type, bool removeObsolete = false)
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
                    tempList.Add(list[i] != null ? list[i].ToString() : "null");
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
                var fieldType = field.FieldType;
                if (!IsFieldIgnored(fieldType))
                    AddDiff(current, defaults, field, fieldType, diff);
            }

            return diff;
        }

        private static void AddDiff(object current, object defaults, FieldInfo field, Type fieldType, Dictionary<string, string> diff)
        {
            try
            {
                var valueCurrent = current != null ? field.GetValue(current) : null;
                var valueDefault = defaults != null ? field.GetValue(defaults) : null;
                AddIfDifferent(field, fieldType, diff, valueCurrent, valueDefault);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Exception found while parsing {field}, {ex}");
            }
        }

        private static void AddIfDifferent(FieldInfo field, Type fieldType, Dictionary<string, string> diff, object valueCurrent, object valueDefault)
        {
            if (!AreValuesEqual(fieldType, valueCurrent, valueDefault))
            {
                if (IsComplexType(fieldType))
                {
                    var subDiff = GetDiffAsDictionary(fieldType, valueCurrent, valueDefault);
                    foreach (var d in subDiff)
                    {
                        diff[$"{field.Name}.{d.Key}"] = d.Value;
                    }
                }
                else
                {
                    diff[field.Name] = ConvertValueToString(valueCurrent);
                }
            }
        }

        static bool IsFieldIgnored(Type fieldType)
        {
            return fieldType.GetCustomAttribute<ObsoleteAttribute>() != null || typeof(ScriptableObject).IsAssignableFrom(fieldType);
        }

        internal static bool AreValuesEqual(Type fieldType, object valueCurrent, object valueDefault)
        {
            if (fieldType == typeof(string))
                return (string)valueCurrent == (string)valueDefault;

            if (fieldType.IsPrimitive || fieldType.IsEnum)
                return valueCurrent.Equals(valueDefault);

            if (fieldType.IsArray && valueCurrent is IList currentList)
                return !AreArraysDifferent(currentList, valueDefault as IList);

            if (valueCurrent == null && valueDefault == null)
                return true;

            return valueDefault?.Equals(valueCurrent) ?? valueCurrent?.Equals(null) ?? false;
        }

        internal static bool IsComplexType(Type fieldType)
        {
            // Primitive types and enums are not considered complex
            if (fieldType.IsPrimitive || fieldType.IsEnum)
                return false;

            // String is considered a primitive type for our purposes
            if (fieldType == typeof(string))
                return false;

            // Arrays can be converted to string easy without sub-elements
            if (fieldType.IsArray)
                return false;

            // Value types (structs) that are not primitive are considered complex
            // Classes are considered complex types
            return fieldType.IsValueType || fieldType.IsClass;
        }

        static string ConvertValueToString(object value)
        {
            if (value == null) return null;
            if (value is IList list) return list.DumpValues();
            return ConvertPrimitiveWithInvariants(value);
        }

        static string ConvertPrimitiveWithInvariants(object obj)
        {
            if (obj is IConvertible convertible)
                return convertible.ToString(CultureInfo.InvariantCulture);
            return obj.ToString();
        }

        static string[] ToStringArray(Dictionary<string, string> diff, string format = null)
        {
            var changedSettings = new string[diff.Count];

            if (string.IsNullOrEmpty(format))
                format = @"{{""{0}"":""{1}""}}";

            int i = 0;
            foreach (var d in diff)
                changedSettings[i++] = string.Format(format, d.Key, d.Value);

            return changedSettings;
        }

        private static string[] EnumerableToNestedColumn<T>([DisallowNull] this IEnumerable collection)
        {
            using (ListPool<string>.Get(out var tmp))
            {
                foreach (var element in collection)
                {
                    string[] elementColumns = ToStringArray(DumpValues(element.GetType(), element), @"""{0}"":""{1}""");
                    tmp.Add("{" + string.Join(", ", elementColumns) + "}");
                }

                return tmp.ToArray();
            }
        }

        private static string[] ToNestedColumnSimplify<T>([DisallowNull] this T current)
            where T : new()
        {
            var type = current.GetType();

            if (typeof(UnityEngine.Object).IsAssignableFrom(typeof(T)))
            {
                var instance = ScriptableObject.CreateInstance(type);
                ToStringArray(GetDiffAsDictionary(type, current, instance));
                ScriptableObject.DestroyImmediate(instance);
            }

            return ToStringArray(GetDiffAsDictionary(type, current, new T()));
        }

        /// <summary>
        /// Obtains the Serialized fields and values in form of nested columns for BigQuery
        /// https://cloud.google.com/bigquery/docs/nested-repeated
        /// </summary>
        /// <typeparam name="T">The given type</typeparam>
        /// <param name="current">The current object to obtain the fields and values.</param>
        /// <param name="compareAndSimplifyWithDefault">If a comparison against the default value must be done.</param>
        /// <returns>The nested columns in form of {key.nestedKey : value} </returns>
        /// <exception cref="ArgumentNullException">Throws an exception if current parameter is null.</exception>
        public static string[] ToNestedColumn<T>([DisallowNull] this T current, bool compareAndSimplifyWithDefault = false)
            where T : new()
        {
            if (current == null)
                throw new ArgumentNullException(nameof(current));

            if (current is IEnumerable currentAsEnumerable)
                return EnumerableToNestedColumn<T>(currentAsEnumerable);

            if (compareAndSimplifyWithDefault)
                return ToNestedColumnSimplify(current);

            return ToStringArray(DumpValues(current.GetType(), current));
        }

        /// <summary>
        /// Obtains the Serialized fields and values in form of nested columns for BigQuery
        /// https://cloud.google.com/bigquery/docs/nested-repeated
        /// </summary>
        /// <typeparam name="T">The given type</typeparam>
        /// <param name="current">The current object to obtain the fields and values.</param>
        /// <param name="defaultInstance">The default instance to compare values</param>
        /// <returns>The nested columns in form of {key.nestedKey : value} </returns>
        /// <exception cref="ArgumentNullException">Throws an exception if the current or defaultInstance parameters are null.</exception>
        public static string[] ToNestedColumn<T>([DisallowNull] this T current, T defaultInstance)
        {
            if (current == null)
                throw new ArgumentNullException(nameof(current));

            if (defaultInstance == null)
                throw new ArgumentNullException(nameof(defaultInstance));

            var type = current.GetType();

            Dictionary<string, string> diff = GetDiffAsDictionary(type, current, defaultInstance);
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
        /// <exception cref="ArgumentNullException">Throws an exception if the current parameter is null.</exception>
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
