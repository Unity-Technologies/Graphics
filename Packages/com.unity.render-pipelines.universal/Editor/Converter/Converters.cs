using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using UnityEditor.Rendering.Converter;
using UnityEngine;

namespace UnityEditor.Rendering.Universal
{
    /// <summary>
    /// Filter for the list of converters used in batch mode.
    /// </summary>
    /// <seealso cref="Converters.RunInBatchMode(UnityEditor.Rendering.Universal.ConverterContainerId, List{UnityEditor.Rendering.Universal.ConverterId}, UnityEditor.Rendering.Universal.ConverterFilter)"/>
    public enum ConverterFilter
    {
        /// <summary>
        /// Use this to include converters matching the filter.
        /// </summary>
        Inclusive,

        /// <summary>
        /// Use this to exclude converters matching the filter.
        /// </summary>
        Exclusive
    }

    /// <summary>
    /// The container to run in batch mode.
    /// </summary>
    /// <seealso cref="Converters.RunInBatchMode(UnityEditor.Rendering.Universal.ConverterContainerId)"/>
    public enum ConverterContainerId
    {
        /// <summary>
        /// Use this for Built-in to URP converter.
        /// </summary>
        BuiltInToURP,

        /// <summary>
        /// Use this for Built-in to 2D (URP) converter.
        /// </summary>
        BuiltInToURP2D,

        /// <summary>
        /// Use this for Built-in and 3D URP to 2D (URP) converter.
        /// </summary>
        BuiltInAndURPToURP2D,

        /// <summary>
        /// Use this to upgrade 2D (URP) assets.
        /// </summary>
        UpgradeURP2DAssets,
    }

    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    internal class BatchModeConverterInfo : Attribute
    {
        public Type converterType { get; }
        public ConverterContainerId containerName {get;}

        public BatchModeConverterInfo(ConverterContainerId containerName, Type converterType)
        {
            this.converterType = converterType;
            this.containerName = containerName;
        }
    }

    /// <summary>
    /// The converter to run in batch mode.
    /// </summary>
    /// <seealso cref="Converters.RunInBatchMode(UnityEditor.Rendering.Universal.ConverterContainerId, List{UnityEditor.Rendering.Universal.ConverterId}, UnityEditor.Rendering.Universal.ConverterFilter)"/>
    public enum ConverterId
    {
        /// <summary>
        /// Use this for the material converters.
        /// </summary>
        [BatchModeConverterInfo(ConverterContainerId.BuiltInToURP,typeof(BuiltInToURP3DMaterialUpgrader))]
        Material,

        /// <summary>
        /// Use this for the render settings converters.
        /// </summary>
        [BatchModeConverterInfo(ConverterContainerId.BuiltInToURP, typeof(RenderSettingsConverter))]
        RenderSettings,

        /// <summary>
        /// Use this for the animation clip converters.
        /// </summary>
        [BatchModeConverterInfo(ConverterContainerId.BuiltInToURP, typeof(AnimationClipConverter))]
        AnimationClip,

        /// <summary>
        /// Use this for readonly material converters.
        /// </summary>
        [BatchModeConverterInfo(ConverterContainerId.BuiltInToURP, typeof(BuiltInToURP3DReadonlyMaterialConverter))]
        ReadonlyMaterial,

        /// <summary>
        /// Use this for 2D material conversion
        /// </summary>
        [BatchModeConverterInfo(ConverterContainerId.BuiltInToURP2D, typeof(BuiltInToURP2DReadonlyMaterialConverter))]
        ReadonlyMaterial2D,

        /// <summary>
        /// Use this for 3D URP material conversion
        /// </summary>
        [BatchModeConverterInfo(ConverterContainerId.UpgradeURP2DAssets, typeof(BuiltInAndURP3DTo2DMaterialUpgrader))]
        URPToReadonlyMaterial2D,

#if PPV2_EXISTS
        /// <summary>
        /// Use this for post processing V2 converters.
        /// </summary>
        [BatchModeConverterInfo(ConverterContainerId.BuiltInToURP, typeof(PPv2Converter))]
        PPv2,
#endif

        /// <summary>
        /// Use this for parametric to freeform light converters.
        /// </summary>
        [BatchModeConverterInfo(ConverterContainerId.UpgradeURP2DAssets, typeof(ParametricToFreeformLightUpgrader))]
        ParametricToFreeformLight,
    }

    /// <summary>
    /// Class for the converter framework.
    /// </summary>
    public static class Converters
    {
        private static void DumpAvailableConverters()
        {
            StringBuilder sb = new();
            foreach (var converter in TypeCache.GetTypesDerivedFrom<IRenderPipelineConverter>())
            {
                if (converter.IsAbstract || converter.IsInterface)
                    continue;

                sb.AppendLine(converter.AssemblyQualifiedName);
            }

            Debug.Log(sb.ToString());
        }

