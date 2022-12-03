using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

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
        static Type GetContainerType(ConverterContainerId containerName)
        {
            switch (containerName)
            {
                case ConverterContainerId.BuiltInToURP:
                    return typeof(BuiltInToURPConverterContainer);
                case ConverterContainerId.BuiltInToURP2D:
                    return typeof(BuiltInToURP2DConverterContainer);
                case ConverterContainerId.UpgradeURP2DAssets:
                    return typeof(UpgradeURP2DAssetsContainer);
            }

            return null;
        }

        static Type GetConverterType(ConverterId converterName)
        {
            switch (converterName)
            {
                case ConverterId.Material:
                    return typeof(UniversalRenderPipelineMaterialUpgrader);
                case ConverterId.RenderSettings:
                    return typeof(RenderSettingsConverter);
                case ConverterId.AnimationClip:
                    return typeof(AnimationClipConverter);
                case ConverterId.ReadonlyMaterial:
                    return typeof(ReadonlyMaterialConverter);
#if PPV2_EXISTS
                case ConverterId.PPv2:
                    return typeof(PPv2Converter);
#endif
                case ConverterId.ParametricToFreeformLight:
                    return typeof(ParametricToFreeformLightUpgrader);
            }

            return null;
        }

        /// <summary>
        /// Call this method to run all the converters in a specific container in batch mode.
        /// </summary>
        /// <param name="containerName">The name of the container which will be batched. All Converters in this Container will run if prerequisites are met.</param>
        public static void RunInBatchMode(ConverterContainerId containerName)
        {
            Type typeName = GetContainerType(containerName);
            if (typeName != null)
            {
                RunInBatchMode(typeName);
            }
        }

        /// <summary>
        /// Call this method to run a specific list of converters in a specific container in batch mode.
        /// </summary>
        /// <param name="containerName">The name of the container which will be batched.</param>
        /// <param name="converterList">The list of converters that will be either included or excluded from batching. These converters need to be part of the passed in container for them to run.</param>
        /// <param name="converterFilter">The enum that decide if the list of converters will be included or excluded when batching.</param>
        public static void RunInBatchMode(ConverterContainerId containerName, List<ConverterId> converterList, ConverterFilter converterFilter)
        {
            Type containerType = GetContainerType(containerName);
            List<Type> converterTypes = new List<Type>(converterList.Count);
            foreach (ConverterId typeName in converterList)
            {
                var converterType = GetConverterType(typeName);
                if (containerType != null && !converterTypes.Contains(converterType))
                {
                    converterTypes.Add(converterType);
                }
            }

            if (containerType != null && converterTypes.Any())
            {
                RunInBatchMode(containerType, converterTypes, converterFilter);
            }
        }

        internal static void RunInBatchMode(Type containerName, List<Type> converterList, ConverterFilter converterFilter)
        {
            Debug.Log($"Converter Batch Mode: {containerName}");
            var container = (RenderPipelineConverterContainer)Activator.CreateInstance(containerName);
            List<RenderPipelineConverter> converters = GetConvertersInContainer(container);

            List<RenderPipelineConverter> convertersToBatch = new List<RenderPipelineConverter>(converters.Count);
            // This is just a temp to deal with the Include and Exclude enum
            List<RenderPipelineConverter> tempConvertersToBatch = new List<RenderPipelineConverter>(converters.Count);

            if (converterFilter == ConverterFilter.Inclusive)
            {
                foreach (RenderPipelineConverter converter in converters)
                {
                    if (converterList.Contains(converter.GetType()))
                    {
                        tempConvertersToBatch.Add(converter);
                    }
                }
            }
            else if (converterFilter == ConverterFilter.Exclusive)
            {
                tempConvertersToBatch = converters;
                foreach (RenderPipelineConverter converter in converters)
                {
                    if (converterList.Contains(converter.GetType()))
                    {
                        tempConvertersToBatch.Remove(converter);
                    }
                }
            }

            convertersToBatch = tempConvertersToBatch;
            BatchConverters(convertersToBatch);
        }

        /// <summary>
        /// The method that will be run when converting the assets in batch mode.
        /// </summary>
        /// <param name="containerName">The name of the container which will be batched.</param>
        internal static void RunInBatchMode(Type containerName)
        {
            List<RenderPipelineConverter> converters = new List<RenderPipelineConverter>();
            var containers = TypeCache.GetTypesDerivedFrom<RenderPipelineConverterContainer>();
            foreach (var containerType in containers)
            {
                if (containerType == containerName)
                {
                    var container = (RenderPipelineConverterContainer)Activator.CreateInstance(containerType);
                    converters = GetConvertersInContainer(container);
                }
            }

            BatchConverters(converters);
        }

        internal static void BatchConverters(List<RenderPipelineConverter> converters)
        {
            // This need to be sorted by Priority property
            converters = converters.OrderBy(o => o.priority).ToList();

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

        internal static List<RenderPipelineConverter> GetConvertersInContainer(RenderPipelineConverterContainer container)
        {
            List<RenderPipelineConverter> listOfConverters = new List<RenderPipelineConverter>();
            var converterList = TypeCache.GetTypesDerivedFrom<RenderPipelineConverter>();

            for (int i = 0; i < converterList.Count; ++i)
            {
                // Iterate over the converters that are used by the current container
                RenderPipelineConverter conv = (RenderPipelineConverter)Activator.CreateInstance(converterList[i]);
                if (conv.container == container.GetType())
                {
                    listOfConverters.Add(conv);
                }
            }

            return listOfConverters;
        }
    }
}
