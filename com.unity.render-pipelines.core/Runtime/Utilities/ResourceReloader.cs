using System;
using System.IO;
using UnityEngine.Assertions;
#if UNITY_EDITOR
using UnityEditor;
using System.Reflection;
#endif

namespace UnityEngine.Rendering
{
#if UNITY_EDITOR
    /// <summary>
    /// The resources that need to be reloaded in Editor can live in Runtime.
    /// The reload call should only be done in Editor context though but it
    /// could be called from runtime entities.
    /// </summary>
    public static class ResourceReloader
    {
        /// <summary>
        /// Looks for resources in the given <paramref name="container"/> object and reload the ones
        /// that are missing or broken.
        /// </summary>
        /// <param name="container">The object containing reload-able resources</param>
        /// <param name="basePath">The base path for the package</param>
        public static void ReloadAllNullIn(System.Object container, string basePath)
        {
            if (IsNull(container))
                return;

            foreach (var fieldInfo in container.GetType().GetFields())
            {
                //Recurse on sub-containers
                if (IsReloadGroup(fieldInfo))
                {
                    FixGroupIfNeeded(container, fieldInfo);
                    ReloadAllNullIn(fieldInfo.GetValue(container), basePath);
                }

                //Find null field and reload them
                var attribute = GetReloadAttribute(fieldInfo);
                if (attribute != null)
                {
                    if (attribute.paths.Length == 1)
                    {
                        SetAndLoadIfNull(container, fieldInfo, GetFullPath(basePath, attribute),
                            attribute.package == ReloadAttribute.Package.Builtin);
                    }
                    else if (attribute.paths.Length > 1)
                    {
                        FixArrayIfNeeded(container, fieldInfo, attribute.paths.Length);

                        var array = (Array)fieldInfo.GetValue(container);
                        if (IsReloadGroup(array))
                        {
                            //Recurse on each sub-containers
                            for (int index = 0; index < attribute.paths.Length; ++index)
                            {
                                FixGroupIfNeeded(array, index);
                                ReloadAllNullIn(array.GetValue(index), basePath);
                            }
                        }
                        else
                        {
                            bool builtin = attribute.package == ReloadAttribute.Package.Builtin;
                            //Find each null element and reload them
                            for (int index = 0; index < attribute.paths.Length; ++index)
                                SetAndLoadIfNull(array, index, GetFullPath(basePath, attribute, index), builtin);
                        }
                    }
                }
            }

            if (container is UnityEngine.Object c)
                EditorUtility.SetDirty(c);
        }

        static void FixGroupIfNeeded(System.Object container, FieldInfo info)
        {
            if (IsNull(container, info))
            {
                var type = info.FieldType;
                var value = type.IsSubclassOf(typeof(ScriptableObject))
                    ? ScriptableObject.CreateInstance(type)
                    : Activator.CreateInstance(type);

                info.SetValue(
                    container,
                    value
                );
            }
        }

        static void FixGroupIfNeeded(Array array, int index)
        {
            Assert.IsNotNull(array);

            if (IsNull(array.GetValue(index)))
            {
                var type = array.GetType().GetElementType();
                var value = type.IsSubclassOf(typeof(ScriptableObject))
                    ? ScriptableObject.CreateInstance(type)
                    : Activator.CreateInstance(type);

                array.SetValue(
                    value,
                    index
                );
            }
        }

        static void FixArrayIfNeeded(System.Object container, FieldInfo info, int length)
        {
            if (IsNull(container, info) || ((Array)info.GetValue(container)).Length < length)
            {
                info.SetValue(
                    container,
                    Activator.CreateInstance(info.FieldType, length)
                );
            }
        }

        static ReloadAttribute GetReloadAttribute(FieldInfo fieldInfo)
        {
            var attributes = (ReloadAttribute[])fieldInfo
                .GetCustomAttributes(typeof(ReloadAttribute), false);
            if (attributes.Length == 0)
                return null;
            return attributes[0];
        }

        static bool IsReloadGroup(FieldInfo info)
            => info.FieldType
            .GetCustomAttributes(typeof(ReloadGroupAttribute), false).Length > 0;

        static bool IsReloadGroup(Array field)
            => field.GetType().GetElementType()
            .GetCustomAttributes(typeof(ReloadGroupAttribute), false).Length > 0;

