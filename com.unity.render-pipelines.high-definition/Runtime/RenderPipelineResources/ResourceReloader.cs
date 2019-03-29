using System;

#if UNITY_EDITOR
using UnityEditor;
using System.Reflection;
#endif

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
#if UNITY_EDITOR
    /// <summary>
    /// The resources that need to be reloaded in Editor can live in Runtime.
    /// The reload call should only be done in Editor context though but it
    /// could be called from runtime entities.
    /// </summary>
    internal static class ResourceReloader
    {
        public static void ReloadAllNullIn(System.Object container)
        {
            if (IsNull(container))
                return;

            foreach (var fieldInfo in container.GetType().GetFields())
            {
                //Recurse on subcontainers
                if (IsReloadGroup(fieldInfo))
                {
                    FixGroupIfNeeded(container, fieldInfo);
                    ReloadAllNullIn(fieldInfo.GetValue(container));
                }

                //Find null field and reload them
                var attribute = GetReloadAttribute(fieldInfo);
                if (attribute != null)
                {
                    if (attribute.pathes.Length == 1)
                    {
                        SetAndLoadIfNull(container, fieldInfo, GetFullPath(attribute),
                            attribute.package == ReloadAttribute.Package.Builtin);
                    }
                    else if (attribute.pathes.Length > 1)
                    {
                        FixArrayIfNeeded(container, fieldInfo, attribute.pathes.Length);

                        var array = (Array)fieldInfo.GetValue(container);
                        if (IsReloadGroup(array))
                        {
                            //Recurse on each subcontainers
                            for (int index = 0; index < attribute.pathes.Length; ++index)
                            {
                                FixGroupIfNeeded(array, index);
                                ReloadAllNullIn(array.GetValue(index));
                            }
                        }
                        else
                        {
                            bool builtin = attribute.package == ReloadAttribute.Package.Builtin;
                            //Find each null element and reload them
                            for (int index = 0; index < attribute.pathes.Length; ++index)
                                SetAndLoadIfNull(array, index, GetFullPath(attribute, index), builtin);
                        }
                    }
                }
            }
        }

        static void FixGroupIfNeeded(System.Object container, FieldInfo info)
        {
            if (IsNull(container, info))
            {
                info.SetValue(
                    container,
                    Activator.CreateInstance(info.FieldType)
                );
            }
        }

        static void FixGroupIfNeeded(Array array, int index)
        {
            if (IsNull(array.GetValue(index)))
            {
                array.SetValue(
                    Activator.CreateInstance(array.GetType().GetElementType()),
                    index
                );
            }
        }

        static void FixArrayIfNeeded(System.Object container, FieldInfo info, int length)
        {
            if (IsNull(container, info) || ((Array)info.GetValue(container)).Length < length)
                info.SetValue(
                    container,
                    Activator.CreateInstance(info.FieldType, new object[] { length })
                );
        }

        static ReloadAttribute GetReloadAttribute(FieldInfo fieldInfo)
        {
            var attributes = (ReloadAttribute[])fieldInfo
                .GetCustomAttributes(typeof(ReloadAttribute), false);
            if (attributes == null || attributes.Length == 0)
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
                throw new Exception(
                    String.Format("Cannot load. Incorrect path: {0} Null returned.", path));
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

        static string GetFullPath(ReloadAttribute attribute, int index = 0)
        {
            string basePath;
            switch (attribute.package)
            {
                case ReloadAttribute.Package.Builtin:
                    basePath = "";
                    break;
                case ReloadAttribute.Package.CoreEditor:
                    // All CoreRP have been move to HDRP currently for out of preview of SRP and LW
                    // basePath = HDUtils.GetCorePath() + "Editor/"
                    basePath = HDUtils.GetHDRenderPipelinePath() + "Editor/Core/";
                    break;
                case ReloadAttribute.Package.CoreRuntime:
                    // All CoreRP have been move to HDRP currently for out of preview of SRP and LW
                    // basePath = HDUtils.GetCorePath() + "Runtime/"
                    basePath = HDUtils.GetHDRenderPipelinePath() + "Runtime/Core/";
                    break;
                case ReloadAttribute.Package.HDRPEditor:
                    basePath = HDUtils.GetHDRenderPipelinePath() + "Editor/";
                    break;
                case ReloadAttribute.Package.HDRPRuntime:
                    basePath = HDUtils.GetHDRenderPipelinePath() + "Runtime/";
                    break;
                default:
                    throw new ArgumentException("Unknown Package Path!");
            }
            return basePath + attribute.pathes[index];
        }
    }
#endif



    /// <summary>
    /// Attribute specifying information to reload with ResourceReloader.
    /// Stock and do nothing at runtime.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    internal class ReloadAttribute : Attribute
    {
        public enum Package { Builtin, CoreRuntime, CoreEditor, HDRPRuntime, HDRPEditor };
#if UNITY_EDITOR
        public readonly Package package;
        public readonly string[] pathes;
#endif

        public ReloadAttribute(string[] pathes, Package package = Package.HDRPRuntime)
        {
#if UNITY_EDITOR
            this.pathes = pathes;
            this.package = package;
#endif
        }

        public ReloadAttribute(string path, Package package = Package.HDRPRuntime)
            : this(new string[] { path }, package)
        { }

        public ReloadAttribute(string pathFormat, int rangeMin, int rangeMax,
            Package package = Package.HDRPRuntime)
        {
#if UNITY_EDITOR
            this.package = package;
            pathes = new string[rangeMax - rangeMin];
            for (int index = rangeMin, i = 0; index < rangeMax; ++index, ++i)
                pathes[i] = string.Format(pathFormat, index);
#endif
        }
    }

    /// <summary>
    /// Attribute specifying that it contains element that should be reloaded.
    /// If the instance of the class is null, the system will try to recreate
    /// it with default constructor.
    /// Be sure class using it have default constructor!
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    internal class ReloadGroupAttribute : Attribute
    { }
}
