using System;
using System.Collections.Generic;
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
                    //Debug.Log($"Found converter {conv.name} in {container.name}");
                    listOfConverters.Add(conv);
                }
            }

            return listOfConverters;
        }

        public static void RunInBatchMode(string containerName, List<string> converterList, ConverterFilter converterFilter)
        {
            List<RenderPipelineConverter> convertersToBatch = new List<RenderPipelineConverter>();
            // Get all containers
            var containers = TypeCache.GetTypesDerivedFrom<RenderPipelineConverterContainer>();
            foreach (var containerType in containers)
            {
                // Create container to get
                if (containerType.FullName == containerName)
                {
                    var container = (RenderPipelineConverterContainer)Activator.CreateInstance(containerType);
                    List<RenderPipelineConverter> converters = GetConvertersInContainer(container);
                    foreach (RenderPipelineConverter converter in converters)
                    {




                        //Debug.Log($"Fullname: {converter.GetType().FullName}");
                        if (converterList.Contains(converter.GetType().FullName))
                        {
                            if (converterFilter == ConverterFilter.Inclusive)
                            {
                                convertersToBatch.Add(converter);

                                //Debug.Log("BATCHING:: " + converter.name);
                                //converter.OnRunInBatchMode();
                            }
                            else
                            {
                                Debug.Log("WILL NOT BATCH:: " + converter.name);
                            }
                        }

                    }
                }
            }
            BatchConverters(convertersToBatch);
            // Get all the containers
            // Select the correct container ( option when calling this method )
            // Get all the converters
            // Run the converters that has implemented Batch method ( or maybe again have a named option when calling this method )
            // Use the full typename
        }

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
            // Get all the containers
            // Select the correct container ( option when calling this method )
            // Get all the converters
            // Run the converters that has implemented Batch method ( or maybe again have a named option when calling this method )
            // Use the full typename
        }

        private static void BatchConverters(List<RenderPipelineConverter> converters)
        {
            foreach (RenderPipelineConverter converter in converters)
            {
                Debug.Log($"Batching {converter.name}");
                List<ConverterItemDescriptor> converterItemInfos = new List<ConverterItemDescriptor>();
                var initCtx = new InitializeConverterContext { items = converterItemInfos };
                converter.OnInitialize(initCtx, () => { });

                converter.OnPreRun();
                //foreach (ConverterItemDescriptor initCtxItem in initCtx.items)
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
