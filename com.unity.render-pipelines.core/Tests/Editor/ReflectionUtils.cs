using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace UnityEngine.Rendering.Tests
{
    public static class ReflectionUtils
    {
        /// <summary>
        /// Finds a type by full name
        /// </summary>
        /// <param name="name">The full type name with namespace</param>
        /// <returns>The found type</returns>
        public static Type FindTypeByName(string name)
        {
            var type = AppDomain.CurrentDomain
                .GetAssemblies()
                .Select(assembly => assembly.GetType(name))
                .FirstOrDefault(tt => tt != null);

            Assert.True(type != null, "Type not found");
            return type;
        }

        /// <summary>
        /// Calls a private method from a class
        /// </summary>
        /// <param name="methodName">The method name</param>
        /// <param name="args">The arguments to pass to the method</param>
        public static object InvokeStatic(this Type targetType, string methodName, params object[] args)
        {
            Assert.True(targetType != null, "Invalid Type");
            Assert.IsNotEmpty(methodName, "The methodName to set could not be null");

            var mi = targetType.GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Static);
            Assert.True(mi != null, $"Could not find method `{methodName}` on type `{targetType}`");
            return mi.Invoke(null, args);
        }

        /// <summary>
        /// Calls a private method from a class
        /// </summary>
        /// <param name="methodName">The method name</param>
        /// <param name="args">The arguments to pass to the method</param>
        public static object Invoke(this object target, string methodName, params object[] args)
        {
            Assert.True(target != null, "The target could not be null");
            Assert.IsNotEmpty(methodName, "The method name to set could not be null");

            var mi = target.GetType().GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.True(mi != null, $"Could not find method `{methodName}` on object `{target}`");
            return mi.Invoke(target, args);
        }

        private static FieldInfo FindField(this Type type, string fieldName)
        {
            FieldInfo fi = null;

            while (type != null)
            {
                fi = type.GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);

                if (fi != null) break;

                type = type.BaseType;
            }

            Assert.True(fi != null, $"Could not find method `{fieldName}` on object `{type}`");

            return fi;
        }

        /// <summary>
        /// Sets a private field from a class
        /// </summary>
        /// <param name="fieldName">The field to change</param>
        /// <param name="value">The new value</param>
        public static void SetField(this object target, string fieldName, object value)
        {
            Assert.True(target != null, "The target could not be null");
            Assert.IsNotEmpty(fieldName, "The field to set could not be null");
            target.GetType().FindField(fieldName).SetValue(target, value);
        }

        /// <summary>
        /// Gets the value of a private field from a class
        /// </summary>
        /// <param name="fieldName">The field to get</param>
        public static object GetField(this object target, string fieldName)
        {
            Assert.True(target != null, "The target could not be null");
            Assert.IsNotEmpty(fieldName, "The field to set could not be null");
            return target.GetType().FindField(fieldName).GetValue(target);
        }

        /// <summary>
        /// Gets all the fields from a class
        /// </summary>
        public static IEnumerable<FieldInfo> GetFields(this object target)
        {
            Assert.True(target != null, "The target could not be null");

            return target.GetType()
                .GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                .OrderBy(t => t.MetadataToken);
        }
    }
}
