using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

namespace UnityEditor.Rendering.Universal
{
    /// <summary>
    /// Filter for the list of converters used in batch mode.
    /// </summary>
    /// <seealso cref="Converters.RunInBatchMode(UnityEditor.Rendering.Universal.ConverterContainerId, List{UnityEditor.Rendering.Universal.ConverterId}, UnityEditor.Rendering.Universal.ConverterFilter)"/>.)
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
    /// <seealso cref="Converters.RunInBatchMode(UnityEditor.Rendering.Universal.ConverterContainerId)"/>.)
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

    /// <summary>
    /// The converter to run in batch mode.
    /// </summary>
    /// <seealso cref="Converters.RunInBatchMode(UnityEditor.Rendering.Universal.ConverterContainerId, List{UnityEditor.Rendering.Universal.ConverterId}, UnityEditor.Rendering.Universal.ConverterFilter)"/>.)
    public enum ConverterId
    {
        /// <summary>
        /// Use this for the material converters.
        /// </summary>
        Material,

        /// <summary>
        /// Use this for the render settings converters.
        /// </summary>
        RenderSettings,

        /// <summary>
        /// Use this for the animation clip converters.
        /// </summary>
        AnimationClip,

        /// <summary>
        /// Use this for readonly material converters.
        /// </summary>
        ReadonlyMaterial,

        /// <summary>
        /// Use this for 2D material conversion
        /// </summary>
        ReadonlyMaterial2D,

        /// <summary>
        /// Use this for 3D URP material conversion
        /// </summary>
        URPToReadonlyMaterial2D,

        /// <summary>
        /// Use this for post processing V2 converters.
        /// </summary>
        PPv2,

        /// <summary>
        /// Use this for parametric to freeform light converters.
        /// </summary>
        ParametricToFreeformLight,
    }

    /// <summary>
    /// Class for the converter framework.
    /// </summary>
    public static class Converters
    {
        internal abstract class EnumTypeMap<TEnum>
            where TEnum : struct, Enum
        {
            protected abstract (TEnum id, Type type)[] Map { get; }

            public Type GetTypeForId(TEnum id)
            {
                for (int i = 0; i < Map.Length; i++)
                {
                    if (Map[i].id.Equals(id))
                        return Map[i].type;
                }
                return null;
            }

            public TEnum? GetIdForType(Type type)
            {
                for (int i = 0; i < Map.Length; i++)
                {
                    if (Map[i].type == type)
                        return Map[i].id;
                }
                return null;
            }
        }

        internal class ConverterTypeMap : EnumTypeMap<ConverterId>
        {
            protected override (ConverterId id, Type type)[] Map { get; } =
            {
                (ConverterId.Material, typeof(BuiltInToURP3DMaterialUpgrader)),
                (ConverterId.RenderSettings, typeof(RenderSettingsConverter)),
                (ConverterId.AnimationClip, typeof(AnimationClipConverter)),
                (ConverterId.ReadonlyMaterial, typeof(ReadonlyMaterialConverter)),
                (ConverterId.ReadonlyMaterial2D, typeof(BuiltInToURP2DMaterialUpgrader)),
                (ConverterId.URPToReadonlyMaterial2D, typeof(BuiltInAndURP3DTo2DMaterialUpgrader)),
        #if PPV2_EXISTS
                (ConverterId.PPv2, typeof(PPv2Converter)),
        #endif
                (ConverterId.ParametricToFreeformLight, typeof(ParametricToFreeformLightUpgrader)),
            };
        }

        internal class ConverterContainerTypeMap : EnumTypeMap<ConverterContainerId>
        {
            protected override (ConverterContainerId id, Type type)[] Map { get; } =
            {
                (ConverterContainerId.BuiltInToURP, typeof(BuiltInToURPConverterContainer)),
                (ConverterContainerId.BuiltInToURP2D, typeof(BuiltInToURP2DConverterContainer)),
                (ConverterContainerId.BuiltInAndURPToURP2D, typeof(BuiltInAndURP3DTo2DConverterContainer)),
                (ConverterContainerId.UpgradeURP2DAssets, typeof(UpgradeURP2DAssetsContainer)),
            };
        }

