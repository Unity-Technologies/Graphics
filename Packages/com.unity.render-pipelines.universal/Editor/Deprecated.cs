using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using UnityEditor.Rendering.Converter;
using UnityEngine;

namespace UnityEditor.Rendering.Universal
{
    /// <summary>
    /// Editor script for a <c>ForwardRendererData</c> class.
    /// </summary>
    [Obsolete("ForwardRendererDataEditor has been deprecated. Use UniversalRendererDataEditor instead #from(2021.2) #breakingFrom(2021.2) (UnityUpgradable) -> UniversalRendererDataEditor", true)]
    public class ForwardRendererDataEditor : ScriptableRendererDataEditor
    {
        /// <inheritdoc />
        public override void OnInspectorGUI()
        {
            throw new NotSupportedException("ForwardRendererDataEditor has been deprecated. Use UniversalRendererDataEditor instead");
        }
    }

    static partial class EditorUtils
    {
    }

    /// <summary>
    /// Filter for the list of converters used in batch mode.
    /// </summary>
    /// <seealso cref="Converters.RunInBatchMode(UnityEditor.Rendering.Universal.ConverterContainerId, List{UnityEditor.Rendering.Universal.ConverterId}, UnityEditor.Rendering.Universal.ConverterFilter)"/>
    [Obsolete("ConverterFilter has been deprecated.", false)]

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
    [Obsolete("ConverterContainerId has been deprecated.", false)]
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
    [Obsolete("BatchModeConverterInfo has been deprecated. Please use BatchModeConverterClassInfo or upgrade using UnityEditor.Rendering.Universal.Converters.RunInBatchModeCmdLine instead.", false)]
    internal class BatchModeConverterInfo : Attribute
    {
        public Type converterType { get; }
        public ConverterContainerId containerName { get; }

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
    [Obsolete("ConverterId has been deprecated. Please upgrade using UnityEditor.Rendering.Universal.Converters.RunInBatchModeCmdLine instead.", false)]
    public enum ConverterId
    {
        /// <summary>
        /// Use this for the material converters.
        /// </summary>
        [BatchModeConverterInfo(ConverterContainerId.BuiltInToURP, typeof(BuiltInToURP3DMaterialUpgrader))]
        Material,

        /// <summary>
        /// Use this for the render settings converters.
        /// </summary>
        [BatchModeConverterInfo(ConverterContainerId.BuiltInToURP, typeof(BuiltInToURP3DRenderSettingsConverter))]
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
        [BatchModeConverterInfo(ConverterContainerId.BuiltInToURP2D,
            typeof(BuiltInToURP2DReadonlyMaterialConverter))]
        ReadonlyMaterial2D,

        /// <summary>
        /// Use this for 2D material conversion
        /// </summary>
        [BatchModeConverterInfo(ConverterContainerId.BuiltInToURP2D, typeof(BuiltInToURP2DRenderSettingsConverter))]
        RenderSettings2D,

        /// <summary>
        /// Use this for 3D URP material conversion
        /// </summary>
        [BatchModeConverterInfo(ConverterContainerId.UpgradeURP2DAssets,
            typeof(BuiltInAndURP3DTo2DMaterialUpgrader))]
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

    public static partial class Converters
    {
        /// <summary>
        /// Call this method to run all the converters in a specific container in batch mode.
        /// </summary>
        /// <param name="containerName">The name of the container which will be batched. All Converters in this Container will run if prerequisites are met.</param>
        [Obsolete("RunInBatchMode(ConverterContainerId) has been deprecated. Please use RunInBatchMode(string) or upgrade using UnityEditor.Rendering.Universal.Converters.RunInBatchModeCmdLine instead.", false)]
        public static void RunInBatchMode(ConverterContainerId containerName)
        {
            RunInBatchMode(containerName, new List<ConverterId>() { }, ConverterFilter.Exclusive);
        }

        [Obsolete("TryGetTypeInContainer has been deprecated. Please upgrade using UnityEditor.Rendering.Universal.Converters.RunInBatchModeCmdLine instead.", false)]
        static bool TryGetTypeInContainer(ConverterId value, ConverterContainerId containerName, out Type type)
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
        [Obsolete("RunInBatchMode(ConverterContainerId, List<ConverterId>, ConverterFilter) has been deprecated. Please use RunInBatchMode(string, List<string>, bool) or upgrade using UnityEditor.Rendering.Universal.Converters.RunInBatchModeCmdLine instead.", false)]
        public static void RunInBatchMode(ConverterContainerId containerName, List<ConverterId> converterList, ConverterFilter converterFilter)
        {
            var types = FilterConverters(containerName, converterList, converterFilter);
            SuggestUpdatedCommand(containerName.ToString(), converterList.ConvertAll(id => id.ToString()), converterFilter == ConverterFilter.Inclusive);
            RunInBatchMode(types);
        }

        [Obsolete("FilterConverters(ConverterContainerId, List<ConverterId>, ConverterFilter) has been deprecated. Please use FilterConverters(string, List<string>, bool) instead.", false)]
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
    }
}
