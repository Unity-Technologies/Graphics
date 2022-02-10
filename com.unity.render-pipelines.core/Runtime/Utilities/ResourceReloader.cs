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
        /// This version will still return null value without throwing error if the issue is due to
        /// AssetDatabase being not ready. But in this case the assetDatabaseNotReady result will be true.
        /// </summary>
        /// <param name="container">The object containing reload-able resources</param>
        /// <param name="basePath">The base path for the package</param>
        /// <returns>
        ///   - 1 hasChange: True if something have been reloaded.
        ///   - 2 assetDatabaseNotReady: True if the issue preventing loading is due to state of AssetDatabase
        /// </returns>
        public static (bool hasChange, bool assetDatabaseNotReady) TryReloadAllNullIn(System.Object container, string basePath)
        {
            try
            {
                return (ReloadAllNullIn(container, basePath), false);
            }
            catch (Exception e)
            {
                if (!(e.Data.Contains("InvalidImport") && e.Data["InvalidImport"] is int && (int)e.Data["InvalidImport"] == 1))
                    throw e;
                return (false, true);
            }
        }

        /// <summary>
        /// Looks for resources in the given <paramref name="container"/> object and reload the ones
        /// that are missing or broken.
        /// </summary>
        /// <param name="container">The object containing reload-able resources</param>
        /// <param name="basePath">The base path for the package</param>
        /// <returns>True if something have been reloaded.</returns>
        public static bool ReloadAllNullIn(System.Object container, string basePath)
        {
            if (IsNull(container))
                return false;

            var changed = false;
            foreach (var fieldInfo in container.GetType()
                .GetFields(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static))
            {
                //Recurse on sub-containers
                if (IsReloadGroup(fieldInfo))
                {
                    changed |= FixGroupIfNeeded(container, fieldInfo);
                    changed |= ReloadAllNullIn(fieldInfo.GetValue(container), basePath);
                }

                //Find null field and reload them
                var attribute = GetReloadAttribute(fieldInfo);
                if (attribute != null)
                {
                    if (attribute.paths.Length == 1)
                    {
                        changed |= SetAndLoadIfNull(container, fieldInfo, GetFullPath(basePath, attribute),
                            attribute.package);
                    }
                    else if (attribute.paths.Length > 1)
                    {
                        changed |= FixArrayIfNeeded(container, fieldInfo, attribute.paths.Length);

                        var array = (Array)fieldInfo.GetValue(container);
                        if (IsReloadGroup(array))
                        {
                            //Recurse on each sub-containers
                            for (int index = 0; index < attribute.paths.Length; ++index)
                            {
                                changed |= FixGroupIfNeeded(array, index);
                                changed |= ReloadAllNullIn(array.GetValue(index), basePath);
                            }
                        }
                        else
                        {
                            //Find each null element and reload them
                            for (int index = 0; index < attribute.paths.Length; ++index)
                                changed |= SetAndLoadIfNull(array, index, GetFullPath(basePath, attribute, index),
                                    attribute.package);
                        }
                    }
                }
            }

            if (changed && container is UnityEngine.Object c)
                EditorUtility.SetDirty(c);
            return changed;
        }

        static void CheckReloadGroupSupportedType(Type type)
        {
            if (type.IsSubclassOf(typeof(ScriptableObject)))
                throw new Exception(@$"ReloadGroup attribute must not be used on {nameof(ScriptableObject)}.
If {nameof(ResourceReloader)} create an instance of it, it will be not saved as a file, resulting in corrupted ID when building.");
        }

        static bool FixGroupIfNeeded(System.Object container, FieldInfo info)
        {
            var type = info.FieldType;
            CheckReloadGroupSupportedType(type);

            if (IsNull(container, info))
            {
                var value = Activator.CreateInstance(type);

                info.SetValue(
                    container,
                    value
                );
                return true;
            }

            return false;
        }

        static bool FixGroupIfNeeded(Array array, int index)
        {
            Assert.IsNotNull(array);

            var type = array.GetType().GetElementType();
            CheckReloadGroupSupportedType(type);

            if (IsNull(array.GetValue(index)))
            {
                var value = type.IsSubclassOf(typeof(ScriptableObject))
                    ? ScriptableObject.CreateInstance(type)
                    : Activator.CreateInstance(type);

                array.SetValue(value, index);
                return true;
            }

            return false;
        }

        static bool FixArrayIfNeeded(System.Object container, FieldInfo info, int length)
        {
            if (IsNull(container, info) || ((Array)info.GetValue(container)).Length < length)
            {
                info.SetValue(container, Activator.CreateInstance(info.FieldType, length));
                return true;
            }

            return false;
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

        static UnityEngine.Object Load(string path, Type type, ReloadAttribute.Package location)
        {
            // Check if asset exist.
            // Direct loading can be prevented by AssetDatabase being reloading.
            var guid = AssetDatabase.AssetPathToGUID(path);
            if (location == ReloadAttribute.Package.Root && String.IsNullOrEmpty(guid))
                throw new Exception($"Cannot load. Incorrect path: {path}");

            // Else the path is good. Attempt loading resource if AssetDatabase available.
            UnityEngine.Object result;
            switch (location)
            {
                case ReloadAttribute.Package.Builtin:
                    if (type == typeof(Shader))
                        result = Shader.Find(path);
                    else
                        result = Resources.GetBuiltinResource(type, path); //handle wrong path error
                    break;
                case ReloadAttribute.Package.BuiltinExtra:
                    if (type == typeof(Shader))
                        result = Shader.Find(path);
                    else
                        result = AssetDatabase.GetBuiltinExtraResource(type, path); //handle wrong path error
                    break;
                case ReloadAttribute.Package.Root:
                    result = AssetDatabase.LoadAssetAtPath(path, type);
                    break;
                default:
                    throw new NotImplementedException($"Unknown {location}");
            }

            if (IsNull(result))
            {
                var e = new Exception($"Cannot load. Path {path} is correct but AssetDatabase cannot load now.");
                e.Data["InvalidImport"] = 1;
                throw e;
            }
            return result;
        }

        static bool SetAndLoadIfNull(System.Object container, FieldInfo info,
            string path, ReloadAttribute.Package location)
        {
            if (IsNull(container, info))
            {
                info.SetValue(container, Load(path, info.FieldType, location));
                return true;
            }

            return false;
        }

        static bool SetAndLoadIfNull(Array array, int index, string path, ReloadAttribute.Package location)
        {
            var element = array.GetValue(index);
            if (IsNull(element))
            {
                array.SetValue(Load(path, array.GetType().GetElementType(), location), index);
                return true;
            }

            return false;
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
            Root,

            /// <summary>
            /// Used for builtin extra resources when the resource isn't part of the package (i.e. builtin
            /// extra Sprite).
            /// </summary>
            BuiltinExtra,
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
