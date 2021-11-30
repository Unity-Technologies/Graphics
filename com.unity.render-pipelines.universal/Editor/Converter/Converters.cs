using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UnityEditor.Rendering.Universal
{
    public static class Converters
    {
        /// <summary>
        /// Used for Converting in Batch Mode.
        /// </summary>
        public enum Filter
        {
            Inclusive,
            Exclusive
        }


        public enum ContainerType
        {
            BuiltInToURP,
            BuiltInToURP2D,
            UpgradeURP2DAssets,
        }

        public enum TypeName
        {
            Material,
            RenderSettings,
            AnimationClip,
            ReadonlyMaterial,
            PPv2,
            ParametricToFreeformLight,
        }

        static string GetContainerTypeName(ContainerType containerName)
        {
            string returnContainerName = "";
            switch (containerName)
            {
                case ContainerType.BuiltInToURP:
                    returnContainerName = "UnityEditor.Rendering.Universal.BuiltInToURPConverterContainer";
                    break;
                case ContainerType.BuiltInToURP2D:
                    returnContainerName = "UnityEditor.Rendering.Universal.BuiltInToURP2DConverterContainer";
                    break;
                case ContainerType.UpgradeURP2DAssets:
                    returnContainerName = "UnityEditor.Rendering.Universal.UpgradeURP2DAssetsContainer";
                    break;
            }

            return returnContainerName;
        }

        static string GetConverterTypeName(TypeName converterName)
        {
            string returnConverterName = "";
            switch (converterName)
            {
                case TypeName.Material:
                    returnConverterName = "UnityEditor.Rendering.Universal.UniversalRenderPipelineMaterialUpgrader";
                    break;
                case TypeName.RenderSettings:
                    returnConverterName = "UnityEditor.Rendering.Universal.RenderSettingsConverter";
                    break;
                case TypeName.AnimationClip:
                    returnConverterName = "UnityEditor.Rendering.Universal.AnimationClipConverter";
                    break;
                case TypeName.ReadonlyMaterial:
                    returnConverterName = "UnityEditor.Rendering.Universal.ReadonlyMaterialConverter";
                    break;
                case TypeName.PPv2:
                    returnConverterName = "UnityEditor.Rendering.Universal.PPv2Converter";
                    break;
                case TypeName.ParametricToFreeformLight:
                    returnConverterName = "UnityEditor.Rendering.Universal.ParametricToFreeformLightUpgrader";
                    break;
            }

            return returnConverterName;
        }

        public static void RunInBatchMode(ContainerType containerName)
        {
            string typeName = GetContainerTypeName(containerName);
            if (!string.IsNullOrEmpty(typeName))
            {
                RunInBatchMode(typeName);
            }
        }

        public static void RunInBatchMode(ContainerType containerName, List<TypeName> converterList, Filter converterFilter)
        {
            string containerTypeName = GetContainerTypeName(containerName);
            List<string> converterNames = new List<string>();
            foreach (TypeName typeName in converterList)
            {
                converterNames.Add(GetConverterTypeName(typeName));
            }

            RunInBatchMode(containerTypeName, converterNames, converterFilter);
        }

        /// <summary>
        /// The method that will be run when converting the assets in batch mode.
        /// </summary>
        /// <param name="containerName">The name of the container which will be batched.</param>
        /// <param name="converterList">The list of converters that will be either included or excluded from batching. String is the full typename.</param>
        /// <param name="converterFilter">The enum that decide if the list of converters will be included or excluded when batching.</param>
        internal static void RunInBatchMode(string containerName, List<string> converterList, Filter converterFilter)
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
                    Debug.Log($"Batch Mode:\nContainer named: {containerName}");
                    var container = (RenderPipelineConverterContainer)Activator.CreateInstance(containerType);
                    List<RenderPipelineConverter> converters = GetConvertersInContainer(container);

                    if (converterFilter == Filter.Inclusive)
                    {
                        foreach (RenderPipelineConverter converter in converters)
                        {
                            if (converterList.Contains(converter.GetType().FullName))
                            {
                                tempConvertersToBatch.Add(converter);
                            }
                        }
                    }
                    else if (converterFilter == Filter.Exclusive)
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
        internal static void RunInBatchMode(string containerName)
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