        static bool IsNull(System.Object container, FieldInfo info)
            => IsNull(info.GetValue(container));

        static bool IsNull(System.Object field)
            => field == null || field.Equals(null);

        static UnityEngine.Object Load(string path, Type type, bool builtin)
        {
            UnityEngine.Object result;
            if (builtin && type == typeof(Shader))
                result = Shader.Find(path);
            else
                result = AssetDatabase.LoadAssetAtPath(path, type);
            if (IsNull(result))
                throw new Exception($"Cannot load. Incorrect path: {path} Null returned.");
            return result;
        }


        static void SetAndLoadIfNull(System.Object container, FieldInfo info,
            string path, bool builtin)
        {
            if (IsNull(container, info))
                info.SetValue(container, Load(path, info.FieldType, builtin));
        }

        static void SetAndLoadIfNull(Array array, int index, string path, bool builtin)
        {
            var element = array.GetValue(index);
            if (IsNull(element))
                array.SetValue(Load(path, array.GetType().GetElementType(), builtin), index);
        }

        static string GetFullPath(string basePath, ReloadAttribute attribute, int index = 0)
        {
            string path;
            switch (attribute.package)
            {
                case ReloadAttribute.Package.Builtin:
                    path = attribute.paths[index];
                    break;
                case ReloadAttribute.Package.Root:
                    path = basePath + "/" + attribute.paths[index];
                    break;
                default:
                    throw new ArgumentException("Unknown Package Path!");
            }
            return path;
        }
    }
#endif

    /// <summary>
    /// Attribute specifying information to reload with <see cref="ResourceReloader"/>. This is only
    /// used in the editor and doesn't have any effect at runtime.
    /// </summary>
    /// <seealso cref="ResourceReloader"/>
    /// <seealso cref="ReloadGroupAttribute"/>
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class ReloadAttribute : Attribute
    {
        /// <summary>
        /// Lookup method for a resource.
        /// </summary>
        public enum Package
        {
            /// <summary>
            /// Used for builtin resources when the resource isn't part of the package (i.e. builtin
            /// shaders).
            /// </summary>
            Builtin,

            /// <summary>
            /// Used for resources inside the package.
            /// </summary>
            Root
        };

#if UNITY_EDITOR
        /// <summary>
        /// The lookup method.
        /// </summary>
        public readonly Package package;

        /// <summary>
        /// Search paths.
        /// </summary>
        public readonly string[] paths;
#endif

        /// <summary>
        /// Creates a new <see cref="ReloadAttribute"/> for an array by specifying each resource
        /// path individually.
        /// </summary>
        /// <param name="paths">Search paths</param>
        /// <param name="package">The lookup method</param>
        public ReloadAttribute(string[] paths, Package package = Package.Root)
        {
#if UNITY_EDITOR
            this.paths = paths;
            this.package = package;
#endif
        }

        /// <summary>
        /// Creates a new <see cref="ReloadAttribute"/> for a single resource.
        /// </summary>
        /// <param name="path">Search path</param>
        /// <param name="package">The lookup method</param>
        public ReloadAttribute(string path, Package package = Package.Root)
            : this(new[] { path }, package)
        { }

        /// <summary>
        /// Creates a new <see cref="ReloadAttribute"/> for an array using automatic path name
        /// numbering.
        /// </summary>
        /// <param name="pathFormat">The format used for the path</param>
        /// <param name="rangeMin">The array start index (inclusive)</param>
        /// <param name="rangeMax">The array end index (exclusive)</param>
        /// <param name="package">The lookup method</param>
        public ReloadAttribute(string pathFormat, int rangeMin, int rangeMax,
            Package package = Package.Root)
        {
#if UNITY_EDITOR
            this.package = package;
            paths = new string[rangeMax - rangeMin];
            for (int index = rangeMin, i = 0; index < rangeMax; ++index, ++i)
                paths[i] = string.Format(pathFormat, index);
#endif
        }
    }

    /// <summary>
    /// Attribute specifying that it contains element that should be reloaded.
    /// If the instance of the class is null, the system will try to recreate
    /// it with the default constructor.
    /// Be sure classes using it have default constructor!
    /// </summary>
    /// <seealso cref="ReloadAttribute"/>
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class ReloadGroupAttribute : Attribute
    { }
}
