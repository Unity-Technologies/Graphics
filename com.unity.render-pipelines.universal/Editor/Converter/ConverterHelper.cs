using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UnityEditor.Rendering.Universal.Converters
{
    public class ConverterHelper
    {
        public enum ConverterFilter
        {
            Inclusive,
            Exclusive
        }

        static List<RenderPipelineConverter> GetConvertersInContainer(RenderPipelineConverterContainer container)
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

        /// <summary>
        /// The method that will be run when converting the assets in batch mode.
        /// </summary>
        /// <param name="containerName">The name of the container which will be batched.</param>
        /// <param name="converterList">The list of converters that will be either included or excluded from batching. String is the full typename.</param>
        /// <param name="converterFilter">The enum that decide if the list of converters will be included or excluded when batching.</param>
        public static void RunInBatchMode(string containerName, List<string> converterList, ConverterFilter converterFilter)
        {
            List<RenderPipelineConverter> convertersToBatch = new List<RenderPipelineConverter>();
            // This is just a temp to deal with the Include and Exclude enum
            List<RenderPipelineConverter> tempConvertersToBatch = new List<RenderPipelineConverter>();
            // Get all containers
            var containers = TypeCache.GetTypesDerivedFrom<RenderPipelineConverterContainer>();
            foreach (var containerType in containers)
            {
                // Create container to get
                if (containerType.FullName == containerName)
                {
                    var container = (RenderPipelineConverterContainer)Activator.CreateInstance(containerType);
                    List<RenderPipelineConverter> converters = GetConvertersInContainer(container);

                    if (converterFilter == ConverterFilter.Inclusive)
                    {
                        foreach (RenderPipelineConverter converter in converters)
                        {
                            if (converterList.Contains(converter.GetType().FullName))
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
                            if (converterList.Contains(converter.GetType().FullName))
                            {
                                tempConvertersToBatch.Remove(converter);
                            }
                        }
                    }
                    break;
                }
            }

            convertersToBatch = tempConvertersToBatch;
            BatchConverters(convertersToBatch);
        }

        /// <summary>
        /// The method that will be run when converting the assets in batch mode.
        /// </summary>
        /// <param name="containerName">The name of the container which will be batched.</param>
        public static void RunInBatchMode(string containerName)
        {
            List<RenderPipelineConverter> converters = new List<RenderPipelineConverter>();
            var containers = TypeCache.GetTypesDerivedFrom<RenderPipelineConverterContainer>();
            foreach (var containerType in containers)
            {
                if (containerType.FullName == containerName)
                {
                    var container = (RenderPipelineConverterContainer)Activator.CreateInstance(containerType);
                    converters = GetConvertersInContainer(container);
                }
            }

            BatchConverters(converters);
        }

        private static void BatchConverters(List<RenderPipelineConverter> converters)
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
                }

                converter.OnPostRun();

                AssetDatabase.SaveAssets();
            }
        }
    }
}
