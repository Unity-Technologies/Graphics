using System;
using System.Collections.Generic;
using NUnit.Framework;
using static UnityEditor.Rendering.Universal.Converters;

namespace UnityEditor.Rendering.Universal.Tools
{
    [Category("Graphics Tools")]
    class ConverterTests
    {
        public static IEnumerable<TestCaseData> TestCases()
        {
            yield return new TestCaseData(
                ConverterContainerId.BuiltInToURP,
                new List<ConverterId> { ConverterId.Material },
                ConverterFilter.Inclusive,
                new List<Type> { typeof(UniversalRenderPipelineMaterialUpgrader) }
            ).SetName("When Using Inclusive filter with Material in the correct category. The Filter only returns that converter");
            yield return new TestCaseData(
                ConverterContainerId.BuiltInToURP,
                new List<ConverterId> { ConverterId.ParametricToFreeformLight },
                ConverterFilter.Inclusive,
                new List<Type> ()
            ).SetName("When Using Inclusive filter with Light in the wrong category. The Filter returns nothing");

            yield return new TestCaseData(
                ConverterContainerId.BuiltInToURP,
                new List<ConverterId>
                {
                    ConverterId.RenderSettings,
#if PPV2_EXISTS
                    ConverterId.PPv2
#endif
                },
                ConverterFilter.Exclusive,
                new List<Type>
                {
                    typeof(AnimationClipConverter),
                    typeof(UniversalRenderPipelineMaterialUpgrader),
                    typeof(ReadonlyMaterialConverter),
                }
            ).SetName("When Using Exclusive filter. The filter returns everything except the given ids");

            yield return new TestCaseData(
                ConverterContainerId.BuiltInToURP,
                new List<ConverterId>(),
                ConverterFilter.Inclusive,
                new List<Type>() // No converters match
            ).SetName("When Using Inclusive filter with no converters. The filter returns nothing");

            yield return new TestCaseData(
                ConverterContainerId.BuiltInToURP,
                new List<ConverterId>(),
                ConverterFilter.Exclusive,
                 new List<Type>
                 {
#if PPV2_EXISTS
                    typeof(PPv2Converter),
#endif
                    typeof(RenderSettingsConverter),
                    typeof(AnimationClipConverter),
                    typeof(UniversalRenderPipelineMaterialUpgrader),
                    typeof(ReadonlyMaterialConverter),
                 }
            ).SetName("BuiltInToURP - When Using Exclusive filter with no converters. The filter returns everything");

            yield return new TestCaseData(
                ConverterContainerId.BuiltInToURP2D,
                new List<ConverterId>(),
                ConverterFilter.Exclusive,
                 new List<Type>
                 {
                    typeof(BuiltInToURP2DMaterialUpgrader),
                 }
            ).SetName("BuiltInToURP2D - When Using Exclusive filter with no converters. The filter returns everything");

            yield return new TestCaseData(
                ConverterContainerId.BuiltInAndURPToURP2D,
                new List<ConverterId>(),
                ConverterFilter.Exclusive,
                 new List<Type>
                 {
                    typeof(BuiltInAndURP3DTo2DMaterialUpgrader),
                 }
            ).SetName("BuiltInAndURPToURP2D - When Using Exclusive filter with no converters. The filter returns everything");

            yield return new TestCaseData(
               ConverterContainerId.UpgradeURP2DAssets,
               new List<ConverterId>(),
               ConverterFilter.Exclusive,
                new List<Type>
                {
                    typeof(ParametricToFreeformLightUpgrader)
                }
           ).SetName("UpgradeURP2DAssets - When Using Exclusive filter with no converters. The filter returns everything")
           .Ignore("Temporarily disabled because of 2D pixel perfect upgrader");
        }

        [TestCaseSource(nameof(TestCases))]
        public void FilterConverters_ShouldReturnExpectedConverters(
                ConverterContainerId containerId,
                List<ConverterId> filterList,
                ConverterFilter filterMode,
                List<Type> expectedTypes)
        {
            var result = Converters.FilterConverters(containerId, filterList, filterMode);

            var actualTypes = new List<Type>();
            foreach (var converter in result)
            {
                actualTypes.Add(converter.GetType());
            }

            CollectionAssert.AreEquivalent(expectedTypes, actualTypes);
        }

        [Test]
        [Ignore("Temporarily disabled because of 2D pixel perfect upgrader")]
        public void EnsureConverterIsNotForgottenForBatchMode()
        {
            var converterMap = new ConverterTypeMap();
            foreach (var converter in TypeCache.GetTypesDerivedFrom<RenderPipelineConverter>())
            {
                if (converter.IsAbstract || converter.IsInterface)
                    continue;

                var id = converterMap.GetIdForType(converter);
                Assert.IsNotNull(id, $"The converter '{converter.Name}' is missing from the ConverterTypeMap. Please add it to the mapping array.");
            }

            var converterContainerMap = new ConverterContainerTypeMap();
            foreach (var converterContainer in TypeCache.GetTypesDerivedFrom<RenderPipelineConverterContainer>())
            {
                if (converterContainer.IsAbstract || converterContainer.IsInterface)
                    continue;

                var id = converterContainerMap.GetIdForType(converterContainer);
                Assert.IsNotNull(id, $"The converter container '{converterContainer.Name}' is missing from the ConverterContainerTypeMap. Please add it to the mapping array.");
            }
        }

    }

}