        /// <summary>
        /// Call this method to run all the converters in a specific container in batch mode.
        /// </summary>
        /// <param name="containerName">The name of the container which will be batched. All Converters in this Container will run if prerequisites are met.</param>
        public static void RunInBatchMode(ConverterContainerId containerName)
        {
            Array enumValues = Enum.GetValues(typeof(ConverterId));
            List<ConverterId> converterList = new List<ConverterId>();

            foreach (object value in enumValues)
            {
                converterList.Add((ConverterId)value);
            }

            RunInBatchMode(containerName, converterList, ConverterFilter.Inclusive); 
        }

        /// <summary>
        /// Call this method to run a specific list of converters in a specific container in batch mode.
        /// </summary>
        /// <param name="containerName">The name of the container which will be batched.</param>
        /// <param name="converterList">The list of converters that will be either included or excluded from batching. These converters need to be part of the passed in container for them to run.</param>
        /// <param name="converterFilter">The enum that decide if the list of converters will be included or excluded when batching.</param>
        public static void RunInBatchMode(ConverterContainerId containerName, List<ConverterId> converterList, ConverterFilter converterFilter)
        {
            BatchConverters(FilterConverters(containerName, converterList, converterFilter));
        }

        internal static List<RenderPipelineConverter> FilterConverters(ConverterContainerId containerName, List<ConverterId> converterList, ConverterFilter converterFilter)
        {
            var converterContainerMap = new ConverterContainerTypeMap();
            var containerID = converterContainerMap.GetTypeForId(containerName);
            if (containerID == null)
                throw new KeyNotFoundException($"Container ID '{containerName}' not found.");

            using (HashSetPool<Type>.Get(out var tmpConverterFilter))
            {
                var converterMap = new ConverterTypeMap();
                foreach (var converterID in converterList)
                {
                    var converterType = converterMap.GetTypeForId(converterID);
                    if (converterType == null)
                        throw new KeyNotFoundException($"Container Type '{converterType}' not found.");
                    tmpConverterFilter.Add(converterType);
                }

                List<RenderPipelineConverter> convertersToExecute = new List<RenderPipelineConverter>();
                foreach (var converter in TypeCache.GetTypesDerivedFrom<RenderPipelineConverter>())
                {
                    if (converter.IsAbstract || converter.IsInterface)
                        continue;

                    // If Inclusive and inFilter is true will add the converter
                    // If Exclusive and inFilter is false will add the converter
                    bool inFilter = tmpConverterFilter.Contains(converter);
                    if ((converterFilter == ConverterFilter.Inclusive) ^ !inFilter)
                    {
                        var instance = Activator.CreateInstance(converter) as RenderPipelineConverter;
                        if (instance.container == containerID)
                            convertersToExecute.Add(instance);
                    }
                }

                return convertersToExecute;
            }
        }

        internal static void BatchConverters(List<RenderPipelineConverter> converters)
        {
            foreach (RenderPipelineConverter converter in converters)
            {
                List<ConverterItemDescriptor> converterItemInfos = new List<ConverterItemDescriptor>();
                var initCtx = new InitializeConverterContext { items = converterItemInfos };
                initCtx.isBatchMode = true;
                converter.OnInitialize(initCtx, () => { });

                converter.OnPreRun();
                for (int i = 0; i < initCtx.items.Count; i++)
                {
                    var item = new ConverterItemInfo()
                    {
                        index = i,
                        descriptor = initCtx.items[i],
                    };
                    var ctx = new RunItemContext(item);
                    ctx.isBatchMode = true;
                    converter.OnRun(ref ctx);

                    string converterStatus = ctx.didFail ? $"Fail\nInfo: {ctx.info}" : "Pass";
                    Debug.Log($"Name: {ctx.item.descriptor.name}\nConverter Status: {converterStatus}");
                }

                converter.OnPostRun();

                AssetDatabase.SaveAssets();
            }
        }
    }
}
