using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine.TestTools;
using UnityEngine;

namespace UnityEditor.Rendering.Universal.Tools
{
    [Category("Graphics Tools")]
    class ConverterTests
    {
        [Serializable]
        class BatchModeConverter : RenderPipelineConverter
        {
            public static bool s_Initialize = false;
            public static bool s_Run = false;

            public override void OnInitialize(InitializeConverterContext context, Action callback)
            {
                s_Initialize = true;
                context.AddAssetToConvert(new ConverterItemDescriptor { name = "Dummy", info = "Some placeholder info." });
                callback?.Invoke();
            }

            public override void OnRun(ref RunItemContext context)
            {
                s_Run = true;
            }
        }

        [Test]
        public void BatchModeRuns()
        {
            BatchModeConverter.s_Initialize = false;
            BatchModeConverter.s_Run = false;
            bool ok = Converters.RunInBatchMode(new List<Type>() { typeof(BatchModeConverter) });
            Assert.IsTrue(BatchModeConverter.s_Initialize);
            Assert.IsTrue(BatchModeConverter.s_Run);
            Assert.IsTrue(ok);
        }

        class NotAConverterType
        {

        }

        [Test]
        public void BatchModeFails()
        {
            bool ok = Converters.RunInBatchMode(new List<Type>() { typeof(NotAConverterType) });
            Assert.IsFalse(ok);
        }

        [Test]
        public void RunInBatchMode_LogsUsageWarning()
        {
            LogAssert.Expect(
                LogType.Warning,
                "Using this API can lead to incomplete or unpredictable conversion outcomes. " +
                "For reliable results, please perform the conversion via the dedicated window: " +
                "Window > Rendering > Render Pipeline Converter."
            );

            bool _ = Converters.RunInBatchMode(new List<Type>() {});
        }

#pragma warning disable CS0618 // Type or member is obsolete
        public static IEnumerable<TestCaseData> TestCases()
        {
            yield return new TestCaseData(
                ConverterContainerId.BuiltInToURP,
                new List<ConverterId> { ConverterId.Material },
                ConverterFilter.Inclusive,
                new List<Type> { typeof(BuiltInToURP3DMaterialUpgrader) }
            ).SetName("When Using Inclusive filter with Material in the correct category. The Filter only returns that converter");
            yield return new TestCaseData(
                ConverterContainerId.BuiltInToURP,
                new List<ConverterId> { ConverterId.ParametricToFreeformLight },
                ConverterFilter.Inclusive,
                new List<Type>()
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
                    typeof(BuiltInToURP3DMaterialUpgrader),
                    typeof(BuiltInToURP3DReadonlyMaterialConverter),
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
                    typeof(BuiltInToURP3DMaterialUpgrader),
                    typeof(BuiltInToURP3DReadonlyMaterialConverter),
                 }
            ).SetName("BuiltInToURP - When Using Exclusive filter with no converters. The filter returns everything");

            yield return new TestCaseData(
                ConverterContainerId.BuiltInToURP2D,
                new List<ConverterId>(),
                ConverterFilter.Exclusive,
                 new List<Type>
                 {
                    typeof(BuiltInToURP2DReadonlyMaterialConverter),
                 }
            ).SetName("BuiltInToURP2D - When Using Exclusive filter with no converters. The filter returns everything");

            yield return new TestCaseData(
                ConverterContainerId.UpgradeURP2DAssets,
                new List<ConverterId>(),
                ConverterFilter.Exclusive,
                new List<Type>
                {
                    typeof(BuiltInAndURP3DTo2DMaterialUpgrader),
                    typeof(ParametricToFreeformLightUpgrader)
                }
            ).SetName("UpgradeURP2DAssets - When Using Exclusive filter with no converters. The filter returns everything");
        }

        [TestCaseSource(nameof(TestCases))]
        public void FilterConverters_ShouldReturnExpectedConverters(
                ConverterContainerId containerId,
                List<ConverterId> filterList,
                ConverterFilter filterMode,
                List<Type> expectedTypes)
        {
            var actualTypes = Converters.FilterConverters(containerId, filterList, filterMode);
            CollectionAssert.AreEquivalent(expectedTypes, actualTypes);
        }

#pragma warning restore CS0618 // Type or member is obsolete
    }

}