        /// <summary>
        /// Call this method to run all the converters in a specific container in batch mode.
        /// </summary>
        /// <param name="containerName">The name of the container which will be batched. All Converters in this Container will run if prerequisites are met.</param>
        public static void RunInBatchMode(ConverterContainerId containerName)
        {
            RunInBatchMode(containerName, new List<ConverterId>() { }, ConverterFilter.Exclusive);
        }

        internal static bool TryGetTypeInContainer(ConverterId value, ConverterContainerId containerName, out Type type)
        {
            type = null;
            var memberInfo = typeof(ConverterId).GetMember(value.ToString());
            if (memberInfo.Length > 0)
            {
                var attr = memberInfo[0].GetCustomAttribute<BatchModeConverterInfo>();
                if (attr != null)
                {
                    if(attr.containerName == containerName)
                        type = attr.converterType;
                }   
            }
            return type != null;
        }

        /// <summary>
        /// Call this method to run a specific list of converters in a specific container in batch mode.
        /// </summary>
        /// <param name="containerName">The name of the container which will be batched.</param>
        /// <param name="converterList">The list of converters that will be either included or excluded from batching. These converters need to be part of the passed in container for them to run.</param>
        /// <param name="converterFilter">The enum that decide if the list of converters will be included or excluded when batching.</param>
        public static void RunInBatchMode(ConverterContainerId containerName, List<ConverterId> converterList, ConverterFilter converterFilter)
        {
            var types = FilterConverters(containerName, converterList, converterFilter);
            RunInBatchMode(types);
        }

        internal static List<Type> FilterConverters(ConverterContainerId containerName, List<ConverterId> converterList, ConverterFilter converterFilter)
        {
            Array converters = Enum.GetValues(typeof(ConverterId));

            List<Type> converterTypes = new();
            foreach (object value in converters)
            {
                var converterEnum = (ConverterId)value;
                if (TryGetTypeInContainer(converterEnum, containerName, out var type))
                {
                    bool inFilter = converterList.Contains(converterEnum);
                    if ((converterFilter == ConverterFilter.Inclusive) ^ !inFilter)
                        converterTypes.Add(type);
                }
            }

            return converterTypes;
        }

        /// <summary>
        /// Call this method to run a specific list of converters in batch mode.
        /// </summary>
        /// <param name="converterTypes">The list of converters to run</param>
        /// <returns>False if there were errors.</returns>
        internal static bool RunInBatchMode(List<Type> converterTypes)
        {
            Debug.LogWarning("Using this API can lead to incomplete or unpredictable conversion outcomes. For reliable results, please perform the conversion via the dedicated window: Window > Rendering > Render Pipeline Converter.");

            List<IRenderPipelineConverter> convertersToExecute = new();

            bool errors = false;
            foreach (var type in converterTypes)
            {
                try
                {
                    var instance = Activator.CreateInstance(type) as IRenderPipelineConverter;
                    if (instance == null)
                    {
                        Debug.LogWarning($"{type} is not a converter type.");
                        errors = true;
                    }
                    else
                        convertersToExecute.Add(instance);
                }
                catch
                {
                    Debug.LogWarning($"Unable to create instance of type {type}.");
                    errors = true;
                }
            }

            if (errors)
            {
                Debug.LogWarning($"Please use any of the given Converter Types.");
                DumpAvailableConverters();
            }

            BatchConverters(convertersToExecute);

            return !errors;
        }

        internal static void BatchConverters(List<IRenderPipelineConverter> converters)
        {
            foreach (var converter in converters)
            {
                var sb = new StringBuilder();

                converter.Scan(OnConverterCompleteDataCollection);

                void OnConverterCompleteDataCollection(List<IRenderPipelineConverterItem> items)
                {
                    converter.BeforeConvert();
                    foreach (var item in items)
                    {
                        var status = converter.Convert(item, out var message);
                        switch (status)
                        {
                            case Status.Pending:
                                throw new InvalidOperationException("Converter returned a pending status when converting. This is not supported.");
                            case Status.Error:
                            case Status.Warning:
                                sb.AppendLine($"- {item.name} ({status}) ({message})");
                                break;
                            case Status.Success:
                            {
                                sb.AppendLine($"- {item.name} ({status})");
                                message = "Conversion successful!";
                            }
                            break;
                        }
                    }
                    converter.AfterConvert();

                    var conversionResult = sb.ToString();
                    if (!string.IsNullOrEmpty(conversionResult))
                        Debug.Log(sb.ToString());
                }
            }

            AssetDatabase.SaveAssets();
        }
    }
}
