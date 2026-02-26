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

        public static IEnumerable<TestCaseData> TestCases()
        {
            yield return new TestCaseData(
                "BuiltInToURP",
                new List<string> { "Material" },
                true,
                new List<Type> { typeof(BuiltInToURP3DMaterialUpgrader) }
            ).SetName("When Using Inclusive filter with Material in the correct category. The Filter only returns that converter");
            yield return new TestCaseData(
                "BuiltInToURP",
                new List<string> { "ParametricToFreeformLight" },
                true,
                new List<Type>()
            ).SetName("When Using Inclusive filter with Light in the wrong category. The Filter returns nothing");

            yield return new TestCaseData(
                "BuiltInToURP",
                new List<string>
                {
                    "RenderSettings",
#if PPV2_EXISTS
                    "PPv2"
#endif
                },
                false,
                new List<Type>
                {
                    typeof(AnimationClipConverter),
                    typeof(BuiltInToURP3DMaterialUpgrader),
                    typeof(BuiltInToURP3DReadonlyMaterialConverter),
                }
            ).SetName("When Using Exclusive filter. The filter returns everything except the given ids");

            yield return new TestCaseData(
                "BuiltInToURP",
                new List<string>(),
                true,
                new List<Type>() // No converters match
            ).SetName("When Using Inclusive filter with no converters. The filter returns nothing");

            yield return new TestCaseData(
                "BuiltInToURP",
                new List<string>(),
                false,
                 new List<Type>
                 {
#if PPV2_EXISTS
                    typeof(PPv2Converter),
#endif
                    typeof(BuiltInToURP3DRenderSettingsConverter),
                    typeof(AnimationClipConverter),
                    typeof(BuiltInToURP3DMaterialUpgrader),
                    typeof(BuiltInToURP3DReadonlyMaterialConverter),
                 }
            ).SetName("BuiltInToURP - When Using Exclusive filter with no converters. The filter returns everything");

            yield return new TestCaseData(
                "BuiltInToURP2D",
                new List<string>(),
                false,
                 new List<Type>
                 {
                    typeof(BuiltInToURP2DRenderSettingsConverter),
                    typeof(BuiltInToURP2DReadonlyMaterialConverter),
                 }
            ).SetName("BuiltInToURP2D - When Using Exclusive filter with no converters. The filter returns everything");

            yield return new TestCaseData(
                "UpgradeURP2DAssets",
                new List<string>(),
                false,
                new List<Type>
                {
                    typeof(BuiltInAndURP3DTo2DMaterialUpgrader),
                    typeof(ParametricToFreeformLightUpgrader)
                }
            ).SetName("UpgradeURP2DAssets - When Using Exclusive filter with no converters. The filter returns everything");
        }

        [TestCaseSource(nameof(TestCases))]
        public void FilterConverters_ShouldReturnExpectedConverters(
            string containerName,
            List<string> filterList,
            bool filterModeIsInclusive,
            List<Type> expectedTypes)
        {
            var actualTypes = Converters.FilterConverters(containerName, filterList, filterModeIsInclusive);
            CollectionAssert.AreEquivalent(expectedTypes, actualTypes);
        }

        [Test]
        public void CommandLine_ArgumentsParsedProperly()
        {
            // case 1 - working command
            var dummyArgs = "-batchmode -executeMethod UnityEditor.Rendering.Universal.Converters.RunInBatchModeCmdLine --flagA -paramA 1 2 3 --flagB --flagC -paramB --flagD";
            var actualArgs = Converters.ParseArgs(dummyArgs.Split(' '));
            Dictionary<string, List<string>> expectedArgs = new()
            {
                { "-executeMethod", new List<string> {"UnityEditor.Rendering.Universal.Converters.RunInBatchModeCmdLine"} },
                { "-paramA", new List<string> {"1", "2", "3"} },
                { "-paramB", new List<string> {} },
                { "Flags", new List<string> {"--flagA", "--flagB", "--flagC", "--flagD"} }
            };
            CollectionAssert.AreEquivalent(expectedArgs, actualArgs);

            // case 2 - Error: No -batchmode flag
            dummyArgs = "-executeMethod UnityEditor.Rendering.Universal.Converters.RunInBatchModeCmdLine --flagA -paramA 1 2 3";
            var exception = Assert.Throws<ArgumentException>(() => Converters.ParseArgs(dummyArgs.Split(' ')));
            Assert.That(exception.Message, Is.EqualTo("No -batchmode argument found. Exiting."));

            // case 3 - Error: Adding values without a key
            dummyArgs = "-batchmode -executeMethod UnityEditor.Rendering.Universal.Converters.RunInBatchModeCmdLine --flagA wrongArgument";
            exception = Assert.Throws<ArgumentException>(() => Converters.ParseArgs(dummyArgs.Split(' ')));
            Assert.That(exception.Message, Is.EqualTo("Unrecognized argument: wrongArgument"));
        }

#pragma warning disable CS0618 // Type or member is obsolete
        public static IEnumerable<TestCaseData> TestCasesDeprecated()
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
                    typeof(BuiltInToURP3DRenderSettingsConverter),
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
                    typeof(BuiltInToURP2DRenderSettingsConverter),
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

        [TestCaseSource(nameof(TestCasesDeprecated))]
        public void FilterConverters_ShouldReturnExpectedConverters_DeprecatedAPI(
                ConverterContainerId containerId,
                List<ConverterId> filterList,
                ConverterFilter filterMode,
                List<Type> expectedTypes)
        {
            var actualTypes = Converters.FilterConverters(containerId, filterList, filterMode);
            CollectionAssert.AreEquivalent(expectedTypes, actualTypes);
        }

        [Test]
        public void CommandLine_SuggestCorrectCommands()
        {
            var expectedOutputTemplate = "The method you're trying to use is deprecated. Try running the following command in the command line:\n{0}";

            // case 1
            var containerID = ConverterContainerId.BuiltInToURP;
            var filterList = new List<ConverterId> { ConverterId.Material, ConverterId.RenderSettings };
            Converters.SuggestUpdatedCommand(containerID.ToString(), filterList.ConvertAll(id => id.ToString()), true);
            var suggestedCommand = "<path to Unity> -projectPath <path to project> -batchmode -executeMethod UnityEditor.Rendering.Universal.Converters.RunInBatchModeCmdLine --inclusive -container BuiltInToURP -typesFilter Material RenderSettings";
            LogAssert.Expect(LogType.Log, String.Format(expectedOutputTemplate, suggestedCommand));

            // case 2
            containerID = ConverterContainerId.BuiltInToURP2D;
            filterList = new List<ConverterId> { };
            Converters.SuggestUpdatedCommand(containerID.ToString(), filterList.ConvertAll(id => id.ToString()), false);
            suggestedCommand = "<path to Unity> -projectPath <path to project> -batchmode -executeMethod UnityEditor.Rendering.Universal.Converters.RunInBatchModeCmdLine --exclusive -container BuiltInToURP2D";
            LogAssert.Expect(LogType.Log, String.Format(expectedOutputTemplate, suggestedCommand));
        }

#pragma warning restore CS0618 // Type or member is obsolete
    }

}
