using System;
using System.Collections.Generic;
using UnityEditor.GraphToolsFoundation.Overdrive.Bridge;
using UnityEngine.Scripting.APIUpdating;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    [InitializeOnLoad]
    static class TypeSerializerHelper
    {
        static TypeSerializerHelper()
        {
            UnityEngine.GraphToolsFoundation.Overdrive.TypeHandleHelpers.GetMovedFromType = GetMovedFromType;
        }

        static Dictionary<string, Type> s_MovedFromTypes;
        static Dictionary<string, Type> MovedFromTypes
        {
            get
            {
                if (s_MovedFromTypes == null)
                {
                    s_MovedFromTypes = new Dictionary<string, Type>();
                    var movedFromTypes = TypeCache.GetTypesWithAttribute<MovedFromAttribute>();
                    foreach (var t in movedFromTypes)
                    {
                        var attributes = Attribute.GetCustomAttributes(t, typeof(MovedFromAttribute));
                        foreach (var attribute in attributes)
                        {
                            var movedFromAttribute = (MovedFromAttribute)attribute;
                            movedFromAttribute.GetData(out _, out var nameSpace, out var assembly, out var className);

                            var currentClassName = GetFullNameNoNamespace(t.FullName, t.Namespace);
                            var currentNamespace = t.Namespace;
                            var currentAssembly = t.Assembly.GetName().Name;

                            var oldNamespace = string.IsNullOrEmpty(nameSpace) ? currentNamespace : nameSpace;
                            var oldClassName = string.IsNullOrEmpty(className) ? currentClassName : className;
                            var oldAssembly = string.IsNullOrEmpty(assembly) ? currentAssembly : assembly;

                            var oldAssemblyQualifiedName =
                                oldNamespace != null ? $"{oldNamespace}.{oldClassName}, {oldAssembly}" : $"{oldClassName}, {oldAssembly}";
                            s_MovedFromTypes.Add(oldAssemblyQualifiedName, t);
                        }
                    }
                }

                return s_MovedFromTypes;
            }
        }

        static Type GetMovedFromType(string typeName)
        {
            return MovedFromTypes.TryGetValue(typeName, out var type) ? type : null;
        }

        /// <summary>
        /// Gets the full name of a type without the namespace.
        /// </summary>
        /// <remarks>
        /// The full name of a type nested type includes the outer class type name. The type names are normally
        /// separated by '+' but Unity serialization uses the '/' character as separator.
        ///
        /// This method returns the full type name of a class and switches the type separator to '/' to follow Unity.
        /// </remarks>
        /// <param name="typeName">The full type name, including the namespace.</param>
        /// <param name="nameSpace">The namespace to be removed.</param>
        /// <returns>Returns a string.</returns>
        static string GetFullNameNoNamespace(string typeName, string nameSpace)
        {
            if (typeName != null && nameSpace != null && typeName.Contains(nameSpace))
            {
                return typeName.Substring(nameSpace.Length + 1).Replace("+", "/");
            }
            return typeName;
        }
    }
}
